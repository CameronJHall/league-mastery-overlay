namespace league_mastery_overlay.Render;

public enum MasteryIconSet
{
    /// <summary>
    /// Modern crest-and-banner icons supporting mastery levels 0–10.
    /// Downloaded from CommunityDragon CDN and cached in AppData on first run.
    /// Filename pattern: crest-and-banner-mastery-{level}.png
    /// </summary>
    Modern,

    /// <summary>
    /// Legacy icons supporting mastery levels 0–7.
    /// Levels above 7 fall back to the level 7 icon.
    /// Downloaded from CommunityDragon CDN and cached in AppData on first run.
    /// Filename pattern: mastery-{level}.png
    /// </summary>
    Legacy
}
