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

                string[] rosterNames = _players.Select(player => player.Name).ToArray();
                var recognizedTeams = await Task.Run(() => _teamRecognitionService.ReadTeams(screenshot, rosterNames));
                int matchedCount = 0;

                foreach (var pair in recognizedTeams)
                {
                    foreach (PlayerRow player in _players)
                    {
                        if (!player.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        _currentTeamByPlayer[player.Name] = pair.Value;
                        player.Team = pair.Value;
                        matchedCount++;
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
