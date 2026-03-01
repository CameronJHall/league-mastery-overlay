using System.Diagnostics;
using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

/// <summary>
/// Fetches the last 20 games for a given puuid and computes PlayerStats from them.
/// Match history via the LCU only returns the local player's participantIdentity entry,
/// so we identify the player's participant by matching the puuid in participantIdentities.
/// </summary>
internal sealed class MatchHistoryService(LcuClient client)
{
    public async Task<PlayerStats?> GetPlayerStatsAsync(string puuid)
    {
        var dto = await client.GetAsync<MatchHistoryDto>(
            $"/lol-match-history/v1/products/lol/{puuid}/matches?begIndex=0&endIndex=19"
        );

        var games = dto?.Games?.Games;
        if (games == null || games.Count == 0)
            return null;

        int  winStreak    = 0;
        int  lossStreak   = 0;
        bool streakBroken = false;

        // Exponential decay weighting — game 0 (most recent) = weight 1.0, each older
        // game is multiplied by DecayFactor. Tune to adjust recency bias.
        const double DecayFactor = 0.85;

        double wDamage        = 0;
        double wHealing       = 0;
        double wDamageTaken   = 0;
        double wSelfMitigated = 0;
        double wCCTime        = 0;
        double wVisionScore   = 0;
        double wWardsPlaced   = 0;
        double wCS            = 0;
        double wKills         = 0;
        double wDeaths        = 0;
        double wAssists       = 0;
        double wSurrenders    = 0;
        double totalWeight    = 0;
        int    validGames     = 0;

        // Games are ordered newest-first; i is used as the decay exponent directly
        // so skipped games don't shift the weights of games that follow them.
        for (int i = 0; i < games.Count; i++)
        {
            var game = games[i];

            // Locate this player's participant entry
            var identity = game.ParticipantIdentities?
                .FirstOrDefault(p => p.Player?.Puuid == puuid);

            if (identity?.ParticipantId == null)
            {
                Debug.WriteLine($"[MatchHistoryService] Could not find participant for puuid {puuid} in game {game.GameId}");
                continue;
            }

            var participant = game.Participants?
                .FirstOrDefault(p => p.ParticipantId == identity.ParticipantId);

            if (participant?.Stats == null)
                continue;

            var stats = participant.Stats;
            bool won        = stats.Win ?? false;
            bool surrendered = (stats.GameEndedInSurrender ?? false)
                            || (stats.GameEndedInEarlySurrender ?? false);

            // Streak: count consecutive same-outcome from most recent game backwards.
            if (!streakBroken)
            {
                if (i == 0)
                {
                    if (won) winStreak++;
                    else     lossStreak++;
                }
                else
                {
                    bool prevWon = winStreak > 0;
                    if (won == prevWon)
                    {
                        if (won) winStreak++;
                        else     lossStreak++;
                    }
                    else
                    {
                        streakBroken = true;
                    }
                }
            }

            double w = Math.Pow(DecayFactor, i);
            wDamage        += (stats.TotalDamageDealtToChampions ?? 0) * w;
            wHealing       += (stats.TotalHeal                   ?? 0) * w;
            wDamageTaken   += (stats.TotalDamageTaken            ?? 0) * w;
            wSelfMitigated += (stats.DamageSelfMitigated         ?? 0) * w;
            wCCTime        += (stats.TimeCCingOthers             ?? 0) * w;
            wVisionScore   += (stats.VisionScore                 ?? 0) * w;
            wWardsPlaced   += (stats.WardsPlaced                 ?? 0) * w;
            wCS            += (stats.TotalMinionsKilled          ?? 0) * w;
            wKills         += (stats.Kills                       ?? 0) * w;
            wDeaths        += (stats.Deaths                      ?? 0) * w;
            wAssists       += (stats.Assists                     ?? 0) * w;
            if (surrendered) wSurrenders += w;
            totalWeight += w;
            validGames++;
        }

        if (validGames == 0)
            return null;

        return new PlayerStats(
            WinStreak:        winStreak,
            LossStreak:       lossStreak,
            AvgDamage:        wDamage        / totalWeight,
            AvgHealing:       wHealing       / totalWeight,
            SurrenderRate:    wSurrenders    / totalWeight,
            AvgDeaths:        wDeaths        / totalWeight,
            AvgKills:         wKills         / totalWeight,
            AvgAssists:       wAssists       / totalWeight,
            AvgDamageTaken:   wDamageTaken   / totalWeight,
            AvgSelfMitigated: wSelfMitigated / totalWeight,
            AvgCCTime:        wCCTime        / totalWeight,
            AvgVisionScore:   wVisionScore   / totalWeight,
            AvgWardsPlaced:   wWardsPlaced   / totalWeight,
            AvgCS:            wCS            / totalWeight
        );
    }
}

