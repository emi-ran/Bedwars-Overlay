using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TesseractOCR;
using TesseractOCR.Enums;
using PixImage = TesseractOCR.Pix.Image;

namespace BedwarsOverlay.App.Services
{
    /// <summary>Reads Hypixel team-colored player names from a captured Minecraft player list.</summary>
    public sealed class TeamRecognitionService : IDisposable
    {
        private static readonly (string Name, byte Red, byte Green, byte Blue)[] Teams =
        {
            ("Blue", 0x55, 0x55, 0xFF),   // Minecraft dark blue (§9)
            ("Green", 0x55, 0xFF, 0x55),  // Minecraft green (§a)
            ("Red", 0xFF, 0x55, 0x55),    // Minecraft red (§c)
            ("Yellow", 0xFF, 0xFF, 0x55)  // Minecraft yellow (§e)
        };

        /// <summary>
        /// Minecraft renders player-list names in exact team colors, while the world behind the list is
        /// blended, shaded, or textured. A strict per-channel tolerance keeps the painted world out of
        /// the OCR masks, which is what used to break captures.
        /// </summary>
        private const int TeamColorTolerance = 36;

        /// <summary>Below this many text pixels a team color is treated as not present in the capture.</summary>
        private const int MinimumTeamPixels = 12;

        private readonly string _tessdataPath;
        private readonly object _engineLock = new();
        private Engine? _engine;

        public TeamRecognitionService(string applicationDirectory)
        {
            if (string.IsNullOrWhiteSpace(applicationDirectory))
            {
                throw new ArgumentException("Application directory must not be empty.", nameof(applicationDirectory));
            }

            _tessdataPath = Path.Combine(applicationDirectory, "Assets", "tessdata");
        }

        public IReadOnlyDictionary<string, string> ReadTeams(byte[] screenshot, IEnumerable<string> rosterNames)
        {
            return ReadTeams(screenshot, rosterNames, out _);
        }

        /// <summary>
        /// Reads team-colored names from a captured player list.
        /// </summary>
        /// <param name="diagnostics">Per-color pixel counts and match counts, for the diagnostics log.</param>
        public IReadOnlyDictionary<string, string> ReadTeams(
            byte[] screenshot,
            IEnumerable<string> rosterNames,
            out string diagnostics)
        {
            var teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            diagnostics = string.Empty;
            if (screenshot == null || screenshot.Length == 0 || rosterNames == null)
            {
                return teams;
            }

            string[] roster = rosterNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (roster.Length == 0)
            {
                return teams;
            }

            using var stream = new MemoryStream(screenshot);
            using var source = new Bitmap(stream);
            Rectangle captureArea = GetPlayerListArea(source.Width, source.Height);
            Bitmap[] masks = CreateColorMasks(source, captureArea, out int[] colorCounts);

            try
            {
                Engine engine = GetEngine();

                // Each team color gets its own OCR pass. Merging every color into one image threw the
                // color away and forced the team to be guessed from the characters around a matched
                // name, so the wrong team (or none) was reported. Reading each color separately makes
                // the team a property of the mask that produced the match.
                var candidates = new Dictionary<string, (string Team, double Score)>(StringComparer.OrdinalIgnoreCase);
                for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
                {
                    if (colorCounts[teamIndex] < MinimumTeamPixels)
                    {
                        continue;
                    }

                    using var image = PixImage.LoadFromMemory(EncodeForOcr(masks[teamIndex]));
                    using var page = engine.Process(image, PageSegMode.SparseText);

                    foreach (string line in page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!TryMatchRosterName(line, roster, out string playerName, out double score))
                        {
                            continue;
                        }

                        if (candidates.TryGetValue(playerName, out var existing) && existing.Score >= score)
                        {
                            continue;
                        }

                        candidates[playerName] = (Teams[teamIndex].Name, score);
                    }
                }

                foreach (var candidate in candidates)
                {
                    teams[candidate.Key] = candidate.Value.Team;
                }

                diagnostics = string.Join(
                    ", ",
                    Enumerable.Range(0, Teams.Length).Select(i => $"{Teams[i].Name} {colorCounts[i]}px"));
                diagnostics += $" -> {teams.Count} matched";
            }
            finally
            {
                foreach (Bitmap teamMask in masks)
                {
                    teamMask.Dispose();
                }
            }

            return teams;
        }

        private Engine GetEngine()
        {
            lock (_engineLock)
            {
                if (_engine == null)
                {
                    _engine = new Engine(_tessdataPath, Language.English, EngineMode.LstmOnly);
                    _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_ ");
                    _engine.SetVariable("preserve_interword_spaces", true);
                }

                return _engine;
            }
        }

        public void Dispose()
        {
            lock (_engineLock)
            {
                _engine?.Dispose();
                _engine = null;
            }
        }

