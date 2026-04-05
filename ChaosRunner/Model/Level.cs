namespace ChaosRunner.Model;

// карта + за один тик таймера: гравитация, прыжок, упёрся в блок — откат
public sealed class Level
{
    public const float Gravity = 0.62f;
    public const float JumpVelocity = -12.4f;
    public const float MaxFallSpeed = 15f;

    public Level(TileKind[,] tiles, int tileSize, int startTileX, int startTileY, int finishTileX, int finishTileY)
    {
        Tiles = tiles;
        TileSize = tileSize;
        StartTileX = startTileX;
        StartTileY = startTileY;
        FinishTileX = finishTileX;
        FinishTileY = finishTileY;
    }

    public TileKind[,] Tiles { get; }
    public int TileSize { get; }
    public int StartTileX { get; }
    public int StartTileY { get; }
    public int FinishTileX { get; }
    public int FinishTileY { get; }

    public int WidthInTiles => Tiles.GetLength(1);
    public int HeightInTiles => Tiles.GetLength(0);

    public TileKind GetTile(int tileX, int tileY)
    {
        // за краем карты слева/справа/сверху — как стена; под низом пустота (у меня пол цельный)
        if (tileX < 0 || tileX >= WidthInTiles || tileY < 0)
            return TileKind.Solid;
        if (tileY >= HeightInTiles)
            return TileKind.Empty;
        return Tiles[tileY, tileX];
    }

    public void PlacePlayerAtStart(Player player)
    {
        int floorY = StartTileY + 1;
        player.X = StartTileX * TileSize + (TileSize - player.Width) / 2f;
        player.Y = floorY * TileSize - player.Height;
    }

    public void ApplyPlatformTick(Player player, float horizontalInput, bool jumpEdge)
    {
        if (jumpEdge && IsOnGround(player))
            player.VelocityY = JumpVelocity;

        player.VelocityY += Gravity;
        if (player.VelocityY > MaxFallSpeed)
            player.VelocityY = MaxFallSpeed;

        player.X += horizontalInput * player.MoveSpeed;
        ResolveHorizontal(player, horizontalInput);

        float vy = player.VelocityY;
        player.Y += vy;
        ResolveVertical(player, vy);
        if (IsOnGround(player) && player.VelocityY >= 0)
            player.VelocityY = 0;
    }

    public bool IsOnGround(Player player)
    {
        player.Y += 1f;
        bool hit = IntersectsSolid(player);
        player.Y -= 1f;
        return hit;
    }

    public bool PlayerReachedFinish(Player player)
    {
        float fx = FinishTileX * TileSize;
        float fy = FinishTileY * TileSize;
        return player.X + player.Width > fx && player.X < fx + TileSize
               && player.Y + player.Height > fy && player.Y < fy + TileSize;
    }

    public bool IntersectsSolid(Player player)
    {
        float l = player.X;
        float t = player.Y;
        float r = player.X + player.Width;
        float b = player.Y + player.Height;

        int minTx = (int)Math.Floor(l / TileSize);
        int minTy = (int)Math.Floor(t / TileSize);
        int maxTx = (int)Math.Floor((r - 0.001f) / TileSize);
        int maxTy = (int)Math.Floor((b - 0.001f) / TileSize);

        for (int ty = minTy; ty <= maxTy; ty++)
        {
            for (int tx = minTx; tx <= maxTx; tx++)
            {
                if (GetTile(tx, ty) == TileKind.Solid)
                    return true;
            }
        }

        return false;
    }

    private void ResolveHorizontal(Player player, float horizontalInput)
    {
        if (!IntersectsSolid(player))
            return;
        int guard = 0;
        float step = horizontalInput > 0 ? -1f : horizontalInput < 0 ? 1f : -1f;
        while (IntersectsSolid(player) && guard++ < TileSize * 2)
            player.X += step;
    }

    private void ResolveVertical(Player player, float vyBefore)
    {
        int guard = 0;
        while (IntersectsSolid(player) && guard++ < TileSize * 2)
        {
            if (vyBefore > 0)
                player.Y -= 1f;
            else
                player.Y += 1f;
        }

        if (guard > 0)
            player.VelocityY = 0;
    }

    // набросок карты из строк, внизу пол; S/F — где старт и финиш (пустые клетки)
    public static Level CreateWeekOneSample()
    {
        const int inner = 26;
        string[] inners =
        {
            new string('.', inner),
            new string('.', inner),
            new string('.', inner),
            new string('.', inner),
            "S" + new string('.', 14) + "F" + new string('.', 10)
        };

        int w = inner + 2;
        int h = inners.Length + 1;
        var tiles = new TileKind[h, w];
        int sx = 0, sy = 0, fx = 0, fy = 0;

        for (int y = 0; y < inners.Length; y++)
        {
            for (int x = 0; x < w; x++)
            {
                char c = x == 0 || x == w - 1 ? '#' : inners[y][x - 1];
                switch (c)
                {
                    case '#':
                        tiles[y, x] = TileKind.Solid;
                        break;
                    case 'S':
                        tiles[y, x] = TileKind.Empty;
                        sx = x;
                        sy = y;
                        break;
                    case 'F':
                        tiles[y, x] = TileKind.Empty;
                        fx = x;
                        fy = y;
                        break;
                    default:
                        tiles[y, x] = TileKind.Empty;
                        break;
                }
            }
        }

        for (int x = 0; x < w; x++)
            tiles[h - 1, x] = TileKind.Solid;

        return new Level(tiles, tileSize: 40, sx, sy, fx, fy);
    }
}
