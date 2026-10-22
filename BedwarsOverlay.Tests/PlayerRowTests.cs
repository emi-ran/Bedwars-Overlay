using System.Threading.Tasks;
using System.Windows.Media;
using BedwarsOverlay.Core.Models;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class PlayerRowTests
    {
        // Frozen so xUnit can hand the brush between test threads, like the row brushes are.
        private static readonly Brush RankBrush = CreateRankBrush();

        private static Brush CreateRankBrush()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
            brush.Freeze();
            return brush;
        }

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

        /// <summary>
        /// Rows are built off the UI thread in the app and in this suite (xUnit spreads test classes
        /// over a thread pool), so reading their brushes from another thread must not throw.
        /// </summary>
        [Fact]
        public async Task Row_BrushesStayReadable_WhenRowIsBuiltOnAnotherThread()
        {
            PlayerRow row = await Task.Run(() =>
            {
                var created = new PlayerRow { Name = "Player" };
                created.Team = "Green";
                return created;
            });

            Assert.Equal(Color.FromRgb(0x22, 0xC5, 0x5E), ColorOf(row.TeamColor));
            Assert.Equal(Color.FromRgb(0x22, 0xC5, 0x5E), ColorOf(row.NameColor));
            Assert.Equal(Color.FromRgb(0x0F, 0x0F, 0x11), ColorOf(row.RowBackground));
        }
    }
}
