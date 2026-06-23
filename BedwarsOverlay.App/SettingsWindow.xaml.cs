using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using BedwarsOverlay.Core.Storage;
using BedwarsOverlay.Core.LogWatching;

namespace BedwarsOverlay.App
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsRepository _settingsRepository;
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
            ShowLogDiagnosticsBox.IsChecked = _settingsRepository.ShowLogDiagnostics;
        }

        private void ApiKeyVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            _isApiKeyVisible = !_isApiKeyVisible;

            if (_isApiKeyVisible)
            {
                ApiKeyBox.Text = ApiKeyMaskedBox.Password;
                ApiKeyMaskedBox.Visibility = Visibility.Collapsed;
                ApiKeyBox.Visibility = Visibility.Visible;
                ApiKeyVisibilityButton.Content = "\uE8F5";
                ApiKeyVisibilityButton.ToolTip = "Hide API key";
                ApiKeyBox.Focus();
                ApiKeyBox.CaretIndex = ApiKeyBox.Text.Length;
            }
            else
            {
                ApiKeyMaskedBox.Password = ApiKeyBox.Text;
                ApiKeyBox.Visibility = Visibility.Collapsed;
                ApiKeyMaskedBox.Visibility = Visibility.Visible;
                ApiKeyVisibilityButton.Content = "\uE890";
                ApiKeyVisibilityButton.ToolTip = "Show API key";
                ApiKeyMaskedBox.Focus();
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
                Title = "Select Minecraft latest.log File"
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
                    MessageBox.Show("Auto-detected path is not a valid file:\n" + detectedPath + "\n\nPlease click Browse to select it manually.", 
                                    "Log Path Detection", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to auto-detect log path: {ex.Message}", "Log Path Detection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = (_isApiKeyVisible ? ApiKeyBox.Text : ApiKeyMaskedBox.Password).Trim();
            string logPath = LogPathBox.Text.Trim();
            string localName = LocalPlayerBox.Text.Trim();
            bool showLogDiagnostics = ShowLogDiagnosticsBox.IsChecked == true;

            // Save settings via repository
            _settingsRepository.ApiKey = apiKey;
            _settingsRepository.LogFilePath = logPath;
            _settingsRepository.LocalPlayerName = localName;
            _settingsRepository.ShowLogDiagnostics = showLogDiagnostics;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
