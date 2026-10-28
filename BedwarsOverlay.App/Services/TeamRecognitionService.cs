using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BedwarsOverlay.Core.Teams;
using TesseractOCR;
using TesseractOCR.Enums;
using PixImage = TesseractOCR.Pix.Image;

namespace BedwarsOverlay.App.Services
{
    /// <summary>Reads Hypixel team-colored player names from a captured Minecraft player list.</summary>
    public sealed class TeamRecognitionService : IDisposable
    {
        internal const PageSegMode OcrPageSegmentation = PageSegMode.SingleColumn;

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
            return ReadTeams(screenshot, rosterNames, out diagnostics, null);
        }

        public IReadOnlyDictionary<string, string> ReadTeams(
            byte[] screenshot,
            IEnumerable<string> rosterNames,
            out string diagnostics,
            TeamOcrTrace? trace)
        {
            var teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var palette = TeamPalette.Teams;
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
            Bitmap[] masks = CreateColorMasks(source, captureArea, out int[] colorCounts, out Rectangle[] maskAreas);

            try
            {
                Engine engine = GetEngine();

                // Each team color gets its own OCR pass. Merging every color into one image threw the
                // color away and forced the team to be guessed from the characters around a matched
                // name, so the wrong team (or none) was reported. Reading each color separately makes
                // the team a property of the mask that produced the match.
                var candidates = new Dictionary<string, (string Team, double Score)>(StringComparer.OrdinalIgnoreCase);
                var anchors = new List<Rectangle>();
                for (int teamIndex = 0; teamIndex < palette.Count; teamIndex++)
                {
                    if (colorCounts[teamIndex] < MinimumTeamPixels)
                    {
                        trace?.Passes.Add(new TeamOcrPass(palette[teamIndex].Name, colorCounts[teamIndex], null));
                        continue;
                    }

                    string text;
                    using (var image = PixImage.LoadFromMemory(EncodeForOcr(masks[teamIndex])))
                    using (var page = engine.Process(image, OcrPageSegmentation))
                    {
                        text = page.Text;
                        foreach (string row in page.TsvText.Split('\n'))
                        {
                            string[] fields = row.TrimEnd('\r').Split('\t');
                            if (fields.Length < 12 || fields[0] != "5" ||
                                !HasMatchingTeamPrefix(fields[11] + " ", palette[teamIndex].Name)) continue;
                            if (int.TryParse(fields[6], out int x) && int.TryParse(fields[7], out int y) &&
                                int.TryParse(fields[8], out int w) && int.TryParse(fields[9], out int h))
                                anchors.Add(new Rectangle(captureArea.X + maskAreas[teamIndex].X + x / 2,
                                    captureArea.Y + maskAreas[teamIndex].Y + y / 2, w / 2, h / 2));
                        }
                    }
                    trace?.Passes.Add(new TeamOcrPass(palette[teamIndex].Name, colorCounts[teamIndex], text));

                    foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!HasMatchingTeamPrefix(line, palette[teamIndex].Name))
                        {
                            continue;
                        }

                        if (!TryMatchRosterName(line, roster, out string playerName, out double score))
                        {
                            continue;
                        }

                        if (candidates.TryGetValue(playerName, out var existing) && existing.Score >= score)
                        {
                            continue;
                        }

                        candidates[playerName] = (palette[teamIndex].Name, score);
                    }
                }

                if (!ReadAnchoredRows(source, anchors, roster, candidates, trace)) candidates.Clear();

                foreach (var candidate in candidates)
                {
                    teams[candidate.Key] = candidate.Value.Team;
                }

                diagnostics = string.Join(
                    ", ",
                    Enumerable.Range(0, palette.Count).Select(i => $"{palette[i].Name} {colorCounts[i]}px"));
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

        private static Bitmap[] CreateColorMasks(Bitmap source, Rectangle area, out int[] coloredPixelCounts, out Rectangle[] maskAreas)
        {
            var palette = TeamPalette.Teams;
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

            coloredPixelCounts = new int[palette.Count];
            maskAreas = new Rectangle[palette.Count];
            var masks = new Bitmap[palette.Count];
            for (int teamIndex = 0; teamIndex < palette.Count; teamIndex++)
            {
                bool[] textPixels = KeepTextComponents(hitsByTeam[teamIndex], width, height);
                masks[teamIndex] = CreateMaskBitmap(textPixels, width, height, out coloredPixelCounts[teamIndex], out maskAreas[teamIndex]);
            }

            return masks;
        }

