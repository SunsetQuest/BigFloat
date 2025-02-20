// Copyright Ryan Scott White. 2020-2025
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
public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
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
    /// <param name="format">Format specifier: 'X' for hex, 'B' for binary, or empty for base-10 decimal.</param>
    /// <returns>The value as a string.</returns>
    public string ToString(string format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return ToString();
        }

        //// Lets round and remove the ExtraHiddenBits now.
        //BigInteger newInt = DataIntValueWithRound(BigInteger.Abs(DataBits), out bool needToRound);
        //int size = (int)newInt.GetBitLength();
        //int newScale = Scale;

        if (format.Equals("X", StringComparison.InvariantCultureIgnoreCase)) //hex with radix point
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

        if (format.Equals("B", StringComparison.InvariantCultureIgnoreCase)) // Signals a binary (with radix point)
        {
            //future: can we use BigIntegerToBinaryString() instead?

            // Setup destination and allocate memory
            Span<char> dstBytes = stackalloc char[_size - ExtraHiddenBits
            + Math.Max(Math.Max(Scale, -(_size - ExtraHiddenBits) - Scale), 0) // total number of out-of-precision zeros in the output.
                + (DataBits.Sign < 0 ? 1 : 0)   // add one if a leading '-' sign (-0.1)
                + (Scale < 0 ? 1 : 0)       // add one if it has a point like (1.1)
                + (BinaryExponent < 0 ? 1 : 0)];  // add one if <1 for leading Zero (0.1) 
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

            if (BinaryExponent < 0)  // For binary numbers less then one. (e.g. 0.001101)
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

                //Write the point
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
        switch (format.ToUpperInvariant())
        {
            case "G":
            case "R":
                // Typically "R" means Round-trip, so you might want
                // maximum decimal digits. For now, call your existing
                // decimal version:
                return ToStringDecimal(this, includeOutOfPrecisionBits: false);

            case "X":
                // Hex representation, ignoring formatProvider for now:
                // You already have "ToString(string format)" overload below,
                // so you could just do:
                return this.ToString("X");


            case "B":
                // Binary representation:
                return this.ToString("B");
                //todo: use something like the below
                //return BigIntegerToBinaryString()

            // Future: "N", "F", "E"...
 
            default:
                // For truly custom numeric format strings, you'd parse
                // `format` here. Or just fallback:
                return ToStringDecimal(this, includeOutOfPrecisionBits: false);
        }
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
    /// Returns the value's bits as a string. 
    /// Negative values will have a leading '-' sign.
    /// </summary>
    public string GetBitsAsString()
    {
        return BigIntegerToBinaryString(UnscaledValue);
    }
    /////////////////////////// [END] TO_STRING FUNCTIONS [END] ////////////////////////////////
}
