using System.Reflection;
using BedwarsOverlay.App.Services;
using TesseractOCR.Enums;
using BedwarsOverlay.Core.Teams;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class TeamRecognitionTests
    {
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
