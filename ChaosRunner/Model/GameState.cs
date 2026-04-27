namespace ChaosRunner.Model;

public enum GamePhase
{
    Menu,
    LevelSelect,
    Playing,
    Pause,
    Victory,
    Defeat
}

public sealed class GameState
{
    public GamePhase Phase { get; set; } = GamePhase.Menu;

    public int CurrentLevelId { get; set; } = 1;

    public int LevelSelectIndex { get; set; }

    public int RunElapsedMs { get; set; }

    public int RunDeaths { get; set; }

    public int LastFinishElapsedMs { get; set; }

    public bool LastVictoryWasRecord { get; set; }

    public string? ActiveHallucinationLabel { get; set; }

    public int ShakeOffsetX { get; set; }

    public int ShakeOffsetY { get; set; }

    public int MaxUnlockedLevel { get; set; } = 1;

    public void ResetRunForNewLevel()
    {
        RunElapsedMs = 0;
        RunDeaths = 0;
    }
}
