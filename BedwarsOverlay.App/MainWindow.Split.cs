using System;
using System.Windows;
using System.Windows.Threading;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.LogWatching;
using BedwarsOverlay.Core.Teams;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private readonly DispatcherTimer _splitAlertTimer = new() { Interval = TimeSpan.FromSeconds(5) };
        private int _teamRosterVersion;

        private void HandleSplitAlert(PlayerDeathEvent death)
        {
            string localName = _settingsRepository?.LocalPlayerName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(localName)) return;

            if (death.Victim.Equals(localName, StringComparison.OrdinalIgnoreCase))
            {
                HideSplitAlert();
                return;
            }
            if (death.IsFinal) return;

            bool isTeammate = TeammateResolver.IsTeammate(death.Victim, localName, _partyPlayers, _currentTeamByPlayer);
            if (!isTeammate) return;

            SplitAlertPlayer.Text = LocalizationService.Instance["Split_TeammateDied"]
                .Replace("{0}", death.Victim);
            SplitAlert.Visibility = Visibility.Visible;
            _splitAlertTimer.Stop();
            _splitAlertTimer.Start();
            AddDiagnosticLine($"Split reminder: {death.Victim} died (teammate).");
        }

        private void HideSplitAlert()
        {
            _splitAlertTimer.Stop();
            SplitAlert.Visibility = Visibility.Collapsed;
        }

        private void SplitAlertTimer_Tick(object? sender, EventArgs e) => HideSplitAlert();
    }
}
