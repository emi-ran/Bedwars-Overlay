using System;
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
        public string SortColumn { get; set; } = string.Empty;
        public string SortDirection { get; set; } = string.Empty;
    }

    public class SettingsRepository
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;

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
                Save();
            }
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
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Suppress save issues for reliability
            }
        }
    }
}
