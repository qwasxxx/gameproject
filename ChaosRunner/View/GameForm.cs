using ChaosRunner.Controller;
using ChaosRunner.Model;

namespace ChaosRunner.View;

// окно: клеит модель + контроллер, в Paint только вызов рендера
internal sealed class GameForm : Form
{
    private readonly GamePanel _panel = new();
    private readonly GameRenderer _renderer = new();
    private readonly Level _level;
    private readonly Player _player;
    private readonly GameState _gameState;
    private readonly GameController _controller;

    public GameForm()
    {
        Text = "Chaos Runner";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _level = Level.CreateWeekOneSample();
        _player = new Player();
        _level.PlacePlayerAtStart(_player);
        _gameState = new GameState();

        _controller = new GameController(_level, _player, _gameState, RequestRedraw);
        _controller.AttachInput(this);
        Shown += (_, _) => Focus();

        _panel.Dock = DockStyle.Fill;
        _panel.Paint += OnPanelPaint;
        Controls.Add(_panel);

        ClientSize = new Size(
            _level.WidthInTiles * _level.TileSize,
            _level.HeightInTiles * _level.TileSize);

        _controller.StartGameLoop();
    }

    private void RequestRedraw() => _panel.Invalidate();

    private void OnPanelPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _level, _player, _gameState);
    }

    private sealed class GamePanel : Panel
    {
        public GamePanel()
        {
            DoubleBuffered = true;
        }
    }
}
