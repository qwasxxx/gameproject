using System.Text.Json;

namespace ChaosRunner.Model;

public sealed class ProgressStorage
{
    private readonly string _path;

    public ProgressStorage()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChaosRunner");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "progress.json");
    }

    public GameProgress Load()
    {
        if (!File.Exists(_path))
            return new GameProgress();
        try
        {
            var json = File.ReadAllText(_path);
            var data = JsonSerializer.Deserialize<GameProgress>(json);
            return data ?? new GameProgress();
        }
        catch
        {
            return new GameProgress();
        }
    }

    public void Save(GameProgress progress)
    {
        var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
