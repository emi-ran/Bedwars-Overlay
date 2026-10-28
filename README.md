# Bedwars Overlay

**English** | [Türkçe](README.tr.md)

Bedwars Overlay is a small Windows overlay for Hypixel Bed Wars players. It reads your Minecraft log, detects `/who` results, and shows useful Bed Wars stats in a clean overlay while you play.

![Bedwars Overlay with players](docs/assets/overlay-with-players.png)

## What It Does

- Shows players from your `/who` list.
- Keeps disconnected match players with a `DC` badge. A reconnect message or inclusion in a later `/who` clears the badge; reconnect can add missing players, with ignore filters applied. A subsequent `FINAL KILL!` removes the player and prevents reconnect from restoring them in that match. Disconnect alone does not infer elimination from bed state and never triggers `Split!`.
- Keeps known match players during respawns even if a later `/who` omits them, and fills missing names from recognized death messages without reloading existing stats. Final-killed players stay excluded until the next match.
- Reads player teams from death-message color codes when the client preserves them; otherwise keeps OCR assignments or leaves the team unknown.
- Shows a small five-second `Split!` reminder when a teammate dies normally. Set your own Minecraft name in Settings; matching OCR teams take priority, with `/p list` membership as a fallback when teams are unknown. Self deaths and final kills do not trigger it. Hidden self/party rows are included in TAB+END team matching. This is a death reminder, not a check of your distance from the generator; it appears only while the overlay is visible.
- Reads team prefixes from the visible Minecraft player list with `END` (hold `TAB` and press `END`) and lets you sort the table by team. All eight Bed Wars teams are recognized, so Solos and Doubles work as well as 4v4v4v4. OCR uses single-column segmentation to preserve short player names; uncertain names are still left blank rather than assigned to a guessed team. Each team color is read separately, so a known team also colors the player's name; names without a known team keep their rank color (MVP+ cyan, VIP green, and so on). The OCR engine is bundled with the app.
- Lets you load specific players with `/bwstats`.
- Lets you hide players with `/bwignore`, Settings ignored players, `/p list` party ignore, and self ignore.
- Displays useful Bed Wars stats such as level, winstreak, FKDR, WLR, final kills, wins, and a 0-100 threat score.
- Band the threat score as Low / Medium / High / Extreme, with a per-row tooltip listing the signals behind it (FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, repeat opponents).
- Remembers which players were in which game and flags repeat opponents with a `×N` badge on the name column.
- Lets you hide or show individual stat columns from Settings.
- Helps you quickly notice risky lobbies and unusual stats.
- Marks final-killed players with a short red `FINAL KILL` effect, then removes them from the table.
- Clears the current list when a Bed Wars game ends, on both wins and losses.
- Starts in edit mode so you can move and resize the overlay.
- Remembers the overlay position and size between runs.
- Switches into transparent play mode so it does not block Minecraft.
- Hides from the taskbar and Alt-Tab in play mode, and appears there in edit mode.
- Runs from the system tray; closing the overlay hides it, and the tray menu can show it again or exit the app.
- Supports English and Turkish.

## Shortcuts

- `Ctrl+Alt+O`: switch between edit mode and transparent play mode.
- `Ctrl+Alt+H`: hide or show the overlay. Hidden overlays stay available from the system tray.
- `END`: read teams while Minecraft is focused and its player list is open. Hold `TAB` and press `END`; the overlay briefly hides while it captures the game window.

## Overlay Commands

Type these in Minecraft chat. Hypixel will report them as unknown commands, and the overlay reads that log output.

- `/who`: load current game players and apply ignore filters.
- `/bwstats <name...>`: load only the listed players. This ignores filters so you can inspect a player even if they are ignored.
- `/pstats`: load stats for the cached party roster. The roster is detected automatically from party join/leave messages, with `/p list` as a refresh fallback; party ignore settings do not suppress this explicit request.
- `/bwignore <name...>`: add players to the ignored players list and remove them from the current overlay.
- `/bwclear`: clear the current overlay list.
- `/bwhelp`: show the compact command help in the overlay diagnostics/status area.
- `/p list`: refresh the party list used by the `Ignore party players` setting. Party membership is also detected automatically from join/leave messages.

