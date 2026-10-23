namespace SudokuCoach.Core.Tests;

/// <summary>Known positions used across tests.</summary>
internal static class TestPuzzles
{
    /// <summary>"Puzzle 412" from the design mockups: 28 givens, unique solution.</summary>
    public const string Puzzle412 =
        "000006400003500700000000825500067103030000070706130002285000000001002600007300000";

    /// <summary>
    /// Puzzle 412 after every naked and hidden single has been placed (21 placements).
    /// Singles alone get no further: the next move is the naked pair 5/9 in box 6.
    /// </summary>
    public const string Puzzle412Stuck =
        "052006431013520769670013825520067103130200076706130002285600300301002600067300200";

    public const string Puzzle412Solution =
        "852796431413528769679413825528967143134285976796134582285679314341852697967341258";

    public static Board Stuck412() => Board.Parse(Puzzle412, Puzzle412Stuck);

    /// <summary>Cell index from one-based row and column, matching the "r5c7" names players see.</summary>
    public static int Cell(int row, int column) => (row - 1) * 9 + (column - 1);
}
