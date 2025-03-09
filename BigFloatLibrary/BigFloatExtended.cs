// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT was used in the development of this library.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static BigFloatLibrary.BigIntegerTools;

namespace BigFloatLibrary;


public readonly partial struct BigFloat
{
    /// <summary>
    /// The number of data bits. ExtraHiddenBits are included.  
    /// </summary>
    public readonly int SizeWithHiddenBits => _size;


    /// <summary>
    /// Returns true if the value is beyond exactly zero. A data bits and ExtraHiddenBits are zero.
    /// Example: IsStrictZero is true for "1.3 * (Int)0" and is false for "(1.3 * 2) - 2.6"
    /// </summary>
    public bool IsStrictZero => DataBits.IsZero;


    /// <summary>
    /// Returns the precision of the BigFloat. This is the same as the size of the data bits. The precision can be zero or negative. A negative precision means the number is below the number of bits(HiddenBits) that are deemed precise. 
    /// </summary>
    public int Precision => _size - ExtraHiddenBits;


    /// <summary>
    /// Returns the accuracy of the BigFloat. The accuracy is equivalent to the opposite of the scale. A negative accuracy means the least significant bit is above the one place. A value of zero is equivalent to an integer. A positive value is the number of accurate places(in binary) to the right of the radix point.
    /// </summary>
    public int Accuracy => -Scale;


    /// <summary>
    /// Returns a Zero with a given lower bound of precision. Example: -4 would result of 0.0000(in binary). ExtraHiddenBits will be added.
    /// </summary>
    /// <param name="pointOfLeastPrecision">The precision can be positive or negative.</param>
    public static BigFloat ZeroWithSpecifiedLeastPrecision(int pointOfLeastPrecision)
    {
        return new(BigInteger.Zero, pointOfLeastPrecision, 0);
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(BigInteger intVal, int precisionInBits)
    {
        int intSize = (int)BigInteger.Abs(intVal).GetBitLength();
        // if the precision is shrunk to a size of zero it cannot contain any data bits
        return precisionInBits < -(ExtraHiddenBits + intSize)
            ? ZeroWithNoPrecision
            : new(intVal << (ExtraHiddenBits + precisionInBits), -precisionInBits, ExtraHiddenBits + intSize + precisionInBits);
        // alternative: throw new ArgumentException("The requested precision would not leave any bits.");
    }

    /// <summary>
    /// Returns an integer with additional accuracy. This is beyond the ExtraHiddenBits.
    /// </summary>
    /// <param name="precisionInBits">The precision between (-ExtraHiddenBits - intVal.BitSize) to Int.MaxValue.</param>
    public static BigFloat IntWithAccuracy(int intVal, int precisionInBits)
    {
        int size = int.Log2(int.Abs(intVal)) + 1 + ExtraHiddenBits;
        return precisionInBits < -size
            ? ZeroWithNoPrecision
            : new(((BigInteger)intVal) << (ExtraHiddenBits + precisionInBits), -precisionInBits, size + precisionInBits);
    }

    public static BigFloat NegativeOne => new(BigInteger.MinusOne << ExtraHiddenBits, 0, ExtraHiddenBits + 1);
}