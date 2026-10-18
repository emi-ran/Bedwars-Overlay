using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        private readonly HashSet<string> _partyPlayers = new(StringComparer.OrdinalIgnoreCase);
        private const int FinalKillRemoveDelayMs = 2500;

        private AppDatabase? _database;
        private SettingsRepository? _settingsRepository;
        private MojangClient? _mojangClient;
        private HypixelClient? _hypixelClient;
        private PlayerLookupService? _playerLookupService;
        private EncounterRepository? _encounterRepository;
        private long? _currentMatchId;
        private long? _lastEndedMatchId;
        private UpdateCheckService? _updateCheckService;
        private UpdateCheckResult? _availableUpdate;
        private TrayIconService? _trayIconService;
        private bool _showingConfigNeeded;
        private bool _isSettingsOpen;
        private bool _isHiddenMode;
        private bool _needsConfigPrompt;
        private bool _isExiting;
        private bool _hasLoaded;
        private readonly DispatcherTimer _windowBoundsSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            _windowBoundsSaveTimer.Tick += WindowBoundsSaveTimer_Tick;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize database settings and API clients
            try
            {
                _database = new AppDatabase();
                _settingsRepository = new SettingsRepository();
                RestoreWindowBounds();
                _mojangClient = new MojangClient(_httpClient, _database);
                _hypixelClient = new HypixelClient(_httpClient, _database, _settingsRepository);
                _updateCheckService = new UpdateCheckService(_httpClient);
                _encounterRepository = new EncounterRepository(_database);
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

            _playerLookupService = new PlayerLookupService(_mojangClient, _hypixelClient, _settingsRepository, AddDiagnosticLine, _encounterRepository);
            GridLocalizer.ApplyLocalization(PlayersGrid);
            ApplyColumnVisibility();
            _trayIconService = new TrayIconService(this, ShowOverlayFromTray, HideOverlayToTray, OpenSettings, ExitFromTray);

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
                listColView.LiveSortingProperties.Add("ThreatRaw");
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

            _hasLoaded = true;
            _ = CheckForUpdatesAsync(showPrompt: true);

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

        private void UpdateDiagnosticsVisibility()
        {
            if (DiagnosticsPanel == null)
            {
                return;
            }

            bool showDiagnostics = _isEditMode && _settingsRepository?.ShowLogDiagnostics == true;
            DiagnosticsPanel.Visibility = showDiagnostics ? Visibility.Visible : Visibility.Collapsed;
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

        protected override void OnClosed(EventArgs e)
        {
            _windowBoundsSaveTimer.Stop();
            SaveWindowBounds();
            _trayIconService?.Dispose();
            _playerLookupService?.Dispose();
            _hotkeyService?.Dispose();
            _logTailer?.Stop();
            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                HideOverlayToTray();
                return;
            }

            base.OnClosing(e);
        }
    }
}
