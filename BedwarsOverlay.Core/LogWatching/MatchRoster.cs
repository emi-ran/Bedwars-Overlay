using System;
using System.Collections.Generic;
using System.Linq;

namespace BedwarsOverlay.Core.LogWatching
{
    /// <summary>In-memory match membership; /who is a partial snapshot during respawns.</summary>
    public sealed class MatchRoster
    {
        private readonly HashSet<string> _players = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _eliminated = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _disconnected = new(StringComparer.OrdinalIgnoreCase);
        public bool IsActive { get; private set; }
        public List<string> GetPlayers() => _players.ToList();
        public bool IsDisconnected(string name) => _disconnected.Contains(name);

        // A death can establish an in-memory match after a mid-game launch. This never opens
        // a persisted encounter match; encounter tracking still requires the real start block.
        public void Activate() => IsActive = true;

        public void ClearPlayers()
        {
            _players.Clear();
            _disconnected.Clear();
            if (!IsActive) _eliminated.Clear();
        }

        public void Reset(bool active = false)
        {
            _players.Clear();
            _eliminated.Clear();
            _disconnected.Clear();
            IsActive = active;
        }

        public List<string> ObservePlayers(IEnumerable<string> players)
        {
            if (!IsActive) _players.Clear();
            foreach (string player in players)
            {
                _disconnected.Remove(player);
                if (!_eliminated.Contains(player)) _players.Add(player);
            }
            return _players.ToList();
        }

        public List<string> ObserveDeath(PlayerDeathEvent death)
        {
            if (death.IsFinal) Eliminate(death.Victim);
            else if (!_eliminated.Contains(death.Victim)) _players.Add(death.Victim);
            if (death.Killer != null && !_eliminated.Contains(death.Killer)) _players.Add(death.Killer);
            return _players.ToList();
        }

        public bool ObserveConnection(PlayerConnectionEvent connection)
        {
            if (_eliminated.Contains(connection.Name)) return false;
            _players.Add(connection.Name);
            if (connection.IsConnected) _disconnected.Remove(connection.Name);
            else _disconnected.Add(connection.Name);
            return true;
        }

        public void Eliminate(string name)
        {
            _eliminated.Add(name);
            _players.Remove(name);
            _disconnected.Remove(name);
        }
    }
}
