using System;
using System.Threading.Tasks;
using System.Windows;
using BedwarsOverlay.App.Services;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void UpdateAvailableButtonVisibility()
        {
            if (UpdateAvailableButton == null)
            {
                return;
            }

            UpdateAvailableButton.Visibility = _isEditMode && _availableUpdate?.IsUpdateAvailable == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (_availableUpdate?.IsUpdateAvailable == true)
            {
                UpdateAvailableButton.Content = LocalizationService.Instance["Update_Button"].Replace("{0}", _availableUpdate.LatestVersion);
            }
        }

        private async Task CheckForUpdatesAsync(bool showPrompt)
        {
            if (_updateCheckService == null || _settingsRepository == null)
            {
                return;
            }

            try
            {
                var result = await _updateCheckService.CheckLatestReleaseAsync();
                if (!result.IsUpdateAvailable || result.LatestVersion.Equals(_settingsRepository.SkippedUpdateVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _availableUpdate = result;
                Dispatcher.Invoke(UpdateAvailableButtonVisibility);

                if (showPrompt && !_isSettingsOpen && OnboardingPanel.Visibility != Visibility.Visible)
                {
                    Dispatcher.Invoke(() => ShowUpdatePrompt(result));
                }
            }
            catch
            {
                // Update checks must never interrupt overlay startup or gameplay.
            }
        }

        private void ShowUpdatePrompt(UpdateCheckResult update)
        {
            var loc = LocalizationService.Instance;
            string message = loc["Update_Prompt"]
                .Replace("{0}", UpdateCheckService.CurrentVersion)
                .Replace("{1}", update.LatestVersion);

            MessageBoxResult result = MessageBox.Show(
                message,
                loc["Update_Title"],
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                OpenUrl(update.ReleaseUrl);
            }
            else if (result == MessageBoxResult.Cancel && _settingsRepository != null)
            {
                _settingsRepository.SkippedUpdateVersion = update.LatestVersion;
                _availableUpdate = null;
                UpdateAvailableButtonVisibility();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_availableUpdate != null)
            {
                OpenUrl(_availableUpdate.ReleaseUrl);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Instance["OpenUrl_Error"].Replace("{0}", ex.Message),
                    LocalizationService.Instance["Error_Title"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
