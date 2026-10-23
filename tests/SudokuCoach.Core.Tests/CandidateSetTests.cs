namespace SudokuCoach.Core.Tests;

public class CandidateSetTests
{
    [Fact]
    public void Of_builds_the_set_and_reports_count_and_digits()
    {
        var set = CandidateSet.Of(9, 4, 8);

        Assert.Equal(3, set.Count);
        Assert.Equal(new[] { 4, 8, 9 }, set.Digits.ToArray());
        Assert.Equal("489", set.ToString());
        Assert.True(set.Contains(8));
        Assert.False(set.Contains(5));
    }

    [Fact]
    public void Set_operations_combine_as_expected()
    {
        var a = CandidateSet.Of(4, 5, 8, 9);
        var b = CandidateSet.Of(5, 9);

        Assert.Equal(CandidateSet.Of(4, 8), a.Except(b));
        Assert.Equal(b, a.Intersect(b));
        Assert.Equal(a, b.Union(CandidateSet.Of(4, 8)));
        Assert.Equal(CandidateSet.Of(4, 8, 9), a.Without(5));
        Assert.Equal(a, CandidateSet.Of(4, 8, 9).With(5));
    }

    [Fact]
    public void Single_returns_the_only_digit_and_throws_otherwise()
    {
        Assert.Equal(7, CandidateSet.Of(7).Single);
        Assert.Throws<InvalidOperationException>(() => CandidateSet.Of(1, 2).Single);
        Assert.Throws<InvalidOperationException>(() => CandidateSet.Empty.Single);
    }

    [Fact]
    public void All_holds_nine_digits_and_rejects_out_of_range_digits()
    {
        Assert.Equal(9, CandidateSet.All.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => CandidateSet.Of(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CandidateSet.All.Without(10));
        Assert.False(CandidateSet.All.Contains(0));
    }
}
