// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok were used in the development of this library.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BigFloatLibrary;

public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{

    /// <summary>
    /// Initializes a BigFloat from a decimal value.
    /// Converts from decimal (base-10) to binary representation.
    /// </summary>
    public BigFloat(decimal value, int binaryScaler = 0, int addedBinaryPrecision = 96)
    {
        if (value == 0m)
        {
            _mantissa = 0;
            Scale = binaryScaler + GuardBits - addedBinaryPrecision;
            _size = 0;
            AssertValid();
            return;
        }
        
        // Extract decimal components
        int[] bits = decimal.GetBits(value);
        bool isNegative = (bits[3] & 0x80000000) != 0;
        byte scale = (byte)((bits[3] >> 16) & 0x7F);

        // Reconstruct the 96-bit integer mantissa
        BigInteger numerator = ((BigInteger)(uint)bits[2] << 64) |
                                     ((BigInteger)(uint)bits[1] << 32) |
                                     (uint)bits[0];

        int numeratorBits = (bits[2] != 0) ?
             64 + BitOperations.Log2((uint)bits[2])
             : ((bits[1] != 0) ?
                32 + BitOperations.Log2((uint)bits[1])
                : BitOperations.Log2((uint)bits[0])) + 1;

        // The decimal value is: decimalMantissa * 10^(-scale)
        // We need to convert this to binary: mantissa * 2^exponent

        // Strategy: Convert 10^(-scale) to 2^x * 5^(-scale)
        // Then: value = decimalMantissa * 5^(-scale) * 2^(-scale)

        BigInteger denominator = BigInteger.Pow(5, scale);
        int binaryExponent = -scale;
        int denominatorBits = (int)(scale * 2.321928094887362) + 1;


        // Normalize: ensure numerator is odd (extract all factors of 2)
        int trailingZeros = 0;
        BigInteger temp = numerator;
        while (!temp.IsZero && temp.IsEven)
        {
            temp >>= 1;
            trailingZeros++;
        }

        if (trailingZeros > 0)
        {
            numerator >>= trailingZeros;
            binaryExponent += trailingZeros;
            numeratorBits -= trailingZeros;
        }

        // We want enough precision: shift left to get desired precision
        int shiftBits = int.Clamp(addedBinaryPrecision + denominatorBits, denominatorBits, 96);
        //int shiftBits = Math.Max(addedBinaryPrecision - numeratorBits + denominatorBits, 0);

        BigInteger shiftedNumerator = numerator << shiftBits;
        BigInteger mantissa = shiftedNumerator / denominator;

        // Adjust binary exponent
        binaryExponent -= shiftBits;

        // Set fields
        _mantissa = !isNegative ? mantissa : -mantissa;
        _size = (int)mantissa.GetBitLength();
        Scale = binaryExponent + binaryScaler + GuardBits;

        AssertValid();
    }

    /// <summary>
    /// Defines an explicit conversion of a BigFloat to a Decimal.
    /// Handles conversion from binary to decimal representation with proper rounding.
    /// Caution: Precision may be lost as Decimal has limited range and precision.
    /// </summary>
    public static explicit operator decimal(BigFloat value)
    {
        return BigFloatToDecimalConverter.ToDecimal(value);
    }
    public static class BigFloatToDecimalConverter
    {
        private const int MaxScale = 28;
        private const int PrecBits = 96;
        private const int ExtraBits = 3;
        private const uint SignMask = 0x8000_0000;
        private const int ScaleShift = 16;
        private const double Log10Of2 = 0.3010299956639812;

        // --------------------------------------------------------------------
        // Pre-computed powers of ten   10^0 … 10^28   (≈1 KiB once per process)
        // --------------------------------------------------------------------
        private static readonly BigInteger[] TenPow = BuildPow10Table();
        private static BigInteger[] BuildPow10Table()
        {
            var tbl = new BigInteger[MaxScale + 1];
            tbl[0] = BigInteger.One;
            for (int i = 1; i <= MaxScale; ++i)
                tbl[i] = tbl[i - 1] * 10;
            return tbl;
        }

        public static decimal ToDecimal(BigFloat v)
        {
            // ---------------------------------------------------------
            // 0.  Handle special cases / sign / trivial under-/overflow
            // ---------------------------------------------------------
            if (v._size == 0)
                return decimal.Zero;

            BigInteger m = v._mantissa;
            int bitLen = v._size;             // current #significant bits
            uint flags = 0;
            if (m.Sign < 0)
            {
                m = -m;
                flags = SignMask;
            }

            // ---------------------------------------------------------
            // 1.  Keep   PrecBits-ExtraBits   worth of precision in ‘m’
            // ---------------------------------------------------------
            int bitsDropped = Math.Max(0, v._size - PrecBits); // v._size == bit-length of mantissa
            if (bitsDropped > 0)
            {
                m >>= bitsDropped;
                bitLen -= bitsDropped;
            }

            // ---------------------------------------------------------
            // 2.  Binary → decimal scaling
            // ---------------------------------------------------------
            int scale2 = v.Scale + bitsDropped - GuardBits;  // base-2 exponent
            int decScale = 0;

            if (scale2 >= 0)
            {
                // ----- overflow detection ------------------------------------
                if (bitLen + scale2 > PrecBits)   // need >96 bits after the shift
                   throw new OverflowException(); // or, return (flags != 0) ? decimal.MinValue : decimal.MaxValue;
                // -------------------------------------------------------------

                m <<= scale2;                     // safe: fits in 96 bits
                bitLen += scale2;                 // bit-length increases by scale2
            }
            else 
            {
                // decimal digits needed to offset the *entire* negative scale
                int idealDec = (int)Math.Ceiling(-scale2 * Log10Of2);
                decScale = idealDec > MaxScale ? MaxScale : idealDec;

                if (decScale != 0)
                    m *= TenPow[decScale];

                // **always** shift by the full |scale2| bits
                m = BigIntegerTools.RightShiftWithRound(m, -scale2, ref bitLen);
                //bitLen = (int)m.GetBitLength();
            }

            // ---------------------------------------------------------
            // 3.  Guarantee ≤ 96 significant bits (rounding, not truncate)
            // ---------------------------------------------------------
            if (bitLen > PrecBits)
            {
                int excessBits = bitLen - PrecBits;
                int extraDec = (int)Math.Floor(excessBits * Log10Of2);

                if (extraDec > 0 && decScale + extraDec <= MaxScale)
                {
                    decScale += extraDec;
                    m *= TenPow[extraDec];

                    bitLen = (int)m.GetBitLength();
                    excessBits = Math.Max(0, bitLen - PrecBits);
                }

                if (excessBits > 0)
                    m = BigIntegerTools.RoundingRightShift(m, excessBits);
            }

            // did the rounding spill into a new MSB?
            if (m.GetBitLength() > PrecBits)
            {
                m /= 10;
                decScale--;
            }

            // ---------------------------------------------------------
            // 4.  Pack the low/mid/high 32-bit limbs without allocations
            // ---------------------------------------------------------
            Span<byte> buf = stackalloc byte[16];   // little-endian, plenty for 96 bits
            m.TryWriteBytes(buf, out int bytesWritten, isUnsigned: true, isBigEndian: false);

            uint low = Unsafe.ReadUnaligned<uint>(ref buf[0]);
            uint mid = bytesWritten > 4 ? Unsafe.ReadUnaligned<uint>(ref buf[4]) : 0u;
            uint high = bytesWritten > 8 ? Unsafe.ReadUnaligned<uint>(ref buf[8]) : 0u;

            Span<int> bits =
            [
                (int)low,
            (int)mid,
            (int)high,
            (int)(flags | ((uint)decScale << ScaleShift)),
        ];
            return new decimal(bits);
        }
    }

}