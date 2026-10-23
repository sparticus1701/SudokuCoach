# Sudoku Coach — notes for Claude Code

## What this is

A .NET MAUI sudoku app that teaches technique. The design concept (four screens: play with coach
panel, staged hints, post-game report, technique tree) was mocked up before any code; this repo is
the first implementation.

## Architecture rules

- **All game logic lives in `SudokuCoach.Core`** and is unit tested. The MAUI app only adapts it for
  binding and drawing. If you're about to put a rule in a view model, put it in Core instead.
- **`Move` is the shared currency.** Counting, deduping, hints and grading all read moves. A new
  technique = a new `ITechniqueFinder` + a `Technique` value + a rank in `TechniqueInfo` + tests.
- **Only productive moves.** Finders never return a pattern that eliminates nothing.
- **Cells are 0–80 in reading order**; player-facing names are one-based ("r5c7", "box 6") via
  `Geometry.CellName` and `House.Name`.
- **Coach candidates** (`GameSession.CoachCandidates`) narrow to the player's notes but always keep
  the correct digit, so a wrong note can never produce wrong advice.

## Reference position

`TestPuzzles.Puzzle412` / `Puzzle412Stuck`: after 21 singles, the board is stuck with one naked pair
(5/9, r5c7 & r6c7), its mirror hidden pair (4/8), 4 pointing, 4 box-line, 2 triple moves — 12 moves,
6 distinct effects. It is the example used in the mockups. Note that this puzzle needs a technique
beyond the current set to finish.

## Status (first pass)

- Engine and 48 tests: written and passing (verified with the tests run against a local stand-in for
  xUnit, since the authoring environment couldn't reach NuGet). Run `dotnet test` to confirm with the
  real xUnit.
- **The MAUI app has not been compiled yet.** Its C# was compile-checked against stand-ins for the
  MAUI types, and all XAML is well-formed, but the first real build may need small fixes — most
  likely candidates: `DisplayActionSheetAsync` (new in .NET 10; fall back to `DisplayActionSheet`),
  the `Keys[n].Remaining` indexer bindings, and the FlexLayout two-column coach rows.

## Next steps

1. Build and run on Windows and Android; fix anything the first build turns up.
2. Hint flow: four levels (technique → house → cells → eliminations), drawing highlights on the board.
3. Lesson flow for "Learn" (currently a placeholder that just marks the technique learned).
4. Persist learned techniques and per-technique spot rate (`Preferences` to start).
5. Post-game report: record stalls (no progress for N seconds while a move was available).
6. More techniques: X-wing, swordfish, XY-wing.

## Commands

```sh
dotnet test tests/SudokuCoach.Core.Tests
dotnet build src/SudokuCoach.App -f net10.0-windows10.0.19041.0 -t:Run
dotnet build src/SudokuCoach.App -f net10.0-android -t:Run
```
