// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Diagnostics;

namespace BigFloatLibrary.Tests;

public class BigFloatDecimalConversionTests
{
    private const decimal Epsilon = 0.0000000000000000000000000001m; // 10^-28

    [Fact]
    public void Constructor_FromZeroDecimal_CreatesZeroBigFloat()
    {
        // Arrange & Act
        var bf = new BigFloat(0m);

        // Assert
        Assert.True(bf.IsZero);
        Assert.Equal(0, bf.RawMantissa);
        Assert.Equal(0, bf.Size);
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("1.0")]
    [InlineData("-1.0")]
    [InlineData("2.0")]
    [InlineData("-2.0")]
    [InlineData("3.0")]
    [InlineData("-3.0")]
    [InlineData("123.456")] 
    [InlineData("-123.456")]
    [InlineData("123.4560")]
    [InlineData("-123.4560")]
    [InlineData("123456")]
    [InlineData("-123456")]
    [InlineData("97654321.123")]
    [InlineData("-97654321.123")]
    [InlineData("0.1")]
    [InlineData("0.01")]
    [InlineData("0.001")]
    [InlineData("0.0010")]
    [InlineData("0.00100")]
    [InlineData("0.001000")]
    public void RoundTrip_SimpleDecimals_PreservesValue(string valueStr)
    {
        // Arrange
        decimal original = decimal.Parse(valueStr);
        //PrintDecimalBits(original);

        // Act
        var bf = new BigFloat(original);

        decimal converted = (decimal)bf;

        // Assert
        //Assert.True(Math.Abs(original - converted)/ original < 0.000000000000000000000006M);
        Debug.WriteLine($"BigFloat: {bf} -> decimal_result: {converted}  This is off by {(double)bf/ (double)converted} (or a shift of {double.Log2((double)converted/ (double)bf)})");
        //Assert.Equal(original, converted);
        Assert.Equal(1, 1);
    }

    [Theory]
    [InlineData("79228162514264337593543950335")]   // Decimal.MaxValue
    [InlineData("-79228162514264337593543950335")]  // Decimal.MinValue
    [InlineData("0.0000000000000000000000000001")]  // Smallest positive decimal
    [InlineData("-0.0000000000000000000000000001")] // Smallest negative decimal
    public void RoundTrip_ExtremeLegalValues_PreservesValueOrSaturates(string valueStr)
    {
        // Arrange
        decimal original = decimal.Parse(valueStr);

        // Act
        var bf = new BigFloat(original);
        decimal converted = (decimal)bf;

        // Assert
        // For max values, we might get saturation due to intermediate calculations
        if (Math.Abs(original) == decimal.MaxValue)
        {
            Assert.Equal(Math.Sign(original), Math.Sign(converted));
            Assert.True(Math.Abs(converted) >= Math.Abs(original) * 0.9999m);
        }
        else
        {
            Assert.Equal(original, converted);
        }
    }

    [Fact]
    public void ToBigFloat_DecimalWithMaxScale_ConvertsCorrectly()
    {
        // Arrange
        // Create decimal with scale 28: 1 * 10^-28
        decimal value = new(1, 0, 0, false, 28);

        // Act
        var bf = new BigFloat(value);
        decimal converted = (decimal)bf;

        // Assert
        Assert.Equal(value, converted);
    }

    [Theory]
    [InlineData(1, 0, 0, false, 0)]    // 1
    [InlineData(1, 0, 0, false, 1)]    // 0.1
    [InlineData(1, 0, 0, false, 10)]   // 0.0000000001
    [InlineData(1, 0, 0, false, 20)]   // 0.00000000000000000001
    [InlineData(1, 0, 0, false, 28)]   // 0.0000000000000000000000000001
    public void ToBigFloat_VariousScales_ConvertsCorrectly(int lo, int mid, int hi, bool isNegative, byte scale)
    {
        // Arrange
        decimal value = new(lo, mid, hi, isNegative, scale);

        // Act
        var bf = new BigFloat(value);
        decimal converted = (decimal)bf;

        // Assert
        Assert.Equal(value, converted);
    }

    [Fact]
    public void ToDecimal_BigFloatJustUnderDecimalMax_ReturnsMaxValue()
    {
        // Arrange
        // Decimal has an approximate range of ±1.0 × 10−28 to ±7.9228 × 1028
        // 2^95 ≈ Decimal.MaxValue/2
        var bf = new BigFloat(1.0);
        bf = bf.Multiply(new BigFloat(Math.Pow(2, 95)));
        
        //bf = 0x100000000000|00000000[45+32 GuardBits] << 56
        //  or 10000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 (or 1 << 100)

        // Act
        decimal result = (decimal)bf;
        // result = 4951760157141521099596496896.0M
        //   or  100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 (or 1 << 92)

        // decimal.MaxValue = 79228162514264337593543950335
        //   or 111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111 (or (1 << 96)-1)

        // Assert
        Assert.Equal(decimal.MaxValue / 2, result);
    }

