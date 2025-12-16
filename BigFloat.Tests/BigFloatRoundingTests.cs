// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class BigFloatRoundingTests
{
    private const int GuardBits = BigFloat.GuardBits;

    #region Ceiling Tests

    [Fact]
    public void Ceiling_PositiveWholeNumber_ReturnsUnchanged()
    {
        var bf = new BigFloat(5.0);
        var result = bf.Ceiling();
        Assert.Equal(5.0, (double)result);
    }

    [Fact]
    public void Ceiling_PositiveFraction_RoundsUp()
    {
        var bf = new BigFloat(3.1);
        var result = bf.Ceiling();
        Assert.Equal(4.0, (double)result);

        bf = new BigFloat(3.9);
        result = bf.Ceiling();
        Assert.Equal(4.0, (double)result);
    }

    [Fact]
    public void Ceiling_NegativeFraction_RoundsTowardZero()
    {
        var bf = new BigFloat(-3.1);
        var result = bf.Ceiling();
        Assert.Equal(-3.0, (double)result);

        bf = new BigFloat(-3.9);
        result = bf.Ceiling();
        Assert.Equal(-3.0, (double)result);
    }

    [Fact]
    public void Ceiling_Zero_ReturnsZero()
    {
        var bf = BigFloat.ZeroWithAccuracy(0);
        var result = bf.Ceiling();
        Assert.True(result.IsZero);
    }

    [Fact]
    public void Ceiling_SmallPositiveFraction_ReturnsOne()
    {
        var bf = new BigFloat(0.001);
        var result = bf.Ceiling();
        Assert.Equal(1.0, (double)result);
    }

    [Fact]
    public void Ceiling_PowerOfTwoEdgeCase_HandlesOverflow()
    {
        // Test case where rounding up causes bit overflow (e.g., 15.9 -> 16)
        var bf = BigFloat.Parse("15.9");
        var result = bf.Ceiling();
        Assert.Equal(16.0, (double)result);

        // Test with binary: 111.111 -> 1000
        var bf2 = BigFloat.Parse("7.875"); // 111.111 in binary
        var result2 = bf2.Ceiling();
        Assert.Equal(8.0, (double)result2);
    }

    [Fact]
    public void Ceiling_LargeScaleValues()
    {
        // Test with large positive scale (very small numbers)
        var small = new BigFloat(1, -100); // 2^-100
        var result = small.Ceiling();
        Assert.Equal(1.0, (double)result);

        // Test with large negative scale (very large numbers)
        var large = new BigFloat(BigInteger.Parse("123456789"), 20);
        result = large.Ceiling();
        Assert.Equal(0, result.CompareTo(large)); // Should be unchanged as it's already integer
    }

    #endregion

    #region Floor Tests

    [Fact]
    public void Floor_PositiveFraction_RoundsDown()
    {
        var bf = new BigFloat(3.1);
        var result = bf.Floor();
        Assert.Equal(3.0, (double)result);

        bf = new BigFloat(3.9);
        result = bf.Floor();
        Assert.Equal(3.0, (double)result);
    }

    [Fact]
    public void Floor_NegativeFraction_RoundsAwayFromZero()
    {
        var bf = new BigFloat(-3.1);
        var result = bf.Floor();
        Assert.Equal(-4.0, (double)result);

        bf = new BigFloat(-3.9);
        result = bf.Floor();
        Assert.Equal(-4.0, (double)result);
    }

    [Fact]
    public void Floor_WholeNumber_ReturnsUnchanged()
    {
        var bf = new BigFloat(5.0);
        var result = bf.Floor();
        Assert.Equal(5.0, (double)result);

        bf = new BigFloat(-5.0);
        result = bf.Floor();
        Assert.Equal(-5.0, (double)result);
    }

    [Fact]
    public void Floor_Zero_ReturnsZero()
    {
        var bf = BigFloat.ZeroWithAccuracy(0);
        var result = bf.Floor();
        Assert.True(result.IsZero);
    }

    #endregion

    #region Truncate Tests

    [Fact]
    public void Truncate_PositiveFraction_RoundsTowardZero()
    {
        var bf = new BigFloat(3.9);
        var result = bf.Truncate();
        Assert.Equal(3.0, (double)result);
    }

    [Fact]
    public void Truncate_NegativeFraction_RoundsTowardZero()
    {
        var bf = new BigFloat(-3.9);
        var result = bf.Truncate();
        Assert.Equal(-3.0, (double)result);
    }

    [Fact]
    public void Truncate_WholeNumber_ReturnsUnchanged()
    {
        var bf = new BigFloat(5.0);
        var result = bf.Truncate();
        Assert.Equal(5.0, (double)result);

        bf = new BigFloat(-5.0);
        result = bf.Truncate();
        Assert.Equal(-5.0, (double)result);
    }

    [Fact]
    public void Truncate_VerySmallNumbers()
    {
        var bf = new BigFloat(0.999);
        var result = bf.Truncate();
        Assert.Equal(0.0, (double)result);

        bf = new BigFloat(-0.999);
        result = bf.Truncate();
        Assert.Equal(0.0, (double)result);
    }

    [Fact]
    public void TruncateByAndRound_RoundsUpWhenDiscardedBitSet()
    {
        var value = new BigFloat(new BigInteger(0b1111), BigFloat.GuardBits, valueIncludesGuardBits: true);

        var result = BigFloat.TruncateByAndRound(value, 2);

        Assert.Equal(15.0, (double)value);
        Assert.Equal(16.0, (double)result);
        Assert.Equal(new BigInteger(0b100), result.RawMantissa);
        Assert.Equal(BigFloat.GuardBits + 2, result.Scale);
        Assert.Equal(3, result.RawMantissa.GetBitLength());
    }

    [Fact]
    public void TruncateByAndRound_DoesNotRoundWhenDiscardedBitsAreZero()
    {
        var value = new BigFloat(new BigInteger(0b1100), BigFloat.GuardBits, valueIncludesGuardBits: true);

        var result = BigFloat.TruncateByAndRound(value, 2);

        Assert.Equal(12.0, (double)value);
        Assert.Equal(12.0, (double)result);
        Assert.Equal(new BigInteger(0b11), result.RawMantissa);
        Assert.Equal(BigFloat.GuardBits + 2, result.Scale);
        Assert.Equal(2, result.RawMantissa.GetBitLength());
    public void TruncateToIntegerKeepingAccuracy_PreservesAccuracyForFractionalValues()
    {
        var value = new BigFloat(new BigInteger(42), binaryScaler: -5, valueIncludesGuardBits: false, addedBinaryPrecision: 10);
        int originalAccuracy = value.Accuracy;

        var truncated = value.TruncateToIntegerKeepingAccuracy();

        Assert.Equal(originalAccuracy, truncated.Accuracy);
        Assert.True(truncated.IsInteger);
    }

    [Fact]
    public void TruncateToIntegerKeepingAccuracy_ToZeroRetainsAccuracyBudget()
    {
        var tiny = new BigFloat(1, binaryScaler: -200, valueIncludesGuardBits: false, binaryPrecision: 1);
        int originalAccuracy = tiny.Accuracy;

        var truncated = BigFloat.TruncateToIntegerKeepingAccuracy(tiny);

        Assert.True(truncated.IsStrictZero);
        Assert.Equal(originalAccuracy, truncated.Accuracy);
    }

    #endregion

    #region Round Tests (Half Away From Zero)


    [Fact]
    public void RoundToInteger_PositiveAndNegativeWholeNumbers_ReturnUnchanged()
    {
        var positive = new BigFloat(7.0);
        var negative = new BigFloat(-7.0);

        Assert.Equal(positive, BigFloat.RoundToInteger(positive));
        Assert.Equal(negative, BigFloat.RoundToInteger(negative));
    }

    [Fact]
    public void RoundToInteger_FractionalValues_RoundHalfAwayFromZero()
    {
        Assert.Equal(3.0, (double)BigFloat.RoundToInteger(new BigFloat(2.6)));
        Assert.Equal(2.0, (double)BigFloat.RoundToInteger(new BigFloat(2.4)));

        Assert.Equal(-3.0, (double)BigFloat.RoundToInteger(new BigFloat(-2.6)));
        Assert.Equal(-2.0, (double)BigFloat.RoundToInteger(new BigFloat(-2.4)));

        Assert.Equal(3.0, (double)BigFloat.RoundToInteger(new BigFloat(2.5)));
        Assert.Equal(-3.0, (double)BigFloat.RoundToInteger(new BigFloat(-2.5)));
    }

    [Fact]
    public void Round_ExactlyHalf()
    {
        // 2.5 -> 3 (away from zero)
        var bf = new BigFloat(2.5);
        var result = bf.Round();
        Assert.Equal(3.0, (double)result);

        // 3.5 -> 4 (away from zero)
        bf = new BigFloat(3.5);
        result = bf.Round();
        Assert.Equal(4.0, (double)result);

        // -2.5 -> -3 (away from zero)
        bf = new BigFloat(-2.5);
        result = bf.Round();
        Assert.Equal(-3.0, (double)result);

        // -3.5 -> -4 (away from zero)
        bf = new BigFloat(-3.5);
        result = bf.Round();
        Assert.Equal(-4.0, (double)result);
    }

    [Fact]
    public void Round_NotExactlyHalf_RoundsNormally()
    {
        var bf = new BigFloat(2.6);
        var result = bf.Round();
        Assert.Equal(3.0, (double)result);

        bf = new BigFloat(2.4);
        result = bf.Round();
        Assert.Equal(2.0, (double)result);

        bf = new BigFloat(-2.6);
        result = bf.Round();
        Assert.Equal(-3.0, (double)result);

        bf = new BigFloat(-2.4);
        result = bf.Round();
        Assert.Equal(-2.0, (double)result);
    }

    [Fact]
    public void Round_WholeNumber_ReturnsUnchanged()
    {
        var bf = new BigFloat(5.0);
        var result = bf.Round();
        Assert.Equal(5.0, (double)result);
    }

    [Fact]
    public void Round_VerySmallNumbers()
    {
        // 0.4 -> 0
        var bf = new BigFloat(0.4);
        var result = bf.Round();
        Assert.Equal(0.0, (double)result);

        // 0.5 -> 1 (away from zero)
        bf = new BigFloat(0.5);
        result = bf.Round();
        Assert.Equal(1.0, (double)result);

        // 0.6 -> 1
        bf = new BigFloat(0.6);
        result = bf.Round();
        Assert.Equal(1.0, (double)result);
    }

    [Fact]
    public void Round_BinaryPrecisionCases()
    {
        // Test with exact binary representations
        // 0.25 (0.01 in binary)
        var bf = new BigFloat(0.25);
        var result = bf.Round();
        Assert.Equal(0.0, (double)result);

        // 0.75 (0.11 in binary)
        bf = new BigFloat(0.75);
        result = bf.Round();
        Assert.Equal(1.0, (double)result);
    }

    #endregion

    #region FractionalPart Tests

    [Fact]
    public void FractionalPart_PositiveNumber_ReturnsFractionalPart()
    {
        var bf = new BigFloat(3.14159);
        var result = bf.FractionalPart();
        var resultDouble = (double)result;
        Assert.Equal(0.14159, resultDouble, 5); // 5 decimal places precision
    }

    [Fact]
    public void FractionalPart_NegativeNumber_ReturnsFractionalPart()
    {
        var bf = new BigFloat(-3.14159);
        var result = bf.FractionalPart();
        var resultDouble = (double)result;
        Assert.Equal(-0.14159, resultDouble, 5); // 5 decimal places precision
    }

    [Fact]
    public void FractionalPart_WholeNumber_ReturnsZero()
    {
        var bf = new BigFloat(5.0);
        var result = bf.FractionalPart();
        Assert.True(result.IsZero);

        bf = new BigFloat(-5.0);
        result = bf.FractionalPart();
        Assert.True(result.IsZero);
    }

    [Fact]
    public void FractionalPart_Zero_ReturnsZero()
    {
        var bf = BigFloat.ZeroWithAccuracy(0);
        var result = bf.FractionalPart();
        Assert.True(result.IsZero);
    }

    [Fact]
    public void FractionalPart_SmallFraction_ReturnsItself()
    {
        var bf = new BigFloat(0.123);
        var result = bf.FractionalPart();
        Assert.Equal(bf, result);
    }

    #endregion

    #region ModF Tests

    [Fact]
    public void ModF_PositiveNumber_SplitsCorrectly()
    {
        var bf = new BigFloat(3.14159);
        var (intPart, fracPart) = bf.SplitIntegerAndFractionalParts();

        Assert.Equal(3.0, (double)intPart);
        var fracDouble = (double)fracPart;
        Assert.Equal(0.14159, fracDouble, 5);
    }

    [Fact]
    public void ModF_NegativeNumber_SplitsCorrectly()
    {
        var bf = new BigFloat(-3.14159);
        var (intPart, fracPart) = bf.SplitIntegerAndFractionalParts();

        Assert.Equal(-3.0, (double)intPart);
        var fracDouble = (double)fracPart;
        Assert.Equal(-0.14159, fracDouble, 5);
    }

    [Fact]
    public void ModF_WholeNumber_ReturnsSelfAndZero()
    {
        var bf = new BigFloat(5.0);
        var (intPart, fracPart) = bf.SplitIntegerAndFractionalParts();

        Assert.Equal(5.0, (double)intPart);
        Assert.True(fracPart.IsZero);
    }

    [Fact]
    public void ModF_SmallFraction_ReturnsZeroAndSelf()
    {
        var bf = new BigFloat(0.123);
        var (intPart, fracPart) = bf.SplitIntegerAndFractionalParts();

        Assert.Equal(0.0, (double)intPart);
        Assert.Equal((double)bf, (double)fracPart);
    }

    [Fact]
    public void ModF_RecombineEqualsOriginal()
    {
        var bf = new BigFloat(123.456);
        var (intPart, fracPart) = bf.SplitIntegerAndFractionalParts();

        var recombined = intPart + fracPart;
        Assert.Equal(0, bf.CompareTo(recombined));
    }

    #endregion

    #region Edge Cases and Special Values

    [Fact]
    public void RoundingFunctions_VeryLargeScale()
    {
        // Test with numbers that have scales beyond GuardBits
        var bf = new BigFloat(BigInteger.One, 100); // 2^100

        Assert.Equal(bf, bf.Ceiling());
        Assert.Equal(bf, bf.Floor());
        Assert.Equal(bf, bf.Truncate());
        Assert.Equal(bf, bf.Round());
        Assert.True(bf.FractionalPart().IsZero);
    }

    [Fact]
    public void RoundingFunctions_PrecisionEdgeCases()
    {
        // Test numbers at the edge of precision
        var bf = BigFloat.Parse("1.000000000000001");

        var ceil = bf.Ceiling();
        Assert.Equal(2.0, (double)ceil);

        var floor = bf.Floor();
        Assert.Equal(1.0, (double)floor);

        var trunc = bf.Truncate();
        Assert.Equal(1.0, (double)trunc);

        var round = bf.Round();
        Assert.Equal(1.0, (double)round);
    }

    [Fact]
    public void CeilingWithScale_PreservesScale()
    {
        var bf = new BigFloat(3.14, -10); // aka 0.00306640625|000000012143064
        int accuracy = bf.Accuracy;
        var result1 = bf.CeilingPreservingAccuracy();
        var result2 = bf.Ceiling();
        Assert.Equal(result1, BigFloat.OneWithAccuracy(accuracy));
        Assert.Equal(result2, 1);

        Assert.Equal(-accuracy, result1.Scale);
        Assert.Equal(0, result2.Scale);

        Assert.Equal(accuracy + 1, result1.Size);
        Assert.Equal(1, result2.Size);
        
        Assert.Equal(1.0, (double)result2);
    }

    #endregion
}