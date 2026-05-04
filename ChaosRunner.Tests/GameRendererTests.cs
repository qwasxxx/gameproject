using System.Drawing;
using ChaosRunner.Model;
using ChaosRunner.View;
using Xunit;

namespace ChaosRunner.Tests;

public class GameRendererTests
{
    [Fact]
    public void Render_AllPhases_DoesNotThrow()
    {
        var renderer = new GameRenderer();
        var level = Level.CreateWeekOneSample();
        int pw = level.WidthInTiles * level.TileSize;
        int ph = level.HeightInTiles * level.TileSize;
        using var bmp = new Bitmap(Math.Max(800, pw + 32), Math.Max(600, ph + 32));
        using var g = Graphics.FromImage(bmp);
        var player = new Player();
        level.PlacePlayerAtStart(player);
        var progress = new GameProgress { MaxUnlockedLevel = Level.LevelCount };

        foreach (GamePhase phase in Enum.GetValues<GamePhase>())
        {
            var state = new GameState
            {
                Phase = phase,
                MaxUnlockedLevel = Level.LevelCount,
                LevelSelectIndex = 1,
                RunElapsedMs = 12345,
                RunDeaths = 2,
                ActiveHallucinationLabel = "test",
                LastFinishElapsedMs = 5000,
                LastVictoryWasRecord = true,
                CurrentLevelId = 2,
                ShakeOffsetX = 1,
                ShakeOffsetY = -1
            };

            renderer.Render(g, new Rectangle(0, 0, bmp.Width, bmp.Height), level, player, state, progress);
        }
    }
}
