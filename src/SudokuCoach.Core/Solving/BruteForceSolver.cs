using System.Numerics;

namespace SudokuCoach.Core.Solving;

/// <summary>
/// Backtracking solver for checking uniqueness and finding the answer. Not used for teaching.
/// </summary>
public static class BruteForceSolver
{
    /// <summary>Counts solutions, stopping once <paramref name="limit"/> is reached.</summary>
    public static int CountSolutions(Board board, int limit = 2)
    {
        var grid = board.ToArray();
        if (!board.IsValid)
        {
            return 0;
        }
        var count = 0;
        Search(grid, limit, ref count, null);
        return count;
    }

    /// <summary>The solution if there is exactly one; otherwise null.</summary>
    public static Board? SolveUnique(Board board)
    {
        var grid = board.ToArray();
        if (!board.IsValid)
        {
            return null;
        }
        var count = 0;
        int[]? first = null;
        Search(grid, 2, ref count, solved => first ??= (int[])solved.Clone());
        if (count != 1 || first is null)
        {
            return null;
        }
        var givens = Enumerable.Range(0, 81).Select(board.IsGiven).ToArray();
        return Board.FromValues(first, givens);
    }

    private static void Search(int[] grid, int limit, ref int count, Action<int[]>? onSolution)
    {
        var bestCell = -1;
        var bestMask = 0;
        var bestCount = 10;
        for (var cell = 0; cell < 81; cell++)
        {
            if (grid[cell] != 0)
            {
                continue;
            }
            var used = 0;
            foreach (var p in Geometry.Peers(cell))
            {
                used |= 1 << grid[p];
            }
            var mask = CandidateSet.AllBits & ~used;
            var n = BitOperations.PopCount((uint)mask);
            if (n == 0)
            {
                return;
            }
            if (n < bestCount)
            {
                bestCell = cell;
                bestMask = mask;
                bestCount = n;
                if (n == 1)
                {
                    break;
                }
            }
        }

        if (bestCell < 0)
        {
            count++;
            onSolution?.Invoke(grid);
            return;
        }

        for (var d = 1; d <= 9 && count < limit; d++)
        {
            if ((bestMask & (1 << d)) == 0)
            {
                continue;
            }
            grid[bestCell] = d;
            Search(grid, limit, ref count, onSolution);
            grid[bestCell] = 0;
        }
    }
}
