using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public class PlayerRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _rankTag = string.Empty;
        public string RankTag
        {
            get => _rankTag;
            set
            {
                if (SetProperty(ref _rankTag, value))
                {
                    OnPropertyChanged(nameof(RankTagVisibility));
                }
            }
        }

        public Visibility RankTagVisibility => string.IsNullOrEmpty(RankTag) ? Visibility.Collapsed : Visibility.Visible;

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _statusText = "OK";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _level = "-";
        public string Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        private string _winstreak = "-";
        public string Winstreak
        {
            get => _winstreak;
            set
            {
                if (SetProperty(ref _winstreak, value))
                {
                    OnPropertyChanged(nameof(WinstreakColor));
                }
            }
        }

        private string _fkdr = "-";
        public string Fkdr
        {
            get => _fkdr;
            set => SetProperty(ref _fkdr, value);
        }

        private string _wlr = "-";
        public string Wlr
        {
            get => _wlr;
            set => SetProperty(ref _wlr, value);
        }

        private string _finalKills = "-";
        public string FinalKills
        {
            get => _finalKills;
            set => SetProperty(ref _finalKills, value);
        }

        private string _wins = "-";
        public string Wins
        {
            get => _wins;
            set => SetProperty(ref _wins, value);
        }

        private string _sniper = "0%";
        public string Sniper
        {
            get => _sniper;
            set => SetProperty(ref _sniper, value);
        }

        private string _riskScore = "LOW";
        public string RiskScore
        {
            get => _riskScore;
            set => SetProperty(ref _riskScore, value);
        }

        private string _tag = "";
        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        private Visibility _tagVisibility = Visibility.Collapsed;
        public Visibility TagVisibility
        {
            get => _tagVisibility;
            set => SetProperty(ref _tagVisibility, value);
        }

        private int _levelRaw = 0;
        public int LevelRaw
        {
            get => _levelRaw;
            set => SetProperty(ref _levelRaw, value);
        }

        private int _winstreakRaw = 0;
        public int WinstreakRaw
        {
            get => _winstreakRaw;
            set => SetProperty(ref _winstreakRaw, value);
        }

        private double _fkdrRaw = 0.0;
        public double FkdrRaw
        {
            get => _fkdrRaw;
            set => SetProperty(ref _fkdrRaw, value);
        }

        private double _wlrRaw = 0.0;
        public double WlrRaw
        {
            get => _wlrRaw;
            set => SetProperty(ref _wlrRaw, value);
        }

        private int _finalKillsRaw = 0;
        public int FinalKillsRaw
        {
            get => _finalKillsRaw;
            set => SetProperty(ref _finalKillsRaw, value);
        }

        private int _winsRaw = 0;
        public int WinsRaw
        {
            get => _winsRaw;
            set => SetProperty(ref _winsRaw, value);
        }

        private double _sniperRaw = 0.0;
        public double SniperRaw
        {
            get => _sniperRaw;
            set => SetProperty(ref _sniperRaw, value);
        }

        private bool _isLocalPlayer = false;
        public bool IsLocalPlayer
        {
            get => _isLocalPlayer;
            set
            {
                if (SetProperty(ref _isLocalPlayer, value))
                {
                    OnPropertyChanged(nameof(LocalPlayerIconsVisibility));
                }
            }
        }

        public Visibility LocalPlayerIconsVisibility => IsLocalPlayer ? Visibility.Visible : Visibility.Collapsed;

        private Brush _nameColor = Brushes.White;
        public Brush NameColor
        {
            get => _nameColor;
            set => SetProperty(ref _nameColor, value);
        }

        private Brush _levelColor = Brushes.White;
        public Brush LevelColor
        {
            get => _levelColor;
            set => SetProperty(ref _levelColor, value);
        }

        public Brush WinstreakColor => Winstreak == "?" ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

        private Brush _fkdrColor = Brushes.White;
        public Brush FkdrColor
        {
            get => _fkdrColor;
            set => SetProperty(ref _fkdrColor, value);
        }

        private Brush _wlrColor = Brushes.White;
        public Brush WlrColor
        {
            get => _wlrColor;
            set => SetProperty(ref _wlrColor, value);
        }

        private Brush _finalsColor = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush FinalsColor
        {
            get => _finalsColor;
            set => SetProperty(ref _finalsColor, value);
        }

        private Brush _winsColor = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush WinsColor
        {
            get => _winsColor;
            set => SetProperty(ref _winsColor, value);
        }

        private Brush _sniperColor = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush SniperColor
        {
            get => _sniperColor;
            set => SetProperty(ref _sniperColor, value);
        }

        private Brush _riskColor = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        public Brush RiskColor
        {
            get => _riskColor;
            set => SetProperty(ref _riskColor, value);
        }

        private Brush _rowBackground = new SolidColorBrush(Color.FromRgb(0x0F, 0x0F, 0x11));
        public Brush RowBackground
        {
            get => _rowBackground;
            set => SetProperty(ref _rowBackground, value);
        }

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
        /// Creates a placeholder PlayerRow showing loading status.
        /// </summary>
        public static PlayerRow CreateLoading(string name, bool isEvenRow)
        {
            return new PlayerRow
            {
                Name = name,
                Level = "-",
                Winstreak = "-",
                Fkdr = "-",
                Wlr = "-",
                FinalKills = "-",
                Wins = "-",
                Sniper = "-",
                NameColor = Brushes.Gray,
                RowBackground = isEvenRow ? 
                    new SolidColorBrush(Color.FromArgb(0x35, 0x0E, 0x0E, 0x10)) : 
                    new SolidColorBrush(Color.FromArgb(0x35, 0x16, 0x16, 0x18)),
                LevelRaw = 0,
                WinstreakRaw = 0,
                FkdrRaw = 0.0,
                WlrRaw = 0.0,
                FinalKillsRaw = 0,
                WinsRaw = 0,
                SniperRaw = 0.0
            };
        }

        /// <summary>
        /// Creates a styled PlayerRow from stats data.
        /// </summary>
        public static PlayerRow Create(
            string name, 
            int level, 
            bool hasWinstreak, 
            int winstreak, 
            double fkdr, 
            double wlr, 
            int finals, 
            int wins, 
            string sniper, 
            bool isLocalPlayer, 
            Brush nameBrush, 
            bool isEvenRow,
            string rankTag = "")
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
            if (fkdr >= 5.0) risk = "EXTREME";
            else if (fkdr >= 2.0) risk = "HIGH";

            return new PlayerRow
            {
                Name = name,
                Level = level.ToString(),
                Winstreak = hasWinstreak ? winstreak.ToString() : (isLocalPlayer ? "?" : "0"),
                Fkdr = fkdr.ToString("N2"),
                Wlr = wlr.ToString("N2"),
                FinalKills = finals.ToString("N0"),
                Wins = wins.ToString("N0"),
                Sniper = sniper,
                RiskScore = risk,
                IsLocalPlayer = isLocalPlayer,
                Tag = (risk == "EXTREME") ? "WARNING" : "",
                TagVisibility = (risk == "EXTREME") ? Visibility.Visible : Visibility.Collapsed,
                NameColor = nameBrush,
                LevelColor = GetLevelColor(level),
                FkdrColor = fkdr >= 5.0 ? greenBrush : (fkdr >= 1.0 ? whiteBrush : grayBrush),
                WlrColor = wlr >= 1.0 ? greenBrush : (wlr >= 0.3 ? whiteBrush : grayBrush),
                FinalsColor = finals >= 5000 ? goldBrush : (finals >= 1000 ? greenBrush : grayBrush),
                WinsColor = wins >= 1000 ? goldBrush : (wins >= 300 ? greenBrush : (wins >= 150 ? whiteBrush : grayBrush)),
                SniperColor = currentSniperColor,
                RiskColor = risk == "EXTREME" ? redBrush : (risk == "HIGH" ? new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C)) : grayBrush),
                RowBackground = currentBg,
                LevelRaw = level,
                WinstreakRaw = hasWinstreak ? winstreak : (isLocalPlayer ? -1 : 0),
                FkdrRaw = fkdr,
                WlrRaw = wlr,
                FinalKillsRaw = finals,
                WinsRaw = wins,
                SniperRaw = sniperPct,
                RankTag = rankTag
            };
        }

        /// <summary>
        /// Updates the properties of this instance using stats data.
        /// </summary>
        public void Update(
            int level, 
            bool hasWinstreak, 
            int winstreak, 
            double fkdr, 
            double wlr, 
            int finals, 
            int wins, 
            string sniper, 
            bool isLocalPlayer, 
            Brush nameBrush,
            string rankTag = "")
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
                currentBg = RowBackground;
            }

            // Threat evaluation
            string risk = "LOW";
            if (fkdr >= 5.0) risk = "EXTREME";
            else if (fkdr >= 2.0) risk = "HIGH";

            Level = level.ToString();
            Winstreak = hasWinstreak ? winstreak.ToString() : (isLocalPlayer ? "?" : "0");
            Fkdr = fkdr.ToString("N2");
            Wlr = wlr.ToString("N2");
            FinalKills = finals.ToString("N0");
            Wins = wins.ToString("N0");
            Sniper = sniper;
            RiskScore = risk;
            IsLocalPlayer = isLocalPlayer;
            Tag = (risk == "EXTREME") ? "WARNING" : "";
            TagVisibility = (risk == "EXTREME") ? Visibility.Visible : Visibility.Collapsed;
            NameColor = nameBrush;
            LevelColor = GetLevelColor(level);
            FkdrColor = fkdr >= 5.0 ? greenBrush : (fkdr >= 1.0 ? whiteBrush : grayBrush);
            WlrColor = wlr >= 1.0 ? greenBrush : (wlr >= 0.3 ? whiteBrush : grayBrush);
            FinalsColor = finals >= 5000 ? goldBrush : (finals >= 1000 ? greenBrush : grayBrush);
            WinsColor = wins >= 1000 ? goldBrush : (wins >= 300 ? greenBrush : (wins >= 150 ? whiteBrush : grayBrush));
            SniperColor = currentSniperColor;
            RiskColor = risk == "EXTREME" ? redBrush : (risk == "HIGH" ? new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C)) : grayBrush);
            RowBackground = currentBg;
            LevelRaw = level;
            WinstreakRaw = hasWinstreak ? winstreak : (isLocalPlayer ? -1 : 0);
            FkdrRaw = fkdr;
            WlrRaw = wlr;
            FinalKillsRaw = finals;
            WinsRaw = wins;
            SniperRaw = sniperPct;
            RankTag = rankTag;
        }
    }
}
