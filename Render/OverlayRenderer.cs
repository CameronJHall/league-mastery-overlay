using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using league_mastery_overlay.Eval;
using league_mastery_overlay.Layout;
using league_mastery_overlay.State;
using league_mastery_overlay.Util;
using static league_mastery_overlay.Util.WindowTracker;

namespace league_mastery_overlay.Render;

/// <summary>
/// Renders champion mastery overlays during champ select and player title cards during lobby.
/// Layout math lives in OverlayLayout; this class only consumes the computed positions.
/// </summary>
public sealed class OverlayRenderer
{
    private readonly Canvas _root;
    private readonly StateStore _stateStore;
    private readonly OverlayLayout _layout;
    private readonly GridMapper? _gridMapper;

    // Decoded BitmapImages keyed by (iconSet, level); null means not yet on disk — retried each frame.
    private readonly Dictionary<(MasteryIconSet, int), BitmapImage?> _imageCache = new();

    // Title cache — re-evaluated only when lobby membership or stats availability changes.
    // Key format: sorted puuids joined with '|', '!' suffix on each puuid that has stats loaded.
    // e.g. "aaa|bbb!|ccc" — bbb has stats, aaa and ccc do not.
    private string? _lastTitleKey;
    private Dictionary<string, TitleEvaluator.TitleResult?> _cachedTitles = new();

    public bool ShowDebugCrosses { get; set; } = false;

    public bool ShowDebugPanel { get; set; } = false;

    // When true, renders overlay content even when League is not the foreground window.
    // Intended for debug/calibration use only.
    public bool ForceRender { get; set; } = false;

