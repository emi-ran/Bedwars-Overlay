using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Models;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private bool _isCapturingTeams;

        private async Task CaptureTeamsFromPlayerListAsync()
        {
            if (_isCapturingTeams)
            {
                return;
            }

            if ((GetAsyncKeyState(0x09) & 0x8000) == 0) // VK_TAB
            {
                SetStatus(LocalizationService.Instance["Teams_HoldTab"]);
                return;
            }

            if (_players.Count == 0)
            {
                SetStatus(LocalizationService.Instance["Teams_LoadPlayersFirst"]);
                return;
            }

            IntPtr gameWindow = GetForegroundWindow();
            if (gameWindow == IntPtr.Zero || gameWindow == new System.Windows.Interop.WindowInteropHelper(this).Handle ||
                !GetWindowRect(gameWindow, out NativeRect bounds) || bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top)
            {
                SetStatus(LocalizationService.Instance["Teams_FocusMinecraft"]);
                return;
            }

            _isCapturingTeams = true;
            var captureTimer = Stopwatch.StartNew();
            SetStatus(LocalizationService.Instance["Teams_Reading"]);
            bool restoreOverlay = IsVisible;

            try
            {
                int rosterVersion = _teamRosterVersion;
                if (restoreOverlay)
                {
                    Hide();
                    await Task.Delay(120);
                }

                byte[] screenshot;
                using (var bitmap = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
                    screenshot = TeamRecognitionService.EncodeCapture(bitmap);
                }

                if (restoreOverlay)
                {
                    ShowWithoutActivating();
                }

                // Hidden self/party rows still need team assignments for teammate alerts.
                string[] rosterNames = _matchRoster.GetPlayers()
                    .Concat(_players.Select(player => player.Name))
                    .Concat(_partyPlayers)
                    .Append(_settingsRepository?.LocalPlayerName ?? string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var recognizedTeams = await Task.Run(() => _teamRecognitionService.ReadTeams(screenshot, rosterNames));
                if (rosterVersion != _teamRosterVersion) return;
                int matchedCount = 0;

                foreach (var pair in recognizedTeams)
                {
                    _currentTeamByPlayer[pair.Key] = pair.Value;
                    matchedCount++;
                    foreach (PlayerRow player in _players)
                    {
                        if (!player.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        player.Team = pair.Value;
                        break;
                    }
                }

                if (matchedCount == 0)
                {
                    SetStatus(LocalizationService.Instance["Teams_NoneFound"]);
                    AddDiagnosticLine($"Team capture found no roster matches from {bounds.Right - bounds.Left}x{bounds.Bottom - bounds.Top} in {captureTimer.ElapsedMilliseconds} ms.");
                    return;
                }

                string result = LocalizationService.Instance["Teams_Updated"].Replace("{0}", matchedCount.ToString());
                SetStatus(result);
                AddDiagnosticLine($"{result} ({bounds.Right - bounds.Left}x{bounds.Bottom - bounds.Top}, {captureTimer.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                if (restoreOverlay && !IsVisible)
                {
                    ShowWithoutActivating();
                }

                SetStatus(LocalizationService.Instance["Teams_Failed"]);
                AddDiagnosticLine($"[ERROR] Team capture failed after {captureTimer.ElapsedMilliseconds} ms: {ex.Message}");
            }
            finally
            {
                if (restoreOverlay && !IsVisible)
                {
                    ShowWithoutActivating();
                }

                _isCapturingTeams = false;
            }
        }

        private void ShowWithoutActivating()
        {
            bool previousShowActivated = ShowActivated;
            ShowActivated = false;
            Show();
            ShowActivated = previousShowActivated;
        }
    }
}
