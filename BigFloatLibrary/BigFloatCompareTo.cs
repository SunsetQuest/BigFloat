// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok were used in the development of this library.

using System;
using System.Numerics;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;

public readonly partial struct BigFloat : IComparable, IComparable<BigFloat>, IEquatable<BigFloat>
{
    /// <summary>
    /// Compares two values and returns...
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other (Least significant bits are removed on more accurate number.)
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11==1.1,  1.00==1.0,  1.11!=1.10)
    ///   Returns Positive => this instance is greater than other OR <paramref name="obj"/> is null.
    /// </summary>
    public int CompareTo(object obj) // for IComparable
    {
        return obj switch
        {
            null => 1,   // If other is not a valid object reference, this instance is greater.
            BigFloat => CompareTo((BigFloat)obj),
            BigInteger => CompareTo((BigInteger)obj),
            _ => throw new ArgumentException("Object is not a BigFloat")
        };
    }

    /// <summary>
    /// A more accurate version of CompareTo() however it is not compatible with IEquatable. Compares the two numbers by subtracting them and if they are less then 0|1000 (i.e. Zero) then they are considered equal.
    /// e.g. Using 10|01111111 AND 10|10000000, CompareTo() returns not equal, but CompareInPrecisionBitsTo() returns Equal
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other. (or the difference is less then 0|1000 )
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11 == 1.1,  1.00 == 1.0,  1.11 != 1.10
    ///   Returns Positive => this instance is greater than other
    /// </summary>
    public int CompareInPrecisionBitsTo(BigFloat other)
    {
        if (CheckForQuickCompareWithExponentOrSign(other, out int result)) { return result; }

        // At this point, the exponent is equal or off by one because of a rollover.

        int sizeDiff = other.Scale - Scale;

        BigInteger diff = sizeDiff switch
        {
            //> 0 => -(other.DataBits - (DataBits >> (sizeDiff - expDifference))),    // slightly faster version
            > 0 => RightShiftWithRound(_mantissa, sizeDiff) - other._mantissa, // slightly more precise version
            //< 0 => -((other.DataBits >> (expDifference - sizeDiff)) - DataBits),    // slightly faster version
            < 0 => _mantissa - RightShiftWithRound(other._mantissa, -sizeDiff),// slightly more precise version
            0 => _mantissa - other._mantissa
        };

        // a quick exit
        int bytes = diff.GetByteCount();
        if (bytes != 4)
        {
            return (bytes > 4) ? diff.Sign : 0;
        }

        // Since we are subtracting, we can run into an issue where a 0|100000 should be considered a match.  e.g. 11|000 == 10|100
        diff -= diff.Sign; // decrements towards 0

        // Future: need to benchmark A, B or C
        //int a = RightShiftWithRound(temp, GuardBits).Sign;
        //int b = (BigInteger.Abs(temp) >> (GuardBits - 1)).IsZero ? 0 : temp.Sign;
        int c = ((int)((diff.Sign >= 0) ? diff : -diff).GetBitLength() < GuardBits) ? 0 : diff.Sign;

        return c;
    }

    /// <summary> 
    /// Compares two values, including the out-of-precision guard bits, and returns:
    ///   -1 when this instance is less than <paramref name="other"/>
    ///    0 when this instance is equal to <paramref name="other"/>
    ///   +1 when this instance is greater than <paramref name="other"/>
    /// Since rounding may occur, out-of-precision guard bits that are off by one are considered equal.
    /// Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// CompareTo() is more often used as it is used to compare the in-precision digits.
    /// This Function is faster then the CompareTo() as no rounding takes place.
    /// </summary>
    public int StrictCompareTo(BigFloat other)
    {
        int thisPos = _mantissa.Sign;
        int otherPos = other._mantissa.Sign;

        // Let's first make sure the signs are the same, if not, the positive input is greater.
        if (thisPos != otherPos)
        {
            //  OTHER-> -1  0  1
            //       -1| X -1 -1
            //  THIS: 0| 1  X -1  <-Return
            //        1| 1  1  X
            return thisPos == 0 ? -otherPos : thisPos;
        }

        // At this point the signs are the same. 

        // if both are zero then they are equal
        if (thisPos == 0 /*&& otherPos == 0*/) { return 0; }

        //Note: CompareTo would be the opposite for negative numbers

        // A fast general size check. (aka. Exponent vs Exponent)
        if ((Scale + _size) != (other.Scale + other._size))
        {
            return (Scale + _size).CompareTo(other.Scale + other._size) * thisPos;
        }

        // If we made it here we know that both items have the same exponent 
        if (_size == other._size)
        {
            return _mantissa.CompareTo(other._mantissa);
        }

        if (_size > other._size)
        {
            // We must shrink the larger - in this case THIS
            BigInteger adjustedVal = _mantissa >> (_size - other._size);
            BigInteger diff2 = adjustedVal - other._mantissa;
            return ((diff2 > 1) ? 1 : (diff2 < -1) ? -1 : 0) * thisPos;
        }

        // We must shrink the larger - in this case other
        BigInteger adjustedOther = other._mantissa >> (other._size - _size);
        BigInteger diff = _mantissa - adjustedOther;
        return ((diff > 1) ? 1 : (diff < -1) ? -1 : 0) * thisPos; // DataBits.CompareTo(adjustedOther) * thisPos;
    }

    /// <summary> 
    /// Compares two BigFloat's to make sure they are essentially the same value. Different precisions are allowed.
    /// The lower precision number is up-shifted with zero bits to match the higher precision number.
    /// Returns the following:
    ///   -1 when this instance is less than <paramref name="other"/>
    ///    0 when mantissa and scale are both equal.
    ///   +1 when this instance is greater than <paramref name="other"/>
    /// Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// CompareTo() is more often used as it is used to compare the in-precision digits.
    /// This Function is faster then the CompareTo() as no rounding takes place.
    /// </summary>
    public int FullPrecisionCompareTo(BigFloat other)
    {
        int thisPos = _mantissa.Sign;
        int otherPos = other._mantissa.Sign;

        // Let's first make sure the signs are the same, if not, the positive input is greater.
        if (thisPos != otherPos)
        {
            //  OTHER-> -1  0  1
            //       -1| X -1 -1
            //  THIS: 0| 1  X -1  <-Return
            //        1| 1  1  X
            return thisPos == 0 ? -otherPos : thisPos;
        }

        // At this point the signs are the same. 

        // if both are zero then they are equal
        if (thisPos == 0 /*&& otherPos == 0*/) { return 0; }

        //Note: CompareTo would be the opposite for negative numbers

        // A fast general size check. (aka. Exponent vs Exponent)
        if ((Scale + _size) != (other.Scale + other._size))
        {
            return (Scale + _size).CompareTo(other.Scale + other._size) * thisPos;
        }

        // If we made it here we know that both items have the same exponent 
        if (_size == other._size)
        {
            return _mantissa.CompareTo(other._mantissa);
        }

        if (_size < other._size)
        {
            // We must grow the smaller - in this case THIS
            BigInteger adjustedVal = _mantissa << (other._size - _size);
            return adjustedVal.CompareTo(other._mantissa) * thisPos;
        }

        // We must grow the smaller - in this case OTHER
        BigInteger adjustedOther = other._mantissa << (_size - other._size);
        return _mantissa.CompareTo(adjustedOther) * thisPos;
    }

    /// <summary> 
    /// Returns true if the mantissa and scale are exactly the same.
    /// The lower precision number is up-shifted with zero bits to match the higher precision number.
    /// Returns the following:
    ///   -1 when this instance is less than <paramref name="other"/>
    ///    0 when mantissa and scale are both equal.
    ///   +1 when this instance is greater than <paramref name="other"/>
    /// Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// CompareTo() is more often used as it is used to compare the in-precision digits.
    /// This Function is faster then the CompareTo() as no rounding takes place.
    /// </summary>
    public bool IsExactMatchOf(BigFloat other)
    {
        return other._mantissa == _mantissa && other.Scale == Scale;
    }

    /// <summary>  
    /// Compares two values ignoring the least number of significant bits specified. 
    /// e.g. CompareToIgnoringLeastSigBits(0b1001.1111, 0b1000.111101, 3) => (b1001.11, 0b1001.0)
    /// Valid ranges are from -GuardBits and up.
    ///   Returns -1 when <paramref name="b"/> is less than <paramref name="a"/>
    ///   Returns  0 when <paramref name="b"/> is equal to <paramref name="a"/> when ignoring the least significant bits.
    ///   Returns  1 when <paramref name="b"/> is greater than <paramref name="a"/>
    /// </summary>
    public static int CompareToIgnoringLeastSigBits(BigFloat a, BigFloat b, int leastSignificantBitsToIgnore)
    {
        //if (leastSignificantBitsToIgnore == 0) return a.CompareTo(b);

        // future: if (leastSignificateBitToIgnore == -GuardBits) return CompareToExact(other);

        // Future: need to benchmark, next line is optional, escapes early if size is small
        //if (other._size < leastSignificantBitsToIgnore) return 0;

        leastSignificantBitsToIgnore += GuardBits;

        if (leastSignificantBitsToIgnore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leastSignificantBitsToIgnore),
                $"Param cannot be less then -GuardBits({-GuardBits}).");
        }

        int scaleDiff = b.Scale - a.Scale;

        BigInteger temp = scaleDiff switch
        {
            > 0 => (a._mantissa >> scaleDiff) - b._mantissa,  // 'a' has more accuracy
            < 0 => a._mantissa - (b._mantissa >> -scaleDiff), // 'b' has more accuracy
            _ => a._mantissa - b._mantissa
        };


        // since we are subtracting, we can run into an issue where a 0|100000 should be considered a match.  e.g. 11|000 == 10|100
        temp -= temp.Sign; //decrements towards 0

        // Future: need to benchmark A, B or C

        // Method A
        //if (temp.GetBitLength() < (leastSignificantBitsToIgnore-1))  return 0;
        //return (temp >> (leastSignificantBitsToIgnore - 1)).Sign;

        // Method B
        return (BigInteger.Abs(temp).GetBitLength() > (leastSignificantBitsToIgnore - 1)) ? temp.Sign : 0;

        // Method C
        //if (temp >= 0)
        //    return (temp >> (leastSignificantBitsToIgnore - 1)).Sign;
        //else // is neg
        //    return -((-temp) >> (leastSignificantBitsToIgnore - 1)).Sign;
    }

    /// <summary>
    /// Compares two values and returns...
    ///   Returns -1 when this instance is less than the other
    ///   Returns  0 when this instance is equal to the other 
    ///   Returns  1 when this instance is greater than the other
    /// The guard bits are removed. 
    /// </summary>
    public int CompareTo(BigInteger bigInteger)
    {
        int thisSign = _mantissa.Sign;
        int otherSign = bigInteger.Sign;

        // A fast sign check.
        if (thisSign != otherSign)
        {
            return thisSign == 0 ? -otherSign : thisSign;
        }

        // If both are zero then they are equal.
        if (thisSign == 0) { return 0; }

        // A fast general size check.
        int bigIntegerSizeLessOne = (int)BigInteger.Abs(bigInteger).GetBitLength() - 1;

        if (BinaryExponent != bigIntegerSizeLessOne)
        {
            return BinaryExponent.CompareTo(bigIntegerSizeLessOne) * thisSign;
        }


        // Future: Benchmark A and B
        // Option A:
        // At this point both items have the same exponent and sign. 
        //int bigIntLargerBy = bigIntegerSize - _size;
        //return bigIntLargerBy switch
        //{
        //    0 => DataBits.CompareTo(bigInteger),
        //    < 0 => (DataBits << bigIntegerSize - _size).CompareTo(bigInteger),
        //    > 0 => DataBits.CompareTo(bigInteger << _size - bigIntegerSize)
        //};

        // Option B:
        return RightShiftWithRound(_mantissa, -Scale + GuardBits).CompareTo(bigInteger);
    }
}
