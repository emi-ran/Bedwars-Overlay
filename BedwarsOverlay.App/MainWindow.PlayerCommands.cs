using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Models;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void ShowCommandHelp()
        {
            var loc = LocalizationService.Instance;
            string[] helpLines =
            {
                loc["Help_Title"],
                loc["Help_BwStats"],
                loc["Help_BwIgnore"],
                loc["Help_BwClear"],
                loc["Help_BwHelp"],
                loc["Help_PList"],
                loc["Help_Who"]
            };

            foreach (var helpLine in helpLines)
            {
                AddDiagnosticLine(helpLine);
            }

            SetStatus(loc["Status_CommandHelp"]);
        }

        private void ClearPlayers()
        {
            _playerLookupService?.CancelAndReset();
            _players.Clear();
            PlaceholderPanel.Visibility = Visibility.Visible;
            AddDiagnosticLine(LocalizationService.Instance["Status_Cleared"]);
            SetStatus(LocalizationService.Instance["Status_Cleared"]);
        }

        private void LoadPlayers(List<string> players)
        {
            if (_playerLookupService == null)
            {
                return;
            }

            var token = _playerLookupService.CancelAndReset();
            _players.Clear();
            PlaceholderPanel.Visibility = players.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            int index = 0;
            foreach (var name in players)
            {
                var placeholder = PlayerRow.CreateLoading(name, index % 2 == 0);
                _players.Add(placeholder);

                _ = _playerLookupService.ResolvePlayerAsync(name, placeholder, token);
                index++;
            }

            RestoreSorting();
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
                .Where(player => !_settingsRepository.IgnorePartyPlayers || !_partyPlayers.Contains(player))
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
                bool ignoreParty = _settingsRepository.IgnorePartyPlayers && _partyPlayers.Contains(player);
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
    }
}
