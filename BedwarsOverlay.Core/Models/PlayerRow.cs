using System;
using System.Windows;
using System.Windows.Media;

namespace BedwarsOverlay.Core.Models
{
    public enum PlayerStatus
    {
        OK,
        NICKED_OR_UNKNOWN,
        NO_HYPIXEL_DATA
    }

    public class PlayerRow
    {
        public string Name { get; set; } = string.Empty;
        public string StatusText { get; set; } = "OK";
        
        public string Level { get; set; } = "-";
        public string Winstreak { get; set; } = "-";
        public string Fkdr { get; set; } = "-";
        public string Wlr { get; set; } = "-";
        public string FinalKills { get; set; } = "-";
        public string Wins { get; set; } = "-";
        public string Sniper { get; set; } = "0%";
        
        public string RiskScore { get; set; } = "LOW";
        public string Tag { get; set; } = "";
        public Visibility TagVisibility { get; set; } = Visibility.Collapsed;

        // Special visual features for Cubelify design matching
        public bool IsLocalPlayer { get; set; } = false;
        public Visibility LocalPlayerIconsVisibility => IsLocalPlayer ? Visibility.Visible : Visibility.Collapsed;

        // UI styling brushes for direct data-driven row rendering
        public Brush NameColor { get; set; } = Brushes.White;
        public Brush LevelColor { get; set; } = Brushes.White;
        public Brush WinstreakColor => Winstreak == "?" ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush FkdrColor { get; set; } = Brushes.White;
        public Brush WlrColor { get; set; } = Brushes.White;
        public Brush FinalsColor { get; set; } = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // slate-400 grey
        public Brush WinsColor { get; set; } = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush SniperColor { get; set; } = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush RiskColor { get; set; } = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush RowBackground { get; set; } = new SolidColorBrush(Color.FromRgb(0x0F, 0x0F, 0x11));

        /// <summary>
        /// Gets the prestige color for a specific Bedwars level.
        /// </summary>
        public static Brush GetLevelColor(int level)
        {
            if (level < 100) return new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // gray
            if (level < 200) return Brushes.White; // white/iron
            if (level < 300) return new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)); // gold
            if (level < 400) return new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE)); // aqua/cyan
            if (level < 500) return new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)); // green
            if (level < 600) return new SolidColorBrush(Color.FromRgb(0x0D, 0x94, 0x88)); // dark aqua
            if (level < 700) return new SolidColorBrush(Color.FromRgb(0xF4, 0x72, 0xB6)); // pink
            if (level < 800) return new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)); // purple
            if (level < 900) return new SolidColorBrush(Color.FromRgb(0x81, 0x8C, 0xF8)); // indigo
            return new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85)); // rose/rainbow
        }

        /// <summary>
        /// Creates a styled PlayerRow for demonstration and testing.
        /// </summary>
        public static PlayerRow CreateMock(string name, int level, double fkdr, double wlr, int finals, int wins, string sniper, bool isLocalPlayer, Brush nameBrush, bool isEvenRow)
        {
            var greenBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)); // bright green
            var goldBrush = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));  // warm gold
            var redBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));   // soft red
            var whiteBrush = Brushes.White;
            var grayBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

            // Parse sniper percentage to determine its color
            Brush currentSniperColor = grayBrush;
            if (double.TryParse(sniper.Replace("%", "", StringComparison.OrdinalIgnoreCase), out double sniperPct) && sniperPct >= 10.0)
            {
                currentSniperColor = redBrush;
            }

            // Row background logic
            Brush currentBg;
            if (isLocalPlayer)
            {
                currentBg = new SolidColorBrush(Color.FromArgb(0x55, 0x0C, 0x1B, 0x33)); // Transparent dark blue highlight
            }
            else
            {
                currentBg = isEvenRow ? 
                    new SolidColorBrush(Color.FromArgb(0x35, 0x0E, 0x0E, 0x10)) : 
                    new SolidColorBrush(Color.FromArgb(0x35, 0x16, 0x16, 0x18));
            }

            // Threat evaluation
            string risk = "LOW";
            if (fkdr >= 5.0 || sniperPct >= 10.0) risk = "EXTREME";
            else if (fkdr >= 2.0) risk = "HIGH";

            return new PlayerRow
            {
                Name = name,
                Level = level.ToString(),
                Winstreak = name == "Metriusz" ? "?" : "0",
                Fkdr = fkdr.ToString("N2"),
                Wlr = wlr.ToString("N2"),
                FinalKills = finals.ToString("N0"),
                Wins = wins.ToString("N0"),
                Sniper = sniper,
                RiskScore = risk,
                IsLocalPlayer = isLocalPlayer,
                Tag = (risk == "EXTREME" || risk == "HIGH") ? "WARNING" : "",
                TagVisibility = (risk == "EXTREME" || risk == "HIGH") ? Visibility.Visible : Visibility.Collapsed,
                NameColor = nameBrush,
                LevelColor = GetLevelColor(level),
                FkdrColor = fkdr >= 5.0 ? greenBrush : (fkdr >= 1.0 ? whiteBrush : grayBrush),
                WlrColor = wlr >= 1.0 ? greenBrush : (wlr >= 0.3 ? whiteBrush : grayBrush),
                FinalsColor = finals >= 5000 ? goldBrush : (finals >= 1000 ? greenBrush : grayBrush),
                WinsColor = wins >= 1000 ? goldBrush : (wins >= 300 ? greenBrush : (wins >= 150 ? whiteBrush : grayBrush)),
                SniperColor = currentSniperColor,
                RiskColor = risk == "EXTREME" ? redBrush : (risk == "HIGH" ? new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C)) : grayBrush),
                RowBackground = currentBg
            };
        }
    }
}
