using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class FallingBlockTests
{
    [Fact]
    public void FallingBlocks_DoNotActAsSolidTiles()
    {
        var tiles = new TileKind[,]
        {
            { TileKind.Solid, TileKind.Solid, TileKind.Solid },
            { TileKind.Solid, TileKind.Empty, TileKind.Solid },
            { TileKind.Solid, TileKind.Solid, TileKind.Solid }
        };
        var level = new Level(tiles, tileSize: 40, 1, 1, 1, 1);
        var player = new Player { X = 45f, Y = 45f };
        level.FallingBlocks.Add(new FallingBlock { X = 44f, Y = 44f, Width = 22f, Height = 22f });
        Assert.False(level.IntersectsTileSolid(player));
        Assert.False(level.IntersectsSolid(player));
    }

    [Fact]
    public void UpdateFallingBlocks_MovingBlockCrushesPlayer()
    {
        var tiles = new TileKind[,]
        {
            { TileKind.Empty, TileKind.Empty, TileKind.Empty },
            { TileKind.Solid, TileKind.Solid, TileKind.Solid }
        };
        var level = new Level(tiles, tileSize: 40, 0, 0, 2, 0);
        var player = new Player { X = 50f, Y = 0f };

        level.FallingBlocks.Add(new FallingBlock
        {
            X = 48f,
            Y = -20f,
            Width = 30f,
            Height = 20f,
            VelocityY = 8f
        });

        bool crushed = level.UpdateFallingBlocks(player);
        Assert.True(crushed);
    }
}
