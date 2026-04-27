using ChaosRunner.Model;

namespace ChaosRunner.Controller;

public sealed class GameController
{
    private const int HallucinationPeriodMs = 10_000;

    private Level _level;
    private readonly Player _player;
    private readonly GameState _gameState;
    private readonly GameProgress _progress;
    private readonly ProgressStorage _storage;
    private readonly Action _requestRedraw;
    private readonly Action _layoutMenuChrome;
    private readonly Action<int> _beginLevel;
    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _prevWHeld;
    private readonly System.Windows.Forms.Timer _timer;
    private KeyRemapSpec? _dynamicRemap;
    private int _nextHallucinationChangeAtMs;
    private readonly List<(int ms, bool a, bool w, bool d)> _inputLagSamples = new();

    public GameController(
        Level level,
        Player player,
        GameState gameState,
        GameProgress progress,
        ProgressStorage storage,
        Action requestRedraw,
        Action layoutMenuChrome,
        Action<int> beginLevel)
    {
        _level = level;
        _player = player;
        _gameState = gameState;
        _progress = progress;
        _storage = storage;
        _requestRedraw = requestRedraw;
        _layoutMenuChrome = layoutMenuChrome;
        _beginLevel = beginLevel;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => OnTick();
        InitHallucinationForLevel();
    }

    public void SetLevel(Level level)
    {
        _level = level;
        _inputLagSamples.Clear();
        if (level.InputLagMilliseconds > 0)
            _inputLagSamples.Add((0, false, false, false));
        InitHallucinationForLevel();
    }

    private void InitHallucinationForLevel()
    {
        if (_level.RotatingKeyHallucination)
        {
            _dynamicRemap = KeyRemapSpec.CreateRandomPermutationAwd(Random.Shared);
            _nextHallucinationChangeAtMs = HallucinationPeriodMs;
        }
        else
        {
            _dynamicRemap = null;
            _nextHallucinationChangeAtMs = 0;
        }
    }

    public void StartGameLoop() => _timer.Start();

