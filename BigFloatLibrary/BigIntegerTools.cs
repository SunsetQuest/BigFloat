﻿// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace BigFloatLibrary;

public static class BigIntegerTools
{
    /// <summary>
    /// A high performance BigInteger to binary string converter that supports 0 and negative numbers.
    /// Negative numbers are returned with a leading '-' sign.
    /// </summary>
    private static int BigIntegerToBinarySpan(BigInteger x, Span<char> dstBytes)
    {
        bool isNegative = x.Sign < 0;
        if (isNegative)
        {
            x = -x;
        }

        // Setup source
        // This produces the minimal bytes for the absolute value in little-endian order.
        ReadOnlySpan<byte> srcBytes = x.ToByteArray(true, false);
        int srcLoc = srcBytes.Length - 1;

        // We'll track how many chars get written
        int dstLoc = 0;

        // If negative, write the sign
        if (isNegative)
        {
            dstBytes[dstLoc++] = '-';
        }

        // The first byte is special so we skip leading zero bits
        byte firstByte = srcBytes[srcLoc--];
        int msb = byte.Log2(firstByte); // highest set bit in the first byte
        for (int j = msb; j >= 0; j--)
        {
            dstBytes[dstLoc++] = (char)('0' + ((firstByte >> j) & 1));
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

        return dstLoc;
    }

    /// <summary>
    /// A high performance BigInteger to binary string converter that supports 0 and negative numbers.
    /// Negative numbers will be returned as two's complement with no sign. 
    /// The output char[] size will be a multiple of 8. 
    /// </summary>
    private static int BigIntegerToBinarySpanTwosComplement(BigInteger x, Span<char> dstBytes)
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

        return dstLoc;
    }

