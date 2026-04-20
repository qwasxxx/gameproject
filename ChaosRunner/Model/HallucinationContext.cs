using System.Windows.Forms;

namespace ChaosRunner.Model;

public sealed class ShakeState
{
    public bool Active { get; set; }
    public float Amplitude { get; set; } = 4f;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    public void Reset()
    {
        Active = false;
        OffsetX = 0;
        OffsetY = 0;
        Amplitude = 4f;
    }
}

public sealed class InputRemap
{
    public Keys MoveLeft { get; set; } = Keys.A;
    public Keys MoveRight { get; set; } = Keys.D;
    public Keys Jump { get; set; } = Keys.W;
    public bool InvertHorizontal { get; set; }

    public void ResetDefaults()
    {
        MoveLeft = Keys.A;
        MoveRight = Keys.D;
        Jump = Keys.W;
        InvertHorizontal = false;
    }
}

/// <summary>Общее состояние для Apply/Revert галлюцинаций.</summary>
public sealed class HallucinationContext
{
    public InputRemap Input { get; } = new();
    public ShakeState Shake { get; } = new();
    public Level? Level { get; set; }
    public Random Rng { get; } = new();
}
