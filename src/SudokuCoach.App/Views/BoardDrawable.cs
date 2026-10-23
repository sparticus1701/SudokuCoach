using SudokuCoach.Core.Game;
using SudokuCoach.ViewModels;
using CandidateSet = SudokuCoach.Core.CandidateSet;
using Geometry = SudokuCoach.Core.Geometry;

namespace SudokuCoach.Views;

/// <summary>
/// Draws the grid: cell shading for the selection, digits (givens bold, the player's in blue,
/// conflicts in orange), pencil marks, and box lines. A hint on screen adds its highlights in
/// orange: the house shaded, the pattern cells outlined, eliminated candidates struck out and the
/// placement shown faintly. The board is a square from the top-left corner, sized to the smaller
/// side, so taps map back with the same arithmetic.
/// </summary>
public sealed class BoardDrawable : IDrawable
{
    private static readonly Color Surface = Color.FromArgb("#FFFFFF");
    private static readonly Color Ink = Color.FromArgb("#14202E");
    private static readonly Color Muted = Color.FromArgb("#4B5768");
    private static readonly Color Accent = Color.FromArgb("#2563EB");
    private static readonly Color Warm = Color.FromArgb("#C2410C");
    private static readonly Color ThinLine = Color.FromArgb("#C9D1DC");
    private static readonly Color SelectedFill = Color.FromArgb("#C7D8FA");
    private static readonly Color PeerFill = Color.FromArgb("#EEF3FB");
    private static readonly Color SameDigitFill = Color.FromArgb("#DBE7FE");
    private static readonly Color HintHouseFill = Color.FromArgb("#FFF1E6");
    private static readonly Color HintFocusFill = Color.FromArgb("#FFDCC2");
    private static readonly Color GhostDigit = Color.FromArgb("#C2410C").WithAlpha(0.6f);

    private readonly PlayViewModel _viewModel;

    public BoardDrawable(PlayViewModel viewModel) => _viewModel = viewModel;