If the overlay starts while you are already in a party, run `/p list` once: automatic tracking only knows members mentioned in new log messages. Invitations alone do not count as membership. The cached roster is shared by party ignore, `/pstats`, and the Split reminder fallback.

Use `/who` just after the game starts to load the roster; the status bar prompts you. During the match, later `/who` results are merged with known players because players waiting to respawn can be absent. Recognized death messages add missing victims and killers, with the same ignore filters. Final kills remove players permanently for that match. A new match, game end, or detected lobby transfer resets the match roster. After a mid-game launch, load `/who`; the next recognized death enables roster retention without creating a tracked encounter match.

The inspected Badlion logs contain no team colors in death messages, so `TAB` + `END` remains necessary there. Clients that preserve Minecraft color codes can supply team assignments through recognized death messages. Plain names, killer/victim relationships, and destroyed-bed messages are not enough to infer a team.

Nicked teammates are supported by their current match name: after `/who` and `TAB` + `END`, players on your recognized team count as teammates even when their names differ from `/p list`. Your configured Minecraft name anchors the team; if its team is unknown, recognized party members must all agree on one team. Conflicting party teams do not identify unknown teammates. `Ignore party players` also hides these same-team players (including non-party teammates) after capture and on later roster loads; normal teammate deaths can trigger `Split!` using the same rule. Set your own name for Split reminders. This does not identify a nick's real account, prove party membership, or fetch the real account's stats. Names must still match the loaded roster confidently; inferred membership is match-local, not added to the party roster.

Team OCR preserves dimmed strokes in saturated team-colored text, including vertically shaded text at small GUI scales. It still requires a matching team prefix and a sufficiently confident roster-name match; uncertain rows stay unknown.

Gray uses Hypixel's `S` prefix (not Green's `G`). Repeated detected prefix positions locate the TAB column and row spacing for isolated row OCR, avoiding nearby heads/world text. White block-font `W` prefixes can also be validated from their glyph pixels when Tesseract misreads them as `HW`/`WH`; roster confidence thresholds remain unchanged. Six reviewed captures improve from 57/76 to 76/76 correct visible prefixed roster assignments, with no extra assignments, on both full originals and redacted regression fixtures. This is corpus-specific evidence, not a guarantee for every GUI scale/client.

## Setup

