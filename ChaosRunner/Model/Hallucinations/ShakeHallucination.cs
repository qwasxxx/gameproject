namespace ChaosRunner.Model.Hallucinations;

public sealed class ShakeHallucination : IHallucination
{
    private bool _savedActive;
    private float _savedAmplitude;

    public void Apply(HallucinationContext ctx)
    {
        _savedActive = ctx.Shake.Active;
        _savedAmplitude = ctx.Shake.Amplitude;
        ctx.Shake.Active = true;
        ctx.Shake.Amplitude = 6f;
    }

    public void Revert(HallucinationContext ctx)
    {
        ctx.Shake.Active = _savedActive;
        ctx.Shake.Amplitude = _savedAmplitude;
        if (!ctx.Shake.Active)
        {
            ctx.Shake.OffsetX = 0;
            ctx.Shake.OffsetY = 0;
        }
    }
}
