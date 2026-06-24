using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BedwarsOverlay.Core.LogWatching
{
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

        private static readonly Regex GameEndWinRewardPattern = new(
            @"^\+\d+\s+(?:tokens!|Slumber\s+Tickets)\s+\(Win\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        private bool _sawGameEndSeparator;
        private int _gameEndSummaryLinesRemaining;

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

        /// <summary>
        /// Detects Hypixel's Bed Wars end-of-game summary block.
        /// </summary>
        public bool IsGameEndSummary(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine)) return false;

            string cleanLine = Regex.Replace(logLine, "§.", "");
            var chatMatch = ChatMessagePattern.Match(cleanLine);
            if (!chatMatch.Success)
            {
                _sawGameEndSeparator = false;
                _gameEndSummaryLinesRemaining = 0;
                return false;
            }

            string message = chatMatch.Groups["message"].Value.Trim();
            if (message.Length >= 20 && message.Trim('?').Length == 0)
            {
                _sawGameEndSeparator = true;
                _gameEndSummaryLinesRemaining = 0;
                return false;
            }

            if (message.Equals("Bed Wars", StringComparison.OrdinalIgnoreCase))
            {
                _gameEndSummaryLinesRemaining = _sawGameEndSeparator ? 20 : 0;
                _sawGameEndSeparator = false;
                return false;
            }

            if (_gameEndSummaryLinesRemaining <= 0)
            {
                return false;
            }

            _gameEndSummaryLinesRemaining--;
            if (GameEndWinRewardPattern.IsMatch(message))
            {
                _gameEndSummaryLinesRemaining = 0;
                return true;
            }

            return false;
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
