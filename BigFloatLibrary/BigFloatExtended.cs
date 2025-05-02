// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Numerics;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// The number of data bits. ExtraHiddenBits are included.
    /// </summary>
    public readonly int SizeWithHiddenBits => _size;

    /// <summary>
    /// Returns true if the value is exactly zero. All data bits and ExtraHiddenBits are zero.
    /// Example: IsStrictZero is true for "1.3 * (Int)0" and is false for "(1.3 * 2) - 2.6"
    /// </summary>
    public bool IsStrictZero => Mantissa.IsZero;

    /// <summary>
    /// Returns the precision of the BigFloat. This is the same as the size of the data bits. The precision can be zero or negative. A negative precision means the number is below the number of bits (HiddenBits) that are deemed precise.
    /// </summary>
    public int Precision => _size - GuardBits;

    /// <summary>
    /// Returns the accuracy of the BigFloat. The accuracy is equivalent to the opposite of the scale. A negative accuracy means the least significant bit is above the one place. A value of zero is equivalent to an integer. A positive value is the number of accurate places (in binary) to the right of the radix point.
    /// </summary>
    public int Accuracy => -Scale;

    //future: rename to ZeroWithSpecifiedAccuracy  (like IntWithAccuracy?)
    /// <summary>
    /// Returns a Zero with a given lower bound of precision. Example: -4 would result in 0.0000 (in binary). ExtraHiddenBits will be appended as well.
    /// </summary>
    /// <param name="pointOfLeastPrecision">The precision can be positive or negative.</param>
    public static BigFloat ZeroWithSpecifiedLeastPrecision(int pointOfLeastPrecision)
    {
        return new(BigInteger.Zero, pointOfLeastPrecision, 0);
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(BigInteger intVal, int precisionInBits)
    {
        int intSize = (int)BigInteger.Abs(intVal).GetBitLength();
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return precisionInBits < -(GuardBits + intSize)
            ? Zero
            : new(intVal << (GuardBits + precisionInBits), -precisionInBits, GuardBits + intSize + precisionInBits);
        // alternative: throw new ArgumentException("The requested precision would not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(int intVal, int precisionInBits)
    {
        int size = int.Log2(int.Abs(intVal)) + 1 + GuardBits;
        return precisionInBits < -size
            ? Zero
            : new(((BigInteger)intVal) << (GuardBits + precisionInBits), -precisionInBits, size + precisionInBits);
    }

    public static BigFloat NegativeOne => new(BigInteger.MinusOne << GuardBits, 0, GuardBits + 1);

    /////////////////////////    CONVERSION FUNCTIONS     /////////////////////////

    public BigFloat(uint value, int scale = 0)
    {
        Mantissa = (BigInteger)value << GuardBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + GuardBits;
        AssertValid();
    }

    public BigFloat(char integerPart, int binaryScaler = 0)
    {
        Mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;

        // Special handling required for int.MinValue
        _size = integerPart >= 0
            ? integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + GuardBits
            : integerPart != char.MinValue
                ? integerPart == 0 ? 0 : BitOperations.Log2((byte)-integerPart) + 1 + GuardBits
                : 7 + GuardBits;

        AssertValid();
    }

    public BigFloat(byte integerPart, int binaryScaler = 0)
    {
        Mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;
        _size = integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + GuardBits;
        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler = 0)
    {
        Mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + GuardBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + GuardBits : 0;

        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler, bool valueIncludesHiddenBits)
    {
        Mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + GuardBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + GuardBits : 0;

        AssertValid();

        int applyHiddenBits = valueIncludesHiddenBits ? 0 : GuardBits;
        // we need Abs() so items that are a negative power of 2 have the same size as the positive version.
        _size = (int)((BigInteger)(integerPart >= 0 ? integerPart : -integerPart)).GetBitLength() + applyHiddenBits;
        Mantissa = integerPart << applyHiddenBits;
        Scale = binaryScaler; // DataBits of zero can have scale
        AssertValid();
    }

    /////////////////////////// Implicit CASTS ///////////////////////////

    /// <summary>Defines an implicit conversion of an 8-bit signed integer to a BigFloat.</summary>
    public static implicit operator BigFloat(sbyte value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a 16-bit unsigned integer to a BigFloat.</summary>
    public static implicit operator BigFloat(ushort value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 16-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(short value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a 32-bit unsigned integer to a BigFloat.</summary>
    public static implicit operator BigFloat(uint value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a 64-bit unsigned integer to a BigFloat.</summary>
    public static implicit operator BigFloat(ulong value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 64-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(long value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 64-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(Int128 value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 64-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(UInt128 value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 32-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(int value)
    {
        return new BigFloat(value);
    }

    /////////////////////////// Explicit CASTS ///////////////////////////

    /// <summary>Defines an explicit conversion of a System.Single to a BigFloat.</summary>
    public static explicit operator BigFloat(float value)
    {
        return new BigFloat(value);
    }
}