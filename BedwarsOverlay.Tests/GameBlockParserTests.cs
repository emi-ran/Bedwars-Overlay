using System;
using System.IO;
using BedwarsOverlay.Core.LogWatching;

namespace BedwarsOverlay.Tests
{
    /// <summary>
    /// Regression tests using real client log blocks. The win block wraps its team roster onto a
    /// continuation line without the "[CHAT]" prefix; the old implementation reset its state on that
    /// line, so the end of a game was never detected in real matches.
    /// </summary>
    public class GameBlockParserTests
    {
        private const string Separator = "[15:08:23] [Client thread/INFO]: [CHAT] ????????????????????????????????????????????????????????????????";
        private const string Header = "[15:08:23] [Client thread/INFO]: [CHAT]                                   Bed Wars";

        [Fact]
        public void ParseGameBlock_StartBlock_ReturnsMatchStart()
        {
            var parser = new HypixelLogParser();

            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Header).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[14:42:37] [Client thread/INFO]: [CHAT]").Kind);

            var start = parser.ParseGameBlock("[14:42:37] [Client thread/INFO]: [CHAT]      Protect your bed and destroy the enemy beds.");
            Assert.Equal(GameBlockKind.MatchStart, start.Kind);

            // The start block has a second separator right after the intro text.
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[14:42:37] [Client thread/INFO]: [CHAT]       Upgrade yourself and your team by collecting").Kind);
        }

        [Fact]
        public void ParseGameBlock_WinBlockWithWrappedRoster_ReturnsWin()
        {
            var parser = new HypixelLogParser();

            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Header).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[14:55:54] [Client thread/INFO]: [CHAT]     Red - [MVP+] AnExploit, [VIP+] TryHardnt, [MVP+] SirMidou,").Kind);

            // Continuation line: no timestamp, no [CHAT] prefix. It must not break the block.
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("                                [MVP+] J2847").Kind);

            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[14:55:54] [Client thread/INFO]: [CHAT]                      1st Killer - [MVP+] J2847 - 9").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);

            var timePlayed = parser.ParseGameBlock("[14:55:54] [Client thread/INFO]: [CHAT] +10 Slumber Tickets (Time Played)");
            Assert.Equal(GameBlockKind.MatchEnd, timePlayed.Kind);
            Assert.Equal(GameOutcome.Loss, timePlayed.Outcome);

            var win = parser.ParseGameBlock("[14:55:54] [Client thread/INFO]: [CHAT] +120 tokens! (Win)");
            Assert.Equal(GameBlockKind.MatchEnd, win.Kind);
            Assert.Equal(GameOutcome.Win, win.Outcome);
        }

        [Fact]
        public void ParseGameBlock_LossBlock_ReturnsLoss()
        {
            var parser = new HypixelLogParser();

            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Header).Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[15:08:23] [Client thread/INFO]: [CHAT]                    Yellow - _MaKo_1, ShoichiImayoshi").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[15:08:23] [Client thread/INFO]: [CHAT]                      1st Killer - [MVP+] J2847 - 9").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[15:08:23] [Client thread/INFO]: [CHAT]                    You failed the Collector challenge!").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock(Separator).Kind);

            var loss = parser.ParseGameBlock("[15:08:23] [Client thread/INFO]: [CHAT] +10 Slumber Tickets (Time Played)");
            Assert.Equal(GameBlockKind.MatchEnd, loss.Kind);
            Assert.Equal(GameOutcome.Loss, loss.Outcome);
        }

        [Fact]
        public void ParseGameBlock_RepeatedWinRewards_DoNotRepeatSameOutcome()
        {
            var parser = new HypixelLogParser();

            parser.ParseGameBlock(Separator);
            parser.ParseGameBlock(Header);
            var first = parser.ParseGameBlock("[01:29:22] [Client thread/INFO]: [CHAT] +80 tokens! (Win)");
            Assert.Equal(GameBlockKind.MatchEnd, first.Kind);
            Assert.Equal(GameOutcome.Win, first.Outcome);

            // Additional win rewards of the same block do not report the end again.
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[01:29:22] [Client thread/INFO]: [CHAT] +40 Slumber Tickets (Win)").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[01:29:22] [Client thread/INFO]: [CHAT] +10 tokens! (Win)").Kind);
        }

        [Fact]
        public void ParseGameBlock_TimePlayedOutsideBlock_ReturnsNone()
        {
            var parser = new HypixelLogParser();

            var result = parser.ParseGameBlock("[01:29:22] [Client thread/INFO]: [CHAT] +10 Slumber Tickets (Time Played)");

            Assert.Equal(GameBlockKind.None, result.Kind);
        }

        [Fact]
        public void ParseGameBlock_PerMinuteTimePlayedRewardsInsideBlock_DoNotEndMatch()
        {
            // Hypixel prints "+12 tokens! (Time Played)" and "+17 Bed Wars XP (Time Played)" every
            // minute while the game is still running. Only the Slumber Tickets variant ends a match.
            var parser = new HypixelLogParser();

            parser.ParseGameBlock(Separator);
            parser.ParseGameBlock(Header);

            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[01:24:24] [Client thread/INFO]: [CHAT] +12 tokens! (Time Played)").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[01:24:24] [Client thread/INFO]: [CHAT] +17 Bed Wars XP (Time Played)").Kind);
            Assert.Equal(GameBlockKind.None, parser.ParseGameBlock("[01:25:24] [Client thread/INFO]: [CHAT] +24 tokens! (Time Played)").Kind);
        }
    }
}
