﻿// Copyright Ryan Scott White. 2020, 2021, 2022, 2023, 2024

// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// Written by human hand - unless noted. This may change soon.
// Code written by Ryan Scott White unless otherwise noted.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using static BigFloatLibrary.BigIntegerTools;
using static BigFloatLibrary.Int128Tools;


namespace BigFloatLibrary;


// for notes on zero see "BigFloatZeroNotes.txt"

/// <summary>
/// BigFloat stores a BigInteger with a floating decimal point.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{
    /// <summary>
    /// ExtraHiddenBits helps with precision by keeping an extra 32 bits. ExtraHiddenBits are a fixed amount of least-signification sub-precise bits.
    /// These bits helps guard against some nuisances such as "7" * "9" being 60. 
    /// </summary>
    public const int ExtraHiddenBits = 32;  // 0-62, must be even (for sqrt)

    /// <summary>
    /// Gets the full integer with the hidden bits.
    /// </summary>
    public readonly BigInteger DataBits { get; }

    /// <summary>
    /// _size are the number of precision bits. It is equal to "ABS(_int).GetBitLength()". The ABS is for 
    ///       power-of-two negative BigIntegers (-1,-2,-4,-8...) so it is the same whether positive or negative.
    /// _size INCLUDES ExtraHiddenBits (the Property Size subtracts out ExtraHiddenBits)
    /// _size does not include rounding from ExtraHiddenBits. (11[111...111] (where [111...111] is ExtraHiddenBits) is still 2 bits. So the user will see it as 0b100 with a size of 2.)
    /// _size is 0 only when '_int==0'
    /// When BigFloat is Zero, the size is zero.
    /// </summary>
    internal readonly int _size; // { get; init; }

    //future: Possible future feature
    ///// <summary>
    ///// When positive, it's the number of least significant digits in DataBits that repeat.
    /////    Example: DataBits:11.001(with _extraPrecOrRepeat = 3) would be 11.001001001001...
    ///// When negative, it is the number of extra virtual zeros tacked on the end of the internal _int for better precision and accuracy.  
    ///// Example: 11.001(with _extraPrecOrRepeat = -3) would be the same as 11.001000  
    /////   For the above example "000" would not take up any space and is also guaranteed to be all 0 bits.
    ///// When zero, this feature does not get used. (Default)
    ///// </summary>
    // private readonly int _extraPrecOrRepeat;

    /// <summary>
    /// The Scale (or -Accuracy) is the amount to left shift (<<) the integer (or right shift the radix point) to get to the desired value. 
    /// When BigFloat is Zero, scale is the point of least accuracy.
    /// note: _scale = Scale-ExtraHiddenBits (or Scale = _scale + ExtraHiddenBits)
    /// </summary>
    public readonly int Scale { get; init; }

    /// <summary>
    /// The Size is the precision. It in number of bits required to hold the number. 
    /// ExtraHiddenBits are subtracted out.
    /// </summary>
    public readonly int Size => Math.Max(0, _size - ExtraHiddenBits);

    /// <summary>
    /// The number of data bits. ExtraHiddenBits are counted.
    /// </summary>
    public readonly int SizeWithHiddenBits => _size;

    //TODO: Exponent should have a "-1" added as 1.0 has an exponent of zero.  (this is also the same a log2 so add that in the comment)
    /// <summary>
    /// The resulting binary point position when counting from the most significant bit. 
    /// Or where the [.]dataBits x 2^exp. Example: 0.11010 x 2^3 = 110.10 [Scale + Size]
    /// Examples: 0.11 -> 0; 1.11 -> 1; 10.1 -> 2; .001 = -2
    /// </summary>
    public int Exponent => Scale + _size - ExtraHiddenBits;

    //see BigFloatZeroNotes.txt for notes
    //perf: should we keep the shortcut "...&& Scale < 0 &&..."?
    /// <summary>
    /// Returns true if the internal data bits round to zero. 
    /// </summary>
    public bool IsZero => _size < (ExtraHiddenBits - 2) && (_size + Scale) < ExtraHiddenBits; // && Scale < 0

    // What is considered Zero: any dataInt that is LESS then 0:100000000, and also the shift results in a 0:100000000.
    // 
    //   IntData    Scale Size Sz+Sc Precision  Zero
    // 1:111111111 << -2   33    31      1       N
    // 1:000000000 << -2   33    31      1       N
    // 1:000000000 << -1   33    32      1       N
    // 1:000000000 <<  0   33    33      1       N
    // 0:111111111 << -1   32    31      0       N
    // 0:100000000 << -1   32    31      0       N
    // 0:100000000 <<  0   32    32      0       N
    // 0:011111111 << -1   31    30     -1       Y
    // 0:011111111 <<  0   31    31     -1       Y (borderline)
    // 0:011111111 <<  1   31    32     -1       N
    // 0:001111111 <<  1   31    32     -2       Y (borderline)
    // 0:001111111 <<  2   31    33     -2       N

    /// <summary>
    /// Returns true if there is less than 1 bit of precision. However, a false value does not guarantee that the number are precise. 
    /// </summary>
    public bool OutOfPrecision => _size < ExtraHiddenBits;

    /// <summary>
    /// Returns the precision of the BigFloat. This is the same as the size of the data bits. The precision can be zero or negative. A negative precision means the number is below the number of bits(HiddenBits) that are deemed precise. 
    /// </summary>
    public int GetPrecision => _size - ExtraHiddenBits;

    /// <summary>
    /// Returns the accuracy of the BigFloat. The accuracy is equivalent to the opposite of the scale. A negative accuracy means the least significant bit is above the one place. A value of zero is equivalent to an integer. A positive value is the number of accurate decimal places(in binary) the number has.
    /// </summary>
    public int GetAccuracy => -Scale;

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
    public readonly BigInteger Int => DataIntValueWithRound(DataBits);

    public string DebuggerDisplay
    {
        get
        {
            string bottom8HexChars = (BigInteger.Abs(DataBits) & ((BigInteger.One << ExtraHiddenBits) - 1)).ToString("X8").PadLeft(8)[^8..];
            StringBuilder sb = new(32);
            _ = sb.Append($"{ToString(true)}, "); //  integer part using ToString()
            _ = sb.Append($"{(DataBits.Sign >= 0 ? " " : "-")}0x{BigInteger.Abs(DataBits) >> ExtraHiddenBits:X}:{bottom8HexChars}"); // hex part
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
        //Console.WriteLine($"  Int|hex: {_int >> ExtraHiddenBits:X}:{(_int & (uint.MaxValue)).ToString("X")[^8..]}[{Size}] {shift} (Hidden-bits round {(WouldRound() ? "up" : "down")})");
        Console.WriteLine($" Int|Hex : {ToStringHexScientific(true, true, false)} (Hidden-bits round {(WouldRound() ? "up" : "down")})");
        Console.WriteLine($"    |Hex : {ToStringHexScientific(true, true, true)} (two's comp)");
        Console.WriteLine($"    |Dec : {DataBits >> ExtraHiddenBits}{((double)(DataBits & (((ulong)1 << ExtraHiddenBits) - 1)) / ((ulong)1 << ExtraHiddenBits)).ToString()[1..]} {shift}");
        Console.WriteLine($"    |Dec : {DataBits >> ExtraHiddenBits}:{DataBits & (((ulong)1 << ExtraHiddenBits) - 1)} {shift}");  // decimal part (e.g. .75)
        if (DataBits < 0)
        {
            Console.WriteLine($"   or -{-DataBits >> ExtraHiddenBits:X4}:{(-DataBits & (((ulong)1 << ExtraHiddenBits) - 1)).ToString("X8")[^8..]}");
        }

        Console.WriteLine($"    |_int: {DataBits}");
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
    /// Contracts a BigFloat using the raw elemental parts. The user is responsible to pre-up-shift rawValue and set <param name="scale"> and <param name="rawValueSize">.
    /// </summary>
    /// <param name="rawValue">The raw integerPart. It should INCLUDE the ExtraHiddenBits.</param>
    /// <param name="rawValueSize">The size of rawValue. </param>
    private BigFloat(BigInteger rawValue, int scale, int rawValueSize)
    {
        DataBits = rawValue;
        Scale = scale;
        _size = rawValueSize;

        AssertValid();
    }

    /// <summary>
    /// Constructs a BigFloat using its elemental parts.
    /// </summary>
    /// <param name="integerPart">The integer part of the BigFloat that will have a <param name="scale"> applied to it. </param>
    /// <param name="scale">How much should the <param name="integerPart"> be shifted or scaled? This shift (base-2 exponent) will be applied to the <param name="integerPart">.</param>
    /// <param name="valueIncludesHiddenBits">if true, then the hidden bits should be included in the integer part.</param>
    public BigFloat(BigInteger integerPart, int scale = 0, bool valueIncludesHiddenBits = false)
    {
        int applyHiddenBits = valueIncludesHiddenBits ? 0 : ExtraHiddenBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        DataBits = integerPart << applyHiddenBits;
        _size = (int)BigInteger.Abs(DataBits).GetBitLength();
        Scale = scale; // _int of zero can have scale

        AssertValid();
    }

    public BigFloat(char integerPart, int scale = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;

        // Special handing required for int.MinValue
        _size = integerPart >= 0
            ? integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart != char.MinValue
                ? integerPart == 0 ? 0 : BitOperations.Log2((byte)-integerPart) + 1 + ExtraHiddenBits
                : 7 + ExtraHiddenBits;

        AssertValid();
    }

    public BigFloat(byte integerPart, int scale = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;
        _size = integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(int integerPart, int scale = 0) : this((long)integerPart, scale) { }

    public BigFloat(uint value, int scale = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(long value, int scale = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value switch
        {
            > 0 => BitOperations.Log2((ulong)value) + 1 + ExtraHiddenBits,
            < 0 => 64 - BitOperations.LeadingZeroCount(~((ulong)value - 1)) + ExtraHiddenBits,
            _ => 0,
        };
        AssertValid();
    }

    public BigFloat(ulong value, int scale = 0)
    {
        DataBits = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(Int128 integerPart, int scale = 0)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();
    }

    public BigFloat(Int128 integerPart, int scale, bool valueIncludesHiddenBits)
    {
        DataBits = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();

        int applyHiddenBits = valueIncludesHiddenBits ? 0 : ExtraHiddenBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        _size = (int)((BigInteger)(integerPart >= 0 ? integerPart : -integerPart)).GetBitLength() + applyHiddenBits;
        DataBits = integerPart << applyHiddenBits;
        Scale = scale; // _int of zero can have scale
        AssertValid();
    }

    public BigFloat(double value, int additionalScale = 0)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        long mantissa = bits & 0xfffffffffffffL;
        int exp = (int)((bits >> 52) & 0x7ffL);

        if (exp == 2047)  // 2047 represents inf or NAN
        {
            //if (double.IsNaN(value))
            //{
            //    _int = 0;
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
            Scale = exp - 1023 - 52 + additionalScale;
            _size = 53 + ExtraHiddenBits; //_size = BitOperations.Log2((ulong)Int);
        }
        else // exp is 0 so this is a denormalized float (leading "1" is "0" instead)
        {
            // 0:00000000000:00...0001 -> smallest value (Epsilon)  Int:1, Scale: Size:1
            // ...

            if (mantissa == 0)
            {
                DataBits = 0;
                Scale = additionalScale;
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
                Scale = -1023 - 52 + 1 + additionalScale;
                _size = size + ExtraHiddenBits;
            }
        }

        AssertValid();
    }

    public BigFloat(float value, int additionalScale = 0)
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
              //    _int = 0;
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
            Scale = exp - 127 - 23 + additionalScale;
            _size = 24 + ExtraHiddenBits;
        }
        else // exp is 0 so this is a denormalized(Subnormal) float (leading "1" is "0" instead)
        {
            if (mantissa == 0)
            {
                DataBits = 0;
                Scale = additionalScale;
                _size = 0; //24 + ExtraHiddenBits;
            }
            else
            {
                BigInteger mant = new(value >= 0 ? mantissa : -mantissa);
                DataBits = mant << ExtraHiddenBits;
                Scale = -126 - 23 + additionalScale; //hack: 23 is a guess
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

    /// <summary>
    /// Parses an input string and returns a BigFloat. If it fails, an exception is thrown.
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Radix point (aka. decimal point for base 10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    /// </summary>
    /// <param name="numericString">The input decimal/hex/binary number.</param>
    /// <param name="additionalScale">Optional apply positive or negative base-2 scaling.(default is zero)</param>
    public BigFloat(string value, int additionalScale = 0)
    {
        this = Parse(value, additionalScale);
    }
    ///////////////////////// [END] INIT / CONVERSION  FUNCTIONS [END] /////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////    TO_STRING  FUNCTIONS     ////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    // see "BigFloatToStringNotes.txt" and "BigFloatTryParseNotes.txt" for additional notes
    //   string ToString() - calls ToStringDecimal()
    //   string ToString(string format) - to Hex(e.g. A4B.F2) and Binary(e.g. 1010111.001)
    //   string ToStringDecimal() - To Decimal, e.g. 9999.99
    //   string ToStringHexScientific(bool showHiddenBits = false, bool showSize = false, bool showInTwosComplement = false) - e.g. "12AC<<22"


    [DebuggerHidden()]
    public override string ToString()
    {
        return ToStringDecimal(this, false);
    }

    [DebuggerHidden()]
    public string ToString(bool includeOutOfPrecisionBits = false)
    {
        return ToStringDecimal(this, includeOutOfPrecisionBits);
    }

    /// <summary>
    /// Format the value of the current instance to a decimal number.
    /// </summary>
    /// <param name="val">The BigFloat that should be converted to a string.</param>
    /// <param name="includeOutOfPrecisionBits">Include out-of-precision bits in result. This will include additional decimal places.</param>
    //[DebuggerHidden()]
    public static string ToStringDecimal(BigFloat val, bool includeOutOfPrecisionBits = false)
    {
        BigInteger intVal = val.DataBits;
        int scale = val.Scale;
        int valSize = val._size;

        if (includeOutOfPrecisionBits)
        {
            intVal <<= ExtraHiddenBits;
            scale -= ExtraHiddenBits;
            valSize += ExtraHiddenBits;
        }

        if (scale < -1)
        {
            // Number will have a decimal point. (e.g. 222.22, 0.01, 3.1)
            // -1 is not enough to form a full decimal digit.

            // Get the number of places that should be returned after the decimal point.
            int decimalDigits = -(int)((scale - 1.5) / 3.32192809488736235);

            BigInteger power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);

            // Applies the scale to the number and rounds from bottom bit
            BigInteger power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + ExtraHiddenBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {
                if (RightShiftWithRound(intVal, ExtraHiddenBits).IsZero)
                {
                    return $"0.{new string('0', decimalDigits)}";
                }

                // future: The below should not be needed.
                //// solves an issue when a "BigFloat(1, -8)" being 0.000
                decimalDigits++;
                power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);
                power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + ExtraHiddenBits);
            }

            string numberText = power5Scaled.ToString();

            int decimalOffset = numberText.Length - decimalDigits;
            //int decimalOffset2 = ((int)((_size - ExtraHiddenBits + scale2) / 3.32192809488736235)) - ((numberText[0] - '5') / 8.0);  //alternative

            if (decimalOffset < -10)  // 0.0000000000xxxxx 
            {
                return $"{(intVal.Sign < 0 ? "-" : "")}{numberText}e-{decimalDigits}";
            }

            int exponent = scale + valSize - ExtraHiddenBits;

            // The length should have room for: [-][digits][.][digits]
            int length = (intVal < 0 ? 3 : 2) + numberText.Length - (exponent <= 0 ? decimalOffset : 1);
            char[] chars = new char[length];
            int position = 0;

            if (intVal < 0)
            {
                chars[position++] = '-';
            }

            // 0.#### or 0.000##### - lets check for these formats
            // We can round a 0.99 to a 1.00, hence the "(Exponent==0 && decimalOffset <= 0)"
            if (exponent < 0 || (exponent == 0 && decimalOffset <= 0))
            {
                chars[position++] = '0';
                chars[position++] = '.';

                for (int i = decimalOffset; i < 0; i++)
                {
                    chars[position++] = '0';
                }

                numberText.CopyTo(0, chars, position, numberText.Length);

                return new string(chars);
            }

            // ####.##### - at this point it must be this format
            numberText.CopyTo(0, chars, position, decimalOffset);
            position += decimalOffset;

            chars[position++] = '.';

            numberText.CopyTo(decimalOffset, chars, position, decimalDigits);

            return new string(chars);
        }

        // Check to see if we have an integer, if so no Pow(5) scaling required
        if (scale == 0)
        {
            return DataIntValueWithRound(intVal).ToString();
        }

        // At this point we the number have a positive exponent. e.g 7XXXXX or 7e+10 (no decimal point)

        int maskSize = (int)((scale + 2.5) / 3.32192809488736235); // 2.5 is adjustable 
        BigInteger resUnScaled = (intVal << (scale - maskSize)) / BigInteger.Pow(5, maskSize);

        // Applies the scale to the number and rounds from bottom bit
        BigInteger resScaled = RightShiftWithRound(resUnScaled, ExtraHiddenBits);

        // Let put together the string. 
        StringBuilder result = new();
        result.Append(resScaled);
        if (maskSize > 10)
        {
            result.Append("e+");
            result.Append(maskSize);
        }
        else
        {
            result.Append(new string('X', maskSize));
        }

        return result.ToString();
    }

    /// <summary>
    /// Writes a BigFloat in Hex('X') or Binary('B'). A radix point is supported. Negative values must have a leading '-'. 
    /// </summary>
    /// <param name="format">Format specifier: 'X' for hex, 'B' for binary, or empty for decimal.</param>
    /// <returns>The value as a string.</returns>
    public string ToString(string format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return ToString();
        }

        //// Lets round and remove the ExtraHiddenBits now.
        //BigInteger newInt = DataIntValueWithRound(BigInteger.Abs(_int), out bool needToRound);
        //int size = (int)newInt.GetBitLength();
        //int newScale = Scale;

        if (format[0] == 'X') //hex with radix point
        {
            if (Scale >= 0)
            {
                //return (newInt >> Scale).ToString("X");
                return (DataBits >> (ExtraHiddenBits - Scale)).ToString("X"); // This version includes hidden bits in result
            }

            // We have to align the INT to the nearest 4 bits for hex. We also want to remove the ExtraHiddenBits.
            // The number of bits between the radix point and the end should be divisible by 4. We will dig into the ExtraHiddenBits for this.
            int rightShift = (ExtraHiddenBits - Scale) & 0x03;

            BigInteger shiftedBigIntForDisplay = RightShiftWithRound(DataBits, rightShift);

            return shiftedBigIntForDisplay.ToString("X").Insert((-Scale / 4) - 1, ".");
        }

        if (format[0] == 'B') // Signals a binary (with radix point)
        {
            // Setup destination and allocate memory
            Span<char> dstBytes = stackalloc char[_size - ExtraHiddenBits
                + Math.Max(Math.Max(Scale, -(_size - ExtraHiddenBits) - Scale), 0) // total number of out-of-precision zeros in the output.
                + (DataBits.Sign < 0 ? 1 : 0)   // add one if a leading '-' sign (-0.1)
                + (Scale < 0 ? 1 : 0)       // add one if it has a point like (1.1)
                + (Exponent <= 0 ? 1 : 0)];  // add one if <1 for leading Zero (0.1) 
            int dstIndex = 0;

            // Three types
            //   Type '12300' - if all bits are to the left of the radix point(no radix point required)
            //   Type '12.30' - has numbers below AND above the point. (e.g. 11.01)
            //   Type '0.123' - all numbers are to the right of the radix point. (has leading 0.or - 0.)

            // Pre-append the leading sign.
            if (DataBits.Sign < 0)
            {
                dstBytes[dstIndex] = '-';
                dstIndex++;
            }

            // Setup source bits to read.
            ReadOnlySpan<byte> srcBytes = DataIntValueWithRound(BigInteger.Abs(DataBits)).ToByteArray();
            int leadingZeroCount = BitOperations.LeadingZeroCount(srcBytes[^1]) - 24;

            if (Exponent <= 0)  // For binary numbers less then one. (e.g. 0.001101)
            {
                int outputZerosBetweenPointAndNumber = Math.Max(0, -(_size - ExtraHiddenBits) - Scale);
                dstBytes[dstIndex++] = '0';
                dstBytes[dstIndex++] = '.';

                // Add the leading zeros
                for (int i = 0; i < outputZerosBetweenPointAndNumber; i++)
                {
                    dstBytes[dstIndex++] = '0';
                }

                WriteValueBits(srcBytes, leadingZeroCount, Size, dstBytes[dstIndex..]);
            }
            else if (Scale >= 0)   // For binary numbers with no radix point. (e.g. 1101)
            {
                int outputZerosBetweenNumberAndPoint = Math.Max(0, Scale);
                dstBytes[^outputZerosBetweenNumberAndPoint..].Fill('0');
                WriteValueBits(srcBytes, leadingZeroCount, Size, dstBytes[dstIndex..]);
            }
            else // For numbers with a radix point in the middle (e.g. 101.1 or 10.01, or 1.00)
            {
                int outputBitsBeforePoint = _size - ExtraHiddenBits + Scale;
                int outputBitsAfterPoint = Math.Max(0, -Scale);

                WriteValueBits(srcBytes, leadingZeroCount, outputBitsBeforePoint, dstBytes[dstIndex..]);

                dstIndex += outputBitsBeforePoint;

                //Write Decimal point
                dstBytes[dstIndex++] = '.';
                WriteValueBits(srcBytes, leadingZeroCount + outputBitsBeforePoint, outputBitsAfterPoint, dstBytes[dstIndex..]);
            }

            return new string(dstBytes);
        }

        // If none of the above formats ('X' or 'B') matched, then fail.
        throw new FormatException($"The {format} format string is not supported.");

        static void WriteValueBits(ReadOnlySpan<byte> srcBytes, int bitStart, int bitCount, Span<char> dstBytes)
        {
            int srcLoc = srcBytes.Length - 1;
            int dstByte = 0;
            int cur = bitStart;

            while (cur < bitStart + bitCount)
            {
                int curSrcByte = srcLoc - (cur >> 3);
                int curSrcBit = 7 - (cur & 0x7);

                byte b2 = srcBytes[curSrcByte];

                dstBytes[dstByte++] = (char)('0' + ((b2 >> curSrcBit) & 1));
                cur++;
            }
        }
    }

    /// <summary>
    /// Generates the data-bits in hex followed by the amount to shift(in decimal). Example: 12AC<<22 or B1>>3
    /// </summary>
    /// <param name="showHiddenBits">Includes the extra 32 hidden bits. Example: 12AC:F0F00000<<22</param>
    /// <param name="showSize">Appends a [##] to the number with it's size in bits. Example: 22AC[14]<<22</param>
    /// <param name="showInTwosComplement">When enabled, shows the show result in two's complement form with no leading sign. Example: -5 --> B[3]<<0</param>
    public string ToStringHexScientific(bool showHiddenBits = false, bool showSize = false, bool showInTwosComplement = false)
    {
        StringBuilder sb = new();

        BigInteger intVal = DataBits;
        if (!showInTwosComplement && DataBits.Sign < 0)
        {
            _ = sb.Append('-');
            intVal = -intVal;
        }
        _ = sb.Append($"{intVal >> ExtraHiddenBits:X}");
        if (showHiddenBits)
        {
            _ = sb.Append($":{(intVal & (uint.MaxValue)).ToString("X8")[^8..]}");
        }

        if (showSize)
        {
            _ = sb.Append($"[{Size}]");
        }

        _ = sb.Append($" {((Scale >= 0) ? "<<" : ">>")} {Math.Abs(Scale)}");
        return sb.ToString();
    }

    /// <summary>
    /// This function returns a specified number of most-significant bits (MSBs) as a char[] array.  If the requested number of bits is larger than the data bits, it will be left-shifted and padded with underscores.
    /// </summary>
    public string GetMostSignificantBits(int numberOfBits)
    {
        BigInteger abs = BigInteger.Abs(DataBits);
        int shiftAmount = _size - numberOfBits;
        return shiftAmount >= 0
            ? BigIntegerToBinaryString(abs >> shiftAmount)
            : BigIntegerToBinaryString(abs) + new string('_', -shiftAmount);
    }

    /// <summary>
    /// Returns the value's bits, including hidden bits, as a string. 
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetAllBitsAsString(bool twosComplement = false)
    {
        return BigIntegerToBinaryString(DataBits, twosComplement);
    }

    /// <summary>
    /// Returns the value's bits as a string. 
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetBitsAsString()
    {
        return BigIntegerToBinaryString(Int);
    }


    /////////////////////////// [END] TO_STRING FUNCTIONS [END] ////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////// PARSE  FUNCTIONS  FUNCTIONS ////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////

    // see "BigFloatTryParseNotes.txt" for additional notes

    /// <summary>
    /// Parses an input string and returns a BigFloat. If it fails, an exception is thrown.
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Radix point (aka. decimal point for base 10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    /// </summary>
    /// <param name="numericString">The input decimal/hex/binary number.</param>
    /// <param name="scale">Optional apply positive or negative base-2 scaling.(default is zero)</param>
    public static BigFloat Parse(string numericString, int scale = 0)
    {
        bool success = TryParse(numericString, out BigFloat biRes, scale);

        if (!success)
        {
            throw new ArgumentException("Unable to convert string to BigFloat.");
        }

        biRes.AssertValid();

        return biRes;
    }

    /// <summary>
    /// Parses a <param name="numericString"> to a BigFloat. 
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Radix point (aka. decimal point for base 10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    /// </summary>
    /// <param name="numericString">The input decimal/hex/binary number.</param>
    /// <param name="result">The resulting BigFloat. Zero is returned if conversion failed.</param>
    /// <param name="scale">Optional apply positive or negative base-2 scaling.(default is zero)</param>
    /// <returns>Returns true if successful.</returns>
    public static bool TryParse(string numericString, out BigFloat result, int scale = 0)
    {
        //string orgValue = numericString;
        if (string.IsNullOrEmpty(numericString))
        {
            result = new BigFloat(0);
            return false;
        }

        // Let us check for invalid short strings, 0x___ , or 0b___
        {
            int locAfterSign = (numericString[0] is '-' or '+') ? 1 : 0;
            if (numericString.Length == locAfterSign)    //[-,+][END] - fail  
            {
                result = new BigFloat(0);
                return false;
            }
            else if (numericString[locAfterSign] == '0')  //[-,+]0___
            {
                bool isNeg = numericString[0] == '-';
                if (numericString.Length > 2 && numericString[locAfterSign + 1] is 'b' or 'B')  //[-,+]0b___
                {
                    // remove leading "0x" or "-0x"

                    return TryParseBinary(numericString.AsSpan(isNeg ? 3 : 2), out result, scale, isNeg ? -1 : 0);
                }
                else if (numericString.Length > 2 && numericString[locAfterSign + 1] is 'x' or 'X')  //[-,+]0x___
                {
                    return TryParseHex(numericString, out result, scale);
                }
                //else { } // [-,+]0[END] OR [-,+]0___  - continue(exceptions handled by BigInteger.Parse)
            }
        }
        //else if (numericString[1] > '0' && numericString[1] <= '9') { } // [-,+][1-9]__ - continue(exceptions handled by BigInteger.Parse)
        //else if (numericString[1] == '.') { }                      // [-,+].___    - continue(exceptions handled by BigInteger.Parse)

        int radixLoc = numericString.IndexOf('.');

        // There is a decimal point, so let's remove it to convert it to a BigInteger.
        if (radixLoc >= 0)
        {
            numericString = numericString.Remove(radixLoc, 1);
        }

        // Check for 'e'  like 123e10 or 123.123e+100
        int eLoc = numericString.IndexOf('e');

        int exp = 0;
        if (eLoc > 0)
        {
            int endOfNub = eLoc;
            int begOfExp = eLoc + 1;
            int expSign = 1;
            char sign = numericString[eLoc + 1];

            if (sign == '+')
            {
                begOfExp++;
            }
            if (sign == '-')
            {
                begOfExp++;
                expSign = -1;
            }

            string expString = numericString[begOfExp..];
            exp = int.Parse(expString) * expSign;
            numericString = numericString[0..endOfNub];
        }

        // now that we removed the "." and/or "e", let us make sure the length is not zero
        if (numericString.Length == 0)
        {
            result = new BigFloat(0);
            return false;
        }

        if (!BigInteger.TryParse(numericString.AsSpan(), out BigInteger asInt))
        {
            result = new BigFloat(0);
            return false;
        }

        // There is no decimal point, so let's use BigInteger to convert.
        if (radixLoc < 0)
        {
            radixLoc = numericString.Length;
        }

        if (asInt.IsZero)
        {
            int scaleAmt = (int)((radixLoc - numericString.Length + exp) * 3.32192809488736235);
            result = new BigFloat(BigInteger.Zero, scaleAmt, 0);
            return true;
        }

        // If the user specifies a one (e.g., 1XXX OR 1 OR 0.01), the intended precision is closer to 2 bits.
        if (BigInteger.Abs(asInt).IsOne)
        {
            asInt <<= 1;
            scale -= 1;
        }

        // Set ROUND to 1 to enable round to nearest.
        // When 1, an extra LSBit is kept and if it's set it will round up. (e.g. 0.1011 => 0.110)
        const int ROUND = 1;
        BigInteger intPart;

        int radixDepth = numericString.Length - radixLoc - exp;
        if (radixDepth == 0)
        {
            result = new BigFloat(asInt, scale);
        }
        else if (radixDepth >= 0) //111.111 OR 0.000111
        {
            BigInteger a = BigInteger.Pow(5, radixDepth);
            int multBitLength = (int)a.GetBitLength();
            multBitLength += (int)(a >> (multBitLength - 2)) & 0x1;      // Round up if closer to larger size 
            int shiftAmt = multBitLength + ExtraHiddenBits - 1 + ROUND;  // added  "-1" because it was adding one to many digits 
                                                                         // make asInt larger by the size of "a" before we dividing by "a"
            intPart = (((asInt << shiftAmt) / a) + ROUND) >> ROUND;
            scale += -multBitLength + 1 - radixDepth;
            result = new BigFloat(intPart, scale, true);
        }
        else // 100010XX
        {
            BigInteger a = BigInteger.Pow(5, -radixDepth);
            int multBitLength = (int)a.GetBitLength();
            int shiftAmt = multBitLength - ExtraHiddenBits - ROUND;
            // Since we are making asInt larger by multiplying it by "a", we now need to shrink it by size "a".
            intPart = (((asInt * a) >> shiftAmt) + ROUND) >> ROUND;
            scale += multBitLength - radixDepth;
            result = new BigFloat(intPart, scale, true);
        }

        //Console.WriteLine(
        //    $"Cur: {orgValue} -> {asInt,5}/{a,7}[{shiftAmt,3}] " +
        //    $"->{asInt,3}({BigIntegerToBinaryString(asInt),10})[{BigIntegerToBinaryString(asInt).Length}] " +
        //    $"->{BigInteger.Abs(intPart),3}({BigIntegerToBinaryString(BigInteger.Abs(intPart)),10})[{BigIntegerToBinaryString(BigInteger.Abs(intPart)).Length}] " +
        //    $"-> AsBF: {result,11} " +
        //    $"AsDbl: {double.Parse(orgValue),8}({DecimalToBinary(double.Parse(orgValue), 40)})");

        result.AssertValid();
        return true;
    }

    // Allowed: 
    //  * ABC.DEF
    //  * abc.abc      both uppercase/lowercases okay
    //  * -ABC.DEF     leading minus okay
    //  * 123 456 789  spaces or commas okay
    //  * {ABC.DEF}    wrapped in {..} or (..) or ".."
    //  * ABC_____     trailing spaces okay
    // Not Allowed:
    //  * 0xABC.DEF    leading 0x - use Parse for this)
    //  * {ABC.DEF     must have leading and closing bracket
    //  * {ABC.DEF)    brackets types must match
    //  * {{ABC.DEF}}  limit of one bracket only
    //  * 123,456 789  mixing different kinds of separators)

    /// <summary>
    /// Parses a hex string to a BigFloat. It supports a radix point(like a decimal point in base 10) and
    /// negative numbers. It will also ignore spaces and tolerate values wrapped with double quotes and brackets.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="result">(out) The returned result.</param>
    /// <param name="additionalScale">(optional) Any additional power-of-two scale amount to include. Negative values are okay.</param>
    /// <returns>Returns true if successful.</returns>
    public static bool TryParseHex(ReadOnlySpan<char> input, out BigFloat result, int additionalScale = 0)
    {
        if (input.IsEmpty)
        {
            result = 0;
            return false;
        }

        bool usingComma = false;
        bool usingSpace = false;
        int radixLocation = 0;
        int BraceTypeAndStatus = 0;  // 0=not used, 1=usingCurlBraces, 3=usingRoundBrackets, 4=usingParentheses,  [neg means it has been closed]

        // Go through and remove invalid chars
        int destLoc = 1;

        // skip negative or positive sign
        bool isNeg = input[0] == '-';
        int inputCurser = (isNeg || input[0] == '+') ? 1 : 0;

        Span<char> cleaned = stackalloc char[input.Length - inputCurser + 1];

        cleaned[0] = '0'; // Ensure we have a positive number

        for (; inputCurser < input.Length; inputCurser++)
        {
            char c = input[inputCurser];
            switch (c)
            {
                case (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'):
                    cleaned[destLoc++] = c;
                    break;
                case '.':
                    if (radixLocation != 0)
                    {
                        // radix point already found earlier
                        result = 0;
                        return false;
                    }
                    radixLocation = destLoc;
                    break;
                case ' ':
                    if (usingComma)
                    {
                        // already using Commas
                        result = 0;
                        return false;
                    }
                    usingSpace = true;
                    break;
                case ',':
                    if (usingSpace)
                    {
                        // already using Spaces
                        result = 0;
                        return false;
                    }
                    usingComma = true;
                    break;

                case '{':
                    if (BraceTypeAndStatus != 0)
                    {
                        // already using Spaces
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = 1;
                    break;

                case '}':
                    if (BraceTypeAndStatus != 1)
                    {
                        // fail: no had leading '{' or another type used
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = -1;
                    break;
                case '(':
                    if (BraceTypeAndStatus != 0)
                    {
                        // already using Spaces
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = 3;
                    break;

                case ')':
                    if (BraceTypeAndStatus != 3)
                    {
                        // fail: no had leading '(' or another type used
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = -3;
                    break;
                case '"':
                    if (BraceTypeAndStatus is not 0 or 4)
                    {
                        // already using Spaces
                        result = 0;
                        return false;
                    }
                    if (BraceTypeAndStatus == 4)
                    {
                        BraceTypeAndStatus = -4;
                    }
                    break;
                default:
                    // fail: unexpected char found
                    result = 0;
                    return false;
            }

            // if we hit a closing brace/bracket/param then only whitespace remaining
            if (BraceTypeAndStatus < 0)
            {
                // should just be whitespace left after closing brace
                for (; inputCurser < input.Length; inputCurser++)
                {
                    if (!char.IsWhiteSpace(input[inputCurser]))
                    {
                        // only whitespace expected after closing brace
                        result = 0;
                        return false;
                    }
                }
                break;
            }
        }

        // check if no digits were found
        if (destLoc <= 1)
        {
            result = 0;
            return false;
        }

        // radixLocation is the distance from the MSB, it should be from the LSB. (or leave at 0 if radix point not found)
        if (radixLocation > 0)
        {
            radixLocation -= destLoc;
        }

        // hex are just bits of 4 so the scale is easy
        int newScale = (radixLocation * 4) + additionalScale;

        if (!BigInteger.TryParse(cleaned, NumberStyles.AllowHexSpecifier, null, out BigInteger asInt))
        {
            result = new BigFloat(0);
            return false;
        }

        if (isNeg)
        {
            asInt = BigInteger.Negate(asInt);
        }

        result = new BigFloat(asInt, newScale);
        return true;
    }

    /// <summary>
    /// Converts the binary number in a string to a BigFloat. 
    /// If it fails, an exception is thrown.
    /// e.g, '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="scale">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <param name="includesHiddenBits">(optional)The number of bits that should be included in the sub-precision hidden-bits.</param>
    /// <returns>A BigFloat result of the input binary string.</returns>
    public static BigFloat ParseBinary(string input, int scale = 0, int forceSign = 0, int includesHiddenBits = -1)
    {
        ArgumentException.ThrowIfNullOrEmpty(input); // .Net 7 or later
        //ArgumentNullException.ThrowIfNullOrWhiteSpace(input); // .Net 8 or later

        return !TryParseBinary(input.AsSpan(), out BigFloat result, scale, forceSign, includesHiddenBits)
            ? throw new ArgumentException("Unable to convert the binary string to a BigFloat.", input)
            : result;
    }

    /// <summary>
    /// Converts the binary text in ReadOnlySpan<char> to a BigFloat. 
    /// e.g. '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="result">(out) The BigFloat result.</param>
    /// <param name="scale">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <param name="includesHiddenBits">(optional)The number of bits that should be included in the sub-precision hidden-bits.</param>
    /// <returns>Returns false if it fails or is given an empty or null string.</returns>
    public static bool TryParseBinary(ReadOnlySpan<char> input, out BigFloat result, int scale = 0, int forceSign = 0, int includesHiddenBits = -1)
    {
        int inputLen = input.Length;

        if (inputLen == 0)
        {
            result = new BigFloat(0);
            return false;
        }

        byte[] bytes = new byte[(inputLen + 7) / 8];
        bool radixPointFound = false;
        int outputBitPosition = 0;      // The current bit we are writing to.

        // if it starts with a '-' then set negative rawValue to zero
        bool isNeg = input[0] == '-'; // 0x2D;

        // if starting with at - or + then headPosition should be 1.
        int headPosition = isNeg | input[0] == '+' ? 1 : 0;

        if (forceSign != 0)
        {
            isNeg = forceSign < 0;
        }

        int orgScale = scale;
        //                                01234567 89012345
        // Given the Input String:        00000001 00000010 00000011  
        // Output Byte Array should be:      [2]1    [1]2     [0]3  
        //                                
        // Now we are going to work our way from the end of the string forward.
        // We work backward to ensure the byte array is correctly aligned.
        int hiddenBitsFound = -1;
        int tailPosition = inputLen - 1;
        for (; tailPosition >= headPosition; tailPosition--)
        {
            switch (input[tailPosition])
            {
                case '1':
                    bytes[outputBitPosition >> 3] |= (byte)(1 << (outputBitPosition & 0x7));
                    goto case '0';
                case '0':
                    outputBitPosition++;
                    if (!radixPointFound)
                    {
                        scale--;
                    }
                    break;
                case '.':
                    // Let's make sure the decimal was not already found.
                    if (radixPointFound)
                    {
                        result = new BigFloat(0);
                        return false; // Function was not successful - duplicate '.'
                    }
                    radixPointFound = true;
                    break;
                case ',' or '_' or ' ': // allow commas, underscores, and spaces (e.g.  1111_1111_0000) (optional - remove for better performance)
                    break;
                case ':' or '|':
                    if (hiddenBitsFound >= 0)
                    {
                        // multiple precision spacers found (| or :)
                        result = new BigFloat(0);
                        return false;
                    }
                    hiddenBitsFound = outputBitPosition;
                    break;
                default:
                    result = new BigFloat(0);
                    return false; // Function was not successful - unsupported char found
            }
        }

        if (outputBitPosition == 0)
        {
            result = new BigFloat(0);
            return false; // Function was not successful - duplicate '.'
        }

        // if the user specified a precision spacer (| or :) 
        if (hiddenBitsFound >= 0)
        {
            // includedHiddenBits is specified?  if so, they must match!
            if (includesHiddenBits >= 0)
            {
                // make sure they match and fail if they do not.
                if (hiddenBitsFound != includesHiddenBits)
                {
                    result = new BigFloat(0);
                    return false;
                }
            }
            else // includedHiddenBits NOT specified 
            {
                includesHiddenBits = hiddenBitsFound;
            }
        }
        //else if (includedHiddenBits >= 0) { } // if no precision spacer (| or :) AND but includedHiddenBits was specified
        //else { } //nether specified.

        // Lets add the missing zero hidden bits
        if (includesHiddenBits >= 0)
        {
            int zerosNeededStill = ExtraHiddenBits - includesHiddenBits;
            //outputBitPosition += zerosNeededStill;
            if (!radixPointFound)
            {
                scale -= zerosNeededStill;
            }
        }
        else
        {
            includesHiddenBits = 0;
        }

        // If the number is negative, let's perform Two's complement: (1) negate the bits (2) add 1 to the bottom byte
        //111111110111111111111111111111111111111111111111111111110001010110100001
        //        1000000000000000000000000000000000000000000000001110101001011111
        if (isNeg)
        {
            int byteCount = bytes.Length;

            //   (1) negate the bits
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] ^= 0xff;
            }

            //   (2) increment the LSB and increment more significant bytes as needed.
            bytes[0]++;
            for (int i = 0; bytes[i] == 0; i++)
            {
                if (i + 1 >= byteCount)
                {
                    break;
                }

                bytes[i + 1]++;
            }
        }

        BigInteger bi = new(bytes, !isNeg);

        result = new BigFloat(bi << (ExtraHiddenBits - includesHiddenBits), radixPointFound ? scale + includesHiddenBits : orgScale, true);

        result.AssertValid();

        return true; // return true if success
    }


    ///////////////////////////////////////////////////////////////////////////////
    ///////////////////////// [END] Parse FUNCTIONS [END] /////////////////////////
    ///////////////////////////////////////////////////////////////////////////////


    ///////////////////////////////////////////////////////////////////////////////
    /////////////////////////    CompareTo  FUNCTIONS     /////////////////////////
    ///////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Compares two values and returns...
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other (Least significant bits are removed on more accurate number.)
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11==1.1,  1.00==1.0,  1.11!=1.10)
    ///   Returns Positive => this instance is greater than other OR <paramref name="obj"/> is null.
    /// </summary>
    public int CompareTo(object obj) // for IComparable
    {
        return obj switch
        {
            null => 1,   // If other is not a valid object reference, this instance is greater.
            BigFloat => CompareTo((BigFloat)obj),
            BigInteger => CompareTo((BigInteger)obj),
            _ => throw new ArgumentException("Object is not a BigFloat")
        };
    }

    /// <summary>
    ///  Compares the in-precision bits between two values. Only the most significant bit in the HiddenBits is considered.
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other (Accuracy of higher number reduced 
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11==1.1,  1.00==1.0,  1.11!=1.10)
    ///   Returns Positive => this instance is greater than other
    /// </summary>
    public int CompareTo(BigFloat other)
    {
        if (CheckForQuickCompareWithExponentOrSign(other, out int result)) { return result; }

        // At this point, the exponent is equal or off by one because of a rollover.

        int sizeDiff = _size - other._size - Exponent + other.Exponent;

        BigInteger a = RightShiftWithRound(DataBits, (sizeDiff > 0 ? sizeDiff : 0) + ExtraHiddenBits);
        BigInteger b = RightShiftWithRound(other.DataBits, (sizeDiff < 0 ? -sizeDiff : 0) + ExtraHiddenBits);
        return a.CompareTo(b);
    }

    /// <summary>
    /// A more accurate version of CompareTo() however it is not compatible with IEquatable. Compares the two numbers by subtracting them and if they are less then 0|1000 (i.e. Zero) then they are considered equal.
    /// e.g. Using 10|01111111 AND 10|10000000, CompareTo() returns not equal, but CompareInPrecisionBitsTo() returns Equal
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other. (or the difference is less then 0|1000 )
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11 == 1.1,  1.00 == 1.0,  1.11 != 1.10
    ///   Returns Positive => this instance is greater than other
    /// </summary>
    public int CompareInPrecisionBitsTo(BigFloat other)
    {
        if (CheckForQuickCompareWithExponentOrSign(other, out int result)) { return result; }

        // At this point, the exponent is equal or off by one because of a rollover.

        int sizeDiff = _size - other._size - Exponent + other.Exponent;

        BigInteger diff = sizeDiff switch
        {
            //> 0 => -(other.DataBits - (DataBits >> (sizeDiff - expDifference))),    // slightly faster version
            > 0 => RightShiftWithRound(DataBits, sizeDiff) - other.DataBits, // slightly more precise version
            //< 0 => -((other.DataBits >> (expDifference - sizeDiff)) - DataBits),    // slightly faster version
            < 0 => DataBits - RightShiftWithRound(other.DataBits, -sizeDiff),// slightly more precise version
            0 => DataBits - other.DataBits
        };

        // a quick exit
        int bytes = diff.GetByteCount();
        if (bytes != 4)
        {
            return (bytes > 4) ? diff.Sign : 0;
        }

        // Since we are subtracting, we can run into an issue where a 0:100000 should be considered a match.  e.g. 11:000 == 10:100
        diff -= diff.Sign; // decrements towards 0

        // Future: need to benchmark A, B or C
        //int a = RightShiftWithRound(temp, ExtraHiddenBits).Sign;
        //int b = (BigInteger.Abs(temp) >> (ExtraHiddenBits - 1)).IsZero ? 0 : temp.Sign;
        int c = ((int)((diff.Sign >= 0) ? diff : -diff).GetBitLength() < ExtraHiddenBits) ? 0 : diff.Sign;

        return c;
    }

    private bool CheckForQuickCompareWithExponentOrSign(BigFloat other, out int result)
    {
        if (OutOfPrecision)
        {
            result = other.OutOfPrecision ? 0 : -other.DataBits.Sign;
            return true;
        }

        if (other.OutOfPrecision)
        {
            result = OutOfPrecision ? 0 : DataBits.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Sign.
        if (DataBits.Sign != other.DataBits.Sign)
        {
            result = DataBits.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Exponent.
        int expDifference = Exponent - other.Exponent;
        if (Math.Abs(expDifference) > 1)
        {
            result = Exponent.CompareTo(other.Exponent) * DataBits.Sign;
            return true;
        }

        // At this point, the sign is the same, and the exp are within 1 bit of each other.

        //There are three special cases when the Exponent is off by just 1 bit:
        // case 1:  The smaller of the two rounds up to match the size of the larger and, therefore, can be equal(11 | 111 == 100 | 000)
        // case 2:  The smaller of the two rounds up, but the larger one also rounds up, so they are again not equal(depends on #1 happening first)
        // case 3:  Both round-up and are, therefore, equal

        //If "this" is larger by one bit AND "this" is not in the format 10000000..., THEN "this" must be larger(or smaller if neg)
        if (expDifference == 1 && !IsOneBitFollowedByZeroBits)
        {
            result = DataBits.Sign;
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
    /// Compares two values(including the hidden precision bits) and returns: 
    ///   Returns -1 when this instance is less than <param name="other">
    ///   Returns  0 when this instance is equal to <param name="other">
    ///   Returns +1 when this instance is greater than <param name="other">
    /// An Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// This Function is faster then the CompareTo() as no rounding needs to take place.
    /// </summary>
    public int CompareToExact(BigFloat other)
    {
        int thisPos = DataBits.Sign;
        int otherPos = other.DataBits.Sign;

        // Let's first make sure the signs are the same, if not, the positive input is greater.
        if (thisPos != otherPos)
        {
            //  OTHER-> -1  0  1
            //       -1| X -1 -1
            //  THIS: 0| 1  X -1  <-Return
            //        1| 1  1  X
            return thisPos == 0 ? -otherPos : thisPos;
        }

        // At this point the signs are the same. 

        // if both are zero then they are equal
        if (thisPos == 0 /*&& otherPos == 0*/) { return 0; }

        //Note: CompareTo would be the opposite for negative numbers

        // A fast general size check. (aka. Exponent vs Exponent)
        if ((Scale + _size) != (other.Scale + other._size))
        {
            return (Scale + _size).CompareTo(other.Scale + other._size) * thisPos;
        }

        // If we made it here we know that both items have the same exponent 
        if (_size == other._size)
        {
            return DataBits.CompareTo(other.DataBits);
        }

        if (_size > other._size)
        {
            // We must grow the smaller - in this case THIS
            BigInteger adjustedVal = DataBits << (other._size - _size);
            return adjustedVal.CompareTo(other.DataBits) * thisPos;
        }

        // at this point _size < other._size - we must grow the smaller - in this case OTHER
        BigInteger adjustedOther = other.DataBits << (_size - other._size);
        return DataBits.CompareTo(adjustedOther) * thisPos;
    }

    /// <summary>  
    /// Compares two values ignoring the least number of significant bits specified. 
    /// e.g. CompareToIgnoringLeastSigBits(0b1001.1111, 0b1000.111101, 3) => (b1001.11, 0b1001.0)
    /// Valid ranges are from -ExtraHiddenBits and up.
    ///   Returns -1 when <param name="b"> is less than <param name="a">
    ///   Returns  0 when <param name="b"> is equal to <param name="a"> when ignoring the least significant bits.
    ///   Returns  1 when <param name="b"> is greater than <param name="a">
    /// </summary>
    public static int CompareToIgnoringLeastSigBits(BigFloat a, BigFloat b, int leastSignificantBitsToIgnore)
    {
        //if (leastSignificantBitsToIgnore == 0) return a.CompareTo(b);

        // future: if (leastSignificateBitToIgnore == -ExtraHiddenBits) return CompareToExact(other);

        // Future: need to benchmark, next line is optional, escapes early if size is small
        //if (other._size < leastSignificantBitsToIgnore) return 0;

        leastSignificantBitsToIgnore += ExtraHiddenBits;

        if (leastSignificantBitsToIgnore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leastSignificantBitsToIgnore),
                $"Param cannot be less then -ExtraHiddenBits({-ExtraHiddenBits}).");
        }

        int scaleDiff = b.Scale - a.Scale;

        BigInteger temp = scaleDiff switch
        {
            > 0 => (a.DataBits >> scaleDiff) - b.DataBits,  // 'a' has more accuracy
            < 0 => a.DataBits - (b.DataBits >> -scaleDiff), // 'b' has more accuracy
            _ => a.DataBits - b.DataBits
        };


        // since we are subtracting, we can run into an issue where a 0:100000 should be considered a match.  e.g. 11:000 == 10:100
        temp -= temp.Sign; //decrements towards 0

        // Future: need to benchmark A, B or C

        // Method A
        //if (temp.GetBitLength() < (leastSignificantBitsToIgnore-1))  return 0;
        //return (temp >> (leastSignificantBitsToIgnore - 1)).Sign;

        // Method B
        return (BigInteger.Abs(temp).GetBitLength() > (leastSignificantBitsToIgnore - 1)) ? temp.Sign : 0;

        // Method C
        //if (temp >= 0)
        //    return (temp >> (leastSignificantBitsToIgnore - 1)).Sign;
        //else // is neg
        //    return -((-temp) >> (leastSignificantBitsToIgnore - 1)).Sign;
    }

    /// <summary>
    /// Compares two values and returns...
    ///   Returns -1 when this instance is less than the other
    ///   Returns  0 when this instance is equal to the other 
    ///   Returns  1 when this instance is greater than the other
    /// The hidden bits are removed. 
    /// </summary>
    public int CompareTo(BigInteger bigInteger)
    {
        int thisSign = DataBits.Sign;
        int otherSign = bigInteger.Sign;

        // A fast sign check.
        if (thisSign != otherSign)
        {
            return thisSign == 0 ? -otherSign : thisSign;
        }

        // If both are zero then they are equal.
        if (thisSign == 0) { return 0; }

        // A fast general size check.
        int bigIntegerSize = (int)BigInteger.Abs(bigInteger).GetBitLength();

        if (Exponent != bigIntegerSize)
        {
            return Exponent.CompareTo(bigIntegerSize) * thisSign;
        }


        // Future: Benchmark A and B
        // Option A:
        // At this point both items have the same exponent and sign. 
        //int bigIntLargerBy = bigIntegerSize - _size;
        //return bigIntLargerBy switch
        //{
        //    0 => _int.CompareTo(bigInteger),
        //    < 0 => (_int << bigIntegerSize - _size).CompareTo(bigInteger),
        //    > 0 => _int.CompareTo(bigInteger << _size - bigIntegerSize)
        //};

        // Option B:
        return RightShiftWithRound(DataBits, -Scale + ExtraHiddenBits).CompareTo(bigInteger);
    }

    ///////////////////////////////////////////////////////////////////////////////
    ////////////////////// [END] CompareTo  FUNCTIONS [END] ///////////////////////
    ///////////////////////////////////////////////////////////////////////////////

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
                //return (ulong)((BigInteger.Abs(_int) >> ExtraHiddenBits) & ulong.MaxValue); //perf: benchmark

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
        int bitsToClear = ExtraHiddenBits - Scale; // number of bits to clear from _int

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
            //BigInteger intPart = ((_int >> bitsToClear) + 1) << ExtraHiddenBits;
            //return new BigFloat((_int >> bitsToClear) +  (IsInteger?0:1));
            return new BigFloat(DataBits >> bitsToClear);
        }
        else  // if (_int.Sign <= 0)
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
        int bitsToClear = ExtraHiddenBits - Scale; // number of bits to clear from _int

        // 'Scale' will be zero or positive. (since all fraction bits are stripped away)
        // 'Size'  will be the size of the new integer part.
        // Precision of the decimal bits are stripped away. 

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
        else  // if (_int.Sign <= 0)
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
        int expDiff = a.Exponent - b.Exponent;
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

    // Anonymous 1/22/2021 at 9:06 AM https://www.java67.com/2014/11/modulo-or-remainder-operator-in-java.html :
    //   Technically(n % m) is a remainder operator, but not a modulus operator. There's a difference.
    //   For nonnegative n and positive m, the remainder and modulus are the same thing. But for negative n, they are
    //   different. -2 mod 10 is defined to be 8 in standard mathematics, going back centuries. But the remainder
    //   of -2 over 10 is -2. So (-2 % 10) gives -2, which is the remainder.It doesn't give 8, which is the mod.
    //   
    //   If you ever need an actual mod operator, then (((n % m) + m) % m) gives the mod.In most cases where you have a
    //   negative n, you'll actually need to find the mod rather than the remainder. So it's unfortunate Java doesn't
    //   have a mod operator built in. It inherited this from C.
    //   
    //   I wish C had defined % to be remainder and %% to be mod.That would have allowed us to avoid having to use ugly
    //   constructions like(((n % m) + m) % m) when we need the mod.
    //   Some languages actually do have both.For example, LISP has both "mod" and "rem" as operators.So does Ada. But
    //   sadly, C and all its descendants have only rem, not mod.
    // 
    // Also nice video on negatives: https://www.youtube.com/watch?v=AbGVbgQre7I
    // More notes here on windows calculator: https://github.com/microsoft/calculator/issues/111

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

        // Alternative version - less accurate. 
        //return scaleDiff switch
        //{
        //    > 0 => new(dividend._int % (divisor._int >> scaleDiff), dividend.Scale, true),
        //    < 0 => new((dividend._int << scaleDiff) % divisor._int, divisor.Scale, true),
        //    _ => new(dividend._int % divisor._int, divisor.Scale, true),
        //};
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
    /*                                         | BI | RoundTo| Scales  |Can Round  | Shift     |
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
      BI  Int                                  | I | Rounds  | Scales  |  Yes  |ExtraHiddenBits| i.e. Int => DataIntValueWithRound(_int);
Other:                                         |   |         |         |       |               |
    P bool=WouldRound()                        | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(_int, ExtraHiddenBits);
    P bool=WouldRound(int bottomBitsRemoved)   | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(_int, bottomBitsRemoved);
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
    public static bool WouldRound(BigInteger bi)
    {
        return BigIntegerTools.WouldRound(bi, ExtraHiddenBits);
    }

    /////////////////////////////////////////////
    ////    DataIntValue() for BigInteger    ////
    /////////////////////////////////////////////

    /// <summary>
    /// Retrieves the internal data bits and removes ExtraHiddenBits and rounds.
    /// </summary>
    /// <param name="x">The _int part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x)
    {
        return RightShiftWithRound(x, ExtraHiddenBits);
    }

    /// <summary>
    /// Removes ExtraHiddenBits and rounds. It also requires the current size and will adjust it if it grows.
    /// </summary>
    /// <param name="x">The _int part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x, ref int size)
    {
        return RightShiftWithRound(x, ExtraHiddenBits, ref size);
    }

    /// <summary>
    /// Checks to see if the integerPart would round-up if the ExtraHiddenBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRound()
    {
        return BigIntegerTools.WouldRound(DataBits, ExtraHiddenBits);
    }

    /// <summary>
    /// Checks to see if this integerPart would round-up given bottomBitsRemoved. 
    /// e.g. 11010101 with bottomBitsRemoved=3 would be 11011
    /// </summary>
    /// <param name="bottomBitsRemoved">The number of newSizeInBits from the least significant bit where rounding would take place.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRound(int bottomBitsRemoved)
    {
        return BigIntegerTools.WouldRound(DataBits, bottomBitsRemoved);
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

    //todo: finish and test (or delete)
    ///// <summary>
    ///// Calculates a BigFloat to the power of 2 with a maximum output precision required.
    ///// This function can save on compute cycles by not calculating bits that are needed.
    ///// </summary>
    ///// <param name="val">The base.</param>
    ///// <param name="maxOutputPrecisionInBits">The maximum number of bits needed in the output. </param>
    ///// <returns>Returns a BigFloat that is val^exp where the precision is </returns>
    //public static BigFloat PowerOf2(BigFloat val, int maxOutputPrecisionInBits)
    //{
    //    /*  valSz    resSize       skipIf
    //     *   3         5-6           maxOutputPrecisionInBits >= valSz*2
    //     *   4         7-8
    //     *   5         9-10                                                          */

    //    int overSized = (val.Size * 2) - maxOutputPrecisionInBits;

    //    // We can just use PowerOf2 Function since output will never be larger then maxOutputPrecisionInBits.
    //    if (overSized <= 1)
    //    {
    //        BigFloat p2 = PowerOf2(val);

    //        // if size difference is 1 BUT the outputSize is still correct just return
    //        if (overSized <= 0 || p2._size == maxOutputPrecisionInBits)
    //        {
    //            return p2;
    //        }
    //        // output is oversized by 1 
    //        return new BigFloat(p2._int, p2.Scale - 1, p2._size);
    //    }

    //    // at this point it is oversized by at least 2

    //    //oversized by 2 then shrink input by 1
    //    //oversized by 3 then shrink input by 1
    //    //oversized by 4 then shrink input by 2
    //    //oversized by 5 then shrink input by 2

    //    int inputShrink = (overSized + 1) / 2;

    //    BigInteger valWithLessPrec = val._int >> inputShrink;

    //    BigInteger prod = valWithLessPrec * valWithLessPrec;

    //    int resBitLen = (int)prod.GetBitLength();
    //    int shrinkBy = resBitLen - val.Size - ExtraHiddenBits;
    //    int sizePart = resBitLen - shrinkBy;
    //    prod = RightShiftWithRound(prod, shrinkBy);
    //    int resScalePart = (2 * val.Scale) + shrinkBy - ExtraHiddenBits;

    //    return new(prod, resScalePart, sizePart);
    //}

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
        //return (byte)(value._int << (value.Scale - ExtraHiddenBits));
        return (byte)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);

    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed byte.</summary>
    public static explicit operator sbyte(BigFloat value)
    {
        //return (sbyte)(value._int << (value.Scale - ExtraHiddenBits));
        return (sbyte)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 16-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ushort(BigFloat value)
    {
        //return (ushort)(value._int << (value.Scale - ExtraHiddenBits));
        return (ushort)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 16-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator short(BigFloat value)
    {
        //return (short)(value._int << (value.Scale - ExtraHiddenBits));
        return (short)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 64-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ulong(BigFloat value)
    {
        //return (ulong)(value._int << (value.Scale - ExtraHiddenBits));
        return (ulong)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);

    }
    /// <summary>Defines an explicit conversion of a BigFloat to a 64-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator long(BigFloat value)
    {
        //return (long)(value._int << (value.Scale - ExtraHiddenBits));
        return (long)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator UInt128(BigFloat value)
    {
        //return (UInt128)(value._int << (value.Scale - ExtraHiddenBits));
        return (UInt128)BigFloat.DataIntValueWithRound(value.DataBits << value.Scale);
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator Int128(BigFloat value)
    {
        //return (Int128)(value._int << (value.Scale - ExtraHiddenBits));
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
        if (value.OutOfPrecision)
        {
            return value.IsZero ? 0.0 : double.NaN;
        }
        // Aline and move input.val to show top 53 bits then pre-append a "1" bit.
        // was: long mantissa = (long)(value._int >> (value._size - 53)) ^ ((long)1 << 52);

        long mantissa = (long)(BigInteger.Abs(value.DataBits) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.Exponent + 1023 - 1;// + 52 -4;

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
        if (value.OutOfPrecision)
        {
            return value.IsZero ? 0.0f : float.NaN;
        }

        int mantissa = (int)(BigInteger.Abs(value.DataBits) >> (value._size - 24)) ^ (1 << 23);
        int exp = value.Exponent + 127 - 1;

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
        return (Exponent + 1023 - 1) is not (< -52 or > 2046);
    }

    /////////////////////////////////// COMPARE FUNCTIONS ////////////////////////////////////////////////////////

    /// <summary>Returns an input that indicates whether the current instance and a signed 64-bit integer have the same input.</summary>
    public bool Equals(long other)
    {
        //Todo: what about zero?
        if (Exponent > 64) // 'this' is too large, not possible to be equal.
        {
            return false;
        }
        else if (Exponent < 0)
        {
            return other == 0;
        }
        else if (Exponent == 64)
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
        if (Exponent > 64)
        {
            return false; // too large
        }
        else if (Exponent < 0)
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
        return other.Equals(Int);
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
    public override bool Equals(object obj)
    {
        //Check for null and compare run-time types.
        if (obj == null || !GetType().Equals(obj.GetType()))
        {
            return false;
        }

        BigFloat p = (BigFloat)obj;
        return Equals(p); //todo: to test
    }

    /// <summary>Returns a 32-bit signed integer hash code for the current BigFloat object.</summary>
    public override int GetHashCode()
    {
        return DataIntValueWithRound(DataBits).GetHashCode() ^ Scale;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////// MATH FUNCTIONS ////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Calculates the square root of a big floating point number.
    /// </summary>
    /// <param name="x">The input.</param>
    /// <param name="wantedPrecision">(Optional)The number of in-precision bits to return.</param>
    /// <returns>Returns the Sqrt of x as a BigFloat.</returns>
    public static BigFloat Sqrt(BigFloat x0, int wantedPrecision = 0)
    {
        BigFloat x = x0;// new BigFloat(x0.Int*8, x0._scale-3);
        if (wantedPrecision == 0)
        {
            wantedPrecision = x._size - ExtraHiddenBits;
        }

        if (x.DataBits == 0)
        {
            return new BigFloat((BigInteger)0, wantedPrecision, 0);
        }

        // Output should be (_int.GetBitLength()/2)+16 
        int totalLen = x.Scale + (x._size - ExtraHiddenBits);
        int needToShiftInputBy = (2 * wantedPrecision) - (x._size - ExtraHiddenBits) - (totalLen & 1);
        BigInteger intPart = NewtonPlusSqrt(x.DataBits << (needToShiftInputBy + ExtraHiddenBits));
        int retShift = ((totalLen + (totalLen > 0 ? 1 : 0)) / 2) - wantedPrecision;

        BigFloat result = new(intPart, retShift, (int)intPart.GetBitLength());    //new version 2022-11-12
        return result;
    }


    /// <summary>
    /// Returns the inverse of a BigFloat.
    /// </summary>
    public static BigFloat Inverse(BigFloat x)
    {
        // We need to oversize T (using left shift) so when we divide, it is the correct size.
        int leftShiftTBy = 2 * (x._size - 1);

        BigInteger one = BigInteger.One << leftShiftTBy;

        // Now we can just divide, and we should have the correct size
        BigInteger resIntPart = one / x.DataBits;

        //int resScalePartOrig = x._size - x.Scale - leftShiftTBy + ExtraHiddenBits;
        int resScalePart = -x.Scale - leftShiftTBy + ExtraHiddenBits + ExtraHiddenBits;
        //int resScalePart2 = x._size - (x.Scale*2) - leftShiftTBy + ExtraHiddenBits - 1;

        int sizePart = (int)BigInteger.Abs(resIntPart).GetBitLength();

        BigFloat result = new(resIntPart, resScalePart, sizePart);
        return result;
    }

    /// <summary>
    /// Calculates the a BigFloat as the base and an integer as the exponent. The integer part is treated as exact.
    /// </summary>
    /// <param name="value">The base of the exponent. </param>
    /// <param name="exponent">The number of times value should be multiplied.</param>
    /// <param name="outPrecisionMatchesInput">When true, output precision is matched to input precision. When false, precision uses exponent rules based on "value^exp ± exp*error^(n-1)".</param>
    public static BigFloat Pow(BigFloat value, int exponent, bool outPrecisionMatchesInput = false)
    {
        uint pwr = (uint)Math.Abs(exponent);

        if (pwr < 3)
        {
            return exponent switch
            {
                0 => BigFloat.One, //new BigFloat(BigInteger.One >> value.Scale, value.Scale),
                1 => value,
                -1 => Inverse(value),
                2 => value * value,
                _ /*-2*/ => Inverse(value * value)
            };
        }

        // Used a Genetic Algorithm in Excel to figure out the formula's below (2 options)
        int expectedFinalPrecision = value._size;
        if (outPrecisionMatchesInput)
        {
            expectedFinalPrecision += /*(int)(power / (1 - value)) -*/ BitOperations.Log2(pwr); // the first part is only for smaller values with large exponents
        }

        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if (expectedFinalPrecision < 53)
        {
            // Lets first make sure we would have some precision remaining after our exponent operation.
            if (expectedFinalPrecision <= 0)
            {
                return ZeroWithNoPrecision; // technically more of a "NA".
            }

            //bool expOverflows = value.Exponent < -1022 || value.Exponent > 1023;
            int removedExp = value.Exponent - 1;

            // todo: can be improved without using BigFloat  (See Pow(BigInteger,BigInteger) below)
            double valAsDouble = (double)new BigFloat(value.DataBits, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)

            //// if final result's scale would not fit in a double. 
            //int finalSizeWillBe = (int)(power * double.Log2(double.Abs(valAsDouble)));
            //bool finalResultsScaleFitsInDouble = finalSizeWillBe < 1020;  // should be <1023, but using 1020 for safety
            //if (!finalResultsScaleFitsInDouble)
            //    valAsDouble = (double)new BigFloat(value._int, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)

            // perform opp  
            double res = double.Pow(valAsDouble, exponent);
            BigFloat tmp = (BigFloat)res;
            value = SetPrecision(tmp, expectedFinalPrecision - ExtraHiddenBits);

            // restore Scale
            value = new BigFloat(value.DataBits, value.Scale + (removedExp * exponent), true);

            return value;
        }

        // the expectedFinalPrecision >= 53 bits and Power >= 3, so pretty big.

        // for each bit in the exponent, we need to multiply in 2^position
        int powerBitCount = BitOperations.Log2(pwr) + 1;

        // First Loop
        BigFloat product = ((pwr & 1) == 1) ? value : BigFloat.OneWithAccuracy(value.Size);
        BigFloat powers = value;

        for (int i = 1; i < powerBitCount; i++)
        {
            powers = PowerOf2(powers);

            if (((pwr >> i) & 1) == 1) // bit is set
            {
                product *= powers;
            }
        }

        if (exponent < 0)
        {
            product = Inverse(product);
        }

        //product.DebugPrint("bf1");
        return product;
    }




    public static BigFloat NthRoot_INCOMPLETE_DRAFT_BF(BigFloat value, int root)
    {
        bool DEBUG = true;

        //if (DEBUG) Console.WriteLine();
        bool rootIsNeg = root < 0;
        if (rootIsNeg)
        {
            root = -root;
        }

        bool resultIsPos = value.DataBits.Sign > 0;
        if (!resultIsPos)
        {
            value = -value;
        }

        resultIsPos = resultIsPos || ((root & 1) == 0);

        // Check if Value is zero.
        if (value.DataBits.Sign == 0)
        {
            return BigFloat.ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots... 
        switch (root)
        {
            case 0:
                return OneWithAccuracy(value.Size);
            case 1:
                return resultIsPos ? value : -value;
                //case 2:
                //    return resultIsPos ? Sqrt(value) : -Sqrt(value);
                //case 4:
                //    return resultIsPos ? Sqrt(Sqrt(value)) : -Sqrt(Sqrt(value));
        }

        //int xLen = value._size;
        int rootSize = BitOperations.Log2((uint)root);
        int wantedPrecision = (int)BigInteger.Log2(value.DataBits) + rootSize; // for better accuracy for small roots add: "+ rootSize / Math.Pow(( root >> (rootSize - 3)), root) - 0.5"

        ////////// Lets remove value's scale (and just leave the last bit so scale is 0 or 1) ////////
        int removedScale = value.Scale & ~1;
        int newScale = value.Scale - removedScale;

        ////////// Use double's hardware to get the first 53-bits ////////
        //long tempX = (long)(value._int >> (value._size - 52 /*- newScale*/ +22));
        ////////////////////////////////////////////////////////////////////////////
        long mantissa = (long)(BigInteger.Abs(value.DataBits) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.Exponent + 1023 - 1;// + 52 -4;

        // if exp is oversized for double we need to pull out some exp:
        if (Math.Abs(value.Exponent) > 1021) // future: using 1021(not 1022) to be safe
        {
            // old: (1)Pre: pre=(value<<(preShift*root)) (2)Root: result=pre^(1/root) (3)post: result/(1<<s)
            // new:  (1)Pre: pre=(value>>preShift) (2)Root: result=pre^(1/root) (3)post: result/(2^(-preShift/root)

            //double finalDiv = Math.Pow(2,-value.Exponent/root);
            exp = 0;
        }
        double dubVal = BitConverter.Int64BitsToDouble(mantissa | (exp << 52));
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // todo: what about just casting from BigFloat to double?
        //double test = Math.Log2(dubVal); //Math.Log2((double)tempX);
        double tempRoot = Math.Pow(dubVal, 1.0 / root);  //Math.Pow(tempX, 1.0/root)
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(tempRoot);
        ulong tempVal = (bits & 0x1fffffffffffffL) | (1UL << 52);
        int tempExp = (int)((bits >> 52) & 0x7ffL) - 1023 - 20;
        newScale += tempExp;

        ////////////////// BigFloat Version ////////////////////////////
        BigFloat x = new((BigInteger)tempVal << 100, newScale - 100, true);

        BigFloat rt = new((BigInteger)root << value.Size, -value.Size);  // get a proper sized "root" (only needed for BigFloat version)
        BigFloat t = Pow(x, root) - value;
        BigFloat b = rt * Pow(x, root - 1); // Init the "b" and "t" for "oldX - (t / b)"
        while (t._size > 3) //(!t.OutOfPrecision)
        {
            BigFloat oldX = x;
            BigFloat tb = t / b;
            x -= tb;
            if (DEBUG) Console.WriteLine($"{oldX} - ({t} / {b}) = ... - {tb} = {x}");
            b = rt * Pow(x, root - 1);
            t = Pow(x, root) - value; Console.WriteLine($"f-t:  {t.GetMostSignificantBits(196)}[{t._size}]");
        }
        return x;
    }





    public static BigFloat NthRoot_INCOMPLETE_DRAFT9(BigFloat value, int root)
    {
        bool rootIsNeg = root < 0;
        if (rootIsNeg)
        {
            root = -root;
        }

        bool resultIsPos = value.DataBits.Sign > 0;
        if (!resultIsPos)
        {
            value = -value;
        }

        resultIsPos = resultIsPos || ((root & 1) == 0);

        // Check if Value is zero.
        if (value.DataBits.Sign == 0)
        {
            return BigFloat.ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots... 
        switch (root)
        {
            case 0:
                return OneWithAccuracy(value.Size);
            case 1:
                return resultIsPos ? value : -value;
                //case 2:
                //    return resultIsPos ? Sqrt(value) : -Sqrt(value);
                //case 4:
                //    return resultIsPos ? Sqrt(Sqrt(value)) : -Sqrt(Sqrt(value));
        }

        //int xLen = value._size;
        int rootSize = BitOperations.Log2((uint)root);
        int wantedPrecision = (int)BigInteger.Log2(value.DataBits) + rootSize; // for better accuracy for small roots add: "+ rootSize / Math.Pow(( root >> (rootSize - 3)), root) - 0.5"



        //BigInteger val;

        ////////// Lets remove value's scale (and just leave the last bit so scale is 0 or 1) ////////
        int removedScale = value.Scale & ~1;
        int newScale = value.Scale - removedScale;

        ////////// Use double's hardware to get the first 53-bits ////////
        // todo: what about just casting from BigFloat to double?

        //long tempX = (long)(value._int >> (value._size - 52 /*- newScale*/ +22));
        ////////////////////////////////////////////////////////////////////////////
        long mantissa = (long)(BigInteger.Abs(value.DataBits) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.Exponent + 1023 - 1;// + 52 -4;

        // if exp is oversized for double we need to pull out some exp:
        if (Math.Abs(value.Exponent) > 1021) // future: using 1021(not 1022) to be safe
        {
            // old: (1)Pre: pre=(value<<(preShift*root)) (2)Root: result=pre^(1/root) (3)post: result/(1<<s)
            // new: (1)Pre: pre=(value>>preShift)        (2)Root: result=pre^(1/root) (3)post: result/(2^(-preShift/root)

            //double finalDiv = Math.Pow(2,-value.Exponent/root);
            exp = 0;
        }
        double dubVal = BitConverter.Int64BitsToDouble(mantissa | (exp << 52));
        ///////////////////////////////////////////////////////////////////////////////////////////////

        double tempRoot = Math.Pow(dubVal, 1.0 / root); // double.RootN(dubVal, root)
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(tempRoot);
        ulong tempVal = (bits & 0x1fffffffffffffL) | (1UL << 52);
        int tempExp = (int)((bits >> 52) & 0x7ffL) - 1023 - 20;
        newScale += tempExp;



        // If 53 bits enough precision, lets use that and return.
        //if (value._size < 53)
        //{  //  Shrink result to wanted Precision
        //    int shrinkAmt = (53 - value._size);
        //    BigFloat newVal = new BigFloat(tempVal >> shrinkAmt, newScale + shrinkAmt, value._size);
        //    return newVal;
        //}


        BigInteger x = tempVal;
        //x_Scale -= 100; //TEMP
        //xVal <<= 100; //TEMP

        ////////////////// BigFloat Version ////////////////////////////
        //BigFloat f_x = new((BigInteger)tempVal << 100, newScale - 100, true);
        //BigFloat rt = new((BigInteger)root << value.Size, -value.Size);  // get a proper sized "root" (only needed for BigFloat version)
        //BigFloat t = Pow(x, root) - value;
        //BigFloat b = rt * Pow(x, root - 1); // Init the "b" and "t" for "oldX - (t / b)"
        //while (t._size > 3) //(!t.OutOfPrecision)
        //{
        //    BigFloat oldX = x;
        //    BigFloat tb = t / b;
        //    x -= tb;
        //    if (DEBUG) Console.WriteLine($"{oldX} - ({t} / {b}) = {oldX} - {tb} =\r\n     {x}");
        //    b = rt * Pow(x, root - 1);
        //    t = Pow(x, root) - value;
        //}
        //BigFloat usingBigFloats = x; //new BigFloat(xVal, x_Scale, true);

        //x <<= 16;

        //===========================================================================================================================
        //Console.WriteLine($"i-x:  {BigIntegerToBinaryString(x)}[{x.GetBitLength()}]");
        int size = 53 - (2 * (int.Log2(root) + 1));

        //Console.WriteLine($"===================================================================================");

        BigInteger pow = PowMostSignificantBits(x, root, out _, 53, size * 2);
        BigInteger t = pow - (value.DataBits >> (int)(value.DataBits.GetBitLength() - pow.GetBitLength())); //Console.WriteLine($"i-t:  {BigIntegerToBinaryString(t)}[{t.GetBitLength()}]");

        BigInteger b = root * PowMostSignificantBits(x, root - 1, out int totalShift, 53, size, false, true);  //Console.WriteLine($"i-b:  {BigIntegerToBinaryString(b)}[{b.GetBitLength()}] totalShift:{totalShift}"); //we only get 50 bits from math.pow() =(
        int ADJ = 0;//(int)x.GetBitLength() + 1 - size; //0,1,5, //50 - (int)b.GetBitLength();
        //Console.WriteLine($"x:{x.GetBitLength()} x:{BigIntegerToBinaryString(x)[..1]} root:{int.Log2(root)} root:{root} value:{value._size} value:{BigIntegerToBinaryString(value.DataBits)[..1]} pow{pow.GetBitLength()} t:{t.GetBitLength()} b:{b.GetBitLength()} tb:{((t << (size - ADJ)) / b).GetBitLength()} size:{size} a:{2 * (int.Log2(root) + 1)} exp:{exp}");
        Console.WriteLine($"{x.GetBitLength()}, {BigIntegerToBinaryString(x)[1]},{BigIntegerToBinaryString(x)[..1]},{BigIntegerToBinaryString(x)[..2]}, {int.Log2(root)}, {root}, {value._size}, {BigIntegerToBinaryString(value.DataBits)[..1]}, {pow.GetBitLength()}, {t.GetBitLength()}, {b.GetBitLength()}, {((t << (size - ADJ)) / b).GetBitLength()}, {size}, {2 * (int.Log2(root) + 1)}, {exp}");
        Console.WriteLine($"{x.GetBitLength()}, {BigIntegerToBinaryString(x)[1]},{BigIntegerToBinaryString(x)[..1]},{BigIntegerToBinaryString(x)[..2]}, {int.Log2(root)}, {root}, {value._size}, {BigIntegerToBinaryString(value.DataBits)[..1]}, {pow.GetBitLength()}, {t.GetBitLength()}, {b.GetBitLength()}, {((t << (size - ADJ)) / b).GetBitLength()}, {size}, {2 * (int.Log2(root) + 1)}, {exp}");
        BigInteger tb = (t << (size - ADJ)) / b;                    //Console.WriteLine($"i-tb: {BigIntegerToBinaryString(tb)}[{tb.GetBitLength()}]");  // was 50 (sometimes 49)

        x = (x << 41) - tb;                                           //Console.WriteLine($"x:  {BigIntegerToBinaryString(x)}[{x.GetBitLength()}]");  // was 47
                                                                      //Console.WriteLine($"res:  10011111110011001010011000111011000001110100011100011000000010100100010111110000001100000101011010010001100100111010011000101101110010101110010111100010011011101011001101001011101101010011110111010111111001111111011100011001100011101001100110011000101001110000111001001010000101011...");
                                                                      //Console.WriteLine("Exact:123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345");
                                                                      //Console.WriteLine("Exact:000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222");


        while (((x.GetBitLength() * 7) / 8) < value.Size)
        {
            //Console.WriteLine($"========================== {((x.GetBitLength() * 7) / 8)} < {value.Size} ============================================="); 
            size *= 2;
            pow = PowMostSignificantBits(x, root, out _, (int)x.GetBitLength(), size * 2);  //Console.WriteLine($"pow:{BigIntegerToBinaryString(pow)}[{pow.GetBitLength()}]");
                                                                                            //Console.WriteLine($"val:{BigIntegerToBinaryString((value.DataBits >> (int)(value.DataBits.GetBitLength() - pow.GetBitLength())))}[{(value.DataBits.GetBitLength() - pow.GetBitLength())}]");
            t = pow - (value.DataBits >> (value._size - size * 2));                       //Console.WriteLine($"t:  {BigIntegerToBinaryString(t)}[{t.GetBitLength()}]");
            b = root * PowMostSignificantBits(x, root - 1, out _);                        //Console.WriteLine($"b:  {BigIntegerToBinaryString(b)}[{b.GetBitLength()}]");
            tb = (t << (size - ADJ)) / b;                                                   //Console.WriteLine($"tb: {BigIntegerToBinaryString(tb)}[{tb.GetBitLength()}]");
            x = (x << size) - tb;
            //Console.WriteLine($"x:    {BigIntegerToBinaryString(x)}[{x.GetBitLength()}]");
            //Console.WriteLine($"res:  10011111110011001010011000111011000001110100011100011000000010100100010111110000001100000101011010010001100100111010011000101101110010101110010111100010011011101011001101001011101101010011110111010111111001111111011100011001100011101001100110011000101001110000111001001010000101011...");
            //Console.WriteLine("Exact:10011111110011001010011000111011000001110100011100011000000010100100010111110000001100000101011010010001100100111010011000101101110010101110010111100010011011101011001101001011101101010011110111010111111001111111011100011001100011101001100110011000101001110000111001001010000101011");
        }

        int x_size = (int)x.GetBitLength();
        x = RightShiftWithRound(x, x_size - value._size, ref x_size);

        //Console.WriteLine($"down: {BigIntegerToBinaryString(x)}[{x.GetBitLength()}]");

        //calculate scale
        double test = Math.Log2(dubVal);
        int a = (int)-(Math.Log2(dubVal) / root);

        int scale = -x_size + ExtraHiddenBits - a + 1;

        BigFloat ret = new(x, scale, x_size);
        return ret;
    }

    [Conditional("DEBUG")]
    private void AssertValid()
    {
        int realSize = (int)BigInteger.Abs(DataBits).GetBitLength();

        // Make sure size is set correctly. Zero is allowed to be any size.
        Debug.Assert(_size == realSize, $"_size({_size}), expected ({realSize})");
    }

    [Conditional("DEBUG")]
    private static void AssertValid(BigFloat val)
    {
        int realSize = (int)BigInteger.Abs(val.DataBits).GetBitLength();

        // Make sure size is set correctly. Zero is allowed to be any size.
        Debug.Assert(val._size == realSize, $"_size({val._size}), expected ({realSize})");
    }

    public static BigFloat NthRoot_INCOMPLETE_DRAFT_10(BigFloat value, int n)
    {
        bool rootIsNeg = n < 0;
        if (rootIsNeg)
        {
            n = -n;
        }

        bool resultIsPos = value.DataBits.Sign > 0;
        if (!resultIsPos)
        {
            value = -value;
        }

        resultIsPos = resultIsPos || ((n & 1) == 0);

        // Check if Value is zero.
        if (value.DataBits.Sign == 0)
        {
            return BigFloat.ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots. 
        switch (n)
        {
            case 0:
                return OneWithAccuracy(value.Size);
            case 1:
                return resultIsPos ? value : -value;
                //case 2:
                //    return resultIsPos ? Sqrt(value) : -Sqrt(value);
                //case 4:
                //    return resultIsPos ? Sqrt(Sqrt(value)) : -Sqrt(Sqrt(value));
        }

        int mod = n - (32 % n); 
        BigInteger valueData = value.DataBits << mod;  //0
        BigInteger root = NewtonNthRoot(ref valueData, n, 0);
        double valueBitLengthyness = BigFloat.Log2(value);
        double resultBitLengthyness = valueBitLengthyness / n;
        double retLog2 = double.Log2((double)root);
        double retLog3 = (retLog2 - double.Floor(retLog2));

        resultBitLengthyness -= retLog3;
        int resultBitLength = (int)(resultBitLengthyness + 0.5);

        int rootLen = (int)root.GetBitLength();

        BigFloat ret = new(root, resultBitLength - rootLen + 32+1, true);

        //Console.WriteLine($"n:{n}[{int.Log2(n) + 1}] valueSz:{value._size} rootSz:{root.GetBitLength()} diff:{value._size - root.GetBitLength()} i{i} j{j}"); /*ret:\r\n{ret}*/
        //Console.WriteLine($"result: {ret} i{i} j{j}"); /**/
        AssertValid(ret);
        return ret;
    }

    //todo: untested
    /// <summary>
    /// Returns the Log2 of a BigFloat number as a double. Log2 is equivalent to the number of bits between the radix point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// Sign is ignored. Zero and negative values is undefined and will return double.NaN.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as a double. If Zero or less then returns Not-a-Number.</returns>
    public static double Log2(BigFloat n)
    {
        // Special case for zero and negative numbers.
        if (((n._size >= ExtraHiddenBits - 1) ? n.DataBits.Sign : 0) <= 0)
        // if (!n.IsPositive)
            return double.NaN;

        //The exponent is too large. We need to bring it closer to zero and then add it back in the log after.
        long mantissa = (long)(n.DataBits >> (n._size - 53));// ^ ((long)1 << 52);
        long dubAsLong =  (1023L << 52) | long.Abs(mantissa);
        double val = BitConverter.Int64BitsToDouble(dubAsLong);
        return double.Log2(val) + n.Exponent - 1;
    }

    //todo: untested (or maybe better should be merged with exponent as that seems to be what most classes/structs use like BigInteger and Int)
    /// <summary>
    /// Returns the Log2 of a BigFloat number as a integer. Log2 is equivalent to the number of bits between the decimal point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// Sign is ignored. Negative values will return the same value as there positive counterpart. Negative exponents are not valid in non-complex math however when using log2 a user might be expecting the number of bits from the radix point to the top bit.
    /// A zero input will follow BigInteger and return a zero, technically however Log2(0) is undefined. Log2 is often use to indicated size in bits so returning 0 with Log2(0) is in-line with this.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as a Int.</returns>
    public static int Log2Int(BigFloat n) => n.Exponent - 1;


    //public static BigInteger NewtonNthRoot(ref BigInteger x, int n)
    //{
    //    if (x == 0) return 0; // The n-th root of 0 is 0.
    //    if (n == 1) return x; // The 1st  root of x is x itself.
    //    if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

    //    int xLen = (int)x.GetBitLength();
    //    BigInteger scaledX = x;

    //    // If xLen is over 1023 bits, reduce the size of x to fit in a double
    //    int scaleDownCount = (xLen - 1024 + n) / n;
    //    scaledX >>= n * scaleDownCount; // Right-shift x by n bits

    //    // Calculate initial guess using scaled down x
    //    double initialGuess = Math.Pow((double)scaledX, 1.0 / n);

    //    // Adjust the initial guess by scaling it back up
    //    BigInteger val = new BigInteger(initialGuess);

    //    val <<= scaleDownCount;

    //    BigInteger lastVal = 0;
    //    while (val != lastVal) // Repeat until convergence
    //    {
    //        lastVal = val;
    //        BigInteger pow = BigInteger.Pow(val, n - 1);
    //        BigInteger numerator = pow * val - x;
    //        BigInteger denominator = n * pow;
    //        Console.WriteLine((BigIntegerToBinaryString(val)[0..150] + " val"));
    //        Console.WriteLine((BigIntegerToBinaryString(pow)[0..150] + " powNMinus1"));
    //        Console.WriteLine((BigIntegerToBinaryString(numerator)[0..150] + " numerator"));
    //        Console.WriteLine((BigIntegerToBinaryString(denominator)[0..150] + " denominator"));
    //        val -= numerator / denominator;
    //    }

    //    return val;


    //}

    //public static BigInteger NewtonNthRoot(ref BigInteger x, int n)
    //{
    //    if (x == 0) return 0; // The n-th root of 0 is 0.
    //    if (n == 1) return x; // The 1st  root of x is x itself.
    //    if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

    //    int xLen = (int)x.GetBitLength();

    //    // If xLen is over 1023 bits, reduce the size of x to fit in a double
    //    int scaleDownCount = Math.Max(0, ((xLen - 1024) / n) + 1);
    //    BigInteger scaledX = x >> n * scaleDownCount; // Right-shift x by n bits

    //    // Calculate initial guess using scaled down x
    //    double initialGuess = Math.Pow((double)scaledX, 1.0 / n);

    //    BigInteger val = new BigInteger(initialGuess);

    //    val <<= scaleDownCount;
    //    Console.WriteLine(BigIntegerToBinaryString(val));

    //    int loops = 0;
    //    int ballparkSize = 50;
    //    int estSize = xLen / n + 1;
    //    BigInteger lastVal = 0;
    //    while (val != lastVal) // Repeat until convergence
    //    {
    //        int reduceBy = Math.Max(0, estSize - ballparkSize);
    //        lastVal = val;
    //        BigInteger pow = PowMostSignificantBits(val, n - 1, out int shifted);

    //        BigInteger numerator = ((pow * val) >> reduceBy) - (x >> (shifted + reduceBy));
    //        //Console.WriteLine(BigIntegerToBinaryString(pow * val));
    //        //Console.WriteLine(BigIntegerToBinaryString(x >> shifted));
    //        BigInteger denominator = (n * pow) >> reduceBy;
    //        val -= numerator / denominator;

    //        Console.WriteLine(BigIntegerToBinaryString(val));
    //        loops++;
    //        ballparkSize *= 2;
    //    }
    //    Console.WriteLine($"Loops:{loops}  ballparkSize{ballparkSize}/{val.GetBitLength()}");

    //    return val;
    //}


    /// <summary>
    /// Calculates the nth root of a BigInteger. i.e. x^(1/n)
    /// </summary>
    /// <param name="x">The input value(or radicand) to find the nth root of.</param>
    /// <param name="n">The input nth root(or index) that should be used.</param>
    /// <param name="outputLen">The requested output length. If positive, then this number of bits will be returned. If negative(default), then proper size is returned. If 0, then an output will be returned with the same number of digits as the input. </param>
    /// <param name="xLen">If available, size in bits of input x. If negative, x.GetBitLength() is called to find the value.</param>
    /// <returns>Returns the nth root(or radical) or x^(1/n)</returns>
    public static BigInteger NewtonNthRoot(ref BigInteger x, int n, int outputLen = -1, int xLen = -1)
    {
        if (x == 0) return 0; // The n-th root of 0 is 0.
        if (n == 0) return 1; // The 1st  root of x is x itself.
        if (n == 1) return x; // The 1st  root of x is x itself.
        //if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

        if (xLen < 0) xLen = (int)x.GetBitLength();

        // If requested outputLen is neg then set to proper size, if outputLen==0 then use maintain precision.
        if (outputLen <= 0)
            outputLen = (outputLen == 0)?xLen : (int)BigInteger.Log2(x) / n + 1;
        
        // If xLen is over 1023 bits, reduce the size of x to fit in a double
        int scaleDownCount = Math.Max(0, ((xLen - 1024) / n) + 0);
        BigInteger scaledX = x >> n * scaleDownCount;

        ////////// Use double's hardware to get the first 53-bits ////////
        double initialGuess = Math.Pow((double)scaledX, 1.0 / n);
        long bits = BitConverter.DoubleToInt64Bits(initialGuess);
        long mantissa = bits & 0xfffffffffffffL | (1L << 52);

        // Return if we have enough bits.
        //if (outputLen < 48) return mantissa >> (53 - outputLen);
        if (outputLen < 48)
        {

            int bitsToRemove = 53 - outputLen;
            long mask = ((long)1 << (bitsToRemove + 1)) - 1;
            long removedBits = (mantissa + 1) & mask;
            if (removedBits == 0)
                mantissa++;

            return mantissa >> (53 - outputLen);
                //(mantissa, 53 - outputLen); 
        }

        //BigInteger val = new BigInteger(initialGuess); Console.WriteLine(val.GetBitLength() + " + " + scaleDownCount + " = " + (val.GetBitLength() + scaleDownCount)); Console.WriteLine($"{BigIntegerToBinaryString(val)}[{val.GetBitLength()}] << {scaleDownCount} val1");

        //////////////////////////////////////////////////////////////
        UInt128 val2 = ((UInt128)mantissa) << (127 - 52);
        
        UInt128 pow3 = Int128Tools.PowerFast(val2, n - 1);
        UInt128 pow4 = Int128Tools.MultHiFast(pow3, val2);

        Int128 numerator2 = (Int128)(pow4 >> 5) - (Int128)(x << ((int)UInt128.Log2(pow4) - 4 - xLen)); //todo: should use  "pow4>>127"
        Int128 denominator2 = n * (Int128)(pow3 >> 89);

        BigInteger val = ((Int128)(val2 >> 44) - numerator2 / denominator2);
        //Console.WriteLine((BigIntegerToBinaryString(val2) + " val1")); Console.WriteLine((BigIntegerToBinaryString(pow3) + " powNMinus1")); Console.WriteLine((BigIntegerToBinaryString(numerator2) + " numerator2")); Console.WriteLine((BigIntegerToBinaryString(denominator2)+ " denominator")); Console.WriteLine((BigIntegerToBinaryString(val) + " val2"));
        if (outputLen < 100) // 100?
            return val >> (84 - outputLen); 

        int tempShift = outputLen - (int)val.GetBitLength() + 0;  // FIX(for some): CHANGE +0 to +1
        if (UInt128.Log2(pow4) == 126) tempShift++;
        //Console.WriteLine(val.GetBitLength()+ " << " + tempShift + " = " + ((int)val.GetBitLength() + tempShift));
        val <<= tempShift;        // should be 241 now

        //////////////////////////////////////////////////////////////
        BigInteger lastVal = 0;
        int loops = 2;
        int ballparkSize = 200;
        while (val != lastVal) // Repeat until convergence
        {
            int reduceBy = Math.Max(0, outputLen - ballparkSize) + 1;
            lastVal = val;
            int valSize = (int)val.GetBitLength();
            BigInteger pow = BigIntegerTools.PowMostSignificantBits(val, n - 1, out int shifted, valSize, valSize - reduceBy);
            BigInteger numerator = ((pow * (val >> reduceBy))) - (x >> (2 * reduceBy - valSize)); // i: -200 j: 0  OR  i: -197 j: 2
            //Console.WriteLine(BigIntegerToBinaryString(((pow * (val >> (reduceBy * 1)))))); Console.WriteLine(BigIntegerToBinaryString((x >> (0 + reduceBy * 1)))); Console.WriteLine(BigIntegerToBinaryString(numerator)); Console.WriteLine(BigIntegerToBinaryString(x >> shifted));
            val = ((val >> (reduceBy + 0)) - numerator / (n * pow)) << reduceBy; // FIX: CHANGE +0 to +2
            loops++; // Console.WriteLine($"{BigIntegerToBinaryString(val)} loop:{loops}");
            ballparkSize *= 2;
        }
        Console.WriteLine($"======== Loops:{loops} == ballparkSize{ballparkSize}/{val.GetBitLength()} =========");
        Console.WriteLine("Grew by: " + (val.GetBitLength() - xLen));

        return val;
    }

    /// <summary>
    /// Multiplies two UInt128 values and only returns product as a BigInteger.
    /// Source: njuffa, 2015, https://stackoverflow.com/a/31662911/23187163
    /// </summary>
    /// <param name="a">The first UInt128 to multiply.</param>
    /// <param name="b">The second UInt128 to multiply.</param>
    /// <returns>Returns the result as a BigInteger.</returns>
    public static BigInteger BigIntegerMult(UInt128 a, UInt128 b)
    {
        UInt128 a_lo = (UInt64)a;
        UInt128 a_hi = a >> 64;
        UInt128 b_lo = (UInt64)b;
        UInt128 b_hi = b >> 64;

        UInt128 p0 = a_lo * b_lo;
        UInt128 p1 = a_lo * b_hi;
        UInt128 p2 = a_hi * b_lo;
        UInt128 p3 = a_hi * b_hi;

        UInt64 cy = (UInt64)(((p0 >> 64) + (UInt64)p1 + (UInt64)p2) >> 64);

        UInt128 lo = p0 + (p1 << 64) + (p2 << 64);
        UInt128 hi = p3 + (p1 >> 64) + (p2 >> 64) + cy;
        return ((BigInteger)hi << 128) + lo;
    }




}