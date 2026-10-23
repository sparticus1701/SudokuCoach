using SudokuCoach.Core.Game;
using SudokuCoach.Core.Techniques;
using static SudokuCoach.Core.Tests.TestPuzzles;

namespace SudokuCoach.Core.Tests;

public class GameSessionTests
{
    private static GameSession NewGame() => new(Board.Parse(Puzzle412));

    [Fact]
    public void Solves_the_puzzle_on_creation()
    {
        Assert.Equal(Puzzle412Solution, NewGame().Solution.ToString());
    }

    [Fact]
    public void Entering_a_digit_places_it_and_updates_remaining()
    {
        var game = NewGame();
        var before = game.Remaining(8);

        game.Select(Cell(1, 1));
        Assert.True(game.Enter(8));

        Assert.Equal(8, game.Board[Cell(1, 1)]);
        Assert.Equal(before - 1, game.Remaining(8));
        Assert.False(game.IsWrong(Cell(1, 1)));
    }

    [Fact]
    public void Entering_the_same_digit_again_clears_it()
    {
        var game = NewGame();
        game.Select(Cell(1, 1));
        game.Enter(8);
        game.Enter(8);

        Assert.True(game.Board.IsEmpty(Cell(1, 1)));
    }

    [Fact]
    public void Givens_and_no_selection_are_ignored()
    {
        var game = NewGame();
        Assert.False(game.Enter(1));

        game.Select(Cell(1, 6)); // a given 6
        Assert.False(game.Enter(1));
        Assert.False(game.Erase());
        Assert.Equal(6, game.Board[Cell(1, 6)]);
    }

    [Fact]
    public void Undo_restores_digits_and_notes()
    {
        var game = NewGame();
        game.Select(Cell(1, 1));
        game.NotesMode = true;
        game.Enter(8);
        game.Enter(9);
        game.NotesMode = false;
        game.Enter(8);

        Assert.True(game.Undo());
        Assert.True(game.Board.IsEmpty(Cell(1, 1)));
        Assert.Equal(CandidateSet.Of(8, 9), game.Notes[Cell(1, 1)]);
        Assert.True(game.Undo());
        Assert.True(game.Undo());
        Assert.False(game.CanUndo);
        Assert.True(game.Notes[Cell(1, 1)].IsEmpty);
    }

    [Fact]
    public void Placing_a_digit_clears_it_from_peer_notes()
    {
        var game = NewGame();
        game.FillAllNotes();
        Assert.True(game.Notes[Cell(1, 4)].Contains(7));

        game.Select(Cell(1, 5));
        game.Enter(7);

        Assert.False(game.Notes[Cell(1, 4)].Contains(7));
        Assert.True(game.Notes[Cell(1, 5)].IsEmpty);
    }

    [Fact]
    public void Note_errors_flag_cells_whose_notes_miss_the_answer()
    {
        var game = NewGame();
        game.Select(Cell(1, 1)); // answer is 8
        game.NotesMode = true;
        game.Enter(9);

        Assert.Equal(new[] { Cell(1, 1) }, game.NoteErrors());
    }

    [Fact]
    public void Coach_candidates_keep_the_answer_even_when_notes_drop_it()
    {
        var game = NewGame();
        game.Select(Cell(1, 1));
        game.NotesMode = true;
        game.Enter(9);

        var candidates = game.CoachCandidates();

        Assert.True(candidates[Cell(1, 1)].Contains(8));
        Assert.False(candidates[Cell(1, 1)].Contains(1)); // narrowed to the player's notes
    }

    [Fact]
    public void Coach_ignores_wrong_placements()
    {
        var game = NewGame();
        game.Select(Cell(1, 1));
        game.Enter(9); // wrong: the answer is 8

        Assert.True(game.IsWrong(Cell(1, 1)));
        Assert.True(game.CoachCandidates()[Cell(1, 1)].Contains(8));
    }

    [Fact]
    public void Coach_on_the_stuck_position_shows_the_pair()
    {
        var game = new GameSession(Stuck412());

        var report = game.Coach(new HashSet<Technique>
        {
            Technique.NakedSingle, Technique.HiddenSingle, Technique.NakedPair, Technique.HiddenPair,
        });

        Assert.Equal(1, report.CountOf(Technique.NakedPair));
        Assert.Equal(0, report.CountOf(Technique.NakedSingle));
        Assert.Equal(Technique.Pointing, report.NextTechniqueToLearn);
    }

    [Fact]
    public void Changed_fires_on_edits()
    {
        var game = NewGame();
        var count = 0;
        game.Changed += (_, _) => count++;

        game.Select(Cell(1, 1));
        game.Enter(8);
        game.Undo();

        Assert.Equal(3, count);
    }
}
