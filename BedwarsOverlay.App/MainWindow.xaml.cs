using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Api;
using BedwarsOverlay.Core.LogWatching;
using BedwarsOverlay.Core.Models;
using BedwarsOverlay.Core.Stats;
using BedwarsOverlay.Core.Storage;

namespace BedwarsOverlay.App
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

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
        private CancellationTokenSource? _lookupCts;
        private bool _showingConfigNeeded;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start in play mode (transparent, click-through)
            ApplyPlayMode();

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
            ApplyLocalization();

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

            // Verify API key configuration on startup
            string apiKey = _settingsRepository.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_real_hypixel_api_key_here", StringComparison.OrdinalIgnoreCase))
            {
                _showingConfigNeeded = true;
                SetStatus(LocalizationService.Instance["Status_ConfigureApi"]);
                // Modelessly prompt settings dialog after load finishes
                Dispatcher.BeginInvoke(new Action(() => OpenSettings()));
            }
            else
            {
                RestartLogTailer();
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
            SearchPanel.Visibility = Visibility.Visible;
            EditModeBadge.Visibility = Visibility.Visible;

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

        private void ApplyLocalization()
        {
            if (_settingsRepository == null) return;

            // DataGrid column headers cannot use DynamicResource or direct bindings easily,
            // so we set them programmatically after language change.
            var loc = LocalizationService.Instance;

            // Column headers
            PlayersGrid.Columns[0].Header = loc["Column_Lvl"];
            PlayersGrid.Columns[1].Header = loc["Column_Name"];
            PlayersGrid.Columns[2].Header = loc["Column_WS"];
            PlayersGrid.Columns[3].Header = loc["Column_FKDR"];
            PlayersGrid.Columns[4].Header = loc["Column_WLR"];
            PlayersGrid.Columns[5].Header = loc["Column_FKills"];
            PlayersGrid.Columns[6].Header = loc["Column_Wins"];
            PlayersGrid.Columns[7].Header = loc["Column_Sniper"];

            // Column header tooltips (set via HeaderStyle tooltip setter)
            SetColumnTooltip(0, loc["Tooltip_Lvl"]);
            SetColumnTooltip(1, loc["Tooltip_Name"]);
            SetColumnTooltip(2, loc["Tooltip_WS"]);
            SetColumnTooltip(3, loc["Tooltip_FKDR"]);
            SetColumnTooltip(4, loc["Tooltip_WLR"]);
            SetColumnTooltip(5, loc["Tooltip_FKills"]);
            SetColumnTooltip(6, loc["Tooltip_Wins"]);
            SetColumnTooltip(7, loc["Tooltip_Sniper"]);

            // Title bar - keep brand name "Bedwars Overlay" unchanged
        }

        private void SetColumnTooltip(int columnIndex, string tooltipText)
        {
            if (columnIndex < 0 || columnIndex >= PlayersGrid.Columns.Count)
                return;

            var column = PlayersGrid.Columns[columnIndex];
            if (column.HeaderStyle != null)
            {
                // Remove existing tooltip setter and add new one
                var newStyle = new Style(typeof(DataGridColumnHeader), column.HeaderStyle.BasedOn);
                foreach (var setter in column.HeaderStyle.Setters)
                {
                    if (setter is Setter s && s.Property == FrameworkElement.ToolTipProperty)
                        continue;
                    newStyle.Setters.Add(setter);
                }
                newStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, tooltipText));
                column.HeaderStyle = newStyle;
            }
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

        private CancellationToken ResetLookupCancellation()
        {
            _lookupCts?.Cancel();
            _lookupCts?.Dispose();
            _lookupCts = new CancellationTokenSource();
            return _lookupCts.Token;
        }

        private void OpenSettings()
        {
            if (_settingsRepository == null) return;
            
            var settingsWin = new SettingsWindow(_settingsRepository) { Owner = this };
            if (settingsWin.ShowDialog() == true)
            {
                // Re-apply localization after settings (language may have changed)
                LocalizationService.Instance.Initialize(_settingsRepository.Language);
                ApplyLocalization();

                if (_mojangClient == null && _database != null)
                {
                    _mojangClient = new MojangClient(_httpClient, _database);
                }
                if (_hypixelClient == null && _database != null)
                {
                    _hypixelClient = new HypixelClient(_httpClient, _database, _settingsRepository);
                }

                RestartLogTailer();
                UpdateDiagnosticsVisibility();

                // Re-evaluate stats of existing players (if any) with the new settings
                if (_players.Count > 0)
                {
                    var token = ResetLookupCancellation();
                    int index = 0;
                    foreach (var row in _players)
                    {
                        _ = ResolvePlayerAsync(row.Name, row, index % 2 == 0, token);
                        index++;
                    }
                }
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
                if (players != null && players.Count > 0)
                {
                    var token = ResetLookupCancellation();
                    _players.Clear();
                    PlaceholderPanel.Visibility = Visibility.Collapsed;
                    int index = 0;
                    foreach (var name in players)
                    {
                        var placeholder = PlayerRow.CreateLoading(name, index % 2 == 0);
                        _players.Add(placeholder);

                        // Asynchronously resolve player profile and stats
                        _ = ResolvePlayerAsync(name, placeholder, index % 2 == 0, token);
                        index++;
                    }

                    RestoreSorting();
                }
            }));
        }

        private async Task ResolvePlayerAsync(string name, PlayerRow row, bool isEvenRow, CancellationToken cancellationToken)
        {
            if (_mojangClient == null || _hypixelClient == null || _settingsRepository == null) return;

            try
            {
                // 1. Resolve Username to UUID
                cancellationToken.ThrowIfCancellationRequested();
                var mojangResult = await _mojangClient.ResolveUsernameAsync(name);
                cancellationToken.ThrowIfCancellationRequested();
                if (mojangResult.Status != MojangLookupStatus.Success || mojangResult.Uuid == null)
                {
                    string statusText = mojangResult.Status switch
                    {
                        MojangLookupStatus.RateLimited => "MOJANG_RATE_LIMIT",
                        MojangLookupStatus.NetworkError => "MOJANG_NETWORK_ERROR",
                        MojangLookupStatus.InvalidResponse => "MOJANG_INVALID_RESPONSE",
                        _ => "NICKED_OR_UNKNOWN"
                    };
                    AddDiagnosticLine($"{name}: {statusText}");
                    Dispatcher.Invoke(() =>
                    {
                        row.StatusText = statusText;
                        row.NameColor = Brushes.Red; // highlight nicked/unknown players
                        row.Level = "?";
                        row.Winstreak = "?";
                        row.Fkdr = "?";
                        row.Wlr = "?";
                        row.FinalKills = "?";
                        row.Wins = "?";
                        row.Sniper = "?";
                    });
                    return;
                }

                // 2. Fetch Hypixel stats JSON (with automatic DB caching and rate limit check)
                var hypixelResult = await _hypixelClient.GetPlayerAsync(mojangResult.Uuid);
                cancellationToken.ThrowIfCancellationRequested();
                if (hypixelResult.Status != HypixelLookupStatus.Success || hypixelResult.PlayerJson == null)
                {
                    string statusText = hypixelResult.Status switch
                    {
                        HypixelLookupStatus.MissingApiKey => "MISSING_API_KEY",
                        HypixelLookupStatus.InvalidApiKey => "INVALID_API_KEY",
                        HypixelLookupStatus.RateLimited => "HYPIXEL_RATE_LIMIT",
                        HypixelLookupStatus.NetworkError => "HYPIXEL_NETWORK_ERROR",
                        HypixelLookupStatus.InvalidResponse => "HYPIXEL_INVALID_RESPONSE",
                        _ => "NO_HYPIXEL_DATA"
                    };
                    AddDiagnosticLine($"{name}: {statusText}");
                    Dispatcher.Invoke(() =>
                    {
                        row.StatusText = statusText;
                        row.NameColor = Brushes.Orange;
                        row.Level = "-";
                        row.Winstreak = "-";
                        row.Fkdr = "-";
                        row.Wlr = "-";
                        row.FinalKills = "-";
                        row.Wins = "-";
                        row.Sniper = "-";
                    });
                    return;
                }

                // 3. Parse stats and build updated UI row
                var stats = StatsCalculator.ParseStats(hypixelResult.PlayerJson);

                // Rank Color
                Brush rankBrush = Brushes.White;
                string rankColorName = stats.GetRankColorName();
                if (rankColorName == "Cyan") rankBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                else if (rankColorName == "Green") rankBrush = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                else if (rankColorName == "Gold") rankBrush = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                else if (rankColorName == "Red") rankBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                else if (rankColorName == "Gray") rankBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

                string localName = _settingsRepository.LocalPlayerName;
                bool isLocalPlayer = !string.IsNullOrEmpty(localName) && name.Equals(localName, StringComparison.OrdinalIgnoreCase);

                string sniperStr = $"{CalculateThreatPercentage(stats):F1}%";

                // Apply calculations to WPF UI model
                Dispatcher.Invoke(() =>
                {
                    row.Update(
                        stats.Level,
                        stats.HasWinstreak,
                        stats.Winstreak,
                        stats.Fkdr,
                        stats.Wlr,
                        stats.FinalKills,
                        stats.Wins,
                        sniperStr,
                        isLocalPlayer,
                        rankBrush,
                        stats.GetRankTag()
                    );
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AddDiagnosticLine($"{name}: ERROR {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    row.StatusText = "ERROR";
                    row.NameColor = Brushes.Red;
                });
            }
        }

        private void OnLogTailerError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDiagnosticLine($"[ERROR] {error}");
            }));
        }

        private static double CalculateThreatPercentage(BedwarsStats stats)
        {
            double threatPct;
            if (stats.Fkdr >= 10.0) threatPct = stats.Fkdr * 1.5;
            else if (stats.Fkdr >= 5.0) threatPct = stats.Fkdr;
            else if (stats.Fkdr >= 1.0) threatPct = stats.Fkdr * 0.5;
            else threatPct = stats.Fkdr * 0.2;

            if (stats.HasWinstreak && stats.Winstreak > 5)
            {
                threatPct += stats.Winstreak * 0.8;
            }

            return Math.Min(threatPct, 100.0);
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
            _lookupCts?.Cancel();
            _lookupCts?.Dispose();
            _hotkeyService?.Dispose();
            _logTailer?.Stop();
            base.OnClosed(e);
        }
    }
}
