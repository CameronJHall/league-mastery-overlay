using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

public sealed class ChampionSelectService
{
    private readonly LcuClient _client;

    public ChampionSelectService(LcuClient client)
    {
        _client = client;
    }

    public async Task<ChampionSelectState?> PollAsync()
    {
        var dto = await _client.GetAsync<ChampionSelectDto>(
            "/lol-champ-select/v1/session"
        );

        if (dto == null)
            return null;

        // TODO:
        // - Resolve champion IDs to names
        // - Identify "me"
        // - Extract picks/bans properly

        return new ChampionSelectState(
            MyChampion: null,
            Picks: Array.Empty<string>(),
            Bans: Array.Empty<string>()
        );
    }
}