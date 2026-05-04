namespace ChaosRunner.Model;

// Позиция, скорость, хитбокс; Lives сбрасываются при рестарте забега.
public sealed class Player
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityY { get; set; }
    public int Lives { get; set; } = 3;
    public float Width { get; } = 22f;
    public float Height { get; } = 32f;
    public float MoveSpeed { get; } = 4.8f;

    public void ResetVelocity() => VelocityY = 0;
}
