using System.Threading;

namespace league_mastery_overlay.State;

public sealed class StateStore
{
    private GamePhase _phase = GamePhase.None;
    private ChampionSelectState? _championSelect = null;
    private Dictionary<int, MasteryData> _masteryData = new();

    public void UpdateGamePhase(GamePhase phase)
    {
        Interlocked.Exchange(ref _phase, phase);
    }
    
    public void UpdateChampionSelectState(ChampionSelectState? state)
    {
        Interlocked.Exchange(ref _championSelect, state);
    }
    
    public void UpdateMasteryData(Dictionary<int, MasteryData> data)
    {
        Interlocked.Exchange(ref _masteryData, data);
    }

    public GamePhase GetGamePhase() => _phase;

    public ChampionSelectState? GetChampionSelectState() => _championSelect;

    public Dictionary<int, MasteryData> GetMasteryData() => _masteryData;

    /// <summary>
    /// Returns a consistent snapshot of all state fields for rendering.
    /// </summary>
    public LeagueState Get() => new(_phase, _championSelect, _masteryData);
    
    
}