using System.Windows;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Api;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            if (_settingsRepository == null || _isSettingsOpen) return;

            _isSettingsOpen = true;
            try
            {
                var settingsWin = new SettingsWindow(_settingsRepository) { Owner = this };
                if (settingsWin.ShowDialog() == true)
                {
                    LocalizationService.Instance.Initialize(_settingsRepository.Language);
                    GridLocalizer.ApplyLocalization(PlayersGrid);
                    ApplyColumnVisibility();
                    _trayIconService?.RefreshText();
                    UpdateTitleBarLayout();

                    if (_mojangClient == null && _database != null)
                    {
                        _mojangClient = new MojangClient(_httpClient, _database);
                    }
                    if (_hypixelClient == null && _database != null)
                    {
                        _hypixelClient = new HypixelClient(_httpClient, _database, _settingsRepository);
                    }

                    _playerLookupService?.Dispose();
                    _playerLookupService = new PlayerLookupService(_mojangClient!, _hypixelClient!, _settingsRepository, AddDiagnosticLine);

                    RestartLogTailer();
                    UpdateDiagnosticsVisibility();
                    RemoveIgnoredExistingPlayers();

                    if (_players.Count > 0)
                    {
                        var token = _playerLookupService.CancelAndReset();
                        foreach (var row in _players)
                        {
                            _ = _playerLookupService.ResolvePlayerAsync(row.Name, row, token);
                        }
                    }
                }
            }
            finally
            {
                _isSettingsOpen = false;
            }
        }
    }
}
