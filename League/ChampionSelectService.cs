using System.Diagnostics;
using System.Linq;
using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

public sealed class ChampionSelectService
{
    private readonly LcuClient _client;
    private string? _cachedPuuid;

    public ChampionSelectService(LcuClient client)
    {
        _client = client;
    }

    public async Task<ChampionSelectState?> PollAsync()
    {
        if (string.IsNullOrEmpty(_cachedPuuid))
        {
            var session = await _client.GetAsync<LoginSessionDto>("/lol-login/v1/session");
            if (!string.IsNullOrEmpty(session?.Puuid))
            {
                _cachedPuuid = session.Puuid;
                Debug.WriteLine($"[DEBUG_LOG] Player PUUID: {_cachedPuuid}");
            }
        }

        var dto = await _client.GetAsync<ChampionSelectDto>(
            "/lol-champ-select/v1/session"
        );

        if (dto == null)
            return null;

        // Determine the player's selected champion
        int? myChampionId = dto.Actions?
            .SelectMany(group => group)
            .FirstOrDefault(a => a.ActorCellId == dto.LocalPlayerCellId && a.Type == "pick" && a.Completed == true)?
            .ChampionId;

        // Extract bench champion IDs and mastery
        var benchChampions = new List<ChampionData>();
        var ids = dto.BenchChampionIds ?? dto.BenchChampions?.Select(c => c.ChampionId ?? 0).ToList() ?? new List<int>();

        Debug.WriteLine($"[DEBUG_LOG] Selected Champion ID: {myChampionId?.ToString() ?? "None"}");
        Debug.WriteLine($"[DEBUG_LOG] Bench Champion IDs ({ids.Count}): {string.Join(", ", ids)}");

        ChampionData? myChampionData = null;

        // Fetch champion mastery data
        if (!string.IsNullOrEmpty(_cachedPuuid))
        {
            var masteries = await _client.GetAsync<List<ChampionMasteryDto>>(
                $"/lol-champion-mastery/v1/{_cachedPuuid}/champion-mastery"
            );

            if (masteries != null)
            {
                if (myChampionId.HasValue)
                {
                    var myMastery = masteries.FirstOrDefault(m => m.ChampionId == myChampionId.Value);
                    myChampionData = new ChampionData(myChampionId.Value, myMastery?.ChampionLevel ?? 0, ComputeMasteryProgress(myMastery));
                    Debug.WriteLine($"[DEBUG_LOG] Selected Champ Mastery: Level {myMastery?.ChampionLevel ?? 0} ({myMastery?.ChampionPoints ?? 0} pts, Progress: {myChampionData.MasteryProgress:P1})");
                }

                foreach (var id in ids)
                {
                    var m = masteries.FirstOrDefault(mast => mast.ChampionId == id);
                    benchChampions.Add(new ChampionData(id, m?.ChampionLevel ?? 0, ComputeMasteryProgress(m)));
                }

                var benchMasteriesLog = ids.Take(5).Select(id =>
                {
                    var m = masteries.FirstOrDefault(mast => mast.ChampionId == id);
                    var progress = ComputeMasteryProgress(m);
                    return $"ID {id}: Level {m?.ChampionLevel ?? 0} ({progress:P1})";
                });

                Debug.WriteLine($"[DEBUG_LOG] Bench Masteries (Top 5): {string.Join(" | ", benchMasteriesLog)}");
            }
        }

        return new ChampionSelectState(
            MyChampion: myChampionData,
            BenchChampions: benchChampions.ToArray()
        );
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