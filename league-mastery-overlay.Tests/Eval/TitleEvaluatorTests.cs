using league_mastery_overlay.Eval;
using league_mastery_overlay.Eval.Titles;
using league_mastery_overlay.State;

namespace league_mastery_overlay.Tests.Eval;

public class TitleEvaluatorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LobbyFriend Friend(string puuid) =>
        new(puuid, 0, puuid, "", false);

    /// <summary>
    /// Builds a PlayerStats with sensible zero defaults so tests only set what they care about.
    /// </summary>
    private static PlayerStats Stats(
        int    winStreak      = 0,
        int    lossStreak     = 0,
        double avgDamage      = 0,
        double avgHeal        = 0,
        double surrender      = 0,
        double avgDeaths      = 0,
        double avgKills       = 0,
        double avgAssists     = 0,
        double avgDamageTaken = 0,
        double avgSelfMit     = 0,
        double avgCCTime      = 0,
        double avgVision      = 0,
        double avgWards       = 0,
        double avgCS          = 0) =>
        new(winStreak, lossStreak, avgDamage, avgHeal, surrender,
            avgDeaths, avgKills, avgAssists, avgDamageTaken, avgSelfMit,
            avgCCTime, avgVision, avgWards, avgCS);

    private static List<(LobbyFriend, PlayerStats?)> Entries(
        params (string puuid, PlayerStats? stats)[] items) =>
        items.Select(x => (Friend(x.puuid), x.stats)).ToList();

    /// <summary>Always returns 0.0 — every Uncommon and Rare title enters the pool.</summary>
    private static AlwaysZeroRng AllInRng() => new();

    // ── Player archetypes ─────────────────────────────────────────────────────
    //
    // A realistic 5-player lobby where each player is dominant in exactly one
    // dimension and near-zero in all others.  Used by archetype-based theories.

    private static readonly IReadOnlyList<(string Puuid, PlayerStats Stats, string ExpectedTitle)> Archetypes =
    [
        // Win-streak monster → "Who Wants a Piece of the Champ"
        ("streaker", Stats(winStreak: 6), "Who Wants a Piece of the Champ"),

        // Damage carry → "Tons of Damage"
        ("carry",    Stats(avgDamage: 50_000), "Tons of Damage"),

        // Support healer → "All For You"
        ("healer",   Stats(avgHeal: 10_000), "All For You"),

        // Tanky frontliner → "Unkillable Demon King"
        ("tank",     Stats(avgSelfMit: 30_000), "Unkillable Demon King"),

        // Chronic feeder → "Grey Screen Enjoyer"
        ("feeder",   Stats(avgDeaths: 14), "Grey Screen Enjoyer"),
    ];

    // ── Archetype lobby theory ────────────────────────────────────────────────

    /// <summary>
    /// Each archetype receives the expected title when evaluated in a 5-player
    /// lobby where every other player is average in all stats.
    /// </summary>
    [Theory]
    [MemberData(nameof(ArchetypeData))]
    public void Archetype_InFullLobby_ReceivesExpectedTitle(
        string puuid, string expectedTitle)
    {
        var entries = Archetypes
            .Select(a => (Friend(a.Puuid), (PlayerStats?)a.Stats))
            .ToList();

        var result = TitleEvaluator.Evaluate(entries, AllInRng());

        Assert.Equal(expectedTitle, result[puuid]?.Title);
    }

    /// <summary>
    /// Each archetype's awarded title has a non-empty StatLine.
    /// </summary>
    [Theory]
    [MemberData(nameof(ArchetypeData))]
    public void Archetype_AwardedTitle_HasNonEmptyStatLine(
        string puuid, string _)
    {
        var entries = Archetypes
            .Select(a => (Friend(a.Puuid), (PlayerStats?)a.Stats))
            .ToList();

        var result = TitleEvaluator.Evaluate(entries, AllInRng());

        Assert.False(string.IsNullOrWhiteSpace(result[puuid]?.StatLine),
            $"StatLine for {puuid} should not be empty");
    }

    public static TheoryData<string, string> ArchetypeData()
    {
        var data = new TheoryData<string, string>();
        foreach (var (puuid, _, expected) in Archetypes)
            data.Add(puuid, expected);
        return data;
    }

    // ── Score / MinScore eligibility theory ───────────────────────────────────
    //
    // For each title in TitleCatalogue.All, assert that a hand-crafted PlayerStats
    // that clearly embodies that title's concept yields Score >= MinScore.
    // This validates the Score lambdas and MinScore constants directly,
    // independent of the assignment logic.
    
    private static readonly IReadOnlyList<(string Title, PlayerStats EligibleStats)> TitleEligibilityInputs =
    [
        ("Who Wants a Piece of the Champ", Stats(winStreak: 5)),
        ("On a Roll",                      Stats(winStreak: 3)),
        ("Stuck in Bronze",                Stats(lossStreak: 4)),
        ("It's Just a Bad Day",            Stats(lossStreak: 3)),
        ("Tons of Damage",                 Stats(avgDamage: 25_000)),
        ("Glass Cannon",                   Stats(avgDamage: 30_000, avgDamageTaken: 10_000)),   // ratio 3.0 ≥ 1.5
        ("Poke Master",                    Stats(avgDamage: 25_000, avgKills: 4)),              // 25000/4 = 6250 ≥ 5000
        ("Golden Mop",                     Stats(avgDamage: 15_000, avgKills: 12)),             // 12/(15000/1000)=0.8 ≥ 0.5
        ("Unkillable Demon King",          Stats(avgSelfMit: 20_000)),
        ("Human Shield",                   Stats(avgDamageTaken: 30_000)),
        ("Frontline Forever",              Stats(avgDamageTaken: 10_000, avgSelfMit: 10_000)),  // sum 20000 ≥ 15000
        ("All For You",                    Stats(avgHeal: 5_000)),
        ("Grey Screen Enjoyer",            Stats(avgDeaths: 13)),
        ("KDA Player",                     Stats(avgKills: 8, avgAssists: 8, avgDeaths: 2)),    // 16/2=8 ≥ 4
        ("Always a Bridesmaid",            Stats(avgAssists: 12)),
        ("Dive Bomber",                    Stats(avgDeaths: 12, avgKills: 11)),                  // deaths≥10, kills=11≥10
        ("Chain CC Enjoyer",               Stats(avgCCTime: 60)),
        ("Always Watching",                Stats(avgVision: 35)),
        ("Legally Blind",                  Stats(avgVision: 5)),                                 // 100-5=95 ≥ 85
        ("Ward Bot",                       Stats(avgWards: 10)),
        ("CS or Feed",                     Stats(avgCS: 200)),
        ("Retired Pro",                    Stats(avgCS: 250, avgDamage: 20_000)),               // 250/(20000/10000)=125≥5
        ("Rage Quitter",                   Stats(surrender: 0.7)),
        ("Never Surrender",                Stats(surrender: 0.05)),                              // 1-0.05=0.95 ≥ 0.9
    ];

    [Theory]
    [MemberData(nameof(EligibilityData))]
    public void Score_WithEligibleStats_MeetsMinScore(string title, PlayerStats eligibleStats)
    {
        var def = TitleCatalogue.All.Single(d => d.Title == title);

        var score = def.Score(eligibleStats);

        Assert.True(score >= def.MinScore,
            $"'{title}': score {score:F4} should be >= MinScore {def.MinScore:F4}");
    }

    public static TheoryData<string, PlayerStats> EligibilityData()
    {
        var data = new TheoryData<string, PlayerStats>();
        foreach (var (title, s) in TitleEligibilityInputs)
            data.Add(title, s);
        return data;
    }

    // ── Null-stats guard ──────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_AllWithoutStats_ReturnsAllNull()
    {
        var entries = Entries(("a", null), ("b", null));
        var result  = TitleEvaluator.Evaluate(entries, AllInRng());

        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
    }

    [Fact]
    public void Evaluate_ReturnsEntryForEveryPlayer()
    {
        var entries = Entries(
            ("a", Stats(avgDamage: 10_000)),
            ("b", null));

        var result = TitleEvaluator.Evaluate(entries, AllInRng());

        Assert.True(result.ContainsKey("a"));
        Assert.True(result.ContainsKey("b"));
        Assert.Null(result["b"]);
    }

    // ── Tie breaking ─────────────────────────────────────────────────────────

    [Fact]
    public void Tie_BothPlayersEqual_NeitherReceivesTitle()
    {
        var entries = Entries(
            ("a", Stats(avgDamage: 20_000)),
            ("b", Stats(avgDamage: 20_000)));

        var result = TitleEvaluator.Evaluate(entries, AllInRng());

        Assert.DoesNotContain(result.Values, v => v?.Title == "Tons of Damage");
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    [Fact]
    public void Invariant_NoTitleAwardedToMoreThanOnePlayer()
    {
        var entries = Archetypes
            .Select(a => (Friend(a.Puuid), (PlayerStats?)a.Stats))
            .ToList();

        var result = TitleEvaluator.Evaluate(entries, AllInRng());

        var awarded = result.Values.Where(v => v.HasValue).Select(v => v!.Value.Title).ToList();
        Assert.Equal(awarded.Count, awarded.Distinct().Count());
    }

    [Fact]
    public void Invariant_Deterministic_WithSameSeed()
    {
        var entries = Entries(
            ("a", Stats(winStreak: 3, avgDamage: 25_000)),
            ("b", Stats(winStreak: 0, avgDamage: 10_000)));

        var r1 = TitleEvaluator.Evaluate(entries, new Random(42));
        var r2 = TitleEvaluator.Evaluate(entries, new Random(42));

        Assert.Equal(r1["a"]?.Title, r2["a"]?.Title);
        Assert.Equal(r1["b"]?.Title, r2["b"]?.Title);
    }

    // ── Rarity pool ───────────────────────────────────────────────────────────

    [Fact]
    public void Rarity_CommonTitlesAlwaysIncluded_EvenWhenRngReturnsOne()
    {
        // NeverIncludeRng returns 1.0, excluding all Uncommon/Rare.
        // A Common title (win streak) should still be awarded.
        var entries = Entries(
            ("a", Stats(winStreak: 5)),
            ("b", Stats(winStreak: 0)));

        var result = TitleEvaluator.Evaluate(entries, new NeverIncludeRng());

        Assert.Equal("Who Wants a Piece of the Champ", result["a"]?.Title);
    }

    [Fact]
    public void Rarity_UncommonExcluded_CommonTitleFillsInstead()
    {
        // With NeverIncludeRng, "On a Roll" (Uncommon, same win-streak score fn)
        // is excluded, so "Who Wants a Piece of the Champ" (Common) wins instead.
        var entries = Entries(
            ("a", Stats(winStreak: 5)),
            ("b", Stats(winStreak: 1)));

        var result = TitleEvaluator.Evaluate(entries, new NeverIncludeRng());

        Assert.Equal("Who Wants a Piece of the Champ", result["a"]?.Title);
        // "On a Roll" must not appear
        Assert.DoesNotContain(result.Values, v => v?.Title == "On a Roll");
    }

    // ── Helper RNG stubs ──────────────────────────────────────────────────────

    /// <summary>Always returns 0.0 — every Uncommon/Rare title enters the pool.</summary>
    private sealed class AlwaysZeroRng : Random
    {
        public override double NextDouble() => 0.0;
    }

    /// <summary>Always returns 1.0 — no Uncommon/Rare title passes the inclusion check.</summary>
    private sealed class NeverIncludeRng : Random
    {
        public override double NextDouble() => 1.0;
    }
}
