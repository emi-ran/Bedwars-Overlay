# Bedwars Overlay - Roadmap

This file tracks forward-looking work only. Current implemented state belongs in [PROGRESS.md](PROGRESS.md).

---

## High Priority

- Add safer error reporting for settings save/load failures instead of silently suppressing all failures.
- Expand parser fixtures with more real Hypixel Bed Wars game-end, party, and command log messages.
- Join wrapped Hypixel chat continuation lines (no `[CHAT]` prefix) into the message they belong to so long team rosters, reward summaries, and multi-line messages parse as one entry.
- Review and update screenshots in `docs/assets/` after the Threat column, repeat-opponent badge, ignored-player settings, command help, final-kill row state, and taskbar/Alt-Tab mode behavior changes.
- Validate team OCR with additional Badlion captures at different resolutions and GUI scales; improve row matching where needed.

---

## Medium Priority

- Separate more of `MainWindow.xaml.cs` into view-model or service classes.
- Move UI-specific `PlayerRow` concerns out of `BedwarsOverlay.Core` if the core project should become platform-agnostic later.
- Add UI/behavior tests around settings persistence, ignored-player filtering, taskbar/Alt-Tab mode behavior, and delayed final-kill row removal.
- Improve diagnostics for Mojang/Hypixel API failures without exposing API keys.

---

## Low Priority

- Add configurable hotkeys.
- Add richer tray notifications for first launch and configuration errors.
- Add optional cache cleanup controls.

---

## Deferred / Not Implemented

- Tuning the threat weights and band thresholds with real match data (the current values are a first draft).
- Predicting a specific "sniper" player by name; the overlay only scores statistical danger and repeat encounters.
- Match history UI (past matches, win/loss record, per-player history view). The data is stored, but nothing renders it yet.
- Identifying a nicked player's real account.
- Reading team assignments requires the player list to be open for a user-triggered screen capture; team data is not available from the `latest.log` stream.
- Auto-loading the roster at game start. `X has joined (N/16)!` messages only cover players who join after the client is transferred in (measured: 4 of 16 names recovered), and the overlay cannot send `/who` itself without input injection, which is out of scope.
