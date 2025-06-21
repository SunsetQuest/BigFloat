// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

// Ignore Spelling: Aprox

using System;
using System.Numerics;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// Returns the inverse of a BigFloat.
    /// </summary>
    public static BigFloat Inverse(BigFloat x)
    {
        int resScalePart = -x.Scale - (2 * (x._size - 1)) + GuardBits + GuardBits;
        BigInteger resIntPartNew = BigIntegerTools.Inverse(x._mantissa, x._size);
        BigFloat resultNew = new(resIntPartNew, resIntPartNew.IsPowerOfTwo ? resScalePart : resScalePart - 1, x.SizeWithGuardBits);
        return resultNew;
    }

    /// <summary>
    /// Returns the absolute value of a BigFloat.
    /// </summary>
    public static BigFloat Abs(BigFloat x) => x.Abs();

    /// <summary>
    /// Returns the absolute value of a BigFloat.
    /// </summary>
    public BigFloat Abs() => new(_mantissa.Sign >= 0 ? _mantissa : -_mantissa, Scale, _size);

    /// <summary>
    /// Calculates a BigFloat as the base and an integer as the exponent. The integer part is treated as exact.
    /// </summary>
    /// <param name="value">The base of the exponent.</param>
    /// <param name="exponent">The number of times value should be multiplied.</param>
    /// <param name="outPrecisionMatchesInput">When true, output precision is matched to input precision. When false, precision uses exponent rules based on "value^exp ± exp*error^(n-1)".</param>
    public static BigFloat Pow(BigFloat value, int exponent)
    {
        uint pwr = (uint)Math.Abs(exponent);

        if (pwr < 3)
        {
            return exponent switch
            {
                0 => One,
                1 => value,
                -1 => Inverse(value),
                2 => value * value,
                _ /*-2*/ => Inverse(value * value)
            };
        }

        // Used a Genetic Algorithm in Excel to figure out the formulas below (2 options)
        int expectedFinalPrecision = value._size;

        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if (expectedFinalPrecision < 53)
        {
            // Let's first make sure we would have some precision remaining after our exponent operation.
            if (expectedFinalPrecision <= 0)
            {
                return Zero; // technically more of a "NA".
            }

            int removedExp = value.BinaryExponent;

            double valAsDouble = (double)new BigFloat(value._mantissa, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)
            //if (double.IsFinite(valAsDouble))
            {
                // perform operation  
                double res = double.Pow(valAsDouble, exponent);
                BigFloat tmp = (BigFloat)res;
                value = SetPrecision(tmp, expectedFinalPrecision - GuardBits);

                // restore Scale
                value = new BigFloat(value._mantissa, value.Scale + (removedExp * exponent), true);

                return value;
            }
        }

        // At this point the expectedFinalPrecision >= 53 bits and Power >= 3

        // for each bit in the exponent, we need to multiply in 2^position
        int powerBitCount = BitOperations.Log2(pwr) + 1;

        // First Loop
        BigFloat product = ((pwr & 1) == 1) ? value : BigFloat.OneWithAccuracy(value.Size);
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

        return product;
    }

    /// <summary>Returns √x with <paramref name="wantedPrecision"/> data‑bits (ex‑guard‑bits).</summary>
    public static BigFloat Sqrt(BigFloat x, int wantedPrecision = 0)
    {
        // ---- 0. Quick exits ----------------------------------------------------
        if (x.Sign < 0)
            throw new ArgumentException("Square‑root of a negative BigFloat is undefined.", nameof(x));

        if (x._mantissa.IsZero)
            return new BigFloat(BigInteger.Zero, 0, 0);

        if (wantedPrecision <= 0)
            wantedPrecision = x._size - GuardBits;  

        // ---- 1. Normalize the radicand so that we can take an *integer* √ -------
        // totalLen   = total useful bits of the radicand (ex‑guard)
        // needShift  = shift that makes totalLen even and gives us exactly
        //              2*wantedPrecision bits before we call the integer sqrt.
        int totalLen = x.Scale + (x._size - BigFloat.GuardBits);
        int needShift = (wantedPrecision << 1)                // 2·prec
                            - (x._size - BigFloat.GuardBits)  // minus existing bits
                            - (totalLen & 1);                 // make len even

        BigInteger xx = x._mantissa << (needShift + BigFloat.GuardBits);

        // ---- 2. Fast integer √ via the original Newton+ algorithm ---------------
        BigInteger root = NewtonPlusSqrt(xx);

        // ---- 3. Undo the scaling we did in step 1 and pack into BigFloat --------
        int retShift = ((totalLen + 1) >> 1) - wantedPrecision; // == ⌈totalLen/2⌉ – prec
        return new BigFloat(root, retShift, (int)root.GetBitLength());
    }


    public static BigFloat CubeRoot(BigFloat value)
    {
        // Similar to square root but optimized for cube root
        int targetPrecision = value.Size + GuardBits;

        // Get initial approximation 
        BigFloat x = NthRootAprox(value, 3);
        x = SetPrecision(x, targetPrecision);

        // Newton's method for cube root: x_{n+1} = x_n * (2 + value/(x_n^3)) / 3
        int maxIterations = 30;

        for (int i = 0; i < maxIterations; i++)
        {
            BigFloat xCubed = x * x * x;
            BigFloat error = (xCubed - value) / value;

            // Check convergence
            if (error._size <= 5 || Math.Abs(error.BinaryExponent) <= -targetPrecision / 2)
            {
                break;
            }

            // Efficient update formula for cube root
            x *= ((One * 2) - (xCubed - value) / (3 * x * x * value));
        }

        return SetPrecision(x, targetPrecision - GuardBits);
    }

    /// <summary>
    /// Gets an initial approximation for the nth root using double precision arithmetic.
    /// </summary>
    public static BigFloat NthRootAprox(BigFloat value, int root)
    {
        // Use the binary exponent to create a better initial estimate
        int binaryExp = value.BinaryExponent;

        // Handle large exponents safely by scaling
        long mantissa;
        int adjustedExp;

        if (Math.Abs(binaryExp) > 1020)
        {
            // Scale exponent to avoid double precision overflow/underflow
            int expRemainder = binaryExp % root;

            // Adjust to keep mantissa in normalized range
            if (expRemainder < 0)
            {
                binaryExp -= root;
                expRemainder += root;
            }

            // Compute adjusted exponent for our scaled value
            adjustedExp = expRemainder + 1023;
        }
        else
        {
            // No scaling needed for normal range
            adjustedExp = binaryExp + 1023;
        }

        // Use double's hardware to get the top 53-bits
        mantissa = (long)(BigInteger.Abs(value._mantissa) >> (value._size - 53)) ^ (1L << 52);

        // Build double from components and take root
        double doubleValue = BitConverter.Int64BitsToDouble(mantissa | ((long)adjustedExp << 52));
        double approxRoot = Math.Pow(doubleValue, 1.0 / root);

        // Apply exponent scaling adjustment if we decomposed the exponent earlier
        int leftShift = (Math.Abs(binaryExp) > 1020) ? binaryExp / root : 0;
        int shrinkBy = (value._size < 53) ? 53 - value._size - GuardBits : 0;

        long bits = BitConverter.DoubleToInt64Bits(approxRoot);
        long mantissa2 = (bits & 0xfffffffffffffL) | 0x10000000000000L;
        int exp = (int)((bits >> 52) & 0x7ffL);

        BigInteger mantissa3 = new BigInteger(mantissa2) << (GuardBits- shrinkBy);
        int scale = exp - 1023 - 52 + shrinkBy + leftShift;
        int size = 53 + GuardBits - shrinkBy;
        return new(mantissa3, scale, size);
    }

    public static BigFloat NthRoot(BigFloat value, int root)
    {
        //future: add support for negative roots and values
        if (root < 3)
        {
            if (root < 0) { throw new ArgumentOutOfRangeException(nameof(root), "Root must be positive."); }
            if (root == 0) { return OneWithAccuracy(value.Size); }
            if (root == 1) { return value; }
            if (root == 2) { return Sqrt(value); }
            if (root == 3) { return CubeRoot(value); }
            // if (root == 4) { return Sqrt(Sqrt(value)); }
        }
        //if (root > 20) { throw new ArgumentOutOfRangeException(nameof(root), "Root must be below 22."); }
        if (value._mantissa.Sign <= 0)
        {
            bool isNegative = value._mantissa.Sign < 0;
            bool rootIsEven = (root & 1) == 0;
            if (isNegative && rootIsEven) { throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative for even roots."); }
            if (isNegative && !rootIsEven) { return -NthRoot(-value, root); }
            // Check if Value is zero, and if so, return Zero with a precision of value.Size
            if (value._mantissa.Sign == 0) { return new(BigInteger.Zero, value.Size, 0); } 
        }

        if (value._size < 53)
        {
            return NthRootAprox(value, root);
        }


        // Use double's hardware to get the first 53-bits
        long mantissa = (long)(BigInteger.Abs(value._mantissa)
                        >> (value._size - 53))
                    | (1L << 52);
        int valExp = value.BinaryExponent;

        int shift = 0;
        if (Math.Abs(valExp) > 1021)
        {
            shift = valExp - (valExp % root) + root;
            valExp -= shift;
        }
        long expBits = (long)(valExp + 1023);

        // build double, take root
        double dubVal = BitConverter.Int64BitsToDouble(mantissa | (expBits << 52));
        double tempRoot = Math.Pow(dubVal, 1.0 / root);

        // back to BigFloat
        BigFloat x = (BigFloat)tempRoot;
        if (shift != 0)
            x <<= (shift / root);

        //x = SetPrecision(x, x.Size + 100); //hack because precision runs out in while loop below because it loops too many times
        // future: if value._size<53, we just use the 53 double value and return

        //Future: we could use Newton Plus here to right size

        // get a proper sized "root"
        BigFloat rt = new((BigInteger)root << value.Size, -value.Size);
        BigFloat t = Pow(x, root) - value;
        BigFloat b = rt * Pow(x, root - 1); // Init the "b" and "t" for "oldX - (t / b)"
        int lastSize;
        do
        {
            BigFloat tb = t / b;
            x -= tb;
            b = rt * Pow(x, root - 1);
            lastSize = t._size;
            t = Pow(x, root) - value;
        } while (t._size < lastSize); //Performance: while (t._size < lastSize | t._size < 5);
        return x;
    }


    /// <summary>
    /// Returns the Log2 of a BigFloat number as a double. Log2 is equivalent to the number of bits between the radix point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// Sign is ignored. Zero and negative values are undefined and will return double.NaN.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as a double. If Zero or less then returns Not-a-Number.</returns>
    public static double Log2(BigFloat n)
    {
        // Special case for zero and negative numbers.
        if (((n._size >= GuardBits - 1) ? n._mantissa.Sign : 0) <= 0)
        {
            // if (!n.IsPositive)
            return double.NaN;
        }

        //The exponent is too large. We need to bring it closer to zero and then add it back in the log after.
        long mantissa = (long)(n._mantissa >> (n._size - 53));// ^ ((long)1 << 52);
        long dubAsLong = (1023L << 52) | long.Abs(mantissa);
        double val = BitConverter.Int64BitsToDouble(dubAsLong);
        return double.Log2(val) + n.BinaryExponent;
    }

    /// <summary>
    /// Returns the Log2 of a BigFloat number as an integer. Log2 is equivalent to the number of bits between the point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// A zero input will follow BigInteger and return a zero, technically however Log2(0) is undefined. Log2 is often used to indicate size in bits so returning 0 with Log2(0) is in-line with this.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as an integer.</returns>
    public static int Log2Int(BigFloat n)
    {
        if (n.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "Log2 is undefined for zero or negative values.");
        }
        return n.BinaryExponent;
    }


    //////////////////////////////// Trig FUNCTIONS ////////////////////////////////////////////
    // source: Claude 3.7, ChatGPT o3, and Ryan

    /// <summary>Switch point: if log2(|x|) ≤ –4 (~ 0.0625 rad) a direct Taylor series is fastest.</summary>
    private const int _taylorExpSwitch = -4;    // 1/16 rad ~ 3.6 deg

    // ------------------------------------------------------------------
    //  Cosine & tangent – reuse sine to avoid duplication
    // ------------------------------------------------------------------
    public static BigFloat Cos(BigFloat x) //=> Sin(x + (Constants.GetConstant(Catalog.Pi, x.Precision) >> 1)); 
    { return SinCos(x, true); }
    public static BigFloat Sin(BigFloat x)
    { return SinCos(x, false); }

    private static BigFloat SinCos(BigFloat x, bool isCos) //=> Sin(x + (Constants.GetConstant(Catalog.Pi, x.Precision) >> 1)); 
    {
        int prec = Math.Max(x.Size, x.Accuracy) + 1;
        BigFloat pi = Constants.GetConstant(Catalog.Pi, prec);
        BigFloat halfPi = pi >> 1;

        if (isCos)
        {
            x += halfPi;
        }

        //if (x.IsZero) return ZeroWithSpecifiedLeastPrecision(x.Accuracy);       // cheap exit
        // future: if fits in double then return double Math.Sin(double(x));

        BigFloat twoPi = pi << 1;

        // ---------- Payne‑Hanek style reduction to [‑pi/2, pi/2] ----------
        BigFloat r = x % twoPi;                 // |r| ≤ pi
        if (r > pi) { r -= twoPi; }
        if (r < -pi) { r += twoPi; }

        bool negate = false;
        if (r.Sign < 0) { r = -r; negate = true; }
        if (r > halfPi) { r = pi - r; }             // sin(pi–x)=sin(x)

        // now 0 ≤ r ≤ pi/2

        // when zero, return zero
        if (r._mantissa < 2) { return ZeroWithAccuracy(x.Accuracy); }

        // ----- ----- choose the core routine ----------
        BigFloat result = (r.BinaryExponent <= _taylorExpSwitch)
                            ? SinCosTyler(r, r, -prec - 10, 2)  // already tiny
                            : SinByHalving(r, prec);            // scale down first

        result = TruncateByAndRound(result, 3);
        return negate ? -result : result;
    }

    public static BigFloat Tan(BigFloat x)
    {
        BigFloat s = Sin(x);
        BigFloat c = Cos(x);
        return s / c; // BigFloat.TruncateByAndRound(s / c, 1);
    }

    public static BigFloat SinAprox(BigFloat x)
    {
        int prec = x.Size;                              // how many bits of precision x carries
        double xd = (double)x;                          // cast to double (rounded to ≤ 53 bits)
        const double twoPiDouble = 2.0 * Math.PI;       // ≃ 6.283185307179586

        // 1) only do the expensive BigFloat mod‐reduction if |xd| > 2pi or cast overflowed
        if (double.IsInfinity(xd) || Math.Abs(xd) > twoPiDouble)
        {
            // get a 2pi BigFloat at a little extra precision (to avoid rounding errors in the reduction)
            BigFloat twoPi = Constants.GetConstant(Catalog.Pi, x.Accuracy + 1) << 1;
            x %= twoPi;                                 // now x is in [–2pi, 2pi]
            xd = (double)x;                             // re-cast the reduced argument
        }

        // 2) hardware‐accelerated sine
        double sd = Math.Sin(xd);

        // 3) lift back to BigFloat and clamp to min(input‐precision, 52 bits)
        BigFloat result = new(sd);
        int wanted = Math.Min(prec, 52);
        return BigFloat.SetPrecisionWithRound(result, wanted);
    }

    private static BigFloat SinCosTyler(BigFloat x, BigFloat term, int stopExp, int k)
    {
        BigFloat sum = term;
        BigFloat x2 = x * x;
        while(true)
        {
            // sin when k=2: term *= −x^2 / ((2k‑1)(2k))
            // cos when k=1: term *= −x^2 / ((2k)(2k+1))
            term = -term * x2 / (k * (k + 1));
            sum += term;
            if (term.BinaryExponent <= stopExp) { break; }
            k += 2;
        }
        return sum;
    }

    private static BigFloat SinByHalving(BigFloat x, int p)
    {
        // halve until |x| < 2^-4
        int halves = 0;
        BigFloat y = x;
        while (y.BinaryExponent > _taylorExpSwitch)
        {
            y >>= 1;
            halves++;
        }

        // do the small‑angle evaluation with guard bits
        int workP = p + halves + 13;
        BigFloat s = SinCosTyler(y, y, -workP - 8, 2); //SinTaylor
        BigFloat c = SinCosTyler(y, OneWithAccuracy(x.Size + 2), -workP, 1); //CosTaylor

        // rebuild the original angle via repeated double‑angle
        for (int i = 0; i < halves; i++)
        {
            BigFloat sNew = (s * c) << 1;       // sin(2 theta)
            BigFloat cNew = (c * c) - (s * s);  // cos(2 theta)
            s = sNew;
            c = cNew;
        }
        return s;
    }
}
