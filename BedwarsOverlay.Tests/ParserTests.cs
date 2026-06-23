using System.Collections.Generic;
using BedwarsOverlay.Core.LogWatching;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("[18:42:15] [Client thread/INFO]: [CHAT] ONLINE: Notch, player2, player_3", new[] { "Notch", "player2", "player_3" })]
        [InlineData("[Client thread/INFO]: [CHAT] Online players (3): Notch, player2, player_3", new[] { "Notch", "player2", "player_3" })]
        [InlineData("ONLINE: Notch, player2, player_3", new[] { "Notch", "player2", "player_3" })]
        [InlineData("[CHAT] ONLINE: Notch", new[] { "Notch" })]
        [InlineData("[18:42:15] [Client thread/INFO]: [CHAT] §aONLINE: §r§7Notch§r§7, §r§7player2§r§7, §r§7player_3§r", new[] { "Notch", "player2", "player_3" })]
        public void ParsePlayerList_ValidLines_ExtractsCorrectPlayers(string line, string[] expectedPlayers)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParsePlayerList(line);

            Assert.NotNull(players);
            Assert.Equal(expectedPlayers.Length, players.Count);
            for (int i = 0; i < expectedPlayers.Length; i++)
            {
                Assert.Equal(expectedPlayers[i], players[i]);
            }
        }

        [Theory]
        [InlineData("[18:42:15] [Client thread/INFO]: [CHAT] player1 joined the lobby.")]
        [InlineData("[CHAT] ONLINE: ")]
        [InlineData("Some random log line")]
        [InlineData("[CHAT] ONLINE: ab, too_long_username_over_sixteen_chars")] // invalid lengths
        public void ParsePlayerList_InvalidLines_ReturnsNull(string line)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParsePlayerList(line);

            Assert.Null(players);
        }
    }
}
