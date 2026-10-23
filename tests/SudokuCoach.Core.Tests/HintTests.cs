using SudokuCoach.Core.Game;
using SudokuCoach.Core.Techniques;
using static SudokuCoach.Core.Tests.TestPuzzles;

namespace SudokuCoach.Core.Tests;

public class HintTests
{
    private static Move NakedPairAtStuck() =>
        Assert.Single(Analyzer.FindAll(Stuck412(), Stuck412().ComputeCandidates(), [Technique.NakedPair]));

    private static Move Find(Technique technique, Func<Move, bool>? where = null) =>
        Analyzer.FindAll(Stuck412(), Stuck412().ComputeCandidates(), [technique]).First(where ?? (_ => true));

    [Fact]
    public void Naked_pair_ladder_reveals_a_little_more_each_level()
    {
        var pair = NakedPairAtStuck();
        var box6 = Geometry.Boxes[5].Cells;

        var technique = Hint.ForMove(pair, HintLevel.Technique);
        Assert.Equal("There's a naked pair on the board.", technique.Text);
        Assert.Empty(technique.HouseCells);
        Assert.Empty(technique.FocusCells);

        var house = technique.Next();
        Assert.Equal(HintLevel.House, house.Level);
        Assert.Equal("There's a naked pair in box 6.", house.Text);
        Assert.Equal(box6, house.HouseCells);
        Assert.Empty(house.FocusCells);

        var cells = house.Next();
        Assert.Equal("r5c7 and r6c7 can only be 5 or 9.", cells.Text);
        Assert.Equal(new[] { Cell(5, 7), Cell(6, 7) }, cells.FocusCells);
        Assert.Empty(cells.Eliminations);
        Assert.False(cells.IsFinal);

        var effect = cells.Next();
        Assert.Equal("So remove 9 from r4c8; 5 and 9 from r6c8.", effect.Text);
        Assert.Equal(pair.Eliminations, effect.Eliminations);
        Assert.Null(effect.Placement);
        Assert.True(effect.IsFinal);
        Assert.Same(effect, effect.Next());
    }

    [Fact]
    public void Hidden_pair_names_digits_and_cells()
    {
        var hint = Hint.ForMove(Find(Technique.HiddenPair), HintLevel.Cells);

        Assert.Equal("In box 6, 4 and 8 fit only in r4c8 and r6c8.", hint.Text);
    }

    [Fact]
    public void Pointing_effect_reaches_into_the_line()
    {
        var pointing = Find(Technique.Pointing, m => m.PatternDigits == CandidateSet.Of(4));

        var cells = Hint.ForMove(pointing, HintLevel.Cells);
        Assert.StartsWith($"In {pointing.House!.Name}, 4 can only go in column 8 (", cells.Text);

        var effect = Hint.ForMove(pointing, HintLevel.Effect);
        Assert.Equal("So remove 4 from r7c8, r8c8 and r9c8.", effect.Text);
        Assert.Superset(Geometry.Columns[7].Cells.ToHashSet(), effect.HouseCells.ToHashSet());
        Assert.Superset(pointing.House.Cells.ToHashSet(), effect.HouseCells.ToHashSet());
    }

    [Fact]
    public void Hidden_single_keeps_the_cell_hidden_until_the_end()
    {
        var board = Board.Parse(Puzzle412);
        var single = Analyzer.FindAll(board, board.ComputeCandidates(), [Technique.HiddenSingle]).First();
        var cell = single.Placement!.Value.Cell;

        var cells = Hint.ForMove(single, HintLevel.Cells);
        Assert.Equal($"Where can {single.Placement.Value.Digit} go in {single.House!.Name}?", cells.Text);
        Assert.Empty(cells.FocusCells);

        var effect = Hint.ForMove(single, HintLevel.Effect);
        Assert.Equal($"So {Geometry.CellName(cell)} is {single.Placement.Value.Digit}.", effect.Text);
        Assert.Equal(new[] { cell }, effect.FocusCells);
        Assert.Equal(single.Placement, effect.Placement);
    }

