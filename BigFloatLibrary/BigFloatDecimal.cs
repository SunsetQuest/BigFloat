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
        int shiftBits = int.Clamp(addedBinaryPrecision + denominatorBits - numeratorBits, denominatorBits, 96);
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

    /// <summary>
    /// Converts BigFloat to Decimal using the high-performance approach similar to Microsoft's VarDecFromR8.
    /// Handles precision truncation, scaling, and rounding with optimal performance characteristics.
    /// </summary>
    public static class BigFloatToDecimalConverter
    {
        private const int DecimalMaxScale = 28;
        private const int DecimalPrecisionBits = 96; // 96-bit mantissa in decimal
        private const uint DecimalSignMask = 0x80000000;
        private const int DecimalScaleShift = 16;

        public static decimal ToDecimal(BigFloat bigFloat)
        {
            if (bigFloat._size == 0)
                return decimal.Zero;

            if (Math.Abs(bigFloat.BinaryExponent) > DecimalPrecisionBits)
            {
                if (bigFloat.BinaryExponent < 0)
                    return decimal.Zero;
                throw new OverflowException("BigFloat value is too large to represent as Decimal");
            }

            BigInteger mantissa = bigFloat._mantissa;
            uint flags = 0;
            if (mantissa.Sign < 0)
            {
                mantissa = -mantissa;
                flags = DecimalSignMask;
            }

            // Use data bit size (without GuardBits) when determining precision to remove.
            int bitsToRemove = Math.Max(0, bigFloat._size - 93);

            if (bitsToRemove > 0)
            {
                mantissa = BigIntegerTools.RightShiftWithRound(mantissa, bitsToRemove);
            }

            var (low, mid, high, decimalScale) = ConvertMantissaToDecimal(mantissa, bigFloat.Scale + bitsToRemove - BigFloat.GuardBits);

            Span<int> bits = stackalloc int[4];
            bits[0] = (int)low;
            bits[1] = (int)mid;
            bits[2] = (int)high;
            bits[3] = (int)(flags | ((uint)decimalScale << DecimalScaleShift));
            return new decimal(bits);
        }

        private static (uint low, uint mid, uint high, int decimalScale) ConvertMantissaToDecimal(BigInteger mantissa, int scale)
        {
            BigInteger workingValue = mantissa;
            int decimalScale = 0;

            if (scale > 0)
            {
                workingValue <<= scale;
            }
            else if (scale < 0)
            {
                decimalScale = ConvertNegativeScaleToDecimal(ref workingValue, -scale);
            }

            workingValue = TruncateToDecimalPrecision(workingValue, ref decimalScale);

            uint low = 0, mid = 0, high = 0;
            var bytes = workingValue.ToByteArray();
            for (int i = 0; i < 4 && i < bytes.Length; i++)
                low |= (uint)bytes[i] << (i * 8);
            for (int i = 4; i < 8 && i < bytes.Length; i++)
                mid |= (uint)bytes[i] << ((i - 4) * 8);
            for (int i = 8; i < 12 && i < bytes.Length; i++)
                high |= (uint)bytes[i] << ((i - 8) * 8);

            return (low, mid, high, decimalScale);
        }

        private static int ConvertNegativeScaleToDecimal(ref BigInteger value, int negativeScale)
        {
            int estimatedDecimalScale = (int)Math.Ceiling(negativeScale * 0.30102999566);
            if (estimatedDecimalScale > DecimalMaxScale)
                estimatedDecimalScale = DecimalMaxScale;

            if (estimatedDecimalScale > 0)
            {
                value *= BigInteger.Pow(10, estimatedDecimalScale);
            }

            value = BigIntegerTools.RightShiftWithRound(value, negativeScale);
            return estimatedDecimalScale;
        }

        private static BigInteger TruncateToDecimalPrecision(BigInteger value, ref int decimalScale)
        {
            long bitLength = value.GetBitLength();
            if (bitLength <= DecimalPrecisionBits)
                return value;

            int excessBits = (int)bitLength - DecimalPrecisionBits;
            int additionalDecimalScale = (int)Math.Floor(excessBits * 0.30103);

            if (decimalScale + additionalDecimalScale <= DecimalMaxScale)
            {
                decimalScale += additionalDecimalScale;

                if (additionalDecimalScale > 0)
                {
                    value *= BigInteger.Pow(10, additionalDecimalScale);

                    bitLength = value.GetBitLength();
                    if (bitLength <= DecimalPrecisionBits)
                        return value;
                    excessBits = (int)bitLength - DecimalPrecisionBits;
                }
            }

            value = BigIntegerTools.RightShiftWithRound(value, excessBits);
            return value;
        }
    }
}