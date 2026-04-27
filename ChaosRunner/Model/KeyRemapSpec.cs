namespace ChaosRunner.Model;

public sealed class KeyRemapSpec
{
    private readonly Dictionary<string, string> _physToVirt;

    public KeyRemapSpec(params (string physicalActsAsVirtual, string logicalKey)[] pairs)
    {
        _physToVirt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (physical, logical) in pairs)
        {
            if (string.IsNullOrWhiteSpace(physical) || string.IsNullOrWhiteSpace(logical))
                continue;
            _physToVirt[physical.Trim()] = logical.Trim();
        }
    }

    public static KeyRemapSpec CreateRandomPermutationAwd(Random rng)
    {
        string[] names = { "A", "W", "D" };
        string[] shuffled = names.OrderBy(_ => rng.Next()).ToArray();
        return new KeyRemapSpec(("A", shuffled[0]), ("W", shuffled[1]), ("D", shuffled[2]));
    }

    public bool IsEmpty => _physToVirt.Count == 0;

    public string MapPhysicalToLogical(string physicalKeyName)
    {
        if (_physToVirt.Count == 0)
            return physicalKeyName;
        return _physToVirt.TryGetValue(physicalKeyName, out var v) ? v : physicalKeyName;
    }

    public string FormatAwdCaption()
    {
        if (IsEmpty)
            return "";
        static string M(KeyRemapSpec s, string p) => s.MapPhysicalToLogical(p);
        return $"A→{M(this, "A")} W→{M(this, "W")} D→{M(this, "D")}";
    }
}
