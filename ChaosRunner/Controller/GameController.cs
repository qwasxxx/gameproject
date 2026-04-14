using ChaosRunner.Model;

namespace ChaosRunner.Controller;

public sealed class GameController
{
    private readonly Level _level;
    private readonly Player _player;
    private readonly GameState _gameState;
    private readonly Action _requestRedraw;
    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _prevWHeld;
    private readonly System.Windows.Forms.Timer _timer;

    public GameController(Level level, Player player, GameState gameState, Action requestRedraw)
    {
        _level = level;
        _player = player;
        _gameState = gameState;
        _requestRedraw = requestRedraw;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => OnTick();
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
                if (key == Keys.Enter)
                    _gameState.Phase = GamePhase.Playing;
                break;
            case GamePhase.Playing:
                if (key == Keys.Escape)
                    _gameState.Phase = GamePhase.Pause;
                break;
            case GamePhase.Pause:
                if (key == Keys.Escape)
                    _gameState.Phase = GamePhase.Playing;
                break;
            case GamePhase.Victory:
            case GamePhase.Defeat:
                if (key == Keys.Enter)
                    RestartRun();
                break;
        }
    }

    private void RestartRun()
    {
        _player.Lives = 3;
        _player.ResetVelocity();
        _level.ResetDynamicState();
        _level.PlacePlayerAtStart(_player);
        _gameState.Phase = GamePhase.Playing;
    }

    private void OnTick()
    {
        if (_gameState.Phase != GamePhase.Playing)
        {
            _player.ResetVelocity();
            _prevWHeld = _pressedKeys.Contains(Keys.W);
            return;
        }

        bool wNow = _pressedKeys.Contains(Keys.W);
        bool jumpEdge = wNow && !_prevWHeld;
        _prevWHeld = wNow;

        float horiz = 0;
        if (_pressedKeys.Contains(Keys.A))
            horiz -= 1f;
        if (_pressedKeys.Contains(Keys.D))
            horiz += 1f;

        _level.ApplyPlatformTick(_player, horiz, jumpEdge);

        bool crushed = _level.UpdateFallingBlocks(_player);

        if (_level.PlayerReachedFinish(_player))
            _gameState.Phase = GamePhase.Victory;
        else if (crushed || _level.PlayerTouchesSpikes(_player) || _level.PlayerFellOutOfWorld(_player))
            _gameState.Phase = GamePhase.Defeat;

        _requestRedraw();
    }
}
