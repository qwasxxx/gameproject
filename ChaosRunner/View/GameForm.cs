using ChaosRunner.Controller;
using ChaosRunner.Model;

namespace ChaosRunner.View;

// окно: клеит модель + контроллер, в Paint только вызов рендера
internal sealed class GameForm : Form
{
    private readonly GamePanel _panel = new();
    private readonly GameRenderer _renderer = new();
    private Level _level;
    private readonly Player _player = new();
    private readonly GameState _gameState = new();
    private readonly HallucinationContext _hallucinationCtx = new();
    private readonly GameController _controller;

    public GameForm()
    {
        Text = "Chaos Runner";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _gameState.Phase = GamePhase.Menu;
        _gameState.MenuLevelIndex = 0;
        _gameState.CurrentLevelIndex = 0;

        _level = LevelFactory.Create(0);
        _level.PlacePlayerAtStart(_player);

        _controller = new GameController(_level, _player, _gameState, RequestRedraw, _hallucinationCtx, LoadLevel);
        _controller.AttachInput(this);
        Shown += (_, _) => Focus();

        _panel.Dock = DockStyle.Fill;
        _panel.Paint += OnPanelPaint;
        Controls.Add(_panel);

        ClientSize = new Size(LevelFactory.ViewportPixelWidth, LevelFactory.ViewportPixelHeight);

        _controller.StartGameLoop();
    }

    private void LoadLevel(int index)
    {
        _level = LevelFactory.Create(index);
        _controller.SetLevel(_level, index);
        _level.ResetDynamicState();
        _level.PlacePlayerAtStart(_player);
        _player.ResetVelocity();

        ClientSize = new Size(LevelFactory.ViewportPixelWidth, LevelFactory.ViewportPixelHeight);

        RequestRedraw();
    }

    private void RequestRedraw() => _panel.Invalidate();

    private void OnPanelPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _level, _player, _gameState, _hallucinationCtx);
    }

    private sealed class GamePanel : Panel
    {
        public GamePanel()
        {
            DoubleBuffered = true;
        }
    }
}
