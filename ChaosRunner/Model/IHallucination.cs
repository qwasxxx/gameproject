namespace ChaosRunner.Model;

public interface IHallucination
{
    void Apply(HallucinationContext ctx);
    void Revert(HallucinationContext ctx);
}
