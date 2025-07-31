// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok was used in the development of this library.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;
#nullable enable

[DebuggerDisplay("{DebuggerDisplay}")]
public readonly partial struct BigFloat : IFormattable, ISpanFormattable
{
    // see "BigFloatToStringNotes.txt" and "BigFloatTryParseNotes.txt" for additional notes
    //   string ToString() - calls ToStringDecimal()
    //   string ToString(string format) - to Hex(e.g. A4B.F2) and Binary(e.g. 1010111.001)
    //   string ToStringDecimal() - To Decimal, e.g. 9999.99
    //   string ToStringHexScientific(bool showGuardBits = false, bool showSize = false, bool showInTwosComplement = false) - e.g. "12AC<<22"

    [DebuggerHidden()]
    public override string ToString()
    {
        return ToStringDecimal(this, false);
    }

    [DebuggerHidden()]
    public string ToString(bool includeOutOfPrecisionBits = false, bool showGuard = false)
    {
        return ToStringDecimal(this, includeOutOfPrecisionBits, showGuard);
    }

    /// <summary>
    /// Writes a BigFloat in Hex ('X') or Binary ('B'). A radix point is supported. Negative values must have a leading '-'.
    /// </summary>
    /// <param name="format">
    /// Format specifier: 'X' for hexadecimal, 'B' for binary, or empty for base‑10 decimal.
    /// </param>
    /// <returns>The value as a string.</returns>
    public string ToString(string format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return ToString(); // default decimal conversion
        }

        // Use an invariant upper-case switch to select the conversion.
        return format.ToUpperInvariant() switch
        {
            "X" => ToHexString(),
            "B" => ToBinaryString(),
            _ => throw new FormatException($"The {format} format string is not supported.")
        };
    }

    /// <summary>
    /// Converts this BigFloat to a hexadecimal string.
    /// </summary>
    public string ToHexString()
    {
        // --- 1.  ‘binExp’  tells us whether we have a fraction at all -----------
        int binExp = Scale - GuardBits;          // =  power‑of‑two exponent

        // a) nothing after the radix point, no rounding necessary
        if (binExp >= 0)
            return (_mantissa << binExp).ToString("X");

        // b) still an integer, but part of the 32 guard bits ends up in it
        if (Scale > -2)
            return BigIntegerTools.RoundingRightShift(_mantissa, -binExp).ToString("X");
        
        // --- 2.  real fraction ---------------------------------------------------
        // how many *hex* digits contain real information?
        int fracNibbles = (-Scale + 2) >> 2;     // == ceil(−Scale / 4)

        // bits we must drop *before* rounding (always ≥ 0)
        int shift = GuardBits - Scale - (fracNibbles << 2);

        // --- 3.  build the textual representation -------------------------------
        string hex = RoundingRightShift(_mantissa, shift).ToString("X");

        // make sure we have at least ‘fracNibbles’ characters to slice
        if (hex.Length <= fracNibbles)
            hex = new string('0', fracNibbles - hex.Length + 1) + hex;

        int dotPos = hex.Length - fracNibbles;
        hex = hex.Insert(dotPos, ".");

        // strip trailing zeros in the fraction; drop the dot if nothing is left
        return hex.TrimStart('0');
    }

    /// <summary>
    /// Implements ISpanFormattable-style formatting for BigFloat.
    /// Allows writing the formatted result directly into a span.
    /// </summary>
    /// <param name="destination">Destination span into which to write.</param>
    /// <param name="charsWritten">Number of characters written.</param>
    /// <param name="format">A format specifier (if empty, default decimal conversion is used).</param>
    /// <param name="provider">Format provider (ignored in this implementation).</param>
    /// <returns>True if the formatting fits in the destination span; otherwise, false.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (format.IsEmpty)
        {
            // Default decimal formatting.
            string s = ToString();
            if (s.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }
            s.AsSpan().CopyTo(destination);
            charsWritten = s.Length;
            return true;
        }

        char fmt = char.ToUpperInvariant(format[0]);
        switch (fmt)
        {
            case 'X':
                {
                    string hex = ToHexString();
                    if (hex.Length > destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }
                    hex.AsSpan().CopyTo(destination);
                    charsWritten = hex.Length;
                    return true;
                }
            case 'B':
                {
                    int required = CalculateBinaryStringLength();
                    if (required > destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }
                    WriteBinaryToSpan(destination, out charsWritten);
                    return true;
                }
            default:
                throw new FormatException($"The {format.ToString()} format string is not supported.");
        }
    }

    public void WriteBinaryToSpan(Span<char> destination, out int charsWritten, int numberOfGuardBitsToInclude = 0)
    {
        numberOfGuardBitsToInclude = Math.Clamp(numberOfGuardBitsToInclude, 0, GuardBits);
        charsWritten = 0;

        // Handle zero mantissa special case
        bool isZero = (_size == 0) || (_size <= (GuardBits - numberOfGuardBitsToInclude));

        if (isZero)
        {
            destination[charsWritten++] = '0';

            // Add decimal point and trailing zeros if scale is negative
            int zerosToWrite = numberOfGuardBitsToInclude - Scale;
            if (zerosToWrite > 0)
            {
                destination[charsWritten++] = '.';
                for (int i = 0; i < zerosToWrite; i++)
                {
                    destination[charsWritten++] = '0';
                }
            }
            return;
        }

        // Work with absolute value
        var absMantissa = BigInteger.Abs(_mantissa);

        int totalBits = _size;

        // Key insight: Scale determines how bits are interpreted
        // Scale = 0: Real bits are whole, guard bits are fractional
        // Scale > 0: More bits become whole (shift left)
        // Scale < 0: Some real bits become fractional (shift right)

        // Calculate which bits are whole vs fractional
        int wholeBitCount = totalBits + (Scale - GuardBits);

        // Determine output range based on guard bit inclusion
        int outputStart = totalBits - 1;
        int outputEnd = GuardBits - numberOfGuardBitsToInclude;

        // Check if we have any bits to output
        if (outputStart < outputEnd)
        {
            destination[charsWritten++] = '0';
            return;
        }

        // Handle sign
        if (_mantissa.Sign < 0)
        {
            destination[charsWritten++] = '-';
        }

        // Determine if we need a decimal point
        bool hasFracPart = wholeBitCount < totalBits && outputEnd < totalBits - wholeBitCount;

        // Write whole part
        if (wholeBitCount <= 0)
        {
            destination[charsWritten++] = '0';
        }
        else
        {
            // Output whole bits
            int wholeStart = outputStart;
            //int wholeEnd = Math.Max(outputEnd, totalBits - wholeBitCount);
            int wholeEnd = Math.Max(0, totalBits - wholeBitCount);

            if (wholeStart >= wholeEnd)
            {
                for (int pos = wholeStart; pos >= wholeEnd; pos--)
                {
                    destination[charsWritten++] = GetBit(absMantissa, pos);
                }
            }

            // Add trailing zeros if scale extends beyond total bits
            if (wholeBitCount > totalBits)
            {
                int trailingZeros = wholeBitCount - totalBits;
                for (int i = 0; i < trailingZeros; i++)
                {
                    destination[charsWritten++] = '0';
                }
            }
        }

        // Write fractional part
        if (hasFracPart)
        {
            destination[charsWritten++] = '.';

            // Add leading zeros if needed
            if (wholeBitCount < 0)
            {
                int leadingZeros = -wholeBitCount;
                for (int i = 0; i < leadingZeros; i++)
                {
                    destination[charsWritten++] = '0';
                }
            }

            // Output fractional bits
            int fracStart = Math.Min(outputStart, totalBits - wholeBitCount - 1);
            int fracEnd = outputEnd;

            if (fracStart >= fracEnd)
            {
                for (int pos = fracStart; pos >= fracEnd; pos--)
                {
                    destination[charsWritten++] = GetBit(absMantissa, pos);
                }
            }
        }
    }

    private char GetBit(BigInteger value, int bitPosition)
    {
        if (bitPosition < 0 || bitPosition >= _size) return '0';
        return ((value >> bitPosition) & 1) == 1 ? '1' : '0';
    }

    public string ToBinaryString(bool includeGuardBits = false)
    {
        int guardBitsToInclude = includeGuardBits ? 32 : 0;
        int bufferSize = CalculateBinaryStringLength(guardBitsToInclude);
        Span<char> buffer = stackalloc char[bufferSize];
        WriteBinaryToSpan(buffer, out int charsWritten, guardBitsToInclude);
        return new string(buffer[..charsWritten]);
    }

    public string ToBinaryString(int numberOfGuardBitsToInclude)
    {
        numberOfGuardBitsToInclude = Math.Clamp(numberOfGuardBitsToInclude, 0, 32);
        int bufferSize = CalculateBinaryStringLength(numberOfGuardBitsToInclude);
        Span<char> buffer = stackalloc char[bufferSize];
        WriteBinaryToSpan(buffer, out int charsWritten, numberOfGuardBitsToInclude);
        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Computes the total number of characters required for the binary representation.
    /// </summary>
    private int CalculateBinaryStringLength(int numberOfGuardBitsToOutput = 0)
    {
        numberOfGuardBitsToOutput = int.Clamp(numberOfGuardBitsToOutput, 0, GuardBits);
        //numberOfGuardBitsToOutput = int.Clamp(Scale - GuardBits, 0, 32);
        int guardBitsToHide = GuardBits - numberOfGuardBitsToOutput;

        int size = 0;
        size += (_mantissa.Sign < 0) ? 1 : 0;    // add one if a leading '-' sign (-1.1)
        size += 1;                               // add one in case rollover (only needed if rounding is enabled)

        //if (((_size == 0) || (_size + Scale < guardBitsToHide))) //is zero
        if ((_size == 0) || (_size <= 32 && numberOfGuardBitsToOutput == 0)) //is zero
        {
            //size += Math.Max(0, numberOfGuardBitsToOutput - Scale) + 1; //zerosToWrite and +1 for point
            size += Math.Max(0, numberOfGuardBitsToOutput - Scale) + 1; //+1 for point

            //size += Math.Max(0,-Scale) + 1; //+1 for point
            if (Scale < 0) size++; //+1 for leading zero in '0.'
        }
        else if ((Scale- numberOfGuardBitsToOutput) >= 0)  // is Integer  (maybe subtract "GuardBits-numberOfGuardBitsToOutput)?????
        {
            size += _size - (GuardBits - numberOfGuardBitsToOutput); //the bits themselves
            size += Scale - numberOfGuardBitsToOutput; // trailing bits that are either GuardBits in the whole number range and/or 0 bits when GuardBits are exhausted. 
        }
        else if (BinaryExponent <= 0) // numbers in the form: 0.1
        {
            size += _size - guardBitsToHide - BinaryExponent + 2;   // leading zeros, 0.00000 and +2 for leading '0.'
        }
        else  // numbers in the form: 1.1
        {
            size += _size - (GuardBits - numberOfGuardBitsToOutput) + 1; // +1 for '.'
        }
        return size;
    }


    /// <summary>
    /// Provides custom format-string support for BigFloat.
    /// This is the standard .NET entry point when users call
    /// <c>bigFloat.ToString("X", someProvider)</c>, etc.
    /// </summary>
    /// <param name="format">
    ///   The .NET format string, e.g. "G", "R", "X", "B", "E".
    /// </param>
    /// <param name="formatProvider">
    ///   Culture or number-format info (ignored in this implementation).
    /// </param>
    /// <returns>A string representation of this BigFloat.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        // For the simplest usage, treat a null/empty format as "G".
        // "R" often implies round-trip decimal format, "G" is general, etc.
        if (string.IsNullOrEmpty(format)) { format = "G"; }

        return format.ToUpperInvariant() switch
        {
            // Typically "R" means Round-trip, "G" is general - both use decimal representation
            "G" or "R" => ToStringDecimal(this, includeGuardBits: false),

            "X" => ToHexString(),

            "B" => ToBinaryString(),

            "E" => ToStringExponential(this, includeGuardBits: false),

            // Default to decimal representation for unknown specifiers
            _ => ToStringDecimal(this, includeGuardBits: false),
        };
    }

    /// <summary>
    /// Format the value of the current instance to a decimal number.
    /// </summary>
    /// <param name="val">The BigFloat that should be converted to a string.</param>
    /// <param name="includeGuardBits">Include out-of-precision bits in result. This will include additional decimal places.</param>
    /// <param name="digitMaskingForm">Allows for the format of 77XXXXX to be used. Trailing digits are replaced with placeholders indicating out-of-precision.</param>
    //[DebuggerHidden()]
    public static string ToStringDecimal(BigFloat val, bool includeGuardBits = false, bool digitMaskingForm = false)
    {
        BigInteger intVal = val._mantissa;
        int scale = val.Scale;
        int valSize = val._size;

        if (includeGuardBits)
        {
            intVal <<= GuardBits;
            scale -= GuardBits;
            valSize += GuardBits;
        }

        if (scale < -1)
        {
            // Number will have a decimal point. (e.g. 222.22, 0.01, 3.1)
            // -1 is not enough to form a full decimal digit.

            // Get the number of places that should be returned after the decimal point.
            int decimalDigits = -(int)((scale - 1.5) / LOG2_OF_10);

            BigInteger power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);

            // Applies the scale to the number and rounds from bottom bit
            BigInteger power5Scaled = RoundingRightShift(power5, -scale - decimalDigits + GuardBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {
                if (RoundingRightShift(intVal, GuardBits).IsZero)
                {
                    return $"0.{new string('0', decimalDigits)}";
                }

                // future: The below should not be needed.
                //// solves an issue when a "BigFloat(1, -8)" being 0.000
                decimalDigits++;
                power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);
                power5Scaled = RoundingRightShift(power5, -scale - decimalDigits + GuardBits);
            }

            string numberText = power5Scaled.ToString();

            int decimalOffset = numberText.Length - decimalDigits;
            //int decimalOffset2 = ((int)((_size - GuardBits + scale2) / LOG2_OF_10)) - ((numberText[0] - '5') / 8.0);  //alternative

            // 0.0000000000000000000#####
            if (decimalOffset < -10) 
            {
                return $"{(intVal.Sign < 0 ? "-" : "")}{numberText}e-{decimalDigits}";
            }

            int exponent = scale + valSize - GuardBits;

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

        // #########.  - check to see if we have an integer, if so, no Pow(5) scaling required
        if (scale < 1)
        {
            return GetRoundedMantissa(intVal << scale).ToString();
        }

        // 7XXXXX or 7e+10 - at this point we the number have a positive exponent. e.g no decimal point
        int maskSize = (int)((scale + 2.5) / LOG2_OF_10); // 2.5 is adjustable 
        BigInteger resUnScaled = (intVal << (scale - maskSize)) / BigInteger.Pow(5, maskSize);

        // Applies the scale to the number and rounds from bottom bit
        BigInteger resScaled = RoundingRightShift(resUnScaled, GuardBits);
        
        // #########e+NN   ->  want  D.DDDDDDe+MMM
        // maskSize is your "raw" decimal exponent,
        // resScaled is the integer part you’ve currently computed.

        string number = resScaled.ToString();
        // handle negative values
        bool isNegative = number[0] == '-';
        string digits = isNegative ? number[1..] : number;

        // if we’re in the "use X placeholders" case, leave that untouched:
        if (digitMaskingForm && maskSize <= 10)
        {
            // e.g. "12345XXXXX"
            return (isNegative ? "-" : "") + digits + new string('X', maskSize);
        }

        // otherwise, normalized scientific notation:
        int numDigits = digits.Length;                   // total significant digits we have
        int adjustedExponent = maskSize + (numDigits - 1);

        // build mantissa: first digit, then "." + rest (if any)
        string mantissa = digits[0]
                         + (numDigits > 1
                                ? string.Concat(".", digits.AsSpan(1))
                                : "");

        // final string: [−]D.ddddDe+EEE
        return (isNegative ? "-" : "")
             + mantissa
             + "e+"
             + adjustedExponent;
    }

    /// <summary>
    /// Converts this BigFloat to exponential (scientific) notation.
    /// Forces exponential representation regardless of magnitude.
    /// </summary>
    /// <param name="val">The BigFloat that should be converted to a string.</param>
    /// <param name="includeGuardBits">Include out-of-precision bits in result. This will include additional decimal places.</param>
    //[DebuggerHidden()]
    public static string ToStringExponential(BigFloat val, bool includeGuardBits = false)
    {
        BigInteger intVal = val._mantissa;
        int scale = val.Scale;

        if (includeGuardBits)
        {
            intVal <<= GuardBits;
            scale -= GuardBits;
        }

        // 0.##### or ####.######
        if (scale < -1)
        {
            // Number will have a decimal point. (e.g. 222.22, 0.01, 3.1)
            // -1 is not enough to form a full decimal digit.

            // Get the number of places that should be returned after the decimal point.
            int decimalDigits = -(int)((scale - 1.5) / LOG2_OF_10);

            BigInteger power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);

            // Applies the scale to the number and rounds from bottom bit
            BigInteger power5Scaled = RoundingRightShift(power5, -scale - decimalDigits + GuardBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {

                // future: The below should not be needed.
                //// solves an issue when a "BigFloat(1, -8)" being 0.000
                decimalDigits++;
                power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);
                power5Scaled = RoundingRightShift(power5, -scale - decimalDigits + GuardBits);
            }

            string numberText = power5Scaled.ToString();

            
            return $"{(intVal.Sign < 0 ? "-" : "")}{numberText}e-{decimalDigits}";
        }

        // 7XXXXX or 7e+10 - at this point we the number have a positive exponent. e.g no decimal point
        int maskSize = (int)((scale + 2.5) / LOG2_OF_10); // 2.5 is adjustable 
        BigInteger resUnScaled = (intVal << (scale - maskSize)) / BigInteger.Pow(5, maskSize);

        // Applies the scale to the number and rounds from bottom bit
        BigInteger resScaled = RoundingRightShift(resUnScaled, GuardBits);

        // #########e+NN   ->  want  D.DDDDDDe+MMM
        // maskSize is your "raw" decimal exponent,
        // resScaled is the integer part you’ve currently computed.

        string number = resScaled.ToString();
        // handle negative values
        bool isNegative = number[0] == '-';
        string digits = isNegative ? number[1..] : number;

        // otherwise, normalized scientific notation:
        int numDigits = digits.Length;                   // total significant digits we have
        int adjustedExponent = maskSize + (numDigits - 1);

        // build mantissa: first digit, then "." + rest (if any)
        string mantissa = digits[0]
                         + (numDigits > 1
                                ? string.Concat(".", digits.AsSpan(1))
                                : "");

        // final string: [−]D.ddddDe+EEE
        return (isNegative ? "-" : "")
             + mantissa
             + "e+"
             + adjustedExponent;
    }

    /// <summary>
    /// Generates the data-bits in hex followed by the amount to shift(in decimal). Example: 12AC<<22 or B1>>3
    /// </summary>
    /// <param name="includeGuardBits">Includes the extra 32 hidden guard bits. Example: 12AC|F0F00000<<22</param>
    /// <param name="showSize">Appends a [##] to the number with it's size in bits. Example: 22AC[14]<<22</param>
    /// <param name="showInTwosComplement">When enabled, shows the show result in two's complement form with no leading sign. Example: -5 --> B[3]<<0</param>
    public string ToStringHexScientific(bool includeGuardBits = false, bool showSize = false, bool showInTwosComplement = false)
    {
        StringBuilder sb = new();

        BigInteger intVal = _mantissa;
        if (!showInTwosComplement && _mantissa.Sign < 0)
        {
            _ = sb.Append('-');
            intVal = -intVal;
        }
        _ = sb.Append($"{intVal >> GuardBits:X}");
        if (includeGuardBits)
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
        BigInteger abs = BigInteger.Abs(_mantissa);
        int shiftAmount = _size - numberOfBits;
        return shiftAmount >= 0
            ? BigIntegerTools.ToBinaryString(abs >> shiftAmount)
            : BigIntegerTools.ToBinaryString(abs) + new string('_', -shiftAmount);
    }

    /// <summary>
    /// Returns the value's bits, including extra hidden guard bits, as a string. 
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetAllBitsAsString(bool twosComplement = false)
    {
        return BigIntegerTools.ToBinaryString(_mantissa, twosComplement ? 
            BinaryStringFormat.TwosComplement : 
            BinaryStringFormat.Standard);
    }

    /// <summary>
    /// Returns the value's data bits as a string. GuardBits are not included and result is rounded.
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetBitsAsString()
    {
        BigInteger shiftedMantissa = RoundingRightShift(_mantissa, GuardBits);
        return BigIntegerTools.ToBinaryString(shiftedMantissa);
    }

    public string DebuggerDisplay
    {
        get
        {
            string bottom8HexChars = (BigInteger.Abs(_mantissa) & ((BigInteger.One << GuardBits) - 1)).ToString("X8").PadLeft(8)[^8..];
            StringBuilder sb = new(32);
            _ = sb.Append($"{ToString(true)}, "); //  integer part using ToString()
            _ = sb.Append($"{(_mantissa.Sign >= 0 ? " " : "-")}0x{BigInteger.Abs(_mantissa) >> GuardBits:X}|{bottom8HexChars}"); // hex part
            //_ = sb.Append($"{ToBinaryString()}"); // hex part
            if (_size > GuardBits)  { _ = sb.Append($"[{_size - GuardBits}+{GuardBits} GuardBits]"); }
            if (_size == GuardBits) { _ = sb.Append($"[{GuardBits}]"); }
            if (_size < GuardBits)  { _ = sb.Append($"[{_size} - Out Of Precision!]"); }

            _ = sb.Append($" << {Scale}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Prints debug information for the BigFloat to the console.  
    /// </summary>
    /// <param name="varName">Prints an optional name of the variable.</param>
    public void DebugPrint(string? varName = null)
    {
        string shift = $"{((Scale >= 0) ? "<<" : ">>")} {Math.Abs(Scale)}";
        if (!string.IsNullOrEmpty(varName))
        {
            Console.WriteLine($"{varName + ":"}");
        }

        Console.WriteLine($"   Debug : {DebuggerDisplay}");
        Console.WriteLine($"  String : {ToString()}");
        //Console.WriteLine($"  Int|hex: {DataBits >> GuardBits:X}:{(DataBits & (uint.MaxValue)).ToString("X")[^8..]}[{Size}] {shift} (Guard-bits round {(WouldRound() ? "up" : "down")})");
        Console.WriteLine($" Int|Hex : {ToStringHexScientific(true, true, false)} (Guard-bits round {(WouldRoundUp() ? "up" : "down")})");
        Console.WriteLine($"    |Hex : {ToStringHexScientific(true, true, true)} (two's comp)");
        Console.WriteLine($"    |Dec : {_mantissa >> GuardBits}{((double)(_mantissa & (((ulong)1 << GuardBits) - 1)) / ((ulong)1 << GuardBits)).ToString()[1..]} {shift}");
        Console.WriteLine($"    |Dec : {_mantissa >> GuardBits}:{_mantissa & (((ulong)1 << GuardBits) - 1)} {shift}");  // decimal part (e.g. .75)
        if (_mantissa < 0)
        {
            Console.WriteLine($"   or -{-_mantissa >> GuardBits:X4}:{(-_mantissa & (((ulong)1 << GuardBits) - 1)).ToString("X8")[^8..]}");
        }

        Console.WriteLine($"    |Bits: {_mantissa}");
        Console.WriteLine($"   Scale : {Scale}");
        Console.WriteLine();
    }
}
