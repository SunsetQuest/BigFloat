// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Numerics;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{
    ////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////// MATH FUNCTIONS ////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Calculates the square root of a big floating point number.
    /// </summary>
    /// <param name="x">The input.</param>
    /// <param name="wantedPrecision">(Optional)The number of in-precision bits to return.</param>
    /// <returns>Returns the Sqrt of x as a BigFloat.</returns>
    public static BigFloat Sqrt(BigFloat x0, int wantedPrecision = 0)
    {
        BigFloat x = x0;// new BigFloat(x0.Int*8, x0._scale-3);
        if (wantedPrecision == 0)
        {
            wantedPrecision = x._size - ExtraHiddenBits;
        }

        if (x.DataBits == 0)
        {
            return new BigFloat((BigInteger)0, wantedPrecision, 0);
        }

        // Output should be (DataBits.GetBitLength()/2)+16 
        int totalLen = x.Scale + (x._size - ExtraHiddenBits);
        int needToShiftInputBy = (2 * wantedPrecision) - (x._size - ExtraHiddenBits) - (totalLen & 1);
        BigInteger intPart = NewtonPlusSqrt(x.DataBits << (needToShiftInputBy + ExtraHiddenBits));
        int retShift = ((totalLen + (totalLen > 0 ? 1 : 0)) / 2) - wantedPrecision;

        BigFloat result = new(intPart, retShift, (int)intPart.GetBitLength());
        return result;
    }

    /// <summary>
    /// Returns the inverse of a BigFloat.
    /// </summary>
    public static BigFloat Inverse(BigFloat x)
    {
        int resScalePart = -x.Scale - (2 * (x._size - 1)) + ExtraHiddenBits + ExtraHiddenBits;
        BigInteger resIntPartNew = BigIntegerTools.Inverse(x.DataBits, x._size);
        BigFloat resultNew = new(resIntPartNew, resIntPartNew.IsPowerOfTwo ? resScalePart : resScalePart - 1, x.SizeWithHiddenBits);
        return resultNew;
    }

    /// <summary>
    /// Calculates the a BigFloat as the base and an integer as the exponent. The integer part is treated as exact.
    /// </summary>
    /// <param name="value">The base of the exponent. </param>
    /// <param name="exponent">The number of times value should be multiplied.</param>
    /// <param name="outPrecisionMatchesInput">When true, output precision is matched to input precision. When false, precision uses exponent rules based on "value^exp ± exp*error^(n-1)".</param>
    public static BigFloat Pow(BigFloat value, int exponent, bool outPrecisionMatchesInput = false)
    {
        uint pwr = (uint)Math.Abs(exponent);

        if (pwr < 3)
        {
            return exponent switch
            {
                0 => BigFloat.One, //new BigFloat(BigInteger.One >> value.Scale, value.Scale),
                1 => value,
                -1 => Inverse(value),
                2 => value * value,
                _ /*-2*/ => Inverse(value * value)
            };
        }

        // Used a Genetic Algorithm in Excel to figure out the formula's below (2 options)
        int expectedFinalPrecision = value._size;
        if (outPrecisionMatchesInput)
        {
            expectedFinalPrecision += /*(int)(power / (1 - value)) -*/ BitOperations.Log2(pwr); // the first part is only for smaller values with large exponents
        }

        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if (expectedFinalPrecision < 53)
        {
            // Lets first make sure we would have some precision remaining after our exponent operation.
            if (expectedFinalPrecision <= 0)
            {
                return ZeroWithNoPrecision; // technically more of a "NA".
            }

            //bool expOverflows = value.Exponent < -1022 || value.Exponent > 1023;
            int removedExp = value.BinaryExponent;

            // todo: can be improved without using BigFloat  (See Pow(BigInteger,BigInteger) below)
            double valAsDouble = (double)new BigFloat(value.DataBits, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)

            //// if final result's scale would not fit in a double. 
            //int finalSizeWillBe = (int)(power * double.Log2(double.Abs(valAsDouble)));
            //bool finalResultsScaleFitsInDouble = finalSizeWillBe < 1020;  // should be <1023, but using 1020 for safety
            //if (!finalResultsScaleFitsInDouble)
            //    valAsDouble = (double)new BigFloat(value.DataBits, value.Scale - removedExp, true);  //or just  "1-_size"?  (BigFloat should be between 1 and 2)

            // perform opp  
            double res = double.Pow(valAsDouble, exponent);
            BigFloat tmp = (BigFloat)res;
            value = SetPrecision(tmp, expectedFinalPrecision - ExtraHiddenBits);

            // restore Scale
            value = new BigFloat(value.DataBits, value.Scale + (removedExp * exponent), true);

            return value;
        }

        // the expectedFinalPrecision >= 53 bits and Power >= 3, so pretty big.

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

        //product.DebugPrint("bf1");
        return product;
    }

    public static BigFloat NthRoot_INCOMPLETE_DRAFT(BigFloat value, int root) // todo:
    {
        if (root < 0) //future: add support for negative roots
        {
            throw new ArgumentOutOfRangeException(nameof(root), "Root must be a positive number.");
        }
        if (value.Sign < 0) //future: add support for negative roots
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be a positive number.");
        }

        // Check if Value is zero.
        if (value.DataBits.Sign == 0)
        {
            return ZeroWithSpecifiedLeastPrecision(value.Size);
        }

        // Check for common roots... 
        switch (root)
        {
            case 0:
                return OneWithAccuracy(value.Size);
            case 1:
                return value;
        }

        //int xLen = value._size;
        int rootSize = BitOperations.Log2((uint)root);
        int wantedPrecision = (int)BigInteger.Log2(value.DataBits) + rootSize; // for better accuracy for small roots add: "+ rootSize / Math.Pow(( root >> (rootSize - 3)), root) - 0.5"

        ////////// Lets remove value's scale (and just leave the last bit so scale is 0 or 1) ////////
        int removedScale = value.Scale & ~1;
        int newScale = value.Scale - removedScale;

        ////////// Use double's hardware to get the first 53-bits ////////
        long mantissa = (long)(BigInteger.Abs(value.DataBits) >> (value._size - 53)) ^ ((long)1 << 52);
        long exp = value.BinaryExponent + 1023;// + 52 -4;

        // if exp is oversized for double we need to pull out some exp:
        if (Math.Abs(value.BinaryExponent) > 1020) // future: using 1020(not 1021) to be safe
        {
            // new: (1)Pre: pre=(value>>preShift)  (2)Root: result=pre^(1/root) (3)post: result/(2^(-preShift/root)
            //double finalDiv = Math.Pow(2,-value.Exponent/root);
            exp = 0;
        }
        double dubVal = BitConverter.Int64BitsToDouble(mantissa | (exp << 52));
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // future: what about just casting from BigFloat to double?
        double tempRoot = Math.Pow(dubVal, 1.0 / root);  //Math.Pow(tempX, 1.0/root)
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(tempRoot);
        ulong tempVal = (bits & 0x1fffffffffffffL) | (1UL << 52);
        int tempExp = (int)((bits >> 52) & 0x7ffL) - 1023 - 20;
        newScale += tempExp;

        ////////////////// BigFloat Version ////////////////////////////
        BigFloat x = new((BigInteger)tempVal << 100, newScale - 100, true);

        // get a proper sized "root" (only needed for BigFloat version)
        BigFloat rt = new((BigInteger)root << value.Size, -value.Size);
        BigFloat t = Pow(x, root) - value;
        BigFloat b = rt * Pow(x, root - 1); // Init the "b" and "t" for "oldX - (t / b)"
        while (t._size > 3) //(!t.OutOfPrecision)
        {
            BigFloat oldX = x;
            BigFloat tb = t / b;
            x -= tb;
            b = rt * Pow(x, root - 1);
            t = Pow(x, root) - value;
        }
        return x;
    }


    /// <summary>
    /// Returns the Log2 of a BigFloat number as a double. Log2 is equivalent to the number of bits between the radix point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// Sign is ignored. Zero and negative values is undefined and will return double.NaN.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as a double. If Zero or less then returns Not-a-Number.</returns>
    public static double Log2(BigFloat n)
    {
        // Special case for zero and negative numbers.
        if (((n._size >= ExtraHiddenBits - 1) ? n.DataBits.Sign : 0) <= 0)
        {
            // if (!n.IsPositive)
            return double.NaN;
        }

        //The exponent is too large. We need to bring it closer to zero and then add it back in the log after.
        long mantissa = (long)(n.DataBits >> (n._size - 53));// ^ ((long)1 << 52);
        long dubAsLong = (1023L << 52) | long.Abs(mantissa);
        double val = BitConverter.Int64BitsToDouble(dubAsLong);
        return double.Log2(val) + n.BinaryExponent;
    }

    //todo: untested (or maybe better should be merged with exponent as that seems to be what most classes/structs use like BigInteger and Int)
    /// <summary>
    /// Returns the Log2 of a BigFloat number as a integer. Log2 is equivalent to the number of bits between the point and the right side of the leading bit. (i.e. 100.0=2, 1.0=0, 0.1=-1)
    /// Sign is ignored. Negative values will return the same value as there positive counterpart. Negative exponents are not valid in non-complex math however when using log2 a user might be expecting the number of bits from the radix point to the top bit.
    /// A zero input will follow BigInteger and return a zero, technically however Log2(0) is undefined. Log2 is often use to indicated size in bits so returning 0 with Log2(0) is in-line with this.
    /// </summary>
    /// <param name="n">The BigFloat input argument.</param>
    /// <returns>Returns the Log2 of the value (or exponent) as a Int.</returns>
    public static int Log2Int(BigFloat n)
    {
        return n.BinaryExponent;
    }
}
