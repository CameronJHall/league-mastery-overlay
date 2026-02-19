using System.Threading;

namespace league_mastery_overlay.State;

public sealed class StateStore
{
    private GamePhase _phase = GamePhase.None;
    private ChampionSelectState? _championSelect = null;

    public void UpdateGamePhase(GamePhase phase)
    {
        Interlocked.Exchange(ref _phase, phase);
    }
    
    public void UpdateChampionSelectState(ChampionSelectState? state)
    {
        Interlocked.Exchange(ref _championSelect, state);
    }

    public GamePhase GetGamePhase() => _phase;

    public ChampionSelectState? GetChampionSelectState() => _championSelect;

    /// <summary>
    /// Returns a consistent snapshot of both state fields for rendering.
    /// </summary>
    public LeagueState Get() => new(_phase, _championSelect);
}