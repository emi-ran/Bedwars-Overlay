# Minecraft BedWars Overlay - Project Progress & Handover

This document is the current project state and handover guide. Read it before continuing implementation work.

---

## 1. Project Goal

Develop a **Windows-only external stats overlay** for Minecraft BedWars. The app reads `latest.log`, uses public Mojang/Hypixel APIs, caches lookups locally in SQLite, and displays useful Bed Wars stats in a transparent, topmost, click-through overlay.

*No memory injection, macro commands, memory reading, or packet sniffing.*

---

## 2. Technical Architecture

- **Stack:** C# .NET 8, WPF, Windows Forms `NotifyIcon` for system tray integration.
- **Solution:** `BedwarsOverlay.slnx`.
- **Projects:**
  - `BedwarsOverlay.App` (WPF desktop app, overlay UI, settings UI, tray icon, hotkeys, localization, Win32 window behavior)
  - `BedwarsOverlay.Core` (log watching, parsing, Mojang/Hypixel API clients, SQLite storage, settings, stat calculation, player row model)
  - `BedwarsOverlay.Tests` (xUnit tests)
- **Target framework:** all projects target `net8.0-windows`.
- **Storage:** settings and cache data are stored under `%LOCALAPPDATA%\BedwarsOverlay`.
- **Security:** Hypixel API key is encrypted locally with Windows DPAPI before being saved to JSON settings.

---

## 3. Implemented Features

- Reads Minecraft `latest.log` and detects `/who` output.
- Parses `/who` player lists, including Minecraft color/formatting codes.
- Supports overlay chat commands through Hypixel unknown-command log echoes: `/bwstats`, `/bwignore`, `/bwclear`, and `/bwhelp`.
- Supports `/bwstats <name...>` to load specific players while bypassing ignore filters.
- Supports `/bwignore <name...>` plus Settings ignored-player management to hide selected players from filtered lists.
- Supports `/p list` parsing for party-member detection and optional party ignore.
- Supports optional self ignore based on the configured local player name.
- Supports `/bwclear` to clear the current overlay list and `/bwhelp` to show localized command help.
- Detects Bed Wars end-of-game summary output and clears the current player list.
- Marks players with a red `FINAL KILL` row state before removing them when a matching Hypixel `FINAL KILL!` log line appears.
- Resolves usernames through Mojang API with local SQLite cache.
- Fetches Hypixel Bed Wars stats with API-key authentication and local SQLite cache.
- Calculates level, FKDR, WLR, wins, final kills, winstreak, and sniper risk display values.
- Renders a styled WPF overlay table with sorting and search.
- Supports visible column settings for `LVL`, `WS`, `FKDR`, `WLR`, `FKILLS`, `WINS`, and `SNIPER`.
- Supports English and Turkish localization through JSON files.
- Provides Settings UI for API key, log path, local player name, diagnostics, language, visible columns, ignored players, party ignore, and self ignore.
- Supports global hotkeys:
  - `Ctrl+Alt+O`: edit/play mode
  - `Ctrl+Alt+H`: hide/show overlay
- Starts in edit mode after onboarding so the overlay can be moved and resized.
- Saves and restores window position and size between runs.
- Hides from taskbar and Alt-Tab in play mode, appears there in edit mode, and keeps a system tray icon.
- Closing the overlay hides it to tray; tray menu can show/hide, open settings, or exit the app.
- Disables maximize/Aero snap behavior for the overlay window.
- Includes release packaging script and Inno Setup installer script.

---

## 4. Current File Structure

```txt
BedwarsOverlay/
├─ README.md
├─ README.tr.md
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ PROGRESS.md
├─ PLAN.md
├─ LICENSE
├─ HypixelApi.txt
├─ BedwarsOverlay.slnx
├─ scripts/
│  └─ publish-release.ps1
├─ installer/
│  └─ BedwarsOverlay.iss
├─ docs/
│  └─ assets/
│     ├─ overlay-with-players.png
│     └─ settings-window.png
├─ BedwarsOverlay.App/
│  ├─ BedwarsOverlay.App.csproj
│  ├─ App.xaml
│  ├─ App.xaml.cs
│  ├─ AssemblyInfo.cs
│  ├─ MainWindow.xaml
│  ├─ MainWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
│  ├─ Assets/
│  ├─ Localization/
│  │  ├─ en.json
│  │  └─ tr.json
│  └─ Services/
│     ├─ ClickThroughService.cs
│     ├─ GridLocalizer.cs
│     ├─ HotkeyService.cs
│     ├─ LocalizationService.cs
│     ├─ PlayerLookupService.cs
│     └─ TrayIconService.cs
├─ BedwarsOverlay.Core/
│  ├─ BedwarsOverlay.Core.csproj
│  ├─ Api/
│  │  ├─ HypixelClient.cs
│  │  └─ MojangClient.cs
│  ├─ LogWatching/
│  │  ├─ HypixelLogParser.cs
│  │  ├─ LogPathDetector.cs
│  │  └─ LogTailer.cs
│  ├─ Models/
│  │  └─ PlayerRow.cs
│  ├─ Stats/
│  │  ├─ BedwarsStats.cs
│  │  └─ StatsCalculator.cs
│  └─ Storage/
│     ├─ AppDatabase.cs
│     └─ SettingsRepository.cs
└─ BedwarsOverlay.Tests/
   ├─ BedwarsOverlay.Tests.csproj
   ├─ ParserTests.cs
   ├─ StatsTests.cs
   └─ UnitTest1.cs
```

---

## 5. Build & Test

From the root workspace folder:

```powershell
dotnet build "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
dotnet test "BedwarsOverlay.slnx"
```

Current expected state: build succeeds with 0 errors, tests pass. If the running app locks `BedwarsOverlay.App\bin\Debug\net8.0-windows\BedwarsOverlay.Core.dll`, close the app or build the app project with a temporary `OutputPath`.

---

## 6. Notes For Future Work

- Keep user-facing text in both `BedwarsOverlay.App/Localization/en.json` and `BedwarsOverlay.App/Localization/tr.json`.
- Update both README files when setup, usage, or user-facing behavior changes.
- `PLAN.md` is now reserved for forward-looking work. `PROGRESS.md` is the current-state handover.
