using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BedwarsOverlay.Core.Storage
{
    /// <summary>
    /// Owns the local SQLite database (%LOCALAPPDATA%\BedwarsOverlay\data.db) and its schema version.
    /// The schema version is stored in SQLite's <c>user_version</c> pragma so migrations can run once.
    /// </summary>
    public class AppDatabase
    {
        /// <summary>
        /// v1: username/UUID cache, player cache and blacklist.
        /// v2: match tracking (matches + encounters rewritten with a match id).
        /// </summary>
        private const int CurrentSchemaVersion = 2;

        private readonly string _dbPath;

        public AppDatabase()
            : this(GetDefaultDatabasePath())
        {
        }

        /// <summary>Creates the database at a specific path. Used by tests.</summary>
        public AppDatabase(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path must not be empty.", nameof(databasePath));
            }

            string? directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _dbPath = databasePath;
            InitializeDatabase();
        }

        private static string GetDefaultDatabasePath()
        {
            // Database is saved in %LOCALAPPDATA%\BedwarsOverlay\data.db
            string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BedwarsOverlay");
            return Path.Combine(localAppFolder, "data.db");
        }

        /// <summary>
        /// Gets an opened SQLite database connection. Caller is responsible for disposing it.
        /// </summary>
        public SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                // Read the version before opening a transaction: commands must carry the active
                // transaction in Microsoft.Data.Sqlite, and a silently failed read would re-run
                // the v2 migration on every start.
                int schemaVersion = GetSchemaVersion(conn);

                using (var transaction = conn.BeginTransaction())
                {
                    CreateBaseSchema(conn, transaction);

                    if (schemaVersion < 2)
                    {
                        ApplyV2Schema(conn, transaction);
                    }

                    SetSchemaVersion(conn, transaction, CurrentSchemaVersion);
                    transaction.Commit();
                }
            }
        }

        private static int GetSchemaVersion(SqliteConnection conn)
        {
            using (var cmd = new SqliteCommand("PRAGMA user_version;", conn))
            {
                object? result = cmd.ExecuteScalar();
                return result == null ? 0 : Convert.ToInt32(result);
            }
        }

        private static void SetSchemaVersion(SqliteConnection conn, SqliteTransaction transaction, int version)
        {
            using (var cmd = new SqliteCommand($"PRAGMA user_version = {version};", conn, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void Execute(SqliteConnection conn, SqliteTransaction transaction, string sql)
        {
            using (var cmd = new SqliteCommand(sql, conn, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Schema shared by every version (created idempotently).</summary>
        private static void CreateBaseSchema(SqliteConnection conn, SqliteTransaction transaction)
        {
            // 1. Table for Minecraft Username to undashed UUID mapping
            Execute(conn, transaction, @"
                CREATE TABLE IF NOT EXISTS username_uuid_cache (
                    username TEXT PRIMARY KEY COLLATE NOCASE,
                    uuid TEXT NOT NULL,
                    last_updated INTEGER NOT NULL
                );");

            // 2. Table for Hypixel Player Stats JSON Cache
            Execute(conn, transaction, @"
                CREATE TABLE IF NOT EXISTS player_cache (
                    uuid TEXT PRIMARY KEY COLLATE NOCASE,
                    displayname TEXT,
                    stats_json TEXT NOT NULL,
                    last_updated INTEGER NOT NULL
                );");

            // 3. Table for player blacklist
            Execute(conn, transaction, @"
                CREATE TABLE IF NOT EXISTS blacklist (
                    uuid TEXT PRIMARY KEY COLLATE NOCASE,
                    reason TEXT,
                    date_added INTEGER NOT NULL
                );");
        }

        /// <summary>
        /// v2 adds match tracking so the overlay can tell whether a player was already seen in an
        /// earlier game. The v1 <c>encounters(uuid, encounter_time)</c> table was never read or
        /// written by the application, so it is replaced instead of migrated.
        /// </summary>
        private static void ApplyV2Schema(SqliteConnection conn, SqliteTransaction transaction)
        {
            Execute(conn, transaction, "DROP TABLE IF EXISTS encounters;");

            Execute(conn, transaction, @"
                CREATE TABLE IF NOT EXISTS matches (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    started_at INTEGER NOT NULL,
                    ended_at INTEGER NULL,
                    outcome TEXT NULL
                );");

            // player_key is the lowercase UUID when Mojang resolved the name, otherwise "name:<lowercase>"
            // so nicked / unresolvable players can still be tracked without a UUID.
            Execute(conn, transaction, @"
                CREATE TABLE IF NOT EXISTS encounters (
                    match_id INTEGER NOT NULL,
                    player_key TEXT NOT NULL,
                    username TEXT NOT NULL,
                    first_seen_at INTEGER NOT NULL,
                    PRIMARY KEY (match_id, player_key)
                );");

            Execute(conn, transaction, "CREATE INDEX IF NOT EXISTS ix_encounters_player_key ON encounters(player_key);");
        }
    }
}
