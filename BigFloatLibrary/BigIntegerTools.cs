// some of the below if from others, some is modified by me, and some is created by me(see notes)

using System;
using System.Numerics;
using System.Text;
using System.Linq;

/////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////           OTHER STUFF                    ///////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////
///
public static class BigIntegerTools
{
    //source: https://stackoverflow.com/a/15447131/2352507  Kevin P. Rice  2013 (modified by Ryan Scott White)
    public static string ToBinaryString(BigInteger bigint, int padZeros = 0)
    {
        byte[] bytes = bigint.ToByteArray();
        int idx = bytes.Length - 1;

        // Create a StringBuilder having appropriate capacity.
        StringBuilder base2 = new StringBuilder(bytes.Length * 8);

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

        return base2.ToString().TrimStart('0').PadLeft(padZeros, '0');
    }
    public static string ToBinaryShades(BigInteger bigint, int padZeros = 0)
    {
        return ToBinaryString(bigint, padZeros).Replace('1', '█').Replace('0', '·'); // · ░
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

            StringBuilder sb = new StringBuilder();

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

            StringBuilder sb = new StringBuilder();

            while (bigInt > 0)
            {
                BigInteger remainder;
                bigInt = BigInteger.DivRem(bigInt, 2, out remainder);
                sb.Insert(0, remainder.ToString());
            }

            if (isNegative)
                sb.Insert(0, "-");

            return sb.ToString();
        }
    }

    //source: chatgpt 10-6-2024 o1
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

    public static BigInteger InverseBigIntegerClassic(BigInteger x, int requestedPrecision)
    {
        //BigInteger res = (BigInteger.One << ((x.GetByteCount() << 3) + precisionBits - 1)) / x;
        //return res >> ((int)BigInteger.Log2(res) - precisionBits + 1);
        //return (BigInteger.One << ((int)x.GetBitLength() + (precisionBits << 1))) / x;
        return (BigInteger.One << ((int)x.GetBitLength() + requestedPrecision - 1)) / x;
    }

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

    public static BigInteger ConcatenateBigIntegers2(BigInteger a, BigInteger b)
    {
        // Assume both BigIntegers are positive (ignore negative numbers)
        // Get the byte counts needed for 'a' and 'b'
        int byteCountA = a.GetByteCount(isUnsigned: true);
        int byteCountB = b.GetByteCount(isUnsigned: true);

        // Allocate a single buffer for the concatenated bytes
        byte[] concatenatedBytes = new byte[byteCountA + byteCountB];
        Span<byte> concatenatedSpan = concatenatedBytes.AsSpan();

        a.TryWriteBytes(concatenatedSpan.Slice(0), out int _, isUnsigned: false, isBigEndian: true);
        b.TryWriteBytes(concatenatedSpan.Slice(byteCountA), out int _, isUnsigned: false, isBigEndian: true);

        // Create a new BigInteger from the concatenated bytes
        BigInteger result = new BigInteger(concatenatedSpan, isUnsigned: true, isBigEndian: true);
        return result;
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
}



