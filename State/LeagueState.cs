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
    ChampionSelectState? ChampionSelect,
    Dictionary<int, MasteryData>  ChampMasteryData
);

public record MasteryData(
    int Level,
    float MasteryProgress
);

public record ChampionSelectState(
    int? MyChampion,
    int[] BenchChampions
);
