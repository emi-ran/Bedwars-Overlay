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

        public SettingsWindow(SettingsRepository settingsRepository)
        {
            InitializeComponent();
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            
            // Populate settings fields on load
            ApiKeyBox.Text = _settingsRepository.ApiKey;
            LogPathBox.Text = _settingsRepository.LogFilePath;
            LocalPlayerBox.Text = _settingsRepository.LocalPlayerName;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
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
            string apiKey = ApiKeyBox.Text.Trim();
            string logPath = LogPathBox.Text.Trim();
            string localName = LocalPlayerBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_real_hypixel_api_key_here", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please enter a valid Hypixel API Key to enable player statistics retrieval.", 
                                "Invalid API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save settings via repository
            _settingsRepository.ApiKey = apiKey;
            _settingsRepository.LogFilePath = logPath;
            _settingsRepository.LocalPlayerName = localName;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
