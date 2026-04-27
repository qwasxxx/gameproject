using System.Drawing;
using System.Drawing.Drawing2D;
using ChaosRunner.Model;

namespace ChaosRunner.View;

public sealed class GameRenderer
{
    private static Font UiFont(float emSize, FontStyle style = FontStyle.Regular)
    {
        try
        {
            return new Font("Segoe UI", emSize, style, GraphicsUnit.Point);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, emSize, style);
        }
    }

    private static void DrawStringWithShadow(Graphics g, string text, Font font, Brush fill, RectangleF bounds, StringFormat format)
    {
        using var sh = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        var b = bounds;
        g.DrawString(text, font, sh, new RectangleF(b.X + 1.5f, b.Y + 1.5f, b.Width, b.Height), format);
        g.DrawString(text, font, fill, bounds, format);
    }

    public void Render(Graphics g, Rectangle client, Level level, Player player, GameState state, GameProgress progress)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int cw = client.Width;
        int ch = client.Height;

        switch (state.Phase)
        {
            case GamePhase.Menu:
                DrawMenuBackdrop(g, cw, ch);
                DrawMainMenu(g, cw, ch);
                return;
            case GamePhase.LevelSelect:
                DrawMenuBackdrop(g, cw, ch);
                DrawLevelSelect(g, cw, ch, state, progress);
                return;
        }

        int pixelW = level.WidthInTiles * level.TileSize;
        int pixelH = level.HeightInTiles * level.TileSize;

        var worldState = g.Save();
        try
        {
            g.TranslateTransform(state.ShakeOffsetX, state.ShakeOffsetY);
            DrawGameWorld(g, level, player, pixelW, pixelH);
        }
        finally
        {
            g.Restore(worldState);
        }

        if (state.Phase != GamePhase.Victory)
        {
            DrawHud(g, pixelW, state);
            if (state.Phase == GamePhase.Playing || state.Phase == GamePhase.Pause)
                DrawPlayingFooter(g, pixelW, pixelH);
        }

        if (state.Phase == GamePhase.Pause)
            DrawPauseOverlay(g, pixelW, pixelH);

        if (state.Phase == GamePhase.Victory)
            DrawVictoryScreen(g, pixelW, pixelH, state, progress);
        else
            DrawOverlay(g, state);
    }

    private static void DrawGameWorld(Graphics g, Level level, Player player, int pixelW, int pixelH)
    {
        DrawSky(g, pixelW, pixelH);
        DrawClouds(g, pixelW, pixelH);

        int ts = level.TileSize;
        int tw = level.WidthInTiles;
        int th = level.HeightInTiles;
        for (int ty = 0; ty < th; ty++)
        {
            int tx = 0;
            while (tx < tw)
            {
                if (level.Tiles[ty, tx] != TileKind.Solid)
                {
                    tx++;
                    continue;
                }

                int x0 = tx;
                while (tx < tw && level.Tiles[ty, tx] == TileKind.Solid)
                    tx++;

                var rect = new Rectangle(x0 * ts, ty * ts, (tx - x0) * ts, ts);
                DrawGroundOrCeilingTile(g, rect, ty == 0);
            }
        }

        for (int ty = 0; ty < th; ty++)
        {
            for (int tx = 0; tx < tw; tx++)
            {
                if (tx == level.StartTileX && ty == level.StartTileY)
                    DrawStartMarker(g, new Rectangle(tx * ts, ty * ts, ts, ts));
                if (tx == level.FinishTileX && ty == level.FinishTileY)
                    DrawFinishFlag(g, new Rectangle(tx * ts, ty * ts, ts, ts));
            }
        }

        DrawFloorSpikes(g, level, ts);

        foreach (var block in level.FallingBlocks)
            DrawFallingBlock(g, block);

        DrawPlayer(g, player);
    }

    private static void DrawMenuBackdrop(Graphics g, int w, int h)
    {
        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, w, h),
            Color.FromArgb(255, 24, 18, 48),
            Color.FromArgb(255, 60, 95, 160),
            LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(brush, 0, 0, w, h);
        using var vignette = new SolidBrush(Color.FromArgb(120, 10, 5, 30));
        g.FillEllipse(vignette, -w * 0.35f, h * 0.45f, w * 1.7f, h * 0.9f);

        using var star = new SolidBrush(Color.FromArgb(35, 255, 255, 255));
        var rng = new Random(42);
        for (int i = 0; i < 70; i++)
        {
            float x = rng.Next(w);
            float y = rng.Next(h / 2);
            float s = rng.Next(2, 5);
            g.FillEllipse(star, x, y, s, s);
        }
    }

    private static void DrawMainMenu(Graphics g, int w, int h)
    {
        var savedHint = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        float pad = w * 0.08f;
        var panel = new RectangleF(pad, h * 0.1f, w - 2 * pad, h * 0.72f);
        using (var glow = new Pen(Color.FromArgb(60, 180, 230, 255), 8f))
        {
            using var pathGlow = RoundedRect(new RectangleF(panel.X - 2, panel.Y - 2, panel.Width + 4, panel.Height + 4), 24f);
            g.DrawPath(glow, pathGlow);
        }

        using (var pbrush = new LinearGradientBrush(panel,
                   Color.FromArgb(237, 48, 62, 118), Color.FromArgb(242, 22, 36, 72), 100f))
        {
            using var path = RoundedRect(panel, 22f);
            g.FillPath(pbrush, path);
            using var border = new Pen(Color.FromArgb(215, 195, 235, 255), 2.2f);
            g.DrawPath(border, path);
        }

        using var titleFont = UiFont(Math.Max(30f, w * 0.044f), FontStyle.Bold);
        using var tagFont = UiFont(Math.Max(13.5f, w * 0.017f), FontStyle.Italic);
        using var hintFont = UiFont(Math.Max(12f, w * 0.018f), FontStyle.Regular);
        var centerMid = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoWrap
        };

        const string title = "Chaos Runner";
        const string tag = "раннер с нарастающим хаосом";

        float titleSize = g.MeasureString(title, titleFont).Height;
        float titleBlockH = Math.Max(titleSize + 20f, 72f);
        float yTitle = h * 0.22f;
        g.DrawString(title, titleFont, Brushes.White, new RectangleF(0, yTitle, w, titleBlockH), centerMid);

        float yTag = yTitle + titleBlockH + 8f;
        g.DrawString(tag, tagFont, new SolidBrush(Color.FromArgb(225, 218, 240, 255)),
            new RectangleF(pad + 4, yTag, w - 2 * pad - 8, 40f), centerMid);

        string hint = "Enter — выбор уровня\r\nW A D — движение и прыжок\r\nEsc — пауза · M — к выбору уровня (в игре или в паузе)";
        g.DrawString(hint, hintFont, new SolidBrush(Color.FromArgb(238, 232, 242, 255)),
            new RectangleF(pad + 12, h * 0.58f, w - 2 * pad - 24, h * 0.28f), centerMid);

        g.TextRenderingHint = savedHint;
    }

    private static void DrawLevelSelect(Graphics g, int w, int h, GameState state, GameProgress progress)
    {
        var savedHint = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using var titleFont = UiFont(Math.Max(24f, w * 0.028f), FontStyle.Bold);
        using var small = UiFont(11f, FontStyle.Regular);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        DrawStringWithShadow(g, "Выбор уровня", titleFont, Brushes.White, new RectangleF(0, h * 0.055f, w, 48f), center);

        using (var subFont = UiFont(12f, FontStyle.Italic))
        {
            g.DrawString("← → или цифры 1–5 · Enter — играть · Esc — главное меню", subFont,
                new SolidBrush(Color.FromArgb(220, 225, 238, 255)), new RectangleF(0, h * 0.115f, w, 36f), center);
        }

        int n = Level.LevelCount;
        float margin = 24f;
        float totalW = w - margin * 2;
        float gap = 12f;
        float cardW = (totalW - gap * (n - 1)) / n;
        float cardH = h * 0.55f;
        float top = h * 0.22f;
        float left0 = margin;

        for (int i = 0; i < n; i++)
        {
            int levelId = i + 1;
            bool unlocked = levelId <= state.MaxUnlockedLevel;
            bool sel = i == state.LevelSelectIndex;
            float x = left0 + i * (cardW + gap);
            var card = new RectangleF(x, top, cardW, cardH);

            using (var b = new LinearGradientBrush(card,
                       unlocked ? Color.FromArgb(245, 58, 78, 128) : Color.FromArgb(230, 38, 42, 72),
                       unlocked ? Color.FromArgb(250, 22, 30, 58) : Color.FromArgb(245, 24, 26, 48),
                       100f))
            {
                using var path = RoundedRect(card, 16f);
                g.FillPath(b, path);
                float glow = sel ? 3.2f : 1.4f;
                using var edge = new Pen(
                    sel ? Color.FromArgb(255, 160, 220, 255) : Color.FromArgb(160, 90, 120, 200), glow);
                g.DrawPath(edge, path);
            }

            g.DrawString($"№{levelId}", UiFont(Math.Max(17f, cardW * 0.085f), FontStyle.Bold),
                Brushes.White, new RectangleF(x, top + 18f, cardW, 44f), center);

            string meta;
            if (!unlocked)
                meta = "Закрыто";
            else if (progress.BestTimeMs.TryGetValue(levelId, out int ms))
                meta = $"Рекорд: {FormatTime(ms)}";
            else
                meta = "Рекорд: —";

            g.DrawString(meta, small, new SolidBrush(Color.FromArgb(230, 220, 230, 255)),
                new RectangleF(x + 8f, top + cardH * 0.42f, cardW - 16f, 40f), center);

            int diffStars = Math.Min(5, 1 + i);
            string stars = new string('★', diffStars) + new string('☆', 5 - diffStars);
            g.DrawString(stars, UiFont(12.5f, FontStyle.Regular),
                new SolidBrush(Color.FromArgb(240, 255, 210, 120)), new RectangleF(x, top + cardH * 0.62f, cardW, 26f), center);

            if (!unlocked)
                DrawLockBadge(g, new RectangleF(x + cardW * 0.35f, top + cardH * 0.72f, cardW * 0.3f, cardW * 0.28f));
        }

        g.TextRenderingHint = savedHint;
    }

    private static void DrawLockBadge(Graphics g, RectangleF r)
    {
        using var body = new SolidBrush(Color.FromArgb(230, 90, 95, 120));
        float bh = r.Height * 0.55f;
        g.FillRectangle(body, r.X, r.Y + r.Height - bh, r.Width, bh);
        using var arc = new Pen(Color.FromArgb(220, 200, 205, 230), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float cx = r.X + r.Width / 2f;
        g.DrawArc(arc, cx - r.Width * 0.28f, r.Y, r.Width * 0.56f, r.Height * 0.55f, 0f, 180f);
    }

    private static void DrawHud(Graphics g, int pixelW, GameState state)
    {
        if (state.Phase != GamePhase.Playing && state.Phase != GamePhase.Pause)
            return;

        var savedHint = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float barH = 54f;
        var bar = new RectangleF(12, 11, Math.Max(220f, pixelW - 24), barH);
        using (var shade = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
        {
            using var pathSh = RoundedRect(new RectangleF(bar.X + 2, bar.Y + 3, bar.Width, bar.Height), 16f);
            g.FillPath(shade, pathSh);
        }

        using (var b = new LinearGradientBrush(bar,
                   Color.FromArgb(228, 22, 28, 58), Color.FromArgb(215, 38, 72, 130), 92f))
        {
            using var path = RoundedRect(bar, 16f);
            g.FillPath(b, path);
            using var edge = new Pen(Color.FromArgb(175, 170, 220, 255), 1.8f);
            g.DrawPath(edge, path);
        }

        using var valueFont = UiFont(13.5f, FontStyle.Bold);
        using var sub = UiFont(9.75f, FontStyle.Regular);
        var fg = new SolidBrush(Color.FromArgb(252, 253, 255));
        var muted = new SolidBrush(Color.FromArgb(215, 205, 220, 240));
        var sfCol = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

        float innerPad = 14f;
        float usable = bar.Width - innerPad * 2;
        float colW = usable / 3f;
        float baseY = bar.Y + 9f;

        void DrawColumn(int index, string icon, string value, string caption, Font valFont)
        {
            float x = bar.X + innerPad + index * colW;
            var col = new RectangleF(x, baseY, colW, barH - 18f);
            string line = $"{icon}  {value}";
            g.DrawString(line, valFont, fg, col, sfCol);
            float capY = baseY + 22f;
            g.DrawString(caption, sub, muted, new RectangleF(x, capY, colW, 20f), sfCol);
        }

        using var effectFont = UiFont(11.25f, FontStyle.Bold);
        DrawColumn(0, "⏱", FormatTime(state.RunElapsedMs), "время", valueFont);
        DrawColumn(1, "☠", state.RunDeaths.ToString(), "падений", valueFont);
        string hall = string.IsNullOrEmpty(state.ActiveHallucinationLabel) ? "—" : state.ActiveHallucinationLabel!;
        DrawColumn(2, "◆", hall, "эффект", effectFont);

        g.TextRenderingHint = savedHint;
    }

    private static void DrawPlayingFooter(Graphics g, int pixelW, int pixelH)
    {
        using var font = UiFont(10f, FontStyle.Regular);
        using var bg = new SolidBrush(Color.FromArgb(160, 12, 16, 36));
        float fh = 26f;
        var r = new RectangleF(12, pixelH - fh - 10, pixelW - 24, fh);
        using (var path = RoundedRect(r, 10f))
            g.FillPath(bg, path);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("Esc — пауза · M — к выбору уровня", font,
            new SolidBrush(Color.FromArgb(235, 225, 235, 250)), r, sf);
    }

    private static void DrawPauseOverlay(Graphics g, int w, int h)
    {
        using var veil = new SolidBrush(Color.FromArgb(175, 8, 10, 28));
        g.FillRectangle(veil, 0, 0, w, h);

        float pad = Math.Min(w, h) * 0.12f;
        var panel = new RectangleF(pad, h * 0.28f, w - 2 * pad, h * 0.44f);
        using (var pbrush = new LinearGradientBrush(panel,
                   Color.FromArgb(240, 40, 48, 92), Color.FromArgb(245, 18, 24, 52), 95f))
        {
            using var path = RoundedRect(panel, 20f);
            g.FillPath(pbrush, path);
            using var border = new Pen(Color.FromArgb(210, 175, 220, 255), 2.4f);
            g.DrawPath(border, path);
        }

        var savedHint = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var titleFont = UiFont(Math.Max(22f, w * 0.035f), FontStyle.Bold);
        using var bodyFont = UiFont(13f, FontStyle.Regular);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        float y = h * 0.34f;
        DrawStringWithShadow(g, "Пауза", titleFont, Brushes.White, new RectangleF(0, y, w, 44f), sf);

        const string lines = "Esc — продолжить игру\r\nM — выйти к выбору уровня\r\n(текущий забег не сохраняется)";
        g.DrawString(lines, bodyFont, new SolidBrush(Color.FromArgb(235, 228, 236, 255)),
            new RectangleF(pad + 16, y + 50f, w - 2 * pad - 32, h * 0.32f), sf);
        g.TextRenderingHint = savedHint;
    }

    private static string FormatTime(int ms)
    {
        int s = ms / 1000;
        int m = s / 60;
        s %= 60;
        int centi = (ms % 1000) / 10;
        return $"{m:00}:{s:00}.{centi:00}";
    }

    private static void DrawSky(Graphics g, int w, int h)
    {
        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, w, h),
            Color.FromArgb(255, 100, 175, 235),
            Color.FromArgb(255, 225, 240, 252),
            LinearGradientMode.Vertical);
        g.FillRectangle(brush, 0, 0, w, h);

        using var glow = new SolidBrush(Color.FromArgb(25, 255, 255, 255));
        g.FillEllipse(glow, w * 0.55f, -h * 0.08f, w * 0.55f, h * 0.35f);
    }

    private static void DrawClouds(Graphics g, int w, int h)
    {
        using var b = new SolidBrush(Color.FromArgb(55, 255, 255, 255));
        void Cloud(float x, float y, float s)
        {
            g.FillEllipse(b, x, y, s * 1.2f, s * 0.55f);
            g.FillEllipse(b, x + s * 0.35f, y - s * 0.08f, s, s * 0.5f);
            g.FillEllipse(b, x + s * 0.85f, y, s * 0.9f, s * 0.5f);
        }

        Cloud(w * 0.08f, h * 0.12f, w * 0.12f);
        Cloud(w * 0.42f, h * 0.08f, w * 0.1f);
        Cloud(w * 0.72f, h * 0.15f, w * 0.11f);
    }

    private static void DrawGroundOrCeilingTile(Graphics g, Rectangle rect, bool ceiling)
    {
        var dark = ceiling ? Color.FromArgb(62, 55, 70) : Color.FromArgb(38, 92, 52);
        var light = ceiling ? Color.FromArgb(155, 148, 172) : Color.FromArgb(118, 198, 108);

        float ang = ceiling ? 38f : 28f;
        using (var brush = new LinearGradientBrush(rect, light, dark, ang))
            g.FillRectangle(brush, rect);

        int t = Math.Max(5, rect.Height / 4);
        using var topStrip = new SolidBrush(ceiling ? Color.FromArgb(175, 168, 188) : Color.FromArgb(132, 218, 118));
        g.FillRectangle(topStrip, rect.X, rect.Y, rect.Width, t);

        if (!ceiling)
        {
            using var mortar = new Pen(Color.FromArgb(90, 30, 65, 38), 1.1f);
            int step = Math.Max(14, rect.Width / 5);
            for (int bx = rect.X + step; bx < rect.Right - 4; bx += step)
                g.DrawLine(mortar, bx, rect.Y + t, bx, rect.Bottom - 1);
            using var blade = new Pen(Color.FromArgb(50, 45, 130, 58), 1f);
            for (int i = 9; i < rect.Width - 6; i += 16)
            {
                int bx = rect.X + i;
                g.DrawLine(blade, bx, rect.Y + t + 1, bx + 3, rect.Y + t - 2);
            }
        }

        using var rim = new Pen(Color.FromArgb(200, 255, 255, 255), 1.2f);
        g.DrawLine(rim, rect.X, rect.Y + t, rect.Right, rect.Y + t);
        using var foot = new Pen(Color.FromArgb(55, 0, 0, 0), 1f);
        g.DrawLine(foot, rect.X, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
    }

    private static void DrawFloorSpikes(Graphics g, Level level, int ts)
    {
        int floor = level.HeightInTiles - 1;
        for (int tx = 1; tx < level.WidthInTiles - 1; tx++)
        {
            if (level.Tiles[floor, tx] != TileKind.Empty)
                continue;

            float x0 = tx * ts;
            float y0 = floor * ts;
            int n = 5;
            float step = (ts - 8f) / n;
            for (int i = 0; i < n; i++)
            {
                float lx = x0 + 4 + i * step;
                var spike = new[]
                {
                    new PointF(lx + step * 0.5f, y0 + 4),
                    new PointF(lx, y0 + ts - 2),
                    new PointF(lx + step, y0 + ts - 2)
                };
                using var fill = new SolidBrush(Color.FromArgb(255, 190, 55, 55));
                using var edge = new Pen(Color.FromArgb(255, 120, 20, 20), 1.4f);
                g.FillPolygon(fill, spike);
                g.DrawPolygon(edge, spike);
            }
        }
    }

    private static void DrawStartMarker(Graphics g, Rectangle rect)
    {
        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;
        using var glow = new SolidBrush(Color.FromArgb(90, 100, 180, 255));
        g.FillEllipse(glow, cx - 14, cy - 14, 28, 28);
        using var core = new SolidBrush(Color.FromArgb(90, 150, 255));
        g.FillEllipse(core, cx - 10, cy - 10, 20, 20);
        using var hi = new SolidBrush(Color.FromArgb(200, 230, 255));
        g.FillEllipse(hi, cx - 5, cy - 7, 8, 6);
    }

    private static void DrawFinishFlag(Graphics g, Rectangle rect)
    {
        int px = rect.X + rect.Width / 4;
        int py = rect.Y + rect.Height / 6;
        int ph = rect.Height * 2 / 3;
        using var pole = new Pen(Color.FromArgb(220, 220, 230), 3f) { EndCap = LineCap.Round };
        g.DrawLine(pole, px, py, px, py + ph);

        var flag = new Point[]
        {
            new(px + 2, py + 2),
            new(px + rect.Width * 2 / 3, py + rect.Height / 5),
            new(px + 2, py + rect.Height / 2)
        };
        using var fb = new LinearGradientBrush(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height),
            Color.FromArgb(90, 240, 90), Color.FromArgb(220, 60, 200), 25f);
        g.FillPolygon(fb, flag);
        using var fp = new Pen(Color.FromArgb(180, 40, 120), 1.2f);
        g.DrawPolygon(fp, flag);
    }

    private static void DrawFallingBlock(Graphics g, FallingBlock block)
    {
        var r = new RectangleF(block.X, block.Y, block.Width, block.Height);
        float inset = 3f;
        using (var baseFill = new LinearGradientBrush(r,
                   Color.FromArgb(255, 140, 130, 125), Color.FromArgb(255, 72, 68, 78), LinearGradientMode.ForwardDiagonal))
            g.FillRectangle(baseFill, r);

        var face = new RectangleF(r.X + inset, r.Y + inset, r.Width - inset * 2, r.Height - inset * 2);
        using (var faceBr = new LinearGradientBrush(face,
                   Color.FromArgb(255, 210, 95, 55), Color.FromArgb(255, 145, 55, 35), LinearGradientMode.Vertical))
            g.FillRectangle(faceBr, face);

        using var crack = new Pen(Color.FromArgb(160, 55, 35, 28), 1.2f);
        g.DrawLine(crack, face.Left + 4, face.Top + 6, face.Left + face.Width * 0.45f, face.Bottom - 5);
        using var edge = new Pen(Color.FromArgb(230, 40, 25, 22), 2f);
        g.DrawRectangle(edge, r.X, r.Y, r.Width, r.Height);
        using var hi = new Pen(Color.FromArgb(180, 255, 250, 220), 1.6f);
        g.DrawLine(hi, face.X + 4, face.Y + 4, face.Right - 5, face.Y + 4);
        using var shade = new Pen(Color.FromArgb(100, 0, 0, 0), 1f);
        g.DrawLine(shade, face.Right - 3, face.Y + 6, face.Right - 3, face.Bottom - 4);

        using var warn = new SolidBrush(Color.FromArgb(230, 255, 60, 70));
        float cx = r.X + r.Width / 2f;
        float ty = r.Y - 2;
        g.FillPolygon(warn, new[]
        {
            new PointF(cx - 9, ty + 13),
            new PointF(cx, ty),
            new PointF(cx + 9, ty + 13)
        });
        using var ex = new Pen(Color.FromArgb(240, 120, 15, 15), 2f);
        g.DrawLine(ex, cx - 3, ty + 4, cx + 3, ty + 10);
        g.DrawLine(ex, cx + 3, ty + 4, cx - 3, ty + 10);
    }

    private static void DrawPlayer(Graphics g, Player player)
    {
        float x = player.X;
        float y = player.Y;
        float w = player.Width;
        float h = player.Height;

        using (var sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            g.FillEllipse(sh, x + 4, y + h - 4, w, 9);

        float legW = w * 0.28f;
        float legH = h * 0.22f;
        float legY = y + h - legH;
        using var legBr = new LinearGradientBrush(new RectangleF(x, legY, w, legH),
            Color.FromArgb(255, 38, 52, 110), Color.FromArgb(255, 22, 30, 75), 90f);
        g.FillRectangle(legBr, x + 1f, legY, legW - 1f, legH);
        g.FillRectangle(legBr, x + w - legW, legY, legW - 1f, legH);

        var torso = new RectangleF(x + 0.5f, y + h * 0.38f, w - 1f, h * 0.38f);
        using (var vest = new LinearGradientBrush(torso, Color.FromArgb(255, 72, 125, 255), Color.FromArgb(255, 32, 58, 190), 95f))
        {
            using var path = RoundedRect(torso, 5.5f);
            g.FillPath(vest, path);
            using var stripe = new SolidBrush(Color.FromArgb(255, 255, 220, 80));
            g.FillRectangle(stripe, torso.X + torso.Width * 0.18f, torso.Y + torso.Height * 0.38f, torso.Width * 0.64f, 3.2f);
            using var outline = new Pen(Color.FromArgb(235, 18, 35, 95), 1.15f);
            g.DrawPath(outline, path);
        }

        using (var arm = new SolidBrush(Color.FromArgb(255, 245, 205, 185)))
        {
            g.FillEllipse(arm, x - 1f, y + h * 0.42f, 7f, 11f);
            g.FillEllipse(arm, x + w - 6f, y + h * 0.42f, 7f, 11f);
        }

        float headR = Math.Min(w, h) * 0.44f;
        float hx = x + w / 2f - headR / 2f;
        float hy = y + 1f;
        using (var headBr = new LinearGradientBrush(new RectangleF(hx, hy, headR, headR),
                   Color.FromArgb(255, 255, 218, 200), Color.FromArgb(255, 235, 180, 155), 40f))
            g.FillEllipse(headBr, hx, hy, headR, headR);
        using var face = new Pen(Color.FromArgb(200, 85, 55, 45), 1f);
        g.DrawEllipse(face, hx + 0.5f, hy + 0.5f, headR - 1f, headR - 1f);

        using var cap = new LinearGradientBrush(new RectangleF(hx - 1, hy - 3, headR + 2, headR * 0.42f),
            Color.FromArgb(255, 55, 55, 72), Color.FromArgb(255, 28, 28, 42), 0f);
        var capPath = new GraphicsPath();
        capPath.AddArc(hx - 1, hy - 2, headR + 2, headR * 0.9f, 180, 180);
        g.FillPath(cap, capPath);
        using var capBrim = new Pen(Color.FromArgb(230, 90, 95, 120), 1.2f);
        g.DrawArc(capBrim, hx - 1.5f, hy + headR * 0.18f, headR + 3, headR * 0.75f, 0f, 180f);

        using var eye = new SolidBrush(Color.FromArgb(45, 28, 38, 58));
        g.FillEllipse(eye, hx + headR * 0.28f, hy + headR * 0.38f, 4.2f, 4.2f);
        g.FillEllipse(eye, hx + headR * 0.55f, hy + headR * 0.38f, 4.2f, 4.2f);
        using var glint = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        g.FillEllipse(glint, hx + headR * 0.30f, hy + headR * 0.39f, 1.6f, 1.6f);
        g.FillEllipse(glint, hx + headR * 0.57f, hy + headR * 0.39f, 1.6f, 1.6f);
    }

    private static GraphicsPath RoundedRect(RectangleF b, float r)
    {
        var path = new GraphicsPath();
        float d = r * 2;
        path.AddArc(b.X, b.Y, d, d, 180, 90);
        path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
        path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void DrawVictoryScreen(Graphics g, int w, int h, GameState state, GameProgress progress)
    {
        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                   Color.FromArgb(215, 38, 52, 92),
                   Color.FromArgb(225, 12, 18, 42),
                   LinearGradientMode.Vertical))
            g.FillRectangle(veil, 0, 0, w, h);

        float pad = Math.Min(w, h) * 0.09f;
        var panel = new RectangleF(pad, h * 0.08f, w - 2 * pad, h * 0.76f);
        using (var glow = new Pen(Color.FromArgb(80, 200, 230, 255), 10f))
        {
            using var glowPath = RoundedRect(new RectangleF(panel.X - 1, panel.Y - 1, panel.Width + 2, panel.Height + 2), 22f);
            g.DrawPath(glow, glowPath);
        }

        using (var pbrush = new LinearGradientBrush(panel, Color.FromArgb(248, 48, 62, 112), Color.FromArgb(252, 22, 28, 58), 95f))
        {
            using var path = RoundedRect(panel, 20f);
            g.FillPath(pbrush, path);
            using var border = new Pen(Color.FromArgb(215, 185, 230, 255), 2.6f);
            g.DrawPath(border, path);
        }

        var savedText = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using var titleFont = UiFont(Math.Max(28f, w * 0.05f), FontStyle.Bold);
        using var bodyFont = UiFont(Math.Max(15f, w * 0.028f), FontStyle.Regular);
        using var hintFont = UiFont(13f, FontStyle.Italic);

        var lineFmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoClip
        };

        int id = state.CurrentLevelId;
        int finish = state.LastFinishElapsedMs;
        progress.BestTimeMs.TryGetValue(id, out int best);
        bool isRecord = state.LastVictoryWasRecord;

        float y = panel.Y + 26f;
        float innerW = panel.Width - 40f;
        float textLeft = panel.X + 20f;

        void DrawCenteredLine(string text, Font font, Brush brush)
        {
            SizeF sz = g.MeasureString(text, font, (int)innerW, lineFmt);
            float lineH = Math.Max(sz.Height + 16f, 44f);
            var rect = new RectangleF(textLeft, y, innerW, lineH);
            g.DrawString(text, font, brush, rect, lineFmt);
            y += lineH + 8f;
        }

        DrawCenteredLine("Победа!", titleFont, Brushes.White);
        DrawCenteredLine($"Время: {FormatTime(finish)}", bodyFont, new SolidBrush(Color.FromArgb(250, 238, 245, 255)));
        DrawCenteredLine($"Лучший результат: {FormatTime(best)}", bodyFont, new SolidBrush(Color.FromArgb(235, 215, 228, 248)));
        if (isRecord)
            DrawCenteredLine("Новый рекорд!", UiFont(15f, FontStyle.Bold), new SolidBrush(Color.FromArgb(255, 255, 230, 140)));
        DrawCenteredLine("Enter — к выбору уровня", hintFont, new SolidBrush(Color.FromArgb(215, 200, 210, 230)));

        g.TextRenderingHint = savedText;
    }

    private static void DrawOverlay(Graphics g, GameState state)
    {
        if (state.Phase is GamePhase.Menu or GamePhase.LevelSelect or GamePhase.Victory or GamePhase.Pause)
            return;

        using var font = UiFont(14f, FontStyle.Bold);
        using var sub = UiFont(11f, FontStyle.Regular);
        using var sh = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        using var fg = new SolidBrush(Color.FromArgb(250, 250, 255));
        using var fgSub = new SolidBrush(Color.FromArgb(235, 230, 240, 255));

        if (state.Phase != GamePhase.Defeat)
            return;

        const string a = "Поражение";
        const string b = "Enter — попробовать снова";
        const string c = "M — выйти к выбору уровня";

        g.DrawString(a, font, sh, 15, 73);
        g.DrawString(a, font, fg, 14, 72);
        g.DrawString(b, sub, sh, 15, 100);
        g.DrawString(b, sub, fgSub, 14, 99);
        g.DrawString(c, sub, sh, 15, 124);
        g.DrawString(c, sub, fgSub, 14, 123);
    }
}
