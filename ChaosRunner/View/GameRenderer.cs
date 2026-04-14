using System.Drawing;
using System.Drawing.Drawing2D;
using ChaosRunner.Model;

namespace ChaosRunner.View;

public sealed class GameRenderer
{
    public void Render(Graphics g, Level level, Player player, GameState state)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int pixelW = level.WidthInTiles * level.TileSize;
        int pixelH = level.HeightInTiles * level.TileSize;

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

        if (state.Phase == GamePhase.Victory)
            DrawVictoryScreen(g, pixelW, pixelH);
        else
            DrawOverlay(g, state);
    }

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

    private static void DrawGroundOrCeilingTile(Graphics g, Rectangle rect, bool ceiling)
    {
        var dark = ceiling ? Color.FromArgb(75, 72, 88) : Color.FromArgb(48, 105, 62);
        var light = ceiling ? Color.FromArgb(140, 135, 155) : Color.FromArgb(105, 185, 98);

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
        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                   Color.FromArgb(210, 40, 55, 95),
                   Color.FromArgb(220, 15, 25, 45),
                   LinearGradientMode.Vertical))
        {
            g.FillRectangle(veil, 0, 0, w, h);
        }

        float pad = Math.Min(w, h) * 0.1f;
        var panel = new RectangleF(pad, h * 0.12f, w - 2 * pad, h * 0.62f);
        using (var pbrush = new LinearGradientBrush(panel, Color.FromArgb(255, 55, 75, 115), Color.FromArgb(255, 25, 35, 65), 90f))
        {
            using var path = RoundedRect(panel, 20f);
            g.FillPath(pbrush, path);
            using var border = new Pen(Color.FromArgb(180, 130, 200, 255), 3f);
            g.DrawPath(border, path);
        }

        using var titleFont = new Font(FontFamily.GenericSansSerif, Math.Max(24f, w * 0.052f), FontStyle.Bold);
        using var bodyFont = new Font(FontFamily.GenericSansSerif, Math.Max(13f, w * 0.024f), FontStyle.Regular);
        using var hintFont = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Italic);

        var center = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.None
        };

        const string title = "Победа!";
        const string sub = "Вы дошли до финиша";
        const string hint = "Enter — играть снова";

        float titleH = g.MeasureString(title, titleFont, w, center).Height;
        float y1 = h * 0.22f;
        g.DrawString(title, titleFont, Brushes.White, new RectangleF(0, y1, w, titleH + 6), center);

        float subH = g.MeasureString(sub, bodyFont, w, center).Height;
        float y2 = y1 + titleH + 22f;
        g.DrawString(sub, bodyFont, new SolidBrush(Color.FromArgb(235, 220, 235)), new RectangleF(0, y2, w, subH + 4), center);

        float y3 = y2 + subH + 28f;
        g.DrawString(hint, hintFont, new SolidBrush(Color.FromArgb(200, 190, 210)), new RectangleF(0, y3, w, 36f), center);
    }

    private static void DrawOverlay(Graphics g, GameState state)
    {
        using var font = new Font(FontFamily.GenericSansSerif, 13f, FontStyle.Bold);
        using var sh = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        using var fg = new SolidBrush(Color.FromArgb(250, 250, 255));

        string? a = null, b = null;
        switch (state.Phase)
        {
            case GamePhase.Menu:
                a = "Chaos Runner";
                b = "Enter — старт   A/D   W — прыжок   Esc — пауза";
                break;
            case GamePhase.Pause:
                a = "Пауза";
                b = "Esc — продолжить";
                break;
            case GamePhase.Defeat:
                a = "Поражение";
                b = "Enter — заново";
                break;
        }

        if (a is null)
            return;

        g.DrawString(a, font, sh, 14, 12);
        g.DrawString(a, font, fg, 13, 11);
        g.DrawString(b!, font, sh, 14, 36);
        g.DrawString(b!, font, fg, 13, 35);
    }
}
