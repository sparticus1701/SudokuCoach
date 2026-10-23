using SudokuCoach.Core.Techniques;

namespace SudokuCoach.Core.Solving;

public sealed class SolveResult
{
    internal SolveResult(Board final, CandidateSet[] finalCandidates, IReadOnlyList<Move> steps, bool contradiction)
    {
        Final = final;
        FinalCandidates = finalCandidates;
        Steps = steps;
        Contradiction = contradiction;
    }

    /// <summary>True when the board was filled with no conflicts.</summary>
    public bool Solved => Final.IsSolved;

    /// <summary>True when an empty cell ran out of candidates (the input was inconsistent).</summary>
    public bool Contradiction { get; }

    /// <summary>The board after the last step; a stuck position when <see cref="Solved"/> is false.</summary>
    public Board Final { get; }

    public CandidateSet[] FinalCandidates { get; }

    /// <summary>Moves applied, in order.</summary>
    public IReadOnlyList<Move> Steps { get; }

    public IReadOnlyCollection<Technique> TechniquesUsed => Steps.Select(s => s.Technique).Distinct().ToArray();

    /// <summary>The hardest technique the solve needed, or null if it took no steps.</summary>
    public Technique? Hardest => Steps.Count == 0 ? null : Steps.Select(s => s.Technique).MaxBy(t => t.Rank());
}

/// <summary>
/// Solves the way a person would: always applies the easiest technique available.
/// The hardest technique used is the puzzle's grade.
/// </summary>
public static class HumanSolver
{
    public static SolveResult Solve(Board puzzle, IReadOnlySet<Technique>? allowed = null, int maxSteps = 2000) =>
        Solve(puzzle, puzzle.ComputeCandidates(), allowed, maxSteps);

    public static SolveResult Solve(
        Board puzzle,
        CandidateSet[] startCandidates,
        IReadOnlySet<Technique>? allowed = null,
        int maxSteps = 2000)
    {
        var board = puzzle.Clone();
        var candidates = (CandidateSet[])startCandidates.Clone();
        var steps = new List<Move>();
        var contradiction = false;

        while (!board.IsComplete && steps.Count < maxSteps)
        {
            if (Enumerable.Range(0, 81).Any(c => board.IsEmpty(c) && candidates[c].IsEmpty))
            {
                contradiction = true;
                break;
            }
            var move = Analyzer.FindEasiest(board, candidates, allowed);
            if (move is null)
            {
                break;
            }
            move.ApplyTo(board, candidates);
            steps.Add(move);
        }

        return new SolveResult(board, candidates, steps, contradiction);
    }
}
