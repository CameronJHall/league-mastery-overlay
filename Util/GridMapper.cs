using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace league_mastery_overlay.Util;

public sealed class GridMapper
{
    private readonly Canvas _canvas;
    private readonly TextBlock _mousePos;
    private bool _isActive = false;

    public GridMapper(Canvas canvas)
    {
        _canvas = canvas;
        
        // Create persistent mouse position tracker
        _mousePos = new TextBlock
        {
            Foreground = Brushes.Yellow,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(8)
        };
        
        _canvas.MouseMove += OnMouseMove;
    }

    public void Toggle()
    {
        _isActive = !_isActive;
        
        if (_isActive)
        {
            ShowGrid();
        }
        else
        {
            HideGrid();
        }
    }

    public void Render()
    {
        if (!_isActive) return;

        // Re-add grid elements if they were cleared
        if (!_canvas.Children.Contains(_mousePos))
        {
            ShowGrid();
        }
    }

    private void ShowGrid()
    {
        // Add gridlines every 100px
        for (int x = 0; x < 2000; x += 100)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = 2000,
                Stroke = Brushes.Cyan,
                StrokeThickness = 1,
                Opacity = 0.3,
                IsHitTestVisible = false,
                Tag = "DebugGrid" // Tag so we can identify it
            };
            _canvas.Children.Add(line);

            // Add coordinate label
            var label = new TextBlock
            {
                Text = x.ToString(),
                Foreground = Brushes.Cyan,
                FontSize = 10,
                Background = Brushes.Black,
                IsHitTestVisible = false,
                Tag = "DebugGrid"
            };
            Canvas.SetLeft(label, x + 2);
            Canvas.SetTop(label, 2);
            _canvas.Children.Add(label);
        }

        for (int y = 0; y < 2000; y += 100)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = 2000,
                Y2 = y,
                Stroke = Brushes.Cyan,
                StrokeThickness = 1,
                Opacity = 0.3,
                IsHitTestVisible = false,
                Tag = "DebugGrid"
            };
            _canvas.Children.Add(line);

            // Add coordinate label
            var label = new TextBlock
            {
                Text = y.ToString(),
                Foreground = Brushes.Cyan,
                FontSize = 10,
                Background = Brushes.Black,
                IsHitTestVisible = false,
                Tag = "DebugGrid"
            };
            Canvas.SetLeft(label, 2);
            Canvas.SetTop(label, y + 2);
            _canvas.Children.Add(label);
        }

        // Add mouse position tracker
        _mousePos.Text = "Mouse: (0, 0) | Press G to toggle grid";
        Canvas.SetLeft(_mousePos, 10);
        Canvas.SetTop(_mousePos, 10);
        Canvas.SetZIndex(_mousePos, 9999); // Always on top
        _mousePos.Tag = "DebugGrid";
        _canvas.Children.Add(_mousePos);
    }

    private void HideGrid()
    {
        // Remove all grid elements
        var toRemove = _canvas.Children
            .OfType<FrameworkElement>()
            .Where(e => e.Tag?.ToString() == "DebugGrid")
            .ToList();

        foreach (var element in toRemove)
        {
            _canvas.Children.Remove(element);
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isActive) return;

        var pos = e.GetPosition(_canvas);
        _mousePos.Text = $"Mouse: ({pos.X:F0}, {pos.Y:F0}) | Press G to toggle grid";
    }
}