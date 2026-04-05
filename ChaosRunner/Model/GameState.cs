namespace ChaosRunner.Model;

public enum GamePhase
{
    Menu,
    Playing,
    Pause,
    GameOver
}

// меню / игра / пауза / конец
public sealed class GameState
{
    public GamePhase Phase { get; set; } = GamePhase.Menu;
}
