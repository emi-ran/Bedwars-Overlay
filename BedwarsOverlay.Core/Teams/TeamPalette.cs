using System;
using System.Collections.Generic;

namespace BedwarsOverlay.Core.Teams
{
    /// <summary>
    /// One Bed Wars team color as Minecraft renders it in the player list. The values are the exact
    /// chat colors from the Minecraft palette, for example <c>§c</c> red (FF5555).
    /// </summary>
    public sealed class TeamColorDefinition
    {
        internal TeamColorDefinition(string name, byte red, byte green, byte blue, int tolerance)
        {
            Name = name;
            Red = red;
            Green = green;
            Blue = blue;
            Tolerance = tolerance;
        }

        public string Name { get; }

        public byte Red { get; }

        public byte Green { get; }

        public byte Blue { get; }

        /// <summary>Per-channel tolerance used when comparing a captured pixel with <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>.</summary>
        public int Tolerance { get; }
    }

    /// <summary>
    /// The eight Bed Wars team colors. 4v4v4v4 only uses Red/Blue/Green/Yellow, while Solos and
    /// Doubles also use Aqua, White, Pink and Gray, so a palette that stops at four teams leaves
    /// half of a Solos/Doubles player list unrecognized.
    /// </summary>
    public static class TeamPalette
    {
        /// <summary>
        /// Minecraft draws the player list on a dark translucent background (roughly 3C3B36).
        /// Saturated team colors are far away from it, so they get a forgiving tolerance against
        /// client color shifts.
        /// </summary>
        public const int WideTolerance = 36;

        /// <summary>
        /// <c>§8</c> dark gray is close to that player-list background (about 25 per channel away),
        /// so it needs a tight tolerance or the whole list background becomes part of its mask.
        /// </summary>
        public const int DarkTolerance = 10;

        public static IReadOnlyList<TeamColorDefinition> Teams { get; } = new[]
        {
            new TeamColorDefinition("Blue", 0x55, 0x55, 0xFF, WideTolerance),   // §9
            new TeamColorDefinition("Green", 0x55, 0xFF, 0x55, WideTolerance),  // §a
            new TeamColorDefinition("Red", 0xFF, 0x55, 0x55, WideTolerance),    // §c
            new TeamColorDefinition("Yellow", 0xFF, 0xFF, 0x55, WideTolerance), // §e
            new TeamColorDefinition("Aqua", 0x55, 0xFF, 0xFF, WideTolerance),   // §b
            new TeamColorDefinition("White", 0xFF, 0xFF, 0xFF, WideTolerance),  // §f
            new TeamColorDefinition("Pink", 0xFF, 0x55, 0xFF, WideTolerance),   // §d
            new TeamColorDefinition("Gray", 0x55, 0x55, 0x55, DarkTolerance)    // §8
        };

        /// <summary>Exact palette test: is this pixel drawn in <paramref name="team"/>'s color?</summary>
        public static bool Matches(TeamColorDefinition team, byte red, byte green, byte blue)
        {
            return Math.Abs(red - team.Red) <= team.Tolerance
                && Math.Abs(green - team.Green) <= team.Tolerance
                && Math.Abs(blue - team.Blue) <= team.Tolerance;
        }

        /// <summary>
        /// Ratio-based fallback for clients that do not render the exact Minecraft palette. Gray is
        /// excluded on purpose: every dark, unsaturated pixel of the list background looks like it.
        /// </summary>
        public static bool MatchesLoose(TeamColorDefinition team, byte red, byte green, byte blue)
        {
            return team.Name switch
            {
                "Blue" => blue > 130 && blue > red * 1.35 && blue > green * 1.2,
                "Green" => green > 130 && green > red * 1.25 && green > blue * 1.2,
                "Red" => red > 150 && red > green * 1.3 && red > blue * 1.3,
                "Yellow" => red > 150 && green > 150 && blue < 140 && Math.Abs(red - green) < 100,
                "Aqua" => green > 150 && blue > 150 && red < 140 && Math.Abs(green - blue) < 110,
                "Pink" => red > 150 && blue > 150 && green < 140 && Math.Abs(red - blue) < 110,
                "White" => red > 225 && green > 225 && blue > 225,
                _ => false
            };
        }
    }
}
