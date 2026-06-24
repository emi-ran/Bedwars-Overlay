using System;
using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace BedwarsOverlay.App.Services
{
    public sealed class TrayIconService : IDisposable
    {
        private readonly Window _owner;
        private readonly Action _showOverlay;
        private readonly Action _hideOverlay;
        private readonly Action _openSettings;
        private readonly Action _exitApplication;
        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly Forms.ToolStripMenuItem _showHideItem;

        public TrayIconService(
            Window owner,
            Action showOverlay,
            Action hideOverlay,
            Action openSettings,
            Action exitApplication)
        {
            _owner = owner;
            _showOverlay = showOverlay;
            _hideOverlay = hideOverlay;
            _openSettings = openSettings;
            _exitApplication = exitApplication;

            _showHideItem = new Forms.ToolStripMenuItem(GetShowHideText());
            if (System.Drawing.SystemFonts.MenuFont != null)
            {
                _showHideItem.Font = new Font(System.Drawing.SystemFonts.MenuFont, System.Drawing.FontStyle.Bold);
            }
            _showHideItem.Click += (_, _) => ToggleOverlay();

            var settingsItem = new Forms.ToolStripMenuItem(LocalizationService.Instance["Tray_Settings"]);
            settingsItem.Click += (_, _) => _owner.Dispatcher.BeginInvoke(_openSettings);

            var exitItem = new Forms.ToolStripMenuItem(LocalizationService.Instance["Tray_Exit"]);
            exitItem.Click += (_, _) => _owner.Dispatcher.BeginInvoke(_exitApplication);

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(_showHideItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon = new Forms.NotifyIcon
            {
                ContextMenuStrip = menu,
                Icon = LoadIcon(),
                Text = "Bedwars Overlay",
                Visible = true
            };
            _notifyIcon.DoubleClick += (_, _) => ToggleOverlay();
        }

        public void RefreshText()
        {
            _showHideItem.Text = GetShowHideText();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        private void ToggleOverlay()
        {
            _owner.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_owner.IsVisible)
                {
                    _hideOverlay();
                }
                else
                {
                    _showOverlay();
                }

                RefreshText();
            }));
        }

        private string GetShowHideText()
        {
            return _owner.IsVisible
                ? LocalizationService.Instance["Tray_HideOverlay"]
                : LocalizationService.Instance["Tray_ShowOverlay"];
        }

        private static Icon LoadIcon()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    var icon = Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }
            catch
            {
            }

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "bedwars-overlay-icon.ico");
            return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
        }
    }
}
