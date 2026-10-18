using System;
using System.Collections.Generic;
using System.IO;
using BedwarsOverlay.Core.Storage;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class EncounterRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly AppDatabase _database;
        private readonly EncounterRepository _repository;

        public EncounterRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"bw-overlay-test-{Guid.NewGuid():N}.db");
            _database = new AppDatabase(_dbPath);
            _repository = new EncounterRepository(_database);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
                // Temp file cleanup is best effort.
            }
        }

        [Fact]
        public void StartMatch_ThenGetOpenMatchId_ReturnsTheNewMatch()
        {
            long id = _repository.StartMatch();

            Assert.True(id > 0);
            Assert.Equal(id, _repository.GetOpenMatchId());
        }

        [Fact]
        public void EndMatch_ClosesTheMatch()
        {
            long id = _repository.StartMatch();

            _repository.EndMatch(id, "win");

            Assert.Null(_repository.GetOpenMatchId());
        }

        [Fact]
        public void EndMatch_UpgradesLossToWin()
        {
            long id = _repository.StartMatch();

            // Hypixel prints "(Time Played)" before the "(Win)" line of the same game.
            _repository.EndMatch(id, "loss");
            _repository.EndMatch(id, "win");

            Assert.Equal("win", GetOutcome(id));
        }

        [Fact]
        public void StartMatch_ClosesAbandonedMatches()
        {
            long stale = _repository.StartMatch();
            SetStartedAt(stale, DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds());

            long fresh = _repository.StartMatch();

            Assert.NotEqual(stale, fresh);
            Assert.Equal(fresh, _repository.GetOpenMatchId());
            Assert.NotNull(GetEndedAt(stale));
        }

        [Fact]
        public void RecordPlayers_IgnoresDuplicateNamesInsideTheSameMatch()
        {
            long id = _repository.StartMatch();

            _repository.RecordPlayers(id, new[] { ((string?)null, "Notch"), (null, "notch") });

            Assert.Equal(1, CountEncounters(id));
        }

        [Fact]
        public void GetRepeatCounts_ExcludesTheCurrentMatch()
        {
            long first = _repository.StartMatch();
            _repository.RecordPlayers(first, new[] { ((string?)null, "Notch") });
            _repository.EndMatch(first, "win");

            long current = _repository.StartMatch();
            _repository.RecordPlayers(current, new[] { ((string?)null, "Notch") });

            var counts = _repository.GetRepeatCounts(10, current);

            Assert.Equal(1, counts["notch"]);
        }

        [Fact]
        public void GetRepeatCounts_CountsEachMatchOnce()
        {
            for (int i = 0; i < 3; i++)
            {
                long match = _repository.StartMatch();
                _repository.RecordPlayers(match, new[] { ((string?)null, "Notch"), (null, "notch") });
                _repository.EndMatch(match, "win");
            }

            long current = _repository.StartMatch();
            var counts = _repository.GetRepeatCounts(10, current);

            Assert.Equal(3, counts["notch"]);
        }

        [Fact]
        public void GetRepeatCounts_IsKeyedByUuidAndUsername()
        {
            long match = _repository.StartMatch();
            _repository.RecordPlayers(match, new (string? Uuid, string Username)[] { ("069A79F444E94726A5BAFAA9B63CAE30", "Notch") });
            _repository.EndMatch(match, "win");

            var counts = _repository.GetRepeatCounts(10, null);

            Assert.Equal(1, counts["notch"]);
            Assert.Equal(1, counts["069a79f444e94726a5bafaa9b63cae30"]);
        }

        [Fact]
        public void GetRepeatCounts_RespectsTheLookbackWindow()
        {
            for (int i = 0; i < 5; i++)
            {
                long match = _repository.StartMatch();
                _repository.RecordPlayers(match, new[] { ((string?)null, "Notch") });
                _repository.EndMatch(match, "win");
            }

            var counts = _repository.GetRepeatCounts(2, null);

            Assert.Equal(2, counts["notch"]);
        }

        [Fact]
        public void GetRecentEncounters_ReturnsMatchesNewestFirst()
        {
            var matchIds = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                long match = _repository.StartMatch();
                _repository.RecordPlayers(match, new[] { ((string?)null, "Notch") });
                _repository.EndMatch(match, i == 0 ? "loss" : "win");
                matchIds.Add(match);
            }

            var recent = _repository.GetRecentEncounters(EncounterRepository.BuildPlayerKey(null, "Notch"), 10);

            Assert.Equal(3, recent.Count);
            Assert.Equal(matchIds[2], recent[0].MatchId);
            Assert.Equal("win", recent[0].Outcome);
        }

        [Theory]
        [InlineData(null, "Notch", "name:notch")]
        [InlineData("", "Notch", "name:notch")]
        [InlineData("069A79F444E94726A5BAFAA9B63CAE30", "Notch", "069a79f444e94726a5bafaa9b63cae30")]
        public void BuildPlayerKey_PrefersUuidAndLowercases(string? uuid, string name, string expected)
        {
            Assert.Equal(expected, EncounterRepository.BuildPlayerKey(uuid, name));
        }

        private string? GetOutcome(long matchId)
        {
            using var conn = _database.GetConnection();
            using var cmd = new SqliteCommand("SELECT outcome FROM matches WHERE id = @id;", conn);
            cmd.Parameters.AddWithValue("@id", matchId);
            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToString(result);
        }

        private object? GetEndedAt(long matchId)
        {
            using var conn = _database.GetConnection();
            using var cmd = new SqliteCommand("SELECT ended_at FROM matches WHERE id = @id;", conn);
            cmd.Parameters.AddWithValue("@id", matchId);
            object? result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        private void SetStartedAt(long matchId, long startedAt)
        {
            using var conn = _database.GetConnection();
            using var cmd = new SqliteCommand("UPDATE matches SET started_at = @startedAt WHERE id = @id;", conn);
            cmd.Parameters.AddWithValue("@startedAt", startedAt);
            cmd.Parameters.AddWithValue("@id", matchId);
            cmd.ExecuteNonQuery();
        }

        private int CountEncounters(long matchId)
        {
            using var conn = _database.GetConnection();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM encounters WHERE match_id = @id;", conn);
            cmd.Parameters.AddWithValue("@id", matchId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    public class AppDatabaseMigrationTests
    {
        [Fact]
        public void LegacyDatabase_IsMigratedToVersion2_AndKeepsCacheTables()
        {
            string path = Path.Combine(Path.GetTempPath(), $"bw-overlay-migration-{Guid.NewGuid():N}.db");

            try
            {
                CreateLegacyDatabase(path);

                var database = new AppDatabase(path);

                using var conn = database.GetConnection();

                Assert.Equal(2, GetUserVersion(conn));
                Assert.True(TableExists(conn, "matches"));
                Assert.True(EncounterTableHasMatchId(conn));

                // The legacy (unused) encounters table had no match_id and is replaced, not migrated.
                Assert.Equal(0, ScalarInt(conn, "SELECT COUNT(*) FROM encounters;"));

                // Unrelated caches must survive the migration.
                Assert.Equal(1, ScalarInt(conn, "SELECT COUNT(*) FROM username_uuid_cache;"));
                Assert.Equal(1, ScalarInt(conn, "SELECT COUNT(*) FROM player_cache;"));
                Assert.Equal(1, ScalarInt(conn, "SELECT COUNT(*) FROM blacklist;"));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        [Fact]
        public void FreshDatabase_IsCreatedAtVersion2()
        {
            string path = Path.Combine(Path.GetTempPath(), $"bw-overlay-fresh-{Guid.NewGuid():N}.db");

            try
            {
                var database = new AppDatabase(path);
                using var conn = database.GetConnection();

                Assert.Equal(2, GetUserVersion(conn));
                Assert.True(TableExists(conn, "matches"));
                Assert.True(EncounterTableHasMatchId(conn));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        [Fact]
        public void ExistingVersion2Database_IsNotMigratedTwice()
        {
            string path = Path.Combine(Path.GetTempPath(), $"bw-overlay-idempotent-{Guid.NewGuid():N}.db");

            try
            {
                var first = new AppDatabase(path);
                var repository = new EncounterRepository(first);
                long match = repository.StartMatch();
                repository.RecordPlayers(match, new[] { ((string?)null, "Notch") });
                repository.EndMatch(match, "win");

                // Re-opening must keep the data instead of re-running the v2 migration.
                SqliteConnection.ClearAllPools();
                var second = new AppDatabase(path);
                using var conn = second.GetConnection();

                Assert.Equal(2, GetUserVersion(conn));
                Assert.Equal(1, ScalarInt(conn, "SELECT COUNT(*) FROM encounters;"));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        private static void CreateLegacyDatabase(string path)
        {
            using var conn = new SqliteConnection($"Data Source={path}");
            conn.Open();

            using var cmd = new SqliteCommand(@"
                CREATE TABLE username_uuid_cache (
                    username TEXT PRIMARY KEY COLLATE NOCASE,
                    uuid TEXT NOT NULL,
                    last_updated INTEGER NOT NULL
                );
                CREATE TABLE player_cache (
                    uuid TEXT PRIMARY KEY COLLATE NOCASE,
                    displayname TEXT,
                    stats_json TEXT NOT NULL,
                    last_updated INTEGER NOT NULL
                );
                CREATE TABLE blacklist (
                    uuid TEXT PRIMARY KEY COLLATE NOCASE,
                    reason TEXT,
                    date_added INTEGER NOT NULL
                );
                CREATE TABLE encounters (
                    uuid TEXT,
                    encounter_time INTEGER
                );
                INSERT INTO username_uuid_cache VALUES ('Notch', '069a79f444e94726a5bafaa9b63cae30', 1);
                INSERT INTO player_cache VALUES ('069a79f444e94726a5bafaa9b63cae30', 'Notch', '{}', 1);
                INSERT INTO blacklist VALUES ('069a79f444e94726a5bafaa9b63cae30', 'test', 1);
                INSERT INTO encounters VALUES ('069a79f444e94726a5bafaa9b63cae30', 1);", conn);
            cmd.ExecuteNonQuery();
        }

        private static int GetUserVersion(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand("PRAGMA user_version;", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static bool TableExists(SqliteConnection conn, string table)
        {
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;", conn);
            cmd.Parameters.AddWithValue("@name", table);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool EncounterTableHasMatchId(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM pragma_table_info('encounters') WHERE name = 'match_id';", conn);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static int ScalarInt(SqliteConnection conn, string sql)
        {
            using var cmd = new SqliteCommand(sql, conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
