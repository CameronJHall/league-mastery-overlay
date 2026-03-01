using System.Windows;

namespace league_mastery_overlay.Layout;

/// <summary>
/// Responsible for converting normalized anchor positions to pixel coordinates
/// based on the League client window size. This is where all scaling math lives.
/// </summary>
public sealed class OverlayLayout
{
    private Size _leagueSize;

    // Runtime overrides injected by the debug tuner (null in Release builds).
    // When set, Recalculate reads these instead of the Anchors constants.
    private AnchorOverrides? _overrides;

    // Cached coordinates - recalculated whenever league size changes
    private Point _playerChampionPos;
    private Point[] _benchIconPositions = Array.Empty<Point>();
    private Point[] _lobbyCardPositions = Array.Empty<Point>();

    public OverlayLayout()
    {
        _leagueSize = new Size(0, 0);
    }

    /// <summary>
    /// Attaches a live overrides object. Every subsequent Recalculate() call
    /// will prefer override values over the Anchors constants.
    /// Call with null to detach and revert to Anchors defaults.
    /// </summary>
    public void SetOverrides(AnchorOverrides? overrides)
    {
        _overrides = overrides;
        // Force a full recalculation on the next Recalculate() call.
        _leagueSize = new Size(0, 0);
    }

    /// <summary>
    /// Forces an immediate recalculation regardless of whether the size changed.
    /// Used by the tuner after updating override values at runtime.
    /// </summary>
    public void ForceRecalculate()
    {
        _leagueSize = new Size(0, 0);
    }

    /// <summary>
    /// Recalculates all layout positions based on the current League client size.
    /// Call this whenever the League window is resized.
    /// </summary>
    public void Recalculate(Size leagueSize)
    {
        // Only recalculate if the size actually changed
        if (_leagueSize == leagueSize)
            return;

        _leagueSize = leagueSize;
        
        _playerChampionPos = CalculatePlayerChampionPosition(leagueSize);
        
        // Bench icons - calculated in a grid pattern
        _benchIconPositions = CalculateBenchIconPositions(leagueSize);

        // Lobby player card slots - spaced horizontally
        _lobbyCardPositions = CalculateLobbyCardPositions(leagueSize);
    }
    
    private Point CalculatePlayerChampionPosition(Size leagueSize)
    {
        double x = (_overrides?.PlayerChampionX ?? Anchors.PlayerChampion.X) * leagueSize.Width;
        double y = (_overrides?.PlayerChampionY ?? Anchors.PlayerChampion.Y) * leagueSize.Height;

        return new Point(x, y);
    }

    /// <summary>
    /// Calculate bench champion icon positions in a grid.
    /// </summary>
    private Point[] CalculateBenchIconPositions(Size leagueSize)
    {
        const int benchSlots = 10;

        var positions = new Point[benchSlots];

        // Start position for bench grid
        var x       = (_overrides?.BenchBaseX    ?? Anchors.BenchBase.X)    * leagueSize.Width;
        var y       = (_overrides?.BenchBaseY    ?? Anchors.BenchBase.Y)    * leagueSize.Height;
        var xOffset = (_overrides?.BenchXSpacing ?? Anchors.BenchXSpacing)  * leagueSize.Width;

        for (var i = 0; i < benchSlots; i++)
        {
            positions[i] = new Point(x + (i * xOffset), y);
        }

        return positions;
    }

    /// <summary>
    /// Calculate lobby player card top-left positions, spaced horizontally.
    /// Update Anchors.LobbyCardFirst and Anchors.LobbyCardXSpacing to calibrate.
    /// </summary>
    private Point[] CalculateLobbyCardPositions(Size leagueSize)
    {
        var positions = new Point[Anchors.LobbyCardSlots];
        double x     = (_overrides?.LobbyCardFirstX  ?? Anchors.LobbyCardFirst.X)  * leagueSize.Width;
        double y     = (_overrides?.LobbyCardFirstY  ?? Anchors.LobbyCardFirst.Y)  * leagueSize.Height;
        double xStep = (_overrides?.LobbyCardXSpacing ?? Anchors.LobbyCardXSpacing) * leagueSize.Width;

        for (int i = 0; i < Anchors.LobbyCardSlots; i++)
        {
            positions[i] = new Point(x + i * xStep, y);
        }

        return positions;
    }

    // Public properties for the renderer to use
    public Point PlayerChampionPos => _playerChampionPos;
    public Point[] BenchIconPositions => _benchIconPositions;
    public Point[] LobbyCardPositions => _lobbyCardPositions;

    /// <summary>
    /// Pixel dimensions of the player champion tile, scaled to the current league window size.
    /// Update Anchors.PlayerTileSize to calibrate.
    /// </summary>
    public Size PlayerTileSize => new Size(
        Anchors.PlayerTileSize.W * _leagueSize.Width,
        Anchors.PlayerTileSize.H * _leagueSize.Height);

    /// <summary>
    /// Pixel dimensions of a bench champion tile, scaled to the current league window size.
    /// Update Anchors.BenchTileSize to calibrate.
    /// </summary>
    public Size BenchTileSize => new Size(
        Anchors.BenchTileSize.W * _leagueSize.Width,
        Anchors.BenchTileSize.H * _leagueSize.Height);

    /// <summary>
    /// Pixel dimensions of a single lobby player card, scaled to the current league window size.
    /// Update Anchors.LobbyCardSize to calibrate.
    /// </summary>
    public Size LobbyCardSize => new Size(
        (_overrides?.LobbyCardSizeW ?? Anchors.LobbyCardSize.W) * _leagueSize.Width,
        (_overrides?.LobbyCardSizeH ?? Anchors.LobbyCardSize.H) * _leagueSize.Height);
}