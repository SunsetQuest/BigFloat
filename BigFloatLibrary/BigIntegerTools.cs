// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace BigFloatLibrary;

public enum BinaryStringFormat
{
    /// <summary> Ordinary magnitude with an optional leading “‑” sign. </summary>
    Standard = 0,

    /// <summary> Write the canonical two’s–complement representation (no sign char, byte‑aligned). </summary>
    TwosComplement = 1 << 0,

    /// <summary> Emit ‘█’ for 1‑bits and ‘·’ for 0‑bits. May be OR‑ed with either format above. </summary>
    Shades = 1 << 1
}

public static class BigIntegerTools
{
    /// <summary>
    /// Converts <paramref name="value"/> to a binary string.
    /// </summary>
    /// <param name="value">The number to encode.</param>
    /// <param name="format">
    /// Use <see cref="BinaryStringFormat.Standard"/> (default) or
    /// <see cref="BinaryStringFormat.TwosComplement"/>; optionally OR with
    /// <see cref="BinaryStringFormat.Shades"/> to substitute █/· for 1/0.
    /// </param>
    /// <param name="minWidth">
    /// Left‑pads with ‘0’ (or ‘·’) so that the digit section is at least this wide.
    /// Ignored when it is ≤ the natural width.
    /// </param>
    public static string ToBinaryString(
        this BigInteger value,
        BinaryStringFormat format = BinaryStringFormat.Standard,
        int minWidth = 0)
    {
        bool twoC = format.HasFlag(BinaryStringFormat.TwosComplement);
        bool shades = format.HasFlag(BinaryStringFormat.Shades);
        bool neg = !twoC && value.Sign < 0;

        if (neg) { value = BigInteger.Abs(value); }    // sign handled separately

        // ==== 1.  Determine buffer sizes ====
        int byteCount = value.GetByteCount(isUnsigned: !twoC);
        if (byteCount == 0) { byteCount = 1; }         // BigInteger 0 → 0 bytes

        int tentativeChars = Math.Max(minWidth, 8 * byteCount) + (neg ? 1 : 0);
        const int STACK_THRESHOLD = 512;

        Span<char> dest = tentativeChars <= STACK_THRESHOLD
            ? stackalloc char[tentativeChars]
            : ArrayPool<char>.Shared.Rent(tentativeChars);

        try
        {
            // ==== 2.  Write the raw bit pattern ====
            int written = twoC
                ? WriteTwosComplement(value, dest)
                : WriteStandard(value, dest, neg);

            // ==== 3.  Enforce minWidth ====
            int signOfs = neg ? 1 : 0;
            int digitSpan = written - signOfs;
            if (digitSpan < minWidth)
            {
                int pad = minWidth - digitSpan;
                // shift right and fill with the right glyph
                dest.Slice(signOfs, digitSpan).CopyTo(dest[(signOfs + pad)..]);
                char padChar = shades ? '·' : ((twoC && value.Sign < 0) ? '1' : '0');
                dest.Slice(signOfs, pad).Fill(padChar);
                written += pad;
            }

            // ==== 4.  Apply shades, if requested ====
            if (shades)
            {
                for (int i = signOfs; i < written; ++i)
                {
                    dest[i] = dest[i] == '1' ? '█' : '·';
                }
            }

            return new string(dest[..written]);
        }
        finally
        {
            if (dest.Length > STACK_THRESHOLD)
            {
                ArrayPool<char>.Shared.Return(dest.ToArray());
            }
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

    // ======== private helpers =========

    private static int WriteStandard(
        BigInteger absValue, Span<char> dest, bool addMinus)
    {
        Span<byte> bytes = stackalloc byte[absValue.GetByteCount(isUnsigned: true)];
        _ = absValue.TryWriteBytes(bytes, out int n, isUnsigned: true, isBigEndian: false);

        int d = 0;
        if (addMinus)
        {
            dest[d++] = '-';
        }

        // top byte: skip its leading zero bits
        byte hi = bytes[n - 1];
        int msb = BitOperations.Log2(hi);
        for (int b = msb; b >= 0; --b)
        {
            dest[d++] = ((hi >> b) & 1) == 1 ? '1' : '0';
        }

        // full bytes
        for (int i = n - 2; i >= 0; --i)
        {
            byte v = bytes[i];
            dest[d++] = ((v >> 7) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 6) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 5) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 4) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 3) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 2) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 1) & 1) == 1 ? '1' : '0';
            dest[d++] = (v & 1) == 1 ? '1' : '0';
        }
        return d;
    }

    private static int WriteTwosComplement(
        BigInteger value, Span<char> dest)
    {
        Span<byte> bytes = stackalloc byte[value.GetByteCount(isUnsigned: false)];
        _ = value.TryWriteBytes(bytes, out int n, isUnsigned: false, isBigEndian: false);

        int d = 0;
        for (int i = n - 1; i >= 0; --i)
        {
            byte v = bytes[i];
            dest[d++] = ((v >> 7) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 6) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 5) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 4) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 3) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 2) & 1) == 1 ? '1' : '0';
            dest[d++] = ((v >> 1) & 1) == 1 ? '1' : '0';
            dest[d++] = (v & 1) == 1 ? '1' : '0';
        }
        return d == 0 ? (dest[0] = '0', 1).Item2 : d;   // special‑case BigInteger.Zero
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
    /// Floor n-th root of a non-negative BigInteger (n ≥ 1).
    /// </summary>
	/// <param name="x">The input value(or radicand) to find the nth root of.</param>
    /// <param name="n">The input nth root(or index) that should be used.</param>
    public static BigInteger NewtonNthRoot(BigInteger x, int n)
    {
        if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "n ≥ 1");
        if (x.IsZero || x.IsOne || n == 1) return x;

        if (x.Sign < 0)
        {
            if ((n & 1) == 0)
                throw new ArgumentException("Even root of a negative number.");
            return -NewtonNthRoot(BigInteger.Negate(x), n);
        }

        // Use double's hardware to get the first approximation
        int bitLen = (int)x.GetBitLength();

        // Handle large inputs by scaling down first
        int shift = 0;
        double xDouble;
        if (bitLen > 1022)
        {
            // Scale down to avoid overflow in double conversion
            //shift = bitLen - (bitLen % n) + 0/*n*/;
            shift = 1 + ((bitLen - 1022) / n);
            xDouble = (double)(x >> ((shift * n) /*- 53*/));
        }
        else
        {
            xDouble = (double)x;
        }

        // Initial approximation using double's hardware
        double initialGuess = Math.Pow(xDouble, 1.0 / n);

        // Convert back to BigInteger, with adjustment for large inputs
        BigInteger r = (BigInteger)initialGuess;
        r += 1;
        r <<= shift;


        /* ---------- 2. integer Newton iterations --------------------------- *
         * rₖ₊₁ = ((n-1)·rₖ + x / rₖⁿ⁻¹) / n                                  *
         * Each step roughly doubles the correct bits.                        */
        int mainLoops = 0;
        while (true)
        {
            BigInteger rToNMinus1 = BigInteger.Pow(r, n - 1);    // fast power below
            BigInteger next = ((n - 1) * (r << 0) + x / (rToNMinus1)) / n;
            if (next == r)
            {
                return r;
            }
            if (BigInteger.Abs(next - r) == 1)
            {
                r = BigInteger.Min(r, next);             // stay ≤ true root
                break;
            }
            r = next;
            mainLoops++;
        }

        /* ---------- 3. final correction by at most one step ---------------- */
        //    Debug.WriteLine($"{0}  r:{ToBinaryString(r, 0)}, n:{n}, xLen:{x.GetBitLength()} mainLoops:{mainLoops}");
        //if (mainLoops>15)
        //{ }
        //while (PowInt(r + 1, n) <= x) ++r;
        //int counter2 = 0;
        //while (BigInteger.Pow(r + 1, n) <= x)
        //{
        //    ++r;
        //    counter2++;
        //}
        //if (counter2 > 0)
        //{
        //    Console.WriteLine($"tooLowwBy{counter2}  r:{ToBinaryString(r, 0)}, n:{n}, xLen:{x.GetBitLength()} mainLoops:{mainLoops}");
        //}

        //int counter = 0;
        while (BigInteger.Pow(r, n) > x)
        {
            --r;
            //counter++;
        }
        //if (counter > 3)
        //{
        //    //Console.WriteLine($"tooHighBy{counter}  r:{ToBinaryString(r, 0)}, n:{n}, xLen:{x.GetBitLength()} mainLoops:{mainLoops}");
        //}

        return r;
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
    public static (BigInteger value, int totalShift) PowMostSignificantBitsApprox(BigInteger val, int exp, int valSize = -1, int wantedBits = 0, bool extraAccurate = false, bool roundDown = false)
    {
        int totalShift = 0;

        if (valSize <= 0)
        {
            if (val.IsZero)
            {
                return (BigInteger.Zero, 0); // technically more of a "NA".
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
                    return ( BigInteger.One << (wantedBits - 1), wantedBits - 1);
                case 1:
                    totalShift = valSize - wantedBits;
                    if (roundDown)
                    {
                        return (val >> totalShift, totalShift);
                    }
                    (BigInteger result, bool carried) = RightShiftWithRoundAndCarry(val, totalShift);
                    if (carried)
                    {
                        totalShift++;
                    }
                    return (result, totalShift);
                case 2:
                    BigInteger sqr = val * val;
                    int sqrSize = (2 * valSize) - ((sqr >> ((2 * valSize) - 1) > 0) ? 0 : 1);
                    totalShift = sqrSize - wantedBits;
                    if (roundDown)
                    {
                        return (sqr >> totalShift, totalShift);
                    }
                    (result, carried) = RightShiftWithRoundAndCarry(sqr, totalShift);
                    if (carried)
                    {
                        totalShift++;
                    }
                    return (result, totalShift);

                default: // negative exp would be less then 1 (unless 1)
                    return (val != 1 ? BigInteger.Zero : val.Sign, 0);
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
                totalShift = ((valSize - 1) * (exp - 1)) + outExp + (valSize - wantedBits)  /*+ (1<<(expSz-2))*/;

                // outMantissa is 53 in size at this point
                // we need to Right Shift With Round but if it rounds up to a larger number (e.g. 1111->10000) then we must increment totalShift.
                bool roundsUp = ((outMantissa >> (bitsToDrop - 1)) & 0x1) > 0;
                if (!roundsUp)
                {
                    return (outMantissa >> bitsToDrop, totalShift);
                }

                long withRoundUp = (outMantissa >> bitsToDrop) + 1;

                // if carried to the 54th place then it rolled over and we must shrink by one.
                if ((withRoundUp >> (53 - bitsToDrop)) > 0)
                {
                    withRoundUp >>= 1;
                    totalShift++;
                }

                return (withRoundUp, totalShift);
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
                product = (product * powerPostShift) >> (shrinkSize + 0);
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

        (BigInteger res, bool carry) = RightShiftWithRoundAndCarry(product, bitsToRemove);
        if (carry)
        {
            totalShift++;
        }
        return (res, totalShift);
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
        const int EXTRA_START = 5; // fails under 5
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
                if ((int)(doubleCurSize * 0.95) > requestedPrecision)
                {
                    Console.WriteLine($"Overshot size:{doubleCurSize} requestedPrecision:{requestedPrecision}");
                }

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigInteger RightShiftWithRound(BigInteger val, in int targetBitsToRemove)
    {
        if (targetBitsToRemove == 0)
        {
            return val; // no change
        }

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
    /// Also see: ReducePrecision, TruncateByAndRound, RightShiftWithRoundWithCarry
    /// </summary>
    /// <param name="result">The result of val being right shifted and rounded. The size will be "size-bitsToRemove".</param>
    /// <param name="value">The source BigInteger we would like right-shift.</param>
    /// <param name="bitsToRemove">The number of bits that will be removed.</param>
    /// <returns>Returns the result and if a carry took place.  e.g. 1111111 << 2 = (10000, true)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (BigInteger result, bool carry) RightShiftWithRoundAndCarry(BigInteger value, int bitsToRemove)
    {
        if (bitsToRemove <= 0) { return (value, false); }

        bool isNegative = value.Sign < 0;
        value = BigInteger.Abs(value);

        BigInteger result = (value >> bitsToRemove) + ((value >> (bitsToRemove - 1)) & 1);

        if (!result.IsPowerOfTwo)
        {
            return (isNegative ? -result : result, false);
        }

        return ((isNegative ? -result : result) >> 1, true);
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


#nullable enable

    public static BigInteger RandomBigInteger(int bitLength, Random? rand = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitLength);

        rand ??= Random.Shared;

        if (bitLength == 0)
        {
            return BigInteger.Zero;
        }

        byte[] bytes = new byte[(bitLength + 7) / 8];
        rand.NextBytes(bytes);
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
    public static BigInteger RandomBigInteger(int minBitLength, int maxBitLength, Random? rand = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minBitLength);

        rand ??= Random.Shared;

        int bits = rand.Next(minBitLength, maxBitLength);
        if (bits == 0)
        {
            return BigInteger.Zero;
        }

        byte[] bytes = new byte[(bits + 7) / 8];
        rand.NextBytes(bytes);
        // For the top byte, place a leading 1-bit then down-shift to achieve desired length.
        bytes[^1] = (byte)((0x80 | bytes[^1]) >> (7 - ((bits - 1) % 8)));
        return new BigInteger(bytes, true);
    }
#nullable disable

    /// <summary>
    /// Generate a random BigInteger in the half-open interval
    /// [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when min ≥ max.
    /// </exception>
    public static BigInteger RandomBigInteger(
    BigInteger minInclusive,
    BigInteger maxExclusive,
    RandomNumberGenerator rand)
    {
        return RandomBigIntegerCore(minInclusive, maxExclusive, buf => rand.GetBytes(buf));
    }

#nullable enable
    /// <summary>
    /// Generate a random BigInteger in the half-open interval
    /// [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when min ≥ max.
    /// </exception>
    public static BigInteger RandomBigInteger(
        BigInteger minInclusive,
        BigInteger maxExclusive,
        Random? rand = null)
    {
        rand ??= new Random();
        return RandomBigIntegerCore(minInclusive, maxExclusive, buf => rand.NextBytes(buf));
    }
#nullable disable

    // source: Source: ChatGPT 3o on 5/21/2025
    private static BigInteger RandomBigIntegerCore(
        BigInteger minInclusive,
        BigInteger maxExclusive,
        Action<Span<byte>> fillBuffer)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentException("minInclusive must be < maxExclusive");

        // Width of the interval we need to sample.
        BigInteger range = maxExclusive - minInclusive;   // strictly positive

        // --- 1.  Determine how many bits/bytes we must draw -----------------
        int bitLen = (int)range.GetBitLength();               // .NET 8 API
        int byteLen = (bitLen + 7) >> 3;                  // ceil(bits/8)

        // Mask out the spare high bits in the top byte to cut rejection rate
        byte msbMask = (byte)((1 << ((bitLen - 1) & 7) + 1) - 1); // 255 iff bitLen mod 8 == 0

        // Use stackalloc up to 512 bytes, else rent.
        Span<byte> buf = byteLen <= 512
            ? stackalloc byte[byteLen]
            : ArrayPool<byte>.Shared.Rent(byteLen);

        try
        {
            BigInteger candidate;
            do
            {
                fillBuffer(buf); // CS1503 error here
                // Trim leading bits we don’t need.
                if (msbMask != 0xFF)
                    buf[0] &= msbMask;

                candidate = new BigInteger(buf, isUnsigned: true, isBigEndian: true);
            }
            while (candidate >= range);   // rejection

            return candidate + minInclusive;
        }
        finally
        {
            if (byteLen > 512)
                ArrayPool<byte>.Shared.Return(buf.ToArray(), clearArray: true);
        }
    }

}
