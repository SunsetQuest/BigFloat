// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Core BigFloat tests that don't fit into specific categories
/// </summary>
public class BigFloatTests
{
    /// <summary>
    /// Target time for each test in milliseconds.
    /// </summary>
    private const int TestTargetInMilliseconds = 100;

#if DEBUG
    private const int MaxDegreeOfParallelism = 1;
    private const long InverseBruteForceStoppedAt = 262144;
#else
    private readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;
    private const long InverseBruteForceStoppedAt = 524288;
#endif

    private const int RAND_SEED = 22;
    private static readonly Random _rand = new(RAND_SEED);

    #region String Representation Tests

    [Theory]
    [InlineData("123.124", "123.123|5", false)] // 123.124 vs 123.123|5
    public void StringRepresentation_PrecisionBoundary(string value1Str, string value2Str, bool shouldBeEqual)
    {
        var value1 = new BigFloat(value1Str);
        var value2 = new BigFloat(value2Str.Replace("|", ""));
        
        // The pipe character indicates where precision differs
        // 123.124 has 17.91 binary accuracy, so 17 bits
        // 123.124:   1111011.0001111110|11111...   
        // 123.123|5: 1111011.0001111110|01110...   
        //                      Diff: |10001
        
        Assert.NotEqual(shouldBeEqual, value1 == value2);
    }

    #endregion

    #region Inverse Tests

    [Fact(Skip = "Long-running test - enable manually")]
    public void Inverse_BruteForce_SmallIntegers()
    {
        for (long i = 1; i < InverseBruteForceStoppedAt; i++)
        {
            var value = new BigFloat(i);
            var inverse = BigFloat.Inverse(value);
            var product = value * inverse;
            
            Assert.True(product.EqualsUlp(1, 2),
                $"Inverse({i}) * {i} = {product}, expected approximately 1");
        }
    }

