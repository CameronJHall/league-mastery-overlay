namespace league_mastery_overlay.Layout;

public static class Anchors
{
    // Normalized (0..1) positions
    public static readonly (double X, double Y) BenchBase = (0.275, 0.014); // 401/1278, 11/719
    public static readonly double BenchXSpacing = 0.04576;
    public static readonly (double X, double Y) PlayerChampion = (0.456, 0.7155); // 695/1278, 518/719

    // --- Bench champion tile ---
    // Tile dimensions (normalized 0..1) overlaid on each bench portrait.
    // px: (48.0, 50.0) @ 1280x720
    public static readonly (double W, double H) BenchTileSize = (0.0375, 0.06944);

    // Mastery crest badge size for bench tiles.
    public static readonly double BenchMasteryCrestSize = 24.0;

    // Mastery crest offset for bench tiles (left, top, right, bottom).
    // Positive right/top values pull it inward from the tile edges.
    public static readonly (double Left, double Top, double Right, double Bottom) BenchMasteryCrestOffset = (0, -3, -4, 0);

    // Progress bar height for bench tiles.
    public static readonly double BenchProgressBarHeight = 3.0;

    // --- Player champion tile ---
    // Tile dimensions (normalized 0..1) overlaid on the player's champion portrait.
    // px: (113.0, 65.0) @ 1280x720
    public static readonly (double W, double H) PlayerTileSize = (0.08828, 0.09028);

    // Mastery crest badge size for the player tile.
    public static readonly double PlayerMasteryCrestSize = 24.0;

    // Mastery crest offset for the player tile (left, top, right, bottom).
    public static readonly (double Left, double Top, double Right, double Bottom) PlayerMasteryCrestOffset = (0, -3, -5, 0);

    // Progress bar height for the player tile.
    public static readonly double PlayerProgressBarHeight = 3.0;

    // --- Lobby player cards ---
    // Normalized (0..1) top-left origin of the first player card slot.
    // TODO: calibrate against the actual League client lobby layout.
    public static readonly (double X, double Y) LobbyCardFirst = (0.034, 0.254);

    // Horizontal spacing between successive player card slots (normalized 0..1).
    public static readonly double LobbyCardXSpacing = 0.1531;

    // Number of player card slots (max 5 in a standard lobby).
    public const int LobbyCardSlots = 5;

    // Lobby card tile dimensions (normalized 0..1).
    // Cards are sized to fit within each slot without overlapping the next.
    // px: ~190 wide × 70 tall @ 1280x720
    public static readonly (double W, double H) LobbyCardSize = (0.1454, 0.08922);
}
