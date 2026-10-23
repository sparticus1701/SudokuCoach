namespace SudokuCoach.ViewModels;

/// <summary>A digit key and how many more of that digit the board needs.</summary>
public sealed class KeyItem : ObservableObject
{
    private int _remaining;

    public KeyItem(int digit) => Digit = digit;

    public int Digit { get; }

    public int Remaining
    {
        get => _remaining;
        set
        {
            if (SetProperty(ref _remaining, value))
            {
                OnPropertyChanged(nameof(IsDone));
            }
        }
    }

    public bool IsDone => Remaining == 0;
}
