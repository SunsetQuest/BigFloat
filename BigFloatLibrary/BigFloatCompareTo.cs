// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

/* ===================== BigFloat Equality and Comparison Guidance =====================
Overview
BigFloat exposes multiple notions of equality and ordering. Choose based on intent:
  - Canonical value equality/order for .NET semantics and hashing: CompareTo,Equals
  - ULP/tolerance comparisons for numeric analysis: CompareUlp, EqualsUlp, Is*Ulp
  - Deterministic total order including guard bits: CompareTotalOrder
  - Representation equality (bitwise) and value equality with zero-extension: IsBitwiseEqual, IsEqualWithZeroExtension

Canonical value semantics  (IComparable/IEquatable-compliant)
 * int CompareTo(BigFloat other)
    - Canonicalizes both operands (rounds guard bits, handles carry) then compares.
    - Aligns with GetHashCode() and Equals(BigFloat).
    - Use when you need .NET-consistent ordering, dictionary keys, or set membership.

 * bool Equals(BigFloat other)
    - Equivalent to CompareTo(other) == 0, slightly optimized.
    - Use for canonical value equality in general .NET code.

ULP/tolerance semantics  (precision-aware numerical work)
 * int  CompareUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
 * bool EqualsUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
 * bool IsGreaterThanUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
 * bool IsLessThanUlp(BigFloat other,  int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
 * bool IsGreaterThanOrEqualToUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
 * bool IsLessThanOrEqualToUlp(BigFloat other,  int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
    - Aligns scales, performs rounded shift and subtract, then compares against a tolerance.
    - Use for physics-style comparisons, algorithmic stopping criteria, and “close enough” tests.
    - Set ulpScopeIncludeGuardBits=true to count guard bits in the tolerance window.

Deterministic total order  (fast, guard bits included)
 * int CompareTotalOrder(BigFloat other)
    - No canonical rounding; includes guard bits; stable total order; faster than CompareTo.
    - Use for sorting/ordering when you must distinguish values that CompareTo would coalesce,
      or when you need speed and a strict total order.

Representation and encoding semantics
 * bool IsBitwiseEqual(BigFloat other)
    - Exact identity of internal encoding: mantissa, scale, precision, and sign all match.
    - Use for caches, serialization validation, and low-level invariants.

 * bool IsEqualWithZeroExtension(BigFloat other)    // aka “value equality with precision alignment”
    - Values must match exactly when the smaller precision side is zero-extended to the larger.
    - Example: 2.5 == 2.50000 after aligning precision by appending zeros.
    - Use when precision context differs across pipelines but trailing zeros are semantically inert.

Operators (==, !=, <, <=, >, >=)
 * These map to Equals/CompareTo to remain IEquatable/IComparable compliant.
 * They do NOT apply ULP tolerances and do NOT include guard bits beyond canonicalization.
 * For numeric workflows with tolerances, prefer the ULP helpers above.
   Examples:
     if (a.EqualsUlp(b, ulps)) { ... }        // instead of a == b
     if (a.IsLessThanOrEqualToUlp(b, ulps)) { ... }  // instead of a <= b

Quick selector
  End-user equality in general .NET code ............ Equals (or ==)
  Hash keys / sets / canonical ordering ............. CompareTo / Equals / GetHashCode
  Sort with strict total order + speed .............. CompareTotalOrder
  Tolerant numeric comparisons (ULP/epsilon-like) ... CompareUlp / EqualsUlp / Is*Ulp
  Bit-for-bit internal identity ..................... IsBitwiseEqual
  Cross-precision exact value match ................. IsEqualWithZeroExtension

Notes
 * CompareTo/Equals perform canonicalization; CompareTotalOrder does not.
 * GuardBits affect CompareTotalOrder but are rounded away for CompareTo/Equals.
 * Be explicit with ULP scope: include guard bits only when that is intended.
===================================================================================== */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using static BigFloatLibrary.BigIntegerTools;
using static BigFloatLibrary.BigFloatNumerics;

