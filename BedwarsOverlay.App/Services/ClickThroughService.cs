using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BedwarsOverlay.App.Services
{
    public static class ClickThroughService
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;

        /// <summary>
        /// Enables click-through on the specified window.
        /// </summary>
        public static void EnableClickThrough(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// Disables click-through on the specified window (makes it focusable and interactive).
        /// </summary>
        public static void DisableClickThrough(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            // Remove WS_EX_TRANSPARENT so the window receives mouse clicks while remaining layered.
            SetWindowLong(hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED);
        }

        /// <summary>
        /// Removes the maximize style from the window to disable all Windows Aero Snap snapping and maximization behaviors.
        /// </summary>
        public static void DisableMaximizing(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int currentStyle = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, currentStyle & ~WS_MAXIMIZEBOX);
        }

        /// <summary>
        /// Controls whether the overlay appears in the taskbar and Alt-Tab switcher.
        /// </summary>
        public static void SetTaskbarAndAltTabVisibility(Window window, bool isVisible)
        {
            window.ShowInTaskbar = isVisible;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (isVisible)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_TOOLWINDOW) | WS_EX_APPWINDOW);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_APPWINDOW) | WS_EX_TOOLWINDOW);
            }
        }
    }
}
