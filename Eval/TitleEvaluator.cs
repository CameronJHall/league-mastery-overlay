using league_mastery_overlay.State;

namespace league_mastery_overlay.Eval;

/// <summary>
/// Pure static logic that maps a group of lobby friends + their stats to title strings.
///
/// Title rules (only one title per player; first matching rule wins):
///   "Who Wants a Piece of the Champ"  — highest win streak (≥3) in the group
///   "Tons of Damage"                  — highest average damage in the group
///   "All For You"                     — highest average healing in the group
///   "Grey Screen Enjoyer"             — highest surrender rate in the group (≥25%)
///
/// A title is only awarded when the player is strictly the best in the group for that
/// category (no ties). Players without stats always get null.
/// </summary>
internal static class TitleEvaluator
{
    public static Dictionary<string, string?> Evaluate(
        IReadOnlyList<(LobbyFriend Friend, PlayerStats? Stats)> entries)
    {
        var result = new Dictionary<string, string?>();
        foreach (var (f, _) in entries)
            result[f.Puuid] = null;

        var withStats = entries
            .Where(e => e.Stats != null)
            .ToList();

        if (withStats.Count == 0)
            return result;

        // Win streak: highest among group, must be ≥3, must be unique
        AssignTitle(result, withStats,
            score:    e => e.Stats!.WinStreak,
            minScore: 3,
            title:    "Who Wants a Piece of the Champ");

        // Damage: highest average, must be unique
        AssignTitle(result, withStats,
            score:    e => e.Stats!.AvgDamage,
            minScore: 1,
            title:    "Tons of Damage");

        // Healing: highest average, must be unique
        AssignTitle(result, withStats,
            score:    e => e.Stats!.AvgHealing,
            minScore: 1,
            title:    "All For You");

        // Surrender rate: highest, must be ≥25%, must be unique
        AssignTitle(result, withStats,
            score:    e => e.Stats!.SurrenderRate,
            minScore: 0.25,
            title:    "Grey Screen Enjoyer");

        return result;
    }

    private static void AssignTitle(
        Dictionary<string, string?> result,
        List<(LobbyFriend Friend, PlayerStats? Stats)> candidates,
        Func<(LobbyFriend Friend, PlayerStats? Stats), double> score,
        double minScore,
        string title)
    {
        // Only consider players who don't already have a title
        var eligible = candidates
            .Where(e => result[e.Friend.Puuid] == null)
            .ToList();

        if (eligible.Count == 0)
            return;

        double best = eligible.Max(score);
        if (best < minScore)
            return;

        var winners = eligible.Where(e => score(e) == best).ToList();
        if (winners.Count == 1)
            result[winners[0].Friend.Puuid] = title;
        // ties: no title awarded
    }
}
