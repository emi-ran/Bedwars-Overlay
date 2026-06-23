using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BedwarsOverlay.App.Services;

namespace BedwarsOverlay.App
{
    public partial class MainWindow : Window
    {
        private HotkeyService? _hotkeyService;
        private bool _isEditMode = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start in play mode (transparent, click-through)
            ApplyPlayMode();

            // Disable Windows Aero Snap maximizing & vertical snapping behaviors natively
            ClickThroughService.DisableMaximizing(this);

            // Set up global hotkey
            try
            {
                _hotkeyService = new HotkeyService(this);
                _hotkeyService.HotkeyPressed += ToggleEditMode;
                
                // Register Ctrl + Alt + O (Modifiers: Control = 0x0002, Alt = 0x0001 -> 3; Virtual Key 'O' = 0x4F)
                bool success = _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_ALT, 0x4F);
                if (!success)
                {
                    MessageBox.Show("Failed to register global hotkey Ctrl+Alt+O! Please check if another app is using this shortcut.", 
                                    "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing hotkey service: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleEditMode()
        {
            _isEditMode = !_isEditMode;
            if (_isEditMode)
            {
                ApplyEditMode();
            }
            else
            {
                ApplyPlayMode();
            }
        }

        private void ApplyPlayMode()
        {
            // Enable Win32 Click-Through
            ClickThroughService.EnableClickThrough(this);

            // Style UI for background mode (non-obtrusive)
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x0A, 0x0A, 0x0A)); // semi-transparent black
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)); // very faint white border
            StatusText.Text = "PLAY MODE • Ctrl+Alt+O to Edit";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)); // bright readable light grey
            ControlButtons.Visibility = Visibility.Collapsed;
            TitleBar.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x00, 0x00));
        }

        private void ApplyEditMode()
        {
            // Disable Win32 Click-Through (allows clicks to interact with window)
            ClickThroughService.DisableClickThrough(this);

            // Style UI for interactive mode
            OverlayBorder.Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1B)); // solid zinc dark gray
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)); // premium indigo border
            StatusText.Text = "EDIT MODE • Ctrl+Alt+O to Lock";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0x8C, 0xF8)); // light indigo (high contrast)
            ControlButtons.Visibility = Visibility.Visible;
            TitleBar.Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));

            // Force activate/focus the window
            this.Activate();
            this.Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isEditMode)
            {
                // Prevent maximizing on double-click
                if (e.ClickCount == 2)
                {
                    e.Handled = true;
                    return;
                }

                var previousMode = this.ResizeMode;
                this.ResizeMode = ResizeMode.NoResize; // Temporarily disable resize to bypass Windows Aero Snap
                try
                {
                    this.DragMove();
                }
                finally
                {
                    this.ResizeMode = previousMode; // Restore original resize mode (CanResize)
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings panel implementation is scheduled for a future phase.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyService?.Dispose();
            base.OnClosed(e);
        }
    }
}