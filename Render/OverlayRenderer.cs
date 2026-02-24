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

    // Decoded BitmapImages keyed by (iconSet, level) so we only load each file once per session.
    // A null value means the file wasn't cached on disk at last check; we retry next frame.
    private readonly Dictionary<(MasteryIconSet, int), BitmapImage?> _imageCache = new();

    // Toggle to visualise raw anchor positions as crosses.
    // Each cross marks the exact point passed to PlaceElement (top-left of the tile grid).
    public bool ShowDebugCrosses { get; set; } = false;

    // Controls which set of mastery crest PNGs is used.
    // Toggled via the system tray context menu.
    public MasteryIconSet ActiveIconSet
    {
        get => _activeIconSet;
        set
        {
            if (_activeIconSet == value) return;
            _activeIconSet = value;
            // Clear cache so the new set is loaded on the next render.
            _imageCache.Clear();
        }
    }
    private MasteryIconSet _activeIconSet = MasteryIconSet.Modern;

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
            var overlay = CreateChampionOverlay(mastery, ChampionOverlayConfig.ForPlayer(_layout));
            PlaceElement(overlay, _layout.PlayerChampionPos);
            _root.Children.Add(overlay);
        }

        // Render bench champions at their grid positions
        var benchIds = state.ChampionSelect?.BenchChampions ?? Array.Empty<int>();
        for (int i = 0; i < benchIds.Length && i < _layout.BenchIconPositions.Length; i++)
        {
            state.ChampMasteryData.TryGetValue(benchIds[i], out var mastery);
            var overlay = CreateChampionOverlay(mastery, ChampionOverlayConfig.ForBench(_layout));
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
        public static ChampionOverlayConfig ForPlayer(OverlayLayout layout) => new(
            TileSize: layout.PlayerTileSize,
            CrestSize: Anchors.PlayerMasteryCrestSize,
            CrestOffset: Anchors.PlayerMasteryCrestOffset,
            ProgressBarHeight: Anchors.PlayerProgressBarHeight);

        public static ChampionOverlayConfig ForBench(OverlayLayout layout) => new(
            TileSize: layout.BenchTileSize,
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
        int level = Math.Clamp(mastery?.Level ?? 0, 0, 10);
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
        var (brush, glowColor) = GetProgressBrush(progress);
        var barFill = new Border
        {
            Height = config.ProgressBarHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Width = config.TileSize.Width * progress,
            Background = brush,
            CornerRadius = new CornerRadius(1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = glowColor,
                ShadowDepth = 0,
                BlurRadius = 6,
                Opacity = 0.8
            }
        };
        tile.Children.Add(barFill);

        return tile;
    }

    /// <summary>
    /// Returns the decoded BitmapImage for the given level and icon set.
    /// Hits an in-memory cache after the first load so disk is only read once per image per session.
    /// Returns null if the file has not been downloaded yet — WPF Image renders blank, and we
    /// retry on the next frame so icons appear as soon as the download completes.
    /// </summary>
    private BitmapImage? LoadMasteryCrest(int level, MasteryIconSet iconSet)
    {
        var key = (iconSet, level);

        // Return the cached result if we already have one (including a cached null).
        // A cached null means the file wasn't on disk last time; re-check so we pick it up
        // as soon as the background download finishes.
        if (_imageCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var fileName = iconSet switch
        {
            MasteryIconSet.Legacy =>
                $"mastery-{Math.Clamp(level, 0, 7)}.png",
            _ =>
                $"crest-and-banner-mastery-{Math.Clamp(level, 0, 10)}.png"
        };

        var path = IconCache.GetCachedPath(fileName);
        if (path == null)
            return null;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            _imageCache[key] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

/// <summary>
/// Returns a LinearGradientBrush and glow color based on progress (0 to 1).
/// Color transitions from red -> yellow -> green, with a light highlight on the right edge.
/// </summary>
    private static (LinearGradientBrush brush, Color glowColor) GetProgressBrush(double progress)
    {
        byte r = progress < 0.5
            ? (byte)255
            : (byte)(255 * (1 - (progress - 0.5) * 2));
        byte g = progress < 0.5
            ? (byte)(255 * progress * 2)
            : (byte)255;

        var baseColor  = Color.FromArgb(160, r, g, 0);
        var lightColor = Color.FromArgb(220, (byte)Math.Min(255, r + 80), (byte)Math.Min(255, g + 80), 80);

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint   = new Point(1, 0),
            GradientStops =
            {
                new GradientStop(baseColor,  0.0),
                new GradientStop(baseColor,  0.6),
                new GradientStop(lightColor, 1.0),
            }
        };

        return (brush, baseColor);
    }
}