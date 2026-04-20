namespace ChaosRunner.Model;

public enum GamePhase
{
    Menu,
    Playing,
    Pause,
    Victory,
    Defeat
}

// меню / игра / пауза / конец
public sealed class GameState
{
    public GamePhase Phase { get; set; } = GamePhase.Menu;
    public int MenuLevelIndex { get; set; }
    public int CurrentLevelIndex { get; set; }
}
