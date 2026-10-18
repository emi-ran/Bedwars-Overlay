using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using BedwarsOverlay.Core.Models;
using BedwarsOverlay.Core.Stats;
using Xunit;

namespace BedwarsOverlay.Tests;

public class UnitTest1
{
    [Fact]
    public void TestSortingByLevelRaw()
    {
        var players = new ObservableCollection<PlayerRow>();
        var row1 = PlayerRow.CreateLoading("Player1", false);
        var row2 = PlayerRow.CreateLoading("Player2", false);
        players.Add(row1);
        players.Add(row2);

        var view = CollectionViewSource.GetDefaultView(players);
        view.SortDescriptions.Add(new SortDescription("LevelRaw", ListSortDirection.Ascending));

        // Update levels: row1 is lvl 106, row2 is lvl 12
        row1.Update(106, false, 0, 0, 0, 0, 0, ThreatResult.Empty, false, System.Windows.Media.Brushes.White);
        row2.Update(12, false, 0, 0, 0, 0, 0, ThreatResult.Empty, false, System.Windows.Media.Brushes.White);

        view.Refresh();

        var sorted = new System.Collections.Generic.List<PlayerRow>();
        foreach (PlayerRow p in view)
        {
            sorted.Add(p);
        }

        // Expected order: Player2 (Lvl 12) should be first, then Player1 (Lvl 106)
        Assert.Equal("Player2", sorted[0].Name);
        Assert.Equal("Player1", sorted[1].Name);
    }

    [Fact]
    public void TestSettingsRepositorySortPersistence()
    {
        var repo = new BedwarsOverlay.Core.Storage.SettingsRepository();
        
        // Save current values to restore later
        string originalCol = repo.SortColumn;
        string originalDir = repo.SortDirection;
        
        try
        {
            repo.SortColumn = "FkdrRaw";
            repo.SortDirection = "Descending";
            
            // Re-instantiate repository to verify persistence
            var repo2 = new BedwarsOverlay.Core.Storage.SettingsRepository();
            Assert.Equal("FkdrRaw", repo2.SortColumn);
            Assert.Equal("Descending", repo2.SortDirection);
        }
        finally
        {
            // Restore original settings
            repo.SortColumn = originalCol;
            repo.SortDirection = originalDir;
        }
    }

    [Fact]
    public void TestSettingsRepositoryDoesNotOverwriteNewerSettingsFromOtherInstances()
    {
        var staleRepo = new BedwarsOverlay.Core.Storage.SettingsRepository();
        var newerRepo = new BedwarsOverlay.Core.Storage.SettingsRepository();

        string originalLocalPlayerName = staleRepo.LocalPlayerName;
        double originalLeft = staleRepo.WindowLeft;
        double originalTop = staleRepo.WindowTop;
        double originalWidth = staleRepo.WindowWidth;
        double originalHeight = staleRepo.WindowHeight;

        try
        {
            newerRepo.LocalPlayerName = "RepoMergeTest";
            staleRepo.SaveWindowBounds(11, 22, 333, 444);

            var repoAfterSave = new BedwarsOverlay.Core.Storage.SettingsRepository();
            Assert.Equal("RepoMergeTest", repoAfterSave.LocalPlayerName);
            Assert.Equal(11, repoAfterSave.WindowLeft);
            Assert.Equal(22, repoAfterSave.WindowTop);
            Assert.Equal(333, repoAfterSave.WindowWidth);
            Assert.Equal(444, repoAfterSave.WindowHeight);
        }
        finally
        {
            var restoreRepo = new BedwarsOverlay.Core.Storage.SettingsRepository();
            restoreRepo.LocalPlayerName = originalLocalPlayerName;
            restoreRepo.SaveWindowBounds(originalLeft, originalTop, originalWidth, originalHeight);
        }
    }

    [Fact]
    public void TestPlayerRowPropertyChangedNotification()
    {
        var row = PlayerRow.CreateLoading("Test", false);
        
        bool levelChanged = false;
        bool levelRawChanged = false;
        
        row.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PlayerRow.Level)) levelChanged = true;
            if (e.PropertyName == nameof(PlayerRow.LevelRaw)) levelRawChanged = true;
        };
        
        row.Update(106, false, 0, 0, 0, 0, 0, ThreatResult.Empty, false, System.Windows.Media.Brushes.White);
        
        Assert.True(levelChanged);
        Assert.True(levelRawChanged);
        Assert.Equal("106", row.Level);
        Assert.Equal(106, row.LevelRaw);
    }

    [Fact]
    public void TestPlayerRowRepeatBadge_RespectsMinimumCount()
    {
        var row = PlayerRow.CreateLoading("Notch", false);

        row.ApplyEncounterInfo(1, 2, "seen once");
        Assert.Equal(System.Windows.Visibility.Collapsed, row.RepeatBadgeVisibility);

        row.ApplyEncounterInfo(2, 2, "seen twice");
        Assert.Equal(System.Windows.Visibility.Visible, row.RepeatBadgeVisibility);
        Assert.Equal("×2", row.RepeatBadgeText);
        Assert.Equal("seen twice", row.RepeatTooltip);

        row.ApplyEncounterInfo(5, 2, "seen a lot");
        Assert.Equal("×5", row.RepeatBadgeText);
    }

    [Fact]
    public void TestPlayerRowThreat_ComesFromScorer()
    {
        var row = PlayerRow.CreateLoading("Notch", false);
        var threat = BedwarsOverlay.Core.Stats.ThreatScorer.Score(new BedwarsStats
        {
            Level = 3000,
            HasWinstreak = true,
            Winstreak = 500,
            FinalKills = 200000,
            FinalDeaths = 100,
            Wins = 50000,
            Losses = 100,
            Kills = 400000,
            Deaths = 10,
            BedsBroken = 50000,
            BedsLost = 1
        });

        row.ApplyThreat(threat);

        Assert.Equal(threat.Score, row.ThreatRaw);
        Assert.Equal(ThreatLevel.Extreme.ToString().ToUpperInvariant(), row.RiskScore);
        Assert.Equal(System.Windows.Visibility.Visible, row.TagVisibility);
        Assert.Contains("%", row.Threat);
    }

    [Fact]
    public void TestPlayerRowRankTags()
    {
        var stats = new BedwarsOverlay.Core.Stats.BedwarsStats
        {
            NewPackageRank = "MVP_PLUS"
        };
        
        Assert.Equal("[MVP+]", stats.GetRankTag());
        Assert.Equal("Cyan", stats.GetRankColorName());

        var stats2 = new BedwarsOverlay.Core.Stats.BedwarsStats
        {
            MonthlyPackageRank = "SUPERSTAR"
        };
        Assert.Equal("[MVP++]", stats2.GetRankTag());
        Assert.Equal("Gold", stats2.GetRankColorName());
    }
}
