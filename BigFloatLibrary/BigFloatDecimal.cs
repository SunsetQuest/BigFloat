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
            int bitsToRemove = Math.Max(0, bigFloat.Size - DecimalPrecisionBits);

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