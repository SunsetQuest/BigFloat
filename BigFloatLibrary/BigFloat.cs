// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.
// Note: Starting 2/25, ChatGPT/Claude/GitHubCopilot/Grok were used in library development.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

// BigFloat.cs (this file) - contains core BigFloat struct and its core properties and methods.
// BigIntegerTools.cs - helper methods for working with BigIntegers
// optional: (contains additional methods that are not part of the core)
//   BigFloatCompareTo.cs: extra string functions as well as the IComparable, IEquatable, and IFormattable interfaces.
//   BigFloatExtended.cs: extra functions that do not fall into the other categories.
//   BigFloatMath.cs: extra math functions like Log, Sqrt, Exp, etc.
//   BigFloatParsing.cs: extra parsing functions for converting strings to BigFloat.
//   BigFloatRandom.cs: functions for generating random BigFloats
//   BigFloatRoundShiftTruncate.cs: extra rounding, shifting, truncating, or splitting functions.
//   BigFloatStringsAndSpans.cs: extra functions related to converting BigFloat to strings/spans
//   Constants.cs,ConstantInfo.cs,ConstantsCatalog.cs,ConstantBuilder.cs: extra features for returning common constants. 

/// <summary>
/// BigFloat stores a BigInteger with a floating radix point.
/// </summary>
public readonly partial struct BigFloat
// IFormattable, ISpanFormattable - see BigFloatCompareTo.cs
// IComparable, IComparable<BigFloat>, IEquatable<BigFloat> - see BigFloatCompareTo.cs
{
    /// <summary>
    /// The number of extra hidden guard bits in the mantissa to aid in better precision. 
    /// GuardBits are a fixed amount of least-significant sub-precise bits.
    /// These bits help guard against some nuisances such as "7" * "9" being "60". 
    /// </summary>
    public const int GuardBits = 32;  // 0-62, must be even (for sqrt)

    /// <summary>
    /// Performance tuning constants for optimized operations
    /// </summary>
    private const int KARATSUBA_THRESHOLD = 256;  // Threshold for switching to Karatsuba multiplication
    private const int BURNIKEL_ZIEGLER_THRESHOLD = 1024;  // Threshold for advanced division algorithms

    /// <summary>
    /// Gets the full integer's data bits, including guard bits.
    /// </summary>
    private readonly BigInteger _mantissa;

    /// <summary>
    /// _size is the count of precision bits and equals ABS(DataBits).GetBitLength(). Using ABS handles power-of-two negatives
    /// consistently for positive and negative values.
    /// _size includes GuardBits (the Size property subtracts them).
    /// _size does not include rounding from GuardBits; for example 11[111...111] (where [111...111] represents the guard bits)
    /// is still 2 bits, so the user will see 0b100 with a size of 2.
    /// _size is 0 only when 'DataBits==0'. When BigFloat is Zero, the size is zero.
    /// </summary>
    internal readonly int _size;

    //future: Possible future feature for repeating decimals
    ///// <summary>
    ///// When positive, it's the number of least significant digits in DataBits that repeat.
    /////    Example: DataBits:11.001(with _extraPrecOrRepeat = 3) would be 11.001001001001...
    ///// When negative, it is the number of extra virtual zeros tacked on the end of the internal DataBits for better precision and accuracy.
    ///// Example: 11.001(with _extraPrecOrRepeat = -3) would be the same as 11.001000
    /////   For the above example "000" would not take up any space and is also guaranteed to be all 0 bits.
    ///// When zero, this feature does not get used. (Default)
    ///// </summary>
    // private readonly int _extraPrecOrRepeat;

    /// <summary>
    /// The binary Scale (or -Accuracy) is the amount to left shift (<<) the DataBits (i.e. right shift the radix point) to get to the desired value.
    /// When Scale is Zero, the value is equal to the DataBits with the GuardBits removed. (i.e. DataBits >> GuardBits)
    /// When BigFloat is Zero, scale is the point of least accuracy.
    /// note: _scale = Scale-GuardBits (or Scale = _scale + GuardBits)
    /// 11|1.1000  Scale < 0
    /// 111.|1000  Scale ==0
    /// 111.10|00  Scale > 0
    /// </summary>
    public readonly int Scale { get; init; }

    /// <summary>
    /// The Size is the precision. It is the number of bits required to hold the number. 
    /// GuardBits are subtracted out. Use SizeWithGuardBits to include GuardBits.
    /// </summary>
    public readonly int Size => Math.Max(0, _size - GuardBits);

    /// <summary>
    /// Returns the base-2 exponent of the number. This is the amount shift a simple 1 bit to the leading bit location.
    /// Examples: dataBits:11010 with BinExp: 3 -> 1101.0 -> 1.1010 x 2^ 3  
    ///           dataBits:11    with BinExp:-1 -> 0.11   -> 1.1    x 2^-1 
    /// </summary>
    public int BinaryExponent => Scale + _size - GuardBits - 1;

    //see BigFloatZeroNotes.txt for notes
    /// <summary>
    /// Returns true if the value is essentially zero.
    /// </summary>
    public bool IsZero => _size < 32 && ((_size == 0) || (_size + Scale < 32));

    /// <summary>
    /// Returns true if there is less than 1 bit of precision. However, a false value does not guarantee that the number is precise. 
    /// </summary>
    public bool IsOutOfPrecision => _size < GuardBits;

    /// <summary>
    /// Rounds and returns true if this value is positive. Zero is not considered positive or negative. Only the top bit in GuardBits is counted.
    /// </summary>
    public bool IsPositive => _mantissa.Sign > 0 && !IsZero;

    /// <summary>
    /// Rounds and returns true if this value is negative. Only the top bit in GuardBits is counted.
    /// </summary>
    public bool IsNegative => _mantissa.Sign < 0 && !IsZero;

    /// <summary>
    /// Rounds with GuardBits and returns -1 if negative, 0 if zero, and +1 if positive.
    /// </summary>
    public int Sign => !IsZero ? _mantissa.Sign : 0;

    /// <summary>
    /// Returns the default zero with with a zero size, precision, scale, and accuracy.
    /// </summary>
    [Obsolete("Use the integer value 0 instead or ZeroWithAccuracy.")]
    public static BigFloat Zero => new(BigInteger.Zero, 0, 0);

    /// <summary>
    /// Returns a '1' with only 1 bit of precision. (1 << GuardBits)
    /// </summary>
    [Obsolete("Use the integer value 1 instead or OneWithAccuracy.")]
    public static BigFloat One => new(BigInteger.One << GuardBits, 0, GuardBits + 1);

    const double LOG2_OF_10 = 3.32192809488736235;

    /// <summary>
    /// Returns a zero BigFloat with specified least precision for maintaining accuracy context.
    /// Value can range from -32(GuardBits) to Int.MaxValue.
    /// Example: -4 would result in 0.0000(binary) + GuardBits appended as well.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat ZeroWithAccuracy(int accuracy)
    {
        return new BigFloat(BigInteger.Zero, -accuracy, 0);
    }

    /// <summary>
    /// Returns a one BigFloat with specified least precision for maintaining accuracy context
    /// </summary>
    /// <param name="accuracy">The wanted accuracy between -32(GuardBits) to Int.MaxValue.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat OneWithAccuracy(int accuracy)
    {
        //ArgumentOutOfRangeException.ThrowIfLessThan(accuracy, -32);
        return new(BigInteger.One << (GuardBits + accuracy), -accuracy, GuardBits + 1 + accuracy);
    }

    /////////////////////////    CONVERSION  FUNCTIONS     /////////////////////////

    /// <summary>
    /// Constructs a BigFloat using the raw elemental parts. The user is responsible to pre-up-shift rawValue and set <paramref name="binaryScaler"/> and <paramref name="rawValueSize"/> with respect to the GuardBits.
    /// </summary>
    /// <param name="rawValue">The raw mantissa value as a BigInteger. It should INCLUDE the GuardBits.</param>
    /// <param name="binaryScaler">How much should the <paramref name="rawValue"/> be shifted or scaled? This shift (base-2 exponent) will be applied to the <paramref name="integerPart"/>.</param>
    /// <param name="rawValueSize">The size of rawValue. </param>
    private BigFloat(BigInteger rawValue, int binaryScaler, int rawValueSize)
    {
        _mantissa = rawValue;
        Scale = binaryScaler;
        _size = rawValueSize;

        AssertValid();
    }

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

        if (value == int.MinValue && binaryPrecision == 31) binaryPrecision++; // Handle special case when value is MinValue

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

    public static BigFloat CreateWithPrecisionFromValue(long value, bool valueIncludesGuardBits = false, int adjustBinaryPrecision = 0, int binaryScaler = 0)
    {
        if (value == 0)
        {
            return new BigFloat(BigInteger.Zero, binaryScaler - adjustBinaryPrecision, 0);
        }

        ulong magnitude = value > 0
            ? (ulong)value
            : unchecked((ulong)(-value));

        int valueSize = (int)ulong.Log2(magnitude) + 1;

        int effectivePrecision = valueSize + adjustBinaryPrecision;
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        BigInteger _mantissa = (BigInteger)value << applyGuardBits;
        return new BigFloat(_mantissa,
            binaryScaler - effectivePrecision + valueSize,
            guardBitsToAdd + effectivePrecision);
    }

    public static BigFloat CreateWithPrecisionFromValue(ulong value, bool valueIncludesGuardBits = false, int adjustBinaryPrecision = 0, int binaryScaler = 0)
    {
        if (value == 0)
        {
            return new BigFloat(BigInteger.Zero, binaryScaler - adjustBinaryPrecision, 0);
        }

        int valueSize = (int)ulong.Log2(value) + 1;

        int effectivePrecision = valueSize + adjustBinaryPrecision;
        int guardBitsToAdd = valueIncludesGuardBits ? 0 : GuardBits;
        int applyGuardBits = guardBitsToAdd + (effectivePrecision - valueSize);

        BigInteger _mantissa = (BigInteger)value << applyGuardBits;
        return new BigFloat(_mantissa,
            binaryScaler - effectivePrecision + valueSize,
            guardBitsToAdd + effectivePrecision);
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

        if (value == long.MinValue && binaryPrecision == 63) binaryPrecision++; // Handle special case when value is MinValue

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

                // Ensure at least one in-precision bit exists outside of the guard area so that
                // subnormal inputs (e.g., double.Epsilon) are not represented entirely within
                // the guard bits. When that happens, Size becomes zero and the value is reported
                // as an integer. Shift the mantissa until we have one precision bit and adjust
                // the scale so the numeric value remains unchanged.
                int deficit = GuardBits + 1 - _size;
                if (deficit > 0)
                {
                    _mantissa <<= deficit;
                    _size += deficit;
                    Scale -= deficit;
                }
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

    /// <summary>
    /// Constructs a BigFloat using the raw elemental components. The user is responsible to pre-up-shift rawValue and set <paramref name="binaryScaler"/> and <paramref name="mantissaSize"/> with respect to the GuardBits.
    /// </summary>
    /// <param name="mantissa">The raw integer part that includes the GuardBits.</param>
    /// <param name="binaryScaler">How much should the <paramref name="mantissa"/> be shifted or scaled? This shift (base-2 exponent) will be applied to the <paramref name="integerPart"/>.</param>
    /// <param name="mantissaSize">The size of the <paramref name="mantissa"/>.</param>
    public static BigFloat CreateFromRawComponents(BigInteger mantissa, int binaryScaler, int mantissaSize)
    {
        return new(mantissa, binaryScaler, mantissaSize);
    }

    [DoesNotReturn]
    private static void ThrowInvalidInitializationException(string reason)
    {
        throw new OverflowException($"Invalid BigFloat initialization: {reason}");
    }
    ///////////////////////// [END] INIT / CONVERSION  FUNCTIONS [END] /////////////////////////

    /// <summary>
    /// Checks to see if the value is an integer.
    /// Returns True if...
    ///  - the scale >= (GuardBits/2)
    ///  - or, all bits between the point and 16 bits into the GuardBits are all 0 or 1.
    /// 
    /// If an integer, it should follow that ...
    ///  - it should not round-up based on GuardBits
    /// -  Ceiling() would not round-up and Floor() would not round-down.
    /// </summary>
    public bool IsInteger  //v8 - check to see if all the bits between the point and the 16 most significant guard bits are uniform. (111.??|?)
    {
        get => Ceiling() == Floor();
    }

    /// <summary>
    /// Tests to see if the number is in the format of "10000000..." after rounding guard-bits.
    /// </summary>
    public bool IsOneBitFollowedByZeroBits => BigInteger.IsPow2(BigInteger.Abs(_mantissa) >> (GuardBits - 1));


    /// <summary>
    /// Returns the number of matching leading bits with rounding. e.g. 10.111 - 10.101 is 00.010 so returns 4
    /// The Exponent(or Scale + _size) is considered. e.g. 100. and 1000. would return 0
    /// If the signs do not match then 0 is returned. 
    /// When a rollover is near these bits are included. e.g. 11110 and 100000 returns 3
    /// GuardBits are included.
    /// </summary>
    /// <param name="sign">Returns the sign of a-b. Example: If a is larger, sign is set to 1.</param>
    public static int NumberOfMatchingLeadingBitsWithRounding(BigFloat a, BigFloat b, out int sign)
    {
        int maxSize = Math.Max(a._size, b._size);
        int expDiff = a.BinaryExponent - b.BinaryExponent;
        if (maxSize == 0 || a.Sign != b.Sign || Math.Abs(expDiff) > 1)
        {
            sign = (expDiff > 0) ? a.Sign : -b.Sign;
            return 0;
        }

        int scaleDiff = a.Scale - b.Scale;

        BigInteger temp = (scaleDiff < 0) ?
                a._mantissa - (b._mantissa << scaleDiff)
                : (a._mantissa >> scaleDiff) - b._mantissa;

        sign = temp.Sign;

        return maxSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    /// <summary>
    /// Returns the number of matching leading bits that exactly match. GuardBits are included.
    /// i.e. The number of leading bits that exactly match.
    /// e.g. 11010 and 11111 returns 2
    /// e.g. 100000 and 111111 returns 1
    /// If the signs do not match then 0 is returned.
    /// The scale and precision(size) is ignored. e.g. 11101000000 and 11111 returns 3
    /// </summary>
    public static int NumberOfMatchingLeadingMantissaBits(BigFloat a, BigFloat b)
    {
        if (a.Sign != b.Sign) { return 0; }

        int sizeDiff = a._size - b._size;
        int newSize = sizeDiff > 0 ? b._size : a._size;

        if (newSize == 0) { return 0; }

        BigInteger temp = (sizeDiff < 0) ?
                a._mantissa - (b._mantissa << sizeDiff)
                : (a._mantissa >> sizeDiff) - b._mantissa;

        return newSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    ///////////////////////// Min / Max /////////////////////////

    /// <summary>
    /// Returns the smaller of two <see cref="BigFloat"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Min(in BigFloat x, in BigFloat y)
        => SelectMinMax(in x, in y, pickMin: true);

    /// <summary>
    /// Returns the larger of two <see cref="BigFloat"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Max(in BigFloat x, in BigFloat y)
        => SelectMinMax(in x, in y, pickMin: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigFloat SelectMinMax(in BigFloat x, in BigFloat y, bool pickMin)
    {
        if (x.Scale == y.Scale && x._size == y._size && x._mantissa == y._mantissa)
        {
            return x;
        }

        if (x._size > GuardBits && y._size > GuardBits)
        {
            int sx = x._mantissa.Sign;
            int sy = y._mantissa.Sign;
            if (sx != sy)
            {
                if (pickMin)
                {
                    return sx < sy ? x : y;
                }

                return sx > sy ? x : y;
            }
        }

        int cmp = Compare(in x, in y);

        if (cmp == 0)
        {
            return TieBreakEqual(in x, in y);
        }

        if (pickMin)
        {
            return cmp < 0 ? x : y;
        }

        return cmp > 0 ? x : y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigFloat TieBreakEqual(in BigFloat x, in BigFloat y)
    {
        if (x._size != y._size)
        {
            return x._size >= y._size ? x : y;
        }

        if (x.Scale != y.Scale)
        {
            return x.Scale <= y.Scale ? x : y;
        }

        return x;
    }

    ///////////////////////// Operator Overloads: BigFloat <--> BigFloat /////////////////////////

    /// <summary>Returns true if the left side BigFloat is equal to the right side BigFloat.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(BigFloat left, BigFloat right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right BigFloat.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BigFloat left, BigFloat right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) < 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) <= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) >= 0;
    }

    ///////////////////////// Operator Overloads: BigFloat <--> BigInteger /////////////////////////



    /// <summary>
    /// Enhanced division with adaptive algorithm selection based on operand sizes
    /// </summary>
    public static BigFloat operator /(BigFloat numerator, BigFloat denominator)
    {
        const int SMALL_NUMBER_THRESHOLD = 64;  // Threshold for small number optimizations
        
        // Early exit for zero divisor
        if (denominator.IsStrictZero)
        {
            throw new DivideByZeroException("Division by zero");
        }

        // Use optimized algorithm for small numbers
        if (numerator._size < SMALL_NUMBER_THRESHOLD && denominator._size < SMALL_NUMBER_THRESHOLD)
        {
            return DivideSmallNumbers(numerator, denominator);
        }

        // Use advanced division algorithms for large numbers
        if (numerator._size > BURNIKEL_ZIEGLER_THRESHOLD || denominator._size > BURNIKEL_ZIEGLER_THRESHOLD)
        {
            return DivideLargeNumbers(numerator, denominator);
        }

        // Standard division algorithm
        return DivideStandard(numerator, denominator);
    }

    /// <summary>
    /// Optimized division for small numbers using hardware arithmetic when possible
    /// </summary>
    private static BigFloat DivideSmallNumbers(BigFloat divisor, BigFloat dividend)
    {
        // future: implement optimized small number division using hardware arithmetic
        return DivideStandard(divisor, dividend);
    }

    /// <summary>
    /// Advanced division algorithm for large numbers
    /// </summary>
    private static BigFloat DivideLargeNumbers(BigFloat divisor, BigFloat dividend)
    {
        // future: implement Burnikel-Ziegler division algorithm for better performance on large numbers
        return DivideStandard(divisor, dividend);
    }

    /// <summary>
    /// Standard division algorithm with optimizations
    /// </summary>
    private static BigFloat DivideStandard(BigFloat divisor, BigFloat dividend)
    {
        // find the size of the smaller input to determine output size
        int outputSize = Math.Min(divisor.Size, dividend.Size);

        // If we right-shift divisor to align it with dividend and then divisor < dividend, then we need to decrement the output size.
        // This is because we would only have a partial bit of precision on the last bit, and it could introduce error.
        // note: We could also left shift dividend so it is left aligned with divisor but that would be more expensive. (but could be more accurate)
        // note: We can maybe speed this up by just checking the top 32 or 64 bits of each.
        if (divisor._mantissa >> (divisor.Size - dividend.Size) <= dividend._mantissa)
        {
            outputSize--;
        }

        // We need to oversize T (using left shift) so when we divide, it is the correct size.
        int wantedSizeForT = dividend.Size + outputSize + GuardBits;

        int leftShiftTBy = wantedSizeForT - divisor.Size;

        BigInteger leftShiftedT = divisor._mantissa << leftShiftTBy;
        BigInteger resIntPart = leftShiftedT / dividend._mantissa;

        int resScalePart = divisor.Scale - dividend.Scale - leftShiftTBy + GuardBits;
        int sizePart = (int)BigInteger.Abs(resIntPart).GetBitLength();

        return new BigFloat(resIntPart, resScalePart, sizePart);
    }

    /// <summary>
    /// Performs a Modulus operation. 
    /// For positive values, Modulus is identical to Remainder, for negatives, Modulus and Remainder differ. 
    /// The remainder is slightly faster.
    /// </summary>
    public static BigFloat Remainder(BigFloat dividend, BigFloat divisor)
    {
        int scaleDiff = dividend.Scale - divisor.Scale;
        if (divisor._mantissa == 0)
            throw new DivideByZeroException();

        if (scaleDiff == 0)
            return new(dividend._mantissa % divisor._mantissa, divisor.Scale, true);

        if (scaleDiff < 0)
        {
            int t = -scaleDiff;
            // ((A % (B<<t)) >> t) == ((A >> t) % B)  — no giant left shifts
            BigInteger r = (dividend._mantissa >> t) % divisor._mantissa;
            return new(r, divisor.Scale, true);
        }
        else
        {
            // s > 0
            BigInteger m = divisor._mantissa;

            // Optional fast path when divisor has ≥ s trailing zeros:
            int tz = (int)BigInteger.TrailingZeroCount(BigInteger.Abs(m));
            if (scaleDiff <= tz)
            {
                BigInteger r = dividend._mantissa % (m >> scaleDiff);
                return new(r, dividend.Scale, true);
            }

            // General path: ((A<<s) % m) >> s  →  ((|A| % |m|) * (2^s mod |m|) % |m|) with sign(A), then >> s
            int u = scaleDiff - tz;
            BigInteger mOdd = m >> tz;                  // strip 2^tz from modulus
            BigInteger mAbs = BigInteger.Abs(mOdd);

            BigInteger aRem = dividend._mantissa % mOdd;        // sign matches dividend
            BigInteger pow2 = BigInteger.ModPow(2, u, mAbs);    // O(log s), no big temps
            BigInteger tRem = (BigInteger.Abs(aRem) * pow2) % mAbs;
            if (aRem.Sign < 0) tRem = -tRem;

            BigInteger r2 = tRem >> u;
            return new(r2, dividend.Scale, true);
        }
    }

    /// <summary>
    /// Performs a modulus operation. For negative numbers there are two approaches, a math and programmers version. For negative numbers this version uses the programmers version.
    /// </summary>
    public static BigFloat operator %(BigFloat dividend, BigFloat divisor)
    {
        return Remainder(dividend, divisor);
    }

    /// <summary>
    /// Mathematical modulo operation. 
    /// The result has the same sign as <paramref name="divisor"/>.
    /// For positive values, modulo is identical to Remainder
    /// Implemented as: r = Remainder(dividend, divisor); if (r == 0 or sign(r)==sign(divisor)) return r; else return r + divisor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Mod(in BigFloat dividend, in BigFloat divisor)
    {
        // Remainder is scale-aware and avoids huge shifts; it throws on divide-by-zero. 
        // (See implementation in this file.) 
        var rem = Remainder(dividend, divisor);  

        // Exact multiple ⇒ keep exact zero (and its accuracy context).
        if (rem._mantissa.IsZero) return rem;

        // Already the right sign (same as divisor) ⇒ done.
        // Uses raw mantissa signs to avoid CompareTo/Zero construction or rounding.
        if (rem._mantissa.Sign == divisor._mantissa.Sign) return rem;

        // Otherwise, shift into the correct range by adding one divisor.
        return rem + divisor;
    }


    /// <summary>
    /// Splits the BigFloat into integer and fractional parts. (i.e. ModF)
    /// </summary>
    public (BigFloat integer, BigFloat fraction) SplitIntegerAndFractionalParts()
    {
        int bitsToClear = GuardBits - Scale;

        if (bitsToClear <= 0) return (this, ZeroWithAccuracy(0));
        if (bitsToClear >= _size) return (ZeroWithAccuracy(0), this);

        // For integer part, use shift operations to avoid two's complement issues
        BigInteger intPart = ClearLowerNBits(_mantissa, bitsToClear);
        BigInteger fracPart = _mantissa - intPart;

        return (
            new BigFloat(intPart, Scale, _size),
            fracPart.IsZero ? ZeroWithAccuracy(0) : new BigFloat(fracPart, Scale, (int)fracPart.GetBitLength())
        );
    }

    /// <summary>
    /// Bitwise Complement Operator - Reverses each bit in the data bits. Scale is not changed.
    /// The size is reduced by at least 1 bit. This is because the leading bit is flipped to a zero.
    /// </summary>
    public static BigFloat operator ~(BigFloat value)
    {
        BigInteger temp = value._mantissa ^ ((BigInteger.One << value._size) - 1);
        return new(temp, value.Scale, true);
    }

    /// <summary>
    /// Left shifts by increasing the scale by the amount left shift amount. 
    /// The precision is unchanged.
    /// </summary>
    /// <param name="value">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator <<(BigFloat value, int shift)
    {
        return new(value._mantissa, value.Scale + shift, value._size);
    }

    /// <summary>
    /// Right shifts by decreasing the scale by the amount right shift amount. 
    /// The precision is unchanged.
    /// </summary>
    /// <param name="value">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator >>(BigFloat value, int shift)
    {
        return new(value._mantissa, value.Scale - shift, value._size);
    }

    /// <summary>
    /// Increments the integer part of a BigFloat by one.
    /// </summary>
    public static BigFloat operator ++(BigFloat r)
    {
        // assuming GuardBits is 4:
        //  1111|1111__. => 1111|1111<< 6   +1  =>  1111|1111__.
        //  1111|1111_.  => 1111|1111<< 5   +1  =>  10000|0000#.
        //  1111|1111.   => 1111|1111<< 4   +1  =>  10000|0000.
        //  1111|1.111   => 1111|1111<< 1   +1  =>  10000|0.111
        // 1111.|1111    => 1111|1111<< 0   +1  =>  10000.|1111
        // 111.1|1111    => 1111|1111<< -1  +1  =>  1000.1|1111
        // .1111|1111    => 1111|1111<< -4  +1  =>  1.1111|1111
        //.01111|1111    => 1111|1111<< -5  +1  =>  1.01111|1111

        int onesPlace = GuardBits - r.Scale;

        if (onesPlace < 1)
        {
            return r; // A => -2 or less
        }

        // In the special case, we may not always want to round up when adding a 1 bit just below the LSB. 
        if (onesPlace == -1 && !r._mantissa.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r._mantissa + (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        // int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //future: for performance, faster just to calc?
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);
        return new BigFloat(intVal, r.Scale, sizeVal);
    }

    /// <summary>
    /// Decrements the integer part of a BigFloat by one.
    /// </summary>
    public static BigFloat operator --(BigFloat r)
    {
        int onesPlace = GuardBits - r.Scale;

        if (onesPlace < 1)
        {
            return r;
        }

        // In the special case, we may not always want to round up when adding a 1 bit just below the LSB. 
        if (onesPlace == -1 && !r._mantissa.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r._mantissa - (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        //int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //future: faster just to calc?
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);

        return new BigFloat(intVal, r.Scale, sizeVal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator +(BigFloat r)
    {
        return r;
    }

    /// <summary>
    /// Negates a BigFloat value (i.e. changes its sign).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator -(BigFloat r)
    {
        return new(-r._mantissa, r.Scale, r._size);
    }

    public static BigFloat operator +(BigFloat r1, BigFloat r2)
    {
        // Shortcuts (to benchmark, does it actually save any time)
        // Given GuardBits = 8, a number like "B2D"00 + 0.00"3F" should be just "B2D"00 since the smaller number is below the precision range.
        //
        // Example: "12345678"9ABCDEF0________.         (Size: 29, _size: 61, Scale: 64)
        //        +                  "12"34560.789A     (Size:  5, _size: 37, Scale: 20)
        //        =  12345678"9ABCDEF0________.
        //         
        // (if 64(r1.Scale) > 37(r2._size) + (20)r2.Scale then just return r1)

        int scaleDiff = r1.Scale - r2.Scale;

        // Optimized shortcuts for very different scales
        if (scaleDiff > r2._size)
        {
            return r1;
        }

        if (-scaleDiff > r1._size)
        {
            return r2;
        }

        // Any Precision that is below the precision of the number with a larger scale would be dropped off.
        // Example: all the 7's would just be dropped off.
        //   "5555"00000     input:5555 shift:5(decimal)
        //    +"55577777"
        //  -------------
        //     "49"9922223   <--- answer is 50, only 2 significant digits.

        if (r1.Scale < r2.Scale)
        {
            BigInteger intVal0 = RoundingRightShift(r1._mantissa, -scaleDiff) + r2._mantissa;
            int resSize0 = (int)BigInteger.Abs(intVal0).GetBitLength();
            return new BigFloat(intVal0, r2.Scale, resSize0);
        }

        BigInteger intVal = r1._mantissa + RoundingRightShift(r2._mantissa, scaleDiff);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        return new BigFloat(intVal, r1.Scale, sizeVal);
    }

    public static BigFloat operator +(BigFloat r1, int r2) // Ryan
    {
        if (int.Log2(int.Abs(r2)) + 1 + (GuardBits - r1.Scale) <= 0)
        {
            return r1;
        }
        BigInteger addVal = (BigInteger)r2 << (GuardBits - r1.Scale);
        addVal += r1._mantissa;

        return new BigFloat(addVal, r1.Scale, (int)BigInteger.Abs(addVal).GetBitLength());
    }

    ///////////////////////// Rounding, Shifting, Truncate /////////////////////////

    /// <summary>
    /// Checks to see if this integerPart would round away from zero.
    /// </summary>
    /// <param name="bi">The BigInteger we would like check if it would round up.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public static bool WouldRoundUp(BigInteger bi)
    {
        return WouldRoundUp(bi, GuardBits);
    }

    /// <summary>
    /// Checks to see if the integerPart would round away from zero.
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRoundUp()
    {
        return WouldRoundUp(_mantissa, GuardBits);
    }

    /// <summary>
    /// Checks to see if this integerPart would round away from zero. 
    /// e.g. 11010101 with bottomBitsRemoved=3 would be 11011
    /// </summary>
    /// <param name="bottomBitsRemoved">The number of newSizeInBits from the least significant bit where rounding would take place.</param>
    public bool WouldRoundUp(int bottomBitsRemoved)
    {
        return WouldRoundUp(_mantissa, bottomBitsRemoved);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the GuardBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool WouldRoundUp(BigInteger val, int bottomBitsRemoved)
    {
        bool isPos = val.Sign >= 0;
        return isPos ^ ((isPos ? val : val - 1) >>> (bottomBitsRemoved - 1)).IsEven;
    }


    /// <summary>
    /// Computes the rounded mantissa without guard bits for any BigInteger input.
    /// Rounding is applied based on the guard bits; assumes the input is non-negative (mantissa is typically unsigned).
    /// </summary>
    /// <param name="x">The input mantissa including guard bits.</param>
    /// <returns>The rounded and shifted mantissa.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger GetRoundedMantissa(BigInteger x)
    {
        return RoundingRightShift(x, GuardBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigInteger GetRoundedMantissa() => GetRoundedMantissa(_mantissa);

    /// <summary>
    /// Computes the rounded mantissa without guard bits, also updating the size (e.g., bit length or exponent)
    /// if rounding causes a carry-over (e.g., all guard bits set, leading to increment).
    /// This is useful in normalization steps where overflow affects the exponent.
    /// </summary>
    /// <param name="x">The input mantissa including guard bits.</param>
    /// <param name="size">The current size (e.g., bit count); incremented if carry occurs.</param>
    /// <returns>The rounded and shifted mantissa.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger GetRoundedMantissa(BigInteger x, ref int size)
    {
        return RoundingRightShift(x, GuardBits, ref size);
    }

    /// <summary>
    /// Represents the raw mantissa including guard bits.
    /// </summary>
    public readonly BigInteger RawMantissa => _mantissa;

    /// <summary>
    /// Gets the integer part of the BigFloat with no scaling is applied. GuardBits are rounded and removed.
    /// </summary>
    public readonly BigInteger RoundedMantissa => RoundingRightShift(_mantissa, GuardBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger GetIntegralValue(BigFloat value)
    {
        if (value.Scale >= 0)
        {
            BigInteger mantissaWithScale = value._mantissa << value.Scale;
            return RoundingRightShift(mantissaWithScale, GuardBits);
        }

        BigInteger mantissa = RoundingRightShift(value._mantissa, GuardBits);
        int fractionalBits = -value.Scale;
        return (mantissa.Sign >= 0) ? (mantissa >> fractionalBits) : -((-mantissa) >> fractionalBits);
    }

    /// <summary>
    /// Gets the integer part of the BigFloat with no scaling is applied. GuardBits are rounded and removed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigInteger GetIntegralValue() => GetIntegralValue(this);

    /// <summary>
    /// Truncates a value by a specified number of bits by increasing the scale and reducing the precision.
    /// If the most significant bit of the removed bits is set then the least significant bit will increment away from zero. 
    /// e.g. 10.10010 << 2 = 10.101
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Example: 11.11 with 1 bit removed would result in 100.0 (the same size)
    /// This function uses the internal BigInteger RightShiftWithRound().
    /// Also see: ReducePrecision, RightShiftWithRoundWithCarry, RightShiftWithRound
    /// </summary>
    /// <param name="targetBitsToRemove">Specifies the target number of least-significant bits to remove.</param>
    public static BigFloat TruncateByAndRound(BigFloat x, int targetBitsToRemove)
    {
        if (targetBitsToRemove < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetBitsToRemove), $"Param {nameof(targetBitsToRemove)} must be 0 or greater.");
        }

        int newScale = x.Scale + targetBitsToRemove;
        int size = x._size;

        BigInteger b = RoundingRightShift(x._mantissa, targetBitsToRemove, ref size);

        return new(b, newScale, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat TruncateByAndRound(int targetBitsToRemove) => TruncateByAndRound(this, targetBitsToRemove);

    /// <summary>
    /// Rounds to nearest integer, preserving accuracy.
    /// </summary> 
    public static BigFloat Round(BigFloat x)
    {
        int bitsToClear = GuardBits - x.Scale;

        if (bitsToClear <= 0) return x;
        if (bitsToClear > x._size)
            return ZeroWithAccuracy(x.Accuracy);
        if (bitsToClear == x._size)
            return OneWithAccuracy(x.Accuracy);

        //BigInteger result= RightShiftWithRound(Mantissa, bitsToClear) << bitsToClear;
        //return new BigFloat(result, Scale, _size);

        //// below keeps the same size (it does not rollover to 1 bit larger)
        (BigInteger result, bool carry) = RoundingRightShiftWithCarry(x._mantissa, bitsToClear);
        return new BigFloat(result << bitsToClear, x.Scale + (carry ? 1 : 0), x._size);
    }

    /// <summary>
    /// Rounds to nearest integer, preserving accuracy.
    /// </summary> 
    public BigFloat Round() => Round(this);

    /// <summary>
    /// Rounds to nearest integer, preserving precision.
    /// </summary>
    [Obsolete("Use BigFloat.Round(BigFloat x) myBigFloat.Round()")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static BigFloat RoundToInteger(BigFloat x) => Round(x);


    /// <summary>
    /// Truncates towards zero, preserving accuracy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat TruncateToIntegerKeepingAccuracy()
    {
        int bitsToClear = GuardBits - Scale;
        
        if (bitsToClear <= 0) return this;
        if (bitsToClear >= _size) return ZeroWithAccuracy(Accuracy); 

        return new BigFloat(ClearLowerNBits(_mantissa, bitsToClear), Scale, _size);
    }

    /// <summary>
    /// Truncates towards zero, preserving accuracy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat TruncateToIntegerKeepingAccuracy(BigFloat x) => x.TruncateToIntegerKeepingAccuracy();

    /// <summary>
    /// Truncates towards zero. Removes all fractional bits and sets negative scales to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat Truncate()
    {
        int bitsToClear = GuardBits - Scale;

        if (bitsToClear <= 0) return this;
        if (bitsToClear >= _size) return 0;

        BigInteger newMantissa = (_mantissa.Sign >= 0) ? _mantissa >> bitsToClear : -(-_mantissa >> bitsToClear);
        return new BigFloat(newMantissa << GuardBits, Scale + bitsToClear- GuardBits, _size - bitsToClear + GuardBits);
    }

    /// <summary>
    /// Truncates towards zero. Removes all fractional bits and sets negative scales to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat Truncate(BigFloat x) => x.Truncate();

    /// <summary>
    /// Adjust the scale of a value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat AdjustScale(BigFloat x, int changeScaleAmount)
        => new(x._mantissa, x.Scale + changeScaleAmount, x._size);

    /// <summary>
    /// Adjust the scale of a value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat AdjustScale(int changeScaleAmount) => AdjustScale(this, changeScaleAmount);

    /// <summary>
    /// Adjust accuracy by <paramref name="deltaBits"/>.
    /// Positive delta increases fractional capacity; negative delta reduces it and rounds
    /// using the same semantics as precision reduction.
    /// Value-preserving when delta ≥ 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat AdjustAccuracy(BigFloat x, int deltaBits)
        => AdjustPrecision(x, deltaBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat AdjustAccuracy(int deltaBits) => AdjustAccuracy(this, deltaBits);

    /// <summary>
    /// Set accuracy to <paramref name="newAccuracyBits"/> (in bits).
    /// Internally computes <c>delta = newAccuracyBits - x.Accuracy</c> and delegates to
    /// <see cref="AdjustAccuracy(BigFloat,int)"/>/<see cref="AdjustPrecision(BigFloat,int)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat SetAccuracy(BigFloat x, int newAccuracyBits) 
        => (newAccuracyBits + x.Scale) == 0 ? x : AdjustPrecision(x, newAccuracyBits + x.Scale);

    /// <summary>
    /// Set accuracy to <paramref name="newAccuracyBits"/> (in bits).
    /// Internally computes <c>delta = newAccuracyBits - x.Accuracy</c> and delegates to
    /// <see cref="AdjustAccuracy(BigFloat,int)"/>/<see cref="AdjustPrecision(BigFloat,int)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat SetAccuracy(int newAccuracyBits) => SetAccuracy(this, newAccuracyBits);

    /// <summary>
    /// Sets the precision(and accuracy) of a number by appending 0 bits if too small or cropping bits if too large.
    /// This can be useful for extending whole or rational numbers precision. 
    /// No rounding is performed.
    /// Example: SetPrecision(0b1101, 8) --> 0b11010000;  SetPrecision(0b1101, 3) --> 0b110
    /// Also see: TruncateToAndRound, SetPrecisionWithRound
    /// </summary>
    public static BigFloat SetPrecision(BigFloat x, int newSize) =>
        new (x._mantissa << (newSize - x.Size), x.Scale + (x.Size - newSize), newSize + GuardBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat SetPrecision(int newSize) => SetAccuracy(this, newSize);

    /// <summary>
    /// Reduces the precision to the new specified size. To help maintain the most significant digits, the bits are not simply cut off. 
    /// When reducing, the least significant bit will rounded up if the most significant bit is set of the removed bits. 
    /// This can be used to reduce the precision of a number before prior to a calculation.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Also see: SetPrecision, TruncateToAndRound
    /// </summary>
    public static BigFloat SetPrecisionWithRound(BigFloat x, int newSize) => 
        (x.Size - newSize) switch
        {
            0 => x,
            > 0 => TruncateByAndRound(x, x.Size - newSize),
            < 0 => SetPrecision(x, newSize),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat SetPrecisionWithRound(int newSize) => SetPrecisionWithRound(this, newSize);

    /// <summary>
    /// Adjusts precision by shifting the mantissa and compensating the scale.
    /// Positive <paramref name="deltaBits"/> appends zero bits (extends precision).
    /// Negative <paramref name="deltaBits"/> rounds then drops low bits (reduces precision).
    /// Also see: AdjustAccuracy, SetPrecision, SetPrecisionWithRound
    /// </summary>
    /// <remarks>
    /// Semantics for negative mantissas: reduction truncates toward zero (bit-drop), not toward -∞.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat AdjustPrecision(BigFloat x, int deltaBits)
    {
        if (deltaBits == 0) return x;

        if (deltaBits > 0)
        {
            // Extend precision: left shift mantissa; scale decreases; size increases
            // validate: deltaBits <= MaxAllowedBits, size bounds, etc.
            return new BigFloat(
                x._mantissa << deltaBits,
                x.Scale - deltaBits,
                checked(x._size + deltaBits)
            );
        }
        else
        {
            int shrinkBy = -deltaBits;

            // If k >= current size, result should be zero with well-defined size/scale.
            // Decide policy: here we zero mantissa and clamp size to 0.
            if (shrinkBy >= x._size)
            {
                return new BigFloat(BigInteger.Zero, x.Scale + shrinkBy, 0);
            }

            return new BigFloat(
                RoundingRightShift(x._mantissa, shrinkBy),
                x.Scale + shrinkBy,
                checked(x._size - shrinkBy)
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat AdjustPrecision(int deltaBits) => AdjustPrecision(this, deltaBits);

    /// <summary>
    /// [Obsolete] Extends the precision and accuracy of a number by appending 0 bits (no rounding).
    /// Prefer <see cref="AdjustPrecision(BigFloat, int)"/> with a positive delta.
    /// </summary>
    [Obsolete("Use AdjustPrecision(x, +bitsToAdd). This method will be removed in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
        => AdjustPrecision(x, bitsToAdd);

    /// <summary>
    /// [Obsolete] Reduces the precision by dropping low bits (no rounding).
    /// Prefer <see cref="AdjustPrecision(BigFloat, int)"/> with a negative delta.
    /// </summary>
    [Obsolete("Use AdjustPrecision(x, -reduceBy). This method will be removed in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat ReducePrecision(BigFloat x, int reduceBy)
        => AdjustPrecision(x, -reduceBy);

    public static BigFloat operator -(BigFloat r1, BigFloat r2)
    {
        //// Early exit for zero operands
        if (r2.IsStrictZero) return r1; // Future: review if this is needed and is accuracy preserved.
        if (r1.IsStrictZero) return -r2;

        BigInteger r1Bits = (r1.Scale < r2.Scale) ? (r1._mantissa >> (r2.Scale - r1.Scale)) : r1._mantissa;
        BigInteger r2Bits = (r1.Scale > r2.Scale) ? (r2._mantissa >> (r1.Scale - r2.Scale)) : r2._mantissa;

        BigInteger diff = r1Bits - r2Bits;
        if (r1.Scale < r2.Scale ? r1.Sign < 0 : r2._mantissa.Sign < 0)
        {
            diff--;
        }

        int size = Math.Max(0, (int)BigInteger.Abs(diff).GetBitLength());

        return new BigFloat(diff, r1.Scale < r2.Scale ? r2.Scale : r1.Scale, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator -(BigFloat r1, int r2) 
        => r1 + (-r2);

    public static BigFloat PowerOf2(BigFloat val)
    {
        BigInteger prod = val._mantissa * val._mantissa;
        int resSize = (int)prod.GetBitLength();
        int shrinkBy = resSize - val._size;
        prod = RoundingRightShift(prod, shrinkBy, ref resSize);
        int resScalePart = (2 * val.Scale) + shrinkBy - GuardBits;
        BigFloat res = new(prod, resScalePart, resSize);
        AssertValid(res);
        return res;
    }

    /// <summary>
    /// Calculates a BigFloat to the power of 2 with a maximum output precision required.
    /// This function can save on compute cycles by not calculating bits that are needed.
    /// </summary>
    /// <param name="val">The base.</param>
    /// <param name="maxOutputPrecisionInBits">The maximum number of bits needed in the output. </param>
    /// <returns>Returns a BigFloat that is val^exp where the precision is </returns>
    public static BigFloat PowerOf2(BigFloat val, int maxOutputPrecisionInBits)
    {
        /*  valSz    resSize       skipIf
         *   3         5-6           maxOutputPrecisionInBits >= valSz*2
         *   4         7-8
         *   5         9-10                                                          */

        int overSized = (val._size * 2) - maxOutputPrecisionInBits - (2 * GuardBits);

        // We can just use PowerOf2() since output will never be larger then maxOutputPrecisionInBits.
        if (overSized <= 1)
        {
            BigFloat p2 = PowerOf2(val);

            // if size difference is 1 BUT the outputSize is still correct just return
            if (overSized <= 0 || p2._size == maxOutputPrecisionInBits)
            {
                return p2;
            }
            // output is oversized by 1 
            return new BigFloat(p2._mantissa, p2.Scale - 1, p2._size);
        }

        int inputShink = (overSized + 1) / 2;
        BigInteger valWithLessPrec = val._mantissa >> inputShink;
        BigInteger prod = valWithLessPrec * valWithLessPrec;

        int resBitLen = (int)prod.GetBitLength();
        int shrinkBy = resBitLen - val._size - (2 * GuardBits);
        int sizePart = resBitLen - shrinkBy;
        prod = RoundingRightShift(prod, shrinkBy);
        int resScalePart = (2 * val.Scale) + shrinkBy - GuardBits;

        return new(prod, resScalePart, sizePart);
    }

    /// <summary>
    /// Enhanced multiplication with adaptive algorithm selection
    /// </summary>
    public static BigFloat operator *(BigFloat a, BigFloat b)
    {
        // Early exit for zero operands
        if (a.IsStrictZero || b.IsStrictZero) return ZeroWithAccuracy(Math.Min(a.Accuracy, b.Accuracy));

        return Multiply(a, b);
    }

    /// <summary>
    /// Standard multiplication algorithm with optimizations
    /// </summary>
    public BigFloat Multiply(BigFloat other)
    {
        return Multiply(this, other);
    }

    public static BigFloat Multiply(BigFloat a, BigFloat b)
    {
        BigInteger prod;
        int shouldBe;
        const int SKIP_IF_SIZE_DIFF_SMALLER = 32;
        const int KEEP_EXTRA_PREC = 16;

        // future: for performance, what about no shift when _sizes are around the same size. (like within 32) 

        int sizeDiff = a._size - b._size;
        int shiftBy = Math.Max(0, Math.Abs(sizeDiff) - KEEP_EXTRA_PREC);

        // for size differences that are:
        //   0 to 31(SKIP_IF_SIZE_DIFF_SMALLER), no shift takes place (saves time on shift and increases precision on the LSB in rare cases)
        //   > 32, there is a shift of 16 or more (but size difference will be limited to 16 for extra precision)

        if (Math.Abs(sizeDiff) < SKIP_IF_SIZE_DIFF_SMALLER)
        {
            shiftBy = 0;
            prod = b._mantissa * a._mantissa;
            shouldBe = Math.Min(a._size, b._size);
        }
        else if (sizeDiff > 0)
        {
            prod = (a._mantissa >> shiftBy) * b._mantissa;
            shouldBe = b._size;
        }
        else
        {
            prod = (b._mantissa >> shiftBy) * a._mantissa;
            shouldBe = a._size;
        }

        int sizePart = (int)BigInteger.Abs(prod).GetBitLength();
        int shrinkBy = sizePart - shouldBe;

        prod = RoundingRightShift(prod, shrinkBy, ref sizePart);

        int resScalePart = a.Scale + b.Scale + shrinkBy + shiftBy - GuardBits;

        return new BigFloat(prod, resScalePart, sizePart);
    }

    public static BigFloat operator *(BigFloat a, int b) //ChatGPT o4-mini-high
    {
        // 1) extract unsigned magnitude and sign
        int sign = (b < 0) ? -1 : 1;
        uint ub = (uint)Math.Abs(b);

        // 2) zero and trivial cases
        if (ub <= 4)
        {
            return b switch
            {
                0 => ZeroWithAccuracy(a.Accuracy),         // exactly 0  
                1 => a,            // unchanged  
                -1 => -a,          // flip sign  
                2 => a << 1,
                -2 => -a << 1,
                3 => (a << 1) + a,
                -3 => (-a << 1) - a,
                4 => a << 2,
                -4 => -a << 2,
                _ => throw new NotImplementedException(),
            };
        }

        // if b == 2^k, just adjust exponent
        //   
        if ((ub & (ub - 1)) == 0)
        {
            int k = BitOperations.TrailingZeroCount(ub);
            return new BigFloat(
                a._mantissa * sign,
                a.Scale + k,
                a._size      // mantissa bit-length unchanged
            );
        }

        // General multiplication with size management
        BigInteger mant = a._mantissa * new BigInteger(ub);
        int sizePart = (int)BigInteger.Abs(mant).GetBitLength();
        int origSize = a._size;
        int shrinkBy = sizePart - origSize;

        if (shrinkBy > 0)
        {
            mant = RoundingRightShift(mant, shrinkBy, ref sizePart);
        }

        int resScale = a.Scale + shrinkBy;

        return new BigFloat(
            mant * sign,
            resScale,
            sizePart
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator *(int a, BigFloat b)
    {
        return b * a;
    }

    /// <summary>
    /// Enhanced division by integer with optimized algorithm selection
    /// </summary>
    public static BigFloat operator /(BigFloat divisor, int dividend)
    {
        if (dividend == 0) { throw new DivideByZeroException(); }
        if (divisor.IsZero) { return ZeroWithAccuracy(-divisor.Size); }

        // Extract the sign once and apply at the end
        int sign = Math.Sign(dividend) * divisor._mantissa.Sign;
        int absDividend = Math.Abs(dividend);

        // Optimize for powers of 2
        if ((absDividend & (absDividend - 1)) == 0)
        {
            int k = BitOperations.TrailingZeroCount((uint)absDividend);
            return new BigFloat(
                BigInteger.Abs(divisor._mantissa) * sign,
                divisor.Scale - k,
                divisor._size
            );
        }

        // Case 2: General division
        // First, determine target precision based on divisor's size
        int targetSize = divisor._size;

        // Shift the divisor's mantissa to ensure we maintain precision
        // We add GuardBits for rounding plus a small buffer for division
        int extraShift = GuardBits + 2; // 2 extra bits as buffer
        BigInteger shifted = BigInteger.Abs(divisor._mantissa) << extraShift;

        BigInteger result = shifted / absDividend;

        // Apply rounding (round to nearest)
        BigInteger remainder = shifted % absDividend;
        if (remainder * 2 >= absDividend)
        {
            result += 1;
        }

        int newScale = divisor.Scale - extraShift;
        int resultSize = (int)result.GetBitLength();

        // Precision adjustment
        if (resultSize < targetSize)
        {
            // Shift left to match the target size
            int adjustShift = targetSize - resultSize;
            result <<= adjustShift;
            newScale -= adjustShift;
        }
        else if (resultSize > targetSize)
        {
            // Shift right with rounding to match the target size
            int adjustShift = resultSize - targetSize;
            BigInteger roundingBit = BigInteger.One << (adjustShift - 1);
            result = (result + roundingBit) >> adjustShift;
            newScale += adjustShift;

            // Check if rounding caused a carry that increased the bit length
            if (result.GetBitLength() > targetSize)
            {
                result >>= 1;
                newScale += 1;
            }
        }

        result *= sign;
        return new BigFloat(result, newScale, targetSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator /(int a, BigFloat b)
    {
        // Future: a should be exact
        return new BigFloat(a) / b;
    }

    ///////////////////////// Explicit CASTS /////////////////////////

    /// <summary>Defines an explicit conversion of a System.Decimal object to a BigFloat. </summary>
    // future: public static explicit operator BigFloat(decimal input) => new BigFloat(input);

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator byte(BigFloat value)
    {
        return checked((byte)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator sbyte(BigFloat value)
    {
        return checked((sbyte)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 16-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ushort(BigFloat value)
    {
        return checked((ushort)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 16-bit signed integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator short(BigFloat value)
    {
        return checked((short)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 32-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(BigFloat value)
    {
        return checked((int)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 32-bit integer input. 
    /// The fractional part (including guard bits) are simply discarded.</summary>
    public static explicit operator uint(BigFloat value)
    {
        return checked((uint)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 64-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ulong(BigFloat value)
    {
        return checked((ulong)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 64-bit signed integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(BigFloat value)
    {
        return checked((long)GetIntegralValue(value));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 128-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UInt128(BigFloat value)
    {
        return (UInt128)GetRoundedMantissa(value._mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed 128-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int128(BigFloat value)
    {
        return (Int128)GetRoundedMantissa(value._mantissa << value.Scale);
    }

    /// <summary>Casts a BigFloat to a BigInteger. The fractional part (including guard bits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigInteger(BigFloat value)
    {
        return GetIntegralValue(value);
    }

    /// <summary>
    /// Casts a BigInteger to a BigFloat. The GuardBits are set to zero. 
    /// Example: a BigInteger of 1 would translate to "1+GuardBits" bits of precision.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigFloat(BigInteger value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an explicit conversion of a System.Double to a BigFloat.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigFloat(double value)
    {
        return new BigFloat(value);
    }

    /// <summary>
    /// Defines an explicit conversion of a BigFloat to a Double with IEEE‑754 semantics
    /// (round to nearest, ties to even). Handles normal, subnormal, overflow, and underflow.
    /// Precision is limited to 53 bits by IEEE‑754.
    /// </summary>
    public static explicit operator double(BigFloat value)
    {
        // Local helper: right-shift with round-to-nearest, ties-to-even.
        static BigInteger ShiftRightRoundEven(BigInteger mag, int shift)
        {
            if (shift <= 0) return mag;
            BigInteger q = mag >> shift;
            BigInteger r = mag - (q << shift);
            BigInteger half = BigInteger.One << (shift - 1);
            bool tie = r == half;
            bool up = r > half || (tie && ((q & BigInteger.One) != BigInteger.Zero));
            return up ? q + 1 : q;
        }

        if (value._mantissa.IsZero) return 0.0;

        bool neg = value._mantissa.Sign < 0;
        BigInteger absMant = BigInteger.Abs(value._mantissa);
        int L = value._size;                          // total bits in mantissa (incl. guard)
        long E = value.BinaryExponent;                // unbiased exponent for normalized (1.x)·2^E
        long e = E + 1023;                            // biased exponent

        // Overflow → ±Infinity
        if (e > 2046) return neg ? double.NegativeInfinity : double.PositiveInfinity;

        // Normalized numbers (1 ≤ |·| < 2)
        if (e >= 1)
        {
            // Provide exactly 53 significant bits (incl. hidden 1)
            BigInteger sig53 = L > 53
                ? ShiftRightRoundEven(absMant, L - 53)
                : (absMant << (53 - L));

            // Handle carry that turns 1.111.. → 10.000..
            if (sig53 == (BigInteger.One << 53))
            {
                sig53 >>= 1;
                e++;
                if (e > 2046) return neg ? double.NegativeInfinity : double.PositiveInfinity;
            }

            ulong mantField = (ulong)(sig53 & ((BigInteger.One << 52) - 1)); // drop leading 1
            long bits = ((long)e << 52) | (long)mantField;
            if (neg) bits |= (1L << 63);
            return BitConverter.Int64BitsToDouble(bits);
        }

        // Subnormals (e <= 0). Compute n = round(|x| * 2^1074).
        int shift = value.Scale - GuardBits + 1074;
        BigInteger n = shift >= 0
            ? (absMant << shift)
            : ShiftRightRoundEven(absMant, -shift);

        if (n.IsZero) return neg ? -0.0 : 0.0;       // underflow to signed zero

        // Rounding can push into the smallest normal (ed=1, mantissa=0)
        BigInteger two52 = BigInteger.One << 52;
        if (n >= two52)
        {
            long bits = 1L << 52;                    // exponent field = 1, fraction = 0
            if (neg) bits |= (1L << 63);
            return BitConverter.Int64BitsToDouble(bits);
        }

        long sub = (long)n;                           // fits in 52 bits
        long bitsSub = sub;                           // exponent field = 0
        if (neg) bitsSub |= (1L << 63);
        return BitConverter.Int64BitsToDouble(bitsSub);
    }

    /// <summary>
    /// Defines an explicit conversion of a BigFloat to a Single (float) with IEEE‑754 semantics
    /// (round to nearest, ties to even). Precision is limited to 24 bits (incl. hidden 1).
    /// </summary>
    public static explicit operator float(BigFloat value)
    {
        // Local helper (see double converter).
        static BigInteger ShiftRightRoundEven(BigInteger mag, int shift)
        {
            if (shift <= 0) return mag;
            BigInteger q = mag >> shift;
            BigInteger r = mag - (q << shift);
            BigInteger half = BigInteger.One << (shift - 1);
            bool tie = r == half;
            bool up  = r >  half || (tie && ((q & BigInteger.One) != BigInteger.Zero));
            return up ? q + 1 : q;
        }

        if (value._mantissa.IsZero) return 0.0f;

        bool neg = value._mantissa.Sign < 0;
        BigInteger absMant = BigInteger.Abs(value._mantissa);
        int L = value._size;
        int E = value.BinaryExponent;
        int e = E + 127;

        // Overflow → ±Infinity
        if (e > 254) return neg ? float.NegativeInfinity : float.PositiveInfinity;

        // Normalized
        if (e >= 1)
        {
            BigInteger sig24 = L > 24
                ? ShiftRightRoundEven(absMant, L - 24)
                : (absMant << (24 - L));

            if (sig24 == (BigInteger.One << 24))
            {
                sig24 >>= 1;
                e++;
                if (e > 254) return neg ? float.NegativeInfinity : float.PositiveInfinity;
            }

            uint mantField = (uint)(sig24 & ((BigInteger.One << 23) - 1));
            int bits = (e << 23) | (int)mantField;
            if (neg) bits |= 1 << 31;
            return BitConverter.Int32BitsToSingle(bits);
        }

        // Subnormals: n = round(|x| * 2^149)
        int shift = value.Scale - GuardBits + 149;
        BigInteger n = shift >= 0
            ? (absMant << shift)
            : ShiftRightRoundEven(absMant, -shift);

        if (n.IsZero) return neg ? -0.0f : 0.0f;

        BigInteger two23 = BigInteger.One << 23;
        if (n >= two23)
        {
            int bits = 1 << 23; // smallest normal
            if (neg) bits |= 1 << 31;
            return BitConverter.Int32BitsToSingle(bits);
        }

        int sub = (int)n; // fits in 23 bits
        if (neg) sub |= 1 << 31;
        return BitConverter.Int32BitsToSingle(sub);
    }

    // todo: test
    /// <summary>
    /// Round-to-Nearest at '.' using only the first fractional bit (ignores guard bits), then truncate.
    /// No round-to-even (ties go away-from-zero implicitly by using the top fractional bit only). (source ChatGPT 5T)
    /// </summary>
    public static int ToNearestInt(BigFloat x)
    {
        if (x.IsZero) return 0;

        // Ignore guard bits entirely: drop them WITHOUT rounding
        BigInteger mNoGuard = (x._mantissa.Sign >= 0)
            ? (x._mantissa >> GuardBits)
            : -((-x._mantissa) >> GuardBits);

        if (x.Scale >= 0)
        {
            // No working fractional field; just scale up to an integer
            BigInteger whole = mNoGuard << x.Scale;
            return checked((int)whole);
        }
        else
        {
            int fracBits = -x.Scale;                 // # of working fractional bits
            BigInteger trunc = (mNoGuard.Sign >= 0)  // truncate toward zero at '.'
                ? (mNoGuard >> fracBits)
                : -((-mNoGuard) >> fracBits);

            // Look only at the first fractional bit right after '.'
            bool roundUp = false;
            if (fracBits > 0)
            {
                BigInteger abs = BigInteger.Abs(mNoGuard);
                roundUp = ((abs >> (fracBits - 1)) & BigInteger.One) == BigInteger.One;
            }

            if (roundUp)
            {
                trunc += (mNoGuard.Sign >= 0) ? BigInteger.One : -BigInteger.One;
            }

            return checked((int)trunc);
        }
    }

    /// <summary>
    /// Checks whether this BigFloat struct holds a valid internal state.
    /// Returns true if valid; otherwise false.
    /// </summary>
    public bool Validate()
    {
        int realSize = (int)BigInteger.Abs(_mantissa).GetBitLength();
        bool valid = _size == realSize;

        Debug.Assert(valid,
            $"Invalid BigFloat: _size({_size}) does not match actual bit length ({realSize}).");

        return valid;
    }

    [Conditional("DEBUG")]
    private void AssertValid()
    {
        _ = Validate();
    }

    [Conditional("DEBUG")]
    private static void AssertValid(BigFloat val)
    {
        val.AssertValid();
    }
}