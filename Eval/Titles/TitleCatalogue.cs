namespace league_mastery_overlay.Eval.Titles;

/// <summary>
/// Master list of all title definitions.
///
/// To add a new title: add a single entry to <see cref="All"/>.
/// No logic changes are required anywhere else.
///
/// Rarity guide:
///   Common   — thematic staples; always in the pool; cover the broadest player archetypes.
///   Uncommon — flavorful alternatives; appear ~60% of lobbies; add variety without dominating.
///   Rare     — very specific or comedic; appear ~25% of lobbies; feel like a surprise when shown.
/// </summary>
internal static class TitleCatalogue
{
    public static readonly IReadOnlyList<TitleDefinition> All =
    [
        // ── Win / loss streaks ────────────────────────────────────────────────

        new(
            Title:    "Who Wants a Piece of the Champ",
            Rarity:   TitleRarity.Common,
            Score:    s => s.WinStreak,
            MinScore: 3,
            StatLine: s => $"{s.WinStreak} win streak"
        ),
        new(
            Title:    "On a Roll",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.WinStreak,
            MinScore: 2,
            StatLine: s => $"{s.WinStreak} win streak"
        ),
        new(
            Title:    "Stuck in Bronze",
            Rarity:   TitleRarity.Common,
            Score:    s => s.LossStreak,
            MinScore: 3,
            StatLine: s => $"{s.LossStreak} loss streak"
        ),
        new(
            Title:    "It's Just a Bad Day",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.LossStreak,
            MinScore: 2,
            StatLine: s => $"{s.LossStreak} loss streak"
        ),

        // ── Damage dealt ─────────────────────────────────────────────────────

        new(
            Title:    "Tons of Damage",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgDamage,
            MinScore: 1,
            StatLine: s => $"{s.AvgDamage:N0} avg dmg"
        ),
        new(
            Title:    "Glass Cannon",
            Rarity:   TitleRarity.Uncommon,
            // High damage relative to damage taken — deals a lot, gets hit a little
            Score:    s => s.AvgDamageTaken > 0 ? s.AvgDamage / s.AvgDamageTaken : 0,
            MinScore: 1.5,
            StatLine: s => $"{s.AvgDamage:N0} dmg / {s.AvgDamageTaken:N0} taken"
        ),
        new(
            Title:    "Poke Master",
            Rarity:   TitleRarity.Rare,
            // High damage but low kills — chips without finishing
            Score:    s => s.AvgKills > 0 ? s.AvgDamage / s.AvgKills : s.AvgDamage,
            MinScore: 5000,
            StatLine: s => $"{s.AvgDamage:N0} dmg, {s.AvgKills:F1} kills"
        ),
        new(
            Title:    "Golden Mop",
            Rarity:   TitleRarity.Rare,
            // Low damage but high kills — finishes champs at low health (20 kills w/ <40,000 dmg)
            Score:    s => s.AvgDamage > 0 ? s.AvgKills / ( s.AvgDamage / 1000) : s.AvgKills,
            MinScore: 0.5,
            StatLine: s => $"{s.AvgKills:F1} kills, {s.AvgDamage:N0} dmg"
        ),

        // ── Damage taken / tankiness ─────────────────────────────────────────

        new(
            Title:    "Unkillable Demon King",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgSelfMitigated,
            MinScore: 5000,
            StatLine: s => $"{s.AvgSelfMitigated:N0} avg mitigated"
        ),
        new(
            Title:    "Human Shield",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.AvgDamageTaken,
            MinScore: 1,
            StatLine: s => $"{s.AvgDamageTaken:N0} avg taken"
        ),
        new(
            Title:    "Frontline Forever",
            Rarity:   TitleRarity.Rare,
            // Tanks AND mitigates a lot — the true frontliner
            Score:    s => s.AvgDamageTaken + s.AvgSelfMitigated,
            MinScore: 15000,
            StatLine: s => $"{s.AvgDamageTaken:N0} taken, {s.AvgSelfMitigated:N0} mitigated"
        ),

        // ── Healing ───────────────────────────────────────────────────────────

        new(
            Title:    "All For You",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgHealing,
            MinScore: 1,
            StatLine: s => $"{s.AvgHealing:N0} avg healing"
        ),

        // ── KDA ──────────────────────────────────────────────────────────────

        new(
            Title:    "Grey Screen Enjoyer",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgDeaths,
            MinScore: 12,
            StatLine: s => $"{s.AvgDeaths:F1} avg deaths"
        ),
        new(
            Title:    "KDA Player",
            Rarity:   TitleRarity.Uncommon,
            // High kill participation, very low deaths
            Score:    s => s.AvgDeaths > 0 ? (s.AvgKills + s.AvgAssists) / s.AvgDeaths : s.AvgKills + s.AvgAssists,
            MinScore: 4,
            StatLine: s => $"{s.AvgKills:F1} / {s.AvgDeaths:F1} / {s.AvgAssists:F1}"
        ),
        new(
            Title:    "Always a Bridesmaid",
            Rarity:   TitleRarity.Uncommon,
            // Highest assists — maximum support/leech energy
            Score:    s => s.AvgAssists,
            MinScore: 5,
            StatLine: s => $"{s.AvgAssists:F1} avg assists"
        ),
        new(
            Title:    "Dive Bomber",
            Rarity:   TitleRarity.Rare,
            // High deaths AND high kills — dies for the team but still fragging
            Score:    s => s.AvgDeaths >= 10 ? s.AvgKills : 0,
            MinScore: 10,
            StatLine: s => $"{s.AvgKills:F1} kills, {s.AvgDeaths:F1} deaths"
        ),

        // ── CC ────────────────────────────────────────────────────────────────

        new(
            Title:    "Chain CC Enjoyer",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgCCTime,
            MinScore: 10,
            StatLine: s => $"{s.AvgCCTime:F0}s avg CC time"
        ),

        // ── Vision ────────────────────────────────────────────────────────────

        new(
            Title:    "Always Watching",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.AvgVisionScore,
            MinScore: 20,
            StatLine: s => $"{s.AvgVisionScore:F0} avg vision score"
        ),
        new(
            Title:    "Legally Blind",
            Rarity:   TitleRarity.Rare,
            // Lowest vision score — genuinely does not ward; invert so highest score = worst warding
            Score:    s => s.AvgVisionScore >= 0 ? 100 - s.AvgVisionScore : 0,
            MinScore: 85,   // i.e. avg vision score ≤ 15
            StatLine: s => $"{s.AvgVisionScore:F0} avg vision score"
        ),
        new(
            Title:    "Ward Bot",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.AvgWardsPlaced,
            MinScore: 5,
            StatLine: s => $"{s.AvgWardsPlaced:F1} avg wards"
        ),

        // ── CS / economy ──────────────────────────────────────────────────────

        new(
            Title:    "CS or Feed",
            Rarity:   TitleRarity.Common,
            Score:    s => s.AvgCS,
            MinScore: 100,
            StatLine: s => $"{s.AvgCS:F0} avg CS"
        ),
        new(
            Title:    "Retired Pro",
            Rarity:   TitleRarity.Rare,
            // Very high CS but mediocre damage — farms perfectly, doesn't convert
            Score:    s => s.AvgDamage > 0 ? s.AvgCS / (s.AvgDamage / 10000) : 0,
            MinScore: 5,
            StatLine: s => $"{s.AvgCS:F0} CS, {s.AvgDamage:N0} dmg"
        ),

        // ── Surrender ─────────────────────────────────────────────────────────

        new(
            Title:    "Rage Quitter",
            Rarity:   TitleRarity.Uncommon,
            Score:    s => s.SurrenderRate,
            MinScore: 0.5,
            StatLine: s => $"{s.SurrenderRate:P0} surrender rate"
        ),
        new(
            Title:    "Never Surrender",
            Rarity:   TitleRarity.Rare,
            // Lowest surrender rate — never gives up; invert so highest score = least surrendering
            Score:    s => 1.0 - s.SurrenderRate,
            MinScore: 0.9,  // i.e. surrender rate ≤ 10%
            StatLine: s => $"{s.SurrenderRate:P0} surrender rate"
        ),
    ];
}
