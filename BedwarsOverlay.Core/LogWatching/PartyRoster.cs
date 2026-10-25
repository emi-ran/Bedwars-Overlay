using System;
using System.Collections.Generic;
using System.Linq;

namespace BedwarsOverlay.Core.LogWatching
{
    /// <summary>In-memory party membership; automatic Hypixel party events and /p list snapshots update it.</summary>
    public sealed class PartyRoster : IEnumerable<string>
    {
        private readonly HashSet<string> _players = new(StringComparer.OrdinalIgnoreCase);

        public int Count => _players.Count;

        public bool Contains(string? name) =>
            !string.IsNullOrWhiteSpace(name) && _players.Contains(name);

        public bool Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 16)
            {
                return false;
            }

            return _players.Add(name.Trim());
        }

        public bool Remove(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return _players.Remove(name.Trim());
        }

        public void Clear() => _players.Clear();

        public bool Apply(PartyEvent? partyEvent)
        {
            if (partyEvent == null || partyEvent.Kind == PartyEventKind.None)
            {
                return false;
            }

            switch (partyEvent.Kind)
            {
                case PartyEventKind.OwnJoin:
                    _players.Clear();
                    if (!string.IsNullOrWhiteSpace(partyEvent.PlayerName))
                    {
                        _players.Add(partyEvent.PlayerName.Trim());
                    }
                    return true;

                case PartyEventKind.CompanionRoster:
                    if (partyEvent.PlayerNames != null)
                    {
                        foreach (string name in partyEvent.PlayerNames)
                        {
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                _players.Add(name.Trim());
                            }
                        }
                    }
                    return true;

                case PartyEventKind.MemberJoined:
                    if (!string.IsNullOrWhiteSpace(partyEvent.PlayerName))
                    {
                        _players.Add(partyEvent.PlayerName.Trim());
                    }
                    return true;

                case PartyEventKind.MemberLeft:
                    if (!string.IsNullOrWhiteSpace(partyEvent.PlayerName))
                    {
                        _players.Remove(partyEvent.PlayerName.Trim());
                    }
                    return true;

                case PartyEventKind.OwnLeave:
                case PartyEventKind.OwnRemoved:
                case PartyEventKind.Disbanded:
                case PartyEventKind.NotInParty:
                    _players.Clear();
                    return true;

                default:
                    return false;
            }
        }

        public IEnumerator<string> GetEnumerator() => _players.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}