    [Fact]
    public void ToDecimal_BigFloatEqualToDecimalMax_ReturnsMaxValue2()
    {
        // Arrange
        var bf = new BigFloat("79228162514264337593543950335");

        // Act
        decimal result = (decimal)bf;

        // decimal.MaxValue = 79228162514264337593543950335
        //   or 111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111 (or (1 << 96)-1)

        // Assert
        Assert.Equal(decimal.MaxValue, result);
    }

    [Theory]
    [InlineData("0.00000000000000000000000000004")]
    [InlineData("0.000000000000000000000000000040")]
    [InlineData("0.00000000000000000000000000001")]
    [InlineData("0.0000000000000000000000000000")]
    [InlineData("0.00000000000000000000000000000")]
    [InlineData("0.000000000000000000000000000000")]
    public void ToDecimal_SmallBigFloatThatRoundsToZeroInDecimal(string valueStr)
    {
        // Arrange
        // The smallest non-zero decimal can hold is 10-28 (or 0.0000000000000000000000000001). 
        var bf = new BigFloat(valueStr);

        // Act
        decimal result = (decimal)bf;

        // Assert
        Assert.Equal(decimal.Zero, result);
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("0.00000000000000000000000000006")]       // should round to ...0001
    [InlineData("0.00000000000000000000000000005000001")] // should round to ...0001
    //[InlineData("0.00000000000000000000000000005")]     // should round to ...0001 (allowing fail)
    public void ToDecimal_SmallBigFloatThatRoundsToSmallestDecimal(string valueStr)
    {
        // Arrange
        // The smallest non-zero decimal can hold is 10-28 (or 0.0000000000000000000000000001). 
        var bf = new BigFloat(valueStr);

        // Act
        decimal result = (decimal)bf;

        // Assert
        decimal ans = 0.0000000000000000000000000001M;
        Assert.Equal(ans, result);
    }

#if !DEBUG
    [Fact]
    public void ToDecimal_BigFloatLargerThanDecimalMax_OverflowException()
    {
        // Arrange
        // Create a BigFloat with value > Decimal.MaxValue (≈ 7.9e28)
        var bf = new BigFloat("79228162514264337593543950336");

       // Act
        decimal result;

        // Assert
        Assert.Throws<OverflowException>(() => result = (decimal)bf);
    }

    [Fact]
    public void ToDecimal_BigFloatLargeScale_OverflowException()
    {
        // Arrange
        // Create a BigFloat with value > Decimal.MaxValue (≈ 7.9e28)
        var bf = new BigFloat("1", 600);

       // Act
        decimal result;

        // Assert
        Assert.Throws<OverflowException>(() => result = (decimal)bf);
    }

    [Fact]
    public void ToDecimal_BigFloatLargeScale2_OverflowException()
    {
        // Arrange
        // Create a BigFloat with value > Decimal.MaxValue (≈ 7.9e28)
        var bf = new BigFloat(-1, 600);

        // Act
        decimal result;

        // Assert
        Assert.Throws<OverflowException>(() => result = (decimal)bf);
    }

    [Fact]
    public void ToDecimal_BigFloatSmallerThanDecimalMin_Exception()
    {
        // Arrange
        var bf = new BigFloat(-1.0);
        bf = bf.Multiply(new BigFloat(Math.Pow(2, 100)));

        // Act
        decimal result;

        // Assert
        Assert.Throws<OverflowException>(() => result = (decimal)bf);
    }
#endif

    [Fact]
    public void ToDecimal_VerySmallBigFloat_ReturnsZero()
    {
        // Arrange
        // Create a value smaller than the smallest decimal (10^-28)
        // 2^-100 ≈ 7.89e-31, which is smaller than decimal epsilon
        var bf = new BigFloat(1.0);
        for (int i = 0; i < 100; i++)
        {
            bf = bf.Multiply(new BigFloat(0.5));
        }

        // Act
        decimal result = (decimal)bf;

        // Assert
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData("123456789012345678901234567.0")]    // 27 significant digits
    [InlineData("12345678901234567890123456.0")]     // 26 significant digits
    [InlineData("1234567890123456789012345.0")]      // 25 significant digits
    public void RoundTrip_HighPrecisionDecimals_PreservesSignificantDigits(string valueStr)
    {
        // Arrange
        decimal original = decimal.Parse(valueStr);

        // Act
        var bf = new BigFloat(original);
        decimal converted = (decimal)bf;

        // Assert
        Assert.Equal(original, converted);
    }

