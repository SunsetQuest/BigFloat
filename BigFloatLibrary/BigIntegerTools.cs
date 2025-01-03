﻿// Copyright Ryan Scott White. 2020, 2021, 2022, 2023, 2024

// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// Written by human hand - unless noted. This may change soon.
// Code written by Ryan Scott White unless otherwise noted.

using System;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace BigFloatLibrary;

public static class BigIntegerTools
{
    /// <summary>
    /// A high performance BigInteger to binary string converter that supports 0 and negative numbers.
    /// Negative numbers are returned with a leading '-' sign.
    /// </summary>
    private static void BigIntegerToBinarySpan(BigInteger x, ref Span<char> dstBytes)
    {
        bool isNegitive = x.Sign < 0;
        if (isNegitive)
        {
            x = -x;
        }

        // Setup source
        ReadOnlySpan<byte> srcBytes = x.ToByteArray(true, false);
        int srcLoc = srcBytes.Length - 1;

        // Find the first bit set in the first byte so we don't print extra zeros.
        int msb = byte.Log2(srcBytes[srcLoc]);

        // Setup Target
        //Span<char> dstBytes = stackalloc char[srcByte * 8 + MSB + 2];
        int dstLoc = 0;

        // Add leading '-' sign if negative.
        if (isNegitive)
        {
            dstBytes[dstLoc++] = '-';
        }

        // The first byte is special because we don't want to print leading zeros.
        byte b = srcBytes[srcLoc--];
        for (int j = msb; j >= 0; j--)
        {
            dstBytes[dstLoc++] = (char)('0' + ((b >> j) & 1));
        }

        // Add the remaining bits.
        for (; srcLoc >= 0; srcLoc--)
        {
            byte b2 = srcBytes[srcLoc];
            for (int j = 7; j >= 0; j--)
            {
                dstBytes[dstLoc++] = (char)('0' + ((b2 >> j) & 1));
            }
        }
    }

    /// <summary>
    /// A high performance BigInteger to binary string converter that supports 0 and negative numbers.
    /// Negative numbers will be returned as two's complement with no sign. 
    /// The output char[] size will be a multiple of 8. 
    /// </summary>
    private static void BigIntegerToBinarySpanTwosComplement(BigInteger x, ref Span<char> dstBytes)
    {
        // Setup source
        ReadOnlySpan<byte> srcBytes = x.ToByteArray();
        int srcLoc = srcBytes.Length - 1;

        // Setup Target
        int dstLoc = 0;

        // Add the remaining bits.
        for (; srcLoc >= 0; srcLoc--)
        {
            byte b2 = srcBytes[srcLoc];
            for (int j = 7; j >= 0; j--)
            {
                dstBytes[dstLoc++] = (char)('0' + ((b2 >> j) & 1));
            }
        }
    }

    public static string BigIntegerToBinaryString(BigInteger x, bool twosComplement = false)
    {
        if (twosComplement)
        {
            Span<char> charsSpan = stackalloc char[(int)x.GetBitLength() + 7];
            //char[] chars = new char[x.GetBitLength() + 2];
            //Span<char> charsSpan = new(chars);
            BigIntegerToBinarySpanTwosComplement(x, ref charsSpan);
            return new string(charsSpan);
        }
        else
        {
            Span<char> charsSpan = stackalloc char[(int)x.GetBitLength() + ((x < 0) ? 2 : 1)];
            //char[] chars = new char[x.GetBitLength() + ((x < 0) ? 1 : 0)];
            //Span<char> charsSpan = new(chars);
            BigIntegerToBinarySpan(x, ref charsSpan);
            return new string(charsSpan);
        }
    }

    // todo - can we merge this into the fast I made above
    // Source: https://stackoverflow.com/a/15447131/2352507  Kevin P. Rice  2013 (modified by Ryan Scott White)
    public static string ToBinaryString(BigInteger bigIntValue, int padLeadingZeros = 0)
    {
        byte[] bytes = bigIntValue.ToByteArray();
        int idx = bytes.Length - 1;

        // Create a StringBuilder having appropriate capacity.
        StringBuilder base2 = new(bytes.Length * 8);

        // Convert first byte to binary.
        string binary = Convert.ToString(bytes[idx], 2);

        //// Ensure leading zero exists if value is positive.
        //if (binary[0] != '0' && bigint.Sign == 1)
        //{
        //    base2.Append('0');
        //}

        // Append binary string to StringBuilder.
        base2.Append(binary);

        // Convert remaining bytes adding leading zeros.
        for (idx--; idx >= 0; idx--)
        {
            base2.Append(Convert.ToString(bytes[idx], 2).PadLeft(8, '0'));
        }

        return base2.ToString().TrimStart('0').PadLeft(padLeadingZeros, '0');
    }

    /// <summary>
    /// Returns a BigInteger value as a string in Binary with █ and · in place of 1 and 0
    /// </summary>
    /// <param name="padLeadingZeros">Pads the value with leading zeros, in this case, leading '·'.</param>
    public static string ToBinaryShades(BigInteger x, int padLeadingZeros = 0)
    {
        return ToBinaryString(x, padLeadingZeros).Replace('1', '█').Replace('0', '·'); // · ░
    }


