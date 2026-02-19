using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace league_mastery_overlay.Util;

public sealed class GridMapper
{
    private readonly Canvas _canvas;
    private bool _isActive = false;

    public GridMapper(Canvas canvas)
    {
        _canvas = canvas;
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
}