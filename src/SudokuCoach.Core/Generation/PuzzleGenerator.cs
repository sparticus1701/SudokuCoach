using SudokuCoach.Core.Solving;
using SudokuCoach.Core.Techniques;

namespace SudokuCoach.Core.Generation;

public enum Difficulty
{
    /// <summary>Singles only.</summary>
    Easy,

    /// <summary>Needs a naked or hidden pair.</summary>
    Medium,

    /// <summary>Needs pointing, box-line reduction or a triple.</summary>
    Hard,
}

public sealed record GeneratedPuzzle(Board Puzzle, Board Solution, Difficulty Difficulty, Technique Hardest, int Seed);

/// <summary>
/// Makes puzzles: a random full grid, then clues removed in symmetric pairs while the
/// solution stays unique. Deterministic for a given seed.
/// </summary>
public sealed class PuzzleGenerator
{
    private readonly Random _random;

    public PuzzleGenerator(int seed) => _random = new Random(seed);

    /// <summary>
    /// Generates a puzzle whose human-style grade matches <paramref name="difficulty"/>.
    /// Hard puzzles are rarer, so they can take more attempts.
    /// </summary>
    public static GeneratedPuzzle Generate(Difficulty difficulty, int seed, int maxAttempts = 3000)
    {
        var generator = new PuzzleGenerator(seed);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var solution = generator.RandomSolution();
            var puzzle = generator.Carve(solution);
            var result = HumanSolver.Solve(puzzle);
            if (!result.Solved || result.Hardest is not { } hardest)
            {
                continue;
            }
            if (Classify(hardest) == difficulty)
            {
                return new GeneratedPuzzle(puzzle, solution, difficulty, hardest, seed);
            }
        }
        throw new InvalidOperationException($"No {difficulty} puzzle found in {maxAttempts} attempts from seed {seed}.");
    }

    public static Difficulty Classify(Technique hardest) => hardest switch
    {
        Technique.NakedSingle or Technique.HiddenSingle => Difficulty.Easy,
        Technique.NakedPair or Technique.HiddenPair => Difficulty.Medium,
        _ => Difficulty.Hard,
    };

    /// <summary>A random valid full grid: a pattern grid shuffled by validity-preserving symmetries.</summary>
    public Board RandomSolution()
    {
        var grid = new int[81];
        for (var r = 0; r < 9; r++)
        {
            for (var c = 0; c < 9; c++)
            {
                grid[r * 9 + c] = (r * 3 + r / 3 + c) % 9 + 1;
            }
        }

        var digits = Shuffled(Enumerable.Range(1, 9).ToArray());
        grid = grid.Select(v => digits[v - 1]).ToArray();
        grid = PermuteRows(grid, LinePermutation());
        grid = Transpose(grid);
        grid = PermuteRows(grid, LinePermutation());
        if (_random.Next(2) == 0)
        {
            grid = Transpose(grid);
        }
        return Board.FromValues(grid);
    }

    /// <summary>Removes clues in 180°-symmetric pairs, keeping each removal only if the solution stays unique.</summary>
    public Board Carve(Board solution)
    {
        var grid = solution.ToArray();
        foreach (var cell in Shuffled(Enumerable.Range(0, 41).ToArray()))
        {
            var mirror = 80 - cell;
            var (a, b) = (grid[cell], grid[mirror]);
            grid[cell] = 0;
            grid[mirror] = 0;
            if (BruteForceSolver.CountSolutions(Board.FromValues(grid)) != 1)
            {
                grid[cell] = a;
                grid[mirror] = b;
            }
        }
        return Board.FromValues(grid);
    }

    /// <summary>A row order that keeps rows within bands: shuffle the bands, then the rows inside each.</summary>
    private int[] LinePermutation()
    {
        var order = new List<int>(9);
        foreach (var band in Shuffled(new[] { 0, 1, 2 }))
        {
            order.AddRange(Shuffled(new[] { band * 3, band * 3 + 1, band * 3 + 2 }));
        }
        return order.ToArray();
    }

    private static int[] PermuteRows(int[] grid, int[] rowOrder)
    {
        var result = new int[81];
        for (var r = 0; r < 9; r++)
        {
            Array.Copy(grid, rowOrder[r] * 9, result, r * 9, 9);
        }
        return result;
    }

    private static int[] Transpose(int[] grid)
    {
        var result = new int[81];
        for (var r = 0; r < 9; r++)
        {
            for (var c = 0; c < 9; c++)
            {
                result[c * 9 + r] = grid[r * 9 + c];
            }
        }
        return result;
    }

    private T[] Shuffled<T>(T[] items)
    {
        var copy = (T[])items.Clone();
        for (var i = copy.Length - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }
}
