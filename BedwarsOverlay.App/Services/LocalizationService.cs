using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace BedwarsOverlay.App.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new();

        private string _language = "en";
        private Dictionary<string, string> _strings = new();

        public string Language => _language;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? LanguageChanged;

        private LocalizationService()
        {
            _strings = _builtInEnglish;
        }

        public string GetString(string key)
        {
            if (_strings.TryGetValue(key, out var val))
                return val;
            if (_builtInEnglish.TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }

        public string this[string key] => GetString(key);

        // XAML-bound properties - updated via PropertyChanged("") on language change

        public string WarningTooltip => GetString("WarningTooltip");
        public string Tooltip_ClearPlayers => GetString("Tooltip_ClearPlayers");
        public string Tooltip_Settings => GetString("Tooltip_Settings");
        public string Search_Placeholder => GetString("Search_Placeholder");
        public string EditMode_Badge => GetString("EditMode_Badge");
        public string Placeholder_Active => GetString("Placeholder_Active");
        public string Placeholder_Waiting => GetString("Placeholder_Waiting");
        public string Diagnostics_Title => GetString("Diagnostics_Title");
        public string Column_Lvl => GetString("Column_Lvl");
        public string Column_Name => GetString("Column_Name");
        public string Column_WS => GetString("Column_WS");
        public string Column_FKDR => GetString("Column_FKDR");
        public string Column_WLR => GetString("Column_WLR");
        public string Column_FKills => GetString("Column_FKills");
        public string Column_Wins => GetString("Column_Wins");
        public string Column_Sniper => GetString("Column_Sniper");
        public string Tooltip_Lvl => GetString("Tooltip_Lvl");
        public string Tooltip_Name => GetString("Tooltip_Name");
        public string Tooltip_WS => GetString("Tooltip_WS");
        public string Tooltip_FKDR => GetString("Tooltip_FKDR");
        public string Tooltip_WLR => GetString("Tooltip_WLR");
        public string Tooltip_FKills => GetString("Tooltip_FKills");
        public string Tooltip_Wins => GetString("Tooltip_Wins");
        public string Tooltip_Sniper => GetString("Tooltip_Sniper");
        public string ContextMenu_CopyUsername => GetString("ContextMenu_CopyUsername");
        public string ContextMenu_OpenPlancke => GetString("ContextMenu_OpenPlancke");

        public void SetLanguage(string lang)
        {
            if (lang != "en" && lang != "tr")
                lang = "en";

            if (_language == lang)
                return;

            _language = lang;
            LoadDictionary(lang);

            // Notify all bound properties
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Initialize language from system culture and saved preference.
        /// </summary>
        public void Initialize(string savedLanguage)
        {
            // Determine effective language:
            // 1. Use saved preference if valid
            // 2. Fall back to system UI culture if supported (tr or en)
            // 3. Default to English

            string lang;
            if (savedLanguage == "en" || savedLanguage == "tr")
            {
                lang = savedLanguage;
            }
            else
            {
                var culture = CultureInfo.CurrentUICulture;
                lang = culture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";
            }

            _language = lang;
            LoadDictionary(lang);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }

        private void LoadDictionary(string lang)
        {
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Localization",
                $"{lang}.json");

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        _strings = loaded;
                        return;
                    }
                }
            }
            catch
            {
                // File missing, bad JSON, or deserialization failure.
                // Fall through to built-in English fallback.
            }

            // Fallback: use built-in English dictionary
            _strings = _builtInEnglish;
        }

        // ──────────────────────────────────────────────
        //  Built-in English fallback strings
        //  Used when JSON file is missing or fails to load.
        // ──────────────────────────────────────────────
        private static readonly Dictionary<string, string> _builtInEnglish = new()
        {
            // MainWindow - Title / Badge
            ["Title_BedwarsOverlay"] = "Bedwars Overlay",
            ["EditMode_Badge"] = "EDIT MODE",

            // MainWindow - Tooltips
            ["Tooltip_ClearPlayers"] = "Clear current player list",
            ["Tooltip_Settings"] = "Settings",

            // MainWindow - Search
            ["Search_Placeholder"] = "Search player(s)",

            // MainWindow - Placeholder
            ["Placeholder_Active"] = "Bedwars Overlay Active",
            ["Placeholder_Waiting"] = "Waiting for Minecraft logs... Type /who in-game.",

            // MainWindow - Column Headers
            ["Column_Lvl"] = "Lvl",
            ["Column_Name"] = "Name",
            ["Column_WS"] = "WS",
            ["Column_FKDR"] = "FKDR",
            ["Column_WLR"] = "WLR",
            ["Column_FKills"] = "F. Kills",
            ["Column_Wins"] = "Wins",
            ["Column_Sniper"] = "Sniper",

            // MainWindow - Column Tooltips
            ["Tooltip_Lvl"] = "BedWars Level (Stars & Prestige)",
            ["Tooltip_Name"] = "Minecraft Username (Local player marked in blue with badges)",
            ["Tooltip_WS"] = "BedWars Winstreak (WS)",
            ["Tooltip_FKDR"] = "Final Kill / Death Ratio (FKDR)",
            ["Tooltip_WLR"] = "Win / Loss Ratio (WLR)",
            ["Tooltip_FKills"] = "Total BedWars Final Kills",
            ["Tooltip_Wins"] = "Total BedWars Wins",
            ["Tooltip_Sniper"] = "Threat Score / Sniper Chance",

            // MainWindow - Context Menu
            ["ContextMenu_CopyUsername"] = "Copy Username",
            ["ContextMenu_OpenPlancke"] = "Open Plancke Stats",

            // MainWindow - Diagnostics
            ["Diagnostics_Title"] = "LOG DIAGNOSTICS (Last 20 Log Lines):",

            // MainWindow - DataTemplate Tooltip
            ["WarningTooltip"] = "High Threat: Player has extreme stats (FKDR >= 5.0)!",

            // Status messages
            ["Status_ConfigureApi"] = "Configure Hypixel API key in Settings.",
            ["Status_WaitingWho"] = "Waiting for /who in Minecraft...",
            ["Status_LogNotFound"] = "Minecraft log file not found. Check Settings.",
            ["Status_Cleared"] = "Current player list cleared.",
            ["Status_CannotWatchLog"] = "Could not start Minecraft log watcher. Check Settings.",

            // Dialog titles / messages
            ["Error_Title"] = "Error",
            ["Init_ErrorTitle"] = "Initialization Error",
            ["Init_FailedDb"] = "Failed to initialize database/settings: {0}",
            ["Hotkey_ErrorTitle"] = "Hotkey Registration Error",
            ["Hotkey_Failed"] = "Failed to register global hotkey Ctrl+Alt+O! Please check if another app is using this shortcut.",
            ["Hotkey_InitError"] = "Error initializing hotkey service: {0}",
            ["Clipboard_Error"] = "Failed to copy to clipboard: {0}",
            ["Plancke_Error"] = "Failed to open Plancke link: {0}",

            // SettingsWindow
            ["Settings_Title"] = "Overlay Settings",
            ["Label_ApiKey"] = "Hypixel API Key",
            ["Label_LogPath"] = "Minecraft Client Log Path (latest.log)",
            ["Label_LocalPlayer"] = "Local Player Username (Optional, highlights yourself in blue)",
            ["Checkbox_Diagnostics"] = "Show Log Diagnostics in edit mode",
            ["Button_AutoDetect"] = "Auto-Detect",
            ["Button_Browse"] = "Browse...",
            ["Button_Cancel"] = "Cancel",
            ["Button_SaveSettings"] = "Save Settings",
            ["Tooltip_ShowApiKey"] = "Show API key",
            ["Tooltip_HideApiKey"] = "Hide API key",

            // SettingsWindow - Dialog
            ["Browse_Title"] = "Select Minecraft latest.log File",
            ["Detection_Title"] = "Log Path Detection",
            ["Detection_NotFound"] = "Auto-detected path is not a valid file:\n{0}\n\nPlease click Browse to select it manually.",
            ["Detection_ErrorTitle"] = "Log Path Detection Error",
            ["Detection_Failed"] = "Failed to auto-detect log path: {0}",

            // Language
            ["Lang_Label"] = "Language",
            ["Lang_English"] = "English",
            ["Lang_Turkish"] = "Türkçe",
        };
    }
}
