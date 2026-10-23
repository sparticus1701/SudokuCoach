using SudokuCoach.Core.Techniques;
using static SudokuCoach.Core.Tests.TestPuzzles;

namespace SudokuCoach.Core.Tests;

/// <summary>Technique finders on the Puzzle 412 stuck position shown in the mockups.</summary>
public class AnalyzerTests
{
    private static IReadOnlyList<Move> MovesAtStuck() =>
        Analyzer.FindAll(Stuck412(), Stuck412().ComputeCandidates());

    private static IReadOnlyList<Move> Of(Technique t) =>
        MovesAtStuck().Where(m => m.Technique == t).ToArray();

    private static Elimination E(int row, int column, int digit) => new(Cell(row, column), digit);

    [Fact]
    public void Stuck_position_has_no_singles()
    {
        Assert.Empty(Of(Technique.NakedSingle));
        Assert.Empty(Of(Technique.HiddenSingle));
    }

    [Fact]
    public void Finds_the_naked_pair_59_in_box_6()
    {
        var pair = Assert.Single(Of(Technique.NakedPair));

        Assert.Equal("box 6", pair.House!.Name);
        Assert.Equal(new[] { Cell(5, 7), Cell(6, 7) }, pair.PatternCells);
        Assert.Equal(CandidateSet.Of(5, 9), pair.PatternDigits);
        Assert.Equal(new[] { E(4, 8, 9), E(6, 8, 5), E(6, 8, 9) }, pair.Eliminations);
    }

    [Fact]
    public void Finds_the_hidden_pair_48_with_the_same_effect()
    {
        var hidden = Assert.Single(Of(Technique.HiddenPair));
        var naked = Assert.Single(Of(Technique.NakedPair));

        Assert.Equal(new[] { Cell(4, 8), Cell(6, 8) }, hidden.PatternCells);
        Assert.Equal(CandidateSet.Of(4, 8), hidden.PatternDigits);
        Assert.Equal(naked.EffectKey, hidden.EffectKey);
    }

    [Fact]
    public void Finds_the_four_pointing_moves_in_column_8()
    {
        var pointing = Of(Technique.Pointing);

        Assert.Equal(4, pointing.Count);
        Assert.All(pointing, m => Assert.Equal("column 8", m.SecondaryHouse!.Name));
        Assert.Contains(pointing, m => m.PatternDigits == CandidateSet.Of(4) &&
            m.Eliminations.SequenceEqual(new[] { E(7, 8, 4), E(8, 8, 4), E(9, 8, 4) }));
        Assert.Contains(pointing, m => m.PatternDigits == CandidateSet.Of(9) && m.PatternCells.Count == 3);
    }

    [Fact]
    public void Box_line_reductions_reach_the_same_eliminations_as_pointing()
    {
        var boxLine = Of(Technique.BoxLineReduction);
        var pointingKeys = Of(Technique.Pointing).Select(m => m.EffectKey).ToHashSet();

        Assert.Equal(4, boxLine.Count);
        Assert.All(boxLine, m => Assert.Contains(m.EffectKey, pointingKeys));
    }

    [Fact]
    public void Every_elimination_removes_a_candidate_the_solution_does_not_need()
    {
        var solution = Board.Parse(Puzzle412Solution);

        foreach (var move in MovesAtStuck())
        {
            Assert.All(move.Eliminations, e => Assert.NotEqual(solution[e.Cell], e.Digit));
        }
    }

    [Fact]
    public void Describe_reads_like_a_hint()
    {
        var pair = Assert.Single(Of(Technique.NakedPair));

        Assert.Equal(
            "Naked pair 5/9 in box 6 (r5c7, r6c7): remove 9 from r4c8; 5, 9 from r6c8",
            pair.Describe());
    }

    [Fact]
    public void Applying_the_pair_narrows_the_box_and_opens_more_moves()
    {
        var board = Stuck412();
        var candidates = board.ComputeCandidates();
        Assert.Single(Of(Technique.NakedPair)).ApplyTo(board, candidates);

        Assert.Equal(CandidateSet.Of(4, 8), candidates[Cell(4, 8)]);
        Assert.Equal(CandidateSet.Of(4, 8), candidates[Cell(6, 8)]);
        Assert.NotNull(Analyzer.FindEasiest(board, candidates));
    }
}
