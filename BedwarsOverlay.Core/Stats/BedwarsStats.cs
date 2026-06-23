using System;

namespace BedwarsOverlay.Core.Stats
{
    public class BedwarsStats
    {
        public string UUID { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        
        public int Level { get; set; } = 1;
        public int Winstreak { get; set; } = 0;
        public bool HasWinstreak { get; set; } = false; // Hypixel API winstreak can be private/missing

        public int FinalKills { get; set; } = 0;
        public int FinalDeaths { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int BedsBroken { get; set; } = 0;
        public int BedsLost { get; set; } = 0;

        public string Rank { get; set; } = string.Empty;
        public string PackageRank { get; set; } = string.Empty;
        public string NewPackageRank { get; set; } = string.Empty;
        public string MonthlyPackageRank { get; set; } = string.Empty;

        // Computed stats helper properties
        public double Fkdr => FinalDeaths == 0 ? FinalKills : Math.Round((double)FinalKills / FinalDeaths, 2);
        public double Wlr => Losses == 0 ? Wins : Math.Round((double)Wins / Losses, 2);
        public double Kdr => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
        public double Bblr => BedsLost == 0 ? BedsBroken : Math.Round((double)BedsBroken / BedsLost, 2);

        /// <summary>
        /// Gets the rank color based on Hypixel rank status.
        /// </summary>
        public string GetRankColorName()
        {
            // MonthlyPackageRank "SUPERSTAR" is MVP++ (gold/orange color)
            if (MonthlyPackageRank == "SUPERSTAR") return "Gold";
            
            // ADMIN, MODERATOR, HELPER, YOUTUBER (various colors, but let's handle MVP+ cyan / VIP green / default grey)
            string package = !string.IsNullOrEmpty(NewPackageRank) ? NewPackageRank : PackageRank;
            if (package == "MVP_PLUS" || package == "MVP") return "Cyan";
            if (package == "VIP_PLUS" || package == "VIP") return "Green";
            
            if (Rank == "ADMIN" || Rank == "MODERATOR") return "Red";
            if (Rank == "YOUTUBER") return "Red";

            return "Gray"; // default unranked
        }

        /// <summary>
        /// Gets the formatted rank tag (e.g. "[MVP+]", "[VIP]").
        /// </summary>
        public string GetRankTag()
        {
            if (Rank == "ADMIN") return "[ADMIN]";
            if (Rank == "MODERATOR") return "[MOD]";
            if (Rank == "HELPER") return "[HELPER]";
            if (Rank == "YOUTUBER") return "[YOUTUBE]";

            if (MonthlyPackageRank == "SUPERSTAR") return "[MVP++]";

            string package = !string.IsNullOrEmpty(NewPackageRank) ? NewPackageRank : PackageRank;
            if (package == "MVP_PLUS") return "[MVP+]";
            if (package == "MVP") return "[MVP]";
            if (package == "VIP_PLUS") return "[VIP+]";
            if (package == "VIP") return "[VIP]";

            return string.Empty; // default unranked
        }
    }
}
