using System.Drawing;
using ChaosRunner.Model;

namespace ChaosRunner.View;

// весь GDI+ тут; модель не меняю, только читаю
public sealed class GameRenderer
{
    private static readonly Color Sky = Color.FromArgb(100, 160, 210);
    private static readonly Color Solid = Color.FromArgb(55, 95, 55);
    private static readonly Color SolidTop = Color.FromArgb(110, 170, 90);
    private static readonly Color StartMarker = Color.FromArgb(80, 120, 200);
    private static readonly Color FinishMarker = Color.FromArgb(80, 180, 100);
    private static readonly Color PlayerFill = Color.FromArgb(220, 80, 80);

    public void Render(Graphics g, Level level, Player player, GameState state)
    {
        g.Clear(Sky);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int ts = level.TileSize;
        using var solidBrush = new SolidBrush(Solid);
        using var topBrush = new SolidBrush(SolidTop);
        using var startB = new SolidBrush(StartMarker);
        using var finB = new SolidBrush(FinishMarker);
        using var plB = new SolidBrush(PlayerFill);

        for (int y = 0; y < level.HeightInTiles; y++)
        {
            for (int x = 0; x < level.WidthInTiles; x++)
            {
                var rect = new Rectangle(x * ts, y * ts, ts, ts);
                if (level.Tiles[y, x] == TileKind.Solid)
                {
                    g.FillRectangle(solidBrush, rect);
                    g.FillRectangle(topBrush, rect.X, rect.Y, ts, ts / 5);
                }

                if (x == level.StartTileX && y == level.StartTileY)
                    g.FillEllipse(startB, rect.X + ts / 4, rect.Y + ts / 4, ts / 2, ts / 2);
                if (x == level.FinishTileX && y == level.FinishTileY)
                    g.FillRectangle(finB, rect.X + ts / 4, rect.Y + ts / 4, ts / 2, ts / 2);
            }
        }

        // игрок — пока овал, не человечек (меньше кода на неделю 1)
        g.FillEllipse(plB, player.X, player.Y, player.Width, player.Height);

        DrawOverlay(g, state);
    }

    private static void DrawOverlay(Graphics g, GameState state)
    {
        using var font = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold);
        using var sh = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
        using var fg = new SolidBrush(Color.White);

        string? a = null, b = null;
        switch (state.Phase)
        {
            case GamePhase.Menu:
                a = "Chaos Runner";
                b = "Enter старт  A/D  W прыжок  Esc пауза";
                break;
            case GamePhase.Pause:
                a = "Пауза";
                b = "Esc продолжить";
                break;
            case GamePhase.GameOver:
                a = "Финиш";
                b = "Enter заново";
                break;
        }

        if (a is null)
            return;

        g.DrawString(a, font, sh, 13, 11);
        g.DrawString(a, font, fg, 12, 10);
        g.DrawString(b!, font, sh, 13, 32);
        g.DrawString(b!, font, fg, 12, 31);
    }
}
