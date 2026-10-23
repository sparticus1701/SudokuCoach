namespace SudokuCoach.Core.Techniques;

/// <summary>Moves that have exactly the same effect on the grid, possibly found by different techniques.</summary>
public sealed class MoveGroup
{
    internal MoveGroup(string effectKey, IReadOnlyList<Move> moves)
    {
        EffectKey = effectKey;
        Moves = moves;
    }

    public string EffectKey { get; }

    public IReadOnlyList<Move> Moves { get; }

    public IEnumerable<Technique> Techniques => Moves.Select(m => m.Technique).Distinct();

    /// <summary>The easiest way to see this effect.</summary>
    public Move Easiest => Moves.OrderBy(m => m.Technique.Rank()).First();
}

/// <summary>
/// What the coach panel shows: which techniques apply right now, grouped so that the same
/// deduction seen two ways is not counted as two, and which moves need a technique the player
/// hasn't learned and can't already get from what they know.
/// </summary>
public sealed class CoachReport
{
    private readonly Dictionary<Technique, int> _counts;

    private CoachReport(
        IReadOnlyList<Move> moves,
        IReadOnlyList<MoveGroup> groups,
        IReadOnlyList<Move> learnedMoves,
        IReadOnlyList<Move> newToYou)
    {
        Moves = moves;
        Groups = groups;
        LearnedMoves = learnedMoves;
        NewToYou = newToYou;
        _counts = moves.GroupBy(m => m.Technique).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>Every productive move on the board.</summary>
    public IReadOnlyList<Move> Moves { get; }

    /// <summary>Moves grouped by identical effect.</summary>
    public IReadOnlyList<MoveGroup> Groups { get; }

    /// <summary>Moves found with techniques the player has learned.</summary>
    public IReadOnlyList<Move> LearnedMoves { get; }

    /// <summary>
    /// Moves that need an unlearned technique and whose effect the learned moves don't already
    /// achieve. This is the honest number behind "moves you haven't learned yet".
    /// </summary>
    public IReadOnlyList<Move> NewToYou { get; }

    /// <summary>Distinct effects among <see cref="NewToYou"/>.</summary>
    public int NewToYouEffectCount => NewToYou.Select(m => m.EffectKey).Distinct().Count();

    /// <summary>Distinct effects reachable with learned techniques.</summary>
    public int LearnedEffectCount => LearnedMoves.Select(m => m.EffectKey).Distinct().Count();

    /// <summary>The easiest unlearned technique that would make new progress, if any.</summary>
    public Technique? NextTechniqueToLearn =>
        NewToYou.Count == 0 ? null : NewToYou.Select(m => m.Technique).OrderBy(t => t.Rank()).First();

    /// <summary>How many moves the technique finds (not deduplicated).</summary>
    public int CountOf(Technique technique) => _counts.GetValueOrDefault(technique);

    public static CoachReport Build(IReadOnlyList<Move> moves, IReadOnlySet<Technique> learned)
    {
        var groups = moves
            .GroupBy(m => m.EffectKey)
            .Select(g => new MoveGroup(g.Key, g.ToArray()))
            .ToArray();

        var learnedMoves = moves.Where(m => learned.Contains(m.Technique)).ToArray();
        var learnedPlacements = learnedMoves.Where(m => m.Placement is not null).Select(m => m.Placement!.Value).ToHashSet();
        var learnedEliminations = learnedMoves.SelectMany(m => m.Eliminations).ToHashSet();

        bool Covered(Elimination e) =>
            learnedEliminations.Contains(e) ||
            learnedPlacements.Any(p => p.Cell == e.Cell || (p.Digit == e.Digit && Geometry.AreSeen(p.Cell, e.Cell)));

        var newToYou = moves
            .Where(m => !learned.Contains(m.Technique))
            .Where(m => m.Placement is { } p
                ? !learnedPlacements.Contains(p)
                : !m.Eliminations.All(Covered))
            .ToArray();

        return new CoachReport(moves, groups, learnedMoves, newToYou);
    }

    /// <summary>Analyzes a position and builds the report.</summary>
    public static CoachReport For(Board board, CandidateSet[] candidates, IReadOnlySet<Technique> learned) =>
        Build(Analyzer.FindAll(board, candidates), learned);
}
