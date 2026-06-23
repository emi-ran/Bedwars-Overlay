using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.LogWatching;
using BedwarsOverlay.Core.Models;

namespace BedwarsOverlay.App
{
    public partial class MainWindow : Window
    {
        private HotkeyService? _hotkeyService;
        private bool _isEditMode = false;
        private LogTailer? _logTailer;
        private HypixelLogParser? _logParser;
        private readonly ObservableCollection<string> _diagnosticLines = new();
        private readonly ObservableCollection<PlayerRow> _players = new();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start in play mode (transparent, click-through)
            ApplyPlayMode();

            // Disable Windows Aero Snap maximizing & vertical snapping behaviors natively
            ClickThroughService.DisableMaximizing(this);

            // Bind UI Elements
            LogDiagnosticsList.ItemsSource = _diagnosticLines;
            PlayersGrid.ItemsSource = _players;

            // Load initial mock players matching the Cubelify UI screenshot for styling demo
            LoadMockPlayers();

            // Setup search filtering
            var view = CollectionViewSource.GetDefaultView(_players);
            view.Filter = FilterPlayer;

            // Start Log Watcher and Parser
            try
            {
                string logPath = LogPathDetector.GetDefaultLogPath();
                _logParser = new HypixelLogParser();
                _logTailer = new LogTailer(logPath);
                
                _logTailer.LineAppended += OnLogLineAppended;
                _logTailer.ErrorEncountered += OnLogTailerError;
                _logTailer.Start();
                
                _diagnosticLines.Add($"Log watch started on: {logPath}");
            }
            catch (Exception ex)
            {
                _diagnosticLines.Add($"Failed to start log watcher: {ex.Message}");
            }

