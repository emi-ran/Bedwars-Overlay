using System;
using System.Collections.Generic;
using System.Linq;

namespace BedwarsOverlay.Core.Teams
{
    public static class TeammateResolver
    {
        // Team membership does not establish a nick's real account or party identity.
        public static bool IsTeammate(string playerName, string localPlayerName,
            IEnumerable<string> partyPlayers, IReadOnlyDictionary<string, string> teams)
        {
            string localName = localPlayerName.Trim();
            if (string.IsNullOrWhiteSpace(playerName) || playerName.Equals(localName, StringComparison.OrdinalIgnoreCase))
                return false;

            var party = new HashSet<string>(partyPlayers, StringComparer.OrdinalIgnoreCase);
            string? ownTeam = FindTeam(localName, teams);
            if (ownTeam == null)
            {
                // Parties can span opposing teams. Only a single consistent team is usable
                // when the configured local player is absent from the capture.
                string[] partyTeams = party.Where(name => !name.Equals(localName, StringComparison.OrdinalIgnoreCase))
                    .Select(name => FindTeam(name, teams))
                    .Where(team => team != null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToArray();
                if (partyTeams.Length == 1) ownTeam = partyTeams[0];
            }

            string? playerTeam = FindTeam(playerName, teams);
            if (ownTeam != null && playerTeam != null)
                return ownTeam.Equals(playerTeam, StringComparison.OrdinalIgnoreCase);

            return party.Contains(playerName);
        }

        private static string? FindTeam(string name, IReadOnlyDictionary<string, string> teams) =>
            teams.FirstOrDefault(pair => pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                TeamPalette.Teams.Any(team => team.Name.Equals(pair.Value, StringComparison.OrdinalIgnoreCase))).Value;
    }
}
