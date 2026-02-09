namespace league_mastery_overlay.State;

public enum GamePhase
{
    None,
    ChampSelect,
    InGame
}

public record LeagueState(
    GamePhase Phase,
    ChampionSelectState? ChampionSelect
);

public record ChampionSelectState(
    string? MyChampion,
    string[] Picks,
    string[] Bans
);