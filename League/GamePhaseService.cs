using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

internal class GamePhaseService(LcuClient client)
{
    public async Task<GamePhase> PollAsync()
    {
        var raw = await client.GetAsync<string>("/lol-gameflow/v1/gameflow-phase");
        return raw?.Trim('"') switch
        {
            "Lobby" => GamePhase.Lobby,
            "Matchmaking" => GamePhase.Matchmaking,
            "ReadyCheck" => GamePhase.ReadyCheck,
            "ChampSelect" => GamePhase.ChampSelect,
            "InProgress" => GamePhase.InProgress,
            "PreEndOfGame" => GamePhase.PreEndOfGame,
            "EndOfGame" => GamePhase.EndOfGame,
            _ => GamePhase.None
        };
    }
}