    [Fact]
    public void Naked_single_points_at_its_box()
    {
        var board = Stuck412();
        var candidates = board.ComputeCandidates();
        NakedPairAtStuck().ApplyTo(board, candidates);
        // Narrow one cell to a single candidate by hand to get a naked single to hint.
        var cell = Cell(4, 8);
        candidates[cell] = CandidateSet.Of(Board.Parse(Puzzle412Solution)[cell]);
        var single = Analyzer.FindAll(board, candidates, [Technique.NakedSingle]).First(m => m.PatternCells[0] == cell);

        var house = Hint.ForMove(single, HintLevel.House);
        Assert.Equal("There's a naked single in box 6.", house.Text);
        Assert.Equal("r4c8 has only one candidate left.", Hint.ForMove(single, HintLevel.Cells).Text);
    }

    [Fact]
    public void Session_climbs_the_ladder_then_stays_on_the_last_step()
    {
        var game = new GameSession(Stuck412());

        Assert.Equal(HintLevel.Technique, game.NextHint().Level);
        Assert.Equal(Technique.NakedPair, game.CurrentHint!.Move!.Technique);
        Assert.Equal(HintLevel.House, game.NextHint().Level);
        Assert.Equal(HintLevel.Cells, game.NextHint().Level);
        Assert.Equal(HintLevel.Effect, game.NextHint().Level);

        // Asking again after the last step starts over with the same (still unsolved) move.
        Assert.Equal(HintLevel.Technique, game.NextHint().Level);
        Assert.Equal(5, game.HintsUsed);
    }

    [Fact]
    public void Session_hint_from_the_start_is_a_single()
    {
        var game = new GameSession(Board.Parse(Puzzle412));

        var hint = game.NextHint();

        Assert.Equal(HintKind.Move, hint.Kind);
        Assert.Contains(hint.Move!.Technique, new[] { Technique.NakedSingle, Technique.HiddenSingle });
    }

    [Fact]
    public void Wrong_digits_come_before_moves()
    {
        var game = new GameSession(Board.Parse(Puzzle412));
        game.Select(Cell(1, 1));
        game.Enter(9); // the answer is 8

        var hint = game.NextHint();

        Assert.Equal(HintKind.WrongDigit, hint.Kind);
        Assert.Equal("The 9 in r1c1 is wrong.", hint.Text);
        Assert.Equal(new[] { Cell(1, 1) }, hint.FocusCells);
        Assert.True(hint.IsFinal);
    }

    [Fact]
    public void Note_errors_come_before_moves()
    {
        var game = new GameSession(Board.Parse(Puzzle412));
        game.Select(Cell(1, 1));
        game.NotesMode = true;
        game.Enter(9); // notes {9}, but the answer is 8

        var hint = game.NextHint();

        Assert.Equal(HintKind.NoteError, hint.Kind);
        Assert.Equal("Your notes in r1c1 are missing the digit that goes there.", hint.Text);
    }

    [Fact]
    public void Hint_survives_edits_that_leave_the_move_in_place()
    {
        var game = new GameSession(Stuck412());
        game.NextHint();
        game.NextHint();

        // Filling in every note leaves the pair's candidates as they were.
        game.FillAllNotes();

        Assert.NotNull(game.CurrentHint);
        Assert.Equal(HintLevel.House, game.CurrentHint!.Level);
        Assert.Equal(Technique.NakedPair, game.CurrentHint.Move!.Technique);
    }

    [Fact]
    public void Hint_goes_away_once_the_player_acts_on_it()
    {
        var game = new GameSession(Stuck412());
        game.FillAllNotes();
        game.NextHint();

        // Remove 9 from r4c8: part of the pair's effect, so the hinted move has changed.
        game.Select(Cell(4, 8));
        game.NotesMode = true;
        game.Enter(9);

        Assert.Null(game.CurrentHint);
    }

    [Fact]
    public void Clear_hint_dismisses_it()
    {
        var game = new GameSession(Stuck412());
        game.NextHint();

        game.ClearHint();

        Assert.Null(game.CurrentHint);
    }

    [Fact]
    public void Stuck_hint_when_nothing_applies()
    {
        var hint = Hint.Stuck();

        Assert.Equal(HintKind.Stuck, hint.Kind);
        Assert.True(hint.IsFinal);
        Assert.Empty(hint.FocusCells);
    }
}
