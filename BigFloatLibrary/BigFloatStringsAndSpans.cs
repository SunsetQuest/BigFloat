// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
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
        // When Scale is non-negative, shift off the extra guard bits and format.
        if (Scale >= 0)
        {
            return (Mantissa >> (GuardBits - Scale)).ToString("X");
        }
        else
        {
            // For negative Scale, we must align to a 4‑bit boundary so that the radix point falls correctly.
            int rightShift = (GuardBits - Scale) & 0x03;
            BigInteger rounded = RightShiftWithRound(Mantissa, rightShift);
            string hex = rounded.ToString("X");
            // Insert the radix point at the appropriate position.
            int insertIndex = (-Scale / 4) - 1;
            return hex.Insert(insertIndex, ".");
        }
    }

    /// <summary>
    /// Converts this BigFloat to a binary string.
    /// </summary>
    public string ToBinaryString()
    {
        // Allocate exactly as much space as needed.
        Span<char> buffer = stackalloc char[CalculateBinaryStringLength()];
        WriteBinaryToSpan(buffer, out int charsWritten);
        return new string(buffer[..charsWritten]);
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

    /// <summary>
    /// Computes the total number of characters required for the binary representation.
    /// </summary>
    private int CalculateBinaryStringLength()
    {
        // Future: what if we just add a few instead of calculating. Also with last update it is now Aprox size.
        bool isNeg = Mantissa.Sign < 0;
        int size = _size - GuardBits
            + Math.Max(Math.Max(Scale, -(_size - GuardBits) - Scale), 0)// out-of-precision zeros in the output.
            + (Scale < 0 ? 1 : 0)                                       // add one if it has a point like (1.1)
            + (isNeg ? 1 : 0)                                           // add one if a leading '-' sign (-0.1)
            + 1                                                         // add one in case rollover
            + (BinaryExponent <= 0 ? 1 : 0);                            // add one if <1 for leading Zero (0.1) 
        return size;
    }

    /// <summary>
    /// Writes the binary representation into the provided span.
    /// </summary>
    /// <param name="destination">Destination span to write into.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    private void WriteBinaryToSpan(Span<char> destination, out int charsWritten)
    {
        int pos = 0;

        // Write a leading sign if needed.
        if (Mantissa.Sign < 0)
        {
            destination[pos++] = '-';
        }

        // Get the absolute value as a byte array (after rounding off guard bits).
        int size = _size;
        ReadOnlySpan<byte> bytes = RightShiftWithRound(BigInteger.Abs(Mantissa), GuardBits, ref size).ToByteArray();

        // Compute the number of leading zeros in the most significant byte.
        int msbLeadingZeros = BitOperations.LeadingZeroCount(bytes[^1]) - 24;

        int binaryExponent = Scale + size - 1;

        // Three cases:
        //   Type '0.110' - all bits are to the right of the radix point. (has leading '0.' or '-0.')
        //   Type '11010' - if all bits are to the left of the radix point(no radix point required)
        //   Type '11.01' - has bits below AND above the point.

        // Special cases: '0.999' - like 0.123 above, however because of rounding, has leading '1.' or '-1.'

        if (binaryExponent < 0) // Type '0.110'
        {
            // For numbers less than one, prepend "0." and any extra zeros.
            destination[pos++] = '0';
            destination[pos++] = '.';

            int zerosCount = Math.Max(0, -size - Scale);
            for (int i = 0; i < zerosCount; i++)
            {
                destination[pos++] = '0';
            }
            pos += WriteBits(bytes, msbLeadingZeros, size, destination[pos..]);
        }
        else if (Scale >= 0) // Type '11010'
        {
            // For integer numbers (no radix point) write the bits...
            pos += WriteBits(bytes, msbLeadingZeros, size, destination[pos..]);
            // ...and then append any trailing zeros.
            int trailingZeros = Math.Max(0, Scale);
            destination.Slice(pos, trailingZeros).Fill('0');
            pos += trailingZeros;
        }
        else // Type '11.01'
        {
            // For numbers with a fractional part, split the bits before and after the radix point.
            int bitsBeforePoint = size + Scale; // Scale is negative
            int bitsAfterPoint = Math.Max(0, -Scale);

            pos += WriteBits(bytes, msbLeadingZeros, bitsBeforePoint, destination[pos..]);
            destination[pos++] = '.';
            pos += WriteBits(bytes, msbLeadingZeros + bitsBeforePoint, bitsAfterPoint, destination[pos..]);
        }

        charsWritten = pos;
    }

    /// <summary>
    /// Writes a specified number of bits from the source bytes (starting at a bit offset) into the destination span.
    /// Returns the number of characters written.
    /// </summary>
    /// <param name="bytes">The source bytes (little‑endian representation of the absolute value).</param>
    /// <param name="bitStart">The starting bit offset (from the most significant bit).</param>
    /// <param name="bitCount">The number of bits to write.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    private static int WriteBits(ReadOnlySpan<byte> bytes, int bitStart, int bitCount, Span<char> destination)
    {
        int written = 0;
        int byteIndex = bytes.Length - 1; // Most significant byte is at the end
        int bitIndex = bitStart;
        while (written < bitCount)
        {
            int curByteIndex = byteIndex - (bitIndex >> 3);
            int curBit = 7 - (bitIndex & 7);
            byte b = bytes[curByteIndex];
            destination[written] = (char)('0' + ((b >> curBit) & 1));
            written++;
            bitIndex++;
        }
        return written;
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
        BigInteger intVal = val.Mantissa;
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
            BigInteger power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + GuardBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {
                if (RightShiftWithRound(intVal, GuardBits).IsZero)
                {
                    return $"0.{new string('0', decimalDigits)}";
                }

                // future: The below should not be needed.
                //// solves an issue when a "BigFloat(1, -8)" being 0.000
                decimalDigits++;
                power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);
                power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + GuardBits);
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
            return MantissaWithoutGuardBits(intVal << scale).ToString();
        }

        // 7XXXXX or 7e+10 - at this point we the number have a positive exponent. e.g no decimal point
        int maskSize = (int)((scale + 2.5) / LOG2_OF_10); // 2.5 is adjustable 
        BigInteger resUnScaled = (intVal << (scale - maskSize)) / BigInteger.Pow(5, maskSize);

        // Applies the scale to the number and rounds from bottom bit
        BigInteger resScaled = RightShiftWithRound(resUnScaled, GuardBits);
        
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
        BigInteger intVal = val.Mantissa;
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
            BigInteger power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + GuardBits);

            // If zero, then special handling required. Add as many precision zeros based on scale.
            if (power5Scaled.IsZero)
            {

                // future: The below should not be needed.
                //// solves an issue when a "BigFloat(1, -8)" being 0.000
                decimalDigits++;
                power5 = BigInteger.Abs(intVal) * BigInteger.Pow(5, decimalDigits);
                power5Scaled = RightShiftWithRound(power5, -scale - decimalDigits + GuardBits);
            }

            string numberText = power5Scaled.ToString();

            
            return $"{(intVal.Sign < 0 ? "-" : "")}{numberText}e-{decimalDigits}";
        }

        // 7XXXXX or 7e+10 - at this point we the number have a positive exponent. e.g no decimal point
        int maskSize = (int)((scale + 2.5) / LOG2_OF_10); // 2.5 is adjustable 
        BigInteger resUnScaled = (intVal << (scale - maskSize)) / BigInteger.Pow(5, maskSize);

        // Applies the scale to the number and rounds from bottom bit
        BigInteger resScaled = RightShiftWithRound(resUnScaled, GuardBits);

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

        BigInteger intVal = Mantissa;
        if (!showInTwosComplement && Mantissa.Sign < 0)
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
        BigInteger abs = BigInteger.Abs(Mantissa);
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
        return BigIntegerTools.ToBinaryString(Mantissa, twosComplement ? 
            BinaryStringFormat.TwosComplement : 
            BinaryStringFormat.Standard);
    }

    /// <summary>
    /// Returns the value's data bits as a string. GuardBits are not included and result is rounded.
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetBitsAsString()
    {
        BigInteger shiftedMantissa = RightShiftWithRound(Mantissa, GuardBits);
        return BigIntegerTools.ToBinaryString(shiftedMantissa);
    }

    public string DebuggerDisplay
    {
        get
        {
            string bottom8HexChars = (BigInteger.Abs(Mantissa) & ((BigInteger.One << GuardBits) - 1)).ToString("X8").PadLeft(8)[^8..];
            StringBuilder sb = new(32);
            _ = sb.Append($"{ToString(true)}, "); //  integer part using ToString()
            _ = sb.Append($"{(Mantissa.Sign >= 0 ? " " : "-")}0x{BigInteger.Abs(Mantissa) >> GuardBits:X}|{bottom8HexChars}"); // hex part
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
        Console.WriteLine($"    |Dec : {Mantissa >> GuardBits}{((double)(Mantissa & (((ulong)1 << GuardBits) - 1)) / ((ulong)1 << GuardBits)).ToString()[1..]} {shift}");
        Console.WriteLine($"    |Dec : {Mantissa >> GuardBits}:{Mantissa & (((ulong)1 << GuardBits) - 1)} {shift}");  // decimal part (e.g. .75)
        if (Mantissa < 0)
        {
            Console.WriteLine($"   or -{-Mantissa >> GuardBits:X4}:{(-Mantissa & (((ulong)1 << GuardBits) - 1)).ToString("X8")[^8..]}");
        }

        Console.WriteLine($"    |Bits: {Mantissa}");
        Console.WriteLine($"   Scale : {Scale}");
        Console.WriteLine();
    }
}
