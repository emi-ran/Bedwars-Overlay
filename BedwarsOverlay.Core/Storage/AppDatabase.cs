using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BedwarsOverlay.Core.Storage
{
    public class AppDatabase
    {
        private readonly string _dbPath;

        public AppDatabase()
        {
            // Database is saved in %LOCALAPPDATA%\BedwarsOverlay\data.db
            string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BedwarsOverlay");
            if (!Directory.Exists(localAppFolder))
            {
                Directory.CreateDirectory(localAppFolder);
            }
            _dbPath = Path.Combine(localAppFolder, "data.db");
            InitializeDatabase();
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
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Table for Minecraft Username to undashed UUID mapping
                    string createUuidCacheTable = @"
                        CREATE TABLE IF NOT EXISTS username_uuid_cache (
                            username TEXT PRIMARY KEY COLLATE NOCASE,
                            uuid TEXT NOT NULL,
                            last_updated INTEGER NOT NULL
                        );";

                    // 2. Table for Hypixel Player Stats JSON Cache
                    string createPlayerCacheTable = @"
                        CREATE TABLE IF NOT EXISTS player_cache (
                            uuid TEXT PRIMARY KEY COLLATE NOCASE,
                            displayname TEXT,
                            stats_json TEXT NOT NULL,
                            last_updated INTEGER NOT NULL
                        );";

                    // 3. Table for tracking player encounters (frequency metrics)
                    string createEncountersTable = @"
                        CREATE TABLE IF NOT EXISTS encounters (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            uuid TEXT NOT NULL,
                            encounter_time INTEGER NOT NULL
                        );";

                    // 4. Table for player blacklist
                    string createBlacklistTable = @"
                        CREATE TABLE IF NOT EXISTS blacklist (
                            uuid TEXT PRIMARY KEY COLLATE NOCASE,
                            reason TEXT,
                            date_added INTEGER NOT NULL
                        );";

                    using (var cmd = new SqliteCommand(createUuidCacheTable, conn, transaction)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new SqliteCommand(createPlayerCacheTable, conn, transaction)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new SqliteCommand(createEncountersTable, conn, transaction)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new SqliteCommand(createBlacklistTable, conn, transaction)) { cmd.ExecuteNonQuery(); }

                    transaction.Commit();
                }
            }
        }
    }
}
