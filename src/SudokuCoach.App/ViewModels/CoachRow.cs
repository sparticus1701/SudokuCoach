namespace SudokuCoach.ViewModels;

/// <summary>One technique row in the coach panel: "Naked pair  1".</summary>
public sealed class CoachRow
{
    public CoachRow(string name, int count)
    {
        Name = name;
        Count = count;
    }

    public string Name { get; }

    public int Count { get; }

    public bool IsActive => Count > 0;

    public bool IsInactive => Count == 0;
}
