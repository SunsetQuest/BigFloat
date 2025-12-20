// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

// Random sampling helpers for BigFloat values.

using System.Numerics;

namespace BigFloatLibrary;
#nullable enable


public readonly partial struct BigFloat
{
    private const int _defaultFractionBits = 128;       // enough for most uses

    /// <summary>
    /// For long running methods only, This specifies the target time in milliseconds.
    /// </summary>
    public const int TestTargetInMilliseconds = 100;
    public const int RAND_SEED = 0x51C0_F00D;
    public static readonly Random _rand = new(RAND_SEED);

    /// <summary>Uniform or logarithmic sample in [min,max] (inclusive of min, exclusive of max).</summary>
    public static BigFloat RandomInRange(
        BigFloat min,
        BigFloat max,
        bool logarithmic = false,
        Random? rand = null)
    {
        rand ??= _rand;
        if (min > max) (min, max) = (max, min);
        if (min == max) return min;

        if (!logarithmic)
        {
            // linear:  min + (max‑min)*U ,   U ∈ [0,1)
            BigFloat span = max - min;
            BigFloat u = UniformFraction(_defaultFractionBits, rand);
            return min + span * u;
        }
        else
        {
            // logarithmic: exponent uniform, mantissa uniform
            int minExp = min.BinaryExponent;
            int maxExp = max.BinaryExponent;
            if (minExp == maxExp)               // small gap ⇒ fall back to linear
                return RandomInRange(min, max, false, rand);

            return RandomWithMantissaBits(
                mantissaBits: Math.Max(min.Size, max.Size),
                minBinaryExponent: minExp,
                maxBinaryExponent: maxExp,
                logarithmic: true,
                rand: rand);
        }
    }

    /// <summary>
    /// Random BigFloat with exactly <paramref name="mantissaBits"/> (incl. guard bits) and
    /// an exponent in [minBinaryExponent,maxBinaryExponent]. When
    /// <paramref name="logarithmic"/> is true, exponents are chosen uniformly across the
    /// range; otherwise, exponents are weighted toward the lower end for a nearly linear
    /// distribution.
    /// </summary>
    public static BigFloat RandomWithMantissaBits(
        int mantissaBits,
        int minBinaryExponent,
        int maxBinaryExponent,
        bool logarithmic = false,
        Random? rand = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mantissaBits);
        if (minBinaryExponent > maxBinaryExponent)
            (minBinaryExponent, maxBinaryExponent) = (maxBinaryExponent, minBinaryExponent);

        rand ??= _rand;

        /* ---- choose exponent ---- */
        int chosenExp = logarithmic
            ? rand.Next(minBinaryExponent, maxBinaryExponent + 1)        // uniform in exponent
            : minBinaryExponent +                                          // nearly‑linear:
              (int)Math.Floor((maxBinaryExponent - minBinaryExponent + 1) *
                              rand.NextDouble());

        /* ---- choose mantissa ---- */
        BigInteger mant = NextPositiveBigInteger(rand, mantissaBits);

        /* ---- map mantissa+scale so that BinaryExponent == chosenExp ----
         * BinaryExponent = Scale + _size - GuardBits - 1
         * _size == mantissaBits
         * ⇒ Scale = chosenExp - mantissaBits + GuardBits + 1
         */
        int scale = chosenExp - mantissaBits + GuardBits + 1;

        return CreateFromRawComponents(mant, scale, mantissaBits);
    }

    /* ------------------------------------------------------------ *
     *  helpers
     * ------------------------------------------------------------ */

    /// <summary>Uniform fraction in [0,1) with <paramref name="precisionBits"/> bits of precision.</summary>
    private static BigFloat UniformFraction(int precisionBits, Random rand)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(precisionBits);

        int byteCount = (precisionBits + 7) / 8;
        Span<byte> buf = stackalloc byte[byteCount];
        rand.NextBytes(buf);

        int extraBits = (byteCount * 8) - precisionBits;
        if (extraBits > 0)
        {
            byte mask = (byte)((1 << (8 - extraBits)) - 1);
            buf[^1] &= mask;
        }

        BigInteger r = new(buf, isUnsigned: true, isBigEndian: false);
        // Build mantissa so that value = r / 2^precisionBits
        BigFloat bf = CreateFromRawComponents(
            mantissa: r,
            binaryScaler: -(precisionBits),     // i.e. *2^(‑precisionBits)
            mantissaSize: precisionBits);

        // ensure it’s in [0,1)
        return bf;
    }

    /// <summary>Random positive BigInteger with exactly <paramref name="bits"/> bits.</summary>
    private static BigInteger NextPositiveBigInteger(Random rand, int bits)
    {
        int byteCount = (bits + 7) / 8;
        Span<byte> buf = stackalloc byte[byteCount + 1];   // +1 for sign‑byte == 0
        rand.NextBytes(buf[..byteCount]);

        int highestBit = (bits - 1) & 7;
        buf[byteCount - 1] &= (byte)((1 << (highestBit + 1)) - 1);  // clear superfluous high bits
        buf[byteCount - 1] |= (byte)(1 << highestBit);              // make sure top bit = 1

        buf[byteCount] = 0;      // sign

        return new BigInteger(buf, isUnsigned: true, isBigEndian: false);
    }
}
