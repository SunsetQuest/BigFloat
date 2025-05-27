// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot are used in the development of this library.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// Helpers for visualizing and displaying mathematical constants.
    /// </summary>
    public static class ConstantVisualization
    {
        /// <summary>
        /// Formats a mathematical constant for display with customizable formatting.
        /// </summary>
        /// <param name="value">The BigFloat value to format.</param>
        /// <param name="decimalDigits">The number of decimal digits to include.</param>
        /// <param name="groupDigits">Whether to group digits for readability.</param>
        /// <param name="digitGroupSize">The number of digits per group.</param>
        /// <returns>A formatted string representation of the constant.</returns>
        public static string FormatConstant(BigFloat value, int decimalDigits = 100, bool groupDigits = true, int digitGroupSize = 5)
        {
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
            StringBuilder result = new();

            // Add the integer part (before decimal)
            _ = result.Append(strValue.AsSpan(0, decimalPos));

            // Add the decimal point
            _ = result.Append('.');

            // Add the fractional part with grouping
            string fractionalPart = strValue[(decimalPos + 1)..];
            for (int i = 0; i < fractionalPart.Length; i++)
            {
                _ = result.Append(fractionalPart[i]);

                // Add space after every digitGroupSize digits (except at the end)
                if (groupDigits && (i + 1) % digitGroupSize == 0 && i < fractionalPart.Length - 1)
                {
                    _ = result.Append(' ');
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Creates a formatted table comparing multiple constants.
        /// </summary>
        /// <param name="constants">Dictionary of constant names and values.</param>
        /// <param name="decimalDigits">Number of decimal digits to display for each constant.</param>
        /// <returns>A formatted string containing the comparison table.</returns>
        public static string CreateComparisonTable(Dictionary<string, BigFloat> constants, int decimalDigits = 20)
        {
            StringBuilder table = new();

            // Calculate appropriate column width
            int nameWidth = Math.Max(15, constants.Keys.Max(k => k.Length) + 2);
            int valueWidth = decimalDigits + 5; // Add space for decimal point, integer part, etc.

            // Build header
            string header = $"| {"Constant".PadRight(nameWidth)} | {"Value".PadRight(valueWidth)} |";
            string separator = $"|{new string('-', nameWidth + 2)}|{new string('-', valueWidth + 2)}|";

            _ = table.AppendLine(separator);
            _ = table.AppendLine(header);
            _ = table.AppendLine(separator);

            // Add each constant
            foreach (KeyValuePair<string, BigFloat> entry in constants.OrderBy(e => e.Key))
            {
                string value = FormatConstant(entry.Value, decimalDigits, false);
                if (value.Length > valueWidth)
                {
                    value = string.Concat(value.AsSpan(0, valueWidth - 3), "...");
                }

                _ = table.AppendLine($"| {entry.Key.PadRight(nameWidth)} | {value.PadRight(valueWidth)} |");
            }

            _ = table.AppendLine(separator);

            return table.ToString();
        }

        /// <summary>
        /// Creates a visual representation of the continued fraction expansion of a constant.
        /// </summary>
        /// <param name="value">The BigFloat value to analyze.</param>
        /// <param name="terms">The number of continued fraction terms to compute.</param>
        /// <returns>A string containing the continued fraction representation.</returns>
        public static string GetContinuedFraction(BigFloat value, int terms = 10)
        {
            StringBuilder result = new();
            List<BigInteger> continuedFractionTerms = [];

            // Extract the integer part
            BigFloat remaining = value;
            BigInteger integerPart = (BigInteger)remaining;
            continuedFractionTerms.Add(integerPart);

            // Calculate continued fraction terms
            for (int i = 1; i < terms && !remaining.IsZero; i++)
            {
                // Subtract integer part
                remaining -= new BigFloat(integerPart);

                // If the remainder is effectively zero, we're done
                if (remaining.IsZero) break;

                // Take reciprocal and continue
                remaining = One / remaining;

                // Extract next integer term
                integerPart = (BigInteger)remaining;
                continuedFractionTerms.Add(integerPart);
            }

            // Format the output
            _ = result.Append(continuedFractionTerms[0]);

            if (continuedFractionTerms.Count > 1)
            {
                _ = result.Append(" + 1/(");
                _ = result.Append(continuedFractionTerms[1]);

                for (int i = 2; i < continuedFractionTerms.Count; i++)
                {
                    _ = result.Append(" + 1/(");
                    _ = result.Append(continuedFractionTerms[i]);
                }

                // Close all the parentheses
                for (int i = 1; i < continuedFractionTerms.Count; i++)
                {
                    _ = result.Append(')');
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Generates information about a mathematical constant including value, 
        /// continued fraction expansion, and known properties.
        /// </summary>
        /// <param name="constantId">The ID of the constant from the Catalog.</param>
        /// <param name="decimalDigits">Number of decimal digits to display.</param>
        /// <returns>A formatted string containing information about the constant.</returns>
        public static string GetConstantInfo(string constantId, int decimalDigits = 50)
        {
            if (!Catalog.TryGetInfo(constantId, out ConstantInfo info))
            {
                return $"Constant '{constantId}' not found in the catalog.";
            }

            // Get the value with sufficient precision
            if (!info.TryGetAsBigFloat(out BigFloat value, decimalDigits * 4, true))
            {
                return $"Could not compute value for constant '{constantId}'.";
            }

            StringBuilder result = new();

            // Add name and basic information
            _ = result.AppendLine($"Constant: {info.Name ?? constantId}");
            _ = result.AppendLine(new string('=', 50));

            if (!string.IsNullOrEmpty(info.Formula))
            {
                _ = result.AppendLine($"Formula: {info.Formula}");
            }

            // Add the value with formatted digits
            _ = result.AppendLine("\nValue:");
            _ = result.AppendLine(FormatConstant(value, decimalDigits));

            // Add continued fraction expansion if appropriate
            _ = result.AppendLine("\nContinued Fraction Expansion:");
            _ = result.AppendLine(GetContinuedFraction(value, 15));

            // Add additional information if available
            if (!string.IsNullOrEmpty(info.MoreInfoURL))
            {
                _ = result.AppendLine($"\nMore Information: {info.MoreInfoURL}");
            }

            if (!string.IsNullOrEmpty(info.SourceOfDigitsURL))
            {
                _ = result.AppendLine($"Source of Digits: {info.SourceOfDigitsURL}");
            }

            if (!string.IsNullOrEmpty(info.SourceOfDigitsName))
            {
                _ = result.AppendLine($"Calculated by: {info.SourceOfDigitsName}");
            }

            // Add precision information
            _ = result.AppendLine($"\nPrecision Available:");
            _ = result.AppendLine($"- In Memory: {Math.Round(info.BitsInBase64.Length * 6 / LOG2_OF_10, 0)} decimal digits");

            if (info.SizeAvailableInFile > 0)
            {
                _ = result.AppendLine($"- In External Files: {info.SizeAvailableInFile} decimal digits");
            }

            return result.ToString();
        }
    }
}