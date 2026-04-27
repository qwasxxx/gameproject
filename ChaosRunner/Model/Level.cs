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
        int spawnIntervalTicks = 72,
        int maxFallingBlocks = 8,
        bool rotatingKeyHallucination = false,
        int inputLagMilliseconds = 0,
        string? hallucinationLabel = null,
        int screenShakeAmplitude = 0)
    {
        Tiles = tiles;
        TileSize = tileSize;
        StartTileX = startTileX;
        StartTileY = startTileY;
        FinishTileX = finishTileX;
        FinishTileY = finishTileY;
        _fallSpawnTileXs = fallSpawnTileXs ?? Array.Empty<int>();
        _spawnIntervalTicks = spawnIntervalTicks;
        MaxFallingBlocks = maxFallingBlocks;
        _spawnCooldownTicks = Math.Min(10, spawnIntervalTicks);
        FallingBlocks = new List<FallingBlock>();
        RotatingKeyHallucination = rotatingKeyHallucination;
        InputLagMilliseconds = inputLagMilliseconds;
        HallucinationLabel = hallucinationLabel;
        ScreenShakeAmplitude = screenShakeAmplitude;
    }

    public int MaxFallingBlocks { get; }

    public bool RotatingKeyHallucination { get; }

    public int InputLagMilliseconds { get; }

    public string? HallucinationLabel { get; }

    public int ScreenShakeAmplitude { get; }

    public TileKind[,] Tiles { get; }
    public int TileSize { get; }
    public int StartTileX { get; }
    public int StartTileY { get; }
    public int FinishTileX { get; }
    public int FinishTileY { get; }

    public List<FallingBlock> FallingBlocks { get; }

    public int WidthInTiles => Tiles.GetLength(1);
    public int HeightInTiles => Tiles.GetLength(0);

    public float WorldPixelHeight => HeightInTiles * TileSize;

    public static bool IsSolidTile(TileKind k) => k == TileKind.Solid;

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
                if (IsSolidTile(GetTile(tx, ty)))
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
        if (FallingBlocks.Count >= MaxFallingBlocks)
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

    public const int LevelCount = 5;

    public static Level Create(int id) => id switch
    {
        1 => BuildLevel1(),
        2 => BuildLevel2(),
        3 => BuildLevel3(),
        4 => BuildLevel4(),
        5 => BuildLevel5(),
        _ => BuildLevel1()
    };

    public static Level CreateLevelOne() => Create(1);

    private static Level BuildLevel1()
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

        const int sx = 3;
        const int sy = 12;
        const int fx = 28;
        const int fy = 12;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        int[] spawns = { meteorCol };
        return new Level(tiles, tileSize, sx, sy, fx, fy, spawns, spawnIntervalTicks: 36,
            hallucinationLabel: "Управление как обычно");
    }

    private static Level BuildLevel2()
    {
        const int tileSize = 40;
        const int iw = 32;
        const int h = 14;
        const int w = iw + 2;
        var tiles = new TileKind[h, w];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tiles[y, x] = TileKind.Empty;

        for (int x = 0; x < w; x++)
            tiles[0, x] = TileKind.Solid;

        int[] drops = { 14, 21 };
        tiles[0, drops[0]] = TileKind.Empty;
        tiles[0, drops[1]] = TileKind.Empty;

        for (int y = 1; y < h; y++)
        {
            tiles[y, 0] = TileKind.Solid;
            tiles[y, w - 1] = TileKind.Solid;
        }

        int floorY = h - 1;
        for (int x = 1; x < w - 1; x++)
            tiles[floorY, x] = TileKind.Solid;

        const int pitL = 11;
        const int pitR = 24;
        for (int x = pitL; x <= pitR; x++)
            tiles[floorY, x] = TileKind.Empty;

        void Stone(int tx, int ty)
        {
            tiles[ty, tx] = TileKind.Solid;
        }

        Stone(8, 12);
        Stone(10, 11);
        Stone(12, 10);
        Stone(16, 11);
        Stone(18, 10);
        Stone(20, 11);
        Stone(24, 10);
        Stone(27, 12);

        const int sx = 3;
        const int sy = 12;
        const int fx = w - 4;
        const int fy = 12;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        return new Level(tiles, tileSize, sx, sy, fx, fy, drops, spawnIntervalTicks: 30, maxFallingBlocks: 9,
            rotatingKeyHallucination: true,
            hallucinationLabel: "Случайная перестановка A/W/D каждые 10 с");
    }

    private static Level BuildLevel3()
    {
        const int tileSize = 40;
        const int iw = 36;
        const int h = 14;
        const int w = iw + 2;
        var tiles = new TileKind[h, w];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tiles[y, x] = TileKind.Empty;

        for (int x = 0; x < w; x++)
            tiles[0, x] = TileKind.Solid;

        int[] drops = { 11, 19, 27 };
        foreach (var c in drops)
            tiles[0, c] = TileKind.Empty;

        for (int y = 1; y < h; y++)
        {
            tiles[y, 0] = TileKind.Solid;
            tiles[y, w - 1] = TileKind.Solid;
        }

        int floorY = h - 1;
        for (int x = 1; x < w - 1; x++)
            tiles[floorY, x] = TileKind.Solid;

        const int pitL = 10;
        const int pitR = 27;
        for (int x = pitL; x <= pitR; x++)
            tiles[floorY, x] = TileKind.Empty;

        void Stone(int tx, int ty) => tiles[ty, tx] = TileKind.Solid;

        Stone(7, 12);
        Stone(9, 11);
        Stone(14, 10);
        Stone(16, 11);
        Stone(21, 10);
        Stone(24, 11);
        Stone(29, 12);
        Stone(31, 11);

        const int sx = 3;
        const int sy = 12;
        const int fx = w - 4;
        const int fy = 12;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        return new Level(tiles, tileSize, sx, sy, fx, fy, drops, spawnIntervalTicks: 24, maxFallingBlocks: 10,
            rotatingKeyHallucination: true,
            hallucinationLabel: "Случайная перестановка A/W/D каждые 10 с");
    }

    private static Level BuildLevel4()
    {
        const int tileSize = 40;
        const int iw = 40;
        const int h = 15;
        const int w = iw + 2;
        var tiles = new TileKind[h, w];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tiles[y, x] = TileKind.Empty;

        for (int x = 0; x < w; x++)
            tiles[0, x] = TileKind.Solid;

        int[] drops = { 10, 17, 24, 31 };
        foreach (var c in drops)
            tiles[0, c] = TileKind.Empty;

        for (int y = 1; y < h; y++)
        {
            tiles[y, 0] = TileKind.Solid;
            tiles[y, w - 1] = TileKind.Solid;
        }

        int floorY = h - 1;
        for (int x = 1; x < w - 1; x++)
            tiles[floorY, x] = TileKind.Solid;

        for (int x = 9; x <= 30; x++)
            tiles[floorY, x] = TileKind.Empty;
        tiles[floorY, 14] = TileKind.Solid;
        tiles[floorY, 20] = TileKind.Solid;
        tiles[floorY, 26] = TileKind.Solid;

        void Stone(int tx, int ty) => tiles[ty, tx] = TileKind.Solid;

        Stone(6, 13);
        Stone(32, 13);
        Stone(8, 12);
        Stone(11, 11);
        Stone(29, 12);
        Stone(34, 13);

        const int sx = 3;
        int sy = floorY - 1;
        const int fx = w - 4;
        int fy = floorY - 1;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        return new Level(tiles, tileSize, sx, sy, fx, fy, drops, spawnIntervalTicks: 18, maxFallingBlocks: 12,
            rotatingKeyHallucination: true,
            inputLagMilliseconds: 500,
            hallucinationLabel: "Перестановка каждые 10 с + задержка ввода ~0.5 с",
            screenShakeAmplitude: 3);
    }

    private static Level BuildLevel5()
    {
        const int tileSize = 40;
        const int iw = 46;
        const int h = 16;
        const int w = iw + 2;
        var tiles = new TileKind[h, w];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tiles[y, x] = TileKind.Empty;

        for (int x = 0; x < w; x++)
            tiles[0, x] = TileKind.Solid;

        int[] drops = { 8, 14, 21, 28, 35, 41 };
        foreach (var c in drops)
            tiles[0, c] = TileKind.Empty;

        for (int y = 1; y < h; y++)
        {
            tiles[y, 0] = TileKind.Solid;
            tiles[y, w - 1] = TileKind.Solid;
        }

        int floorY = h - 1;
        for (int x = 1; x < w - 1; x++)
            tiles[floorY, x] = TileKind.Solid;

        for (int x = 7; x <= 38; x++)
            tiles[floorY, x] = TileKind.Empty;
        for (var i = 0; i < 9; i++)
        {
            int x = 9 + i * 3;
            if (x <= 37)
                tiles[floorY, x] = TileKind.Solid;
        }

        void Stone(int tx, int ty) => tiles[ty, tx] = TileKind.Solid;

        Stone(5, floorY - 1);
        Stone(38, floorY - 1);
        Stone(7, floorY - 2);
        Stone(36, floorY - 2);
        Stone(12, floorY - 3);
        Stone(30, floorY - 3);

        const int sx = 3;
        int sy = floorY - 1;
        const int fx = w - 4;
        int fy = floorY - 1;
        tiles[sy, sx] = TileKind.Empty;
        tiles[sy, fx] = TileKind.Empty;

        return new Level(tiles, tileSize, sx, sy, fx, fy, drops, spawnIntervalTicks: 12, maxFallingBlocks: 14,
            rotatingKeyHallucination: true,
            hallucinationLabel: "Случайная перестановка A/W/D каждые 10 с (тряска как на lvl 4)",
            screenShakeAmplitude: 3);
    }
}
