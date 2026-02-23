namespace league_mastery_overlay.Render;

public enum MasteryIconSet
{
    /// <summary>
    /// Modern crest-and-banner icons supporting mastery levels 0–9.
    /// Loaded from Resources/MasteryIcons/crest-and-banner-mastery-{level}.png.
    /// </summary>
    Modern,

    /// <summary>
    /// Legacy icons supporting mastery levels 0–7.
    /// Levels above 7 fall back to the level 7 icon.
    /// Loaded from Resources/LegacyMasteryIcons/mastery-{level}.png.
    /// </summary>
    Legacy
}
