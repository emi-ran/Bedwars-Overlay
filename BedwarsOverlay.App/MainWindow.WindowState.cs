using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            Services.ClickThroughService.SetTaskbarAndAltTabVisibility(this, _isEditMode);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleBarLayout();
            ScheduleWindowBoundsSave();
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            ScheduleWindowBoundsSave();
        }

        private void WindowBoundsSaveTimer_Tick(object? sender, EventArgs e)
        {
            _windowBoundsSaveTimer.Stop();
            SaveWindowBounds();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ToggleEditMode()
        {
            if (_isSettingsOpen || _isHiddenMode || OnboardingPanel.Visibility == Visibility.Visible)
            {
                return;
            }

            _isEditMode = !_isEditMode;
            IsEditMode = _isEditMode;
            if (_isEditMode)
            {
                ApplyEditMode();
            }
            else
            {
                ApplyPlayMode();
            }
        }

        private void ToggleVisibilityMode()
        {
            if (_isSettingsOpen)
            {
                return;
            }

            if (_isHiddenMode)
            {
                ShowOverlayFromTray();
                return;
            }

            HideOverlayToTray();
        }

        private void ShowOverlayFromTray()
        {
            _isHiddenMode = false;
            Show();
            Activate();
            _trayIconService?.RefreshText();
        }

        private void HideOverlayToTray()
        {
            SaveWindowBounds();
            _isHiddenMode = true;
            Hide();
            _trayIconService?.RefreshText();
        }

        private void ExitFromTray()
        {
            _isExiting = true;
            Close();
        }

        private void ApplyPlayMode()
        {
            SaveWindowBounds();

            Services.ClickThroughService.EnableClickThrough(this);
            Services.ClickThroughService.SetTaskbarAndAltTabVisibility(this, false);

            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x11, 0x11, 0x11));
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));

            TitleBar.Background = Brushes.Transparent;
            ControlButtons.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            EditModeBadge.Visibility = Visibility.Collapsed;
            ClearSearchFilter();
            UpdateTitleBarLayout();

            if (DiagnosticsPanel != null)
            {
                UpdateDiagnosticsVisibility();
            }

            UpdateAvailableButtonVisibility();
        }

        private void ApplyEditMode()
        {
            Services.ClickThroughService.DisableClickThrough(this);
            Services.ClickThroughService.SetTaskbarAndAltTabVisibility(this, true);

            OverlayBorder.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
            OverlayBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1));

            TitleBar.Background = Brushes.Transparent;
            ControlButtons.Visibility = Visibility.Visible;
            EditModeBadge.Visibility = Visibility.Visible;
            UpdateTitleBarLayout();

            if (DiagnosticsPanel != null)
            {
                UpdateDiagnosticsVisibility();
            }
            UpdateAvailableButtonVisibility();

            Activate();
            Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed && _isEditMode)
            {
                if (e.ClickCount == 2)
                {
                    e.Handled = true;
                    return;
                }

                var previousMode = ResizeMode;
                ResizeMode = ResizeMode.NoResize;
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can only run while the left mouse button is still held.
                }
                finally
                {
                    ResizeMode = previousMode;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideOverlayToTray();
        }

        private void UpdateTitleBarLayout()
        {
            if (TitleText == null || EditModeBadgeText == null || SearchPanel == null)
            {
                return;
            }

            var loc = Services.LocalizationService.Instance;
            bool narrowTitle = ActualWidth > 0 && ActualWidth < 560;
            bool narrowBadge = ActualWidth > 0 && ActualWidth < 660;
            bool hideSearch = !_isEditMode || (ActualWidth > 0 && ActualWidth < 720);

            TitleText.Text = loc[narrowTitle ? "Title_BedwarsOverlayShort" : "Title_BedwarsOverlay"];
            EditModeBadgeText.Text = loc[narrowBadge ? "EditMode_BadgeShort" : "EditMode_Badge"];
            SearchPanel.Visibility = hideSearch ? Visibility.Collapsed : Visibility.Visible;
            UpdateAvailableButtonVisibility();
        }

        private void RestoreWindowBounds()
        {
            if (_settingsRepository == null)
            {
                return;
            }

            double left = _settingsRepository.WindowLeft;
            double top = _settingsRepository.WindowTop;
            double width = _settingsRepository.WindowWidth;
            double height = _settingsRepository.WindowHeight;
            if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(width) || double.IsNaN(height))
            {
                return;
            }

            width = Math.Max(MinWidth, width);
            height = Math.Max(MinHeight, height);
            var savedBounds = new Rect(left, top, width, height);
            var screenBounds = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);

            if (!screenBounds.IntersectsWith(savedBounds))
            {
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        private void SaveWindowBounds()
        {
            if (_settingsRepository == null || !_hasLoaded || WindowState == WindowState.Minimized || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            _settingsRepository.SaveWindowBounds(Left, Top, ActualWidth, ActualHeight);
        }

        private void ScheduleWindowBoundsSave()
        {
            if (!_hasLoaded || !_isEditMode || _isHiddenMode || WindowState == WindowState.Minimized)
            {
                return;
            }

            _windowBoundsSaveTimer.Stop();
            _windowBoundsSaveTimer.Start();
        }
    }
}
