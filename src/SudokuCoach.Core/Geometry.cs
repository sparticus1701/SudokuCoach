namespace SudokuCoach.Core;

public enum HouseKind
{
    Row,
    Column,
    Box,
}

/// <summary>A row, column or box: nine cells that must hold each digit once.</summary>
public sealed class House
{
    internal House(HouseKind kind, int index, int[] cells)
    {
        Kind = kind;
        Index = index;
        Cells = cells;
    }

    public HouseKind Kind { get; }

    /// <summary>Zero-based index (row 0–8, column 0–8, box 0–8 reading left to right, top to bottom).</summary>
    public int Index { get; }

    public IReadOnlyList<int> Cells { get; }

    /// <summary>Player-facing name, one-based: "row 4", "column 8", "box 6".</summary>
    public string Name => Kind switch
    {
        HouseKind.Row => $"row {Index + 1}",
        HouseKind.Column => $"column {Index + 1}",
        _ => $"box {Index + 1}",
    };

    public bool Contains(int cell) => Kind switch
    {
        HouseKind.Row => Geometry.RowOf(cell) == Index,
        HouseKind.Column => Geometry.ColumnOf(cell) == Index,
        _ => Geometry.BoxOf(cell) == Index,
    };

    public override string ToString() => Name;
}

/// <summary>
/// Board geometry. Cells are numbered 0–80 in reading order (cell = row * 9 + column).
/// </summary>
public static class Geometry
{
    private static readonly int[][] PeerTable = BuildPeers();

    static Geometry()
    {
        Rows = Enumerable.Range(0, 9)
            .Select(r => new House(HouseKind.Row, r, Enumerable.Range(0, 9).Select(c => r * 9 + c).ToArray()))
            .ToArray();
        Columns = Enumerable.Range(0, 9)
            .Select(c => new House(HouseKind.Column, c, Enumerable.Range(0, 9).Select(r => r * 9 + c).ToArray()))
            .ToArray();
        Boxes = Enumerable.Range(0, 9)
            .Select(b => new House(HouseKind.Box, b, BoxCells(b)))
            .ToArray();
        Houses = Rows.Concat(Columns).Concat(Boxes).ToArray();
    }

    public static IReadOnlyList<House> Rows { get; }

    public static IReadOnlyList<House> Columns { get; }

    public static IReadOnlyList<House> Boxes { get; }

    /// <summary>All 27 houses: rows, then columns, then boxes.</summary>
    public static IReadOnlyList<House> Houses { get; }

    public static int RowOf(int cell) => cell / 9;

    public static int ColumnOf(int cell) => cell % 9;

    public static int BoxOf(int cell) => cell / 27 * 3 + cell % 9 / 3;

    public static House RowHouse(int cell) => Rows[RowOf(cell)];

    public static House ColumnHouse(int cell) => Columns[ColumnOf(cell)];

    public static House BoxHouse(int cell) => Boxes[BoxOf(cell)];

    /// <summary>The 20 cells that share a row, column or box with <paramref name="cell"/>.</summary>
    public static IReadOnlyList<int> Peers(int cell) => PeerTable[cell];

    public static bool AreSeen(int a, int b) =>
        a != b && (RowOf(a) == RowOf(b) || ColumnOf(a) == ColumnOf(b) || BoxOf(a) == BoxOf(b));

    /// <summary>Player-facing cell name, one-based: "r5c7".</summary>
    public static string CellName(int cell) => $"r{RowOf(cell) + 1}c{ColumnOf(cell) + 1}";

    private static int[] BoxCells(int box)
    {
        var top = box / 3 * 3;
        var left = box % 3 * 3;
        var cells = new int[9];
        var i = 0;
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                cells[i++] = (top + r) * 9 + left + c;
            }
        }
        return cells;
    }

    private static int[][] BuildPeers()
    {
        var table = new int[81][];
        for (var cell = 0; cell < 81; cell++)
        {
            var peers = new List<int>(20);
            for (var other = 0; other < 81; other++)
            {
                if (other != cell &&
                    (RowOf(other) == RowOf(cell) || ColumnOf(other) == ColumnOf(cell) || BoxOf(other) == BoxOf(cell)))
                {
                    peers.Add(other);
                }
            }
            table[cell] = peers.ToArray();
        }
        return table;
    }
}
