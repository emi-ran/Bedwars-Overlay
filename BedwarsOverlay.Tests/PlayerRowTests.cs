using System.Windows.Media;
using BedwarsOverlay.Core.Models;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class PlayerRowTests
    {
        private static readonly Brush RankBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));

        private static Color ColorOf(Brush brush)
        {
            return ((SolidColorBrush)brush).Color;
        }

        [Fact]
        public void Name_UsesRankColor_WhenTeamIsUnknown()
        {
            var row = new PlayerRow { Name = "Player", RankColor = RankBrush };

            Assert.Equal(ColorOf(RankBrush), ColorOf(row.NameColor));
        }

        [Fact]
        public void Name_UsesTeamColor_WhenTeamIsKnown()
        {
            var row = new PlayerRow { Name = "Player", RankColor = RankBrush };

            row.Team = "Red";

            Assert.Equal(ColorOf(row.TeamColor), ColorOf(row.NameColor));
            Assert.NotEqual(ColorOf(RankBrush), ColorOf(row.NameColor));
        }

        [Fact]
        public void Name_FallsBackToRankColor_WhenTeamIsCleared()
        {
            var row = new PlayerRow { Name = "Player", RankColor = RankBrush };
            row.Team = "Blue";

            row.Team = string.Empty;

            Assert.Equal(ColorOf(RankBrush), ColorOf(row.NameColor));
        }

        [Fact]
        public void Name_KeepsTeamColor_WhenRankStatsArriveLater()
        {
            var row = new PlayerRow { Name = "Player" };
            row.Team = "Green";

            row.RankColor = RankBrush;

            Assert.Equal(ColorOf(row.TeamColor), ColorOf(row.NameColor));
        }

        [Fact]
        public void Name_KeepsEliminationColor_WhenTeamIsSetAfterFinalKill()
        {
            var row = new PlayerRow { Name = "Player", RankColor = RankBrush };
            row.MarkEliminated();
            Color eliminationColor = ColorOf(row.NameColor);

            row.Team = "Yellow";

            Assert.Equal(eliminationColor, ColorOf(row.NameColor));
        }
    }
}
