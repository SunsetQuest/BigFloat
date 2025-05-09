// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// The below was created with ChatGPT o3. 

using System;
using System.Numerics;

namespace BigFloatLibrary;


public static class BigFloatRandom
{
    private const int _defaultFractionBits = 128;       // enough for most uses

    /* ------------------------------------------------------------ *
     *  Public API
     * ------------------------------------------------------------ */

    /// <summary>Uniform or logarithmic sample in [min,max] (inclusive of min, exclusive of max).</summary>
    public static BigFloat InRange(
        BigFloat min,
        BigFloat max,
        bool logarithmic = false,
        Random? rand = null)
    {
        rand ??= Random.Shared;
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
                return InRange(min, max, false, rand);

            return WithMantissaBits(
                mantissaBits: Math.Max(min.Size, max.Size),
                minBinaryExponent: minExp,
                maxBinaryExponent: maxExp,
                logarithmic: true,
                rand: rand);
        }
    }

    /// <summary>
    /// Random BigFloat with exactly <paramref name="mantissaBits"/> (incl. guard bits) and
    /// an exponent in [minBinaryExponent,maxBinaryExponent].
    /// </summary>
    public static BigFloat WithMantissaBits(
        int mantissaBits,
        int minBinaryExponent,
        int maxBinaryExponent,
        bool logarithmic = false,
        Random? rand = null)
    {
        if (mantissaBits <= 0) throw new ArgumentOutOfRangeException(nameof(mantissaBits));
        if (minBinaryExponent > maxBinaryExponent)
            (minBinaryExponent, maxBinaryExponent) = (maxBinaryExponent, minBinaryExponent);

        rand ??= Random.Shared;

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
        int scale = chosenExp - mantissaBits + BigFloat.GuardBits + 1;

        return BigFloat.CreateFromRawComponents(mant, scale, mantissaBits);
    }

    /* ------------------------------------------------------------ *
     *  helpers
     * ------------------------------------------------------------ */

    /// <summary>Uniform fraction with <paramref name="precisionBits"/> bits of precision.</summary>
    private static BigFloat UniformFraction(int precisionBits, Random rand)
    {
        BigInteger r = NextPositiveBigInteger(rand, precisionBits);
        // Build mantissa so that value = r / 2^precisionBits
        BigFloat bf = BigFloat.CreateFromRawComponents(
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