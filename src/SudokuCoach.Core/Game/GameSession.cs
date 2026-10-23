using SudokuCoach.Core.Solving;
using SudokuCoach.Core.Techniques;

namespace SudokuCoach.Core.Game;

/// <summary>
/// One game in progress: the board, the player's notes, selection, notes mode and undo.
/// UI-free so it can be unit tested; the app binds to it.
/// </summary>
public sealed class GameSession
{
    private readonly Stack<(int[] Values, CandidateSet[] Notes)> _undo = new();
    private CandidateSet[] _notes = new CandidateSet[81];

    public GameSession(Board puzzle, Board? solution = null)
    {
        Board = puzzle.Clone();
        Solution = solution ?? BruteForceSolver.SolveUnique(puzzle.GivensOnly())
            ?? throw new ArgumentException("The puzzle does not have exactly one solution.", nameof(puzzle));
    }

    /// <summary>Raised after anything visible changes.</summary>
    public event EventHandler? Changed;

    public Board Board { get; }

    public Board Solution { get; }

    /// <summary>The player's pencil marks. Filled cells have none.</summary>
    public IReadOnlyList<CandidateSet> Notes => _notes;

    public int? Selected { get; private set; }

    public bool NotesMode { get; set; }

    public bool CanUndo => _undo.Count > 0;

    public bool IsSolved => Board.IsSolved;

    /// <summary>The hint on screen, if any. Survives edits as long as the hinted move still holds.</summary>
    public Hint? CurrentHint { get; private set; }

    /// <summary>How many times the player has asked for a hint or for more of one.</summary>
    public int HintsUsed { get; private set; }

