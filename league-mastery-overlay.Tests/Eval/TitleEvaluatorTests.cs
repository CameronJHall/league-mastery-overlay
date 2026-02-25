using league_mastery_overlay.Eval;
using league_mastery_overlay.State;

namespace league_mastery_overlay.Tests.Eval;

public class TitleEvaluatorTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LobbyFriend Friend(string puuid) =>
        new(puuid, 0, puuid, "", false);

    private static PlayerStats Stats(
        int winStreak    = 0,
        int lossStreak   = 0,
        double avgDamage = 0,
        double avgHeal   = 0,
        double surrender = 0) =>
        new(winStreak, lossStreak, avgDamage, avgHeal, surrender);

    private static List<(LobbyFriend, PlayerStats?)> Entries(
        params (string puuid, PlayerStats? stats)[] items) =>
        items.Select(x => (Friend(x.puuid), x.stats)).ToList();

    // ── No stats ──────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_AllWithoutStats_ReturnsAllNull()
    {
        var entries = Entries(("a", null), ("b", null));
        var result  = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    // ── Who Wants a Piece of the Champ ───────────────────────────────────────

    [Fact]
    public void WinStreak_AboveThreshold_AwardsTitle()
    {
        var entries = Entries(
            ("a", Stats(winStreak: 5)),
            ("b", Stats(winStreak: 1)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Who Wants a Piece of the Champ", result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void WinStreak_BelowThreshold_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(winStreak: 2)),
            ("b", Stats(winStreak: 1)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void WinStreak_Tie_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(winStreak: 4)),
            ("b", Stats(winStreak: 4)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    // ── Tons of Damage ────────────────────────────────────────────────────────

    [Fact]
    public void AvgDamage_Highest_AwardsTitle()
    {
        var entries = Entries(
            ("a", Stats(avgDamage: 30000)),
            ("b", Stats(avgDamage: 15000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Tons of Damage", result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void AvgDamage_Tie_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(avgDamage: 20000)),
            ("b", Stats(avgDamage: 20000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    // ── All For You ───────────────────────────────────────────────────────────

    [Fact]
    public void AvgHealing_Highest_AwardsTitle()
    {
        var entries = Entries(
            ("a", Stats(avgHeal: 5000)),
            ("b", Stats(avgHeal: 1000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("All For You", result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void AvgHealing_Tie_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(avgHeal: 3000)),
            ("b", Stats(avgHeal: 3000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    // ── Grey Screen Enjoyer ───────────────────────────────────────────────────

    [Fact]
    public void SurrenderRate_AboveThreshold_AwardsTitle()
    {
        var entries = Entries(
            ("a", Stats(surrender: 0.5)),
            ("b", Stats(surrender: 0.1)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Grey Screen Enjoyer", result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void SurrenderRate_BelowThreshold_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(surrender: 0.2)),
            ("b", Stats(surrender: 0.1)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void SurrenderRate_Tie_NoTitle()
    {
        var entries = Entries(
            ("a", Stats(surrender: 0.5)),
            ("b", Stats(surrender: 0.5)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    // ── Priority ordering ─────────────────────────────────────────────────────

    [Fact]
    public void Priority_WinStreakBeatsHighDamage()
    {
        // "a" qualifies for both WinStreak and Damage — WinStreak should win
        var entries = Entries(
            ("a", Stats(winStreak: 5, avgDamage: 40000)),
            ("b", Stats(winStreak: 0, avgDamage: 10000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Who Wants a Piece of the Champ", result["a"]);
        // "b" gets damage title since "a" is taken
        Assert.Equal("Tons of Damage", result["b"]);
    }

    [Fact]
    public void Priority_EachPlayerGetsAtMostOneTitle()
    {
        // "a" wins on all four categories — should only get the highest priority one
        var entries = Entries(
            ("a", Stats(winStreak: 5, avgDamage: 50000, avgHeal: 10000, surrender: 0.9)),
            ("b", Stats(winStreak: 0, avgDamage:  5000, avgHeal:  1000, surrender: 0.1)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Who Wants a Piece of the Champ", result["a"]);
        // "b" should fall through to damage (next unawarded category)
        Assert.Equal("Tons of Damage", result["b"]);
    }

    [Fact]
    public void Priority_FallsThrough_WhenHigherTierTied()
    {
        // Both tied on WinStreak — damage title should still be awarded to the higher damage player
        var entries = Entries(
            ("a", Stats(winStreak: 4, avgDamage: 30000)),
            ("b", Stats(winStreak: 4, avgDamage: 10000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Tons of Damage", result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void Priority_FallsThrough_WhenHigherTierBelowMinimum()
    {
        // No one has a win streak ≥3, but damage title should still be awarded
        var entries = Entries(
            ("a", Stats(winStreak: 1, avgDamage: 30000)),
            ("b", Stats(winStreak: 2, avgDamage: 10000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Tons of Damage", result["a"]);
    }

    // ── Single player ─────────────────────────────────────────────────────────

    [Fact]
    public void SinglePlayer_WithStats_GetsDamageTitle()
    {
        var entries = Entries(("a", Stats(avgDamage: 20000)));
        var result  = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Tons of Damage", result["a"]);
    }

    [Fact]
    public void SinglePlayer_WithoutStats_GetsNull()
    {
        var entries = Entries(("a", null));
        var result  = TitleEvaluator.Evaluate(entries);

        Assert.Null(result["a"]);
    }

    // ── Mixed stats/no-stats ──────────────────────────────────────────────────

    [Fact]
    public void MixedEntries_OnlyPlayersWithStatsEligible()
    {
        var entries = Entries(
            ("a", Stats(avgDamage: 20000)),
            ("b", null),
            ("c", Stats(avgDamage: 10000)));

        var result = TitleEvaluator.Evaluate(entries);

        Assert.Equal("Tons of Damage", result["a"]);
        Assert.Null(result["b"]);
    }
}
