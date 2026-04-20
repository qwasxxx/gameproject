using ChaosRunner.Model;
using ChaosRunner.Model.Hallucinations;

namespace ChaosRunner.Controller;

public sealed class HallucinationManager
{
    private readonly HallucinationContext _ctx;
    private readonly KeySwapHallucination _keySwap = new();
    private readonly InvertHallucination _invert = new();
    private readonly GhostBlockHallucination _ghost = new();
    private readonly ShakeHallucination _shake = new();

    private int _levelIndex;
    private bool _keySwapZoneTriggered;
    private bool _ghostZoneTriggered;
    private int _invertCooldownTicks;
    private int _keyReshuffleTicks;
    private bool _invertOnFromTimer;

    private const int InvertPeriodTicks = 720;
    /// <summary>Интервал между перетасовками клавиш (~16.8 с при тике 16 мс).</summary>
    private const int KeyReshufflePeriodTicks = 1050;

    public HallucinationManager(HallucinationContext ctx) => _ctx = ctx;

    public void SetLevel(Level level, int levelIndex)
    {
        _ctx.Level = level;
        _levelIndex = levelIndex;
        _keySwapZoneTriggered = false;
        _ghostZoneTriggered = false;
        _invertCooldownTicks = InvertPeriodTicks / 2;
        _keyReshuffleTicks = 0;
        FullRevert();
    }

    public void ResetForNewRun()
    {
        _keySwapZoneTriggered = false;
        _ghostZoneTriggered = false;
        _invertCooldownTicks = InvertPeriodTicks / 2;
        _keyReshuffleTicks = 0;
        FullRevert();
    }

    private void FullRevert()
    {
        if (_invertOnFromTimer)
        {
            _invert.Revert(_ctx);
            _invertOnFromTimer = false;
        }

        _keySwap.Revert(_ctx);
        _ghost.Revert(_ctx);
        _shake.Revert(_ctx);
        _ctx.Input.ResetDefaults();
        _ctx.Shake.Reset();
        if (_ctx.Level != null)
            _ctx.Level.GhostTileHallucinationActive = false;
    }

    public void Tick(Player player, GamePhase phase)
    {
        if (phase != GamePhase.Playing)
        {
            _ctx.Shake.OffsetX = 0;
            _ctx.Shake.OffsetY = 0;
            return;
        }

        int centerTx = (int)Math.Floor((player.X + player.Width * 0.5f) / _ctx.Level!.TileSize);

        if (_levelIndex == 0)
        {
            if (!_keySwapZoneTriggered && centerTx >= 6)
            {
                _keySwapZoneTriggered = true;
                _keySwap.Apply(_ctx);
                _keyReshuffleTicks = KeyReshufflePeriodTicks;
            }

            if (!_ghostZoneTriggered && centerTx >= 15)
            {
                _ghostZoneTriggered = true;
                _ghost.Apply(_ctx);
                _shake.Apply(_ctx);
            }
        }

        if (_keySwapZoneTriggered && _levelIndex == 0)
        {
            _keyReshuffleTicks--;
            if (_keyReshuffleTicks <= 0)
            {
                _keyReshuffleTicks = KeyReshufflePeriodTicks;
                _keySwap.Revert(_ctx);
                _keySwap.Apply(_ctx);
            }
        }

        if (_levelIndex == 0)
        {
            _invertCooldownTicks--;
            if (_invertCooldownTicks <= 0)
            {
                _invertCooldownTicks = InvertPeriodTicks;
                if (_invertOnFromTimer)
                {
                    _invert.Revert(_ctx);
                    _invertOnFromTimer = false;
                }
                else
                {
                    _invert.Apply(_ctx);
                    _invertOnFromTimer = true;
                }
            }
        }

        if (_ctx.Shake.Active)
        {
            double a = _ctx.Shake.Amplitude;
            _ctx.Shake.OffsetX = (float)((_ctx.Rng.NextDouble() * 2 - 1) * a);
            _ctx.Shake.OffsetY = (float)((_ctx.Rng.NextDouble() * 2 - 1) * a);
        }
        else
        {
            _ctx.Shake.OffsetX = 0;
            _ctx.Shake.OffsetY = 0;
        }
    }
}
