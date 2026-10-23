using SudokuCoach.Core.Solving;

namespace SudokuCoach.Core.Tests;

public class BoardTests
{
    [Fact]
    public void Parse_round_trips_and_marks_givens()
    {
        var board = Board.Parse(TestPuzzles.Puzzle412);

        Assert.Equal(TestPuzzles.Puzzle412, board.ToString());
        Assert.Equal(28, board.GivenCount);
        Assert.True(board.IsGiven(5));
        Assert.Equal(6, board[5]);
        Assert.True(board.IsEmpty(0));
    }

    [Fact]
    public void Parse_accepts_dots_and_whitespace()
    {
        var dotted = TestPuzzles.Puzzle412.Replace('0', '.');
        var spaced = string.Join('\n', Enumerable.Range(0, 9).Select(r => dotted.Substring(r * 9, 9)));

        Assert.Equal(TestPuzzles.Puzzle412, Board.Parse(spaced).ToString());
    }

    [Fact]
    public void Parse_rejects_bad_input()
    {
        Assert.Throws<FormatException>(() => Board.Parse("123"));
        Assert.Throws<FormatException>(() => Board.Parse(new string('x', 81)));
    }

    [Fact]
    public void Parse_with_progress_keeps_givens_separate_from_placements()
    {
        var board = TestPuzzles.Stuck412();

        Assert.Equal(28, board.GivenCount);
        Assert.Equal(32, board.EmptyCount);
        Assert.False(board.IsGiven(1));
        Assert.Equal(5, board[1]);
        Assert.Equal(TestPuzzles.Puzzle412, board.GivensString());
    }

    [Fact]
    public void Givens_cannot_be_changed()
    {
        var board = Board.Parse(TestPuzzles.Puzzle412);

        Assert.Throws<InvalidOperationException>(() => board.Set(5, 1));
    }

    [Fact]
    public void Conflicts_are_detected_in_any_house()
    {
        var board = Board.Parse(TestPuzzles.Puzzle412);
        board.Set(0, 6); // r1c1 = 6 clashes with the given 6 at r1c6

        Assert.True(board.HasConflict(0));
        Assert.True(board.HasConflict(5));
        Assert.False(board.IsValid);
    }

    [Fact]
    public void Computed_candidates_match_the_stuck_position()
    {
        var candidates = TestPuzzles.Stuck412().ComputeCandidates();

        Assert.Equal(CandidateSet.Of(5, 9), candidates[TestPuzzles.Cell(5, 7)]);
        Assert.Equal(CandidateSet.Of(5, 9), candidates[TestPuzzles.Cell(6, 7)]);
        Assert.Equal(CandidateSet.Of(4, 8, 9), candidates[TestPuzzles.Cell(4, 8)]);
        Assert.Equal(CandidateSet.Of(4, 5, 8, 9), candidates[TestPuzzles.Cell(6, 8)]);
        Assert.True(candidates[TestPuzzles.Cell(1, 2)].IsEmpty); // filled cell
    }

    [Fact]
    public void Brute_force_finds_the_unique_solution()
    {
        var puzzle = Board.Parse(TestPuzzles.Puzzle412);

        Assert.Equal(1, BruteForceSolver.CountSolutions(puzzle));
        Assert.Equal(TestPuzzles.Puzzle412Solution, BruteForceSolver.SolveUnique(puzzle)!.ToString());
    }

    [Fact]
    public void Brute_force_reports_multiple_solutions()
    {
        var empty = Board.Parse(new string('0', 81));

        Assert.Equal(2, BruteForceSolver.CountSolutions(empty));
        Assert.Null(BruteForceSolver.SolveUnique(empty));
    }
}
