using SudokuCoach.Core.Techniques;

namespace SudokuCoach.Core.Game;

/// <summary>How much a move hint gives away. Each level adds to the one before.</summary>
public enum HintLevel
{
    /// <summary>"There's a naked pair on the board."</summary>
    Technique = 1,

    /// <summary>"There's a naked pair in box 6." Shades the house.</summary>
    House = 2,

    /// <summary>"r5c7 and r6c7 can only be 5 or 9." Marks the pattern cells.</summary>
    Cells = 3,

    /// <summary>"So remove 9 from r4c8; 5 and 9 from r6c8." Shows the placement or eliminations.</summary>
    Effect = 4,
}

public enum HintKind
{
    /// <summary>A staged hint for the easiest available move.</summary>
    Move,

    /// <summary>A placed digit doesn't match the solution. Shown before any move hint.</summary>
    WrongDigit,

    /// <summary>A cell's notes leave out its answer, so patterns in the notes would mislead.</summary>
    NoteError,

    /// <summary>No technique the engine knows applies.</summary>
    Stuck,
}

/// <summary>
/// One step of a hint: the text to show and what to highlight on the board.
/// Move hints climb four levels (<see cref="HintLevel"/>); the other kinds are a single step.
/// </summary>
public sealed class Hint
{
    private Hint(
        HintKind kind,
        HintLevel level,
        Move? move,
        string text,
        IReadOnlyList<int> houseCells,
        IReadOnlyList<int> focusCells,
        IReadOnlyList<Elimination> eliminations,
        Placement? placement)
    {
        Kind = kind;
        Level = level;
        Move = move;
        Text = text;
        HouseCells = houseCells;
        FocusCells = focusCells;
        Eliminations = eliminations;
        Placement = placement;
    }

    public HintKind Kind { get; }

    /// <summary>For move hints, how far along the ladder this step is. Other kinds report <see cref="HintLevel.Effect"/>.</summary>
    public HintLevel Level { get; }

    /// <summary>The move being hinted, for <see cref="HintKind.Move"/>.</summary>
    public Move? Move { get; }

    public string Text { get; }

    /// <summary>Cells to shade lightly: the house (and, at the last level, the house the effect reaches into).</summary>
    public IReadOnlyList<int> HouseCells { get; }

    /// <summary>Cells to mark strongly: the pattern, the placement cell, or the mistakes.</summary>
    public IReadOnlyList<int> FocusCells { get; }

    /// <summary>Candidates to strike out. Only filled in at the last level.</summary>
    public IReadOnlyList<Elimination> Eliminations { get; }

    /// <summary>The digit to write. Only filled in at the last level.</summary>
    public Placement? Placement { get; }

    /// <summary>True when there is nothing more to reveal.</summary>
    public bool IsFinal => Kind != HintKind.Move || Level == HintLevel.Effect;

    /// <summary>The same hint one level further, or this hint if it is final.</summary>
    public Hint Next() => IsFinal ? this : ForMove(Move!, Level + 1);

    /// <summary>Builds the step of a move hint at <paramref name="level"/>.</summary>
    public static Hint ForMove(Move move, HintLevel level)
    {
        if (level is < HintLevel.Technique or > HintLevel.Effect)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        // A naked single has no house of its own; its box is the natural place to point at.
        var house = move.House ?? Geometry.BoxHouse(move.PatternCells[0]);
        var name = move.Technique.DisplayName().ToLowerInvariant();

        var text = level switch
        {
            HintLevel.Technique => $"There's a {name} on the board.",
            HintLevel.House => $"There's a {name} in {house.Name}.",
            HintLevel.Cells => CellsText(move, house),
            _ => EffectText(move),
        };

        IReadOnlyList<int> houseCells = level switch
        {
            HintLevel.Technique => [],
            HintLevel.Effect when move.SecondaryHouse is { } second =>
                house.Cells.Union(second.Cells).Order().ToArray(),
            _ => house.Cells,
        };

        IReadOnlyList<int> focusCells = level switch
        {
            HintLevel.Technique or HintLevel.House => [],
            // The hidden single's pattern cell is the answer, so it stays hidden until the end.
            HintLevel.Cells when move.Technique == Technique.HiddenSingle => [],
            _ => move.PatternCells,
        };

        var final = level == HintLevel.Effect;
        return new Hint(
            HintKind.Move,
            level,
            move,
            text,
            houseCells,
            focusCells,
            final ? move.Eliminations : [],
            final ? move.Placement : null);
    }

