using System;
using System.Collections.Generic;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Save;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Renders crisp 1080x1920 vertical pixel-art social share cards for Home/Profile Stats
    /// and Game Over Run results. Built on a 180x320 logical pixel grid upscaled 6x with
    /// nearest-neighbor so every character, border, mascot, and badge is 100% authentic pixel art.
    /// </summary>
    public static class ShareCardGenerator
    {
        public const int CardWidth = 1080;
        public const int CardHeight = 1920;
        private const int LogicalW = 180;
        private const int LogicalH = 320;
        private const int Scale = 6; // 180*6 = 1080, 320*6 = 1920

        // Canonical palette matching in-game City Zone & UI
        private static readonly Color32 BgTop = new Color32(16, 20, 30, 255);
        private static readonly Color32 BgBottom = new Color32(10, 12, 18, 255);
        private static readonly Color32 CardSurface = new Color32(23, 30, 42, 255);
        private static readonly Color32 CardSurfaceDark = new Color32(17, 22, 32, 255);
        private static readonly Color32 NeonGreen = new Color32(173, 235, 92, 255);
        private static readonly Color32 BrightGreen = new Color32(64, 217, 89, 255);
        private static readonly Color32 CriticalRed = new Color32(235, 56, 68, 255);
        private static readonly Color32 GoldCoin = new Color32(242, 199, 64, 255);
        private static readonly Color32 CyanAccent = new Color32(89, 214, 242, 255);
        private static readonly Color32 TextWhite = new Color32(244, 248, 255, 255);
        private static readonly Color32 TextMuted = new Color32(148, 163, 184, 255);
        private static readonly Color32 RimSilver = new Color32(217, 224, 235, 255);
        private static readonly Color32 RimShadow = new Color32(128, 138, 156, 255);
        private static readonly Color32 FaceChamber = new Color32(19, 22, 26, 255);
        private static readonly Color32 SkylineFar = new Color32(26, 33, 48, 255);
        private static readonly Color32 SkylineNear = new Color32(19, 25, 36, 255);
        private static readonly Color32 WindowLit = new Color32(242, 199, 64, 180);

        public static byte[] GenerateHomeStatsCardPng()
        {
            var data = SaveSystem.Data;
            Color32[] grid = CreateBaseCanvas(NeonGreen);

            // Top branding header
            DrawCenteredText(grid, "1% BATTERY", 18, 2, NeonGreen, true);
            DrawCenteredText(grid, "LOW POWER. HIGH STAKES.", 34, 1, TextMuted, false);

            // Section pill
            FillRect(grid, 42, 46, 96, 13, CardSurface);
            DrawBorder(grid, 42, 46, 96, 13, 1, CyanAccent);
            DrawCenteredText(grid, "PLAYER STATS", 50, 1, CyanAccent, false);

            // Mascot stage with equipped skin color
            Color32 mascotBodyColor = GetEquippedSkinColor(data);
            DrawMascot(grid, 90, 96, mascotBodyColor, 0.72f, true);

            // Hero stat card: Overall Best Distance
            FillRect(grid, 16, 138, 148, 44, CardSurface);
            DrawBorder(grid, 16, 138, 148, 44, 1, NeonGreen);
            DrawCenteredText(grid, " PERSONAL BEST ", 145, 1, TextMuted, false);
            int overallBest = Mathf.FloorToInt(data.OverallBestDistance());
            DrawCenteredText(grid, $"{overallBest} M", 158, 2, NeonGreen, true);

            // 2x2 Stats Grid
            int colW = 71;
            int rowH = 34;
            // Top-left: Total Runs
            DrawStatBox(grid, 16, 188, colW, rowH, "TOTAL RUNS", data.totalRuns.ToString(), TextWhite);
            // Top-right: Total Distance
            int totalDist = Mathf.FloorToInt(data.lifetimeDistanceMeters);
            DrawStatBox(grid, 93, 188, colW, rowH, "TOTAL DIST", $"{totalDist} M", TextWhite);

            // Bottom-left: Best 1% Survival
            DrawStatBox(grid, 16, 226, colW, rowH, "1% SURVIVAL", $"{data.bestSurvivalModeSeconds:0.0}S", CriticalRed);
            // Bottom-right: Achievements
            int unlockedCount = data.unlockedAchievementIds != null ? data.unlockedAchievementIds.Count : 0;
            int totalAch = AchievementCatalog.All != null ? AchievementCatalog.All.Length : 8;
            DrawStatBox(grid, 93, 226, colW, rowH, "UNLOCKED", $"{unlockedCount}/{totalAch}", GoldCoin);

            // Mode Breakdown Bar
            FillRect(grid, 16, 264, 148, 28, CardSurfaceDark);
            DrawBorder(grid, 16, 264, 148, 28, 1, RimShadow);
            int easyBest = Mathf.FloorToInt(data.bestDistanceEasy);
            int frenzyBest = Mathf.FloorToInt(data.bestDistanceFrenzy);
            int proBest = Mathf.FloorToInt(data.bestDistancePro);
            DrawCenteredText(grid, "MODE RECORDS", 268, 1, TextMuted, false);
            DrawCenteredText(grid, $"EASY {easyBest}M  FRENZY {frenzyBest}M  PRO {proBest}M", 279, 1, TextWhite, false);

            // Footer
            DrawCenteredText(grid, "CAN YOU BEAT MY RECORD?", 300, 1, NeonGreen, false);

            return EncodeUpscaledPng(grid);
        }

        public static byte[] GenerateRunResultCardPng(RunResult result)
        {
            var data = SaveSystem.Data;
            Color32 accent = result.IsNewRecord ? GoldCoin : CriticalRed;
            Color32[] grid = CreateBaseCanvas(accent);

            // Top branding header
            DrawCenteredText(grid, "1% BATTERY", 18, 2, NeonGreen, true);

            string modeName = RunModeConfig.For(result.Mode).DisplayName.ToUpperInvariant();
            FillRect(grid, 44, 36, 92, 13, CardSurface);
            DrawBorder(grid, 44, 36, 92, 13, 1, CyanAccent);
            DrawCenteredText(grid, $"{modeName} RUN", 40, 1, CyanAccent, false);

            if (result.IsNewRecord)
            {
                FillRect(grid, 34, 53, 112, 14, GoldCoin);
                DrawCenteredText(grid, "* NEW HIGH SCORE! *", 57, 1, new Color32(20, 16, 6, 255), false);
            }
            else
            {
                DrawCenteredText(grid, DescribeCauseShort(result.Cause), 57, 1, CriticalRed, false);
            }

            // Mascot in critical 1% state
            Color32 mascotBodyColor = GetEquippedSkinColor(data);
            DrawMascot(grid, 90, 102, mascotBodyColor, 0.08f, false);

            // Hero stat card: Run Distance
            FillRect(grid, 16, 144, 148, 48, CardSurface);
            DrawBorder(grid, 16, 144, 148, 48, 1, result.IsNewRecord ? GoldCoin : NeonGreen);
            DrawCenteredText(grid, "RUN DISTANCE", 151, 1, TextMuted, false);
            int runMeters = Mathf.FloorToInt(result.DistanceMeters);
            DrawCenteredText(grid, $"{runMeters} M", 165, 2, TextWhite, true);

            // Secondary run stats row
            int colW = 71;
            int rowH = 38;
            DrawStatBox(grid, 16, 198, colW, rowH, "COINS EARNED", $"+{result.CoinsCollected}", GoldCoin);
            DrawStatBox(grid, 93, 198, colW, rowH, "1% SURVIVAL", $"{result.SurvivalModeSeconds:0.0}S", CriticalRed);

            // Mode Best comparison box
            int modeBest = Mathf.Max(runMeters, Mathf.FloorToInt(data.BestDistanceFor(result.Mode)));
            FillRect(grid, 16, 242, 148, 36, CardSurfaceDark);
            DrawBorder(grid, 16, 242, 148, 36, 1, RimShadow);
            DrawCenteredText(grid, $"{modeName} BEST", 248, 1, TextMuted, false);
            DrawCenteredText(grid, $"{modeBest} M", 260, 2, NeonGreen, false);

            // Footer CTA
            DrawCenteredText(grid, "SURVIVE AT 1% BATTERY!", 296, 1, NeonGreen, false);

            return EncodeUpscaledPng(grid);
        }

        private static string DescribeCauseShort(RunEndCause cause)
        {
            switch (cause)
            {
                case RunEndCause.HitObstacle: return "HIT AN OBSTACLE";
                case RunEndCause.FellInPit: return "FELL INTO A PIT";
                default: return "OUT OF CHARGE";
            }
        }

        private static Color32 GetEquippedSkinColor(SaveData data)
        {
            var skin = ShopCatalog.FindSkin(data != null ? data.equippedSkinId : "skin_default");
            if (skin != null)
            {
                Color c = skin.AccentColor;
                return new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255),
                    255);
            }
            return BrightGreen;
        }

        private static Color32[] CreateBaseCanvas(Color32 borderAccent)
        {
            var grid = new Color32[LogicalW * LogicalH];
            for (int y = 0; y < LogicalH; y++)
            {
                float t = y / (float)(LogicalH - 1);
                Color32 rowColor = Color32.Lerp(BgBottom, BgTop, t);
                for (int x = 0; x < LogicalW; x++)
                {
                    grid[y * LogicalW + x] = rowColor;
                }
            }

            // Starfield in upper sky
            int[] starCoords = {
                18, 290, 42, 302, 75, 296, 135, 305, 160, 288,
                24, 260, 152, 265, 14, 210, 166, 220, 30, 130, 155, 125
            };
            for (int i = 0; i < starCoords.Length; i += 2)
            {
                SetPixel(grid, starCoords[i], starCoords[i + 1], new Color32(200, 220, 255, 180));
            }

            // Pixel skyline silhouette at bottom
            int[] bldgsFar = { 8, 28, 38, 24, 34, 48, 30, 40, 26, 52, 32, 36 };
            int bx = 6;
            for (int i = 0; i < bldgsFar.Length; i += 2)
            {
                int bw = bldgsFar[i];
                int bh = bldgsFar[i + 1];
                FillRect(grid, bx, 6, bw, bh, SkylineFar);
                bx += bw - 2;
            }

            int[] bldgsNear = { 22, 22, 28, 32, 26, 18, 32, 28, 28, 24, 30, 20 };
            bx = 6;
            for (int i = 0; i < bldgsNear.Length; i += 2)
            {
                int bw = bldgsNear[i];
                int bh = bldgsNear[i + 1];
                FillRect(grid, bx, 6, bw, bh, SkylineNear);
                for (int wy = 10; wy < 6 + bh - 4; wy += 5)
                {
                    for (int wx = bx + 3; wx < bx + bw - 3; wx += 5)
                    {
                        if (((wx + wy) % 3) == 0)
                        {
                            FillRect(grid, wx, wy, 2, 2, WindowLit);
                        }
                    }
                }
                bx += bw;
            }

            // Outer pixel frame
            DrawBorder(grid, 4, 4, LogicalW - 8, LogicalH - 8, 2, borderAccent);
            DrawBorder(grid, 7, 7, LogicalW - 14, LogicalH - 14, 1, new Color32(borderAccent.r, borderAccent.g, borderAccent.b, 90));

            return grid;
        }

        private static void DrawStatBox(Color32[] grid, int x, int topY, int w, int h, string label, string value, Color32 valueColor)
        {
            FillRectTop(grid, x, topY, w, h, CardSurface);
            DrawBorderTop(grid, x, topY, w, h, 1, RimShadow);
            int centerX = x + w / 2;
            DrawTextAtCenter(grid, label, centerX, topY + 6, 1, TextMuted, false);
            DrawTextAtCenter(grid, value, centerX, topY + 18, 1, valueColor, true);
        }

        private static void DrawMascot(Color32[] grid, int centerX, int centerTopY, Color32 bodyColor, float fillRatio, bool happy)
        {
            int w = 40;
            int h = 50;
            int left = centerX - w / 2;
            int top = centerTopY - h / 2;

            // Terminal Cap
            int capW = 16;
            int capH = 5;
            FillRectTop(grid, centerX - capW / 2, top - capH, capW, capH, RimSilver);
            DrawBorderTop(grid, centerX - capW / 2, top - capH, capW, capH, 1, RimShadow);

            // Outer Metallic Rim
            FillRectTop(grid, left, top, w, h, RimSilver);
            DrawBorderTop(grid, left, top, w, h, 2, RimShadow);

            // Colored Battery Body
            FillRectTop(grid, left + 3, top + 3, w - 6, h - 6, bodyColor);

            // Inner Dark Chamber
            int chX = left + 6;
            int chY = top + 6;
            int chW = w - 12;
            int chH = h - 12;
            FillRectTop(grid, chX, chY, chW, chH, FaceChamber);

            // Charge Fill inside chamber
            int fillH = Mathf.Clamp(Mathf.RoundToInt(chH * fillRatio), 3, chH);
            Color32 fillCol = happy ? bodyColor : CriticalRed;
            FillRectTop(grid, chX + 1, chY + chH - fillH, chW - 2, fillH, fillCol);

            // Eyes
            int eyeY = top + 16;
            FillRectTop(grid, centerX - 9, eyeY, 4, 5, TextWhite);
            FillRectTop(grid, centerX + 5, eyeY, 4, 5, TextWhite);

            // Mouth
            int mouthY = top + 26;
            if (happy)
            {
                FillRectTop(grid, centerX - 5, mouthY + 1, 10, 2, TextWhite);
                FillRectTop(grid, centerX - 7, mouthY - 1, 2, 2, TextWhite);
                FillRectTop(grid, centerX + 5, mouthY - 1, 2, 2, TextWhite);
            }
            else
            {
                FillRectTop(grid, centerX - 5, mouthY - 1, 10, 2, CriticalRed);
                FillRectTop(grid, centerX - 7, mouthY + 1, 2, 2, CriticalRed);
                FillRectTop(grid, centerX + 5, mouthY + 1, 2, 2, CriticalRed);
            }
        }

        // Coordinate helpers (topY = 0 at top of card, LogicalH-1 at bottom)
        private static void SetPixelTop(Color32[] grid, int x, int topY, Color32 color)
        {
            int y = (LogicalH - 1) - topY;
            SetPixel(grid, x, y, color);
        }

        private static void SetPixel(Color32[] grid, int x, int y, Color32 color)
        {
            if (x < 0 || x >= LogicalW || y < 0 || y >= LogicalH) return;
            grid[y * LogicalW + x] = color;
        }

        private static void FillRect(Color32[] grid, int x, int topY, int w, int h, Color32 color)
        {
            FillRectTop(grid, x, topY, w, h, color);
        }

        private static void FillRectTop(Color32[] grid, int x, int topY, int w, int h, Color32 color)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    SetPixelTop(grid, x + dx, topY + dy, color);
                }
            }
        }

        private static void DrawBorder(Color32[] grid, int x, int topY, int w, int h, int thickness, Color32 color)
        {
            DrawBorderTop(grid, x, topY, w, h, thickness, color);
        }

        private static void DrawBorderTop(Color32[] grid, int x, int topY, int w, int h, int thickness, Color32 color)
        {
            FillRectTop(grid, x, topY, w, thickness, color);
            FillRectTop(grid, x, topY + h - thickness, w, thickness, color);
            FillRectTop(grid, x, topY, thickness, h, color);
            FillRectTop(grid, x + w - thickness, topY, thickness, h, color);
        }

        private static void DrawCenteredText(Color32[] grid, string text, int topY, int scale, Color32 color, bool shadow)
        {
            DrawTextAtCenter(grid, text, LogicalW / 2, topY, scale, color, shadow);
        }

        private static void DrawTextAtCenter(Color32[] grid, string text, int centerX, int topY, int scale, Color32 color, bool shadow)
        {
            if (string.IsNullOrEmpty(text)) return;
            int charW = 4 * scale;
            int spacing = 1 * scale;
            int totalW = text.Length * charW + (text.Length - 1) * spacing;
            int startX = centerX - totalW / 2;

            if (shadow)
            {
                DrawTextRaw(grid, text, startX + scale, topY + scale, scale, new Color32(8, 10, 15, 255));
            }
            DrawTextRaw(grid, text, startX, topY, scale, color);
        }

        private static void DrawTextRaw(Color32[] grid, string text, int startX, int topY, int scale, Color32 color)
        {
            int cursorX = startX;
            string upper = text.ToUpperInvariant();
            for (int i = 0; i < upper.Length; i++)
            {
                byte[] glyph = GetGlyph(upper[i]);
                for (int row = 0; row < 5; row++)
                {
                    byte bits = glyph[row];
                    for (int col = 0; col < 4; col++)
                    {
                        if ((bits & (1 << (3 - col))) != 0)
                        {
                            FillRectTop(grid, cursorX + col * scale, topY + row * scale, scale, scale, color);
                        }
                    }
                }
                cursorX += 5 * scale;
            }
        }

        private static byte[] EncodeUpscaledPng(Color32[] logicalGrid)
        {
            var tex = new Texture2D(CardWidth, CardHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[CardWidth * CardHeight];

            for (int ly = 0; ly < LogicalH; ly++)
            {
                for (int lx = 0; lx < LogicalW; lx++)
                {
                    Color32 c = logicalGrid[ly * LogicalW + lx];
                    int baseY = ly * Scale;
                    int baseX = lx * Scale;
                    for (int sy = 0; sy < Scale; sy++)
                    {
                        int rowOffset = (baseY + sy) * CardWidth + baseX;
                        for (int sx = 0; sx < Scale; sx++)
                        {
                            pixels[rowOffset + sx] = c;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.Destroy(tex);
            return png;
        }

        private static readonly Dictionary<char, byte[]> Glyphs = new Dictionary<char, byte[]>
        {
            { 'A', new byte[] { 0b0110, 0b1001, 0b1111, 0b1001, 0b1001 } },
            { 'B', new byte[] { 0b1110, 0b1001, 0b1110, 0b1001, 0b1110 } },
            { 'C', new byte[] { 0b0111, 0b1000, 0b1000, 0b1000, 0b0111 } },
            { 'D', new byte[] { 0b1110, 0b1001, 0b1001, 0b1001, 0b1110 } },
            { 'E', new byte[] { 0b1111, 0b1000, 0b1110, 0b1000, 0b1111 } },
            { 'F', new byte[] { 0b1111, 0b1000, 0b1110, 0b1000, 0b1000 } },
            { 'G', new byte[] { 0b0111, 0b1000, 0b1011, 0b1001, 0b0111 } },
            { 'H', new byte[] { 0b1001, 0b1001, 0b1111, 0b1001, 0b1001 } },
            { 'I', new byte[] { 0b1110, 0b0100, 0b0100, 0b0100, 0b1110 } },
            { 'J', new byte[] { 0b0011, 0b0001, 0b0001, 0b1001, 0b0110 } },
            { 'K', new byte[] { 0b1001, 0b1010, 0b1100, 0b1010, 0b1001 } },
            { 'L', new byte[] { 0b1000, 0b1000, 0b1000, 0b1000, 0b1111 } },
            { 'M', new byte[] { 0b1001, 0b1111, 0b1111, 0b1001, 0b1001 } },
            { 'N', new byte[] { 0b1001, 0b1101, 0b1011, 0b1001, 0b1001 } },
            { 'O', new byte[] { 0b0110, 0b1001, 0b1001, 0b1001, 0b0110 } },
            { 'P', new byte[] { 0b1110, 0b1001, 0b1110, 0b1000, 0b1000 } },
            { 'Q', new byte[] { 0b0110, 0b1001, 0b1001, 0b1011, 0b0111 } },
            { 'R', new byte[] { 0b1110, 0b1001, 0b1110, 0b1010, 0b1001 } },
            { 'S', new byte[] { 0b0111, 0b1000, 0b0110, 0b0001, 0b1110 } },
            { 'T', new byte[] { 0b1111, 0b0100, 0b0100, 0b0100, 0b0100 } },
            { 'U', new byte[] { 0b1001, 0b1001, 0b1001, 0b1001, 0b0110 } },
            { 'V', new byte[] { 0b1001, 0b1001, 0b1001, 0b0110, 0b0110 } },
            { 'W', new byte[] { 0b1001, 0b1001, 0b1111, 0b1111, 0b1001 } },
            { 'X', new byte[] { 0b1001, 0b0110, 0b0110, 0b0110, 0b1001 } },
            { 'Y', new byte[] { 0b1001, 0b1001, 0b0110, 0b0100, 0b0100 } },
            { 'Z', new byte[] { 0b1111, 0b0010, 0b0100, 0b1000, 0b1111 } },
            { '0', new byte[] { 0b0110, 0b1001, 0b1001, 0b1001, 0b0110 } },
            { '1', new byte[] { 0b0100, 0b1100, 0b0100, 0b0100, 0b1110 } },
            { '2', new byte[] { 0b1110, 0b0001, 0b0110, 0b1000, 0b1111 } },
            { '3', new byte[] { 0b1110, 0b0001, 0b0110, 0b0001, 0b1110 } },
            { '4', new byte[] { 0b1001, 0b1001, 0b1111, 0b0001, 0b0001 } },
            { '5', new byte[] { 0b1111, 0b1000, 0b1110, 0b0001, 0b1110 } },
            { '6', new byte[] { 0b0111, 0b1000, 0b1110, 0b1001, 0b0110 } },
            { '7', new byte[] { 0b1111, 0b0001, 0b0010, 0b0100, 0b0100 } },
            { '8', new byte[] { 0b0110, 0b1001, 0b0110, 0b1001, 0b0110 } },
            { '9', new byte[] { 0b0110, 0b1001, 0b0111, 0b0001, 0b1110 } },
            { '%', new byte[] { 0b1001, 0b0010, 0b0100, 0b1000, 0b1001 } },
            { '+', new byte[] { 0b0000, 0b0100, 0b1110, 0b0100, 0b0000 } },
            { '-', new byte[] { 0b0000, 0b0000, 0b1110, 0b0000, 0b0000 } },
            { '.', new byte[] { 0b0000, 0b0000, 0b0000, 0b0000, 0b0100 } },
            { ':', new byte[] { 0b0000, 0b0100, 0b0000, 0b0100, 0b0000 } },
            { '/', new byte[] { 0b0001, 0b0010, 0b0100, 0b1000, 0b0000 } },
            { '!', new byte[] { 0b0100, 0b0100, 0b0100, 0b0000, 0b0100 } },
            { '?', new byte[] { 0b1110, 0b0001, 0b0110, 0b0000, 0b0100 } },
            { '*', new byte[] { 0b1001, 0b0110, 0b1111, 0b0110, 0b1001 } },
            { ' ', new byte[] { 0b0000, 0b0000, 0b0000, 0b0000, 0b0000 } },
        };

        private static byte[] GetGlyph(char c)
        {
            if (Glyphs.TryGetValue(c, out byte[] glyph)) return glyph;
            return Glyphs[' '];
        }
    }
}
