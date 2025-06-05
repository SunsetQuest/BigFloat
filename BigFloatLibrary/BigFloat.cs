// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot are used in the development of this library.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

// BigFloat.cs (this file) - contains the main BigFloat struct and its core properties and methods.
// BigIntegerTools.cs - contains helper methods for working with BigIntegers.
// optional: (contains additional methods that are not part of the core)
//   BigFloatCompareTo.cs: contains additional string functions as well as the IComparable, IEquatable, and IFormattable interfaces.
//   BigFloatExtended.cs: contains additional functions that do not fall into the other categories.
//   BigFloatMath.cs: contains extended math functions like Log, Sqrt, Exp, etc.
//   BigFloatParsing.cs: contains parsing functions for converting strings to BigFloat.
//   BigFloatRandom.cs
//   BigFloatStringsAndSpans.cs: contains functions related to converting BigFloat to strings/spans
//   Constants.cs,ConstantInfo.cs,ConstantsCatalog.cs,ConstantBuilder.cs: contains features for pulling up constants. 

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
    private const int SIMD_THRESHOLD = 64;  // Threshold for SIMD operations
    private const int SMALL_NUMBER_THRESHOLD = 64;  // Threshold for small number optimizations

    /// <summary>
    /// Gets the full integer's data bits, including guard bits.
    /// </summary>
    public readonly BigInteger Mantissa { get; }

    /// <summary>
    /// _size are the number of precision bits. It is equal to "ABS(DataBits).GetBitLength()". The ABS is for 
    ///       power-of-two negative BigIntegers (-1,-2,-4,-8...) so it is the same whether positive or negative.
    /// _size INCLUDES GuardBits (the Property Size subtracts out GuardBits)
    /// _size does not include rounding from GuardBits. (11[111...111] (where [111...111] is GuardBits) is still 2 bits. So the user will see it as 0b100 with a size of 2.)
    /// _size is 0 only when 'DataBits==0'
    /// When BigFloat is Zero, the size is zero.
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
    public bool IsPositive => Mantissa.Sign > 0 && !IsZero;

    /// <summary>
    /// Rounds and returns true if this value is negative. Only the top bit in GuardBits is counted.
    /// </summary>
    public bool IsNegative => Mantissa.Sign < 0 && !IsZero;

    /// <summary>
    /// Rounds with GuardBits and returns -1 if negative, 0 if zero, and +1 if positive.
    /// </summary>
    public int Sign => !IsZero ? Mantissa.Sign : 0;

    /// <summary>
    /// Returns the default zero with with a zero size, precision, scale, and accuracy.
    /// </summary>
    public static BigFloat Zero => new(0, 0, 0);

    /// <summary>
    /// Returns a '1' with only 1 bit of precision. (1 << GuardBits)
    /// </summary>
    public static BigFloat One => new(BigInteger.One << GuardBits, 0, GuardBits + 1);

    const double LOG2_OF_10 = 3.32192809488736235;

    /// <summary>
    /// Returns a "1" with a specific accuracy. 
    /// </summary>
    /// <param name="accuracy">The wanted accuracy between -32(GuardBits) to Int.MaxValue.</param>
    public static BigFloat OneWithAccuracy(int accuracy)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(accuracy, -32);
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
        Mantissa = rawValue;
        Scale = binaryScaler;
        _size = rawValueSize;

        AssertValid();
    }

    /// <summary>
    /// Constructs a BigFloat using its elemental parts. A starting <paramref name="integerPart"/> on how may binary places the point should be shifted (base-2 exponent) using <paramref name="binaryScaler"/>.
    /// </summary>
    /// <param name="integerPart">The integer part of the BigFloat that will have a <paramref name="binaryScaler"/> applied to it. </param>
    /// <param name="binaryScaler">How much should the <paramref name="integerPart"/> be shifted or scaled? This shift (base-2 exponent) will be applied to the <paramref name="integerPart"/>.</param>
    /// <param name="valueIncludesGuardBits">if true, then the guard bits should be included in the integer part.</param>
    public BigFloat(BigInteger integerPart, int binaryScaler = 0, bool valueIncludesGuardBits = false)
    {
        int applyGuardBits = valueIncludesGuardBits ? 0 : GuardBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        Mantissa = integerPart << applyGuardBits;
        _size = (int)BigInteger.Abs(Mantissa).GetBitLength();
        Scale = binaryScaler; // DataBits of zero can have scale

        AssertValid();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat(int integerPart, int binaryScaler = 0, int addedBinaryPrecision = 32) : this((long)integerPart, binaryScaler, addedBinaryPrecision) { }

    public BigFloat(long value, int binaryScaler = 0, int addedBinaryPrecision = 64)
    {
        Mantissa = (BigInteger)value << (GuardBits + addedBinaryPrecision);

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

    public BigFloat(ulong value, int binaryScaler = 0, int addedBinaryPrecision = 64)
    {
        Mantissa = (BigInteger)value << (GuardBits + addedBinaryPrecision);
        _size = value == 0 ? 0 : (GetBitLength(value) + GuardBits + addedBinaryPrecision);
        Scale = binaryScaler - addedBinaryPrecision;
        AssertValid();
    }

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
            Mantissa = new BigInteger(mantissa) << addedBinaryPrecision;
            Scale = exp - 1023 - 52 + binaryScaler + GuardBits - addedBinaryPrecision;
            _size = 53 + addedBinaryPrecision; //_size = BitOperations.Log2((ulong)Int);
        }
        else // exp is 0 so this is a denormalized float (leading "1" is "0" instead)
        {
            // 0.00000000000|00...0001 -> smallest value (Epsilon)  Int:1, Scale: Size:1
            // ...

            if (mantissa == 0)
            {
                Mantissa = 0;
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
                Mantissa = (new BigInteger(mantissa)) << addedBinaryPrecision;
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
                    ThrowInvalidInitializationException("Value is infinity or NaN");
                }
                else if (float.IsInfinity(value))
                {
                    ThrowInvalidInitializationException("Value is infinity or NaN");
                }
            }
            // Add leading 1 bit
            mantissa |= 0x800000;
            if (value < 0)
            {
                mantissa = -mantissa;
            }
            Mantissa = new BigInteger(mantissa) << GuardBits;
            Scale = exp - 127 - 23 + binaryScaler;
            _size = 24 + GuardBits;
        }
        else // exp is 0 so this is a denormalized(Subnormal) float (leading "1" is "0" instead)
        {
            if (mantissa == 0)
            {
                Mantissa = 0;
                Scale = binaryScaler;
                _size = 0;
            }
            else
            {
                BigInteger mant = new(value >= 0 ? mantissa : -mantissa);
                Mantissa = mant << GuardBits;
                Scale = -126 - 23 + binaryScaler; //hack: 23 is a guess
                _size = GuardBits - BitOperations.LeadingZeroCount((uint)mantissa) + GuardBits;
            }
        }

        AssertValid();
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
    /// Checks to see if the value is an integer. Returns true if all the bits between the radix point and the middle of GuardBits are all 0 or all 1.
    ///   for scale <= 0, if all bits are 0 or 1 between radix and half-way through the GuardBits
    ///   for scale >= (GuardBits/2), is always true.
    /// 
    /// if we call it an integer then it should follow that ...
    ///   it should not round up based on GuardBits
    ///   Ceiling would round up (and Floor down for negative)
    /// </summary>
    public bool IsInteger  //v4 - check to see if all the bits between the radix and one bit into the guard are zero (111.??|?)
    {
        get
        {
            // Assuming GuardBits is 4...
            // 11|1.1000  Scale < 0 - false b/c inconclusive (any scale < 0 is invalid since the unit value is out of scope)
            // 111.|1000  Scale ==0 - when scale is 0, it is always an integer
            // 111.10|00  Scale > 0 - if after rounding, any bits between the radix and guard are '1' then not an integer 
            const int topBitsToCheck = 8;
            if (Scale < 0)
            {
                // check to see if all the bits between the radix and one bit into the guard are zero (111.??|?)
                //BigInteger val3 = (Mantissa >> (GuardBits - 1)) & ((BigInteger.One << (-Scale + 1)) - 1);
                //return (val3 & (val3 + 1)) == 0; //return true if top 2 bits are all 0 or 1
                return BitsUniformInRange(Mantissa, GuardBits - Scale - 0, GuardBits - 1);
            }

            if (Scale == 0)
            {
                // The radix point and guard are in the same place. This is typically an integer however lets verify the top 1/4 the guard are 0 or 1.
                //int topBits = (int)(Mantissa >> (GuardBits - topBitsToCheck)) & ((1 << topBitsToCheck) - 1);
                //return (topBits & (topBits + 1)) == 0; //return true if top 2 bits are all 0 or 1
                return BitsUniformInRange(Mantissa, GuardBits, GuardBits - topBitsToCheck);
            }

            if (Scale > topBitsToCheck)
            {
                // The radix point is at least 8 bits into the guard area so it is very inconclusive at this point.
                // If someone says, I have around 1000 kg of rocks, is that an integer? not really
                return false;
            }

            // If here then Scale > 0 and the decimal is right shifted. This results in the radix is in the guard area.
            // This area is technically "inconclusive" so false, but to be more conforming to expectations, we use the 8 bits just below the guard up future more we only allow the radix to go 8 deep into the radix. So up to the top 8-8 bits are used in the guard area.
            return BitsUniformInRange(Mantissa, GuardBits - Scale, GuardBits - 8 - Scale);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool BitsUniformInRange(BigInteger value, int a, int b)
    {
        // precondition: 0 ≤ b < a ≤ 32
        int width = a - b;
        // mask == (1<<width)-1, i.e. width low bits = 1, the rest = 0
        BigInteger mask = (BigInteger.One << width) - 1;
        // extract the [b..a) bits down into the low bits of 'bits'
        BigInteger bits = (BigInteger.Abs(value) >> b) & mask;
        // true iff all those bits are 0, or all are 1
        return bits == 0u || bits == mask;
    }

    /// <summary>
    /// Tests to see if the number is in the format of "10000000..." after rounding.
    /// </summary>
    public bool IsOneBitFollowedByZeroBits => BigInteger.TrailingZeroCount(Mantissa >> (GuardBits - 1)) == (_size - GuardBits);

    public ulong Lowest64BitsWithGuardBits
    {
        get
        {
            ulong raw = (ulong)(Mantissa & ulong.MaxValue);

            if (Mantissa.Sign < 0)
            {
                raw = ~raw + (ulong)(((_size >> 64) > 0) ? 1 : 0);
            }
            return raw;
        }
    }

    public ulong Lowest64Bits
    {
        get
        {
            if (Mantissa.Sign >= 0)
            {
                ulong raw = (ulong)((Mantissa >> GuardBits) & ulong.MaxValue);
                return raw;
            }
            else if (_size >= GuardBits)
            {
                return ~(ulong)(((Mantissa - 1) >> GuardBits) & ulong.MaxValue);
                //return (ulong)((BigInteger.Abs(DataBits) >> GuardBits) & ulong.MaxValue); //perf: benchmark

            }
            else
            {
                ulong raw = (ulong)((Mantissa >> GuardBits) & ulong.MaxValue);
                //raw--;
                raw = ~raw;
                return raw;
            }
        }
    }

    /// <summary>
    /// Returns the 64 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 64 bits, then the LSBs are padded with zeros.
    /// </summary>
    public ulong Highest64Bits => (ulong)((BigInteger.IsPositive(Mantissa) ? Mantissa : -Mantissa) >> (_size - 64));

    /// <summary>
    /// Returns the 128 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 128 bits, then the LSBs are padded with zeros.
    /// </summary>
    public UInt128 Highest128Bits => (UInt128)((BigInteger.IsPositive(Mantissa) ? Mantissa : -Mantissa) >> (_size - 128));

    /// <summary>
    /// Rounds to the next integer towards negative infinity. Any fractional bits are removed, negative scales are set
    /// to zero, and the precision(size) will be resized to just the integer part.
    /// </summary>
    public BigFloat Floor()
    {
        int bitsToClear = GuardBits - Scale; // number of bits to clear from DataBits

        // 'Scale' will be zero or positive. (since all fraction bits are stripped away)
        // 'Size'  will be the size of the new integer part.
        // Precision of the decimal bits are stripped away. 

        // If bitsToClear <= 0, then all fraction bits are implicitly zero and nothing needs to be done.
        //   Example: Scale = 32+7, int=45, size=6+32=38 -> bitsToClear=-7   -101101[10101010010...00010]0000000.
        if (bitsToClear <= 0)
        {
            return this;
        }

        int sign = Sign;

        // If less then zero, we can just return 0 for positive numbers and -1 for negative.
        //   Example: Scale = -11, int=45, size=6+32=38  -> bitsToClear=32+11   -.00000 101101[10101010010...00010]
        if (bitsToClear >= _size)
        {
            return sign >= 0 ? new BigFloat(0, 0, 0) : new BigFloat(BigInteger.MinusOne << GuardBits, 0, 1 + GuardBits);
        }

        if (sign == 0)
        {
            return new BigFloat(0, 0, 0);
        }

        if (Mantissa.Sign > 0)
        {
            // If Positive and Floor, the size should always remain the same.
            // If Scale is between 0 and GuardBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            if (Scale >= 0) // SCALE >= 0 and SCALE < GuardBits
            {
                return new BigFloat((Mantissa >> bitsToClear) << bitsToClear, Scale, _size);
            }

            // If Scale is between -size and 0..
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]
            //BigInteger intPart = ((DataBits >> bitsToClear) + 1) << GuardBits;
            //return new BigFloat((DataBits >> bitsToClear) +  (IsInteger?0:1));
            return new BigFloat(Mantissa >> bitsToClear);
        }
        else  // sign <= 0
        {
            //   If Negative and Flooring, and the abs(result) is a PowerOfTwo the size will grow by 1.  -1111.1 -> -10000, -10000 -> -10000
            // Lets just remove the bits and clear GuardBits
            //   Example: Scale =  4, int=45, size=8+32=40  -> bitsToClear=32-4  11101101[1010.1010010...00010]  -> 11101101[1010.0000000...00000]

            // clear bitToClear bits 

            _ = GuardBits - Math.Max(0, Scale);

            // If Scale is between 0 and GuardBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
            if (Scale >= 0) // SCALE >= 0 and SCALE < GuardBits
            {
                bool roundsUp = (Mantissa & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = Mantissa >> bitsToClear << bitsToClear;
                int newSize = _size;

                if (roundsUp)
                {
                    intPart += 1 << (bitsToClear);
                    newSize = (int)intPart.GetBitLength(); //future: maybe slow (maybe use BigInteger.TrailingZeroCount to detect rollover)

                }
                return new BigFloat(intPart, Scale, newSize);
            }

            // If Scale is between -size and 0..
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  -11.1101[10101010010...00010]  -> -100.[00000000000...00000]
            else //if (Scale < 0)
            {
                return new BigFloat(Mantissa >> bitsToClear);
            }
        }
    }

    /// <summary>
    /// Rounds to the next integer towards positive infinity. Any fractional bits are removed, negative scales are set
    /// to zero, and the precision(size) will be resized to just the integer part.
    /// </summary>
    public BigFloat Ceiling()
    {
        int bitsToClear = GuardBits - Scale;

        // 'Scale' will be zero or positive. (since all fraction bits are stripped away)
        // 'Size'  will be the size of the new integer part.
        // Fractional bits are removed. (i.e. Negative precisions are set to zero.)

        // If bitsToClear <= 0, then all fraction bits are implicitly zero and nothing needs to be done.
        //   Example: Scale = 32+7, int=45, size=6+32=38 -> bitsToClear=-7   -101101[10101010010...00010]0000000.
        if (bitsToClear <= 0) // Scale >= GuardBits
        {
            return this;
        }

        int sign = Sign;

        // If less then zero, we can just return 1 for positive numbers and 0 for negative.
        //   Example: Scale = -11, int=45, size=6+32=38  -> bitsToClear=32+11   -.00000 101101[10101010010...00010]
        if (bitsToClear >= _size)
        {
            return (sign <= 0) ? new BigFloat(0, 0, 0) : new BigFloat(BigInteger.One << GuardBits, 0, 1 + GuardBits);
        }

        // Radix point is in the GuardBits area
        //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
        if (Scale < GuardBits) // SCALE >= 0 and SCALE<GuardBits
        {
            // Future: optimization here?
        }

        if (sign == 0)
        {
            return new BigFloat(0, 0, 0);
        }

        if (sign > 0)
        {
            //   If Positive and Ceiling, and the abs(result) is a PowerOfTwo the size will grow by 1.  -1111.1 -> -10000, -10000 -> -10000
            // Lets just remove the bits and clear GuardBits
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]

            if (Scale >= 0) // Scale is between 0 and GuardBits
            {
                //  Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
                bool roundsUp = (Mantissa & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = Mantissa >> bitsToClear << bitsToClear;
                int newSize = _size;

                if (roundsUp)
                {
                    intPart += 1 << (bitsToClear);
                    newSize = (int)intPart.GetBitLength(); //future: maybe slow (maybe use BigInteger.TrailingZeroCount to detect rollover)
                }
                return new BigFloat(intPart, Scale, newSize);
            }

            // If Scale is between -size and 0..
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  -11.1101[10101010010...00010]  -> -100.[00000000000...00000]
            else //if (Scale < 0)
            {
                // round up if any bits set between (GuardBits/2) and (GuardBits-Scale) 
                bool roundsUp = (Mantissa & (((BigInteger.One << ((GuardBits / 2) - Scale)) - 1) << (GuardBits / 2))) > 0;

                BigInteger intPart = Mantissa >> bitsToClear << GuardBits;

                if (roundsUp)
                {
                    intPart += BigInteger.One << GuardBits;
                }

                int newSize = roundsUp ? (int)intPart.GetBitLength() : _size - bitsToClear + GuardBits; //future: optimize using bit scan operations when the rollover is predictable

                return new BigFloat(intPart, 0, newSize);
            }
        }
        else  // sign < 0
        {
            // If Negative and Ceiling, the size should always remain the same.
            // If Scale is between 0 and GuardBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            if (Scale >= 0)
            {
                return new BigFloat((Mantissa >> bitsToClear) << bitsToClear, Scale, _size);
            }
            BigInteger intPart = Mantissa >> bitsToClear;

            if (!IsInteger)
            {
                intPart++;
            }

            return new BigFloat(intPart);
        }
    }

    /// <summary>
    /// Compares two BigFloats and returns negative if this instance is less, Zero if difference is 2^(GuardBits-1) or less, or Positive if this instance is greater
    /// </summary>
    public int CompareTo(BigFloat other)
    {
        if (CheckForQuickCompareWithExponentOrSign(other, out int result)) { return result; }

        // At this point, the exponent is equal or off by one because of a rollover.
        int sizeDiff = other.Scale - Scale;

        BigInteger diff = ((sizeDiff < 0) ? (other.Mantissa << sizeDiff) : other.Mantissa)
            - ((sizeDiff > 0) ? (Mantissa >> sizeDiff) : Mantissa);

        return diff.Sign >= 0 ?
            -(diff >> (GuardBits - 1)).Sign :
            (-diff >> (GuardBits - 1)).Sign;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckForQuickCompareWithExponentOrSign(BigFloat other, out int result)
    {
        if (IsOutOfPrecision)
        {
            result = other.IsOutOfPrecision ? 0 : -other.Mantissa.Sign;
            return true;
        }

        if (other.IsOutOfPrecision)
        {
            result = IsOutOfPrecision ? 0 : Mantissa.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Sign.
        if (Mantissa.Sign != other.Mantissa.Sign)
        {
            result = Mantissa.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Exponent.
        int expDifference = BinaryExponent - other.BinaryExponent;
        if (Math.Abs(expDifference) > 1)
        {
            result = BinaryExponent.CompareTo(other.BinaryExponent) * Mantissa.Sign;
            return true;
        }

        // At this point, the sign is the same, and the exp are within 1 bit of each other.

        //There are three special cases when the Exponent is off by just 1 bit:
        // case 1:  The smaller of the two rounds up to match the size of the larger and, therefore, can be equal(11 : 111 == 100 : 000)
        // case 2:  The smaller of the two rounds up, but the larger one also rounds up, so they are again not equal(depends on #1 happening first)
        // case 3:  Both round-up and are, therefore, equal

        //If "this" is larger by one bit AND "this" is not in the format 10000000..., THEN "this" must be larger(or smaller if neg)
        if (expDifference == 1 && !IsOneBitFollowedByZeroBits)
        {
            result = Mantissa.Sign;
            return true;
        }

        // If "other" is larger by one bit AND "other" is not in the format 10000000..., THEN "other" must be larger(or smaller if neg)
        if (expDifference == -1 && !other.IsOneBitFollowedByZeroBits)
        {
            result = -Sign;
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    /// Returns the number of matching leading bits with rounding.
    /// e.g. 10.111 - 10.101 is 00.010 so returns 4
    /// 
    /// The Exponent(or Scale + _size) is considered. 
    ///   e.g. 100. and 1000. would return 0
    /// 
    /// If the signs do not match then 0 is returned. 
    ///   
    /// When a rollover is near these bits are included. 
    ///   e.g. 11110 and 100000 returns 3
    ///   
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
                a.Mantissa - (b.Mantissa << scaleDiff)
                : (a.Mantissa >> scaleDiff) - b.Mantissa;

        sign = temp.Sign;

        return maxSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    /// <summary>
    /// Returns the number of matching leading bits that exactly match. GuardBits are included.
    /// i.e. The number of leading bits that exactly match.
    /// e.g. 11010 and 11111 returns 2
    /// e.g. 100000 and 111111 returns 1
    /// If the signs do not match then 0 is returned.
    /// 
    /// The scale and precision(size) is ignored.
    /// e.g. 11101000000 and 11111 returns 3
    /// </summary>
    public static int NumberOfMatchingLeadingMantissaBits(BigFloat a, BigFloat b)
    {
        if (a.Sign != b.Sign) { return 0; }

        int sizeDiff = a._size - b._size;
        int newSize = sizeDiff > 0 ? b._size : a._size;

        if (newSize == 0) { return 0; }

        BigInteger temp = (sizeDiff < 0) ?
                a.Mantissa - (b.Mantissa << sizeDiff)
                : (a.Mantissa >> sizeDiff) - b.Mantissa;

        return newSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    ///////////////////////// Operator Overloads: BigFloat <--> BigFloat /////////////////////////

    /// <summary>Returns true if the left side BigFloat is equal to the right side BigFloat.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right BigFloat.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BigFloat left, BigFloat right)
    {
        return right.CompareTo(left) != 0;
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

    /// <summary>Returns true if the left side BigFloat is equal to the right side BigInteger.  If the BigFloat is not an integer then always returns false.</summary>
    public static bool operator ==(BigFloat left, BigInteger right)
    {
        return left.IsInteger && new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side BigInteger is equal to the right side BigFloat. If the BigFloat is not an integer then always returns false.</summary>
    public static bool operator ==(BigInteger left, BigFloat right)
    {
        return right.IsInteger && new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right side BigInteger. If the BigFloat is not an integer then always returns true.</summary>
    public static bool operator !=(BigFloat left, BigInteger right)
    {
        return !(left == right);
    }

    /// <summary>Returns true if the left side BigInteger is not equal to the right side BigFloat. If the BigFloat is not an integer then always returns true.</summary>
    public static bool operator !=(BigInteger left, BigFloat right)
    {
        return !(left == right);
    }

    ///////////////////////// Operator Overloads: BigFloat <--> ulong/long /////////////////////////

    /// <summary>Returns true if the left side BigFloat is equal to the right side unsigned long.</summary>
    public static bool operator ==(BigFloat left, ulong right)
    {
        return new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is equal to the right side long.</summary>
    public static bool operator ==(BigFloat left, long right)
    {
        return new BigFloat(right).CompareTo(left) == 0;
    }

    /// <summary>Returns true if the left side long is equal to the right side BigFloat.</summary>
    public static bool operator ==(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side unsigned long is equal to the right side BigFloat.</summary>
    public static bool operator ==(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right side unsigned long.</summary>
    public static bool operator !=(BigFloat left, ulong right)
    {
        return new BigFloat(right).CompareTo(left) != 0;
    }

    /// <summary>Returns true if the left side unsigned long is not equal to the right side BigFloat.</summary>
    public static bool operator !=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) != 0;
    }

    /// <summary>Returns true if the left side BigFloat is equal to the right side unsigned long.</summary>
    public static bool operator !=(BigFloat left, long right)
    {
        return new BigFloat(right).CompareTo(left) != 0;
    }

    public static bool operator !=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) != 0;
    }

    public static bool operator <(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) < 0;
    }

    public static bool operator <(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) < 0;
    }

    public static bool operator <(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) < 0;
    }

    public static bool operator <(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) < 0;
    }

    public static bool operator >(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) > 0;
    }

    public static bool operator >(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) > 0;
    }

    public static bool operator >(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) > 0;
    }

    public static bool operator >(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) > 0;
    }

    public static bool operator <=(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) <= 0;
    }

    public static bool operator <=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) <= 0;
    }

    public static bool operator <=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) <= 0;
    }

    public static bool operator <=(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) <= 0;
    }

    public static bool operator >=(long left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) >= 0;
    }

    public static bool operator >=(BigFloat left, long right)
    {
        return left.CompareTo(new BigFloat(right)) >= 0;
    }

    public static bool operator >=(BigFloat left, ulong right)
    {
        return left.CompareTo(new BigFloat(right)) >= 0;
    }

    public static bool operator >=(ulong left, BigFloat right)
    {
        return new BigFloat(left).CompareTo(right) >= 0;
    }

    /// <summary>
    /// Enhanced division with adaptive algorithm selection based on operand sizes
    /// </summary>
    public static BigFloat operator /(BigFloat divisor, BigFloat dividend)
    {
        // Early exit for zero divisor
        if (dividend.IsZero)
        {
            throw new DivideByZeroException("Division by zero");
        }

        // Early exit for zero dividend
        if (divisor.IsZero)
        {
            return Zero;
        }

        // Use optimized algorithm for small numbers
        if (divisor._size < SMALL_NUMBER_THRESHOLD && dividend._size < SMALL_NUMBER_THRESHOLD)
        {
            return DivideSmallNumbers(divisor, dividend);
        }

        // Use advanced division algorithms for large numbers
        if (divisor._size > BURNIKEL_ZIEGLER_THRESHOLD || dividend._size > BURNIKEL_ZIEGLER_THRESHOLD)
        {
            return DivideLargeNumbers(divisor, dividend);
        }

        // Standard division algorithm
        return DivideStandard(divisor, dividend);
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
        if (divisor.Mantissa >> (divisor.Size - dividend.Size) <= dividend.Mantissa)
        {
            outputSize--;
        }

        // We need to oversize T (using left shift) so when we divide, it is the correct size.
        int wantedSizeForT = dividend.Size + outputSize + GuardBits;

        int leftShiftTBy = wantedSizeForT - divisor.Size;

        BigInteger leftShiftedT = divisor.Mantissa << leftShiftTBy;
        BigInteger resIntPart = leftShiftedT / dividend.Mantissa;

        int resScalePart = divisor.Scale - dividend.Scale - leftShiftTBy + GuardBits;
        int sizePart = (int)BigInteger.Abs(resIntPart).GetBitLength();

        return new BigFloat(resIntPart, resScalePart, sizePart);
    }

    /// <summary>
    /// Performs a modulus operation. For negative numbers there are two approaches, a math and programmers version. For negative numbers this version uses the programmers version.
    /// </summary>
    public static BigFloat operator %(BigFloat dividend, BigFloat divisor)
    {
        return Remainder(dividend, divisor);
    }

    /// <summary>
    /// Performs a Modulus operation. 
    /// For positive values, Modulus is identical to Remainder, for negatives, Modulus and Remainder differ. 
    /// The remainder is slightly faster.
    /// </summary>
    public static BigFloat Remainder(BigFloat dividend, BigFloat divisor)
    {
        int scaleDiff = dividend.Scale - divisor.Scale;

        return scaleDiff switch
        {
            > 0 => new(((dividend.Mantissa << scaleDiff) % divisor.Mantissa) >> scaleDiff, dividend.Scale, true),
            < 0 => new((dividend.Mantissa % (divisor.Mantissa >> scaleDiff)) << scaleDiff, divisor.Scale, true),
            0 => new(dividend.Mantissa % divisor.Mantissa, divisor.Scale, true),
        };
    }

    /// <summary>
    /// Performs a Modulus operation. 
    /// For positive values, Modulus is identical to Remainder, for negatives, Modulus and Remainder differ. 
    /// The remainder is slightly faster.
    /// </summary>
    public static BigFloat Mod(BigFloat dividend, BigFloat divisor)
    {
        return Remainder(dividend, divisor) + ((dividend < 0) ^ (divisor > 0) ?
            Zero :
            divisor);
    }

    /// <summary>
    /// Bitwise Complement Operator - Reverses each bit in the data bits. Scale is not changed.
    /// The size is reduced by at least 1 bit. This is because the leading bit is flipped to a zero.
    /// </summary>
    public static BigFloat operator ~(BigFloat value)
    {
        BigInteger temp = value.Mantissa ^ ((BigInteger.One << value._size) - 1);
        return new(temp, value.Scale, true);
    }

    /// <summary>
    /// Left shift - Increases the scale by the amount left shift amount. 
    /// The precision is unchanged.
    /// </summary>
    /// <param name="value">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator <<(BigFloat value, int shift)
    {
        return new(value.Mantissa, value.Scale + shift, value._size);
    }

    /// <summary>
    /// Right shift - Decreases the scale by the amount right shift amount. 
    /// The precision is unchanged.
    /// </summary>
    /// <param name="value">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator >>(BigFloat value, int shift)
    {
        return new(value.Mantissa, value.Scale - shift, value._size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator +(BigFloat r)
    {
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator -(BigFloat r)
    {
        return new(-r.Mantissa, r.Scale, r._size);
    }

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
        if (onesPlace == -1 && !r.Mantissa.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r.Mantissa + (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        // int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //perf: faster just to calc
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);
        return new BigFloat(intVal, r.Scale, sizeVal);
    }

    public static BigFloat operator --(BigFloat r)
    {
        int onesPlace = GuardBits - r.Scale;

        if (onesPlace < 1)
        {
            return r;
        }

        // In the special case, we may not always want to round up when adding a 1 bit just below the LSB. 
        if (onesPlace == -1 && !r.Mantissa.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r.Mantissa - (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        //int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //future: faster just to calc?
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);

        return new BigFloat(intVal, r.Scale, sizeVal);
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
            BigInteger intVal0 = RightShiftWithRound(r1.Mantissa, -scaleDiff) + r2.Mantissa;
            int resSize0 = (int)BigInteger.Abs(intVal0).GetBitLength();
            return new BigFloat(intVal0, r2.Scale, resSize0);
        }

        BigInteger intVal = r1.Mantissa + RightShiftWithRound(r2.Mantissa, scaleDiff);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        return new BigFloat(intVal, r1.Scale, sizeVal);
    }

    public static BigFloat operator +(BigFloat r1, int r2) //ChatGPT o4-mini-high
    {
        if (r2 == 0) { return r1; }

        BigInteger r2Bits = new BigInteger(r2) << GuardBits;
        int r2Size = (int)BigInteger.Abs(r2Bits).GetBitLength();
        int scaleDiff = r1.Scale;

        // if r2 is too small to affect r1 at r1’s precision ⇒ drop it
        if (scaleDiff > r2Size) { return r1; }

        // if r1 is too small compared to r2 ⇒ result ≅ r2
        if (-scaleDiff > r1._size) { return new BigFloat(r2Bits, 0, r2Size); }

        // align mantissas and add
        BigInteger sum;
        int resScale;
        if (r1.Scale == 0)
        {
            // same exponent
            sum = r1.Mantissa + r2Bits;
            resScale = 0;
        }
        else if (r1.Scale < 0)
        {
            // r2 has larger exponent: shift r1 down
            sum = RightShiftWithRound(r1.Mantissa, -scaleDiff) + r2Bits;
            resScale = 0;
        }
        else
        {
            // r1 has larger exponent: shift r2 down
            sum = r1.Mantissa + RightShiftWithRound(r2Bits, scaleDiff);
            resScale = r1.Scale;
        }

        int resSize = (int)BigInteger.Abs(sum).GetBitLength();
        return new BigFloat(sum, resScale, resSize);
    }

    ///////////////////////// Rounding, Shifting, Truncate /////////////////////////

    /// <summary>
    /// Checks to see if this integerPart would round-up if GuardBits are removed.
    /// </summary>
    /// <param name="bi">The BigInteger we would like check if it would round up.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public static bool WouldRoundUp(BigInteger bi)
    {
        return WouldRoundUp(bi, GuardBits);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the GuardBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRoundUp()
    {
        return WouldRoundUp(Mantissa, GuardBits);
    }

    /// <summary>
    /// Checks to see if this integerPart would round-up given bottomBitsRemoved. 
    /// e.g. 11010101 with bottomBitsRemoved=3 would be 11011
    /// </summary>
    /// <param name="bottomBitsRemoved">The number of newSizeInBits from the least significant bit where rounding would take place.</param>
    public bool WouldRoundUp(int bottomBitsRemoved)
    {
        return WouldRoundUp(Mantissa, bottomBitsRemoved);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the GuardBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool WouldRoundUp(BigInteger val, int bottomBitsRemoved)
    {
        // Future: for .net 7 and later use ">>>" instead of >> for a slight performance boost.
        bool isPos = val.Sign >= 0;
        return isPos ^ ((isPos ? val : val - 1) >> (bottomBitsRemoved - 1)).IsEven;
    }

    /// <summary>
    /// Mantissa extraction with guard bits rounded off
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger MantissaWithoutGuardBits(BigInteger x)
    {
        return RightShiftWithRound(x, GuardBits);
    }

    /// <summary>
    /// Removes GuardBits and rounds. It also requires the current size and will adjust it if it grows.
    /// </summary>
    private static BigInteger DataIntValueWithRound(BigInteger x, ref int size)
    {
        return RightShiftWithRound(x, GuardBits, ref size);
    }

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
            throw new ArgumentOutOfRangeException(nameof(targetBitsToRemove), $"Param {nameof(targetBitsToRemove)}({targetBitsToRemove}) be 0 or greater.");
        }

        int newScale = x.Scale + targetBitsToRemove;
        int size = x._size;

        BigInteger b = RightShiftWithRound(x.Mantissa, targetBitsToRemove, ref size);

        return new(b, newScale, size);
    }

    /// <summary>
    /// Adjust the scale of a value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat AdjustScale(BigFloat x, int changeScaleAmount)
    {
        return new BigFloat(x.Mantissa, x.Scale + changeScaleAmount, x._size);
    }

    /// <summary>
    /// Sets the precision(and accuracy) of a number by appending 0 bits if too small or cropping bits if too large.
    /// This can be useful for extending whole or rational numbers precision. 
    /// No rounding is performed.
    /// Example: SetPrecision(0b1101, 8) --> 0b11010000;  SetPrecision(0b1101, 3) --> 0b110
    /// Also see: TruncateToAndRound, SetPrecisionWithRound
    /// </summary>
    public static BigFloat SetPrecision(BigFloat x, int newSize)
    {
        return new BigFloat(x.Mantissa << (newSize - x.Size), x.Scale + (x.Size - newSize), newSize + GuardBits);
    }

    /// <summary>
    /// Reduces the precision of the a number but keeps the value the same.
    /// i.e. Down-shifts the value but and increases the scale. 
    /// Example: ReducePrecision(0b1101.1101, 3) --> 0b1101.1; 
    /// No rounding is performed.
    /// Also see: TruncateByAndRound, RightShiftWithRoundWithCarry, RightShiftWithRound
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat ReducePrecision(BigFloat x, int reduceBy)
    {
        return new BigFloat(x.Mantissa >> reduceBy, x.Scale + reduceBy, x._size - reduceBy);
    }

    /// <summary>
    /// Reduces the precision to the new specified size. To help maintain the most significant digits, the bits are not simply cut off. 
    /// When reducing the least significant bit will rounded up if the most significant bit is set of the removed bits. 
    /// This can be used to reduce the precision of a number before prior to a calculation.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Also see: SetPrecision, TruncateToAndRound
    /// </summary>
    public static BigFloat SetPrecisionWithRound(BigFloat x, int desiredNewBitSize)
    {
        int reduceBy = x.Size - desiredNewBitSize;
        BigFloat result = TruncateByAndRound(x, reduceBy);
        return result;
    }

    /// <summary>
    /// Extends the precision and accuracy of a number by appending 0 bits. 
    /// e.g. 1.1 --> 1.100000
    /// This can be useful for extending whole or rational numbers precision. 
    /// </summary>
    public static BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
    {
        return bitsToAdd < 0
            ? throw new ArgumentOutOfRangeException(nameof(bitsToAdd), "cannot be a negative number")
            : new BigFloat(x.Mantissa << bitsToAdd, x.Scale - bitsToAdd, x._size + bitsToAdd);
    }

    public static BigFloat operator -(BigFloat r1, BigFloat r2)
    {
        // Early exit for zero operands
        if (r2.IsZero) return r1;
        if (r1.IsZero) return -r2;

        BigInteger r1Bits = (r1.Scale < r2.Scale) ? (r1.Mantissa >> (r2.Scale - r1.Scale)) : r1.Mantissa;
        BigInteger r2Bits = (r1.Scale > r2.Scale) ? (r2.Mantissa >> (r1.Scale - r2.Scale)) : r2.Mantissa;

        BigInteger diff = r1Bits - r2Bits;
        if (r1.Scale < r2.Scale ? r1.Sign < 0 : r2.Mantissa.Sign < 0)
        {
            diff--;
        }

        int size = Math.Max(0, (int)BigInteger.Abs(diff).GetBitLength());

        return new BigFloat(diff, r1.Scale < r2.Scale ? r2.Scale : r1.Scale, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat operator -(BigFloat r1, int r2)
    {
        return r1 + (-r2);
    }

    public static BigFloat PowerOf2(BigFloat val)
    {
        BigInteger prod = val.Mantissa * val.Mantissa;
        int resSize = (int)prod.GetBitLength();
        int shrinkBy = resSize - val._size;
        prod = RightShiftWithRound(prod, shrinkBy, ref resSize);
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
            return new BigFloat(p2.Mantissa, p2.Scale - 1, p2._size);
        }

        int inputShink = (overSized + 1) / 2;
        BigInteger valWithLessPrec = val.Mantissa >> inputShink;
        BigInteger prod = valWithLessPrec * valWithLessPrec;

        int resBitLen = (int)prod.GetBitLength();
        int shrinkBy = resBitLen - val._size - (2 * GuardBits);
        int sizePart = resBitLen - shrinkBy;
        prod = RightShiftWithRound(prod, shrinkBy);
        int resScalePart = (2 * val.Scale) + shrinkBy - GuardBits;

        return new(prod, resScalePart, sizePart);
    }

    /// <summary>
    /// Enhanced multiplication with adaptive algorithm selection
    /// </summary>
    public static BigFloat operator *(BigFloat a, BigFloat b)
    {
        // Early exit for zero operands
        if (a.IsZero || b.IsZero) return Zero;

        // Use optimized algorithms based on size
        if (a._size < SMALL_NUMBER_THRESHOLD && b._size < SMALL_NUMBER_THRESHOLD)
        {
            return MultiplySmallNumbers(a, b);
        }

        if (a._size > KARATSUBA_THRESHOLD || b._size > KARATSUBA_THRESHOLD)
        {
            return MultiplyLargeNumbers(a, b);
        }

        return MultiplyStandard(a, b);
    }

    /// <summary>
    /// Optimized multiplication for small numbers
    /// </summary>
    private static BigFloat MultiplySmallNumbers(BigFloat a, BigFloat b)
    {
        // future: implement optimized small number multiplication using hardware arithmetic when possible
        return MultiplyStandard(a, b);
    }

    /// <summary>
    /// Optimized multiplication for large numbers using advanced algorithms
    /// </summary>
    private static BigFloat MultiplyLargeNumbers(BigFloat a, BigFloat b)
    {
        // future: implement Karatsuba or FFT multiplication for better performance on large numbers
        return MultiplyStandard(a, b);
    }

    /// <summary>
    /// Standard multiplication algorithm with optimizations
    /// </summary>
    private static BigFloat MultiplyStandard(BigFloat a, BigFloat b)
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
            prod = b.Mantissa * a.Mantissa;
            shouldBe = Math.Min(a._size, b._size);
        }
        else if (sizeDiff > 0)
        {
            prod = (a.Mantissa >> shiftBy) * b.Mantissa;
            shouldBe = b._size;
        }
        else
        {
            prod = (b.Mantissa >> shiftBy) * a.Mantissa;
            shouldBe = a._size;
        }

        int sizePart = (int)BigInteger.Abs(prod).GetBitLength();
        int shrinkBy = sizePart - shouldBe;

        prod = RightShiftWithRound(prod, shrinkBy, ref sizePart);

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
                0 => Zero,         // exactly 0  
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
                a.Mantissa * sign,
                a.Scale + k,
                a._size      // mantissa bit-length unchanged
            );
        }

        // General multiplication with size management
        BigInteger mant = a.Mantissa * new BigInteger(ub);
        int sizePart = (int)BigInteger.Abs(mant).GetBitLength();
        int origSize = a._size;
        int shrinkBy = sizePart - origSize;

        if (shrinkBy > 0)
        {
            mant = RightShiftWithRound(mant, shrinkBy, ref sizePart);
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
        if (divisor.IsZero) { return ZeroWithSpecifiedLeastPrecision(divisor.Size); }

        // Extract the sign once and apply at the end
        int sign = Math.Sign(dividend) * divisor.Mantissa.Sign;
        int absDividend = Math.Abs(dividend);

        // Optimize for powers of 2
        if ((absDividend & (absDividend - 1)) == 0)
        {
            int k = BitOperations.TrailingZeroCount((uint)absDividend);
            return new BigFloat(
                BigInteger.Abs(divisor.Mantissa) * sign,
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
        BigInteger shifted = BigInteger.Abs(divisor.Mantissa) << extraShift;

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
        return (byte)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator sbyte(BigFloat value)
    {
        return (sbyte)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 16-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ushort(BigFloat value)
    {
        return (ushort)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 16-bit signed integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator short(BigFloat value)
    {
        return (short)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 64-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ulong(BigFloat value)
    {
        return (ulong)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 64-bit signed integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(BigFloat value)
    {
        return (long)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 128-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UInt128(BigFloat value)
    {
        return (UInt128)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed 128-bit integer. 
    /// The fractional part (including GuardBits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int128(BigFloat value)
    {
        return (Int128)MantissaWithoutGuardBits(value.Mantissa << value.Scale);
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
    /// Defines an explicit conversion of a BigFloat to a Double with full IEEE 754 compliance.
    /// Handles normal numbers, subnormal numbers, and special values with optimal performance.
    /// Caution: Precision is not preserved since double is hard coded with 53 bits of precision.</summary>
    /// </summary>
    public static explicit operator double(BigFloat value)
    {
        if (value.IsZero) { return 0.0; }

        bool isNegative = value.Mantissa.Sign < 0;
        long biasedExp = value.BinaryExponent + 1023L;

        // Fast path: Handle overflow to infinity
        if (biasedExp > 2046L)
        {
            return isNegative ? double.NegativeInfinity : double.PositiveInfinity;
        }

        // Fast path: Handle normal numbers (most common case)
        if (biasedExp >= 1L)
        {
            // Extract top 53 bits from mantissa
            long mantissaBits = (long)(BigInteger.Abs(value.Mantissa) >> (value._size - 53));

            // Remove the implicit leading bit for IEEE 754 format
            mantissaBits &= (1L << 52) - 1;

            // Construct the double bits: sign(1) + exponent(11) + mantissa(52)
            long doubleBits2 = (biasedExp << 52) | mantissaBits;
            if (isNegative)
                doubleBits2 |= 1L << 63;

            return BitConverter.Int64BitsToDouble(doubleBits2);
        }

        // Handle subnormal numbers and underflow
        // Subnormal range: 2^(-1074) ≤ |value| < 2^(-1022)
        // IEEE 754 subnormal: value = (-1)^sign × 2^(-1022) × (mantissa/2^52)

        // Check if value is too small even for subnormals
        if (value.BinaryExponent < -1074)
        {
            return isNegative ? double.NegativeZero : 0.0;
        }

        // Calculate the subnormal mantissa field
        // We need: BigFloat_value = 2^(-1022) × (result_mantissa / 2^52)
        // Therefore: result_mantissa = BigFloat_value × 2^(1022 + 52) = BigFloat_value × 2^1074

        int mantissaShift = 1074 + value.BinaryExponent;

        // Validate shift amount for subnormal range
        if (mantissaShift < 1 || mantissaShift > 52)
        {
            return isNegative ? double.NegativeZero : 0.0;
        }

        // Extract the appropriate bits for subnormal mantissa
        BigInteger absMantissa = BigInteger.Abs(value.Mantissa);
        long subnormalMantissa;

        if (mantissaShift <= value._size)
        {
            // Right shift to extract the relevant bits
            subnormalMantissa = (long)(absMantissa >> (value._size - mantissaShift));
        }
        else
        {
            // Left shift needed - check bounds to prevent overflow
            int leftShift = mantissaShift - value._size;
            if (leftShift > 64 - value._size)
        {
                return isNegative ? double.NegativeZero : 0.0;
            }
            subnormalMantissa = (long)(absMantissa << leftShift);
        }

        // Ensure mantissa fits in 52-bit field and handle rounding
        subnormalMantissa = Math.Min(subnormalMantissa, (1L << 52) - 1);

        if (subnormalMantissa == 0L)
        {
            return isNegative ? double.NegativeZero : 0.0;
        }

        // Construct subnormal double (exponent field = 0)
        long doubleBits = subnormalMantissa;
        if (isNegative)
            doubleBits |= 1L << 63;

        return BitConverter.Int64BitsToDouble(doubleBits);
    }

    /// <summary>
    /// Defines an explicit conversion of a BigFloat to a Single (float) with full IEEE 754 compliance.
    /// Handles normal numbers, subnormal numbers, and special values with optimal performance.
    /// Caution: Precision is not preserved since float is hard coded with 26 bits of precision.</summary>
    /// </summary>
    public static explicit operator float(BigFloat value)
    {
        if (value.IsZero) { return 0.0f; }

        bool isNegative = value.Mantissa.Sign < 0;
        int biasedExp = value.BinaryExponent + 127;

        // Fast path: Handle overflow to infinity
        if (biasedExp > 254)
        {
            return isNegative ? float.NegativeInfinity : float.PositiveInfinity;
        }

        // Fast path: Handle normal numbers (most common case)
        if (biasedExp >= 1)
        {
            // Extract top 24 bits from mantissa, then remove implicit leading bit
            int mantissaBits = (int)(BigInteger.Abs(value.Mantissa) >> (value._size - 24));

            // Remove the implicit leading bit for IEEE 754 format (keep only 23 bits)
            mantissaBits &= (1 << 23) - 1;

            // Construct the float bits: sign(1) + exponent(8) + mantissa(23)
            int floatBits2 = (biasedExp << 23) | mantissaBits;
            if (isNegative)
                floatBits2 |= 1 << 31;

            return BitConverter.Int32BitsToSingle(floatBits2);
        }

        // Handle subnormal numbers and underflow
        // Subnormal range: 2^(-149) ≤ |value| < 2^(-126)
        // IEEE 754 subnormal: value = (-1)^sign × 2^(-126) × (mantissa/2^23)

        // Check if value is too small even for subnormals
        if (value.BinaryExponent < -149)
        {
            return isNegative ? float.NegativeZero : 0.0f;
        }

        // Calculate the subnormal mantissa field
        // We need: BigFloat_value = 2^(-126) × (result_mantissa / 2^23)
        // Therefore: result_mantissa = BigFloat_value × 2^(126 + 23) = BigFloat_value × 2^149

        int mantissaShift = 149 + value.BinaryExponent;

        // Validate shift amount for subnormal range
        if (mantissaShift < 1 || mantissaShift > 23)
        {
            return isNegative ? float.NegativeZero : 0.0f;
        }

        // Extract the appropriate bits for subnormal mantissa
        BigInteger absMantissa = BigInteger.Abs(value.Mantissa);
        int subnormalMantissa;

        if (mantissaShift <= value._size)
        {
            // Right shift to extract the relevant bits
            subnormalMantissa = (int)(absMantissa >> (value._size - mantissaShift));
        }
        else
        {
            // Left shift needed - check bounds to prevent overflow
            int leftShift = mantissaShift - value._size;
            if (leftShift > 32 - value._size)
        {
                return isNegative ? float.NegativeZero : 0.0f;
            }
            subnormalMantissa = (int)(absMantissa << leftShift);
        }

        // Ensure mantissa fits in 23-bit field
        subnormalMantissa = Math.Min(subnormalMantissa, (1 << 23) - 1);

        if (subnormalMantissa == 0)
        {
            return isNegative ? float.NegativeZero : 0.0f;
        }

        // Construct subnormal float (exponent field = 0)
        int floatBits = subnormalMantissa;
        if (isNegative)
            floatBits |= 1 << 31;

        return BitConverter.Int32BitsToSingle(floatBits);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 32-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(BigFloat value)
    {
        return (int)RightShiftWithRound(value.Mantissa, GuardBits - value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 32-bit integer input. The fractional part (including guard bits) are simply discarded.</summary>
    public static explicit operator uint(BigFloat value)
    {
        return (uint)RightShiftWithRound(value.Mantissa, GuardBits - value.Scale);
    }

    /// <summary>Casts a BigFloat to a BigInteger. The fractional part (including guard bits) are simply discarded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigInteger(BigFloat value)
    {
        return RightShiftWithRound(value.Mantissa, GuardBits - value.Scale);
    }

    /// <summary>Checks to see if a BigFloat's value would fit into a normalized double without the exponent overflowing or underflowing. 
    /// Since BigFloats can be any precision and doubles have fixed 53-bits precision, precision is ignored.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FitsInADouble()
    {
        // future: possibly add denormalized support 
        return (BinaryExponent + 1023) is not (< -52 or > 2046);
    }

    ///////////////////////// COMPARE FUNCTIONS /////////////////////////

    // Examples (Assuming GuardBit Size Constant is 4)
    // 11|1.1000  Scale < 0 - false b/c inconclusive (any scale < 0 is invalid since the unit value is out of scope)
    // 111.|1000  Scale ==0 - when scale is 0, it is always an integer
    // 111.10|00  Scale > 0 - if after rounding, any bits between the radix and guard are '1' then not an integer 

    /// <summary>Returns an input that indicates whether the current instance and a signed 64-bit integer have the same input.</summary>
    public bool Equals(long other)
    {
        // 'this' is too large, not possible to be equal. The only 64 bit long is long.MinValue
        if (BinaryExponent > 62) { return BinaryExponent == 63 && other == long.MinValue; }
        if (BinaryExponent < -1) { return other == 0; }
        if (BinaryExponent == 63 && WouldRoundUp(Mantissa, GuardBits)) { return false; } // too large by 1
        if (!IsInteger) { return false; }

        return other == (long)RightShiftWithRound(Mantissa << Scale, GuardBits);
    }

    /// <summary>Returns an input that indicates whether the current instance and an unsigned 64-bit integer have the same input.</summary>
    public bool Equals(ulong other)
    {
        if (BinaryExponent >= 64) { return false; }  // 'this' is too large, not possible to be equal.
        if (BinaryExponent < -1) { return other == 0; }
        if ((Mantissa >> (GuardBits - 1)).Sign < 0) { return false; }   // is negative
        if (BinaryExponent == 63 && WouldRoundUp(Mantissa, GuardBits)) { return false; }// too large by 1
        if (!IsInteger) { return false; } // are the top 1/4 of the guard bits zero?

        return (ulong)RightShiftWithRound(Mantissa << Scale, GuardBits) == other;
    }

    /// <summary>
    /// Returns true if the integer part of the BigFloat matches 'other'. 
    /// Examples:  1.1 == 1,  1.6 != 1,  0.6==1
    /// </summary>
    public bool Equals(BigInteger other)
    {
        return other.Equals(RightShiftWithRound(Mantissa, GuardBits));
    }

    /// <summary>
    /// Returns true if the parents BigFloat object have the same value (within the precision). 
    /// Examples:  1.11 == 1.1,  1.00 == 1.0,  1.11 != 1.10,  1.1 == 1.01
    /// </summary>
    public bool Equals(BigFloat other)
    {
        return CompareTo(other) == 0;
    }

    /// <summary>
    /// Returns true if the parent's BigFloat value has the same value of the object considering their precisions. 
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        AssertValid();
        return obj is BigFloat other && Equals(other);
    }

    /// <summary>Returns a 32-bit signed integer hash code for the current BigFloat object.</summary>
    public override int GetHashCode()
    {
        return RightShiftWithRound(Mantissa, GuardBits).GetHashCode() ^ Scale;
    }

    /// <summary>
    /// Checks whether this BigFloat struct holds a valid internal state.
    /// Returns true if valid; otherwise false.
    /// </summary>
    public bool Validate()
    {
        int realSize = (int)BigInteger.Abs(Mantissa).GetBitLength();
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

    /// <summary>
    /// Returns a zero BigFloat with specified least precision for maintaining accuracy context
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigFloat ZeroWithSpecifiedLeastPrecision(int precision)
    {
        return new BigFloat(0, precision, 0);
    }
}