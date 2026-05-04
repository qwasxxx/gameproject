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

    [Fact]
    public void GetTile_OutsideLeftRight_ReturnsSolid()
    {
        var tiles = new TileKind[,] { { TileKind.Empty, TileKind.Empty } };
        var level = new Level(tiles, 40, 0, 0, 1, 0);
        Assert.Equal(TileKind.Solid, level.GetTile(-1, 0));
        Assert.Equal(TileKind.Solid, level.GetTile(2, 0));
    }

    [Fact]
    public void GetTile_AboveMap_ReturnsSolid_Below_ReturnsEmpty()
    {
        var tiles = new TileKind[,] { { TileKind.Empty } };
        var level = new Level(tiles, 40, 0, 0, 0, 0);
        Assert.Equal(TileKind.Solid, level.GetTile(0, -1));
        Assert.Equal(TileKind.Empty, level.GetTile(0, 1));
    }

    [Fact]
    public void IsSolidTile_EmptyAndSolid()
    {
        Assert.True(Level.IsSolidTile(TileKind.Solid));
        Assert.False(Level.IsSolidTile(TileKind.Empty));
    }

    [Fact]
    public void Create_InvalidId_FallsBackToLevelOne()
    {
        var a = Level.Create(1);
        var b = Level.Create(999);
        Assert.Equal(a.WidthInTiles, b.WidthInTiles);
        Assert.Equal(a.HeightInTiles, b.HeightInTiles);
        Assert.Equal(a.StartTileX, b.StartTileX);
        Assert.Equal(a.StartTileY, b.StartTileY);
    }

    [Fact]
    public void PlayerTouchesSpikes_InPitGap_DetectsDanger()
    {
        var tiles = new TileKind[3, 5];
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 5; x++)
            tiles[y, x] = TileKind.Solid;
        tiles[2, 1] = TileKind.Empty;
        tiles[2, 2] = TileKind.Empty;
        tiles[2, 3] = TileKind.Empty;
        var level = new Level(tiles, tileSize: 40, 0, 0, 0, 0);
        float floorY = 2 * 40;
        float dangerMid = floorY + 40 * 0.38f + 5f;
        var player = new Player { X = 80f };
        player.Y = dangerMid - player.Height + 2f;
        Assert.True(level.PlayerTouchesSpikes(player));
    }
}