            // Set up global hotkey
            try
            {
                _hotkeyService = new HotkeyService(this);
                _hotkeyService.HotkeyPressed += ToggleEditMode;
                
                // Register Ctrl + Alt + O (Modifiers: Control = 0x0002, Alt = 0x0001 -> 3; Virtual Key 'O' = 0x4F)
                bool success = _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_ALT, 0x4F);
                if (!success)
                {
                    MessageBox.Show("Failed to register global hotkey Ctrl+Alt+O! Please check if another app is using this shortcut.", 
                                    "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing hotkey service: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMockPlayers()
        {
            var cyanBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));  // MVP/MVP+ Cyan
            var greenBrush = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)); // VIP/VIP+ Emerald green
            var whiteBrush = Brushes.White;                                       // Normal white
            var grayBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));  // Unranked gray

            // Match the screenshot players perfectly
            _players.Add(PlayerRow.CreateMock("Metriusz", 157, 6.29, 1.98, 6054, 1770, "1.1%", true, cyanBrush, false));
            _players.Add(PlayerRow.CreateMock("MTBMBsideB", 45, 0.97, 0.36, 379, 147, "2.6%", false, cyanBrush, true));
            _players.Add(PlayerRow.CreateMock("DominoKidX", 87, 0.59, 0.14, 1648, 412, "0.9%", false, greenBrush, false));
            _players.Add(PlayerRow.CreateMock("5Wizards2056", 39, 0.77, 0.24, 322, 98, "0%", false, grayBrush, true));
            _players.Add(PlayerRow.CreateMock("ya_YEET_2000", 70, 0.44, 0.17, 447, 191, "4.6%", false, cyanBrush, false));
            _players.Add(PlayerRow.CreateMock("mak47", 10, 1.15, 0.31, 78, 21, "0%", false, cyanBrush, true));
            _players.Add(PlayerRow.CreateMock("Cyteriax", 11, 0.72, 0.31, 63, 28, "0.9%", false, grayBrush, false));
            _players.Add(PlayerRow.CreateMock("TheHitmanTurtle", 16, 0.50, 0.11, 122, 28, "0%", false, cyanBrush, true));
            _players.Add(PlayerRow.CreateMock("NerdyEnderman", 6, 0.57, 0.57, 16, 16, "0%", false, grayBrush, false));
            _players.Add(PlayerRow.CreateMock("RugRat4000", 3, 0.38, 0.19, 12, 6, "0.4%", false, grayBrush, true));
            _players.Add(PlayerRow.CreateMock("ShiningScarlet", 14, 0.19, 0.04, 63, 13, "1.6%", false, grayBrush, false));
            _players.Add(PlayerRow.CreateMock("Avenger7447", 21, 0.15, 0.22, 66, 91, "0%", false, grayBrush, true));
            _players.Add(PlayerRow.CreateMock("curry_taco02", 14, 0.18, 0.19, 39, 40, "0%", false, grayBrush, false));
            _players.Add(PlayerRow.CreateMock("magnus12674", 4, 0.12, 0.06, 2, 1, "0%", false, grayBrush, true));
            _players.Add(PlayerRow.CreateMock("Maxiporkbun", 4, 0.00, 0.07, 0, 1, "0.3%", false, grayBrush, false));
            _players.Add(PlayerRow.CreateMock("filankee", 2, 0.00, 0.17, 0, 1, "13.3%", false, grayBrush, true));
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void EllipsisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void CopyUsername_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                try
                {
                    Clipboard.SetText(player.Name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPlancke_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"https://plancke.io/hypixel/player/stats/{player.Name}",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open Plancke link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                MessageBox.Show($"Added {player.Name} to local blacklist.", "Blacklist", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                MessageBox.Show($"Removed {player.Name} from local blacklist.", "Blacklist", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool FilterPlayer(object obj)
        {
            if (obj is PlayerRow row)
            {
                string query = SearchBox.Text.Trim();
                if (string.IsNullOrEmpty(query)) return true;
                return row.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private void ToggleEditMode()
        {
            _isEditMode = !_isEditMode;
            if (_isEditMode)
            {
                ApplyEditMode();
            }
            else
            {
                ApplyPlayMode();
            }
        }

        private void ApplyPlayMode()
        {
            // Enable Win32 Click-Through
            ClickThroughService.EnableClickThrough(this);

            // Style UI for background play mode (semi-transparent)
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x11, 0x11, 0x11)); // translucent dark background
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));  // very faint border
            
            TitleBar.Background = new SolidColorBrush(Color.FromArgb(0x1F, 0x11, 0x11, 0x11));
            ControlButtons.Visibility = Visibility.Collapsed;

            if (DiagnosticsPanel != null)
            {
                DiagnosticsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyEditMode()
        {
            // Disable Win32 Click-Through (allows clicks to interact with window)
            ClickThroughService.DisableClickThrough(this);

            // Style UI for interactive edit mode (solid background)
            OverlayBorder.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)); // solid dark background
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)); // premium indigo border
            
            TitleBar.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
            ControlButtons.Visibility = Visibility.Visible;

            if (DiagnosticsPanel != null)
            {
                DiagnosticsPanel.Visibility = Visibility.Visible;
            }
            
            // Force activate/focus the window
            this.Activate();
            this.Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isEditMode)
            {
                // Prevent maximizing on double-click
                if (e.ClickCount == 2)
                {
                    e.Handled = true;
                    return;
                }

                var previousMode = this.ResizeMode;
                this.ResizeMode = ResizeMode.NoResize; // Temporarily disable resize to bypass Windows Aero Snap
                try
                {
                    this.DragMove();
                }
                finally
                {
                    this.ResizeMode = previousMode; // Restore original resize mode (CanResize)
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings panel implementation is scheduled for a future phase.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _players.Clear();
            _diagnosticLines.Clear();
            _diagnosticLines.Add("Cache cleared and log display refreshed.");
        }

        private void OnLogLineAppended(string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_diagnosticLines.Count >= 20)
                {
                    _diagnosticLines.RemoveAt(0);
                }
                _diagnosticLines.Add(line);

                // Run the parser to extract player names from `/who` logs
                var players = _logParser?.ParsePlayerList(line);
                if (players != null && players.Count > 0)
                {
                    _players.Clear();
                    int index = 0;
                    foreach (var name in players)
                    {
                        // Create placeholder player rows. As Mojang and Hypixel clients are integrated
                        // in subsequent phases, these will be populated with actual API statistics.
                        _players.Add(PlayerRow.CreateMock(name, 1, 0.0, 0.0, 0, 0, "0%", false, Brushes.White, index % 2 == 0));
                        index++;
                    }
                }
            }));
        }

        private void OnLogTailerError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_diagnosticLines.Count >= 20)
                {
                    _diagnosticLines.RemoveAt(0);
                }
                _diagnosticLines.Add($"[ERROR] {error}");
            }));
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_players).Refresh();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyService?.Dispose();
            _logTailer?.Stop();
            base.OnClosed(e);
        }
    }
}