namespace SudokuCoach.Core.Techniques;

/// <summary>Runs technique finders over a position.</summary>
public static class Analyzer
{
    private static readonly IReadOnlyDictionary<Technique, ITechniqueFinder> FinderMap =
        new ITechniqueFinder[]
        {
            new NakedSingleFinder(),
            new HiddenSingleFinder(),
            new NakedSubsetFinder(2),
            new HiddenSubsetFinder(2),
            new PointingFinder(),
            new BoxLineReductionFinder(),
            new NakedSubsetFinder(3),
            new HiddenSubsetFinder(3),
        }.ToDictionary(f => f.Technique);

    /// <summary>The finder for each technique, easiest first.</summary>
    public static IReadOnlyList<ITechniqueFinder> Finders { get; } =
        TechniqueInfo.ByDifficulty.Select(t => FinderMap[t]).ToArray();

    public static ITechniqueFinder FinderFor(Technique technique) => FinderMap[technique];

    /// <summary>Every productive move available, easiest technique first.</summary>
    public static IReadOnlyList<Move> FindAll(
        Board board,
        CandidateSet[] candidates,
        IEnumerable<Technique>? techniques = null)
    {
        var allowed = techniques?.ToHashSet();
        return Finders
            .Where(f => allowed is null || allowed.Contains(f.Technique))
            .SelectMany(f => f.Find(board, candidates))
            .ToArray();
    }

    /// <summary>The first move of the easiest technique that applies, or null if none do.</summary>
    public static Move? FindEasiest(
        Board board,
        CandidateSet[] candidates,
        IReadOnlySet<Technique>? allowed = null)
    {
        foreach (var finder in Finders)
        {
            if (allowed is not null && !allowed.Contains(finder.Technique))
            {
                continue;
            }
            var move = finder.Find(board, candidates).FirstOrDefault();
            if (move is not null)
            {
                return move;
            }
        }
        return null;
    }
}
