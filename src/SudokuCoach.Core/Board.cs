using System.Text;

namespace SudokuCoach.Core;

/// <summary>
/// The digits on a 9×9 grid: which cells hold which digit (0 = empty) and which digits are givens.
/// Candidates and player notes live outside the board.
/// </summary>
public sealed class Board
{
    private readonly int[] _values;
    private readonly bool[] _givens;

    private Board(int[] values, bool[] givens)
    {
        _values = values;
        _givens = givens;
    }

    /// <summary>
    /// Parses 81 characters in reading order. Digits 1–9 are givens; '0' or '.' is an empty cell.
    /// Whitespace is ignored.
    /// </summary>
    public static Board Parse(string puzzle) => Parse(puzzle, puzzle);

    /// <summary>
    /// Parses a puzzle in progress: <paramref name="givens"/> sets which digits are givens,
    /// <paramref name="current"/> the digits on the grid now (givens plus the player's placements).
    /// </summary>
    public static Board Parse(string givens, string current)
    {
        var g = ParseDigits(givens, nameof(givens));
        var c = ParseDigits(current, nameof(current));
        var isGiven = new bool[81];
        for (var i = 0; i < 81; i++)
        {
            if (g[i] != 0)
            {
                if (c[i] != g[i])
                {
                    throw new FormatException($"Cell {Geometry.CellName(i)} is a given {g[i]} but the current grid has {c[i]}.");
                }
                isGiven[i] = true;
            }
        }
        return new Board(c, isGiven);
    }

    /// <summary>Builds a board from values; every non-zero value is a given unless <paramref name="givens"/> says otherwise.</summary>
    public static Board FromValues(IReadOnlyList<int> values, IReadOnlyList<bool>? givens = null)
    {
        if (values.Count != 81)
        {
            throw new ArgumentException("A board has 81 cells.", nameof(values));
        }
        var v = new int[81];
        var g = new bool[81];
        for (var i = 0; i < 81; i++)
        {
            if (values[i] is < 0 or > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(values), $"Cell {i} holds {values[i]}.");
            }
            v[i] = values[i];
            g[i] = givens?[i] ?? values[i] != 0;
        }
        return new Board(v, g);
    }

    public int this[int cell] => _values[cell];

    public bool IsGiven(int cell) => _givens[cell];

    public bool IsEmpty(int cell) => _values[cell] == 0;

    public int EmptyCount => _values.Count(v => v == 0);

    public int GivenCount => _givens.Count(g => g);

    public bool IsComplete => EmptyCount == 0;

    /// <summary>Complete and with no digit repeated in any house.</summary>
    public bool IsSolved => IsComplete && IsValid;

    /// <summary>No digit appears twice in any house (empty cells are fine).</summary>
    public bool IsValid => Enumerable.Range(0, 81).All(c => !HasConflict(c));

    /// <summary>Places or clears (digit 0) a non-given cell.</summary>
    public void Set(int cell, int digit)
    {
        if (_givens[cell])
        {
            throw new InvalidOperationException($"Cell {Geometry.CellName(cell)} is a given.");
        }
        if (digit is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }
        _values[cell] = digit;
    }

    /// <summary>True when the cell's digit also appears in one of its peers.</summary>
    public bool HasConflict(int cell)
    {
        var v = _values[cell];
        if (v == 0)
        {
            return false;
        }
        foreach (var p in Geometry.Peers(cell))
        {
            if (_values[p] == v)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>How many times <paramref name="digit"/> is on the grid.</summary>
    public int CountOf(int digit) => _values.Count(v => v == digit);

    /// <summary>
    /// Candidates implied by the digits alone: for each empty cell, the digits no peer holds.
    /// Filled cells get an empty set.
    /// </summary>
    public CandidateSet[] ComputeCandidates()
    {
        var result = new CandidateSet[81];
        for (var cell = 0; cell < 81; cell++)
        {
            if (_values[cell] != 0)
            {
                continue;
            }
            var set = CandidateSet.All;
            foreach (var p in Geometry.Peers(cell))
            {
                if (_values[p] != 0)
                {
                    set = set.Without(_values[p]);
                }
            }
            result[cell] = set;
        }
        return result;
    }

    public Board Clone() => new((int[])_values.Clone(), (bool[])_givens.Clone());

    /// <summary>A board holding only this board's givens.</summary>
    public Board GivensOnly()
    {
        var v = new int[81];
        for (var i = 0; i < 81; i++)
        {
            v[i] = _givens[i] ? _values[i] : 0;
        }
        return new Board(v, (bool[])_givens.Clone());
    }

    public int[] ToArray() => (int[])_values.Clone();

    /// <summary>81 characters in reading order, '0' for empty cells.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder(81);
        foreach (var v in _values)
        {
            sb.Append((char)('0' + v));
        }
        return sb.ToString();
    }

    /// <summary>The givens as 81 characters, '0' elsewhere.</summary>
    public string GivensString() => GivensOnly().ToString();

    private static int[] ParseDigits(string text, string paramName)
    {
        ArgumentNullException.ThrowIfNull(text, paramName);
        var digits = new List<int>(81);
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }
            digits.Add(ch switch
            {
                '.' or '0' => 0,
                >= '1' and <= '9' => ch - '0',
                _ => throw new FormatException($"Unexpected character '{ch}' in {paramName}."),
            });
        }
        if (digits.Count != 81)
        {
            throw new FormatException($"{paramName} has {digits.Count} cells; a board has 81.");
        }
        return digits.ToArray();
    }
}
