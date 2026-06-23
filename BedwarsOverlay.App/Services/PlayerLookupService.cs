using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BedwarsOverlay.Core.Api;
using BedwarsOverlay.Core.Models;
using BedwarsOverlay.Core.Stats;
using BedwarsOverlay.Core.Storage;

namespace BedwarsOverlay.App.Services
{
    /// <summary>
    /// Handles async player resolution (Mojang UUID lookup → Hypixel stats → row update).
    /// Owns cancellation token source for stale lookup cancellation.
    /// </summary>
    public sealed class PlayerLookupService : IDisposable
    {
        private readonly MojangClient _mojangClient;
        private readonly HypixelClient _hypixelClient;
        private readonly SettingsRepository _settingsRepository;
        private readonly Action<string> _addDiagnosticLine;
        private CancellationTokenSource? _cts;

        public PlayerLookupService(
            MojangClient mojangClient,
            HypixelClient hypixelClient,
            SettingsRepository settingsRepository,
            Action<string> addDiagnosticLine)
        {
            _mojangClient = mojangClient;
            _hypixelClient = hypixelClient;
            _settingsRepository = settingsRepository;
            _addDiagnosticLine = addDiagnosticLine;
        }

        /// <summary>Cancel any in-flight lookups and return a fresh cancellation token.</summary>
        public CancellationToken CancelAndReset()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public async Task ResolvePlayerAsync(string name, PlayerRow row, CancellationToken cancellationToken)
        {
            if (_mojangClient == null || _hypixelClient == null || _settingsRepository == null)
                return;

            try
            {
                // 1. Resolve Username to UUID
                cancellationToken.ThrowIfCancellationRequested();
                var mojangResult = await _mojangClient.ResolveUsernameAsync(name);
                cancellationToken.ThrowIfCancellationRequested();
                if (mojangResult.Status != MojangLookupStatus.Success || mojangResult.Uuid == null)
                {
                    string statusText = mojangResult.Status switch
                    {
                        MojangLookupStatus.RateLimited => "MOJANG_RATE_LIMIT",
                        MojangLookupStatus.NetworkError => "MOJANG_NETWORK_ERROR",
                        MojangLookupStatus.InvalidResponse => "MOJANG_INVALID_RESPONSE",
                        _ => "NICKED_OR_UNKNOWN"
                    };
                    _addDiagnosticLine($"{name}: {statusText}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        row.StatusText = statusText;
                        row.NameColor = Brushes.Red;
                        row.Level = "?";
                        row.Winstreak = "?";
                        row.Fkdr = "?";
                        row.Wlr = "?";
                        row.FinalKills = "?";
                        row.Wins = "?";
                        row.Sniper = "?";
                    });
                    return;
                }

                // 2. Fetch Hypixel stats JSON (with automatic DB caching and rate limit check)
                var hypixelResult = await _hypixelClient.GetPlayerAsync(mojangResult.Uuid);
                cancellationToken.ThrowIfCancellationRequested();
                if (hypixelResult.Status != HypixelLookupStatus.Success || hypixelResult.PlayerJson == null)
                {
                    string statusText = hypixelResult.Status switch
                    {
                        HypixelLookupStatus.MissingApiKey => "MISSING_API_KEY",
                        HypixelLookupStatus.InvalidApiKey => "INVALID_API_KEY",
                        HypixelLookupStatus.RateLimited => "HYPIXEL_RATE_LIMIT",
                        HypixelLookupStatus.NetworkError => "HYPIXEL_NETWORK_ERROR",
                        HypixelLookupStatus.InvalidResponse => "HYPIXEL_INVALID_RESPONSE",
                        _ => "NO_HYPIXEL_DATA"
                    };
                    _addDiagnosticLine($"{name}: {statusText}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        row.StatusText = statusText;
                        row.NameColor = Brushes.Orange;
                        row.Level = "-";
                        row.Winstreak = "-";
                        row.Fkdr = "-";
                        row.Wlr = "-";
                        row.FinalKills = "-";
                        row.Wins = "-";
                        row.Sniper = "-";
                    });
                    return;
                }

                // 3. Parse stats and build updated UI row
                var stats = StatsCalculator.ParseStats(hypixelResult.PlayerJson);

                // Rank Color
                Brush rankBrush = Brushes.White;
                string rankColorName = stats.GetRankColorName();
                if (rankColorName == "Cyan") rankBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                else if (rankColorName == "Green") rankBrush = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                else if (rankColorName == "Gold") rankBrush = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                else if (rankColorName == "Red") rankBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                else if (rankColorName == "Gray") rankBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

                string localName = _settingsRepository.LocalPlayerName;
                bool isLocalPlayer = !string.IsNullOrEmpty(localName) && name.Equals(localName, StringComparison.OrdinalIgnoreCase);

                string sniperStr = $"{CalculateThreatPercentage(stats):F1}%";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    row.Update(
                        stats.Level,
                        stats.HasWinstreak,
                        stats.Winstreak,
                        stats.Fkdr,
                        stats.Wlr,
                        stats.FinalKills,
                        stats.Wins,
                        sniperStr,
                        isLocalPlayer,
                        rankBrush,
                        stats.GetRankTag()
                    );
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _addDiagnosticLine($"{name}: ERROR {ex.Message}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    row.StatusText = "ERROR";
                    row.NameColor = Brushes.Red;
                });
            }
        }

        public static double CalculateThreatPercentage(BedwarsStats stats)
        {
            double threatPct;
            if (stats.Fkdr >= 10.0) threatPct = stats.Fkdr * 1.5;
            else if (stats.Fkdr >= 5.0) threatPct = stats.Fkdr;
            else if (stats.Fkdr >= 1.0) threatPct = stats.Fkdr * 0.5;
            else threatPct = stats.Fkdr * 0.2;

            if (stats.HasWinstreak && stats.Winstreak > 5)
            {
                threatPct += stats.Winstreak * 0.8;
            }

            return Math.Min(threatPct, 100.0);
        }
    }
}
