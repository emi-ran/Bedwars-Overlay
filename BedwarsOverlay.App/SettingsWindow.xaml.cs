using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Api;
using BedwarsOverlay.Core.Storage;
using BedwarsOverlay.Core.LogWatching;

namespace BedwarsOverlay.App
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsRepository _settingsRepository;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
        private bool _isApiKeyVisible;

        public SettingsWindow(SettingsRepository settingsRepository)
        {
            InitializeComponent();
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));

            // Populate settings fields on load
            ApiKeyBox.Text = _settingsRepository.ApiKey;
            ApiKeyMaskedBox.Password = ApiKeyBox.Text;
            LogPathBox.Text = _settingsRepository.LogFilePath;
            LocalPlayerBox.Text = _settingsRepository.LocalPlayerName;
            IgnorePartyPlayersBox.IsChecked = _settingsRepository.IgnorePartyPlayers;
            IgnoreLocalPlayerBox.IsChecked = _settingsRepository.IgnoreLocalPlayer;
            IgnoredPlayersBox.Text = string.Join(Environment.NewLine, _settingsRepository.GetIgnoredPlayers());
            ShowLogDiagnosticsBox.IsChecked = _settingsRepository.ShowLogDiagnostics;
            ShowLevelColumnBox.IsChecked = _settingsRepository.ShowLevelColumn;
            ShowWinstreakColumnBox.IsChecked = _settingsRepository.ShowWinstreakColumn;
            ShowFkdrColumnBox.IsChecked = _settingsRepository.ShowFkdrColumn;
            ShowWlrColumnBox.IsChecked = _settingsRepository.ShowWlrColumn;
            ShowFinalKillsColumnBox.IsChecked = _settingsRepository.ShowFinalKillsColumn;
            ShowWinsColumnBox.IsChecked = _settingsRepository.ShowWinsColumn;
            ShowSniperColumnBox.IsChecked = _settingsRepository.ShowSniperColumn;

            // Set language combo to match saved preference
            string lang = _settingsRepository.Language;
            if (string.IsNullOrEmpty(lang))
            {
                lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";
            }
            for (int i = 0; i < LanguageCombo.Items.Count; i++)
            {
                if (LanguageCombo.Items[i] is System.Windows.Controls.ComboBoxItem item && item.Tag.ToString() == lang)
                {
                    LanguageCombo.SelectedIndex = i;
                    break;
                }
            }

            // Apply localization to UI controls
            ApplyLocalization();

            // React to language combo changes immediately
            LanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;
        }

        private void LanguageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                string lang = item.Tag as string ?? "en";
                LocalizationService.Instance.SetLanguage(lang);
                ApplyLocalization();
            }
        }

        private void ApplyLocalization()
        {
            var loc = LocalizationService.Instance;

            SettingsTitleText.Text = loc["Settings_Title"];
            LabelApiKey.Text = loc["Label_ApiKey"];
            LabelLogPath.Text = loc["Label_LogPath"];
            LabelLocalPlayer.Text = loc["Label_LocalPlayer"];
            LabelLanguage.Text = loc["Lang_Label"];
            LabelVisibleColumns.Text = loc["Label_VisibleColumns"];
            LabelIgnoredPlayers.Text = loc["Label_IgnoredPlayers"];
            ShortcutsTitleText.Text = loc["Shortcuts_Title"];
            ShortcutsHelpText.Text = loc["Shortcuts_Help"];
            IgnorePartyPlayersBox.Content = loc["Checkbox_IgnorePartyPlayers"];
            IgnoreLocalPlayerBox.Content = loc["Checkbox_IgnoreLocalPlayer"];
            ShowLogDiagnosticsBox.Content = loc["Checkbox_Diagnostics"];
            BtnAutoDetect.Content = loc["Button_AutoDetect"];
            BtnBrowse.Content = loc["Button_Browse"];
            BtnGetApiKey.Content = loc["Button_GetApiKey"];
            BtnCheckApiKey.Content = loc["Button_CheckApiKey"];
            BtnCancel.Content = loc["Button_Cancel"];
            BtnSaveSettings.Content = loc["Button_SaveSettings"];
            GitHubButtonText.Text = loc["Button_GitHub"];
            LicenseText.Text = loc["Footer_License"];
            ApiKeyStatusText.Text = loc["ApiKey_StatusIdle"];

            // Update eye toggle tooltip based on current visibility state
            ApiKeyVisibilityButton.ToolTip = _isApiKeyVisible ? loc["Tooltip_HideApiKey"] : loc["Tooltip_ShowApiKey"];
        }

        private void ApiKeyVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            _isApiKeyVisible = !_isApiKeyVisible;

            if (_isApiKeyVisible)
            {
                ApiKeyBox.Text = ApiKeyMaskedBox.Password;
                ApiKeyMaskedBox.Visibility = Visibility.Collapsed;
                ApiKeyBox.Visibility = Visibility.Visible;
                ApiKeyVisibilityButton.ToolTip = LocalizationService.Instance["Tooltip_HideApiKey"];
                ApiKeyBox.Focus();
                ApiKeyBox.CaretIndex = ApiKeyBox.Text.Length;
            }
            else
            {
                ApiKeyMaskedBox.Password = ApiKeyBox.Text;
                ApiKeyBox.Visibility = Visibility.Collapsed;
                ApiKeyMaskedBox.Visibility = Visibility.Visible;
                ApiKeyVisibilityButton.ToolTip = LocalizationService.Instance["Tooltip_ShowApiKey"];
                ApiKeyMaskedBox.Focus();
            }
        }

        private void GetApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://developer.hypixel.net/");
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/emi-ran/Bedwars-Overlay");
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

        private async void CheckApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = (_isApiKeyVisible ? ApiKeyBox.Text : ApiKeyMaskedBox.Password).Trim();
            var loc = LocalizationService.Instance;

            BtnCheckApiKey.IsEnabled = false;
            ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.LightGray;
            ApiKeyStatusText.Text = loc["ApiKey_StatusChecking"];

            try
            {
                var database = new AppDatabase();
                var client = new HypixelClient(_httpClient, database, _settingsRepository);
                var result = await client.ValidateApiKeyAsync(apiKey);
                ApplyApiKeyValidationStatus(result.Status);
            }
            finally
            {
                BtnCheckApiKey.IsEnabled = true;
            }
        }

        private void ApplyApiKeyValidationStatus(HypixelLookupStatus status)
        {
            var loc = LocalizationService.Instance;
            switch (status)
            {
                case HypixelLookupStatus.Success:
                    ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                    ApiKeyStatusText.Text = loc["ApiKey_StatusValid"];
                    break;
                case HypixelLookupStatus.MissingApiKey:
                    ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    ApiKeyStatusText.Text = loc["ApiKey_StatusMissing"];
                    break;
                case HypixelLookupStatus.InvalidApiKey:
                    ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    ApiKeyStatusText.Text = loc["ApiKey_StatusInvalid"];
                    break;
                case HypixelLookupStatus.RateLimited:
                    ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    ApiKeyStatusText.Text = loc["ApiKey_StatusRateLimited"];
                    break;
                default:
                    ApiKeyStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    ApiKeyStatusText.Text = loc["ApiKey_StatusError"];
                    break;
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can only run while the left mouse button is still held.
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Minecraft Log Files (latest.log)|*.log|All Log Files (*.log)|*.log|All Files (*.*)|*.*",
                Title = LocalizationService.Instance["Browse_Title"]
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LogPathBox.Text = openFileDialog.FileName;
            }
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string detectedPath = LogPathDetector.GetDefaultLogPath();
                if (System.IO.File.Exists(detectedPath))
                {
                    LogPathBox.Text = detectedPath;
                }
                else
                {
                    MessageBox.Show(
                        LocalizationService.Instance["Detection_NotFound"].Replace("{0}", detectedPath),
                        LocalizationService.Instance["Detection_Title"],
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Instance["Detection_Failed"].Replace("{0}", ex.Message),
                    LocalizationService.Instance["Detection_ErrorTitle"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = (_isApiKeyVisible ? ApiKeyBox.Text : ApiKeyMaskedBox.Password).Trim();
            string logPath = LogPathBox.Text.Trim();
            string localName = LocalPlayerBox.Text.Trim();
            string ignoredPlayers = IgnoredPlayersBox.Text.Trim();
            bool showLogDiagnostics = ShowLogDiagnosticsBox.IsChecked == true;
            bool ignorePartyPlayers = IgnorePartyPlayersBox.IsChecked == true;
            bool ignoreLocalPlayer = IgnoreLocalPlayerBox.IsChecked == true;

            // Read selected language from combo box
            string lang = "en";
            if (LanguageCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                lang = selectedItem.Tag as string ?? "en";
            }

            // Save settings via repository
            _settingsRepository.ApiKey = apiKey;
            _settingsRepository.LogFilePath = logPath;
            _settingsRepository.LocalPlayerName = localName;
            _settingsRepository.IgnoredPlayers = ignoredPlayers;
            _settingsRepository.IgnorePartyPlayers = ignorePartyPlayers;
            _settingsRepository.IgnoreLocalPlayer = ignoreLocalPlayer;
            _settingsRepository.ShowLogDiagnostics = showLogDiagnostics;
            _settingsRepository.Language = lang;
            _settingsRepository.ShowLevelColumn = ShowLevelColumnBox.IsChecked == true;
            _settingsRepository.ShowWinstreakColumn = ShowWinstreakColumnBox.IsChecked == true;
            _settingsRepository.ShowFkdrColumn = ShowFkdrColumnBox.IsChecked == true;
            _settingsRepository.ShowWlrColumn = ShowWlrColumnBox.IsChecked == true;
            _settingsRepository.ShowFinalKillsColumn = ShowFinalKillsColumnBox.IsChecked == true;
            _settingsRepository.ShowWinsColumn = ShowWinsColumnBox.IsChecked == true;
            _settingsRepository.ShowSniperColumn = ShowSniperColumnBox.IsChecked == true;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
