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
    /// Determines whether Burnikel–Ziegler division should be preferred over the
    /// standard division implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUseBurnikelZiegler(int numeratorSize, int denominatorSize)
        => numeratorSize >= BURNIKEL_ZIEGLER_THRESHOLD || denominatorSize >= BURNIKEL_ZIEGLER_THRESHOLD;
}
