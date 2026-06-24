# Changelog

All notable user-facing changes to Bedwars Overlay are tracked here.

## Unreleased

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
