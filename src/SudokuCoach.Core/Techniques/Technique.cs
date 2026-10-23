namespace SudokuCoach.Core.Techniques;

/// <summary>Solving techniques the engine can find. Order of declaration is not difficulty; see <see cref="TechniqueInfo"/>.</summary>
public enum Technique
{
    NakedSingle,
    HiddenSingle,
    NakedPair,
    HiddenPair,
    NakedTriple,
    HiddenTriple,

    /// <summary>A digit's spots in a box all lie in one row or column (pointing pair or triple).</summary>
    Pointing,

    /// <summary>A digit's spots in a row or column all lie in one box (also called claiming).</summary>
    BoxLineReduction,
}

/// <summary>Display names and difficulty ranks. Lower rank = easier; the solver tries easier techniques first.</summary>
public static class TechniqueInfo
{
    private static readonly Dictionary<Technique, (string Name, string Plural, int Rank)> Info = new()
    {
        [Technique.NakedSingle] = ("Naked single", "Naked singles", 10),
        [Technique.HiddenSingle] = ("Hidden single", "Hidden singles", 12),
        [Technique.NakedPair] = ("Naked pair", "Naked pairs", 20),
        [Technique.HiddenPair] = ("Hidden pair", "Hidden pairs", 22),
        [Technique.Pointing] = ("Pointing pair", "Pointing pairs", 30),
        [Technique.BoxLineReduction] = ("Box-line reduction", "Box-line reductions", 32),
        [Technique.NakedTriple] = ("Naked triple", "Naked triples", 40),
        [Technique.HiddenTriple] = ("Hidden triple", "Hidden triples", 42),
    };

    public static string DisplayName(this Technique technique) => Info[technique].Name;

    public static string PluralName(this Technique technique) => Info[technique].Plural;

    public static int Rank(this Technique technique) => Info[technique].Rank;

    /// <summary>All techniques, easiest first.</summary>
    public static IReadOnlyList<Technique> ByDifficulty { get; } =
        Info.OrderBy(kv => kv.Value.Rank).Select(kv => kv.Key).ToArray();

    /// <summary>The techniques a new player starts with.</summary>
    public static IReadOnlySet<Technique> Beginner { get; } =
        new HashSet<Technique> { Technique.NakedSingle, Technique.HiddenSingle };
}
