using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class KeyRemapSpecTests
{
    [Fact]
    public void EmptySpec_MapsIdentity()
    {
        var spec = new KeyRemapSpec();
        Assert.True(spec.IsEmpty);
        Assert.Equal("A", spec.MapPhysicalToLogical("A"));
        Assert.Equal("X", spec.MapPhysicalToLogical("X"));
    }

    [Fact]
    public void ExplicitMap_ResolvesLogical()
    {
        var spec = new KeyRemapSpec(("A", "D"), ("W", "W"), ("D", "A"));
        Assert.False(spec.IsEmpty);
        Assert.Equal("D", spec.MapPhysicalToLogical("A"));
        Assert.Equal("A", spec.MapPhysicalToLogical("D"));
        Assert.Equal("W", spec.MapPhysicalToLogical("W"));
    }

    [Fact]
    public void CreateRandomPermutation_IsBijectionOnAwd()
    {
        var rng = new Random(12345);
        var spec = KeyRemapSpec.CreateRandomPermutationAwd(rng);
        Assert.False(spec.IsEmpty);
        var images = new HashSet<string>
        {
            spec.MapPhysicalToLogical("A"),
            spec.MapPhysicalToLogical("W"),
            spec.MapPhysicalToLogical("D")
        };
        Assert.Equal(3, images.Count);
        foreach (var k in new[] { "A", "W", "D" })
            Assert.Contains(k, images);
    }

    [Fact]
    public void FormatAwdCaption_NonEmpty_HasArrows()
    {
        var spec = new KeyRemapSpec(("A", "W"), ("W", "D"), ("D", "A"));
        var s = spec.FormatAwdCaption();
        Assert.Contains("A→", s);
        Assert.Contains("W→", s);
        Assert.Contains("D→", s);
    }

    [Fact]
    public void Constructor_SkipsBlankPairs()
    {
        var spec = new KeyRemapSpec(("", "A"), ("W", " "), ("D", "D"));
        Assert.Equal("A", spec.MapPhysicalToLogical("A"));
        Assert.Equal("D", spec.MapPhysicalToLogical("D"));
    }
}
