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

        private readonly List<Regex> _patterns;

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
    }
}
