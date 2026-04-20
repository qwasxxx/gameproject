using System.Windows.Forms;
using ChaosRunner.Model;

namespace ChaosRunner.Controller;

public sealed class GameController
{
    private Level _level;
    private readonly Player _player;
    private readonly GameState _gameState;
    private readonly Action _requestRedraw;
    private readonly Action<int> _loadLevel;
    private readonly HallucinationContext _hallucinationCtx;
    private readonly HallucinationManager _hallucinationMgr;
    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _prevJumpHeld;
    private readonly System.Windows.Forms.Timer _timer;

    public GameController(
        Level initialLevel,
        Player player,
        GameState gameState,
        Action requestRedraw,
        HallucinationContext hallucinationCtx,
        Action<int> loadLevel)
    {
        _level = initialLevel;
        _player = player;
        _gameState = gameState;
        _requestRedraw = requestRedraw;
        _hallucinationCtx = hallucinationCtx;
        _loadLevel = loadLevel;
        _hallucinationCtx.Level = initialLevel;
        _hallucinationMgr = new HallucinationManager(_hallucinationCtx);
        _hallucinationMgr.SetLevel(initialLevel, gameState.CurrentLevelIndex);
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => OnTick();
    }

    public void SetLevel(Level level, int levelIndex)
    {
        _level = level;
        _hallucinationCtx.Level = level;
        _gameState.CurrentLevelIndex = levelIndex;
        _hallucinationMgr.SetLevel(level, levelIndex);
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

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.KeyCode);
    }

    private void HandleImmediateActions(Keys key)
    {
        switch (_gameState.Phase)
        {
            case GamePhase.Menu:
                if (key == Keys.Up)
                    _gameState.MenuLevelIndex = (_gameState.MenuLevelIndex - 1 + LevelFactory.Count) % LevelFactory.Count;
                if (key == Keys.Down)
                    _gameState.MenuLevelIndex = (_gameState.MenuLevelIndex + 1) % LevelFactory.Count;
                if (key == Keys.Enter)
                {
                    _loadLevel(_gameState.MenuLevelIndex);
                    _gameState.Phase = GamePhase.Playing;
                }

                break;
            case GamePhase.Playing:
                if (key == Keys.Escape)
                    _gameState.Phase = GamePhase.Pause;
                break;
            case GamePhase.Pause:
                if (key == Keys.Escape)
                    _gameState.Phase = GamePhase.Playing;
                else if (key == Keys.M)
                    GoToMainMenu();
                break;
            case GamePhase.Victory:
            case GamePhase.Defeat:
                if (key == Keys.Enter)
                    RestartRun();
                else if (key == Keys.Escape)
                    GoToMainMenu();
                break;
        }
    }

    private void GoToMainMenu()
    {
        _player.ResetVelocity();
        _level.ResetDynamicState();
        _hallucinationMgr.ResetForNewRun();
        _level.PlacePlayerAtStart(_player);
        _gameState.Phase = GamePhase.Menu;
    }

    private void RestartRun()
    {
        _player.Lives = 3;
        _player.ResetVelocity();
        _level.ResetDynamicState();
        _hallucinationMgr.ResetForNewRun();
        _level.PlacePlayerAtStart(_player);
        _gameState.Phase = GamePhase.Playing;
    }

    private void OnTick()
    {
        _hallucinationMgr.Tick(_player, _gameState.Phase);

        if (_gameState.Phase != GamePhase.Playing)
        {
            _player.ResetVelocity();
            _prevJumpHeld = _pressedKeys.Contains(_hallucinationCtx.Input.Jump);
            return;
        }

        var input = _hallucinationCtx.Input;
        bool jumpNow = _pressedKeys.Contains(input.Jump);
        bool jumpEdge = jumpNow && !_prevJumpHeld;
        _prevJumpHeld = jumpNow;

        float horiz = 0;
        if (_pressedKeys.Contains(input.MoveLeft))
            horiz -= 1f;
        if (_pressedKeys.Contains(input.MoveRight))
            horiz += 1f;
        if (input.InvertHorizontal)
            horiz = -horiz;

        _level.ApplyPlatformTick(_player, horiz, jumpEdge);

        bool crushed = _level.UpdateFallingBlocks(_player);

        if (_level.PlayerReachedFinish(_player))
            _gameState.Phase = GamePhase.Victory;
        else if (crushed || _level.PlayerTouchesSpikes(_player) || _level.PlayerFellOutOfWorld(_player))
            _gameState.Phase = GamePhase.Defeat;

        _requestRedraw();
    }
}