namespace BigFloatLibrary;

public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{
    #region Core Comparison Infrastructure

    private (BigInteger Mant, int Scale, int Size) GetCanonicalComponents()
    {
        if (_mantissa.IsZero) return (BigInteger.Zero, 0, 0);

        // Drop/round the guard area to the nearest using the library's rule.
        BigInteger roundedMain = RoundingRightShift(_mantissa, GuardBits); // removes guard bits with rounding

        // Detect carry that adds a new top bit after rounding (e.g., 0111.. -> 1000..).
        int oldMainSize = Math.Max(0, _size - GuardBits);
        int newMainSize = MantissaSize(roundedMain);

        int scale = Scale;
        if (newMainSize > oldMainSize && oldMainSize != 0)
        {
            bool neg = roundedMain.Sign < 0;
            BigInteger mag = BigInteger.Abs(roundedMain) >> 1;
            roundedMain = neg ? -mag : mag;
            newMainSize--;
            scale++;
        }

        // Reattach a zero guard area to keep internal invariants (_size includes GuardBits).
        BigInteger canonMant = roundedMain << GuardBits;
        int size = newMainSize == 0 ? 0 : newMainSize + GuardBits;
        return (canonMant, scale, size);
    }

    // Compare with hybrid alignment on Size to avoid gigantic shifts.
    // Returns sign of (a-b) assuming both are nonzero and same sign.
    private static int CmpAligned(
        in (BigInteger Mant, int Scale, int Size) a,
        in (BigInteger Mant, int Scale, int Size) b)
    {
        // Use effective exponent based on MAIN size (exclude GuardBits).
        // a.Size/b.Size here include GuardBits unless zero; subtract them out.
        int aMain = a.Size == 0 ? 0 : a.Size - BigFloat.GuardBits;
        int bMain = b.Size == 0 ? 0 : b.Size - BigFloat.GuardBits;

        long e1 = (long)a.Scale + aMain;
        long e2 = (long)b.Scale + bMain;
        if (e1 != e2) return e1 < e2 ? -1 : 1;

        BigInteger am = BigInteger.Abs(a.Mant);
        BigInteger bm = BigInteger.Abs(b.Mant);

        if (a.Size == b.Size) return am.CompareTo(bm);

        int d = Math.Abs(a.Size - b.Size);
        const int SMALL_SHIFT_BITS = 128;

        if (a.Size > b.Size)
        {
            if (d <= SMALL_SHIFT_BITS) return am.CompareTo(bm << d);

            BigInteger aHi = am >> d;
            int cmp = aHi.CompareTo(bm);
            if (cmp != 0) return cmp;
            bool sticky = (aHi << d) != am;
            return sticky ? 1 : 0;
        }
        else
        {
            if (d <= SMALL_SHIFT_BITS) return (am << d).CompareTo(bm);

            BigInteger bHi = bm >> d;
            int cmp = am.CompareTo(bHi);
            if (cmp != 0) return cmp;
            bool sticky = (bHi << d) != bm;
            return sticky ? -1 : 0;
        }
    }
    #endregion

    #region Primary Comparison Methods
    /// <summary>
    /// Standard numeric comparison (IComparable).
    /// Fast path: if the raw effective exponents differ by ≥ 2 (taking sign into account),
    /// we can decide without paying the cost of canonicalization. Otherwise, we fall back
    /// to the canonicalized alignment/compare path to preserve exact semantics.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(BigFloat other)
    {
        // 0) Sign checks (no allocations; matches existing semantics).
        int s1 = _mantissa.Sign;
        int s2 = other._mantissa.Sign;
        if (s1 != s2) return s1 < s2 ? -1 : 1;
        if (s1 == 0) return 0; // both zero encodings compare equal

        // 1) Raw effective exponent (no rounding): Scale + (main-size).
        //    _size is the mantissa bit-length INCLUDING guard bits.
        //    mainSize = max(0, _size - GuardBits); when _size==0 the value is zero (handled above).
        int aMainRaw = (_size > 0) ? (_size - GuardBits) : 0;
        int bMainRaw = (other._size > 0) ? (other._size - GuardBits) : 0;
        long e1Raw = (long)Scale + aMainRaw;
        long e2Raw = (long)other.Scale + bMainRaw;
        long de = e1Raw - e2Raw;

        // 2) Rounding guard bits can change the effective exponent by at most ±1.
        //    If |de| >= 2, the ordering cannot flip under canonicalization.
        if (de <= -2) return s1 > 0 ? -1 : 1;  // this is definitely smaller (or larger if negative)
        if (de >= +2) return s1 > 0 ? 1 : -1; // this is definitely larger (or smaller if negative)

        // 3) Close exponents (equal/adjacent): pay the canonicalization cost only now.
        var a = GetCanonicalComponents();   // rounds away guard bits, handles carry and scale adjust
        var b = other.GetCanonicalComponents();

        // Signs can’t change under canonicalization; compare aligned canonicals.
        int core = CmpAligned(a, b);
        return s1 > 0 ? core : -core;
    }

    /// <summary>
    /// Raw ULP-distance (after aligning scales via rounding right-shifts).
    /// Positive if this &gt; other, negative if this &lt; other, zero if aligned mantissas match.
    /// Intended for diagnostics and analysis, not ordering.
    /// </summary>
    public readonly BigInteger UlpDistance(BigFloat other)
    {
        BigInteger a = this._mantissa;
        BigInteger b = other._mantissa;
        int scaleDiff = other.Scale - this.Scale;
        if (scaleDiff > 0)
        {
            a = RoundingRightShift(a, scaleDiff);
        }
        else if (scaleDiff < 0)
        {
            b = RoundingRightShift(b, -scaleDiff);
        }

        return a - b;
    }


    /// <summary>
    /// ULP-aware comparison for numerical analysis and tolerancing.
    /// Compares after aligning scales, then ignores the specified number of least-significant bits
    /// (optionally counting or excluding guard bits).
    /// Returns -1/0/1 where 0 means |a - b| &lt; 2^{ulpTolerance} at the aligned scale.
    /// Not a strict weak ordering; do not use as a general sort comparer.
    /// </summary>
    /// <param name="other">Value to compare.</param>
    /// <param name="ulpTolerance">
    /// Units-in-the-last-place to ignore. 0 means exact at current precision. Negative values
    /// can reach into guard bits; set <paramref name="ulpScopeIncludeGuardBits"/> to control scope.
    /// </param>
    /// <param name="ulpScopeIncludeGuardBits">
    /// If true, the tolerance counts guard bits; if false, tolerance is applied to in-precision bits only.
    /// </param>
    public readonly int CompareUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
    {
        // Add guard bits internally - user specifies in terms of value precision
        // but can use negative values to be more strict
        int effectiveBitsToIgnore = ulpTolerance + (ulpScopeIncludeGuardBits ? 0 : GuardBits-1);
        BigInteger a = this._mantissa;
        BigInteger b = other._mantissa;

        int scaleDiff = other.Scale - this.Scale;
        if (scaleDiff > 0)
        {
            a = RoundingRightShift(a,scaleDiff);
        }
        else if (scaleDiff < 0)
        {
            b = RoundingRightShift(b,-scaleDiff);
        }

        if (a > b)
        {
            BigInteger diff = (a - b) >> (effectiveBitsToIgnore);
            return diff.Sign;
        }
        else
        {
            BigInteger diff = (b - a) >> (effectiveBitsToIgnore);
            return -diff.Sign;
        }
    }

    /// <summary>
    /// Fast approximate ULP comparison.
    /// Trades accuracy for speed by using coarse shifts and a small fixed window. Same intent as
    /// <see cref="CompareUlp(BigFloat, int, bool)"/> but with looser guarantees. Not suitable for sorting.
    /// </summary>
    public readonly int CompareUlpFast(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
    {
        // Add guard bits internally - user specifies in terms of value precision
        // but can use negative values to be more strict
        int effectiveBitsToIgnore = ulpTolerance + (ulpScopeIncludeGuardBits ? 0 : GuardBits - 1);
        BigInteger a = this._mantissa;
        BigInteger b = other._mantissa;

        int scaleDiff = other.Scale - this.Scale;
        int shiftA = effectiveBitsToIgnore - 4, shiftB = effectiveBitsToIgnore - 4;

        if (scaleDiff > 0)
        {
            shiftA += scaleDiff;
        }
        else
        {
            shiftB += scaleDiff;
        }

        BigInteger res = (a >> shiftA) - (b >> shiftB);

        if (res >= 0)
            return (res < 16) ? 0 : 1;
        else
            return (-res < 16) ? 0 : -1;   
    }


    /// <summary>
    /// Strict total order on the raw encoding (bitwise-aware).
    /// Returns 0 only for bitwise-identical values (same Scale and mantissa).
    /// Distinguishes numerically equal encodings such as 2.5 and 2.50.
    /// Order keys, in sequence:
    ///   1) sign (negatives < zeros < positives),
    ///   2) effective exponent (Scale + _size), sign-aware direction,
    ///   3) aligned magnitude, sign-aware direction,
    ///   4) tie-breakers for numerically equal values: first by Scale, then by mantissa.
    /// No rounding occurs; at most one shift is performed on one operand for alignment.
    /// Suitable for deterministic sorting and encoding-level de-duplication.
    /// Not consistent with <see cref="Equals(BigFloat)"/> (which canonicalizes); use only where
    /// equality is defined as bitwise identity (compare == 0).
    /// </summary>
    /// <returns>-1 if this precedes <paramref name="other"/>, 0 if bitwise-identical, 1 if follows.</returns>
    public readonly int CompareTotalOrderBitwise(in BigFloat other)
    {
        // Fast path: 0 only if bitwise-identical
        if (Scale == other.Scale && _mantissa == other._mantissa) return 0;

        // 1) Primary key: sign
        int s1 = _mantissa.Sign, s2 = other._mantissa.Sign;
        if (s1 != s2) return s1 < s2 ? -1 : 1;

        // Zeros: still here implies encodings differ; order zero encodings by Scale
        if (s1 == 0) // this == 0
            return other._mantissa.Sign == 0 ? Scale.CompareTo(other.Scale) : -1;
        if (s2 == 0) return 1;

        // 2) Effective exponent (Scale + bit-length), sign-aware
        long e1 = (long)Scale + _size;
        long e2 = (long)other.Scale + other._size;
        if (e1 != e2)
            return s1 > 0 ? (e1 < e2 ? -1 : 1) : (e1 < e2 ? 1 : -1);

        // 3) Same effective exponent: compare aligned magnitudes with a single shift
        var a = BigInteger.Abs(_mantissa);
        var b = BigInteger.Abs(other._mantissa);
        int shift = other.Scale - Scale; // align LSBs without copying

        int magCmp = shift >= 0 ? a.CompareTo(b << shift)
                                : (a << -shift).CompareTo(b);

        if (magCmp != 0) return s1 > 0 ? magCmp : -magCmp;

        // 4) Numerically equal but not bitwise-identical: deterministic tie-breakers
        int t = Scale.CompareTo(other.Scale);
        if (t != 0) return t;

        // Same scale, different mantissa encodings
        return _mantissa.CompareTo(other._mantissa);
    }

    /// <summary>
    /// Deterministic total-order comparison for sorting, without rounding.
    /// Orders by sign, then effective exponent (Scale + _size), then magnitude with strict alignment.
    /// When effective exponents match, the longer operand is right-shifted by the size difference
    /// before comparing. This makes values equal if the shorter operand is a zero-extension of the longer
    /// (e.g., 2.5 == 2.50).
    ///
    /// Properties:
    /// - Deterministic and total over all finite values with the same sign.
    /// - No rounding is performed; uses raw mantissas and sizes.
    /// - Representation differences that are zero-extensions do NOT affect ordering.
    /// - Suitable when distinct encodings of the same value should collapse in sort keys.
    /// - If you need a bitwise/encoding order that distinguishes 2.5 from 2.50, do not use this; use
    ///   <see cref="CompareTotalOrderBitwise(BigFloat)"/>.
    ///
    /// Note: Because numerically equal values compare as 0, unstable sorts may permute equal elements.
    /// Use a stable sort to preserve input order of ties.
    /// </summary>
    /// <returns>-1 if this < other, 0 if equal under zero-extension, 1 if this > other.</returns>
    public readonly int CompareTotalPreorder(BigFloat other)
    {
        int s1 = _mantissa.Sign, s2 = other._mantissa.Sign;
        if (s1 != s2) return s1 < s2 ? -1 : 1;
        if (s1 == 0) return 0;

        int e1 = Scale + _size;
        int e2 = other.Scale + other._size;
        if (e1 != e2) return e1 < e2 ? -1 : 1;

        // same sign, same effective exponent: use strict magnitude alignment

        int d = _size - other._size;

        if (d >= 0)
        {
            return (_mantissa >> d).CompareTo(other._mantissa);
        }
        else
        {
            return _mantissa.CompareTo(other._mantissa >> -d);
        }
    }

    /// <summary>
    /// Bitwise/encoding equality.
    /// True iff mantissa and scale are exactly identical. Distinguishes 2.5 from 2.50.
    /// </summary>
    public bool IsBitwiseEqual(BigFloat other) => other._mantissa == _mantissa && other.Scale == Scale;

    /// <summary>
    /// Equality under zero-extension at equal effective exponent.
    /// True iff signs match, (Scale + _size) matches, and the shorter precision zero-extends to the longer.
    /// Example: 2.5 == 2.50. Differs from <see cref="IsBitwiseEqual(BigFloat)"/>.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public bool EqualsZeroExtended(BigFloat other)
    {
        // Handle size differences by zero-extending the smaller precision
        int thisPos = _mantissa.Sign;
        int otherPos = other._mantissa.Sign;

        if (thisPos != otherPos) return false;
        if (thisPos == 0) return true;

        // Check if effective exponents match
        if ((Scale + _size) != (other.Scale + other._size)) return false;

        if (_size == other._size)
            return _mantissa == other._mantissa;

        if (_size < other._size)
        {
            BigInteger adjustedThis = _mantissa << (other._size - _size);
            return adjustedThis == other._mantissa;
        }
        else
        {
            BigInteger adjustedOther = other._mantissa << (_size - other._size);
            return _mantissa == adjustedOther;
        }
    }

    /// <summary>
    /// Static convenience for <see cref="CompareTo(BigFloat)"/>.
    /// </summary>
    public static int Compare(in BigFloat x, in BigFloat y) => x.CompareTo(y);

    /// <summary> 
    /// Compares two BigFloat's to make sure they are essentially the same value. Different precisions are allowed.
    /// The lower precision number is up-shifted with zero bits to match the higher precision number.
    /// Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// CompareTo() is more often used as it is used to compare the in-precision digits.
    /// This Function is faster then the CompareTo() as no rounding takes place.
    /// </summary>
    public static int CompareTotalOrderBitwise(in BigFloat x, in BigFloat y) => x.CompareTotalOrderBitwise(y);

    /// <summary>
    /// Static convenience for <see cref="CompareTotalPreorder(BigFloat)"/>.
    /// </summary>
    public static int CompareUlp(in BigFloat x, in BigFloat y, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => x.CompareUlp(y, ulpTolerance, ulpScopeIncludeGuardBits);

    #endregion

    #region Deprecated Methods
    [Obsolete("Use CompareTotalOrder() for deterministic ordering or IsExactMatchOf() for exact equality")]
    public int FullPrecisionCompareTo(BigFloat other)
    {
        return CompareTotalOrderBitwise(other);
    }

    [Obsolete("Use CompareUlp(other, 0) instead")]
    public int CompareInPrecisionBitsTo(BigFloat other)
    {
        return CompareUlp(other, 0);
    }

    [Obsolete("Use instance method CompareUlp instead")]
    public static int CompareToIgnoringLeastSigBits(BigFloat a, BigFloat b, int leastSignificantBitsToIgnore)
    {
        return a.CompareUlp(b, leastSignificantBitsToIgnore);
    }

    [Obsolete("Use instance method CompareUlp(other, 1, true) OR the more strict CompareTotalOrder(other) instead")]
    public int StrictCompareTo(BigFloat other) => CompareUlp(other, 1, true);

    #endregion

    #region IComparable Implementation

    /// <summary>
    /// Compares this instance to an object (IComparable).
    /// </summary>
    public int CompareTo(object obj) =>
        obj switch
        {
            null => 1,
            BigFloat bf => CompareTo(bf),
            BigInteger bi => CompareTo(bi),
            _ => throw new ArgumentException("Object is not a BigFloat")
        };

    /// <summary>
    /// Compares two values
    /// The guard bits are removed. 
    /// </summary>
    public int CompareTo(BigInteger bigInteger)
    {
        int thisSign = _mantissa.Sign;
        int otherSign = bigInteger.Sign;

        // A fast sign check.
        if (thisSign != otherSign)
        {
            return thisSign == 0 ? -otherSign : thisSign;
        }

        // If both are zero then they are equal.
        if (thisSign == 0) { return 0; }

        // A fast general size check.
        int bigIntegerSizeLessOne = MantissaSize(bigInteger) - 1;

        if (BinaryExponent != bigIntegerSizeLessOne)
        {
            return BinaryExponent.CompareTo(bigIntegerSizeLessOne) * thisSign;
        }

        // Future: Benchmark A and B
        // Option A:
        // At this point both items have the same exponent and sign. 
        //int bigIntLargerBy = (bigIntegerSizeLessOne+1) - _size;
        //return bigIntLargerBy switch
        //{
        //    0 => _mantissa.CompareTo(bigInteger),
        //    < 0 => (_mantissa << (bigIntegerSizeLessOne + 1) - _size).CompareTo(bigInteger),
        //    > 0 => _mantissa.CompareTo(bigInteger << _size - (bigIntegerSizeLessOne + 1))
        //};

        // Option B:
        return RoundingRightShift(_mantissa, -Scale + GuardBits).CompareTo(bigInteger);
    }

    #endregion

    #region IComparer Implementations

    /// <summary>
    /// IComparer that uses standard numeric value ordering (<see cref="CompareTo(BigFloat)"/>).
    /// Collapses representation differences that do not change the rounded value.
    /// Use when you want sorting by numeric value only; equal numbers tie.
    /// </summary>
    public sealed class ValueComparer : IComparer<BigFloat>
    {
        public static readonly ValueComparer Instance = new();
        private ValueComparer() { }
        public int Compare(BigFloat x, BigFloat y) => BigFloat.Compare(in x, in y);
    }

    /// <summary>
    /// IComparer that uses deterministic total order (<see cref="CompareTotalPreorder(BigFloat)"/>).
    /// No rounding. Equal effective exponents are compared after truncating the longer operand to the shorter size,
    /// so zero-extensions of the same value tie (e.g., 2.5 and 2.50 compare equal).
    /// Use for stable, deterministic sorting keys where different precisions of the same value should not reorder.
    /// </summary>
    public sealed class TotalOrderComparer : IComparer<BigFloat>
    {
        public static readonly TotalOrderComparer Instance = new();
        private TotalOrderComparer() { }
        public int Compare(BigFloat x, BigFloat y) => BigFloat.CompareTotalOrderBitwise(in x, in y);
    }

    /// <summary>
    /// IComparer that treats values within a ULP tolerance as equal (<see cref="CompareUlp(BigFloat, int, bool)"/>).
    /// Useful for numerics where small last-place differences are noise. Not a strict weak ordering; avoid for
    /// data structures requiring transitive comparators (e.g., SortedSet/SortedDictionary).
    /// </summary>
    public sealed class UlpToleranceComparer(int ulps, bool includeGuardBits = true) : IComparer<BigFloat>
    {
        private readonly int _ulps = ulps;
        private readonly bool _includeGuardBits = includeGuardBits;

        public int Compare(BigFloat x, BigFloat y) => CompareUlp(in x, in y, _ulps, _includeGuardBits);

        public static UlpToleranceComparer WithTolerance(int ulps, bool includeGuardBits = true) => new(ulps, includeGuardBits);
    }

    public sealed class BitwiseEqualityComparer : IEqualityComparer<BigFloat>
    {
        public static readonly BitwiseEqualityComparer Instance = new();
        public bool Equals(BigFloat x, BigFloat y) => x.IsBitwiseEqual(y);
        public int GetHashCode(BigFloat v) => HashCode.Combine(v.Scale, v._mantissa);
    }
    #endregion

    #region Convenience Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EqualsUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => CompareUlp(other, ulpTolerance, ulpScopeIncludeGuardBits) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsGreaterThanUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => CompareUlp(other, ulpTolerance, ulpScopeIncludeGuardBits) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLessThanUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => CompareUlp(other, ulpTolerance, ulpScopeIncludeGuardBits) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsGreaterThanOrEqualToUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => CompareUlp(other, ulpTolerance, ulpScopeIncludeGuardBits) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLessThanOrEqualToUlp(BigFloat other, int ulpTolerance = 0, bool ulpScopeIncludeGuardBits = false)
        => CompareUlp(other, ulpTolerance, ulpScopeIncludeGuardBits) <= 0;
    #endregion

    #region Equality Methods
    /// <summary> 
    /// Returns true if the parents BigFloat object have the same value (within the precision). 
    /// Examples: 1.11 == 1.1, 1.00 == 1.0, 1.11 != 1.10, 1.1 == 1.01 
    /// </summary> 
    public bool Equals(BigFloat other)// IEquatable<BigFloat>
    {
        var a = GetCanonicalComponents();
        var b = other.GetCanonicalComponents();

        if (a.Mant.Sign != b.Mant.Sign) return false;
        if (a.Mant.Sign == 0) return true;

        // same sign, both nonzero: value equality iff aligned compare is zero
        return CmpAligned(a, b) == 0;
    }


    [Obsolete("Use IsBitwiseEqual.")]
    public bool IsExactMatchOf(BigFloat other) => IsBitwiseEqual(other);

    public bool Equals(long other)
    {
        if (_mantissa.IsZero) return other == 0;

        // Align the ones place, then round away the guard field using the SAME rule
        // canonicalization uses (top-guard policy included).
        BigInteger aligned = (Scale >= 0) ? (_mantissa << Scale) : (_mantissa >> -Scale);
        BigInteger q = RoundingRightShift(aligned, GuardBits);   // signed integer units

        // Exact Int64 range test, no heuristics.
        // just do straight int32 test
        // if (q < (BigInteger)long.MinValue || q > (BigInteger)long.MaxValue) return false;

        return q == other;
    }

    public bool Equals(ulong other)
    {
        if (_mantissa.IsZero) return other == 0;
        if (BinaryExponent >= 64) { return false; }  // 'this' is too large, not possible to be equal.
        if (BinaryExponent < -1) { return other == 0; }

        // Align the ones place, then round away the guard field using the SAME rule
        // canonicalization uses (top-guard policy included).
        BigInteger aligned = (Scale >= 0) ? (_mantissa << Scale) : (_mantissa >> -Scale);
        BigInteger q = RoundingRightShift(aligned, GuardBits);   // signed integer units

        // Exact Int64 range test, no heuristics.
        // just do straight int32 test
        //if (q.GetBitLength() > 64) return false;

        return q == other;
    }

    public bool Equals(int other) => Equals((long)other);

    public bool Equals(uint other) => Equals((ulong)other);


    /// <summary>
    /// Returns true if the integer part of the BigFloat matches 'other'. 
    /// Examples:  1.1 == 1,  1.6 != 1,  0.6==1
    /// </summary>
    public bool RoundsToNearest(BigInteger other)
    {
        return other.Equals(RoundingRightShift(_mantissa, GuardBits - Scale));
    }

    [Obsolete("Use RoundsToNearest.")]
    public bool Equals(BigInteger other) => RoundsToNearest(other);

    /// <summary>
    /// Value equality after canonicalization (guard bits rounded away).
    /// Equivalent to <c>CompareTo(other) == 0</c>.
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        AssertValid();
        return obj is BigFloat other && Equals(other);
    }

    #endregion

    /// <summary>Returns true if the left side BigFloat is equal to the right side BigInteger.  If the BigFloat is not an integer then always returns false.</summary>
    public static bool operator ==(BigFloat left, BigInteger right)
    {
        return left.IsInteger && new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side BigInteger is equal to the right side BigFloat. If the BigFloat is not an integer then always returns false.</summary>
    public static bool operator ==(BigInteger left, BigFloat right)
    {
        return right.IsInteger && new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right side BigInteger. If the BigFloat is not an integer then always returns true.</summary>
    public static bool operator !=(BigFloat left, BigInteger right)
    {
        return !(left == right);
    }

    /// <summary>Returns true if the left side BigInteger is not equal to the right side BigFloat. If the BigFloat is not an integer then always returns true.</summary>
    public static bool operator !=(BigInteger left, BigFloat right)
    {
        return !(left == right);
    }

    ///////////////////////// Operator Overloads: BigFloat <--> ulong/long /////////////////////////

    /// <summary>Returns true if the left side BigFloat is equal to the right side unsigned long.</summary>
    public static bool operator ==(BigFloat left, ulong right)
    {
        return new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is equal to the right side long.</summary>
    public static bool operator ==(BigFloat left, long right)
    {
        return new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side long is equal to the right side BigFloat.</summary>
    public static bool operator ==(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side unsigned long is equal to the right side BigFloat.</summary>
    public static bool operator ==(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right side unsigned long.</summary>
    public static bool operator !=(BigFloat left, ulong right)
    {
        return new BigFloat(right).CompareTo(left) != 0;
    }

    /// <summary>Returns true if the left side unsigned long is not equal to the right side BigFloat.</summary>
    public static bool operator !=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) != 0;
    }

    /// <summary>Returns true if the left side BigFloat is equal to the right side unsigned long.</summary>
    public static bool operator !=(BigFloat left, long right)
    {
        return new BigFloat(right).CompareTo(left) != 0;
    }

    public static bool operator !=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) != 0;
    }

    public static bool operator <(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) < 0;
    }

    public static bool operator <(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) < 0;
    }

    public static bool operator <(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) < 0;
    }

    public static bool operator <(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) < 0;
    }

    public static bool operator >(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) > 0;
    }

    public static bool operator >(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) > 0;
    }

    public static bool operator >(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) > 0;
    }

    public static bool operator >(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) > 0;
    }

    public static bool operator <=(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) <= 0;
    }

    public static bool operator <=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) <= 0;
    }

    public static bool operator <=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) <= 0;
    }

    public static bool operator <=(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) <= 0;
    }

    public static bool operator >=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) >= 0;
    }

    public static bool operator >=(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) >= 0;
    }

    public static bool operator >=(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) >= 0;
    }

    public static bool operator >=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) >= 0;
    }

    /// <summary>Returns a 32-bit signed integer hash code for the current BigFloat object.</summary>
    public override int GetHashCode()
    {
        var c = GetCanonicalComponents();     // same path as Equals()
        if (c.Mant.IsZero) return 0;

        // Hash canonical pair (rounded mantissa, scale).
        // roundedMain includes sign; guard bits are removed.
        BigInteger roundedMain = c.Mant >> GuardBits;
        return HashCode.Combine(roundedMain, c.Scale);
    }
}
