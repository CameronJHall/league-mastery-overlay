using System.Diagnostics;
using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

internal sealed class LeagueClient : ILeagueClient
{
    private readonly LcuAuthProvider _authProvider = new();
    private LcuClient? _client;
    private GamePhaseService? _gamePhaseService;
    private ChampionSelectService? _champSelectService;
    private MasteryDataService? _masteryDataService;
    private LobbyService? _lobbyService;
    private MatchHistoryService? _matchHistoryService;
    private Dictionary<string, FriendDto> _friendsCache = new();

    public IReadOnlyDictionary<string, FriendDto> CachedFriends => _friendsCache;

    public bool IsConnected => _client != null;

    public bool TryConnect()
    {
        if (IsConnected)
            return true;

        if (!_authProvider.TryGetAuth(out var auth))
        {
            Debug.WriteLine("[LeagueClient] TryConnect: League not running or lockfile not found");
            return false;
        }

        Debug.WriteLine($"[LeagueClient] Connecting to port {auth.Port}");
        _client = new LcuClient(auth);
        _gamePhaseService = new GamePhaseService(_client);
        _champSelectService = new ChampionSelectService(_client);
        _masteryDataService = new MasteryDataService(_client);
        _lobbyService = new LobbyService(_client);
        _matchHistoryService = new MatchHistoryService(_client);
        return true;
    }

    public Task<bool> IsStillAliveAsync()
    {
        if (_client == null)
            return Task.FromResult(false);

        return _client.IsConnectedAsync();
    }

    public void Disconnect()
    {
        if (_client == null)
            return;

        Debug.WriteLine("[LeagueClient] Disconnecting");
        _client.Dispose();
        _client = null;
        _gamePhaseService = null;
        _champSelectService = null;
        _masteryDataService = null;
        _lobbyService = null;
        _matchHistoryService = null;
        _friendsCache = new();
    }

    public Task<GamePhase> GetGamePhaseAsync()
    {
        if (_gamePhaseService == null)
            return Task.FromResult(GamePhase.None);

        return _gamePhaseService.PollAsync();
    }

    public Task<ChampionSelectState?> GetChampionSelectStateAsync()
    {
        if (_champSelectService == null)
            return Task.FromResult<ChampionSelectState?>(null);

        return _champSelectService.PollAsync();
    }
    
    public Task<Dictionary<int, MasteryData>> GetMasteryDataAsync()
    {
        if (_masteryDataService == null)
            return Task.FromResult<Dictionary<int, MasteryData>>(new());

        return _masteryDataService.GetMasteryDataAsync();
    }

    public async Task FetchFriendsAsync()
    {
        if (_client == null)
            return;

        var friends = await _client.GetAsync<List<FriendDto>>("/lol-chat/v1/friends");
        _friendsCache = (friends ?? [])
            .Where(f => f.Puuid != null)
            .ToDictionary(f => f.Puuid!, f => f);

        Debug.WriteLine($"[LeagueClient] Friends cache populated: {_friendsCache.Count} entries");
    }

    public Task<LobbyState?> GetLobbyStateAsync()
    {
        if (_lobbyService == null)
            return Task.FromResult<LobbyState?>(null);

        return _lobbyService.PollAsync(_friendsCache);
    }

    public Task<PlayerStats?> GetPlayerStatsAsync(string puuid)
    {
        if (_matchHistoryService == null)
            return Task.FromResult<PlayerStats?>(null);

        return _matchHistoryService.GetPlayerStatsAsync(puuid);
    }

    public void Dispose() => Disconnect();
}
