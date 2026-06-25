using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BedwarsOverlay.App.Services
{
    public sealed class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; init; }
        public string LatestVersion { get; init; } = string.Empty;
        public string ReleaseUrl { get; init; } = string.Empty;
    }

    public sealed class UpdateCheckService
    {
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/emi-ran/Bedwars-Overlay/releases/latest";
        private const string LatestReleaseUrl = "https://github.com/emi-ran/Bedwars-Overlay/releases/latest";
        private readonly HttpClient _httpClient;

        public UpdateCheckService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public static string CurrentVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        public async Task<UpdateCheckResult> CheckLatestReleaseAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
            request.Headers.UserAgent.ParseAdd($"BedwarsOverlay/{CurrentVersion}");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            JsonElement root = document.RootElement;

            string latestTag = root.TryGetProperty("tag_name", out JsonElement tagElement)
                ? tagElement.GetString() ?? string.Empty
                : string.Empty;
            string releaseUrl = root.TryGetProperty("html_url", out JsonElement urlElement)
                ? urlElement.GetString() ?? LatestReleaseUrl
                : LatestReleaseUrl;

            return new UpdateCheckResult
            {
                IsUpdateAvailable = IsNewerVersion(latestTag, CurrentVersion),
                LatestVersion = latestTag,
                ReleaseUrl = string.IsNullOrWhiteSpace(releaseUrl) ? LatestReleaseUrl : releaseUrl
            };
        }

        private static bool IsNewerVersion(string latestTag, string currentVersion)
        {
            string normalizedLatest = latestTag.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(normalizedLatest, out Version? latest))
            {
                return false;
            }

            if (!Version.TryParse(currentVersion.Trim().TrimStart('v', 'V'), out Version? current))
            {
                return false;
            }

            return latest > current;
        }
    }
}
