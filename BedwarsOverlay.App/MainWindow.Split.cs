using System;
using System.Windows;
using System.Windows.Threading;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.LogWatching;

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

            bool isTeammate;
            if (_currentTeamByPlayer.TryGetValue(localName, out string? localTeam) &&
                _currentTeamByPlayer.TryGetValue(death.Victim, out string? victimTeam))
            {
                // A party member on a known opposing team must not trigger the reminder.
                isTeammate = localTeam.Equals(victimTeam, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isTeammate = _partyPlayers.Contains(death.Victim);
            }
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
