using ChaosRunner.Controller;
using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class GameControllerTests
{
    private static GameController CreateController(
        GameState state,
        out Level level,
        out Player player,
        out GameProgress progress,
        out ProgressStorage storage,
        Action<int>? beginLevel = null)
    {
        level = Level.CreateWeekOneSample();
        player = new Player();
        level.PlacePlayerAtStart(player);
        progress = new GameProgress { MaxUnlockedLevel = state.MaxUnlockedLevel };
        var path = Path.Combine(Path.GetTempPath(), $"ChaosRunnerCtrl_{Guid.NewGuid():N}.json");
        storage = new ProgressStorage(path);
        return new GameController(
            level,
            player,
            state,
            progress,
            storage,
            () => { },
            () => { },
            beginLevel ?? (_ => { }));
    }

    [Fact]
    public void Menu_Enter_OpensLevelSelect()
    {
        var state = new GameState();
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Enter);
        Assert.Equal(GamePhase.LevelSelect, state.Phase);
        Assert.Equal(0, state.LevelSelectIndex);
    }

    [Fact]
    public void LevelSelect_Esc_ReturnsToMenu()
    {
        var state = new GameState { Phase = GamePhase.LevelSelect };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Escape);
        Assert.Equal(GamePhase.Menu, state.Phase);
    }

    [Fact]
    public void LevelSelect_Enter_StartsFirstLevelWhenUnlocked()
    {
        int started = 0;
        var state = new GameState { Phase = GamePhase.LevelSelect, MaxUnlockedLevel = 5, LevelSelectIndex = 0 };
        var c = CreateController(state, out _, out _, out _, out _, id => started = id);
        c.SimulateKeyDown(Keys.Enter);
        Assert.Equal(GamePhase.Playing, state.Phase);
        Assert.Equal(1, started);
        Assert.Equal(1, state.CurrentLevelId);
    }

    [Fact]
    public void LevelSelect_Arrows_WrapIndex()
    {
        var state = new GameState { Phase = GamePhase.LevelSelect, MaxUnlockedLevel = 5, LevelSelectIndex = 0 };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Left);
        Assert.Equal(Level.LevelCount - 1, state.LevelSelectIndex);
        c.SimulateKeyDown(Keys.Right);
        Assert.Equal(0, state.LevelSelectIndex);
    }

    [Fact]
    public void Playing_Escape_Pauses()
    {
        var state = new GameState { Phase = GamePhase.Playing };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Escape);
        Assert.Equal(GamePhase.Pause, state.Phase);
    }

    [Fact]
    public void Pause_Escape_Resumes()
    {
        var state = new GameState { Phase = GamePhase.Pause };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Escape);
        Assert.Equal(GamePhase.Playing, state.Phase);
    }

    [Fact]
    public void Playing_M_GoesToLevelSelect()
    {
        var state = new GameState { Phase = GamePhase.Playing };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.M);
        Assert.Equal(GamePhase.LevelSelect, state.Phase);
    }

    [Fact]
    public void AdvanceOneFrame_Playing_IncrementsRunTime()
    {
        var state = new GameState { Phase = GamePhase.Playing, MaxUnlockedLevel = 5 };
        var c = CreateController(state, out _, out _, out _, out _);
        int before = state.RunElapsedMs;
        c.AdvanceOneFrame();
        Assert.True(state.RunElapsedMs > before);
    }

    [Fact]
    public void AdvanceOneFrame_NotPlaying_DoesNotAdvanceTime()
    {
        var state = new GameState { Phase = GamePhase.Menu };
        var c = CreateController(state, out _, out _, out _, out _);
        c.AdvanceOneFrame();
        Assert.Equal(0, state.RunElapsedMs);
    }

    [Fact]
    public void LevelSelect_Digit2_StartsLevelTwoWhenUnlocked()
    {
        int started = 0;
        var state = new GameState { Phase = GamePhase.LevelSelect, MaxUnlockedLevel = 5, LevelSelectIndex = 0 };
        var c = CreateController(state, out _, out _, out _, out _, id => started = id);
        c.SimulateKeyDown(Keys.D2);
        Assert.Equal(GamePhase.Playing, state.Phase);
        Assert.Equal(2, started);
        Assert.Equal(2, state.CurrentLevelId);
        Assert.Equal(1, state.LevelSelectIndex);
    }

    [Fact]
    public void Victory_Enter_GoesToLevelSelect()
    {
        var state = new GameState { Phase = GamePhase.Victory };
        var c = CreateController(state, out _, out _, out _, out _);
        c.SimulateKeyDown(Keys.Enter);
        Assert.Equal(GamePhase.LevelSelect, state.Phase);
    }
}
