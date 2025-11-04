// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

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

    public BigFloat(short value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int addedBinaryPrecision = 15)
    {
        int applyGuardBits = (valueIncludesGuardBits ? 0 : GuardBits) + addedBinaryPrecision;
        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - addedBinaryPrecision;
        _size = (value == 0) ? 0 : (int)short.Log2(value) + 1 + applyGuardBits;
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

        int valueSize = value == Int128.MinValue
            ? 128
            : (int)Int128.Log2(Int128.Abs(value)) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = effectivePrecision + GuardBits;
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

    /////////// DELETE BELOW //////
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public BigFloat(int integerPart, int binaryScaler/* = 0,*/, int addedBinaryPrecision/* = 32*/) : this((long)integerPart, binaryScaler, addedBinaryPrecision) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat(int integerPart, int binaryScaler/* = 0,*/, int addedBinaryPrecision/* = 32*/)
    {
        //todo: this should be deleted
        int applyGuardBits = GuardBits + addedBinaryPrecision;
        _mantissa = (BigInteger)integerPart << applyGuardBits;
        Scale = binaryScaler - addedBinaryPrecision;
        _size = (integerPart == 0) ? 0 : int.Log2(integerPart) + 1 + applyGuardBits;
        AssertValid();
    }




    public BigFloat(long value, int binaryScaler/* = 0*/, int addedBinaryPrecision/* = 64*/)
    {
        _mantissa = (BigInteger)value << (GuardBits + addedBinaryPrecision);

        // Optimized bit length calculation using hardware intrinsics when available
        _size = value switch
        {
            > 0 => GetBitLength((ulong)value) + GuardBits + addedBinaryPrecision,
            < 0 => GetBitLength(~((ulong)value - 1)) + GuardBits + addedBinaryPrecision,
            _ => 0,
        };

        Scale = binaryScaler - addedBinaryPrecision;
        AssertValid();
    }

    /////////// DELETE ABOVE //////


    ///////////// todo: MOVE BACK to BigFloat.cs ////////////////////////////////////////
    /// <summary>
    /// Constructs a BigFloat using its elemental parts. A starting <paramref name="value"/> on how may binary places the point should be shifted (base-2 exponent) using <paramref name="binaryScaler"/>.
    /// </summary>
    /// <param name="value">The integer part of the BigFloat that will have a <paramref name="binaryScaler"/> applied to it. </param>
    /// <param name="binaryScaler">How much should the <paramref name="value"/> be shifted or scaled? This shift (base-2 exponent) will be applied to the <paramref name="value"/>.</param>
    /// <param name="valueIncludesGuardBits">if true, then the guard bits should be included in the integer part.</param>
    public BigFloat(BigInteger value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int addedBinaryPrecision = 0)
    {
        int applyGuardBits = (valueIncludesGuardBits ? 0 : GuardBits) + addedBinaryPrecision;
        Scale = binaryScaler - addedBinaryPrecision;

        if (value.IsZero)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            AssertValid();
            return;
        }
        
        _mantissa = value << applyGuardBits;
        _size = (int)BigInteger.Abs(_mantissa).GetBitLength();

        AssertValid();
    }

    //public BigFloat(int value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int addedBinaryPrecision = 31)
    //{
    //    int applyGuardBits = (valueIncludesGuardBits ? 0 : GuardBits) + addedBinaryPrecision;
    //    _mantissa = (BigInteger)value << applyGuardBits;
    //    Scale = binaryScaler - addedBinaryPrecision;
    //    //_size = (value == 0) ? 0 : BitOperations.Log2((uint)int.Abs(value)) + 1 + applyGuardBits;
    //    _size = value > Int128.Zero
    //        ? int.Log2(value) + 1 + applyGuardBits
    //        : value < 0 ? 32 - int.LeadingZeroCount(~(value - 1)) + applyGuardBits : 0;
    //    AssertValid();
    //}

    public BigFloat(int value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 31)
    {
        if (value == 0)
        {
            _mantissa = BigInteger.Zero;
            _size = 0;
            Scale = binaryScaler - binaryPrecision;
            AssertValid();
            return;
        }

        if (binaryPrecision < 0)
        {
            ThrowInvalidInitializationException($"binaryPrecision ({binaryPrecision}) cannot be negative.");
        }

        uint magnitude = value > 0
            ? (uint)value
            : unchecked((uint)(-value));

        int valueSize = BitOperations.Log2(magnitude) + 1;
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(long value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 63)
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

        BigInteger absValue = BigInteger.Abs((BigInteger)value);
        int valueSize = (int)absValue.GetBitLength();
        int effectivePrecision = Math.Max(binaryPrecision, valueSize);
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        _mantissa = (BigInteger)value << applyGuardBits;
        Scale = binaryScaler - effectivePrecision + valueSize;
        _size = guardBitsToAdd + effectivePrecision;

        AssertValid();
    }

    public BigFloat(ulong value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 64)
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

    // future: maybe change addedBinaryPrecision to binaryPrecision. Set default to 45(53-8)
    // Note: changed the default 32(GuardBits) to 24 since double is not exact. There is no great solution to this but moving 8 bits of the mantissa into the GuardBit area is a good compromise. In the past we had zero mantissa bits stored in the GuardBit area and this created some issues.  If we move all 32 bits into the GuardBit area then we would just be left with 53-32 GuardBits= 21 bits. A balance of 8 bits was selected but it is best if the user selects the value.
    public BigFloat(double value, int binaryScaler = 0, int addedBinaryPrecision = 24)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        long mantissa = bits & 0xfffffffffffffL;
        int exp = (int)((bits >> 52) & 0x7ffL);

        if (exp == 2047)  // 2047 represents inf or NAN
        { //special values
            if (double.IsNaN(value))
            {
                ThrowInvalidInitializationException("Value is NaN");
            }
            else if (double.IsInfinity(value))
            {
                ThrowInvalidInitializationException("Value is infinity");
            }
        }
        else if (exp != 0)
        {
            mantissa |= 0x10000000000000L;
            if (value < 0)
            {
                mantissa = -mantissa;
            }
            _mantissa = new BigInteger(mantissa) << addedBinaryPrecision;
            Scale = exp - 1023 - 52 + binaryScaler + GuardBits - addedBinaryPrecision;
            _size = 53 + addedBinaryPrecision; //_size = BitOperations.Log2((ulong)Int);
        }
        else // exp is 0 so this is a denormalized float (leading "1" is "0" instead)
        {
            // 0.00000000000|00...0001 -> smallest value (Epsilon)  Int:1, Scale: Size:1
            if (mantissa == 0)
            {
                _mantissa = 0;
                Scale = binaryScaler + GuardBits - addedBinaryPrecision;
                _size = 0;
            }
            else
            {
                int size = GetBitLength((ulong)mantissa);
                if (value < 0)
                {
                    mantissa = -mantissa;
                }
                _mantissa = (new BigInteger(mantissa)) << addedBinaryPrecision;
                Scale = -1023 - 52 + 1 + binaryScaler + GuardBits - addedBinaryPrecision;
                _size = size + addedBinaryPrecision;
            }
        }

        AssertValid();
    }

    public BigFloat(float value, int binaryScaler = 0)
    {
        int bits = BitConverter.SingleToInt32Bits(value);
        int mantissa = bits & 0x007fffff;
        int exp = (int)((bits >> 23) & 0xffL);

        if (exp != 0)
        {
            if (exp == 255)
            { //special values
                if (float.IsNaN(value))
                {
                    ThrowInvalidInitializationException("Value is NaN");
                }
                else if (float.IsInfinity(value))
                {
                    ThrowInvalidInitializationException("Value is infinity");
                }
            }
            // Add leading 1 bit
            mantissa |= 0x800000;
            if (value < 0)
            {
                mantissa = -mantissa;
            }
            _mantissa = new BigInteger(mantissa) << GuardBits;
            Scale = exp - 127 - 23 + binaryScaler;
            _size = 24 + GuardBits;
        }
        else // exp is 0 so this is a denormalized(Subnormal) float (leading "1" is "0" instead)
        {
            if (mantissa == 0)
            {
                _mantissa = 0;
                Scale = binaryScaler;
                _size = 0;
            }
            else
            {
                BigInteger mant = new(value >= 0 ? mantissa : -mantissa);
                _mantissa = mant << GuardBits;
                Scale = -126 - 23 + binaryScaler; //hack: 23 is a guess
                _size = GuardBits - BitOperations.LeadingZeroCount((uint)mantissa) + GuardBits;
            }
        }

        AssertValid();
    }


    /// <summary>
    /// Bit length calculation using hardware intrinsics when available
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetBitLength(ulong value)
    {
        if (value == 0) return 0;

        // Use hardware intrinsics for better performance when available
        if (Lzcnt.X64.IsSupported)
        {
            return 64 - (int)Lzcnt.X64.LeadingZeroCount(value);
        }

        return BitOperations.Log2(value) + 1;
    }

    //////////////////////// end of move back bigfloat.cs /////////////////////////////////////////

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
            // fallback to your existing GetBitLength (rarely hit)
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
        => (BinaryExponent + 1023) is (>= 1 and <= 2046);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as an
    /// IEEE 754 double-precision value, allowing both normalized and
    /// denormalized (subnormal) forms. Precision loss due to rounding is ignored. 
    /// </summary>
    public bool FitsInADoubleWithDenormalization
        => (BinaryExponent + 1023) is (>= -1074 and <= 2046);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as a
    /// normalized IEEE 754 single-precision (float) value without exponent
    /// overflow or underflow. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInAFloat
        => (BinaryExponent + 127) is (>= 1 and <= 254);

    /// <summary>
    /// Returns true if this <see cref="BigFloat"/> can be represented as an
    /// IEEE 754 single-precision (float) value, allowing both normalized and
    /// denormalized (subnormal) forms. Precision loss due to rounding is ignored.
    /// </summary>
    public bool FitsInAFloatWithDenormalization
        => (BinaryExponent + 127) is (>= -149 and <= 254);


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