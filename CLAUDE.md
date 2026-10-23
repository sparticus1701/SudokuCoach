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
- **Hints** (`Core/Game/Hint.cs`) are built from a `Move` one `HintLevel` at a time; each step
  carries its own text and board highlights, and `BoardDrawable` just draws them. `GameSession`
  owns the current hint: mistakes (wrong digits, then note errors) come before move hints, and a
  move hint survives edits only while the same deduction is still on the board.

## Reference position

`TestPuzzles.Puzzle412` / `Puzzle412Stuck`: after 21 singles, the board is stuck with one naked pair
(5/9, r5c7 & r6c7), its mirror hidden pair (4/8), 4 pointing, 4 box-line, 2 triple moves — 12 moves,
6 distinct effects. It is the example used in the mockups. Note that this puzzle needs a technique
beyond the current set to finish.

## Status

- Engine: 61 xUnit tests, all passing on the .NET 10 SDK.
- App: the shared code and all XAML compile with zero warnings against the real MAUI packages
  (10.0.110), with XAML compilation on — see the compile-check command below. That covers
  `DisplayActionSheetAsync`, the `Keys[n].Remaining` bindings and the coach rows.
- **Not yet verified:** a full platform build and a run on a device. The Linux build environment
  couldn't reach the Android SDK download, so Android packaging and the Windows target are untested,
  as is how the layout looks on screen.
- Hint flow is in: Hint button in the header, a hint card under the board with "Show more", and
  highlights on the board (house shaded, pattern cells outlined, eliminations struck out, the
  placement shown faintly).

## Next steps

1. Build and run on Windows and Android; fix anything the first build turns up. Check the header
   fits on a narrow phone now that it has four items (New, title, Hint, Fill notes).
2. Lesson flow for "Learn" (currently a placeholder that just marks the technique learned).
3. Persist learned techniques and per-technique spot rate (`Preferences` to start).
4. Post-game report: record stalls (no progress for N seconds while a move was available) and
   hints used (`GameSession.HintsUsed` counts them already).
5. More techniques: X-wing, swordfish, XY-wing. Each needs a level-3 sentence in `Hint.CellsText`.

## Commands

```sh
dotnet test tests/SudokuCoach.Core.Tests

# Compile-check the app's shared code and XAML on any OS, no platform workloads or SDKs needed
dotnet build src/SudokuCoach.App -p:TargetFrameworks=net10.0 -p:OutputType=Library

dotnet build src/SudokuCoach.App -f net10.0-windows10.0.19041.0 -t:Run
dotnet build src/SudokuCoach.App -f net10.0-android -t:Run
```
