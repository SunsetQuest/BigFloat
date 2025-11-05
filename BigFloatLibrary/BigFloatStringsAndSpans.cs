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
        return ToString(format, null);
    }

    /// <summary>
    /// Converts this BigFloat to a hexadecimal string with radix point.
    /// </summary>
    public string ToHexString(bool includeGuardBits = false)
    {
        int binExp = Scale - GuardBits;       // power-of-two exponent after guard
        if (!includeGuardBits && binExp >= 0)
            return (_mantissa << binExp).ToString("X");

        // If we are NOT including guard bits and the fractional part is < 1 hex digit,
        // round to an integer and return without a dot.
        if (!includeGuardBits && binExp > -4)
            return BigIntegerTools.RoundingRightShift(_mantissa, -binExp).ToString("X");

        // --- fractional formatting ---
        // base fractional nibbles from Scale (ceil(-Scale/4))
        int fracNibbles = (-Scale + 2) >> 2;
        if (fracNibbles < 0) fracNibbles = 0;

        // when including guard bits, expose them as additional hex digits
        int guardNibbles = includeGuardBits ? (GuardBits + 3) >> 2 : 0;
        int totalFracNibbles = fracNibbles + guardNibbles;

        // How many bits to drop before printing (≥0 ⇒ right shift, <0 ⇒ left shift)
        int shift = GuardBits - Scale - (totalFracNibbles << 2);

        System.Numerics.BigInteger shown =
            shift >= 0
                ? (includeGuardBits ? (_mantissa >> shift)            // no rounding
                                    : BigIntegerTools.RoundingRightShift(_mantissa, shift))
                : (_mantissa << -shift);                              // extend with zeros

        string hex = shown.ToString("X").TrimStart('0');

        // ensure we can insert the dot
        if (hex.Length <= totalFracNibbles)
            hex = new string('0', totalFracNibbles - hex.Length + 1) + hex;

        int dotPos = hex.Length - totalFracNibbles;
        hex = hex.Insert(dotPos, ".");

        if (!includeGuardBits)
        {
            // trim trailing zeros after the dot; drop dot if empty fraction
            int end = hex.Length;
            while (end > dotPos + 1 && hex[end - 1] == '0') end--;
            if (end > dotPos && hex[end - 1] == '.') end--;
            hex = hex.Substring(0, end);
        }

        // normalize leading zero before dot
        if (hex.StartsWith(".")) hex = "0" + hex;
        return hex;
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
    public bool TryFormat(Span<char> destination, out int charsWritten,
                          ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (format.IsEmpty)
        {
            var s = ToString();
            if (s.Length > destination.Length) { charsWritten = 0; return false; }
            s.AsSpan().CopyTo(destination);
            charsWritten = s.Length;
            return true;
        }

        char fmt = char.ToUpperInvariant(format[0]);
        switch (fmt)
        {
            case 'X':
                var hex = ToHexString();
                if (hex.Length > destination.Length) { charsWritten = 0; return false; }
                hex.AsSpan().CopyTo(destination);
                charsWritten = hex.Length;
                return true;

            case 'B':
                int required = CalculateBinaryStringLength();
                if (required > destination.Length) { charsWritten = 0; return false; }
                WriteBinaryToSpan(destination, out charsWritten);
                return true;

            case 'E':
            case 'G':
            case 'R':
            default:
                // Fallback to IFormattable semantics
                var s = ToString(format.ToString(), provider);
                if (s.Length > destination.Length) { charsWritten = 0; return false; }
                s.AsSpan().CopyTo(destination);
                charsWritten = s.Length;
                return true;
        }
    }


    public void WriteBinaryToSpan(Span<char> destination, out int charsWritten, int numberOfGuardBitsToInclude = 0, bool showGuardBitsSeparator = false)
    {
        numberOfGuardBitsToInclude = Math.Clamp(numberOfGuardBitsToInclude, 0, GuardBits);
        charsWritten = 0;

        // --- Common fast-path info
        int totalBits = _size;
        int outputStart = totalBits - 1;                                 // inclusive
        int outputEnd = GuardBits - numberOfGuardBitsToInclude;         // inclusive

        // "Zero" per original semantics
        bool isZero = (totalBits == 0) || (totalBits <= (GuardBits - numberOfGuardBitsToInclude));

        // Sign only for non-zero
        if (!isZero && _mantissa.Sign < 0) destination[charsWritten++] = '-';

        // Helper state for streaming
        bool sepNeeded = showGuardBitsSeparator;
        bool sepWritten = false;
        int bitIndex = 0;     // counts only '0'/'1' written (ignoring '.' and '|')

        // Local inline to maybe write '|' before the next bit
        void MaybeWriteSeparator(int sepIdx, Span<char> destination, ref int charsWritten)
        {
            if (sepNeeded && !sepWritten && bitIndex == sepIdx)
            {
                destination[charsWritten++] = '|';
                sepWritten = true;
            }
        }

        // ZERO CASE --------------------------------------------------------------------
        if (isZero)
        {
            // Bit structure: whole '0' then (optional) fractional zeros
            int zerosToWrite = Math.Max(0, numberOfGuardBitsToInclude - Scale); // if > 0 we show '.' and that many '0's

            if (_mantissa.IsZero && _size == 0 && Scale == -GuardBits && zerosToWrite > numberOfGuardBitsToInclude)
            {
                zerosToWrite = numberOfGuardBitsToInclude;
            }

            int baseBitChars = 1 + (zerosToWrite > 0 ? zerosToWrite : 0);

            // Separator index among bit chars (before any extra-leading zeros)
            int sepIndex = int.MaxValue;
            int extraLeadingZeros = 0;
            if (sepNeeded)
            {
                int idx = baseBitChars - numberOfGuardBitsToInclude;
                if (idx <= 0) { extraLeadingZeros = 1 - idx; idx = 1; baseBitChars += extraLeadingZeros; }
                sepIndex = idx;
            }

            // Dot index among bit chars (between whole and frac)
            int dotIndex = (zerosToWrite > 0) ? (extraLeadingZeros + 1) : int.MaxValue;

            // Write extra leading zeros (for the separator only)
            for (int i = 0; i < extraLeadingZeros; i++)
            {
                MaybeWriteSeparator(sepIndex, destination, ref charsWritten);
                destination[charsWritten++] = '0'; bitIndex++;
            }

            // Whole '0'
            MaybeWriteSeparator(sepIndex, destination, ref charsWritten);
            destination[charsWritten++] = '0'; bitIndex++;

            // Dot (if any)
            if (dotIndex != int.MaxValue && bitIndex == dotIndex) destination[charsWritten++] = '.';

            // Fractional zeros
            for (int i = 0; i < (zerosToWrite > 0 ? zerosToWrite : 0); i++)
            {
                MaybeWriteSeparator(sepIndex, destination, ref charsWritten);
                destination[charsWritten++] = '0'; bitIndex++;
            }

            // Trailing separator (if at end)
            if (sepNeeded && !sepWritten && bitIndex == sepIndex) { destination[charsWritten++] = '|'; sepWritten = true; }
            return;
        }

        // NON-ZERO CASE ----------------------------------------------------------------
        var absMantissa = BigInteger.Abs(_mantissa);

        // If nothing to output from mantissa window, print a single '0' (per original)
        if (outputStart < outputEnd)
        {
            int baseBitChars = 1;
            int sepIndex = int.MaxValue;
            int extraLeadingZeros = 0;
            if (sepNeeded)
            {
                int idx = baseBitChars - numberOfGuardBitsToInclude;
                if (idx <= 0) { extraLeadingZeros = 1 - idx; idx = 1; baseBitChars += extraLeadingZeros; }
                sepIndex = idx;
            }

            for (int i = 0; i < extraLeadingZeros; i++)
            {
                MaybeWriteSeparator(sepIndex, destination, ref charsWritten);
                destination[charsWritten++] = '0'; bitIndex++;
            }

            MaybeWriteSeparator(sepIndex, destination, ref charsWritten);
            destination[charsWritten++] = '0'; bitIndex++;

            if (sepNeeded && !sepWritten && bitIndex == sepIndex) { destination[charsWritten++] = '|'; sepWritten = true; }
            return;
        }

        // Compute whole/frac layout
        int wholeBitCount = totalBits + (Scale - GuardBits);

        // Whole part counts
        int wholeCount = 0;         // number of whole-part bits (not including trailingZeros when > totalBits)
        int trailingZeros = 0;
        if (wholeBitCount <= 0)
        {
            wholeCount = 1;         // single '0' whole part
        }
        else
        {
            int wholeEnd = Math.Max(0, totalBits - wholeBitCount);     // inclusive
            wholeCount = Math.Max(0, outputStart - wholeEnd + 1);
            if (wholeBitCount > totalBits) trailingZeros = (wholeBitCount - totalBits);
            wholeCount += trailingZeros;
        }

        bool hasFracPart = (wholeBitCount < totalBits) && (outputEnd < totalBits - wholeBitCount);

        // Fraction counts
        int fracLeadZeros = hasFracPart && (wholeBitCount < 0) ? -wholeBitCount : 0;
        int fracCount = 0;
        if (hasFracPart)
        {
            int fracStart = Math.Min(outputStart, totalBits - wholeBitCount - 1);
            int fracEnd = outputEnd;
            if (fracStart >= fracEnd) fracCount = fracStart - fracEnd + 1;
        }

        // Total bit chars (no '.' yet, no extra-leading)
        int baseBitChars2 = wholeCount + (hasFracPart ? (fracLeadZeros + fracCount) : 0);

        // Separator index & extra leading zeros
        int sepIndexNZ = int.MaxValue;
        int extraLeadingZerosNZ = 0;
        if (sepNeeded)
        {
            int idx = baseBitChars2 - numberOfGuardBitsToInclude;
            if (idx <= 0) { extraLeadingZerosNZ = 1 - idx; idx = 1; baseBitChars2 += extraLeadingZerosNZ; }
            sepIndexNZ = idx;
        }

        // Dot index among bit chars
        int dotIndexNZ = hasFracPart ? (extraLeadingZerosNZ + wholeCount) : int.MaxValue;

        // --- Stream write ---

        // Extra leading zeros (separator-only)
        for (int i = 0; i < extraLeadingZerosNZ; i++)
        {
            MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
            destination[charsWritten++] = '0'; bitIndex++;
        }

        // WHOLE PART
        if (wholeBitCount <= 0)
        {
            MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
            destination[charsWritten++] = '0'; bitIndex++;
        }
        else
        {
            // Mantissa-sourced whole bits
            int wholeEndMant = Math.Max(0, totalBits - wholeBitCount);     // inclusive
            for (int pos = outputStart; pos >= wholeEndMant; pos--)
            {
                MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
                destination[charsWritten++] = GetBit(absMantissa, pos);
                bitIndex++;
            }

            // Trailing zeros when wholeBitCount > totalBits
            for (int i = 0; i < trailingZeros; i++)
            {
                MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
                destination[charsWritten++] = '0'; bitIndex++;
            }
        }

        // If separator sits right before radix, place it now
        if (sepNeeded && !sepWritten && bitIndex == sepIndexNZ)
        {
            destination[charsWritten++] = '|';
            sepWritten = true;
        }

        // FRACTION
        if (hasFracPart)
        {
            // Radix point
            if (dotIndexNZ != int.MaxValue && bitIndex == dotIndexNZ) destination[charsWritten++] = '.';

            // Leading fractional zeros
            for (int i = 0; i < fracLeadZeros; i++)
            {
                MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
                destination[charsWritten++] = '0'; bitIndex++;
            }

            // Mantissa-sourced fractional bits
            int fracStartPos = Math.Min(outputStart, totalBits - wholeBitCount - 1);
            int fracEndPos = outputEnd;
            for (int pos = fracStartPos; pos >= fracEndPos; pos--)
            {
                MaybeWriteSeparator(sepIndexNZ, destination, ref charsWritten);
                destination[charsWritten++] = GetBit(absMantissa, pos);
                bitIndex++;
            }
        }

        // Trailing separator if it belongs at the very end
        if (sepNeeded && !sepWritten && bitIndex == sepIndexNZ)
        {
            destination[charsWritten++] = '|';
            sepWritten = true;
        }
    }

    private char GetBit(BigInteger value, int bitPosition)
    {
        if (bitPosition < 0 || bitPosition >= _size) return '0';
        return ((value >> bitPosition) & 1) == 1 ? '1' : '0';
    }

    public string ToBinaryString(bool includeGuardBits = false, bool showPrecisionSeparator = false)
    {
        return ToBinaryString(includeGuardBits ? 32 : 0, showPrecisionSeparator);
    }

    public string ToBinaryString(int numberOfGuardBitsToInclude, bool showPrecisionSeparator = false)
    {
        if (numberOfGuardBitsToInclude <= 0) showPrecisionSeparator = false;
        numberOfGuardBitsToInclude = Math.Clamp(numberOfGuardBitsToInclude, 0, 32);
        int bufferSize = CalculateBinaryStringLength(numberOfGuardBitsToInclude, showPrecisionSeparator);
        Span<char> buffer = stackalloc char[bufferSize];
        WriteBinaryToSpan(buffer, out int charsWritten, numberOfGuardBitsToInclude, showPrecisionSeparator);
        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Computes the total number of characters required for the binary representation.
    /// </summary>
    private int CalculateBinaryStringLength(int numberOfGuardBitsToOutput = 0, bool showPrecisionSeparator = false)
    {
        numberOfGuardBitsToOutput = Math.Clamp(numberOfGuardBitsToOutput, 0, GuardBits);

        int totalBits = _size;                           // total stored bits
        int outputStart = totalBits - 1;                   // inclusive
        int outputEnd = GuardBits - numberOfGuardBitsToOutput; // inclusive

        // "Zero" per writer semantics: nothing above the kept guard range
        bool isZero = (totalBits == 0) || (totalBits <= (GuardBits - numberOfGuardBitsToOutput));

        int signChars = (!isZero && _mantissa.Sign < 0) ? 1 : 0;

        int bitChars = 0;   // count of '0'/'1' characters only (no '.' and no sign)
        int dotChars = 0;   // 1 if a radix point will be written, else 0

        if (isZero)
        {
            // Writer prints: "0" and, if needed, ".000..." where
            // zerosToWrite = numberOfGuardBitsToOutput - Scale
            bitChars = 1;
            int zerosToWrite = Math.Max(0, numberOfGuardBitsToOutput - Scale);

            // When a zero value is created from an integer constructor, the scale
            // reflects the implicit guard bits (e.g. Scale == -GuardBits). In that
            // scenario we only want to emit guard-bit zeros when explicitly
            // requested via numberOfGuardBitsToOutput.
            if (_mantissa.IsZero && _size == 0 && Scale == -GuardBits && zerosToWrite > numberOfGuardBitsToOutput)
            {
                zerosToWrite = numberOfGuardBitsToOutput;
            }

            if (zerosToWrite > 0)
            {
                dotChars = 1;
                bitChars += zerosToWrite;
            }
        }
        else
        {
            // If nothing from mantissa falls in the output window, writer prints just "0"
            if (outputStart < outputEnd)
            {
                bitChars = 1;
                // no radix
            }
            else
            {
                // Partition by scale: how many bits are whole vs fractional conceptually
                int wholeBitCount = totalBits + (Scale - GuardBits);

                // Whole part
                if (wholeBitCount <= 0)
                {
                    // Writer prints a single '0' for the whole part
                    bitChars += 1;
                }
                else
                {
                    // Bits coming from mantissa for whole part
                    int wholeEnd = Math.Max(0, totalBits - wholeBitCount); // inclusive
                    int wholeCount = Math.Max(0, outputStart - wholeEnd + 1);
                    bitChars += wholeCount;

                    // Trailing zeros if wholeBitCount exceeds totalBits
                    if (wholeBitCount > totalBits)
                        bitChars += (wholeBitCount - totalBits);
                }

                // Fractional part?
                bool hasFracPart = (wholeBitCount < totalBits) && (outputEnd < totalBits - wholeBitCount);
                if (hasFracPart)
                {
                    dotChars = 1;

                    // Leading fractional zeros if scale pushes radix left of all real bits
                    if (wholeBitCount < 0)
                        bitChars += (-wholeBitCount);

                    // Mantissa-sourced fractional bits
                    int fracStart = Math.Min(outputStart, totalBits - wholeBitCount - 1); // inclusive
                    int fracEnd = outputEnd;                                            // inclusive
                    int fracCount = Math.Max(0, fracStart - fracEnd + 1);
                    bitChars += fracCount;
                }
            }
        }

        // Guard-bit separator '|' placement (ignoring the radix point):
        // It is inserted so exactly numberOfGuardBitsToOutput bit characters lie to its right.
        // If that puts it before all digits, we prepend enough leading zeros so there is
        // at least one digit before the separator.
        int extraLeadingZeros = 0;
        if (showPrecisionSeparator)
        {
            int sepIndex = bitChars - numberOfGuardBitsToOutput; // position BEFORE which '|' goes
            if (sepIndex <= 0)
            {
                extraLeadingZeros = 1 - sepIndex; // make sepIndex == 1
                bitChars += extraLeadingZeros;
            }
        }

        int separatorChars = showPrecisionSeparator ? 1 : 0;

        // Total length = sign + digits (including any extra leading zeros) + optional '.' + optional '|'
        return signChars + bitChars + dotChars + separatorChars;
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

        //if (intVal.IsZero) return "0";

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
