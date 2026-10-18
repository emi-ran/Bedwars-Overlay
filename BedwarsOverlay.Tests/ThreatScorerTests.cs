using BedwarsOverlay.Core.Stats;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class ThreatScorerTests
    {
        private static BedwarsStats Stats(
            int level = 1,
            int winstreak = 0,
            bool hasWinstreak = false,
            int finalKills = 0,
            int finalDeaths = 0,
            int wins = 0,
            int losses = 0,
            int kills = 0,
            int deaths = 0,
            int bedsBroken = 0,
            int bedsLost = 0)
        {
            return new BedwarsStats
            {
                Level = level,
                Winstreak = winstreak,
                HasWinstreak = hasWinstreak,
                FinalKills = finalKills,
                FinalDeaths = finalDeaths,
                Wins = wins,
                Losses = losses,
                Kills = kills,
                Deaths = deaths,
                BedsBroken = bedsBroken,
                BedsLost = bedsLost
            };
        }

        [Fact]
        public void Score_IsMonotoneInFkdr()
        {
            double previous = -1.0;

            foreach (double fkdr in new[] { 0.5, 1.0, 2.0, 5.0, 10.0, 20.0 })
            {
                var stats = Stats(level: 100, finalKills: (int)(fkdr * 100), finalDeaths: 100, wins: 200, losses: 200, kills: 400, deaths: 400, bedsBroken: 100, bedsLost: 100);
                double score = ThreatScorer.Score(stats).Score;

                Assert.True(score > previous, $"{fkdr:0.0} FKDR did not increase the score ({score} <= {previous})");
                previous = score;
            }
        }

        [Fact]
        public void Score_NeverExceedsHundred()
        {
            var stats = Stats(level: 3000, hasWinstreak: true, winstreak: 500, finalKills: 200000, finalDeaths: 100, wins: 50000, losses: 100, kills: 400000, deaths: 10, bedsBroken: 50000, bedsLost: 1);

            var result = ThreatScorer.Score(stats, repeatEncounters: 10, repeatLookbackMatches: 10);

            Assert.True(result.Score <= 100.0);
            Assert.Equal(ThreatLevel.Extreme, result.Level);
        }

        [Fact]
        public void Score_NewAccountWithHighFkdr_FlagsAltAccount()
        {
            var stats = Stats(level: 40, finalKills: 250, finalDeaths: 40, wins: 30, losses: 10);

            var result = ThreatScorer.Score(stats);

            Assert.Contains(result.Signals, s => s.Contains("Alt account"));
        }

        [Fact]
        public void Score_ExperiencedAccount_DoesNotFlagAltAccount()
        {
            var stats = Stats(level: 500, finalKills: 20000, finalDeaths: 4000, wins: 3000, losses: 1500);

            var result = ThreatScorer.Score(stats);

            Assert.DoesNotContain(result.Signals, s => s.Contains("Alt account"));
        }

        [Fact]
        public void Score_PrivateWinstreak_IsRenormalizedInsteadOfCountedAsZero()
        {
            var withPrivate = Stats(level: 300, hasWinstreak: false, winstreak: 40, finalKills: 3000, finalDeaths: 1000, wins: 500, losses: 300);
            var withPublicZero = Stats(level: 300, hasWinstreak: true, winstreak: 0, finalKills: 3000, finalDeaths: 1000, wins: 500, losses: 300);

            // A hidden winstreak drops its weight; scoring it as 0 would drag the threat down.
            Assert.True(ThreatScorer.Score(withPrivate).Score >= ThreatScorer.Score(withPublicZero).Score);
            Assert.DoesNotContain(ThreatScorer.Score(withPrivate).Signals, s => s.StartsWith("Winstreak"));
            Assert.Contains(ThreatScorer.Score(Stats(level: 300, hasWinstreak: true, winstreak: 40, finalKills: 3000, finalDeaths: 1000, wins: 500, losses: 300)).Signals, s => s.StartsWith("Winstreak"));
        }

        [Fact]
        public void Score_RepeatEncounters_RaiseScoreAndAddSignal()
        {
            var stats = Stats(level: 200, finalKills: 2000, finalDeaths: 1000, wins: 400, losses: 400);

            double withoutRepeats = ThreatScorer.Score(stats).Score;
            var withRepeats = ThreatScorer.Score(stats, repeatEncounters: 4, repeatLookbackMatches: 10);

            Assert.True(withRepeats.Score > withoutRepeats);
            Assert.Contains(withRepeats.Signals, s => s.Contains("4 of the last 10"));
        }

        [Fact]
        public void Score_Calibration_StrongRegularPlayerIsHighAndSmurfIsAtLeastHigh()
        {
            // FKDR 6.0, WLR 1.2, KDR 2.5, BBLR 1.0, level 300, winstreak 20, established account.
            var regular = Stats(level: 300, hasWinstreak: true, winstreak: 20, finalKills: 18000, finalDeaths: 3000, wins: 3000, losses: 2500, kills: 40000, deaths: 16000, bedsBroken: 6000, bedsLost: 6000);

            var regularResult = ThreatScorer.Score(regular);

            Assert.Equal(ThreatLevel.High, regularResult.Level);

            // 35-game account with FKDR 15 seen in 5 of the last 10 matches.
            var smurf = Stats(level: 60, finalKills: 300, finalDeaths: 20, wins: 25, losses: 10, kills: 600, deaths: 120, bedsBroken: 40, bedsLost: 20);

            var smurfResult = ThreatScorer.Score(smurf, repeatEncounters: 5, repeatLookbackMatches: 10);

            // Extreme is reachable exactly for the smurf + repeat-encounter profile the column is for.
            Assert.Equal(ThreatLevel.Extreme, smurfResult.Level);
            Assert.True(smurfResult.Score > regularResult.Score);
        }

        [Theory]
        [InlineData(0.0, ThreatLevel.Low)]        [InlineData(24.99, ThreatLevel.Low)]
        [InlineData(25.0, ThreatLevel.Medium)]
        [InlineData(49.99, ThreatLevel.Medium)]
        [InlineData(50.0, ThreatLevel.High)]
        [InlineData(74.99, ThreatLevel.High)]
        [InlineData(75.0, ThreatLevel.Extreme)]
        [InlineData(100.0, ThreatLevel.Extreme)]
        public void LevelFor_MapsBands(double score, ThreatLevel expected)
        {
            Assert.Equal(expected, ThreatScorer.LevelFor(score));
        }

        [Fact]
        public void Score_WithoutWinstreak_DoesNotAddWinstreakSignal()
        {
            var stats = Stats(level: 100, hasWinstreak: false, winstreak: 40, finalKills: 1000, finalDeaths: 1000, wins: 100, losses: 100);

            var result = ThreatScorer.Score(stats);

            Assert.DoesNotContain(result.Signals, s => s.StartsWith("Winstreak"));
        }
    }
}
