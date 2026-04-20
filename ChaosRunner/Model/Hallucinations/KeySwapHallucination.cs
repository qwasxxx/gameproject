using System.Windows.Forms;

namespace ChaosRunner.Model.Hallucinations;

/// <summary>Перестановка W/A/D на роли «влево / вправо / прыжок».</summary>
public sealed class KeySwapHallucination : IHallucination
{
    private Keys _savedLeft;
    private Keys _savedRight;
    private Keys _savedJump;

    public void Apply(HallucinationContext ctx)
    {
        _savedLeft = ctx.Input.MoveLeft;
        _savedRight = ctx.Input.MoveRight;
        _savedJump = ctx.Input.Jump;

        var pool = new[] { Keys.W, Keys.A, Keys.D };
        Shuffle(ctx.Rng, pool);
        ctx.Input.MoveLeft = pool[0];
        ctx.Input.MoveRight = pool[1];
        ctx.Input.Jump = pool[2];
    }

    public void Revert(HallucinationContext ctx)
    {
        ctx.Input.MoveLeft = _savedLeft;
        ctx.Input.MoveRight = _savedRight;
        ctx.Input.Jump = _savedJump;
    }

    private static void Shuffle(Random rng, Keys[] keys)
    {
        for (int i = keys.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (keys[i], keys[j]) = (keys[j], keys[i]);
        }
    }
}
