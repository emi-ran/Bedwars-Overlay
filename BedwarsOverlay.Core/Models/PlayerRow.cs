using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using BedwarsOverlay.Core.Stats;

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

        /// <summary>
        /// WPF brushes are thread-affine until frozen. Rows are created from background work
        /// (log parsing, API lookups, tests), so every brush handed out by this model is frozen:
        /// a frozen <see cref="Freezable"/> can be read from any thread, an unfrozen one throws
        /// "The calling thread cannot access this object because a different thread owns it".
        /// </summary>
        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush Frozen(byte red, byte green, byte blue)
        {
            return Frozen(0xFF, red, green, blue);
        }

        private static SolidColorBrush Frozen(byte alpha, byte red, byte green, byte blue)
        {
            return Frozen(Color.FromArgb(alpha, red, green, blue));
        }

        private static readonly Brush GrayBrush = Frozen(0x94, 0xA3, 0xB8);
        private static readonly Brush WhiteBrush = Brushes.White;
        private static readonly Brush GreenBrush = Frozen(0x22, 0xC5, 0x5E);
        private static readonly Brush GoldBrush = Frozen(0xFB, 0xBF, 0x24);
        private static readonly Brush RedBrush = Frozen(0xEF, 0x44, 0x44);
        private static readonly Brush OrangeBrush = Frozen(0xFB, 0x92, 0x3C);
        private static readonly Brush BlueBrush = Frozen(0x55, 0xAA, 0xFF);
        private static readonly Brush AquaBrush = Frozen(0x55, 0xFF, 0xFF);
        private static readonly Brush PinkBrush = Frozen(0xFF, 0x55, 0xFF);

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

        private string _team = string.Empty;
        public string Team
        {
            get => _team;
            set
            {
                if (SetProperty(ref _team, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(TeamColor));
                    RefreshNameColor();
                }
            }
        }

        public Brush TeamColor => Team switch
        {
            "Blue" => BlueBrush,
            "Green" => GreenBrush,
            "Red" => RedBrush,
            "Yellow" => GoldBrush,
            "Aqua" => AquaBrush,
            "White" => Brushes.White,
            "Pink" => PinkBrush,
            "Gray" => GrayBrush,
            _ => GrayBrush
        };

        private string _statusText = "OK";
        private bool _isDisconnected;
        public bool IsDisconnected
        {
            get => _isDisconnected;
            set
            {
                if (SetProperty(ref _isDisconnected, value))
                    OnPropertyChanged(nameof(DisconnectedVisibility));
            }
        }

        public Visibility DisconnectedVisibility => IsDisconnected ? Visibility.Visible : Visibility.Collapsed;

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

        private string _threat = "-";
        public string Threat
        {
            get => _threat;
            set => SetProperty(ref _threat, value);
        }

        /// <summary>Signals behind the threat score, shown as a per-cell tooltip.</summary>
        private string _threatDetail = string.Empty;
        public string ThreatDetail
        {
            get => _threatDetail;
            set => SetProperty(ref _threatDetail, value);
        }

        private string _repeatBadgeText = string.Empty;
        public string RepeatBadgeText
        {
            get => _repeatBadgeText;
            set => SetProperty(ref _repeatBadgeText, value);
        }

        private string _repeatTooltip = string.Empty;
        public string RepeatTooltip
        {
            get => _repeatTooltip;
            set => SetProperty(ref _repeatTooltip, value);
        }

        private Visibility _repeatBadgeVisibility = Visibility.Collapsed;
        public Visibility RepeatBadgeVisibility
        {
            get => _repeatBadgeVisibility;
            set => SetProperty(ref _repeatBadgeVisibility, value);
        }

        private Brush _repeatBadgeColor = GoldBrush;
        public Brush RepeatBadgeColor
        {
            get => _repeatBadgeColor;
            set => SetProperty(ref _repeatBadgeColor, value);
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

        private double _threatRaw = 0.0;
        public double ThreatRaw
        {
            get => _threatRaw;
            set => SetProperty(ref _threatRaw, value);
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

        private Brush _rankColor = Brushes.White;
        /// <summary>
        /// Name color implied by the player's rank (MVP+ cyan, VIP green, default gray). Used as the
        /// name color whenever the team is unknown.
        /// </summary>
        public Brush RankColor
        {
            get => _rankColor;
            set
            {
                if (SetProperty(ref _rankColor, value))
                {
                    RefreshNameColor();
                }
            }
        }

        /// <summary>
        /// A known team wins over the rank color, so names follow their team like they do in game.
        /// </summary>
        private void RefreshNameColor()
        {
            if (IsEliminated)
            {
                return;
            }

            NameColor = string.IsNullOrEmpty(Team) ? _rankColor : TeamColor;
        }

        private Brush _levelColor = Brushes.White;
        public Brush LevelColor
        {
            get => _levelColor;
            set => SetProperty(ref _levelColor, value);
        }

        public Brush WinstreakColor => Winstreak == "?" ? RedBrush : GrayBrush;

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

        private Brush _finalsColor = GrayBrush;
        public Brush FinalsColor
        {
            get => _finalsColor;
            set => SetProperty(ref _finalsColor, value);
        }

        private Brush _winsColor = GrayBrush;
        public Brush WinsColor
        {
            get => _winsColor;
            set => SetProperty(ref _winsColor, value);
        }

        private Brush _threatColor = GrayBrush;
        public Brush ThreatColor
        {
            get => _threatColor;
            set => SetProperty(ref _threatColor, value);
        }

        private Brush _riskColor = GrayBrush;
        public Brush RiskColor
        {
            get => _riskColor;
            set => SetProperty(ref _riskColor, value);
        }

        private Brush _rowBackground = Frozen(Color.FromRgb(0x0F, 0x0F, 0x11));
        public Brush RowBackground
        {
            get => _rowBackground;
            set => SetProperty(ref _rowBackground, value);
        }

        private bool _isEliminated;
        public bool IsEliminated
        {
            get => _isEliminated;
            set
            {
                if (SetProperty(ref _isEliminated, value))
                {
                    OnPropertyChanged(nameof(EliminationTagVisibility));
                }
            }
        }

        public Visibility EliminationTagVisibility => IsEliminated ? Visibility.Visible : Visibility.Collapsed;

        public void MarkEliminated()
        {
            IsEliminated = true;
            RowBackground = Frozen(Color.FromArgb(0xAA, 0x7F, 0x1D, 0x1D));
            NameColor = Frozen(Color.FromRgb(0xFE, 0xCA, 0xCA));
            Tag = "FINAL KILL";
            TagVisibility = Visibility.Visible;
            RiskColor = Frozen(Color.FromRgb(0xFE, 0xF2, 0xF2));
        }

        /// <summary>
        /// Gets the prestige color for a specific Bedwars level.
        /// </summary>
        public static Brush GetLevelColor(int level)
        {
            if (level < 100) return Frozen(Color.FromRgb(0x94, 0xA3, 0xB8)); // gray
            if (level < 200) return Brushes.White; // white/iron
            if (level < 300) return Frozen(Color.FromRgb(0xFB, 0xBF, 0x24)); // gold
            if (level < 400) return Frozen(Color.FromRgb(0x22, 0xD3, 0xEE)); // aqua/cyan
            if (level < 500) return Frozen(Color.FromRgb(0x34, 0xD3, 0x99)); // green
            if (level < 600) return Frozen(Color.FromRgb(0x0D, 0x94, 0x88)); // dark aqua
            if (level < 700) return Frozen(Color.FromRgb(0xF4, 0x72, 0xB6)); // pink
            if (level < 800) return Frozen(Color.FromRgb(0xA7, 0x8B, 0xFA)); // purple
            if (level < 900) return Frozen(Color.FromRgb(0x81, 0x8C, 0xF8)); // indigo
            return Frozen(Color.FromRgb(0xFB, 0x71, 0x85)); // rose/rainbow
        }

        /// <summary>Color used for a threat band. Single source of truth for the threat column.</summary>
        public static Brush GetThreatColor(ThreatLevel level)
        {
            return level switch
            {
                ThreatLevel.Extreme => RedBrush,
                ThreatLevel.High => OrangeBrush,
                ThreatLevel.Medium => GoldBrush,
                _ => GrayBrush
            };
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
                Threat = "-",
                NameColor = Brushes.Gray,
                RowBackground = isEvenRow ?
                    Frozen(Color.FromArgb(0x35, 0x0E, 0x0E, 0x10)) :
                    Frozen(Color.FromArgb(0x35, 0x16, 0x16, 0x18)),
                LevelRaw = 0,
                WinstreakRaw = 0,
                FkdrRaw = 0.0,
                WlrRaw = 0.0,
                FinalKillsRaw = 0,
                WinsRaw = 0,
                ThreatRaw = 0.0
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
            ThreatResult threat,
            bool isLocalPlayer,
            Brush nameBrush,
            bool isEvenRow,
            string rankTag = "")
        {
            // Row background logic
            Brush currentBg;
            if (isLocalPlayer)
            {
                currentBg = Frozen(Color.FromArgb(0x55, 0x0C, 0x1B, 0x33)); // Transparent dark blue highlight
            }
            else
            {
                currentBg = isEvenRow ?
                    Frozen(Color.FromArgb(0x35, 0x0E, 0x0E, 0x10)) :
                    Frozen(Color.FromArgb(0x35, 0x16, 0x16, 0x18));
            }

            var row = new PlayerRow
            {
                Name = name,
                Level = level.ToString(),
                Winstreak = hasWinstreak ? winstreak.ToString() : (isLocalPlayer ? "?" : "0"),
                Fkdr = fkdr.ToString("N2"),
                Wlr = wlr.ToString("N2"),
                FinalKills = finals.ToString("N0"),
                Wins = wins.ToString("N0"),
                IsLocalPlayer = isLocalPlayer,
                RankColor = nameBrush,
                LevelColor = GetLevelColor(level),
                FkdrColor = fkdr >= 5.0 ? GreenBrush : (fkdr >= 1.0 ? WhiteBrush : GrayBrush),
                WlrColor = wlr >= 1.0 ? GreenBrush : (wlr >= 0.3 ? WhiteBrush : GrayBrush),
                FinalsColor = finals >= 5000 ? GoldBrush : (finals >= 1000 ? GreenBrush : GrayBrush),
                WinsColor = wins >= 1000 ? GoldBrush : (wins >= 300 ? GreenBrush : (wins >= 150 ? WhiteBrush : GrayBrush)),
                RowBackground = currentBg,
                LevelRaw = level,
                WinstreakRaw = hasWinstreak ? winstreak : (isLocalPlayer ? -1 : 0),
                FkdrRaw = fkdr,
                WlrRaw = wlr,
                FinalKillsRaw = finals,
                WinsRaw = wins,
                RankTag = rankTag
            };

            row.ApplyThreat(threat);
            return row;
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
            ThreatResult threat,
            bool isLocalPlayer,
            Brush nameBrush,
            string rankTag = "")
        {
            if (IsEliminated)
            {
                return;
            }

            // Row background logic
            Brush currentBg;
            if (isLocalPlayer)
            {
                currentBg = Frozen(Color.FromArgb(0x55, 0x0C, 0x1B, 0x33)); // Transparent dark blue highlight
            }
            else
            {
                currentBg = RowBackground;
            }

            Level = level.ToString();
            Winstreak = hasWinstreak ? winstreak.ToString() : (isLocalPlayer ? "?" : "0");
            Fkdr = fkdr.ToString("N2");
            Wlr = wlr.ToString("N2");
            FinalKills = finals.ToString("N0");
            Wins = wins.ToString("N0");
            IsLocalPlayer = isLocalPlayer;
            RankColor = nameBrush;
            LevelColor = GetLevelColor(level);
            FkdrColor = fkdr >= 5.0 ? GreenBrush : (fkdr >= 1.0 ? WhiteBrush : GrayBrush);
            WlrColor = wlr >= 1.0 ? GreenBrush : (wlr >= 0.3 ? WhiteBrush : GrayBrush);
            FinalsColor = finals >= 5000 ? GoldBrush : (finals >= 1000 ? GreenBrush : GrayBrush);
            WinsColor = wins >= 1000 ? GoldBrush : (wins >= 300 ? GreenBrush : (wins >= 150 ? WhiteBrush : GrayBrush));
            RowBackground = currentBg;
            LevelRaw = level;
            WinstreakRaw = hasWinstreak ? winstreak : (isLocalPlayer ? -1 : 0);
            FkdrRaw = fkdr;
            WlrRaw = wlr;
            FinalKillsRaw = finals;
            WinsRaw = wins;
            RankTag = rankTag;

            ApplyThreat(threat);
        }

        /// <summary>
        /// Applies a threat score. The band drives the column color, the risk label and the warning
        /// badge, so all of those can no longer disagree with each other.
        /// </summary>
        public void ApplyThreat(ThreatResult threat)
        {
            threat ??= ThreatResult.Empty;

            Threat = threat.Display;
            ThreatRaw = threat.Score;
            ThreatColor = GetThreatColor(threat.Level);
            RiskScore = threat.Level.ToString().ToUpperInvariant();
            RiskColor = GetThreatColor(threat.Level);
            ThreatDetail = threat.Signals.Count > 0 ? string.Join(" · ", threat.Signals) : string.Empty;

            bool warn = threat.Level == ThreatLevel.Extreme;
            Tag = warn ? "WARNING" : string.Empty;
            TagVisibility = warn ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Shows how often this player was seen in earlier matches. <paramref name="count"/> is the
        /// number of previous matches the player appeared in.
        /// </summary>
        public void ApplyEncounterInfo(int count, int minCount, string tooltip, string badgeFormat = "×{0}")
        {
            if (count < minCount || count <= 0)
            {
                RepeatBadgeText = string.Empty;
                RepeatTooltip = string.Empty;
                RepeatBadgeVisibility = Visibility.Collapsed;
                return;
            }

            RepeatBadgeText = string.Format(CultureInfo.InvariantCulture, badgeFormat, count);
            RepeatTooltip = tooltip ?? string.Empty;
            RepeatBadgeColor = count >= 3 ? RedBrush : GoldBrush;
            RepeatBadgeVisibility = Visibility.Visible;
        }
    }
}
