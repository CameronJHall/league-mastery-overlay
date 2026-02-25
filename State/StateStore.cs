using System.Threading;

namespace league_mastery_overlay.State;

public sealed class StateStore
{
    private GamePhase _phase = GamePhase.None;
    private ChampionSelectState? _championSelect = null;
    private Dictionary<int, MasteryData> _masteryData = new();
    private LobbyState? _lobby = null;

    // Stats cache: keyed by puuid, populated async as stats are fetched.
    // Written from background threads; read from the render thread.
    // Cleared when leaving the lobby phase.
    private Dictionary<string, PlayerStats> _playerStatsCache = new();
    private readonly object _statsCacheLock = new();

    public void UpdateGamePhase(GamePhase phase)
    {
        Interlocked.Exchange(ref _phase, phase);
    }
    
    public void UpdateChampionSelectState(ChampionSelectState? state)
    {
        Interlocked.Exchange(ref _championSelect, state);
    }
    
    public void UpdateMasteryData(Dictionary<int, MasteryData> data)
    {
        Interlocked.Exchange(ref _masteryData, data);
    }

    public void UpdateLobbyState(LobbyState? lobby)
    {
        Interlocked.Exchange(ref _lobby, lobby);
    }

    /// <summary>
    /// Adds or updates a single player's stats in the cache.
    /// Safe to call from any thread.
    /// </summary>
    public void UpdatePlayerStats(string puuid, PlayerStats stats)
    {
        lock (_statsCacheLock)
            _playerStatsCache[puuid] = stats;
    }

    /// <summary>
    /// Removes all cached stats. Call when leaving the lobby phase.
    /// </summary>
    public void ClearPlayerStatsCache()
    {
        lock (_statsCacheLock)
            _playerStatsCache = new();
    }

    /// <summary>
    /// Returns true if stats are already cached for this puuid.
    /// </summary>
    public bool HasPlayerStats(string puuid)
    {
        lock (_statsCacheLock)
            return _playerStatsCache.ContainsKey(puuid);
    }

    public GamePhase GetGamePhase() => _phase;

    public ChampionSelectState? GetChampionSelectState() => _championSelect;

    public Dictionary<int, MasteryData> GetMasteryData() => _masteryData;

    public LobbyState? GetLobbyState() => _lobby;

    /// <summary>
    /// Returns a snapshot of the current stats cache for rendering.
    /// The returned dictionary is a copy — safe to read without holding the lock.
    /// </summary>
    public Dictionary<string, PlayerStats> GetPlayerStatsSnapshot()
    {
        lock (_statsCacheLock)
            return new Dictionary<string, PlayerStats>(_playerStatsCache);
    }

    /// <summary>
    /// Returns a consistent snapshot of all state fields for rendering.
    /// </summary>
    public LeagueState Get() => new(_phase, _championSelect, _masteryData, _lobby);
}