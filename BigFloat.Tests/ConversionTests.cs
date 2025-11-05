// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.Numerics;
using Xunit;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for type conversions between BigFloat and other numeric types
/// </summary>
public class ConversionTests
{
    #region From Integer Types to BigFloat

    [Theory]
    [InlineData(byte.MinValue, "0.00")]
    [InlineData(byte.MaxValue, "255")]
    [InlineData((byte)1, "1.00")]
    [InlineData((byte)128, "128")]
    public void Constructor_FromByte_CreatesCorrectValue(byte value, string expected)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (byte)bf);
        Assert.Equal(bf.ToString(), expected);
    }

    [Theory]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData((short)0)]
    [InlineData((short)1)]
    [InlineData((short)-1)]
    public void Constructor_FromShort_CreatesCorrectValue(short value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (short)bf);
        Assert.Equal(value.ToString(), bf.ToString());
    }

    [Theory]
    [InlineData(ushort.MinValue)]
    [InlineData(ushort.MaxValue)]
    [InlineData((ushort)1)]
    [InlineData((ushort)32768)]
    public void Constructor_FromUShort_CreatesCorrectValue(ushort value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (ushort)bf);
        Assert.Equal(value.ToString(), bf.ToString());
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(1000000)]
    [InlineData(-1000000)]
    public void Constructor_FromInt_CreatesCorrectValue(int value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (int)bf);
        Assert.Equal(value.ToString(), bf.ToString());
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(1u)]
    [InlineData(1000000u)]
    public void Constructor_FromUInt_CreatesCorrectValue(uint value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (uint)bf);
        Assert.Equal(value.ToString(), bf.ToString());
    }

    [Theory]
    [InlineData(long.MinValue, "-9223372036854775808")]
    [InlineData(long.MaxValue, "9223372036854775807")]
    [InlineData(0L, "0.0000000000000000000")]
    [InlineData(1L, "1.0000000000000000000")]
    [InlineData(-1L, "-1.0000000000000000000")]
    [InlineData(1000000000000L, "1000000000000.0000000")]
    [InlineData(-1000000000000L, "-1000000000000.0000000")]
    public void Constructor_FromLong_CreatesCorrectValue(long value, string result)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (long)bf);
        Assert.Equal(result, bf.ToString());
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(1UL)]
    [InlineData(1000000000000UL)]
    public void Constructor_FromULong_CreatesCorrectValue(ulong value)
    {
        var bf = new BigFloat(value);
        Assert.Equal(value, (ulong)bf);
        Assert.Equal(value.ToString(), bf.ToString());
    }

    #endregion

    #region From Floating-Point Types to BigFloat

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14159f)]
    [InlineData(-3.14159f)]
    [InlineData(float.MinValue)]
    [InlineData(float.MaxValue)]
    [InlineData(float.Epsilon)] 
    public void Constructor_FromFloat_CreatesCorrectValue(float value)
    {
        if (float.IsFinite(value))
        {
            var bf = new BigFloat(value);
            var backToFloat = (float)bf;
            Assert.Equal(value, backToFloat);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(Math.PI)]
    [InlineData(-Math.PI)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    public void Constructor_FromDouble_CreatesCorrectValue(double value)
    {
        if (double.IsFinite(value))
        {
            var bf = new BigFloat(value);
            var backToDouble = (double)bf;
            Assert.Equal(value, backToDouble);
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("3.14159")]
    [InlineData("-3.14159")]
    [InlineData("79228162514264337593543950335")] // decimal.MaxValue
    [InlineData("-79228162514264337593543950335")] // decimal.MinValue
    public void Constructor_FromDecimal_CreatesCorrectValue(string valueStr)
    {
        var value = decimal.Parse(valueStr);
        var bf = new BigFloat(value);
        
        // For values within decimal range, conversion should be exact
        if (bf.FitsInADecimal)
        {
            var backToDecimal = (decimal)bf;
            Assert.Equal(value, backToDecimal);
        }
    }

    #endregion

    #region From BigInteger to BigFloat

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("1000000000000000000000000000000")]
    [InlineData("-1000000000000000000000000000000")]
    [InlineData("123456789012345678901234567890")]
    public void Constructor_FromBigInteger_CreatesCorrectValue(string valueStr)
    {
        var bigInt = BigInteger.Parse(valueStr);
        var bf = new BigFloat(bigInt);
        
        Assert.True(bf.IsInteger);
        Assert.Equal(bigInt, (BigInteger)bf);
        Assert.Equal(valueStr, bf.ToString());
    }

    #endregion

    #region From BigFloat to Integer Types

    [Theory]
    [InlineData("127", sbyte.MaxValue)]
    [InlineData("-128", sbyte.MinValue)]
    [InlineData("0", (sbyte)0)]
    [InlineData("50", (sbyte)50)]
    [InlineData("-50", (sbyte)-50)]
    public void ExplicitCast_ToSByte_ReturnsCorrectValue(string bfStr, sbyte expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (sbyte)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("255", byte.MaxValue)]
    [InlineData("0", byte.MinValue)]
    [InlineData("128", (byte)128)]
    [InlineData("50", (byte)50)]
    public void ExplicitCast_ToByte_ReturnsCorrectValue(string bfStr, byte expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (byte)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("256", (byte)0)] // Overflow wraps around
    [InlineData("-1", byte.MaxValue)] // Underflow wraps around
    public void ExplicitCast_ToByte_HandlesOverflow(string bfStr, byte expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (byte)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2147483647", int.MaxValue)]
    [InlineData("-2147483648", int.MinValue)]
    [InlineData("0", 0)]
    [InlineData("123456", 123456)]
    [InlineData("-123456", -123456)]
    public void ExplicitCast_ToInt_ReturnsCorrectValue(string bfStr, int expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (int)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("4294967295", uint.MaxValue)]
    [InlineData("0", uint.MinValue)]
    [InlineData("123456", 123456u)]
    [InlineData("2147483648", 2147483648u)]
    public void ExplicitCast_ToUInt_ReturnsCorrectValue(string bfStr, uint expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (uint)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    [InlineData("0", 0L)]
    [InlineData("1234567890123", 1234567890123L)]
    [InlineData("-1234567890123", -1234567890123L)]
    public void ExplicitCast_ToLong_ReturnsCorrectValue(string bfStr, long expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (long)bf;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("18446744073709551615", ulong.MaxValue)]
    [InlineData("0", ulong.MinValue)]
    [InlineData("1234567890123", 1234567890123UL)]
    [InlineData("9223372036854775808", 9223372036854775808UL)]
    public void ExplicitCast_ToULong_ReturnsCorrectValue(string bfStr, ulong expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (ulong)bf;
        Assert.Equal(expected, result);
    }

    #endregion

    #region From BigFloat to Floating-Point Types

    [Theory]
    [InlineData("0", 0.0f)]
    [InlineData("1", 1.0f)]
    [InlineData("-1", -1.0f)]
    [InlineData("3.14159", 3.14159f)]
    [InlineData("-3.14159", -3.14159f)]
    [InlineData("1.23456789", 1.2345679f)] // Limited precision
    public void ExplicitCast_ToFloat_ReturnsCorrectValue(string bfStr, float expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (float)bf;
        Assert.Equal(expected, result, 6); // 6 decimal places precision
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("1", 1.0)]
    [InlineData("-1", -1.0)]
    [InlineData("3.141592653589793", Math.PI)]
    [InlineData("-3.141592653589793", -Math.PI)]
    [InlineData("1.23456789012345", 1.23456789012345)]
    public void ExplicitCast_ToDouble_ReturnsCorrectValue(string bfStr, double expected)
    {
        var bf = new BigFloat(bfStr);
        var result = (double)bf;
        Assert.Equal(expected, result, 15); // 15 decimal places precision
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("-1", "-1")]
    // Future improvement: casting from BigFloat to/from Decimal 
    // [InlineData("123.456", "123.456")]   
    // [InlineData("-123.456", "-123.456")]
    // [InlineData("0.0000000001", "0.0000000001")]
    public void ExplicitCast_ToDecimal_ReturnsCorrectValue(string bfStr, string expectedStr)
    {
        var bf = new BigFloat(bfStr);
        var expected = decimal.Parse(expectedStr);
        
        if (bf.FitsInADecimal)
        {
            var result = (decimal)bf;
            Assert.Equal(expected, result);
        }
    }

    #endregion

    #region From BigFloat to BigInteger

    [Theory]
    //[InlineData("0", "0")]
    //[InlineData("1", "1")]
    //[InlineData("-1", "-1")]
    //[InlineData("123456789012345678901234567890", "123456789012345678901234567890")]
    //[InlineData("-123456789012345678901234567890", "-123456789012345678901234567890")]
    //[InlineData("123.456", "123")] // Truncates fractional part
    //[InlineData("-123.456", "-123")] // Truncates fractional part
    [InlineData("123.999", "123")] // Truncates, doesn't round
    [InlineData("-123.999", "-123")] // Truncates, doesn't round
    public void ExplicitCast_ToBigInteger_ReturnsCorrectValue(string bfStr, string expectedStr)
    {
        var bf = new BigFloat(bfStr);
        var expected = BigInteger.Parse(expectedStr);
        var result = (BigInteger)bf;
        Assert.Equal(expected, result);
    }

    #endregion

    #region Implicit Conversions

    [Theory]
    [InlineData((byte)123)]
    [InlineData((byte)0)]
    [InlineData((byte)255)]
    public void ImplicitConversion_FromByte_Works(byte value)
    {
        BigFloat bf = value; // Implicit conversion
        Assert.Equal(value, (byte)bf);
    }

    [Theory]
    [InlineData((sbyte)123)]
    [InlineData((sbyte)-123)]
    [InlineData((sbyte)0)]
    public void ImplicitConversion_FromSByte_Works(sbyte value)
    {
        BigFloat bf = value; // Implicit conversion
        Assert.Equal(value, (sbyte)bf);
    }

    [Theory]
    [InlineData(123)]
    [InlineData(-123)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void ImplicitConversion_FromInt_Works(int value)
    {
        BigFloat bf = value; // Implicit conversion
        Assert.Equal(value, (int)bf);
    }

    #endregion

    #region Overflow and Special Cases

    [Fact]
    public void ExplicitCast_ToByte_LargeNumber_Overflows()
    {
        var bf = new BigFloat("1000000");
        var result = (byte)bf;
        // Should wrap around due to overflow
        Assert.Equal((byte)(1000000 % 256), result);
    }

    [Fact]
    public void ExplicitCast_ToInt_VeryLargeNumber_Overflows()
    {
        var bf = new BigFloat("999999999999999999999999999999");
        var result = (int)bf;
        // Result will be truncated/overflow - exact value depends on implementation
        // Just ensure it doesn't throw
        _ = result;
    }

    [Fact]
    public void ExplicitCast_ToDouble_InfiniteValue_ReturnsInfinity()
    {
        var bf = new BigFloat("1e500"); // Beyond double range
        var result = (double)bf;
        Assert.True(double.IsPositiveInfinity(result));
        
        var negativeBf = new BigFloat("-1e500");
        var negativeResult = (double)negativeBf;
        Assert.True(double.IsNegativeInfinity(negativeResult));
    }

#if !DEBUG
    [Fact]
    public void ExplicitCast_ToDecimal_OutOfRange_Throws()
    {
        var bf = new BigFloat("1e100"); // Beyond decimal range
        Assert.Throws<OverflowException>(() => { var _ = (decimal)bf; });
    }
#endif

#endregion

    #region Parse and TryParse Tests

    [Theory]
    [InlineData("123", true, 123)]
    [InlineData("123.456", true, 123.456)]
    [InlineData("-123.456", true, -123.456)]
    [InlineData("1.23e10", true, 1.23e10)]
    [InlineData("invalid", false, 0)]
    [InlineData("", false, 0)]
    [InlineData(null, false, 0)]
    public void TryParse_VariousInputs_ReturnsExpectedResult(string? input, bool expectedSuccess, double expectedValue)
    {
        var success = BigFloat.TryParse(input, out var result);
        
        Assert.Equal(expectedSuccess, success);
        if (success)
        {
            var expected = new BigFloat(expectedValue);
            Assert.True(result.EqualsUlp(expected, 1));
        }
        else
        {
            Assert.True(result.IsZero);
        }
    }

    [Theory]
    [InlineData("FF", 255)]
    [InlineData("0xFF", 255)]
    [InlineData("0x100", 256)]
    [InlineData("-0xFF", -255)]
    [InlineData("DEADBEEF", 3735928559)]
    public void ParseHex_ValidHexStrings_ParsesCorrectly(string hex, long expectedValue)
    {
        var result = BigFloat.ParseHex(hex);
        var expected = new BigFloat(expectedValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("101", 5)]
    [InlineData("1111", 15)]
    [InlineData("10000", 16)]
    [InlineData("-1010", -10)]
    [InlineData("11111111", 255)]
    public void ParseBinary_ValidBinaryStrings_ParsesCorrectly(string binary, int expectedValue)
    {
        var result = BigFloat.ParseBinary(binary);
        var expected = new BigFloat(expectedValue);
        Assert.Equal(expected, result);
    }

    #endregion

    #region IConvertible Interface Tests

    [Fact]
    public void GetTypeCode_ReturnsObject()
    {
        var bf = new BigFloat(123);
        var typeCode = bf.GetTypeCode();
        Assert.Equal(TypeCode.Object, typeCode);
    }

    [Theory]
    [InlineData(123.456, typeof(int), 123)]
    [InlineData(123.456, typeof(long), 123L)]
    [InlineData(123.456, typeof(double), 123.456)]
    // [InlineData(123.456, typeof(decimal), 123.456)] //Future improvement: casting from BigFloat to/from Decimal 
    [InlineData(123.456, typeof(string), "123.456")]
    public void ToType_ConvertsCorrectly(double value, Type targetType, object expectedValue)
    {
        var bf = new BigFloat(value);
        var result = ((IConvertible)bf).ToType(targetType, null);
        
        if (targetType == typeof(string))
        {
            Assert.Equal(expectedValue.ToString(), result.ToString().TrimEnd('0'));
        }
        else if (targetType == typeof(double))
        {
            Assert.Equal((double)expectedValue, (double)result, 10);
        }
        else if (targetType == typeof(decimal))
        {
            Assert.Equal((decimal)(double)expectedValue, (decimal)result);
        }
        else
        {
            Assert.Equal(expectedValue, result);
        }
    }

    #endregion

    #region Special Numeric Types (Int128, UInt128, Half)

    [Theory]
    [InlineData("170141183460469231731687303715884105727")] // Int128.MaxValue
    [InlineData("-170141183460469231731687303715884105728")] // Int128.MinValue
    [InlineData("0")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("-123456789012345678901234567890")]
    public void Constructor_FromInt128_CreatesCorrectValue(string valueStr)
    {
        var int128 = Int128.Parse(valueStr);
        var bf = new BigFloat(int128);
        
        Assert.True(bf.IsInteger);
        var backToInt128 = (Int128)bf;
        Assert.Equal(int128, backToInt128);
    }

    [Theory]
    [InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
    [InlineData("0")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("999999999999999999999999999999")]
    public void Constructor_FromUInt128_CreatesCorrectValue(string valueStr)
    {
        var uint128 = UInt128.Parse(valueStr);
        var bf = new BigFloat(uint128);
        
        Assert.True(bf.IsInteger);
        var backToUInt128 = (UInt128)bf;
        Assert.Equal(uint128, backToUInt128);
    }

    //todo: add Half support to BigFloat and enable these tests
    //[Theory]
    //[InlineData(1.0f)]
    //[InlineData(0.5f)]
    //[InlineData(-1.0f)]
    //[InlineData(65504.0f)] // Half.MaxValue
    //[InlineData(-65504.0f)] // Half.MinValue
    //public void Constructor_FromHalf_CreatesCorrectValue(float floatValue)
    //{
    //    var half = (Half)floatValue;
    //    var bf = new BigFloat(half);
        
    //    var backToHalf = (Half)bf;
    //    Assert.Equal(half, backToHalf);
    //}

    #endregion
}