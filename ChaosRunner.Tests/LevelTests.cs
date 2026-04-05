using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

// пару проверок на карту и тик физики
public class LevelTests
{
    [Fact]
    public void CreateWeekOneSample_HasStartFinishAndFloor()
    {
        var level = Level.CreateWeekOneSample();
        Assert.Equal(TileKind.Empty, level.GetTile(level.StartTileX, level.StartTileY));
        Assert.Equal(TileKind.Empty, level.GetTile(level.FinishTileX, level.FinishTileY));
        Assert.Equal(TileKind.Solid, level.GetTile(level.StartTileX, level.StartTileY + 1));
        Assert.NotEqual(level.StartTileX, level.FinishTileX);
    }

    [Fact]
    public void PlacePlayerAtStart_StandsOnFloor()
    {
        var level = Level.CreateWeekOneSample();
        var player = new Player();
        level.PlacePlayerAtStart(player);

        Assert.True(level.IsOnGround(player));
        float expectedBottom = (level.StartTileY + 1) * level.TileSize;
        Assert.InRange(player.Y + player.Height, expectedBottom - 1.5f, expectedBottom + 1.5f);
    }

    [Fact]
    public void ApplyPlatformTick_DoesNotPassThroughWall()
    {
        var tiles = new TileKind[,]
        {
            { TileKind.Solid, TileKind.Solid, TileKind.Solid },
            { TileKind.Solid, TileKind.Empty, TileKind.Solid },
            { TileKind.Solid, TileKind.Solid, TileKind.Solid }
        };
        var level = new Level(tiles, tileSize: 40, 1, 1, 2, 1);
        var player = new Player();
        level.PlacePlayerAtStart(player);

        level.ApplyPlatformTick(player, horizontalInput: 10f, jumpEdge: false);

        Assert.True(player.X + player.Width <= 80.02f);
        Assert.False(level.IntersectsSolid(player));
    }

    [Fact]
    public void PlayerReachedFinish_OverlapsFinishTile()
    {
        var tiles = new TileKind[,]
        {
            { TileKind.Empty, TileKind.Empty }
        };
        var level = new Level(tiles, tileSize: 40, 0, 0, 1, 0);
        var player = new Player
        {
            X = 40 + 2f,
            Y = 8f
        };

        Assert.True(level.PlayerReachedFinish(player));
    }

    [Fact]
    public void JumpEdge_AppliesWhenOnGround()
    {
        var tiles = new TileKind[,]
        {
            { TileKind.Empty },
            { TileKind.Empty },
            { TileKind.Solid }
        };
        var level = new Level(tiles, tileSize: 40, 0, 1, 0, 0);
        var player = new Player();
        player.X = 9f;
        player.Y = 2 * 40 - player.Height;

        level.ApplyPlatformTick(player, 0f, jumpEdge: true);

        Assert.True(player.VelocityY < -5f);
    }
}
