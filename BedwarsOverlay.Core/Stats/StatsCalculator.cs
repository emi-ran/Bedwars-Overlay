using System;
using System.Text.Json;

namespace BedwarsOverlay.Core.Stats
{
    public static class StatsCalculator
    {
        /// <summary>
        /// Parses the raw Hypixel player JSON string into a structured BedwarsStats object.
        /// </summary>
        public static BedwarsStats ParseStats(string playerJson)
        {
            var stats = new BedwarsStats();
            if (string.IsNullOrWhiteSpace(playerJson)) return stats;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(playerJson))
                {
                    JsonElement root = doc.RootElement;

                    // Parse profile properties
                    if (root.TryGetProperty("uuid", out var uuidProp)) stats.UUID = uuidProp.GetString() ?? "";
                    if (root.TryGetProperty("displayname", out var nameProp)) stats.DisplayName = nameProp.GetString() ?? "";
                    if (root.TryGetProperty("rank", out var rankProp)) stats.Rank = rankProp.GetString() ?? "";
                    if (root.TryGetProperty("packageRank", out var pRankProp)) stats.PackageRank = pRankProp.GetString() ?? "";
                    if (root.TryGetProperty("newPackageRank", out var npRankProp)) stats.NewPackageRank = npRankProp.GetString() ?? "";
                    if (root.TryGetProperty("monthlyPackageRank", out var mpRankProp)) stats.MonthlyPackageRank = mpRankProp.GetString() ?? "";

                    // Parse achievements for level star
                    bool hasLevelFromAchievements = false;
                    if (root.TryGetProperty("achievements", out var achievements))
                    {
                        if (achievements.TryGetProperty("bedwars_level", out var bwLevelProp))
                        {
                            stats.Level = bwLevelProp.GetInt32();
                            hasLevelFromAchievements = true;
                        }
                    }

                    // Parse game stats
                    if (root.TryGetProperty("stats", out var statsNode) && statsNode.TryGetProperty("Bedwars", out var bwNode))
                    {
                        // Experience & Level Calculation if not from achievements
                        long xp = 0;
                        if (bwNode.TryGetProperty("Experience", out var xpProp))
                        {
                            xp = xpProp.GetInt64();
                        }

                        if (!hasLevelFromAchievements)
                        {
                            stats.Level = GetLevelFromXp(xp);
                        }

                        // Basic stats
                        if (bwNode.TryGetProperty("wins_bedwars", out var winsProp)) stats.Wins = winsProp.GetInt32();
                        if (bwNode.TryGetProperty("losses_bedwars", out var lossesProp)) stats.Losses = lossesProp.GetInt32();
                        if (bwNode.TryGetProperty("final_kills_bedwars", out var fKillsProp)) stats.FinalKills = fKillsProp.GetInt32();
                        if (bwNode.TryGetProperty("final_deaths_bedwars", out var fDeathsProp)) stats.FinalDeaths = fDeathsProp.GetInt32();
                        if (bwNode.TryGetProperty("kills_bedwars", out var killsProp)) stats.Kills = killsProp.GetInt32();
                        if (bwNode.TryGetProperty("deaths_bedwars", out var deathsProp)) stats.Deaths = deathsProp.GetInt32();
                        if (bwNode.TryGetProperty("beds_broken_bedwars", out var bBrokenProp)) stats.BedsBroken = bBrokenProp.GetInt32();
                        if (bwNode.TryGetProperty("beds_lost_bedwars", out var bLostProp)) stats.BedsLost = bLostProp.GetInt32();

                        // Winstreak handling
                        if (bwNode.TryGetProperty("winstreak", out var winstreakProp))
                        {
                            stats.Winstreak = winstreakProp.GetInt32();
                            stats.HasWinstreak = true;
                        }
                        else
                        {
                            stats.HasWinstreak = false;
                        }
                    }
                }
            }
            catch
            {
                // Return defaults on parse failure
            }

            return stats;
        }

        /// <summary>
        /// Computes Bedwars level from raw Bedwars experience (XP) based on the official formula.
        /// Prestige cycles repeat every 100 levels.
        /// </summary>
        public static int GetLevelFromXp(long xp)
        {
            if (xp <= 0) return 1;

            int level = 100 * (int)(xp / 490000);
            long remainingXp = xp % 490000;
            
            int[] firstFourLevelsXp = { 1000, 2000, 3000, 4000 };
            for (int i = 0; i < 4; i++)
            {
                if (remainingXp < firstFourLevelsXp[i])
                {
                    return level + i + 1;
                }
                remainingXp -= firstFourLevelsXp[i];
            }
            
            return level + 5 + (int)(remainingXp / 5000);
        }
    }
}
