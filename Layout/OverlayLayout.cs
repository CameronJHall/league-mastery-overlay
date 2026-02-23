using System.Windows;

namespace league_mastery_overlay.Layout;

/// <summary>
/// Responsible for converting normalized anchor positions to pixel coordinates
/// based on the League client window size. This is where all scaling math lives.
/// </summary>
public sealed class OverlayLayout
{
    private Size _leagueSize;
    
    // Cached coordinates - recalculated whenever league size changes
    private Point _playerChampionPos;
    private Point[] _benchIconPositions = Array.Empty<Point>();

    public OverlayLayout()
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
    }
    
    private Point CalculatePlayerChampionPosition(Size leagueSize)
    {
        double x = Anchors.PlayerChampion.X * leagueSize.Width;
        double y = Anchors.PlayerChampion.Y * leagueSize.Height;

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
        var x = Anchors.BenchBase.X * leagueSize.Width;
        var y = Anchors.BenchBase.Y * leagueSize.Height;
        var xOffset = Anchors.BenchXSpacing * leagueSize.Width;

        for (var i = 0; i < benchSlots; i++)
        {
            positions[i] = new Point(x + (i * xOffset), y);
        }

        return positions;
    }

    // Public properties for the renderer to use
    public Point PlayerChampionPos => _playerChampionPos;
    public Point[] BenchIconPositions => _benchIconPositions;

    /// <summary>
    /// Pixel dimensions of the player champion tile.
    /// Update Anchors.PlayerTileSize to calibrate.
    /// </summary>
    public Size PlayerTileSize => new Size(Anchors.PlayerTileSize.W, Anchors.PlayerTileSize.H);

    /// <summary>
    /// Pixel dimensions of a bench champion tile.
    /// Update Anchors.BenchTileSize to calibrate.
    /// </summary>
    public Size BenchTileSize => new Size(Anchors.BenchTileSize.W, Anchors.BenchTileSize.H);
}