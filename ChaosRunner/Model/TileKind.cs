namespace ChaosRunner.Model;

public enum TileKind
{
    Empty,
    Solid,
    Pit,
    /// <summary>Визуально камень, физика переключается при «призрачной» галлюцинации.</summary>
    GhostSolid,
    /// <summary>Визуально пусто, при галлюцинации становится твёрдым.</summary>
    DisguisedSolid
}
