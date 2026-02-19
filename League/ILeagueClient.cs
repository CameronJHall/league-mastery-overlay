using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

public interface ILeagueClient : IDisposable
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
}
