# Minecraft BedWars Overlay - Project Progress & Handover

This document is the current project state and handover guide. Read it before continuing implementation work.

## Release 1.4.10 — published, 2026-10-28

Includes accumulated OCR fixes (Gray S, isolated rows/White W/name retries, O/0 ambiguity handling, closed-TAB rejection), 29 reviewed image fixtures, and shared nicked/current-name teammate resolution for Split and party-ignore filtering. App/assembly/file, installer fallback and publish examples now use 1.4.10. Corpus is 363/369 correct assignments with zero extra/wrong teams and six explicitly tested Gray-background misses; this is not a speed optimization or general 100% accuracy claim. No account deanonymization.

Published commit `da407e25f061e1f13f7b2e34aedb8173e53a22fd` with annotated tag `v1.4.10`. Release workflow `40691550799` succeeded in 3m33s; CI passed 212/212 tests and signed app EXE, App/Core DLLs and installer. Both release assets are uploaded: portable `BedwarsOverlay-1.4.10-win-x64.zip` (82,298,921 bytes) and installer `BedwarsOverlaySetup-1.4.10-win-x64.exe` (58,220,864 bytes). Local Release suite passed 212/212 and app build passed with zero warnings/errors. Node20 action deprecation annotation is non-blocking. Live Minecraft, installer execution and independent downloaded-binary signature checks remain unperformed.

## Release 1.4.9 - 2026-10-25

Published at commit `3f43e4e` with annotated tag `v1.4.9`. Signed-package preflight `40177296157` and release workflow `40177472525` succeeded; release CI passed 193/193 tests and signed the app EXE, App/Core DLLs, and installer. Both assets are uploaded: `BedwarsOverlay-1.4.9-win-x64.zip` (82,288,846 bytes) and `BedwarsOverlaySetup-1.4.9-win-x64.exe` (58,214,952 bytes). Downloaded-binary signature inspection, installer execution, and live-game validation were not performed. The Node20 action deprecation annotation is non-blocking.

Automatic Hypixel party detection updates the in-memory party roster from party lifecycle chat: own join resets the roster and adds the leader, `You'll be partying with:` adds companions, `X joined the party.` adds members, `X has left/has been removed from the party.` removes members, and own leave/removal, disband, and not-in-party lines clear the roster. Invitations do not add members. `/p list` remains the authoritative refresh fallback. Party state persists across lobby/game transitions and is cleared on log source reset; if the app starts while already in a party, run `/p list` once. The same roster is used by party ignore, `/pstats`, and the Split reminder fallback. Release preparation verified the existing Release suite: 193/193 tests passed and the Release app build succeeded with 0 warnings/errors.

## Previous release: 1.4.8 - 2026-10-23

Brightness-aware team OCR preserves vertically dimmed glyph strokes at small GUI scales without relaxing prefix validation or roster matching. The user's original 1287x427 capture now matches all nine visible players in three repeated runs (previously two); the user also confirmed live laptop team recognition works. Added a redacted native-size OCR fixture and ten color/background regression cases. Release suite: 169/169 passed. Slow live capture was reported but not benchmarked or optimized; this release makes no speed claim. Version metadata, installer and publish examples use 1.4.8.

Published v1.4.8 from commit `781c13d9b9df0d75072c01461c2f43e1bffc61e5`. Workflow `40015308154` succeeded with 169/169 CI tests; signing logs confirm app EXE, App/Core DLLs and installer signing. Release assets are uploaded: portable ZIP (82,286,179 bytes) and setup EXE (58,214,432 bytes). Local Release build passed with 0 warnings/errors. Downloaded-package installation/signature inspection was not performed. The workflow emitted a non-blocking Node 20 action deprecation warning.

## Previous release: 1.4.7 - 2026-10-23

Release v1.4.7 includes `/pstats`, compact bilingual `/bwhelp`, stricter team-prefix validation for OCR candidates, and compact rank badges. The 2026-10-23 12:06:42 capture showed `B DyeTypical` incorrectly colored Green; on the exact capture a local harness consistently recognized Blue, but code inspection identified that candidates from any team mask were accepted without checking the OCR row prefix. Candidates now require the row's initial team letter to agree with the producing mask. Local verification: 158/158 tests and Release build (0 warnings/errors) passed. Published GitHub Release v1.4.7 on 2026-10-23 from commit `8a133de8a5d3925a72e680fa8c4b9e95e7f1bc0c`; workflow `39934161266` succeeded with 158/158 CI tests and attached the portable ZIP (78.5 MB) and setup EXE (55.5 MB).

## Previous release: 1.4.6 - 2026-10-23

