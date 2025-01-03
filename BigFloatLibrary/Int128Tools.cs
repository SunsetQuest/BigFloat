using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BigFloatLibrary;

public class Int128Tools
{
    /// <summary>
    /// Multiplies two UInt128 values and only returns the high UInt128, discarding the lower 128 bits.
    /// Source: njuffa, 2015, https://stackoverflow.com/a/31662911/23187163
    /// </summary>
    /// <param name="a">The first UInt128 to multiply.</param>
    /// <param name="b">The second UInt128 to multiply.</param>
    public static UInt128 MultHi(UInt128 a, UInt128 b)
    {
        UInt128 a_lo = (UInt64)a;
        UInt128 a_hi = a >> 64;
        UInt128 b_lo = (UInt64)b;
        UInt128 b_hi = b >> 64;

        UInt128 p0 = (a_lo * b_lo) >> 64;
        UInt128 p1 = a_lo * b_hi;
        UInt128 p2 = a_hi * b_lo;

        UInt64 cy = (UInt64)((p0 + (UInt64)p1 + (UInt64)p2) >> 64);

        return (a_hi * b_hi) + (p1 >> 64) + (p2 >> 64) + cy;
    }

    /// <summary>
    /// Multiplies two UInt128 values and only returns the high UInt128, discarding the lower 128 bits. The result can be short by up to 2.
    /// </summary>
    /// <param name="a">The first UInt128 to multiply.</param>
    /// <param name="b">The second UInt128 to multiply.</param>
    public static UInt128 MultHiFast(UInt128 a, UInt128 b)
    {
        UInt128 a_hi = a >> 64;
        UInt128 b_hi = b >> 64;
        return (a_hi * b_hi) + (((UInt64)a * b_hi) >> 64) + ((a_hi * (UInt64)b) >> 64);
    }

    /// <summary>
    /// Multiplies two UInt128 values and only returns the high UInt128 and low UInt128.
    /// Source: njuffa, 2015, https://stackoverflow.com/a/31662911/23187163
    /// </summary>
    /// <param name="a">The first UInt128 to multiply.</param>
    /// <param name="b">The second UInt128 to multiply.</param>
    /// <returns>Returns the result in two UInt128 - high and 128 bits.</returns>
    public static (UInt128 hi, UInt128 lo) Mult(UInt128 a, UInt128 b)
    {
        UInt128 a_lo = (UInt64)a;
        UInt128 a_hi = a >> 64;
        UInt128 b_lo = (UInt64)b;
        UInt128 b_hi = b >> 64;

        UInt128 p0 = a_lo * b_lo;
        UInt128 p1 = a_lo * b_hi;
        UInt128 p2 = a_hi * b_lo;
        UInt128 p3 = a_hi * b_hi;

        UInt64 cy = (UInt64)(((p0 >> 64) + (UInt64)p1 + (UInt64)p2) >> 64);

        UInt128 lo = p0 + (p1 << 64) + (p2 << 64);
        UInt128 hi = p3 + (p1 >> 64) + (p2 >> 64) + cy;
        return (hi, lo);
    }



    /// <summary>
    /// Squares a UInt128 and only returns the high UInt128, discarding the bottom 128 bits.
    /// </summary>
    /// <param name="a">The UInt128 to Square.</param>
    /// <returns>Returns the Square with the bottom 128 bits removed.</returns>
    public static UInt128 SquareHi(UInt128 a)
    {
        UInt128 lo = (ulong)a;
        UInt128 hi = a >> 64;

        UInt128 p = lo * hi;

        ulong cy = (ulong)((((lo * lo) >> 64) + (ulong)p + (ulong)p) >> 64);

        return hi * hi + (2 * (p >> 64)) + cy;
    }


    /// <summary>
    /// Squares a UInt128 and only returns the high UInt128, discarding the bottom 128 bits. The result can be short by up to 2.
    /// </summary>
    /// <param name="a">The UInt128 to Square.</param>
    /// <returns>Returns the Square with the bottom 128 bits removed.</returns>
    public static UInt128 SquareHiFast(UInt128 a)
    {
        UInt128 hi = a >> 64;
        return (hi * hi) + (((ulong)a * hi) >> 63) + (a >> 127); // + (((a >> 124) > 0) ? UInt128.One : 0)
    }

    /// <summary>
    /// Calculates the power of a value. For overflows, the top 128 bits are returned. 
    /// This is a fast approximate function and the lowest order bits may not be correct.
    /// </summary>
    /// <param name="b">The base in UInt128 format.</param>
    /// <param name="exp">The exponent(or power) in Int32 format.</param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static UInt128 PowerFast(UInt128 b, int exp) //partial source: chatgpt 4
    {
        UInt128 result = UInt128.MaxValue;
        while (true)
        {
            // If the exponent is odd, multiply the result by val.
            if ((exp & 1) == 1)
            {
                result = MultHiFast(result, b) + 2;
                if (result >> 127 == 0)
                {
                    result <<= 1;
                }
            }

            exp >>= 1;
            if (exp == 0)
                break;

            b = SquareHiFast(b) + 2;

            if (b >> 127 == 0)
            {
                b <<= 1;
                b--;
            }
        }

        return result;
    }
}
