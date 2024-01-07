// Copyright Ryan Scott White. 11/29/2020, 12/26/2020, 1/3/2021, 1/9/2021, 1/13/2021, 1/17/2021, 3/22/2022, 3/28/2022, 7/10/2022, 12/2022, 1/2023, 2/2023, 3/2023, 6/2023, 11/2023, 12/2023

// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// As of 12/1/2023, This class was written by a human only(Ryan).

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace BigFloatLibrary;

// for notes on zero see "BigFloatZeroNotes.txt"

// Considerations when naming this class
//   BigFloat : This would indicate a number with a floating decimal point. This describes this class.
//   BigRational: This indicates the faction part stored as an actual fraction (Numerator/Denominator). 
//   BigDecimal: This indicates processing/storage is base-10. However, this class is base-2 based.

/// <summary>
/// BigFloat stores a BigInteger with a floating decimal point.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{
    /// <summary>
    /// ExtraHiddenBits helps with precision by keeping an extra 32 bits. However, the ExtraHiddenBits should not be considered precise.
    /// The extra ExtraHiddenBits helps guard against some nuisances such as "7" * "9" being 60. 
    /// However, this has a performance cost as it does to process and store 32 additional bits.
    /// </summary>
    public const int ExtraHiddenBits = 32;  // 0-62, must be even (for sqrt)

    /// <summary>
    /// The stored BigInteger data with ExtraHiddenBits. This with the applied scaling make up the number.
    /// </summary>
    private readonly BigInteger _int;

    /// <summary>
    /// _size are the number of precision bits. It is equal to "ABS(_int).GetBitLength()". The ABS is for 
    ///       power-of-two negative BigIntegers (-1,-2,-4,-8...) so it is the same whether positive or negative.
    /// _size INCLUDES ExtraHiddenBits (the Property Size subtracts out ExtraHiddenBits)
    /// _size does not include rounding from ExtraHiddenBits. (11[111...111] (where [111...111] is ExtraHiddenBits) is still 2 bits. So the user will see it as 0b100 with a size of 2.)
    /// _size is 0 only when '_int==0'
    /// When BigFloat is Zero, the size is zero.
    /// </summary>
    internal readonly int _size;// { get; init; }

    // future: Possible future feature
    ///// <summary>
    ///// When positive, it is the number of extra virtual zeros tacked on the end of the internal _int for better precision and accuracy.  
    ///// Example: 11.001(with _extraPrec = 3) would be the same as 11.001000  
    /////   For the above example "000" would not take up any space and is also guaranteed to be all 0 bits.
    ///// When zero, this feature does not get used. (Default)
    ///// When negative, it's the number of least significant digits that repeat.
    /////    Example: 11.001(with _extraPrec = -3) would be 11.001001001001...
    ///// </summary>
    //private readonly int _extraPrec;

    /// <summary>
    /// The Scale (or -Accuracy) is the amount to left shift (<<) the integer(user's view) to get to the desired value. 
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

    /// <summary>
    /// The resulting binary point position when counting from the most significant bit. 
    /// Or where the [.]dataBits x 2^exp. Example: 0.11010 x 2^3 = 110.10 [Scale + Size]
    /// Examples: 0.11 -> 0; 1.11 -> 1; 10.1 -> 2; .001 = -2
    /// </summary>
    public int Exponent => Scale + _size - ExtraHiddenBits;

    //see BigFloatZeroNotes.txt for notes
    //perf: keep the shortcut "...&& Scale < 0 &&..."
    /// <summary>
    /// Returns true if the internal data bits round to zero. 
    /// </summary>
    public bool IsZero => _size < ExtraHiddenBits && Scale < 0 && (_size + Scale) < ExtraHiddenBits;

    // What is considered Zero: any dataInt that is LESS then 0:100000000, and also the shift results in a 0:100000000.
    //
    //  IntData    Scale Size Sz+Sc Precision  Zero
    //1:111111111 << -2   33    31      1       N 
    //1:000000000 << -2   33    31      1       N
    //1:000000000 << -1   33    32      1       N
    //1:000000000 <<  0   33    33      1       N
    //0:111111111 << -1   32    31      0       N 
    //0:100000000 << -1   32    31      0       N
    //0:100000000 <<  0   32    32      0       N
    //0:011111111 << -1   31    30     -1       Y
    //0:011111111 <<  0   31    31     -1       Y (borderline)
    //0:011111111 <<  1   31    32     -1       N
    //0:001111111 <<  1   31    32     -2       Y (borderline)
    //0:001111111 <<  2   31    33     -2       N    

    /// <summary>
    /// Returns true there are is less then 1 bit of precision.
    /// </summary>
    public bool OutOfPrecision => _size < ExtraHiddenBits;

    /// <summary>
    /// Rounds and returns true if this value is positive. Zero is not considered positive or negative. Only the top bit in ExtraHiddenBits is counted.
    /// </summary>
    public bool IsPositive => Sign > 0;

    /// <summary>
    /// Rounds and returns true if this value is negative. Only the top bit in ExtraHiddenBits is counted.
    /// </summary>
    public bool IsNegative => Sign < 0;

    /// <summary>
    /// Rounds and returns -1 if negative, 0 if zero, and +1 if positive. Only the top bit in ExtraHiddenBits is counted.
    /// </summary>
    public int Sign => (_size >= ExtraHiddenBits) ? _int.Sign : 0;

    /// <summary>
    /// Returns the integer part of the BigFloat. No scaling is applied. ExtraHiddenBits are rounded and removed.
    /// </summary>
    public readonly BigInteger Int => DataIntValueWithRound(_int);

    public string DebuggerDisplay
    {
        get
        {
            string bottom8HexChars = (BigInteger.Abs(_int) & ((BigInteger.One << ExtraHiddenBits) - 1)).ToString("X8").PadLeft(8)[^8..];
            StringBuilder sb = new(32);
            _ = sb.Append($"{ToString()}, "); //  integer part using ToString()
            _ = sb.Append($"{(_int.Sign >= 0 ? " " : "-")}0x{BigInteger.Abs(_int) >> ExtraHiddenBits:X}:{bottom8HexChars}"); // hex part
            _ = sb.Append($"[{Size}+{ExtraHiddenBits}={_size}], {((Scale >= 0) ? "<<" : ">>")} {Math.Abs(Scale)}");

            return sb.ToString();
        }
    }

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
        Console.WriteLine($"    |Dec : {_int >> ExtraHiddenBits}{((double)(_int & (((ulong)1 << ExtraHiddenBits) - 1)) / ((ulong)1 << ExtraHiddenBits)).ToString()[1..]} {shift}");
        Console.WriteLine($"    |Dec : {_int >> ExtraHiddenBits}:{_int & (((ulong)1 << ExtraHiddenBits) - 1)} {shift}");  // decimal part (e.g. .75)
        if (_int < 0)
        {
            Console.WriteLine($"   or -{-_int >> ExtraHiddenBits:X4}:{(-_int & (((ulong)1 << ExtraHiddenBits) - 1)).ToString("X8")[^8..]}");
        }

        Console.WriteLine($"    |_int: {_int}");
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
    /// Returns a "1" with a given precision. ExtraHiddenBits will be added.
    /// </summary>
    /// <param name="precisionInBits">The precision between -32(ExtraHiddenBits) to +Inf.</param>
    public static BigFloat OneWithPrecision(int precisionInBits)
    {
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return precisionInBits <= -ExtraHiddenBits
            ? ZeroWithNoPrecision
            : new(BigInteger.One << (ExtraHiddenBits + precisionInBits), -precisionInBits, ExtraHiddenBits + 1 + precisionInBits);
        // alternative: throw new ArgumentException("The requested precision would leave not leave a bit for data.");
    }

    public static BigFloat NegativeOne => new(-BigInteger.One << ExtraHiddenBits, 0, ExtraHiddenBits + 1);

    /////////////////////////    INIT / CONVERSION  FUNCTIONS     /////////////////////////

    /// <summary>
    /// Contracts a BigFloat using the raw elemental parts. The user is responsible to pre-up-shift rawValue and set <param name="scale"> and <param name="rawValueSize">.
    /// </summary>
    /// <param name="rawValue">The raw integerPart. It should INCLUDE the ExtraHiddenBits.</param>
    /// <param name="rawValueSize">The size of rawValue. </param>
    private BigFloat(BigInteger rawValue, int scale, int rawValueSize)
    {
        _int = rawValue;
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
        _int = integerPart << applyHiddenBits;
        _size = (int)BigInteger.Abs(_int).GetBitLength();
        Scale = scale; // _int of zero can have scale

        AssertValid();
    }

    public BigFloat(char integerPart, int scale = 0)
    {
        _int = (BigInteger)integerPart << ExtraHiddenBits;
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
        _int = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;
        _size = integerPart == 0 ? 0 : BitOperations.Log2(integerPart) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(int integerPart, int scale = 0) : this((long)integerPart, scale) { }

    public BigFloat(uint value, int scale = 0)
    {
        _int = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(long value, int scale = 0)
    {
        _int = (BigInteger)value << ExtraHiddenBits;
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
        _int = (BigInteger)value << ExtraHiddenBits;
        Scale = scale;
        _size = value == 0 ? 0 : BitOperations.Log2(value) + 1 + ExtraHiddenBits;
        AssertValid();
    }

    public BigFloat(Int128 integerPart, int scale = 0)
    {
        _int = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();
    }

    public BigFloat(Int128 integerPart, int scale, bool valueIncludesHiddenBits)
    {
        _int = (BigInteger)integerPart << ExtraHiddenBits;
        Scale = scale;

        _size = integerPart > Int128.Zero
            ? (int)Int128.Log2(integerPart) + 1 + ExtraHiddenBits
            : integerPart < Int128.Zero ? 128 - (int)Int128.LeadingZeroCount(~(integerPart - 1)) + ExtraHiddenBits : 0;

        AssertValid();

        int applyHiddenBits = valueIncludesHiddenBits ? 0 : ExtraHiddenBits;
        // we need Abs() so items that are a negative power of 2 has the same size as the positive version.
        _size = (int)((BigInteger)(integerPart >= 0 ? integerPart : -integerPart)).GetBitLength() + applyHiddenBits;
        _int = integerPart << applyHiddenBits;
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
            _int = new BigInteger(mantissa) << ExtraHiddenBits;
            Scale = exp - 1023 - 52 + additionalScale;
            _size = 53 + ExtraHiddenBits; //_size = BitOperations.Log2((ulong)Int);
        }
        else // exp is 0 so this is a denormalized float (leading "1" is "0" instead)
        {
            // 0:00000000000:00...0001 -> smallest value (Epsilon)  Int:1, Scale: Size:1
            // ...

            if (mantissa == 0)
            {
                _int = 0;
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
                _int = (new BigInteger(mantissa)) << (ExtraHiddenBits);
                Scale = -1023 - 52 + 1 + additionalScale; // todo: is this correct?
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
            _int = new BigInteger(mantissa) << ExtraHiddenBits;
            Scale = exp - 127 - 23 + additionalScale;
            _size = 24 + ExtraHiddenBits;
        }
        else // exp is 0 so this is a denormalized(Subnormal) float (leading "1" is "0" instead)
        {
            if (mantissa == 0)
            {
                _int = 0;
                Scale = additionalScale;
                _size = 0; //24 + ExtraHiddenBits;
            }
            else
            {
                BigInteger mant = new(value >= 0 ? mantissa : -mantissa);
                _int = mant << ExtraHiddenBits;
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

    public BigFloat(string value, int scale = 0)
    {
        this = Parse(value, scale);
    }
    ///////////////////////// [END] INIT / CONVERSION  FUNCTIONS [END] /////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////    TO_STRING  FUNCTIONS     ////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////

    // see "BigFloatToStringNotes.txt" and "BigFloatTryParseNotes.txt" for additional notes
    //   string ToString() - To Decimal(Ex: 222.22)   
    //   string ToString(string format) - to Hex(Ex: A4B.F2) and Binary(Ex: 1010111.001)
    //   string ToStringHexScientific(bool showHiddenBits = false, bool showSize = false, bool showInTwosComplement = false) - example "12AC<<22"

    [DebuggerHidden()]
    public override string ToString()
    {
        if (Scale < 0)// Number will have a decimal point. (e.g. 222.22, 0.01, 3.1)
        {
            // How many digits do we need? (does not need to be exact at this stage)
            int digitsNeeded = (int)Math.Round(-Scale / 3.32192809488736235);
            //int digitsNeeded = (int)(-Scale / 3.32192809488736235) + 1;

            BigInteger power5 = BigInteger.Abs(_int) * BigInteger.Pow(5, digitsNeeded);

            // Applies the scale to the number and rounds from bottom bit
            BigInteger power5Scaled = RightShiftWithRound(power5, -Scale - digitsNeeded + ExtraHiddenBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {
                if (Int.IsZero)
                {
                    return $"0.{new string('0', digitsNeeded)}";
                }

                // solves an issue when with a "BigFloat(1, -8)" being 0.000
                digitsNeeded++;
                power5 = BigInteger.Abs(_int) * BigInteger.Pow(5, digitsNeeded);
                power5Scaled = RightShiftWithRound(power5, -Scale - digitsNeeded + ExtraHiddenBits);
            }

            string numberText = power5Scaled.ToString();

            int decimalOffset = numberText.Length - digitsNeeded;
            //int decimalOffset2 = ((int)((_size - ExtraHiddenBits + Scale) / 3.32192809488736235)) - ((numberText[0] - '5') / 8.0);  //alternative

            // The length should have room for [-][digits][.][digits]
            int length = (_int < 0 ? 3 : 2) + numberText.Length - (Exponent <= 0 ? decimalOffset : 1);
            char[] chars = new char[length];
            int position = 0;

            if (_int < 0)
            {
                chars[position++] = '-';
            }

            // We can round a 0.99 to a 1.00, hence the "(Exponent==0 && decimalOffset <= 0)"
            if (Exponent < 0 || (Exponent == 0 && decimalOffset <= 0))  // 0.xxxxx 
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
            else  // xxxx.xxxx
            {
                numberText.CopyTo(0, chars, position, decimalOffset);
                position += decimalOffset;

                chars[position++] = '.';

                numberText.CopyTo(decimalOffset, chars, position, numberText.Length - decimalOffset);

                return new string(chars);
            }
        }
        else  //  XXXXX or XXX0000  (The numbers with no decimal precision.)
        {
            // Future: Just print zeros or use Scientific notation for out of precision numbers instead of continuing to evaluate. 
            BigInteger withoutPoint = RightShiftWithRound(_int, ExtraHiddenBits - Scale);
            return withoutPoint.ToString();
        }
    }

    /// <summary>
    /// Writes a BigFloat in Hex('X') or Binary('B'). A radix point is supported. Negative values must have a leading '-'. 
    /// </summary>
    /// <param name="format">Format specifier: 'X' for hex, 'B' for binary, or empty for decimal</param>
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
                return (_int >> (ExtraHiddenBits - Scale)).ToString("X"); // This version includes hidden bits in result
            }

            // We have to align the INT to the nearest 4 bits for hex. We also want to remove the ExtraHiddenBits.
            // The number of bits between the radix point and the end should be divisible by 4. We will dig into the ExtraHiddenBits for this.
            int rightShift = (ExtraHiddenBits - Scale) & 0x03;

            BigInteger shiftedBigIntForDisplay = RightShiftWithRound(_int, rightShift);

            return shiftedBigIntForDisplay.ToString("X").Insert((-Scale / 4) - 1, ".");
        }

        if (format[0] == 'B') // Signals a binary (with radix point)
        {
            // Setup destination and allocate memory
            Span<char> dstBytes = stackalloc char[_size - ExtraHiddenBits
                + Math.Max(Math.Max(Scale, -(_size - ExtraHiddenBits) - Scale), 0) // total number of out-of-precision zeros in the output.
                + (_int.Sign < 0 ? 1 : 0)   // add one if a leading '-' sign (-0.1)
                + (Scale < 0 ? 1 : 0)       // add one if it has a point like (1.1)
                + (Exponent <= 0 ? 1 : 0)];  // add one if <1 for leading Zero (0.1) 
            int dstIndex = 0;

            // Three types
            //   Type '12300' - if all bits are to the left of the radix point(no radix point required)
            //   Type '12.30' - has numbers below AND above the point. (e.g. 11.01)
            //   Type '0.123' - all numbers are to the right of the radix point. (has leading 0.or - 0.)

            // Pre-append the leading sign.
            if (_int.Sign < 0)
            {
                dstBytes[dstIndex] = '-';
                dstIndex++;
            }

            // Setup source bits to read.
            ReadOnlySpan<byte> srcBytes = DataIntValueWithRound(BigInteger.Abs(_int)).ToByteArray();
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

        BigInteger intVal = _int;
        if (!showInTwosComplement && _int.Sign < 0)
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
    /// A high performance BigInteger to binary string converter
    /// that supports 0 and negative numbers.
    /// License: MIT / Created by Ryan Scott White, 7/16/2022;
    /// </summary>
    public static void BigIntegerToBinarySpan(BigInteger x, ref Span<char> dstBytes)
    {
        // Setup source
        ReadOnlySpan<byte> srcBytes = x.ToByteArray();
        int srcLoc = srcBytes.Length - 1;

        // Find the first bit set in the first byte so we don't print extra zeros.
        int msb = BitOperations.Log2(srcBytes[srcLoc]);

        // Setup Target
        //Span<char> dstBytes = stackalloc char[srcByte * 8 + MSB + 2];
        int dstLoc = 0;

        // Add leading '-' sign if negative.
        if (x.Sign < 0)
        {
            dstBytes[dstLoc++] = '-';
        }
        //else if (!x.OutOfPrecision) dstBytes[dstByte++] = '0'; // add adding leading '0' (optional)

        // The first byte is special because we don't want to print leading zeros.
        byte b = srcBytes[srcLoc--];
        for (int j = msb; j >= 0; j--)
        {
            dstBytes[dstLoc++] = (char)('0' + ((b >> j) & 1));
        }

        // Add the remaining bits.
        for (; srcLoc >= 0; srcLoc--)
        {
            byte b2 = srcBytes[srcLoc];
            for (int j = 7; j >= 0; j--)
            {
                dstBytes[dstLoc++] = (char)('0' + ((b2 >> j) & 1));
            }
        }
    }

    public static char[] BigIntegerToBinaryString(BigInteger x)
    {
        char[] chars = new char[x.GetBitLength() + 1]; // ( (x<0) ? 1 : 0 )];  // not tested!
        Span<char> charsSpan = new(chars);
        BigIntegerToBinarySpan(x, ref charsSpan);
        return chars;
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
    /// <param name="scale">Optional apply and positive or negative base 2 scaling.(default is zero)</param>
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
    /// <param name="scale">Optional apply and positive or negative base 2 scaling.(default is zero)</param>
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

        //Enable either ROUND or EXTRABIT with a 1
        const int ROUND = 0;    // Set to 1 to enable round to nearest. (An extra bit added and if it is a 1 it will round up. (e.g. 0.1011 => 0.110)
        BigInteger intPart;

        int radixDepth = numericString.Length - radixLoc - exp;
        if (radixDepth == 0)
        {
            result = new BigFloat(asInt, scale);
        }
        else if (radixDepth >= 0)
        {
            BigInteger a = BigInteger.Pow(5, radixDepth);
            int multBitLength = (int)a.GetBitLength();
            multBitLength += (int)(a >> (multBitLength - 2)) & 0x1;  // Round up if closer to larger size 
            int shiftAmt = multBitLength + ExtraHiddenBits - 1 + ROUND;  // added  "-1" because it was adding one to many digits 
                                                                         // we have to make asInt larger by size of "a" before we divide it by "a"
            intPart = (((asInt << shiftAmt) / a) + ROUND) >> ROUND;
            scale += -multBitLength + 1 - radixDepth;
            result = new BigFloat(intPart, scale, true);
        }
        else
        {
            BigInteger a = BigInteger.Pow(5, -radixDepth);
            int multBitLength = (int)a.GetBitLength();
            int shiftAmt = multBitLength - ExtraHiddenBits - ROUND;
            // since we are making asInt larger by size multiplying it by "a", we now need to shrink it by size "a"
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
    /// negative number. It will also ignore spaces and tolerate values wrapped with double quotes and brackets.
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
    /// If it fails it returns false.
    /// e.g '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="result">(out) The BigFloat result.</param>
    /// <param name="scale">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <returns>True is successful; False if it fails.</returns>
    public static bool TryParseBinary(string input, out BigFloat result, int scale = 0, int forceSign = 0)
    {
        if (string.IsNullOrEmpty(input))
        {
            result = new BigFloat(0);
            return false;
        }

        return TryParseBinary(input.AsSpan(), out result, scale, forceSign);
    }

    /// <summary>
    /// Converts the binary text in ReadOnlySpan<char> to a BigFloat. 
    /// If it fails it returns false.
    /// e.g '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="result">(out) The BigFloat result.</param>
    /// <param name="scale">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <returns>True is successful; False if it fails.</returns>
    public static bool TryParseBinary(ReadOnlySpan<char> input, out BigFloat result, int scale = 0, int forceSign = 0)
    {
        int inputLen = input.Length;

        int orgScale = scale;

        //if (inputLen == 1)
        //{
        //    //if (inputLen == 0)
        //    //{
        //    //    result = new BigFloat(0);
        //    //    return false;
        //    //}

        //    int bin = input[0] - '0';
        //    if (bin is 0 or 1)
        //    {
        //        result = new BigFloat(bin);
        //        return true;
        //    }

        //    // digit is not 0 or 1
        //    result = new BigFloat(0);
        //    return false;
        //}

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

        //                                01234567 89012345
        // Given the Input String:        00000001 00000010 00000011  
        // Output Byte Array should be:      [2]1    [1]2     [0]3  
        //                                
        // Now we are going to work our way from the end of the string forward.
        // We work backward to ensure the byte array is correctly aligned.
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
                default:
                    result = new BigFloat(0);
                    return false; // Function was not successful - unsupported char found
            }
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

        if (bi.IsZero)
        {
            scale = 0;
        }

        // There is no decimal point so let's just use BigInteger to convert.
        result = new BigFloat(bi, radixPointFound ? scale : orgScale);

        // future: use "private BigFloat(BigInteger rawValue, int scale, int size)" for performance

        result.AssertValid();

        // return true if success, if no 0/1 bits found then return false.
        return outputBitPosition != 0;
    }

    /// <summary>
    /// Converts the binary text in ReadOnlySpan<char> to a BigFloat. 
    /// If it fails it returns false.
    /// e.g '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">(out) The binary string input. It should be only [-/+, 0,1,' ',comma,_]</param>
    /// <param name="result">The BigInteger result.</param>
    /// <returns>True is successful; False if it fails.</returns>
    public static bool TryParseBinary(ReadOnlySpan<char> input, out BigInteger result)
    {
        int inputLen = input.Length;

        byte[] bytes = new byte[(inputLen + 7) / 8];
        int outputBitPosition = 0;   // The current bit we are writing to.

        // if it starts with a '-' then set negative rawValue to zero
        bool isNeg = input[0] == '-'; // 0x2D;

        // if starting with at - or + then headPosition should be 1.
        int headPosition = isNeg | input[0] == '+' ? 1 : 0;

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
                    break;
                case ',' or '_' or ' ': // allow commas, underscores, and spaces (e.g.  1111_1111_0000) (optional - remove for better performance)
                    break;
                default:
                    result = new BigInteger(0);
                    return false; // Function was not successful - unsupported char found
            }
        }

        // If the number is negative, let's perform Two's complement: (1) negate the bits (2) add 1 to the bottom byte
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

        result = new(bytes, !isNeg);

        // return true if success, if no 0/1 bits found then return false.
        return outputBitPosition != 0;
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
    /// Compares two values and returns...
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other (Accuracy of higher number reduced 
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11==1.1,  1.00==1.0,  1.11!=1.10)
    ///   Returns Positive => this instance is greater than other
    /// </summary>
    public int CompareTo(BigFloat other)
    {
        if (OutOfPrecision)
        {
            return other.OutOfPrecision ? 0 : -other._int.Sign;
        }

        if (other.OutOfPrecision)
        {
            return OutOfPrecision ? 0 : _int.Sign;
        }

        // Lets see if we can escape early by just looking at the Exponent.
        if (_int.Sign != other._int.Sign)
        {
            return _int.Sign;
        }

        // Lets see if we can escape early by just looking at the Exponent.
        int expDifference = Exponent - other.Exponent;
        if (Math.Abs(expDifference) > 1)
        {
            return Exponent.CompareTo(other.Exponent) * _int.Sign;
        }

        // At this point the sign is the same and the exp are within 1 bit of each other.

        // There are three special cases when the Exponent is off by just 1 bit:
        //  case 1: the smaller of the two rounds up to match the size of the larger and therefor can be equal (11|111 == 100|000)
        //  case 2: the smaller of the two rounds up, but the larger also rounds up so not equal (depends on #1 happening first)
        //  case 3: both round up and are therefor equal again

        // If "this" is larger by one bit AND "this" is not in the format 10000000..., THEN "this" must be larger(or smaller if neg)
        if (expDifference == 1 && !IsOneBitFollowedByZeroBits)
        {
            return _int.Sign;
        }
        // If "other" is larger by one bit AND "other" is not in the format 10000000..., THEN "other" must be larger(or smaller if neg)
        if (expDifference == -1 && !other.IsOneBitFollowedByZeroBits)
        {
            return -Sign;
        }
        else // if (expDifference == 0)
        {
            int sizeDiff = _size - other._size;

            BigInteger temp = (_int.Sign >= 0) switch
            {
                true => sizeDiff switch  // Both positive values
                {
                    > 0 => -(other._int - (_int >> (sizeDiff - expDifference))),
                    < 0 => -((other._int << (sizeDiff - expDifference)) - _int),
                    _ => expDifference switch
                    {
                        0 => _int - other._int,
                        1 => _int - (other._int >> 1),
                        _/*-1*/ => (_int >> 1) - other._int,
                    }
                },

                false => sizeDiff switch // Both negative values
                {
                    > 0 => -(other._int - (_int >> (sizeDiff - expDifference))),
                    < 0 => -((other._int << (sizeDiff - expDifference)) - _int),
                    _/*0*/ => expDifference switch
                    {
                        0 => _int - other._int,
                        1 => _int - (other._int >> 1),
                        _/*-1*/ => (_int >> 1) - other._int,
                    }
                }
            };

            // a quick exit
            int bytes = temp.GetByteCount();
            if (bytes != 4)
                return (bytes > 4)?temp.Sign : 0;

            // since we are subtracting, we can run into an issue where a 0:100000 should be considered a match.  e.g. 11:000 == 10:100
            temp -= temp.Sign; //decrements towards 0

            // Future: need to benchmark A, B or C
            //int a = RightShiftWithRound(temp, ExtraHiddenBits).Sign;
            //int b = (BigInteger.Abs(temp) >> (ExtraHiddenBits - 1)).IsZero ? 0 : temp.Sign;
            int c = ((int)((temp.Sign >= 0) ? temp : -temp).GetBitLength() < ExtraHiddenBits) ? 0 : temp.Sign;

            return c;
        }
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
        int thisPos = _int.Sign;
        int otherPos = other._int.Sign;

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
            return _int.CompareTo(other._int);
        }

        if (_size > other._size)
        {
            // We must grow the smaller - in this case THIS
            BigInteger adjustedVal = _int << (other._size - _size);
            return adjustedVal.CompareTo(other._int) * thisPos;
        }

        // at this point _size < other._size - we must grow the smaller - in this case OTHER
        BigInteger adjustedOther = other._int << (_size - other._size);
        return _int.CompareTo(adjustedOther) * thisPos;
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
            > 0 => (a._int >> scaleDiff) - b._int,  // 'a' has more accuracy
            < 0 => a._int - (b._int >> -scaleDiff), // 'b' has more accuracy
            _ => a._int - b._int
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
    /// </summary>
    public int CompareTo(BigInteger bigInteger)
    {
        int thisSign = _int.Sign;
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
        return RightShiftWithRound(_int,-Scale + ExtraHiddenBits).CompareTo(bigInteger);
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
            BigInteger maskApplied = _int & mask;
            int bitsSet = (int)BigInteger.PopCount(maskApplied);
            return (bitsSet == 0) || (bitsSet == endMask - begMask);
        }
    }

    /// <summary>
    /// Tests to see if the number is in the format of "10000000..." after rounding.
    /// </summary>
    public bool IsOneBitFollowedByZeroBits
    {
        // todo: test
        get
        {
            return BigInteger.TrailingZeroCount(_int >> (ExtraHiddenBits - 1)) == (_size - ExtraHiddenBits);
        }
    }

    //todo: test these
    public ulong Lowest64BitsWithHiddenBits
    {
        get
        {
            ulong raw = (ulong)(_int & ulong.MaxValue);

            if (_int.Sign < 0)
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
            if (_int.Sign >= 0)
            {
                ulong raw = (ulong)((_int >> ExtraHiddenBits) & ulong.MaxValue);
                return raw;
            }
            else if (_size >= ExtraHiddenBits)
            {
                return ~(ulong)(((_int - 1) >> ExtraHiddenBits) & ulong.MaxValue);
                //return (ulong)((BigInteger.Abs(_int) >> ExtraHiddenBits) & ulong.MaxValue); //todo: benchmark

            }
            else
            {
                ulong raw = (ulong)((_int >> ExtraHiddenBits) & ulong.MaxValue);
                //raw--;
                raw = ~raw;
                return raw;
            }
        }
    }

    /// <summary>
    /// Returns the 64 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 64 bits, then the LSBs are padded with zeros.
    /// </summary>
    public ulong Highest64Bits => (ulong)((BigInteger.IsPositive(_int) ? _int : -_int) >> /*Math.Abs*/(_size - 64));

    /// <summary>
    /// Rounds to the next integer towards negative infinity. 
    /// Since we are stripping any fraction bit, any negative scales will be set to zero, also the precision(size) will be resized to just the integer part.
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
            return _int.Sign >= 0 ? new BigFloat(0, 0, 0) : new BigFloat(-BigInteger.One << ExtraHiddenBits, 0, 1 + ExtraHiddenBits);
        }

        if (_int.Sign > 0)
        {
            // If Positive and Floor, the size should always remain the same.
            // If Scale is between 0 and ExtraHiddenBits..
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            if (Scale >= 0) // SCALE >= 0 and SCALE < ExtraHiddenBits
            {
                return new BigFloat((_int >> bitsToClear) << bitsToClear, Scale, _size);
            }

            // If Scale is between -size and 0..
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]
            //BigInteger intPart = ((_int >> bitsToClear) + 1) << ExtraHiddenBits;
            //return new BigFloat((_int >> bitsToClear) +  (IsInteger?0:1));
            return new BigFloat(_int >> bitsToClear);
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
                bool roundsUp = (_int & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = _int >> bitsToClear << bitsToClear;
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
                //bool roundsUp = (_int & ((((BigInteger)1 << (ExtraHiddenBits/2 - Scale)) - 1) << (ExtraHiddenBits / 2))) > 0;
                BigInteger intPart = _int >> bitsToClear;
                //todo:  I think Size for something like -1111111111111111 might not be correct size(maybe)
                //-0.111
                //if (IsInteger)
                //    intPart++; 

                //int size = IsInteger ? _size - bitsToClear + ExtraHiddenBits : (int)intPart.GetBitLength(); //future: maybe slow (maybe use BigInteger.TrailingZeroCount to detect rollover)

                return new BigFloat(intPart);
            }
        }
    }

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
            return _int.Sign <= 0 ? new BigFloat(0, 0, 0) : new BigFloat(BigInteger.One << ExtraHiddenBits, 0, 1 + ExtraHiddenBits);
        }

        // Radix point is in the ExtraHiddenBits area
        //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
        if (Scale < ExtraHiddenBits) // SCALE >= 0 and SCALE<ExtraHiddenBits
        {
            // optimization here?
        }

        if (_int.Sign > 0)
        {
            //   If Positive and Ceiling, and the abs(result) is a PowerOfTwo the size will grow by 1.  -1111.1 -> -10000, -10000 -> -10000
            // Lets just remove the bits and clear ExtraHiddenBits
            //   Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  101101[1010.1010010...00010]  -> 101101[1010.0000000...00000]
            //   Example: Scale = -4, int=45, size=6+32=38  -> bitsToClear=32+4  10.1101[10101010010...00010]  -> 10.[00000000000...00000]

            if (Scale >= 0) // Scale is between 0 and ExtraHiddenBits
            {
                //  Example: Scale =  4, int=45, size=6+32=38  -> bitsToClear=32-4  -101101[1010.1010010...00010]  -> -101101[1011.0000000...00000]
                bool roundsUp = (_int & ((1 << bitsToClear) - 1)) > 0;
                BigInteger intPart = _int >> bitsToClear << bitsToClear;
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
                bool roundsUp = (_int & ((((BigInteger)1 << ((ExtraHiddenBits / 2) - Scale)) - 1) << (ExtraHiddenBits / 2))) > 0;

                BigInteger intPart = _int >> bitsToClear << ExtraHiddenBits;

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
                return new BigFloat((_int >> bitsToClear) << bitsToClear, Scale, _size);
            }
            BigInteger intPart = _int >> bitsToClear;
            //todo:  I think Size for something like -1111111111111111 might not be correct size(maybe)

            if (!IsInteger)
            {
                intPart++;
            }

            return new BigFloat(intPart);
        }
    }

    // todo: test below
    /// <summary>
    /// Returns the number of matching leading bits, or, the difference in their Log2 values.
    /// e.g. 10.111 and 10.101 returns 
    /// When a rollover is near these bits are included. 
    /// e.g. 0b1111100 and 0b10000000 returns 4 matching bits.
    /// </summary>
    /// <param name="a">The first BigFloat to compare to.</param>
    /// <param name="b">The second BigFloat to compare to.</param>
    /// <param name="sign">(out) Returns the sign of a-b. Example: If a is larger the sign is set to 1.</param>
    public static int NumberOfMatchingLeadingBits(BigFloat a, BigFloat b, out int sign)
    {
        if (a.Exponent > (b.Exponent + 1))
        {
            sign = (a.Sign == b.Sign) ? 1 : a.Sign;
            return -1;
        }

        if ((a.Exponent + 1) < b.Exponent)
        {
            sign = (a.Sign == b.Sign) ? -1 : a.Sign;
            return -1;
        }

        // only 1 bit or less size difference, so we could have a...
        //    0xFF/0x100 that would have 7 matching bits   difference b1
        //    0xF0/0x100 that would have 3 matching bits   difference b10000
        //   -0xF0/0x100 that would have 0 matching bits   difference b10000

        if (a.Sign != b.Sign)
        {
            sign = (a.Exponent > b.Exponent) ? a.Sign : -b.Sign;
            return -1;
        }

        BigInteger temp;
        int scaleDiff = a.Scale - b.Scale;
        int size;
        switch (scaleDiff)
        {
            case < 0:
                temp = (a._int << scaleDiff) - b._int;
                size = b._size;
                break;
            case > 0:
                temp = (a._int - b._int) >> scaleDiff;
                size = a._size;
                break;
            default:
                temp = a._int - b._int;
                size = a._size;
                break;
        }

        // special case as right shifting a negative number past its size will always be -1.
        sign = (temp != -1) ? temp.Sign : 0;

        return size - (int)BigInteger.Log2(temp);
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
            > 0 => new(((dividend._int << scaleDiff) % divisor._int) >> scaleDiff, dividend.Scale, true),
            < 0 => new((dividend._int % (divisor._int >> scaleDiff)) << scaleDiff, divisor.Scale, true),
            0 => new(dividend._int % divisor._int, divisor.Scale, true),
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

    // todo: test ~
    /// <summary>
    /// Performs a bit flip on all the bits in precision, basically intValue.
    /// </summary>
    public static BigFloat operator ~(BigFloat value)
    {
        return new(~value._int, value.Scale);
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
    public static BigFloat operator <<(BigFloat x, int shift) => new(x._int << shift, x.Scale, x._size + shift);

    // todo: test >>
    /// <summary>
    /// Right shift - Decreases the size by removing the least-signification bits. 
    /// i.e. The precision is reduced and effective value is reduced by 2^shift.
    /// No rounding is preformed. Scale is unaffected. 
    /// </summary>
    /// <param name="x">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    public static BigFloat operator >>(BigFloat x, int shift) => new(x._int >> shift, x.Scale, x._size - shift);

    public static BigFloat operator +(BigFloat r) => r;

    public static BigFloat operator -(BigFloat r) => new(-r._int, r.Scale, r._size);

    public static BigFloat operator ++(BigFloat r)
    {
        //return r + 1;  6/21/2023

        //int scaleDiff = r.Scale - 0;

        // hidden bits = 4
        // A)  1111|1111__.  = 1111|1111<< 6   +1  1111|1111__.
        // B)  1111|1111_.   = 1111|1111<< 5   +1  10000|0000#.
        // C)  1111|1111.    = 1111|1111<< 4   +1  10000|0000.
        // D)  1111|1.111    = 1111|1111<< 1   +1  10000|0.111
        // E)  1111.|1111    = 1111|1111<< 0   +1  10000.|1111
        // F)  111.1|1111    = 1111|1111<< -1  +1  1000.1|1111
        // G)  .1111|1111    = 1111|1111<< -4  +1  1.1111|1111
        // H) .01111|1111    = 1111|1111<< -5  +1  1.01111|1111

        // A
        if (r.Scale > ExtraHiddenBits + 1)
        {
            return r;
        }
        // B or C
        if (r.Scale > ExtraHiddenBits - 1)
        {
            BigInteger intVal = r._int + BigInteger.One;
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r.Scale, sizeVal);
        }
        // D-H  
        {
            BigInteger intVal = r._int + (BigInteger.One << (ExtraHiddenBits - r.Scale));
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r.Scale, sizeVal);
        }
    }

    public static BigFloat operator --(BigFloat r)
    {
        //todo: what is this?  and why "+"
        BigFloat res = r + 1;
        res.AssertValid();

        return r + 1;
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
            BigInteger intVal = r1._int + r2._int;
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();  // todo: maybe improve the performance on all the BigInteger.Abs(_int).GetBitLength()  everywhere
            return new BigFloat(intVal, r1.Scale, sizeVal);
        }
        else if (r1.Scale < r2.Scale)
        {
            BigInteger intVal = RightShiftWithRound(r1._int, -scaleDiff) + r2._int;
            int sizeVal = (int)BigInteger.Abs(intVal).GetBitLength();
            return new BigFloat(intVal, r2.Scale, sizeVal);
        }
        else // if (r1.Scale > r2.Scale)
        {
            BigInteger intVal = r1._int + RightShiftWithRound(r2._int, scaleDiff);
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
Other:                                         |   |         |         |       |               |
    P bool=WouldRound()                        | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(_int, ExtraHiddenBits);
    P bool=WouldRound(int bottomBitsRemoved)   | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(_int, bottomBitsRemoved);
    P bool=WouldRound(BI)                      | F | Rounds  | n/a     |  Yes  |ExtraHiddenBits| return WouldRound(bi, ExtraHiddenBits);
    P bool=WouldRound(BI,int bottomBitsRemove) | F | Rounds  | n/a     |  Yes  |    (param)    | return !(bi & ((BigInteger)1 << (bottomBitsRemoved - 1))).OutOfPrecision;

      
    * SetsSize = forces a particular size using the param (instead of add/removes the size)
    * Scale    = adjusts the size by a specific amt
    */

    //todo: add "TruncateAndRound()" above
    //todo: Where does BigInteger INT() fit in?

    /////////////////////////////////
    ////      WouldRound()
    /////////////////////////////////

    /// <summary>
    /// Checks to see if the integerPart would round-up if the ExtraHiddenBits were removed. 
    /// e.g. 11010101 with 3 bits removed would be 11011.
    /// </summary>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRound() => WouldRound(_int, ExtraHiddenBits);

    /// <summary>
    /// Checks to see if this integerPart would round-up given bottomBitsRemoved. 
    /// e.g. 11010101 with bottomBitsRemoved=3 would be 11011
    /// </summary>
    /// <param name="bottomBitsRemoved">The number of newSizeInBits from the least significant bit where rounding would take place.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public bool WouldRound(int bottomBitsRemoved) => WouldRound(_int, bottomBitsRemoved);

    /// <summary>
    /// Checks to see if this integerPart would round-up if ExtraHiddenBits are removed.
    /// </summary>
    /// <param name="bi">The BigInteger we would like check if it would round up.</param>
    /// <returns>Returns true if this integerPart would round away from zero.</returns>
    public static bool WouldRound(BigInteger bi) => WouldRound(bi, ExtraHiddenBits);

    private static bool WouldRound(BigInteger val, int bottomBitsRemoved)
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
    /// <param name="x">The _int part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x) => RightShiftWithRound(x, ExtraHiddenBits);

    /// <summary>
    /// Removes ExtraHiddenBits and rounds. It also requires the current size and will adjust it if it grows.
    /// </summary>
    /// <param name="x">The _int part where to remove ExtraHiddenBits and round.</param>
    private static BigInteger DataIntValueWithRound(BigInteger x, ref int size) => RightShiftWithRound(x, ExtraHiddenBits, ref size);

    /////////////////////////////////////////////
    ////      RightShift() for BigInteger    ////
    /////////////////////////////////////////////

    // Performance idea: what about doing:  rolledOver = (x == (1 << x.bitLen))   (do this before the inc for neg numbers and do this after the inc for pos numbers)
    // Performance idea: what about doing:  "(b & uint.MaxValue) == 0" first as a quick check. (or use x.IsPowerOfTwo)
    // Performance idea:  rolledOver = b.IsPowerOfTwo; 

    /// <summary>
    /// Removes x number of bits of precision. 
    /// A special case of RightShift(>>) that will round based off the most significant bit in the removed bits(bitsToRemove).
    /// This function will not adjust the scale. Like any shift, the value with be changed by some power of 2.
    /// Caution: a round up could cause the number to grow in size. (example: RightShiftWithRound(0b111, 1) --> 0b100
    /// Notes: 
    /// * Works on positive and negative numbers. 
    /// * If the part being removed has the most significant bit set, then the result will be rounded away from zero. 
    /// * THIS FUNCTION IS HIGHLY TUNED!
    /// </summary>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="bitsToRemove">The number of bits to reduce the precision.</param>
    /// <returns>The rounded result of shifting val to the right by bitsToRemove.</returns>
    public static BigInteger RightShiftWithRound(BigInteger val, int bitsToRemove) //put back to private  // todo: use "in" on bitsToRemove
    {
        //todo: What about this instead of the below?  (basically merge the two below into one)
        //if (val.Sign < 0) val--;
        //BigInteger temp = val >> (bitsToRemove - 1);
        //BigInteger result = temp >>= 1; // on .net 7 and later use >>> instead of >> for a performance boost
        //if (!temp.IsEven)  // on .net 7 and later use >>> instead of >> for a performance boost
        //    result++;
        //return result;

        // if bitsToRemove is negative, we would up-shift and no rounding is needed.
        if (bitsToRemove < 0)
        {
            return val >> bitsToRemove; ;
        }

        if (val.Sign >= 0)
        {
            BigInteger result = val >> bitsToRemove; // on .net 7 and later use >>> instead of >> for a performance boost

            if (!(val >> (bitsToRemove - 1)).IsEven)  // on .net 7 and later use >>> instead of >> for a performance boost
            {
                result++;
            }

            return result;
        }

        // BigInteger will automatically round when down-shifting larger negative values.
        //performance idea...if bits to remove is say 10 and there are zeros in it then val-- does nothing! (so skip this step)
        val--;

        BigInteger result2 = val >> bitsToRemove;

        if (!(val >> (bitsToRemove - 1)).IsEven)  // on .net 7 and later use >>> for a slight performance boost
        {
            result2++;
        }

        return result2;
    }

    /// <summary>
    /// Removes x number of bits of precision. It also requires the current size and will increment it if it grows by a bit.
    /// If the most significant bit of the removed bits is set, then the least significant bit will increment away from zero. 
    /// e.g. 1010010 << 2 = 10101
    /// Caution: Round-ups may percolate to the most significate bit, adding an extra bit in size.   
    /// THIS FUNCTION IS HIGHLY TUNED!
    /// </summary>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="bitsToRemove">The number of bits to reduce the precision.</param>
    /// <param name="size">IN: the size of Val.  OUT: The size of the output.</param>
    public static BigInteger RightShiftWithRound(BigInteger val, in int bitsToRemove, ref int size)
    {
        size = Math.Max(0, size - bitsToRemove);

        if (val.Sign >= 0)
        {
            BigInteger result = val >> bitsToRemove; // on .net 7 and later use >>> instead of >> for a slight performance boost

            if (!(val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                result++;

                if ((result >> size).IsOne)
                {
                    size++;
                }
            }
            return result;
        }

        // is Neg

        val--;

        BigInteger result2 = val >> bitsToRemove;

        if ((val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
        {
            if (((result2 - 1) >> size).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                size++;
            }
        }
        else
        {
            result2++;
        }

        return result2;
    }

    /// <summary>
    /// Removes x number of bits of precision.
    /// If the most significant bit of the removed bits is set, then the least significant bit will increment away from zero. 
    /// e.g. 1010010 << 2 = 10101
    /// Caution: Round-ups may percolate to the most significate bit. This function will automaticlly remove that extra bit. 
    /// e.g. 1111111 << 2 = 10000
    /// </summary>
    /// <param name="result">The result of val being right shifted and rounded. The size will be "size-bitsToRemove".</param>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="bitsToRemove">The number of bits that will be removed.</param>
    /// <param name="size">The size of the input value if available. If negative number then val.GetBitLength() is called.</param>
    /// <returns>Returns True if an additional bit needed to be removed to achieve the desired size because of a round up. 
    /// e.g. 1111111 << 2 = 10000</returns>
    public static bool RightShiftWithRoundWithOverflowDownsize(out BigInteger result, BigInteger val, in int bitsToRemove, int size = -1)
    {
        if (size < 0)
        {
            size = (int)val.GetBitLength();
        }

        size = Math.Max(0, size - bitsToRemove);

        if (val.Sign >= 0)
        {
            result = val >> bitsToRemove; // on .net 7 and later use >>> instead of >> for a slight performance boost

            if (!(val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                result++;

                if ((result >> size).IsOne)
                {
                    //rounded up to larger size so remove zero to keep it same size.
                    result >>= 1;
                    return true;
                }
                return false;
            }
        }
        else // is Neg
        {
            val--;

            result = val >> bitsToRemove;

            if ((val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                if (((result - 1) >> size).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
                {
                    result >>= 1;
                    return true;
                }
            }
            else
            {
                result++;
            }
        }

        return false;
    }

    ///////////////////////////////////////////////////
    ////      Set/Reduce Precision for BigFloat    ////
    ///////////////////////////////////////////////////

    /// <summary>
    /// Truncates a value by a specified number of bits by increasing the scale and reducing the precision.
    /// If the most significant bit of the removed bits is set then the least significant bit will increment away from zero. 
    /// e.g. 10.10010 << 2 = 10.101
    /// Caution: Round-ups may percolate to the most significate bit, adding an extra bit in size.   
    /// Example: 11.11 with 1 bit removed would result in 100.0 (the same size)
    /// This function uses the internal BigInteger RightShiftWithRound().
    /// </summary>
    /// <param name="bitsToRemove">Specifies the number of least-significant bits to remove.</param>
    public static BigFloat TruncateByAndRound(BigFloat x, int bitsToRemove)
    {
        if (bitsToRemove < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsToRemove), $"Param {nameof(bitsToRemove)}({bitsToRemove}) be 0 or greater.");
        }

        int newScale = x.Scale + bitsToRemove;
        int size = x._size;

        BigInteger b = RightShiftWithRound(x._int, bitsToRemove, ref size);

        return new(b, newScale, size);
    }


    public static BigFloat UpScale(BigFloat x, int upScaleAmount)
    {
        if (upScaleAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(upScaleAmount), $"Param {nameof(upScaleAmount)}({upScaleAmount}) be 0 or greater.");
        }

        return new BigFloat(x._int, x.Scale - upScaleAmount, x._size);
    }


    public static BigFloat DownScale(BigFloat x, int downScaleAmount)
    {
        if (downScaleAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(downScaleAmount), $"Param {nameof(downScaleAmount)}({downScaleAmount}) be 0 or greater.");
        }

        return new BigFloat(x._int, x.Scale + downScaleAmount, x._size);
    }

    /// <summary>
    /// Adjust the scale of a value
    /// </summary>
    /// <param name="x">The value who's scale should be adjusted.</param>
    /// <param name="changeScaleAmount">The amount to change the scale.</param>
    /// <returns>Returns a BigFloat with the updated scale.</returns>
    public static BigFloat AdjustScale(BigFloat x, int changeScaleAmount)
    {
        return new BigFloat(x._int, x.Scale + changeScaleAmount, x._size);
    }

    /// <summary>
    /// This function will reduce the precision of a BigInteger to the number of bits specified.
    /// If the part being removed has the most significant bit set, then the result will be rounded 
    /// away from zero. This can be used to reduce the precision prior to a large calculation.
    /// Caution: Since the value can be incremented during rounding the result can be newSizeInBits+1 in size. 
    /// Example: SetPrecisionWithRound(15, 3) = 8[4 bits]
    /// <param name="newSizeInBits">The new requested size. The resulting size might be rounded up.</param>
    public static BigInteger TruncateToAndRound(BigInteger x, int newSizeInBits)
    {
        if (newSizeInBits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSizeInBits), $"Param newSizeInBits({newSizeInBits}) be 0 or greater.");
        }
        int currentSize = (int)BigInteger.Abs(x).GetBitLength();
        BigInteger result = RightShiftWithRound(x, currentSize - newSizeInBits);
        return result;
    }

    /// <summary>
    /// Sets the precision(and accuracy) of a number by appending 0 bits if too small or cropping bits if too large.
    /// This can be useful for extending whole or rational numbers precision. 
    /// No rounding is performed.
    /// Example: SetPrecision(0b1101, 8) --> 0b11010000;  SetPrecision(0b1101, 3) --> 0b110
    /// Also see: ExtendPrecision, SetPrecisionWithRound
    /// </summary>
    /// <param name="x">The source BigFloat where a new size will be forced.</param>
    /// <param name="newSize">The number of zero bits to add.</param>
    /// <returns>A new BigFloat with the requested precision.</returns>
    public static BigFloat SetPrecision(BigFloat x, int newSize)
    {
        return new BigFloat(x._int << (newSize - x.Size), x.Scale + (x.Size - newSize), newSize + ExtraHiddenBits);
    }

    /// <summary>
    /// Reduces the precision of the a number but keeps the value the same.
    /// i.e. Down-shifts the value but and increases the scale. 
    /// Example: ReducePrecision(0b1101.1101, 3) --> 0b1101.1; 
    /// No rounding is performed.
    /// </summary>
    public static BigFloat ReducePrecision(BigFloat x, int reduceBy)
    {
        return new BigFloat(x._int >> reduceBy, x.Scale + reduceBy, x._size - reduceBy);
    }

    /// <summary>
    /// Reduces the precision to the new specified size. To help maintain the most significant digits, the bits are not simply cut off. 
    /// When reducing the least significant bit will rounded up if the most significant bit is set of the removed bits. 
    /// This can be used to reduce the precision of a number before prior to a calculation.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size.   
    /// Also see: SetPrecision
    /// </summary>
    /// <param name="newSizeInBits">The desired precision in bits.</param>
    public static BigFloat SetPrecisionWithRound(BigFloat x, int newSizeInBits)
    {
        int reduceBy = x.Size - newSizeInBits;
        BigFloat result = TruncateByAndRound(x, reduceBy); //todo: what about size when rolls over
        return result;
    }

    /// <summary>
    /// Extends the precision and accuracy of a number by appending 0 bits. 
    /// e.g. 1.1 --> 1.100000
    /// This can be useful for extending whole or rational numbers precision. 
    /// Also see: SetPrecision
    /// </summary>
    /// <param name="x">The source BigFloat that will be extended.</param>
    /// <param name="bitsToAdd">The number of zero bits to add. The number must be positive</param>
    /// <returns>Returns the larger value.</returns>
    public static BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
    {
        return bitsToAdd < 0
            ? throw new ArgumentOutOfRangeException(nameof(bitsToAdd), "cannot be a negative number")
            : new BigFloat(x._int << bitsToAdd, x.Scale + bitsToAdd, x._size + bitsToAdd);
    }

    public static BigFloat operator -(BigFloat r1, BigFloat r2)
    {
        //   "5555"00000   "1000010001110"00100000111100000    Value:4238  Shift:17  DecValue:5555 DecShift:5
        //    -"55577777"     -"110101000 00000110010110001"
        //  -------------     = 110101000 (3814)
        //     "49"9922223   <--- answer is 50, only 2 significant digits.

        if (r1.Scale == r2.Scale)
        {
            BigInteger bi = r1._int - r2._int;
            int size = Math.Max(0, (int)BigInteger.Abs(bi).GetBitLength());

            return new BigFloat(bi, r1.Scale, size);
        }
        else if (r1.Scale < r2.Scale)
        {
            //BigInteger bi = (r1._int >> (r2.Scale - r1.Scale)) - r2._int - (r1._int.Sign < 0 ? 1 : 0); //removed 12/4/2022
            BigInteger bi = (r1._int >> (r2.Scale - r1.Scale)) - r2._int; if (bi.Sign < 0)
            {
                bi--;
            }

            int size = Math.Max(0, (int)BigInteger.Abs(bi).GetBitLength());

            return new BigFloat(bi, r2.Scale, size);
        }
        else // if (r1.Scale > r2.Scale)
        {
            BigInteger bi = r1._int - (r2._int >> (r1.Scale - r2.Scale))
                - (r2._int.Sign < 0 ? 1 : 0);  //todo: do like above
            int size = Math.Max(0, (int)BigInteger.Abs(bi).GetBitLength());

            return new BigFloat(bi, r1.Scale, size);
        }
    }

    public static BigFloat PowerOf2(BigFloat val)
    {
        BigInteger prod = val._int * val._int;
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
            return new BigFloat(p2._int, p2.Scale - 1, p2._size);
        }

        // at this point it is oversized by at least 2

        //oversized by 2 then shrink input by 1
        //oversized by 3 then shrink input by 1
        //oversized by 4 then shrink input by 2
        //oversized by 5 then shrink input by 2

        int inputShink = (overSized + 1) / 2;

        BigInteger valWithLessPrec = val._int >> inputShink;

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

    // todo: check precision on this - I feel like it can be simplified and "int shouldBe = Math.Min(a.Size, b.Size) + ExtraHiddenBits;" is not right
    public static BigFloat operator *(BigFloat a, BigFloat b)
    {
        BigInteger prod;
        int shouldBe;
        const int SKIP_IF_SIZE_DIFF_SMALLER = 32;
        const int KEEP_EXTRA_PREC = 16;

        // todo: for performance what about no shift when _size's are around the same size. (like within 32) 
        // should be 42392656037190875851739737828733 but should be 42392656037190875851739737828732

        int sizeDiff = a._size - b._size;
        int shiftBy = Math.Max(0, Math.Abs(sizeDiff) - KEEP_EXTRA_PREC);

        // for size differences that are:
        //   0 to 31(SKIP_IF_SIZE_DIFF_SMALLER), no shift takes place (saves time on shift and increases precision on the LSB in rare cases)
        //   > 32, there is a shift of 16 or more (but size difference will be limited to 16 for extra precision)

        if (Math.Abs(sizeDiff) < SKIP_IF_SIZE_DIFF_SMALLER)
        {
            shiftBy = 0;
            prod = b._int * a._int;
            shouldBe = Math.Min(a._size, b._size);
        }
        else if (sizeDiff > 0)
        {
            prod = (a._int >> shiftBy) * b._int;
            shouldBe = b._size;
        }
        else //if (sizeDiff < 0)
        {
            prod = (b._int >> shiftBy) * a._int;
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
        if (divisor._int >> (divisor.Size - dividend.Size) <= dividend._int)
        {
            outputSize--;
        }

        // We need to oversize T (using left shift) so when we divide, it is the correct size.
        int wantedSizeForT = (1 * dividend.Size) + outputSize + ExtraHiddenBits;

        int leftShiftTBy = wantedSizeForT - divisor.Size;

        BigInteger leftShiftedT = divisor._int << leftShiftTBy; // rightShift used here instead of SetPrecisionWithRound for performance

        // Now we can just divide, and we should have the correct size
        BigInteger resIntPart = leftShiftedT / dividend._int;

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
    //todo: decimal support?
    /// <summary>Defines an explicit conversion of a System.Decimal object to a BigFloat.</summary>
    //public static explicit operator BigFloat(decimal input) => new BigFloat(input);

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned byte.</summary>
    public static explicit operator byte(BigFloat value)
    {
        return (byte)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed byte.</summary>
    public static explicit operator sbyte(BigFloat value)
    {
        return (sbyte)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 16-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ushort(BigFloat value)
    {
        return (ushort)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a 16-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator short(BigFloat value)
    {
        return (short)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 64-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator ulong(BigFloat value)
    {
        return (ulong)(value._int << (value.Scale - ExtraHiddenBits));

    }
    /// <summary>Defines an explicit conversion of a BigFloat to a 64-bit signed integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator long(BigFloat value)
    {
        return (long)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator UInt128(BigFloat value)
    {
        return (UInt128)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a signed 128-bit integer. 
    /// The fractional part (including ExtraHiddenBits) are simply discarded.</summary>
    public static explicit operator Int128(BigFloat value)
    {
        return (Int128)(value._int << (value.Scale - ExtraHiddenBits));
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
            return 0.0; // todo: maybe should return double.NaN?;
        }
        // Aline and move input.val to show top 53 bits then pre-append a "1" bit.
        // was: long mantissa = (long)(value._int >> (value._size - 53)) ^ ((long)1 << 52);

        long mantissa = (long)(BigInteger.Abs(value._int) >> (value._size - 53)) ^ ((long)1 << 52);
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
        dubAsLong &= 0x7FFFFFFFFFFFFFFF;  // todo: needed? - i think we can remove this
        if (value._int.Sign < 0)
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
        return value._int << (value.Scale - ExtraHiddenBits);
    }

    //todo: test below

    /// <summary>Defines an explicit conversion of a BigFloat to a single floating-point.
    /// Caution: Precision is not preserved since float is hard coded with 26 bits of precision.</summary>
    public static explicit operator float(BigFloat value)
    {
        // Future: handle Subnormal numbers (when the exponent field contains all 0's) for anything from 2.2250738585072014 × 10−308 up to 4.9406564584124654E-324.
        if (value.OutOfPrecision)
        {
            return 0.0f; // todo: maybe should return double.NaN?;
        }

        int mantissa = (int)(BigInteger.Abs(value._int) >> (value._size - 24)) ^ (1 << 23);
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
        singleAsInteger &= 0x7FFFFFFF;  // todo: needed? - i think we can remove this
        if (value._int.Sign < 0)
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
        return (int)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Defines an explicit conversion of a BigFloat to a unsigned 32-bit integer input. The fractional part (including hidden bits) are simply discarded.</summary>
    public static explicit operator uint(BigFloat value)
    {
        return (uint)(value._int << (value.Scale - ExtraHiddenBits));
    }

    /// <summary>Checks to see if a BigFloat's value would fit into a normalized double without the exponent overflowing or underflowing. 
    /// Since BigFloats can be any precision and doubles are 53-bits of precision, precision changes are ignored.</summary>
    public bool FitsInADouble() // todo: untested
    {
        // future (possibly): add denormalized support 
        return (Exponent + 1023 - 1) is not (<= 0 or > 2046);
    }

    /////////////////////////////////// COMPARE FUNCTIONS ////////////////////////////////////////////////////////

    /// <summary>Returns an input that indicates whether the current instance and a signed 64-bit integer have the same input.</summary>
    public bool Equals(long other)
    {
        //Todo: what about zero?
        if (Exponent > 64)
        {
            // 'this' is too large, not possible to be equal.
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

        return Scale >= 0 ? _int >> ExtraHiddenBits == other >> Scale : _int << (Scale - ExtraHiddenBits) == other;

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
        else if (_int.Sign < 0)
        {
            return false; // negative
        }

        return Scale >= 0 ? _int >> ExtraHiddenBits == other >> Scale : _int << (Scale - ExtraHiddenBits) == other;
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
        return CompareTo(other) == 0; //todo: to test
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
        return DataIntValueWithRound(_int).GetHashCode() ^ Scale;
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

        if (x._int == 0)
        {
            return new BigFloat((BigInteger)0, wantedPrecision, 0);
        }

        // Output should be (_int.GetBitLength()/2)+16 
        int totalLen = x.Scale + (x._size - ExtraHiddenBits);
        int needToShiftInputBy = (2 * wantedPrecision) - (x._size - ExtraHiddenBits) - (totalLen & 1);
        BigInteger intPart = NewtonPlusSqrt(x._int << (needToShiftInputBy + ExtraHiddenBits));
        int retShift = ((totalLen + (totalLen > 0 ? 1 : 0)) / 2) - wantedPrecision;

        BigFloat result = new(intPart, retShift, (int)intPart.GetBitLength());    //new version 2022-11-12
        return result;
    }

    // The world's fastest sqrt for C# and Java. 
    // https://www.codeproject.com/Articles/5321399/NewtonPlus-A-Fast-Big-Number-Square-Root-Function
    private static BigInteger NewtonPlusSqrt(BigInteger x)
    {
        if (x < 144838757784765629)    // 1.448e17 = ~1<<57
        {
            uint vInt = (uint)Math.Sqrt((ulong)x);
            if (x >= 4503599761588224 && (ulong)vInt * vInt > (ulong)x)  // 4.5e15 =  ~1<<52
            {
                vInt--;
            }
            return vInt;
        }

        double xAsDub = (double)x;
        if (xAsDub < 8.5e37)   //  long.max*long.max
        {
            ulong vInt = (ulong)Math.Sqrt(xAsDub);
            BigInteger v = (vInt + (ulong)(x / vInt)) >> 1;
            return v * v <= x ? v : v - 1;
        }

        if (xAsDub < 4.3322e127)
        {
            BigInteger v = (BigInteger)Math.Sqrt(xAsDub);
            v = (v + (x / v)) >> 1;
            if (xAsDub > 2e63)
            {
                v = (v + (x / v)) >> 1;
            }
            return v * v <= x ? v : v - 1;
        }

        int xLen = (int)x.GetBitLength();
        int wantedPrecision = (xLen + 1) / 2;
        int xLenMod = xLen + (xLen & 1) + 1;

        //////// Do the first Sqrt on hardware ////////
        long tempX = (long)(x >> (xLenMod - 63));
        double tempSqrt1 = Math.Sqrt(tempX);
        ulong valLong = (ulong)BitConverter.DoubleToInt64Bits(tempSqrt1) & 0x1fffffffffffffL;
        if (valLong == 0)
        {
            valLong = 1UL << 53;
        }

        ////////  Classic Newton Iterations ////////
        BigInteger val = ((BigInteger)valLong << 52) + ((x >> (xLenMod - (3 * 53))) / valLong);
        int size = 106;
        for (; size < 256; size <<= 1)
        {
            val = (val << (size - 1)) + ((x >> (xLenMod - (3 * size))) / val);
        }

        if (xAsDub > 4e254) // 4e254 = 1<<845.76973610139
        {
            int numOfNewtonSteps = BitOperations.Log2((uint)(wantedPrecision / size)) + 2;

            //////  Apply Starting Size  ////////
            int wantedSize = (wantedPrecision >> numOfNewtonSteps) + 2;
            int needToShiftBy = size - wantedSize;
            val >>= needToShiftBy;
            size = wantedSize;
            do
            {
                ////////  Newton Plus Iterations  ////////
                int shiftX = xLenMod - (3 * size);
                BigInteger valSqrd = (val * val) << (size - 1);
                BigInteger valSU = (x >> shiftX) - valSqrd;
                val = (val << size) + (valSU / val);
                size *= 2;

            } while (size < wantedPrecision);
        }

        /////// There are a few extra digits here, let's save them. ///////
        int oversizedBy = size - wantedPrecision;

        ////////  Shrink result to wanted Precision  ////////
        val >>= oversizedBy;

        return val;
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
        BigInteger resIntPart = one / x._int;

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
            double valAsDouble = (double)new BigFloat(value._int, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)

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
            value = new BigFloat(value._int, value.Scale + (removedExp * exponent), true);

            return value;
        }

        // the expectedFinalPrecision >= 53 bits and Power >= 3, so pretty big.

        // for each bit in the exponent, we need to multiply in 2^position
        int powerBitCount = BitOperations.Log2(pwr) + 1;

        // First Loop
        BigFloat product = ((pwr & 1) == 1) ? value : BigFloat.OneWithPrecision(value.Size);
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

    //future: Create BigFloat version of PowMostSignificantBits()
    /// <summary>
    /// Returns the top n bits for a BigInteger raised to a power. 
    /// If wantedBits is not specified the output precision will match the input value's precision. 
    /// The number of removed bits are returned in in totalShift. 
    /// The result left shifted by totalShift would return the expanded result.
    /// The result is rounded using the top most removed bit. 
    /// The result is occasionally rounded up/down in marginal cases.  least significant bit  in value may not be _______________
    /// </summary>
    /// <param name="val">The input value.</param>
    /// <param name="valSize">The input values size.</param>
    /// <param name="exp">The exponent to raise the value by.</param>
    /// <param name="totalShift">(out) The number of bits that were removed from the result.</param>
    /// <param name="wantedBits">The number of bits to return.</param>
    /// <returns>The top bits val raised to the power of exp.</returns>
    public static BigInteger PowMostSignificantBits(BigInteger val, int valSize, int exp, out int totalShift, int wantedBits = 0)
    {
        if (valSize == 0)
        {
            valSize = (int)val.GetBitLength();
        }

        totalShift = 0;

        if (((long)exp * valSize) >= int.MaxValue)
        {
            throw new OverflowException("Overflow: The output 'totalShift' is not large enough. exp * size");
        }

        int expSz = BitOperations.Log2((uint)exp) + 1;

        int workingSize;
        if (wantedBits == 0)
        {
            wantedBits = valSize;
        }

        if (wantedBits > 0)
        {
            // Odds of an incorrect round-up(ex: 7.50001 not rounding up to 8) ~= 18.12/(2^ExtraBits)
            //   0=18.1%; 1=9.1%; 2=4.5%; 3=2.3%; 4=1.1%; 5=0.6%; 8=0.07%
            workingSize = wantedBits + expSz + 8/*extraPrecisionBits(adjustable)*/;
        }
        else
        {
            wantedBits = -wantedBits;

            // This version is more accurate but create a slower function. There is just one known incident when it does not round up like it should.
            // JUST ONE KNOWN ROUND ERROR between 16 to 20 is 51^17938 (goes away above 20)
            //   searches @16: (1-2000)^(2-39,999), (1-126,000)^(2-3999), (1-134,654,818)^(1-1500)
            workingSize = (2 * wantedBits) + expSz + 22/*extraPrecisionBits*/;
        }

        if (exp < 3)
        {
            switch (exp)
            {
                case 0:
                    return BigInteger.One; //totalShift = 0
                case 1:
                    return val; //totalShift = 0
                case 2:
                    BigInteger sqr = val * val;
                    //totalShift = valSize - ((square >> ((2 * valSize) - 1) > 0) ? 0 : 1);
                    //return RightShiftWithRound(square, totalShift);

                    int sqrSize = (2 * valSize) - ((sqr >> ((2 * valSize) - 1) > 0) ? 0 : 1);

                    //RightShiftWithRoundWithOverflowDownsize(out BigInteger result, BigInteger inVal, in int bitsToRemove, int size)

                    _ = RightShiftWithRoundWithOverflowDownsize(out BigInteger result, sqr, wantedBits, sqrSize);
                    return result;

                default: // negative exp would be less then 1 (unless 1)
                    return val != 1 ? BigInteger.Zero : val.Sign;
            }
        }

        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if ((wantedBits > 2) && (wantedBits < 53) && (valSize * exp) < 3807)
        {
            //// Lets first make sure we would have some precision remaining after our exponent operation.
            if (valSize == 0)
            {
                return BigInteger.Zero; // technically more of a "NA".
            }

            // 1) create a double with the bits. 
            // Aline input to the top 53 bits then pre-append a "1" bit.
            long inMantissa = (long)(BigInteger.Abs(val) << (53 - valSize));
            long dubAsLong = inMantissa | ((long)1023 << 52);
            double normInput = BitConverter.Int64BitsToDouble(dubAsLong);

            // 2) perform a power
            double normPow = double.Pow(normInput, exp);
            if (normPow == double.PositiveInfinity)
            {
                throw new OverflowException($"Internal Error: PositiveInfinity valSize:{valSize} exp:{exp} val:{val} wantedBits:{wantedBits}");
            }

            // 3) extract "bottom 52 bits" and that is our answer.
            long bits = BitConverter.DoubleToInt64Bits(normPow);
            long outMantissa = bits & 0xfffffffffffffL;
            int outExp = (int)(bits >> 52) /*& 0x7ffL*/;  // should be either 1023(so, +0) 1024(so, +1)  1025(so, +2) // todo:maybe do not need 7ffL
            outMantissa |= 0x10000000000000L;
            int _scale = outExp - 1023;
            int bitsToDrop = 53 - wantedBits;  // wantedBits OR size????
            long mask1 = ((long)1 << bitsToDrop) - 1;  // OR ((long)1 << (53 - size)) - 1  ?????

            // no known issues if not all removed bits are 1 or val < 13511613
            if ((~(outMantissa & mask1)) >= 0 || val < 13511613)
            {
                totalShift = ((valSize - 1) * (exp - 1)) + _scale + (valSize - wantedBits)  /*+ (1<<(expSz-2))*/;  // wantedBits OR size????
                return RightShiftWithRound(outMantissa, bitsToDrop);
            }
        }

        // First Loop
        BigInteger product = ((exp & 1) > 0) ? val : 1;
        BigInteger powerPostShift = val;
        int shiftSum = 0;
        int shift = 0;

        // Second Loop
        BigInteger pwrPreShift = powerPostShift * powerPostShift;
        int prdSize = (valSize * 2) - (((pwrPreShift >> ((valSize * 2) - 1)) > 0) ? 0 : 1);
        int H = valSize + prdSize;  //OR  size + shift

        int J = ((exp & 0x1) == 1) ? 0 : valSize;
        int I = 0;

        powerPostShift = pwrPreShift;
        if ((exp & 0x2) > 0)
        {
            I = H - workingSize;
            int shrinkSize = I - J;
            J = 0;
            product = (product * powerPostShift) >> shrinkSize;
            totalShift += shrinkSize;
        }
        else
        {
            J += prdSize;
        }

        // for each bit in the exponent, we need to multiply in 2^position
        for (int i = 2; i < expSz; i++)
        {
            pwrPreShift = powerPostShift * powerPostShift;

            // checks if a leading bit resulted from the multiply and if so adds it.
            int tmp = ((prdSize - shift) * 2) - 1;
            prdSize = tmp + (int)(pwrPreShift >> tmp);

            shift = Math.Max(prdSize - workingSize, 0);
            H += prdSize - shift - I;

            //powerPostShift = RightShiftWithRound(pwrPreShift, shift);  ///better precision by 1.7 buts but 25% slower
            powerPostShift = pwrPreShift >> shift; // 25% faster; 5 times more round errors; always one direction(good thing)

            shiftSum = (shiftSum * 2) + shift;
            bool bit = ((exp >> i) & 1) == 1;
            if (bit)
            {
                I = H - workingSize;
                int shrinkSize = I - J;
                J = 0;
                product = (product * powerPostShift) >> shrinkSize;
                totalShift += shrinkSize + shiftSum;
            }
            else
            {
                I = 0;
                J += prdSize - shift;  //OR  shift OR prdSize - shift
            }
        }
        int productSize = (int)product.GetBitLength();
        int bitsToRemove = productSize - wantedBits;

        totalShift += bitsToRemove;

        //BigInteger res =  RightShiftWithRound(product, bitsToRemove, ref productSize); // 

        bool carry = RightShiftWithRoundWithOverflowDownsize(out BigInteger res, product, bitsToRemove, productSize);
        if (carry)
        {
            totalShift++;
        }

        return res;
    }



    // 10/17/2023
    public static BigFloat NthRootDRAFT7(BigFloat value, int root)
    {
        bool rootIsNeg = root < 0;
        if (rootIsNeg)
        {
            root = -root;
        }

        bool resultIsPos = value._int.Sign > 0;
        if (!resultIsPos)
        {
            value = -value;
        }

        resultIsPos = resultIsPos || ((root & 1) == 0);

        // Check if Value is zero.
        if (value._int.Sign == 0)
        {
            return BigFloat.ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots...
        switch (root)
        {
            case 0:
                return OneWithPrecision(value.Size);
            case 1:
                return resultIsPos ? value : -value;
            //case 2:
            //    return resultIsPos ? Sqrt(value) : -Sqrt(value);
                //case 4:
                //    return resultIsPos ? Sqrt(Sqrt(value)) : -Sqrt(Sqrt(value));
        }

        //int xLen = value._size;
        int rootSize = BitOperations.Log2((uint)root);
        int wantedPrecision = (int)BigInteger.Log2(value._int) + rootSize; // for better accuracy for small roots add: "+ rootSize / Math.Pow(( root >> (rootSize - 3)), root) - 0.5"



        //BigInteger val;

        ////////// Lets remove value's scale (and just leave the last bit so scale is 0 or 1) ////////
        int removedScale = value.Scale & ~1;
        int newScale = value.Scale - removedScale;

        ////////// Use double's hardware to get the first 53-bits ////////
        //long tempX = (long)(value._int >> (value._size - 52 /*- newScale*/ +22));
        ////////////////////////////////////////////////////////////////////////////
        long mantissa = (long)(BigInteger.Abs(value._int) >> (value._size - 53)) ^ ((long)1 << 52);
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
        ulong tempVal = bits & 0x1fffffffffffffL | (1UL << 52);
        int tempExp = (int)((bits >> 52) & 0x7ffL) - 1023 - 20;
        newScale += tempExp;


        // If 53 bits enough precision, lets use that and return.
        //if (value._size < 53)
        //{  //  Shrink result to wanted Precision
        //    int shrinkAmt = (53 - value._size);
        //    BigFloat newVal = new BigFloat(tempVal >> shrinkAmt, newScale + shrinkAmt, value._size);
        //    return newVal;
        //}


        BigInteger x_B_Value = tempVal;
        int x_B_Scale = newScale;


        BigInteger root_B_Val = (BigInteger)root << value.Size;
        BigInteger root_B_Scale = -30;


        // B: (BigInteger Version)
        BigInteger ttp = PowMostSignificantBits(x_B_Value, 53, root - 1, out int totalShift, 53);
        BigInteger bot_B_Val = (root_B_Val * ttp) >> 32;
        BigInteger bot_B_Scale = 1; //  totalShift;
        BigInteger tbp = PowMostSignificantBits(x_B_Value, 53, root, out totalShift, 53);
        BigInteger top_B_Val = tbp - (value._int >> (value._size - 53));
        BigInteger top_B_Scale = value._size - 53; //totalShift;
        // 101101110001101011111111111111111111111111111111110111000000000000000000000000000000    6442450943999995x2^32
        // 10110111000110101111111111111111111111111111111111100                                   6442450943999995x2

        //BigInteger top_B_Val2;

        x_B_Scale -= 40;  //TEMP
        x_B_Value <<= 40; //TEMP

        while (top_B_Val > 23) 
        {
            BigInteger oldX_B_Value = x_B_Value;
            BigInteger oldX_B_Scale = x_B_Scale;

            BigInteger tb_B_Value = (top_B_Val / bot_B_Val);
            BigInteger tb_B_Scale = 0; //??

            x_B_Value -= tb_B_Value;
            //x_B_Scale = ;

            //Console.WriteLine($"{oldX_B_Value}<<{oldX_B_Scale} - ({top_B_Val }<<{top_B_Scale} / {bot_B_Val,3 }<<{bot_B_Scale,3}) = left - {tb_B_Value,3}<<{tb_B_Scale,3} = {x_B_Value}<<{x_B_Scale}");

            int extra = 16;

            BigInteger ttp2 = PowMostSignificantBits(x_B_Value, 53, root - 1, out int totalShift2, 53 + extra);
            bot_B_Val = (root_B_Val * ttp2) >> (32 + extra);
            BigInteger bot_B_Scale2 = 1;

            BigInteger tbp2 = PowMostSignificantBits(x_B_Value, 53, root, out totalShift2, 53 + extra);
            //top_B_Val = tbp2 - (value._int >> (value._size - 53));
            top_B_Val = (tbp2 - (value._int >> (value._size - 53 - extra))) >> extra;

            BigInteger top_B_Scale2 = value._size - 53;

            //Console.WriteLine($"{oldX_B_Value}<<{oldX_B_Scale} - ({top_B_Val}<<{top_B_Scale} / {bot_B_Val,3}<<{top_B_Scale,3}) = left - {tb_B_Value,3}<<{tb_B_Scale,3} = {x_B_Value}<<{x_B_Scale}");
            //Console.WriteLine($"Ans: {new BigFloat(x_B_Value,x_B_Scale,true)} ({x_B_Value} << {x_B_Scale})");
            //Console.WriteLine();
        }

        //Console.WriteLine($"Ans: {new BigFloat(x_B_Value, x_B_Scale, true)} ({x_B_Value} << {x_B_Scale})");

        return new BigFloat(x_B_Value, x_B_Scale, true);

    }





    public static BigFloat NthRootDRAFT6(BigFloat value, int root)
    {
        bool rootIsNeg = root < 0;
        if (rootIsNeg)
        {
            root = -root;
        }

        bool resultIsPos = value._int.Sign > 0;
        if (!resultIsPos)
        {
            value = -value;
        }

        resultIsPos = resultIsPos || ((root & 1) == 0);

        // Check if Value is zero.
        if (value._int.Sign == 0)
        {
            return BigFloat.ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots...
        switch (root)
        {
            case 0:
                return OneWithPrecision(value.Size);
            case 1:
                return resultIsPos ? value : -value;
            case 2:
                return resultIsPos ? Sqrt(value) : -Sqrt(value);
                //case 4:
                //    return resultIsPos ? Sqrt(Sqrt(value)) : -Sqrt(Sqrt(value));
        }

        //int xLen = value._size;
        int rootSize = BitOperations.Log2((uint)root);
        _ = (int)BigInteger.Log2(value._int) + rootSize; // for better accuracy for small roots add: "+ rootSize / Math.Pow(( root >> (rootSize - 3)), root) - 0.5"

        //BigInteger val;

        ////////// Lets remove value's scale (and just leave the last bit so scale is 0 or 1) ////////
        int removedScale = value.Scale & ~1;
        int newScale = value.Scale - removedScale;

        ////////// Use double's hardware to get the first 53-bits ////////
        //long tempX = (long)(value._int >> (value._size - 52 /*- newScale*/ +22));
        ////////////////////////////////////////////////////////////////////////////
        long mantissa = (long)(BigInteger.Abs(value._int) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.Exponent + 1023 - 1;// + 52 -4;

        // if exp is oversized for double we need to pull out some exp:
        if (Math.Abs(value.Exponent) > 1021) // future: using 1021(not 1022) to be safe
        {
            // old:
            //   (1)Pre:  pre=(value<<(preShift*root))
            //   (2)Root: result=pre^(1/root)
            //   (3)post: result/(1<<s)
            // new:
            //   (1)Pre   pre=(value>>preShift)
            //   (2)Root: result=pre^(1/root)
            //   (3)post: result/(2^(-preShift/root)

            //double finalDiv = Math.Pow(2,-value.Exponent/root);
            exp = 0;
        }
        double dubVal = BitConverter.Int64BitsToDouble(mantissa | (exp << 52));
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // todo: what about just casing from BigFloat to double?
        //double test = Math.Log2(dubVal); //Math.Log2((double)tempX);
        double tempRoot = Math.Pow(dubVal, 1.0 / root);  //Math.Pow(tempX, 1.0/root)
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(tempRoot);
        ulong tempVal = (bits & 0x1fffffffffffffL) | (1UL << 52);
        int tempExp = (int)((bits >> 52) & 0x7ffL) - 1023 - 20;
        newScale += tempExp;

        // If 53 bits enough precision, lets use that and return.
        if (value._size < 53)
        {  //  Shrink result to wanted Precision
            int shrinkAmt = 53 - value._size;
            BigFloat newVal = new(tempVal >> shrinkAmt, newScale + shrinkAmt, value._size);
            return newVal;
        }

        // start with approximate guess
        BigFloat x = new(tempVal, newScale, 53);  // What about when tempVal ix 0xFFFFFFF
                                                  //BigFloat x = new((tempVal>> 49) <<49, newScale, 53);  // use for DEBUGGING below

        // get a proper sized "root" (only needed for BigFloat version)
        BigFloat rt = new((BigInteger)root << value.Size, -value.Size);

        // Init the "b" and "t" for "oldX - (t / b)"
        BigFloat b = rt * Pow(x, root - 1);
        BigFloat t = Pow(x, root) - value;

        while (t._size > 3) //(!t.OutOfPrecision)
        {
            BigFloat oldX = x;

            BigFloat tb = t / b;
            x -= tb;
            Console.WriteLine($"{oldX} - ({t} / {b}) = {oldX} - {tb} = {x - tb}");

            b = rt * Pow(x, root - 1);
            t = Pow(x, root) - value;
        }

        return x;
    }


    [Conditional("DEBUG")]
    private void AssertValid()
    {
        int realSize = (int)BigInteger.Abs(_int).GetBitLength();

        // Make sure size is set correctly. Zero is allowed to be any size.
        Debug.Assert(_size == realSize, $"_size({_size}), expected ({realSize})");
    }

    [Conditional("DEBUG")]
    private static void AssertValid(BigFloat val)
    {
        int realSize = (int)BigInteger.Abs(val._int).GetBitLength();

        // Make sure size is set correctly. Zero is allowed to be any size.
        Debug.Assert(val._size == realSize, $"_size({val._size}), expected ({realSize})");
    }
}