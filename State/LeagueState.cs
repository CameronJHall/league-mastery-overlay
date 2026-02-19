namespace league_mastery_overlay.State;

public enum GamePhase
{
    None,
    Lobby,
    Matchmaking,
    ReadyCheck,
    ChampSelect,
    InProgress,
    PreEndOfGame,
    EndOfGame
}

public record LeagueState(
    GamePhase Phase,
    ChampionSelectState? ChampionSelect
);

public record ChampionData(
    int Id,
    int Level,
    float MasteryProgress
);

public record ChampionSelectState(
    ChampionData? MyChampion,
    ChampionData[] BenchChampions
);

