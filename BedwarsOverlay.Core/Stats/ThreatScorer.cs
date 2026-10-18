using System;
using System.Collections.Generic;
using System.Globalization;

namespace BedwarsOverlay.Core.Stats
{
    /// <summary>Threat band derived from <see cref="ThreatScorer"/>.</summary>
    public enum ThreatLevel
    {
        Low,
        Medium,
        High,
        Extreme
    }

    /// <summary>Threat score with the signals that produced it.</summary>
    public sealed record ThreatResult(double Score, ThreatLevel Level, IReadOnlyList<string> Signals)
    {
        public static ThreatResult Empty { get; } = new(0.0, ThreatLevel.Low, Array.Empty<string>());

        public string Display => Score.ToString("F1", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>
    /// Builds a single 0-100 threat score out of the Bed Wars stats the overlay already has.
    /// Every component uses a monotone saturating curve (1 - 2^(-x/half)) so the previous
    /// piecewise formula's jumps at FKDR 5 and 10 are gone. The score is deliberately not called
    /// "sniper chance": without tracking repeat encounters it only describes how dangerous the
    /// player's stats are. Repeat encounters are folded in when supplied.
    /// </summary>
    public static class ThreatScorer
    {
        public const double MediumThreshold = 25.0;
        public const double HighThreshold = 50.0;
        public const double ExtremeThreshold = 75.0;

        // Half-life values: the stat value that scores 50. They are tuned so a strong regular
        // player lands in the High band while a fresh smurf account with repeat encounters can
        // reach Extreme; review them with real match data.
        private const double FkdrHalf = 2.5;
        private const double WlrHalf = 1.0;
        private const double KdrHalf = 2.5;
        private const double BblrHalf = 1.0;
        private const double LevelHalf = 200.0;
        private const double WinstreakHalf = 8.0;

        // A low game count with a strong FKDR points at a smurf / alt account.
        private const int AltAccountMaxGames = 100;
        private const double AltAccountMinFkdr = 3.0;

        /// <summary>Repeat encounters needed for the repeat signal to saturate.</summary>
        public const int RepeatEncountersForFullSignal = 5;

        private const double FkdrWeight = 0.30;
        private const double WlrWeight = 0.15;
        private const double KdrWeight = 0.10;
        private const double BblrWeight = 0.05;
        private const double LevelWeight = 0.10;
        private const double WinstreakWeight = 0.10;
        private const double AltAccountWeight = 0.10;
        private const double RepeatWeight = 0.10;

        public static ThreatResult Score(BedwarsStats stats, int repeatEncounters = 0, int repeatLookbackMatches = 0)
        {
            if (stats == null)
            {
                return ThreatResult.Empty;
            }

            var signals = new List<string>();

            double weightedSum = 0.0;
            double totalWeight = 0.0;

            AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Fkdr, FkdrHalf), FkdrWeight);
            AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Wlr, WlrHalf), WlrWeight);
            AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Kdr, KdrHalf), KdrWeight);
            AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Bblr, BblrHalf), BblrWeight);
            AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Level, LevelHalf), LevelWeight);

            if (stats.HasWinstreak)
            {
                AddComponent(ref weightedSum, ref totalWeight, Curve(stats.Winstreak, WinstreakHalf), WinstreakWeight);
                if (stats.Winstreak >= 10)
                {
                    signals.Add($"Winstreak {stats.Winstreak}");
                }
            }

            double altAccountScore = AltAccountScore(stats, out int games);
            if (altAccountScore > 0.0)
            {
                signals.Add($"Alt account suspicion ({games} games, FKDR {stats.Fkdr:0.00})");
            }

            AddComponent(ref weightedSum, ref totalWeight, altAccountScore, AltAccountWeight);

            if (repeatEncounters > 0)
            {
                double repeatScore = Math.Min(repeatEncounters / (double)RepeatEncountersForFullSignal, 1.0) * 100.0;
                AddComponent(ref weightedSum, ref totalWeight, repeatScore, RepeatWeight);

                signals.Add(repeatLookbackMatches > 0
                    ? $"Seen in {repeatEncounters} of the last {repeatLookbackMatches} matches"
                    : $"Seen in {repeatEncounters} earlier matches");
            }

            double score = totalWeight > 0.0 ? weightedSum / totalWeight : 0.0;
            score = Math.Clamp(score, 0.0, 100.0);

            return new ThreatResult(score, LevelFor(score), signals);
        }

        public static ThreatLevel LevelFor(double score)
        {
            if (score >= ExtremeThreshold) return ThreatLevel.Extreme;
            if (score >= HighThreshold) return ThreatLevel.High;
            if (score >= MediumThreshold) return ThreatLevel.Medium;
            return ThreatLevel.Low;
        }

        /// <summary>Monotone saturating curve: 50 at <paramref name="half"/>, never exceeds 100.</summary>
        private static double Curve(double value, double half)
        {
            if (value <= 0.0 || half <= 0.0)
            {
                return 0.0;
            }

            double score = 100.0 * (1.0 - Math.Pow(2.0, -value / half));
            return Math.Clamp(score, 0.0, 100.0);
        }

        private static double AltAccountScore(BedwarsStats stats, out int games)
        {
            games = stats.Wins + stats.Losses;
            if (games >= AltAccountMaxGames || stats.Fkdr < AltAccountMinFkdr)
            {
                return 0.0;
            }

            return (1.0 - (games / (double)AltAccountMaxGames)) * 100.0;
        }

        private static void AddComponent(ref double weightedSum, ref double totalWeight, double score, double weight)
        {
            weightedSum += score * weight;
            totalWeight += weight;
        }
    }
}