        private static Rectangle GetPlayerListArea(int width, int height)
        {
            // Minecraft centers the player list, so sample a wide middle band. The outer edges only
            // hold world/chat pixels, and their colored blocks drowned the color masks.
            int left = (int)(width * 0.14);
            int top = (int)(height * 0.01);
            int right = (int)(width * 0.86);
            int bottom = (int)(height * 0.84);
            return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
        }

        private static Bitmap[] CreateColorMasks(Bitmap source, Rectangle area, out int[] coloredPixelCounts)
        {
            int width = area.Width;
            int height = area.Height;
            int rowBytes = width * 4;
            var sourcePixels = new byte[rowBytes * height];

            using (var normalized = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(normalized))
                {
                    graphics.DrawImage(source, new Rectangle(0, 0, width, height), area, GraphicsUnit.Pixel);
                }

                var bounds = new Rectangle(0, 0, width, height);
                BitmapData data = normalized.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), sourcePixels, y * rowBytes, rowBytes);
                    }
                }
                finally
                {
                    normalized.UnlockBits(data);
                }
            }

            var hitsByTeam = ClassifyTeamPixels(sourcePixels, width, height, exactColors: true);
            if (CountTeamPixels(hitsByTeam) < MinimumTeamPixels)
            {
                // Shaders and client color effects can shift the rendered text away from the exact
                // Minecraft palette; the looser color test keeps team capture working there.
                hitsByTeam = ClassifyTeamPixels(sourcePixels, width, height, exactColors: false);
            }

            coloredPixelCounts = new int[Teams.Length];
            var masks = new Bitmap[Teams.Length];
            for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
            {
                bool[] textPixels = KeepTextComponents(hitsByTeam[teamIndex], width, height);
                masks[teamIndex] = CreateMaskBitmap(textPixels, width, height, out coloredPixelCounts[teamIndex]);
            }

            return masks;
        }

        private static bool[][] ClassifyTeamPixels(byte[] sourcePixels, int width, int height, bool exactColors)
        {
            var hitsByTeam = new bool[Teams.Length][];
            for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
            {
                hitsByTeam[teamIndex] = new bool[width * height];
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;
                    int offset = pixelIndex * 4;
                    byte blue = sourcePixels[offset];
                    byte green = sourcePixels[offset + 1];
                    byte red = sourcePixels[offset + 2];
                    for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
                    {
                        bool match = exactColors
                            ? MatchesTeamColor(red, green, blue, teamIndex)
                            : MatchesTeamColorLoose(red, green, blue, teamIndex);
                        if (match)
                        {
                            hitsByTeam[teamIndex][pixelIndex] = true;
                        }
                    }
                }
            }

            return hitsByTeam;
        }

        private static int CountTeamPixels(bool[][] hitsByTeam)
        {
            int total = 0;
            foreach (bool[] hits in hitsByTeam)
            {
                foreach (bool hit in hits)
                {
                    if (hit)
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Keeps only the pixel blobs that can be a glyph of the player list. Team colors also appear
        /// in the world behind the list (wool, terracotta, sky, lava), and those large blobs used to
        /// dominate the OCR input so a capture sometimes produced no usable text at all.
        /// </summary>
        private static bool[] KeepTextComponents(bool[] hits, int width, int height)
        {
            int maxGlyphHeight = Math.Clamp((int)Math.Round(height * 0.055), 14, 44);
            int maxGlyphWidth = maxGlyphHeight * 4;

            var kept = new bool[hits.Length];
            var visited = new bool[hits.Length];
            var stack = new int[hits.Length];
            var component = new List<int>();

            for (int start = 0; start < hits.Length; start++)
            {
                if (!hits[start] || visited[start])
                {
                    continue;
                }

                component.Clear();
                int stackSize = 0;
                stack[stackSize++] = start;
                visited[start] = true;
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;

                while (stackSize > 0)
                {
                    int index = stack[--stackSize];
                    component.Add(index);
                    int x = index % width;
                    int y = index / width;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborY = y + offsetY;
                        if (neighborY < 0 || neighborY >= height)
                        {
                            continue;
                        }

                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            int neighborX = x + offsetX;
                            if (neighborX < 0 || neighborX >= width)
                            {
                                continue;
                            }

                            int neighborIndex = neighborY * width + neighborX;
                            if (!hits[neighborIndex] || visited[neighborIndex])
                            {
                                continue;
                            }

                            visited[neighborIndex] = true;
                            stack[stackSize++] = neighborIndex;
                        }
                    }
                }

                int componentWidth = maxX - minX + 1;
                int componentHeight = maxY - minY + 1;
                if (component.Count < 3 || componentHeight > maxGlyphHeight || componentWidth > maxGlyphWidth)
                {
                    continue;
                }

                foreach (int index in component)
                {
                    kept[index] = true;
                }
            }

            return kept;
        }

        private static Bitmap CreateMaskBitmap(bool[] pixels, int width, int height, out int keptCount)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            keptCount = 0;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (!pixels[rowOffset + x])
                    {
                        continue;
                    }

                    keptCount++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (keptCount == 0)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            const int padding = 4;
            int cropLeft = Math.Max(0, minX - padding);
            int cropTop = Math.Max(0, minY - padding);
            int cropRight = Math.Min(width, maxX + padding + 1);
            int cropBottom = Math.Min(height, maxY + padding + 1);
            int cropWidth = cropRight - cropLeft;
            int cropHeight = cropBottom - cropTop;

            var mask = new Bitmap(cropWidth, cropHeight, PixelFormat.Format32bppArgb);
            BitmapData data = mask.LockBits(
                new Rectangle(0, 0, cropWidth, cropHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                var row = new byte[cropWidth * 4];
                for (int y = 0; y < cropHeight; y++)
                {
                    Array.Fill(row, byte.MaxValue);
                    int sourceRowOffset = (cropTop + y) * width + cropLeft;
                    for (int x = 0; x < cropWidth; x++)
                    {
                        if (!pixels[sourceRowOffset + x])
                        {
                            continue;
                        }

                        int offset = x * 4;
                        row[offset] = 0;
                        row[offset + 1] = 0;
                        row[offset + 2] = 0;
                    }

                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), row.Length);
                }
            }
            finally
            {
                mask.UnlockBits(data);
            }

            return mask;
        }

        private static bool MatchesTeamColor(byte red, byte green, byte blue, int teamIndex)
        {
            var team = Teams[teamIndex];
            return Math.Abs(red - team.Red) <= TeamColorTolerance
                && Math.Abs(green - team.Green) <= TeamColorTolerance
                && Math.Abs(blue - team.Blue) <= TeamColorTolerance;
        }

        /// <summary>Ratio-based fallback for clients that do not render the exact Minecraft palette.</summary>
        private static bool MatchesTeamColorLoose(byte red, byte green, byte blue, int teamIndex)
        {
            return teamIndex switch
            {
                0 => blue > 130 && blue > red * 1.35 && blue > green * 1.2,
                1 => green > 130 && green > red * 1.25 && green > blue * 1.2,
                2 => red > 150 && red > green * 1.3 && red > blue * 1.3,
                3 => red > 150 && green > 150 && blue < 140 && Math.Abs(red - green) < 100,
                _ => false
            };
        }

        private static bool TryMatchRosterName(
            string ocrLine,
            IReadOnlyList<string> roster,
            out string matchedName,
            out double matchedScore)
        {
            matchedName = string.Empty;
            matchedScore = 0;

            string compactLine = Regex.Replace(ocrLine.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
            if (compactLine.Length == 0)
            {
                return false;
            }

            double secondBestScore = 0;
            foreach (string rosterName in roster)
            {
                string normalizedName = Regex.Replace(rosterName.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
                double score = BestSubstringSimilarity(compactLine, normalizedName, out _);

                if (score > matchedScore)
                {
                    secondBestScore = matchedScore;
                    matchedScore = score;
                    matchedName = rosterName;
                }
                else if (score > secondBestScore)
                {
                    secondBestScore = score;
                }
            }

            // Avoid guessing when OCR is too weak or two roster names look equally close.
            return matchedScore >= 0.70 && matchedScore - secondBestScore >= 0.08;
        }

        private static double BestSubstringSimilarity(string ocrLine, string rosterName, out int bestStart)
        {
            int maxNoise = Math.Min(4, Math.Max(1, rosterName.Length / 3));
            int minLength = Math.Max(1, rosterName.Length - maxNoise);
            int maxLength = rosterName.Length + maxNoise;
            double bestScore = Similarity(ocrLine, rosterName);
            bestStart = 0;

            for (int start = 0; start < ocrLine.Length; start++)
            {
                for (int length = minLength; length <= maxLength && start + length <= ocrLine.Length; length++)
                {
                    double score = Similarity(ocrLine.Substring(start, length), rosterName);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestStart = start;
                    }
                }
            }

            return bestScore;
        }

        private static double Similarity(string left, string right)
        {
            if (left.Length == 0 || right.Length == 0)
            {
                return 0;
            }

            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];
            for (int j = 0; j <= right.Length; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= left.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= right.Length; j++)
                {
                    int substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + substitutionCost);
                }

                (previous, current) = (current, previous);
            }

            return 1.0 - (previous[right.Length] / (double)Math.Max(left.Length, right.Length));
        }

        /// <summary>Encodes a captured screenshot without resizing it.</summary>
        internal static byte[] EncodeCapture(Bitmap bitmap)
        {
            using var output = new MemoryStream();
            bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);
            return output.ToArray();
        }

        /// <summary>Enlarges a masked player-list image before it is passed to OCR.</summary>
        internal static byte[] EncodeForOcr(Bitmap bitmap)
        {
            const int scale = 2;
            int width = Math.Max(1, bitmap.Width * scale);
            int height = Math.Max(1, bitmap.Height * scale);
            using var enlarged = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(enlarged))
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, width, height));
            }

            using var output = new MemoryStream();
            enlarged.Save(output, System.Drawing.Imaging.ImageFormat.Png);
            return output.ToArray();
        }
    }
}
