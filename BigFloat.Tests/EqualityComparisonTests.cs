// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;
using Xunit;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for equality and comparison operations
/// </summary>
public class EqualityAndComparisonTests
{
    #region Equals Tests with Primitive Types

    [Theory]
    [InlineData((byte)0, (byte)0, true)]
    [InlineData((byte)1, (byte)1, true)]
    [InlineData((byte)2, (byte)2, true)]
    [InlineData((byte)255, (byte)255, true)]
    [InlineData((byte)254, (byte)254, true)]
    [InlineData((byte)254, (byte)255, false)]
    [InlineData((byte)0, (byte)1, false)]
    public void Equals_Byte_ReturnsExpected(byte bigFloatValue, byte compareValue, bool expected)
    {
        var bigFloat = new BigFloat(bigFloatValue);
        Assert.Equal(expected, bigFloat.Equals(compareValue));
    }

    [Fact]
    public void Equals_Byte_EdgeCases()
    {
        // Test overflow case
        var bigFloat256 = new BigFloat(256);
        Assert.False(bigFloat256.Equals((byte)0));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(-1, -1, true)]
    [InlineData(2, 2, true)]
    [InlineData(-2, -2, true)]
    [InlineData(int.MaxValue, int.MaxValue, true)]
    [InlineData(int.MaxValue - 1, int.MaxValue - 1, true)]
    [InlineData(int.MaxValue - 1, int.MaxValue, false)]
    [InlineData(int.MinValue, int.MinValue, true)]
    [InlineData(int.MinValue + 1, int.MinValue + 1, true)]
    [InlineData(int.MinValue + 1, int.MinValue, false)]
    [InlineData(0, 1, false)]
    [InlineData(-1, 1, false)]
    [InlineData(0, -1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -1, false)]
    public void Equals_Int_ReturnsExpected(int bigFloatValue, int compareValue, bool expected)
    {
        var bigFloat = new BigFloat(bigFloatValue);
        Assert.Equal(expected, bigFloat.Equals(compareValue));
    }

    [Fact]
    public void Equals_Int_EdgeCases()
    {
        // Test overflow case
        var bigFloat = new BigFloat(4294967296);
        Assert.False(bigFloat.Equals(0));
    }

    [Theory]
    [InlineData(0u, 0u, true)]
    [InlineData(1u, 1u, true)]
    [InlineData(2u, 2u, true)]
    [InlineData(uint.MaxValue, uint.MaxValue, true)]
    [InlineData(uint.MaxValue - 1u, uint.MaxValue - 1u, true)]
    [InlineData(uint.MaxValue - 1u, uint.MaxValue, false)]
    [InlineData(0u, 1u, false)]
    public void Equals_UInt_ReturnsExpected(uint bigFloatValue, uint compareValue, bool expected)
    {
        var bigFloat = new BigFloat(bigFloatValue);
        Assert.Equal(expected, bigFloat.Equals(compareValue));
    }

    [Theory]
    [InlineData(0L, 0L, true)]
    [InlineData(1L, 1L, true)]
    [InlineData(-1L, -1L, true)]
    [InlineData(2L, 2L, true)]
    [InlineData(-2L, -2L, true)]
    [InlineData(long.MaxValue, long.MaxValue, true)]
    [InlineData(long.MaxValue - 1L, long.MaxValue - 1L, true)]
    [InlineData(long.MaxValue - 1L, long.MaxValue, false)]
    [InlineData(long.MinValue, long.MinValue, true)]
    [InlineData(long.MinValue + 1L, long.MinValue + 1L, true)]
    [InlineData(long.MinValue + 1L, long.MinValue, false)]
    public void Equals_Long_ReturnsExpected(long bigFloatValue, long compareValue, bool expected)
    {
        var bigFloat = new BigFloat(bigFloatValue);
        Assert.Equal(expected, bigFloat.Equals(compareValue));
    }

    #endregion

    #region CompareTo Tests

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void CompareTo_BasicIntegers(int smaller, int larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.Equal(-1, a.CompareTo(b));
        Assert.Equal(1, b.CompareTo(a));
        Assert.Equal(0, a.CompareTo(a));
        Assert.Equal(0, b.CompareTo(b));
    }

    [Theory]
    [InlineData(-0.0000123, 0.0000123)]
    [InlineData(-0.0000000445, -0.0000000444)]
    [InlineData(0.0000122, 0.0000123)]
    public void CompareTo_SmallFloats(double smaller, double larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.Equal(-1, a.CompareTo(b));
        Assert.Equal(1, b.CompareTo(a));
        Assert.Equal(0, a.CompareTo(a));
        Assert.Equal(0, b.CompareTo(b));
    }

