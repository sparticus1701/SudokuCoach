# Sudoku Coach

A sudoku app that plays normally but teaches you to play better. It tells you what's on the board
("1 naked pair, 1 hidden pair"), gives hints in stages, and points you to the next technique worth
learning — counting the same deduction once even when it can be seen two ways.

## Layout

| Project | What it is |
| --- | --- |
| `src/SudokuCoach.Core` | The engine: board, candidates, technique finders, human-style solver, coach report, puzzle generator, game session. Plain `net10.0`, no UI. |
| `tests/SudokuCoach.Core.Tests` | xUnit tests for the engine. |
| `src/SudokuCoach.App` | .NET MAUI app (Android, iOS, Windows): play screen with board, keypad and coach panel. |

## Build and run

Requires the .NET 10 SDK and the MAUI workload (`dotnet workload install maui`).

```sh
dotnet test tests/SudokuCoach.Core.Tests

# Windows
dotnet build src/SudokuCoach.App -f net10.0-windows10.0.19041.0 -t:Run

# Android (emulator or device attached)
dotnet build src/SudokuCoach.App -f net10.0-android -t:Run
```

iOS builds need a Mac (or Visual Studio's pair-to-Mac). In Visual Studio, open `SudokuCoach.slnx`,
set `SudokuCoach.App` as the startup project, and pick a target from the run menu.

## The engine in one paragraph

A `Move` is one productive deduction: the technique, the pattern cells and digits, and its effect
(a placement or a list of eliminations). Finders produce moves; `HumanSolver` applies the easiest
move repeatedly and grades a puzzle by the hardest technique it needed. `CoachReport` groups moves by
`EffectKey`, so a naked pair and the hidden pair that mirrors it count as one deduction, and it
separates moves that need an unlearned technique *and* make progress the player can't already make.

## Techniques so far

Naked and hidden singles, naked and hidden pairs, pointing pairs and triples, box-line reduction,
naked and hidden triples. Next candidates: X-wing, swordfish, XY-wing, simple coloring.
