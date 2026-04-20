namespace ChaosRunner.Model;

public sealed class Level
{
    public const float Gravity = 0.62f;
    public const float JumpVelocity = -12.4f;
    public const float MaxFallSpeed = 15f;
    public const float BlockFallSpeedCap = 18f;

    private readonly int[] _fallSpawnTileXs;
    private readonly int _spawnIntervalTicks;
    private int _spawnCooldownTicks;
    private int _spawnColumnIndex;

    public Level(
        TileKind[,] tiles,
        int tileSize,
        int startTileX,
        int startTileY,
        int finishTileX,
        int finishTileY,
        int[]? fallSpawnTileXs = null,
        int spawnIntervalTicks = 72)
    {
        Tiles = tiles;
        TileSize = tileSize;
        StartTileX = startTileX;
        StartTileY = startTileY;
        FinishTileX = finishTileX;
        FinishTileY = finishTileY;
        _fallSpawnTileXs = fallSpawnTileXs ?? Array.Empty<int>();
        _spawnIntervalTicks = spawnIntervalTicks;
        _spawnCooldownTicks = Math.Min(10, spawnIntervalTicks);
        FallingBlocks = new List<FallingBlock>();
    }

    public TileKind[,] Tiles { get; }
    public int TileSize { get; }
    public int StartTileX { get; }
    public int StartTileY { get; }
    public int FinishTileX { get; }
    public int FinishTileY { get; }

    public List<FallingBlock> FallingBlocks { get; }

    /// <summary>Активна ли галлюцинация «призрачных» блоков (меняет коллизию GhostSolid / DisguisedSolid).</summary>
    public bool GhostTileHallucinationActive { get; set; }

    public int WidthInTiles => Tiles.GetLength(1);
    public int HeightInTiles => Tiles.GetLength(0);

    public float WorldPixelHeight => HeightInTiles * TileSize;

    /// <summary>Только базовая стена (для тестов и простых проверок).</summary>
    public static bool IsSolidTile(TileKind k) => k == TileKind.Solid;

    /// <summary>Коллизия с учётом режима призрачных блоков.</summary>
    public bool IsBlockingTile(TileKind k)
    {
        if (GhostTileHallucinationActive)
        {
            return k switch
            {
                TileKind.Solid => true,
                TileKind.GhostSolid => false,
                TileKind.DisguisedSolid => true,
                _ => false
            };
        }

        return k switch
        {
            TileKind.Solid => true,
            TileKind.GhostSolid => true,
            TileKind.DisguisedSolid => false,
            _ => false
        };
    }

