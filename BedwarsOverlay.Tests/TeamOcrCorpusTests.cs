using System.IO;
using System.Text.Json;
using BedwarsOverlay.App.Services;

namespace BedwarsOverlay.Tests;

public class TeamOcrCorpusTests
{
    [Fact]
    public void BlankCapture_DoesNotInventTeams()
    {
        using var bitmap = new System.Drawing.Bitmap(640, 360);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) graphics.Clear(System.Drawing.Color.Black);
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        using var recognizer = new TeamRecognitionService(AppContext.BaseDirectory);
        Assert.Empty(recognizer.ReadTeams(stream.ToArray(), new[] { "TryHardnt", "Rulls", "krists" }));
    }

    [Fact]
    public void ReviewedCaptures_MatchVisibleTeamsWithoutFalseAssignments()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TeamOcr");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "expected.json")));
        using var recognizer = new TeamRecognitionService(AppContext.BaseDirectory);
        var failures = new List<string>();
        string? originalDirectory = Environment.GetEnvironmentVariable("BEDWARS_OCR_CORPUS");
        foreach (var entry in manifest.RootElement.EnumerateArray())
        {
            string file = entry.GetProperty("image").GetString()!;
            var expected = entry.GetProperty("expectedTeams").Deserialize<Dictionary<string, string>>()!;
            string[] roster = entry.GetProperty("rosterNames").Deserialize<string[]>()!;
            var trace = new TeamOcrTrace();
            string capturePath = string.IsNullOrWhiteSpace(originalDirectory)
                ? Path.Combine(directory, file)
                : Path.Combine(originalDirectory, entry.GetProperty("sourceImage").GetString()!);
            var actual = recognizer.ReadTeams(File.ReadAllBytes(capturePath), roster, out _, trace);
            string[] knownMissing = entry.TryGetProperty("knownMissingNames", out var missing)
                ? missing.Deserialize<string[]>()! : Array.Empty<string>();
            string? reportDirectory = Environment.GetEnvironmentVariable("BEDWARS_OCR_REPORT");
            if (!string.IsNullOrWhiteSpace(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
                File.WriteAllText(Path.Combine(reportDirectory, file + ".json"), JsonSerializer.Serialize(new
                {
                    image = file, expectedTeams = expected, recognizedTeams = actual, passes = trace.Passes
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            foreach (var pair in expected)
                if (knownMissing.Contains(pair.Key))
                    Assert.False(actual.ContainsKey(pair.Key), $"Known miss {pair.Key} is now recognized; update corpus expectations.");
                else if (!actual.TryGetValue(pair.Key, out string? team) || team != pair.Value)
                    failures.Add($"{file}: {pair.Key} expected {pair.Value}, actual {team ?? "missing"}");
            foreach (var pair in actual)
                if (!expected.TryGetValue(pair.Key, out string? team) || team != pair.Value)
                    failures.Add($"{file}: unexpected {pair.Key}={pair.Value}");
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
