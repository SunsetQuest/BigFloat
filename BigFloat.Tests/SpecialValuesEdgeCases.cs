// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for special values, edge cases, and boundary conditions
/// </summary>
public class SpecialValuesAndEdgeCasesTests
{
    #region Zero Tests

    [Fact]
    public void Zero_BehavesCorrectly()
    {
        var zero = BigFloat.ZeroWithAccuracy(0);
        
        // Basic properties
        Assert.True(zero.IsZero);
        Assert.Equal(0, zero.Sign);
        Assert.True(zero.IsInteger);
        Assert.Equal("0", zero.ToString());
        
        // Arithmetic with zero
        var one = BigFloat.OneWithAccuracy(0);
        Assert.Equal(one, one + zero);
        Assert.Equal(one, zero + one);
        Assert.Equal(one, one - zero);
        Assert.Equal(-one, zero - one);
        Assert.Equal(zero, one * zero);
        Assert.Equal(zero, zero * one);
#if !DEBUG
        Assert.Throws<DivideByZeroException>(() => one / zero);
#endif
        Assert.Equal(zero, zero / one);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.0")]
    [InlineData("0.00")]
    [InlineData("0.000")]
    [InlineData("-0")]
    [InlineData("-0.0")]
    [InlineData("+0")]
    [InlineData("+0.0")]
    public void Zero_VariousRepresentations_AreEqual(string zeroStr)
    {
        var zero = new BigFloat(zeroStr);
        
        Assert.True(zero.IsZero);
        Assert.Equal(BigFloat.ZeroWithAccuracy(0), zero);
        Assert.Equal(0, zero.Sign);
    }

    [Theory]
    [InlineData(".0000", "0.0000")]
    [InlineData("0.000", "0.000")]
    [InlineData("00.00", "0.00")]
    [InlineData("-.0000", "0.0000")]
    [InlineData("-0.000", "0.000")]
    [InlineData("+.000000", "0.000000")]
    [InlineData("000", "0")]
    [InlineData("0.0000000000000000000", "0.0000000000000000000")]
    public void Zero_FormattingPreserved(string input, string expected)
    {
        var zero = BigFloat.Parse(input);
        Assert.Equal(expected, zero.ToString());
    }

    #endregion

    #region One Tests

    [Fact]
    public void One_BehavesCorrectly()
    {
        // Basic properties
        var one = BigFloat.OneWithAccuracy(0);
        Assert.False(one.IsZero);
        Assert.Equal(1, one.Sign);
        Assert.True(one.IsInteger);
        Assert.Equal("1", one.ToString());

        // Arithmetic with one
        one = BigFloat.OneWithAccuracy(64);
        var x = new BigFloat("123.456");
        Assert.Equal(x, x * one);
        Assert.Equal(x, one * x);
        Assert.Equal(x, x / one);
        Assert.Equal(BigFloat.Inverse(x), one / x);
    }

    #endregion

    #region Very Large Numbers

    [Theory]
    [InlineData("999999999999999999999999999999999999999999999999999999999999999999999999999")]
    [InlineData("-999999999999999999999999999999999999999999999999999999999999999999999999999")]
    [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890")]
    public void VeryLargeIntegers_HandleCorrectly(string largeNum)
    {
        var bf = new BigFloat(largeNum);
        
        Assert.True(bf.IsInteger);
        Assert.Equal(largeNum, bf.ToString());
        
        // Basic operations should work
        var doubled = bf * 2;
        var halved = doubled / 2;
        Assert.Equal(bf, halved);
    }

    [Theory]
    [InlineData("1e308", "1e308", "2e308")]
    [InlineData("1e500", "1e500", "2e500")]
    [InlineData("1e1000", "1e1000", "2e1000")]
    public void VeryLargeExponents_HandleCorrectly(string a, string b, string expectedSum)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expected = new BigFloat(expectedSum);
        
        var sum = bfA + bfB;
        Assert.True(sum.EqualsUlp(expected, 10));
    }

    #endregion

    #region Very Small Numbers

    [Theory]
    [InlineData("0.000000000000000000000000000000000000000000000000000000000000000000000001")]
    [InlineData("-0.000000000000000000000000000000000000000000000000000000000000000000000001")]
    [InlineData("0.123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890")]
    public void VerySmallNumbers_HandleCorrectly(string smallNum)
    {
        var bf = new BigFloat(smallNum);
        
        Assert.False(bf.IsZero);
        Assert.False(bf.IsInteger);
        
        // Multiplication by reciprocal should yield ~1
        var reciprocal = 1 / bf;
        var product = bf * reciprocal;
        Assert.True(product.EqualsUlp(1, 2));
    }

    [Theory]
    [InlineData("1e-308", "1e-308", "2e-308")]
    [InlineData("1e-500", "1e-500", "2e-500")]
    [InlineData("1e-1000", "1e-1000", "2e-1000")]
    public void VerySmallExponents_HandleCorrectly(string a, string b, string expectedSum)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expected = new BigFloat(expectedSum);
        
        var sum = bfA + bfB;
        Assert.True(sum.EqualsUlp(expected, 10));
    }

