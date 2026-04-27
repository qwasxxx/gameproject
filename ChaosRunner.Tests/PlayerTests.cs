using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

// мелочи по Player
public class PlayerTests
{
    [Fact]
    public void ResetVelocity_ZerosVertical()
    {
        var p = new Player { VelocityY = -2 };
        p.ResetVelocity();
        Assert.Equal(0, p.VelocityY);
    }

    [Fact]
    public void Defaults_HasLivesAndSize()
    {
        var p = new Player();
        Assert.Equal(3, p.Lives);
        Assert.Equal(22f, p.Width);
        Assert.Equal(32f, p.Height);
        Assert.True(p.MoveSpeed > 0);
    }
}
