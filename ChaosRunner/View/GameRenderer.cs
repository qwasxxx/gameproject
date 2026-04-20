using System.Drawing;
using System.Drawing.Drawing2D;
using ChaosRunner.Model;

namespace ChaosRunner.View;

public sealed class GameRenderer
{
    public void Render(Graphics g, Level level, Player player, GameState state, HallucinationContext hx)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var shake = hx.Shake;
        g.TranslateTransform(shake.OffsetX, shake.OffsetY);
        try
        {
            RenderWorld(g, level, player, state, hx);
        }
        finally
        {
            g.ResetTransform();
        }
    }

    private static void RenderWorld(Graphics g, Level level, Player player, GameState state, HallucinationContext hx)
    {
        int vw = LevelFactory.ViewportPixelWidth;
        int vh = LevelFactory.ViewportPixelHeight;
        int lw = level.WidthInTiles * level.TileSize;
        int lh = level.HeightInTiles * level.TileSize;
        int ox = Math.Max(0, (vw - lw) / 2);
        int oy = Math.Max(0, (vh - lh) / 2);

        using (var letter = new SolidBrush(Color.FromArgb(255, 14, 18, 32)))
            g.FillRectangle(letter, 0, 0, vw, vh);

        g.TranslateTransform(ox, oy);
        try
        {
            DrawSky(g, lw, lh);
            DrawClouds(g, lw, lh);

            int ts = level.TileSize;
            int tw = level.WidthInTiles;
            int th = level.HeightInTiles;
            for (int ty = 0; ty < th; ty++)
            {
                int tx = 0;
                while (tx < tw)
                {
                    if (!IsBrickAppearance(level.Tiles[ty, tx]))
                    {
                        tx++;
                        continue;
                    }

                    int x0 = tx;
                    TileKind segKind = level.Tiles[ty, tx];
                    while (tx < tw && level.Tiles[ty, tx] == segKind && IsBrickAppearance(level.Tiles[ty, tx]))
                        tx++;

                    var rect = new Rectangle(x0 * ts, ty * ts, (tx - x0) * ts, ts);
                    bool ghostBrick = segKind == TileKind.GhostSolid;
                    DrawGroundOrCeilingTile(g, rect, ty == 0, ghostBrick && level.GhostTileHallucinationActive);
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
        finally
        {
            g.ResetTransform();
        }

        if (state.Phase == GamePhase.Victory)
            DrawVictoryScreen(g, vw, vh);
        else
            DrawOverlay(g, state, hx, vw, vh);
    }

    private static bool IsBrickAppearance(TileKind k) =>
        k is TileKind.Solid or TileKind.GhostSolid;

    private static void DrawSky(Graphics g, int w, int h)
    {
        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, w, h),
            Color.FromArgb(120, 185, 235),
            Color.FromArgb(210, 230, 250),
            LinearGradientMode.Vertical);
        g.FillRectangle(brush, 0, 0, w, h);
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

    private static void DrawGroundOrCeilingTile(Graphics g, Rectangle rect, bool ceiling, bool ghostGlitch = false)
    {
        var dark = ceiling ? Color.FromArgb(75, 72, 88) : Color.FromArgb(48, 105, 62);
        var light = ceiling ? Color.FromArgb(140, 135, 155) : Color.FromArgb(105, 185, 98);
        if (ghostGlitch)
        {
            dark = Color.FromArgb(88, 40, 95, 120);
            light = Color.FromArgb(118, 160, 200, 230);
        }

        float ang = ceiling ? 35f : 25f;
        using (var brush = new LinearGradientBrush(rect, light, dark, ang))
        {
            g.FillRectangle(brush, rect);
        }

        int t = Math.Max(4, rect.Height / 5);
        using var topGrass = new SolidBrush(ceiling ? Color.FromArgb(160, 155, 175) : Color.FromArgb(120, 205, 108));
        g.FillRectangle(topGrass, rect.X, rect.Y, rect.Width, t);

        if (!ceiling && rect.Width > 20)
        {
            using var blade = new Pen(Color.FromArgb(45, 50, 120, 55), 1f);
            for (int i = 8; i < rect.Width - 4; i += 14)
            {
                int bx = rect.X + i;
                g.DrawLine(blade, bx, rect.Y + 2, bx + 3, rect.Y - 1);
            }
        }

        using var rim = new Pen(Color.FromArgb(35, 255, 255, 255), 1f);
        g.DrawLine(rim, rect.X, rect.Y + t, rect.Right, rect.Y + t);
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
        using var fill = new LinearGradientBrush(r,
            Color.FromArgb(255, 255, 140, 40),
            Color.FromArgb(255, 200, 40, 20),
            LinearGradientMode.Vertical);
        g.FillRectangle(fill, r);
        using var danger = new Pen(Color.FromArgb(255, 160, 30, 10), 2.5f);
        g.DrawRectangle(danger, r.X, r.Y, r.Width, r.Height);
        using var hi = new Pen(Color.FromArgb(200, 255, 255, 220), 1.5f);
        g.DrawLine(hi, r.X + 4, r.Y + 4, r.Right - 6, r.Y + 4);

        using var warn = new SolidBrush(Color.FromArgb(230, 255, 50, 50));
        float cx = r.X + r.Width / 2f;
        float ty = r.Y - 2;
        g.FillPolygon(warn, new[]
        {
            new PointF(cx - 8, ty + 12),
            new PointF(cx, ty),
            new PointF(cx + 8, ty + 12)
        });
    }

    private static void DrawPlayer(Graphics g, Player player)
    {
        float x = player.X;
        float y = player.Y;
        float w = player.Width;
        float h = player.Height;

        using (var sh = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
        {
            g.FillEllipse(sh, x + 3, y + h - 6, w - 2, 8);
        }

        var body = new RectangleF(x, y + h * 0.28f, w, h * 0.62f);
        using (var bodyBr = new LinearGradientBrush(body, Color.FromArgb(255, 95, 140, 255), Color.FromArgb(255, 35, 70, 200), 90f))
        {
            float rr = 6f;
            using var path = RoundedRect(body, rr);
            g.FillPath(bodyBr, path);
            using var outline = new Pen(Color.FromArgb(220, 20, 40, 120), 1.3f);
            g.DrawPath(outline, path);
        }

        float headR = Math.Min(w, h) * 0.38f;
        float hx = x + w / 2f - headR / 2f;
        float hy = y;
        using (var headBr = new LinearGradientBrush(new RectangleF(hx, hy, headR, headR), Color.FromArgb(255, 255, 200, 190), Color.FromArgb(255, 230, 170, 155), 45f))
        {
            g.FillEllipse(headBr, hx, hy, headR, headR);
        }

        using var eye = new SolidBrush(Color.FromArgb(40, 30, 55));
        g.FillEllipse(eye, hx + headR * 0.28f, hy + headR * 0.35f, 4, 4);
        g.FillEllipse(eye, hx + headR * 0.55f, hy + headR * 0.35f, 4, 4);
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

    private static void DrawVictoryScreen(Graphics g, int w, int h)
    {
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                   Color.FromArgb(200, 18, 22, 42),
                   Color.FromArgb(215, 8, 12, 28),
                   LinearGradientMode.Vertical))
        {
            g.FillRectangle(veil, 0, 0, w, h);
        }

        float cardW = Math.Clamp(w * 0.52f, 340f, 520f);
        float cardH = Math.Clamp(h * 0.55f, 260f, 380f);
        var card = new RectangleF((w - cardW) * 0.5f, (h - cardH) * 0.5f, cardW, cardH);
        float rr = 22f;

        using (var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
        {
            var sh = new RectangleF(card.X + 6, card.Y + 8, card.Width, card.Height);
            using var shPath = RoundedRect(sh, rr);
            g.FillPath(shadow, shPath);
        }

        using (var cardBrush = new LinearGradientBrush(card,
                   Color.FromArgb(255, 42, 58, 98),
                   Color.FromArgb(255, 22, 28, 52),
                   95f))
        using (var cardPath = RoundedRect(card, rr))
        {
            g.FillPath(cardBrush, cardPath);
            using var glow = new Pen(Color.FromArgb(200, 120, 200, 255), 2.2f);
            g.DrawPath(glow, cardPath);
            using var inner = new Pen(Color.FromArgb(60, 255, 255, 255), 1f);
            var inset = card;
            inset.Inflate(-4f, -4f);
            using var innerPath = RoundedRect(inset, rr - 4f);
            g.DrawPath(inner, innerPath);
        }

        var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        float titleSize = FitFontSize(g, "Победа!", FontFamily.GenericSansSerif, Math.Min(40f, cardW * 0.14f), 22f, cardW - 48f, FontStyle.Bold);
        using var titleFont = new Font(FontFamily.GenericSansSerif, titleSize, FontStyle.Bold);
        using var bodyFont = new Font(FontFamily.GenericSansSerif, Math.Max(13f, cardW * 0.038f), FontStyle.Regular);
        using var smallFont = new Font(FontFamily.GenericSansSerif, 11.5f, FontStyle.Regular);

        float yTop = card.Y + 36f;
        var titleRect = new RectangleF(card.X, yTop, card.Width, titleFont.Height + 8f);
        g.DrawString("Победа!", titleFont, new SolidBrush(Color.FromArgb(255, 248, 252, 255)), titleRect, fmt);

        float ySub = titleRect.Bottom + 14f;
        var subRect = new RectangleF(card.X + 20f, ySub, card.Width - 40f, bodyFont.Height * 2.2f);
        g.DrawString("Вы дошли до финиша", bodyFont, new SolidBrush(Color.FromArgb(235, 200, 210, 235)), subRect, fmt);

        float btnY = subRect.Bottom + 28f;
        float btnW = (card.Width - 52f) * 0.5f;
        float btnH = 44f;
        float gap = 16f;
        float btnX0 = card.X + 26f;

        DrawVictoryActionRow(g, btnX0, btnY, btnW, btnH, "Enter", "Играть снова", smallFont);
        DrawVictoryActionRow(g, btnX0 + btnW + gap, btnY, btnW, btnH, "Esc", "Главное меню", smallFont);

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }

    private static void DrawVictoryActionRow(Graphics g, float x, float y, float bw, float bh, string key, string label, Font smallFont)
    {
        var r = new RectangleF(x, y, bw, bh);
        using (var b = new LinearGradientBrush(r, Color.FromArgb(255, 58, 72, 110), Color.FromArgb(255, 36, 44, 72), 90f))
        using (var path = RoundedRect(r, 12f))
        {
            g.FillPath(b, path);
            using var edge = new Pen(Color.FromArgb(160, 160, 210, 255), 1.4f);
            g.DrawPath(edge, path);
        }

        var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var keyFont = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Bold);
        g.DrawString(key, keyFont, new SolidBrush(Color.FromArgb(255, 190, 220, 255)), new RectangleF(x, y + 6f, bw, 16f), fmt);
        g.DrawString(label, smallFont, Brushes.White, new RectangleF(x, y + 22f, bw, 20f), fmt);
    }

    private static float FitFontSize(Graphics g, string text, FontFamily family, float startSize, float minSize, float maxWidth, FontStyle style)
    {
        for (float fs = startSize; fs >= minSize; fs -= 1f)
        {
            using var f = new Font(family, fs, style);
            var sz = g.MeasureString(text, f);
            if (sz.Width <= maxWidth)
                return fs;
        }

        return minSize;
    }

    private static void DrawMainMenu(Graphics g, GameState state, int vw, int vh, Font uiFont, Brush sh, Brush fg)
    {
        _ = fg;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, vw, vh),
                   Color.FromArgb(120, 16, 24, 48),
                   Color.FromArgb(140, 10, 14, 32),
                   LinearGradientMode.Vertical))
            g.FillRectangle(veil, 0, 0, vw, vh);

        float panelW = Math.Clamp(vw * 0.68f, 400f, 640f);
        float panelH = Math.Clamp(vh * 0.58f, 300f, 420f);
        var panel = new RectangleF((vw - panelW) * 0.5f, (vh - panelH) * 0.5f, panelW, panelH);
        float pr = 20f;

        using (var drop = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
        {
            var d = new RectangleF(panel.X + 5, panel.Y + 6, panel.Width, panel.Height);
            using var dp = RoundedRect(d, pr);
            g.FillPath(drop, dp);
        }

        using (var panelBr = new LinearGradientBrush(panel,
                   Color.FromArgb(238, 38, 46, 72),
                   Color.FromArgb(248, 20, 26, 48),
                   92f))
        using (var pp = RoundedRect(panel, pr))
        {
            g.FillPath(panelBr, pp);
            using var border = new Pen(Color.FromArgb(175, 110, 165, 235), 2f);
            g.DrawPath(border, pp);
        }

        float titleMaxW = panel.Width - 56f;
        float titleSize = FitFontSize(g, "Chaos Runner", FontFamily.GenericSansSerif, Math.Min(34f, panelW * 0.085f), 18f, titleMaxW, FontStyle.Bold);
        using var titleFont = new Font(FontFamily.GenericSansSerif, titleSize, FontStyle.Bold);
        using var lineFont = new Font(FontFamily.GenericSansSerif, 12.5f, FontStyle.Regular);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        var titleRect = new RectangleF(panel.X + 8f, panel.Y + 26f, panel.Width - 16f, titleFont.Height + 10f);
        g.DrawString("Chaos Runner", titleFont, sh, new RectangleF(titleRect.X + 1, titleRect.Y + 1, titleRect.Width, titleRect.Height), center);
        g.DrawString("Chaos Runner", titleFont, new SolidBrush(Color.FromArgb(255, 252, 253, 255)), titleRect, center);

        float rowY = titleRect.Bottom + 32f;
        float rowH = 42f;
        float rowPad = 12f;

        for (int i = 0; i < LevelFactory.Count; i++)
        {
            bool sel = i == state.MenuLevelIndex;
            var rowRect = new RectangleF(panel.X + rowPad, rowY, panel.Width - 2 * rowPad, rowH);
            if (sel)
            {
                using (var hi = new SolidBrush(Color.FromArgb(210, 64, 82, 138)))
                using (var hp = RoundedRect(rowRect, 11f))
                    g.FillPath(hi, hp);
                using var outline = new Pen(Color.FromArgb(200, 255, 215, 130), 1.6f);
                using var hp2 = RoundedRect(rowRect, 11f);
                g.DrawPath(outline, hp2);
            }

            string line = (sel ? "▸  " : "    ") + LevelFactory.GetDisplayName(i);
            using var brush = new SolidBrush(sel
                ? Color.FromArgb(255, 255, 238, 170)
                : Color.FromArgb(232, 208, 216, 238));
            var textRect = new RectangleF(rowRect.X + 16f, rowRect.Y, rowRect.Width - 32f, rowRect.Height);
            g.DrawString(line, lineFont, brush, textRect, center);
            rowY += rowH + 10f;
        }

        const string hint = "Стрелки ↑↓ — выбор     Enter — старт";
        var hintRect = new RectangleF(panel.X, panel.Bottom - 48f, panel.Width, 34f);
        g.DrawString(hint, uiFont, sh, new RectangleF(hintRect.X + 1, hintRect.Y + 1, hintRect.Width, hintRect.Height), center);
        g.DrawString(hint, uiFont, new SolidBrush(Color.FromArgb(236, 218, 224, 242)), hintRect, center);

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }

    private static void DrawOverlay(Graphics g, GameState state, HallucinationContext hx, int pixelW, int pixelH)
    {
        using var font = new Font(FontFamily.GenericSansSerif, 13f, FontStyle.Bold);
        using var sh = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        using var fg = new SolidBrush(Color.FromArgb(250, 250, 255));

        if (state.Phase == GamePhase.Menu)
        {
            DrawMainMenu(g, state, pixelW, pixelH, font, sh, fg);
            return;
        }

        string? a = null, b = null;
        switch (state.Phase)
        {
            case GamePhase.Pause:
                a = "Пауза";
                b = "Esc — продолжить\nM — главное меню";
                break;
            case GamePhase.Defeat:
                a = "Поражение";
                b = "Enter — заново\nEsc — меню";
                break;
        }

        if (a is null)
        {
            DrawHudPlaying(g, state, hx, font, sh, fg);
            return;
        }

        g.DrawString(a, font, sh, 14, 12);
        g.DrawString(a, font, fg, 13, 11);
        using (var hintFont = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Regular))
        {
            g.DrawString(b!, hintFont, sh, 14, 36);
            g.DrawString(b!, hintFont, fg, 13, 35);
        }
    }

    private static void DrawHudPlaying(Graphics g, GameState state, HallucinationContext hx, Font font, Brush sh, Brush fg)
    {
        string keys = $"Клавиши: ←{hx.Input.MoveLeft}  →{hx.Input.MoveRight}  прыжок {hx.Input.Jump}";
        if (hx.Input.InvertHorizontal)
            keys += "  [инверсия]";
        string ghost = state.CurrentLevelIndex == 0 && hx.Level != null && hx.Level.GhostTileHallucinationActive
            ? "  [призрачные блоки]"
            : "";
        string line = keys + ghost;
        g.DrawString(line, font, sh, 8, 6);
        g.DrawString(line, font, fg, 7, 5);
    }
}
