// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

/// <summary>
/// BigFloat stores a BigInteger with a floating radix point.
/// </summary>
public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>, IFormattable, ISpanFormattable
{
    /// <summary>
    /// ExtraHiddenBits helps with precision by keeping an extra 32 bits. ExtraHiddenBits are a fixed amount of least-signification sub-precise bits.
    /// These bits helps guard against some nuisances such as "7" * "9" being 60. 
    /// </summary>
    public const int ExtraHiddenBits = 32;  // 0-62, must be even (for sqrt)

    /// <summary>
    /// Gets the full integer, including the hidden bits.
    /// </summary>
    public readonly BigInteger DataBits { get; }

    /// <summary>
    /// _size are the number of precision bits. It is equal to "ABS(DataBits).GetBitLength()". The ABS is for 
    ///       power-of-two negative BigIntegers (-1,-2,-4,-8...) so it is the same whether positive or negative.
    /// _size INCLUDES ExtraHiddenBits (the Property Size subtracts out ExtraHiddenBits)
    /// _size does not include rounding from ExtraHiddenBits. (11[111...111] (where [111...111] is ExtraHiddenBits) is still 2 bits. So the user will see it as 0b100 with a size of 2.)
    /// _size is 0 only when 'DataBits==0'
    /// When BigFloat is Zero, the size is zero.
    /// </summary>
    internal readonly int _size; // { get; init; }

    //future: Possible future feature
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
    /// When Scale is Zero, the value is equal to the DataBits with the ExtraHiddenBits removed. (i.e. DataBits >> ExtraHiddenBits)
    /// When BigFloat is Zero, scale is the point of least accuracy.
    /// note: _scale = Scale-ExtraHiddenBits (or Scale = _scale + ExtraHiddenBits)
    /// </summary>
    public readonly int Scale { get; init; }

    /// <summary>
    /// The Size is the precision. It in number of bits required to hold the number. 
    /// ExtraHiddenBits are subtracted out. Use SizeWithHiddenBits to include ExtraHiddenBits.
    /// </summary>
    public readonly int Size => Math.Max(0, _size - ExtraHiddenBits);

    /// <summary>
    /// The number of data bits. ExtraHiddenBits are included.  
    /// </summary>
    public readonly int SizeWithHiddenBits => _size;

    /// <summary>
    /// Returns the base-2 exponent of the number. This is the amount shift a simple 1 bit to the leading bit location.
    /// Examples: dataBits:11010 with BinExp: 3 -> 1101.0 -> 1.1010 x 2^ 3  
    ///           dataBits:11    with BinExp:-1 -> 0.11   -> 1.1    x 2^-1 
    /// </summary>
    public int BinaryExponent => Scale + _size - ExtraHiddenBits - 1;

    //see BigFloatZeroNotes.txt for notes
    /// <summary>
    /// Returns true if the value is essentially zero.
    /// </summary>
    public bool IsZero => _size == 0 || ((_size + Scale) < ExtraHiddenBits && _size < ExtraHiddenBits);

    // What is considered Zero: any dataInt that is LESS then 0|10000000, and also the shift results in a 0|10000000.
    //   IntData    Scale Size Sz+Sc Precision  Zero
    // 1|11111111.. << -2   33    31      1       N
    // 1|00000000.. << -2   33    31      1       N
    // 1|00000000.. << -1   33    32      1       N
    // 1|00000000.. <<  0   33    33      1       N
    // 0|11111111.. << -1   32    31      0       N
    // 0|10000000.. << -1   32    31      0       N
    // 0|10000000.. <<  0   32    32      0       N
    // 0|01111111.. << -1   31    30     -1       Y
    // 0|01111111.. <<  0   31    31     -1       Y (borderline)
    // 0|01111111.. <<  1   31    32     -1       N
    // 0|00111111.. <<  1   31    32     -2       Y (borderline)
    // 0|00111111.. <<  2   31    33     -2       N

    /// <summary>
    /// Returns true if the value is beyond exactly zero. A data bits and ExtraHiddenBits are zero.
    /// Example: IsStrictZero is true for "1.3 * (Int)0" and is false for "(1.3 * 2) - 2.6"
    /// </summary>
    public bool IsStrictZero => DataBits.IsZero;

    /// <summary>
    /// Returns true if there is less than 1 bit of precision. However, a false value does not guarantee that the number are precise. 
    /// </summary>
    public bool IsOutOfPrecision => _size < ExtraHiddenBits;

    /// <summary>
    /// Returns the precision of the BigFloat. This is the same as the size of the data bits. The precision can be zero or negative. A negative precision means the number is below the number of bits(HiddenBits) that are deemed precise. 
    /// </summary>
    public int Precision => _size - ExtraHiddenBits;

    /// <summary>
    /// Returns the accuracy of the BigFloat. The accuracy is equivalent to the opposite of the scale. A negative accuracy means the least significant bit is above the one place. A value of zero is equivalent to an integer. A positive value is the number of accurate places(in binary) to the right of the radix point.
    /// </summary>
    public int Accuracy => -Scale;

    /// <summary>
    /// Rounds and returns true if this value is positive. Zero is not considered positive or negative. Only the top bit in ExtraHiddenBits is counted.
    /// </summary>
    public bool IsPositive => Sign > 0;

    /// <summary>
    /// Rounds and returns true if this value is negative. Only the top bit in ExtraHiddenBits is counted.
    /// </summary>
    public bool IsNegative => Sign < 0;

    /// <summary>
    /// Rounds and returns -1 if negative, 0 if zero, and +1 if positive. Only the top bit in ExtraHiddenBits and top out-of-precision hidden bit are included.
    /// </summary>
    public int Sign => (_size >= ExtraHiddenBits - 1) ? DataBits.Sign : 0;

    /// <summary>
    /// Gets the integer part of the BigFloat. No scaling is applied. ExtraHiddenBits are rounded and removed.
    /// </summary>
    public readonly BigInteger UnscaledValue => DataIntValueWithRound(DataBits);

    public string DebuggerDisplay
    {
        get
        {
            string bottom8HexChars = (BigInteger.Abs(DataBits) & ((BigInteger.One << ExtraHiddenBits) - 1)).ToString("X8").PadLeft(8)[^8..];
            StringBuilder sb = new(32);
            _ = sb.Append($"{ToString(true)}, "); //  integer part using ToString()
            _ = sb.Append($"{(DataBits.Sign >= 0 ? " " : "-")}0x{BigInteger.Abs(DataBits) >> ExtraHiddenBits:X}|{bottom8HexChars}"); // hex part
            _ = sb.Append($"[{Size}+{ExtraHiddenBits}={_size}], {((Scale >= 0) ? "<<" : ">>")} {Math.Abs(Scale)}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Prints debug information for the BigFloat to the console.  
    /// </summary>
    /// <param name="varName">Prints an optional name of the variable.</param>
    public void DebugPrint(string varName = null)
    {
        string shift = $"{((Scale >= 0) ? "<<" : ">>")} {Math.Abs(Scale)}";
        if (!string.IsNullOrEmpty(varName))
        {
            Console.WriteLine($"{varName + ":"}");
        }

        Console.WriteLine($"   Debug : {DebuggerDisplay}");
        Console.WriteLine($"  String : {ToString()}");
        //Console.WriteLine($"  Int|hex: {DataBits >> ExtraHiddenBits:X}:{(DataBits & (uint.MaxValue)).ToString("X")[^8..]}[{Size}] {shift} (Hidden-bits round {(WouldRound() ? "up" : "down")})");
        Console.WriteLine($" Int|Hex : {ToStringHexScientific(true, true, false)} (Hidden-bits round {(WouldRoundUp() ? "up" : "down")})");
        Console.WriteLine($"    |Hex : {ToStringHexScientific(true, true, true)} (two's comp)");
        Console.WriteLine($"    |Dec : {DataBits >> ExtraHiddenBits}{((double)(DataBits & (((ulong)1 << ExtraHiddenBits) - 1)) / ((ulong)1 << ExtraHiddenBits)).ToString()[1..]} {shift}");
        Console.WriteLine($"    |Dec : {DataBits >> ExtraHiddenBits}:{DataBits & (((ulong)1 << ExtraHiddenBits) - 1)} {shift}");  // decimal part (e.g. .75)
        if (DataBits < 0)
        {
            Console.WriteLine($"   or -{-DataBits >> ExtraHiddenBits:X4}:{(-DataBits & (((ulong)1 << ExtraHiddenBits) - 1)).ToString("X8")[^8..]}");
        }

        Console.WriteLine($"    |Bits: {DataBits}");
        Console.WriteLine($"   Scale : {Scale}");
        Console.WriteLine();
    }

    /// <summary>
    /// Returns a Zero with no size/precision.
    /// </summary>
    public static BigFloat ZeroWithNoPrecision => new(0, 0, 0);

    /// <summary>
    /// Returns a Zero with a given lower bound of precision. Example: -4 would result of 0.0000(in binary). ExtraHiddenBits will be added.
    /// </summary>
    /// <param name="pointOfLeastPrecision">The precision can be positive or negative.</param>
    public static BigFloat ZeroWithSpecifiedLeastPrecision(int pointOfLeastPrecision)
    {
        return new(BigInteger.Zero, pointOfLeastPrecision, 0);
    }

    /// <summary>
    /// Returns a '1' with only 1 bit of precision. (1 << ExtraHiddenBits)
    /// </summary>
    public static BigFloat One => new(BigInteger.One << ExtraHiddenBits, 0, ExtraHiddenBits + 1);

    /// <summary>
    /// Returns a "1" with additional Accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between -32(ExtraHiddenBits) to Int.MaxValue.</param>
    public static BigFloat OneWithAccuracy(int precisionInBits)
    {
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return precisionInBits <= -ExtraHiddenBits
            ? ZeroWithNoPrecision
            : new(BigInteger.One << (ExtraHiddenBits + precisionInBits), -precisionInBits, ExtraHiddenBits + 1 + precisionInBits);
        // alternative: throw new ArgumentException("The requested precision would leave not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(BigInteger intVal, int precisionInBits)
    {
        int intSize = (int)BigInteger.Abs(intVal).GetBitLength();
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return precisionInBits < -(ExtraHiddenBits + intSize)
            ? ZeroWithNoPrecision
            : new(intVal << (ExtraHiddenBits + precisionInBits), -precisionInBits, ExtraHiddenBits + intSize + precisionInBits);
        // alternative: throw new ArgumentException("The requested precision would leave not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(int intVal, int precisionInBits)
    {
        int size = int.Log2(int.Abs(intVal)) + 1 + ExtraHiddenBits;
        return precisionInBits < -size
            ? ZeroWithNoPrecision
            : new(((BigInteger)intVal) << (ExtraHiddenBits + precisionInBits), -precisionInBits, size + precisionInBits);
    }

    public static BigFloat NegativeOne => new(BigInteger.MinusOne << ExtraHiddenBits, 0, ExtraHiddenBits + 1);

    /////////////////////////    INIT / CONVERSION  FUNCTIONS     /////////////////////////

    /// <summary>
    /// Constructs a BigFloat using the raw elemental parts. The user is responsible to pre-up-shift rawValue and set <paramref name="binaryScaler"/> and <paramref name="rawValueSize"/> with respect to the ExtraHiddenBits.
    /// </summary>
    /// <param name="rawValue">The raw integerPart. It should INCLUDE the ExtraHiddenBits.</param>
    /// <param name="rawValueSize">The size of rawValue. </param>
    /// 
    private BigFloat(BigInteger rawValue, int binaryScaler, int rawValueSize)
    {
        DataBits = rawValue;
        Scale = binaryScaler;
        _size = rawValueSize;

        AssertValid();
    }

    /// <summary>
    /// Constructs a BigFloat using its elemental parts. A starting <paramref name="integerPart"/> on how may binary places the point should be shifted (base-2 exponent) using <paramref name="binaryScaler"/>.
    /// </summary>
    /// <param name="integerPart">The integer part of the BigFloat that will have a <paramref name="binaryScaler"/> applied to it. </param>
    /// <param name="binaryScaler">How much should the <paramref name="integerPart"/> be shifted or scaled? This shift (base-2 exponent) will be applied to the <paramref name="integerPart"/>.</param>
    /// <param name="valueIncludesHiddenBits">if true, then the hidden bits should be included in the integer part.</param>
    public BigFloat(BigInteger integerPart, int binaryScaler = 0, bool valueIncludesHiddenBits = false)
    {
        int applyHiddenBits = valueIncludesHiddenBits ? 0 : ExtraHiddenBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        DataBits = integerPart << applyHiddenBits;
        _size = (int)BigInteger.Abs(DataBits).GetBitLength();
        Scale = binaryScaler; // DataBits of zero can have scale

        AssertValid();
    }

    public BigFloat(char integerPart, int binaryScaler = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = binaryScaler;

        // Special handing required for int.MinValue
        _size = integerPart >= 0
            ? integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart != char.MinValue
                ? integerPart == 0 ? 0 : BitOperations.Log2((byte)-integerPart) + 1 + ExtraHiddenBits
                : 7 + ExtraHiddenBits;

        AssertValid();
    }

    public BigFloat(byte integerPart, int binaryScaler = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = binaryScaler;
        _size = integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(int integerPart, int binaryScaler = 0) : this((long)integerPart, binaryScaler) { }

    public BigFloat(uint value, int scale = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(long value, int binaryScaler = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = binaryScaler;
        _size = value switch
        {
            > 0 => BitOperations.Log2((ulong)value) + 1 + ExtraHiddenBits,
            < 0 => 64 - BitOperations.LeadingZeroCount(~((ulong)value - 1)) + ExtraHiddenBits,
            _ => 0,
        };
        AssertValid();
    }

    public BigFloat(ulong value, int binaryScaler = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = binaryScaler;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();
    }

    public BigFloat(Int128 integerPart, int binaryScaler, bool valueIncludesHiddenBits)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = binaryScaler;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();

        int applyHiddenBits = valueIncludesHiddenBits ? 0 : ExtraHiddenBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        _size = (int)((BigInteger)(integerPart >= 0 ? integerPart : -integerPart)).GetBitLength() + applyHiddenBits;
        DataBits = integerPart << applyHiddenBits;
        Scale = binaryScaler; // DataBits of zero can have scale
        AssertValid();
    }

    public BigFloat(double value, int binaryScaler = 0)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        long mantissa = bits & 0xfffffffffffffL;
        int exp = (int)((bits >> 52) & 0x7ffL);

        if (exp == 2047)  // 2047 represents inf or NAN
        {
            //if (double.IsNaN(value))
            //{
            //    DataBits = 0;
            //    Scale = scale;
            //    _size = 0;
            //    return;
            //}
            //if (double.IsInfinity(value))
            //{
            //    ThrowInitializeException();
            //}
            ThrowInitializeException(); // mantissa==0 is Inf else NAN
        }
        else if (exp != 0)
        {
            mantissa |= 0x10000000000000L;
            if (value < 0)
            {
                mantissa = -mantissa;
            }
            DataBits = new BigInteger(mantissa) << ExtraHiddenBits;
            Scale = exp - 1023 - 52 + binaryScaler;
            _size = 53 + ExtraHiddenBits; //_size = BitOperations.Log2((ulong)Int);
        }
        else // exp is 0 so this is a denormalized float (leading "1" is "0" instead)
        {
            // 0:00000000000:00...0001 -> smallest value (Epsilon)  Int:1, Scale: Size:1
            // ...

            if (mantissa == 0)
            {
                DataBits = 0;
                Scale = binaryScaler;
                _size = 0;
            }
            else
            {
                int size = 64 - BitOperations.LeadingZeroCount((ulong)mantissa);
                if (value < 0)
                {
                    mantissa = -mantissa;
                }
                DataBits = (new BigInteger(mantissa)) << (ExtraHiddenBits);
                Scale = -1023 - 52 + 1 + binaryScaler;
                _size = size + ExtraHiddenBits;
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
              //if (float.IsNaN(value))
              //{
              //    DataBits = 0;
              //    Scale = scale;
              //    _size = 0;
              //    return;
              //}
              //if (float.IsInfinity(value))
              //{
              //    ThrowInitializeException();
              //}
                ThrowInitializeException(); // mantissa==0 is Inf else NAN
            }
            // Add leading 1 bit
            mantissa |= 0x800000;
            if (value < 0)
            {
                mantissa = -mantissa;
            }
            DataBits = new BigInteger(mantissa) << ExtraHiddenBits;
            Scale = exp - 127 - 23 + binaryScaler;
            _size = 24 + ExtraHiddenBits;
        }
        else // exp is 0 so this is a denormalized(Subnormal) float (leading "1" is "0" instead)
        {
            if (mantissa == 0)
            {
                DataBits = 0;
                Scale = binaryScaler;
                _size = 0; //24 + ExtraHiddenBits;
            }
            else
            {
                BigInteger mant = new(value >= 0 ? mantissa : -mantissa);
                DataBits = mant << ExtraHiddenBits;
                Scale = -126 - 23 + binaryScaler; //hack: 23 is a guess
                _size = 32 - BitOperations.LeadingZeroCount((uint)mantissa) + ExtraHiddenBits;
            }
        }

        AssertValid();
    }

    [DoesNotReturn]
    private static void ThrowInitializeException()
    {
        throw new OverflowException("Value was too large for a BigFloat.");
    }
    ///////////////////////// [END] INIT / CONVERSION  FUNCTIONS [END] /////////////////////////

    /// <summary>
    /// Checks to see if the value is an integer. Returns true if all the bits between the radix point and the middle of ExtraHiddenBits are all 0 or all 1.
    ///   for scale <= 0, if all bits are 0 or 1 between radix and half-way through the ExtraHiddenBits
    ///   for scale >= (ExtraHiddenBits/2), is always true.
    /// 
    /// if we call it an integer then it should follow that ...
    ///   it should not round up based on ExtraHiddenBits
    ///   Ceiling would round up (and Floor down for negative)
    /// </summary>
    public bool IsInteger  //v3 -  just checks bits between radix and middle of hidden bits
    {
        get
        {
            int begMask = ExtraHiddenBits >> 1;
            int endMask = ExtraHiddenBits - Scale;

            if (begMask <= Scale ||
                begMask >= endMask)
            {
                return true; // technically inconclusive though.
            }

            BigInteger mask = ((BigInteger.One << (endMask - begMask)) - 1) << begMask;
            BigInteger maskApplied = DataBits & mask;
            int bitsSet = (int)BigInteger.PopCount(maskApplied);
            return (bitsSet == 0) || (bitsSet == endMask - begMask);
        }
    }

    /// <summary>
    /// Tests to see if the number is in the format of "10000000..." after rounding.
    /// </summary>
    public bool IsOneBitFollowedByZeroBits => BigInteger.TrailingZeroCount(DataBits >> (ExtraHiddenBits - 1)) == (_size - ExtraHiddenBits);

    public ulong Lowest64BitsWithHiddenBits
    {
        get
        {
            ulong raw = (ulong)(DataBits & ulong.MaxValue);

            if (DataBits.Sign < 0)
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
            if (DataBits.Sign >= 0)
            {
                ulong raw = (ulong)((DataBits >> ExtraHiddenBits) & ulong.MaxValue);
                return raw;
            }
            else if (_size >= ExtraHiddenBits)
            {
                return ~(ulong)(((DataBits - 1) >> ExtraHiddenBits) & ulong.MaxValue);
                //return (ulong)((BigInteger.Abs(DataBits) >> ExtraHiddenBits) & ulong.MaxValue); //perf: benchmark

            }
            else
            {
                ulong raw = (ulong)((DataBits >> ExtraHiddenBits) & ulong.MaxValue);
                //raw--;
                raw = ~raw;
                return raw;
            }
        }
    }

    /// <summary>
    /// Returns the 64 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 64 bits, then the LSBs are padded with zeros.
    /// </summary>
    public ulong Highest64Bits => (ulong)((BigInteger.IsPositive(DataBits) ? DataBits : -DataBits) >> (_size - 64));

    /// <summary>
    /// Returns the 128 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 128 bits, then the LSBs are padded with zeros.
    /// </summary>
    public UInt128 Highest128Bits => (UInt128)((BigInteger.IsPositive(DataBits) ? DataBits : -DataBits) >> (_size - 128));

    /// <summary>
    /// Rounds to the next integer towards negative infinity. Any fractional bits are removed, negative scales are set
    /// to zero, and the precision(size) will be resized to just the integer part.
    /// </summary>
    public BigFloat Floor()
    {
        int bitsToClear = ExtraHiddenBits - Scale; // number of bits to clear from DataBits

        // 'Scale' will be zero or positive. (since all fraction bits are stripped away)
        // 'Size'  will be the size of the new integer part.
        // Precision of the decimal bits are stripped away. 

        // If bitsToClear <= 0, then all fraction bits are implicitly zero and nothing needs to be done.
        //   Example: Scale = 32+7, int=45, size=6+32=38 -> bitsToClear=-7   -101101[10101010010...00010]0000000.
        if (bitsToClear <= 0)
        {
            return this;
        }

        // If less then zero, we can just return 0 for positive numbers and -1 for negative.
        //   Example: Scale = -11, int=45, size=6+32=38  -> bitsToClear=32+11   -.00000 101101[10101010010...00010]
        if (bitsToClear >= _size)
        {
            return DataBits.Sign >= 0 ? new BigFloat(0, 0, 0) : new BigFloat(BigInteger.MinusOne << ExtraHiddenBits, 0, 1 + ExtraHiddenBits);
        }

        if (DataBits.Sign > 0)
        {
            // If Positive and Floor, the size should always remain the same.
            // If Scale is between 0 and ExtraHiddenBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            if (Scale >= 0) // SCALE >= 0 and SCALE < ExtraHiddenBits
            {
                return new BigFloat((DataBits >> bitsToClear) << bitsToClear, Scale, _size);
            }

            // If Scale is between -size and 0..
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]
            //BigInteger intPart = ((DataBits >> bitsToClear) + 1) << ExtraHiddenBits;
            //return new BigFloat((DataBits >> bitsToClear) +  (IsInteger?0:1));
            return new BigFloat(DataBits >> bitsToClear);
        }
        else  // if (DataBits.Sign <= 0)
        {
            //   If Negative and Flooring, and the abs(result) is a PowerOfTwo the size will grow by 1.  -1111.1 -> -10000, -10000 -> -10000
            // Lets just remove the bits and clear ExtraHiddenBits
            //   Example: Scale =  4, int=45, size=8+32=40  -> bitsToClear=32-4  11101101[1010.1010010...00010]  -> 11101101[1010.0000000...00000]

            // clear bitToClear bits 

            _ = ExtraHiddenBits - Math.Max(0, Scale);

            // If Scale is between 0 and ExtraHiddenBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
            if (Scale >= 0) // SCALE >= 0 and SCALE < ExtraHiddenBits
            {
                bool roundsUp = (DataBits & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = DataBits >> bitsToClear << bitsToClear;
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
                return new BigFloat(DataBits >> bitsToClear);
            }
        }
    }

    /// <summary>
    /// Rounds to the next integer towards positive infinity. Any fractional bits are removed, negative scales are set
    /// to zero, and the precision(size) will be resized to just the integer part.
    /// </summary>
    public BigFloat Ceiling()
    {
        int bitsToClear = ExtraHiddenBits - Scale; // number of bits to clear from DataBits

        // 'Scale' will be zero or positive. (since all fraction bits are stripped away)
        // 'Size'  will be the size of the new integer part.
        // Fractional bits are removed. (i.e. Negative precisions are set to zero.)

        // If bitsToClear <= 0, then all fraction bits are implicitly zero and nothing needs to be done.
        //   Example: Scale = 32+7, int=45, size=6+32=38 -> bitsToClear=-7   -101101[10101010010...00010]0000000.
        if (bitsToClear <= 0) // Scale >= ExtraHiddenBits
        {
            return this;
        }

        // If less then zero, we can just return 1 for positive numbers and 0 for negative.
        //   Example: Scale = -11, int=45, size=6+32=38  -> bitsToClear=32+11   -.00000 101101[10101010010...00010]
        if (bitsToClear >= _size)
        {
            return DataBits.Sign <= 0 ? new BigFloat(0, 0, 0) : new BigFloat(BigInteger.One << ExtraHiddenBits, 0, 1 + ExtraHiddenBits);
        }

        // Radix point is in the ExtraHiddenBits area
        //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
        if (Scale < ExtraHiddenBits) // SCALE >= 0 and SCALE<ExtraHiddenBits
        {
            // optimization here?
        }

        if (DataBits.Sign > 0)
        {
            //   If Positive and Ceiling, and the abs(result) is a PowerOfTwo the size will grow by 1.  -1111.1 -> -10000, -10000 -> -10000
            // Lets just remove the bits and clear ExtraHiddenBits
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]

            if (Scale >= 0) // Scale is between 0 and ExtraHiddenBits
            {
                //  Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
                bool roundsUp = (DataBits & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = DataBits >> bitsToClear << bitsToClear;
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
                // round up if any bits set between (ExtraHiddenBits/2) and (ExtraHiddenBits-Scale) 
                bool roundsUp = (DataBits & (((BigInteger.One << ((ExtraHiddenBits / 2) - Scale)) - 1) << (ExtraHiddenBits / 2))) > 0;

                BigInteger intPart = DataBits >> bitsToClear << ExtraHiddenBits;

                if (roundsUp)
                {
                    intPart += BigInteger.One << ExtraHiddenBits;
                }

                int newSize = roundsUp ? (int)intPart.GetBitLength() : _size - bitsToClear + ExtraHiddenBits; //future: maybe slow (maybe use BigInteger.TrailingZeroCount to detect rollover)

                return new BigFloat(intPart, 0, newSize);
            }
        }
        else  // if (DataBits.Sign <= 0)
        {
            // If Negative and Ceiling, the size should always remain the same.
            // If Scale is between 0 and ExtraHiddenBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            if (Scale >= 0)
            {
                return new BigFloat((DataBits >> bitsToClear) << bitsToClear, Scale, _size);
            }
            BigInteger intPart = DataBits >> bitsToClear;

            if (!IsInteger)
            {
                intPart++;
            }

            return new BigFloat(intPart);
        }
    }

    /// <summary>
    /// Returns the number of matching leading bits with rounding.  
    /// i.e. The largest number of leading bits that when rounded, become equal.
    /// i.e. The difference in their Log2 values.
    /// i.e. size - BitSize(abs(a-b) 
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
    /// HiddenBits are included.
    /// </summary>
    /// <param name="a">The first BigFloat to compare to.</param>
    /// <param name="b">The second BigFloat to compare to.</param>
    /// <param name="sign">(out) Returns the sign of a-b. Example: If a is larger the sign is set to 1.</param>
    public static int NumberOfMatchingLeadingBitsWithRounding(BigFloat a, BigFloat b, out int sign)
    {
        // only 1 bit or less size difference, so we could have a...
        //    11111111/100000000 that would have difference b1     so 7 matching bits   
        //    11110000/100000000 that would have difference b10000 so 3 matching bits   
        //   -11110000/100000000 that would have difference b10000 so 0 matching bits

        int maxSize = Math.Max(a._size, b._size);
        int expDiff = a.BinaryExponent - b.BinaryExponent;
        if (maxSize == 0 || a.Sign != b.Sign || Math.Abs(expDiff) > 1)
        {
            sign = (expDiff > 0) ? a.Sign : -b.Sign;
            return 0;
        }

        int scaleDiff = a.Scale - b.Scale;

        BigInteger temp = (scaleDiff < 0) ?
                a.DataBits - (b.DataBits << scaleDiff)
                : (a.DataBits >> scaleDiff) - b.DataBits;

        sign = temp.Sign;

        return maxSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    /// <summary>
    /// Returns the number of matching leading bits that exactly match. HiddenBits are included.
    /// i.e. The number of leading bits that exactly match.
    /// e.g. 11010 and 11111 returns 2
    /// e.g. 100000 and 111111 returns 1
    /// If the signs do not match then 0 is returned.
    /// 
    /// The scale and precision(size) is ignored.
    /// e.g. 11101000000 and 11111 returns 3
    /// </summary>
    /// <param name="a">The first BigFloat to compare to.</param>
    /// <param name="b">The second BigFloat to compare to.</param>
    public static int NumberOfMatchingLeadingBits(BigFloat a, BigFloat b)
    {
        if (a.Sign != b.Sign) { return 0; }

        int sizeDiff = a._size - b._size;
        int newSize = sizeDiff > 0 ? b._size : a._size;

        if (newSize == 0) { return 0; }

        BigInteger temp = (sizeDiff < 0) ?
                a.DataBits - (b.DataBits << sizeDiff)
                : (a.DataBits >> sizeDiff) - b.DataBits;

        return newSize - (int)BigInteger.Log2(BigInteger.Abs(temp)) - 1;
    }

    //////////////////////////////////////////////////////////////////
    /////////////////////// Operator Overloads ///////////////////////
    //////////////////////////////////////////////////////////////////

    /////////// Operator Overloads: BigFloat <--> BigFloat ///////////

    /// <summary>Returns true if the left side BigFloat is equal to the right side BigFloat.</summary>
    public static bool operator ==(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) == 0;
    }

    /// <summary>Returns true if the left side BigFloat is not equal to the right BigFloat.</summary>
    public static bool operator !=(BigFloat left, BigFloat right)
    {
        return right.CompareTo(left) != 0;
    }
    public static bool operator <(BigFloat left, BigFloat right)
    {
        int a = left.CompareTo(right);
        return a < 0;
    }
    public static bool operator >(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) > 0;
    }
    public static bool operator <=(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) <= 0;
    }
    public static bool operator >=(BigFloat left, BigFloat right)
    {
        return left.CompareTo(right) >= 0;
    }

    /////////// Operator Overloads: BigFloat <--> BigInteger ///////////

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

    /////////// Operator Overloads: BigFloat <--> ulong/long ///////////

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
    /// Performs a modulus operation. For negative numbers there are two approaches, a math and programmers version. For negative numbers this version uses the programmers version.
    /// see: https://github.com/microsoft/calculator/issues/111
    /// </summary>
    public static BigFloat operator %(BigFloat dividend, BigFloat divisor)
    {
        // nice video on negative https://www.youtube.com/watch?v=AbGVbgQre7I
        return Remainder(dividend, divisor);
    }

    /// <summary>
    /// Performs a Modulus operation. 
    /// For positive values, Modulus is identical to Remainder, for negatives, Modulus and Remainder differ. 
    /// The remainder is slightly faster.
    /// </summary>
    // see BifFloatModuloNotes.txt for additional notes
    public static BigFloat Remainder(BigFloat dividend, BigFloat divisor)
    {
        int scaleDiff = dividend.Scale - divisor.Scale;

        return scaleDiff switch
        {
            > 0 => new(((dividend.DataBits << scaleDiff) % divisor.DataBits) >> scaleDiff, dividend.Scale, true),
            < 0 => new((dividend.DataBits % (divisor.DataBits >> scaleDiff)) << scaleDiff, divisor.Scale, true),
            0 => new(dividend.DataBits % divisor.DataBits, divisor.Scale, true),
        };
    }

    /// <summary>
    /// Performs a Modulus operation. 
    /// For positive values, Modulus is identical to Remainder, for negatives, Modulus and Remainder differ. 
    /// The remainder is slightly faster.
    /// </summary>
    // see BifFloatModuloNotes.txt for additional notes
    public static BigFloat Mod(BigFloat dividend, BigFloat divisor)
    {
        return Remainder(dividend, divisor) + ((dividend < 0) ^ (divisor > 0) ? 0 : divisor);
    }

    /// <summary>
    /// Bitwise Complement Operator - Reverses each bit in the data bits. Scale is not changed.
    /// The size is reduced by at least 1 bit. This is because the leading bit is flipped to a zero.
    /// </summary>
    public static BigFloat operator ~(BigFloat value)
    {
        BigInteger temp = value.DataBits ^ ((BigInteger.One << value._size) - 1);
        return new(temp, value.Scale, true);
    }

    //future: add logic operations
    //public static BigFloat operator &(BigFloat left, BigInteger right);
    //public static BigFloat operator |(BigFloat left, BigInteger right);
    //public static BigFloat operator ^(BigFloat left, BigInteger right);

    /// <summary>
    /// Left shift - Increases the size by adding least-signification zero bits. 
    /// i.e. The precision is enhanced. 
    /// No rounding is preformed.
    /// </summary>
    /// <param name="x">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    public static BigFloat operator <<(BigFloat x, int shift)
    {
        return new(x.DataBits << shift, x.Scale, x._size + shift);
    }

    /// <summary>
    /// Right shift - Decreases the size by removing the least-signification bits. 
    /// i.e. The precision is reduced. 
    /// No rounding is preformed. Scale is unaffected. 
    /// </summary>
    /// <param name="x">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    public static BigFloat operator >>(BigFloat x, int shift)
    {
        return new(x.DataBits >> shift, x.Scale, x._size - shift);
    }

    public static BigFloat operator +(BigFloat r)
    {
        return r;
    }

    public static BigFloat operator -(BigFloat r)
    {
        return new(-r.DataBits, r.Scale, r._size);
    }

    public static BigFloat operator ++(BigFloat r)
    {
        // hidden bits = 4
        // A)  1111|1111__.  => 1111|1111<< 6   +1  =>  1111|1111__.
        // B)  1111|1111_.   => 1111|1111<< 5   +1  =>  10000|0000#.
        // C)  1111|1111.    => 1111|1111<< 4   +1  =>  10000|0000.
        // D)  1111|1.111    => 1111|1111<< 1   +1  =>  10000|0.111
        // E)  1111.|1111    => 1111|1111<< 0   +1  =>  10000.|1111
        // F)  111.1|1111    => 1111|1111<< -1  +1  =>  1000.1|1111
        // G)  .1111|1111    => 1111|1111<< -4  +1  =>  1.1111|1111
        // H) .01111|1111    => 1111|1111<< -5  +1  =>  1.01111|1111

        int onesPlace = ExtraHiddenBits - r.Scale;

        if (onesPlace < 1)
        {
            return r; // A => -2 or less
        }

        // In the special case, we may not always want to round up when adding a 1 bit just below the LSB. 
        if (onesPlace == -1 && !r.DataBits.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r.DataBits + (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        // int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //perf: faster just to calc
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);
        return new BigFloat(intVal, r.Scale, sizeVal);
    }

    public static BigFloat operator --(BigFloat r)
    {
        int onesPlace = ExtraHiddenBits - r.Scale;

        if (onesPlace < 1)
        {
            return r;
        }

        // In the special case, we may not always want to round up when adding a 1 bit just below the LSB. 
        if (onesPlace == -1 && !r.DataBits.IsEven)
        {
            onesPlace = 0;
        }

        BigInteger intVal = r.DataBits - (BigInteger.One << onesPlace);
        int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
        //int sizeVal = (onesPlace > r._size) ? onesPlace +1 :  //perf: faster just to calc
        //    r._size + ((BigInteger.TrailingZeroCount(intVal) == r._size) ? 1 : 0);

        return new BigFloat(intVal, r.Scale, sizeVal);
    }

    public static BigFloat operator +(BigFloat r1, BigFloat r2)
    {
        // Shortcuts (to benchmark, does it actually save any time)
        // Given ExtraHiddenBits = 8, a number like "B2D"00 + 0.00"3F" should be just "B2D"00 since the smaller number is below the precision range.
        //
        // Example: "12345678"9ABCDEF0________.         (Size: 29, _size: 61, Scale: 64)
        //        +                  "12"34560.789A     (Size:  5, _size: 37, Scale: 20)
        //        =  12345678"9ABCDEF0________.
        //         
        // (if 64(r1.Scale) > 37(r2._size) + (20)r2.Scale then just return r1)

        int scaleDiff = r1.Scale - r2.Scale;

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

        if (r1.Scale == r2.Scale)
        {
            BigInteger intVal = r1.DataBits + r2.DataBits;
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r1.Scale, sizeVal);
        }
        else if (r1.Scale < r2.Scale)
        {
            BigInteger intVal = RightShiftWithRound(r1.DataBits, -scaleDiff) + r2.DataBits;
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r2.Scale, sizeVal);
        }
        else // if (r1.Scale > r2.Scale)
        {
            BigInteger intVal = r1.DataBits + RightShiftWithRound(r2.DataBits, scaleDiff);
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r1.Scale, sizeVal);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// Rounding, Shifting, Truncate ////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /*                                         : BI | RoundTo| Scales  |Can Round  | Shift     |
     *                                         | or | nearest| or Sets | up to     | or        |
    Public                                     | BF | int    | Size    |larger Size| Size by   |             notes
    ====================================================================================================================                  
    P BF  =(<<, >>)BF                          | F |   No    | SetsSize|  No   |    (param)    | Provides a shift similar to other data types. (removes/adds bits)
      BI  =DataIntValueWithRound(BI)/Int       | I | Rounds  | Scales  |  Yes  |ExtraHiddenBits| return WouldRound(val) ? (val >> ExtraHiddenBits) + 1 : val >> ExtraHiddenBits;
      BI  =DataIntValueWithRound(BI,bool)/Int  | I | Rounds  | Scales  |  Yes  |ExtraHiddenBits| return needToRound ? (val >> ExtraHiddenBits) + 1 : val >> ExtraHiddenBits;
    P BF  =RightShiftWithRound(BF,int)         | F | Rounds  | Scales  |  Yes  |    (param)    |
    P BF  =RightShiftWithRound(BF,int,out)     | F | Rounds  | Scales  |  Yes  |    (param)    |
      BI  =RightShiftWithRound(BI,ref int)     | I | Rounds  | Scales  |  Yes  |    (param)    |
      BI  =RightShiftWithRound(BI, int)        | I | Rounds  | Scales  |  Yes  |    (param)    |
      BF  =TruncateByAndRound(BF, int)         | F | Rounds  | SetsSize|  Yes  |    (param)    |
      BF  =TruncateToAndRound(BI, int)         | I | Rounds  | SetsSize|  Yes  |    (param)    |
      BF  =UpScale(BI, int)                    | I |   No    | Scales  |  No   |    (param)    | i.e. Shifts scale up
      BF  =DownScale(BI, int)                  | I |   No    | Scales  |  No   |    (param)    | i.e. Shifts using down
      BF  =AdjustScale(BI, int)                | I |   No    | Scales  |  No   |    (param)    | i.e. Shifts using up or down
      BF  =SetPrecision(BF, int)               | F |   No    | SetsSize|  No   |    (param)    |
    P BF  =SetPrecisionWithRound(BF,int)       | F | Rounds  | SetsSize|  Yes  |    (param)    |
      BF  =ExtendPrecision(BF, int)            | F |   No    | SetsSize|  No   |    (param)    |
      BI  Int                                  | I | Rounds  | Scales  |  Yes  |ExtraHiddenBits| i.e. Int => DataIntValueWithRound(DataBits);
Other:                                         |   |         |         |       |               |
    P bool=WouldRound()                        | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(DataBits, ExtraHiddenBits);
    P bool=WouldRound(int bottomBitsRemoved)   | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(DataBits, bottomBitsRemoved);
    P bool=WouldRound(BI)                      | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(bi, ExtraHiddenBits);
    P bool=WouldRound(BI,int bottomBitsRemove) | F | Rounds  | n/a     |  Yes  |    (param)    | return !(bi & ((BigInteger)1 << (bottomBitsRemoved - 1))).OutOfPrecision;

      
    * SetsSize = forces a particular size using the param (instead of add/removes the size)
    * Scale    = adjusts the size by a specific amt
    */

    /////////////////////////////////
    ////      WouldRound()
    /////////////////////////////////

    /// <summary>
    /// Checks to see if this integerPart would round-up if ExtraHiddenBits are removed.
    /// </summary>
    /// <param name="bi">The BigInteger we would like check if it would round up.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public static bool WouldRoundUp(BigInteger bi)
    {
        return WouldRoundUp(bi, ExtraHiddenBits);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the ExtraHiddenBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRoundUp()
    {
        return WouldRoundUp(DataBits, ExtraHiddenBits);
    }

    /// <summary>
    /// Checks to see if this integerPart would round-up given bottomBitsRemoved. 
    /// e.g. 11010101 with bottomBitsRemoved=3 would be 11011
    /// </summary>
    /// <param name="bottomBitsRemoved">The number of newSizeInBits from the least significant bit where rounding would take place.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRoundUp(int bottomBitsRemoved)
    {
        return WouldRoundUp(DataBits, bottomBitsRemoved);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the ExtraHiddenBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    private static bool WouldRoundUp(BigInteger val, int bottomBitsRemoved)
    {
        // for .net 7 and later use ">>>" instead of >> for a slight performance boost.
        bool isPos = val.Sign >= 0;
        return isPos ^ ((isPos ? val : val - 1) >> (bottomBitsRemoved - 1)).IsEven;
    }

    /////////////////////////////////////////////
    ////    DataIntValue() for BigInteger    ////
    /////////////////////////////////////////////

    /// <summary>
    /// Retrieves the internal data bits and removes ExtraHiddenBits and rounds.
    /// </summary>
    /// <param name="x">The DataBits part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x)
    {
        return RightShiftWithRound(x, ExtraHiddenBits);
    }

    /// <summary>
    /// Removes ExtraHiddenBits and rounds. It also requires the current size and will adjust it if it grows.
    /// </summary>
    /// <param name="x">The DataBits part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x, ref int size)
    {
        return RightShiftWithRound(x, ExtraHiddenBits, ref size);
    }

    ///////////////////////////////////////////////////
    ////      Set/Reduce Precision for BigFloat    ////
    ///////////////////////////////////////////////////

    /// <summary>
    /// Truncates a value by a specified number of bits by increasing the scale and reducing the precision.
    /// If the most significant bit of the removed bits is set then the least significant bit will increment away from zero. 
    /// e.g. 10.10010 << 2 = 10.101
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Example: 11.11 with 1 bit removed would result in 100.0 (the same size)
    /// This function uses the internal BigInteger RightShiftWithRound().
    /// Also see: ReducePrecision, RightShiftWithRoundWithCarryDownsize, RightShiftWithRound
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

        BigInteger b = RightShiftWithRound(x.DataBits, targetBitsToRemove, ref size);

        return new(b, newScale, size);
    }

    /// <summary>
    /// Adjust the scale of a value
    /// </summary>
    /// <param name="x">The value who's scale should be adjusted.</param>
    /// <param name="changeScaleAmount">The amount to change the scale.</param>
    /// <returns>Returns a BigFloat with the updated scale.</returns>
    public static BigFloat AdjustScale(BigFloat x, int changeScaleAmount)
    {
        return new BigFloat(x.DataBits, x.Scale + changeScaleAmount, x._size);
    }

    /// <summary>
    /// Sets the precision(and accuracy) of a number by appending 0 bits if too small or cropping bits if too large.
    /// This can be useful for extending whole or rational numbers precision. 
    /// No rounding is performed.
    /// Example: SetPrecision(0b1101, 8) --> 0b11010000;  SetPrecision(0b1101, 3) --> 0b110
    /// Also see: TruncateToAndRound, SetPrecisionWithRound
    /// </summary>
    /// <param name="x">The source BigFloat where a new size will be forced.</param>
    /// <param name="newSize">The number of zero bits to add.</param>
    /// <returns>A new BigFloat with the requested precision.</returns>
    public static BigFloat SetPrecision(BigFloat x, int newSize)
    {
        return new BigFloat(x.DataBits << (newSize - x.Size), x.Scale + (x.Size - newSize), newSize + ExtraHiddenBits);
    }

    /// <summary>
    /// Reduces the precision of the a number but keeps the value the same.
    /// i.e. Down-shifts the value but and increases the scale. 
    /// Example: ReducePrecision(0b1101.1101, 3) --> 0b1101.1; 
    /// No rounding is performed.
    /// Also see: TruncateByAndRound, RightShiftWithRoundWithCarryDownsize, RightShiftWithRound
    /// </summary>
    public static BigFloat ReducePrecision(BigFloat x, int reduceBy)
    {
        return new BigFloat(x.DataBits >> reduceBy, x.Scale + reduceBy, x._size - reduceBy);
    }

    /// <summary>
    /// Reduces the precision to the new specified size. To help maintain the most significant digits, the bits are not simply cut off. 
    /// When reducing the least significant bit will rounded up if the most significant bit is set of the removed bits. 
    /// This can be used to reduce the precision of a number before prior to a calculation.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Also see: SetPrecision, TruncateToAndRound
    /// </summary>
    /// <param name="newSizeInBits">The desired precision in bits.</param>
    public static BigFloat SetPrecisionWithRound(BigFloat x, int requestedNewSizeInBits)
    {
        int reduceBy = x.Size - requestedNewSizeInBits;
        BigFloat result = TruncateByAndRound(x, reduceBy);
        return result;
    }

    /// <summary>
    /// Extends the precision and accuracy of a number by appending 0 bits. 
    /// e.g. 1.1 --> 1.100000
    /// This can be useful for extending whole or rational numbers precision. 
    /// </summary>
    /// <param name="x">The source BigFloat that will be extended.</param>
    /// <param name="bitsToAdd">The number of zero bits to add. The number must be positive</param>
    /// <returns>Returns the larger value.</returns>
    public static BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
    {
        return bitsToAdd < 0
            ? throw new ArgumentOutOfRangeException(nameof(bitsToAdd), "cannot be a negative number")
            : new BigFloat(x.DataBits << bitsToAdd, x.Scale - bitsToAdd, x._size + bitsToAdd);
    }

    public static BigFloat operator -(BigFloat r1, BigFloat r2)
    {
        BigInteger r1Bits = (r1.Scale < r2.Scale) ? (r1.DataBits >> (r2.Scale - r1.Scale)) : r1.DataBits;
        BigInteger r2Bits = (r1.Scale > r2.Scale) ? (r2.DataBits >> (r1.Scale - r2.Scale)) : r2.DataBits;

        BigInteger diff = r1Bits - r2Bits;
        if (r1.Scale < r2.Scale ? r1.Sign < 0 : r2.DataBits.Sign < 0)
        {
            diff--;
        }

        int size = Math.Max(0, (int)BigInteger.Abs(diff).GetBitLength());

        return new BigFloat(diff, r1.Scale < r2.Scale ? r2.Scale : r1.Scale, size);
    }

    public static BigFloat PowerOf2(BigFloat val)
    {
        BigInteger prod = val.DataBits * val.DataBits;
        int resSize = (int)prod.GetBitLength();
        int shrinkBy = resSize - val._size;
        prod = RightShiftWithRound(prod, shrinkBy, ref resSize);
        int resScalePart = (2 * val.Scale) + shrinkBy - ExtraHiddenBits;
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

        int overSized = (val._size * 2) - maxOutputPrecisionInBits - (2 * ExtraHiddenBits);

        // We can just use PowerOf2 Function since output will never be larger then maxOutputPrecisionInBits.
        if (overSized <= 1)
        {
            BigFloat p2 = PowerOf2(val);

            // if size difference is 1 BUT the outputSize is still correct just return
            if (overSized <= 0 || p2._size == maxOutputPrecisionInBits)
            {
                return p2;
            }
            // output is oversized by 1 
            return new BigFloat(p2.DataBits, p2.Scale - 1, p2._size);
        }

        // at this point it is oversized by at least 2

        //oversized by 2 then shrink input by 1
        //oversized by 3 then shrink input by 1
        //oversized by 4 then shrink input by 2
        //oversized by 5 then shrink input by 2

        int inputShink = (overSized + 1) / 2;

        BigInteger valWithLessPrec = val.DataBits >> inputShink;

        BigInteger prod = valWithLessPrec * valWithLessPrec;

        int resBitLen = (int)prod.GetBitLength();
        int shrinkBy = resBitLen - val._size - (2 * ExtraHiddenBits);
        int sizePart = resBitLen - shrinkBy;
        prod = RightShiftWithRound(prod, shrinkBy);
        int resScalePart = (2 * val.Scale) + shrinkBy - ExtraHiddenBits;

        return new(prod, resScalePart, sizePart);
    }

    public static BigFloat operator *(BigFloat a, BigFloat b)
    {
        BigInteger prod;
        int shouldBe;
        const int SKIP_IF_SIZE_DIFF_SMALLER = 32;
        const int KEEP_EXTRA_PREC = 16;

        //perf: for performance what about no shift when _size's are around the same size. (like within 32) 

        int sizeDiff = a._size - b._size;
        int shiftBy = Math.Max(0, Math.Abs(sizeDiff) - KEEP_EXTRA_PREC);

        // for size differences that are:
        //   0 to 31(SKIP_IF_SIZE_DIFF_SMALLER), no shift takes place (saves time on shift and increases precision on the LSB in rare cases)
        //   > 32, there is a shift of 16 or more (but size difference will be limited to 16 for extra precision)

        if (Math.Abs(sizeDiff) < SKIP_IF_SIZE_DIFF_SMALLER)
        {
            shiftBy = 0;
            prod = b.DataBits * a.DataBits;
            shouldBe = Math.Min(a._size, b._size);
        }
        else if (sizeDiff > 0)
        {
            prod = (a.DataBits >> shiftBy) * b.DataBits;
            shouldBe = b._size;
        }
        else //if (sizeDiff < 0)
        {
            prod = (b.DataBits >> shiftBy) * a.DataBits;
            shouldBe = a._size;
        }

        int sizePart = (int)BigInteger.Abs(prod).GetBitLength();
        int shrinkBy = sizePart - shouldBe;

        prod = RightShiftWithRound(prod, shrinkBy, ref sizePart);

        int resScalePart = a.Scale + b.Scale + shrinkBy + shiftBy - ExtraHiddenBits;

        BigFloat result = new(prod, resScalePart, sizePart);

        return result;
    }

    public static BigFloat operator /(BigFloat divisor, BigFloat dividend)
    {
        // find the size of the smaller input to determine output size
        int outputSize = Math.Min(divisor.Size, dividend.Size);

        // If we right-shift divisor to align it with dividend and then divisor < dividend, then we need to decrement the output size.
        // This is because we would only have a partial bit of precision on the last bit, and it could introduce error.
        // note: We could also left shift dividend so it is left aligned with divisor but that would be more expensive. (but could be more accurate)
        // note: We can maybe speed this up by just checking the top 32 or 64 bits of each.
        if (divisor.DataBits >> (divisor.Size - dividend.Size) <= dividend.DataBits)
        {
            outputSize--;
        }

        // We need to oversize T (using left shift) so when we divide, it is the correct size.
        int wantedSizeForT = (1 * dividend.Size) + outputSize + ExtraHiddenBits;

        int leftShiftTBy = wantedSizeForT - divisor.Size;

        BigInteger leftShiftedT = divisor.DataBits << leftShiftTBy; // rightShift used here instead of SetPrecisionWithRound for performance

        // Now we can just divide, and we should have the correct size
        BigInteger resIntPart = leftShiftedT / dividend.DataBits;

        int resScalePart = divisor.Scale - dividend.Scale - leftShiftTBy + ExtraHiddenBits;

        int sizePart = (int)BigInteger.Abs(resIntPart).GetBitLength();

        BigFloat result = new(resIntPart, resScalePart, sizePart);

        return result;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////// implicit CASTS ///////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Defines an implicit conversion of a 8-bit signed integer to a BigFloat.</summary>
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

    /// <summary>Defines an implicit conversion of a signed 32-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(int value)
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
    public static implicit operator BigFloat(UInt128 value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an implicit conversion of a signed 64-bit integer to a BigFloat.</summary>
    public static implicit operator BigFloat(Int128 value)
    {
        return new BigFloat(value);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////// explicit CASTS ///////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// <summary>Defines an explicit conversion of a System.Decimal object to a BigFloat. </summary>
    //public static explicit operator BigFloat(decimal input) => new BigFloat(input);

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned byte.</summary>
    public static explicit operator byte(BigFloat value)
    {
        //return (byte)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (byte)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);

    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed byte.</summary>
    public static explicit operator sbyte(BigFloat value)
    {
        //return (sbyte)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (sbyte)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 16-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ushort(BigFloat value)
    {
        //return (ushort)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (ushort)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 16-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator short(BigFloat value)
    {
        //return (short)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (short)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 64-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ulong(BigFloat value)
    {
        //return (ulong)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (ulong)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);

    }
    /// <summary>Defines an explicit conversion of a BigFloat to a 64-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator long(BigFloat value)
    {
        //return (long)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (long)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator UInt128(BigFloat value)
    {
        //return (UInt128)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (UInt128)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator Int128(BigFloat value)
    {
        //return (Int128)(value.DataBits << (value.Scale - ExtraHiddenBits));
        return (Int128)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>
    /// Casts a BigInteger to a BigFloat. The ExtraHiddenBits are set to zero. 
    /// Example: a BigInteger of 1 would translate to "1+ExtraHiddenBits" bits of precision.
    /// </summary>
    /// <param name="value">The BigInteger to cast to a BigFloat.</param>
    public static explicit operator BigFloat(BigInteger value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an explicit conversion of a System.Double to a BigFloat.</summary>
    public static explicit operator BigFloat(double value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a Double.
    /// Caution: Precision is not preserved since double is hard coded with 53 bits of precision.</summary>
    public static explicit operator double(BigFloat value)
    {
        // Future: handle Subnormal numbers (when the exponent field contains all 0's) for anything from 2.2250738585072014 × 10−308 up to 4.9406564584124654E-324.
        if (value.IsOutOfPrecision)
        {
            return value.IsZero ? 0.0 : double.NaN;
        }
        // Aline and move input.val to show top 53 bits then pre-append a "1" bit.
        // was: long mantissa = (long)(value.DataBits >> (value._size - 53)) ^ ((long)1 << 52);

        long mantissa = (long)(BigInteger.Abs(value.DataBits) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.BinaryExponent + 1023;// + 52 -4;

        // Check to see if it fits in a normalized double (untested)
        if (exp <= 0)
        {
            return value.IsPositive ? 0 : double.NegativeZero;
        }
        if (exp > 2046)
        {
            return value.IsPositive ? double.PositiveInfinity : double.NegativeInfinity;
        }

        long dubAsLong = mantissa | (exp << 52);

        //set sign if negative
        if (value.DataBits.Sign < 0)
        {
            dubAsLong ^= (long)1 << 63;
        }

        double result = BitConverter.Int64BitsToDouble(dubAsLong);
        return result;
    }

    /// <summary>
    /// Casts a BigFloat to a BigInteger. The fractional part (including hidden bits) are simply discarded.
    /// </summary>
    /// <param name="value">The BigFloat to cast as a BigInteger.</param>
    public static explicit operator BigInteger(BigFloat value)
    {
        return value.DataBits << (value.Scale - ExtraHiddenBits);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a single floating-point.
    /// Caution: Precision is not preserved since float is hard coded with 26 bits of precision.</summary>
    public static explicit operator float(BigFloat value)
    {
        // Future: handle Subnormal numbers (when the exponent field contains all 0's) for anything from 2.2250738585072014 × 10−308 up to 4.9406564584124654E-324.
        if (value.IsOutOfPrecision)
        {
            return value.IsZero ? 0.0f : float.NaN;
        }

        int mantissa = (int)(BigInteger.Abs(value.DataBits) >> (value._size - 24)) ^ (1 << 23);
        int exp = value.BinaryExponent + 127;

        // Check to see if it fits in a normalized double (untested)
        if (exp <= 0)
        {
            return value.IsPositive ? 0 : float.NegativeZero;
        }
        if (exp > 254)
        {
            return value.IsPositive ? float.PositiveInfinity : float.NegativeInfinity;
        }

        int singleAsInteger = mantissa | (exp << 23);

        //set sign if negative
        if (value.DataBits.Sign < 0)
        {
            singleAsInteger ^= 1 << 31;
        }

        float result = BitConverter.Int32BitsToSingle(singleAsInteger);

        return result;
    }

    /// <summary>Defines an explicit conversion of a System.Single to a BigFloat.</summary>
    public static explicit operator BigFloat(float value)
    {
        return new BigFloat(value);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 32-bit signed integer.</summary>
    public static explicit operator int(BigFloat value)
    {
        return (int)(value.DataBits << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 32-bit integer input. The fractional part (including hidden bits) are simply discarded.</summary>
    public static explicit operator uint(BigFloat value)
    {
        return (uint)(value.DataBits << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Checks to see if a BigFloat's value would fit into a normalized double without the exponent overflowing or underflowing. 
    /// Since BigFloats can be any precision and doubles have fixed 53-bits precision, precision is ignored.</summary>
    public bool FitsInADouble()
    {
        // future (possibly): add denormalized support 
        //return (Exponent + 1023 - 1) is not (<= 0 or > 2046);
        return (BinaryExponent + 1023) is not (< -52 or > 2046);
    }

    /////////////////////////////////// COMPARE FUNCTIONS ////////////////////////////////////////////////////////

    /// <summary>Returns an input that indicates whether the current instance and a signed 64-bit integer have the same input.</summary>
    public bool Equals(long other)
    {
        //Todo: what about zero?
        if (BinaryExponent >= 64) // 'this' is too large, not possible to be equal.
        {
            return false;
        }
        else if (BinaryExponent < -1)
        {
            return other == 0;
        }
        else if (BinaryExponent == 63)
        {
            // if 64 bits then 'other' must be long.MinValue as that is the only 64 bit input
            // any Int of the form "1000"000000000 is also valid if the _scale is set correctly.

            // return (other == long.MinValue && Int.Equals(long.MinValue));

            // short-circuit - if 64 bits then other has to be long.MinValue
            if (other != long.MinValue)
            {
                return false;
            }

            //return (Int << _scale) == other;
        }

        return Scale >= 0 ? DataBits >> ExtraHiddenBits == other >> Scale : DataBits << (Scale - ExtraHiddenBits) == other;

    }

    /// <summary>Returns an input that indicates whether the current instance and an unsigned 64-bit integer have the same input.</summary>
    public bool Equals(ulong other)
    {
        if (BinaryExponent >= 64)
        {
            return false; // too large
        }
        else if (BinaryExponent < -1)
        {
            return other == 0;
        }
        else if (DataBits.Sign < 0)
        {
            return false; // negative
        }

        return Scale >= 0 ? DataBits >> ExtraHiddenBits == other >> Scale : DataBits << (Scale - ExtraHiddenBits) == other;
    }

    /// <summary>
    /// Returns true if the integer part of the BigFloat matches 'other'. 
    /// Examples:  1.1 == 1,  1.6 != 1,  0.6==1
    /// </summary>
    public bool Equals(BigInteger other)
    {
        return other.Equals(UnscaledValue);
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
    /// Source: .Net 9, BigInteger.Equals
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        AssertValid();

        return obj is BigFloat other && Equals(other);
    }
    //public override bool Equals(object obj)
    //{
    //    //Check for null and compare run-time types.
    //    if (obj == null || !GetType().Equals(obj.GetType()))
    //    {
    //        return false;
    //    }

    //    BigFloat p = (BigFloat)obj;
    //    return Equals(p); //todo: to test
    //}


    /// <summary>Returns a 32-bit signed integer hash code for the current BigFloat object.</summary>
    public override int GetHashCode()
    {
        return DataIntValueWithRound(DataBits).GetHashCode() ^ Scale;
    }

    /// <summary>
    /// Checks whether this BigFloat struct holds a valid internal state.
    /// Returns true if valid; otherwise false.
    /// </summary>
    public bool Validate()
    {
        // Calculate the bit length (absolute value for sign-agnostic size).
        int realSize = (int)BigInteger.Abs(DataBits).GetBitLength();

        bool valid = _size == realSize;

        // Optional: in Debug builds, assert if something is off:
        Debug.Assert(valid,
            $"Invalid BigFloat: _size({_size}) does not match actual bit length ({realSize}).");

        return valid;
    }

    /// <summary>
    /// Debug-only method to assert validity on an instance.
    /// </summary>
    [Conditional("DEBUG")]
    private void AssertValid()
    {
        // Just call Validate() and assert if invalid. Or rely on the internal Debug.Assert inside Validate().
        _ = Validate();
    }

    /// <summary>
    /// Debug-only static method to assert validity on a given instance.
    /// </summary>
    /// <param name="val">BigFloat instance to validate.</param>
    [Conditional("DEBUG")]
    private static void AssertValid(BigFloat val)
    {
        val.AssertValid();
    }

}