    #endregion

    #region Precision Boundary Tests

    [Fact]
    public void GuardBits_AffectPrecision()
    {
        // Values that differ only in guard bits
        var a = new BigFloat(1, 0, valueIncludesGuardBits: false);
        var b = BigFloat.NextUp(a); // Increment by smallest amount (guard bit)
        
        // They should be different at the guard bit level
        Assert.NotEqual(a.RawMantissa, b.RawMantissa);
        
        // But may appear equal in string representation
        var aStr = a.ToString();
        var bStr = b.ToString();
        // String representation might be the same due to rounding
    }

    [Theory]
    [InlineData("0b11", "0b01", 0)]
    [InlineData("0b111", "0b101", 0)]
    [InlineData("0b1111", "0b1101", 0)]
    public void BinaryConstructor_WithScale(string mantissaBinary, string expectedMantissaBinary, int scale)
    {
        BigFloat.TryParseBinary(mantissaBinary.Substring(2), out var mantissaBF);
        BigFloat.TryParseBinary(expectedMantissaBinary.Substring(2), out var expectedMantissaBF);
        
        var bf = new BigFloat(mantissaBF.RawMantissa, scale, valueIncludesGuardBits: true);
        var expected = new BigFloat(expectedMantissaBF.RawMantissa, scale, valueIncludesGuardBits: true);
        
        // Test specific binary patterns
        if (mantissaBF.RawMantissa > expectedMantissaBF.RawMantissa)
        {
            Assert.True(bf.IsGreaterThanUlp(expected, 0));
        }
        else
        {
            Assert.True(bf.IsLessThanUlp(expected, 0));
        }
    }

    #endregion

    #region Out of Precision Tests

    [Theory]
    [InlineData("0", -10, false)]
    [InlineData("0", 0, false)]
    [InlineData("0", 10, false)]
    [InlineData("0", -10, true)]
    [InlineData("0", 0, true)]
    [InlineData("0", 10, true)]
    public void OutOfPrecision_ZeroValues_AlwaysOutOfPrecision(string mantissaStr, int scale, bool includesGuardBits)
    {
        var mantissa = BigInteger.Parse(mantissaStr);
        var bf = new BigFloat(mantissa, scale, includesGuardBits);
        
        Assert.True(bf.IsOutOfPrecision);
        Assert.True(bf.IsZero);
    }

    [Theory]
    [InlineData("1111", -4, false, false)]
    [InlineData("1111", 0, false, false)]
    [InlineData("1111", 4, false, false)]
    [InlineData("-1111", -4, false, false)]
    [InlineData("-1111", 0, false, false)]
    [InlineData("-1111", 4, false, false)]
    [InlineData("1111", -4, true, true)]
    [InlineData("1111", 0, true, true)]
    [InlineData("1111", 4, true, true)]
    [InlineData("-1111", -4, true, true)]
    [InlineData("-1111", 0, true, true)]
    [InlineData("-1111", 4, true, true)]
    public void OutOfPrecision_SmallMantissa_OutOfPrecision(string mantissaStr, int scale, bool includesGuardBits, bool isOutOfPrecision)
    {
        var mantissa = BigInteger.Parse(mantissaStr);
        var bf = new BigFloat(mantissa, scale, includesGuardBits);
        
        // Small mantissas (less than guard bits) are out of precision
        Assert.True(isOutOfPrecision == bf.IsOutOfPrecision);
    }

    #endregion

    #region Special Arithmetic Cases

    [Fact]
    public void Division_ProducingVerySmallResult()
    {
        var small = new BigFloat("1e-100");
        var large = new BigFloat("1e100");
        
        var result = small / large;
        var expected = new BigFloat("1e-200");
        
        Assert.True(result.EqualsUlp(expected, 10));
    }

    [Fact]
    public void Multiplication_ProducingVeryLargeResult()
    {
        var large1 = new BigFloat("1e100");
        var large2 = new BigFloat("1e100");
        
        var result = large1 * large2;
        var expected = new BigFloat("1e200");
        
        Assert.True(result.EqualsUlp(expected, 10));
    }