    [Fact]
    public void CompareTo_WithBigInteger()
    {
        for (int i = -5; i < 5; i++)
        {
            var bigFloat = new BigFloat(i);
            var bigInt = new BigInteger(i);
            
            Assert.Equal(0, bigFloat.CompareTo((object)bigInt));
            Assert.Equal(0, bigFloat.CompareTo(bigInt));
        }

        for (int i = -5; i < 5; i++)
        {
            var bigFloat = new BigFloat(i - 1.0);
            var bigInt = new BigInteger(i);
            
            Assert.True(bigFloat.CompareTo((object)bigInt) < 0);
            Assert.True(bigFloat.CompareTo(bigInt) < 0);
        }

        for (long i = long.MinValue >> 1; i < (long.MaxValue >> 2); i += long.MaxValue >> 3)
        {
            var bigFloat = new BigFloat(i);
            var bigInt = new BigInteger(i);
            
            Assert.Equal(0, bigFloat.CompareTo((object)bigInt));
            Assert.Equal(0, bigFloat.CompareTo(bigInt));
            
            var bigFloatLess = new BigFloat(i);
            var bigIntMore = new BigInteger(i + 1);
            Assert.True(bigFloatLess.CompareTo((object)bigIntMore) < 0);
            Assert.True(bigFloatLess.CompareTo(bigIntMore) < 0);
            
            var bigFloatMore = new BigFloat(i);
            var bigIntLess = new BigInteger(i - 1);
            Assert.True(bigFloatMore.CompareTo((object)bigIntLess) > 0);
            Assert.True(bigFloatMore.CompareTo(bigIntLess) > 0);
        }
    }

    #endregion

    #region Comparison Operators Tests

    [Fact]
    public void ComparisonOperators_BasicTests()
    {
        var a = new BigFloat(5);
        var b = new BigFloat(10);
        var c = new BigFloat(5);

        // Less than
        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < c);

        // Less than or equal
        Assert.True(a <= b);
        Assert.False(b <= a);
        Assert.True(a <= c);

        // Greater than
        Assert.False(a > b);
        Assert.True(b > a);
        Assert.False(a > c);

        // Greater than or equal
        Assert.False(a >= b);
        Assert.True(b >= a);
        Assert.True(a >= c);

        // Equality
        Assert.True(a == c);
        Assert.False(a == b);

