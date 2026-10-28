using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Models;
using BedwarsOverlay.Core.Teams;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void ShowCommandHelp()
        {
            var loc = LocalizationService.Instance;
            _diagnosticLines.Clear();
            string[] helpLines =
            {
                loc["Help_Title"],
                loc["Help_Commands_Row1"],
                loc["Help_Commands_Row2"],
                loc["Help_Commands_Row3"],
                loc["Help_Commands_Row4"]
            };

            foreach (var helpLine in helpLines)
            {
                AddDiagnosticLine(helpLine);
            }

            SetStatus(loc["Status_CommandHelp"]);
        }

        private void ClearPlayers()
        {
            _teamRosterVersion++;
            HideSplitAlert();
            _playerLookupService?.CancelAndReset();
            _currentTeamByPlayer.Clear();
            _matchRoster.ClearPlayers();
            _players.Clear();
            PlaceholderPanel.Visibility = Visibility.Visible;
            AddDiagnosticLine(LocalizationService.Instance["Status_Cleared"]);
            SetStatus(LocalizationService.Instance["Status_Cleared"]);
        }

        private void LoadPlayers(List<string> players, bool preserveExisting = false)
        {
            if (_playerLookupService == null)
            {
                return;
            }

            var token = preserveExisting ? _playerLookupService.CurrentToken : _playerLookupService.CancelAndReset();
            if (!preserveExisting) _players.Clear();

            var encounterContext = BuildEncounterContext();
            _playerLookupService.SetEncounterContext(encounterContext);

            int index = _players.Count;
            foreach (var name in players)
            {
                var existing = _players.FirstOrDefault(row => row.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (preserveExisting && existing != null)
                {
                    existing.IsDisconnected = _matchRoster.IsDisconnected(name);
                    continue;
                }
                var placeholder = PlayerRow.CreateLoading(name, index % 2 == 0);
                placeholder.IsDisconnected = _matchRoster.IsDisconnected(name);
                if (_currentTeamByPlayer.TryGetValue(name, out string? team))
                {
                    placeholder.Team = team;
                }
                ApplyRepeatBadge(placeholder, name, encounterContext);
                _players.Add(placeholder);

                _ = _playerLookupService.ResolvePlayerAsync(name, placeholder, token);
                index++;
            }

            PlaceholderPanel.Visibility = _players.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RestoreSorting();
        }

        /// <summary>
        /// Collects the repeat-opponent information for the current /who batch. The running match is
        /// excluded, so a player seen twice inside the same game is not flagged.
        /// </summary>
        private EncounterContext BuildEncounterContext()
        {
            int lookback = _settingsRepository?.EncounterLookbackMatches ?? 10;
            int minCount = _settingsRepository?.EncounterMinCount ?? 2;

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (_encounterRepository != null)
            {
                try
                {
                    counts = _encounterRepository.GetRepeatCounts(lookback, _currentMatchId);
                }
                catch (Exception ex)
                {
                    AddDiagnosticLine($"[ERROR] Could not read encounters: {ex.Message}");
                }
            }

            return new EncounterContext
            {
                MatchId = _currentMatchId,
                RepeatCounts = counts,
                MinCount = minCount,
                Lookback = lookback
            };
        }

        /// <summary>Shows the repeat badge immediately, before the UUID lookup resolves.</summary>
        private void ApplyRepeatBadge(PlayerRow row, string name, EncounterContext context)
        {
            if (_settingsRepository?.HighlightRepeatOpponents != true || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!context.RepeatCounts.TryGetValue(name.ToLowerInvariant(), out int count) || count <= 0)
            {
                return;
            }

            string tooltip = LocalizationService.Instance["Repeat_Tooltip"]
                .Replace("{0}", count.ToString())
                .Replace("{1}", context.Lookback.ToString());

            row.ApplyEncounterInfo(count, context.MinCount, tooltip);
        }

        private async Task RemoveFinalKilledPlayerAfterDelayAsync(PlayerRow player)
        {
            await Task.Delay(FinalKillRemoveDelayMs);

            await Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!player.IsEliminated)
                {
                    return;
                }

                int index = _players.IndexOf(player);
                if (index < 0)
                {
                    return;
                }

                string name = player.Name;
                _players.RemoveAt(index);
                AddDiagnosticLine($"Removed final-killed player: {name}");

                if (_players.Count == 0)
                {
                    PlaceholderPanel.Visibility = Visibility.Visible;
                }
            }));
        }

        private List<string> FilterIgnoredPlayers(List<string> players)
        {
            if (_settingsRepository == null)
            {
                return players;
            }

            string localPlayer = _settingsRepository.LocalPlayerName;
            var ignoredPlayers = _settingsRepository.GetIgnoredPlayers();
            return players
                .Where(player => !ignoredPlayers.Exists(ignoredPlayer => ignoredPlayer.Equals(player, StringComparison.OrdinalIgnoreCase)))
                .Where(player => !_settingsRepository.IgnoreLocalPlayer || !player.Equals(localPlayer, StringComparison.OrdinalIgnoreCase))
                .Where(player => !_settingsRepository.IgnorePartyPlayers || !IsPartyOrTeammate(player))
                .ToList();
        }

        private void RemoveIgnoredExistingPlayers()
        {
            if (_settingsRepository == null)
            {
                return;
            }

            string localPlayer = _settingsRepository.LocalPlayerName;
            var ignoredPlayers = _settingsRepository.GetIgnoredPlayers();
            for (int i = _players.Count - 1; i >= 0; i--)
            {
                string player = _players[i].Name;
                bool ignoreListed = ignoredPlayers.Exists(ignoredPlayer => ignoredPlayer.Equals(player, StringComparison.OrdinalIgnoreCase));
                bool ignoreLocal = _settingsRepository.IgnoreLocalPlayer && player.Equals(localPlayer, StringComparison.OrdinalIgnoreCase);
                bool ignoreParty = _settingsRepository.IgnorePartyPlayers && IsPartyOrTeammate(player);
                if (ignoreListed || ignoreLocal || ignoreParty)
                {
                    _players.RemoveAt(i);
                }
            }

            if (_players.Count == 0)
            {
                PlaceholderPanel.Visibility = Visibility.Visible;
            }
        }

        private bool IsPartyOrTeammate(string player) =>
            _partyPlayers.Contains(player) || TeammateResolver.IsTeammate(player,
                _settingsRepository?.LocalPlayerName ?? string.Empty, _partyPlayers, _currentTeamByPlayer);
    }
}