Team OCR reads all eight Bed Wars team colors and uses `SingleColumn` page segmentation so short names survive OCR; the conservative fuzzy-match threshold was not lowered. `/p list` party ignore also parses the separate moderator line. Player-row brushes are frozen for safe cross-thread use.

App/assembly/installer versions and publish script examples use 1.4.6. README EN/TR and changelog describe the behavior. Verified against the user's actual 15-player Doubles screenshot: 15/15 team assignments, including the short name `fgd0`, on five repeated recognizer runs; `fgd0` alone also resolves to Yellow. The short-name change was traced to `SparseText` emitting inconsistent glyphs; `SingleColumn` produces an OCR line that the unchanged matcher resolves at 0.75 confidence. Local verification: 138/138 tests and Release build passed. Published GitHub Release v1.4.6 from commit `bd97a2023c2d108597e1c7db8b38afca729a720e` on 2026-10-23. Release workflow `39880817675` succeeded; CI passed 138/138 tests and uploaded both packages: portable ZIP and setup EXE (78.4/55.5 MB).

---

## Unreleased

- 2026-10-28 second capture batch: added five native-size redacted fixtures (reviewed-25..29) and original-image labels. Last two captures reproduced missed Green `momochiki1089` and Yellow `rezna_2010`: OCR emitted `momochikilgsg` and merged `Yrezna_2010`. Added a bounded name-only retry for TAB-aligned colored rows whose first OCR character matches the team prefix but whose full prefix/name validation fails; prefix character is retained, roster thresholds unchanged, diagnostic mode `TabName`. Both captures now match all 15/15 players. Five-image batch: 73/77 correct, no wrong/extra assignments. Four new Gray background-blending misses (`ciapagallina`, `MatchedAuthor52`, `amisotheist`, `_Puzo_`) remain explicit known misses, alongside the prior two; full corpus 363/369. Original-image corpus passes with those six missing assignments explicitly asserted; this is not 100% OCR accuracy. Existing unrelated teammate work preserved. Live validation pending.

- 2026-10-28 verification: Debug and Release full suites each passed 212/212 using temporary outputs; full-original corpus passed with two explicitly asserted known misses. Corpus now includes the closed-TAB world nametag negative in its redacted fixture too. Live-game validation remains pending.

- 2026-10-28: Expanded independently reviewed OCR corpus from six to 24 available images, restored missing redacted PNGs, and labeled local captures without replacing recorded predictions. New 16-image batch: 190/192 visible prefixed names assigned correctly, zero wrong/extra teams; raw case-sensitive exact name occurs in at least one pass for 154/192 (80.2%). Full corpus: 290/292. `oliverooel` and `Lauucha2` in 20261027-223140 remain explicit known misses (Gray background blends into glyphs), not removed from ground truth. A TAB-closed capture previously read world nametag `Y dallsta`: now results require repeated aligned TAB-prefix anchors and row spacing; source full image negative test passes. Added conservative O/0 equivalence (0.95 maximum normalized score, unchanged ambiguity margin) recovering `8OO85` from `80085`, with ambiguous-name negative coverage. Live capture not tested. Existing unrelated teammate/Split work remains untouched.

- 2026-10-27: Added shared `TeammateResolver` for current-name/nicked teammates already in the loaded roster. Local team takes priority; if unavailable, recognized party teams must agree. Known opposing teams do not trigger Split; unknown-team party fallback remains. Party ignore retains direct party filtering and additionally hides inferred same-team rows after TAB+END and on later filtered loads. Same-team non-party players are intentionally included; no real-identity mapping, party-roster mutation, new persisted data or OCR threshold change. Nine resolver regression cases cover the user's EvilAndPsycho example, party anchors/conflicts, self exclusion, case handling, fallback and cleared evidence. Live Minecraft validation pending.

  Verification: nine resolver tests passed; full Release suite passed 210/210 with `BEDWARS_OCR_CORPUS` pointing to local original captures, and Release app build passed with 0 warnings/errors using temporary output. Default full-suite run failed one existing corpus test because `Fixtures/TeamOcr/reviewed-1.png` (and the other reviewed PNGs) is absent from this checkout; 209 other tests passed. Missing fixtures were not recreated or hidden. WPF filter integration and live Split/capture were not exercised.
- OCR follow-up verification: Debug and Release each pass 201/201 tests using temporary output directories; original full-capture corpus also passes separately. Row diagnostics now identify `Mode=TabRow` and source `RowY`; their PixelCount is the prefix-region count, while `ColorMask` retains full-mask counts. These changes are not a measured speed improvement.

