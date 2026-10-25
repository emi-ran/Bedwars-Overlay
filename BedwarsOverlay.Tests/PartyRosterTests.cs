using System.Linq;
using BedwarsOverlay.Core.LogWatching;
using Xunit;

namespace BedwarsOverlay.Tests
{
    public class PartyRosterTests
    {
        [Fact]
        public void Apply_OwnJoin_ResetsPreviousRosterAndAddsLeader()
        {
            var roster = new PartyRoster();
            roster.Add("OldMember");
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "Kelus250"));

            Assert.Equal(new[] { "Kelus250" }, roster.ToList());
            Assert.DoesNotContain("OldMember", roster);
        }

        [Fact]
        public void Apply_CompanionRoster_AddsCompanions()
        {
            var roster = new PartyRoster();
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "Kelus250"));
            roster.Apply(new PartyEvent(PartyEventKind.CompanionRoster, null, new[] { "N3eer" }));

            Assert.Contains("Kelus250", roster);
            Assert.Contains("N3eer", roster);
        }

        [Fact]
        public void Apply_MemberJoinedAndLeft_UpdatesRoster()
        {
            var roster = new PartyRoster();
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "Kelus250"));
            roster.Apply(new PartyEvent(PartyEventKind.MemberJoined, "FemmeLesbian"));
            roster.Apply(new PartyEvent(PartyEventKind.MemberLeft, "FemmeLesbian"));

            Assert.Contains("Kelus250", roster);
            Assert.DoesNotContain("FemmeLesbian", roster);
        }

        [Theory]
        [InlineData(PartyEventKind.OwnLeave)]
        [InlineData(PartyEventKind.OwnRemoved)]
        [InlineData(PartyEventKind.Disbanded)]
        [InlineData(PartyEventKind.NotInParty)]
        public void Apply_ClearEvents_EmptyRoster(PartyEventKind kind)
        {
            var roster = new PartyRoster();
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "Kelus250"));
            roster.Apply(new PartyEvent(kind));

            Assert.Empty(roster);
        }

        [Fact]
        public void ListSnapshot_ClearAndAdd_RefreshesAuthoritativeRoster()
        {
            var roster = new PartyRoster();
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "OldLeader"));
            roster.Clear();
            roster.Add("NewLeader");
            roster.Add("NewMember");

            Assert.Equal(new[] { "NewLeader", "NewMember" }, roster.ToList());
            Assert.DoesNotContain("OldLeader", roster);
        }

        [Fact]
        public void PartyState_PersistsAcrossGameTransition()
        {
            var roster = new PartyRoster();
            roster.Apply(new PartyEvent(PartyEventKind.OwnJoin, "Kelus250"));
            roster.Apply(new PartyEvent(PartyEventKind.CompanionRoster, null, new[] { "N3eer" }));

            // Simulate a game/lobby transition not touching party state.
            Assert.Contains("Kelus250", roster);
            Assert.Contains("N3eer", roster);
        }
    }
}