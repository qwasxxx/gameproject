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

    [Fact]
    public void CreateLevelOne_HasStartFinishPitAndGapFloor()
    {
        var level = Level.CreateLevelOne();
        Assert.True(level.WidthInTiles > 0);
        Assert.Equal(TileKind.Empty, level.GetTile(level.StartTileX, level.StartTileY));
        Assert.Equal(TileKind.Empty, level.GetTile(level.FinishTileX, level.FinishTileY));
        int bottom = level.HeightInTiles - 1;
        bool hasFloorGap = false;
        for (int x = 1; x < level.WidthInTiles - 1; x++)
        {
            if (level.Tiles[bottom, x] == TileKind.Empty)
                hasFloorGap = true;
        }

        Assert.True(hasFloorGap);
    }

    [Fact]
    public void PlayerFellOutOfWorld_BelowMapHeight()
    {
        var level = Level.CreateWeekOneSample();
        var player = new Player { Y = level.WorldPixelHeight + 10f };
        Assert.True(level.PlayerFellOutOfWorld(player));
    }

    [Fact]
    public void ResetDynamicState_ClearsFallingBlocks()
    {
        var level = Level.CreateLevelOne();
        level.FallingBlocks.Add(new FallingBlock { X = 0, Y = 0, Width = 10, Height = 10 });
        level.ResetDynamicState();
        Assert.Empty(level.FallingBlocks);
    }

    [Fact]
    public void PitTile_IsNotSolidTile()
    {
        Assert.False(Level.IsSolidTile(TileKind.Pit));
    }
}