    /// <summary>The cell under a point, or null when the point is off the board.</summary>
    public static int? CellAt(PointF point, float width, float height)
    {
        var size = Math.Min(width, height);
        if (size <= 0 || point.X < 0 || point.Y < 0 || point.X >= size || point.Y >= size)
        {
            return null;
        }
        var cellSize = size / 9f;
        var row = Math.Min(8, (int)(point.Y / cellSize));
        var column = Math.Min(8, (int)(point.X / cellSize));
        return row * 9 + column;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var cellSize = size / 9f;

        canvas.FillColor = Surface;
        canvas.FillRectangle(0, 0, size, size);

        var game = _viewModel.Game;
        if (game is null)
        {
            canvas.FontColor = Muted;
            canvas.FontSize = 16;
            canvas.DrawString("Making a puzzle…", 0, 0, size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
            DrawLines(canvas, size, cellSize);
            return;
        }

        var selected = game.Selected;
        var selectedDigit = selected is { } s ? game.Board[s] : 0;

        var hint = game.CurrentHint;
        var hintHouse = hint?.HouseCells.ToHashSet() ?? [];
        var hintFocus = hint?.FocusCells.ToHashSet() ?? [];
        var patternDigits = hint?.Move?.PatternDigits ?? CandidateSet.Empty;
        var struck = new Dictionary<int, CandidateSet>();
        foreach (var e in hint?.Eliminations ?? [])
        {
            struck[e.Cell] = struck.GetValueOrDefault(e.Cell).With(e.Digit);
        }

        for (var cell = 0; cell < 81; cell++)
        {
            var x = Geometry.ColumnOf(cell) * cellSize;
            var y = Geometry.RowOf(cell) * cellSize;

            Color? fill = null;
            if (cell == selected)
            {
                fill = SelectedFill;
            }
            else if (hintFocus.Contains(cell))
            {
                fill = HintFocusFill;
            }
            else if (hintHouse.Contains(cell))
            {
                fill = HintHouseFill;
            }
            else if (selectedDigit != 0 && game.Board[cell] == selectedDigit)
            {
                fill = SameDigitFill;
            }
            else if (selected is { } sel && Geometry.AreSeen(sel, cell))
            {
                fill = PeerFill;
            }
            if (fill is not null)
            {
                canvas.FillColor = fill;
                canvas.FillRectangle(x, y, cellSize, cellSize);
            }

            var value = game.Board[cell];
            if (value != 0)
            {
                var given = game.Board.IsGiven(cell);
                canvas.Font = given ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
                canvas.FontColor = given ? Ink : game.HasConflict(cell) ? Warm : Accent;
                canvas.FontSize = cellSize * 0.55f;
                canvas.DrawString(value.ToString(), x, y, cellSize, cellSize, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            else if (hint?.Placement is { } placement && placement.Cell == cell)
            {
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                canvas.FontColor = GhostDigit;
                canvas.FontSize = cellSize * 0.55f;
                canvas.DrawString(placement.Digit.ToString(), x, y, cellSize, cellSize, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            else
            {
                // Struck candidates are drawn even if the player never noted them, so the hint reads on its own.
                var cellStruck = struck.GetValueOrDefault(cell);
                var marks = game.Notes[cell].Union(cellStruck);
                if (marks.IsEmpty)
                {
                    continue;
                }
                var focus = hintFocus.Contains(cell);
                var sub = cellSize / 3f;
                canvas.FontSize = cellSize * 0.24f;
                foreach (var d in marks.Digits)
                {
                    var nx = x + (d - 1) % 3 * sub;
                    var ny = y + (d - 1) / 3 * sub;
                    var isStruck = cellStruck.Contains(d);
                    var isPattern = focus && patternDigits.Contains(d);
                    canvas.Font = isStruck || isPattern ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
                    canvas.FontColor = isStruck ? Warm : isPattern ? Accent : Muted;
                    canvas.DrawString(d.ToString(), nx, ny, sub, sub, HorizontalAlignment.Center, VerticalAlignment.Center);
                    if (isStruck)
                    {
                        canvas.StrokeColor = Warm;
                        canvas.StrokeSize = 1.5f;
                        canvas.DrawLine(nx + sub * 0.2f, ny + sub * 0.8f, nx + sub * 0.8f, ny + sub * 0.2f);
                    }
                }
            }
        }

        DrawLines(canvas, size, cellSize);
        DrawFocus(canvas, hintFocus, cellSize);
    }

    /// <summary>Outlines the cells a hint is pointing at, on top of the grid lines.</summary>
    private static void DrawFocus(ICanvas canvas, IEnumerable<int> cells, float cellSize)
    {
        canvas.StrokeColor = Warm;
        canvas.StrokeSize = 2.5f;
        foreach (var cell in cells)
        {
            var x = Geometry.ColumnOf(cell) * cellSize;
            var y = Geometry.RowOf(cell) * cellSize;
            canvas.DrawRoundedRectangle(x + 2.5f, y + 2.5f, cellSize - 5f, cellSize - 5f, 4f);
        }
    }

    private static void DrawLines(ICanvas canvas, float size, float cellSize)
    {
        canvas.StrokeColor = ThinLine;
        canvas.StrokeSize = 1;
        for (var i = 1; i < 9; i++)
        {
            if (i % 3 == 0)
            {
                continue;
            }
            canvas.DrawLine(i * cellSize, 0, i * cellSize, size);
            canvas.DrawLine(0, i * cellSize, size, i * cellSize);
        }

        canvas.StrokeColor = Ink;
        canvas.StrokeSize = 2;
        for (var i = 0; i <= 9; i += 3)
        {
            var p = Math.Clamp(i * cellSize, 1f, size - 1f);
            canvas.DrawLine(p, 0, p, size);
            canvas.DrawLine(0, p, size, p);
        }
    }
}
