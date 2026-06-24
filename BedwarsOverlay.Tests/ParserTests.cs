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

        [Theory]
        [InlineData("[03:37:51] [Client thread/INFO]: [CHAT] big_can was knocked into the void by binibirocha. FINAL KILL!", "big_can")]
        [InlineData("[03:38:04] [Client thread/INFO]: [CHAT] Jotinha1447 was bzzz'd into the void by Scottyy_. FINAL KILL!", "Jotinha1447")]
        [InlineData("[03:38:14] [Client thread/INFO]: [CHAT] Veqhz was too shy to meet TryHardnt. FINAL KILL!", "Veqhz")]
        [InlineData("[03:38:14] [Client thread/INFO]: [CHAT] §cVeqhz §rwas too shy to meet TryHardnt. §bFINAL KILL!", "Veqhz")]
        public void ParseFinalKillVictim_ValidLines_ExtractsVictim(string line, string expectedVictim)
        {
            var parser = new HypixelLogParser();
            var victim = parser.ParseFinalKillVictim(line);

            Assert.Equal(expectedVictim, victim);
        }

        [Theory]
        [InlineData("[03:37:51] [Client thread/INFO]: [CHAT] big_can was knocked into the void by binibirocha.")]
        [InlineData("[03:37:51] [Client thread/INFO]: [CHAT] ab was knocked into the void by binibirocha. FINAL KILL!")]
        [InlineData("Some random log line")]
        public void ParseFinalKillVictim_InvalidLines_ReturnsNull(string line)
        {
            var parser = new HypixelLogParser();
            var victim = parser.ParseFinalKillVictim(line);

            Assert.Null(victim);
        }
    }
}
