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
            _strings = _builtInEnglish ?? new Dictionary<string, string>();
        }

        public string GetString(string key)
        {
            if (_strings != null && _strings.TryGetValue(key, out var val))
                return val;
            if (_builtInEnglish != null && _builtInEnglish.TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }

        public string this[string key] => GetString(key);
        public string Connection_Disconnected => GetString("Connection_Disconnected");

        // XAML-bound properties - updated via PropertyChanged("") on language change

        public string WarningTooltip => GetString("WarningTooltip");
        public string Tooltip_ClearPlayers => GetString("Tooltip_ClearPlayers");
        public string Tooltip_Settings => GetString("Tooltip_Settings");
        public string Search_Placeholder => GetString("Search_Placeholder");
        public string EditMode_Badge => GetString("EditMode_Badge");
        public string Shortcuts_Help => GetString("Shortcuts_Help");
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
        public string Column_Threat => GetString("Column_Threat");
        public string Tooltip_Lvl => GetString("Tooltip_Lvl");
        public string Tooltip_Name => GetString("Tooltip_Name");
        public string Tooltip_WS => GetString("Tooltip_WS");
        public string Tooltip_FKDR => GetString("Tooltip_FKDR");
        public string Tooltip_WLR => GetString("Tooltip_WLR");
        public string Tooltip_FKills => GetString("Tooltip_FKills");
        public string Tooltip_Wins => GetString("Tooltip_Wins");
        public string Tooltip_Threat => GetString("Tooltip_Threat");
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
            ["Split_TeammateDied"] = "{0} died · Wait for your teammate",
            ["Connection_Disconnected"] = "Disconnected — may rejoin while still in the match.",
            // MainWindow - Title / Badge
            ["Title_BedwarsOverlay"] = "Bedwars Overlay",
            ["Title_BedwarsOverlayShort"] = "BW Overlay",
            ["EditMode_Badge"] = "EDIT MODE",
            ["EditMode_BadgeShort"] = "EDIT",

            // MainWindow - Tooltips
            ["Tooltip_ClearPlayers"] = "Clear current player list",
            ["Tooltip_Settings"] = "Settings",
            ["Update_Button"] = "Update available: {0}",

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
            ["Column_Threat"] = "Threat",
            ["Column_Team"] = "Team",

            // MainWindow - Column Tooltips
            ["Tooltip_Lvl"] = "BedWars Level (Stars & Prestige)",
            ["Tooltip_Name"] = "Minecraft Username (Local player marked in blue with badges)",
            ["Tooltip_WS"] = "BedWars Winstreak (WS)",
            ["Tooltip_FKDR"] = "Final Kill / Death Ratio (FKDR)",
            ["Tooltip_WLR"] = "Win / Loss Ratio (WLR)",
            ["Tooltip_FKills"] = "Total BedWars Final Kills",
            ["Tooltip_Wins"] = "Total BedWars Wins",
            ["Tooltip_Threat"] = "Threat Score (0-100) from FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account and repeat-opponent signals",
            ["Tooltip_Team"] = "Team read from the visible Minecraft player list",

            // MainWindow - Context Menu
            ["ContextMenu_CopyUsername"] = "Copy Username",
            ["ContextMenu_OpenPlancke"] = "Open Plancke Stats",

            // MainWindow - Diagnostics
            ["Diagnostics_Title"] = "LOG DIAGNOSTICS (Last 20 Log Lines):",

            // MainWindow - DataTemplate Tooltip
            ["WarningTooltip"] = "High Threat: this player scores in the extreme threat band.",
            ["Repeat_Tooltip"] = "Repeat opponent: seen in {0} of the last {1} tracked matches.",

            // Status messages
            ["Status_ConfigureApi"] = "Configure Hypixel API key in Settings.",
            ["Status_WaitingWho"] = "Waiting for /who in Minecraft...",
            ["Status_LogNotFound"] = "Minecraft log file not found. Check Settings.",
            ["Status_Cleared"] = "Current player list cleared.",
            ["Status_MatchEnded"] = "Game ended. Current player list cleared.",
            ["Status_CommandHelp"] = "Bedwars Overlay commands shown below.",
            ["Status_CannotWatchLog"] = "Could not start Minecraft log watcher. Check Settings.",

            // Dialog titles / messages
            ["Error_Title"] = "Error",
            ["Init_ErrorTitle"] = "Initialization Error",
            ["Init_FailedDb"] = "Failed to initialize database/settings: {0}",
            ["Hotkey_ErrorTitle"] = "Hotkey Registration Error",
            ["Hotkey_Failed"] = "Failed to register global hotkeys Ctrl+Alt+O / Ctrl+Alt+H / END! Please check if another app is using these shortcuts.",
            ["Hotkey_InitError"] = "Error initializing hotkey service: {0}",
            ["Clipboard_Error"] = "Failed to copy to clipboard: {0}",
            ["Plancke_Error"] = "Failed to open Plancke link: {0}",
            ["OpenUrl_Error"] = "Failed to open link: {0}",
            ["Update_Title"] = "Update Available",
            ["Update_Prompt"] = "A new Bedwars Overlay version is available.\n\nCurrent version: {0}\nLatest version: {1}\n\nYes: open download page\nNo: remind me later\nCancel: skip this version",
            ["Shortcuts_Title"] = "Shortcuts",
            ["Shortcuts_Help"] = "Ctrl+Alt+O toggles edit mode. Ctrl+Alt+H hides or shows the overlay. Hold TAB to open Minecraft's player list, then press END to read teams. Use /bwstats name(s) to load specific players and /bwignore name(s) to hide players.",
            ["Teams_LoadPlayersFirst"] = "Load players with /who before reading teams.",
            ["Teams_FocusMinecraft"] = "Focus Minecraft, hold TAB to keep the player list open, then press END.",
            ["Teams_HoldTab"] = "Hold TAB to keep the player list open, then press END.",
            ["Teams_Reading"] = "Reading teams from the visible player list...",
            ["Teams_NoneFound"] = "No matching team rows found. Keep the player list open and try again.",
            ["Teams_Updated"] = "Team data updated for {0} player(s).",
            ["Teams_Failed"] = "Could not read the player list. Keep Minecraft focused and try again.",
            ["Onboarding_Title"] = "Welcome to Bedwars Overlay",
            ["Onboarding_Body"] = "Track players from /who, spot risky stats, and keep the overlay out of your way while playing.",
            ["Onboarding_EditShortcut"] = "Ctrl+Alt+O: edit / play mode",
            ["Onboarding_HideShortcut"] = "Ctrl+Alt+H: hide/show overlay",
            ["Onboarding_Settings"] = "Settings is where you add your Hypixel API key and Minecraft log path.",
            ["Onboarding_Done"] = "Got it",
            ["Onboarding_OpenSettings"] = "Open Settings",

            // SettingsWindow
            ["Settings_Title"] = "Overlay Settings",
            ["Label_ApiKey"] = "Hypixel API Key",
            ["Label_LogPath"] = "Minecraft Client Log Path (latest.log)",
            ["Label_LocalPlayer"] = "Local Player Username (Optional, highlights yourself in blue)",
            ["Label_IgnoredPlayers"] = "Ignored Players (one name per line, or comma-separated)",
            ["Label_EncounterMemory"] = "Repeat opponents",
            ["Checkbox_HighlightRepeat"] = "Mark players seen in earlier matches with a \u00d7N badge",
            ["Label_EncounterLookback"] = "Matches to look back:",
            ["Label_EncounterMinCount"] = "Min. matches:",
            ["Checkbox_IgnorePartyPlayers"] = "Ignore party players (auto or /p list)",
            ["Checkbox_IgnoreLocalPlayer"] = "Ignore yourself",
            ["Checkbox_Diagnostics"] = "Show Log Diagnostics in edit mode",
            ["Button_AutoDetect"] = "Auto-Detect",
            ["Button_Browse"] = "Browse...",
            ["Button_GetApiKey"] = "Get API Key",
            ["Button_CheckApiKey"] = "Check",
            ["Button_Cancel"] = "Cancel",
            ["Button_SaveSettings"] = "Save Settings",
            ["Button_GitHub"] = "GitHub",
            ["App_Version"] = "Version {0}",
            ["ApiKey_StatusIdle"] = "Use a Hypixel developer API key.",
            ["ApiKey_StatusChecking"] = "Checking API key...",
            ["ApiKey_StatusValid"] = "API key is valid.",
            ["ApiKey_StatusMissing"] = "Enter an API key first.",
            ["ApiKey_StatusInvalid"] = "API key is invalid.",
            ["ApiKey_StatusRateLimited"] = "Hypixel rate limited this check. Try again later.",
            ["ApiKey_StatusError"] = "Could not check API key right now.",
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
            ["Status_PartyStatsUnavailable"] = "Party list is empty. Join a party or run /p list.",
            ["Status_PartyStatsLoaded"] = "Loading stats for {0} party player(s).",
            ["Help_Title"] = "Bedwars Overlay Commands",
            ["Help_Commands_Row1"] = "/who game · /p list refresh party",
            ["Help_Commands_Row2"] = "/pstats party stats · /bwstats <names>",
            ["Help_Commands_Row3"] = "/bwignore <names> hide · /bwclear clear",
            ["Help_Commands_Row4"] = "/bwhelp show help",
        };
    }
}
