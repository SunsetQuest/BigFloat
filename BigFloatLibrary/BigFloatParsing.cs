﻿// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot are used in the development of this library.

using System;
using System.Globalization;
using System.Numerics;

namespace BigFloatLibrary;

// see "BigFloatTryParseNotes.txt" for additional notes

public readonly partial struct BigFloat
{
    /// <summary>
    /// Parses an input string and returns a BigFloat. If it fails, an exception is thrown.
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Radix point (aka decimal point for base 10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    /// </summary>
    /// <param name="numericString">The input decimal, hexadecimal, or binary number.</param>
    /// <param name="binaryScaler">Optional positive or negative base-2 scaling (default is zero).</param>
    public BigFloat(string numericString, int binaryScaler = 0, int hiddenBitsIncluded = int.MinValue)
    {
        this = Parse(numericString, binaryScaler, hiddenBitsIncluded);
    }

    /// <summary>
    /// Parses an input string and returns a BigFloat. If it fails, an exception is thrown.
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Radix point (aka decimal point for base 10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    /// </summary>
    /// <param name="numericString">The input decimal, hexadecimal, or binary number.</param>
    /// <param name="binaryScaler">Optional positive or negative base-2 scaling (default is zero).</param>
    public static BigFloat Parse(string numericString, int binaryScaler = 0, int hiddenBitsIncluded = int.MinValue)
    {
        bool success = TryParse(numericString, out BigFloat biRes, binaryScaler, hiddenBitsIncluded);

        if (!success)
        {
            throw new ArgumentException("Unable to convert string to BigFloat.");
        }

        biRes.AssertValid();

        return biRes;
    }

    /// <summary>
    /// Parses a <paramref name="numericString"/> to a BigFloat. 
    /// This function supports: 
    ///  - Positive or negative leading signs or no sign. 
    ///  - Spaces and commas are ignored.
    ///  - Exponential notation (e.g. 1.23e+4, 123e4)
    ///  - Strings with closing braces, brackets, single and double quotes (e.g. (123), {123e4})
    ///  - Decimal point (and radix point for non base-10)
    ///  - Hex strings starting with a [-,+,_]0x (radix point and sign supported)
    ///  - Binary strings starting with a [-,+,_]0b (radix point and sign supported)
    ///  - The precision separator, '|'.  For example, '1.01|101' parses '1.01' as in-precision and '101' as out of precision bits stored in hidden bits.
    /// </summary>
    /// <param name="numericString">The input decimal, hexadecimal, or binary number.</param>
    /// <param name="result">The resulting BigFloat. Zero is returned if conversion failed.</param>
    /// <param name="binaryScaler">Optional positive or negative base-2 scaling (default is zero).</param>
    /// <returns>Returns true if successful.</returns>
    public static bool TryParse(string numericString, out BigFloat result, int binaryScaler = 0, int hiddenBitsIncluded = int.MinValue)
    {
        //string orgValue = numericString;
        if (string.IsNullOrEmpty(numericString))
        {
            result = new BigFloat(0);
            return false;
        }

        // Let us check for invalid short strings, 0x___ , or 0b___
        int locAfterSign = (numericString[0] is '-' or '+') ? 1 : 0;
        if (numericString.Length == locAfterSign)    //[-,+][END] - fail  
        {
            result = new BigFloat(0);
            return false;
        }
        else if (numericString[locAfterSign] == '0')  //[-,+]0___
        {
            bool isNegHexOrBin = numericString[0] == '-';
            if (numericString.Length > 2 && numericString[locAfterSign + 1] is 'b' or 'B')  //[-,+]0b___
            {
                // remove leading "0x" or "-0x"
                //hiddenBitsIncluded = (hiddenBitsIncluded == int.MinValue) ? 0 : hiddenBitsIncluded;
                return TryParseBinary(numericString.AsSpan(isNegHexOrBin ? 3 : 2), out result, binaryScaler, isNegHexOrBin ? -1 : 0, hiddenBitsIncluded);
            }
            else if (numericString.Length > 2 && numericString[locAfterSign + 1] is 'x' or 'X')  //[-,+]0x___
            {
                hiddenBitsIncluded = (hiddenBitsIncluded == int.MinValue) ? 0 : hiddenBitsIncluded;
                return TryParseHex(numericString, out result, binaryScaler, hiddenBitsIncluded);
            }
            //else { } // [-,+]0[END] OR [-,+]0___  - continue(exceptions handled by BigInteger.Parse)
        }
        return TryParseDecimal(numericString, out result, ref binaryScaler, hiddenBitsIncluded);
    }

    /// <summary>
    /// Parses a decimal string to a BigFloat. It supports a binaryScaler and negative numbers. 
    /// Supports the precision separator, '|'.  For example, '1.23|456' parses '1.23' as in-precision and '456' as out of precision bits stored as hidden bits.
    /// It will also ignore spaces or commas. But mixing is not allowed.
    /// It also accepts values wrapped with double quotes, (), {}, or []. But mixing is not allowed.
    /// Allowed:                                           }
    ///  * -123.456     leading minus (or plus) signs are supported
    ///  * 123 456 789  spaces or commas ignored
    ///  * {123.456}    wrapped in {..} or (..) or ".."
    ///  * 123.456____  trailing spaces are ignored
    /// Not Allowed:
    ///  * 0xABC.DEF    leading 0x or 0b as hex or binary are not supported
    ///  * {123.456     must have leading and closing bracket
    ///  * {123.456)    brackets types must match
    ///  * {{123.456}}  limit of one only
    ///  * 123,456 789  mismatched separators will fail
    /// </summary>
    /// <param name="numericString">The value to parse.</param>
    /// <param name="result">(out) The returned result.</param>
    /// <param name="binaryScaler">(optional) Any additional power-of-two scale amount to include. Negative values are okay.</param>
    /// <returns>Returns true if successful.</returns>
    public static bool TryParseDecimal(string numericString, out BigFloat result, ref int binaryScaler, int hiddenBitsIncluded = int.MinValue)
    {
        bool usedCommaAlready = false;
        bool usedSpaceAlready = false;
        int decimalLocation = -1;
        int sign = 0;
        int BraceTypeAndStatus = 0;  // 0=not used, positive if opening found, negative if closed.
        int accuracyDelimiterPosition = -1;
        int expLocation = -1;
        int exp = 0;
        int expSign = 0;
        int destinationLocation = 0;

        Span<char> cleaned = stackalloc char[numericString.Length];

        for (int inputCurser = 0; inputCurser < numericString.Length; inputCurser++)
        {
            char c = numericString[inputCurser];
            switch (c)
            {
                case (>= '0' and <= '9'):
                    cleaned[destinationLocation++] = c;
                    break;
                case '.':
                    if (decimalLocation >= 0 || expLocation > 0)
                    {   // decimal point already found earlier OR following 'e'
                        result = 0;
                        return false;
                    }
                    decimalLocation = destinationLocation;
                    break;
                case 'e' or 'E':
                    if (expLocation >= 0)
                    {   // 'e' point already found earlier OR position 0
                        result = 0;
                        return false;
                    }
                    expLocation = destinationLocation;
                    break;
                case '-' or '+':
                    int signVal = c == '-' ? -1 : 1;
                    if (destinationLocation == 0)
                    {  // a '-' is allowed in the leading place
                        if (sign != 0)
                        {   // Lets make sure we did not try to add a sign already
                            result = 0;
                            return false;
                        }
                        sign = signVal;
                    }
                    else if (expLocation == destinationLocation)
                    {   // a '-' is allowed immediately following 'e'

                        if (expSign != 0)
                        {   // but not if a sign was already found
                            result = 0;
                            return false;
                        }
                        expSign = signVal;
                    }
                    else
                    {
                        result = 0;
                        return false;
                    }
                    break;
                case '|':
                    if (accuracyDelimiterPosition >= 0 || expLocation > 0)
                    {   // accuracy delimiter already found earlier OR following 'e'
                        result = 0;
                        return false;
                    }
                    accuracyDelimiterPosition = destinationLocation;
                    break;
                case ' ':
                    if (usedCommaAlready)
                    {   // already using Commas
                        result = 0;
                        return false;
                    }
                    usedSpaceAlready = true;
                    break;
                case ',':
                    if (usedSpaceAlready)
                    {   // already using Spaces
                        result = 0;
                        return false;
                    }
                    usedCommaAlready = true;
                    break;

                case '{' or '(':
                    if (BraceTypeAndStatus != 0)
                    {   // already using Spaces
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = c;
                    break;
                case ')' or '}' or ']':
                    if ((c == ')' && BraceTypeAndStatus != '(') ||
                        (c == '}' && BraceTypeAndStatus != '{') ||
                        (c == ']' && BraceTypeAndStatus != '['))
                    {
                        // The closing type should match.
                        result = 0;
                        return false;
                    }
                    if (!Helper_OnlyWhitespaceRemaining(numericString, ref inputCurser))
                    {
                        result = 0;
                        return false;
                    }
                    BraceTypeAndStatus = -c;
                    break;
                case '"' or '\'':
                    if (BraceTypeAndStatus == 0)
                    {
                        BraceTypeAndStatus = c;
                    }
                    else if ((c == '"' && BraceTypeAndStatus == '"') ||
                         (c == '\'' && BraceTypeAndStatus == '\''))
                    {
                        if (!Helper_OnlyWhitespaceRemaining(numericString, ref inputCurser))
                        {
                            result = 0;
                            return false;
                        }
                        BraceTypeAndStatus = -c;
                    }
                    else
                    {   // Should be either 0 (for not used) or c (for closing)
                        result = 0;
                        return false;
                    }
                    break;
                default:
                    // fail: unexpected char found
                    result = 0;
                    return false;
            }
        }

        // Check to make sure for an opening brace/bracket/param that was not closed.
        if (BraceTypeAndStatus > 0)
        {
            result = new BigFloat(0);
            return false;
        }

        // now lets remove trailing null chars off the end of the cleaned Spam
        cleaned = cleaned[..destinationLocation];

        // Check for 'e'  like 123e10 or 123.123e+100
        if (expLocation >= 0)
        {
            Span<char> expString = cleaned[expLocation..];
            if (!int.TryParse(expString, out exp))
            {   // unable to parse exp after 'e'
                result = 0;
                return false;
            }
            if (expSign < 0) exp = -exp;
            cleaned = cleaned[0..expLocation];
        }

        // Lets extract the actual base-10 number
        if (!BigInteger.TryParse(cleaned, out BigInteger val))
        {
            result = new BigFloat(0);
            return false;
        }


        // The 'accuracyDelimiterPosition', specified by '|' is:
        // (1) in base-10 needs to be converted to base-2
        // (2) currently it's measured from the MSB but should measure from LSB
        int hiddenBits = (hiddenBitsIncluded != int.MinValue) ? hiddenBitsIncluded :
            (accuracyDelimiterPosition < 0) ? 0 /*(radixDepth >= 0 ? 0 : 1)*/ : 
            (int)((cleaned.Length - accuracyDelimiterPosition) * 3.321928095f);


        //// Alternative Method - slower - maybe not as accurate either
        //int hiddenBitsFound = 0;
        //if (accuracyDelimiterPosition >= 0)
        //{
        //    if (!BigInteger.TryParse(cleaned[..accuracyDelimiterPosition], out BigInteger hiddenBits))
        //    {
        //        result = new BigFloat(0);
        //        return false;
        //    }
        //    hiddenBitsFound = int.Max((int)(val.GetBitLength() - hiddenBits.GetBitLength()-1),0);
        //}

        if (sign < 0) val = BigInteger.Negate(val);

        // No decimal point found, so place it at the end.
        if (decimalLocation < 0)
        {
            decimalLocation = cleaned.Length;
        }

        if (val.IsZero) 
        {
            int scaleAmt = (int)((decimalLocation - cleaned.Length + exp) * 3.32192809488736235) - hiddenBits;
            result = new BigFloat(BigInteger.Zero, scaleAmt, 0); //future: create a ZeroWithSpecificAccuracy(int prec) method.
            return true;
        }

        // If the user specifies a one (e.g., 1XXX OR 1 OR 0.01), the intended precision is closer to 2 bits.
        if (hiddenBits == 0 && BigInteger.Abs(val).IsOne)
        {
            val <<= 1;
            binaryScaler -= 1;
        }

        // Set ROUND to 1 to enable round to nearest.
        // When 1, an extra LSBit is kept and if it's set it will round up. (e.g. 0.1011 => 0.110)
        const int ROUND = 1;
        BigInteger intPart;
        int radixDepth = cleaned.Length - decimalLocation - exp;

        if (radixDepth == 0)
        {
            intPart = val << GuardBits - hiddenBits;
            binaryScaler += hiddenBits;
        }
        else if (radixDepth >= 0) //111.111 OR 0.000111
        {
            BigInteger a = BigInteger.Pow(5, radixDepth);
            int multBitLength = (int)a.GetBitLength();
            multBitLength += (int)(a >> (multBitLength - 2)) & 0x1;      // Round up if closer to larger size 
            int shiftAmt = multBitLength + GuardBits - 1 + ROUND - hiddenBits;  // added  "-1" because it was adding one to many digits 
                                                                                           // make asInt larger by the size of "a" before we dividing by "a"
            intPart = (((val << shiftAmt) / a) + ROUND) >> ROUND;
            binaryScaler += -multBitLength + 1 - radixDepth + hiddenBits;
        }
        else // 100010XX
        {
            BigInteger a = BigInteger.Pow(5, -radixDepth);
            int multBitLength = (int)a.GetBitLength();
            int shiftAmt = multBitLength - GuardBits - ROUND + hiddenBits;
            // Since we are making asInt larger by multiplying it by "a", we now need to shrink it by size "a".
            intPart = (((val * a) >> shiftAmt) + ROUND) >> ROUND;
            binaryScaler += multBitLength - radixDepth + hiddenBits;
        }

        //if (hiddenBitsIncluded == int.MinValue)
        //{
        //    hiddenBitsIncluded = intPart > 8 ? 1 : 0;
        //}


        result = new BigFloat(intPart, binaryScaler, true);
        var result2 = new BigFloat(intPart, binaryScaler); 
        //result = new BigFloat(intPart, binaryScaler - hiddenBitsIncluded); need to change to is with hiddenbits


        result.AssertValid();
        return true;

        static bool Helper_OnlyWhitespaceRemaining(string numericString, ref int inputCurser)
        {
            // only whitespace is allowed after closing brace/bracket/param 
            while (inputCurser < numericString.Length && char.IsWhiteSpace(numericString[inputCurser]))
                inputCurser++;

            return (inputCurser >= numericString.Length);
        }
    }


    /// <summary>
    /// Parses a hex string to a BigFloat. It supports a binaryScaler (base-2 point shifting) and negative numbers. 
    /// Supports the precision separator, '|'.  For example, '1.01|101' parses '1.01' as in-precision and '101' as out of precision bits stored in hidden bits.
    /// It will also ignore spaces and tolerate values wrapped with double quotes and brackets.
    /// Allowed: 
    ///  * ABCD/abcd    both uppercase/lowercases are supported
    ///  * -ABC.DEF     leading minus or plus signs are supported
    ///  * 123 456 789  spaces or commas ignored
    ///  * {ABC.DEF}    wrapped in {..} or (..) or ".."
    ///  * ABC_____     trailing spaces are ignored
    /// Not Allowed:
    ///  * 0xABC.DEF    leading 0x - use Parse for this
    ///  * {ABC.DEF     must have leading and closing bracket
    ///  * {ABC.DEF)    brackets types must match
    ///  * {{ABC.DEF}}  limit of one bracket only
    ///  * 123,456 789  mismatched separators will fail
    /// </summary>
    /// <param name="hexInput">The value to parse.</param>
    /// <param name="result">(out) The returned result.</param>
    /// <param name="binaryScaler">(optional) Any additional power-of-two scale amount to include. Negative values are okay.</param>
    /// <returns>Returns true if successful.</returns>
    public static bool TryParseHex(ReadOnlySpan<char> hexInput, out BigFloat result, int binaryScaler = 0, int hiddenBitsIncluded = 0)
    {
        if (hexInput.IsEmpty)
        {
            result = 0;
            return false;
        }

        bool usedCommaAlready = false;
        bool usedSpaceAlready = false;
        int radixLocation = 0;
        int BraceTypeAndStatus = 0;  // 0=not used, 1=usingCurlBraces, 3=usingRoundBrackets, 4=usingParentheses,  [neg means it has been closed]
        int accuracyDelimiterPosition = -1;
        int expLocation = -1;
        int destinationLocation = 1;

        // skip negative or positive sign
        bool isNeg = hexInput[0] == '-';
        int inputCurser = (isNeg || hexInput[0] == '+') ? 1 : 0;

        Span<char> cleaned = stackalloc char[hexInput.Length - inputCurser + 1];

        cleaned[0] = '0'; // Ensure we have a positive number

        for (; inputCurser < hexInput.Length; inputCurser++)
        {
            char c = hexInput[inputCurser];
            switch (c)
            {
                case (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'):
                    cleaned[destinationLocation++] = c;
                    break;
                case '.':
                    if (radixLocation != 0)
                    {
                        // radix point already found earlier
                        result = 0;
                        return false;
                    }
                    radixLocation = destinationLocation;
                    break;
                case '|':
                    if (accuracyDelimiterPosition >= 0 || expLocation > 0)
                    {   // accuracy delimiter already found earlier OR following 'e'
                        result = 0;
                        return false;
                    }
                    accuracyDelimiterPosition = destinationLocation;
                    break;
                case ' ':
                    if (usedCommaAlready)
                    {
                        // already using Commas
                        result = 0;
                        return false;
                    }
                    usedSpaceAlready = true;
                    break;
                case ',':
                    if (usedSpaceAlready)
                    {
                        // already using Spaces
                        result = 0;
                        return false;
                    }
                    usedCommaAlready = true;
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
                for (; inputCurser < hexInput.Length; inputCurser++)
                {
                    if (!char.IsWhiteSpace(hexInput[inputCurser]))
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
        if (destinationLocation <= 1)
        {
            result = 0;
            return false;
        }

        // Remove trailing '\0' 
        int spanEnd = cleaned.Length - 1;
        for (; cleaned[spanEnd] == '\0'; spanEnd--);
        cleaned = cleaned[..(spanEnd + 1)];

        // radixLocation is the distance from the MSB, it should be from the LSB. (or leave at 0 if radix point not found)
        if (radixLocation > 0)
        {
            radixLocation -= destinationLocation;
        }

        // hex are just bits of 4 so the scale is easy
        int newScale = (radixLocation * 4) + binaryScaler;

        if (!BigInteger.TryParse(cleaned, NumberStyles.AllowHexSpecifier, null, out BigInteger asInt))
        {
            result = new BigFloat(0);
            return false;
        }

        asInt <<= GuardBits - hiddenBitsIncluded;

        if (isNeg)
        {
            asInt = BigInteger.Negate(asInt);
        }
        result = new BigFloat(asInt, newScale - hiddenBitsIncluded, true);
        return true;
    }

    /// <summary>
    /// Converts the binary number in a string to a BigFloat. 
    /// Negative values must have a leading '-'.
    /// Supports the precision separator, '|'. For example, '1.01|101' parses '1.01' as in-precision and '101' as out of precision bits stored in hidden bits.
    /// e.g, '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// If it fails, an exception is thrown.
    /// </summary>
    /// <param name="binaryInput">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="binaryScaler">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <param name="includedHiddenBits">(optional)The number of sub-precision hidden bits that are included.</param>
    /// <returns>A BigFloat result of the input binary string.</returns>
    public static BigFloat ParseBinary(string binaryInput, int binaryScaler = 0, int forceSign = 0, int includedHiddenBits = int.MinValue) //todo: change "int includedHiddenBits = -1" " to 0
    {
        ArgumentException.ThrowIfNullOrEmpty(binaryInput); // .Net 7 or later
        //ArgumentNullException.ThrowIfNullOrWhiteSpace(input); // .Net 8 or later

        return !TryParseBinary(binaryInput.AsSpan(), out BigFloat result, binaryScaler, forceSign, includedHiddenBits)
            ? throw new ArgumentException("Unable to convert the binary string to a BigFloat.", binaryInput)
            : result;
    }

    /// <summary>
    /// Converts the binary text in ReadOnlySpan<char> to a BigFloat. 
    /// Negative values must have a leading '-'.
    /// Supports the precision separator, '|'.  For example, '1.01|101' parses '1.01' as in-precision and '101' as out of precision bits stored in hidden bits.
    /// e.g. '-11111100.101' would set the BigFloat to that rawValue, -252.625.
    /// </summary>
    /// <param name="input">The binary string input. It should be only [0,1,-,.]</param>
    /// <param name="result">(out) The BigFloat result.</param>
    /// <param name="binaryScaler">(optional)Additional scale - can be positive or negative</param>
    /// <param name="forceSign">(optional)Forces a sign on the output. [negative int = force negative, 0 = do nothing, positive int = force positive]</param>
    /// <param name="includesHiddenBits">(optional)The number of sub-precision hidden bits that are included. However, if the precision separator '|' is also used, this takes precedence.</param>
    /// <returns>Returns false if it fails or is given an empty or null string.</returns>
    public static bool TryParseBinary(ReadOnlySpan<char> input, out BigFloat result, int binaryScaler = 0, int forceSign = 0, int includesHiddenBits = int.MinValue)
    {
        int inputLen = input.Length;

        if (inputLen == 0)
        {
            result = new BigFloat(0);
            return false;
        }

        byte[] bytes = new byte[(inputLen + 7) / 8];
        bool radixPointFound = false;
        int destinationLocation = 0;      // The current bit we are writing to.

        // if it starts with a '-' then set negative rawValue to zero
        bool isNeg = input[0] == '-'; // 0x2D;

        // if starting with at - or + then headPosition should be 1.
        int headPosition = isNeg | input[0] == '+' ? 1 : 0;

        if (forceSign != 0)
        {
            isNeg = forceSign < 0;
        }

        int orgScale = binaryScaler;
        //                                01234567 89012345
        // Given the Input String:        00000001 00000010 00000011  
        // Output Byte Array should be:      [2]1    [1]2     [0]3  
        //                                
        // Now we are going to work our way from the end of the string forward.
        // We work backward to ensure the byte array is correctly aligned.
        int accuracyDelimiterPosition = -1;
        int tailPosition = inputLen - 1;
        for (; tailPosition >= headPosition; tailPosition--)
        {
            switch (input[tailPosition])
            {
                case '1':
                    bytes[destinationLocation >> 3] |= (byte)(1 << (destinationLocation & 0x7));
                    goto case '0';
                case '0':
                    destinationLocation++;
                    if (!radixPointFound)
                    {
                        binaryScaler--;
                    }
                    break;
                case '.':
                    // Let's make sure the radix was not already found.
                    if (radixPointFound)
                    {
                        result = new BigFloat(0);
                        return false; // Function was not successful - duplicate '.'
                    }
                    radixPointFound = true;
                    break;
                case ',' or '_' or ' ': // skip commas, underscores, and spaces (e.g.  1111_1111_0000) (optional - remove for better performance)
                    break;
                case '|':
                    if (accuracyDelimiterPosition >= 0)
                    {
                        // multiple precision spacers found (| or :)
                        result = new BigFloat(0); 
                        return false;
                    }
                    accuracyDelimiterPosition = destinationLocation;
                    break;
                default:
                    result = new BigFloat(0);
                    return false; // Function was not successful - unsupported char found
            }
        }

        if (destinationLocation == 0)
        {
            result = new BigFloat(0);
            return false;
        }

        // if param includesHiddenBits not is specified then use '|' separator if present
        if (includesHiddenBits == int.MinValue)
        {
            includesHiddenBits = (accuracyDelimiterPosition >= 0) ? accuracyDelimiterPosition : 0;
        }

        // Lets add the missing zero hidden bits
        if (includesHiddenBits >= 0)
        {
            int zerosNeededStill = GuardBits - includesHiddenBits;
            //outputBitPosition += zerosNeededStill;
            if (!radixPointFound)
            {
                binaryScaler -= zerosNeededStill;
            }
        }
        else
        {
            includesHiddenBits = 0;
        }

        //// The 'accuracyDelimiterPosition', specified by '|', is currently measured from the MSB but it should be measured from the LSB, so subtract it from val's Length.
        //int hiddenBitsFound;
        //if (accuracyDelimiterPosition >= 0)
        //{
        //    if (accuracyDelimiterPosition == destinationLocation)
        //    {
        //        hiddenBitsFound = 0;
        //    }
        //    else
        //    {
        //        hiddenBitsFound = destinationLocation - accuracyDelimiterPosition;
        //    }
        //}

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

        result = new BigFloat(bi << (GuardBits - includesHiddenBits), radixPointFound ? binaryScaler + includesHiddenBits : orgScale, true);

        result.AssertValid();

        return true; // return true if success
    }
}
