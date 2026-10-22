using System;
using System.Collections.Generic;
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
    /// Repeat-opponent data for the batch of players currently being resolved.
    /// </summary>
    public sealed class EncounterContext
    {
        public long? MatchId { get; init; }
        public IReadOnlyDictionary<string, int> RepeatCounts { get; init; } = new Dictionary<string, int>();
        public int MinCount { get; init; } = 2;
        public int Lookback { get; init; } = 10;
    }

    /// <summary>
    /// Handles async player resolution (Mojang UUID lookup → Hypixel stats → row update) and
    /// records the players of the running match for repeat-opponent detection.
    /// Owns cancellation token source for stale lookup cancellation.
    /// </summary>
    public sealed class PlayerLookupService : IDisposable
    {
        private readonly MojangClient _mojangClient;
        private readonly HypixelClient _hypixelClient;
        private readonly SettingsRepository _settingsRepository;
        private readonly EncounterRepository? _encounterRepository;
        private readonly Action<string> _addDiagnosticLine;
        private CancellationTokenSource? _cts;
        private EncounterContext? _encounterContext;

        public PlayerLookupService(
            MojangClient mojangClient,
            HypixelClient hypixelClient,
            SettingsRepository settingsRepository,
            Action<string> addDiagnosticLine,
            EncounterRepository? encounterRepository = null)
        {
            _mojangClient = mojangClient;
            _hypixelClient = hypixelClient;
            _settingsRepository = settingsRepository;
            _addDiagnosticLine = addDiagnosticLine;
            _encounterRepository = encounterRepository;
        }

        /// <summary>Cancel any in-flight lookups and return a fresh cancellation token.</summary>
        public CancellationToken CancelAndReset()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

        /// <summary>Append lookups without cancelling rows already loading in this roster.</summary>
        public CancellationToken CurrentToken => _cts?.Token ?? CancelAndReset();

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>Repeat-opponent data applied to the rows of the next resolution batch.</summary>
        public void SetEncounterContext(EncounterContext? context)
        {
            _encounterContext = context;
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

                    // Nicks are random per game, so a nicked name is never remembered; a real name that
                    // Mojang temporarily cannot resolve still is.
                    if (mojangResult.Status != MojangLookupStatus.NickedOrUnknown)
                    {
                        RecordEncounter(null, name);
                    }

                    int repeatCount = GetRepeatCount(name, null);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        row.StatusText = statusText;
                        row.RankColor = Brushes.Red;
                        row.Level = "?";
                        row.Winstreak = "?";
                        row.Fkdr = "?";
                        row.Wlr = "?";
                        row.FinalKills = "?";
                        row.Wins = "?";
                        row.Threat = "?";
                        ApplyRepeatInfo(row, repeatCount);
                    });
                    return;
                }

                // The player was in this match even if Hypixel cannot answer right now.
                RecordEncounter(mojangResult.Uuid, name);
                int resolvedRepeatCount = GetRepeatCount(name, mojangResult.Uuid);

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
                        row.RankColor = Brushes.Orange;
                        row.Level = "-";
                        row.Winstreak = "-";
                        row.Fkdr = "-";
                        row.Wlr = "-";
                        row.FinalKills = "-";
                        row.Wins = "-";
                        row.Threat = "-";
                        ApplyRepeatInfo(row, resolvedRepeatCount);
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

                var threat = ThreatScorer.Score(stats, resolvedRepeatCount, _encounterContext?.Lookback ?? 0);

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
                        threat,
                        isLocalPlayer,
                        rankBrush,
                        stats.GetRankTag()
                    );

                    ApplyRepeatInfo(row, resolvedRepeatCount);
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
                    row.RankColor = Brushes.Red;
                });
            }
        }

        private void RecordEncounter(string? uuid, string name)
        {
            var context = _encounterContext;
            if (_encounterRepository == null || context?.MatchId == null)
            {
                return;
            }

            try
            {
                _encounterRepository.RecordPlayers(context.MatchId.Value, new[] { (uuid, name) });
            }
            catch (Exception ex)
            {
                _addDiagnosticLine($"{name}: encounter not stored ({ex.Message})");
            }
        }

        private int GetRepeatCount(string name, string? uuid)
        {
            var counts = _encounterContext?.RepeatCounts;
            if (counts == null)
            {
                return 0;
            }

            int best = 0;
            if (!string.IsNullOrWhiteSpace(name) && counts.TryGetValue(name.ToLowerInvariant(), out int byName))
            {
                best = byName;
            }

            if (!string.IsNullOrWhiteSpace(uuid) && counts.TryGetValue(uuid.ToLowerInvariant(), out int byUuid))
            {
                best = Math.Max(best, byUuid);
            }

            return best;
        }

        private void ApplyRepeatInfo(PlayerRow row, int repeatCount)
        {
            var context = _encounterContext;
            if (_settingsRepository?.HighlightRepeatOpponents != true || context == null || repeatCount <= 0)
            {
                row.ApplyEncounterInfo(0, 1, string.Empty);
                return;
            }

            string tooltip = LocalizationService.Instance["Repeat_Tooltip"]
                .Replace("{0}", repeatCount.ToString())
                .Replace("{1}", context.Lookback.ToString());

            row.ApplyEncounterInfo(repeatCount, context.MinCount, tooltip);
        }
    }
}
