# Changelog

All notable user-facing changes to Bedwars Overlay are tracked here.

## Unreleased

## 1.4.3 - 2026-10-20

- Added manual signed-package builds before publishing and fixed installer asset selection for automatic release uploads.
- Fixed release builds by using the Visual Studio 2022 runner and selecting a Visual C++ runtime directory that contains the required CRT files.
- Includes the first-run settings persistence fix from 1.4.1.

## 1.4.2 - 2026-10-20

- Fixed Windows release packaging so it finds the Visual C++ runtime files across Visual Studio installation versions.
- Includes the first-run settings persistence fix from 1.4.1.

## 1.4.1 - 2026-10-20

- Fixed first-run settings saves: unset window bounds are now serialized and restored as named floating-point values, so other settings are not lost before the window is positioned.

## 1.4.0 - 2026-10-20

- Added offline team recognition from the visible Minecraft player list. Hold `TAB` and press `END`; the Team column can be sorted.
- Improved team capture speed and recognition of left-aligned player lists.
- Added a Windows GitHub Actions release workflow that builds and attaches the signed installer and portable ZIP.

- Added per-match player memory: the overlay now records which players were in which game and marks repeat opponents with a `×N` badge in the name column.
- Added Settings controls for repeat opponents: enable/disable the badge, how many previous matches to search, and the minimum number of matches before the badge appears.
- Replaced the `Sniper` column with a `Threat` score (0-100) built from FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, and repeat-opponent signals, banded as Low / Medium / High / Extreme.
- Added a per-row threat tooltip listing the signals behind the score.
- Added schema versioning and migration for `%LOCALAPPDATA%\BedwarsOverlay\data.db` through `PRAGMA user_version`; v2 adds `matches` and rewrites `encounters` with a match id.
- Fixed Bed Wars game-end detection. Long team rosters wrap onto a continuation line without the `[CHAT]` prefix, which reset the parser state, so the end of a game was effectively never detected in real matches and the previous player list stayed on screen.
- Fixed the loss path: a game that ends with `(Time Played)` and no `(Win)` line is now recognized as the end of a match. Only the `Slumber Tickets (Time Played)` reward counts, because Hypixel prints `tokens! (Time Played)` and `Bed Wars XP (Time Played)` every minute while the game is still running.
- Fixed abandoned matches staying open forever: a player who leaves after being eliminated never sees the end summary, so starting a new game now closes the previous match instead of waiting 30 minutes.
- Fixed the threat column, risk label, and warning badge disagreeing with each other because they used separate FKDR thresholds.
- Renamed the `Sniper` column to `Threat`; saved sorting and the column visibility setting are migrated (`SniperRaw` → `ThreatRaw`, keeping existing `settings.json` values).
- Changed the grid localizer to resolve columns by `SortMemberPath` instead of hard-coded positions, so hiding or reordering columns cannot move a localized header.
- Expanded the test suite from 74 to 115 tests, including real client log fixtures for the start, win, and loss blocks, threat scoring, encounter storage, and schema migration.

## 1.3.0 - 2026-06-25

- Added single-instance startup guard so launching the app twice does not open a second overlay process.
- Added GitHub latest-release update checks with localized prompts, skip-version support, and an edit-mode update button.
- Added app version display to Settings.
- Refactored `MainWindow` code-behind into focused partial files for startup, window state, grid behavior, log handling, player commands, settings, updates, and onboarding.
- Fixed settings persistence so stale app instances cannot overwrite newer saved settings when saving another field such as window bounds.

## 1.2.0 - 2026-06-25

- Added overlay chat commands read from Minecraft log output: `/bwstats`, `/bwignore`, `/bwclear`, and `/bwhelp`.
- Added `/bwstats <name...>` for loading specific players while bypassing ignore filters.
- Added `/bwignore <name...>` and Settings ignored-player management for hiding selected players from `/who` results.
- Added party and self ignore options. `/p list` updates the party list used by party ignore.
- Added `/bwclear` for clearing the current overlay list from Minecraft chat.
- Added localized `/bwhelp` command output in overlay diagnostics/status.
- Added Bed Wars end-of-game summary detection that clears the current player list.
- Changed `FINAL KILL!` handling to briefly show a red `FINAL KILL` row state before removing the player.
- Changed taskbar and Alt-Tab behavior: the overlay is hidden in play mode and visible in edit mode.

## 1.1.0 - 2026-06-24

- Added system tray integration. Closing the overlay now hides it to the tray, and the tray menu can show/hide the overlay, open Settings, or exit the app.
- Added saved overlay position and size between runs.
- Added Settings controls for hiding or showing stat columns: `LVL`, `WS`, `FKDR`, `WLR`, `FKILLS`, `WINS`, and `SNIPER`.
- Added automatic row removal when Hypixel logs a matching `FINAL KILL!` message.
- Added parser tests for multiple `FINAL KILL!` message formats.
- Updated documentation split: `PROGRESS.md` now describes current state, and `PLAN.md` now tracks future work.
