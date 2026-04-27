using ChaosRunner.Controller;
using ChaosRunner.Model;

namespace ChaosRunner.View;

internal sealed class GameForm : Form
{
    private readonly GamePanel _panel = new();
    private readonly GameRenderer _renderer = new();
    private readonly ProgressStorage _storage = new();
    private readonly GameProgress _progress;
    private Level _level;
    private readonly Player _player = new();
    private readonly GameState _gameState;
    private readonly GameController _controller;

    private const int MenuClientWidth = 960;
    private const int MenuClientHeight = 600;

    public GameForm()
    {
        Text = "Chaos Runner";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _progress = _storage.Load();
        _gameState = new GameState { MaxUnlockedLevel = _progress.MaxUnlockedLevel };

        _level = Level.Create(1);
        _level.PlacePlayerAtStart(_player);

        _controller = new GameController(
            _level,
            _player,
            _gameState,
            _progress,
            _storage,
            RequestRedraw,
            LayoutMenuChrome,
            BeginLevel);

        _controller.AttachInput(this);
        Shown += (_, _) => Focus();

        _panel.Dock = DockStyle.Fill;
        _panel.Paint += OnPanelPaint;
        Controls.Add(_panel);

        ClientSize = new Size(MenuClientWidth, MenuClientHeight);
        _controller.StartGameLoop();
    }

    private void LayoutMenuChrome() => ClientSize = new Size(MenuClientWidth, MenuClientHeight);

    private void BeginLevel(int id)
    {
        _level = Level.Create(id);
        _controller.SetLevel(_level);
        _player.Lives = 3;
        _player.ResetVelocity();
        _level.PlacePlayerAtStart(_player);
        _gameState.ActiveHallucinationLabel = _level.HallucinationLabel;
        ClientSize = new Size(_level.WidthInTiles * _level.TileSize, _level.HeightInTiles * _level.TileSize);
    }

    private void RequestRedraw() => _panel.Invalidate();

    private void OnPanelPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _panel.ClientRectangle, _level, _player, _gameState, _progress);
    }

    private sealed class GamePanel : Panel
    {
        public GamePanel() => DoubleBuffered = true;
    }
}
