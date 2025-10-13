// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

//using System;
//using System.Diagnostics;
//using System.Numerics;
//using System.Text;
//using Xunit;

//namespace BigFloatLibrary.Tests;

///// <summary>
///// Tests for square root operations
///// </summary>
//public class SqrtTests
//{
//    /// <summary>
//    /// Target time for each test in milliseconds.
//    /// </summary>
//    private const int TestTargetInMilliseconds = 100;

//#if DEBUG
//    private const int MaxDegreeOfParallelism = 1;
//    private const long SqrtBruteForceStoppedAt = 262144;
//#else
//    private readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;
//    private const long SqrtBruteForceStoppedAt = 524288;
//#endif

//    private const int RAND_SEED = 22;
//    private static readonly Random _rand = new(RAND_SEED);

//    #region Basic Sqrt Tests

//    [Theory]
//    [InlineData(0, "0")]
//    [InlineData(1, "1")]
//    [InlineData(4, "2")]
//    [InlineData(9, "3")]
//    [InlineData(16, "4")]
//    [InlineData(25, "5")]
//    [InlineData(36, "6")]
//    [InlineData(49, "7")]
//    [InlineData(64, "8")]
//    [InlineData(81, "9")]
//    [InlineData(100, "10")]
//    [InlineData(121, "11")]
//    [InlineData(144, "12")]
//    public void Sqrt_PerfectSquares_ReturnsExact(int input, string expected)
//    {
//        var value = new BigFloat(input);
//        var result = BigFloat.Sqrt(value);
//        var expectedValue = new BigFloat(expected);

//        Assert.True(result.EqualsZeroExtended(expectedValue));
//    }

//    [Theory]
//    [InlineData(2, "1.4142135623730950488016887242096980785696718753769480731766797379907324784621")]
//    [InlineData(3, "1.7320508075688772935274463415058723669428052538103806280558069794519330169088")]
//    [InlineData(5, "2.2360679774997896964091736687312762354406183596115257242708972454105209256378")]
//    [InlineData(7, "2.6457513110645905905016157536392604257102591830824501803683344592010688232302")]
//    public void Sqrt_NonPerfectSquares_ReturnsApproximate(int input, string expectedPrefix)
//    {
//        var value = new BigFloat(input);
//        var result = BigFloat.Sqrt(value);

//        // Check that result starts with expected prefix
//        var resultStr = result.ToString();
//        Assert.StartsWith(expectedPrefix.Substring(0, Math.Min(expectedPrefix.Length, resultStr.Length)), resultStr);
//    }

//    #endregion

//    #region Sqrt with High Precision Tests

//    [Theory]
//    [InlineData("0.5", "0.7071067811865475244008443621048490392848359376884740365883398689953662392310")]
//    [InlineData("0.25", "0.5")]
//    [InlineData("0.125", "0.3535533905932737622004221810524245196424179688442370182941699344976831196155")]
//    [InlineData("10", "3.1622776601683793319988935444327185337195551393252168268575048527925944386392")]
//    [InlineData("100", "10")]
//    [InlineData("1000", "31.622776601683793319988935444327185337195551393252168268575048527925944386392")]
//    public void Sqrt_VariousValues_CorrectPrecision(string input, string expectedPrefix)
//    {
//        var value = new BigFloat(input);
//        var result = BigFloat.Sqrt(value);

//        // For exact values, check equality
//        if (expectedPrefix.IndexOf('.') == -1 || expectedPrefix.EndsWith(".5") || expectedPrefix.EndsWith(".0"))
//        {
//            var expected = new BigFloat(expectedPrefix);
//            Assert.True(result.EqualsUlp(expected, 1));
//        }
//        else
//        {
//            // For irrational results, check prefix
//            var resultStr = result.ToString();
//            var compareLength = Math.Min(expectedPrefix.Length, resultStr.Length);
//            Assert.StartsWith(expectedPrefix.Substring(0, compareLength), resultStr);
//        }
//    }

//    #endregion

//    #region Sqrt Edge Cases

//    [Fact]
//    public void Sqrt_Zero_ReturnsZero()
//    {
//        var zero = BigFloat.Zero;
//        var result = BigFloat.Sqrt(zero);
//        Assert.True(result.IsZero);
//        Assert.Equal(BigFloat.Zero, result);
//    }

//    [Fact]
//    public void Sqrt_One_ReturnsOne()
//    {
//        var one = BigFloat.One;
//        var result = BigFloat.Sqrt(one);
//        Assert.Equal(BigFloat.One, result);
//    }

