using SudokuCoach.Core.Techniques;
using static SudokuCoach.Core.Tests.TestPuzzles;

namespace SudokuCoach.Core.Tests;

public class CoachReportTests
{
    private static readonly IReadOnlySet<Technique> SinglesAndPairs = new HashSet<Technique>
    {
        Technique.NakedSingle, Technique.HiddenSingle, Technique.NakedPair, Technique.HiddenPair,
    };

    private static CoachReport ReportAtStuck(IReadOnlySet<Technique> learned) =>
        CoachReport.For(Stuck412(), Stuck412().ComputeCandidates(), learned);

    [Fact]
    public void Naked_and_hidden_pair_are_one_effect()
    {
        var report = ReportAtStuck(SinglesAndPairs);

        Assert.Equal(1, report.CountOf(Technique.NakedPair));
        Assert.Equal(1, report.CountOf(Technique.HiddenPair));
        Assert.Equal(1, report.LearnedEffectCount);
        var group = Assert.Single(report.Groups, g => g.Techniques.Contains(Technique.NakedPair));
        Assert.Contains(Technique.HiddenPair, group.Techniques);
        Assert.Equal(Technique.NakedPair, group.Easiest.Technique);
    }

    [Fact]
    public void Twelve_moves_collapse_to_six_distinct_effects()
    {
        var report = ReportAtStuck(SinglesAndPairs);

        Assert.Equal(12, report.Moves.Count);
        Assert.Equal(6, report.Groups.Count);
    }

    [Fact]
    public void Unlearned_moves_already_covered_by_the_pair_are_not_new()
    {
        var report = ReportAtStuck(SinglesAndPairs);

        // Pointing 5 and 9 (box 9) and box-line 5 and 9 (column 7) only repeat the pair's eliminations.
        Assert.DoesNotContain(report.NewToYou, m => m.PatternDigits == CandidateSet.Of(5));
        Assert.DoesNotContain(report.NewToYou, m => m.PatternDigits == CandidateSet.Of(9));
        Assert.Equal(6, report.NewToYou.Count);
        Assert.Equal(3, report.NewToYouEffectCount);
    }

    [Fact]
    public void Suggests_pointing_as_the_next_technique()
    {
        Assert.Equal(Technique.Pointing, ReportAtStuck(SinglesAndPairs).NextTechniqueToLearn);
    }

    [Fact]
    public void A_beginner_sees_everything_as_new()
    {
        var report = ReportAtStuck(TechniqueInfo.Beginner);

        Assert.Empty(report.LearnedMoves);
        Assert.Equal(report.Moves.Count, report.NewToYou.Count);
        Assert.Equal(Technique.NakedPair, report.NextTechniqueToLearn);
    }

    [Fact]
    public void Knowing_every_technique_leaves_nothing_new()
    {
        var report = ReportAtStuck(TechniqueInfo.ByDifficulty.ToHashSet());

        Assert.Empty(report.NewToYou);
        Assert.Null(report.NextTechniqueToLearn);
    }

    [Fact]
    public void Placements_by_learned_singles_cover_unlearned_eliminations_in_that_cell()
    {
        // At the start of Puzzle 412 singles are available; an elimination in a cell that a
        // learned single fills is not progress the player is missing.
        var puzzle = Board.Parse(Puzzle412);
        var report = CoachReport.For(puzzle, puzzle.ComputeCandidates(), TechniqueInfo.Beginner);
        var filled = report.LearnedMoves.Where(m => m.Placement is not null).Select(m => m.Placement!.Value.Cell).ToHashSet();

        Assert.NotEmpty(filled);
        Assert.All(report.NewToYou, m => Assert.False(m.Eliminations.All(e => filled.Contains(e.Cell))));
    }
}
