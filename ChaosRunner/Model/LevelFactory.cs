namespace ChaosRunner.Model;

public static class LevelFactory
{
    public const int Count = 2;

    /// <summary>Общий размер окна: максимум по всем уровням (без скачков при смене карты).</summary>
    public static int ViewportPixelWidth { get; }

    public static int ViewportPixelHeight { get; }

    static LevelFactory()
    {
        int maxW = 0, maxH = 0;
        for (int i = 0; i < Count; i++)
        {
            var level = Create(i);
            maxW = Math.Max(maxW, level.WidthInTiles * level.TileSize);
            maxH = Math.Max(maxH, level.HeightInTiles * level.TileSize);
        }

        ViewportPixelWidth = maxW;
        ViewportPixelHeight = maxH;
    }

    public static Level Create(int index) => index switch
    {
        0 => Level.CreateLevelOne(),
        1 => Level.CreateWeekOneSample(),
        _ => Level.CreateLevelOne()
    };

    public static string GetDisplayName(int index) => index switch
    {
        0 => "Уровень 1 — яма, блоки, галлюцинации",
        1 => "Уровень 2 — простая трасса",
        _ => "?"
    };
}
