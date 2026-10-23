using SudokuCoach.Core.Generation;
using SudokuCoach.Core.Solving;
using SudokuCoach.Core.Techniques;
using static SudokuCoach.Core.Tests.TestPuzzles;

namespace SudokuCoach.Core.Tests;

public class SolverTests
{
    private static readonly IReadOnlySet<Technique> Singles = TechniqueInfo.Beginner;

    [Fact]
    public void Singles_alone_reach_the_stuck_position_in_21_steps()
    {
        var result = HumanSolver.Solve(Board.Parse(Puzzle412), Singles);

        Assert.False(result.Solved);
        Assert.False(result.Contradiction);
        Assert.Equal(21, result.Steps.Count);
        Assert.Equal(Puzzle412Stuck, result.Final.ToString());
    }

    [Fact]
    public void Puzzle_412_needs_more_than_the_current_techniques()
    {
        var result = HumanSolver.Solve(Board.Parse(Puzzle412));

        Assert.False(result.Solved);
        Assert.Contains(Technique.NakedPair, result.TechniquesUsed);
        Assert.True(result.Final.IsValid);
    }

    [Fact]
    public void Every_digit_the_human_solver_places_is_correct()
    {
        var solution = Board.Parse(Puzzle412Solution);
        var final = HumanSolver.Solve(Board.Parse(Puzzle412)).Final;

        for (var c = 0; c < 81; c++)
        {
            if (!final.IsEmpty(c))
            {
                Assert.Equal(solution[c], final[c]);
            }
        }
    }

    [Fact]
    public void Solves_an_easy_puzzle_with_singles_only()
    {
        var easy = PuzzleGenerator.Generate(Difficulty.Easy, seed: 7);
        var result = HumanSolver.Solve(easy.Puzzle, Singles);

        Assert.True(result.Solved);
        Assert.Equal(easy.Solution.ToString(), result.Final.ToString());
    }

    [Fact]
    public void Generator_is_deterministic_for_a_seed()
    {
        var a = PuzzleGenerator.Generate(Difficulty.Medium, seed: 42);
        var b = PuzzleGenerator.Generate(Difficulty.Medium, seed: 42);

        Assert.Equal(a.Puzzle.ToString(), b.Puzzle.ToString());
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Medium)]
    [InlineData(Difficulty.Hard)]
    public void Generated_puzzles_are_unique_and_graded_correctly(Difficulty difficulty)
    {
        var generated = PuzzleGenerator.Generate(difficulty, seed: 7);
        var result = HumanSolver.Solve(generated.Puzzle);

        Assert.Equal(1, BruteForceSolver.CountSolutions(generated.Puzzle));
        Assert.True(generated.Solution.IsSolved);
        Assert.True(result.Solved);
        Assert.Equal(generated.Hardest, result.Hardest);
        Assert.Equal(difficulty, PuzzleGenerator.Classify(generated.Hardest));
    }

    [Fact]
    public void Random_solutions_are_valid_full_grids()
    {
        var generator = new PuzzleGenerator(123);

        for (var i = 0; i < 20; i++)
        {
            Assert.True(generator.RandomSolution().IsSolved);
        }
    }
}
