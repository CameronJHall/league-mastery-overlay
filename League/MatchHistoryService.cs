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
            $"/lol-match-history/v1/products/lol/{puuid}/matches?begIndex=0&endIndex=5"
        );

        var games = dto?.Games?.Games;
        if (games == null || games.Count == 0)
            return null;

        int winStreak  = 0;
        int lossStreak = 0;
        bool streakBroken = false;

        long totalDamage  = 0;
        long totalHealing = 0;
        int  surrenders   = 0;
        int  validGames   = 0;

        // Games are ordered newest-first in the LCU response
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
            bool won = stats.Win ?? false;
            bool surrendered = (stats.GameEndedInSurrender ?? false)
                            || (stats.GameEndedInEarlySurrender ?? false);

            // Streak: count consecutive same-outcome from game 0 (most recent)
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

            totalDamage  += stats.TotalDamageDealtToChampions ?? 0;
            totalHealing += stats.TotalHeal ?? 0;
            if (surrendered) surrenders++;
            validGames++;
        }

        if (validGames == 0)
            return null;

        return new PlayerStats(
            WinStreak:      winStreak,
            LossStreak:     lossStreak,
            AvgDamage:      (double)totalDamage  / validGames,
            AvgHealing:     (double)totalHealing / validGames,
            SurrenderRate:  (double)surrenders   / validGames
        );
    }
}
