// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;
using System.Runtime.CompilerServices;
using static BigFloatLibrary.BigFloatNumerics;

namespace BigFloatLibrary;

// see "Rounding-Shifting-Truncate.txt" for additional notes

public readonly partial struct BigFloat
{
    public ulong Lowest64BitsWithGuardBits
    {
        get
        {
            // future: can we use return ulong.CreateTruncating(_mantissa);
            ulong raw = (ulong)(_mantissa & ulong.MaxValue);

            if (_mantissa.Sign < 0)
            {
                raw = ~raw + (ulong)(((_size >> 64) > 0) ? 1 : 0);
            }
            return raw;
        }
    }

    public ulong Lowest64Bits
    {
        get
        {
            // future: can we use return ulong.CreateTruncating(_mantissa);
            // future: we may want to be rounding here instead of "_mantissa >> GuardBits"

            if (_mantissa.Sign >= 0)
            {
                ulong raw = (ulong)((_mantissa >> GuardBits) & ulong.MaxValue);
                return raw;
            }
            else if (_size >= GuardBits)
            {
                return ~(ulong)(((_mantissa - 1) >> GuardBits) & ulong.MaxValue);
                //return (ulong)((BigInteger.Abs(DataBits) >> GuardBits) & ulong.MaxValue); //perf: benchmark

            }
            else
            {
                ulong raw = (ulong)((_mantissa >> GuardBits) & ulong.MaxValue);
                //raw--;
                raw = ~raw;
                return raw;
            }
        }
    }

    /// <summary>
    /// Returns the 64 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 64 bits, then the LSBs are padded with zeros.
    /// </summary>
    public ulong Highest64Bits
    {
        get
        {
            BigInteger magnitude = BigInteger.Abs(_mantissa);

            if (_size <= 64)
            {
                // Not enough bits to fill 64, so left-pad with zeros in the LSB positions.
                return (ulong)(magnitude << (64 - _size));
            }

            return (ulong)(magnitude >> (_size - 64));
        }
    }

    /// <summary>
    /// Returns the 128 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 128 bits, then the LSBs are padded with zeros.
    /// </summary>
    public UInt128 Highest128Bits
    {
        get
        {
            BigInteger magnitude = BigInteger.Abs(_mantissa);

            if (_size <= 128)
            {
                // Not enough bits to fill 128, so left-pad with zeros in the LSB positions.
                return (UInt128)(magnitude << (128 - _size));
            }

            return (UInt128)(magnitude >> (_size - 128));
        }
    }

    /// <summary>
    /// Returns true if any fractional bit exists in the working-precision window
    /// (i.e., between '.' and the guard boundary). Guard bits are ignored.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasWorkingFractionBits()
    {
        if (_mantissa.IsZero) return false;

        // If '.' is at or to the right of the guard boundary, there are no working fractional bits.
        if (Scale > 0) return false;

        int availableAboveGuard = _size - GuardBits;   // how many working bits exist at all
        if (availableAboveGuard <= 0) return false;    // number is entirely out-of-precision -> treat as integer

        int k = Math.Min(-Scale, availableAboveGuard) + 1; // number of working fractional bits that actually exist
        if (k <= 0) return false;

        BigInteger mag = BigInteger.Abs(_mantissa);
        BigInteger workingBelowPoint = mag >> (GuardBits - 1);  // align guard boundary to bit 0
        BigInteger mask = (BigInteger.One << k) - 1;            // low k bits are the fractional field
        return (workingBelowPoint & mask) != 0;
    }

    //public BigFloat Ceiling()
    //{
    //    if (_mantissa.IsZero) return this;

    //    int s = GuardBits - Scale;                  // #fraction bits below the 1s place
    //    if (s <= 0) return this;                    // already integer at this scale

    //    // Entirely fractional: |x| < 1
    //    if (s >= _size)
    //        return _mantissa.Sign > 0
    //            ? new BigFloat(BigInteger.One << GuardBits, 0, GuardBits + 1)
    //            : Zero;

    //    int sign = _mantissa.Sign;

    //    if (sign < 0)
    //    {
    //        // Ceiling for negatives = truncate toward zero
    //        BigInteger q = -(-_mantissa >> s);
    //        int qBits = q.IsZero ? 0 : (int)BigInteger.Abs(q).GetBitLength();
    //        return new BigFloat(q << GuardBits, 0, qBits + GuardBits);
    //    }

    //    // Positive: detect working-window fraction
    //    bool hasWorkingFraction = false;
    //    if (Scale < 0)
    //    {
    //        int k = Math.Min(-Scale, Math.Max(_size - GuardBits, 0));
    //        if (k > 0)
    //        {
    //            var mag = BigInteger.Abs(_mantissa);
    //            var workingBelowPoint = mag >> GuardBits;
    //            var mask = (BigInteger.One << k) - 1;
    //            hasWorkingFraction = (workingBelowPoint & mask) != 0;
    //        }
    //    }

    //    // MSB of the entire fractional field (bit s-1) acts as “top guard” trigger
    //    var magAll = BigInteger.Abs(_mantissa);
    //    BigInteger fracMask = (BigInteger.One << s) - 1;
    //    BigInteger frac = magAll & fracMask;
    //    bool topFractionBit = ((frac >> (s - 1)) & BigInteger.One) == BigInteger.One;

    //    BigInteger intUnits = magAll >> s;
    //    if (hasWorkingFraction || topFractionBit) intUnits += BigInteger.One;
    //    else return this; // Optional: Do we want Ceiling to round down if no increment? remove if so

    //    int bits = intUnits.IsZero ? 0 : (int)intUnits.GetBitLength();
    //    return new BigFloat(intUnits << GuardBits, 0, bits + GuardBits);
    //}

    /// <summary>
    /// Ceiling that never reduces the value.
    /// - If there are working-precision fractional bits: step to the next integer (for positives).
    /// - If there are no working-precision fractional bits (only guard fraction or none): return the input.
    /// Preserves canonical “normalize once at precision boundary” behavior but avoids Ceil(x) < x.
    /// </summary>
    public BigFloat Ceiling()
    {
        if (!HasWorkingFractionBits())
        {
            // Sticky: treat guard-only fraction as already an integer; do not move down.
            return this;
        }

        // Usual integer step based on sign
        BigFloat integerPart = Truncate(); // Removes all fractional
        return IsNegative ? integerPart : integerPart 
            + new BigFloat(BigInteger.One << GuardBits, 0, GuardBits + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Ceiling(BigFloat x) => x.Ceiling();

    /// <summary>
    /// Canonical Ceiling that preserves Scale/accuracy via identity.
    /// Ceiling toward +∞:
    /// - If IsInteger, return this (no increment).
    /// - If sign < 0, truncate toward 0.
    /// - If sign > 0 and any fractional bits exist, add 1.
    /// Always returns an integer encoding (Scale==0) unless IsInteger short-circuit returns ‘this’.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat CeilingPreservingAccuracy()
    {
        if (!HasWorkingFractionBits())
        {
            // Sticky: treat guard-only fraction as already an integer; do not move down.
            return this;
        }

        // Usual integer step based on sign
        BigFloat integerPart = TruncateToIntegerKeepingAccuracy(); // preserves precision/accuracy
        return IsNegative ? integerPart : integerPart + 1;
    }

    ///// <summary>
    ///// Canonical Ceiling that preserves Scale/accuracy via identity.
    ///// Ceiling toward +∞:
    ///// - If IsInteger, return this (no increment).
    ///// - If sign < 0, truncate toward 0.
    ///// - If sign > 0 and any fractional bits exist, add 1.
    ///// Always returns an integer encoding (Scale==0) unless IsInteger short-circuit returns ‘this’.
    /////</summary>
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public BigFloat CeilingPreservingAccuracy()
    //{
    //    int s = GuardBits - Scale;
    //    if (_mantissa.IsZero || s <= 0) return this;

    //    // Entirely fractional
    //    if (s >= _size)
    //        return _mantissa.Sign > 0
    //            ? new BigFloat(BigInteger.One << (GuardBits - Scale), Scale, 1 + GuardBits - Scale)
    //            : new BigFloat(BigInteger.Zero, Scale, 0);

    //    // Negative: truncate toward zero
    //    if (_mantissa.Sign < 0)
    //    {
    //        BigInteger res = -(-_mantissa >> s) << s;
    //        int bits = res.IsZero ? 0 : (int)BigInteger.Abs(res).GetBitLength();
    //        return new BigFloat(res, Scale, bits);
    //    }

    //    // Positive: working-fraction or top fractional bit triggers bump
    //    var mag = BigInteger.Abs(_mantissa);
    //    bool hasWorkingFraction = false;
    //    if (Scale < 0)
    //    {
    //        int k = Math.Min(-Scale, Math.Max(_size - GuardBits, 0));
    //        if (k > 0)
    //        {
    //            var workingBelowPoint = mag >> GuardBits;
    //            var mask = (BigInteger.One << k) - 1;
    //            hasWorkingFraction = (workingBelowPoint & mask) != 0;
    //        }
    //    }

    //    BigInteger fracMask = (BigInteger.One << s) - 1;
    //    BigInteger frac = mag & fracMask;
    //    bool topFractionBit = ((frac >> (s - 1)) & BigInteger.One) == BigInteger.One;

    //    BigInteger cleared = _mantissa & ~fracMask;


    //    //BigInteger res2 = (hasWorkingFraction || topFractionBit) ? (cleared + (BigInteger.One << s)) : cleared;

    //    BigInteger res2 = cleared;
    //    if (hasWorkingFraction || topFractionBit) res2 += (BigInteger.One << s);
    //    else return this; // Optional: Do we want Ceiling to round down if no increment? remove if so


    //    int bits2 = res2.IsZero ? 0 : (int)BigInteger.Abs(res2).GetBitLength();
    //    return new BigFloat(res2, Scale, bits2);
    //}


    /// <summary>
    /// Canonical Floor that preserves Scale/accuracy via identity
    /// Rounds to the next integer towards negative infinity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat FloorPreservingAccuracy() => -(-this).CeilingPreservingAccuracy();

    /// <summary>
    /// Canonical Floor (integer, rescaled) via identity.
    /// Rounds towards negative infinity.
    /// Removes all fractional bits, sets negative scales to zero, 
    /// and resizes precision to just the integer part.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat Floor() => -(-this).Ceiling();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Floor(BigFloat x) => -(-x).Ceiling();

    /// <summary>
    /// Returns the fractional part of the BigFloat.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat FractionalPart()
    {
        int bitsToClear = GuardBits - Scale;

        if (bitsToClear <= 0) return ZeroWithAccuracy(-Scale);
        if (bitsToClear >= _size) return this;

        BigInteger mask = (BigInteger.One << (bitsToClear - 1)) - 1;

        //  fractional part using subtraction to avoid masking issues
        if (_mantissa.Sign >= 0)
        {
            BigInteger frac = _mantissa & mask;
            return new BigFloat(frac, Scale, MantissaSize(frac));
        }
        else
        {
            BigInteger frac = -(-_mantissa & mask);
            return new BigFloat(frac, Scale, MantissaSize(-frac));
        }
    }

    /// <summary>
    /// Returns an integer with a specific binary accuracy. This is the number of binary digits to the right of the point. This is beyond the GuardBits.
    /// </summary>
    /// <param name="accuracyBits">The accuracy range can be from -GuardBits to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(BigInteger intVal, int accuracyBits)
    {
        int intSize = MantissaSize(intVal);
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return accuracyBits < -(GuardBits + intSize)
            ? ZeroWithAccuracy(0)
            : new(intVal << (GuardBits + accuracyBits), -accuracyBits, GuardBits + intSize + accuracyBits);
        // alternative: throw new ArgumentException("The requested precision would not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with a specific binary accuracy. This is the number of binary digits to the right of the point. This is beyond the GuardBits.
    /// </summary>
    /// <param name="accuracyBits">The accuracy range can be from -GuardBits to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(int intVal, int accuracyBits)
    {
        int size = int.Log2(int.Abs(intVal)) + 1 + GuardBits;
        return accuracyBits < -size
            ? ZeroWithAccuracy(0)
            : new(((BigInteger)intVal) << (GuardBits + accuracyBits), -accuracyBits, size + accuracyBits);
    }

    /// <summary>
    /// Left shift - Increases the size by adding least-significant zero bits. 
    /// i.e. The precision is artificially enhanced. 
    /// </summary>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    public BigFloat LeftShiftMantissa(int bits)
    {
        return CreateFromRawComponents(_mantissa << bits, Scale, _size + bits);
    }

    /// <summary>
    /// Right shift - Decreases the size by removing the least-significant bits. 
    /// i.e. The precision is reduced. 
    /// No rounding is performed and Scale is unchanged. 
    /// </summary>
    /// <param name="bits">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    public BigFloat RightShiftMantissa(int bits)
    {
        return CreateFromRawComponents(_mantissa >> bits, Scale, _size - bits);
    }

}
