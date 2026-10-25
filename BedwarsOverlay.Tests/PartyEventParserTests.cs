using System;
using System.Linq;
using BedwarsOverlay.Core.LogWatching;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class PartyEventParserTests
    {
        [Fact]
        public void ParsePartyEvent_OwnJoin_ReturnsOwnJoin()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[22:59:24] [Client thread/INFO]: [CHAT] You have joined [MVP+] kelus250's party!");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.OwnJoin, result!.Kind);
            Assert.Equal("kelus250", result.PlayerName);
        }

        [Fact]
        public void ParsePartyEvent_Companions_ParsesCommaSeparatedRanks()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[22:20:44] [Client thread/INFO]: [CHAT] You'll be partying with: [MVP++] iNoobyy, [VIP+] Laitmeid");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.CompanionRoster, result!.Kind);
            Assert.Equal(new[] { "iNoobyy", "Laitmeid" }, result.PlayerNames);
        }

        [Fact]
        public void ParsePartyEvent_Companions_DoesNotTreatAndAsUsername()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[12:29:23] [Client thread/INFO]: [CHAT] You'll be partying with: [MVP++] iNoobyy, [VIP] Q01_ and [MVP] Nobody");
            Assert.NotNull(result);
            Assert.DoesNotContain("and", result!.PlayerNames!, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("iNoobyy", result.PlayerNames!);
            Assert.Contains("Q01_", result.PlayerNames!);
            Assert.Contains("Nobody", result.PlayerNames!);
        }

        [Fact]
        public void ParsePartyEvent_MemberJoined_ParsesRankedMember()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[23:01:32] [Client thread/INFO]: [CHAT] [MVP+] FemmeLesbian joined the party.");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.MemberJoined, result!.Kind);
            Assert.Equal("FemmeLesbian", result.PlayerName);
        }

        [Fact]
        public void ParsePartyEvent_MemberLeft_ParsesRemovedMember()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[23:01:44] [Client thread/INFO]: [CHAT] [VIP] Cornibus1 has been removed from the party.");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.MemberLeft, result!.Kind);
            Assert.Equal("Cornibus1", result.PlayerName);
        }

        [Theory]
        [InlineData("[23:01:08] [Client thread/INFO]: [CHAT] You left the party.")]
        [InlineData("[22:53:58] [Client thread/INFO]: [CHAT] You left the party.")]
        public void ParsePartyEvent_OwnLeave_Parses(string line)
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent(line);
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.OwnLeave, result!.Kind);
        }

        [Fact]
        public void ParsePartyEvent_Disbanded_Parses()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[12:42:18] [Client thread/INFO]: [CHAT] The party was disbanded because all invites expired and the party was empty.");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.Disbanded, result!.Kind);
        }

        [Fact]
        public void ParsePartyEvent_NotInParty_Parses()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[22:59:01] [Client thread/INFO]: [CHAT] You are not currently in a party.");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.NotInParty, result!.Kind);
        }

        [Theory]
        [InlineData("[22:59:48] [Client thread/INFO]: [CHAT] [MVP+] kelus250 invited punk_rackss to the party! They have 60 seconds to accept.")]
        [InlineData("[22:59:19] [Client thread/INFO]: [CHAT] You have 60 seconds to accept. Click here to join!")]
        [InlineData("[MVP+] kelus250 has invited you to join their party!")]
        [InlineData("[22:59:08] [Client thread/INFO]: [CHAT] ?7[13?]?r ?6[MVP?c++?6] Fisse?f: Bros in a mm party")]
        public void ParsePartyEvent_InvitesAndChat_ReturnNull(string line)
        {
            var parser = new HypixelLogParser();
            Assert.Null(parser.ParsePartyEvent(line));
        }

        [Fact]
        public void ParsePartyEvent_LeaderDisbandedSynthetic_Parses()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[23:00:00] [Client thread/INFO]: [CHAT] [MVP+] kelus250 has disbanded the party!");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.Disbanded, result!.Kind);
        }

        [Fact]
        public void ParsePartyEvent_OwnRemovedSynthetic_Parses()
        {
            var parser = new HypixelLogParser();
            var result = parser.ParsePartyEvent("[23:00:00] [Client thread/INFO]: [CHAT] You have been kicked from the party by [MVP+] kelus250.");
            Assert.NotNull(result);
            Assert.Equal(PartyEventKind.OwnRemoved, result!.Kind);
        }
    }
}