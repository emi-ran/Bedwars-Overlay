using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedwarsOverlay.Core.Storage;
using Microsoft.Data.Sqlite;

namespace BedwarsOverlay.Core.Api
{
    public enum HypixelLookupStatus
    {
        Success,
        MissingApiKey,
        InvalidApiKey,
        RateLimited,
        NoPlayerData,
        NetworkError,
        InvalidResponse
    }

    public sealed record HypixelLookupResult(HypixelLookupStatus Status, string? PlayerJson = null);
    public sealed record HypixelApiKeyValidationResult(HypixelLookupStatus Status);

    public class HypixelClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppDatabase _database;
        private readonly SettingsRepository _settingsRepository;

        public HypixelClient(HttpClient httpClient, AppDatabase database, SettingsRepository settingsRepository)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        }

        /// <summary>
        /// Retrieves the Hypixel player API JSON response for a given UUID.
        /// Queries the SQLite cache first. If cache is expired or missing, queries Hypixel public API.
        /// </summary>
        public async Task<string?> GetPlayerJsonAsync(string uuid)
        {
            var result = await GetPlayerAsync(uuid);
            return result.Status == HypixelLookupStatus.Success ? result.PlayerJson : null;
        }

        public async Task<HypixelLookupResult> GetPlayerAsync(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return new HypixelLookupResult(HypixelLookupStatus.NoPlayerData);

            // 1. Check SQLite Cache
            string? cachedJson = GetCachedPlayerJson(uuid);
            if (cachedJson != null)
            {
                return new HypixelLookupResult(HypixelLookupStatus.Success, cachedJson);
            }

            // 2. Fetch from Hypixel API
            string apiKey = _settingsRepository.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Without API key, we cannot query Hypixel
                return new HypixelLookupResult(HypixelLookupStatus.MissingApiKey);
            }

            string url = $"https://api.hypixel.net/v2/player?uuid={uuid}";
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("API-Key", apiKey);
                    
                    var response = await _httpClient.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        // Invalid API Key
                        return new HypixelLookupResult(HypixelLookupStatus.InvalidApiKey);
                    }

                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        // Rate limited
                        return new HypixelLookupResult(HypixelLookupStatus.RateLimited);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return new HypixelLookupResult(HypixelLookupStatus.NetworkError);
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Validate JSON contains player block
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                        {
                            if (doc.RootElement.TryGetProperty("player", out var playerProp) && playerProp.ValueKind != JsonValueKind.Null)
                            {
                                string playerJson = playerProp.GetRawText();
                                string displayname = "";
                                if (playerProp.TryGetProperty("displayname", out var nameProp))
                                {
                                    displayname = nameProp.GetString() ?? "";
                                }

                                // Cache the lookup
                                SavePlayerToCache(uuid, displayname, playerJson);
                                return new HypixelLookupResult(HypixelLookupStatus.Success, playerJson);
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HypixelLookupResult(HypixelLookupStatus.NetworkError);
            }
            catch
            {
                // Suppress network/parsing issues and return null
                return new HypixelLookupResult(HypixelLookupStatus.InvalidResponse);
            }

            return new HypixelLookupResult(HypixelLookupStatus.NoPlayerData);
        }

        public async Task<HypixelApiKeyValidationResult> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new HypixelApiKeyValidationResult(HypixelLookupStatus.MissingApiKey);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.hypixel.net/v2/counts");
                request.Headers.Add("API-Key", apiKey);

                using var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new HypixelApiKeyValidationResult(HypixelLookupStatus.InvalidApiKey);
                }

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    return new HypixelApiKeyValidationResult(HypixelLookupStatus.RateLimited);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new HypixelApiKeyValidationResult(HypixelLookupStatus.NetworkError);
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                bool success = doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                return new HypixelApiKeyValidationResult(success ? HypixelLookupStatus.Success : HypixelLookupStatus.InvalidResponse);
            }
            catch (TaskCanceledException)
            {
                return new HypixelApiKeyValidationResult(HypixelLookupStatus.NetworkError);
            }
            catch
            {
                return new HypixelApiKeyValidationResult(HypixelLookupStatus.InvalidResponse);
            }
        }

        private string? GetCachedPlayerJson(string uuid)
        {
            try
            {
                using (var conn = _database.GetConnection())
                {
                    string query = "SELECT stats_json, last_updated FROM player_cache WHERE uuid = @uuid LIMIT 1;";
                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uuid", uuid);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string statsJson = reader.GetString(0);
                                long lastUpdated = reader.GetInt64(1);

                                // Check Expiration
                                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                long ttlSeconds = _settingsRepository.CacheTtlMinutes * 60;
                                if (now - lastUpdated < ttlSeconds)
                                {
                                    return statsJson; // Cache is valid
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Suppress SQLite read errors
            }
            return null;
        }

        private void SavePlayerToCache(string uuid, string displayname, string playerJson)
        {
            try
            {
                using (var conn = _database.GetConnection())
                {
                    string insert = @"
                        INSERT OR REPLACE INTO player_cache (uuid, displayname, stats_json, last_updated)
                        VALUES (@uuid, @displayname, @stats_json, @last_updated);";
                    using (var cmd = new SqliteCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@uuid", uuid);
                        cmd.Parameters.AddWithValue("@displayname", displayname);
                        cmd.Parameters.AddWithValue("@stats_json", playerJson);
                        cmd.Parameters.AddWithValue("@last_updated", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Suppress SQLite write errors
            }
        }
    }
}
