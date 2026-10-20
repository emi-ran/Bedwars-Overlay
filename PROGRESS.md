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
- **Main window organization:** `MainWindow` is split into partial code-behind files by concern: startup/lifecycle, window state, grid/search, log handling, player commands, settings, updates, and onboarding.

---

## 3. Implemented Features

- Added a bilingual static product website in `docs/`, with an illustrative interactive team/Threat demo, responsive layout and reduced-motion support. GitHub Pages uses `main` / `/docs`. See [Site maintenance](docs/SITE.md).
- Reads Minecraft `latest.log` and detects `/who` output.
- Reads Hypixel team prefixes from a user-triggered capture of the visible Minecraft player list (`END`, while holding `TAB`) and shows the result in a sortable Team column. OCR runs locally using bundled Tesseract; no client mod or separate user installation is required.
- Team OCR scans the left and center player-list region, crops masks to visible team-colored text, reuses the OCR engine, and leaves ambiguous or uncolored names blank. Captures stay at native resolution until the OCR crop is enlarged once.
- Publishing a GitHub Release triggers `.github/workflows/release.yml` to build and attach the signed Windows installer and portable ZIP. Repository secrets `WINDOWS_SIGNING_CERTIFICATE_BASE64` and `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` are required.
- Parses `/who` player lists, including Minecraft color/formatting codes.
- Supports overlay chat commands through Hypixel unknown-command log echoes: `/bwstats`, `/bwignore`, `/bwclear`, and `/bwhelp`.
- Supports `/bwstats <name...>` to load specific players while bypassing ignore filters.
- Supports `/bwignore <name...>` plus Settings ignored-player management to hide selected players from filtered lists.
- Supports `/p list` parsing for party-member detection and optional party ignore.
- Supports optional self ignore based on the configured local player name.
- Supports `/bwclear` to clear the current overlay list and `/bwhelp` to show localized command help.
- Detects Bed Wars start and end blocks (win and loss) and clears the current player list when a game ends.
- Marks players with a red `FINAL KILL` row state before removing them when a matching Hypixel `FINAL KILL!` log line appears.
- Resolves usernames through Mojang API with local SQLite cache.
- Fetches Hypixel Bed Wars stats with API-key authentication and local SQLite cache.
- Calculates level, FKDR, WLR, wins, final kills, and winstreak display values.
- Shows a 0-100 threat score (`ThreatScorer`) built from FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, and repeat-opponent signals, with Low / Medium / High / Extreme bands and a per-row signal tooltip.
- Tracks which player was in which match (`matches` + `encounters`) and marks repeat opponents with a `×N` badge on the name column.
- Detects the Bed Wars start block and the end block (win and loss) from the client log; the current player list is cleared on game end for both outcomes.
- Applies versioned schema migrations to `%LOCALAPPDATA%\BedwarsOverlay\data.db` through `PRAGMA user_version` (v1 = caches, v2 = match tracking).
- Renders a styled WPF overlay table with sorting and search.
- Supports visible column settings for `LVL`, `WS`, `FKDR`, `WLR`, `FKILLS`, `WINS`, and `THREAT`.
- Supports repeat-opponent highlighting settings: lookback match count, minimum match count, and on/off.
- Supports English and Turkish localization through JSON files.
- Provides Settings UI for API key, log path, local player name, diagnostics, language, visible columns, ignored players, party ignore, and self ignore.
- Supports global hotkeys:
  - `Ctrl+Alt+O`: edit/play mode
  - `Ctrl+Alt+H`: hide/show overlay
  - `END`: read teams from the visible Minecraft player list (hold `TAB`)
- Starts in edit mode after onboarding so the overlay can be moved and resized.
- Saves and restores window position and size between runs.
- Hides from taskbar and Alt-Tab in play mode, appears there in edit mode, and keeps a system tray icon.
- Closing the overlay hides it to tray; tray menu can show/hide, open settings, or exit the app.
- Uses a named mutex so a second launch exits before opening another overlay process.
- Checks GitHub latest release for updates, shows localized update prompts, supports skipping a specific version, and exposes an edit-mode update button when an update is available.
- Shows the current app version in Settings.
- Disables maximize/Aero snap behavior for the overlay window.
- Merges settings writes with the latest on-disk settings so stale repository instances do not overwrite newer saved fields.
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
│  ├─ MainWindow.Grid.cs
│  ├─ MainWindow.Log.cs
│  ├─ MainWindow.Onboarding.cs
│  ├─ MainWindow.PlayerCommands.cs
│  ├─ MainWindow.Settings.cs
│  ├─ MainWindow.Update.cs
│  ├─ MainWindow.WindowState.cs
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
│     ├─ TrayIconService.cs
│     └─ UpdateCheckService.cs
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
│  │  ├─ StatsCalculator.cs
│  │  └─ ThreatScorer.cs
│  └─ Storage/
│     ├─ AppDatabase.cs
│     ├─ EncounterRepository.cs
│     └─ SettingsRepository.cs
└─ BedwarsOverlay.Tests/
   ├─ BedwarsOverlay.Tests.csproj
   ├─ GameBlockParserTests.cs
   ├─ ParserTests.cs
   ├─ StatsTests.cs
   ├─ StorageTests.cs
   ├─ ThreatScorerTests.cs
   └─ UnitTest1.cs
