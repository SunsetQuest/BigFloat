// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class ArithmeticBranchCoverageTests
{
#if !DEBUG
    [Fact]
    public void Division_ByZero_Throws()
    {
        var numerator = new BigFloat(5);
        var zero = BigFloat.ZeroWithAccuracy(0);

        Assert.Throws<DivideByZeroException>(() => numerator / zero);
    }
#endif

    [Fact]
    public void Division_WithLargeOperands_KeepsExpectedQuotient()
    {
        var numerator = new BigFloat(BigInteger.One << 520);
        var denominator = new BigFloat(BigInteger.One << 260);
        var expected = new BigFloat(BigInteger.One << 260);

        BigFloat result = numerator / denominator;

        Assert.True(result.EqualsZeroExtended(expected));
    }

    [Fact]
    public void Remainder_WithSmallerDividendScale_PreservesDividend()
    {
        var dividend = new BigFloat(new BigInteger(-5), binaryScaler: -5);
        var divisor = new BigFloat(new BigInteger(2));

        var result = BigFloat.Remainder(dividend, divisor);

        Assert.True(result.EqualsZeroExtended(dividend));
    }

    [Fact]
    public void Remainder_WithLargeScaleDifference_ReturnsZeroForUnityDivisor()
    {
        var dividend = new BigFloat(new BigInteger(5), binaryScaler: 40);
        var divisor = new BigFloat(1);

        var result = BigFloat.Remainder(dividend, divisor);

        Assert.True(result.IsStrictZero);
        Assert.Equal(dividend.Scale, result.Scale);
    }

    [Fact]
    public void Addition_WithIntBelowPrecision_ReturnsOriginal()
    {
        var wide = new BigFloat(new BigInteger(1), binaryScaler: 80);

        BigFloat result = wide + 1;

        Assert.Equal(wide, result);
    }

    [Fact]
    public void Addition_WithIntWithinPrecision_AddsValue()
    {
        var value = new BigFloat(new BigInteger(10));

        BigFloat result = value + 5;

        Assert.Equal(new BigFloat(15), result);
    }

    [Fact]
    public void Increment_SkipsWhenOnesPlaceOutOfRange()
    {
        var value = new BigFloat(new BigInteger(3), binaryScaler: 80);

        BigFloat incremented = ++value;

        Assert.Equal(value, incremented);
    }

    [Fact]
    public void Decrement_SkipsWhenOnesPlaceOutOfRange()
    {
        var value = new BigFloat(new BigInteger(3), binaryScaler: 80);

        BigFloat decremented = --value;

        Assert.Equal(value, decremented);
    }

    [Fact]
    public void SetPrecisionWithRound_ReturnsOriginalWhenSizesMatch()
    {
        var value = new BigFloat("1.5");

        var result = BigFloat.SetPrecisionWithRound(value, value.Size);

        Assert.Equal(value, result);
    }

    [Fact]
    public void SetPrecisionWithRound_ExtendsPrecisionWhenRequested()
    {
        var value = new BigFloat("3.75");
        int newSize = value.Size + 5;

        BigFloat result = BigFloat.SetPrecisionWithRound(value, newSize);

        Assert.Equal(newSize, result.Size);
        Assert.True(result.EqualsZeroExtended(value));
    }

    [Fact]
    public void SetPrecisionWithRound_ReducesPrecisionWithRounding()
    {
        var value = new BigFloat("1.5");
        int reducedSize = Math.Max(1, value.Size - 2);

        BigFloat result = BigFloat.SetPrecisionWithRound(value, reducedSize);
        BigFloat expected = value.AdjustPrecision(reducedSize - value.Size);

        Assert.Equal(reducedSize, result.Size);
        Assert.True(result.EqualsZeroExtended(expected));
    }
}
