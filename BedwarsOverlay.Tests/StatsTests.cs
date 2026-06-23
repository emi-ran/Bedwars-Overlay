using BedwarsOverlay.Core.Stats;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class StatsTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(500, 1)]
        [InlineData(1000, 2)]
        [InlineData(2500, 2)]
        [InlineData(3000, 3)]
        [InlineData(6000, 4)]
        [InlineData(10000, 5)]
        [InlineData(14999, 5)]
        [InlineData(15000, 6)]
        [InlineData(489999, 100)]
        [InlineData(490000, 101)]
        [InlineData(980000, 201)]
        public void GetLevelFromXp_CalculatesCorrectLevels(long xp, int expectedLevel)
        {
            int level = StatsCalculator.GetLevelFromXp(xp);
            Assert.Equal(expectedLevel, level);
        }

        [Fact]
        public void ComputedStatsRatios_HandleZeroDeathsAndLosses()
        {
            var stats = new BedwarsStats
            {
                FinalKills = 10,
                FinalDeaths = 0,
                Wins = 5,
                Losses = 0,
                Kills = 20,
                Deaths = 0,
                BedsBroken = 4,
                BedsLost = 0
            };

            Assert.Equal(10, stats.Fkdr);
            Assert.Equal(5, stats.Wlr);
            Assert.Equal(20, stats.Kdr);
            Assert.Equal(4, stats.Bblr);
        }

        [Fact]
        public void ComputedStatsRatios_CalculateDivisionCorrectly()
        {
            var stats = new BedwarsStats
            {
                FinalKills = 10,
                FinalDeaths = 3,
                Wins = 5,
                Losses = 4
            };

            // 10 / 3 = 3.33333 -> Round to 2 decimals -> 3.33
            Assert.Equal(3.33, stats.Fkdr);
            // 5 / 4 = 1.25 -> Round to 2 decimals -> 1.25
            Assert.Equal(1.25, stats.Wlr);
        }
    }
}
