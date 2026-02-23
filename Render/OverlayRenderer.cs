using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    // Toggle to visualise raw anchor positions as crosses.
    // Each cross marks the exact point passed to PlaceElement (top-left of the tile grid).
    public bool ShowDebugCrosses { get; set; } = false;

    // Controls which set of mastery crest PNGs is used.
    // Toggled via the system tray context menu.
    public MasteryIconSet ActiveIconSet { get; set; } = MasteryIconSet.Modern;

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

        // Render crosses at raw anchor positions for calibration
        if (ShowDebugCrosses)
        {
            PlaceCross(_layout.PlayerChampionPos, Brushes.Red);
            foreach (var pos in _layout.BenchIconPositions)
                PlaceCross(pos, Brushes.Cyan);
        }

        // Render my pick at the anchor position
        if (state.ChampionSelect?.MyChampion != null)
        {
            var id = state.ChampionSelect.MyChampion.Value;
            state.ChampMasteryData.TryGetValue(id, out var mastery);
            var overlay = CreateChampionOverlay(mastery, ChampionOverlayConfig.Player);
            PlaceElement(overlay, _layout.PlayerChampionPos);
            _root.Children.Add(overlay);
        }

        // Render bench champions at their grid positions
        var benchIds = state.ChampionSelect?.BenchChampions ?? Array.Empty<int>();
        for (int i = 0; i < benchIds.Length && i < _layout.BenchIconPositions.Length; i++)
        {
            state.ChampMasteryData.TryGetValue(benchIds[i], out var mastery);
            var overlay = CreateChampionOverlay(mastery, ChampionOverlayConfig.Bench);
            PlaceElement(overlay, _layout.BenchIconPositions[i]);
            _root.Children.Add(overlay);
        }
    }

    /// <summary>
    /// Places a UI element at a calculated position.
    /// </summary>
    private void PlaceElement(FrameworkElement element, Point position)
    {
        Canvas.SetLeft(element, position.X);
        Canvas.SetTop(element, position.Y);
        Canvas.SetZIndex(element, 100);
    }

    /// <summary>
    /// Renders a small cross centred on the given point for anchor calibration.
    /// The cross is drawn as two lines intersecting at (position.X, position.Y).
    /// </summary>
    private void PlaceCross(Point position, Brush color, double size = 10)
    {
        const string tag = "DebugCross";

        var horizontal = new Line
        {
            X1 = position.X - size / 2,
            Y1 = position.Y,
            X2 = position.X + size / 2,
            Y2 = position.Y,
            Stroke = color,
            StrokeThickness = 1,
            Tag = tag
        };

        var vertical = new Line
        {
            X1 = position.X,
            Y1 = position.Y - size / 2,
            X2 = position.X,
            Y2 = position.Y + size / 2,
            Stroke = color,
            StrokeThickness = 1,
            Tag = tag
        };

        Canvas.SetZIndex(horizontal, 200);
        Canvas.SetZIndex(vertical, 200);
        _root.Children.Add(horizontal);
        _root.Children.Add(vertical);
    }

    /// <summary>
    /// Per-slot configuration sourced from Anchors. Passed into CreateChampionOverlay
    /// so the player and bench tiles can be calibrated independently.
    /// </summary>
    private readonly record struct ChampionOverlayConfig(
        Size TileSize,
        double CrestSize,
        (double Left, double Top, double Right, double Bottom) CrestOffset,
        double ProgressBarHeight)
    {
        public static readonly ChampionOverlayConfig Player = new(
            TileSize: new Size(Anchors.PlayerTileSize.W, Anchors.PlayerTileSize.H),
            CrestSize: Anchors.PlayerMasteryCrestSize,
            CrestOffset: Anchors.PlayerMasteryCrestOffset,
            ProgressBarHeight: Anchors.PlayerProgressBarHeight);

        public static readonly ChampionOverlayConfig Bench = new(
            TileSize: new Size(Anchors.BenchTileSize.W, Anchors.BenchTileSize.H),
            CrestSize: Anchors.BenchMasteryCrestSize,
            CrestOffset: Anchors.BenchMasteryCrestOffset,
            ProgressBarHeight: Anchors.BenchProgressBarHeight);
    }

    /// <summary>
    /// Creates a transparent overlay element sized to the champion tile.
    /// The mastery crest PNG is pinned to the top-right corner of the tile,
    /// and a progress bar is anchored to the bottom edge.
    /// </summary>
    private Grid CreateChampionOverlay(MasteryData? mastery, ChampionOverlayConfig config)
    {
        int level = Math.Clamp(mastery?.Level ?? 0, 0, 9);
        float progress = mastery?.MasteryProgress ?? 0f;

        var tile = new Grid
        {
            Width = config.TileSize.Width,
            Height = config.TileSize.Height,
            Background = Brushes.Transparent
        };

        // --- Mastery crest (top-right corner) ---
        var (crestLeft, crestTop, crestRight, crestBottom) = config.CrestOffset;
        var crest = new Image
        {
            Width = config.CrestSize,
            Height = config.CrestSize,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(crestLeft, crestTop, crestRight, crestBottom),
            Source = LoadMasteryCrest(level, ActiveIconSet)
        };
        tile.Children.Add(crest);

        // --- Progress bar (bottom edge) ---
        // Outer track (dark background)
        var barTrack = new Border
        {
            Height = config.ProgressBarHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.FromArgb(160, 20, 20, 20)),
            CornerRadius = new CornerRadius(1)
        };
        tile.Children.Add(barTrack);

        // Inner fill
        var barFill = new Border
        {
            Height = config.ProgressBarHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Width = config.TileSize.Width * progress,
            Background = GetProgressBrush(),
            CornerRadius = new CornerRadius(1)
        };
        tile.Children.Add(barFill);

        return tile;
    }

    /// <summary>
    /// Loads the mastery crest PNG for the given level from embedded resources.
    /// Returns null if the resource cannot be found (WPF Image will render blank).
    /// </summary>
    private static BitmapImage? LoadMasteryCrest(int level, MasteryIconSet iconSet)
    {
        var uri = iconSet switch
        {
            MasteryIconSet.Legacy => new Uri(
                $"pack://application:,,,/Resources/LegacyMasteryIcons/mastery-{Math.Clamp(level, 0, 7)}.png",
                UriKind.Absolute),
            _ => new Uri(
                $"pack://application:,,,/Resources/MasteryIcons/crest-and-banner-mastery-{Math.Clamp(level, 0, 9)}.png",
                UriKind.Absolute)
        };

        try
        {
            return new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }
    
    private SolidColorBrush GetProgressBrush(double progress)
    {
        // Simple gradient from red (0%) to green (100%)
        var r = (byte)(255 * (1 - progress));
        var g = (byte)(255 * progress);
        return new SolidColorBrush(Color.FromArgb(200, r, g, 120));
    }
}