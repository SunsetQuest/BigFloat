// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

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
    /// Binary accuracy (fractional bit budget) of this value; defined as <c>-Scale</c>. 
    /// A positive value is the number of binary accurate places to the right of the radix point.
    /// A negative value means the least significant bit is above the one place. 
    /// A value of zero is equivalent to an integer. 
    /// </summary>
    public int Accuracy => -Scale;

    public static BigFloat NegativeOne => new(BigInteger.MinusOne << GuardBits, 0, GuardBits + 1);

    /////////////////////////    CONVERSION FUNCTIONS     /////////////////////////

    public BigFloat(sbyte value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 7)
    {
        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == 0)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        int valueSize = (int)BigInteger.Abs((BigInteger)value).GetBitLength();
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(byte value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 8)
    {
        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == 0)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        int valueSize = BitOperations.Log2((uint)value) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(short value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 15)
    {
        if (value == 0)
        {
            this = default;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == short.MinValue && binaryPrecision == 15) binaryPrecision++; // Handle special case when value is MinValue

        uint magnitude = value > 0
            ? (uint)value
            : unchecked((uint)(-value));
        
        int valueSize = BitOperations.Log2(magnitude) + 1;
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (binaryPrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - binaryPrecision + valueSize;
        _size = guardBitsToAdd + binaryPrecision;

        AssertValid();
    }

    public BigFloat(ushort value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int addedBinaryPrecision = 16)
    {
        int applyGuardBits = (valueIncludesGuardBits ? 0 : GuardBits) + addedBinaryPrecision;
        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - addedBinaryPrecision;
        _size = (value == 0) ? 0 : (int)ushort.Log2(value) + 1 + applyGuardBits;
        AssertValid();
    }

    public BigFloat(uint value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 32)
    {
        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == 0)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        int valueSize = BitOperations.Log2(value) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(Int128 value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 127)
    {
        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == Int128.Zero)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        UInt128 mag = (UInt128)(value ^ (value >> 127)) - (UInt128)(value >> 127);
        int valueSize = (int)UInt128.Log2(mag) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(UInt128 value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 128)
    {
        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        if (value == UInt128.Zero)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        int valueSize = (int)UInt128.Log2(value) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    // === Canonical API (preferred) ===
    /// <summary>
    /// Increment the mantissa's GuardBit by 1 (0|00...001) in the +infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextUp(in BigFloat x) => BitAdjust(x, +1);

    /// <summary>
    /// Decrement the mantissa's guard bits by 1 (0|00...001) in the -infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextDown(in BigFloat x) => BitAdjust(x, -1);

    /// <summary>
    /// Increment the mantissa's in-precision bits by a full unit(1|00000) in the +infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextUpInPrecisionBit(in BigFloat x) => BitAdjust(x, +1L << GuardBits);

    /// <summary>
    /// Decrement the in-precision area of the mantissa by a full unit(1|00000) in the -infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextDownInPrecisionBit(in BigFloat x) => BitAdjust(x, -1L << GuardBits);

    /// <summary>
    /// Increment the mantissa's in-precision bits by a half unit(0|10000) in the +infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextUpHalfInPrecisionBit(in BigFloat x) => BitAdjust(x, +1L << (GuardBits - 1));

    /// <summary>
    /// Decrement the mantissa's in-precision bits by a half unit(0|10000) in the -infinity direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat NextDownHalfInPrecisionBit(in BigFloat x) => BitAdjust(x, -1L << (GuardBits - 1));

    // === Compatibility shims (deprecated) ===
    [System.Obsolete("Renamed: use NextUp(x) for final-precision ULP.", error: false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat BitIncrement(in BigFloat x) => NextUp(x);

    [System.Obsolete("Renamed: use NextDown(x) for final-precision ULP.", error: false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat BitDecrement(in BigFloat x) => NextDown(x);

    [System.Obsolete("Renamed: use NextUpExtended(x) for guard-bit ULP.", error: false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat GuardBitIncrement(in BigFloat x) => NextUpInPrecisionBit(x);

    [System.Obsolete("Renamed: use NextDownExtended(x) for guard-bit ULP.", error: false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat GuardBitDecrement(in BigFloat x) => NextDownInPrecisionBit(x);

    /// <summary>
    /// For positive delta, the mantissa's GuardBits move toward +infinity. 
    /// For negative delta, the mantissa's GuardBits move toward -infinity. 
    /// </summary>
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
                size++;
        }
        else if (delta == -mant.Sign)
        {
            if ((BigInteger.Abs(mant) & (BigInteger.Abs(mant) - BigInteger.One)).IsZero)
                size--;
        }
        else
        {
            // fallback to GetBitLength (rarely hit)
            size = (int)absNew.GetBitLength();
        }

        return new BigFloat(newVal, x.Scale, size);
    }

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as a
    /// normalized IEEE 754 double-precision value without exponent overflow
    /// or underflow. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInADouble
        => BinaryExponent is (>= -1022 and <= 1023);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as an
    /// IEEE 754 double-precision value, allowing both normalized and
    /// denormalized (subnormal) forms. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInADoubleWithDenormalization
        => BinaryExponent is (>= -1074 and <= 1023);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as a
    /// normalized IEEE 754 single-precision (float) value without exponent
    /// overflow or underflow. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInAFloat
        => BinaryExponent is (>= -126 and <= 127);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as an
    /// IEEE 754 single-precision (float) value, allowing both normalized and
    /// denormalized (subnormal) forms. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInAFloatWithDenormalization
        => BinaryExponent is (>= -149 and <= 127);


    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as an
    /// IEEE 754 base-10 decimal value. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInADecimal
        => IsZero
           || ((_size - GuardBits) <= 96
               && BinaryExponent is >= -95 and <= 96);


    /////////////////////////// Implicit CASTS ///////////////////////////

    /// <summary>Defines an implicit conversion of an 8-bit signed integer to a BigFloat.</summary>
    public static implicit operator BigFloat(sbyte value)
    {
        return new BigFloat(value);
    }
    /// <summary>Defines an implicit conversion of an 8-bit unsigned integer to a BigFloat.</summary>
    public static implicit operator BigFloat(byte value)
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

    /// <summary>Defines an implicit conversion of a decimal to a BigFloat.</summary>
    public static implicit operator BigFloat(decimal value)
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