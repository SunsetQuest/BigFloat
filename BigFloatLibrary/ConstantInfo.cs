// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.IO;
using System.Numerics;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    public readonly struct ConstantInfo(string name, string formula, string moreInfoLink, string sourceOfDigitsURL, string sourceOfDigitsName, int sizeAvailableInFile, int scale, string bitsInBase64)
    {
        /// <summary>
        /// The name of the constant. (Example: Pi, Fine-Structure Constant, Phi)
        /// </summary>
        public readonly string Name = name;
        /// <summary>
        /// A formula that can generate this. Example: 
        /// </summary>
        public readonly string Formula = formula;
        /// <summary>
        /// Provides a URL/Link to more info on the number.
        /// </summary>
        public readonly string MoreInfoURL = moreInfoLink;
        /// <summary>
        /// Provides a URL/Link the location of the digits.
        /// </summary>
        public readonly string SourceOfDigitsURL = sourceOfDigitsURL;
        /// <summary>
        /// Provides a the Person or sites name that generated the list.
        /// </summary>
        public readonly string SourceOfDigitsName = sourceOfDigitsName;
        /// <summary>
        /// The number of decimal placed available in the external file, if available, for numbers over 5000 digits.
        /// </summary>
        public readonly int SizeAvailableInFile = sizeAvailableInFile;
        /// <summary>
        /// The amount need to shift to get proper radix, assuming BASE64 has radix just in front of it.
        /// Scale = RadixShiftFromLeadingBit - DataBits.GetBitLength()
        /// </summary>
        public readonly int RadixShiftFromLeadingBit = scale;
        /// <summary>
        /// Contains the constant as a string in base64. (from 56 to 2780 chars)
        /// </summary>
        public readonly string BitsInBase64 = bitsInBase64;

        /// <summary>
        /// Returns the number bits available in the Base64 string, minus the guard bits.
        /// </summary>
        public int SizeAvailable
        {
            get 
            {
                byte[] first = new byte[3]; // Base64 decoding requires at least 4 bytes
                if (!Convert.TryFromBase64Chars(BitsInBase64.AsSpan()[0..4], first, out _))
                {
                    throw new Exception("Unable to decode 'BitsInBase64'.");
                }
                return (BitsInBase64.Length - 1) * 6 + (byte.Log2(first[0]) - 1) - GuardBits;

                // This should work if first bit is always the top bit.
                //return (BitsInBase64.Length) * 6;   
            }
        }

        /// <summary>
        /// Returns a common constant as a BigFloat with the given accuracy. The accuracy bits will stop on the first 0 after the <paramref name="minAccuracyInBits"/>.
        /// e.g. 110.110110 with a <paramref name="minAccuracyInBits"/> of 4 and <paramref name="cutOnTrailingZero"/>=true returns 110.11011
        /// e.g. 110.110100 with a <paramref name="minAccuracyInBits"/> of 4 and <paramref name="cutOnTrailingZero"/>=true returns 110.1101
        /// e.g. 110.111000 with a <paramref name="minAccuracyInBits"/> of 4 and <paramref name="cutOnTrailingZero"/>=true returns 110.1110
        /// </summary>
        /// <param name="value">The BigFloat to return.</param>
        /// <param name="minAccuracyInBits">The minimum target binary accuracy that should be fetched. The result will be this size or larger.</param>
        /// <param name="cutOnTrailingZero">Cuts off the accuracy just prior to a zero trailing bit. This ensures the result is this size or larger.</param>
        /// <param name="useExternalFiles">Attempt to load larger constants using external files.</param>
        /// <returns>Returns true if successful.</returns>
        public bool TryGetAsBigFloat(out BigFloat value, int minAccuracyInBits, bool cutOnTrailingZero = true, bool useExternalFiles = true)
        {
            value = default;

            try
            {
                ReadOnlySpan<char> base64Part = BitsInBase64.AsSpan();

                // Calculate how many Base64 characters we need for the requested bits
                // Each Base64 character encodes 6 bits, and we need to account for the scale (4 bytes = 32 bits)
                int bitsWithGuard = minAccuracyInBits + GuardBits;
                int extraCharsForTrailingZero = cutOnTrailingZero ? 20 : 0; // Extra chars to find trailing zero

                // Calculate chars needed (ceiling division to ensure we get enough chars)
                int base64CharsNeeded = ((bitsWithGuard + 5) / 6) + extraCharsForTrailingZero;

                // Base64 decoding requires input length to be a multiple of 4
                base64CharsNeeded = (base64CharsNeeded + 3) / 4 * 4;

                // Make sure we don't exceed the available characters
                base64CharsNeeded = Math.Min(base64CharsNeeded, base64Part.Length);

                // If debugging: Debug.WriteLine($"Bits needed: {bitsWithGuard}, Chars needed: {base64CharsNeeded} of {base64Part.Length}");

                // Decode only the part of Base64 that we need
                Span<byte> bytes = stackalloc byte[base64CharsNeeded * 3 / 4]; // Maximum size needed for decoding

                if (!Convert.TryFromBase64Chars(base64Part[..base64CharsNeeded], bytes, out int bytesWritten))
                {
                    // If we failed with partial Base64, try with the full string
                    bytes = new byte[base64Part.Length * 3 / 4]; // Heap allocation, but only in failure case
                    if (!Convert.TryFromBase64Chars(base64Part, bytes, out bytesWritten))
                    {
                        throw new FormatException("Invalid Base64 string");
                    }
                }

                bytes = bytes[..bytesWritten]; // Truncate to actual bytes written

                // Extract dataBits from remaining bytes
                BigInteger dataBits = new(bytes, isUnsigned: true, isBigEndian: true);
                int dataBitsLen = (int)dataBits.GetBitLength();

                // Create BigFloat with valueIncludesGuardBits set to true
                int scale = RadixShiftFromLeadingBit - dataBitsLen + GuardBits;
                value = new BigFloat(dataBits, scale, valueIncludesGuardBits: true);

                // Check if we have enough bits
                int availableBits = value._size - GuardBits;

                if (availableBits < minAccuracyInBits)
                {
                    // We didn't get enough bits from our optimized decoding
                    // Try again with the full Base64 string if it's longer than what we used
                    if (base64CharsNeeded < base64Part.Length)
                    {
                        bytes = new byte[base64Part.Length * 3 / 4];
                        if (Convert.TryFromBase64Chars(base64Part, bytes, out bytesWritten))
                        {
                            bytes = bytes[..bytesWritten];
                            if (bytes.Length >= 4)
                            {
                                dataBits = new BigInteger(bytes, isUnsigned: false);
                                value = new BigFloat(dataBits, RadixShiftFromLeadingBit, valueIncludesGuardBits: true);
                                availableBits = value._size - GuardBits;
                            }
                        }
                    }

                    // If we still don't have enough bits, try external files
                    if (availableBits < minAccuracyInBits)
                    {
                        if (useExternalFiles && SizeAvailableInFile > 0)
                        {
                            return TryLoadFromExternalFile(out value, minAccuracyInBits, cutOnTrailingZero);
                        }

                        value = default;
                        return false;
                    }
                }

                // Apply precision adjustments if we have enough bits
                int overAccurateBy = value._size - GuardBits - minAccuracyInBits;

                // If we don't have enough accuracy, return false
                if (overAccurateBy < 0)
                {
                    value = default;
                    return false;
                }

                // If user doesn't want to cut on trailing zero, just truncate
                if (!cutOnTrailingZero)
                {
                    value = TruncateByAndRound(value, overAccurateBy);
                    return true;
                }

                // Find nearest zero bit and truncate there
                if (overAccurateBy > 0)
                {
                    for (int i = overAccurateBy; i > 0; i--)
                    {
                        if ((value._mantissa & (BigInteger.One << (i - 1))) == 0)
                        {
                            // Found a zero bit, truncate here
                            value = AdjustPrecision(value, deltaBits: i);
                            return true;
                        }
                    }
                }

                // If we didn't find a zero bit in backward search, look forward
                for (int i = overAccurateBy + 1; i < value._size - 1; i++)
                {
                    if ((value._mantissa & (BigInteger.One << i)) == 0)
                    {
                        value = AdjustPrecision(value, deltaBits: i);
                        return true;
                    }
                }

                // If we reach here, we couldn't find a good truncation point
                value = TruncateByAndRound(value, overAccurateBy);
                return true;
            }
            catch (Exception) //Exception ex
            {
                // For debugging: Console.WriteLine($"Error in TryGetAsBigFloat: {ex.Message}");
                value = default;
                return false;
            }
        }

        private bool TryLoadFromExternalFile(out BigFloat value, int minAccuracyInBits, bool cutOnTrailingZero)
        {
            value = default;

            try
            {
                // Generate a filename based on the decimal representation of the number
                string fileName;

                // Try to get a decimal representation with modest precision for the filename
                if (TryGetAsBigFloat(out BigFloat tempValue, 64)) // 64 bits is enough for filename
                {
                    // Convert to string and take first 14 characters
                    fileName = tempValue.ToString();
                    if (fileName.Length > 14) fileName = fileName[..14];
                }
                else
                {
                    // Fallback to Name if available, or use first part of BitsInBase64
                    fileName = Name ?? BitsInBase64[..Math.Min(14, BitsInBase64.Length)];
                }

                string fileToTryAndLoad = Path.Combine("Values", fileName + ".txt");

                if (File.Exists(fileToTryAndLoad))
                {
                    string fileContent = File.ReadAllText(fileToTryAndLoad);
                    ReadOnlySpan<char> span = fileContent;

                    var prefix = "Number:".AsSpan();
                    int start = span.IndexOf(prefix);
                    if (start >= 0)
                    {
                        // Move past "Number:" itself
                        start += prefix.Length;

                        // Skip any spaces/tabs after the colon
                        while (start < span.Length && char.IsWhiteSpace(span[start]))
                            start++;

                        // Find end of line
                        int end = span[start..].IndexOfAny('\r', '\n');
                        if (end < 0) end = span.Length - start;  // last line, no newline

                        // Extract the number substring
                        var numSpan = span.Slice(start, end).Trim();
                        string numberStr = numSpan.ToString();

                        // Calculate how many decimal digits we need for the requested bits
                        int requiredDecimalDigits = (int)Math.Ceiling(minAccuracyInBits / LOG2_OF_10);

                        int decimalPoint = numberStr.IndexOf('.');

                        // Make sure we have enough digits in the file
                        if (decimalPoint >= 0 && numberStr.Length - decimalPoint - 1 >= requiredDecimalDigits)
                        {
                            // Truncate to the required number of digits (plus some buffer)
                            int digitsToKeep = Math.Min(numberStr.Length, decimalPoint + 1 + requiredDecimalDigits + 20);
                            string truncatedNumber = numberStr[..digitsToKeep];

                            value = BigFloat.Parse(truncatedNumber);

                            // Apply the trailing zero cut if needed
                            if (cutOnTrailingZero)
                            {
                                int overAccurateBy = value._size - GuardBits - minAccuracyInBits;
                                if (overAccurateBy > 0)
                                {
                                    value = TruncateByAndRound(value, overAccurateBy);
                                }
                            }

                            return true;
                        }
                    }
                }
            }
            catch (Exception) //Exception ex
            {
                // For debugging: Console.WriteLine($"Error loading from external file: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Returns a human-readable string representation of the constant with the specified number of decimal digits.
        /// </summary>
        /// <param name="decimalDigits">The number of decimal digits to include after the decimal point. Default is 20.</param>
        /// <param name="groupDigits">Whether to group digits for readability. Default is false.</param>
        /// <param name="digitGroupSize">The number of digits per group when grouping is enabled. Default is 5.</param>
        /// <returns>A formatted string representation of the constant.</returns>
        public string ToDisplayString(int decimalDigits = 20, bool groupDigits = false, int digitGroupSize = 5)
        {
            // Try to convert the constant to a BigFloat for formatting
            if (!TryGetAsBigFloat(out BigFloat value, decimalDigits * 4)) // Use 4x the digits for sufficient binary precision
            {
                // If conversion fails, return a placeholder
                return $"[Constant: precision not available]";
            }

            // Convert to string with enough digits
            string strValue = value.ToString();

            // Find decimal point
            int decimalPos = strValue.IndexOf('.');
            if (decimalPos < 0)
            {
                strValue += ".0"; // Add decimal point if there isn't one
                decimalPos = strValue.Length - 2;
            }

            // Determine how many digits we have after decimal point
            int existingDecimals = strValue.Length - decimalPos - 1;

            // If we need more digits than we have, return as is
            if (existingDecimals >= decimalDigits)
            {
                // Truncate to requested number of digits
                strValue = strValue[..(decimalPos + 1 + decimalDigits)];
            }

            // If we don't want to group digits, return the string as is
            if (!groupDigits) { return strValue; }

            // Group digits for readability
            System.Text.StringBuilder result = new();

            // Add the integer part (before decimal)
            _ = result.Append(strValue[..decimalPos]);

            // Add the decimal point
            _ = result.Append('.');

            // Add the fractional part with grouping
            string fractionalPart = strValue[(decimalPos + 1)..];
            for (int i = 0; i < fractionalPart.Length; i++)
            {
                _ = result.Append(fractionalPart[i]);

                // Add space after every digitGroupSize digits (except at the end)
                if ((i + 1) % digitGroupSize == 0 && i < fractionalPart.Length - 1)
                {
                    _ = result.Append(' ');
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets a short description of the constant, including its name, value, and formula if available.
        /// </summary>
        /// <returns>A string containing a brief description of the constant.</returns>
        public string GetSummary()
        {
            string display = ToDisplayString(15);
            string nameText = !string.IsNullOrEmpty(Name) ? Name : "Constant";
            string formulaText = !string.IsNullOrEmpty(Formula) ? $" ({Formula})" : "";

            return $"{nameText}{formulaText}: {display}...";
        }

        /// <summary>
        /// Returns a string representation of the constant.
        /// </summary>
        /// <returns>A string representation of the constant.</returns>
        public override string ToString()
        {
            return GetSummary();
        }
    }
}