1. Download the latest version from the [Latest Release](https://github.com/emi-ran/Bedwars-Overlay/releases/latest).
2. Run `BedwarsOverlaySetup-*-win-x64.exe` and launch Bedwars Overlay.
3. If you prefer portable usage, download the `.zip`, extract it, and run `BedwarsOverlay.App.exe`.
4. Open Settings.
5. Add your Hypixel API key.
6. Check or select your Minecraft `latest.log` path.
7. Optionally choose which stat columns are visible, configure ignored players, and tune repeat-opponent highlighting.
8. Join a Bed Wars lobby and type `/who` in Minecraft.

![Settings window](docs/assets/settings-window.png)

## Getting a Hypixel API Key

You can get an API key from the Hypixel Developer Dashboard:

[https://developer.hypixel.net/](https://developer.hypixel.net/)

The app also includes a button in Settings that opens this page for you.

## Install From Source

Most users should use the latest release. If you want to build it yourself:

```powershell
git clone https://github.com/emi-ran/Bedwars-Overlay.git
cd Bedwars-Overlay
dotnet build "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
dotnet run --project "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
```

Requirements:

- Windows
- .NET 8 SDK

### Development OCR captures

Failed aligned row matches can retry only the name region, retaining the OCR-read team letter (`Mode=TabName`). The latest two 15-player captures now match all players; the expanded 29-image corpus has 363/369 correct assignments with no extras and six explicit Gray-background misses. This supersedes earlier corpus totals below.

OCR results now require repeated aligned TAB prefixes/row spacing; isolated world nametags are not enough. O/0 font confusion can be resolved against an unambiguous roster name without reducing the acceptance threshold. Expanded corpus: 290/292 correct assignments, no wrong/extra teams. Two Gray names on a blending background remain known misses; see `BedwarsOverlay.Tests/Fixtures/TeamOcr/README.md`.

OCR pass records identify `Mode=ColorMask` or `Mode=TabRow`; isolated rows include original-image `RowY`, and their pixel count refers to the prefix region rather than the whole mask.

The default Debug `dotnet run --project "BedwarsOverlay.App\BedwarsOverlay.App.csproj"` saves each completed TAB+END screen capture at its original resolution under repository-root `images/team-ocr/`. It creates `captures.json` if missing and appends an entry otherwise, including the PNG filename, UTC capture time, input roster, raw OCR text and pixel counts per team color, recognized teams, unmatched roster names, diagnostics, and any OCR error. Colors skipped for too few pixels have null text. Captures with no matches and captures whose results become stale are kept too; the existing TAB/loaded-roster/window checks still apply.

`expectedTeams` starts as null: fill it with manually verified name-to-team labels from the original PNG, independently of `recognizedTeams`. Later captures preserve those labels and other existing entry fields. These pairs provide a local corpus for future accuracy/regression tests; no automatic scoring is added yet. Release builds never save captures; Debug builds outside a source checkout do not save them either. Save failures are reported in diagnostics without preventing team updates. A malformed manifest is not overwritten.

The directory is Git-ignored and nothing is uploaded. Images include the entire captured foreground window and may contain chat/private information; review/redact before sharing. Captures accumulate until you manually clean them up.

## Building a Release

Install Inno Setup 6 and Visual Studio Build Tools 2022 with the x64 C++ redistributable files if you want to build the installer or portable release package:

[https://jrsoftware.org/isinfo.php](https://jrsoftware.org/isinfo.php)

Publish a non-draft GitHub Release with a `vMAJOR.MINOR.PATCH` tag that matches the version in `BedwarsOverlay.App.csproj`. This triggers `.github/workflows/release.yml`, which builds the installer and portable ZIP and attaches both files to that release. Configure the repository Actions secrets `WINDOWS_SIGNING_CERTIFICATE_BASE64` and `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` first. The current self-signed certificate will not be trusted automatically by other Windows PCs. Create the Base64 secret from the PFX in PowerShell with:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes((Resolve-Path .\Bedwars-Overlay.pfx))) | gh secret set WINDOWS_SIGNING_CERTIFICATE_BASE64 --repo emi-ran/Bedwars-Overlay
```

Set the password secret without putting the password in shell history:

```powershell
$secure = Read-Host "PFX password" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) | gh secret set WINDOWS_SIGNING_CERTIFICATE_PASSWORD --repo emi-ran/Bedwars-Overlay
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
```

Build release artifacts:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.10"
```

Build and sign release artifacts with a `.pfx` certificate:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.10" -Sign -CertificatePath ".\Bedwars-Overlay.pfx"
```

The script prompts for the certificate password. Do not commit `.pfx` or `.p12` files.
By default it signs the app executable, project DLLs, and installer. Use `-SignAllBinaries` only if you want to sign every published `.exe` and `.dll`.

Before publishing, run the release workflow manually from Actions. It builds and signs the project version on Windows 2022 and stores the installer and ZIP as `signed-windows-packages` artifacts without creating a release.

## Product Website

Visit the [Turkish/English product website](https://emi-ran.github.io/Bedwars-Overlay/). Its source is in `docs/`; see [Site maintenance](docs/SITE.md) for local preview and GitHub Pages publishing instructions.

## Notes

- Bedwars Overlay is not affiliated with Hypixel.
- You are responsible for following Hypixel rules and API policies.
- Keep your API key private.
- Use the system tray icon to reopen the overlay or fully exit the app.

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

GNU Affero General Public License v3.0. See [LICENSE](LICENSE).
