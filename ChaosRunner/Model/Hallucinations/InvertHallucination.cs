namespace ChaosRunner.Model.Hallucinations;

public sealed class InvertHallucination : IHallucination
{
    private bool _saved;

    public void Apply(HallucinationContext ctx)
    {
        _saved = ctx.Input.InvertHorizontal;
        ctx.Input.InvertHorizontal = !_saved;
    }

    public void Revert(HallucinationContext ctx)
    {
        ctx.Input.InvertHorizontal = _saved;
    }
}
