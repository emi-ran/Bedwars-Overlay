using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BedwarsOverlay.Core.LogWatching;

namespace BedwarsOverlay.Core.Storage
{
    public class AppSettings
    {
        public string HypixelApiKeyEncrypted { get; set; } = string.Empty;
        public string LogFilePath { get; set; } = string.Empty;
        public int CacheTtlMinutes { get; set; } = 60;
        public string LocalPlayerName { get; set; } = string.Empty;
        public bool ShowLogDiagnostics { get; set; } = false;
        public bool IgnorePartyPlayers { get; set; } = false;
        public bool IgnoreLocalPlayer { get; set; } = false;
        public string IgnoredPlayers { get; set; } = string.Empty;
        public string SortColumn { get; set; } = string.Empty;
        public string SortDirection { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool HasCompletedOnboarding { get; set; } = false;
        public bool ShowLevelColumn { get; set; } = true;
        public bool ShowWinstreakColumn { get; set; } = true;
        public bool ShowFkdrColumn { get; set; } = true;
        public bool ShowWlrColumn { get; set; } = true;
        public bool ShowFinalKillsColumn { get; set; } = true;
        public bool ShowWinsColumn { get; set; } = true;
        public bool ShowSniperColumn { get; set; } = true;
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowTop { get; set; } = double.NaN;
        public double WindowWidth { get; set; } = double.NaN;
        public double WindowHeight { get; set; } = double.NaN;
        public string SkippedUpdateVersion { get; set; } = string.Empty;
    }

    public class SettingsRepository
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;
        private readonly HashSet<string> _dirtyProperties = new();

        public SettingsRepository()
        {
            string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BedwarsOverlay");
            if (!Directory.Exists(localAppFolder))
            {
                Directory.CreateDirectory(localAppFolder);
            }
            _settingsFilePath = Path.Combine(localAppFolder, "settings.json");
            _settings = LoadSettings();
        }

        /// <summary>
        /// Retrieves or updates the custom Minecraft log file path.
        /// Falls back to auto-detected Badlion/Vanilla paths if empty.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_settings.LogFilePath))
                {
                    return LogPathDetector.GetDefaultLogPath();
                }
                return _settings.LogFilePath;
            }
            set
            {
                _settings.LogFilePath = value;
                _dirtyProperties.Add(nameof(AppSettings.LogFilePath));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets the database cache TTL (Default: 60 minutes).
        /// </summary>
        public int CacheTtlMinutes
        {
            get => _settings.CacheTtlMinutes <= 0 ? 60 : _settings.CacheTtlMinutes;
            set
            {
                _settings.CacheTtlMinutes = value;
                _dirtyProperties.Add(nameof(AppSettings.CacheTtlMinutes));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets the local player name to highlight in the overlay.
        /// </summary>
        public string LocalPlayerName
        {
            get => _settings.LocalPlayerName;
            set
            {
                _settings.LocalPlayerName = value;
                _dirtyProperties.Add(nameof(AppSettings.LocalPlayerName));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets whether log diagnostics are visible in edit mode.
        /// </summary>
        public bool ShowLogDiagnostics
        {
            get => _settings.ShowLogDiagnostics;
            set
            {
                _settings.ShowLogDiagnostics = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowLogDiagnostics));
                Save();
            }
        }

        public bool IgnorePartyPlayers
        {
            get => _settings.IgnorePartyPlayers;
            set
            {
                _settings.IgnorePartyPlayers = value;
                _dirtyProperties.Add(nameof(AppSettings.IgnorePartyPlayers));
                Save();
            }
        }

        public bool IgnoreLocalPlayer
        {
            get => _settings.IgnoreLocalPlayer;
            set
            {
                _settings.IgnoreLocalPlayer = value;
                _dirtyProperties.Add(nameof(AppSettings.IgnoreLocalPlayer));
                Save();
            }
        }

        public string IgnoredPlayers
        {
            get => _settings.IgnoredPlayers;
            set
            {
                _settings.IgnoredPlayers = value;
                _dirtyProperties.Add(nameof(AppSettings.IgnoredPlayers));
                Save();
            }
        }

        public void AddIgnoredPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName) || !System.Text.RegularExpressions.Regex.IsMatch(playerName, "^[a-zA-Z0-9_]{3,16}$"))
            {
                return;
            }

            var players = GetIgnoredPlayers();
            if (!players.Exists(player => player.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
            {
                players.Add(playerName);
                IgnoredPlayers = string.Join(Environment.NewLine, players);
            }
        }

        public List<string> GetIgnoredPlayers()
        {
            var players = new List<string>();
            string[] rawPlayers = _settings.IgnoredPlayers.Split(new[] { '\r', '\n', ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawPlayer in rawPlayers)
            {
                string player = rawPlayer.Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(player, "^[a-zA-Z0-9_]{3,16}$") && !players.Exists(existing => existing.Equals(player, StringComparison.OrdinalIgnoreCase)))
                {
                    players.Add(player);
                }
            }

            return players;
        }

        /// <summary>
        /// Gets or sets the column used for sorting the player overlay.
        /// </summary>
        public string SortColumn
        {
            get => _settings.SortColumn;
            set
            {
                _settings.SortColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.SortColumn));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets the direction of sorting.
        /// </summary>
        public string SortDirection
        {
            get => _settings.SortDirection;
            set
            {
                _settings.SortDirection = value;
                _dirtyProperties.Add(nameof(AppSettings.SortDirection));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets the UI language ("en" or "tr").
        /// Empty string means auto-detect from system culture.
        /// </summary>
        public string Language
        {
            get => _settings.Language;
            set
            {
                _settings.Language = value;
                _dirtyProperties.Add(nameof(AppSettings.Language));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets whether the first-run intro/tutorial was completed.
        /// </summary>
        public bool HasCompletedOnboarding
        {
            get => _settings.HasCompletedOnboarding;
            set
            {
                _settings.HasCompletedOnboarding = value;
                _dirtyProperties.Add(nameof(AppSettings.HasCompletedOnboarding));
                Save();
            }
        }

        /// <summary>
        /// Gets or sets the Hypixel API key. 
        /// Transparently encrypts/decrypts the key using Windows DPAPI.
        /// </summary>
        public string ApiKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_settings.HypixelApiKeyEncrypted))
                {
                    return string.Empty;
                }

                try
                {
                    byte[] encryptedData = Convert.FromBase64String(_settings.HypixelApiKeyEncrypted);
                    byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedData);
                }
                catch
                {
                    return string.Empty; // Return empty on decryption errors (e.g., changed machine context)
                }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _settings.HypixelApiKeyEncrypted = string.Empty;
                }
                else
                {
                    try
                    {
                        byte[] plainData = Encoding.UTF8.GetBytes(value);
                        byte[] encryptedData = ProtectedData.Protect(plainData, null, DataProtectionScope.CurrentUser);
                        _settings.HypixelApiKeyEncrypted = Convert.ToBase64String(encryptedData);
                    }
                    catch
                    {
                        _settings.HypixelApiKeyEncrypted = string.Empty;
                    }
                }
                _dirtyProperties.Add(nameof(AppSettings.HypixelApiKeyEncrypted));
                Save();
            }
        }

