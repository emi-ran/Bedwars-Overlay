# Minecraft BedWars Overlay - Project Progress & Handover

This document serves as a detailed progress report and handover guide. If you are starting a new session or continuation chat, read this file to understand the architecture, what has been implemented, and what the next steps are.

---

## 1. Project Goal
Develop a **Windows-only external stats overlay** for Minecraft BedWars (specifically optimized for Badlion Client, with vanilla fallback). 
The app is purely external: it reads `latest.log`, uses the public Mojang/Hypixel APIs, caches lookups locally in SQLite, and displays player stats in a transparent, topmost, click-through overlay window.

*No memory injection, macro commands, memory reading, or packet sniffing.*

---

## 2. Technical Architecture & Design Decisions
- **Stack:** C# .NET 8 + WPF (Windows Presentation Foundation). Chosen over Tauri/WinUI because native Win32/WPF window manipulation is highly reliable for transparency and overlay behavior on Windows.
- **Project Structure:**
  - `BedwarsOverlay.App` (WPF desktop app, launcher, UI rendering, Win32 overlays)
  - `BedwarsOverlay.Core` (Class Library containing Log Watcher, Mojang/Hypixel API Clients, SQLite storage, stats calculator)
  - `BedwarsOverlay.Tests` (xUnit test project)
- **Overlay Window Logic:**
  - **AllowsTransparency = True**, **WindowStyle = None**, **Background = Transparent**, **Topmost = True**, **ShowInTaskbar = False**.
  - **Play Mode (Locked):** Click-through is enabled using Win32 extended style flags (`WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW`). Clicks pass directly to Minecraft.
  - **Edit Mode (Interactive):** Toggled via global hotkey (`Ctrl + Alt + O`). Click-through is disabled (`WS_EX_TRANSPARENT` is stripped), making the window focusable. The user can drag the title bar to move the window, resize it from borders using `WindowChrome`, or click buttons (Settings / Close).
  - **Windows Aero Snap & Snapping Block:**
    - To prevent the window from maximizing or stretching vertically (Vertical Aero Snap) when dragged near screen edges, the window's maximize capability has been disabled by stripping `WS_MAXIMIZEBOX` (0x00010000) from `GWL_STYLE` using Win32 `SetWindowLong`.
    - Temporarily toggle `ResizeMode` to `NoResize` during `DragMove()` and restore it to `CanResize` immediately after drag finishes.
  - **Window Resize:** Handled natively using `WindowChrome` with `ResizeBorderThickness="6"` allowing resizing from any edge/corner in Edit Mode.
  - **Window Min/Max Sizes:** Set `MinWidth="350"` and `MinHeight="150"` to prevent resizing the overlay into an unreadable state.
- **Security:** Hypixel API key will be encrypted locally using **Windows DPAPI** (ProtectedData) before saving it to a local JSON settings file.

---

## 3. Solution File Structure
```txt
BedwarsOverlay/
├─ .gitignore
├─ PROGRESS.md
├─ BedwarsOverlay.slnx
├─ BedwarsOverlay.App/
│  ├─ BedwarsOverlay.App.csproj (WPF, targets net8.0-windows)
│  ├─ App.xaml
│  ├─ App.xaml.cs
│  ├─ MainWindow.xaml
│  ├─ MainWindow.xaml.cs
│  ├─ Services/
│  │  ├─ ClickThroughService.cs (P/Invoke GWL_EXSTYLE / GWL_STYLE)
│  │  └─ HotkeyService.cs       (P/Invoke RegisterHotKey / WndProc hook)
├─ BedwarsOverlay.Core/
│  ├─ BedwarsOverlay.Core.csproj (Class library, targets net8.0)
│  └─ LogWatching/
│     ├─ LogPathDetector.cs (Minecraft log path resolver)
│     └─ LogTailer.cs       (Asynchronous, non-locking file tailer)
├─ BedwarsOverlay.Tests/
│  ├─ BedwarsOverlay.Tests.csproj (xUnit tests, targets net8.0)
```

---

## 4. Current Progress Status

### Phase 1: WPF Solution & Click-Through Prototype [COMPLETED]
- [x] Initialized the .NET 8 multi-project solution structure.
- [x] Installed required NuGet packages:
  - `BedwarsOverlay.App`: `CommunityToolkit.Mvvm`, `System.Security.Cryptography.ProtectedData`
  - `BedwarsOverlay.Core`: `Microsoft.Data.Sqlite`
