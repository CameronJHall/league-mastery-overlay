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

// ── Lobby (/lol-lobby/v2/lobby) ─────────────────────────────────────────────

internal sealed class LobbyDto
{
    public List<LobbyMemberDto>? Members { get; set; }
    public LobbyMemberDto? LocalMember { get; set; }
}

internal sealed class LobbyMemberDto
{
    public string? Puuid { get; set; }
    public long? SummonerId { get; set; }
    public int? SummonerIconId { get; set; }
    public int? SummonerLevel { get; set; }
    public bool? IsLeader { get; set; }
}

// ── Friends (/lol-chat/v1/friends) ──────────────────────────────────────────

internal sealed class FriendDto
{
    public string? Puuid { get; set; }
    public long? SummonerId { get; set; }
    public string? GameName { get; set; }
    public string? GameTag { get; set; }
    public int? Icon { get; set; }
}

// ── Match history (/lol-match-history/v1/products/lol/{puuid}/matches) ──────

internal sealed class MatchHistoryDto
{
    public MatchHistoryGamesDto? Games { get; set; }
}

internal sealed class MatchHistoryGamesDto
{
    public List<MatchDto>? Games { get; set; }
}

internal sealed class MatchDto
{
    public long? GameId { get; set; }
    public string? GameMode { get; set; }
    public int? MapId { get; set; }
    public int? QueueId { get; set; }
    public long? GameDuration { get; set; }
    public List<ParticipantIdentityDto>? ParticipantIdentities { get; set; }
    public List<ParticipantDto>? Participants { get; set; }
}

internal sealed class ParticipantIdentityDto
{
    public int? ParticipantId { get; set; }
    public ParticipantPlayerDto? Player { get; set; }
}

internal sealed class ParticipantPlayerDto
{
    public string? Puuid { get; set; }
    public string? GameName { get; set; }
    public string? TagLine { get; set; }
}

internal sealed class ParticipantDto
{
    public int? ParticipantId { get; set; }
    public int? ChampionId { get; set; }
    public int? TeamId { get; set; }
    public ParticipantStatsDto? Stats { get; set; }
}

internal sealed class ParticipantStatsDto
{
    public bool? Win { get; set; }
    public int?  Kills { get; set; }
    public int?  Deaths { get; set; }
    public int?  Assists { get; set; }
    public long? TotalDamageDealtToChampions { get; set; }
    public long? TotalHeal { get; set; }
    public long? TotalDamageTaken { get; set; }
    public bool? GameEndedInSurrender { get; set; }
    public bool? GameEndedInEarlySurrender { get; set; }
    public long? DamageSelfMitigated { get; set; }
    public long? TimeCCingOthers { get; set; }
    public int?  VisionScore { get; set; }
    public int?  WardsPlaced { get; set; }
    public int?  TotalMinionsKilled { get; set; }
}