    public void AttachInput(Control host)
    {
        if (host is Form form)
            form.KeyPreview = true;
        host.KeyDown += OnKeyDown;
        host.KeyUp += OnKeyUp;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.KeyCode);
        HandleImmediateActions(e.KeyCode);
        _requestRedraw();
    }

    private void OnKeyUp(object? sender, KeyEventArgs e) => _pressedKeys.Remove(e.KeyCode);

    private static string MovementKeyToken(Keys k) =>
        k switch
        {
            Keys.A => "A",
            Keys.W => "W",
            Keys.D => "D",
            _ => string.Empty
        };

    private Keys PhysicalAsLogicalGameKey(Keys physical)
    {
        var token = MovementKeyToken(physical);
        if (token.Length == 0)
            return physical;
        var spec = _level.RotatingKeyHallucination ? _dynamicRemap : null;
        var mapped = spec is { IsEmpty: false } ? spec.MapPhysicalToLogical(token) : token;
        return mapped switch
        {
            "A" => Keys.A,
            "W" => Keys.W,
            "D" => Keys.D,
            _ => physical
        };
    }

    private bool LogicalGameKeyDown(Keys logicalMovementKey)
    {
        foreach (var p in _pressedKeys)
        {
            if (PhysicalAsLogicalGameKey(p) == logicalMovementKey)
                return true;
        }

        return false;
    }

    private void HandleImmediateActions(Keys key)
    {
        var s = _gameState;
        switch (s.Phase)
        {
            case GamePhase.Menu:
                if (key == Keys.Enter)
                {
                    s.Phase = GamePhase.LevelSelect;
                    s.LevelSelectIndex = 0;
                    _layoutMenuChrome();
                }
                break;

            case GamePhase.LevelSelect:
                if (key == Keys.Escape)
                {
                    s.Phase = GamePhase.Menu;
                    _layoutMenuChrome();
                }
                else if (key == Keys.Left)
                {
                    s.LevelSelectIndex = (s.LevelSelectIndex + Level.LevelCount - 1) % Level.LevelCount;
                }
                else if (key == Keys.Right)
                {
                    s.LevelSelectIndex = (s.LevelSelectIndex + 1) % Level.LevelCount;
                }
                else if (key == Keys.Enter)
                {
                    int id = s.LevelSelectIndex + 1;
                    if (id <= s.MaxUnlockedLevel)
                    {
                        s.CurrentLevelId = id;
                        s.ResetRunForNewLevel();
                        _beginLevel(id);
                        s.Phase = GamePhase.Playing;
                    }
                }
                else if (key >= Keys.D1 && key <= Keys.D5)
                {
                    int id = key - Keys.D0;
                    if (id <= s.MaxUnlockedLevel)
                    {
                        s.LevelSelectIndex = id - 1;
                        s.CurrentLevelId = id;
                        s.ResetRunForNewLevel();
                        _beginLevel(id);
                        s.Phase = GamePhase.Playing;
                    }
                }
                break;

            case GamePhase.Playing:
                if (key == Keys.Escape)
                    s.Phase = GamePhase.Pause;
                else if (key == Keys.M)
                {
                    s.Phase = GamePhase.LevelSelect;
                    _layoutMenuChrome();
                }
                break;

            case GamePhase.Pause:
                if (key == Keys.Escape)
                    s.Phase = GamePhase.Playing;
                else if (key == Keys.M)
                {
                    s.Phase = GamePhase.LevelSelect;
                    _layoutMenuChrome();
                }
                break;

            case GamePhase.Victory:
                if (key == Keys.Enter)
                {
                    s.Phase = GamePhase.LevelSelect;
                    _layoutMenuChrome();
                }
                break;

            case GamePhase.Defeat:
                if (key == Keys.Enter)
                    RestartRunAfterDeath();
                else if (key == Keys.M)
                {
                    s.Phase = GamePhase.LevelSelect;
                    _layoutMenuChrome();
                }
                break;
        }
    }

    private void RestartRunAfterDeath()
    {
        _player.Lives = 3;
        _player.ResetVelocity();
        _level.ResetDynamicState();
        _level.PlacePlayerAtStart(_player);
        _gameState.RunElapsedMs = 0;
        _inputLagSamples.Clear();
        if (_level.InputLagMilliseconds > 0)
            _inputLagSamples.Add((0, false, false, false));
        if (_level.RotatingKeyHallucination)
        {
            _dynamicRemap = KeyRemapSpec.CreateRandomPermutationAwd(Random.Shared);
            _nextHallucinationChangeAtMs = HallucinationPeriodMs;
        }

        _gameState.Phase = GamePhase.Playing;
    }

    private void TickHallucinationTimer()
    {
        if (!_level.RotatingKeyHallucination)
            return;
        while (_gameState.RunElapsedMs >= _nextHallucinationChangeAtMs)
        {
            _dynamicRemap = KeyRemapSpec.CreateRandomPermutationAwd(Random.Shared);
            _nextHallucinationChangeAtMs += HallucinationPeriodMs;
        }
    }

    private void RecordInputSample(int nowMs, bool a, bool w, bool d)
    {
        _inputLagSamples.Add((nowMs, a, w, d));
        int cutoff = nowMs - 3000;
        while (_inputLagSamples.Count > 0 && _inputLagSamples[0].ms < cutoff)
            _inputLagSamples.RemoveAt(0);
    }

    private bool TryLogicalAt(int targetMs, out bool a, out bool w, out bool d)
    {
        a = w = d = false;
        if (targetMs < 0)
            return false;
        for (int i = _inputLagSamples.Count - 1; i >= 0; i--)
        {
            if (_inputLagSamples[i].ms <= targetMs)
            {
                (_, a, w, d) = _inputLagSamples[i];
                return true;
            }
        }

        return false;
    }

    private void SyncHallucinationHud()
    {
        if (!_level.RotatingKeyHallucination)
        {
            _gameState.ActiveHallucinationLabel = _level.HallucinationLabel;
            return;
        }

        if (_dynamicRemap == null)
            return;
        int remain = Math.Max(0, _nextHallucinationChangeAtMs - _gameState.RunElapsedMs);
        int sec = (remain + 999) / 1000;
        string lagNote = _level.InputLagMilliseconds > 0
            ? $" · ввод с задержкой {_level.InputLagMilliseconds / 1000.0:0.#} с"
            : "";
        _gameState.ActiveHallucinationLabel = $"{_dynamicRemap.FormatAwdCaption()} · смена {sec}с{lagNote}";
    }

    private void UpdateShake()
    {
        if (_level.ScreenShakeAmplitude > 0 && _gameState.Phase == GamePhase.Playing)
        {
            int a = _level.ScreenShakeAmplitude;
            _gameState.ShakeOffsetX = Random.Shared.Next(-a, a + 1);
            _gameState.ShakeOffsetY = Random.Shared.Next(-a, a + 1);
        }
        else
        {
            _gameState.ShakeOffsetX = 0;
            _gameState.ShakeOffsetY = 0;
        }
    }

    private void OnTick()
    {
        if (_gameState.Phase == GamePhase.Playing)
            _gameState.RunElapsedMs += _timer.Interval;

        UpdateShake();

        if (_gameState.Phase != GamePhase.Playing)
        {
            _player.ResetVelocity();
            _prevWHeld = LogicalGameKeyDown(Keys.W);
            _requestRedraw();
            return;
        }

        TickHallucinationTimer();

        bool ia = LogicalGameKeyDown(Keys.A);
        bool iw = LogicalGameKeyDown(Keys.W);
        bool id = LogicalGameKeyDown(Keys.D);
        RecordInputSample(_gameState.RunElapsedMs, ia, iw, id);

        int lagMs = _level.InputLagMilliseconds;
        int tick = _timer.Interval;
        bool la, lw, ld;
        bool lwPrev;
        if (lagMs > 0)
        {
            int t = _gameState.RunElapsedMs;
            if (!TryLogicalAt(t - lagMs, out la, out lw, out ld))
                la = lw = ld = false;
            if (!TryLogicalAt(t - lagMs - tick, out _, out lwPrev, out _))
                lwPrev = false;
        }
        else
        {
            la = ia;
            lw = iw;
            ld = id;
            lwPrev = _prevWHeld;
        }

        bool jumpEdge = lw && !lwPrev;
        _prevWHeld = lw;

        float horiz = 0;
        if (la)
            horiz -= 1f;
        if (ld)
            horiz += 1f;

        _level.ApplyPlatformTick(_player, horiz, jumpEdge);
        bool crushed = _level.UpdateFallingBlocks(_player);

        SyncHallucinationHud();

        if (_level.PlayerReachedFinish(_player))
            CompleteVictory();
        else if (crushed || _level.PlayerTouchesSpikes(_player) || _level.PlayerFellOutOfWorld(_player))
        {
            _gameState.Phase = GamePhase.Defeat;
            _gameState.RunDeaths++;
        }

        _requestRedraw();
    }

    private void CompleteVictory()
    {
        int id = _gameState.CurrentLevelId;
        int t = _gameState.RunElapsedMs;
        _gameState.LastFinishElapsedMs = t;

        bool wasRecord = !_progress.BestTimeMs.TryGetValue(id, out var prev) || t < prev;
        _gameState.LastVictoryWasRecord = wasRecord;
        if (wasRecord)
            _progress.BestTimeMs[id] = t;

        int unlocked = Math.Min(Level.LevelCount, Math.Max(_progress.MaxUnlockedLevel, id + 1));
        _progress.MaxUnlockedLevel = unlocked;
        _gameState.MaxUnlockedLevel = unlocked;
        _storage.Save(_progress);
        _gameState.Phase = GamePhase.Victory;
    }
}