```

---

## 5. Build & Test

From the root workspace folder:

```powershell
dotnet build "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
dotnet test "BedwarsOverlay.Tests\BedwarsOverlay.Tests.csproj"
```

`dotnet test` does not accept `BedwarsOverlay.slnx` directly, so point it at the test project.

Current expected state: build succeeds with 0 errors and 0 warnings, and all 115 tests pass. If the running app locks `BedwarsOverlay.App\bin\Debug\net8.0-windows\BedwarsOverlay.Core.dll`, close the app or build the app project with a temporary `OutputPath`.

---

## 6. Notes For Future Work

- Keep user-facing text in both `BedwarsOverlay.App/Localization/en.json` and `BedwarsOverlay.App/Localization/tr.json`.
- Update both README files when setup, usage, or user-facing behavior changes.
- `PLAN.md` is now reserved for forward-looking work. `PROGRESS.md` is the current-state handover.
- Match tracking is anchored to the Bed Wars start block: a match only opens when the `Protect your bed...` intro is seen, so a `/who` typed in the lobby never records encounters. If the overlay starts mid-game, the current game is not tracked; the next game is.
- `HypixelLogParser.ParseGameBlock` counts only `[CHAT]` lines. Long Hypixel messages wrap onto continuation lines without the `[CHAT]` prefix, and those lines must never reset the block state.
- The threat weights, half-life constants, and band thresholds live in `BedwarsOverlay.Core/Stats/ThreatScorer.cs` and are still the first-draft values. They are intended to be tuned with real match data.
- Current threat calibration: a strong regular player (FKDR 6.0, level 300, winstreak 20, established account) scores ~55 → High, while a 35-game FKDR 15.0 account seen in 5 of the last 10 matches scores ~79 → Extreme. `ThreatScorerTests.Score_Calibration_*` pins these two profiles so a future tweak has to be deliberate.
- Verified against a real Badlion client log: all 6 matches in the log produced both a start and an end event (wins report `(Time Played)` first and upgrade to win on the `(Win)` line). Before the fix, a wrapped team roster line reset the parser state and no game end was detected at all.
- `(Time Played)` is **not** a safe end-of-game marker on its own. Hypixel prints `+12 tokens! (Time Played)` and `+17 Bed Wars XP (Time Played)` every minute during a live game; only `+N Slumber Tickets (Time Played)` marks the end of a match. `GameBlockParserTests.ParseGameBlock_PerMinuteTimePlayedRewardsInsideBlock_DoNotEndMatch` pins this.
- A match can end without any summary block: when the player is eliminated and leaves before the game finishes, the log has no `Bed Wars` end block at all (measured: 8 of 14 games in `2026-10-11-3.log.gz`). `EncounterRepository.StartMatch` therefore closes every still-open match instead of relying on a 30-minute staleness cutoff.
- Team membership is **not** available from the client log. There is no "you are on the X team" message, `/who` returns a flat name list, and the scoreboard/tab list is never logged. The only team data in chat is the winning team's roster in the end block (`Yellow - name, name, ...`), `TEAM ELIMINATED > X Team`, and `BED DESTRUCTION > X Bed ... by <name>`. Do not build per-player team features on this.
- Reconstructing the roster from `X has joined (N/16)!` messages does not work: players already in the lobby when the client is transferred in never emit a join message (measured: 4 of 16 names recovered). `/who` remains the only reliable roster source.
- Team recognition requires `/who` players to be loaded first and the Minecraft player list to be visible when `END` is pressed (hold `TAB`). OCR can miss rows on unusual UI scales or client layouts; unknown assignments remain blank. Release packages bundle the English Tesseract model and x64 Visual C++ runtime files.