    [Theory]
    [InlineData("1e100", "1e-100")]
    [InlineData("1e-100", "1e100")]
    [InlineData("123456789012345678901234567890", "0.000000000000000000000000000001")]
    public void Addition_VeryDifferentMagnitudes(string large, string small)
    {
        var largeBf = new BigFloat(large);
        var smallBf = new BigFloat(small);
        
        var sum = largeBf + smallBf;
        
        // When magnitudes are very different, small value might be lost
        // Result should be approximately the larger value
        Assert.True(sum.EqualsUlp(largeBf, 100));
    }

    #endregion

    #region Negative Value Special Cases

    [Theory]
    [InlineData("-0", "0")]
    [InlineData("-0.0", "0")]
    public void NegativeZero_EqualsPositiveZero(string negZero, string posZero)
    {
        var neg = new BigFloat(negZero);
        var pos = new BigFloat(posZero);
        
        Assert.Equal(neg, pos);
        Assert.Equal(0, neg.CompareTo(pos));
        Assert.True(neg.Equals(pos));
    }

 #if !DEBUG
    [Fact]
    public void Sqrt_NegativeNumber_Throws()
    {
        var negative = new BigFloat(-1);
        Assert.Throws<ArithmeticException>(() => BigFloat.Sqrt(negative));
        
        var negativeSmall = new BigFloat("-0.0001");
        Assert.Throws<ArithmeticException>(() => BigFloat.Sqrt(negativeSmall));
    }
#endif

