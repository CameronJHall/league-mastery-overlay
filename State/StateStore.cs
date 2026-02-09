using System.Threading;

namespace league_mastery_overlay.State;

public sealed class StateStore
{
    private LeagueState _state = new(GamePhase.None, null);

    public void Update(LeagueState newState)
    {
        Interlocked.Exchange(ref _state, newState);
    }

    public LeagueState Get()
    {
        return _state;
    }
}