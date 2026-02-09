using System.Windows.Controls;
using league_mastery_overlay.State;

namespace league_mastery_overlay.Render;

public sealed class OverlayRenderer
{
    private readonly Canvas _root;
    private readonly StateStore _stateStore;

    public OverlayRenderer(Canvas root, StateStore stateStore)
    {
        _root = root;
        _stateStore = stateStore;
    }

    public void Render()
    {
        LeagueState state = _stateStore.Get();

        // TODO:
        // - Toggle visibility based on phase
        // - Update text/images
        // - Position elements from layout
    }
}