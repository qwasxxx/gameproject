using System.Text.Json;

namespace ChaosRunner.Model;

public sealed class ProgressStorage
{
    private readonly string _path;

    public ProgressStorage(string? customFilePath = null)
    {
        if (customFilePath is { Length: > 0 })
        {
            _path = customFilePath;
            var parent = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            return;
        }

        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChaosRunner");
        Directory.CreateDirectory(baseDir);
        _path = Path.Combine(baseDir, "progress.json");
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