    // Controls which set of mastery crest PNGs is used; toggled via the system tray menu.
    public MasteryIconSet ActiveIconSet
    {
        get => _activeIconSet;
        set
        {
            if (_activeIconSet == value) return;
            _activeIconSet = value;
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
    /// Called whenever the League window is resized to keep layout positions in sync.
    /// </summary>
    public void UpdateWindowSize(Size newSize)
    {
        _layout.Recalculate(newSize);
    }

    /// <summary>
    /// Main render entry point. Clears previous frame, then draws the appropriate overlay
    /// for the current game phase. No-ops when League is not foregrounded (unless ForceRender).
    /// </summary>
    public void Render(bool isLeagueForegrounded, DebugSnapshot? debug = null)
    {
        LeagueState state = _stateStore.Get();

        // Clear previous frame, preserving the debug grid layer.
        var toRemove = _root.Children
            .OfType<FrameworkElement>()
            .Where(e => e.Tag?.ToString() != "DebugGrid")
            .ToList();
        
        foreach (var element in toRemove)
        {
            _root.Children.Remove(element);
        }

        // Debug panel is shown regardless of foreground state so it's visible when diagnosing
        // why the overlay is hidden.
        if (ShowDebugPanel && debug != null)
            RenderDebugPanel(debug);

        // Hide content rather than the window itself to avoid repaint flicker.
        if (!isLeagueForegrounded && !ForceRender)
            return;

        if (state.Phase != GamePhase.ChampSelect && state.Phase != GamePhase.Lobby)
            return;

        _gridMapper?.Render();

        if (state.Phase == GamePhase.Lobby)
        {
            RenderLobby(state);
            return;
        }

        // ── ChampSelect ───────────────────────────────────────────────────────

        if (ShowDebugCrosses)
        {
            PlaceCross(_layout.PlayerChampionPos, Brushes.Red);
            foreach (var pos in _layout.BenchIconPositions)
                PlaceCross(pos, Brushes.Cyan);
        }

        if (state.ChampionSelect?.MyChampion != null)
        {
            var id = state.ChampionSelect.MyChampion.Value;
            state.ChampMasteryData.TryGetValue(id, out var mastery);
            var overlay = CreateChampionOverlay(mastery, ChampionOverlayConfig.ForPlayer(_layout));
            PlaceElement(overlay, _layout.PlayerChampionPos);
            _root.Children.Add(overlay);
        }

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
    /// Semi-transparent debug panel showing foreground-window PID tracking state.
    /// </summary>
    private void RenderDebugPanel(DebugSnapshot debug)
    {
        string pidMatchStr = debug.PidMatch ? "MATCH" : "NO MATCH";
        string pidMatchColor = debug.PidMatch ? "#FF00FF66" : "#FFFF4444";

        var lines = new[]
        {
            ("Foreground", $"\"{debug.ForegroundTitle}\""),
            ("FG PID",     $"{debug.ForegroundPid}"),
            ("League PID", $"{debug.LeaguePid}"),
            ("PID match",  pidMatchStr),
            ("Overlay",    debug.OverlayVisibility),
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin      = new Thickness(6, 4, 6, 4),
        };

        foreach (var (label, value) in lines)
        {
            bool isPidMatch = label == "PID match";
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text       = label + ": ",
                Foreground = new SolidColorBrush(Color.FromArgb(180, 180, 180, 180)),
                FontSize   = 10,
                FontFamily = new FontFamily("Consolas"),
            });
            row.Children.Add(new TextBlock
            {
                Text       = value,
                Foreground = isPidMatch
                    ? (SolidColorBrush)new BrushConverter().ConvertFromString(pidMatchColor)!
                    : new SolidColorBrush(Colors.White),
                FontSize   = 10,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = isPidMatch ? FontWeights.Bold : FontWeights.Normal,
            });
            stack.Children.Add(row);
        }

        var panel = new Border
        {
            Background      = new SolidColorBrush(Color.FromArgb(200, 10, 10, 30)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(120, 80, 80, 200)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(4),
            Child           = stack,
            Tag             = "DebugPanel",
        };

        // Measure so we can position at bottom-left.
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double panelH = panel.DesiredSize.Height > 0 ? panel.DesiredSize.Height : 90;

        Canvas.SetLeft(panel, 10);
        Canvas.SetTop(panel, _root.ActualHeight > 0 ? _root.ActualHeight - panelH - 10 : 400);
        Canvas.SetZIndex(panel, 500);
        _root.Children.Add(panel);
    }

    /// <summary>
    /// Renders title cards for all lobby members. The local player is always placed at
    /// slot 2 (centre). Other members follow the League client's nonSelfCards layout:
    ///   API join-order index 0 → slot 1, 1 → slot 3, 2 → slot 0, 3 → slot 4.
    /// </summary>
    private void RenderLobby(LeagueState state)
    {
        if (ShowDebugCrosses)
        {
            foreach (var pos in _layout.LobbyCardPositions)
                PlaceCross(pos, Brushes.Yellow, size: 14);
        }

        var lobby = state.Lobby;
        if (lobby == null || lobby.Friends.Count == 0)
            return;

        // Snapshot the stats cache once per frame to avoid holding the lock across rendering.
        var statsSnapshot = _stateStore.GetPlayerStatsSnapshot();

        // Re-evaluate titles only when lobby membership or stats availability changes
        // so the random pool sample stays stable for the lifetime of a lobby.
        var friendsWithStats = lobby.Friends
            .Select(f => (Friend: f, Stats: statsSnapshot.GetValueOrDefault(f.Puuid)))
            .ToList();

        var titleKey = BuildTitleKey(friendsWithStats);
        if (titleKey != _lastTitleKey)
        {
            _cachedTitles  = TitleEvaluator.Evaluate(friendsWithStats);
            _lastTitleKey  = titleKey;
        }
        var titles = _cachedTitles;

        var positions = _layout.LobbyCardPositions;

        // Maps join-order index of non-local members to visual slot (0-based, left-to-right).
        // Derived from the nonSelfCards algorithm in rcp-fe-lol-parties.js.
        int[] slotMap = { 1, 3, 0, 4 };

        var otherFriends = lobby.Friends
            .Where(f => f.Puuid != lobby.LocalPlayerPuuid)
            .ToList();

        for (int i = 0; i < otherFriends.Count && i < slotMap.Length; i++)
        {
            var friend      = otherFriends[i];
            var stats       = statsSnapshot.GetValueOrDefault(friend.Puuid);
            var titleResult = titles.GetValueOrDefault(friend.Puuid);
            int slotIndex   = slotMap[i];

            var card = CreateLobbyCard(stats, titleResult, _layout.LobbyCardSize.Width, _layout.LobbyCardSize.Height);
            Canvas.SetLeft(card, positions[slotIndex].X);
            Canvas.SetTop(card, positions[slotIndex].Y);
            Canvas.SetZIndex(card, 100);
            _root.Children.Add(card);
        }

        // Local player is always in the centre slot (slot 2).
        var localStats  = statsSnapshot.GetValueOrDefault(lobby.LocalPlayerPuuid);
        var localTitle  = titles.GetValueOrDefault(lobby.LocalPlayerPuuid);
        var localCard   = CreateLobbyCard(localStats, localTitle, _layout.LobbyCardSize.Width, _layout.LobbyCardSize.Height);
        Canvas.SetLeft(localCard, positions[2].X);
        Canvas.SetTop(localCard, positions[2].Y);
        Canvas.SetZIndex(localCard, 100);
        _root.Children.Add(localCard);
    }

    /// <summary>
    /// Builds a transparent card containing a title line and (when awarded) a stat line.
    /// Shows "Loading..." while stats are still being fetched.
    /// </summary>
    private static FrameworkElement CreateLobbyCard(
        PlayerStats? stats, TitleEvaluator.TitleResult? titleResult, double width, double height)
    {
        bool hasTitle  = titleResult.HasValue;
        bool isLoading = stats == null;

        var titleBlock = new TextBlock
        {
            Text      = hasTitle  ? titleResult!.Value.Title
                      : isLoading ? "Loading..."
                      :             "",
            Foreground          = hasTitle
                ? new SolidColorBrush(Color.FromArgb(255, 255, 215, 0))    // gold
                : new SolidColorBrush(Color.FromArgb(120, 200, 200, 200)), // dim grey
            FontSize            = hasTitle ? 16 : 12,
            FontWeight          = hasTitle ? FontWeights.Bold : FontWeights.Normal,
            FontStyle           = hasTitle ? FontStyles.Normal : FontStyles.Italic,
            TextAlignment       = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming        = TextTrimming.CharacterEllipsis,
        };

        var stack = new StackPanel
        {
            Orientation         = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };

        stack.Children.Add(titleBlock);

        if (hasTitle)
        {
            stack.Children.Add(new TextBlock
            {
                Text                = titleResult!.Value.StatLine,
                Foreground          = new SolidColorBrush(Color.FromArgb(160, 180, 210, 255)),
                FontSize            = 10,
                TextAlignment       = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming        = TextTrimming.CharacterEllipsis,
                Margin              = new Thickness(0, 2, 0, 0),
            });
        }

        return new Border
        {
            Width      = width,
            Height     = height,
            Background = Brushes.Transparent,
            Child      = stack,
        };
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
    /// Per-slot configuration for champion tile rendering. Player and bench tiles
    /// have independent sizes and crest offsets defined in Anchors.
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
    /// Builds a transparent champion tile overlay with a mastery crest (top-right)
    /// and a progress bar (bottom edge).
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
    /// Returns the decoded BitmapImage for the given level and icon set, caching after first load.
    /// Returns null if the file isn't on disk yet — the caller renders blank and retries next frame.
    /// </summary>
    private BitmapImage? LoadMasteryCrest(int level, MasteryIconSet iconSet)
    {
        var key = (iconSet, level);

        // Cached null means file wasn't on disk last check; re-check so we pick it up as soon
        // as the background download finishes.
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
/// Returns a gradient brush and glow color for the progress bar.
/// Color transitions red → yellow → green across the 0–1 range.
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

    /// <summary>
    /// Builds a stable cache key encoding lobby membership and which members have stats loaded.
    /// Titles are re-evaluated only when this key changes.
    /// Format: sorted puuids joined with '|', '!' suffix on each puuid that has stats.
    /// </summary>
    private static string BuildTitleKey(List<(LobbyFriend Friend, PlayerStats? Stats)> entries)
    {
        var parts = entries
            .Select(e => e.Stats != null ? e.Friend.Puuid + "!" : e.Friend.Puuid)
            .OrderBy(s => s, StringComparer.Ordinal);
        return string.Join("|", parts);
    }
}