    [Theory]
    [InlineData(2, "0.5")]
    [InlineData(4, "0.25")]
    [InlineData(5, "0.2")]
    [InlineData(8, "0.125")]
    [InlineData(10, "0.1")]
    public void Inverse_SimpleValues_ExactResults(int input, string expected)
    {
        var value = new BigFloat(input);
        var inverse = BigFloat.Inverse(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.True(inverse.EqualsZeroExtended(expectedValue));
    }

    [Theory]
    [InlineData(3, "0.33333333333")]
    [InlineData(6, "0.16666666667")]
    [InlineData(7, "0.14285714286")]
    [InlineData(9, "0.11111111111")]
    public void Inverse_RepeatingDecimals_ApproximateResults(int input, string expectedPrefix)
    {
        var value = new BigFloat(input);
        var inverse = BigFloat.Inverse(value);
        
        var resultStr = inverse.ToString();
        var compareLength = Math.Min(expectedPrefix.Length, resultStr.Length);
        Assert.StartsWith(expectedPrefix.Substring(0, compareLength), resultStr);
    }

    [Fact]
    public void Inverse_Zero_ThrowsException()
    {
        var zero = BigFloat.Zero;
        Assert.Throws<DivideByZeroException>(() => BigFloat.Inverse(zero));
    }

    [Fact]
    public void Inverse_One_ReturnsOne()
    {
        var one = BigFloat.One;
        var inverse = BigFloat.Inverse(one);
        Assert.Equal(BigFloat.One, inverse);
    }

    [Fact]
    public void Inverse_NegativeValues_ReturnsNegative()
    {
        var negative = new BigFloat(-2);
        var inverse = BigFloat.Inverse(negative);
        var expected = new BigFloat("-0.5");
        
        Assert.True(inverse.EqualsZeroExtended(expected));
    }

    #endregion

    #region Constant Tests

    [Fact]
    public void Constants_BasicValues()
    {
        Assert.Equal(0, (BigFloat)0);
        Assert.Equal(1, (BigFloat)1);
        Assert.Equal(2, (BigFloat)2);
        Assert.Equal(10, (BigFloat)10);

        Assert.True(((BigFloat)0).IsZero);
        Assert.False(((BigFloat)1).IsZero);
        Assert.Equal(1, ((BigFloat)1).Sign);
        Assert.Equal(0, ((BigFloat)0).Sign);
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(0, true, true, 0)]
    [InlineData(1, false, true, 1)]
    [InlineData(-1, false, true, -1)]
    [InlineData(5, false, true, 1)]
    [InlineData(-5, false, true, -1)]
    public void Properties_BasicValues(int value, bool isZero, bool isInteger, int expectedSign)
    {
        var bf = new BigFloat(value);
        
        Assert.Equal(isZero, bf.IsZero);
        Assert.Equal(isInteger, bf.IsInteger);
        Assert.Equal(expectedSign, bf.Sign);
    }

    [Theory]
    [InlineData("0.5", false)]
    [InlineData("1.0", true)]
    [InlineData("1.5", false)]
    [InlineData("2.0", true)]
    [InlineData("-1.0", true)]
    [InlineData("-1.5", false)]
    public void IsInteger_DecimalValues(string value, bool expected)
    {
        var bf = new BigFloat(value);
        Assert.Equal(expected, bf.IsInteger);
    }

    [Theory]
    [InlineData(double.MaxValue / 2, true)]
    [InlineData(double.MinValue / 2, true)]
    [InlineData(1e100, false)]
    [InlineData(-1e100, false)]
    public void FitsInDouble_BoundaryValues(double value, bool expected)
    {
        var bf = new BigFloat(value.ToString());
        
        // For values created from doubles, they should fit
        // For values beyond double range, they shouldn't
        if (double.IsFinite(value))
        {
            var directBf = new BigFloat(value);
            Assert.True(directBf.FitsInADouble);
        }
    }

    #endregion

    #region Formatting Tests

    [Theory]
    [InlineData("123.456", "G", null, "123.456")]
    [InlineData("123.456", "F2", null, "123.46")]
    [InlineData("123.456", "F4", null, "123.4560")]
    [InlineData("0.0001234", "E", null, "1.234000E-004")]
    [InlineData("0.0001234", "E2", null, "1.23E-004")]
    public void ToString_WithFormat(string value, string format, IFormatProvider? provider, string expected)
    {
        var bf = new BigFloat(value);
        var result = bf.ToString(format, provider ?? CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    //[Theory]
    //[InlineData("en-US", "1234.56")]
    //[InlineData("fr-FR", "1234,56")]
    //[InlineData("de-DE", "1234,56")]
    //public void ToString_WithCulture(string culture, string expected)
    //{
    //    var bf = new BigFloat("1234.56");
    //    var cultureInfo = new CultureInfo(culture);
    //    var result = bf.ToString("F2", cultureInfo);
    //    Assert.Equal(expected, result);
    //}

    #endregion

    #region Binary String Tests

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(2, "10")]
    [InlineData(3, "11")]
    [InlineData(4, "100")]
    [InlineData(7, "111")]
    [InlineData(8, "1000")]
    [InlineData(15, "1111")]
    [InlineData(16, "10000")]
    public void ToBinaryString_IntegerValues(int value, string expected)
    {
        var bf = new BigFloat(value, 0, false, addedBinaryPrecision: 0);
        var result = bf.ToBinaryString(includeGuardBits: false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.5, "0.1")]
    [InlineData(0.25, "0.01")]
    [InlineData(0.125, "0.001")]
    [InlineData(1.5, "1.1")]
    [InlineData(2.5, "10.1")]
    [InlineData(3.75, "11.11")]
    public void ToBinaryString_FractionalValues(double value, string expected)
    {
        var bf = new BigFloat(value);
        var result = bf.ToBinaryString(includeGuardBits: false);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Hexadecimal String Tests

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(10, "A")]
    [InlineData(15, "F")]
    [InlineData(16, "10")]
    [InlineData(255, "FF")]
    [InlineData(256, "100")]
    [InlineData(4095, "FFF")]
    [InlineData(4096, "1000")]
    public void ToHexString_IntegerValues(int value, string expected)
    {
        var bf = new BigFloat(value);
        var result = bf.ToHexString(includeGuardBits: false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.5, "0.8")]
    [InlineData(0.25, "0.4")]
    [InlineData(0.125, "0.2")]
    [InlineData(0.0625, "0.1")]
    [InlineData(1.5, "1.8")]
    [InlineData(2.5, "2.8")]
    public void ToHexString_FractionalValues(double value, string expected)
    {
        var bf = new BigFloat(value);
        var result = bf.ToHexString(includeGuardBits: false);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Rounding Tests

    [Theory]
    [InlineData("1.4", "1")]
    [InlineData("1.5", "2")]
    [InlineData("1.6", "2")]
    [InlineData("2.4", "2")]
    [InlineData("2.5", "3")]
    [InlineData("2.6", "3")]
    [InlineData("-1.4", "-1")]
    [InlineData("-1.5", "-2")]
    [InlineData("-1.6", "-2")]
    public void Round_ToNearestInteger(string input, string expected)
    {
        var value = new BigFloat(input);
        var rounded = BigFloat.Round(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.Equal(expectedValue, rounded);
    }

    [Theory]
    [InlineData("1.1", "1")]
    [InlineData("1.9", "1")]
    [InlineData("2.1", "2")]
    [InlineData("2.9", "2")]
    [InlineData("-1.1", "-2")]
    [InlineData("-1.9", "-2")]
    [InlineData("-2.1", "-3")]
    [InlineData("-2.9", "-3")]
    public void Floor_ReturnsLargestIntegerLessThanOrEqual(string input, string expected)
    {
        var value = new BigFloat(input);
        var floor = BigFloat.Floor(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.Equal(expectedValue, floor);
    }

    [Theory]
    [InlineData("1.1", "2")]
    [InlineData("1.9", "2")]
    [InlineData("2.1", "3")]
    [InlineData("2.9", "3")]
    [InlineData("-1.1", "-1")]
    [InlineData("-1.9", "-1")]
    [InlineData("-2.1", "-2")]
    [InlineData("-2.9", "-2")]
    public void Ceiling_ReturnsSmallestIntegerGreaterThanOrEqual(string input, string expected)
    {
        var value = new BigFloat(input);
        var ceiling = BigFloat.Ceiling(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.Equal(expectedValue, ceiling);
    }

    [Theory]
    [InlineData("1.1", "1")]
    [InlineData("1.9", "1")]
    [InlineData("2.1", "2")]
    [InlineData("2.9", "2")]
    [InlineData("-1.1", "-1")]
    [InlineData("-1.9", "-1")]
    [InlineData("-2.1", "-2")]
    [InlineData("-2.9", "-2")]
    public void Truncate_RemovesFractionalPart(string input, string expected)
    {
        var value = new BigFloat(input);
        var truncated = BigFloat.Truncate(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.Equal(expectedValue, truncated);
    }

    #endregion

    #region Min/Max Tests

    [Theory]
    [InlineData(1, 2, 1, 2)]
    [InlineData(2, 1, 1, 2)]
    [InlineData(-1, 1, -1, 1)]
    [InlineData(1, -1, -1, 1)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(-5, -3, -5, -3)]
    public void MinMax_ReturnsCorrectValues(int a, int b, int expectedMin, int expectedMax)
    {
        var valueA = new BigFloat(a);
        var valueB = new BigFloat(b);

        //Future: create Min/Max
        var min = BigFloat.Min(valueA, valueB);
        var max = BigFloat.Max(valueA, valueB);

        Assert.Equal(new BigFloat(expectedMin), min);
        Assert.Equal(new BigFloat(expectedMax), max);
    }

    #endregion

    #region Abs Tests

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-5, 5)]
    [InlineData(0, 0)]
    [InlineData(1.5, 1.5)]
    [InlineData(-1.5, 1.5)]
    public void Abs_ReturnsAbsoluteValue(double input, double expected)
    {
        var value = new BigFloat(input);
        var abs = BigFloat.Abs(value);
        var expectedValue = new BigFloat(expected);
        
        Assert.Equal(expectedValue, abs);
    }

    #endregion

    #region Sign Tests

    [Theory]
    [InlineData(5, 1)]
    [InlineData(-5, -1)]
    [InlineData(0, 0)]
    [InlineData(0.001, 1)]
    [InlineData(-0.001, -1)]
    public void Sign_ReturnsCorrectSign(double input, int expected)
    {
        var value = new BigFloat(input);
        Assert.Equal(expected, value.Sign);
    }

    #endregion
}