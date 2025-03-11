﻿// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

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
    private string ToHexString()
    {
        // When Scale is non-negative, shift off the extra hidden bits and format.
        if (Scale >= 0)
        {
            return (DataBits >> (ExtraHiddenBits - Scale)).ToString("X");
        }
        else
        {
            // For negative Scale, we must align to a 4‑bit boundary so that the radix point falls correctly.
            int rightShift = (ExtraHiddenBits - Scale) & 0x03;
            BigInteger rounded = RightShiftWithRound(DataBits, rightShift);
            string hex = rounded.ToString("X");
            // Insert the radix point at the appropriate position.
            int insertIndex = (-Scale / 4) - 1;
            return hex.Insert(insertIndex, ".");
        }
    }

    /// <summary>
    /// Converts this BigFloat to a binary string.
    /// </summary>
    private string ToBinaryString()
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
    private int CalculateBinaryStringLength() => _size - ExtraHiddenBits
            + Math.Max(Math.Max(Scale, -(_size - ExtraHiddenBits) - Scale), 0) // out-of-precision zeros in the output.
            + (DataBits.Sign < 0 ? 1 : 0)       // add one if a leading '-' sign (-0.1)
            + (Scale < 0 ? 1 : 0)               // add one if it has a point like (1.1)
            + (BinaryExponent <= 0 ? 1 : 0);    // add one if <1 for leading Zero (0.1) 

    /// <summary>
    /// Writes the binary representation into the provided span.
    /// </summary>
    /// <param name="destination">Destination span to write into.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    private void WriteBinaryToSpan(Span<char> destination, out int charsWritten)
    {
        int pos = 0;

        // Write a leading sign if needed.
        if (DataBits.Sign < 0)
        {
            destination[pos++] = '-';
        }

        // Get the absolute value as a byte array (after rounding off hidden bits).
        int size = _size;
        ReadOnlySpan<byte> bytes = RightShiftWithRound(BigInteger.Abs(DataBits), 32, ref size).ToByteArray();

        // Compute the number of leading zeros in the most significant byte.
        int msbLeadingZeros = BitOperations.LeadingZeroCount(bytes[^1]) - 24;

        int binaryExponent = Scale + size - 1;

        // Three cases
        //   Type '0.123' - all numbers are to the right of the radix point. (has leading '0.' or '-0.')
        //   Type '12300' - if all bits are to the left of the radix point(no radix point required)
        //   Type '12.30' - has numbers below AND above the point. (e.g. 11.01)

        // Special cases: '0.999' - like 0.123 above, however because of rounding, has leading '1.' or '-1.'

        if (binaryExponent < 0)
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
        else if (Scale >= 0) // Type '12300'
        {
            // For integer numbers (no radix point) write the bits...
            pos += WriteBits(bytes, msbLeadingZeros, size, destination[pos..]);
            // ...and then append any trailing zeros.
            int trailingZeros = Math.Max(0, Scale);
            destination.Slice(destination.Length - trailingZeros, trailingZeros).Fill('0');
            pos += trailingZeros;
        }
        else // Type '12.30'
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
    ///   The .NET format string, e.g. "G", "R", "X", "B", or a custom pattern.
    /// </param>
    /// <param name="formatProvider">
    ///   Culture or number-format info (ignored in this simple example).
    /// </param>
    /// <returns>A string representation of this BigFloat.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        // For the simplest usage, treat a null/empty format as "G".
        // You might also treat "R" and "G" similarly. .NET double does that.
        // "R" often implies round-trip decimal format, "G" is general, etc.
        if (string.IsNullOrEmpty(format))
            format = "G";

        // You can do a simple switch:
        return format.ToUpperInvariant() switch
        {
            // Typically "R" means Round-trip, so you might want
            // maximum decimal digits. For now, call your existing
            // decimal version:
            "G" or "R" => ToStringDecimal(this, includeOutOfPrecisionBits: false),

            "X" => ToHexString(),

            "B" => ToBinaryString(),

            // Future: "N", "F", "E"...

            // Future: For truly custom numeric format strings, you'd parse `format`.
            _ => ToStringDecimal(this, includeOutOfPrecisionBits: false),
        };
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
    /// Generates the data-bits in hex followed by the amount to shift(in decimal). Example: 12AC<<22 or B1>>3
    /// </summary>
    /// <param name="showHiddenBits">Includes the extra 32 hidden bits. Example: 12AC|F0F00000<<22</param>
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
    /// Returns the value's data bits as a string. ExtraHiddenBits are not included and result is rounded.
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetBitsAsString()
    {
        return BigIntegerToBinaryString(UnscaledValue);
    }

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
    public void DebugPrint(string? varName = null)
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
}