        // Inequality
        Assert.True(a != b);
        Assert.False(a != c);
    }

    #endregion

    #region ULP Comparison Tests

    [Theory]
    [InlineData(1, 2, 33, false)]    // Different at tolerance 33
    [InlineData(1, 2, 34, true)]     // Equal at tolerance 34
    [InlineData(-1, -2, 33, false)]  // Different at tolerance 33
    [InlineData(-1, -2, 34, true)]   // Equal at tolerance 34
    public void CompareUlp_AdjacentIntegers(int aVal, int bVal, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            if (aVal < bVal)
            {
                Assert.True(a.IsLessThanUlp(b, tolerance));
                Assert.True(b.IsGreaterThanUlp(a, tolerance));
            }
            else
            {
                Assert.True(a.IsGreaterThanUlp(b, tolerance));
                Assert.True(b.IsLessThanUlp(a, tolerance));
            }
        }
    }

    [Fact]
    public void CompareUlp_SmallFloats_SameSize()
    {
        var a = new BigFloat((float)-0.0000123);
        var b = new BigFloat((float)0.0000123);

        Assert.True(a.IsLessThanUlp(b, 3));
        Assert.True(b.IsGreaterThanUlp(a, 3));
    }

    [Theory]
    [InlineData(17, true)]   // Equal at tolerance 17
    [InlineData(16, true)]   // Equal at tolerance 16
    [InlineData(15, false)]  // Different at tolerance 15
    public void CompareUlp_VerySmallFloats(int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat((float)-0.0000000444);
        var b = new BigFloat((float)-0.0000000445);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(b.IsLessThanUlp(a, tolerance));
            Assert.True(a.IsGreaterThanUlp(b, tolerance));
        }
    }

    [Fact]
    public void CompareUlp_DoubleVsFloat_Precision()
    {
        var a = new BigFloat(-0.0000000444);  // double precision
        var b = new BigFloat(-0.0000000445);  // double precision

        Assert.True(a.IsGreaterThanUlp(b, 36));
        Assert.True(b.IsLessThanUlp(a, 36));
        Assert.True(a.EqualsUlp(b, 37));
        Assert.True(b.EqualsUlp(a, 37));
    }

    [Theory]
    [InlineData("0b11", "0b01", 0, 0, false)]  // Different at tolerance 0
    [InlineData("0b11", "0b01", 0, 1, false)]  // Different at tolerance 1
    [InlineData("0b11", "0b01", 0, 3, true)]   // Equal at tolerance 3
    public void CompareUlp_BinaryParsed(string aStr, string bStr, int scale, int tolerance, bool shouldBeEqual)
    {
        BigFloat.TryParseBinary(aStr.Substring(2), out var a);
        BigFloat.TryParseBinary(bStr.Substring(2), out var b);
        a = new BigFloat(a.RawMantissa, scale, true);
        b = new BigFloat(b.RawMantissa, scale, true);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            if (a.RawMantissa > b.RawMantissa)
            {
                Assert.True(a.IsGreaterThanUlp(b, tolerance));
                Assert.True(b.IsLessThanUlp(a, tolerance));
            }
            else
            {
                Assert.True(a.IsLessThanUlp(b, tolerance));
                Assert.True(b.IsGreaterThanUlp(a, tolerance));
            }
        }
    }

    [Theory]
    [InlineData("55555555555555555555552", "55555555555555555555554", 1, false)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 2, false)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 3, true)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 4, true)]
    public void CompareUlp_VeryLargeNumbers(string aStr, string bStr, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aStr);
        var b = new BigFloat(bStr);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Fact]
    public void CompareUlp_DefaultParameters_BackwardsCompatibility()
    {
        var a = new BigFloat(555, 0);
        var b = new BigFloat(554, 0);

        // Test that default parameters work with instance methods
        Assert.True(a.IsGreaterThanUlp(b)); // Uses default ulpTolerance = 0
        Assert.True(b.IsLessThanUlp(a));    // Uses default ulpTolerance = 0
    }

    #endregion

    #region Exact Comparison Tests

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 1f)]
    [InlineData(1f, 2f)]
    public void CompareToExact_SingleIntegers(float smaller, float larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.True(a.IsLessThanUlp(b, 1, true));
    }

    [Theory]
    [InlineData(-0.0000123f, 0.0000123f, true)]
    [InlineData(-0.0000000445f, -0.0000000444f, true)]
    [InlineData(0.0000122f, 0.0000123f, true)]
    [InlineData(-0.0000000444f, -0.0000000445f, false)]
    [InlineData(0.0000123f, 0.0000122f, false)]
    public void CompareToExact_SingleFloats(float aVal, float bVal, bool aLessThanB)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (aLessThanB)
        {
            Assert.True(a.IsLessThanUlp(b, 1, true));
        }
        else
        {
            Assert.True(a.IsGreaterThanUlp(b, 1, true));
        }
    }

    [Fact]
    public void CompareToExact_SingleFloatParse_Precision()
    {
        var a = new BigFloat(float.Parse("0.0000123"));
        var b = new BigFloat(float.Parse("0.0000122"));

        Assert.True(a.IsGreaterThanUlp(b, 1, true));
    }

    [Theory]
    [InlineData(100.000000f, 100.000001f)]  // Beyond single precision
    [InlineData(1.000000001f, 1.000000002f)] // Beyond single precision
    public void CompareToExact_SingleBeyondPrecision(float val1, float val2)
    {
        var a = new BigFloat(val1);
        var b = new BigFloat(val2);

        // Values are identical at single precision limits
        Assert.True(a.EqualsUlp(b, 1, true));
    }

    [Fact]
    public void CompareToExact_SingleDoubleTranslation()
    {
        // These values are first translated from 53 bit doubles, then 24 bit floats
        var a = new BigFloat((float)0.0000123);
        var b = new BigFloat((float)0.00001234);

        Assert.True(a.IsLessThanUlp(b, 1, true));
    }

    #endregion

    #region Equals with Object Tests

    [Fact]
    public void Equals_WithObject()
    {
        Assert.True(new BigFloat(1).Equals((object)BigFloat.One));
        Assert.True(new BigFloat(0).Equals((object)BigFloat.Zero));
        Assert.False(new BigFloat(1).Equals(null));
        Assert.False(new BigFloat(1).Equals((object)1));
    }

    #endregion

    #region NumberOfMatchingLeadingBits Tests

    [Theory]
    [InlineData("10.111", "10.101", 3, 1)]
    [InlineData("1111100", "10000000", 5, -1)]
    [InlineData("10001000", "10000000", 4, 1)]
    [InlineData("10001000", "1000000000", 0, -1)]
    public void NumberOfMatchingLeadingBitsWithRounding_BinaryValues(string aBinary, string bBinary, int expectedResult, int expectedSign)
    {
        BigFloat.TryParseBinary(aBinary, out BigFloat a);
        BigFloat.TryParseBinary(bBinary, out BigFloat b);
        
        int result = BigFloat.NumberOfMatchingLeadingBitsWithRounding(a, b, out int sign);
        
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedSign, sign);
    }

    [Theory]
    [InlineData(-1, 0, 0, -1)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, -1, 0, 1)]
    [InlineData(0, 1, 0, -1)]
    public void NumberOfMatchingLeadingBitsWithRounding_SpecialCases(int aVal, int bVal, int expectedResult, int expectedSign)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);
        
        int result = BigFloat.NumberOfMatchingLeadingBitsWithRounding(a, b, out int sign);
        
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedSign, sign);
    }

    #endregion
}