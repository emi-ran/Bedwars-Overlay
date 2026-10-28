using BedwarsOverlay.Core.Teams;

namespace BedwarsOverlay.Tests;

public class TeammateResolverTests
{
    private static Dictionary<string, string> Teams(params (string Name, string Team)[] entries) =>
        entries.ToDictionary(entry => entry.Name, entry => entry.Team, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void NickedRosterName_OnLocalTeam_IsTeammateWithoutPartyNameMatch()
    {
        var teams = Teams(("TryHardnt", "Green"), ("EvilAndPsycho", "Green"), ("NorthAlaska", "Red"));
        string[] party = { "daphirium", "BritishCuisine", "TryHardnt", "harmlessTwinkie" };

        Assert.True(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt", party, teams));
        Assert.False(TeammateResolver.IsTeammate("NorthAlaska", "TryHardnt", party, teams));
        Assert.False(TeammateResolver.IsTeammate("TryHardnt", "TryHardnt", party, teams));
    }

    [Fact]
    public void MissingLocalTeam_UsesConsistentPartyTeam()
    {
        var teams = Teams(("BritishCuisine", "Green"), ("harmlessTwinkie", "Green"), ("EvilAndPsycho", "Green"));
        Assert.True(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt",
            new[] { "BritishCuisine", "harmlessTwinkie" }, teams));
    }

    [Fact]
    public void ConflictingPartyTeams_DoNotInferNickedTeammate()
    {
        var teams = Teams(("BritishCuisine", "Green"), ("harmlessTwinkie", "Red"), ("EvilAndPsycho", "Green"));
        Assert.False(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt",
            new[] { "BritishCuisine", "harmlessTwinkie" }, teams));
    }

    [Fact]
    public void LocalTeam_TakesPriorityOverPartyTeams()
    {
        var teams = Teams(("TryHardnt", "Red"), ("BritishCuisine", "Green"), ("EvilAndPsycho", "Green"));
        Assert.False(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt", new[] { "BritishCuisine" }, teams));
        Assert.False(TeammateResolver.IsTeammate("BritishCuisine", "TryHardnt", new[] { "BritishCuisine" }, teams));
    }

    [Theory]
    [InlineData("TryHardnt")]
    [InlineData("")]
    public void NoTeamEvidence_OnlyKnownPartyNamesMatch(string localName)
    {
        Assert.True(TeammateResolver.IsTeammate("BritishCuisine", localName, new[] { "BritishCuisine" }, Teams()));
        Assert.False(TeammateResolver.IsTeammate("EvilAndPsycho", localName, new[] { "BritishCuisine" }, Teams()));
    }

    [Fact]
    public void Matching_IsCaseInsensitive_AndTrimsLocalSetting()
    {
        var teams = new Dictionary<string, string> { ["TryHardnt"] = "Green", ["EvilAndPsycho"] = "green" };
        Assert.True(TeammateResolver.IsTeammate("evilandpsycho", " tryhardnt ", Array.Empty<string>(), teams));
    }

    [Fact]
    public void ClearedTeamEvidence_DoesNotKeepPreviousAlias()
    {
        var teams = Teams(("TryHardnt", "Green"), ("EvilAndPsycho", "Green"));
        Assert.True(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt", Array.Empty<string>(), teams));
        teams.Clear();
        Assert.False(TeammateResolver.IsTeammate("EvilAndPsycho", "TryHardnt", Array.Empty<string>(), teams));
    }

    [Fact]
    public void UnknownVictimTeam_PreservesPartyFallback()
    {
        Assert.True(TeammateResolver.IsTeammate("BritishCuisine", "TryHardnt", new[] { "BritishCuisine" },
            Teams(("TryHardnt", "Green"))));
    }
}
