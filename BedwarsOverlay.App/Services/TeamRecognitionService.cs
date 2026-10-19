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
        private static readonly (string Name, string Code)[] Teams =
        {
            ("Blue", "B"),
            ("Green", "G"),
            ("Red", "R"),
            ("Yellow", "Y")
        };

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
            var teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            var captureArea = GetPlayerListArea(source.Width, source.Height);
            Bitmap[] masks = CreateColorMasks(source, captureArea, out int[] colorCounts);

            try
            {
                using var mask = CombineColorMasks(masks, colorCounts);
                if (colorCounts.All(count => count < 12))
                {
                    return teams;
                }

                Engine engine = GetEngine();
                using var image = PixImage.LoadFromMemory(EncodeForOcr(mask));
                using var page = engine.Process(image, PageSegMode.SparseText);

                foreach (string line in page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (TryMatchRosterName(line, roster, out string playerName, out string teamName, out _))
                    {
                        teams[playerName] = teamName;
                    }
                }
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
            // Keep the upper-left and center regions because clients can left-align the list.
            int left = (int)(width * 0.01);
            int top = (int)(height * 0.01);
            int right = (int)(width * 0.62);
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

            var maskPixels = new byte[Teams.Length][];
            coloredPixelCounts = new int[Teams.Length];
            var minX = Enumerable.Repeat(width, Teams.Length).ToArray();
            var minY = Enumerable.Repeat(height, Teams.Length).ToArray();
            var maxX = Enumerable.Repeat(-1, Teams.Length).ToArray();
            var maxY = Enumerable.Repeat(-1, Teams.Length).ToArray();
            for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
            {
                maskPixels[teamIndex] = new byte[sourcePixels.Length];
                Array.Fill(maskPixels[teamIndex], byte.MaxValue);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * 4;
                    byte blue = sourcePixels[offset];
                    byte green = sourcePixels[offset + 1];
                    byte red = sourcePixels[offset + 2];
                    for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
                    {
                        if (!MatchesTeamColor(red, green, blue, Teams[teamIndex].Code))
                        {
                            continue;
                        }

                        maskPixels[teamIndex][offset] = 0;
                        maskPixels[teamIndex][offset + 1] = 0;
                        maskPixels[teamIndex][offset + 2] = 0;
                        coloredPixelCounts[teamIndex]++;
                        minX[teamIndex] = Math.Min(minX[teamIndex], x);
                        minY[teamIndex] = Math.Min(minY[teamIndex], y);
                        maxX[teamIndex] = Math.Max(maxX[teamIndex], x);
                        maxY[teamIndex] = Math.Max(maxY[teamIndex], y);
                    }
                }
            }

            var masks = new Bitmap[Teams.Length];
            for (int teamIndex = 0; teamIndex < Teams.Length; teamIndex++)
            {
                if (coloredPixelCounts[teamIndex] == 0)
                {
                    masks[teamIndex] = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
                    continue;
                }

                const int padding = 4;
                int cropLeft = Math.Max(0, minX[teamIndex] - padding);
                int cropTop = Math.Max(0, minY[teamIndex] - padding);
                int cropRight = Math.Min(width, maxX[teamIndex] + padding + 1);
                int cropBottom = Math.Min(height, maxY[teamIndex] + padding + 1);
                int cropWidth = cropRight - cropLeft;
                int cropHeight = cropBottom - cropTop;
                int cropRowBytes = cropWidth * 4;
                var croppedPixels = new byte[cropRowBytes * cropHeight];
                Array.Fill(croppedPixels, byte.MaxValue);
                for (int y = 0; y < cropHeight; y++)
                {
                    int sourceOffset = ((cropTop + y) * width + cropLeft) * 4;
                    Array.Copy(maskPixels[teamIndex], sourceOffset, croppedPixels, y * cropRowBytes, cropRowBytes);
                }

                var mask = new Bitmap(cropWidth, cropHeight, PixelFormat.Format32bppArgb);
                BitmapData data = mask.LockBits(new Rectangle(0, 0, cropWidth, cropHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < cropHeight; y++)
                    {
                        Marshal.Copy(croppedPixels, y * cropRowBytes, IntPtr.Add(data.Scan0, y * data.Stride), cropRowBytes);
                    }
                }
                finally
                {
                    mask.UnlockBits(data);
                }

                masks[teamIndex] = mask;
            }

            return masks;
        }

        private static Bitmap CombineColorMasks(Bitmap[] masks, int[] colorCounts)
        {
            int[] included = Enumerable.Range(0, masks.Length).Where(i => colorCounts[i] >= 12).ToArray();
            if (included.Length == 0)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            int width = included.Max(i => masks[i].Width);
            int height = included.Sum(i => masks[i].Height) + (included.Length - 1) * 8;
            var combined = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(combined))
            {
                graphics.Clear(Color.White);
                int top = 0;
                foreach (int index in included)
                {
                    graphics.DrawImageUnscaled(masks[index], 0, top);
                    top += masks[index].Height + 8;
                }
            }

            return combined;
        }

        private static bool MatchesTeamColor(byte red, byte green, byte blue, string teamCode)
        {
            return teamCode switch
            {
                "B" => blue > 130 && blue > red * 1.35 && blue > green * 1.2,
                "G" => green > 130 && green > red * 1.25 && green > blue * 1.2,
                "R" => red > 150 && red > green * 1.3 && red > blue * 1.3,
                "Y" => red > 150 && green > 150 && blue < 140 && Math.Abs(red - green) < 100,
                _ => false
            };
        }

        private static bool TryMatchRosterName(
            string ocrLine,
            IReadOnlyList<string> roster,
            out string matchedName,
            out string matchedTeam,
            out double matchedScore)
        {
            matchedName = string.Empty;
            matchedTeam = string.Empty;
            matchedScore = 0;

            string compactLine = Regex.Replace(ocrLine.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
            if (compactLine.Length == 0)
            {
                return false;
            }

            double secondBestScore = 0;
            int matchedStart = -1;
            foreach (string rosterName in roster)
            {
                string normalizedName = Regex.Replace(rosterName.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
                double score = BestSubstringSimilarity(compactLine, normalizedName, out int start);

                if (score > matchedScore)
                {
                    secondBestScore = matchedScore;
                    matchedScore = score;
                    matchedName = rosterName;
                    matchedStart = start;
                }
                else if (score > secondBestScore)
                {
                    secondBestScore = score;
                }
            }

            // Avoid guessing when OCR is too weak or two roster names look equally close.
            if (matchedScore < 0.70 || matchedScore - secondBestScore < 0.08 || matchedStart < 0)
            {
                return false;
            }

            for (int i = matchedStart - 1; i >= Math.Max(0, matchedStart - 4); i--)
            {
                foreach (var team in Teams)
                {
                    if (compactLine[i] == team.Code[0])
                    {
                        matchedTeam = team.Name;
                        return true;
                    }
                }
            }

            return false;
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
