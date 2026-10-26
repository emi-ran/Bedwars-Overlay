using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BedwarsOverlay.App.Services
{
    public sealed class TeamOcrTrace
    {
        public List<TeamOcrPass> Passes { get; } = new();
    }

    public sealed record TeamOcrPass(string Team, int PixelCount, string? Text)
    {
        public string Mode { get; init; } = "ColorMask";
        public int? RowY { get; init; }
    }

    /// <summary>Local development corpus; never uploads captures or modifies reviewed labels.</summary>
    public sealed class TeamOcrCaptureStore
    {
        private static readonly object WriteLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly string _directory;

        public TeamOcrCaptureStore(string directory) => _directory = directory;

        public static string? FindDevelopmentDirectory(string applicationDirectory)
        {
            for (var directory = new DirectoryInfo(applicationDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BedwarsOverlay.slnx")) &&
                    File.Exists(Path.Combine(directory.FullName, "BedwarsOverlay.App", "BedwarsOverlay.App.csproj")))
                {
                    return Path.Combine(directory.FullName, "images", "team-ocr");
                }
            }

            return null;
        }

        public string Save(byte[] screenshot, DateTimeOffset capturedAt, string[] rosterNames,
            IReadOnlyDictionary<string, string> recognizedTeams, TeamOcrTrace trace, string diagnostics, string? error)
        {
            lock (WriteLock)
            {
                Directory.CreateDirectory(_directory);
                string manifestPath = Path.Combine(_directory, "captures.json");
                // Refuse malformed existing data rather than replacing a corpus or human labels.
                var entries = File.Exists(manifestPath)
                    ? JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(manifestPath), JsonOptions)
                        ?? throw new InvalidDataException("OCR capture manifest must be a JSON array, not null.")
                    : new List<JsonElement>();
                string id = $"{capturedAt:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}";
                string image = id + ".png";
                File.WriteAllBytes(Path.Combine(_directory, image), screenshot);
                entries.Add(JsonSerializer.SerializeToElement(new
                {
                    id,
                    capturedAt,
                    image,
                    rosterNames,
                    recognizedTeams,
                    unmatchedRosterNames = rosterNames.Where(name => !recognizedTeams.ContainsKey(name)).ToArray(),
                    passes = trace.Passes,
                    diagnostics,
                    error,
                    expectedTeams = (Dictionary<string, string>?)null
                }, JsonOptions));
                string temporaryPath = Path.Combine(_directory, id + ".tmp");
                try
                {
                    File.WriteAllText(temporaryPath, JsonSerializer.Serialize(entries, JsonOptions));
                    File.Move(temporaryPath, manifestPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }

                return Path.Combine(_directory, image);
            }
        }
    }
}