    public TileKind GetTile(int tileX, int tileY)
    {
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

    public bool PlayerFellOutOfWorld(Player player) =>
        player.Y > WorldPixelHeight;

    public bool PlayerTouchesSpikes(Player player)
    {
        int floorY = HeightInTiles - 1;
        int ts = TileSize;
        float dangerTop = floorY * ts + ts * 0.38f;
        float dangerBot = (floorY + 1) * ts;
        float l = player.X;
        float t = player.Y;
        float r = player.X + player.Width;
        float b = player.Y + player.Height;

        for (int tx = 1; tx < WidthInTiles - 1; tx++)
        {
            if (Tiles[floorY, tx] != TileKind.Empty)
                continue;
            float cl = tx * ts;
            float cr = cl + ts;
            if (r <= cl || l >= cr)
                continue;
            if (b <= dangerTop || t >= dangerBot)
                continue;
            return true;
        }

        return false;
    }

    public bool IntersectsSolid(Player player) =>
        IntersectsTileSolid(player);

    public bool IntersectsTileSolid(Player player)
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
                if (IsBlockingTile(GetTile(tx, ty)))
                    return true;
            }
        }

        return false;
    }

    public void ResetDynamicState()
    {
        FallingBlocks.Clear();
        _spawnColumnIndex = 0;
        _spawnCooldownTicks = Math.Min(10, _spawnIntervalTicks);
        GhostTileHallucinationActive = false;
    }

    public bool UpdateFallingBlocks(Player player)
    {
        bool crushed = false;
        if (_fallSpawnTileXs.Length > 0)
        {
            _spawnCooldownTicks--;
            if (_spawnCooldownTicks <= 0)
            {
                _spawnCooldownTicks = _spawnIntervalTicks;
                int tx = _fallSpawnTileXs[_spawnColumnIndex % _fallSpawnTileXs.Length];
                _spawnColumnIndex++;
                TrySpawnBlock(tx);
            }
        }

        for (int i = FallingBlocks.Count - 1; i >= 0; i--)
        {
            var block = FallingBlocks[i];
            if (block.Y > WorldPixelHeight + TileSize * 2)
            {
                FallingBlocks.RemoveAt(i);
                continue;
            }

            block.VelocityY += Gravity;
            if (block.VelocityY > BlockFallSpeedCap)
                block.VelocityY = BlockFallSpeedCap;

            block.Y += block.VelocityY;

            if (RectsOverlap(player.X, player.Y, player.Width, player.Height, block.X, block.Y, block.Width, block.Height))
                crushed = true;
        }

        return crushed;
    }

    private void TrySpawnBlock(int tileX)
    {
        if (tileX < 1 || tileX >= WidthInTiles - 1)
            return;
        float w = TileSize * 0.82f;
        float h = TileSize * 0.72f;
        float x = tileX * TileSize + (TileSize - w) / 2f;
        float y = -TileSize - h;
        if (FallingBlocks.Count >= 8)
            return;

        FallingBlocks.Add(new FallingBlock
        {
            X = x,
            Y = y,
            Width = w,
            Height = h,
            VelocityY = 2.5f
        });
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

    private static bool RectsOverlap(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh) =>
        ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;

    public static Level CreateWeekOneSample()
    {
        const int inner = 26;
        const int targetH = 14;
        string[] playRows =
        {
            new string('.', inner),
            new string('.', inner),
            new string('.', inner),
            new string('.', inner),
            "S" + new string('.', 14) + "F" + new string('.', 10)
        };

        int padCount = Math.Max(0, targetH - 1 - playRows.Length);
        var inners = new List<string>(padCount + playRows.Length);
        for (int i = 0; i < padCount; i++)
            inners.Add(new string('.', inner));
        inners.AddRange(playRows);

        int w = inner + 2;
        int h = inners.Count + 1;
        var tiles = new TileKind[h, w];
        int sx = 0, sy = 0, fx = 0, fy = 0;

        for (int y = 0; y < inners.Count; y++)
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

    public static Level CreateLevelOne()
    {
        const int tileSize = 40;
        const int iw = 30;
        const int h = 14;
        const int w = iw + 2;
        var tiles = new TileKind[h, w];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tiles[y, x] = TileKind.Empty;

        for (int x = 0; x < w; x++)
            tiles[0, x] = TileKind.Solid;

        const int meteorCol = 16;
        tiles[0, meteorCol] = TileKind.Empty;

        for (int y = 1; y < h; y++)
        {
            tiles[y, 0] = TileKind.Solid;
            tiles[y, w - 1] = TileKind.Solid;
        }

        int floorY = h - 1;

        for (int x = 1; x < w - 1; x++)
            tiles[floorY, x] = TileKind.Solid;

        const int pitL = 11;
        const int pitR = 22;
        for (int x = pitL; x <= pitR; x++)
            tiles[floorY, x] = TileKind.Empty;

        void Stone(int tx, int ty)
        {
            if (tx == meteorCol)
                return;
            tiles[ty, tx] = TileKind.Solid;
        }

        Stone(9, 12);
        Stone(11, 11);
        Stone(13, 10);
        Stone(15, 11);
        Stone(17, 10);
        Stone(19, 11);
        Stone(21, 10);
        Stone(23, 12);

        tiles[11, 12] = TileKind.GhostSolid;
        tiles[11, 13] = TileKind.GhostSolid;
        tiles[10, 11] = TileKind.DisguisedSolid;

        const int sx = 3;
        const int sy = 12;
        const int fx = 28;
        const int fy = 12;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        int[] spawns = { meteorCol };
        return new Level(tiles, tileSize, sx, sy, fx, fy, spawns, spawnIntervalTicks: 36);
    }
}
