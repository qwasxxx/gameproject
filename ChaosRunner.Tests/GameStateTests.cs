using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class GameStateTests // фазы
{
    [Fact]
    public void DefaultPhase_IsMenu()
    {
        var s = new GameState();
        Assert.Equal(GamePhase.Menu, s.Phase);
    }

    [Fact]
    public void Phase_CanBeChanged()
    {
        var s = new GameState { Phase = GamePhase.Playing };
        Assert.Equal(GamePhase.Playing, s.Phase);
    }

    [Fact]
    public void ResetRunForNewLevel_ClearsRunCounters()
    {
        var s = new GameState { RunElapsedMs = 999, RunDeaths = 3 };
        s.ResetRunForNewLevel();
        Assert.Equal(0, s.RunElapsedMs);
        Assert.Equal(0, s.RunDeaths);
    }
}