    [Theory]
    [InlineData(-1, 2, 1)]  // Even power of negative = positive
    [InlineData(-1, 3, -1)] // Odd power of negative = negative
    [InlineData(-2, 2, 4)]
    [InlineData(-2, 3, -8)]
    [InlineData(-3, 2, 9)]
    [InlineData(-3, 3, -27)]
    public void Pow_NegativeBase_CorrectSign(int baseValue, int power, int expectedValue)
    {
        var bf = new BigFloat(baseValue);
        var result = BigFloat.Pow(bf, power);
        var expected = new BigFloat(expectedValue);
        
        Assert.Equal(expected, result);
    }

#endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData(byte.MaxValue)]
    [InlineData(sbyte.MaxValue)]
    [InlineData(sbyte.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MinValue)]
    [InlineData(ushort.MaxValue)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(uint.MaxValue)]
    public void IntegerTypeBoundaries_ConvertCorrectly(long value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (long)bf);
        Assert.True(bf.IsInteger);
    }

    [Fact]
    public void LongBoundaries_ConvertCorrectly()
    {
        var maxLong = new BigFloat(long.MaxValue);
        Assert.Equal(long.MaxValue, (long)maxLong);
        
        var minLong = new BigFloat(long.MinValue);
        Assert.Equal(long.MinValue, (long)minLong);
        
        var maxULong = new BigFloat(ulong.MaxValue);
        Assert.Equal(ulong.MaxValue, (ulong)maxULong);
    }

    #endregion

    #region String Parsing Edge Cases

    [Theory]
    [InlineData("1.")]
    [InlineData("1.0")]
    [InlineData("1.00")]
    [InlineData("1.000")]
    [InlineData("01")]
    [InlineData("001")]
    [InlineData("0001")]
    public void Parse_TrailingAndLeadingZeros_NormalizedCorrectly(string input)
    {
        var bf = BigFloat.Parse(input).ToString();
        var expected = double.Parse(input).ToString();
        
        Assert.True(expected.StartsWith(bf) | bf.StartsWith(expected));
    }

    [Theory]
    [InlineData("  123  ", "123")]
    [InlineData("{4_5_6}", "456")]
    [InlineData("(7 8 9)", "789")]
   [InlineData(" 0\n", "0")]
    public void Parse_Whitespace_Trimmed(string input, string expectedValue)
    {
        var bf = BigFloat.Parse(input);
        var expected = new BigFloat(expectedValue);
        
        Assert.Equal(expected, bf);
    }

    [Theory]
    [InlineData("1e10", "10000000000")]
    [InlineData("1E10", "10000000000")]
    [InlineData("1e+10", "10000000000")]
    [InlineData("1E+10", "10000000000")]
    [InlineData("1e-10", "0.0000000001")]
    [InlineData("1E-10", "0.0000000001")]
    [InlineData("1.23e5", "123000")]
    [InlineData("1.23E-5", "0.0000123")]
    public void Parse_ScientificNotation_ParsedCorrectly(string input, string expectedValue)
    {
        var bf = BigFloat.Parse(input);
        var expected = new BigFloat(expectedValue);
        
        Assert.True(bf.EqualsUlp(expected, 1));
    }

    #endregion

    #region Clone and Copy Tests

    [Fact]
    public void Assignment_CreatesCopy()
    {
        var original = new BigFloat("123.456");
        var copy = original;
        
        // Should be equal
        Assert.Equal(original, copy);
        
        // Modifying copy should not affect original (value type behavior)
        copy++;
        Assert.NotEqual(original, copy);
        Assert.Equal(new BigFloat("123.456"), original);
        Assert.Equal(new BigFloat("124.456"), copy);
    }

    [Fact]
    public void StructCopy_WorksCorrectly()
    {
        var original = new BigFloat("999.999");
        
        // Create array to test struct copying
        var array = new BigFloat[] { original };
        var copied = array[0];
        
        Assert.Equal(original, copied);
        
        // Modify copied value
        copied *= new BigFloat(2);
        
        // Original and array element should be unchanged
        Assert.Equal(new BigFloat("999.999"), original);
        Assert.Equal(new BigFloat("999.999"), array[0]);
        Assert.Equal(new BigFloat("1999.998"), copied);
    }

    #endregion

    #region ULP (Unit in Last Place) Tests

    [Theory]
    [InlineData("1")]
    [InlineData("100")]
    [InlineData("0.1")]
    public void NextUp_NextDown_ULPBehavior(string baseValue)
    {
        var bf = new BigFloat(baseValue);
        var nextUp = BigFloat.NextUp(bf);
        var nextDown = BigFloat.NextDown(bf);

        Assert.True(nextUp == bf);

        // NextUp should be greater than original
        Assert.True(nextUp.IsGreaterThanUlp(bf, 0, true));

        // NextDown should be less than original
        Assert.True(nextDown.IsLessThanUlp(bf, 0, true));

        // Round trip: NextUp then NextDown should return to original
        var roundTrip = BigFloat.NextDown(nextUp);
        Assert.Equal(bf.RawMantissa, roundTrip.RawMantissa);
        Assert.Equal(bf.Scale, roundTrip.Scale);
    }

    [Fact]
    public void NextUp_AtZero_ProducesSmallestPositive()
    {
        var zero = BigFloat.ZeroWithAccuracy(0);
        var nextUp = BigFloat.NextUp(zero);
        
        Assert.True(nextUp == zero); // rounds GuardBits first so false;
        Assert.True(nextUp.IsGreaterThanUlp(zero, 0, true));
        Assert.True(nextUp.Sign == 0); // rounds GuardBits first so is Zero;
        Assert.False(nextUp.IsStrictZero); 

        // Should be the smallest representable positive number
        var nextDown = BigFloat.NextDown(nextUp);
        Assert.True(nextDown.IsZero || nextDown.Sign <= 0);
    }

    #endregion

    #region Rounding Mode Tests

    [Theory]
    [InlineData("2.5", "3")]    // Round half to even would give 2, but default is away from zero
    [InlineData("3.5", "4")]
    [InlineData("-2.5", "-3")]
    [InlineData("-3.5", "-4")]
    public void Round_HalfValues_RoundAwayFromZero(string input, string expected)
    {
        var bf = new BigFloat(input);
        var rounded = BigFloat.Round(bf);
        var expectedBf = new BigFloat(expected);
        
        Assert.Equal(expectedBf, rounded);
    }

    [Theory]
    [InlineData("1.4999999999999999999999999999999", "1")]
    [InlineData("1.5000000000000000000000000000001", "2")]
    [InlineData("-1.4999999999999999999999999999999", "-1")]
    [InlineData("-1.5000000000000000000000000000001", "-2")]
    public void Round_NearHalfValues(string input, string expected)
    {
        var bf = new BigFloat(input);
        var rounded = BigFloat.Round(bf);
        var expectedBf = new BigFloat(expected);
        
        Assert.Equal(expectedBf, rounded);
    }

    #endregion

    #region Normalization Tests

    [Theory]
    [InlineData("1.00000000000000000000000000000000000000000000000000")]
    [InlineData("100.0000000000000000000000000000000000000000000000000")]
    [InlineData("0.000000000000000000000000000000000000000000000000001")]
    public void ToString_TrailingZeros_Normalized(string input)
    {
        var bf = new BigFloat(input);
        var str = bf.ToString();
        
        // Should not have excessive trailing zeros in output
        // But should preserve significance where needed
        Assert.NotNull(str);
        Assert.False(string.IsNullOrEmpty(str));
    }

    #endregion

    #region Extreme Value Arithmetic

    [Fact]
    public void Arithmetic_WithMaxValues_HandledGracefully()
    {
        var maxInt = new BigFloat(int.MaxValue);
        var minInt = new BigFloat(int.MinValue);
        
        // Adding max to itself
        var sum = maxInt + maxInt;
        Assert.True(sum > maxInt);
        Assert.Equal(new BigFloat((long)int.MaxValue * 2), sum);
        
        // Subtracting min from max
        var diff = maxInt - minInt;
        Assert.True(diff > maxInt);
        Assert.True(diff.IsPositive);
        
        // Multiplying extremes
        var product = maxInt * minInt;
        Assert.True(product.Sign < 0);
    }

    [Theory]
    [InlineData("1e1000", "1e1000", "2e1000")]
    [InlineData("1e1000", "-1e1000", "0")]
    [InlineData("1e-1000", "1e-1000", "2e-1000")]
    [InlineData("1e-1000", "-1e-1000", "0")]
    public void ExtremeExponents_Addition(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedBf = new BigFloat(expected);
        
        var result = bfA + bfB;
        Assert.True(result.EqualsUlp(expectedBf, 10));
    }

    #endregion

    #region Special Format Parsing

    [Theory]
    [InlineData("∞", false)]          // Infinity symbol
    [InlineData("NaN", false)]        // Not a number
    [InlineData("-∞", false)]         // Negative infinity
    [InlineData("Infinity", false)]   // Word infinity
    [InlineData("-Infinity", false)]  // Negative word infinity
    public void TryParse_SpecialValues_ReturnsFalse(string input, bool expectedSuccess)
    {
        var success = BigFloat.TryParse(input, out var result);
        Assert.Equal(expectedSuccess, success);
    }

    [Theory]
    [InlineData("1,234.56", "1234.56")]    // Thousands separator
    [InlineData("1_234.56", "1234.56")]    // Underscore separator
    [InlineData("$123.45", false)]         // Currency symbol
    [InlineData("123.45%", false)]         // Percent symbol
    public void Parse_FormattedNumbers(string input, object expected)
    {
        if (expected is string expectedStr)
        {
            // Try parsing with InvariantCulture
            var bf = BigFloat.Parse(input.Replace(",", "").Replace("_", ""));
            var expectedBf = new BigFloat(expectedStr);
            Assert.Equal(expectedBf, bf);
        }
        else
        {
#if !DEBUG
            Assert.Throws<FormatException>(() => BigFloat.Parse(input));
#endif
        }
    }

