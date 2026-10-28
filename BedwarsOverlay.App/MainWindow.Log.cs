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

            _partyPlayers.Clear();

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

                if (_logParser?.IsLobbyTransition(line) == true)
                {
                    HideSplitAlert();
                    bool hadRoster = _matchRoster.IsActive;
                    _matchRoster.Reset();
                    if (hadRoster) ClearPlayers();
                }

                var gameBlock = _logParser?.ParseGameBlock(line) ?? GameBlockEvent.None;
                if (gameBlock.Kind == GameBlockKind.MatchStart)
                {
                    StartTrackedMatch();
                }
                else if (gameBlock.Kind == GameBlockKind.MatchEnd)
                {
                    HideSplitAlert();
                    _teamRosterVersion++;
                    bool closedRunningMatch = EndTrackedMatch(gameBlock.Outcome);
                    bool hadRoster = _matchRoster.IsActive;
                    _matchRoster.Reset();
                    if (closedRunningMatch || hadRoster)
                    {
                        _currentTeamByPlayer.Clear();
                        _playerLookupService?.CancelAndReset();
                        _players.Clear();
                        PlaceholderPanel.Visibility = Visibility.Visible;
                        AddDiagnosticLine(LocalizationService.Instance["Status_MatchEnded"]);
                    }

                    return;
                }

                if (_logParser?.IsPartyListHeader(line) == true)
                {
                    _partyPlayers.Clear();
                    return;
                }

                var partyEvent = _logParser?.ParsePartyEvent(line);
                if (partyEvent != null)
                {
                    if (partyEvent.Kind == PartyEventKind.OwnJoin)
                    {
                        AddDiagnosticLine($"Party joined: {partyEvent.PlayerName}");
                    }
                    else if (partyEvent.Kind == PartyEventKind.CompanionRoster)
                    {
                        AddDiagnosticLine($"Party companions updated: {partyEvent.PlayerNames?.Count ?? 0} player(s)");
                    }
                    else if (partyEvent.Kind == PartyEventKind.MemberJoined)
                    {
                        AddDiagnosticLine($"Party member joined: {partyEvent.PlayerName}");
                    }
                    else if (partyEvent.Kind == PartyEventKind.MemberLeft)
                    {
                        AddDiagnosticLine($"Party member left/removed: {partyEvent.PlayerName}");
                    }
                    else
                    {
                        AddDiagnosticLine("Party cleared.");
                    }

                    bool changedRoster = _partyPlayers.Apply(partyEvent);
                    if (changedRoster && (partyEvent.Kind == PartyEventKind.OwnJoin ||
                        partyEvent.Kind == PartyEventKind.CompanionRoster ||
                        partyEvent.Kind == PartyEventKind.MemberJoined))
                    {
                        RemoveIgnoredExistingPlayers();
                    }

                    return;
                }

                if (_logParser?.IsHelpCommand(line) == true)
                {
                    ShowCommandHelp();
                    return;
                }

                if (_logParser?.IsPartyStatsCommand(line) == true)
                {
                    if (_partyPlayers.Count == 0)
                    {
                        AddDiagnosticLine(LocalizationService.Instance["Status_PartyStatsUnavailable"]);
                        SetStatus(LocalizationService.Instance["Status_PartyStatsUnavailable"]);
                    }
                    else
                    {
                        LoadPlayers(_partyPlayers.ToList());
                        SetStatus(LocalizationService.Instance["Status_PartyStatsLoaded"].Replace("{0}", _partyPlayers.Count.ToString()));
                    }

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

                var connection = _logParser?.ParsePlayerConnection(line);
                if (connection != null)
                {
                    // Like deaths, connection messages can resume in-memory tracking after a
                    // mid-game /who. They never create an encounter match or a Split reminder.
                    if (!_matchRoster.IsActive && _players.Count > 0) _matchRoster.Activate();
                    if (_matchRoster.IsActive && _matchRoster.ObserveConnection(connection))
                    {
                        LoadPlayers(FilterIgnoredPlayers(_matchRoster.GetPlayers()), preserveExisting: true);
                        AddDiagnosticLine($"Player {connection.Name}: {(connection.IsConnected ? "reconnected" : "disconnected")}");
                    }
                    return;
                }

                var players = _logParser?.ParsePlayerList(line);
                if (players != null && players.Count > 0 && _playerLookupService != null)
                {
                    players = FilterIgnoredPlayers(_matchRoster.ObservePlayers(players));
                    LoadPlayers(players, preserveExisting: _matchRoster.IsActive);
                }
                else
                {
                    var death = _logParser?.ParsePlayerDeath(line);
                    if (death != null && !_matchRoster.IsActive && _players.Count > 0)
                    {
                        _matchRoster.Activate();
                    }
                    if (death != null && _matchRoster.IsActive)
                    {
                        ApplyLogTeam(death.Victim, death.VictimTeam);
                        if (death.Killer != null) ApplyLogTeam(death.Killer, death.KillerTeam);
                        RemoveIgnoredExistingPlayers();
                        HandleSplitAlert(death);
                        LoadPlayers(FilterIgnoredPlayers(_matchRoster.ObserveDeath(death)), preserveExisting: true);
                    }

                    var finalKillVictim = _logParser?.ParseFinalKillVictim(line);
                    if (!string.IsNullOrWhiteSpace(finalKillVictim))
                    {
                        _matchRoster.Eliminate(finalKillVictim);
                        for (int i = 0; i < _players.Count; i++)
                        {
                            if (_players[i].Name.Equals(finalKillVictim, StringComparison.OrdinalIgnoreCase))
                            {
                                var eliminatedPlayer = _players[i];
                                if (!eliminatedPlayer.IsEliminated)
                                {
                                    eliminatedPlayer.IsDisconnected = false;
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

        /// <summary>
        /// Opens a tracked match when the Bed Wars start block is seen. Matches are only tracked from
        /// the start block, so /who calls in the lobby never create phantom encounters.
        /// </summary>
        private void StartTrackedMatch()
        {
            _lastEndedMatchId = null;
            ClearPlayers();
            _matchRoster.Reset(active: true);
            _currentTeamByPlayer.Clear();

            if (_encounterRepository == null)
            {
                return;
            }

            try
            {
                _currentMatchId = _encounterRepository.StartMatch();
                AddDiagnosticLine($"Match #{_currentMatchId} started. Tracking player encounters.");
                SetStatus(LocalizationService.Instance["Status_MatchStarted"]);
            }
            catch (Exception ex)
            {
                _currentMatchId = null;
                AddDiagnosticLine($"[ERROR] Could not open match: {ex.Message}");
            }
        }

        private void ApplyLogTeam(string name, string? team)
        {
            if (team == null) return;
            _currentTeamByPlayer[name] = team;
            foreach (var player in _players)
            {
                if (player.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) player.Team = team;
            }
        }

        /// <summary>
        /// Closes the tracked match. Hypixel prints "Slumber Tickets (Time Played)" before the
        /// "(Win)" line, so the same match can be reported twice; the second report only upgrades
        /// the outcome from loss to win.
        /// </summary>
        /// <returns><c>true</c> when a running match was closed by this report.</returns>
        private bool EndTrackedMatch(GameOutcome outcome)
        {
            // The first report closes the running match; a later "(Win)" line for the same block
            // updates the outcome of the match that was just closed.
            long? matchId = _currentMatchId ?? _lastEndedMatchId;
            bool closedRunningMatch = _currentMatchId != null;

            if (closedRunningMatch)
            {
                _lastEndedMatchId = _currentMatchId;
                _currentMatchId = null;
            }

            if (matchId == null || _encounterRepository == null)
            {
                return false;
            }

            string outcomeText = outcome switch
            {
                GameOutcome.Win => "win",
                GameOutcome.Loss => "loss",
                _ => "unknown"
            };

            try
            {
                _encounterRepository.EndMatch(matchId.Value, outcomeText);
                AddDiagnosticLine($"Match #{matchId} finished ({outcomeText}).");
            }
            catch (Exception ex)
            {
                AddDiagnosticLine($"[ERROR] Could not close match: {ex.Message}");
            }

            return closedRunningMatch;
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
