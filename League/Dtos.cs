namespace league_mastery_overlay.League;

// TODO: mirror LCU JSON exactly (nullable everywhere)

public sealed class ChampionSelectDto
{
}

public enum GameFlowPhaseDto
{
    None,
    Lobby,
    ChampSelect,
    InProgress,
    EndOfGame
}

public record LcuAuthInfo(
    int Port,
    string Password
);