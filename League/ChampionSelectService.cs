using System.Diagnostics;
using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

internal sealed class ChampionSelectService(LcuClient client)
{
    
    public async Task<ChampionSelectState?> PollAsync()
    {
        var dto = await client.GetAsync<ChampionSelectDto>(
            "/lol-champ-select/v1/session"
        );

        if (dto == null)
            return null;

        // Determine the player's selected champion
        var myChampionId = dto.Actions?
            .SelectMany(group => group)
            .FirstOrDefault(a => a.ActorCellId == dto.LocalPlayerCellId && a.Type == "pick" && a.Completed == true)?
            .ChampionId;

        // Extract bench champion IDs - API may return either a flat int list or an object list
        var ids = dto.BenchChampionIds?.ToArray()
                  ?? dto.BenchChampions?.Select(c => c.ChampionId ?? 0).Where(id => id > 0).ToArray()
                  ?? Array.Empty<int>();
        Debug.WriteLine($"[ChampionSelectService] Bench IDs: [{string.Join(", ", ids)}]");

        return new ChampionSelectState(
            MyChampion: myChampionId,
            BenchChampions: ids
        );
    }
}