﻿// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok was used in the development of this library.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// The number of data bits. GuardBits are included.
    /// </summary>
    public readonly int SizeWithGuardBits => _size;

    /// <summary>
    /// Returns true if the value is exactly zero. All data bits and GuardBits are zero.
    /// Example: IsStrictZero is true for "1.3 * (Int)0" and is false for "(1.3 * 2) - 2.6"
    /// </summary>
    public bool IsStrictZero => _mantissa.IsZero;

    /// <summary>
    /// Returns the precision of the BigFloat. This is the same as the size of the data bits. The precision can be zero or negative. A negative precision means the number is below the number of bits that are deemed precise(GuardBits).
    /// </summary>
    public int Precision => _size - GuardBits;

    /// <summary>
    /// Returns the accuracy of the BigFloat. The accuracy is equivalent to the opposite of the scale. A negative accuracy means the least significant bit is above the one place. A value of zero is equivalent to an integer. A positive value is the number of accurate places (in binary) to the right of the radix point.
    /// </summary>
    public int Accuracy => -Scale;

    public static BigFloat NegativeOne => new(BigInteger.MinusOne << GuardBits, 0, GuardBits + 1);

    /////////////////////////    CONVERSION FUNCTIONS     /////////////////////////

    public BigFloat(uint value, int scale = 0)
    {
        _mantissa = (BigInteger)value << GuardBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + GuardBits;
        AssertValid();
    }

    public BigFloat(char integerPart, int binaryScaler = 0)
    {
        _mantissa = (BigInteger)integerPart << GuardBits;
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
        _mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;
        _size = integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + GuardBits;
        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler = 0)
    {
        _mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + GuardBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + GuardBits : 0;

        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler, bool valueIncludesGuardBits)
    {
        _mantissa = (BigInteger)integerPart << GuardBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + GuardBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + GuardBits : 0;

        AssertValid();

        int applyGuardBits = valueIncludesGuardBits ? 0 : GuardBits;
        // we need Abs() so items that are a negative power of 2 have the same size as the positive version.
        _size = (int)((BigInteger)(integerPart >= 0 ? integerPart : -integerPart)).GetBitLength() + applyGuardBits;
        _mantissa = integerPart << applyGuardBits;
        Scale = binaryScaler; // DataBits of zero can have scale
        AssertValid();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat BitIncrement(in BigFloat x)
       => BitAdjust(x, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat BitDecrement(in BigFloat x)
       => BitAdjust(x, -1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat GuardBitIncrement(in BigFloat x)
       => BitAdjust(x, 1L << GuardBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat GuardBitDecrement(in BigFloat x)
       => BitAdjust(x, -(1L << GuardBits));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigFloat BitAdjust(in BigFloat x, long delta)
    {
        BigInteger mant = x._mantissa;
        int size = x._size;

        // fast path: small mantissa → ulong
        if (size < 63)
        {
            long uNew = (long)mant + delta;
            size = 64 - BitOperations.LeadingZeroCount((ulong)Math.Abs(uNew));
            return new BigFloat((BigInteger)uNew, x.Scale, size);
        }

        // slow path: BigInteger arithmetic
        BigInteger newVal = mant + delta;
        BigInteger absNew = BigInteger.Abs(newVal);

        if (delta == mant.Sign)
        {
            if ((absNew & (absNew - BigInteger.One)).IsZero)
                size ++;
        }
        else if (delta == -mant.Sign)
        {
            if ((BigInteger.Abs(mant) & (BigInteger.Abs(mant) - BigInteger.One)).IsZero)
                size --;
        }
        else
        {
            // fallback to your existing GetBitLength (rarely hit)
            size = (int)absNew.GetBitLength();
        }

        return new BigFloat(newVal, x.Scale, size);
    }


    /////////////////////////// Implicit CASTS ///////////////////////////

    /// <summary>Defines an implicit conversion of an 8-bit signed integer to a BigFloat.</summary>
    public static implicit operator BigFloat(sbyte value)
    {
        return new BigFloat(value);
    }

    //future: maybe use an unsigned BigFloat constructor for better performance. (using an unsigned BigInteger)
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

    // future: all of these should be the same in terms of a "int addedBinaryPrecision = 32" parameter
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