    //todo: test padLeadingZeros
    public static string BigIntegerToBinaryString(BigInteger x, bool twosComplement = false, int padLeadingZeros = 0)
    {
        // We’ll get the bytes first to know how large a span we might need.
        // For Two's complement, we don't pass special parameters to ToByteArray().
        // For standard representation, we pass (isUnsigned=true, isBigEndian=false).
        // (We do it separately inside each helper, but we also do it here for buffer-size planning.)
        ReadOnlySpan<byte> bytes = twosComplement
            ? x.ToByteArray()
            : x.ToByteArray(true, false);

        // Each byte can produce up to 8 bits (chars). 
        // In standard form, we might have a leading '-' sign. 
        // So “maxNeeded” for standard is 8*bytes.Length + 1 (worst-case).
        // For two's complement, we’ll do 8*bytes.Length (all bits).
        bool isNegative = (!twosComplement && x.Sign < 0);
        int maxNeeded = bytes.Length * 8 + (isNegative ? 1 : 0);

        // Use stackalloc for moderate sizes; allocate on the heap if large.
        // (256 is an arbitrary upper bound for stack usage—adjust to taste.)
        Span<char> buffer = maxNeeded <= 256
            ? stackalloc char[maxNeeded]
            : new char[maxNeeded];

        int written;
        if (twosComplement)
        {
            // Write two's complement bits into buffer
            written = BigIntegerToBinarySpanTwosComplement(x, buffer);
        }
        else
        {
            // Write standard binary (possibly with leading '-') into buffer
            written = BigIntegerToBinarySpan(x, buffer);
        }

        // Now we have [0..written) in `buffer`.
        // Next, do left-padding to meet `padLeadingZeros`.
        // 
        // If standard and negative, buffer[0] is '-', and the actual bits start at buffer[1].
        // We'll handle zero-padding only for the “digits” portion in that case.
        int signOffset = (isNegative && !twosComplement) ? 1 : 0;
        int digitCount = written - signOffset; // how many "bits" (non-sign) were written

        // If the user wants more zero padding than we have digits, we shift right and fill.
        if (padLeadingZeros > digitCount)
        {
            int shift = padLeadingZeros - digitCount;
            // Move the existing digits to the right
            Span<char> digitsSpan = buffer.Slice(signOffset, digitCount);
            digitsSpan.CopyTo(buffer.Slice(signOffset + shift));
            // Fill the gap with '0'
            buffer.Slice(signOffset, shift).Fill('0');
            // Update how many digits we now have
            digitCount = padLeadingZeros;
        }

        // Final length = possible sign + the (now possibly padded) digits
        int finalLength = signOffset + digitCount;

        // Create the string from that slice
        return new string(buffer.Slice(0, finalLength));
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
        // todo: does the next line do anything?
        base2.Append(binary);

        // Convert remaining bytes adding leading zeros.
        for (idx--; idx >= 0; idx--)
        {
            // todo: does the next line do anything?
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

    // I think we can delete this
    //source: ChatGPT 01-preview on 10-6-2024
    public static string ToBinaryString2(BigInteger bigInt, bool useTwoComplement = false, int padLeadingZeros = 0)
    {
        if (bigInt.IsZero)
        {
            return "0";
        }

        if (useTwoComplement)
        {
            // Get the two's complement byte array (little-endian order)
            byte[] bytes = bigInt.ToByteArray();

            StringBuilder sb = new();

            // Process bytes from most significant to least significant
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                // Convert each byte to its binary representation, padded to 8 bits
                // todo: does the next line do anything?
                sb.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0'));
            }

            // Remove leading zeros
            string result = sb.ToString().TrimStart('0');

            // todo - do we need the next line
            // Ensure at least one zero is returned for zero values
            result = string.IsNullOrEmpty(result) ? "0" : result;

            return result.ToString().PadLeft(padLeadingZeros, '0');
        }
        else
        {
            bool isNegative = bigInt.Sign < 0;
            if (isNegative)
            {
                bigInt = BigInteger.Abs(bigInt);
            }

            StringBuilder sb = new();

            while (bigInt > 0)
            {
                bigInt = BigInteger.DivRem(bigInt, 2, out BigInteger remainder);
                // todo: does the next line do anything?
                sb.Insert(0, remainder.ToString());
            }

            if (padLeadingZeros > sb.Length)
            {
                int zerosToAdd = padLeadingZeros - sb.Length;
                if (isNegative)
                {
                    sb.Insert(0, new string('0', zerosToAdd));
                }
                else
                {
                    sb.Insert(0, new string('0', zerosToAdd - 1));
                    sb.Insert(0, "-");
                }
            }
            ////todo - do we need the TrimStart('0')?
            //if (isNegative)
            //{
            //    if (sb[0] == 0)
            //        sb[0] = '-';
            //    else
            //        sb.Insert(0, "-");
            //    sb = sb.ToString().TrimStart('0').PadLeft(padLeadingZeros, '0');
            //}

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
            {
                throw new ArgumentException("Negative numbers are not supported.");
            }

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
        BigInteger val = ((BigInteger)valLong << 52) + ((x >> (xLenMod - (3 * 53))) / valLong);
        int size = 106;
        for (; size < 256; size <<= 1)
        {
            val = (val << (size - 1)) + ((x >> (xLenMod - (3 * size))) / val);
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


    /// <summary>
    /// Calculates the nth root of a BigInteger. i.e. x^(1/n)
    /// </summary>
    /// <param name="x">The input value(or radicand) to find the nth root of.</param>
    /// <param name="n">The input nth root(or index) that should be used.</param>
    /// <param name="outputLen">The requested output length. If positive, then this number of bits will be returned. If negative(default), then proper size is returned. If 0, then an output will be returned with the same number of digits as the input. </param>
    /// <param name="xLen">If available, size in bits of input x. If negative, x.GetBitLength() is called to find the value.</param>
    /// <returns>Returns the nth root(or radical) or x^(1/n)</returns>
    public static BigInteger NewtonNthRoot(ref BigInteger x, int n, int outputLen = -1, int xLen = -1)
    {
        if (x == 0) return 0; // The n-th root of 0 is 0.
        if (n == 0) return 1; // The 1st  root of x is x itself.
        if (n == 1) return x; // The 1st  root of x is x itself.
        //if (n == 2) return NewtonPlusSqrt(x); // Use the existing method for square root.

        if (xLen < 0)
        {
            xLen = (int)x.GetBitLength();
        }

        // If requested outputLen is neg then set to proper size, if outputLen==0 then use maintain precision.
        if (outputLen <= 0)
        {
            outputLen = (outputLen == 0) ? xLen : ((int)BigInteger.Log2(x) / n) + 1;
        }

        // If xLen is over 1023 bits, reduce the size of x to fit in a double
        int scaleDownCount = Math.Max(0, ((xLen - 1024) / n) + 0);
        BigInteger scaledX = x >> (n * scaleDownCount);

        ////////// Use double's hardware to get the first 53-bits ////////
        double initialGuess = Math.Pow((double)scaledX, 1.0 / n);
        long bits = BitConverter.DoubleToInt64Bits(initialGuess);
        long mantissa = (bits & 0xfffffffffffffL) | (1L << 52);

        // Return if we have enough bits.
        //if (outputLen < 48) return mantissa >> (53 - outputLen);
        if (outputLen < 48)
        {

            int bitsToRemove = 53 - outputLen;
            long mask = ((long)1 << (bitsToRemove + 1)) - 1;
            long removedBits = (mantissa + 1) & mask;
            if (removedBits == 0)
            {
                mantissa++;
            }

            return mantissa >> (53 - outputLen);
            //(mantissa, 53 - outputLen); 
        }

        //BigInteger val = new BigInteger(initialGuess); Console.WriteLine(val.GetBitLength() + " + " + scaleDownCount + " = " + (val.GetBitLength() + scaleDownCount)); Console.WriteLine($"{BigIntegerToBinaryString(val)}[{val.GetBitLength()}] << {scaleDownCount} val1");

        //////////////////////////////////////////////////////////////
        UInt128 val2 = ((UInt128)mantissa) << (127 - 52);

        UInt128 pow3 = Int128Tools.PowerFast(val2, n - 1);
        UInt128 pow4 = Int128Tools.MultiplyHighApprox(pow3, val2);

        Int128 numerator2 = (Int128)(pow4 >> 5) - (Int128)(x << ((int)UInt128.Log2(pow4) - 4 - xLen)); //todo: should use  "pow4>>127"
        Int128 denominator2 = n * (Int128)(pow3 >> 89);

        BigInteger val = (Int128)(val2 >> 44) - (numerator2 / denominator2);
        //Console.WriteLine((BigIntegerToBinaryString(val2) + " val1")); Console.WriteLine((BigIntegerToBinaryString(pow3) + " powNMinus1")); Console.WriteLine((BigIntegerToBinaryString(numerator2) + " numerator2")); Console.WriteLine((BigIntegerToBinaryString(denominator2)+ " denominator")); Console.WriteLine((BigIntegerToBinaryString(val) + " val2"));
        if (outputLen < 100) // 100?
        {
            return val >> (84 - outputLen);
        }

        int tempShift = outputLen - (int)val.GetBitLength() + 0;  // FIX(for some): CHANGE +0 to +1
        if (UInt128.Log2(pow4) == 126)
        {
            tempShift++;
        }
        //Console.WriteLine(val.GetBitLength()+ " << " + tempShift + " = " + ((int)val.GetBitLength() + tempShift));
        val <<= tempShift;        // should be 241 now

        //////////////////////////////////////////////////////////////
        BigInteger lastVal = 0;
        int loops = 2;
        int ballparkSize = 200;
        while (val != lastVal) // Repeat until convergence
        {
            int reduceBy = Math.Max(0, outputLen - ballparkSize) + 1;
            lastVal = val;
            int valSize = (int)val.GetBitLength();
            BigInteger pow = BigIntegerTools.PowMostSignificantBits(val, n - 1, out int shifted, valSize, valSize - reduceBy);
            BigInteger numerator = ((pow * (val >> reduceBy))) - (x >> (2 * reduceBy - valSize)); // i: -200 j: 0  OR  i: -197 j: 2
            //Console.WriteLine(BigIntegerToBinaryString(((pow * (val >> (reduceBy * 1)))))); Console.WriteLine(BigIntegerToBinaryString((x >> (0 + reduceBy * 1)))); Console.WriteLine(BigIntegerToBinaryString(numerator)); Console.WriteLine(BigIntegerToBinaryString(x >> shifted));
            val = ((val >> (reduceBy + 0)) - (numerator / (n * pow))) << reduceBy; // FIX: CHANGE +0 to +2
            loops++; // Console.WriteLine($"{BigIntegerToBinaryString(val)} loop:{loops}");
            ballparkSize *= 2;
        }
        Console.WriteLine($"======== Loops:{loops} == ballparkSize{ballparkSize}/{val.GetBitLength()} =========");
        Console.WriteLine("Grew by: " + (val.GetBitLength() - xLen));

        return val;
    }


    //future: Create BigFloat version of PowMostSignificantBits()

    /// <summary>
    /// Returns the top n bits for a BigInteger raised to a power. 
    /// If <paramref name="wantedBits"/> is not specified, the output precision will match <paramref name="valSize"/>. 
    /// The number of removed bits are returned in in totalShift. 
    /// The returned result left shifted by <paramref name="totalShift"/>, would return the actual result. 
    /// The result is rounded using the topmost removed bit.  
    /// When the result is rounded in some borderline cases (e.g. 100|011111), the result can occasionally 
    /// round-up. When it rounds-up, it will be in the upward direction only. This is less likely 
    /// if <paramref name="extraAccurate"/> is true. There are no known rounding errors at this time with <paramref name="extraAccurate"/> enabled.
    /// </summary>
    /// <param name="val">The input value.</param>
    /// <param name="valSize">The input values size. This can be left at zero if unknown.</param>
    /// <param name="exp">The exponent to raise the value by.</param>
    /// <param name="totalShift">(out) The number of bits that were removed from the result.</param>
    /// <param name="wantedBits">The number of bits to return. A unspecified value or a value less then 0 will default 
    /// to the inputs size. A value too large will be limited to <paramref name="valSize"/>. </param>
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
                return BigInteger.Zero; // technically more of a "NA".
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
        if (wantedBits <= 0)
        {
            wantedBits = valSize;
        }
        //else if (wantedBits > valSize)
        //{
        //    // Future: we grow by val here however it can be adjusted at the tail. (we might want to grow by a "valSize % exp" only.

        //    // wantedBits
        //    int growBy = wantedBits - valSize;
        //    val <<= growBy;
        //    valSize += growBy;
        //    totalShift = -growBy * exp;
        //}
        else if (wantedBits < valSize - 32)
        {
            // Future: if wantedBits is much smaller, we can shrink val's size to something like 'wantedBits+32'. Then, at the end do rounding, we would need to check for all 1's in the removed bits as it could be off. In that case we will need to fall back and do the whole operation again. Note: we don't need to check for all zeros the result would always be correct or to high.
        }

        if (((long)exp * valSize) >= int.MaxValue)
        {
            throw new OverflowException("Overflow: The output 'totalShift' would be too large to fit in an 'int'. (exp * size > int.maxSize");
        }

        if (exp < 3)
        {
            switch (exp)
            {
                case 0:
                    totalShift = wantedBits - 1;
                    return BigInteger.One << (wantedBits - 1);
                case 1:
                    totalShift = valSize - wantedBits;
                    if (roundDown)
                    {
                        return val >> totalShift;
                    }
                    if (RightShiftWithRoundWithCarryDownsize(out BigInteger result, val, totalShift, valSize))
                    {
                        totalShift++;
                    }
                    return result;
                case 2:
                    BigInteger sqr = val * val;
                    int sqrSize = (2 * valSize) - ((sqr >> ((2 * valSize) - 1) > 0) ? 0 : 1);
                    totalShift = sqrSize - wantedBits;
                    if (roundDown)
                    {
                        return sqr >> totalShift;
                    }
                    if (RightShiftWithRoundWithCarryDownsize(out result, sqr, totalShift, sqrSize))
                    {
                        totalShift++;
                    }
                    return result;

                default: // negative exp would be less then 1 (unless 1)
                    return val != 1 ? BigInteger.Zero : val.Sign;
            }
        }

        // if the input precision is <53 bits AND the output will not overflow THEN we can fit this in a double.
        if ((wantedBits > 2) && (wantedBits < 53) && (valSize * exp) < 3807)
        {
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
            // This version is more accurate but slower. There is just one known incident when it does not round up like it should.
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

            //powerPostShift = RightShiftWithRound(pwrPreShift, shift);  ///better precision by 1.7 bits but 25% slower
            powerPostShift = pwrPreShift >> shift; // 25% faster; 5 times more round errors; always one direction(good thing)
            //powerPostShift = ((pwrPreShift >> (shift-1))+1)>>1; // ????

            shiftSum = (shiftSum * 2) + shift;
            bool bit = ((exp >> i) & 1) == 1;
            if (bit)
            {
                I = H - workingSize;
                int shrinkSize = I - J;
                J = 0;
                product = (product * powerPostShift) >> (shrinkSize+0);
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

        BigInteger res0 = product >> bitsToRemove;

        bool carry = RightShiftWithRoundWithCarryDownsize(out BigInteger res, product, bitsToRemove, productSize);
        if (carry)
        {
            totalShift++;
        }
        return res;
    }


    /// <summary>
    /// Returns the top n bits for a BigInteger raised to a power. 
    /// If <paramref name="wantedBits"/> is not specified, the output precision will match <paramref name="valSize"/>. 
    /// The number of removed bits are returned in in totalShift. 
    /// The returned result left shifted by <paramref name="totalShift"/>, would return the actual result. 
    /// The result is rounded using the topmost removed bit.  
    /// When the result is rounded in some borderline cases (e.g. 100|011111), the result can occasionally 
    /// round-up. When it rounds-up, it will be in the upward direction only. This is less likely 
    /// if <paramref name="extraAccurate"/> is true. There are no known rounding errors at this time with <paramref name="extraAccurate"/> enabled.
    /// </summary>
    /// <param name="val">The input value.</param>
    /// <param name="valSize">The input values size. This can be left at zero if unknown.</param>
    /// <param name="exp">The exponent to raise the value by.</param>
    /// <param name="totalShift">(out) The number of bits that were removed from the result.</param>
    /// <param name="wantedBits">The number of bits to return. A unspecified value or a value less then 0 will default 
    /// to the inputs size. A value too large will be limited to <paramref name="valSize"/>. </param>
    /// <param name="extraAccurate">When false, about 1-in-4096 will round up when it shouldn't. When true, accuracy 
    /// is much better but performance is slower.</param>
    /// <returns>The top bits val raised to the power of exp.</returns>
    public static BigInteger PowMostSignificantBits0(BigInteger val, int exp, out int totalShift, int valSize = -1, int wantedBits = 0, bool extraAccurate = false, bool roundDown = false)
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
        if (wantedBits <= 0)
        {
            wantedBits = valSize;
        }
        //else if (wantedBits > valSize)
        //{
        //    // Future: we grow by val here however it can be adjusted at the tail. (we might want to grow by a "valSize % exp" only.

        //    // wantedBits
        //    int growBy = wantedBits - valSize;
        //    val <<= growBy;
        //    valSize += growBy;
        //    totalShift = -growBy * exp;
        //}
        else if (wantedBits < valSize - 32)
        {
            // Future: if wantedBits is much smaller, we can shrink val's size to something like 'wantedBits+32'. Then, at the end do rounding, we would need to check for all 1's in the removed bits as it could be off. In that case we will need to fall back and do the whole operation again. Note: we don't need to check for all zeros the result would always be correct or to high.
        }

        if (((long)exp * valSize) >= int.MaxValue)
        {
            throw new OverflowException("Overflow: The output 'totalShift' would be too large to fit in an 'int'. (exp * size > int.maxSize");
        }

        if (exp < 3)
        {
            switch (exp)
            {
                case 0:
                    totalShift = wantedBits - 1;
                    return BigInteger.One << (wantedBits - 1);
                case 1:
                    totalShift = valSize - wantedBits;
                    if (roundDown)
                    {
                        return val >> totalShift;
                    }
                    if (RightShiftWithRoundWithCarryDownsize(out BigInteger result, val, totalShift, valSize))
                    {
                        totalShift++;
                    }
                    return result;
                case 2:
                    BigInteger sqr = val * val;
                    int sqrSize = (2 * valSize) - ((sqr >> ((2 * valSize) - 1) > 0) ? 0 : 1);
                    totalShift = sqrSize - wantedBits;
                    if (roundDown)
                    {
                        return sqr >> totalShift;
                    }
                    if (RightShiftWithRoundWithCarryDownsize(out result, sqr, totalShift, sqrSize))
                    {
                        totalShift++;
                    }
                    return result;

                default: // negative exp would be less then 1 (unless 1)
                    return val != 1 ? BigInteger.Zero : val.Sign;
            }
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
            // This version is more accurate but slower. There is just one known incident when it does not round up like it should.
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


    public static BigInteger Inverse(BigInteger x, int requestedPrecision = 0)
    {
        int xLen = (int)x.GetBitLength();
        if (x.IsZero)
        {
            throw new DivideByZeroException("'x' can not be 0.");
        }
        if (requestedPrecision <= 0)
        {
            if (requestedPrecision < 0)
            {
                throw new ArgumentException("'precisionBits' can not be negative.");
            }
            requestedPrecision = xLen;
        }

        // future: can we pre-shrink x to requestedPrecision OR just keep 32 bits past the
        // precision? This can cause an inaccurate result (a round up for results like 122.999999
        // to 123 on the result) however might offer better performance in some cases.

        // Trailing Zeros never matter
        int trailingZeros = (int)BigInteger.TrailingZeroCount(x);
        if (trailingZeros + 1 == xLen)
        {
            return (BigInteger.One * x.Sign) << trailingZeros;
        }
        x >>= trailingZeros;
        xLen -= trailingZeros;

        // Tuning constants     error at:                             
        const int SIMPLE_CUTOFF = 1024; // 1024
        const int EXTRA_START = 4; //    4
        const int START_CUTOFF = 400; //  400
        const int NEWTON_CUTOFF = 800; //  800
        const int EXTRA_TO_REMOVE1 = 2; //    2 - fails under 2
        const int SKIP_LOWEST = 0; //    0
        const int EXTRA_TO_REMOVE2 = 1; //    1 - fails on large numbers
        int BOOST_LARGER_NUMS = requestedPrecision < 3072 ? 2 : 3; //    2

        if ((requestedPrecision + xLen) <= SIMPLE_CUTOFF)
        {
            return (BigInteger.One << (xLen + requestedPrecision - 1)) / x;
        }

        bool isPos = x.Sign >= 0;
        if (!isPos)
        {
            x = -x;
        }

        // The larger the number the more buffer we should start out with. We can then reduce the buffer as we go along.

        ////////  Get Starting Size  ////////
        int desiredStartSize = requestedPrecision + (EXTRA_START * 2);
        while (desiredStartSize > START_CUTOFF)
        {
            desiredStartSize = ((desiredStartSize + 1) >> 1) + BOOST_LARGER_NUMS;
        }
        int curSize = desiredStartSize;

        BigInteger scaledOne2 = BigInteger.One << ((curSize << 1) + (EXTRA_START * 2));
        BigInteger result = scaledOne2 / (x >> (xLen - curSize - 1 - EXTRA_START));
        curSize += EXTRA_START;

        ////////////////////// Classic Newton version  //////////////////////
        while (true)
        {
            int doubleCurSize = curSize << 1;

            BigInteger scalingFactor = BigInteger.One << (doubleCurSize + 1);
            BigInteger xTimesY = ((x >> (xLen - doubleCurSize)) * result) >> (curSize - 1);
            // future: we only need the bottom half of this.
            BigInteger twoMinusXy = scalingFactor - xTimesY;
            result *= twoMinusXy;

            int pendingInaccurateBottomHalfToRemove = curSize + EXTRA_TO_REMOVE1;
            curSize = doubleCurSize - EXTRA_TO_REMOVE1;

            if (curSize > ((requestedPrecision < NEWTON_CUTOFF * 2) ? requestedPrecision : NEWTON_CUTOFF))
            {
                if (curSize > requestedPrecision)
                {
                    BigInteger tempResult2 = (result) >> (curSize - requestedPrecision + pendingInaccurateBottomHalfToRemove);
                    return isPos ? tempResult2 : -tempResult2;
                }
                result = (result) >> (pendingInaccurateBottomHalfToRemove + SKIP_LOWEST);
                break;
            }

            result >>= pendingInaccurateBottomHalfToRemove;
        }

        // future: can we merge the "result >>= SKIP_LOWEST;" into the result shift above?

        // Lets make sure we are 100% accurate at this point - back off until we see both a 0 and 1
        //int reduceBy2 = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1;
        int reduceBy2 = BitOperations.TrailingZeroCount((ulong)(((result & 1UL) == 0 ? result : ~result) & ulong.MaxValue)) + 1;
        if (reduceBy2 < 32) // 32 is flexible
        {
            result >>= reduceBy2;
            curSize -= reduceBy2 + SKIP_LOWEST;
        }
        else
        {
            // if we have something with lots of trailing zeros or ones, lets just use the classic
            // method to ensure correctness.
            BigInteger res = (BigInteger.One << (xLen + ((requestedPrecision == 0) ? xLen : requestedPrecision) - 1)) / x;
            return isPos ? res : -res;
        }

        ////////////////////// SunsetQuest's NewtonPlus Inverse  //////////////////////
        if (curSize > requestedPrecision)
        {
            BigInteger tempResult2 = result >> (curSize - requestedPrecision);
            return isPos ? tempResult2 : (-tempResult2);
        }
        result++;
        while (true)
        {
            int doubleCurSize = curSize << 1;

            // We need insert our "1" in the middle, we do this by incrementing the upper half with a 1
            // we could just do a add a "(1 << doublecurSize)"
            BigInteger mask = (BigInteger.One << (curSize + 1)) - 1;
            BigInteger xTimesY = ((x >> (xLen - doubleCurSize)) * result) >> (curSize - 1); // future: we only need the bottom half of this.

            //// back off until we see both a zero and one
            if (doubleCurSize - EXTRA_TO_REMOVE2 > requestedPrecision)
            {
                if ((int)(doubleCurSize * 0.95) > requestedPrecision) Console.WriteLine($"Overshot size:{doubleCurSize} requestedPrecision:{requestedPrecision}");

                result = ((result << doubleCurSize) - (result * (xTimesY & mask))) >> (doubleCurSize + curSize - requestedPrecision);
                //result = ((result << curSize) - ((result * (xTimesY & mask)) >> curSize)) >> (doubleCurSize - requestedPrecision - 1);
                return isPos ? result : -result;
            }
            result = ((result << doubleCurSize) - (result * (xTimesY & mask))) >> (curSize + EXTRA_TO_REMOVE2);

            curSize = doubleCurSize - EXTRA_TO_REMOVE2;

            int reduceBy = BitOperations.TrailingZeroCount((ulong)(((result & 1UL) == 0 ? result : ~result) & ulong.MaxValue)) + 1;
            //int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : ~result) + 1;
            //if (reduceBy < 100)
            result >>= reduceBy;
            curSize -= reduceBy;
            result++;
        }
    }


    /// <summary>
    /// This number will take a BigInteger and return the bits to the right side of the decimal point. 
    /// Example: Inverse of 0b11011(i.e. 18) would be 0.037037 and returned as 0b00010010(i.e. 18).
    /// Note: Initial version of this was started with ChatGPT. SunsetQuest's Newton Plus algorithm was then added. 
    /// (see inverseNotes.txt)
    /// </summary>
    /// <param name="x">The value you want to find the inverse (1/x) of x. Negative values are allowed.</param>
    /// <param name="requestedPrecision">The number of bits in the output precision. If 0, then the output will match the input's length.</param>
    /// <returns>Returns the inverse bits as they would appear after the radix point.</returns>
    /// <exception cref="DivideByZeroException">if x is zero, then the inverse is undefined or infinity.</exception>
    /// <exception cref="ArgumentException">A negative requested Precision is not allowed.</exception>
    public static BigInteger Inverse0(BigInteger x, int requestedPrecision = 0)
    {
        int xLen = (int)x.GetBitLength();
        if (x.IsZero)
        {
            throw new DivideByZeroException("'x' can not be 0.");
        }
        if (requestedPrecision <= 0)
        {
            if (requestedPrecision < 0)
            {
                throw new ArgumentException("'precisionBits' can not be negative.");
            }
            requestedPrecision = xLen;
        }

        // future: can we pre-shrink x to requestedPrecision OR just keep 32 bits past the
        // precision? This can cause an inaccurate result (a round up for results like 122.999999
        // to 123 on the result) however might offer better performance in some cases.

        // Trailing Zeros never matter
        int trailingZeros = (int)BigInteger.TrailingZeroCount(x);
        if (trailingZeros + 1 == xLen)
        {
            return (BigInteger.One * x.Sign) << trailingZeros;
        }
        x >>= trailingZeros;
        xLen -= trailingZeros;

        // Tuning constants     error at:                             
        const int SIMPLE_CUTOFF =   1024; // 1024
        const int EXTRA_START =        4; //    4
        const int START_CUTOFF =     400; //  400
        const int NEWTON_CUTOFF =    800; //  800
        const int EXTRA_TO_REMOVE1 =   2; //    2 - fails under 2
        const int SKIP_LOWEST =        0; //    0
        const int EXTRA_TO_REMOVE2 =   1; //    1 - fails on large numbers
        int BOOST_LARGER_NUMS= requestedPrecision < 3072 ? 2 : 3; //    2

        if ((requestedPrecision + xLen) <= SIMPLE_CUTOFF)
        {
            return (BigInteger.One << (xLen + requestedPrecision - 1)) / x;
        }

        bool isPos = x.Sign >= 0;
        if (!isPos)
        {
            x = -x;
        }

        // The larger the number the more buffer we should start out with. We can then reduce the buffer as we go along.

        ////////  Get Starting Size  ////////
        int desiredStartSize = requestedPrecision + (EXTRA_START * 2);
        while (desiredStartSize > START_CUTOFF)
        {
            desiredStartSize = ((desiredStartSize + 1) >> 1) + BOOST_LARGER_NUMS;
        }
        int curSize = desiredStartSize;

        BigInteger scaledOne2 = BigInteger.One << ((curSize << 1) + (EXTRA_START * 2));
        BigInteger result = scaledOne2 / (x >> (xLen - curSize - 1 - EXTRA_START));
        curSize += EXTRA_START;

        ////////////////////// Classic Newton version  //////////////////////
        while (true)
        {
            int doubleCurSize = curSize << 1;

            BigInteger scalingFactor = BigInteger.One << (doubleCurSize + 1);
            BigInteger xTimesY = ((x >> (xLen - doubleCurSize)) * result) >> (curSize - 1);
            // future: we only need the bottom half of this.
            BigInteger twoMinusXy = scalingFactor - xTimesY;
            result *= twoMinusXy;

            int pendingInaccurateBottomHalfToRemove = curSize + EXTRA_TO_REMOVE1;
            curSize = doubleCurSize - EXTRA_TO_REMOVE1;

            if (curSize > ((requestedPrecision < NEWTON_CUTOFF * 2) ? requestedPrecision : NEWTON_CUTOFF))
            {
                if (curSize > requestedPrecision)
                {
                    BigInteger tempResult2 = (result) >> (curSize - requestedPrecision + pendingInaccurateBottomHalfToRemove);
                    return isPos ? tempResult2 : -tempResult2;
                }
                result = (result) >> (pendingInaccurateBottomHalfToRemove + SKIP_LOWEST);
                break;
            }

            result >>= pendingInaccurateBottomHalfToRemove;
        }

        // future: can we merge the "result >>= SKIP_LOWEST;" into the result shift above?

        // Lets make sure we are 100% accurate at this point - back off until we see both a 0 and 1
        //int reduceBy2 = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : (~result)) + 1;
        int reduceBy2 = BitOperations.TrailingZeroCount((ulong)(((result & 1UL) == 0 ? result : ~result) & ulong.MaxValue)) + 1;
        if (reduceBy2 < 32) // 32 is flexible
        {
            result >>= reduceBy2;
            curSize -= reduceBy2 + SKIP_LOWEST;
        }
        else
        {
            // if we have something with lots of trailing zeros or ones, lets just use the classic
            // method to ensure correctness.
            BigInteger res = (BigInteger.One << (xLen + ((requestedPrecision == 0) ? xLen : requestedPrecision) - 1)) / x;
            return isPos ? res : -res;
        }

        ////////////////////// SunsetQuest's NewtonPlus Inverse  //////////////////////
        if (curSize > requestedPrecision)
        {
            BigInteger tempResult2 = result >> (curSize - requestedPrecision);
            return isPos ? tempResult2 : (-tempResult2);
        }
        result++;
        while (true)
        {
            int doubleCurSize = curSize << 1;

            // We need insert our "1" in the middle, we do this by incrementing the upper half with a 1
             // we could just do a add a "(1 << doublecurSize)"
            BigInteger mask = (BigInteger.One << (curSize + 1)) - 1;
            BigInteger xTimesY = ((x >> (xLen - doubleCurSize)) * result) >> (curSize - 1); // future: we only need the bottom half of this.

            //// back off until we see both a zero and one
            if (doubleCurSize - EXTRA_TO_REMOVE2 > requestedPrecision)
            {
                if ((int)(doubleCurSize*0.95) > requestedPrecision) Console.WriteLine($"Overshot size:{doubleCurSize} requestedPrecision:{requestedPrecision}");

                result =   ((result << doubleCurSize) - (result * (xTimesY & mask))) >> (doubleCurSize + curSize - requestedPrecision);
                //result = ((result << curSize) - ((result * (xTimesY & mask)) >> curSize)) >> (doubleCurSize - requestedPrecision - 1);
                return isPos ? result : -result;
            }
            result = ((result << doubleCurSize) - (result * (xTimesY & mask))) >> (curSize + EXTRA_TO_REMOVE2);

            curSize = doubleCurSize - EXTRA_TO_REMOVE2;

            int reduceBy = BitOperations.TrailingZeroCount((ulong)(((result & 1UL) == 0 ? result : ~result) & ulong.MaxValue)) + 1;
            //int reduceBy = (int)BigInteger.TrailingZeroCount(result.IsEven ? result : ~result) + 1;
            //if (reduceBy < 100)
            result >>= reduceBy;
            curSize -= reduceBy;
            result++;
        }
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
        if (val.Sign < 0)
        {
            val--;
        }

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

        // val is Neg
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


    /// <summary>
    /// Returns a random BigInteger of a specific bit length.
    /// </summary>
    /// <param name="bitLength">The bit length the BigInteger should be.</param>
    public static BigInteger CreateRandomBigInteger(this Random random, int bitLength)
    {
        if (bitLength < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (bitLength == 0)
        {
            return BigInteger.Zero;
        }

        byte[] bytes = new byte[(bitLength + 7) / 8];
        random.NextBytes(bytes);
        // For the top byte, place a leading 1-bit then down-shift to achieve desired length.
        bytes[^1] = (byte)((0x80 | bytes[^1]) >> (7 - ((bitLength - 1) % 8)));
        return new BigInteger(bytes, true);
    }


    /// <summary>
    /// Returns a random BigInteger with a bit length between <paramref name="minBitLength"/>(inclusive) and <paramref name="maxBitLength"/>(exclusive).
    /// https://stackoverflow.com/a/72107573/2352507 Ryan S. White in 5/2022
    /// </summary>
    /// <param name="minBitLength">The inclusive lower bit length of the random BigInteger returned.</param>
    /// <param name="maxBitLength">The exclusive upper bit length of the random BigInteger returned. 
    /// <paramref name="maxBitLength"/> must be greater than or equal to minValue.</param>
    public static BigInteger CreateRandomBigInteger(this Random random, int minBitLength, int maxBitLength)
    {
        if (minBitLength < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        int bits = random.Next(minBitLength, maxBitLength);
        if (bits == 0)
        {
            return BigInteger.Zero;
        }

        byte[] bytes = new byte[(bits + 7) / 8];
        random.NextBytes(bytes);
        // For the top byte, place a leading 1-bit then down-shift to achieve desired length.
        bytes[^1] = (byte)((0x80 | bytes[^1]) >> (7 - ((bits - 1) % 8)));
        return new BigInteger(bytes, true);
    }


    //// Converts a double value to a string in base 2 for display.
    //// Example: 123.5 --> "0:10000000101:1110111000000000000000000000000000000000000000000000"
    //// https://stackoverflow.com/a/68052530/2352507  Ryan S. White in 2020
    //public static string DoubleToBinaryString(double val)
    //{
    //    long v = BitConverter.DoubleToInt64Bits(val);
    //    string binary = Convert.ToString(v, 2);
    //    return binary.PadLeft(64, '0').Insert(12, ":").Insert(1, ":");
    //}

    //// Converts a double value in Int64 format to a string in base 2 for display.
    //// https://stackoverflow.com/a/68052530/2352507  Ryan S. White in 2020
    //static string DoubleToBinaryString(long doubleInInt64Format)
    //{
    //    string binary = Convert.ToString(doubleInInt64Format, 2);
    //    binary = binary.PadLeft(64, '0').Insert(12, ":").Insert(1, ":");
    //    return binary;
    //}

}
