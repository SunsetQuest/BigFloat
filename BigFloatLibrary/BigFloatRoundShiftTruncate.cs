// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok were used in the development of this library.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BigFloatLibrary;

// see "Rounding-Shifting-Truncate.txt" for additional notes

public readonly partial struct BigFloat
{
    public ulong Lowest64BitsWithGuardBits
    {
        get
        {
            // future: can we use return ulong.CreateTruncating(_mantissa);
            ulong raw = (ulong)(_mantissa & ulong.MaxValue);

            if (_mantissa.Sign < 0)
            {
                raw = ~raw + (ulong)(((_size >> 64) > 0) ? 1 : 0);
            }
            return raw;
        }
    }

    public ulong Lowest64Bits
    {
        get
        {
            // future: can we use return ulong.CreateTruncating(_mantissa);
            // future: we may want to be rounding here instead of "_mantissa >> GuardBits"

            if (_mantissa.Sign >= 0)
            {
                ulong raw = (ulong)((_mantissa >> GuardBits) & ulong.MaxValue);
                return raw;
            }
            else if (_size >= GuardBits)
            {
                return ~(ulong)(((_mantissa - 1) >> GuardBits) & ulong.MaxValue);
                //return (ulong)((BigInteger.Abs(DataBits) >> GuardBits) & ulong.MaxValue); //perf: benchmark

            }
            else
            {
                ulong raw = (ulong)((_mantissa >> GuardBits) & ulong.MaxValue);
                //raw--;
                raw = ~raw;
                return raw;
            }
        }
    }

    /// <summary>
    /// Returns the 64 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 64 bits, then the LSBs are padded with zeros.
    /// </summary>
    public ulong Highest64Bits => (ulong)((BigInteger.IsPositive(_mantissa) ? _mantissa : -_mantissa) >> (_size - 64));

    /// <summary>
    /// Returns the 128 most significant data bits. If the number is negative the sign is ignored. If the size is smaller then 128 bits, then the LSBs are padded with zeros.
    /// </summary>
    public UInt128 Highest128Bits => (UInt128)((BigInteger.IsPositive(_mantissa) ? _mantissa : -_mantissa) >> (_size - 128));


    /// <summary>
    /// Rounds towards positive infinity while preserving the scale (accuracy).
    /// Fractional bits are set to zero but the scale and precision remain unchanged.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat CeilingPreservingAccuracy()
    {
        int bitsToClear = GuardBits - Scale;

        // Fast path: no fractional bits to clear
        if (bitsToClear <= 0) return this;

        // Fast path: entire value is fractional
        if (bitsToClear >= _size)
        {
            // For positive values, ceiling of 0.xxx is 1
            // For negative/zero values, ceiling is 0
            return _mantissa.Sign > 0
                ? new BigFloat(BigInteger.One << (GuardBits - Scale), Scale, 1 + GuardBits - Scale)
                : new BigFloat(BigInteger.Zero, Scale, 0); // Preserve scale for zero
        }

        // Fast path: zero value
        if (_mantissa.IsZero) return this;

        // Negative values: ceiling just clears fractional bits (rounds toward zero)
        if (_mantissa.Sign < 0)
        {
            // For negative numbers, use shift operations to avoid two's complement issues
            BigInteger truncated = -(-_mantissa >> bitsToClear) << bitsToClear;
            return new BigFloat(truncated, Scale, _size);
        }

        // Positive values: need to check if rounding up is required
        if (Scale >= GuardBits)
        {
            return new BigFloat((_mantissa >> bitsToClear) << bitsToClear, Scale, _size);
        }

        // At this point Scale < 0 and positive sign, we need to check sub-GuardBit precision
        int halfGuard = GuardBits / 2;
        BigInteger checkMask = ((BigInteger.One << (halfGuard - Scale)) - 1) << halfGuard;
        bool roundsUp = (_mantissa & checkMask) > 0;

        if (!roundsUp)
        {
            // Just clear the bits
            return new BigFloat((_mantissa >> bitsToClear) << bitsToClear, Scale, _size);
        }

        // Round up: clear bits and add at guard position
        BigInteger clearedBits = (_mantissa >> bitsToClear) << bitsToClear;
        BigInteger result = clearedBits + (BigInteger.One << bitsToClear);

        // Use bit scan to efficiently determine new size
        int newSize = (int)result.GetBitLength();

        return new BigFloat(result, Scale, newSize);
    }

    // Future: The current implementation rounds up only if any bits are set between the binary point(GuardBits - Scale) and halfway through the guard region(GuardBits / 2). Instead, Ceiling should always round up whenever fractional bits exist, regardless of position within the guard region.This ensures consistency with IsInteger(i.e., x.IsInteger implies x == (BigInteger) x or x == (int) x when representable) and avoids cases where x.IsInteger is true but x.Ceiling still increments. The semantics must strictly follow:
    // If x.IsInteger is true, then x.Ceiling == x.
    // If any fractional bits are set and x > 0, then x.Ceiling > x.
    // If x < 0, truncate toward zero (no rounding up)

    /// <summary>
    /// Rounds to the next integer towards positive infinity. 
    /// Removes all fractional bits, sets negative scales to zero, and resizes precision to just the integer part.
    /// Round-up if any bits are set between the point(GuardBits-Scale) and halfway through the GuardBits(GuardBits/2).
    /// </summary>
    public BigFloat Ceiling()
    {
        int bitsToClear = GuardBits - Scale;

        // Fast path: already an integer with no fractional bits (Scale >= 16, 1010|1010101010101010.1010101010101010)
        if (bitsToClear <= (GuardBits / 2)) return this;

        int sign = _mantissa.Sign;

        // Fast path: entire value is fractional
        if (bitsToClear >= _size)
        {
            return sign > 0
                ? new BigFloat(BigInteger.One << GuardBits, 0, 1 + GuardBits)
                : Zero;
        }

        // Fast path: zero
        if (sign == 0) return Zero;

        // Negative values: just truncate (round toward zero)
        if (sign < 0)
        {
            BigInteger truncated = -(-_mantissa >> bitsToClear);
            return new BigFloat(truncated << GuardBits, 0, _size + Scale);
        }

        // At this point Sign is positive AND Scale >= 0 (decimal is at in the in the GuardBits. i.e. 11|1.1000)
        if (Scale < 0)
        {
            // Round-up if any bits are set between the point(GuardBits - Scale) and halfway through the GuardBits(GuardBits / 2).
            bool roundsUp = (_mantissa & (((BigInteger.One << ((GuardBits / 2) - Scale)) - 1) << (GuardBits / 2))) > 0;

            BigInteger intPart = _mantissa >> bitsToClear << GuardBits;

            if (roundsUp)
            {
                intPart += BigInteger.One << GuardBits;
            }

            int newSize = roundsUp ? (int)intPart.GetBitLength() : _size + Scale; //future: optimize using bit scan operations when the rollover is predictable

            return new BigFloat(intPart >> Scale, Scale, newSize - Scale);
        }

        // ---------------  Sign > 0   AND   Scale >= 0  ------------------

        // Mask of all bits that are going to be discarded.
        BigInteger mask = (BigInteger.One << bitsToClear) - 1;
        bool hasFraction = (_mantissa & mask) != 0;        // true ⇢ something to round-up

        // Remove the fractional field.
        BigInteger intPart2 = _mantissa >> bitsToClear;

        // Ceiling: if *any* fraction existed, add 1.
        if (hasFraction) intPart2 += 1;

        // Re-insert the guard word and build the result.
        return new BigFloat(intPart2 << GuardBits, 0,
                            (int)intPart2.GetBitLength() + GuardBits);
    }


    /// <summary>
    /// Rounds to the next integer towards positive infinity. 
    /// Removes all fractional bits, sets negative scales to zero, and resizes precision to just the integer part.
    /// Round-up if any bits are set between the point(GuardBits-Scale) and halfway through the GuardBits(GuardBits/2).
    /// </summary>
    public BigFloat Ceiling0()
    {
        int bitsToClear = GuardBits - Scale;

        // Fast path: already an integer with no fractional bits (Scale >= 16, 1010|1010101010101010.1010101010101010)
        if (bitsToClear <= (GuardBits / 2)) return this;

        int sign = _mantissa.Sign;

        // Fast path: entire value is fractional
        if (bitsToClear >= _size)
        {
            return sign > 0
                ? new BigFloat(BigInteger.One << GuardBits, 0, 1 + GuardBits)
                : Zero;
        }

        // Fast path: zero
        if (sign == 0) return Zero;

        // Negative values: just truncate (round toward zero)
        if (sign < 0)
        {
            BigInteger truncated = -(-_mantissa >> bitsToClear);
            return new BigFloat(truncated << GuardBits, 0, _size + Scale);
        }

        // At this point Sign is positive AND Scale >= 0 (decimal is at in the in the GuardBits. i.e. 11|1.1000)
        if (Scale < 0)
        {
            // Round-up if any bits are set between the point(GuardBits - Scale) and halfway through the GuardBits(GuardBits / 2).
            bool roundsUp = (_mantissa & (((BigInteger.One << ((GuardBits / 2) - Scale)) - 1) << (GuardBits / 2))) > 0;

            BigInteger intPart = _mantissa >> bitsToClear << GuardBits;

            if (roundsUp)
            {
                intPart += BigInteger.One << GuardBits;
            }

            int newSize = roundsUp ? (int)intPart.GetBitLength() : _size + Scale; //future: optimize using bit scan operations when the rollover is predictable

            return new BigFloat(intPart >> Scale, Scale, newSize - Scale);
        }

        // At this point Sign is positive AND Scale >= 0; i.e. 111.10|0001...

        // Extract integer part with one extra bit for rounding decision
        BigInteger shifted = _mantissa >> (bitsToClear - 1);
        bool roundUp = !shifted.IsEven;

        if (roundUp)
        {
            shifted++;
            // Check for power of 2 overflow using single bit test
            if (shifted.IsPowerOfTwo)
            {
                // Size increased by 1
                return new BigFloat(shifted, GuardBits - 1, _size - bitsToClear + 2);
            }
        }

        return new BigFloat(shifted >> 1, GuardBits, _size - bitsToClear);
    }

    /// <summary>
    /// Rounds to the next integer towards negative infinity. Any fractional bits are removed, negative scales are set
    /// to zero, and the precision(size) will be resized to just the integer part.
    /// </summary>
    public BigFloat FloorPreservingAccuracy()
    {
        return -(-this).CeilingPreservingAccuracy();
    }

    /// <summary>
    /// Rounds towards negative infinity (complement of Ceiling).
    /// Removes all fractional bits, sets negative scales to zero, 
    /// and resizes precision to just the integer part.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat Floor()
    {
        // Elegant implementation using ceiling
        return _mantissa.Sign >= 0 ? -(-this).Ceiling() : -(-this).Ceiling();
    }

    /// <summary>
    /// Returns the fractional part of the BigFloat.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigFloat FractionalPart()
    {
        int bitsToClear = GuardBits - Scale;

        if (bitsToClear <= 0) return Zero;
        if (bitsToClear >= _size) return this;

        BigInteger mask = (BigInteger.One << (bitsToClear - 1)) - 1;

        //  fractional part using subtraction to avoid masking issues
        if (_mantissa.Sign >= 0)
        {
            BigInteger frac = _mantissa & mask;
            return new BigFloat(frac, Scale, (int)frac.GetBitLength());
        }
        else
        {
            BigInteger frac = -(-_mantissa & mask);
            return new BigFloat(frac, Scale, (int)(-frac).GetBitLength());
        }
    }

    /// <summary>
    /// Returns an integer with a specific binary accuracy. This is the number of binary digits to the right of the point. This is beyond the GuardBits.
    /// </summary>
    /// <param name="accuracyBits">The accuracy range can be from -GuardBits to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(BigInteger intVal, int accuracyBits)
    {
        int intSize = (int)BigInteger.Abs(intVal).GetBitLength();
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return accuracyBits < -(GuardBits + intSize)
            ? Zero
            : new(intVal << (GuardBits + accuracyBits), -accuracyBits, GuardBits + intSize + accuracyBits);
        // alternative: throw new ArgumentException("The requested precision would not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with a specific binary accuracy. This is the number of binary digits to the right of the point. This is beyond the GuardBits.
    /// </summary>
    /// <param name="accuracyBits">The accuracy range can be from -GuardBits to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(int intVal, int accuracyBits)
    {
        int size = int.Log2(int.Abs(intVal)) + 1 + GuardBits;
        return accuracyBits < -size
            ? Zero
            : new(((BigInteger)intVal) << (GuardBits + accuracyBits), -accuracyBits, size + accuracyBits);
    }

    /// <summary>
    /// Left shift - Increases the size by adding least-significant zero bits. 
    /// i.e. The precision is artificially enhanced. 
    /// </summary>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    public BigFloat LeftShiftMantissa(int bits)
    {
        return CreateFromRawComponents(_mantissa << bits, Scale, _size + bits);
    }

    /// <summary>
    /// Right shift - Decreases the size by removing the least-significant bits. 
    /// i.e. The precision is reduced. 
    /// No rounding is performed and Scale is unchanged. 
    /// </summary>
    /// <param name="bits">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    public BigFloat RightShiftMantissa(int bits)
    {
        return CreateFromRawComponents(_mantissa >> bits, Scale, _size - bits);
    }

}