    //source: ChatGPT 01-preview on 10-6-2024
    public static string ToBinaryString2(BigInteger bigInt, bool useTwoComplement = false)
    {
        if (bigInt.IsZero)
            return "0";

        if (useTwoComplement)
        {
            // Get the two's complement byte array (little-endian order)
            byte[] bytes = bigInt.ToByteArray();

            StringBuilder sb = new ();

            // Process bytes from most significant to least significant
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                // Convert each byte to its binary representation, padded to 8 bits
                sb.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0'));
            }

            // Remove leading zeros
            string result = sb.ToString().TrimStart('0');

            // Ensure at least one zero is returned for zero values
            return string.IsNullOrEmpty(result) ? "0" : result;
        }
        else
        {
            bool isNegative = bigInt.Sign < 0;
            if (isNegative)
                bigInt = BigInteger.Abs(bigInt);

            StringBuilder sb = new ();

            while (bigInt > 0)
            {
                bigInt = BigInteger.DivRem(bigInt, 2, out BigInteger remainder);
                sb.Insert(0, remainder.ToString());
            }

            if (isNegative)
                sb.Insert(0, "-");

            return sb.ToString();
        }
    }

    
    /// <summary>
    /// Converts the binary text in ReadOnlySpan<char> to a BigInteger. 
    /// If it fails it returns false.
    /// e.g '-11111100.101' would ignore the decimal and set the BigInteger to -252.
    /// </summary>
    /// <param name="input">(out) The binary string input. It should be only [-/+, 0,1,' ', period,comma,_]</param>
    /// <param name="result">The BigInteger result.</param>
    /// <returns>True is successful; False if it fails.</returns>
    public static bool TryParseBinary(ReadOnlySpan<char> input, out BigInteger result)
    {
        int inputLen = input.Length;

        if (inputLen == 0)
        {
            result = new BigInteger(0);
            return false;
        }

        byte[] bytes = new byte[(inputLen + 7) / 8];
        int outputBitPosition = 0;   // The current bit we are writing to.

        // if it starts with a '-' then set negative rawValue to zero
        bool isNeg = input[0] == '-'; // 0x2D;

        // if starting with - or + then headPosition should be 1.
        int headPosition = isNeg | input[0] == '+' ? 1 : 0;

        int periodLoc = input.LastIndexOf('.');
        int tailPosition = (periodLoc < 0 ? inputLen : periodLoc) - 1;
        for (; tailPosition >= headPosition; tailPosition--)
        {
            switch (input[tailPosition])
            {
                case '1':
                    bytes[outputBitPosition >> 3] |= (byte)(1 << (outputBitPosition & 0x7));
                    goto case '0';
                case '0':
                    outputBitPosition++;
                    break;
                case ',' or '_' or ' ': // allow commas, underscores, and spaces (e.g.  1111_1111_0000) (optional - remove for better performance)
                    break;
                default:
                    result = new BigInteger(0);
                    return false; // Function was not successful - unsupported char found
            }
        }

        // If the number is negative, let's perform Two's complement: (1) negate the bits (2) add 1 to the bottom byte
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

        result = new(bytes, !isNeg);

        // return true if success, if no 0/1 bits found then return false.
        return outputBitPosition != 0;
    }

    // The world's fastest sqrt for C# and Java. 
    // https://www.codeproject.com/Articles/5321399/NewtonPlus-A-Fast-Big-Number-Square-Root-Function
    public static BigInteger NewtonPlusSqrt(BigInteger x)
    {
        if (x < 144838757784765629)    // 1.448e17 = ~1<<57
        {
            if (x.Sign < 0)
                throw new ArgumentException("Negitive numbers are not supported.");
            uint vInt = (uint)Math.Sqrt((ulong)x);
            if ((x >= 4503599761588224) && ((ulong)vInt * vInt > (ulong)x))  // 4.5e15 =  ~1<<52
            {
                vInt--;
            }
            return vInt;
        }

        double xAsDub = (double)x;
        if (xAsDub < 8.5e37)   //  long.max*long.max
        {
            ulong vInt = (ulong)Math.Sqrt(xAsDub);
            BigInteger v = (vInt + ((ulong)(x / vInt))) >> 1;
            return (v * v <= x) ? v : v - 1;
        }

        if (xAsDub < 4.3322e127)
        {
            BigInteger v = (BigInteger)Math.Sqrt(xAsDub);
            v = (v + (x / v)) >> 1;
            if (xAsDub > 2e63)
            {
                v = (v + (x / v)) >> 1;
            }
            return (v * v <= x) ? v : v - 1;
        }

        int xLen = (int)x.GetBitLength();
        //int xLen = (int)Math.Ceiling(BigInteger.Log(x, 2));

        int wantedPrecision = (xLen + 1) / 2;
        int xLenMod = xLen + (xLen & 1) + 1;

        //////// Do the first Sqrt on hardware ////////
        long tempX = (long)(x >> (xLenMod - 63));
        double tempSqrt1 = Math.Sqrt(tempX);
        ulong valLong = (ulong)BitConverter.DoubleToInt64Bits(tempSqrt1) & 0x1fffffffffffffL;
        if (valLong == 0)
        {
            valLong = 1UL << 53;
        }

        ////////  Classic Newton Iterations ////////
        BigInteger val = ((BigInteger)valLong << 52) + (x >> xLenMod - (3 * 53)) / valLong;
        int size = 106;
        for (; size < 256; size <<= 1)
        {
            val = (val << (size - 1)) + (x >> xLenMod - (3 * size)) / val;
            //BigInteger temp1 = (val << (size - 1));
            //int temp2 = (xLenMod - (3 * size));
            //BigInteger temp3 = (x >> temp2);
            //BigInteger temp4 = temp3 / val;
            //val = temp1 + temp4;
        }

        if (xAsDub > 4e254) // 4e254 = 1<<845.76973610139
        {
            int numOfNewtonSteps = BitOperations.Log2((uint)(wantedPrecision / size)) + 2;

            //////  Apply Starting Size  ////////
            int wantedSize = (wantedPrecision >> numOfNewtonSteps) + 2;
            int needToShiftBy = size - wantedSize;
            val >>= needToShiftBy;
            size = wantedSize;
            do
            {
                ////////  Newton Plus Iterations  ////////
                int shiftX = xLenMod - (3 * size);
                BigInteger valSqrd = (val * val) << (size - 1);
                BigInteger valSU = (x >> shiftX) - valSqrd;
                val = (val << size) + (valSU / val);
                size *= 2;

            } while (size < wantedPrecision);
        }

        /////// There are a few extra digits here, lets save them ///////
        int oversidedBy = size - wantedPrecision;
        BigInteger saveDroppedDigitsBI = val & ((BigInteger.One << oversidedBy) - 1);
        int downby = (oversidedBy < 64) ? (oversidedBy >> 2) + 1 : (oversidedBy - 32);
        ulong saveDroppedDigits = (ulong)(saveDroppedDigitsBI >> downby);


        ////////  Shrink result to wanted Precision  ////////
        val >>= oversidedBy;


        ////////  Detect a round-ups  ////////
        if ((saveDroppedDigits == 0) && (val * val > x))
        {
            val--;
        }

        ////////// Error Detection ////////
        //// I believe the above has no errors but to guarantee the following can be added.
        //// If an error is found, please report it.
        //BigInteger tmp = val * val;
        //if (tmp > x)
        //{
        //    Console.WriteLine($"Missed  , {ToolsForOther.ToBinaryString(saveDroppedDigitsBI, oversidedBy)}, {oversidedBy}, {size}, {wantedPrecision}, {saveDroppedDigitsBI.GetBitLength()}");
        //    if (saveDroppedDigitsBI.GetBitLength() >= 6)
        //        Console.WriteLine($"val^2 ({tmp}) < x({x})  off%:{((double)(tmp)) / (double)x}");
        //    //throw new Exception("Sqrt function had internal error - value too high");
        //}
        //if ((tmp + 2 * val + 1) <= x)
        //{
        //    Console.WriteLine($"(val+1)^2({((val + 1) * (val + 1))}) >= x({x})");
        //    //throw new Exception("Sqrt function had internal error - value too low");
        //}

        return val;
    }

    public static BigInteger NewtonNthRootV5_3_31(ref BigInteger x, int n)
    {
        if (x == 0) return 0; // The n-th root of 0 is 0.
        if (n == 1) return x; // The 1st  root of x is x itself.
        if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

        int xLen = (int)x.GetBitLength();

        // If xLen is over 1023 bits, reduce the size of x to fit in a double
        int scaleDownCount = Math.Max(0, ((xLen - 1024) / n) + 1);
        BigInteger scaledX = x >> n * scaleDownCount; // Right-shift x by n bits

        // Calculate initial guess using scaled down x
        double initialGuess = Math.Pow((double)scaledX, 1.0 / n);


        long bits = BitConverter.DoubleToInt64Bits(initialGuess);
        // Note that the shift is sign-extended, hence the test against -1 not 1
        //bool negative = (bits & (1L << 63)) != 0;
        int exponent = (int)((bits >> 52) & 0x7ffL) - 1075;
        long mantissa = bits & 0xfffffffffffffL | (1L << 52); ;

        BigInteger val = new(mantissa);
        scaleDownCount += exponent;

        //BigInteger val = new BigInteger(initialGuess);  //241
        Console.WriteLine(val.GetBitLength() + " + " + scaleDownCount + " = " + (val.GetBitLength() + scaleDownCount));
        val <<= scaleDownCount;
        Console.WriteLine(BigIntegerToBinaryString(val));

        int loops = 0;
        int ballparkSize = 100;
        int estSize = xLen / n + 1;
        BigInteger lastVal = 0;

        //////////////////////////////////////////////////////////////

        while (val != lastVal) // Repeat until convergence
        {
            int reduceBy = Math.Max(0, estSize - ballparkSize);
            lastVal = val;
            int valSize = (int)val.GetBitLength();
            BigInteger pow = PowMostSignificantBits(val, n - 1, out int shifted, valSize, valSize - reduceBy);

            BigInteger numerator = ((pow * (val >> (reduceBy * 1)))) - (x >> (shifted + reduceBy * 1));
            BigInteger denominator = (n * pow) >> (reduceBy * 0);
            val = ((val >> reduceBy) - numerator / denominator) << reduceBy;

            loops++;
            ballparkSize *= 2;
        }
        Console.WriteLine($"Loops:{loops}  ballparkSize{ballparkSize}/{val.GetBitLength()}");

        return val;
    }

    public static BigInteger NewtonNthRootV4(ref BigInteger x, int n)
    {
        if (x == 0) return 0; // The n-th root of 0 is 0.
        if (n == 1) return x; // The 1st  root of x is x itself.
        if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

        int xLen = (int)x.GetBitLength();

        // If xLen is over 1023 bits, reduce the size of x to fit in a double
        int scaleDownCount = Math.Max(0, ((xLen - 1024) / n) + 1);
        BigInteger scaledX = x >> n * scaleDownCount; // Right-shift x by n bits

        // Calculate initial guess using scaled down x
        double initialGuess = Math.Pow((double)scaledX, 1.0 / n);

        BigInteger val = new(initialGuess);

        val <<= scaleDownCount;
        Console.WriteLine(BigIntegerToBinaryString(val));

        int loops = 0;
        int ballparkSize = 50;
        int estSize = xLen / n + 1;
        BigInteger lastVal = 0;
        while (val != lastVal) // Repeat until convergence
        {
            int reduceBy = Math.Max(0, estSize - ballparkSize);
            lastVal = val;
            BigInteger pow = PowMostSignificantBits(val, n - 1, out int shifted);

            BigInteger numerator = ((pow * val) >> reduceBy) - (x >> (shifted + reduceBy));
            BigInteger denominator = (n * pow) >> reduceBy;
            val -= numerator / denominator;

            loops++;
            ballparkSize *= 2;
        }
        Console.WriteLine($"Loops:{loops}  ballparkSize{ballparkSize}/{val.GetBitLength()}");

        return val;
    }


    public static BigInteger NewtonNthRootV3_3_27_last(ref BigInteger x, int n)
    {
        if (x == 0) return 0; // The n-th root of 0 is 0.
        if (n == 1) return x; // The 1st  root of x is x itself.
        if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

        int xLen = (int)x.GetBitLength();
        BigInteger scaledX = x;

        // If xLen is over 1023 bits, reduce the size of x to fit in a double
        int scaleDownCount = (xLen - 1024 + n) / n;
        scaledX >>= n * scaleDownCount; // Right-shift x by n bits

        // Calculate initial guess using scaled down x
        double initialGuess = Math.Pow((double)scaledX, 1.0 / n);

        // Adjust the initial guess by scaling it back up
        BigInteger val = new(initialGuess);

        val <<= scaleDownCount;


        //Console.WriteLine(val.ToString());

        int loops = 0;
        BigInteger lastVal = 0;
        while (val != lastVal) // Repeat until convergence
        {
            lastVal = val;
            BigInteger pow = BigInteger.Pow(val, n - 1);
            BigInteger numerator = pow * val - x;
            BigInteger denominator = n * pow;
            val -= numerator / denominator;

            loops++;
        }
        Console.WriteLine(loops);

        return val;
    }




    //future: Create BigFloat version of PowMostSignificantBits()
    //todo: set to private
    /// <summary>
    /// Returns the top n bits for a BigInteger raised to a power. 
    /// If <param name="wantedBits"> is not specified, the output precision will match <param name="valSize">. 
    /// The number of removed bits are returned in in totalShift. 
    /// The returned result, left shifted by <param name="totalShift">, would return the actual result.
    /// The result is rounded using the top most removed bit. 
    /// When the result is rounded in some borderline cases (e.g. 100|011111), the result can occasionally 
    /// round-up. When it rounds-up, it will be in the upward direction only. This is less likely 
    /// if <param name="extraAccurate"> is true. There are no known rounding errors at this time with <param name="extraAccurate"> enabled.
    /// </summary>
    /// <param name="val">The input value.</param>
    /// <param name="valSize">The input values size. This can be left at zero if unknown.</param>
    /// <param name="exp">The exponent to raise the value by.</param>
    /// <param name="totalShift">(out) The number of bits that were removed from the result.</param>
    /// <param name="wantedBits">The number of bits to return. A unspecified value or a value less then 0 will default 
    /// to the inputs size. A value too large will be limited to <param name="valSize">. </param>
    /// <param name="extraAccurate">When false, about 1-in-4096 will round up when it shouldn't. When true, accuracy 
    /// is much better but performance is slower.</param>
    /// <returns>The top bits val raised to the power of exp.</returns>
    public static BigInteger PowMostSignificantBits(BigInteger val, int exp, out int totalShift, int valSize = -1, int wantedBits = 0, bool extraAccurate = false, bool roundDown = false)
    {
        totalShift = 0;

        if (valSize <= 0)
        {
            if (val.IsZero)
            {
                return BigInteger.Zero;
            }

            valSize = (int)val.GetBitLength();
        }
        else
        {
#if DEBUG
            // Make sure the supplied valSize size is set correctly.
            Debug.Assert(BigInteger.Abs(val).GetBitLength() == valSize, $"The supplied {nameof(valSize)} is not correct.");
#endif
        }

        // Lets make sure the number of wanted bits is valid.
        if (wantedBits == 0)
        {
            wantedBits = valSize;
        }
        else if (wantedBits > valSize)
        {
            // 3 choices:
            // A) either shrink wanted bits to valSize
            //wantedBits = valSize;

            // B) or, make val larger
            int growBy = wantedBits - valSize;
            val <<= growBy;
            valSize += growBy;
            totalShift = -growBy * exp;

            // C) or, just throw an error
            //throw new OverflowException("The val's size is less then the wantedBits.");
        }

        if (((long)exp * valSize) >= int.MaxValue)
        {
            throw new OverflowException("Overflow: The output 'totalShift' would be too large to fit in an 'int'. (exp * size > int.maxSize");
        }

        if (exp < 3)
        {
            BigInteger result;
            switch (exp)
            {
                case 0:
                    result = BigInteger.One; //totalShift = 0
                    break;
                case 1:
                    totalShift = valSize - wantedBits;
                    if (roundDown)
                    {
                        result = RightShiftWithRound(val, totalShift);
                    }
                    else
                    {
                        bool carried1 = RightShiftWithRoundWithCarryDownsize(out result, val, totalShift, valSize);
                        if (carried1)
                        {
                            totalShift++;
                        }
                    }
                    break;
                case 2:
                    BigInteger sqr = val * val;
                    int sqrSize = (2 * valSize) - ((sqr >> ((2 * valSize) - 1) > 0) ? 0 : 1);
                    totalShift = sqrSize - wantedBits;
                    if (roundDown)
                    {
                        result = RightShiftWithRound(sqr, totalShift);
                    }
                    else
                    {
                        bool carried1 = RightShiftWithRoundWithCarryDownsize(out result, sqr, totalShift, sqrSize);
                        if (carried1)
                        {
                            totalShift++;
                        }
                    }
                    break;

                default: // negative exp would be less then 1 (unless 1)
                    result = val != 1 ? BigInteger.Zero : val.Sign;
                    break;

            }
            return result;

        }


        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if ((wantedBits > 2) && (wantedBits < 53) && (valSize * exp) < 3807)
        {
            //// Lets first make sure we would have some precision remaining after our exponent operation.
            if (valSize == 0)
            {
                return BigInteger.Zero; // technically more of a "NA".
            }

            // 1) create a double with the bits. 
            // Aline input to the top 53 bits then pre-append a "1" bit.
            long inMantissa = (long)(BigInteger.Abs(val) << (53 - valSize));
            long dubAsLong = inMantissa | ((long)1023 << 52);
            double normInput = BitConverter.Int64BitsToDouble(dubAsLong);

            // 2) perform a power
            double normPow = double.Pow(normInput, exp);
            if (normPow == double.PositiveInfinity)
            {
                throw new OverflowException($"Internal Error: PositiveInfinity valSize:{valSize} exp:{exp} val:{val} wantedBits:{wantedBits}");
            }

            // 3) extract "bottom 52 bits" and that is our answer.
            long bits = BitConverter.DoubleToInt64Bits(normPow);
            long outMantissa = (bits & 0xfffffffffffffL) | 0x10000000000000L;

            int bitsToDrop = 53 - wantedBits;  // wantedBits OR size????
            long mask1 = ((long)1 << bitsToDrop) - 1;  // OR ((long)1 << (53 - size)) - 1  ?????

            // no known issues if val < 13511613  OR removed bits are not all 1's
            if ((~(outMantissa & mask1)) >= 0 || val < 13511613)
            {
                int outExp = (int)(bits >> 52) - 1023;
                totalShift += ((valSize - 1) * (exp - 1)) + outExp + (valSize - wantedBits)  /*+ (1<<(expSz-2))*/;

                // outMantissa is 53 in size at this point
                // we need to Right Shift With Round but if it rounds up to a larger number (e.g. 1111->10000) then we must increment totalShift.
                bool roundsUp = ((outMantissa >> (bitsToDrop - 1)) & 0x1) > 0;
                if (!roundsUp)
                {
                    return outMantissa >> bitsToDrop;
                }

                long withRoundUp = (outMantissa >> bitsToDrop) + 1;

                // if carried to the 54th place then it rolled over and we must shrink by one.
                if ((withRoundUp >> (53 - bitsToDrop)) > 0)
                {
                    withRoundUp >>= 1;
                    totalShift++;
                }

                return withRoundUp;
            }
        }

        int workingSize;
        int expSz = BitOperations.Log2((uint)exp) + 1;

        if (extraAccurate)
        {
            // This version is more accurate but is slower. There is just one known incident when it does not round up like it should.
            // JUST ONE KNOWN ROUND ERROR between 16 to 20 is 51^17938 (no known rounding error when extraPrecisionBits is above 20)
            //   searches @16: (1-2000)^(2-39,999), (1-126,000)^(2-3999), (1-134,654,818)^(1-1500)
            workingSize = (2 * wantedBits) + expSz + 22/*extraPrecisionBits(adjustable)*/;
        }
        else
        {
            // Odds of an incorrect round-up(ex: 7.50001 not rounding up to 8) ~= 18.12/(2^ExtraBits)
            //   0=18.1%; 1=9.1%; 2=4.5%; 3=2.3%; 4=1.1%; 5=0.6%; 8=1/4096
            workingSize = wantedBits + expSz + 8/*extraPrecisionBits(adjustable)*/;
        }


        // First Loop
        BigInteger product = ((exp & 1) > 0) ? val : 1;
        BigInteger powerPostShift = val;
        int shiftSum = 0;
        int shift = 0;

        // Second Loop
        BigInteger pwrPreShift = powerPostShift * powerPostShift;
        int prdSize = (valSize * 2) - (((pwrPreShift >> ((valSize * 2) - 1)) > 0) ? 0 : 1);
        int H = valSize + prdSize;  //OR  size + shift
        int J = ((exp & 0x1) == 1) ? 0 : valSize;
        int I = 0;

        powerPostShift = pwrPreShift;
        if ((exp & 0x2) > 0)
        {
            I = H - workingSize;
            int shrinkSize = I - J;
            J = 0;
            product = (product * powerPostShift) >> shrinkSize;
            totalShift += shrinkSize;
        }
        else
        {
            J += prdSize;
        }

        // for each bit in the exponent, we need to multiply in 2^position
        for (int i = 2; i < expSz; i++)
        {
            pwrPreShift = powerPostShift * powerPostShift;

            // checks if a leading bit resulted from the multiply and if so adds it.
            int tmp = ((prdSize - shift) * 2) - 1;
            prdSize = tmp + (int)(pwrPreShift >> tmp);

            shift = Math.Max(prdSize - workingSize, 0);
            H += prdSize - shift - I;

            //powerPostShift = RightShiftWithRound(pwrPreShift, shift);  ///better precision by 1.7 buts but 25% slower
            powerPostShift = pwrPreShift >> shift; // 25% faster; 5 times more round errors; always one direction(good thing)

            shiftSum = (shiftSum * 2) + shift;
            bool bit = ((exp >> i) & 1) == 1;
            if (bit)
            {
                I = H - workingSize;
                int shrinkSize = I - J;
                J = 0;
                product = (product * powerPostShift) >> shrinkSize;
                totalShift += shrinkSize + shiftSum;
            }
            else
            {
                I = 0;
                J += prdSize - shift;  //OR  shift OR prdSize - shift
            }
        }

        int productSize = (int)product.GetBitLength();
        int bitsToRemove = productSize - wantedBits;

        totalShift += bitsToRemove;

        bool carry = RightShiftWithRoundWithCarryDownsize(out BigInteger res, product, bitsToRemove, productSize);
        if (carry)
        {
            totalShift++;
        }

        return res;
    }

    //source: ChatGPT 10-6-2024 o1
    public static BigInteger ConcatenateBigIntegers(BigInteger a, BigInteger b)
    {
        // Assume both BigIntegers are positive (ignore negative numbers)
        // Get the byte counts needed for 'a' and 'b'
        int byteCountA = a.GetByteCount();//a.GetByteCount(isUnsigned: true);
        int byteCountB = b.GetByteCount();//.GetByteCount(isUnsigned: true);

        // Allocate a single buffer for the concatenated bytes
        byte[] concatenatedBytes = new byte[byteCountA + byteCountB];
        Span<byte> concatenatedSpan = concatenatedBytes.AsSpan();

        // Write the bytes of 'a' into the first part of the span
        if (!a.TryWriteBytes(concatenatedSpan[..byteCountA], out int bytesWrittenA, isUnsigned: false, isBigEndian: true) ||
            bytesWrittenA != byteCountA)
        {
            throw new InvalidOperationException("Failed to write bytes of BigInteger 'a'");
        }

        // Write the bytes of 'b' into the second part of the span
        if (!b.TryWriteBytes(concatenatedSpan.Slice(byteCountA, byteCountB), out int bytesWrittenB, isUnsigned: false, isBigEndian: true) ||
            bytesWrittenB != byteCountB)
        {
            throw new InvalidOperationException("Failed to write bytes of BigInteger 'b'");
        }

        // Create a new BigInteger from the concatenated bytes
        BigInteger result = new(concatenatedSpan, isUnsigned: true, isBigEndian: true);
        return result;
    }

    //source: ChatGPT 10-6-2024 o1  (modified by Ryan)
    public static BigInteger ConcatenateBigIntegers2(BigInteger a, BigInteger b)
    {
        // Assume both BigIntegers are positive (ignore negative numbers)
        // Get the byte counts needed for 'a' and 'b'
        int byteCountA = a.GetByteCount(isUnsigned: true);
        int byteCountB = b.GetByteCount(isUnsigned: true);

        // Allocate a single buffer for the concatenated bytes
        byte[] concatenatedBytes = new byte[byteCountA + byteCountB];
        Span<byte> concatenatedSpan = concatenatedBytes.AsSpan();

        a.TryWriteBytes(concatenatedSpan[..], out int _, isUnsigned: false, isBigEndian: true);
        b.TryWriteBytes(concatenatedSpan[byteCountA..], out int _, isUnsigned: false, isBigEndian: true);

        // Create a new BigInteger from the concatenated bytes
        BigInteger result = new(concatenatedSpan, isUnsigned: true, isBigEndian: true);
        return result;
    }

    public static BigInteger InverseBigIntegerClassic(BigInteger x, int requestedPrecision)
     => (BigInteger.One << ((int)x.GetBitLength() + requestedPrecision - 1)) / x;


    public static BigInteger InverseBigInteger(BigInteger x, int requestedPrecision = 0)
    {
        int xLen = (int)x.GetBitLength();
        if (requestedPrecision <= 0)
        {
            if (requestedPrecision < 0)
                throw new DivideByZeroException("'precisionBits' can not be negative.");
            requestedPrecision = xLen;
        }

        bool isPos = x.Sign >= 0;
        if (!isPos)
            x = -x;

        // Trailing Zeros never matter
        int trailingZeros = (int)BigInteger.TrailingZeroCount(x);
        x >>= trailingZeros;
        xLen -= trailingZeros;

        if (xLen < 65)
        {
            // or if xLen = 33 then requestedPrecision <34
            if (xLen < 33)
            {
                ulong result64 = (1UL << 63) / (ulong)x; // 63 /  31
                result64 >>= 64 - (int)ulong.LeadingZeroCount(result64) - requestedPrecision;
                return isPos ? result64 : (-(BigInteger)result64);
            }
            UInt128 result128 = (UInt128.One << 127) / (ulong)x;
            result128 >>= (int)UInt128.Log2(result128) - requestedPrecision + 1; //what about uint.Log2(res>>32) +32
            return isPos ? result128 : (-(BigInteger)result128);
        }

        if (xLen < 512)
        {
            BigInteger resultBI = (BigInteger.One << (xLen + requestedPrecision - 1)) / x;
            return isPos ? resultBI : (-resultBI);
        }

        ////////  Get Starting Size  ////////
        const int EXTRA = 8;
        int desiredStartSize = requestedPrecision + EXTRA * 2;
        while (desiredStartSize > 330)
            desiredStartSize = (desiredStartSize >> 1) + 1;




        //BigInteger result = (BigInteger.One << ((int)x.GetBitLength() + requestedPrecision - 1)) / x;
        BigInteger result;
        while (true)
        {
            BigInteger scaledOne2 = (BigInteger.One << ((desiredStartSize << 1) + EXTRA * 2));
            result = scaledOne2 / (x >> (xLen - desiredStartSize - 1 - EXTRA));
            //issue here when 1000000000000000000000000
            int reduceBy2 = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1;
            result >>= reduceBy2;
            int desiredStartSize2 = desiredStartSize - reduceBy2 + EXTRA;
            if (desiredStartSize2 > 100)
            {
                desiredStartSize = desiredStartSize2;
                break;
            }
            desiredStartSize <<= 1;
        }



        ////////////////////// Newton version  //////////////////////
        int EXTRA_BITS_TO_REMOVE = 1;
        while (desiredStartSize <= requestedPrecision)
        {
            int doubleDesiredStartSize = (desiredStartSize << 1);

            BigInteger scalingFactor = BigInteger.One << (doubleDesiredStartSize + 1);
            BigInteger xTimesY = ((x >> (xLen - doubleDesiredStartSize)) * result) >> (desiredStartSize - 1); // future: we only need the bottom half of this.
            BigInteger twoMinusXy = scalingFactor - xTimesY;
            result = (result * twoMinusXy) >> (desiredStartSize + EXTRA_BITS_TO_REMOVE);

            int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1; // need one for things like ..100000
            result >>= reduceBy;

            desiredStartSize = doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE;

            // When we reach out 1000 bits lets move to NewtonPlus as it is slightly faster.
            if (desiredStartSize > 1024)
                break;
        }

        ////////////////////// NewtonPlus version  //////////////////////
        EXTRA_BITS_TO_REMOVE = 1;
        while (desiredStartSize <= requestedPrecision)
        {
            int doubleDesiredStartSize = (desiredStartSize << 1);

            // We need insert our "1" in the middle, we do this by incrementing the upper half with a 1
            result++; // we could just do a add a "(1 << doubleDesiredStartSize)"
            BigInteger mask22 = (BigInteger.One << (desiredStartSize + 1)) - 1;
            BigInteger xTimesY22 = ((x >> (xLen - doubleDesiredStartSize)) * result) >> (desiredStartSize - 1); // future: we only need the bottom half of this.
            result = ((result << (doubleDesiredStartSize)) - (result * (xTimesY22 & mask22))) >> (desiredStartSize + EXTRA_BITS_TO_REMOVE);

            //// back off until we see both a zero and one
            int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : ~result) + 1;
            result >>= reduceBy;

            // Check if correct so far and output info if not
            BigInteger checkResult = (BigInteger.One << (xLen + xLen - 1)) / x;
            int correctBits2 = ToBinaryString(result).Zip(ToBinaryString(checkResult), static (c1, c2) => c1 == c2).TakeWhile(static b => b).Count();
            if (correctBits2 < Math.Min(requestedPrecision, doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE))
                Console.WriteLine($"not 100% !!\r\nAns: {ToBinaryString(checkResult)}[{checkResult.GetBitLength()}]\r\nRes: {ToBinaryString(result)}[{result.GetBitLength()}]");

            desiredStartSize = doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE;
        }

        BigInteger tempResult = result >> desiredStartSize - requestedPrecision;
        return isPos ? tempResult : (-tempResult);
    }



    public static BigInteger InverseBigInteger7(BigInteger x, int requestedPrecision)
    {
        int xLen = (int)x.GetBitLength();
        if (requestedPrecision > xLen)
        {
            throw new DivideByZeroException("'precisionBits' can not be greater then x's length.");
        }

        bool isPos = x.Sign >= 0;
        if (!isPos)
            x = -x;

        if (xLen < 65)
        {
            if (xLen < 33)
            {
                ulong result64 = (1UL << 63) / (ulong)x; // 63 /  31
                result64 >>= 64 - (int)ulong.LeadingZeroCount(result64) - requestedPrecision;
                return isPos ? result64 : (-(BigInteger)result64);
            }
            UInt128 result128 = (UInt128.One << 127) / (ulong)x;
            result128 >>= (int)UInt128.Log2(result128) - requestedPrecision + 1; //what about uint.Log2(res>>32) +32
            return isPos ? result128 : (-(BigInteger)result128);
        }

        if (xLen < 512)
        {
            BigInteger resultBI = (BigInteger.One << (xLen + requestedPrecision - 1)) / x;
            return isPos ? resultBI : (-resultBI);
        }

        ////////  Get Starting Size  ////////
        const int EXTRA = 8;
        int desiredStartSize = requestedPrecision + EXTRA * 2;
        while (desiredStartSize > 330)
            desiredStartSize = (desiredStartSize >> 1) + 1;

        BigInteger scaledOne2 = (BigInteger.One << ((desiredStartSize << 1) + EXTRA * 2));
        BigInteger result = scaledOne2 / (x >> (xLen - desiredStartSize - 1 - EXTRA));

        int reduceBy2 = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1;
        result >>= reduceBy2;

        desiredStartSize = desiredStartSize - reduceBy2 + EXTRA;

        ////////////////////// Newton version  //////////////////////
        int EXTRA_BITS_TO_REMOVE = 1;
        while (desiredStartSize <= requestedPrecision)
        {
            int doubleDesiredStartSize = (desiredStartSize << 1);

            BigInteger scalingFactor = BigInteger.One << (doubleDesiredStartSize + 1);
            BigInteger xTimesY = ((x >> (xLen - doubleDesiredStartSize)) * result) >> (desiredStartSize - 1); // future: we only need the bottom half of this.
            BigInteger twoMinusXy = scalingFactor - xTimesY;
            result = (result * twoMinusXy) >> (desiredStartSize + EXTRA_BITS_TO_REMOVE);

            int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1; // need one for things like ..100000
            result >>= reduceBy;

            desiredStartSize = doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE;

            // When we reach out 1000 bits lets move to NewtonPlus as it is slightly faster.
            if (desiredStartSize > 1024)
                break;
        }

        ////////////////////// NewtonPlus version  //////////////////////
        EXTRA_BITS_TO_REMOVE = 1;
        while (desiredStartSize <= requestedPrecision)
        {
            int doubleDesiredStartSize = (desiredStartSize << 1);

            // We need insert our "1" in the middle, we do this by incrementing the upper half with a 1
            result++; // we could just do a add a "(1 << doubleDesiredStartSize)"
            BigInteger mask22 = (BigInteger.One << (desiredStartSize + 1)) - 1;
            BigInteger xTimesY22 = ((x >> (xLen - doubleDesiredStartSize)) * result) >> (desiredStartSize - 1); // future: we only need the bottom half of this.
            result = ((result << (doubleDesiredStartSize)) - (result * (xTimesY22 & mask22))) >> (desiredStartSize + EXTRA_BITS_TO_REMOVE);

            //// back off until we see both a zero and one
            int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : ~result) + 1;
            result >>= reduceBy;

            // Check if correct so far and output info if not
            BigInteger checkResult = (BigInteger.One << (xLen + xLen - 1)) / x;
            int correctBits2 = ToBinaryString(result).Zip(ToBinaryString(checkResult), static (c1, c2) => c1 == c2).TakeWhile(static b => b).Count();
            if (correctBits2 < Math.Min(requestedPrecision, doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE))
                Console.WriteLine($"not 100% !!\r\nAns: {ToBinaryString(checkResult)}[{checkResult.GetBitLength()}]\r\nRes: {ToBinaryString(result)}[{result.GetBitLength()}]");

            desiredStartSize = doubleDesiredStartSize - reduceBy - EXTRA_BITS_TO_REMOVE;
        }

        BigInteger tempResult = result >> desiredStartSize - requestedPrecision;
        return isPos ? tempResult : (-tempResult);
    }


    /// <summary>
    /// A high performance BigInteger to binary string converter
    /// that supports 0 and negative numbers.
    /// License: MIT / Created by Ryan Scott White, 7/16/2022;
    /// https://stackoverflow.com/a/73009264/2352507
    /// </summary>
    public static string ToBinaryString3(BigInteger x)
    {
        // Setup source
        ReadOnlySpan<byte> srcBytes = x.ToByteArray();
        int srcLoc = srcBytes.Length - 1;

        // Find the first bit set in the first byte so we don't print extra zeros.
        int msb = BitOperations.Log2(srcBytes[srcLoc]);

        // Setup Target
        Span<char> dstBytes = stackalloc char[srcLoc * 8 + msb + 2];
        int dstLoc = 0;

        // Add leading '-' sign if negative.
        if (x.Sign < 0)
        {
            dstBytes[dstLoc++] = '-';
        }
        //else if (!x.IsZero) dstBytes[dstLoc++] = '0'; // add adding leading '0' (optional)

        // The first byte is special because we don't want to print leading zeros.
        byte b = srcBytes[srcLoc--];
        for (int j = msb; j >= 0; j--)
        {
            dstBytes[dstLoc++] = (char)('0' + ((b >> j) & 1));
        }

        // Add the remaining bits.
        for (; srcLoc >= 0; srcLoc--)
        {
            byte b2 = srcBytes[srcLoc];
            for (int j = 7; j >= 0; j--)
            {
                dstBytes[dstLoc++] = (char)('0' + ((b2 >> j) & 1));
            }
        }

        return dstBytes.ToString();
    }

    /////////////////////////////////
    ////      WouldRound()
    /////////////////////////////////


    public static bool WouldRound(BigInteger val, int bottomBitsRemoved)
    {
        // for .net 7 and later use ">>>" instead of >> for a slight performance boost.
        bool isPos = val.Sign >= 0;
        return isPos ^ ((isPos ? val : val - 1) >> (bottomBitsRemoved - 1)).IsEven;
    }


    /////////////////////////////////////////////
    ////      RightShift() for BigInteger    ////
    /////////////////////////////////////////////

    // Performance idea: what about doing:  rolledOver = (x == (1 << x.bitLen))   (do this before the inc for neg numbers and do this after the inc for pos numbers)
    // Performance idea: what about doing:  "(b & uint.MaxValue) == 0" first as a quick check. (or use x.IsPowerOfTwo)
    // Performance idea: bool rolledOver = b.IsPowerOfTwo || (b<<1).IsPowerOfTwo; 

    /// <summary>
    /// Removes x number of bits of precision. 
    /// A special case of RightShift(>>) that will round based off the most significant bit in the removed bits(bitsToRemove).
    /// This function will not adjust the scale. Like any shift, the value with be changed by some power of 2.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    ///   e.g. RightShiftWithRound(0b111, 1) --> 0b100
    /// Notes: 
    /// * Works on positive and negative numbers. 
    /// * If the part being removed has the most significant bit set, then the result will be rounded away from zero. 
    /// </summary>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="targetBitsToRemove">The target number of bits to reduce the precision.</param>
    /// <returns>The rounded result of shifting val to the right by bitsToRemove.</returns>
    public static BigInteger RightShiftWithRound(BigInteger val, in int targetBitsToRemove)
    {
        // if bitsToRemove is negative, we would up-shift and no rounding is needed.
        if (targetBitsToRemove < 0)
        {
            return val >> targetBitsToRemove;
        }

        // BigInteger will automatically round when down-shifting larger negative values.
        if (val.Sign < 0) val--;

        BigInteger result2 = val >> targetBitsToRemove;

        if (!(val >>> (targetBitsToRemove - 1)).IsEven)
        {
            result2++;
        }

        return result2;
    }

    /// <summary>
    /// Removes x number of bits of precision. It also requires the current size and will increment it if it grows by a bit.
    /// If the most significant bit of the removed bits is set, then the least significant bit will increment away from zero. 
    /// e.g. 1010010 << 2 = 10101
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// THIS FUNCTION IS HIGHLY TUNED!
    /// </summary>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="targetBitsToRemove">The target number of bits to reduce the precision.</param>
    /// <param name="size">IN: the size of Val.  OUT: The size of the output.</param>
    public static BigInteger RightShiftWithRound(BigInteger val, in int targetBitsToRemove, ref int size)
    {
        size = Math.Max(0, size - targetBitsToRemove);

        if (val.Sign >= 0)
        {
            BigInteger result = val >>> targetBitsToRemove;

            if (!(val >>> (targetBitsToRemove - 1)).IsEven)
            {
                result++;

                if ((result >> size).IsOne)
                {
                    size++;
                }
            }
            return result;
        }

        // is Neg

        val--;

        BigInteger result2 = val >> targetBitsToRemove;

        if ((val >>> (targetBitsToRemove - 1)).IsEven)
        {
            if (((result2 - 1) >>> size).IsEven)
            {
                size++;
            }
        }
        else
        {
            result2++;
        }

        return result2;
    }

    /// <summary>
    /// Removes x number of bits of precision.
    /// If the most significant bit of the removed bits is set, then the least significant bit will increment away from zero. 
    /// e.g. 1010010 << 2 = 10101
    /// Caution: Round-ups may percolate to the most significate bit. This function will automaticlly remove that extra bit. 
    /// e.g. 1111111 << 2 = 10000
    /// Also see: ReducePrecision, TruncateByAndRound, RightShiftWithRoundWithCarryDownsize
    /// </summary>
    /// <param name="result">The result of val being right shifted and rounded. The size will be "size-bitsToRemove".</param>
    /// <param name="val">The source BigInteger we would like right-shift.</param>
    /// <param name="bitsToRemove">The number of bits that will be removed.</param>
    /// <param name="size">The size of the input value if available. If negative number then val.GetBitLength() is called.</param>
    /// <returns>Returns True if an additional bit needed to be removed to achieve the desired size because of a round up. 
    /// e.g. 1111111 << 2 = 10000</returns>
    public static bool RightShiftWithRoundWithCarryDownsize(out BigInteger result, BigInteger val, in int bitsToRemove, int size = -1)
    {
        if (size < 0)
        {
            size = (int)val.GetBitLength();
        }

        size = Math.Max(0, size - bitsToRemove);

        if (val.Sign >= 0)
        {
            result = val >> bitsToRemove; // on .net 7 and later use >>> instead of >> for a slight performance boost

            if (!(val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                result++;

                if ((result >> size).IsOne)
                {
                    //rounded up to larger size so remove zero to keep it same size.
                    result >>= 1;
                    return true;
                }
                return false;
            }
        }
        else // is Neg
        {
            val--;

            result = val >> bitsToRemove;

            if ((val >> (bitsToRemove - 1)).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
            {
                if (((result - 1) >> size).IsEven) // on .net 7 and later use >>> instead of >> for a slight performance boost
                {
                    result >>= 1;
                    return true;
                }
            }
            else
            {
                result++;
            }
        }

        return false;
    }

    ///////////////////////////////////////////////////
    ////      Set/Reduce Precision for BigFloat    ////
    ///////////////////////////////////////////////////

    /// <summary>
    /// This function will reduce the precision of a BigInteger to the number of bits specified.
    /// If the part being removed has the most significant bit set, then the result will be rounded 
    /// away from zero. This can be used to reduce the precision prior to a large calculation.
    /// Caution: Round-ups may percolate to the most significant bit, adding an extra bit to the size. 
    /// Example: SetPrecisionWithRound(15, 3) = 8[4 bits]
    /// Also see: SetPrecision, TruncateToAndRound
    /// <param name="targetNewSizeInBits">The new requested size. The resulting size might be rounded up.</param>
    public static BigInteger TruncateToAndRound(BigInteger x, int targetNewSizeInBits)
    {
        if (targetNewSizeInBits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetNewSizeInBits), $"Param newSizeInBits({targetNewSizeInBits}) be 0 or greater.");
        }
        int currentSize = (int)BigInteger.Abs(x).GetBitLength();
        BigInteger result = RightShiftWithRound(x, currentSize - targetNewSizeInBits);
        return result;
    }


    //// Converts a double value to a string in base 2 for display.
    //// Example: 123.5 --> "0:10000000101:1110111000000000000000000000000000000000000000000000"
    //// Created by Ryan S. White in 2020
    //// Released under the MIT license (should contain author somewhere)
    //// https://stackoverflow.com/a/68052530/2352507
    //public static string DoubleToBinaryString(double val)
    //{
    //    long v = BitConverter.DoubleToInt64Bits(val);
    //    string binary = Convert.ToString(v, 2);
    //    return binary.PadLeft(64, '0').Insert(12, ":").Insert(1, ":");
    //}

    //// Converts a double value in Int64 format to a string in base 2 for display.
    //// Created by Ryan S. White in 2020
    //// Released under the MIT license (should contain author somewhere)
    //static string DoubleToBinaryString(long doubleInInt64Format)
    //{
    //    string binary = Convert.ToString(doubleInInt64Format, 2);
    //    binary = binary.PadLeft(64, '0').Insert(12, ":").Insert(1, ":");
    //    return binary;
    //}



    ////////////////////////////// Below by Nikolai TheSquid ///////////////////////////////////////////////
    // C# implementation to quickly calculate an Nth root for BigInteger value.
    // MIT License, Copyright(c) 2023 TheSquidCombatant
    // https://github.com/TheSquidCombatant/TheSquid.Numerics.Extensions

    /// <summary>
    /// Nth root for non negative BigInteger values.
    /// </summary>
    /// <param name="source">
    /// Root radicand value.
    /// </param>
    /// <param name="exponent">
    /// Root degree value.
    /// </param>
    /// <param name="isExactResult">
    /// True value for exact result or False value for approximate result.
    /// </param>
    /// <returns>
    /// It returns the exact value, in case of the root is completely extracted, otherwise it returns nearest value from below.
    /// </returns>
    /// <exception cref="ArithmeticException">
    /// The value of the exponent leads to an ambiguous results.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Negative exponent values and negative source values are not supported.
    /// </exception>
    public static BigInteger NthRoot(ref BigInteger source, int exponent, out bool isExactResult)
    {
        // validation of input parameter values
        const string negativeValuesMessage = "Negative exponent values and negative source values are not supported.";
        if ((source < 0) || (exponent < 0)) throw new ArgumentOutOfRangeException(negativeValuesMessage);
        const string ambiguousResultMessage = "The value of the exponent leads to an ambiguous results.";
        if (exponent == 0) throw new ArithmeticException(ambiguousResultMessage);
        // stub for the case of trivial values of the radical expression
        isExactResult = true;
        if ((source == 0) || (source == 1)) return source;
        // calculate the worst-case cost for each root extraction method
        var digitsRootWeight = RootByDigitsWeight(ref source, exponent, out var isDigitsApplicable);
        var newtonRootWeight = RootByNewtonWeight(ref source, exponent, out var isNewtonApplicable);
        var doubleRootWeight = RootByDoubleWeight(ref source, exponent, out var isDoubleApplicable);
        // choose the fastest root extraction method for current parameters
        var min = new[] { digitsRootWeight, newtonRootWeight, doubleRootWeight }.Min();
        // call the fastest root extraction method for current parameters
        if ((min == digitsRootWeight) && isDigitsApplicable) return GetRootByDigits(ref source, exponent, out isExactResult);
        if ((min == newtonRootWeight) && isNewtonApplicable) return GetRootByNewton(ref source, exponent, out isExactResult);
        if ((min == doubleRootWeight) && isDoubleApplicable) return GetRootByDouble(ref source, exponent, out isExactResult);
        // stub if something went wrong when extending the functionality
        const string notSupportedMethodMessage = "Not supported nthroot calculation method.";
        throw new NotSupportedException(notSupportedMethodMessage);
    }


    /// <summary>
    /// Method for calculating Nth roots for large N degrees.
    /// </summary>
    /// <remarks>
    /// Digit-by-digit extraction method.
    /// </remarks>
    private static BigInteger GetRootByDigits(ref BigInteger source, int exponent, out bool isExactResult)
    {
        // calculate how many digits of accuracy are cut off from the radicand value for each digit of root value
        const int floor = 10;
        var digitsShift = BigInteger.Pow(floor, exponent);
        var currentSource = source;
        var intermediateResults = new LinkedList<BigInteger>();
        intermediateResults.AddLast(currentSource);
        // remember the values of the radical expression intermediate in accuracy
        while (currentSource >= digitsShift)
        {
            currentSource /= digitsShift;
            intermediateResults.AddLast(currentSource);
        }
        // initial setting for the digits-by-digits root extraction method
        isExactResult = false;
        var minResult = new BigInteger(1);
        var maxResult = new BigInteger(floor);
        var sourceNode = intermediateResults.Last;
        BigInteger currentResult = 0, currentPower = 0;
        // looking for the root one by one digit starting from the most significant digit
        while (sourceNode != null)
        {
            // initial setting for the current iteration of digits-by-digits extraction
            currentSource = sourceNode.Value;
            isExactResult = false;
            // followed by an optional, but almost zero-cost optimization
            if (sourceNode != intermediateResults.Last)
            {
                // use data from previous iteration
                currentResult *= floor;
                currentPower *= digitsShift;
                // build a tangent (y=k*x+b) to the point of the previous root value 
                var k = exponent * currentPower / currentResult;
                var b = currentPower - k * currentResult;
                var x = (currentSource - b) / k + 1;
                // reduces approximately 20% of iterations
                if (x < maxResult) maxResult = x;
            }
            // initial setting for the binary search method
            currentResult = (minResult + maxResult) / 2;
            BigInteger previousResult = 0;
            // looking for the new last digit of the root using the binary search
            while (previousResult != currentResult)
            {
                currentPower = BigInteger.Pow(currentResult, exponent);
                if (currentPower == currentSource) { isExactResult = true; break; }
                previousResult = currentResult;
                if (currentPower < currentSource) minResult = currentResult; else maxResult = currentResult;
                currentResult = (minResult + maxResult) / 2;
            }
            // shift digits to the left for the next iteration
            minResult = currentResult * floor;
            maxResult = (currentResult + 1) * floor;
            sourceNode = sourceNode.Previous;
        }
        // return accumulated root value
        return currentResult;
    }

    /// <summary>
    /// Method for calculating Nth roots for small N degrees.
    /// </summary>
    /// <remarks>
    /// By Newton simplest extraction method.
    /// </remarks>
    private static BigInteger GetRootByNewton(ref BigInteger source, int exponent, out bool isExactResult)
    {
        // calculate the initial guess (equal or greater) the root value with accuracy up to one digit
        const int floor = 10;
        var quotient = (int)Math.Ceiling(BigInteger.Log(source, floor) / exponent);
        var currentResult = BigInteger.Pow(floor, quotient);
        // initial setting for applying Newton's method
        BigInteger previousResult = 0;
        BigInteger delta = 0;
        // looking for the root by averaging the maximum and minimum values by Newton's method
        while ((previousResult != currentResult) && (delta >= 0))
        {
            var counterweight = BigInteger.Pow(currentResult, (exponent - 1));
            previousResult = currentResult;
            currentResult = (((exponent - 1) * currentResult) + (source / counterweight)) / exponent;
            delta = previousResult - currentResult;
        }
        // on any condition loop end, previousResult contains the desired value
        currentResult = previousResult;
        // check if the last obtained approximation is the exact value of the root
        isExactResult = (BigInteger.Pow(currentResult, exponent) == source);
        // return accumulated root value
        return currentResult;
    }

    /// <summary>
    /// Method for calculating Nth roots for doubling N degrees.
    /// </summary>
    /// <remarks>
    /// Inner well optimized square root extraction method was released by Ryan Scott White.
    /// </remarks>
    private static BigInteger GetRootByDouble(ref BigInteger source, int exponent, out bool isExactResult)
    {
        var basement = source;
        var power = exponent;
        for (; power > 1; power >>= 1) basement = NewtonPlusSqrt(basement);
        var target = BigInteger.Pow(basement, exponent);
        isExactResult = (target == source);
        return basement;
        //// below is an adaptation of Ryan's method for .NET Standard
        //BigInteger NewtonPlusSqrt(BigInteger x)
        //{
        //    // 1.448e17 = ~1<<57
        //    if (x < 144838757784765629)
        //    {
        //        uint vInt = (uint)Math.Sqrt((ulong)x);
        //        // 4.5e15 = ~1<<52
        //        if ((x >= 4503599761588224) && ((ulong)vInt * vInt > (ulong)x)) vInt--;
        //        return vInt;
        //    }
        //    double xAsDub = (double)x;
        //    // 8.5e37 is long.max * long.max
        //    if (xAsDub < 8.5e37)
        //    {
        //        ulong vInt = (ulong)Math.Sqrt(xAsDub);
        //        BigInteger v = (vInt + ((ulong)(x / vInt))) >> 1;
        //        return (v * v <= x) ? v : v - 1;
        //    }
        //    if (xAsDub < 4.3322e127)
        //    {
        //        BigInteger v = (BigInteger)Math.Sqrt(xAsDub);
        //        v = (v + (x / v)) >> 1;
        //        if (xAsDub > 2e63) v = (v + (x / v)) >> 1;
        //        return (v * v <= x) ? v : v - 1;
        //    }
        //    int xLen = (int)BigInteger.Log(BigInteger.Abs(x), 2) + 1;
        //    int wantedPrecision = (xLen + 1) / 2;
        //    int xLenMod = xLen + (xLen & 1) + 1;
        //    // do the first sqrt on hardware
        //    long tempX = (long)(x >> (xLenMod - 63));
        //    double tempSqrt1 = Math.Sqrt(tempX);
        //    ulong valLong = (ulong)BitConverter.DoubleToInt64Bits(tempSqrt1) & 0x1fffffffffffffL;
        //    if (valLong == 0) valLong = 1UL << 53;
        //    // classic Newton iterations
        //    BigInteger val = ((BigInteger)valLong << (53 - 1)) + (x >> xLenMod - (3 * 53)) / valLong;
        //    int size = 106;
        //    for (; size < 256; size <<= 1) val = (val << (size - 1)) + (x >> xLenMod - (3 * size)) / val;
        //    if (xAsDub > 4e254)
        //    {
        //        // 1 << 845
        //        int numOfNewtonSteps = (int)BigInteger.Log((uint)(wantedPrecision / size), 2) + 2;
        //        // apply starting size
        //        int wantedSize = (wantedPrecision >> numOfNewtonSteps) + 2;
        //        int needToShiftBy = size - wantedSize;
        //        val >>= needToShiftBy;
        //        size = wantedSize;
        //        do
        //        {
        //            // Newton plus iterations
        //            int shiftX = xLenMod - (3 * size);
        //            BigInteger valSqrd = (val * val) << (size - 1);
        //            BigInteger valSU = (x >> shiftX) - valSqrd;
        //            val = (val << size) + (valSU / val);
        //            size *= 2;
        //        } while (size < wantedPrecision);
        //    }
        //    // there are a few extra digits here, lets save them
        //    int oversidedBy = size - wantedPrecision;
        //    BigInteger saveDroppedDigitsBI = val & ((BigInteger.One << oversidedBy) - 1);
        //    int downby = (oversidedBy < 64) ? (oversidedBy >> 2) + 1 : (oversidedBy - 32);
        //    ulong saveDroppedDigits = (ulong)(saveDroppedDigitsBI >> downby);
        //    // shrink result to wanted precision
        //    val >>= oversidedBy;
        //    // detect a round-ups
        //    if ((saveDroppedDigits == 0) && (val * val > x)) val--;
        //    return val;
        //}
    }

    /// <summary>
    /// Method for calculating weight of digit-by-digit extraction method.
    /// </summary>
    /// <remarks>
    /// The formula for calculating weight is very approximate and relative, so I will be grateful if someone can clarify.
    /// </remarks>
    private static int RootByDigitsWeight(ref BigInteger source, int exponent, out bool isApplicableMethod)
    {
        const int floor = 10;
        isApplicableMethod = true;
        var quotient = (int)Math.Ceiling(BigInteger.Log(source, floor) / exponent);
        var weight = (int)(0.8 * quotient * (BigInteger.Log(floor, 2) + 1));
        return weight;
    }

    /// <summary>
    /// Method for calculating weight of Newton simplest extraction method.
    /// </summary>
    /// <remarks>
    /// The formula for calculating weight is very approximate and relative, so I will be grateful if someone can clarify.
    /// </remarks>
    private static int RootByNewtonWeight(ref BigInteger source, int exponent, out bool isApplicableMethod)
    {
        const int floor = 10;
        isApplicableMethod = true;
        var quotient = (int)Math.Ceiling(BigInteger.Log(source, floor) / exponent);
        var weight = (int)(Math.Log(BigInteger.Log(BigInteger.Pow(floor, quotient) - BigInteger.Pow(floor, quotient - 1), 2), 2) * exponent / 2 + 3);
        return weight;
    }

    /// <summary>
    /// Method for calculating weight of optimized doubling extraction method.
    /// </summary>
    /// <remarks>
    /// The formula for calculating weight is very approximate and relative, so I will be grateful if someone can clarify.
    /// </remarks>
    private static int RootByDoubleWeight(ref BigInteger source, int exponent, out bool isApplicableMethod)
    {
        const int floor = 10;
        var isPowerOfTwo = (exponent != 0 && ((exponent & (exponent - 1)) == 0));
        isApplicableMethod = false;
        if (!isPowerOfTwo) return int.MaxValue;
        isApplicableMethod = true;
        var quotient = (int)Math.Ceiling(BigInteger.Log(source, floor) / exponent);
        var weight = (int)(0.2 * quotient * (BigInteger.Log(floor, 2) + 1));
        return weight;
    }
    ////////////////// Above by Nikolai TheSquid ///////////////////////////////////////////////
}



