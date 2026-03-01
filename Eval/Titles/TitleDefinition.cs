using league_mastery_overlay.State;

namespace league_mastery_overlay.Eval.Titles;

/// <summary>
/// How likely a title is to enter the candidate pool for any given lobby evaluation.
/// Rarity controls inclusion probability, not award probability — once a title is in
/// the pool it competes on equal footing with all others.
/// </summary>
internal enum TitleRarity
{
    /// <summary>Always included in the pool.</summary>
    Common,

    /// <summary>~60% chance of being included per lobby evaluation.</summary>
    Uncommon,

    /// <summary>~25% chance of being included per lobby evaluation.</summary>
    Rare,
}

/// <summary>
/// Declarative description of a single title.  Adding a new title requires only a new
/// instance of this record in <see cref="TitleCatalogue.All"/> — no logic changes needed.
/// </summary>
/// <param name="Title">The display string shown to the player.</param>
/// <param name="Rarity">Controls how often this title enters the candidate pool.</param>
/// <param name="Score">Extracts a comparable numeric score from a player's stats.</param>
/// <param name="MinScore">Minimum score a candidate must reach to be eligible.</param>
/// <param name="StatLine">
///     Formats the single most relevant stat for display beneath the title on the lobby card.
///     Should be a short human-readable string, e.g. "5 win streak" or "42,100 avg dmg".
/// </param>
internal sealed record TitleDefinition(
    string Title,
    TitleRarity Rarity,
    Func<PlayerStats, double> Score,
    double MinScore,
    Func<PlayerStats, string> StatLine
);
