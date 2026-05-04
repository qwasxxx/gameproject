using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class GameProgressTests
{
    [Fact]
    public void Defaults_StartAtLevelOne()
    {
        var p = new GameProgress();
        Assert.Equal(1, p.MaxUnlockedLevel);
        Assert.NotNull(p.BestTimeMs);
        Assert.Empty(p.BestTimeMs);
    }
}