        public bool ShowLevelColumn
        {
            get => _settings.ShowLevelColumn;
            set
            {
                _settings.ShowLevelColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowLevelColumn));
                Save();
            }
        }

        public bool ShowWinstreakColumn
        {
            get => _settings.ShowWinstreakColumn;
            set
            {
                _settings.ShowWinstreakColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowWinstreakColumn));
                Save();
            }
        }

        public bool ShowFkdrColumn
        {
            get => _settings.ShowFkdrColumn;
            set
            {
                _settings.ShowFkdrColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowFkdrColumn));
                Save();
            }
        }

        public bool ShowWlrColumn
        {
            get => _settings.ShowWlrColumn;
            set
            {
                _settings.ShowWlrColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowWlrColumn));
                Save();
            }
        }

        public bool ShowFinalKillsColumn
        {
            get => _settings.ShowFinalKillsColumn;
            set
            {
                _settings.ShowFinalKillsColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowFinalKillsColumn));
                Save();
            }
        }

        public bool ShowWinsColumn
        {
            get => _settings.ShowWinsColumn;
            set
            {
                _settings.ShowWinsColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowWinsColumn));
                Save();
            }
        }

        public bool ShowSniperColumn
        {
            get => _settings.ShowSniperColumn;
            set
            {
                _settings.ShowSniperColumn = value;
                _dirtyProperties.Add(nameof(AppSettings.ShowSniperColumn));
                Save();
            }
        }

        public double WindowLeft
        {
            get => _settings.WindowLeft;
            set
            {
                _settings.WindowLeft = value;
                _dirtyProperties.Add(nameof(AppSettings.WindowLeft));
                Save();
            }
        }

        public double WindowTop
        {
            get => _settings.WindowTop;
            set
            {
                _settings.WindowTop = value;
                _dirtyProperties.Add(nameof(AppSettings.WindowTop));
                Save();
            }
        }

        public double WindowWidth
        {
            get => _settings.WindowWidth;
            set
            {
                _settings.WindowWidth = value;
                _dirtyProperties.Add(nameof(AppSettings.WindowWidth));
                Save();
            }
        }

        public double WindowHeight
        {
            get => _settings.WindowHeight;
            set
            {
                _settings.WindowHeight = value;
                _dirtyProperties.Add(nameof(AppSettings.WindowHeight));
                Save();
            }
        }

        public void SaveWindowBounds(double left, double top, double width, double height)
        {
            _settings.WindowLeft = left;
            _settings.WindowTop = top;
            _settings.WindowWidth = width;
            _settings.WindowHeight = height;
            _dirtyProperties.Add(nameof(AppSettings.WindowLeft));
            _dirtyProperties.Add(nameof(AppSettings.WindowTop));
            _dirtyProperties.Add(nameof(AppSettings.WindowWidth));
            _dirtyProperties.Add(nameof(AppSettings.WindowHeight));
            Save();
        }

        public string SkippedUpdateVersion
        {
            get => _settings.SkippedUpdateVersion;
            set
            {
                _settings.SkippedUpdateVersion = value;
                _dirtyProperties.Add(nameof(AppSettings.SkippedUpdateVersion));
                Save();
            }
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // Suppress load issues and return empty settings
            }

            return new AppSettings();
        }

        private void Save()
        {
            try
            {
                AppSettings latestSettings = LoadSettings();
                ApplyDirtySettings(latestSettings);
                string json = JsonSerializer.Serialize(latestSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
                _settings = latestSettings;
                _dirtyProperties.Clear();
            }
            catch
            {
                // Suppress save issues for reliability
            }
        }

        private void ApplyDirtySettings(AppSettings target)
        {
            foreach (string propertyName in _dirtyProperties)
            {
                switch (propertyName)
                {
                    case nameof(AppSettings.HypixelApiKeyEncrypted):
                        target.HypixelApiKeyEncrypted = _settings.HypixelApiKeyEncrypted;
                        break;
                    case nameof(AppSettings.LogFilePath):
                        target.LogFilePath = _settings.LogFilePath;
                        break;
                    case nameof(AppSettings.CacheTtlMinutes):
                        target.CacheTtlMinutes = _settings.CacheTtlMinutes;
                        break;
                    case nameof(AppSettings.LocalPlayerName):
                        target.LocalPlayerName = _settings.LocalPlayerName;
                        break;
                    case nameof(AppSettings.ShowLogDiagnostics):
                        target.ShowLogDiagnostics = _settings.ShowLogDiagnostics;
                        break;
                    case nameof(AppSettings.IgnorePartyPlayers):
                        target.IgnorePartyPlayers = _settings.IgnorePartyPlayers;
                        break;
                    case nameof(AppSettings.IgnoreLocalPlayer):
                        target.IgnoreLocalPlayer = _settings.IgnoreLocalPlayer;
                        break;
                    case nameof(AppSettings.IgnoredPlayers):
                        target.IgnoredPlayers = _settings.IgnoredPlayers;
                        break;
                    case nameof(AppSettings.SortColumn):
                        target.SortColumn = _settings.SortColumn;
                        break;
                    case nameof(AppSettings.SortDirection):
                        target.SortDirection = _settings.SortDirection;
                        break;
                    case nameof(AppSettings.Language):
                        target.Language = _settings.Language;
                        break;
                    case nameof(AppSettings.HasCompletedOnboarding):
                        target.HasCompletedOnboarding = _settings.HasCompletedOnboarding;
                        break;
                    case nameof(AppSettings.ShowLevelColumn):
                        target.ShowLevelColumn = _settings.ShowLevelColumn;
                        break;
                    case nameof(AppSettings.ShowWinstreakColumn):
                        target.ShowWinstreakColumn = _settings.ShowWinstreakColumn;
                        break;
                    case nameof(AppSettings.ShowFkdrColumn):
                        target.ShowFkdrColumn = _settings.ShowFkdrColumn;
                        break;
                    case nameof(AppSettings.ShowWlrColumn):
                        target.ShowWlrColumn = _settings.ShowWlrColumn;
                        break;
                    case nameof(AppSettings.ShowFinalKillsColumn):
                        target.ShowFinalKillsColumn = _settings.ShowFinalKillsColumn;
                        break;
                    case nameof(AppSettings.ShowWinsColumn):
                        target.ShowWinsColumn = _settings.ShowWinsColumn;
                        break;
                    case nameof(AppSettings.ShowSniperColumn):
                        target.ShowSniperColumn = _settings.ShowSniperColumn;
                        break;
                    case nameof(AppSettings.WindowLeft):
                        target.WindowLeft = _settings.WindowLeft;
                        break;
                    case nameof(AppSettings.WindowTop):
                        target.WindowTop = _settings.WindowTop;
                        break;
                    case nameof(AppSettings.WindowWidth):
                        target.WindowWidth = _settings.WindowWidth;
                        break;
                    case nameof(AppSettings.WindowHeight):
                        target.WindowHeight = _settings.WindowHeight;
                        break;
                    case nameof(AppSettings.SkippedUpdateVersion):
                        target.SkippedUpdateVersion = _settings.SkippedUpdateVersion;
                        break;
                }
            }
        }
    }
}
