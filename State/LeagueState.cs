namespace league_mastery_overlay.State;

using System.Collections.Generic;

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
    Dictionary<int, MasteryData> ChampMasteryData,
    LobbyState? Lobby
);

public record MasteryData(
    int Level,
    float MasteryProgress
);

public record ChampionSelectState(
    int? MyChampion,
    int[] BenchChampions
);

/// <summary>
/// Computed stats derived from a player's recent match history (last 20 games).
/// </summary>
public record PlayerStats(
    int    WinStreak,        // consecutive wins from most recent game backwards (0 if last game was a loss)
    int    LossStreak,       // consecutive losses from most recent game backwards (0 if last game was a win)
    double AvgDamage,        // average totalDamageDealtToChampions over sample
    double AvgHealing,       // average totalHeal over sample
    double SurrenderRate,    // fraction of games that ended in surrender (either kind)
    double AvgDeaths,        // average deaths per game
    double AvgKills,         // average kills per game
    double AvgAssists,       // average assists per game
    double AvgDamageTaken,   // average totalDamageTaken per game
    double AvgSelfMitigated, // average damageSelfMitigated per game
    double AvgCCTime,        // average timeCCingOthers per game
    double AvgVisionScore,   // average visionScore per game
    double AvgWardsPlaced,   // average wardsPlaced per game
    double AvgCS             // average totalMinionsKilled per game
);

/// <summary>
/// A lobby member who is on the local player's friends list.
/// </summary>
public record LobbyFriend(
    string Puuid,
    long SummonerId,
    string GameName,     // e.g. "ThiccThighKing"
    string GameTag,      // e.g. "NA1"
    bool IsLeader
);

/// <summary>
/// Snapshot of the pre-game lobby relevant to the overlay.
/// </summary>
public record LobbyState(
    string LocalPlayerPuuid,
    List<LobbyFriend> Friends  // only friends-list members, up to 4 others + local player
);