    /// <summary>A hint pointing at placed digits that don't match the solution.</summary>
    public static Hint WrongDigits(Board board, IReadOnlyList<int> cells)
    {
        RequireCells(cells);
        var text = cells.Count == 1
            ? $"The {board[cells[0]]} in {Geometry.CellName(cells[0])} is wrong."
            : $"These digits are wrong: {Words.And(cells.Select(c => $"the {board[c]} in {Geometry.CellName(c)}"))}.";
        return Single(HintKind.WrongDigit, text, cells);
    }

    /// <summary>A hint pointing at cells whose notes leave out the answer.</summary>
    public static Hint NoteErrors(IReadOnlyList<int> cells)
    {
        RequireCells(cells);
        var text = cells.Count == 1
            ? $"Your notes in {Geometry.CellName(cells[0])} are missing the digit that goes there."
            : $"Your notes in {Words.And(cells.Select(Geometry.CellName))} are each missing the digit that goes there.";
        return Single(HintKind.NoteError, text, cells);
    }

    /// <summary>The hint when nothing the engine knows applies.</summary>
    public static Hint Stuck() =>
        Single(HintKind.Stuck, "Nothing the coach knows applies here. This spot needs a harder technique.", []);

    public override string ToString() => Text;

    private static Hint Single(HintKind kind, string text, IReadOnlyList<int> cells) =>
        new(kind, HintLevel.Effect, null, text, [], cells.Order().ToArray(), [], null);

    private static void RequireCells(IReadOnlyList<int> cells)
    {
        if (cells.Count == 0)
        {
            throw new ArgumentException("At least one cell is needed.", nameof(cells));
        }
    }

    private static string CellsText(Move move, House house)
    {
        var cells = Words.And(move.PatternCells.Select(Geometry.CellName));
        var digits = move.PatternDigits.Digits.ToArray();
        switch (move.Technique)
        {
            case Technique.NakedSingle:
                return $"{cells} has only one candidate left.";
            case Technique.HiddenSingle:
                return $"Where can {digits[0]} go in {house.Name}?";
            case Technique.NakedPair:
            case Technique.NakedTriple:
                return $"{cells} can only be {Words.Or(digits)}.";
            case Technique.HiddenPair:
            case Technique.HiddenTriple:
                return $"In {house.Name}, {Words.And(digits)} fit only in {cells}.";
            case Technique.Pointing:
            case Technique.BoxLineReduction:
                return $"In {house.Name}, {digits[0]} can only go in {move.SecondaryHouse!.Name} ({cells}).";
            default:
                return $"Look at {cells}.";
        }
    }

    private static string EffectText(Move move)
    {
        if (move.Placement is { } p)
        {
            return $"So {Geometry.CellName(p.Cell)} is {p.Digit}.";
        }

        // One digit (pointing, box-line): "remove 4 from r7c8, r8c8 and r9c8".
        var digits = move.Eliminations.Select(e => e.Digit).Distinct().ToArray();
        if (digits.Length == 1)
        {
            return $"So remove {digits[0]} from {Words.And(move.Eliminations.Select(e => Geometry.CellName(e.Cell)))}.";
        }

        // Several digits: group by cell, "remove 9 from r4c8; 5 and 9 from r6c8".
        var parts = move.Eliminations
            .GroupBy(e => e.Cell)
            .Select(g => $"{Words.And(g.Select(e => e.Digit))} from {Geometry.CellName(g.Key)}");
        return $"So remove {string.Join("; ", parts)}.";
    }

    private static class Words
    {
        public static string And<T>(IEnumerable<T> items) => Join(items, "and");

        public static string Or<T>(IEnumerable<T> items) => Join(items, "or");

        private static string Join<T>(IEnumerable<T> items, string conjunction)
        {
            var list = items.Select(i => i!.ToString()!).ToArray();
            return list.Length switch
            {
                0 => string.Empty,
                1 => list[0],
                _ => $"{string.Join(", ", list[..^1])} {conjunction} {list[^1]}",
            };
        }
    }
}
