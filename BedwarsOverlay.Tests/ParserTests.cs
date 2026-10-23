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
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats ab too_long_username_over_sixteen_chars')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats asdasd')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('getstats asdasd')")]
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

        [Theory]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] Party Leader: [MVP++] Nargle3 ?", new[] { "Nargle3" })]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] Party Members: [VIP] Studying ? [VIP+] TryHardnt ? [VIP+] atseng ?", new[] { "Studying", "TryHardnt", "atseng" })]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] §9Party Members: §a[VIP] Studying ? §b[VIP+] TryHardnt ?", new[] { "Studying", "TryHardnt" })]
        [InlineData("[02:08:39] [Client thread/INFO]: [CHAT] Party Moderators: [VIP+] Colorado17 ? ", new[] { "Colorado17" })]
        [InlineData("[02:08:39] [Client thread/INFO]: [CHAT] Party Moderators: [MVP+] One ? [VIP] Two ?", new[] { "One", "Two" })]
        [InlineData("[02:08:39] [Client thread/INFO]: [CHAT] §9Party Moderators: §b[VIP+] Colorado17 ? ", new[] { "Colorado17" })]
        public void ParsePartyPlayers_ValidLines_ExtractsPlayers(string line, string[] expectedPlayers)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParsePartyPlayers(line);

            Assert.NotNull(players);
            Assert.Equal(expectedPlayers, players);
        }

        [Fact]
        public void ParsePartyPlayers_LeaderAndModeratorsTogether_CollectAllPartyRoles()
        {
            var parser = new HypixelLogParser();
            var captured = new List<string>();
            string[] lines =
            {
                "[02:08:39] [Client thread/INFO]: [CHAT] Party Leader: [VIP+] TryHardnt ?",
                "[02:08:39] [Client thread/INFO]: [CHAT] Party Moderators: [VIP+] Colorado17 ? "
            };

            foreach (string line in lines)
            {
                captured.AddRange(parser.ParsePartyPlayers(line) ?? new List<string>());
            }

            Assert.Equal(new[] { "TryHardnt", "Colorado17" }, captured);
        }

        [Theory]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] Party Members (4)")]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] -----------------------------------------------------")]
        [InlineData("[01:52:13] [Client thread/INFO]: [CHAT] Guild Members: Notch")]
        public void ParsePartyPlayers_InvalidLines_ReturnsNull(string line)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParsePartyPlayers(line);

            Assert.Null(players);
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore asdasd')", new[] { "asdasd" })]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore player1 player2 player3')", new[] { "player1", "player2", "player3" })]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore player1, player2, player3')", new[] { "player1", "player2", "player3" })]
        public void ParseIgnoredPlayers_ValidLines_ExtractsPlayers(string line, string[] expectedPlayers)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParseIgnoredPlayers(line);

            Assert.NotNull(players);
            Assert.Equal(expectedPlayers, players);
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore ab too_long_username_over_sixteen_chars')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats asdasd')")]
        public void ParseIgnoredPlayers_InvalidLines_ReturnsNull(string line)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParseIgnoredPlayers(line);

            Assert.Null(players);
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats asdasd')", new[] { "asdasd" })]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats player1 player2 player3')", new[] { "player1", "player2", "player3" })]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats player1, player2, player3')", new[] { "player1", "player2", "player3" })]
        public void ParseStatsCommandPlayers_ValidLines_ExtractsPlayers(string line, string[] expectedPlayers)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParseStatsCommandPlayers(line);

            Assert.NotNull(players);
            Assert.Equal(expectedPlayers, players);
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats ab too_long_username_over_sixteen_chars')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwignore asdasd')")]
        public void ParseStatsCommandPlayers_InvalidLines_ReturnsNull(string line)
        {
            var parser = new HypixelLogParser();
            var players = parser.ParseStatsCommandPlayers(line);

            Assert.Null(players);
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwhelp')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] §cUnknown command. Type \"help\" for help. ('bwhelp')")]
        public void IsHelpCommand_WhenBwHelpEcho_ReturnsTrue(string line)
        {
            var parser = new HypixelLogParser();

            Assert.True(parser.IsHelpCommand(line));
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats asdasd')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwhelp player1')")]
        [InlineData("Some random log line")]
        public void IsHelpCommand_WhenNotExactBwHelpEcho_ReturnsFalse(string line)
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsHelpCommand(line));
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('pstats')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] §cUnknown command. Type \"help\" for help. ('pstats')")]
        public void IsPartyStatsCommand_WhenPStatsEcho_ReturnsTrue(string line)
        {
            var parser = new HypixelLogParser();

            Assert.True(parser.IsPartyStatsCommand(line));
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('pstats extra')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwstats player1')")]
        [InlineData("Some random log line")]
        public void IsPartyStatsCommand_WhenNotExactPStatsEcho_ReturnsFalse(string line)
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsPartyStatsCommand(line));
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwclear')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] §cUnknown command. Type \"help\" for help. ('bwclear')")]
        public void IsClearCommand_WhenBwClearEcho_ReturnsTrue(string line)
        {
            var parser = new HypixelLogParser();

            Assert.True(parser.IsClearCommand(line));
        }

        [Theory]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwhelp')")]
        [InlineData("[01:48:47] [Client thread/INFO]: [CHAT] Unknown command. Type \"help\" for help. ('bwclear player1')")]
        [InlineData("Some random log line")]
        public void IsClearCommand_WhenNotExactBwClearEcho_ReturnsFalse(string line)
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsClearCommand(line));
        }

        [Fact]
        public void IsPartyListHeader_WhenPartyMembersCountLine_ReturnsTrue()
        {
            var parser = new HypixelLogParser();

            Assert.True(parser.IsPartyListHeader("[01:52:13] [Client thread/INFO]: [CHAT] Party Members (4)"));
            Assert.False(parser.IsPartyListHeader("[01:52:13] [Client thread/INFO]: [CHAT] Party Members: Notch"));
        }

        [Fact]
        public void IsGameEndSummary_WhenBedWarsSummaryAndWinReward_ReturnsTrue()
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT] ????????????????????????????????????????????????????????????????"));
            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT]                                   Bed Wars"));
            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT]      Blue - FakeSlime, [VIP] Studying"));
            Assert.True(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT] +80 tokens! (Win)"));
        }

        [Fact]
        public void IsGameEndSummary_WhenWinRewardWithoutSummary_ReturnsFalse()
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT] +60 tokens! (Win)"));
            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT] +40 Slumber Tickets (Win)"));
        }

        [Fact]
        public void IsGameEndSummary_WhenBedWarsHeaderWithoutSeparator_ReturnsFalse()
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT]                                   Bed Wars"));
            Assert.False(parser.IsGameEndSummary("[01:29:22] [Client thread/INFO]: [CHAT] +60 tokens! (Win)"));
        }

        [Theory]
        [InlineData("[01:29:22] [Client thread/INFO]: [CHAT] TEAM ELIMINATED > Green Team has been eliminated!")]
        [InlineData("[01:29:22] [Client thread/INFO]: [CHAT] +10 Slumber Tickets (Time Played)")]
        [InlineData("[01:29:22] [Client thread/INFO]: [CHAT] Player: Bed Wars +60 tokens! (Win)")]
        public void IsGameEndSummary_WhenNotEndSummary_ReturnsFalse(string line)
        {
            var parser = new HypixelLogParser();

            Assert.False(parser.IsGameEndSummary(line));
        }
    }
}
