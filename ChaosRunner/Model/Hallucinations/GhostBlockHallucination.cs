namespace ChaosRunner.Model.Hallucinations;

public sealed class GhostBlockHallucination : IHallucination
{
    public void Apply(HallucinationContext ctx)
    {
        if (ctx.Level != null)
            ctx.Level.GhostTileHallucinationActive = true;
    }

    public void Revert(HallucinationContext ctx)
    {
        if (ctx.Level != null)
            ctx.Level.GhostTileHallucinationActive = false;
    }
}