//    [Fact]
//    public void Sqrt_NegativeNumber_ThrowsException()
//    {
//        var negative = new BigFloat(-1);
//        Assert.Throws<ArithmeticException>(() => BigFloat.Sqrt(negative));
//    }

//    #endregion

//    #region Sqrt Verification Tests

//    [Fact]
//    public void Sqrt_RandomValues_SquareRootProperty()
//    {
//        // Test that sqrt(x)^2 ≈ x for random values
//        for (int i = 0; i < 100; i++)
//        {
//            var value = BigFloat.RandomWithMantissaBits(
//                mantissaBits: 64,
//                minBinaryExponent: -50,
//                maxBinaryExponent: 50,
//                logarithmic: true,
//                _rand);

//            // Ensure positive
//            if (value.Sign < 0)
//                value = -value;

//            if (value.IsZero)
//                continue;

//            var sqrt = BigFloat.Sqrt(value);
//            var squared = sqrt * sqrt;

//            // Check that squared is approximately equal to original
//            Assert.True(value.EqualsUlp(squared, 3), 
//                $"Sqrt({value})^2 = {squared}, expected approximately {value}");
//        }
//    }

//    [Theory]
//    [InlineData("1000000000000000000000000000000000000000", "1000000000000000000000")]
//    [InlineData("999999999999999999999999999999999999999", "999999999999999999999.999999999999999999999999999999999999999")]
//    public void Sqrt_VeryLargeNumbers(string input, string expectedPrefix)
//    {
//        var value = new BigFloat(input);
//        var result = BigFloat.Sqrt(value);

//        var resultStr = result.ToString();
//        var compareLength = Math.Min(expectedPrefix.Length, resultStr.Length);
//        Assert.StartsWith(expectedPrefix.Substring(0, compareLength), resultStr);
//    }

//    [Theory]
//    [InlineData("0.000000000000000000000000000001", "0.000000000031622776601683")]
//    [InlineData("0.0000000000000000000000000000001", "0.0000000000000001")]
//    public void Sqrt_VerySmallNumbers(string input, string expectedPrefix)
//    {
//        var value = new BigFloat(input);
//        var result = BigFloat.Sqrt(value);

//        var resultStr = result.ToString();
//        var compareLength = Math.Min(expectedPrefix.Length, resultStr.Length);
//        Assert.StartsWith(expectedPrefix.Substring(0, compareLength), resultStr);
//    }

//    #endregion

//    #region Sqrt Performance Tests

//    [Fact(Skip = "Performance test - enable manually")]
//    public void Sqrt_Performance_MeetsTarget()
//    {
//        var sw = Stopwatch.StartNew();
//        var value = new BigFloat("123456789.987654321");

//        const int iterations = 1000;
//        for (int i = 0; i < iterations; i++)
//        {
//            _ = BigFloat.Sqrt(value);
//        }

//        sw.Stop();
//        Assert.True(sw.ElapsedMilliseconds < TestTargetInMilliseconds * 10,
//            $"Performance test took {sw.ElapsedMilliseconds}ms, target was {TestTargetInMilliseconds * 10}ms");
//    }

//    #endregion

//    #region Sqrt Brute Force Tests

//    [Fact(Skip = "Long-running test - enable manually")]
//    public void Sqrt_BruteForce_AllSmallIntegers()
//    {
//        var sb = new StringBuilder();
//        int errorCount = 0;

//        for (long i = 0; i < SqrtBruteForceStoppedAt; i++)
//        {
//            var input = new BigFloat(i);
//            var expectedBigInt = BigIntegerTools.Sqrt(new BigInteger(i));
//            var expectedBigFloat = new BigFloat(expectedBigInt);
//            var result = BigFloat.Sqrt(input);

//            bool isMatch = result.EqualsUlp(expectedBigFloat, 1, true);

//            if (!isMatch)
//            {
//                errorCount++;
//                sb.AppendLine($"Sqrt({i}) = {result}, expected {expectedBigFloat}");

//                if (errorCount > 10)
//                {
//                    sb.AppendLine("... (more errors omitted)");
//                    break;
//                }
//            }
//        }

//        Assert.Equal(0, errorCount);
//    }

//    #endregion

//    #region Helper Method Tests

//    [Fact]
//    public void Sqrt_VerifyWithSpecificFormat()
//    {
//        var inputVal = new BigFloat("256");
//        var output = BigFloat.Sqrt(inputVal);
//        var expectedBF = new BigFloat("16");

//        Assert.True(output.EqualsUlp(expectedBF, 1, true));
//        Assert.Equal(expectedBF.Size, output.Size);
//    }

//    #endregion
//}