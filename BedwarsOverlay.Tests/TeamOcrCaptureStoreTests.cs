using System.IO;
using System.Text.Json;
using BedwarsOverlay.App.Services;

namespace BedwarsOverlay.Tests;

public sealed class TeamOcrCaptureStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BedwarsOcrTests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Save_CreatesAndAppendsCaptures_PreservingExistingLabels()
    {
        var store = new TeamOcrCaptureStore(_root);
        byte[] image = { 1, 2, 3 };
        var trace = new TeamOcrTrace();
        trace.Passes.Add(new TeamOcrPass("Blue", 100, "B PlayerOne\nB Unreadable"));
        var teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["PlayerOne"] = "Blue" };
        string first = store.Save(image, DateTimeOffset.UtcNow, new[] { "PlayerOne", "PlayerTwo" },
            teams, trace, "1 matched", null);
        string manifest = Path.Combine(_root, "captures.json");
        string labeled = File.ReadAllText(manifest).Replace("\"expectedTeams\": null",
            "\"expectedTeams\": { \"PlayerTwo\": \"Red\" }, \"reviewNote\": \"manual label\"");
        File.WriteAllText(manifest, labeled);

        string second = store.Save(image, DateTimeOffset.UtcNow, Array.Empty<string>(),
            new Dictionary<string, string>(), new TeamOcrTrace(), "", "OCR failed");

        Assert.NotEqual(first, second);
        Assert.Equal(image, File.ReadAllBytes(first));
        using var json = JsonDocument.Parse(File.ReadAllText(manifest));
        var entries = json.RootElement;
        Assert.Equal(2, entries.GetArrayLength());
        Assert.Equal("Blue", entries[0].GetProperty("recognizedTeams").GetProperty("PlayerOne").GetString());
        Assert.Equal("PlayerTwo", entries[0].GetProperty("unmatchedRosterNames")[0].GetString());
        Assert.Equal("B PlayerOne\nB Unreadable", entries[0].GetProperty("passes")[0].GetProperty("Text").GetString());
        Assert.Equal("Red", entries[0].GetProperty("expectedTeams").GetProperty("PlayerTwo").GetString());
        Assert.Equal("manual label", entries[0].GetProperty("reviewNote").GetString());
        Assert.Equal(Path.GetFileName(second), entries[1].GetProperty("image").GetString());
        Assert.Equal("OCR failed", entries[1].GetProperty("error").GetString());
        Assert.Equal(JsonValueKind.Null, entries[1].GetProperty("expectedTeams").ValueKind);
    }

    [Theory]
    [InlineData("broken json")]
    [InlineData("null")]
    public void Save_InvalidManifestDoesNotOverwriteExistingData(string original)
    {
        Directory.CreateDirectory(_root);
        string manifest = Path.Combine(_root, "captures.json");
        File.WriteAllText(manifest, original);
        var store = new TeamOcrCaptureStore(_root);

        Assert.ThrowsAny<Exception>(() => store.Save(new byte[] { 1 }, DateTimeOffset.UtcNow,
            Array.Empty<string>(), new Dictionary<string, string>(), new TeamOcrTrace(), "", null));

        Assert.Equal(original, File.ReadAllText(manifest));
        Assert.Empty(Directory.GetFiles(_root, "*.png"));
    }

    [Fact]
    public void DevelopmentDirectory_RequiresSourceCheckout_AndIgnoresWorkingDirectory()
    {
        string output = Path.Combine(_root, "BedwarsOverlay.App", "bin", "Debug", "net8.0-windows");
        Directory.CreateDirectory(output);
        Assert.Null(TeamOcrCaptureStore.FindDevelopmentDirectory(output));
        File.WriteAllText(Path.Combine(_root, "BedwarsOverlay.slnx"), "");
        File.WriteAllText(Path.Combine(_root, "BedwarsOverlay.App", "BedwarsOverlay.App.csproj"), "");

        Assert.Equal(Path.Combine(_root, "images", "team-ocr"), TeamOcrCaptureStore.FindDevelopmentDirectory(output));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
