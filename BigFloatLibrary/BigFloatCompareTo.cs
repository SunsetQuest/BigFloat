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
    ///  Compares the in-precision bits between two values. Only the most significant bit in the HiddenBits is considered.
    ///   Returns negative => this instance is less than other
    ///   Returns Zero     => this instance is equal to other (Accuracy of higher number reduced 
    ///     i.e. Sub-Precision bits rounded and removed. 
    ///     e.g. 1.11==1.1,  1.00==1.0,  1.11!=1.10)
    ///   Returns Positive => this instance is greater than other
    /// </summary>
    public int CompareTo(BigFloat other)
    {
        if (CheckForQuickCompareWithExponentOrSign(other, out int result)) { return result; }

        // At this point, the exponent is equal or off by one because of a rollover.

        int sizeDiff = _size - other._size - BinaryExponent + other.BinaryExponent;

        BigInteger diff;
        if (sizeDiff == 0)
        {
            diff = other.DataBits - DataBits;
        }
        else if (sizeDiff > 0)
        {
            diff = other.DataBits - (DataBits >> sizeDiff);
        }
        else
        {
            diff = (other.DataBits << sizeDiff) - DataBits;
        }

        if (diff.Sign >= 0)
        {
            return -(diff >> (ExtraHiddenBits - 1)).Sign;
        }
        else
        {
            return (-diff >> (ExtraHiddenBits - 1)).Sign;
        }
        // Alternative Method - this method rounds off the ExtraHiddenBits and then compares the numbers. 
        // The drawback to this method are...
        //   - the two numbers can be one tick apart in the hidden bits but considered not equal.
        //   - the two numbers can be very near 1 apart but considered not equal..
        // The advantage to this method are...
        //   - we don't get odd results like 2+3=4.  1:1000000 + 10:1000000 = 100:0000000
        //   - may have slightly better performance.
        // BigInteger a = RightShiftWithRound(DataBits, (sizeDiff > 0 ? sizeDiff : 0) + ExtraHiddenBits);
        // BigInteger b = RightShiftWithRound(other.DataBits, (sizeDiff < 0 ? -sizeDiff : 0) + ExtraHiddenBits);
        // return a.CompareTo(b);
    }

    /// <summary>
    /// A more accurate version of CompareTo() however it is not compatible with IEquatable. Compares the two numbers by subtracting them and if they are less then 0:1000 (i.e. Zero) then they are considered equal.
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

        int sizeDiff = _size - other._size - BinaryExponent + other.BinaryExponent;

        BigInteger diff = sizeDiff switch
        {
            //> 0 => -(other.DataBits - (DataBits >> (sizeDiff - expDifference))),    // slightly faster version
            > 0 => RightShiftWithRound(DataBits, sizeDiff) - other.DataBits, // slightly more precise version
            //< 0 => -((other.DataBits >> (expDifference - sizeDiff)) - DataBits),    // slightly faster version
            < 0 => DataBits - RightShiftWithRound(other.DataBits, -sizeDiff),// slightly more precise version
            0 => DataBits - other.DataBits
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
        //int a = RightShiftWithRound(temp, ExtraHiddenBits).Sign;
        //int b = (BigInteger.Abs(temp) >> (ExtraHiddenBits - 1)).IsZero ? 0 : temp.Sign;
        int c = ((int)((diff.Sign >= 0) ? diff : -diff).GetBitLength() < ExtraHiddenBits) ? 0 : diff.Sign;

        return c;
    }

    private bool CheckForQuickCompareWithExponentOrSign(BigFloat other, out int result)
    {
        if (IsOutOfPrecision)
        {
            result = other.IsOutOfPrecision ? 0 : -other.DataBits.Sign;
            return true;
        }

        if (other.IsOutOfPrecision)
        {
            result = IsOutOfPrecision ? 0 : DataBits.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Sign.
        if (DataBits.Sign != other.DataBits.Sign)
        {
            result = DataBits.Sign;
            return true;
        }

        // Lets see if we can escape early by just looking at the Exponent.
        int expDifference = BinaryExponent - other.BinaryExponent;
        if (Math.Abs(expDifference) > 1)
        {
            result = BinaryExponent.CompareTo(other.BinaryExponent) * DataBits.Sign;
            return true;
        }

        // At this point, the sign is the same, and the exp are within 1 bit of each other.

        //There are three special cases when the Exponent is off by just 1 bit:
        // case 1:  The smaller of the two rounds up to match the size of the larger and, therefore, can be equal(11 : 111 == 100 : 000)
        // case 2:  The smaller of the two rounds up, but the larger one also rounds up, so they are again not equal(depends on #1 happening first)
        // case 3:  Both round-up and are, therefore, equal

        //If "this" is larger by one bit AND "this" is not in the format 10000000..., THEN "this" must be larger(or smaller if neg)
        if (expDifference == 1 && !IsOneBitFollowedByZeroBits)
        {
            result = DataBits.Sign;
            return true;
        }

        // If "other" is larger by one bit AND "other" is not in the format 10000000..., THEN "other" must be larger(or smaller if neg)
        if (expDifference == -1 && !other.IsOneBitFollowedByZeroBits)
        {
            result = -Sign;
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary> 
    /// Compares two values(including the hidden precision bits) and returns: 
    ///   Returns -1 when this instance is less than <paramref name="other"/>
    ///   Returns  0 when this instance is equal to <paramref name="other"/>
    ///   Returns +1 when this instance is greater than <paramref name="other"/>
    /// An Equals(Zero) generally should be avoided as missing accuracy in the less accurate number has 0 appended. And these values would need to much match exactly.
    /// This Function is faster then the CompareTo() as no rounding needs to take place.
    /// </summary>
    public int CompareToExact(BigFloat other)
    {
        int thisPos = DataBits.Sign;
        int otherPos = other.DataBits.Sign;

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
            return DataBits.CompareTo(other.DataBits);
        }

        if (_size > other._size)
        {
            // We must grow the smaller - in this case THIS
            BigInteger adjustedVal = DataBits << (other._size - _size);
            return adjustedVal.CompareTo(other.DataBits) * thisPos;
        }

        // at this point _size < other._size - we must grow the smaller - in this case OTHER
        BigInteger adjustedOther = other.DataBits << (_size - other._size);
        return DataBits.CompareTo(adjustedOther) * thisPos;
    }

    /// <summary>  
    /// Compares two values ignoring the least number of significant bits specified. 
    /// e.g. CompareToIgnoringLeastSigBits(0b1001.1111, 0b1000.111101, 3) => (b1001.11, 0b1001.0)
    /// Valid ranges are from -ExtraHiddenBits and up.
    ///   Returns -1 when <paramref name="b"/> is less than <paramref name="a"/>
    ///   Returns  0 when <paramref name="b"/> is equal to <paramref name="a"/> when ignoring the least significant bits.
    ///   Returns  1 when <paramref name="b"/> is greater than <paramref name="a"/>
    /// </summary>
    public static int CompareToIgnoringLeastSigBits(BigFloat a, BigFloat b, int leastSignificantBitsToIgnore)
    {
        //if (leastSignificantBitsToIgnore == 0) return a.CompareTo(b);

        // future: if (leastSignificateBitToIgnore == -ExtraHiddenBits) return CompareToExact(other);

        // Future: need to benchmark, next line is optional, escapes early if size is small
        //if (other._size < leastSignificantBitsToIgnore) return 0;

        leastSignificantBitsToIgnore += ExtraHiddenBits;

        if (leastSignificantBitsToIgnore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leastSignificantBitsToIgnore),
                $"Param cannot be less then -ExtraHiddenBits({-ExtraHiddenBits}).");
        }

        int scaleDiff = b.Scale - a.Scale;

        BigInteger temp = scaleDiff switch
        {
            > 0 => (a.DataBits >> scaleDiff) - b.DataBits,  // 'a' has more accuracy
            < 0 => a.DataBits - (b.DataBits >> -scaleDiff), // 'b' has more accuracy
            _ => a.DataBits - b.DataBits
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
    /// The hidden bits are removed. 
    /// </summary>
    public int CompareTo(BigInteger bigInteger)
    {
        int thisSign = DataBits.Sign;
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
        return RightShiftWithRound(DataBits, -Scale + ExtraHiddenBits).CompareTo(bigInteger);
    }
}
