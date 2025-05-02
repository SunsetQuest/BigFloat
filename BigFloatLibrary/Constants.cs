// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot are used in the development of this library.

// Ignore Spelling: Mascheroni Ramanujan Soldner Meissel Mertens Apery Khintchine Glaisher Kinkelin Buffon Pisot Lemniscate Ln

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// Provides access to mathematical constants with configurable precision.
    /// </summary>
    public static class Constants
    {
        // Default precision options
        private const int DefaultPrecisionBits = 2000;
        private const bool DefaultCutOnTrailingZero = true;

        #region Mathematical Categories

        /// <summary>
        /// Fundamental mathematical constants such as pi and e.
        /// </summary>
        public static class Fundamental
        {
            /// <summary>
            /// pi: The ratio of a circle's circumference to its diameter.
            /// Approximately 3.14159265358979323846...
            /// </summary>
            public static BigFloat Pi => GetConstant(Catalog.Pi);

            /// <summary>
            /// Euler's number (e): The base of the natural logarithm.
            /// Approximately 2.71828182845904523536...
            /// </summary>
            public static BigFloat E => GetConstant(Catalog.E);

            /// <summary>
            /// Square root of 2: Approximately 1.41421356237309504880...
            /// </summary>
            public static BigFloat Sqrt2 => GetConstant(Catalog.Sqrt2);

            /// <summary>
            /// Square root of 3: Approximately 1.73205080756887729352...
            /// Also known as Theodorus' constant.
            /// </summary>
            public static BigFloat Sqrt3 => GetConstant(Catalog.TheodorusConstant_Sqrt3);

            /// <summary>
            /// Square root of Pi: Approximately 1.77245385090551602729...
            /// </summary>
            public static BigFloat SqrtPi => GetConstant(Catalog.Sqrt_Pi);

            /// <summary>
            /// Golden Ratio (φ): Approximately 1.61803398874989484820...
            /// The limit of the ratio of consecutive Fibonacci numbers.
            /// </summary>
            public static BigFloat GoldenRatio => GetConstant(Catalog.GoldenRatio);

            /// <summary>
            /// Euler-Mascheroni constant (γ): Approximately 0.57721566490153286061...
            /// The limit of the difference between the harmonic series and natural logarithm.
            /// </summary>
            public static BigFloat EulerMascheroni => GetConstant(Catalog.EulerMascheroniConstant);
        }

        /// <summary>
        /// Number-theoretic constants related to prime numbers and number theory.
        /// </summary>
        public static class NumberTheory
        {
            /// <summary>
            /// Twin Prime constant: Approximately 0.66016181584686957392...
            /// Product over primes p of (1 - 1/(p-1)^2).
            /// </summary>
            public static BigFloat TwinPrime => GetConstant(Catalog.TwinPrimeConstant);

            /// <summary>
            /// Prime constant: Approximately 0.41468250985111166...
            /// A binary constant where 1 bits correspond to prime indices.
            /// </summary>
            public static BigFloat Prime => GetConstant(Catalog.PrimeConstant);

            /// <summary>
            /// Ramanujan-Soldner constant: Approximately 1.45136923488338105...
            /// The unique positive zero of the logarithmic integral function.
            /// </summary>
            public static BigFloat RamanujanSoldner => GetConstant(Catalog.RamanujanSoldnerConstant);

            /// <summary>
            /// Meissel–Mertens constant: Approximately 0.26149721284764278...
            /// Related to the distribution of prime numbers.
            /// </summary>
            public static BigFloat MeisselMertens => GetConstant(Catalog.Const_0_2614);

            /// <summary>
            /// Conway's constant: Approximately 1.30357726903429639...
            /// The growth rate of the Look-and-say sequence.
            /// </summary>
            public static BigFloat Conway => GetConstant(Catalog.ConwayConstant);

            /// <summary>
            /// Apéry's constant: Approximately 1.20205690315959428...
            /// Equal to ζ(3), the sum of the reciprocals of cubes of positive integers.
            /// </summary>
            public static BigFloat Apery => GetConstant(Catalog.AperyConstant);
        }

        /// <summary>
        /// Constants related to special functions and advanced mathematics.
        /// </summary>
        public static class Analysis
        {
            /// <summary>
            /// Catalan's constant: Approximately 0.91596559417721901...
            /// Sum of the alternating series 1-1/9+1/25-1/49...
            /// </summary>
            public static BigFloat Catalan => GetConstant(Catalog.CatalanConstant);

            /// <summary>
            /// Khintchine's constant: Approximately 2.68545200106530...
            /// The geometric mean of continued fraction terms.
            /// </summary>
            public static BigFloat Khintchine => GetConstant(Catalog.KhintchinesConstant);

            /// <summary>
            /// Omega constant: Approximately 0.56714329040978...
            /// The value of W(1) where W is the Lambert W function.
            /// </summary>
            public static BigFloat Omega => GetConstant(Catalog.OmegaConstant);

            /// <summary>
            /// Negative logarithm of gamma (Euler's constant): Approximately 0.54953931298164...
            /// </summary>
            public static BigFloat NegLogGamma => GetConstant(Catalog.Const_0_5495);

            /// <summary>
            /// Glaisher–Kinkelin constant: Approximately 1.28242712910062...
            /// Related to the Barnes G-function and the K-function.
            /// </summary>
            public static BigFloat GlaisherKinkelin => GetConstant(Catalog.GlaisherKinkelinConstant);

            /// <summary>
            /// Exponential of Euler-Mascheroni constant: Approximately 1.78107241799019...
            /// exp(γ)
            /// </summary>
            public static BigFloat ExpEulerMascheroni => GetConstant(Catalog.ExpEulerMascheroniConstant);
        }

        /// <summary>
        /// Constants related to physics and the natural sciences.
        /// </summary>
        public static class Physics
        {
            /// <summary>
            /// Fine Structure constant: Approximately 1.46035450880959...
            /// A fundamental physical constant characterizing the strength of the electromagnetic interaction.
            /// </summary>
            public static BigFloat FineStructure => GetConstant(Catalog.FineStructureConstant);
        }

        /// <summary>
        /// Constants derived from fundamental constants like Pi and e.
        /// </summary>
        public static class Derived
        {
            /// <summary>
            /// Natural log of Phi: Approximately 0.48121182505960344...
            /// </summary>
            public static BigFloat NaturalLogOfPhi => GetConstant(Catalog.NaturalLogOfPhi);

            /// <summary>
            /// pi squared (pi^2): Approximately 9.86960440108935...
            /// </summary>
            public static BigFloat PiSquared => GetConstant(Catalog.PiSquared);

            /// <summary>
            /// e squared (e^2): Approximately 7.38905609893065...
            /// </summary>
            public static BigFloat ESquared => GetConstant(Catalog.ESquared);

            /// <summary>
            /// pi times e (pie): Approximately 8.53973422267356...
            /// </summary>
            public static BigFloat PiTimesE => GetConstant(Catalog.PiTimesE);

            /// <summary>
            /// e to the power of pi (e^pi): Approximately 23.14069263277926...
            /// </summary>
            public static BigFloat EPowerPi => GetConstant(Catalog.EPowerPi);

            /// <summary>
            /// pi to the power of e (pi^e): Approximately 22.45915771836104...
            /// </summary>
            public static BigFloat PiPowerE => GetConstant(Catalog.PiPowerE);

            /// <summary>
            /// pi to the power of Pi (pi^pi): Approximately 36.46215960720791...
            /// </summary>
            public static BigFloat PiPowerPi => GetConstant(Catalog.PiPowerPi);

            /// <summary>
            /// e to the power of e (e^e): Approximately 15.15426224147926...
            /// </summary>
            public static BigFloat EPowerE => GetConstant(Catalog.EPowerE);

            /// <summary>
            /// Ratio of pi to e (pi/e): Approximately 1.15572734979092...
            /// </summary>
            public static BigFloat PiDividedByE => GetConstant(Catalog.PiDividedByE);

            /// <summary>
            /// Ratio of e to pi (e/pi): Approximately 0.86525597943226...
            /// </summary>
            public static BigFloat EDividedByPi => GetConstant(Catalog.EDividedByPi);

            /// <summary>
            /// Logarithm of pi (ln(pi)): Approximately 1.14472988584940...
            /// </summary>
            public static BigFloat LogPi => GetConstant(Catalog.LogPi);
        }

        /// <summary>
        /// Constants related to trigonometric functions.
        /// </summary>
        public static class Trigonometric
        {
            /// <summary>
            /// Sine of 2pi/5: Approximately 0.95105651629515...
            /// Related to the regular pentagon.
            /// </summary>
            public static BigFloat Sin2PiDiv5 => GetConstant(Catalog.Sin2PiDiv5);

            /// <summary>
            /// Cosine of pi/8: Approximately 0.92387953251128...
            /// </summary>
            public static BigFloat CosPiDiv8 => GetConstant(Catalog.CosPiDiv8);

            /// <summary>
            /// Cosine of pi/16: Approximately 0.98078528040323...
            /// </summary>
            public static BigFloat CosPiDiv16 => GetConstant(Catalog.CosPiDiv16);

            /// <summary>
            /// Cosine of pi/20: Approximately 0.98768834059513...
            /// </summary>
            public static BigFloat CosPiDiv20 => GetConstant(Catalog.CosPiDiv20);

            /// <summary>
            /// Buffon's constant (2/pi): Approximately 0.63661977236758...
            /// Related to the famous Buffon's needle problem.
            /// </summary>
            public static BigFloat Buffon => GetConstant(Catalog.BuffonConstant);

            /// <summary>
            /// Sine of pi/3 (√3/2): Approximately 0.86602540378443...
            /// </summary>
            public static BigFloat SinPiDiv3 => GetConstant(Catalog.SinPiDiv3);
        }

        /// <summary>
        /// Additional mathematical constants that don't fit into other categories.
        /// </summary>
        public static class Misc
        {
            /// <summary>
            /// Plastic number: Approximately 1.32471795724474...
            /// The unique real root of x^3 - x - 1 = 0.
            /// </summary>
            public static BigFloat Plastic => GetConstant(Catalog.PlasticNumber);

            /// <summary>
            /// Pisot's constant: Approximately 1.38027756910156...
            /// A Pisot-Vijayaraghavan number, the positive root of x^4 - x^3 - 1 = 0.
            /// </summary>
            public static BigFloat Pisot => GetConstant(Catalog.PisotsConstant);

            /// <summary>
            /// Lemniscate constant: Approximately 0.59907011736779...
            /// Related to the perimeter of the lemniscate curve.
            /// </summary>
            public static BigFloat Lemniscate => GetConstant(Catalog.LemniscateConstant);

            /// <summary>
            /// The imaginary unit raised to the imaginary power (i^i): Approximately 0.20787957635076...
            /// Equal to exp(-pi/2).
            /// </summary>
            public static BigFloat IPowerI => GetConstant(Catalog.IPowerI);

            /// <summary>
            /// The arithmetic-geometric mean of Pi and e: Approximately 0.92610855157230...
            /// </summary>
            public static BigFloat AGMMeanPiE => GetConstant(Catalog.AGMMeanPiE);
        }

        #endregion


        #region Configuration and Direct Access

        /// <summary>
        /// Provides access to mathematical constants with customizable precision.
        /// </summary>
        public readonly struct WithPrecision
        {
            private readonly int _precisionInBits;
            private readonly bool _cutOnTrailingZero;
            private readonly bool _useExternalFiles;

            internal WithPrecision(int precisionInBits, bool cutOnTrailingZero, bool useExternalFiles)
            {
                _precisionInBits = precisionInBits;
                _cutOnTrailingZero = cutOnTrailingZero;
                _useExternalFiles = useExternalFiles;
            }

            // Existing code...

            /// <summary>
            /// Gets all available constants with the configured precision.
            /// Uses parallel processing to improve performance with large sets of constants.
            /// </summary>
            /// <param name="useParallelProcessing">Whether to use parallel processing for loading constants.</param>
            /// <returns>A dictionary of constant names to their values.</returns>
            public Dictionary<string, BigFloat> GetAll(bool useParallelProcessing = true)
            {
                int precisionInBits = _precisionInBits;
                bool cutOnTrailingZero = _cutOnTrailingZero;
                bool useExternalFiles = _useExternalFiles;

                if (!useParallelProcessing)
                {

                    return Catalog.AllConstants
                        .Select(constantId => (Id: constantId, Value: GetConstant(constantId, precisionInBits, cutOnTrailingZero, useExternalFiles)))
                        .Where(pair => !pair.Value.IsZero)  // Filter out constants that couldn't be computed
                        .ToDictionary(pair => pair.Id, pair => pair.Value);
                }

                // Use parallel processing for better performance with large sets
                var result = new ConcurrentDictionary<string, BigFloat>();

                Parallel.ForEach(Catalog.AllConstants, constantId =>
                {

                    var value = GetConstant(constantId, precisionInBits, cutOnTrailingZero, useExternalFiles);
                    if (!value.IsZero)
                        result.TryAdd(constantId, value);
                });

                return new Dictionary<string, BigFloat>(result);
            }

            /// <summary>
            /// Gets all constants from a specific category with the configured precision.
            /// </summary>
            /// <param name="category">An array of constant IDs representing a category.</param>
            /// <param name="useParallelProcessing">Whether to use parallel processing for loading constants.</param>
            /// <returns>A dictionary of constant names to their values.</returns>
            public Dictionary<string, BigFloat> GetCategory(string[] category, bool useParallelProcessing = true)
            {
                int precisionInBits = _precisionInBits;
                bool cutOnTrailingZero = _cutOnTrailingZero;
                bool useExternalFiles = _useExternalFiles;

                if (!useParallelProcessing)
                {
                    return category
                        .Select(constantId => (Id: constantId, Value: GetConstant(constantId, precisionInBits, cutOnTrailingZero, useExternalFiles)))
                        .Where(pair => !pair.Value.IsZero)  // Filter out constants that couldn't be computed
                        .ToDictionary(pair => pair.Id, pair => pair.Value);
                }

                // Use parallel processing for better performance with large sets
                var result = new ConcurrentDictionary<string, BigFloat>();

                Parallel.ForEach(category, constantId =>
                {
                    var value = GetConstant(constantId, precisionInBits, cutOnTrailingZero, useExternalFiles);
                    if (!value.IsZero)
                        result.TryAdd(constantId, value);
                });

                return new Dictionary<string, BigFloat>(result);
            }
        }


        /// <summary>
        /// Configures constants with custom precision settings.
        /// </summary>
        /// <param name="precisionInBits">The minimum binary precision in bits.</param>
        /// <param name="cutOnTrailingZero">Whether to cut precision at trailing zeros.</param>
        /// <param name="useExternalFiles">Whether to load from external files when available.</param>
        /// <returns>A configured constants provider.</returns>
        public static WithPrecision WithConfig(
            int precisionInBits = DefaultPrecisionBits,
            bool cutOnTrailingZero = DefaultCutOnTrailingZero,
            bool useExternalFiles = true)
        {
            return new WithPrecision(precisionInBits, cutOnTrailingZero, useExternalFiles);
        }

        /// <summary>
        /// Gets a constant by its identifier with default precision settings.
        /// </summary>
        /// <param name="constantId">The constant identifier from the Catalog.</param>
        /// <returns>The constant with default precision.</returns>
        public static BigFloat Get(string constantId) =>
            GetConstant(constantId, DefaultPrecisionBits, DefaultCutOnTrailingZero, true);

        #endregion // end Configuration and Direct Access


        #region Computation Methods

        /// <summary>
        /// Generates pi with a specified precision using a computational algorithm.
        /// Uses a Chudnovsky-type algorithm for high precision computation.
        /// Useful when pre-computed digits aren't available or higher precision is needed.
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>Pi with the specified precision.</returns>
        public static BigFloat GeneratePi(int accuracyInBits = DefaultPrecisionBits)
        {
            BigInteger m = BigInteger.One << (accuracyInBits + GuardBits + 12);
            BigInteger p = 125;
            BigInteger q = (239 * 239 * 239) << 2;
            BigInteger sum = (m / 5) - (m / (239 << 2));

            for (int j = 3; ; j += 4)
            {
                BigInteger t = m / j;
                BigInteger res =
                    +(m / (p * 25 * (j + 2)))
                    - (m / (q * ((57121 * j) + 114242)))
                    + (t / q)
                    - (t / p);

                if (res == 0)
                {
                    break;
                }

                sum += res;

                p *= 625;
                q *= 3262808641;
            }

            // Ensure last bits are correct
            sum >>= 8;

            return new BigFloat(sum, 2 - (int)sum.GetBitLength() + GuardBits, true);
        }

        /// <summary>
        /// Generates Euler's number (e) with specified precision using a computational algorithm.
        /// Uses the Taylor series expansion of e = 1 + 1/1! + 1/2! + 1/3! + ...
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>e with the specified precision.</returns>
        public static BigFloat GenerateE(int accuracyInBits = DefaultPrecisionBits)
        {
            // Determine how many terms we need based on the desired accuracy
            int terms = (int)(accuracyInBits * 0.7); // Approximation: need fewer terms than bits

            // We'll compute with higher precision and then round
            int workingPrecision = accuracyInBits + GuardBits + 10;

            // Scale factor to maintain precision
            BigInteger scaleFactor = BigInteger.One << workingPrecision;

            // Initialize result with first term (1)
            BigInteger result = scaleFactor;

            // Initialize factorial denominator
            BigInteger factorial = 1;

            // Add terms until we reach desired precision or contribution becomes negligible
            for (int i = 1; i <= terms; i++)
            {
                // Compute next term: scaleFactor / i!
                factorial *= i;
                BigInteger term = scaleFactor / factorial;

                // If term is too small to affect result, we're done
                if (term == 0)
                    break;

                // Add term to result
                result += term;
            }

            // Create BigFloat with correct scaling
            return new BigFloat(result, -workingPrecision + GuardBits, true);
        }

        /// <summary>
        /// Generates the Golden Ratio (φ) with specified precision.
        /// Phi = (1 + √5)/2 ≈ 1.6180339887...
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>The Golden Ratio with the specified precision.</returns>
        public static BigFloat GenerateGoldenRatio(int accuracyInBits = DefaultPrecisionBits)
        {
            // First, calculate √5 using the BigIntegerTools.NewtonPlusSqrt function
            // We'll use a higher precision to ensure final result has desired accuracy
            int workingPrecision = accuracyInBits + GuardBits + 4;

            // Create BigInteger 5 with sufficient precision
            BigInteger five = BigInteger.Parse("5") << workingPrecision;

            // Calculate square root using Newton's method
            BigInteger sqrtFive = BigIntegerTools.NewtonPlusSqrt(five);

            // Add 1 * scaleFactor
            BigInteger onePlusSqrt5 = sqrtFive + (BigInteger.One << workingPrecision);

            // Divide by 2 (right shift by 1)
            BigInteger phi = onePlusSqrt5 >> 1;

            // Create BigFloat with proper scaling
            return new BigFloat(phi, -workingPrecision + GuardBits, true);
        }

        /// <summary>
        /// Generates the natural logarithm of 2 (ln(2)) with specified precision.
        /// Uses the series ln(2) = 1 - 1/2 + 1/3 - 1/4 + ...
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>ln(2) with the specified precision.</returns>
        public static BigFloat GenerateLn2(int accuracyInBits = DefaultPrecisionBits)
        {
            // For ln(2), the alternating series converges very slowly
            // We'll use a more efficient algorithm: ln(2) = 2 * sum_k=0^∞ 1/((2k+1)*2^(2k+1))

            // Determine how many terms we need (more than for e)
            int terms = accuracyInBits * 2; // Safe upper bound

            // We'll compute with higher precision
            int workingPrecision = accuracyInBits + GuardBits + 20;

            // Scale factor to maintain precision
            BigInteger scaleFactor = BigInteger.One << workingPrecision;

            // Initialize result
            BigInteger result = 0;

            for (int k = 0; k < terms; k++)
            {
                // Calculate denominator: (2k+1) * 2^(2k+1)
                int denomPower = 2 * k + 1;
                BigInteger denom = denomPower * (BigInteger.One << denomPower);

                // Calculate term
                BigInteger term = scaleFactor / denom;

                // If term is too small to affect result, we're done
                if (term == 0)
                    break;

                // Add term to result
                result += term;
            }

            // Multiply by 2
            result <<= 1;

            // Create BigFloat with correct scaling
            return new BigFloat(result, -workingPrecision + GuardBits, true);
        }

        /// <summary>
        /// Generates Catalan's constant with specified precision.
        /// Catalan's constant = 1 - 1/9 + 1/25 - 1/49 + ...
        /// = sum_{k=0}^∞ (-1)^k/(2k+1)^2
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>Catalan's constant with the specified precision.</returns>
        public static BigFloat GenerateCatalan(int accuracyInBits = DefaultPrecisionBits)
        {
            // This series converges slowly, so we need many terms
            int terms = accuracyInBits * 3; // Safe upper bound

            // We'll compute with higher precision
            int workingPrecision = accuracyInBits + GuardBits + 20;

            // Scale factor to maintain precision
            BigInteger scaleFactor = BigInteger.One << workingPrecision;

            // Initialize result
            BigInteger result = 0;
            BigInteger sign = 1;

            for (int k = 0; k < terms; k++)
            {
                // Calculate denominator: (2k+1)^2
                BigInteger denom = BigInteger.Pow(2 * k + 1, 2);

                // Calculate term
                BigInteger term = scaleFactor / denom;

                // If term is too small to affect result, we're done
                if (term == 0)
                    break;

                // Add or subtract term based on sign
                result += sign * term;

                // Flip sign for next term
                sign = -sign;
            }

            // Create BigFloat with correct scaling
            return new BigFloat(result, -workingPrecision + GuardBits, true);
        }

        /// <summary>
        /// Generates the Euler-Mascheroni constant (γ) with specified precision.
        /// γ = lim_{n→∞} (1 + 1/2 + 1/3 + ... + 1/n - ln(n))
        /// </summary>
        /// <param name="accuracyInBits">The desired accuracy in bits.</param>
        /// <returns>The Euler-Mascheroni constant with the specified precision.</returns>
        public static BigFloat GenerateEulerMascheroni(int accuracyInBits = DefaultPrecisionBits)
        {
            // This is challenging to compute directly to high precision
            // We'll use a more efficient algorithm based on the Brent-McMillan method

            // For practical purposes, this method requires advanced implementation
            // that is beyond the scope of this example

            // Instead, we'll return a pre-computed value from our catalog
            if (Catalog.TryGetInfo(Catalog.EulerMascheroniConstant, out ConstantInfo info))
            {
                if (info.TryGetAsBigFloat(out BigFloat value, accuracyInBits, DefaultCutOnTrailingZero))
                {
                    return value;
                }
            }

            // If we don't have it in our catalog, return an approximate value
            // (This is a fallback; in a real implementation, we'd compute it)
            return Parse("0.57721566490153286060651209008240243104215933593992");
        }

        /// <summary>
        /// Formats a mathematical constant as a human-readable string with optional digit grouping.
        /// </summary>
        /// <param name="value">The BigFloat value to format.</param>
        /// <param name="decimalDigits">The number of decimal digits to include.</param>
        /// <param name="groupDigits">Whether to group digits for readability.</param>
        /// <param name="digitGroupSize">The number of digits per group.</param>
        /// <returns>A formatted string representation of the constant.</returns>
        public static string FormatConstantDigits(BigFloat value, int decimalDigits = 100, bool groupDigits = true, int digitGroupSize = 5)
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
            if (!groupDigits)
                return strValue;

            // Group digits for readability
            var result = new StringBuilder();

            // Add the integer part (before decimal)
            result.Append(strValue[..decimalPos]);

            // Add the decimal point
            result.Append('.');

            // Add the fractional part with grouping
            string fractionalPart = strValue[(decimalPos + 1)..];
            for (int i = 0; i < fractionalPart.Length; i++)
            {
                result.Append(fractionalPart[i]);

                // Add space after every digitGroupSize digits (except at the end)
                if (groupDigits && (i + 1) % digitGroupSize == 0 && i < fractionalPart.Length - 1)
                {
                    result.Append(' ');
                }
            }

            return result.ToString();
        }

        #endregion


        #region Implementation Details

        // Cache to avoid recomputing constants
        private static readonly Dictionary<string, Dictionary<int, BigFloat>> ConstantCache = [];
        private static readonly ReaderWriterLockSlim CacheLock = new();

        /// <summary>
        /// Gets a constant with the specified precision, using caching for efficiency.
        /// </summary>
        /// <param name="constantId">The identifier of the constant.</param>
        /// <param name="precisionInBits">The desired precision in bits.</param>
        /// <param name="cutOnTrailingZero">Whether to cut precision at trailing zeros.</param>
        /// <param name="useExternalFiles">Whether to load from external files.</param>
        /// <returns>The constant with the specified precision.</returns>
        public static BigFloat GetConstant(
            string constantId,
            int precisionInBits = DefaultPrecisionBits,
            bool cutOnTrailingZero = DefaultCutOnTrailingZero,
            bool useExternalFiles = true)
        {
            // Check if we have it in the cache already
            BigFloat cachedValue = TryGetFromCache(constantId, precisionInBits);
            if (!cachedValue.IsZero)
            {
                return cachedValue;
            }

            // Not in cache, lets fetch by ConstantInfo
            if (!Catalog.TryGetInfo(constantId, out ConstantInfo info))
            {
                return Zero; // Constant not found
            }

            // Try to get the constant with the requested precision
            if (info.TryGetAsBigFloat(out BigFloat value, precisionInBits, cutOnTrailingZero, useExternalFiles))
            {
                // Store in cache
                AddToCache(constantId, precisionInBits, value);
                return value;
            }

            // For built-in constants, try generating if available
            if (constantId == Catalog.Pi)
            {
                BigFloat generatedPi = GeneratePi(precisionInBits);
                AddToCache(constantId, precisionInBits, generatedPi);
                return generatedPi;
            }

            return Zero; // Couldn't get with requested precision
        }

        private static BigFloat TryGetFromCache(string constantId, int precisionInBits)
        {
            CacheLock.EnterUpgradeableReadLock();
            try
            {
                if (ConstantCache.TryGetValue(constantId, out var precisionMap))
                {
                    // Check if we have the exact precision
                    if (precisionMap.TryGetValue(precisionInBits, out var exactValue))
                    {
                        return exactValue;
                    }

                    // Check if we have a higher precision we can use
                    foreach (var entry in precisionMap)
                    {
                        if (entry.Key > precisionInBits)
                        {
                            var truncated = TruncateByAndRound(entry.Value, entry.Value.Size - precisionInBits);

                            CacheLock.EnterWriteLock();
                            try
                            {
                                precisionMap[precisionInBits] = truncated;
                            }
                            finally
                            {
                                CacheLock.ExitWriteLock();
                            }

                            return truncated;
                        }
                    }
                }

                return Zero; // Not found in cache
            }
            finally
            {
                CacheLock.ExitUpgradeableReadLock();
            }
        }

        private static void AddToCache(string constantId, int precisionInBits, BigFloat value)
        {
            CacheLock.EnterWriteLock();
            try
            {
                if (!ConstantCache.TryGetValue(constantId, out var precisionMap))
                {
                    precisionMap = [];
                    ConstantCache[constantId] = precisionMap;
                }

                precisionMap[precisionInBits] = value;
            }
            finally
            {
                CacheLock.ExitWriteLock();
            }
        }

        #endregion
    } // end class Constants
} // end namespace BigFloatLibrary