using System;
using System.Windows;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.LogWatching;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void RestartLogTailer()
        {
            if (_settingsRepository == null) return;

            if (_logTailer != null)
            {
                _logTailer.Stop();
                _logTailer.LineAppended -= OnLogLineAppended;
                _logTailer.ErrorEncountered -= OnLogTailerError;
                _logTailer = null;
            }

            string apiKey = _settingsRepository.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_real_hypixel_api_key_here", StringComparison.OrdinalIgnoreCase))
            {
                _showingConfigNeeded = true;
                SetStatus(LocalizationService.Instance["Status_ConfigureApi"]);
                return;
            }

            if (_showingConfigNeeded)
            {
                _showingConfigNeeded = false;
                SetStatus(LocalizationService.Instance["Status_WaitingWho"]);
            }

            try
            {
                string logPath = _settingsRepository.LogFilePath;
                if (!System.IO.File.Exists(logPath))
                {
                    SetStatus(LocalizationService.Instance["Status_LogNotFound"]);
                    AddDiagnosticLine($"[ERROR] Log file not found: {logPath}");
                    return;
                }

                _logParser = new HypixelLogParser();
                _logTailer = new LogTailer(logPath);

                _logTailer.LineAppended += OnLogLineAppended;
                _logTailer.ErrorEncountered += OnLogTailerError;
                _logTailer.Start();

                SetStatus(LocalizationService.Instance["Status_WaitingWho"]);
                AddDiagnosticLine($"Log watch started on: {logPath}");
            }
            catch (Exception ex)
            {
                SetStatus(LocalizationService.Instance["Status_CannotWatchLog"]);
                AddDiagnosticLine($"[ERROR] Failed to start log watcher: {ex.Message}");
            }
        }

        private void OnLogLineAppended(string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDiagnosticLine(line);

                if (_logParser?.IsPartyListHeader(line) == true)
                {
                    _partyPlayers.Clear();
                    return;
                }

                if (_logParser?.IsHelpCommand(line) == true)
                {
                    ShowCommandHelp();
                    return;
                }

                if (_logParser?.IsClearCommand(line) == true)
                {
                    ClearPlayers();
                    return;
                }

                var ignoredPlayers = _logParser?.ParseIgnoredPlayers(line);
                if (ignoredPlayers != null && _settingsRepository != null)
                {
                    foreach (var player in ignoredPlayers)
                    {
                        _settingsRepository.AddIgnoredPlayer(player);
                    }

                    AddDiagnosticLine($"Ignored player list updated: {string.Join(", ", ignoredPlayers)}");
                    RemoveIgnoredExistingPlayers();
                    return;
                }

                var partyPlayers = _logParser?.ParsePartyPlayers(line);
                if (partyPlayers != null)
                {
                    foreach (var player in partyPlayers)
                    {
                        _partyPlayers.Add(player);
                    }

                    AddDiagnosticLine($"Party list updated: {_partyPlayers.Count} player(s)");
                    RemoveIgnoredExistingPlayers();
                    return;
                }

                var statsCommandPlayers = _logParser?.ParseStatsCommandPlayers(line);
                if (statsCommandPlayers != null && statsCommandPlayers.Count > 0 && _playerLookupService != null)
                {
                    LoadPlayers(statsCommandPlayers);
                    return;
                }

                var players = _logParser?.ParsePlayerList(line);
                if (players != null && players.Count > 0 && _playerLookupService != null)
                {
                    players = FilterIgnoredPlayers(players);
                    LoadPlayers(players);
                }
                else
                {
                    if (_logParser?.IsGameEndSummary(line) == true)
                    {
                        _playerLookupService?.CancelAndReset();
                        _players.Clear();
                        PlaceholderPanel.Visibility = Visibility.Visible;
                        AddDiagnosticLine("Game ended. Current player list cleared.");
                        return;
                    }

                    var finalKillVictim = _logParser?.ParseFinalKillVictim(line);
                    if (!string.IsNullOrWhiteSpace(finalKillVictim))
                    {
                        for (int i = 0; i < _players.Count; i++)
                        {
                            if (_players[i].Name.Equals(finalKillVictim, StringComparison.OrdinalIgnoreCase))
                            {
                                var eliminatedPlayer = _players[i];
                                if (!eliminatedPlayer.IsEliminated)
                                {
                                    eliminatedPlayer.MarkEliminated();
                                    AddDiagnosticLine($"Marked final-killed player: {finalKillVictim}");
                                    _ = RemoveFinalKilledPlayerAfterDelayAsync(eliminatedPlayer);
                                }

                                break;
                            }
                        }
                    }
                }
            }));
        }

        private void OnLogTailerError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDiagnosticLine($"[ERROR] {error}");
            }));
        }
    }
}
