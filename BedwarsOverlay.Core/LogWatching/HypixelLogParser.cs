using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BedwarsOverlay.Core.LogWatching
{
    /// <summary>Kind of Bed Wars chat block detected in the client log.</summary>
    public enum GameBlockKind
    {
        None,
        MatchStart,
        MatchEnd
    }

    /// <summary>Outcome of a finished Bed Wars match.</summary>
    public enum GameOutcome
    {
        Unknown,
        Win,
        Loss
    }

    /// <summary>Result of <see cref="HypixelLogParser.ParseGameBlock"/>.</summary>
    public sealed record GameBlockEvent(GameBlockKind Kind, GameOutcome Outcome)
    {
        public static readonly GameBlockEvent None = new(GameBlockKind.None, GameOutcome.Unknown);
        public static readonly GameBlockEvent MatchStart = new(GameBlockKind.MatchStart, GameOutcome.Unknown);
    }

    public sealed record PlayerDeathEvent(string Victim, string? Killer, bool IsFinal,
        string? VictimTeam, string? KillerTeam);

    public sealed record PlayerConnectionEvent(string Name, bool IsConnected);

    public class HypixelLogParser
    {
        // Default regex patterns to match the /who output in client logs.
        // The group "names" should capture the comma-separated or space-separated list of usernames.
        private static readonly List<Regex> DefaultPatterns = new()
        {
            // Standard client chat format: [CHAT] ONLINE: player1, player2, player3...
            new Regex(@"\[CHAT\]\s+ONLINE:\s*(?<names>[a-zA-Z0-9_, ]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // Snapping/lobby players layout: [CHAT] Online players (12): player1, player2...
            new Regex(@"\[CHAT\]\s+Online\s+players\s*\(\d+\):\s*(?<names>[a-zA-Z0-9_, ]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // Raw console/no-chat tag format: ONLINE: player1, player2...
            new Regex(@"ONLINE:\s*(?<names>[a-zA-Z0-9_, ]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        private static readonly Regex FinalKillPattern = new(
            @"(?:^|\[CHAT\]\s+)(?<victim>[a-zA-Z0-9_]{3,16})\b.*\bFINAL\s+KILL!",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ChatMessagePattern = new(
            @"\[CHAT\]\s*(?<message>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Anchored server messages only: player chat ("name: ...") must never change the roster.
        // Cosmetic kill messages vary, but their actor positions and punctuation are stable.
        private static readonly Regex DeathMessagePattern = new(
            @"^(?<victim>[a-zA-Z0-9_]{3,16}) (?:" +
            @"was [^:.!>]+? (?:by|from|meet|against|of|for) (?<killer>[a-zA-Z0-9_]{3,16})(?:'s [^:.!>]+)?|" +
            @"was (?<killer>[a-zA-Z0-9_]{3,16})'s final #[\d,]+|" +
            @"died in close combat to (?<killer>[a-zA-Z0-9_]{3,16})|" +
            @"(?:didn't distance themselves properly from|tripped while trying to run away from|be cannonballed to death by|slipped into void for|howled into the void for) (?<killer>[a-zA-Z0-9_]{3,16})|" +
            @"fell into the void|fell from a high place|died" +
            @")\.(?<final> FINAL KILL!)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LobbyTransitionPattern = new(
            @"^(?:Sending you to [a-zA-Z0-9_-]+!|[a-zA-Z0-9_]{3,16} has joined \(\d+/\d+\)!)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ConnectionMessagePattern = new(
            @"^(?<name>[a-zA-Z0-9_]{3,16}) (?<state>disconnected|reconnected)\.$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GameEndWinRewardPattern = new(
            @"^\+\d+\s+(?:tokens!|Slumber\s+Tickets)\s+\(Win\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Only the Slumber Tickets variant marks the end of a game. The per-minute "+12 tokens!
        // (Time Played)" and "+17 Bed Wars XP (Time Played)" lines are printed while the game is
        // still running and must never end a match.
        private static readonly Regex GameEndTimePlayedPattern = new(
            @"^\+\d+\s+Slumber\s+Tickets\s+\(Time\s+Played\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BedWarsHeaderPattern = new(
            @"^Bed\s+Wars$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GameBlockIntroPattern = new(
            @"Protect your bed",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Both the game start and the game end block are wrapped by a separator line. Real clients
        // write the separator glyph as literal '?' (the client log charset cannot encode it), but we
        // also accept other symbol-only separators so a properly encoded log keeps working.
        private static readonly Regex SeparatorPattern = new(
            @"^[^a-zA-Z0-9\s]{20,}$",
            RegexOptions.Compiled);

        private static readonly Regex PartyLinePattern = new(
            @"\[CHAT\]\s+Party\s+(?:Leader|Members):\s*(?<names>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PartyHeaderPattern = new(
            @"\[CHAT\]\s+Party\s+Members\s+\(\d+\)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RankPattern = new(
            @"\[[^\]]+\]",
            RegexOptions.Compiled);

        private static readonly Regex IgnoreCommandPattern = new(
            @"\('bwignore\s+(?<names>[a-zA-Z0-9_, ]+)'\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StatsCommandPattern = new(
            @"\('bwstats\s+(?<names>[a-zA-Z0-9_, ]+)'\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HelpCommandPattern = new(
            @"\('bwhelp'\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ClearCommandPattern = new(
            @"\('bwclear'\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly List<Regex> _patterns;

        // Bed Wars block state. Counting is done in chat lines only, because long Hypixel messages
        // wrap onto continuation lines without a [CHAT] prefix and those must not reset the block.
        private const int HeaderWindowChatLines = 3;
        private const int BlockChatLineBudget = 40;

        private int _headerWindow;
        private int _blockLinesRemaining;
        private bool _endReported;
        private GameOutcome _lastEndOutcome = GameOutcome.Unknown;

        public HypixelLogParser(IEnumerable<string>? customPatterns = null)
        {
            _patterns = new List<Regex>();
            
            if (customPatterns != null)
            {
                foreach (var pattern in customPatterns)
                {
                    try
                    {
                        _patterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                    }
                    catch
                    {
                        // Skip invalid regex configurations
                    }
                }
            }

            // Always register the default fallback patterns
            _patterns.AddRange(DefaultPatterns);
        }

        /// <summary>
        /// Parses a log line. If it matches a player list print, returns the list of extracted usernames.
        /// </summary>
        public List<string>? ParsePlayerList(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;

            // Strip Minecraft formatting/color codes (e.g., §a, §r)
            string cleanLine = Regex.Replace(logLine, "§.", "");

            foreach (var regex in _patterns)
            {
                var match = regex.Match(cleanLine);
                if (match.Success)
                {
                    var namesGroup = match.Groups["names"];
                    if (namesGroup.Success && !string.IsNullOrWhiteSpace(namesGroup.Value))
                    {
                        var players = new List<string>();
                        var rawNames = namesGroup.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var name in rawNames)
                        {
                            var trimmed = name.Trim();
                            // Minecraft usernames are 3-16 chars, alphanumeric and underscore
                            if (trimmed.Length >= 3 && trimmed.Length <= 16 && Regex.IsMatch(trimmed, "^[a-zA-Z0-9_]+$"))
                            {
                                players.Add(trimmed);
                            }
                        }

                        if (players.Count > 0)
                        {
                            return players;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses Hypixel final kill messages and returns the eliminated player name.
        /// </summary>
        public string? ParseFinalKillVictim(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var match = FinalKillPattern.Match(cleanLine);
            if (!match.Success)
            {
                return null;
            }

            string victim = match.Groups["victim"].Value;
            return Regex.IsMatch(victim, "^[a-zA-Z0-9_]{3,16}$") ? victim : null;
        }

        public PlayerDeathEvent? ParsePlayerDeath(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;
            var chat = ChatMessagePattern.Match(logLine);
            if (!chat.Success) return null;

            string rawMessage = chat.Groups["message"].Value.Trim();
            string message = Regex.Replace(rawMessage, "§.", "");
            var match = DeathMessagePattern.Match(message);
            if (!match.Success) return null;

            string victim = match.Groups["victim"].Value;
            string? killer = match.Groups["killer"].Success ? match.Groups["killer"].Value : null;
            return new PlayerDeathEvent(victim, killer, match.Groups["final"].Success,
                ReadActorTeam(rawMessage, 0),
                killer == null ? null : ReadActorTeam(rawMessage, match.Groups["killer"].Index));
        }

        public bool IsLobbyTransition(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return false;
            var chat = ChatMessagePattern.Match(Regex.Replace(logLine, "§.", ""));
            return chat.Success && LobbyTransitionPattern.IsMatch(chat.Groups["message"].Value.Trim());
        }

        public PlayerConnectionEvent? ParsePlayerConnection(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;
            var chat = ChatMessagePattern.Match(Regex.Replace(logLine, "§.", ""));
            if (!chat.Success) return null;
            var match = ConnectionMessagePattern.Match(chat.Groups["message"].Value.Trim());
            return match.Success
                ? new PlayerConnectionEvent(match.Groups["name"].Value,
                    match.Groups["state"].Value.Equals("reconnected", StringComparison.OrdinalIgnoreCase))
                : null;
        }

        // Track the active Minecraft color at the actor's plain-text offset. Formatting such as
        // bold does not reset color. Missing/reset colors deliberately leave the team unknown.
        private static string? ReadActorTeam(string message, int actorOffset)
        {
            char color = '\0';
            int plainOffset = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '§' && i + 1 < message.Length)
                {
                    char code = char.ToLowerInvariant(message[++i]);
                    if ("0123456789abcdefr".Contains(code)) color = code;
                    continue;
                }
                if (plainOffset++ == actorOffset) break;
            }

            return color switch
            {
                'c' => "Red", '9' => "Blue", 'a' => "Green", 'e' => "Yellow",
                'b' => "Aqua", 'f' => "White", 'd' => "Pink", '7' => "Gray",
                _ => null
            };
        }

        /// <summary>
        /// Classifies Hypixel's Bed Wars start / end chat blocks.
        /// Verified against real client logs: both blocks are wrapped by a separator line followed by
        /// a "Bed Wars" header. The start block contains the "Protect your bed" intro, the end block
        /// contains a killer table and reward lines ("+N tokens! (Win)", "+N ... (Time Played)").
        /// A win block prints "(Time Played)" before the "(Win)" line, so the win line upgrades the
        /// previously reported loss outcome.
        /// </summary>
        public GameBlockEvent ParseGameBlock(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return GameBlockEvent.None;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var chatMatch = ChatMessagePattern.Match(cleanLine);
            if (!chatMatch.Success)
            {
                // Wrapped chat continuations and unrelated non-chat log lines must not reset the block.
                return GameBlockEvent.None;
            }

            string message = chatMatch.Groups["message"].Value.Trim();

            if (SeparatorPattern.IsMatch(message))
            {
                _headerWindow = HeaderWindowChatLines;
                return GameBlockEvent.None;
            }

            bool headerConsumed = false;
            if (_headerWindow > 0)
            {
                if (BedWarsHeaderPattern.IsMatch(message))
                {
                    _headerWindow = 0;
                    _blockLinesRemaining = BlockChatLineBudget;
                    _endReported = false;
                    _lastEndOutcome = GameOutcome.Unknown;
                    headerConsumed = true;
                }
                else
                {
                    _headerWindow--;
                }
            }

            if (headerConsumed || _blockLinesRemaining <= 0)
            {
                return GameBlockEvent.None;
            }

            _blockLinesRemaining--;

            if (GameBlockIntroPattern.IsMatch(message))
            {
                _blockLinesRemaining = 0;
                return GameBlockEvent.MatchStart;
            }

            if (GameEndWinRewardPattern.IsMatch(message))
            {
                bool isNewEnd = !_endReported;
                bool isUpgrade = _lastEndOutcome != GameOutcome.Win;
                _endReported = true;
                _lastEndOutcome = GameOutcome.Win;

                return (isNewEnd || isUpgrade)
                    ? new GameBlockEvent(GameBlockKind.MatchEnd, GameOutcome.Win)
                    : GameBlockEvent.None;
            }

            if (GameEndTimePlayedPattern.IsMatch(message) && !_endReported)
            {
                _endReported = true;
                _lastEndOutcome = GameOutcome.Loss;
                return new GameBlockEvent(GameBlockKind.MatchEnd, GameOutcome.Loss);
            }

            return GameBlockEvent.None;
        }

        /// <summary>
        /// Detects Hypixel's Bed Wars end-of-game summary block.
        /// Kept as a convenience wrapper over <see cref="ParseGameBlock"/>.
        /// </summary>
        public bool IsGameEndSummary(string logLine)
        {
            return ParseGameBlock(logLine).Kind == GameBlockKind.MatchEnd;
        }

        public List<string>? ParsePartyPlayers(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var match = PartyLinePattern.Match(cleanLine);
            if (!match.Success)
            {
                return null;
            }

            string namesText = RankPattern.Replace(match.Groups["names"].Value, " ");
            var players = new List<string>();
            foreach (Match nameMatch in Regex.Matches(namesText, @"\b[a-zA-Z0-9_]{3,16}\b"))
            {
                string name = nameMatch.Value;
                if (!players.Exists(player => player.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    players.Add(name);
                }
            }

            return players.Count > 0 ? players : null;
        }

        public List<string>? ParseIgnoredPlayers(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var match = IgnoreCommandPattern.Match(cleanLine);
            if (!match.Success)
            {
                return null;
            }

            var players = new List<string>();
            var rawNames = match.Groups["names"].Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawName in rawNames)
            {
                string name = rawName.Trim();
                if (Regex.IsMatch(name, "^[a-zA-Z0-9_]{3,16}$") && !players.Exists(player => player.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    players.Add(name);
                }
            }

            return players.Count > 0 ? players : null;
        }

        public List<string>? ParseStatsCommandPlayers(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return null;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var match = StatsCommandPattern.Match(cleanLine);
            if (!match.Success)
            {
                return null;
            }

            return ParseNames(match.Groups["names"].Value);
        }

        public bool IsHelpCommand(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return false;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            return HelpCommandPattern.IsMatch(cleanLine);
        }

        public bool IsClearCommand(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return false;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            return ClearCommandPattern.IsMatch(cleanLine);
        }

        private static List<string>? ParseNames(string names)
        {
            var players = new List<string>();
            var rawNames = names.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawName in rawNames)
            {
                string name = rawName.Trim();
                if (Regex.IsMatch(name, "^[a-zA-Z0-9_]{3,16}$") && !players.Exists(player => player.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    players.Add(name);
                }
            }

            return players.Count > 0 ? players : null;
        }

        public bool IsPartyListHeader(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return false;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            return PartyHeaderPattern.IsMatch(cleanLine);
        }
    }
}
