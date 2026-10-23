using System.Collections.ObjectModel;
using System.Windows.Input;
using SudokuCoach.Core.Game;
using SudokuCoach.Core.Generation;
using SudokuCoach.Core.Techniques;

namespace SudokuCoach.ViewModels;

/// <summary>
/// The play screen. All game rules live in <see cref="GameSession"/> (SudokuCoach.Core, unit tested);
/// this class only adapts it for binding.
/// </summary>
public sealed class PlayViewModel : ObservableObject
{
    // What the player is assumed to know until lessons and progress tracking exist.
    private readonly HashSet<Technique> _learned =
    [
        Technique.NakedSingle,
        Technique.HiddenSingle,
        Technique.NakedPair,
        Technique.HiddenPair,
    ];

    private IDispatcherTimer? _timer;
    private TimeSpan _elapsed;
    private GameSession? _game;
    private CoachReport? _report;
    private string _title = "Sudoku Coach";
    private string _elapsedText = "0:00";
    private bool _isBusy;
    private bool _coachVisible = true;
    private string _coachIntro = string.Empty;
    private bool _hasNewToYou;
    private string _newToYouText = string.Empty;
    private string _learnText = string.Empty;
    private bool _notesMode;

    public PlayViewModel()
    {
        Keys = Enumerable.Range(1, 9).Select(d => new KeyItem(d)).ToList();
        DigitCommand = new Command<string>(s => Game?.Enter(int.Parse(s)));
        NotesCommand = new Command(() => NotesMode = !NotesMode);
        EraseCommand = new Command(() => Game?.Erase());
        UndoCommand = new Command(() => Game?.Undo());
        FillNotesCommand = new Command(() => Game?.FillAllNotes());
        ShowCoachCommand = new Command(() => CoachVisible = true);
        HideCoachCommand = new Command(() => CoachVisible = false);
        LearnCommand = new Command(LearnNext);
    }

    /// <summary>Raised whenever the board needs redrawing.</summary>
    public event EventHandler? BoardChanged;

    public GameSession? Game
    {
        get => _game;
        private set
        {
            if (_game is not null)
            {
                _game.Changed -= OnGameChanged;
            }
            _game = value;
            if (_game is not null)
            {
                _game.Changed += OnGameChanged;
                _game.NotesMode = NotesMode;
            }
            OnPropertyChanged();
        }
    }

    public List<KeyItem> Keys { get; }

    public ObservableCollection<CoachRow> CoachRows { get; } = [];

    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public string ElapsedText { get => _elapsedText; private set => SetProperty(ref _elapsedText, value); }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    public bool CoachVisible
    {
        get => _coachVisible;
        set
        {
            if (SetProperty(ref _coachVisible, value))
            {
                OnPropertyChanged(nameof(CoachHidden));
                RefreshCoach();
            }
        }
    }

    public bool CoachHidden => !CoachVisible;

    public string CoachIntro { get => _coachIntro; private set => SetProperty(ref _coachIntro, value); }

    public bool HasNewToYou { get => _hasNewToYou; private set => SetProperty(ref _hasNewToYou, value); }

    public string NewToYouText { get => _newToYouText; private set => SetProperty(ref _newToYouText, value); }

    public string LearnText { get => _learnText; private set => SetProperty(ref _learnText, value); }

    public bool NotesMode
    {
        get => _notesMode;
        set
        {
            if (SetProperty(ref _notesMode, value) && Game is not null)
            {
                Game.NotesMode = value;
            }
        }
    }

    public ICommand DigitCommand { get; }

    public ICommand NotesCommand { get; }

    public ICommand EraseCommand { get; }

    public ICommand UndoCommand { get; }

    public ICommand FillNotesCommand { get; }

    public ICommand ShowCoachCommand { get; }

    public ICommand HideCoachCommand { get; }

    public ICommand LearnCommand { get; }

    /// <summary>Selects a cell from a tap on the board.</summary>
    public void TapCell(int cell)
    {
        if (Game is not null)
        {
            Game.Select(Game.Selected == cell ? null : cell);
        }
    }

    /// <summary>Starts the first game the first time the page appears.</summary>
    public Task StartIfNeededAsync(IDispatcher dispatcher)
    {
        if (_timer is null)
        {
            _timer = dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) => Tick();
        }
        return Game is null ? StartNewGameAsync(Difficulty.Medium) : Task.CompletedTask;
    }

    public async Task StartNewGameAsync(Difficulty difficulty)
    {
        if (IsBusy)
        {
            return;
        }
        IsBusy = true;
        try
        {
            var seed = Random.Shared.Next();
            var generated = await Task.Run(() => PuzzleGenerator.Generate(difficulty, seed));
            Game = new GameSession(generated.Puzzle, generated.Solution);
            Title = $"{difficulty} · #{seed % 10000:D4}";
            _elapsed = TimeSpan.Zero;
            ElapsedText = "0:00";
            _timer?.Start();
            RefreshAll();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnGameChanged(object? sender, EventArgs e) => RefreshAll();

    private void RefreshAll()
    {
        if (Game is null)
        {
            return;
        }
        foreach (var key in Keys)
        {
            key.Remaining = Game.Remaining(key.Digit);
        }
        if (Game.IsSolved)
        {
            _timer?.Stop();
        }
        RefreshCoach();
        BoardChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshCoach()
    {
        CoachRows.Clear();
        HasNewToYou = false;
        if (Game is null || !CoachVisible)
        {
            return;
        }
        if (Game.IsSolved)
        {
            CoachIntro = "Solved. Nice work.";
            return;
        }

        _report = Game.Coach(_learned);
        foreach (var technique in TechniqueInfo.ByDifficulty.Where(_learned.Contains))
        {
            CoachRows.Add(new CoachRow(technique.DisplayName(), _report.CountOf(technique)));
        }
        CoachIntro = _report.LearnedMoves.Count > 0
            ? "On the board right now:"
            : "Nothing you know applies here yet.";

        if (_report.NextTechniqueToLearn is { } next)
        {
            HasNewToYou = true;
            NewToYouText = "A technique you haven't learned would help";
            LearnText = $"Learn {next.PluralName().ToLowerInvariant()}";
        }
    }

    /// <summary>
    /// Placeholder for the lesson flow from the mockups: for now, learning a technique
    /// just adds it to the coach's known set so its row appears.
    /// </summary>
    private void LearnNext()
    {
        if (_report?.NextTechniqueToLearn is { } next)
        {
            _learned.Add(next);
            RefreshCoach();
        }
    }

    private void Tick()
    {
        _elapsed += TimeSpan.FromSeconds(1);
        ElapsedText = _elapsed.TotalHours >= 1 ? _elapsed.ToString(@"h\:mm\:ss") : _elapsed.ToString(@"m\:ss");
    }
}