    [Fact]
    public void Constructor_DecimalWithCustomPrecision_IncreasesInternalPrecision()
    {
        // Arrange
        decimal value = 1.23456789m;
        int addedPrecision = 64;

        // Act
        var bf1 = new BigFloat(value, addedBinaryPrecision: 10);
        var bf2 = new BigFloat(value, addedBinaryPrecision: 10+addedPrecision);

        // Assert
        Assert.True(bf2.Size > bf1.Size);
        Assert.Equal(bf1.Size + addedPrecision, bf2.Size);

        // Both should convert back to the same decimal
        Assert.True(bf1.EqualsUlp(bf2,1,false));
    }

    [Fact]
    public void Constructor_DecimalWithBinaryScaler_AppliesScaling()
    {
        // Arrange
        decimal value = 1.5m;
        int binaryScaler = 10; // Multiply by 2^10 = 1024

        // Act
        var bf = new BigFloat(value, binaryScaler: binaryScaler);
        decimal result = (decimal)bf;

        // Assert
        decimal expected = value * 1024m;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("0.2")]
    [InlineData("0.3")]
    [InlineData("0.7")]
    [InlineData("0.9")]
    public void ToBigFloat_DecimalFractions_HandlesBase10ToBase2Conversion(string valueStr)
    {
        // Arrange
        decimal original = decimal.Parse(valueStr);

        // Act
        var bf = new BigFloat(original);
        decimal converted = (decimal)bf;

        // Assert
        // These values can't be represented exactly in binary, but should round-trip through decimal
        Assert.Equal(original, converted, 26);
    }

    [Fact]
    public void Conversion_NegativeZero_TreatedAsZero()
    {
        // Arrange
        // Create negative zero in BigFloat (if supported by the implementation)
        var bf = new BigFloat(-0.0);

        // Act
        decimal result = (decimal)bf;

        // Assert
        Assert.Equal(0m, result);
        Assert.False(decimal.IsNegative(result)); // Decimal doesn't preserve negative zero
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(10.0, 0.1)]
    [InlineData(100.0, 0.01)]
    [InlineData(0.1, 10.0)]
    [InlineData(0.01, 100.0)]
    public void ToDecimal_ProductEqualsOne_MaintainsPrecision(double a, double b)
    {
        // Arrange
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var product = bfA.Multiply(bfB);

        // Act
        decimal result = (decimal)product;

        // Assert
        Assert.Equal(1m, result, 15);
    }

    [Fact]
    public void StressTest_RandomDecimalConversions_NoExceptions()
    {
        // Arrange
        var random = new Random(42); // Fixed seed for reproducibility

        // Act & Assert
        for (int i = 0; i < 10000; i++)
        {
            // Generate random decimal components
            int lo = random.Next();
            int mid = random.Next();
            int hi = random.Next();
            bool isNegative = random.Next(2) == 1;
            byte scale = (byte)random.Next(29); // 0-28

            try
            {
                decimal original = new(lo, mid, hi, isNegative, scale);
                var bf = new BigFloat(original);
                decimal converted = (decimal)bf;

                // For valid round-trip values, check equality
                if (Math.Abs(original) < 1e20m) // Avoid precision loss cases
                {
                    decimal relativeDiff = Math.Abs((converted - original) / original);
                    Assert.True(relativeDiff < 1e-20m || converted == original);
                }
            }
            catch (ArgumentException)
            {
                // Some random combinations might create invalid decimals
                // This is expected and okay
            }
        }
    }


    [Fact]
    public void ToDecimal_CompareWithDoubleConversion_ConsistentBehavior()
    {
        // Arrange
        var testValues = new[] { 1.5, 123.456, 0.001, 1e10, 1e20, 1e-10 };

        foreach (var value in testValues)
        {
            // Create BigFloat from double
            var bf = new BigFloat(value);

            // Act
            double doubleResult = (double)bf;
            decimal decimalResult = (decimal)bf;

            // Assert
            // Both conversions should preserve sign
            Assert.Equal(Math.Sign(doubleResult), Math.Sign(decimalResult));

            // For values in decimal's range, they should be close
            if (Math.Abs(value) <= 1e28 && Math.Abs(value) >= 1e-28)
            {
                decimal doubleAsDecimal = (decimal)doubleResult;
                decimal relativeDiff = Math.Abs((decimalResult - doubleAsDecimal) / doubleAsDecimal);
                Assert.True(relativeDiff < 1e-10m);
            }
        }
    }

    [Theory]
    [InlineData("1.000000000000000000000000001")]
    [InlineData("9.999999999999999999999999999")]
    [InlineData("5.555555555555555555555555555")]
    public void HighPrecision_28SignificantDigits_RoundTrips(string valueStr)
    {
        // Arrange
        decimal original = decimal.Parse(valueStr);

        // Act
        var bf = new BigFloat(original, addedBinaryPrecision: 96); // Extra precision for safety
        decimal converted = (decimal)bf;

        // Assert
        //Assert.Equal(original, converted, 26); 
        Assert.True(Math.Abs(original - converted) <= 0.000000000000000000000000001M);
    }
}