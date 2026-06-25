using System;
using System.Windows;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void ShowOnboarding()
        {
            _isEditMode = true;
            IsEditMode = true;
            ApplyEditMode();
            PlaceholderPanel.Visibility = Visibility.Collapsed;
            OnboardingPanel.Visibility = Visibility.Visible;
        }

        private void ApplyOnboardingLocalization()
        {
            var loc = Services.LocalizationService.Instance;
            OnboardingTitleText.Text = loc["Onboarding_Title"];
            OnboardingBodyText.Text = loc["Onboarding_Body"];
            OnboardingEditShortcutText.Text = loc["Onboarding_EditShortcut"];
            OnboardingHideShortcutText.Text = loc["Onboarding_HideShortcut"];
            OnboardingSettingsText.Text = loc["Onboarding_Settings"];
            OnboardingDoneButton.Content = loc[_needsConfigPrompt ? "Onboarding_OpenSettings" : "Onboarding_Done"];
        }

        private void OnboardingDoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsRepository == null)
            {
                return;
            }

            _settingsRepository.HasCompletedOnboarding = true;
            OnboardingPanel.Visibility = Visibility.Collapsed;
            PlaceholderPanel.Visibility = Visibility.Visible;

            ContinueStartupAfterOnboarding();
        }

        private void ContinueStartupAfterOnboarding()
        {
            if (_settingsRepository == null)
            {
                return;
            }

            if (_needsConfigPrompt)
            {
                _showingConfigNeeded = true;
                SetStatus(Services.LocalizationService.Instance["Status_ConfigureApi"]);
                Dispatcher.BeginInvoke(new Action(() => OpenSettings()));
            }
            else
            {
                RestartLogTailer();
            }
        }
    }
}
