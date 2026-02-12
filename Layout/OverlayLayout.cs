using System.Windows;

namespace league_mastery_overlay.Layout;

/// <summary>
/// Responsible for converting normalized anchor positions to pixel rectangles
/// based on the League client window size. This is where all scaling math lives.
/// </summary>
public sealed class OverlayLayout
{
    private Size _leagueSize;
    
    // Cached rects - recalculated whenever league size changes
    private Rect _playerChampionRect;
    private Rect[] _benchIconRects = Array.Empty<Rect>();

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
        
        _playerChampionRect = CalculatePlayerChampionRect(leagueSize);
        
        // Bench icons - calculated in a grid pattern
        _benchIconRects = CalculateBenchIconRects(leagueSize);
    }
    
    private Rect CalculatePlayerChampionRect(Size leagueSize)
    {
        const int iconSize = 40; // 36px icon + 4px border (2px on each side)

        double x = Anchors.PlayerChampion.X * leagueSize.Width;
        double y = Anchors.PlayerChampion.Y * leagueSize.Height;

        return new Rect(x, y, iconSize, iconSize);
    }

    /// <summary>
    /// Calculate bench champion icon positions in a grid.
    /// </summary>
    private Rect[] CalculateBenchIconRects(Size leagueSize)
    {
        const int benchIconSize = 22; // 18px icon + 4px border (2px on each side)
        const int benchSlots = 10;

        var rects = new Rect[benchSlots];

        // Start position for bench grid
        var x = Anchors.BenchBase.X * leagueSize.Width;
        var y = Anchors.BenchBase.Y * leagueSize.Height;
        var xOffset = Anchors.BenchXSpacing * leagueSize.Width;

        for (var i = 0; i < benchSlots; i++)
        {
            rects[i] = new Rect(x + (i * xOffset), y, benchIconSize, benchIconSize);
        }

        return rects;
    }

    // Public properties for the renderer to use
    public Rect PlayerChampionRect => _playerChampionRect;
    public Rect[] BenchIconRects => _benchIconRects;
}