    public void Select(int? cell)
    {
        if (cell is < 0 or > 80)
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }
        Selected = cell;
        OnChanged();
    }

    /// <summary>
    /// A digit key: toggles a note in notes mode, otherwise places the digit
    /// (pressing the digit already there clears it). Returns false if nothing changed.
    /// </summary>
    public bool Enter(int digit)
    {
        if (digit is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }
        if (Selected is not { } cell || Board.IsGiven(cell))
        {
            return false;
        }

        if (NotesMode)
        {
            if (!Board.IsEmpty(cell))
            {
                return false;
            }
            PushUndo();
            _notes[cell] = _notes[cell].Contains(digit) ? _notes[cell].Without(digit) : _notes[cell].With(digit);
        }
        else
        {
            PushUndo();
            if (Board[cell] == digit)
            {
                Board.Set(cell, 0);
            }
            else
            {
                Board.Set(cell, digit);
                _notes[cell] = CandidateSet.Empty;
                foreach (var peer in Geometry.Peers(cell))
                {
                    if (_notes[peer].Contains(digit))
                    {
                        _notes[peer] = _notes[peer].Without(digit);
                    }
                }
            }
        }
        OnEdited();
        return true;
    }

    /// <summary>Clears the selected cell's digit or notes.</summary>
    public bool Erase()
    {
        if (Selected is not { } cell || Board.IsGiven(cell))
        {
            return false;
        }
        if (Board.IsEmpty(cell) && _notes[cell].IsEmpty)
        {
            return false;
        }
        PushUndo();
        Board.Set(cell, 0);
        _notes[cell] = CandidateSet.Empty;
        OnEdited();
        return true;
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }
        var (values, notes) = _undo.Pop();
        for (var i = 0; i < 81; i++)
        {
            if (!Board.IsGiven(i))
            {
                Board.Set(i, values[i]);
            }
        }
        _notes = notes;
        OnEdited();
        return true;
    }

    /// <summary>Replaces all notes with the candidates implied by the digits on the board.</summary>
    public void FillAllNotes()
    {
        PushUndo();
        _notes = Board.ComputeCandidates();
        OnEdited();
    }

    /// <summary>How many more of <paramref name="digit"/> the board needs.</summary>
    public int Remaining(int digit) => Math.Max(0, 9 - Board.CountOf(digit));

    public bool HasConflict(int cell) => Board.HasConflict(cell);

    /// <summary>A placed digit that doesn't match the solution.</summary>
    public bool IsWrong(int cell) => !Board.IsEmpty(cell) && Board[cell] != Solution[cell];

    /// <summary>Empty cells whose notes leave out the correct digit — the mistake that sinks most solves.</summary>
    public IReadOnlyList<int> NoteErrors() =>
        Enumerable.Range(0, 81)
            .Where(c => Board.IsEmpty(c) && !_notes[c].IsEmpty && !_notes[c].Contains(Solution[c]))
            .ToArray();

    /// <summary>
    /// The candidates the coach reasons from. Starts from the board's candidates, narrows each cell
    /// to the player's notes where they have any (so the coach credits their eliminations), but
    /// never drops the correct digit (so a wrong note can't produce wrong advice).
    /// Placed digits that conflict with the solution are ignored.
    /// </summary>
    public CandidateSet[] CoachCandidates()
    {
        var clean = CleanBoard();
        var candidates = clean.ComputeCandidates();
        for (var c = 0; c < 81; c++)
        {
            if (!clean.IsEmpty(c))
            {
                continue;
            }
            if (!_notes[c].IsEmpty)
            {
                candidates[c] = candidates[c].Intersect(_notes[c]);
            }
            candidates[c] = candidates[c].With(Solution[c]);
        }
        return candidates;
    }

    /// <summary>The coach panel's view of the current position.</summary>
    public CoachReport Coach(IReadOnlySet<Technique> learned) =>
        CoachReport.For(CleanBoard(), CoachCandidates(), learned);

    /// <summary>The board with wrong placements removed.</summary>
    private Board CleanBoard()
    {
        var clean = Board.Clone();
        for (var c = 0; c < 81; c++)
        {
            if (IsWrong(c))
            {
                clean.Set(c, 0);
            }
        }
        return clean;
    }

    private void PushUndo() => _undo.Push((Board.ToArray(), (CandidateSet[])_notes.Clone()));

    /// <summary>
    /// Shows a hint, or reveals more of the current one. Mistakes come first: wrong digits, then
    /// notes that rule out the answer. Otherwise the easiest move on the board, one level at a time.
    /// </summary>
    public Hint NextHint()
    {
        HintsUsed++;
        CurrentHint = CurrentHint is { IsFinal: false } current ? current.Next() : FirstHint();
        OnChanged();
        return CurrentHint;
    }

    /// <summary>Dismisses the current hint.</summary>
    public void ClearHint()
    {
        if (CurrentHint is null)
        {
            return;
        }
        CurrentHint = null;
        OnChanged();
    }

    private Hint FirstHint()
    {
        var wrong = Enumerable.Range(0, 81).Where(IsWrong).ToArray();
        if (wrong.Length > 0)
        {
            return Hint.WrongDigits(Board, wrong);
        }
        var noteErrors = NoteErrors();
        if (noteErrors.Count > 0)
        {
            return Hint.NoteErrors(noteErrors);
        }
        var move = Analyzer.FindEasiest(Board, CoachCandidates());
        return move is null ? Hint.Stuck() : Hint.ForMove(move, HintLevel.Technique);
    }

    /// <summary>
    /// After an edit, keeps a move hint if the same deduction is still on the board (so the player
    /// can work on it), and drops it otherwise. Other hints describe a state that just changed, so they go.
    /// </summary>
    private void RevalidateHint()
    {
        if (CurrentHint is not { Kind: HintKind.Move, Move: { } move } hint)
        {
            CurrentHint = null;
            return;
        }
        var same = Analyzer.FinderFor(move.Technique)
            .Find(CleanBoard(), CoachCandidates())
            .FirstOrDefault(m => m.EffectKey == move.EffectKey && m.PatternCells.SequenceEqual(move.PatternCells));
        CurrentHint = same is null || IsSolved ? null : Hint.ForMove(same, hint.Level);
    }

    /// <summary>Raises <see cref="Changed"/> after an edit to digits or notes.</summary>
    private void OnEdited()
    {
        RevalidateHint();
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
