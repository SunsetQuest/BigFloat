using BigFloatLibrary;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using static BigFloatLibrary.BigIntegerTools;

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

            // BinaryExponent is *signed*; outside [-2^31, +2^31] anyway => decimal.Zero or overflow.
            // (A full-range check would be ~ ±10^28 decimal digits but the
            //  miniature test below is usually enough and cheap.)
            if (v.BinaryExponent > 500 || v.BinaryExponent < -500)
                return v.BinaryExponent < 0 ? decimal.Zero
                                            : throw new OverflowException();

            BigInteger m = v._mantissa;
            uint flags = 0;
            if (m.Sign < 0)
            {
                m = -m;
                flags = SignMask;
            }

            // ---------------------------------------------------------
            // 1.  Keep   PrecBits-ExtraBits   worth of precision in ‘m’
            // ---------------------------------------------------------
            int targetBits = PrecBits - ExtraBits;
            int bitsDropped = Math.Max(0, v._size - targetBits); // v._size == bit-length of mantissa
            if (bitsDropped > 0)
                m >>= bitsDropped;

            // ---------------------------------------------------------
            // 2.  Binary → decimal scaling
            // ---------------------------------------------------------
            int scale2 = v.Scale + bitsDropped - 32;   // base-2 exponent
            int bitLen = (int)m.GetBitLength();             // current #significant bits
            int decScale = 0;

            if (scale2 >= 0)
            {
                // ----- overflow detection ------------------------------------
                if (bitLen + scale2 > PrecBits)          // need >96 bits after the shift
                    throw new OverflowException(); // or, return (flags != 0) ? decimal.MinValue : decimal.MaxValue;
                // -------------------------------------------------------------

                m <<= scale2;                            // safe: fits in 96 bits
                bitLen += scale2;                     // bit-length increases by scale2

            }
            else /*if (scale2 < 0)*/
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
                    m = BigIntegerTools.RightShiftWithRound(m, excessBits);
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

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Converts BigFloat to Decimal using the high-performance approach similar to Microsoft's VarDecFromR8.
    /// Handles precision truncation, scaling, and rounding with optimal performance characteristics.
    /// </summary>
    public static class BigFloatToDecimalConverter2
    {
        private const int DecimalMaxScale = 28;
        private const int DecimalPrecisionBits = 96;
        private static readonly double Log10_2 = Math.Log10(2);

        public static decimal ToDecimal(BigFloat bigFloat)
        {
            int e = bigFloat.Scale;
            BigInteger mantissa = bigFloat._mantissa;
            int decimalScale;
            BigInteger decimalMantissa;

            if (e >= 0)
            {
                // Handle positive or zero exponent
                decimalMantissa = mantissa << e;
                decimalScale = 0;

                // Truncate to 96 bits if necessary
                int bitLength = (int)decimalMantissa.GetBitLength();
                if (bitLength > DecimalPrecisionBits)
                {
                    int excessBits = bitLength - DecimalPrecisionBits;
                    decimalMantissa = BigIntegerTools.RightShiftWithRound(decimalMantissa, excessBits);
                }
            }
            else // e < 0
            {
                // Calculate desired scale, capped at 28
                int desiredScale = (int)Math.Ceiling(-e * Log10_2);
                decimalScale = Math.Min(desiredScale, DecimalMaxScale);

                // Scale mantissa by 10^decimalScale
                BigInteger temp = mantissa * BigInteger.Pow(10, decimalScale);

                // Right-shift by -e bits to adjust for binary exponent
                decimalMantissa = BigIntegerTools.RightShiftWithRound(temp, -e);

                // Truncate to 96 bits, keeping decimalScale unchanged
                while (decimalMantissa.GetBitLength() > DecimalPrecisionBits)
                {
                    int excessBits = (int)decimalMantissa.GetBitLength() - DecimalPrecisionBits;
                    decimalMantissa = BigIntegerTools.RightShiftWithRound(decimalMantissa, excessBits);
                }
            }

            // Check scale bounds
            if (decimalScale > DecimalMaxScale)
            {
                throw new OverflowException("Decimal scale exceeds maximum allowed value");
            }

            // Construct decimal (simplified; actual implementation may vary)
            bool isNegative = mantissa < 0;
            BigInteger absMantissa = BigInteger.Abs(decimalMantissa);

            //The fix for 123456
            //absMantissa >>= 19;
            //or
            //absMantissa *= 5;
            //absMantissa >>= 18;
            //decimalScale = 19; //18
             //or
            //absMantissa *= 5*5;
            //absMantissa >>= 17;
            //decimalScale = 20; //18
                               //or
            absMantissa *= 5*5*5*5*5;
            absMantissa >>= 14;
            decimalScale = 23; //18

            int lo = int.CreateTruncating(absMantissa);           // Low bits
            int mi = int.CreateTruncating((absMantissa >> 32));  // Mid bits
            int hi = int.CreateTruncating((absMantissa >> 64));  // High bits

            decimal val= new decimal(
                lo, //(int)(absMantissa & 0xFFFFFFFF),           // Low bits
                mi, //(int)((absMantissa >> 32) & 0xFFFFFFFF),  // Mid bits
                hi, //(int)((absMantissa >> 64) & 0xFFFFFFFF),  // High bits
                isNegative,
                (byte)decimalScale
            );
            return val;
        }
    }
}