- 2026-10-26 OCR corpus follow-up: six present original captures independently labeled (the first manifest image is missing; excluded). Recorded output is 57/76 correct visible prefixed roster assignments, no wrong teams. Gray actually uses `S`, not the previous `G`; head pixels and world text corrupt prefix OCR, particularly White W→HW/WH. Added TSV prefix anchors to infer TAB column/glyph height/row spacing, bounded isolated-row passes with unchanged prefix/name thresholds, plus pixel-validated block W and a separate name pass. Full original screenshots and native-size redacted TAB fixtures now match all 76/76 with no additional assignments. Respawning TryHardnt in reviewed-4 has no visible prefix and remains unassigned; absent roster names/spectators are excluded. Local expectedTeams labels filled; recorded OCR predictions preserved. New exact corpus test and blank-image negative test added. Live-game behavior/performance still requires validation; no release/version change.

- 2026-10-26: Debug source-checkout TAB+END captures now save native PNGs and append `images/team-ocr/captures.json`. Records contain roster, per-color raw OCR/pixel counts, matches, unmatched names, diagnostics/error and independent nullable `expectedTeams` labels. Existing labels/unknown fields survive appends; invalid JSON is rejected, writes use a temporary manifest, save errors do not stop team updates, and Release builds do not record. Corpus is local/Git-ignored; full foreground-window captures require privacy review before sharing. Accuracy scoring and new OCR fixes remain future work. Debug and Release each passed 197/197 tests in sequential runs using temporary output paths (running Debug app PID 11776 locked normal build output). Concurrent configuration runs exposed existing settings tests sharing the real default settings path; each had one settings failure, then both sequential reruns passed. No unrelated test/storage changes made. Live Minecraft hotkey validation has not been performed; restart/rebuild the running app to use the new feature.

- Laptop OCR investigation and v1.4.8 verification are recorded in the release section above. The live user confirmation supersedes the earlier static-only verification; live capture performance remains unmeasured.

- v1.4.7 release publication status is recorded in the release section above.

- Release 1.4.6 is published and verified: workflow `39880817675` succeeded, 138/138 CI tests passed, and both packages are attached.

- Party ignore missed party moderators. Real Badlion log (`logs/blclient/minecraft/latest.log`, 2026-10-23 02:08-02:37) shows `/p list` printing three lines: `Party Members (2)` header, `Party Leader: [VIP+] TryHardnt ?`, `Party Moderators: [VIP+] Colorado17 ?` — with no `Party Members:` line at all when every other member is a moderator. `HypixelLogParser.PartyLinePattern` only accepted `Party Leader:` and `Party Members:`, so moderators never reached `_partyPlayers` and `RemoveIgnoredExistingPlayers`/`FilterIgnoredPlayers` kept their rows. Pattern now accepts `Party Moderators:` as well; three parser cases added from the real log lines. Verified: 124/124 tests pass.

- Automatic Hypixel party detection now updates `_partyPlayers` from party lifecycle chat lines: own join resets the roster and adds the leader, `You'll be partying with:` adds companions, `X joined the party.` adds members, `X has left/has been removed from the party.` removes members, and own leave/removal, disband, and not-in-party lines clear the roster. Invitations do not add members. `/p list` remains the authoritative refresh fallback. Party state persists across lobby/game transitions and is cleared on log source reset. If the app starts while already in a party, run `/p list` once to seed the roster. Verified: 193/193 Release tests and Release app build (0 warnings/errors). New parser/reducer tests cover lifecycle, negative invitations/chat, stale reset and list refresh; lobby persistence is confirmed by source review, not a WPF integration test. Own-kick and leader-disband variants have synthetic coverage but were not observed in the inspected local log. Live-game behavior has not been exercised; changes remain uncommitted/unpushed.

- v1.4.5 release workflow (`39810461071`) failed inside `publish-release.ps1` at the test step: `PlayerRowTests.Name_UsesTeamColor_WhenTeamIsKnown` and `Name_KeepsTeamColor_WhenRankStatsArriveLater` threw `InvalidOperationException: The calling thread cannot access this object because a different thread owns it` when reading `SolidColorBrush.Color`. Cause: `PlayerRow` created unfrozen WPF brushes (static color brushes, `TeamColor`, row backgrounds, level colors); xUnit spreads test classes over the thread pool, so the brush was created on one thread and read on another. Local runs passed because the static initializer and the assertion happened to share a thread. All `PlayerRow` brushes now go through a freezing factory and team colors are cached as frozen statics; `PlayerRowTests.Row_BrushesStayReadable_WhenRowIsBuiltOnAnotherThread` covers the cross-thread read. Verified locally: Release build of Core/App 0 errors/0 warnings, 121/121 tests pass over repeated runs. The GitHub release `v1.4.5` exists with no assets because the job failed before upload.

