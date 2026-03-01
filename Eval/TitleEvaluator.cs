using league_mastery_overlay.Eval.Titles;
using league_mastery_overlay.State;

namespace league_mastery_overlay.Eval;

/// <summary>
/// Maps a group of lobby friends + their stats to title strings.
///
/// Each call to <see cref="Evaluate"/> builds a fresh candidate pool by sampling
/// <see cref="TitleCatalogue.All"/> according to each title's rarity, then runs a
/// ranked-bidding pass to assign one title per player.
///
/// Rarity inclusion probabilities (sampled once per lobby evaluation):
///   Common   — always included
///   Uncommon — 60% chance
///   Rare     — 25% chance
///
/// Assignment — ranked bidding:
///   For every title in the pool, the strongest eligible candidate is identified.
///   All (title, candidate, score) bids are then sorted by score descending so that
///   the highest-scoring player locks in their best title first.  This prevents a
///   weaker player from "stealing" a title from a stronger one purely due to list order.
///
/// Invariants preserved from the original evaluator:
///   - At most one title per player.
///   - A title is only awarded when one player is strictly the best (no ties).
///   - Players without stats always receive null.
///   - Passing a seeded <see cref="Random"/> produces deterministic output (for tests).
/// </summary>
internal static class TitleEvaluator
{
    private const double UncommonChance = 0.60;
    private const double RareChance     = 0.25;

    /// <summary>
    /// Result of a single title assignment — the display title and the stat line that
    /// explains why this player earned it.
    /// </summary>
    public readonly record struct TitleResult(string Title, string StatLine);

    /// <summary>
    /// Evaluates titles for all entries in the lobby.
    /// Returns a dictionary keyed by puuid; value is null when no title was awarded.
    /// </summary>
    /// <param name="entries">All lobby friends, each paired with their stats (or null).</param>
    /// <param name="rng">
    ///     Optional random instance.  Pass a seeded <see cref="Random"/> for deterministic
    ///     results in tests; leave null to use <see cref="Random.Shared"/>.
    /// </param>
    public static Dictionary<string, TitleResult?> Evaluate(
        IReadOnlyList<(LobbyFriend Friend, PlayerStats? Stats)> entries,
        Random? rng = null)
    {
        rng ??= Random.Shared;

        var result = new Dictionary<string, TitleResult?>();
        foreach (var (f, _) in entries)
            result[f.Puuid] = null;

        var withStats = entries
            .Where(e => e.Stats != null)
            .ToList();

        if (withStats.Count == 0)
            return result;

        // 1. Sample the title pool for this lobby
        var pool = BuildPool(rng);

        if (pool.Count == 0)
            return result;

        // 2. For each title in the pool, find the best eligible candidate.
        //    Collect all valid bids as (puuid, def, score) tuples.
        var bids = new List<(string Puuid, TitleDefinition Def, double Score)>();

        foreach (var def in pool)
        {
            var eligible = withStats
                .Where(e => result[e.Friend.Puuid] == null)
                .ToList();

            if (eligible.Count == 0)
                break;

            double best = eligible.Max(e => def.Score(e.Stats!));
            if (best < def.MinScore)
                continue;

            var winners = eligible.Where(e => def.Score(e.Stats!) == best).ToList();
            if (winners.Count == 1)
                bids.Add((winners[0].Friend.Puuid, def, best));
            // ties: no bid recorded for this title
        }

        // 3. Ranked bidding: sort by score descending so the strongest player picks first.
        //    Within a group, a player can only win one title (the first bid that lands).
        foreach (var (puuid, def, _) in bids.OrderByDescending(b => b.Score))
        {
            if (result[puuid] == null)
            {
                var stats = withStats.First(e => e.Friend.Puuid == puuid).Stats!;
                result[puuid] = new TitleResult(def.Title, def.StatLine(stats));
            }
        }

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<TitleDefinition> BuildPool(Random rng)
    {
        var pool = new List<TitleDefinition>(TitleCatalogue.All.Count);

        foreach (var def in TitleCatalogue.All)
        {
            bool include = def.Rarity switch
            {
                TitleRarity.Common   => true,
                TitleRarity.Uncommon => rng.NextDouble() < UncommonChance,
                TitleRarity.Rare     => rng.NextDouble() < RareChance,
                _                    => false,
            };

            if (include)
                pool.Add(def);
        }

        return pool;
    }
}
