using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Api;
using BedwarsOverlay.Core.LogWatching;
using BedwarsOverlay.Core.Models;
using BedwarsOverlay.Core.Storage;

namespace BedwarsOverlay.App
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private const int ToggleEditModeHotkeyId = 9000;
        private const int ToggleVisibilityHotkeyId = 9001;

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register("IsEditMode", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        private HotkeyService? _hotkeyService;
        private bool _isEditMode = false;
        private LogTailer? _logTailer;
        private HypixelLogParser? _logParser;
        private readonly ObservableCollection<string> _diagnosticLines = new();
        private readonly ObservableCollection<PlayerRow> _players = new();

        private AppDatabase? _database;
        private SettingsRepository? _settingsRepository;
        private MojangClient? _mojangClient;
        private HypixelClient? _hypixelClient;
        private PlayerLookupService? _playerLookupService;
        private bool _showingConfigNeeded;
        private bool _isSettingsOpen;
        private bool _isHiddenMode;
        private bool _needsConfigPrompt;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleBarLayout();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize database settings and API clients
            try
            {
                _database = new AppDatabase();
                _settingsRepository = new SettingsRepository();
                _mojangClient = new MojangClient(_httpClient, _database);
                _hypixelClient = new HypixelClient(_httpClient, _database, _settingsRepository);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Instance["Init_FailedDb"].Replace("{0}", ex.Message),
                    LocalizationService.Instance["Init_ErrorTitle"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Initialize localization from saved settings and apply
            LocalizationService.Instance.Initialize(_settingsRepository.Language);
            UpdateTitleBarLayout();

            _playerLookupService = new PlayerLookupService(_mojangClient, _hypixelClient, _settingsRepository, AddDiagnosticLine);
            GridLocalizer.ApplyLocalization(PlayersGrid);

            // Bind UI Elements
            LogDiagnosticsList.ItemsSource = _diagnosticLines;
            PlayersGrid.ItemsSource = _players;

            // Set up search filter
            var view = CollectionViewSource.GetDefaultView(_players);
            if (view != null)
            {
                view.Filter = FilterPlayer;
            }

            if (view is ListCollectionView listColView)
            {
                listColView.IsLiveSorting = true;
                listColView.LiveSortingProperties.Add("LevelRaw");
                listColView.LiveSortingProperties.Add("WinstreakRaw");
                listColView.LiveSortingProperties.Add("FkdrRaw");
                listColView.LiveSortingProperties.Add("WlrRaw");
                listColView.LiveSortingProperties.Add("FinalKillsRaw");
                listColView.LiveSortingProperties.Add("WinsRaw");
                listColView.LiveSortingProperties.Add("SniperRaw");
            }

            RestoreSorting();

            // Start empty with placeholder text visible
            PlaceholderPanel.Visibility = Visibility.Visible;

            string apiKey = _settingsRepository.ApiKey;
            _needsConfigPrompt = string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_real_hypixel_api_key_here", StringComparison.OrdinalIgnoreCase);
            ApplyOnboardingLocalization();

            if (!_settingsRepository.HasCompletedOnboarding)
            {
                ShowOnboarding();
            }
            else
            {
                _isEditMode = true;
                IsEditMode = true;
                ApplyEditMode();
                ContinueStartupAfterOnboarding();
            }

            // Set up global hotkey
            try
            {
                _hotkeyService = new HotkeyService(this);
                _hotkeyService.HotkeyPressed += HandleHotkeyPressed;

                bool success =
                    _hotkeyService.Register(ToggleEditModeHotkeyId, HotkeyService.MOD_CONTROL | HotkeyService.MOD_ALT, 0x4F) && // Ctrl+Alt+O
                    _hotkeyService.Register(ToggleVisibilityHotkeyId, HotkeyService.MOD_CONTROL | HotkeyService.MOD_ALT, 0x48);  // Ctrl+Alt+H
                if (!success)
                {
                    MessageBox.Show(
                        LocalizationService.Instance["Hotkey_Failed"],
                        LocalizationService.Instance["Hotkey_ErrorTitle"],
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Instance["Hotkey_InitError"].Replace("{0}", ex.Message),
                    LocalizationService.Instance["Init_ErrorTitle"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleHotkeyPressed(int id)
        {
            if (id == ToggleEditModeHotkeyId)
            {
                ToggleEditMode();
            }
            else if (id == ToggleVisibilityHotkeyId)
            {
                ToggleVisibilityMode();
            }
        }

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
            var loc = LocalizationService.Instance;
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
                SetStatus(LocalizationService.Instance["Status_ConfigureApi"]);
                Dispatcher.BeginInvoke(new Action(() => OpenSettings()));
            }
            else
            {
                RestartLogTailer();
            }
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
                    MessageBox.Show(
                        LocalizationService.Instance["Clipboard_Error"].Replace("{0}", ex.Message),
                        LocalizationService.Instance["Error_Title"],
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(
                        LocalizationService.Instance["Plancke_Error"].Replace("{0}", ex.Message),
                        LocalizationService.Instance["Error_Title"],
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            if (_isSettingsOpen || _isHiddenMode || OnboardingPanel.Visibility == Visibility.Visible)
            {
                return;
            }

            _isEditMode = !_isEditMode;
            IsEditMode = _isEditMode;
            if (_isEditMode)
            {
                ApplyEditMode();
            }
            else
            {
                ApplyPlayMode();
            }
        }

        private void ToggleVisibilityMode()
        {
            if (_isSettingsOpen)
            {
                return;
            }

            if (_isHiddenMode)
            {
                _isHiddenMode = false;
                Show();
                return;
            }

            _isHiddenMode = true;
            Hide();
        }

        private void ApplyPlayMode()
        {
            // Enable Win32 Click-Through
            ClickThroughService.EnableClickThrough(this);

            // Style UI for background play mode (semi-transparent)
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x11, 0x11, 0x11)); // translucent dark background
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));  // very faint border
            
            TitleBar.Background = Brushes.Transparent;
            ControlButtons.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            EditModeBadge.Visibility = Visibility.Collapsed;
            ClearSearchFilter();
            UpdateTitleBarLayout();

            if (DiagnosticsPanel != null)
            {
                UpdateDiagnosticsVisibility();
            }
        }

        private void ApplyEditMode()
        {
            // Disable Win32 Click-Through (allows clicks to interact with window)
            ClickThroughService.DisableClickThrough(this);

            // Style UI for interactive edit mode (solid background)
            OverlayBorder.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)); // solid dark background
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)); // premium indigo border
            
            TitleBar.Background = Brushes.Transparent;
            ControlButtons.Visibility = Visibility.Visible;
            EditModeBadge.Visibility = Visibility.Visible;
            UpdateTitleBarLayout();

            if (DiagnosticsPanel != null)
            {
                UpdateDiagnosticsVisibility();
            }
            
            // Force activate/focus the window
            this.Activate();
            this.Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed && _isEditMode)
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
                catch (InvalidOperationException)
                {
                    // DragMove can only run while the left mouse button is still held.
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
            OpenSettings();
        }

        private void UpdateDiagnosticsVisibility()
        {
            if (DiagnosticsPanel == null)
            {
                return;
            }

            bool showDiagnostics = _isEditMode && _settingsRepository?.ShowLogDiagnostics == true;
            DiagnosticsPanel.Visibility = showDiagnostics ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTitleBarLayout()
        {
            if (TitleText == null || EditModeBadgeText == null || SearchPanel == null)
            {
                return;
            }

            var loc = LocalizationService.Instance;
            bool narrowTitle = ActualWidth > 0 && ActualWidth < 560;
            bool narrowBadge = ActualWidth > 0 && ActualWidth < 660;
            bool hideSearch = !_isEditMode || (ActualWidth > 0 && ActualWidth < 720);

            TitleText.Text = loc[narrowTitle ? "Title_BedwarsOverlayShort" : "Title_BedwarsOverlay"];
            EditModeBadgeText.Text = loc[narrowBadge ? "EditMode_BadgeShort" : "EditMode_Badge"];
            SearchPanel.Visibility = hideSearch ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetStatus(string message)
        {
            ParsedPlayersText.Text = message;
        }

        private void AddDiagnosticLine(string line)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => AddDiagnosticLine(line)));
                return;
            }

            if (_diagnosticLines.Count >= 20)
            {
                _diagnosticLines.RemoveAt(0);
            }

            _diagnosticLines.Add(line);
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
                    // Re-apply localization after settings (language may have changed)
                    LocalizationService.Instance.Initialize(_settingsRepository.Language);
                    GridLocalizer.ApplyLocalization(PlayersGrid);
                    UpdateTitleBarLayout();

                    if (_mojangClient == null && _database != null)
                    {
                        _mojangClient = new MojangClient(_httpClient, _database);
                    }
                    if (_hypixelClient == null && _database != null)
                    {
                        _hypixelClient = new HypixelClient(_httpClient, _database, _settingsRepository);
                    }

                    // Ensure lookup service has current client references
                    _playerLookupService?.Dispose();
                    _playerLookupService = new PlayerLookupService(_mojangClient!, _hypixelClient!, _settingsRepository, AddDiagnosticLine);

                    RestartLogTailer();
                    UpdateDiagnosticsVisibility();

                    // Re-evaluate stats of existing players (if any) with the new settings
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

        private void RestartLogTailer()
        {
            if (_settingsRepository == null) return;

            // Stop existing watcher
            if (_logTailer != null)
            {
                _logTailer.Stop();
                _logTailer.LineAppended -= OnLogLineAppended;
                _logTailer.ErrorEncountered -= OnLogTailerError;
                _logTailer = null;
            }

            string apiKey = _settingsRepository.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_real_hypixel_api_key_here", StringComparison.OrdinalIgnoreCase))
            {
                _showingConfigNeeded = true;
                SetStatus(LocalizationService.Instance["Status_ConfigureApi"]);
                return;
            }

            // Restore the normal status text if it was showing configuration error
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _players.Clear();
            _diagnosticLines.Clear();
            AddDiagnosticLine(LocalizationService.Instance["Status_Cleared"]);
            PlaceholderPanel.Visibility = Visibility.Visible;
        }

        private void ClearSearchFilter()
        {
            if (SearchBox.Text.Length == 0)
            {
                return;
            }

            SearchBox.Text = string.Empty;
            SearchPlaceholder.Visibility = Visibility.Visible;
            CollectionViewSource.GetDefaultView(_players)?.Refresh();
        }

        private void OnLogLineAppended(string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDiagnosticLine(line);

                // Run the parser to extract player names from `/who` logs
                var players = _logParser?.ParsePlayerList(line);
                if (players != null && players.Count > 0 && _playerLookupService != null)
                {
                    var token = _playerLookupService.CancelAndReset();
                    _players.Clear();
                    PlaceholderPanel.Visibility = Visibility.Collapsed;
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
            }));
        }

        private void OnLogTailerError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDiagnosticLine($"[ERROR] {error}");
            }));
        }

        private void PlayersGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var column = e.Column;
            var direction = column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            string sortMember = column.SortMemberPath;
            if (string.IsNullOrEmpty(sortMember))
            {
                if (column is DataGridBoundColumn boundCol && boundCol.Binding is Binding binding)
                {
                    sortMember = binding.Path.Path;
                }
            }

            if (string.IsNullOrEmpty(sortMember)) return;

            ApplySort(sortMember, direction);

            // Update column headers SortDirection
            foreach (var col in PlayersGrid.Columns)
            {
                col.SortDirection = (col == column) ? direction : null;
            }

            // Save to settings
            if (_settingsRepository != null)
            {
                _settingsRepository.SortColumn = sortMember;
                _settingsRepository.SortDirection = direction.ToString();
            }
        }

        private void ApplySort(string sortMemberPath, ListSortDirection direction)
        {
            var view = CollectionViewSource.GetDefaultView(_players);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(sortMemberPath, direction));
                view.Refresh();
            }
        }

        private void RestoreSorting()
        {
            if (_settingsRepository == null) return;

            string sortCol = _settingsRepository.SortColumn;
            string sortDirStr = _settingsRepository.SortDirection;

            if (string.IsNullOrEmpty(sortCol) || string.IsNullOrEmpty(sortDirStr))
            {
                // Default sorting (e.g. FKDR descending by default is a premium UX touch)
                sortCol = "FkdrRaw";
                sortDirStr = "Descending";
            }

            if (Enum.TryParse<ListSortDirection>(sortDirStr, out var direction))
            {
                foreach (var col in PlayersGrid.Columns)
                {
                    if (col.SortMemberPath == sortCol)
                    {
                        col.SortDirection = direction;
                        ApplySort(sortCol, direction);
                        break;
                    }
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchPlaceholder();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchPlaceholder();
            CollectionViewSource.GetDefaultView(_players).Refresh();
        }

        private void MainGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchBox.IsKeyboardFocusWithin)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, SearchPanel))
            {
                return;
            }

            MainGrid.Focus();
            Keyboard.ClearFocus();
            UpdateSearchPlaceholder();
        }

        private void UpdateSearchPlaceholder()
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.IsKeyboardFocusWithin
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            for (DependencyObject? current = child; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current == parent)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnClosed(EventArgs e)
        {
            _playerLookupService?.Dispose();
            _hotkeyService?.Dispose();
            _logTailer?.Stop();
            base.OnClosed(e);
        }
    }
}
