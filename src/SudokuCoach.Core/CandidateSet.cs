using System.Numerics;

namespace SudokuCoach.Core;

/// <summary>
/// An immutable set of the digits 1–9, stored as a bitmask (bit <c>d</c> set means digit <c>d</c> is present).
/// Used for cell candidates, player notes and the digits of a pattern.
/// </summary>
public readonly record struct CandidateSet(ushort Bits)
{
    /// <summary>Bits 1 through 9 set.</summary>
    public const ushort AllBits = 0b11_1111_1110;

    public static CandidateSet Empty => new(0);

    public static CandidateSet All => new(AllBits);

    public static CandidateSet Of(params int[] digits)
    {
        ushort bits = 0;
        foreach (var d in digits)
        {
            CheckDigit(d);
            bits |= (ushort)(1 << d);
        }
        return new CandidateSet(bits);
    }

    public int Count => BitOperations.PopCount(Bits);

    public bool IsEmpty => Bits == 0;

    public bool Contains(int digit) => digit is >= 1 and <= 9 && (Bits & (1 << digit)) != 0;

    public CandidateSet With(int digit)
    {
        CheckDigit(digit);
        return new CandidateSet((ushort)(Bits | (1 << digit)));
    }

    public CandidateSet Without(int digit)
    {
        CheckDigit(digit);
        return new CandidateSet((ushort)(Bits & ~(1 << digit)));
    }

    public CandidateSet Union(CandidateSet other) => new((ushort)(Bits | other.Bits));

    public CandidateSet Intersect(CandidateSet other) => new((ushort)(Bits & other.Bits));

    public CandidateSet Except(CandidateSet other) => new((ushort)(Bits & ~other.Bits));

    /// <summary>The only digit in the set. Throws unless <see cref="Count"/> is 1.</summary>
    public int Single => Count == 1
        ? BitOperations.TrailingZeroCount(Bits)
        : throw new InvalidOperationException($"Set {{{this}}} does not hold exactly one digit.");

    /// <summary>The digits in ascending order.</summary>
    public IEnumerable<int> Digits
    {
        get
        {
            for (var d = 1; d <= 9; d++)
            {
                if (Contains(d))
                {
                    yield return d;
                }
            }
        }
    }

    public override string ToString() => string.Concat(Digits);

    private static void CheckDigit(int digit)
    {
        if (digit is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "Digits run from 1 to 9.");
        }
    }
}
