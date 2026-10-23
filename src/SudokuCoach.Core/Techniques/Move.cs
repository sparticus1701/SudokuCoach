using System.Text;

namespace SudokuCoach.Core.Techniques;

/// <summary>Removing a candidate digit from a cell.</summary>
public readonly record struct Elimination(int Cell, int Digit)
{
    public override string ToString() => $"{Geometry.CellName(Cell)}≠{Digit}";
}

/// <summary>Writing a digit into a cell.</summary>
public readonly record struct Placement(int Cell, int Digit)
{
    public override string ToString() => $"{Geometry.CellName(Cell)}={Digit}";
}

/// <summary>
/// One productive deduction: the technique, the pattern that justifies it, and its effect
/// (a placement, or one or more eliminations). Moves that change nothing are never produced.
/// </summary>
public sealed class Move
{
    private string? _effectKey;

    public Move(
        Technique technique,
        House? house,
        IReadOnlyList<int> patternCells,
        CandidateSet patternDigits,
        Placement? placement,
        IReadOnlyList<Elimination> eliminations,
        House? secondaryHouse = null)
    {
        if (placement is null && eliminations.Count == 0)
        {
            throw new ArgumentException("A move must place a digit or eliminate at least one candidate.");
        }
        Technique = technique;
        House = house;
        SecondaryHouse = secondaryHouse;
        PatternCells = patternCells.OrderBy(c => c).ToArray();
        PatternDigits = patternDigits;
        Placement = placement;
        Eliminations = eliminations.Distinct().OrderBy(e => e.Cell).ThenBy(e => e.Digit).ToArray();
    }

    public Technique Technique { get; }

    /// <summary>The house the pattern lives in (for pointing, the box; for box-line, the line).</summary>
    public House? House { get; }

    /// <summary>The house the effect reaches into (for pointing, the line; for box-line, the box).</summary>
    public House? SecondaryHouse { get; }

    /// <summary>The cells that form the pattern, ascending.</summary>
    public IReadOnlyList<int> PatternCells { get; }

    /// <summary>The digits that form the pattern.</summary>
    public CandidateSet PatternDigits { get; }

    public Placement? Placement { get; }

    /// <summary>Candidates removed, ordered by cell then digit.</summary>
    public IReadOnlyList<Elimination> Eliminations { get; }

    /// <summary>
    /// A canonical string for what the move does to the grid. Two moves with the same key have the
    /// same effect even if they come from different techniques (for example a naked pair and the
    /// hidden pair in the rest of the same box).
    /// </summary>
    public string EffectKey => _effectKey ??= BuildEffectKey();

    /// <summary>Applies the move to a candidate grid (and the board, for a placement).</summary>
    public void ApplyTo(Board board, CandidateSet[] candidates)
    {
        if (Placement is { } p)
        {
            board.Set(p.Cell, p.Digit);
            candidates[p.Cell] = CandidateSet.Empty;
            foreach (var peer in Geometry.Peers(p.Cell))
            {
                if (candidates[peer].Contains(p.Digit))
                {
                    candidates[peer] = candidates[peer].Without(p.Digit);
                }
            }
        }
        foreach (var e in Eliminations)
        {
            if (candidates[e.Cell].Contains(e.Digit))
            {
                candidates[e.Cell] = candidates[e.Cell].Without(e.Digit);
            }
        }
    }

    /// <summary>A plain-English description, e.g. "Naked pair 5/9 in box 6 (r5c7, r6c7): remove 9 from r4c8; 5, 9 from r6c8".</summary>
    public string Describe()
    {
        var sb = new StringBuilder(Technique.DisplayName());
        if (!PatternDigits.IsEmpty)
        {
            sb.Append(' ').Append(string.Join('/', PatternDigits.Digits));
        }
        if (House is not null)
        {
            sb.Append(" in ").Append(House.Name);
        }
        if (PatternCells.Count > 0)
        {
            sb.Append(" (").Append(string.Join(", ", PatternCells.Select(Geometry.CellName))).Append(')');
        }
        sb.Append(": ");
        if (Placement is { } p)
        {
            sb.Append($"{Geometry.CellName(p.Cell)} is {p.Digit}");
        }
        else
        {
            sb.Append("remove ");
            sb.Append(string.Join("; ", Eliminations
                .GroupBy(e => e.Cell)
                .Select(g => $"{string.Join(", ", g.Select(e => e.Digit))} from {Geometry.CellName(g.Key)}")));
        }
        return sb.ToString();
    }

    public override string ToString() => Describe();

    private string BuildEffectKey()
    {
        if (Placement is { } p)
        {
            return $"P{p.Cell}:{p.Digit}";
        }
        return "E" + string.Join(',', Eliminations.Select(e => $"{e.Cell}:{e.Digit}"));
    }
}
