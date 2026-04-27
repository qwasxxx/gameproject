namespace ChaosRunner.Model;

public sealed class GameProgress
{
    public int MaxUnlockedLevel { get; set; } = 1;

    public Dictionary<int, int> BestTimeMs { get; set; } = new();
}
