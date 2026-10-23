namespace SudokuCoach.Core.Techniques;

/// <summary>Finds every productive instance of one technique on a board and candidate grid.</summary>
public interface ITechniqueFinder
{
    Technique Technique { get; }

    IEnumerable<Move> Find(Board board, CandidateSet[] candidates);
}

internal static class FinderHelpers
{
    public static bool IsPlacedIn(Board board, House house, int digit)
    {
        foreach (var c in house.Cells)
        {
            if (board[c] == digit)
            {
                return true;
            }
        }
        return false;
    }

    public static List<int> Spots(Board board, CandidateSet[] candidates, House house, int digit)
    {
        var spots = new List<int>(9);
        foreach (var c in house.Cells)
        {
            if (board.IsEmpty(c) && candidates[c].Contains(digit))
            {
                spots.Add(c);
            }
        }
        return spots;
    }

    /// <summary>All k-element combinations of <paramref name="items"/>, in order.</summary>
    public static IEnumerable<T[]> Combinations<T>(IReadOnlyList<T> items, int k)
    {
        if (k <= 0 || k > items.Count)
        {
            yield break;
        }
        var idx = Enumerable.Range(0, k).ToArray();
        while (true)
        {
            yield return idx.Select(i => items[i]).ToArray();
            var pos = k - 1;
            while (pos >= 0 && idx[pos] == items.Count - k + pos)
            {
                pos--;
            }
            if (pos < 0)
            {
                yield break;
            }
            idx[pos]++;
            for (var j = pos + 1; j < k; j++)
            {
                idx[j] = idx[j - 1] + 1;
            }
        }
    }
}

/// <summary>An empty cell with exactly one candidate.</summary>
public sealed class NakedSingleFinder : ITechniqueFinder
{
    public Technique Technique => Technique.NakedSingle;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        for (var cell = 0; cell < 81; cell++)
        {
            if (board.IsEmpty(cell) && candidates[cell].Count == 1)
            {
                var d = candidates[cell].Single;
                yield return new Move(Technique, null, [cell], candidates[cell], new Placement(cell, d), []);
            }
        }
    }
}

