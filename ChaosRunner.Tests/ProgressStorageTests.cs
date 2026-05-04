using ChaosRunner.Model;
using Xunit;

namespace ChaosRunner.Tests;

public class ProgressStorageTests
{
    [Fact]
    public void SaveThenLoad_RoundtripsProgress()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ChaosRunnerTest_{Guid.NewGuid():N}.json");
        try
        {
            var storage = new ProgressStorage(path);
            var original = new GameProgress
            {
                MaxUnlockedLevel = 3,
                BestTimeMs = { [1] = 1000, [2] = 2000 }
            };
            storage.Save(original);

            var loaded = new ProgressStorage(path).Load();
            Assert.Equal(3, loaded.MaxUnlockedLevel);
            Assert.Equal(1000, loaded.BestTimeMs[1]);
            Assert.Equal(2000, loaded.BestTimeMs[2]);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaultProgress()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ChaosRunnerMissing_{Guid.NewGuid():N}.json");
        var storage = new ProgressStorage(path);
        var p = storage.Load();
        Assert.Equal(1, p.MaxUnlockedLevel);
        Assert.Empty(p.BestTimeMs);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultProgress()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ChaosRunnerBad_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ not json");
            var storage = new ProgressStorage(path);
            var p = storage.Load();
            Assert.Equal(1, p.MaxUnlockedLevel);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
