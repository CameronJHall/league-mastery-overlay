namespace league_mastery_overlay.League;

using System.Collections.Generic;

internal sealed class LoginSessionDto
{
    public string? Puuid { get; set; }
    public long? SummonerId { get; set; }
}

internal sealed class ChampionMasteryDto
{
    public int? ChampionId { get; set; }
    public int? ChampionLevel { get; set; }
    public int? ChampionPoints { get; set; }
    public int? ChampionPointsSinceLastLevel { get; set; }
    public int? ChampionPointsUntilNextLevel { get; set; }
}

internal sealed class ChampionSelectDto
{
    public List<List<ChampionSelectAction>>? Actions { get; set; }
    public List<BenchChampion>? BenchChampions { get; set; }
    public List<int>? BenchChampionIds { get; set; }
    public int? LocalPlayerCellId { get; set; }
    public List<ChampionSelectPlayer>? MyTeam { get; set; }
}

internal sealed class ChampionSelectAction
{
    public int? ActorCellId { get; set; }
    public int? ChampionId { get; set; }
    public bool? Completed { get; set; }
    public int? Id { get; set; }
    public bool? IsAllyAction { get; set; }
    public string? Type { get; set; }
}

internal sealed class BenchChampion
{
    public int? ChampionId { get; set; }
}

internal sealed class ChampionSelectPlayer
{
    public int? CellId { get; set; }
    public int? ChampionId { get; set; }
    public int? ChampionPickIntent { get; set; }
}


internal record LcuAuthInfo(
    int Port,
    string Password
);