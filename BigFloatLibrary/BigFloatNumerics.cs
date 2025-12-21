// Copyright Ryan Scott White. 2020-2025
//
// Centralized helpers for core numeric decisions shared across BigFloat partials.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace BigFloatLibrary;

/// <summary>
/// Internal helpers for sizing mantissas and selecting algorithms.
/// </summary>
internal static class BigFloatNumerics
{
    /// <summary>
    /// When both operands reach roughly this many significant bits, Karatsuba
    /// multiplication outpaces schoolbook O(n^2) multiplication. The threshold is
    /// based on the smaller operand because Karatsuba's benefit appears once both
    /// halves of the split are large enough to amortize the extra additions.
    /// Tuned against the threshold sweep in docs/benchmarks/threshold-sweeps-NETCoreAppVersion-v80.md
    /// and docs/benchmarks/threshold-sweeps-NETCoreAppVersion-v90.md. Recent sweeps
    /// on developer hardware show the crossover closer to 8k bits, so we bias toward
    /// schoolbook until both operands are large enough to amortize the extra additions.
    /// </summary>
    public const int KARATSUBA_THRESHOLD = 8192;

    /// <summary>
    /// Burnikel–Ziegler division becomes more efficient than basic long division
    /// once either operand grows past this bit-length. The algorithm works in
    /// word-sized blocks; sweeps on .NET 8/9 (see docs/benchmarks/threshold-sweeps-*.md)
    /// show the quadratic shift/subtract curve starting to climb by ~512 bits, so
    /// we switch above that point to keep large divisions sub-quadratic for real-world inputs.
    /// </summary>
    public const int BURNIKEL_ZIEGLER_THRESHOLD = 512;

    /// <summary>
    /// Returns the bit-length of a mantissa using absolute value to keep
    /// power-of-two negatives consistent with positives. Returns 0 for zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MantissaSize(BigInteger value)
        => (int)BigInteger.Abs(value).GetBitLength();

    /// <summary>
    /// Determines whether Karatsuba multiplication should be preferred over the
    /// schoolbook algorithm given two operand sizes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseKaratsuba(int sizeA, int sizeB)
        => Math.Min(sizeA, sizeB) >= KARATSUBA_THRESHOLD;

    /// <summary>
    /// Determines whether Burnikel–Ziegler division should be preferred over the
    /// standard division implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseBurnikelZiegler(int numeratorSize, int denominatorSize)
        => numeratorSize >= BURNIKEL_ZIEGLER_THRESHOLD || denominatorSize >= BURNIKEL_ZIEGLER_THRESHOLD;
}
