using System.Reflection;
using System.IO;
using BedwarsOverlay.App.Services;
using TesseractOCR.Enums;
using BedwarsOverlay.Core.Teams;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class TeamRecognitionTests
    {
        [Fact]
        public void ReadTeams_DimmedSmallTabList_RecognizesAllNineNames()
        {
            var expected = new Dictionary<string, string>
            {
                ["qxxi_"] = "Blue", ["InsaneFull"] = "Blue",
                ["Space_Child_"] = "Green", ["Kurumiqwp"] = "Green",
                ["mscocotella"] = "Green", ["shadowscrafted"] = "Green",
                ["Laitmeid"] = "Red", ["TryHardnt"] = "Red", ["TheKawaiiPink"] = "Red"
            };
            byte[] capture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dimmed-tab-list.png"));
            using var recognizer = new TeamRecognitionService(AppContext.BaseDirectory);

            var trace = new TeamOcrTrace();
            var actual = recognizer.ReadTeams(capture, expected.Keys, out string diagnostics, trace);

            Assert.Equal(expected.Count, actual.Count);
            foreach (var pair in expected) Assert.Equal(pair.Value, actual[pair.Key]);
            Assert.True(trace.Passes.Count >= 8);
            Assert.Contains(trace.Passes, pass => pass.Team == "Blue" && pass.PixelCount >= 12 && !string.IsNullOrWhiteSpace(pass.Text));
            Assert.Contains("9 matched", diagnostics);
        }

        [Theory]
        [InlineData("Green", 0x44, 0xCA, 0x44)]
        [InlineData("Green", 0x49, 0xDA, 0x49)]
        [InlineData("Blue", 0x44, 0x44, 0xCA)]
        [InlineData("Red", 0xCA, 0x44, 0x44)]
        [InlineData("Yellow", 0xCA, 0xCA, 0x44)]
        [InlineData("Aqua", 0x44, 0xCA, 0xCA)]
        [InlineData("Pink", 0xCA, 0x44, 0xCA)]
        public void CapturePalette_PreservesDimmedGlyphPixels(string expected, byte red, byte green, byte blue)
        {
            Assert.Equal(new[] { expected }, TeamPalette.Teams
                .Where(team => MatchesCaptureColor(team, red, green, blue))
                .Select(team => team.Name).ToArray());
        }

        [Theory]
        [InlineData(0x13, 0x3B, 0x13)] // Minecraft text shadow, not the foreground glyph.
        [InlineData(0x35, 0x69, 0x8B)] // Actual blue world/list background from the laptop capture.
        [InlineData(0x3C, 0x3B, 0x36)]
        public void CapturePalette_DoesNotPromoteShadowsOrBackground(byte red, byte green, byte blue)
        {
            Assert.DoesNotContain(TeamPalette.Teams, team => MatchesCaptureColor(team, red, green, blue));
        }

        private static bool MatchesCaptureColor(TeamColorDefinition team, byte red, byte green, byte blue)
        {
            return (bool)typeof(TeamRecognitionService).GetMethod("MatchesCaptureColor",
                BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { team, red, green, blue })!;
        }

        [Fact]
        public void OcrPageSegmentation_UsesSingleColumnForShortRosterNames()
        {
            FieldInfo modeField = typeof(TeamRecognitionService).GetField(
                "OcrPageSegmentation",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            Assert.Equal(PageSegMode.SingleColumn, modeField.GetValue(null));
        }

        [Fact]
        public void RosterMatcher_ResolvesOcrZeroAsShortPlayerName()
        {
            MethodInfo matcher = typeof(TeamRecognitionService).GetMethod(
                "TryMatchRosterName",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            object[] arguments = { "Y fgda", new[] { "fgd0" }, string.Empty, 0d };
            bool matched = (bool)matcher.Invoke(null, arguments)!;

            Assert.True(matched);
            Assert.Equal("fgd0", arguments[2]);
            Assert.Equal(0.75, arguments[3]);
        }

        [Theory]
        [InlineData("B DyeTypical", "Blue", true)]
        [InlineData("B DyeTypical", "Green", false)]
        [InlineData("R TryHardnt", "Red", true)]
        [InlineData("Y fgd0", "Yellow", true)]
        [InlineData("A PlayerName", "Aqua", true)]
        [InlineData("W PlayerName", "White", true)]
        [InlineData("P PlayerName", "Pink", true)]
        [InlineData("S PlayerName", "Gray", true)]
        [InlineData("G PlayerName", "Gray", false)]
        [InlineData("S PlayerName", "Green", false)]
        [InlineData("DyeTypical", "Blue", false)]
        [InlineData("R DyeTypical", "Blue", false)]
        public void TeamPrefix_MustAgreeWithColorMask(string ocrLine, string teamName, bool expected)
        {
            MethodInfo prefixMatcher = typeof(TeamRecognitionService).GetMethod(
                "HasMatchingTeamPrefix",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            Assert.Equal(expected, prefixMatcher.Invoke(null, new object[] { ocrLine, teamName }));
        }

        [Fact]
        public void TeamPalette_ContainsAllEightBedWarsColors()
        {
            Assert.Equal(8, TeamPalette.Teams.Count);
            Assert.Contains(TeamPalette.Teams, team => team.Name == "Aqua");
            Assert.Contains(TeamPalette.Teams, team => team.Name == "White");
            Assert.Contains(TeamPalette.Teams, team => team.Name == "Pink");
            Assert.Contains(TeamPalette.Teams, team => team.Name == "Gray");
        }

        [Theory]
        [InlineData("Blue", 0x55, 0x55, 0xFF)]
        [InlineData("Green", 0x55, 0xFF, 0x55)]
        [InlineData("Red", 0xFF, 0x55, 0x55)]
        [InlineData("Yellow", 0xFF, 0xFF, 0x55)]
        [InlineData("Aqua", 0x55, 0xFF, 0xFF)]
        [InlineData("White", 0xFF, 0xFF, 0xFF)]
        [InlineData("Pink", 0xFF, 0x55, 0xFF)]
        [InlineData("Gray", 0x55, 0x55, 0x55)]
        public void TeamPalette_MatchesOnlyTheExpectedTeam(string expected, byte red, byte green, byte blue)
        {
            string[] matches = TeamPalette.Teams
                .Where(team => TeamPalette.Matches(team, red, green, blue))
                .Select(team => team.Name)
                .ToArray();

            Assert.Equal(new[] { expected }, matches);
        }

        [Theory]
        [InlineData(0x3C, 0x3B, 0x36)]
        [InlineData(0x28, 0x28, 0x28)]
        public void GrayPalette_DoesNotMatchDarkPlayerListBackground(byte red, byte green, byte blue)
        {
            var gray = TeamPalette.Teams.Single(team => team.Name == "Gray");

            Assert.False(TeamPalette.Matches(gray, red, green, blue));
            Assert.False(TeamPalette.MatchesLoose(gray, red, green, blue));
        }
    }
}
