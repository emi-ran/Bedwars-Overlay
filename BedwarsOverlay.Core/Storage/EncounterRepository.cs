using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace BedwarsOverlay.Core.Storage
{
    /// <summary>A single earlier encounter with a player.</summary>
    public sealed record RecentEncounter(long MatchId, long FirstSeenUtc, string? Outcome);

    /// <summary>
    /// Stores which player was seen in which Bed Wars match so the overlay can flag repeated
    /// opponents across games. One row per (match, player); a second /who inside the same game
    /// does not produce duplicates.
    /// </summary>
    public class EncounterRepository
    {
        /// <summary>Match rows that stay open longer than this are treated as abandoned.</summary>
        public static readonly TimeSpan DefaultMaxOpenMatchAge = TimeSpan.FromMinutes(30);

        private const string NameKeyPrefix = "name:";

        private readonly AppDatabase _database;

        public EncounterRepository(AppDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Builds the identity key used for a player. Resolved players are tracked by UUID because
        /// usernames can change; nicked / unresolvable players fall back to their username.
        /// </summary>
        public static string BuildPlayerKey(string? uuid, string username)
        {
            if (!string.IsNullOrWhiteSpace(uuid))
            {
                return uuid.Trim().ToLowerInvariant();
            }

            return NameKeyPrefix + (username ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Opens a new match and returns its id. Any match still marked as open is closed first: a new
        /// game starting means the previous one is over, even when its end summary never reached the
        /// log (the player left after being eliminated, disconnected, or the client skipped the block).
        /// </summary>
        public long StartMatch()
        {
            CloseOpenMatches();

            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(
                "INSERT INTO matches (started_at, ended_at, outcome) VALUES (@startedAt, NULL, NULL); SELECT last_insert_rowid();",
                conn))
            {
                cmd.Parameters.AddWithValue("@startedAt", Now());
                return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
            }
        }

        /// <summary>Returns the id of the currently open match, or null when no game is in progress.</summary>
        public long? GetOpenMatchId()
        {
            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(
                "SELECT id, started_at FROM matches WHERE ended_at IS NULL ORDER BY id DESC LIMIT 1;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                long id = reader.GetInt64(0);
                long startedAt = reader.GetInt64(1);
                long age = Now() - startedAt;

                if (age > (long)DefaultMaxOpenMatchAge.TotalSeconds)
                {
                    return null;
                }

                return id;
            }
        }

        /// <summary>
        /// Records a finished match. Outcome upgrades are applied in place, so reporting "loss" and
        /// then "win" for the same block (Hypixel prints "(Time Played)" before "(Win)") is safe.
        /// </summary>
        public void EndMatch(long matchId, string? outcome)
        {
            if (matchId <= 0)
            {
                return;
            }

            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(@"
                UPDATE matches
                SET ended_at = COALESCE(ended_at, @endedAt),
                    outcome = COALESCE(@outcome, outcome)
                WHERE id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("@endedAt", Now());
                cmd.Parameters.AddWithValue("@outcome", (object?)NormalizeOutcome(outcome) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", matchId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Closes every match that is still marked as open.</summary>
        public int CloseOpenMatches()
        {
            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(
                "UPDATE matches SET ended_at = @endedAt WHERE ended_at IS NULL;", conn))
            {
                cmd.Parameters.AddWithValue("@endedAt", Now());
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Records the players seen during a match. Duplicate entries inside a match are ignored.</summary>
        public void RecordPlayers(long matchId, IEnumerable<(string? Uuid, string Username)> players)
        {
            if (matchId <= 0 || players == null)
            {
                return;
            }

            using (var conn = _database.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                foreach (var (uuid, username) in players)
                {
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        continue;
                    }

                    using (var cmd = new SqliteCommand(@"
                        INSERT OR IGNORE INTO encounters (match_id, player_key, username, first_seen_at)
                        VALUES (@matchId, @playerKey, @username, @firstSeenAt);", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@matchId", matchId);
                        cmd.Parameters.AddWithValue("@playerKey", BuildPlayerKey(uuid, username));
                        cmd.Parameters.AddWithValue("@username", username.Trim());
                        cmd.Parameters.AddWithValue("@firstSeenAt", Now());
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// Counts in how many of the last <paramref name="lookbackMatches"/> matches each player was
        /// seen, excluding the match that is currently being played.
        /// The result is keyed by lowercase username and, for resolved players, by lowercase UUID.
        /// </summary>
        public Dictionary<string, int> GetRepeatCounts(int lookbackMatches, long? currentMatchId)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (lookbackMatches <= 0)
            {
                return counts;
            }

            const string sql = @"
                SELECT LOWER(username) AS player_key, COUNT(DISTINCT match_id) AS match_count
                FROM encounters
                WHERE match_id IN (
                        SELECT id FROM matches ORDER BY id DESC LIMIT @lookback
                      )
                  AND (@currentMatchId IS NULL OR match_id <> @currentMatchId)
                GROUP BY LOWER(username)
                UNION ALL
                SELECT player_key, COUNT(DISTINCT match_id)
                FROM encounters
                WHERE match_id IN (
                        SELECT id FROM matches ORDER BY id DESC LIMIT @lookback
                      )
                  AND (@currentMatchId IS NULL OR match_id <> @currentMatchId)
                  AND player_key NOT LIKE 'name:%'
                GROUP BY player_key;";

            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@lookback", lookbackMatches);
                cmd.Parameters.AddWithValue("@currentMatchId", (object?)currentMatchId ?? DBNull.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string key = reader.GetString(0);
                        int count = reader.GetInt32(1);
                        counts[key] = counts.TryGetValue(key, out int existing) ? Math.Max(existing, count) : count;
                    }
                }
            }

            return counts;
        }

        /// <summary>Returns the previous matches a player was seen in, newest first.</summary>
        public List<RecentEncounter> GetRecentEncounters(string playerKey, int lookbackMatches)
        {
            var results = new List<RecentEncounter>();
            if (string.IsNullOrWhiteSpace(playerKey) || lookbackMatches <= 0)
            {
                return results;
            }

            const string sql = @"
                SELECT e.match_id, e.first_seen_at, m.outcome
                FROM encounters e
                JOIN matches m ON m.id = e.match_id
                WHERE e.player_key = @playerKey
                ORDER BY e.match_id DESC
                LIMIT @lookback;";

            using (var conn = _database.GetConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@playerKey", playerKey.ToLowerInvariant());
                cmd.Parameters.AddWithValue("@lookback", lookbackMatches);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new RecentEncounter(
                            reader.GetInt64(0),
                            reader.GetInt64(1),
                            reader.IsDBNull(2) ? null : reader.GetString(2)));
                    }
                }
            }

            return results;
        }

        private static string? NormalizeOutcome(string? outcome)
        {
            if (string.IsNullOrWhiteSpace(outcome))
            {
                return null;
            }

            return outcome.Trim().ToLowerInvariant();
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