- [x] Designed `MainWindow.xaml` with transparent borderless styling.
- [x] Implemented `ClickThroughService.cs` to toggle Win32 layered transparency/click-through.
- [x] Implemented `HotkeyService.cs` to listen to global `Ctrl + Alt + O` hotkeys using standard WndProc hooks.
- [x] Prevented Windows Aero Snap, snapping layouts, and double-click maximization.
- [x] Added `MinWidth` and `MinHeight` constraints.
- [x] Verified compilation (`dotnet build` succeeds with 0 warnings/errors).
- [x] Added `.gitignore` file.

### Phase 2: Log Watcher and Path Detection [COMPLETED]
- [x] Implemented `LogTailer.cs` in `BedwarsOverlay.Core` (asynchronous, shared-read background log tailer).
- [x] Implemented `LogPathDetector.cs` to detect Badlion logs and Vanilla fallback logs automatically.
- [x] Verified code builds successfully.

### Phase 3: /who Regex Parser & Diagnostics [COMPLETED]
- [x] Implemented `HypixelLogParser.cs` in `BedwarsOverlay.Core` with regex-based parsing.
- [x] Automatically stripped Minecraft color/formatting signs (`§.`) before matching.
- [x] Implemented a WPF diagnostics panel at the bottom of the window, visible only in Edit Mode.
- [x] Wrote comprehensive xUnit tests in `ParserTests.cs` for multiple `/who` log variations and color formatting.

---

## 5. Next Steps / Remaining Tasks

### Phase 4: SQLite Database & Mojang Resolver
- [ ] Create `AppDatabase.cs` inside `BedwarsOverlay.Core` (schema definition).
- [ ] Store settings, cached player lookups, encounters, and blacklist.
- [ ] Implement DPAPI key storage helper inside database config.
- [ ] Implement `MojangClient.cs` to resolve Username -> UUID (`api.mojang.com`).
  - Implement cache check (24 hour TTL).
  - Handle 204/404 errors (mark as `NICKED_OR_UNKNOWN`).

### Phase 5: Hypixel API Client & Stats Calculator [COMPLETED]
- [x] Implemented `HypixelClient.cs` in `BedwarsOverlay.Core/Api` with API-Key headers and check constraints.
- [x] Implemented `StatsCalculator.cs` in `BedwarsOverlay.Core/Stats` to compute Bedwars level stars prestiges from XP and ratio stats.
- [x] Configured SQLite caching of statistics with custom TTL settings.
- [x] Integrated asynchronous API lookup queries into the main WPF application window.

### Phase 6: WPF Player Table Rendering & Theme Styling [COMPLETED]
- [x] Styled the table with a premium Cubelify-inspired dark theme with alternating obsidian/carbon row colors.
- [x] Implemented local player highlight (blue icons U+E77B/U+EA18 and steel blue background `#0C1B33`).
- [x] Set up dynamic level prestige colors matching in-game BedWars ranges.
- [x] Added action dropdown context menu `...` for each player row containing Plancke shortcut, Copy name, and Blacklist commands.
- [x] Designed custom minimize and close title bar controls using Windows Segoe MDL2 font icons and native hover states.

### Phase 7: Risk Scorer, Blacklist & Encounters
- [ ] Implement `RiskScorer.cs` using the weight formulas:
  - Blacklisted: +40
  - Nicked/Unknown: +20
  - Level < 50 and FKDR > 5: +20
  - FKDR > 10: +15
  - Winstreak > 20: +15
  - Repeated encounter under 10m: +10
- [ ] Add visual risk badges (LOW / MEDIUM / HIGH / EXTREME).

### Phase 8: System Tray Icon & Settings GUI
- [ ] Implement `TrayService.cs` for Minimize-to-Tray.
- [ ] Implement `SettingsWindow.xaml` to edit API Key, log path, cache TTLs, and custom hotkeys.

---

## 6. How to Build & Run
From the root workspace folder:
1. **Restore & Compile:**
   ```powershell
   dotnet build
   ```
2. **Run WPF App:**
   ```powershell
   dotnet run --project BedwarsOverlay.App/BedwarsOverlay.App.csproj
   ```
3. **Run Tests:**
   ```powershell
   dotnet test
   ```
