using SudokuCoach.Core.Generation;
using SudokuCoach.ViewModels;

namespace SudokuCoach.Views;

public partial class PlayPage : ContentPage
{
    private const double MaxContentWidth = 520;

    private readonly PlayViewModel _viewModel = new();

    public PlayPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;

        BoardView.Drawable = new BoardDrawable(_viewModel);
        BoardView.StartInteraction += OnBoardTouched;
        _viewModel.BoardChanged += (_, _) => BoardView.Invalidate();
        SizeChanged += (_, _) => FitToWidth();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartIfNeededAsync(Dispatcher);
        BoardView.Invalidate();
    }

    /// <summary>Phone width on phones; a centered column on tablets and desktop. The board stays square.</summary>
    private void FitToWidth()
    {
        if (Width <= 0)
        {
            return;
        }
        var width = Math.Min(Width, MaxContentWidth);
        Root.WidthRequest = width;
        BoardView.HeightRequest = width - Root.Padding.HorizontalThickness;
    }

    private void OnBoardTouched(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0)
        {
            return;
        }
        var cell = BoardDrawable.CellAt(e.Touches[0], (float)BoardView.Width, (float)BoardView.Height);
        if (cell is { } c)
        {
            _viewModel.TapCell(c);
        }
    }

    private async void OnNewGameClicked(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheetAsync("New puzzle", "Cancel", null, "Easy", "Medium", "Hard");
        if (Enum.TryParse<Difficulty>(choice, out var difficulty))
        {
            await _viewModel.StartNewGameAsync(difficulty);
        }
    }
}
