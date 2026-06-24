using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BedwarsOverlay.App.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private readonly Window _window;
        private IntPtr _hwnd;
        private HwndSource? _hwndSource;
        private readonly HashSet<int> _registeredHotkeyIds = new();

        public event Action<int>? HotkeyPressed;

        public HotkeyService(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
            {
                Initialize();
            }
            else
            {
                _window.SourceInitialized += OnWindowSourceInitialized;
            }
        }

        private void OnWindowSourceInitialized(object? sender, EventArgs e)
        {
            _window.SourceInitialized -= OnWindowSourceInitialized;
            Initialize();
        }

        private void Initialize()
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(HwndHook);
        }

        /// <summary>
        /// Registers a global hotkey.
        /// </summary>
        /// <param name="modifiers">E.g., MOD_CONTROL | MOD_ALT</param>
        /// <param name="key">Virtual Key code (e.g., 0x4F for 'O')</param>
        /// <returns>True if registered successfully.</returns>
        public bool Register(int id, uint modifiers, uint key)
        {
            if (_hwnd == IntPtr.Zero) return false;
            if (!RegisterHotKey(_hwnd, id, modifiers, key)) return false;
            _registeredHotkeyIds.Add(id);
            return true;
        }

        /// <summary>
        /// Unregisters the global hotkey.
        /// </summary>
        public void Unregister()
        {
            if (_hwnd != IntPtr.Zero)
            {
                foreach (int id in _registeredHotkeyIds)
                {
                    UnregisterHotKey(_hwnd, id);
                }
                _registeredHotkeyIds.Clear();
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                long id = wParam.ToInt64();
                if (id >= int.MinValue && id <= int.MaxValue && _registeredHotkeyIds.Contains((int)id))
                {
                    HotkeyPressed?.Invoke((int)id);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            _hwndSource?.RemoveHook(HwndHook);
            _hwndSource?.Dispose();
        }
    }
}