---

## Release 1.4.4 — 2026-10-21

Respawn roster retention, disconnect/reconnect badges, final-kill exclusion, hidden-player OCR matching and the teammate Split reminder are included in 1.4.4. App/assembly/installer versions and release script examples use 1.4.4. README EN/TR, changelog and bilingual site FAQ describe the behavior and its limits. Local compilation and UI fragment rendering passed during implementation; no new automated tests or live-game checks were run. The existing release workflow runs its test suite before signing and attaching packages; its outcome must be checked before reporting release packaging complete.

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

- Parses anchored server `name disconnected.` / `name reconnected.` messages, including Minecraft formatting codes, and keeps connection state in `MatchRoster` separately from API lookup status. A `DC` badge leaves stats/team assignments intact; reconnect or a positive `/who` entry clears it. Reconnect adds missing non-eliminated names with ignore filters. Connection events can activate in-memory retention after a mid-game roster load, but do not open encounter matches or call Split reminders. Final kills clear DC and tombstone the player; subsequent reconnect cannot undo elimination. Match reset/manual clear discard connection state.
- Real Badlion logs show disconnect followed by a separate final message: `miguelmaba1508` at 11:23:37 then FINAL KILL at 11:23:39 (2026-10-19-1), and `7odaJr` at 00:09:41 then FINAL KILL at 00:09:48 (2026-10-20-1). This confirms the ordering is possible, not that every disconnect without a bed always emits a final line. Bed state is not tracked; without explicit final evidence a player remains DC.
- Shows a non-interactive `Split!` banner for five seconds on recognized normal teammate deaths. The configured local name is required; known local/victim teams take priority over party membership, which is a fallback only when either team is unknown. Self deaths hide an existing reminder; final deaths do not create one. Repeated teammate deaths restart the timer. Clear, match/lobby transitions and window disposal clear/stop the reminder. No position/generator proximity detection or sound is used; hidden overlays remain hidden.
- Team OCR matches known roster names, visible rows, party members, and the configured local name, and caches assignments for hidden players too. A roster version discards OCR results after a clear or match transition. A recognized team also drives the name color in the grid; unknown teams fall back to the rank color.
- Added a bilingual static product website in `docs/`, with an illustrative interactive team/Threat demo, responsive layout and reduced-motion support. GitHub Pages uses `main` / `/docs`. See [Site maintenance](docs/SITE.md).
- Reads Minecraft `latest.log` and detects `/who` output.
- Maintains an in-memory `MatchRoster`: in-match `/who` snapshots merge with known players, recognized death messages add missing victims/killers with ignore filters, and final-kill tombstones prevent resurrection. Existing rows/lookups are preserved on incremental additions. Match start/end and detected lobby transfers reset membership. A recognized death after a mid-game `/who` activates retention without opening an encounter record.
- Recognized death messages can update victim/killer teams when Minecraft color codes survive in the log. Inspected Badlion October logs have no such colors; unknown teams stay blank and TAB+END remains the practical source.
- Reads Hypixel team prefixes from a user-triggered capture of the visible Minecraft player list (`END`, while holding `TAB`) and shows the result in a sortable Team column. OCR runs locally using bundled Tesseract; no client mod or separate user installation is required.
- Team OCR scans the centered player-list band, builds one mask per Minecraft team color, filters out blobs that cannot be a glyph of the list, and runs a separate OCR pass per color so the team comes from the matched mask instead of a guessed prefix letter. Ambiguous or uncolored names stay blank. Captures stay at native resolution until the OCR crop is enlarged once.
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
- Inspected Badlion logs do not preserve team colors in death messages. `/who` is flat and the scoreboard/tab list is not logged. The parser now reads actor colors in recognized death messages if another client preserves them; no team is inferred from uncolored actors, killer/victim relationships, or bed destruction. OCR assignments survive uncolored deaths.
- Reconstructing the full roster from `X has joined (N/16)!` messages does not work: players already in the lobby when the client is transferred in never emit a join message (measured: 4 of 16 names recovered). `/who` remains the initial roster source; recognized deaths now supplement it, and later snapshots preserve respawning players. Join/transfer messages reset prior match state, rather than loading incomplete lobby rosters.
- Team recognition requires `/who` players to be loaded first and the Minecraft player list to be visible when `END` is pressed (hold `TAB`). OCR can still miss rows on unusual UI scales, on custom tab-list fonts, or when a client tints the palette away from the Minecraft team colors (a looser color fallback then applies); unknown assignments remain blank. Release packages bundle the English Tesseract model and x64 Visual C++ runtime files.
