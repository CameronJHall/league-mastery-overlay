using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using league_mastery_overlay.Layout;
using league_mastery_overlay.State;
using league_mastery_overlay.Util;

namespace league_mastery_overlay.Render;

/// <summary>
/// Responsible for rendering champion mastery icons at positions calculated by OverlayLayout.
/// Does NOT handle layout math - that's OverlayLayout's job.
/// </summary>
public sealed class OverlayRenderer
{
    private readonly Canvas _root;
    private readonly StateStore _stateStore;
    private readonly OverlayLayout _layout;
    private readonly GridMapper? _gridMapper;

    public OverlayRenderer(Canvas root, StateStore stateStore, OverlayLayout layout, GridMapper? gridMapper = null)
    {
        _root = root;
        _stateStore = stateStore;
        _layout = layout;
        _gridMapper = gridMapper;
    }

    /// <summary>
    /// Called whenever the League window is resized.
    /// This ensures layout rects are always in sync with actual window size.
    /// </summary>
    public void UpdateWindowSize(Size newSize)
    {
        _layout.Recalculate(newSize);
    }

    /// <summary>
    /// Main render loop - place icons at their calculated positions.
    /// </summary>
    public void Render()
    {
        LeagueState state = _stateStore.Get();

        // Hide overlay if not in champ select
        if (state.Phase != GamePhase.ChampSelect)
        {
            _root.Visibility = Visibility.Collapsed;
            return;
        }

        _root.Visibility = Visibility.Visible;

        // Clear previous render (but preserve debug grid elements)
        var toRemove = _root.Children
            .OfType<FrameworkElement>()
            .Where(e => e.Tag?.ToString() != "DebugGrid")
            .ToList();
        
        foreach (var element in toRemove)
        {
            _root.Children.Remove(element);
        }

        // Re-render debug grid if active
        _gridMapper?.Render();

        // Show debug info at the top
        // var debugPanel = CreateDebugPanel(state);
        // Canvas.SetLeft(debugPanel, 10);
        // Canvas.SetTop(debugPanel, 10);
        // Canvas.SetZIndex(debugPanel, 100);
        // _root.Children.Add(debugPanel);

        // Render my pick at the anchor position
        if (state.ChampionSelect?.MyChampion != null)
        {
            var icon = CreateMasteryIcon(state.ChampionSelect.MyChampion);
            PlaceElement(icon, _layout.PlayerChampionRect);
            _root.Children.Add(icon);
        }

        // Render bench champions at their grid positions
        var benchChampions = state.ChampionSelect?.BenchChampions ?? Array.Empty<ChampionData>();
        for (int i = 0; i < benchChampions.Length && i < _layout.BenchIconRects.Length; i++)
        {
            var icon = CreateMasteryIcon(benchChampions[i]);
            PlaceElement(icon, _layout.BenchIconRects[i]);
            _root.Children.Add(icon);
        }
    }

    /// <summary>
    /// Places a UI element at a calculated rectangle position.
    /// </summary>
    private void PlaceElement(FrameworkElement element, Rect position)
    {
        Canvas.SetLeft(element, position.Left);
        Canvas.SetTop(element, position.Top);
        Canvas.SetZIndex(element, 100);
    }

    private Border CreateDebugPanel(LeagueState state)
    {
        var benchCount = state.ChampionSelect?.BenchChampions?.Length ?? 0;
        var hasSelection = state.ChampionSelect?.MyChampion != null;

        var text = new TextBlock
        {
            Text = $"Phase: {state.Phase}\n" +
                   $"Selected: {(hasSelection ? "Yes" : "No")}\n" +
                   $"Bench Champions: {benchCount}",
            Foreground = Brushes.Cyan,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(8)
        };

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Child = text
        };
    }

    /// <summary>
    /// Creates a small mastery icon for placement at anchor positions.
    /// The icon is color-coded by mastery level, with a circular border showing progress.
    /// </summary>
    private Border CreateMasteryIcon(ChampionData champion)
    {
        const double iconSize = 20;
        const double borderThickness = 2.0;
        
        // Color code by mastery level
        Brush backgroundColor = champion.Level switch
        {
            >= 5 => new SolidColorBrush(Color.FromArgb(180, 190, 200, 255)), // Gold
            >= 1 => new SolidColorBrush(Color.FromArgb(180, 255, 215, 160)), // Bronze
            _ => new SolidColorBrush(Color.FromArgb(144, 100, 100, 100)) // Gray - Not played
        };

        // Create a grid to layer the background circle and progress circle
        var grid = new Grid
        {
            Width = iconSize,
            Height = iconSize
        };

        // Background circle with text
        var backgroundCircle = new Border
        {
            Background = backgroundColor,
            CornerRadius = new CornerRadius(iconSize / 2),
            Child = new TextBlock
            {
                Text = $"{champion.Level}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        grid.Children.Add(backgroundCircle);

        // Progress border - uses DashArray to create partial circle effect
        var progressBorder = new Ellipse
        {
            Width = iconSize,
            Height = iconSize,
            Stroke = GetProgressColor(champion.MasteryProgress),
            StrokeThickness = borderThickness,
            RenderTransformOrigin = new Point(0.5, 0.5), // Rotate around center
            RenderTransform = new RotateTransform(-90) // Start from top, fill clockwise
        };

        // Calculate dash array to show progress as a partial circle
        // Circumference = π * diameter
        double circumference = System.Math.PI * iconSize / 2;
        double filledLength = circumference * champion.MasteryProgress;
        double gapLength = circumference - filledLength;

        progressBorder.StrokeDashArray = new System.Windows.Media.DoubleCollection { filledLength, gapLength };
        progressBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 2,
            ShadowDepth = 0,
            Opacity = 0.9
        };
        
        grid.Children.Add(progressBorder);

        // Outer container that maintains size
        var badge = new Border
        {
            Width = iconSize + (borderThickness * 2),
            Height = iconSize + (borderThickness * 2),
            Child = grid
        };

        return badge;
    }
    
    private SolidColorBrush GetProgressColor(double progress)
    {
        // Simple gradient from red (0%) to green (100%)
        var r = (byte)(255 * (1 - progress));
        var g = (byte)(255 * progress);
        return new SolidColorBrush(Color.FromArgb(200, r, g, 100));
    }
}