        private static bool[][] ClassifyTeamPixels(byte[] sourcePixels, int width, int height, bool exactColors)
        {
            var palette = TeamPalette.Teams;
            var hitsByTeam = new bool[palette.Count][];
            for (int teamIndex = 0; teamIndex < palette.Count; teamIndex++)
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
                    for (int teamIndex = 0; teamIndex < palette.Count; teamIndex++)
                    {
                        var team = palette[teamIndex];
                        bool match = exactColors
                            ? MatchesCaptureColor(team, red, green, blue)
                            : TeamPalette.MatchesLoose(team, red, green, blue);
                        if (match)
                        {
                            hitsByTeam[teamIndex][pixelIndex] = true;
                        }
                    }
                }
            }

            return hitsByTeam;
        }

        internal static bool MatchesCaptureColor(TeamColorDefinition team, byte red, byte green, byte blue)
        {
            if (TeamPalette.Matches(team, red, green, blue))
            {
                return true;
            }

            // Some clients shade each glyph vertically. Absolute palette matching cuts off the
            // darker lower strokes, especially at small GUI scales. Preserve the hue while
            // normalizing brightness; do not promote text shadows or neutral backgrounds.
            int brightest = Math.Max(red, Math.Max(green, blue));
            if (brightest < 130 || team.Name is "White" or "Gray")
            {
                return false;
            }

            return TeamPalette.Matches(team,
                (byte)(red * 255 / brightest),
                (byte)(green * 255 / brightest),
                (byte)(blue * 255 / brightest));
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

        private static Bitmap CreateMaskBitmap(bool[] pixels, int width, int height, out int keptCount, out Rectangle maskArea)
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
                maskArea = Rectangle.Empty;
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            const int padding = 4;
            int cropLeft = Math.Max(0, minX - padding);
            int cropTop = Math.Max(0, minY - padding);
            int cropRight = Math.Min(width, maxX + padding + 1);
            int cropBottom = Math.Min(height, maxY + padding + 1);
            int cropWidth = cropRight - cropLeft;
            int cropHeight = cropBottom - cropTop;
            maskArea = new Rectangle(cropLeft, cropTop, cropWidth, cropHeight);

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

        internal static bool TryMatchRosterName(
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
                // O/0 are indistinguishable in some Minecraft fonts. Keep ambiguity checks and
                // score below a literal match; never normalize other letters into arbitrary digits.
                if (normalizedName.Contains('O') || normalizedName.Contains('0'))
                    score = Math.Max(score, BestSubstringSimilarity(compactLine.Replace('O', '0'),
                        normalizedName.Replace('O', '0'), out _) * 0.95);

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

        internal static bool HasMatchingTeamPrefix(string ocrLine, string teamName)
        {
            if (string.IsNullOrWhiteSpace(ocrLine) || string.IsNullOrWhiteSpace(teamName))
            {
                return false;
            }

            char expectedPrefix = teamName == "Gray" ? 'S' : char.ToUpperInvariant(teamName[0]);
            string normalizedLine = ocrLine.TrimStart();
            return normalizedLine.Length > 1
                && char.ToUpperInvariant(normalizedLine[0]) == expectedPrefix
                && char.IsWhiteSpace(normalizedLine[1]);
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

        private bool ReadAnchoredRows(Bitmap source, List<Rectangle> anchors, string[] roster,
            Dictionary<string, (string Team, double Score)> candidates, TeamOcrTrace? trace)
        {
            // Repeated team-prefix positions locate the actual TAB column without assuming a GUI scale.
            var column = anchors.Where(a => a.Height >= 5 && a.Width >= 3)
                .GroupBy(a => a.X / 4).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (column == null || column.Count() < 3) return false;
            Rectangle[] aligned = column.OrderBy(a => a.Y).ToArray();
            int glyphHeight = aligned.Select(a => a.Height).OrderBy(h => h).ElementAt(aligned.Length / 2);
            int[] gaps = aligned.Zip(aligned.Skip(1), (a, b) => b.Y - a.Y)
                .Where(g => g > glyphHeight && g <= glyphHeight * 1.7).ToArray();
            if (gaps.Length < 2) return false;
            int step = gaps.GroupBy(g => g).OrderByDescending(g => g.Count()).First().Key;
            int left = aligned.Select(a => a.X).OrderBy(x => x).ElementAt(aligned.Length / 2);
            int firstY = aligned[0].Y;
            while (firstY - step > 0) firstY -= step;
            int bottom = Math.Min((int)(source.Height * 0.6), aligned[^1].Y + step * 3);
            for (int top = firstY; top + glyphHeight < bottom; top += step)
            {
                foreach (var team in TeamPalette.Teams)
                {
                    int width = Math.Min(source.Width - left, glyphHeight * 18);
                    using var row = new Bitmap(width + 16, glyphHeight + 16);
                    using (var graphics = Graphics.FromImage(row)) graphics.Clear(Color.White);
                    int prefixPixels = 0;
                    for (int y = 0; y < glyphHeight; y++)
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = source.GetPixel(left + x, top + y);
                        if (!MatchesCaptureColor(team, pixel.R, pixel.G, pixel.B)) continue;
                        row.SetPixel(x + 8, y + 8, Color.Black);
                        if (x < glyphHeight) prefixPixels++;
                    }
                    if (prefixPixels < 12) continue;
                    string text;
                    using (var image = PixImage.LoadFromMemory(EncodeForOcr(row)))
                    using (var page = GetEngine().Process(image, PageSegMode.SingleLine)) text = page.Text.Trim();
                    if (team.Name != "White" && text.Length > 0 &&
                        HasMatchingTeamPrefix(text[0] + " ", team.Name) &&
                        (!HasMatchingTeamPrefix(text, team.Name) ||
                         !TryMatchRosterName(text, roster, out _, out _)))
                    {
                        // Read the name independently: a merged Y+name or trailing health digits
                        // must not force relaxed prefix checks or roster similarity thresholds.
                        int offset = glyphHeight;
                        using var nameRow = row.Clone(new Rectangle(8 + offset, 0,
                            row.Width - 8 - offset, row.Height), row.PixelFormat);
                        string nameText;
                        using (var nameImage = PixImage.LoadFromMemory(EncodeForOcr(nameRow)))
                        using (var namePage = GetEngine().Process(nameImage, PageSegMode.SingleLine))
                            nameText = namePage.Text.Trim();
                        string isolated = text[0] + " " + nameText;
                        trace?.Passes.Add(new TeamOcrPass(team.Name, prefixPixels, isolated) { Mode = "TabName", RowY = top });
                        if (TryMatchRosterName(isolated, roster, out string isolatedName, out double isolatedScore) &&
                            (!candidates.TryGetValue(isolatedName, out var prior) || isolatedScore > prior.Score))
                            candidates[isolatedName] = (team.Name, isolatedScore);
                    }
                    // Minecraft's block W is regularly read as HW/WH. Validate its pixels instead
                    // of accepting arbitrary OCR noise ahead of a roster name.
                    if (team.Name == "White" && IsWhitePrefix(row, glyphHeight))
                    {
                        int nameOffset = glyphHeight;
                        using var nameRow = row.Clone(new Rectangle(8 + nameOffset, 0,
                            row.Width - 8 - nameOffset, row.Height), row.PixelFormat);
                        using var nameImage = PixImage.LoadFromMemory(EncodeForOcr(nameRow));
                        using var namePage = GetEngine().Process(nameImage, PageSegMode.SingleLine);
                        text = "W " + namePage.Text.Trim();
                    }
                    trace?.Passes.Add(new TeamOcrPass(team.Name, prefixPixels, text) { Mode = "TabRow", RowY = top });
                    if (!HasMatchingTeamPrefix(text, team.Name) ||
                        !TryMatchRosterName(text, roster, out string name, out double score)) continue;
                    if (!candidates.TryGetValue(name, out var existing) || score > existing.Score)
                        candidates[name] = (team.Name, score);
                }
            }
            return true;
        }

        private static bool IsWhitePrefix(Bitmap row, int height)
        {
            int width = Math.Max(3, (int)Math.Round(height * 5.0 / 7));
            bool Ink(double x, double y) => row.GetPixel(8 + Math.Min(width - 1, (int)(x * width)),
                8 + Math.Min(height - 1, (int)(y * height))).R < 128;
            return Ink(0.05, 0.1) && Ink(0.95, 0.1) && !Ink(0.5, 0.1)
                && Ink(0.05, 0.5) && Ink(0.5, 0.5) && Ink(0.95, 0.5)
                && Ink(0.05, 0.9) && !Ink(0.5, 0.9) && Ink(0.95, 0.9);
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
