using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

internal sealed class MasteryDataService(LcuClient client)
{
    public async Task<Dictionary<int, MasteryData>> GetMasteryDataAsync()
    {
        // Fetch champion mastery data
        var masteries = await client.GetAsync<List<ChampionMasteryDto>>(
            $"/lol-champion-mastery/v1/local-player/champion-mastery"
        );

        // Convert string keys to int
        var masteryData = new Dictionary<int, MasteryData>();
        if (masteries == null)
            return masteryData;

        foreach (var championMastery in masteries)
        {
            if (championMastery.ChampionId is > 0)
            {
                masteryData[championMastery.ChampionId.Value] = new MasteryData(
                    championMastery.ChampionLevel ?? 0,
                    ComputeMasteryProgress(championMastery)
                );
            }
        }

        return masteryData;
    }
    
    private float ComputeMasteryProgress(ChampionMasteryDto? mastery) {
        if (mastery == null) return 0f;
        
        long since = mastery.ChampionPointsSinceLastLevel ?? 0;
        long until = mastery.ChampionPointsUntilNextLevel ?? 0;
        var total = since + until;
        
        if (total == 0) return 0f;
        
        return (float)since / total;
    }
}