/// <summary>A digit with only one possible cell in a house. A cell found through several houses is reported once.</summary>
public sealed class HiddenSingleFinder : ITechniqueFinder
{
    public Technique Technique => Technique.HiddenSingle;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        var seen = new HashSet<Placement>();
        foreach (var house in Geometry.Houses)
        {
            for (var d = 1; d <= 9; d++)
            {
                if (FinderHelpers.IsPlacedIn(board, house, d))
                {
                    continue;
                }
                var spots = FinderHelpers.Spots(board, candidates, house, d);
                if (spots.Count == 1)
                {
                    var placement = new Placement(spots[0], d);
                    if (seen.Add(placement))
                    {
                        yield return new Move(Technique, house, spots, CandidateSet.Of(d), placement, []);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Naked subsets: N cells in a house whose candidates together hold exactly N digits.
/// Those digits can be removed from the house's other cells.
/// </summary>
public sealed class NakedSubsetFinder : ITechniqueFinder
{
    private readonly int _size;

    public NakedSubsetFinder(int size)
    {
        _size = size switch
        {
            2 or 3 => size,
            _ => throw new ArgumentOutOfRangeException(nameof(size), "Supported sizes are 2 and 3."),
        };
    }

    public Technique Technique => _size == 2 ? Technique.NakedPair : Technique.NakedTriple;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        foreach (var house in Geometry.Houses)
        {
            var pool = house.Cells
                .Where(c => board.IsEmpty(c) && candidates[c].Count >= 2 && candidates[c].Count <= _size)
                .ToArray();
            foreach (var combo in FinderHelpers.Combinations(pool, _size))
            {
                var union = CandidateSet.Empty;
                foreach (var c in combo)
                {
                    union = union.Union(candidates[c]);
                }
                if (union.Count != _size)
                {
                    continue;
                }
                var eliminations = new List<Elimination>();
                foreach (var c in house.Cells)
                {
                    if (!board.IsEmpty(c) || Array.IndexOf(combo, c) >= 0)
                    {
                        continue;
                    }
                    foreach (var d in candidates[c].Intersect(union).Digits)
                    {
                        eliminations.Add(new Elimination(c, d));
                    }
                }
                if (eliminations.Count > 0)
                {
                    yield return new Move(Technique, house, combo, union, null, eliminations);
                }
            }
        }
    }
}

/// <summary>
/// Hidden subsets: N digits whose only spots in a house are the same N cells.
/// Every other candidate can be removed from those cells.
/// </summary>
public sealed class HiddenSubsetFinder : ITechniqueFinder
{
    private readonly int _size;

    public HiddenSubsetFinder(int size)
    {
        _size = size switch
        {
            2 or 3 => size,
            _ => throw new ArgumentOutOfRangeException(nameof(size), "Supported sizes are 2 and 3."),
        };
    }

    public Technique Technique => _size == 2 ? Technique.HiddenPair : Technique.HiddenTriple;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        foreach (var house in Geometry.Houses)
        {
            var spotsByDigit = new Dictionary<int, List<int>>();
            for (var d = 1; d <= 9; d++)
            {
                if (FinderHelpers.IsPlacedIn(board, house, d))
                {
                    continue;
                }
                var spots = FinderHelpers.Spots(board, candidates, house, d);
                if (spots.Count >= 2 && spots.Count <= _size)
                {
                    spotsByDigit[d] = spots;
                }
            }
            var digits = spotsByDigit.Keys.Order().ToArray();
            foreach (var combo in FinderHelpers.Combinations(digits, _size))
            {
                var cells = combo.SelectMany(d => spotsByDigit[d]).Distinct().ToArray();
                if (cells.Length != _size)
                {
                    continue;
                }
                var digitSet = CandidateSet.Of(combo);
                var eliminations = new List<Elimination>();
                foreach (var c in cells)
                {
                    foreach (var d in candidates[c].Except(digitSet).Digits)
                    {
                        eliminations.Add(new Elimination(c, d));
                    }
                }
                if (eliminations.Count > 0)
                {
                    yield return new Move(Technique, house, cells, digitSet, null, eliminations);
                }
            }
        }
    }
}

/// <summary>
/// Pointing: a digit's spots within a box all lie in one row or column,
/// so the digit can be removed from the rest of that line.
/// </summary>
public sealed class PointingFinder : ITechniqueFinder
{
    public Technique Technique => Technique.Pointing;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        foreach (var box in Geometry.Boxes)
        {
            for (var d = 1; d <= 9; d++)
            {
                if (FinderHelpers.IsPlacedIn(board, box, d))
                {
                    continue;
                }
                var spots = FinderHelpers.Spots(board, candidates, box, d);
                if (spots.Count < 2)
                {
                    continue;
                }
                House? line = null;
                if (spots.Select(Geometry.RowOf).Distinct().Count() == 1)
                {
                    line = Geometry.RowHouse(spots[0]);
                }
                else if (spots.Select(Geometry.ColumnOf).Distinct().Count() == 1)
                {
                    line = Geometry.ColumnHouse(spots[0]);
                }
                if (line is null)
                {
                    continue;
                }
                var eliminations = line.Cells
                    .Where(c => !box.Contains(c) && board.IsEmpty(c) && candidates[c].Contains(d))
                    .Select(c => new Elimination(c, d))
                    .ToList();
                if (eliminations.Count > 0)
                {
                    yield return new Move(Technique, box, spots, CandidateSet.Of(d), null, eliminations, line);
                }
            }
        }
    }
}

/// <summary>
/// Box-line reduction (claiming): a digit's spots within a row or column all lie in one box,
/// so the digit can be removed from the rest of that box.
/// </summary>
public sealed class BoxLineReductionFinder : ITechniqueFinder
{
    public Technique Technique => Technique.BoxLineReduction;

    public IEnumerable<Move> Find(Board board, CandidateSet[] candidates)
    {
        foreach (var line in Geometry.Rows.Concat(Geometry.Columns))
        {
            for (var d = 1; d <= 9; d++)
            {
                if (FinderHelpers.IsPlacedIn(board, line, d))
                {
                    continue;
                }
                var spots = FinderHelpers.Spots(board, candidates, line, d);
                if (spots.Count < 2 || spots.Select(Geometry.BoxOf).Distinct().Count() != 1)
                {
                    continue;
                }
                var box = Geometry.BoxHouse(spots[0]);
                var eliminations = box.Cells
                    .Where(c => !line.Contains(c) && board.IsEmpty(c) && candidates[c].Contains(d))
                    .Select(c => new Elimination(c, d))
                    .ToList();
                if (eliminations.Count > 0)
                {
                    yield return new Move(Technique, line, spots, CandidateSet.Of(d), null, eliminations, box);
                }
            }
        }
    }
}