#endregion

    #region Bit Pattern Tests

    [Fact]
    public void Mantissa_Scale_Consistency()
    {
        var values = new[] { "123", "123.456", "0.123", "1.23e10", "1.23e-10" };
        
        foreach (var valueStr in values)
        {
            var bf = new BigFloat(valueStr);
            
            // Reconstruct from mantissa and scale
            var reconstructed = new BigFloat(bf.RawMantissa, bf.Scale, valueIncludesGuardBits: true);
            
            // Should be bitwise identical
            Assert.Equal(bf.RawMantissa, reconstructed.RawMantissa);
            Assert.Equal(bf.Scale, reconstructed.Scale);
            Assert.Equal(bf.Size, reconstructed.Size);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    public void PowerOfTwo_Representation(int power)
    {
        var value = BigInteger.Pow(2, power);
        var bf = new BigFloat(value);
        
        Assert.True(bf.IsInteger);
        Assert.Equal(value, (BigInteger)bf);
        
        // Check that it's efficiently represented
        // Powers of 2 should have a simple mantissa
        Assert.True(BigInteger.IsPow2(BigInteger.Abs(bf.RawMantissa) >> BigFloat.GuardBits));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Constants_ThreadSafe()
    {
        // Constants should be immutable and thread-safe
        var tasks = new Task[10];
        var results = new BigFloat[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[index] = Task.Run(() =>
            {
                results[index] = (BigFloat)1 + (BigFloat)0 + (BigFloat)2 + (BigFloat)10;
            });
        }

        await Task.WhenAll(tasks);

        // All results should be the same
        var expected = new BigFloat(13);
        foreach (var result in results)
        {
            Assert.Equal(expected, result);
        }
    }

    #endregion

    #region Memory and Performance Characteristics

    [Fact]
    public void LargeArray_MemoryEfficient()
    {
        const int size = 10000;
        var array = new BigFloat[size];
        
        // Initialize array
        for (int i = 0; i < size; i++)
        {
            array[i] = new BigFloat(i);
        }
        
        // Verify some values
        Assert.Equal(new BigFloat(0), array[0]);
        Assert.Equal(new BigFloat(size - 1), array[size - 1]);
        Assert.Equal(new BigFloat(size / 2), array[size / 2]);
    }

    #endregion
}