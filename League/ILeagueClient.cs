using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

internal interface ILeagueClient : IDisposable
{
    /// <summary>
    /// Attempts to locate the League client lockfile and establish a connection.
    /// Safe to call repeatedly — returns true immediately if already connected.
    /// Returns false if League is not running.
    /// </summary>
    bool TryConnect();

    /// <summary>
    /// True if a live connection to the League client currently exists.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Checks whether the existing connection is still alive via a lightweight HTTP ping.
    /// Should only be called when IsConnected is true.
    /// </summary>
    Task<bool> IsStillAliveAsync();

    /// <summary>
    /// Tears down the current connection. Safe to call when already disconnected.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Fetches the friends list from the LCU and caches it for the session.
    /// Should be called once after a successful connection is established.
    /// </summary>
    Task FetchFriendsAsync();

    /// <summary>
    /// Returns the cached friends list keyed by puuid.
    /// Empty if FetchFriendsAsync has not been called yet or the fetch failed.
    /// </summary>
    IReadOnlyDictionary<string, FriendDto> CachedFriends { get; }

    /// <summary>
    /// Polls the current game flow phase from the League client.
    /// </summary>
    Task<GamePhase> GetGamePhaseAsync();

    /// <summary>
    /// Polls the current champion select session.
    /// Returns null if not in champion select.
    /// </summary>
    Task<ChampionSelectState?> GetChampionSelectStateAsync();
    
    /// <summary>
    /// Polls the current champion mastery data.
    /// </summary>
    Task<Dictionary<int, MasteryData>> GetMasteryDataAsync();

    /// <summary>
    /// Polls the current lobby and returns friends-list members.
    /// Stats and Title fields on each LobbyFriend are null; use GetPlayerStatsAsync to fill them.
    /// </summary>
    Task<LobbyState?> GetLobbyStateAsync();

    /// <summary>
    /// Fetches the last 20 games for the given puuid and computes PlayerStats.
    /// Returns null if history is unavailable.
    /// </summary>
    Task<PlayerStats?> GetPlayerStatsAsync(string puuid);
}
