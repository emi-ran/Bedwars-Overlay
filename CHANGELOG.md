# Changelog

All notable user-facing changes to Bedwars Overlay are tracked here.

## Unreleased

## 1.4.7 - 2026-10-23

- Added `/pstats` to load the cached `/p list` party roster, including players otherwise hidden by ignore filters; `/bwhelp` now fits the diagnostics panel in compact bilingual rows.
- Prevented false team assignments by requiring the OCR-read team prefix to agree with the color mask that produced the candidate.
- Made rank badges more compact by removing brackets and reducing their font size, leaving more room for player names.

## 1.4.6 - 2026-10-23

- Fixed `/p list` party ignore so it includes party moderators printed on their own line.
- Added Aqua, White, Pink and Gray to team OCR, covering all eight teams in Solos and Doubles as well as 4v4v4v4.
- Improved short player-name OCR with single-column segmentation; verified all 15 names in a real Doubles capture, including `fgd0`, without relaxing fuzzy-match thresholds.
- Fixed thread-affinity failures in release tests by freezing WPF row brushes; the local Release test suite passes all 138 tests.

## 1.4.5 - 2026-10-22

- Fix team capture reporting missing or wrong teams. Every team color now gets its own OCR pass and a name inherits the color it was actually drawn in, instead of the team being guessed from the characters next to a matched name. Strict Minecraft palette matching and glyph-sized component filtering keep the world behind the player list out of the OCR input, which used to make a capture silently find nothing.
- Known teams now color player names. Names without a known team keep their rank color (MVP+ cyan, VIP green, unranked gray), so an MVP no longer hides its team color.

## 1.4.4 - 2026-10-21

- Track explicit disconnect/reconnect messages with a `DC` badge while preserving stats and team assignments. Reconnect and `/who` clear the badge; final-killed players remain excluded. Connection messages never trigger Split reminders.
- Added a small five-second `Split!` reminder on normal teammate deaths, using the configured local player, OCR team assignments, and party membership when teams are unknown. Hidden self/party players now participate in team OCR; known opposing teams, self deaths, and final kills do not trigger the reminder.
- Keep known players through respawns and merge in-match `/who` snapshots instead of replacing the roster; recognized death messages fill missing names without restarting existing lookups.
- Keep final-killed players excluded from subsequent snapshots until the next match, and reset roster state on match/lobby transitions.
- Read teams from recognized death messages when Minecraft color codes are preserved; plain Badlion death messages still require team OCR.
- Added a Turkish/English product website with interactive team and Threat demos on GitHub Pages.

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
