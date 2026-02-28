namespace league_mastery_overlay.Layout;

/// <summary>
/// Mutable runtime overrides for all anchor values.
/// When a field is non-null, OverlayLayout uses it instead of the Anchors constant.
/// Only compiled in Debug builds — never ships in Release.
/// </summary>
public sealed class AnchorOverrides
{
    // ── Champ Select ─────────────────────────────────────────────────────────
    public double? BenchBaseX      { get; set; }
    public double? BenchBaseY      { get; set; }
    public double? BenchXSpacing   { get; set; }
    public double? PlayerChampionX { get; set; }
    public double? PlayerChampionY { get; set; }

    // ── Lobby ─────────────────────────────────────────────────────────────────
    public double? LobbyCardFirstX   { get; set; }
    public double? LobbyCardFirstY   { get; set; }
    public double? LobbyCardXSpacing { get; set; }
    public double? LobbyCardSizeW    { get; set; }
    public double? LobbyCardSizeH    { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Resets all overrides back to null (Anchors defaults will be used).</summary>
    public void ResetAll()
    {
        BenchBaseX        = null;
        BenchBaseY        = null;
        BenchXSpacing     = null;
        PlayerChampionX   = null;
        PlayerChampionY   = null;
        LobbyCardFirstX   = null;
        LobbyCardFirstY   = null;
        LobbyCardXSpacing = null;
        LobbyCardSizeW    = null;
        LobbyCardSizeH    = null;
    }
}
