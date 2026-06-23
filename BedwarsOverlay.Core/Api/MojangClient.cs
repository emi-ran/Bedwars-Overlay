using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedwarsOverlay.Core.Storage;
using Microsoft.Data.Sqlite;

namespace BedwarsOverlay.Core.Api
{
    public class MojangProfile
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    public class MojangClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppDatabase _database;
        private const int CacheTtlHours = 24;

        public MojangClient(HttpClient httpClient, AppDatabase database)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Resolves a Minecraft username to its undashed UUID, utilizing a local SQLite cache.
        /// Returns null if the username does not exist (could be nicked or invalid).
        /// </summary>
        public async Task<string?> ResolveUsernameToUuidAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            // 1. Check local cache first
            string? cachedUuid = GetCachedUuid(username);
            if (cachedUuid != null)
            {
                return cachedUuid == "NICKED" ? null : cachedUuid;
            }

            // 2. Query Mojang API
            string url = $"https://api.mojang.com/users/profiles/minecraft/{Uri.EscapeDataString(username)}";
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Negative caching: Save as NICKED to avoid spamming Mojang on every lobby /who
                    SaveUuidToCache(username, "NICKED");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Do not cache server errors or rate limits, just return null
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<MojangProfile>(responseBody);
                
                if (profile != null && !string.IsNullOrEmpty(profile.id))
                {
                    // Cache the successful lookup
                    SaveUuidToCache(username, profile.id);
                    return profile.id;
                }
            }
            catch
            {
                // Network or parsing errors, ignore caching
            }

            return null;
        }

        private string? GetCachedUuid(string username)
        {
            try
            {
                using (var conn = _database.GetConnection())
                {
                    string query = "SELECT uuid, last_updated FROM username_uuid_cache WHERE username = @username LIMIT 1;";
                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string uuid = reader.GetString(0);
                                long lastUpdated = reader.GetInt64(1);
                                
                                // Validate Cache Expiry
                                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                long expirationSeconds = CacheTtlHours * 3600;
                                if (now - lastUpdated < expirationSeconds)
                                {
                                    return uuid; // Cache hits and is valid
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback on database lock or read issues
            }
            return null;
        }

        private void SaveUuidToCache(string username, string uuid)
        {
            try
            {
                using (var conn = _database.GetConnection())
                {
                    string insert = @"
                        INSERT OR REPLACE INTO username_uuid_cache (username, uuid, last_updated)
                        VALUES (@username, @uuid, @last_updated);";
                    using (var cmd = new SqliteCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@uuid", uuid);
                        cmd.Parameters.AddWithValue("@last_updated", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Suppress DB write errors
            }
        }
    }
}
