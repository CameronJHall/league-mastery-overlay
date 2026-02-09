using System.Windows;

namespace league_mastery_overlay.Layout;

public sealed class OverlayLayout
{
    public Rect MyPickRect { get; private set; }

    public void Recalculate(Size leagueSize)
    {
        // TODO:
        // - Convert normalized anchors to pixel rects
        // - Store results
    }
}