// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;
using Xunit;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for parsing operations including TryParse, TryParseBinary, and TryParseHex
/// </summary>
public class ParseTests
{
    #region TryParse Error Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("e")]
    [InlineData(".e")]
    [InlineData("1e")]
    [InlineData("1e1.")]
    [InlineData("1e1.1")]
    [InlineData("1e1+")]
    [InlineData("1e1-")]
    [InlineData("1e--1")]
    [InlineData("1e-+1")]
    [InlineData("1e++1")]
    [InlineData("1e+-1")]
    [InlineData("+-1e1")]
    [InlineData("e1")]
    [InlineData("{1")]
    [InlineData("1}")]
    [InlineData("}")]
    [InlineData("{")]
    [InlineData("{}1")]
    [InlineData("1{}")]
    [InlineData("{{}}")]
    [InlineData("{{1}}")]
    [InlineData("{1)")]
    [InlineData("(1}")]
    [InlineData("+-1")]
    [InlineData("--1")]
    [InlineData("++1")]
    [InlineData("-1+")]
    [InlineData("+1-")]
    [InlineData("-1-")]
    [InlineData("+1+")]
    [InlineData("1-+")]
    [InlineData("1+-")]
    [InlineData("1--")]
    [InlineData("1++")]
    [InlineData("*")]
    [InlineData("00x")]
    [InlineData("-0x")]
    [InlineData("0-x")]
    [InlineData("0x-")]
    [InlineData(".")]
    [InlineData("-")]
    [InlineData("-.")]
    [InlineData(".-")]
    [InlineData("+.")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(BigFloat.TryParse(input, out _));
    }

    #endregion

    #region TryParseBinary Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("+")]
    [InlineData("/")]
    [InlineData(".")]
    [InlineData("-.")]
    [InlineData("-+")]
    [InlineData("0+")]
    [InlineData("0-")]
    [InlineData("0.0.")]
    [InlineData("+.0.")]
    [InlineData("12")]
    [InlineData("--1")]
    [InlineData("1.01.")]
    [InlineData(".41")]
    [InlineData("2")]
    public void TryParseBinary_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(BigFloat.TryParseBinary(input, out _, 0));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("00.", 0)]
    [InlineData("01.", 1)]
    [InlineData("10.", 2)]
    [InlineData("11.", 3)]
    public void TryParseBinary_TrailingDot_ParsesCorrectly(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("1000000", 64)]
    [InlineData("10000000", 128)]
    [InlineData("100000000", 256)]
    [InlineData("1000000000", 512)]
    [InlineData("1111111", 127)]
    [InlineData("11111111", 255)]
    [InlineData("111111111", 511)]
    [InlineData("1111111111", 1023)]
    public void TryParseBinary_ByteBoundaries_Positive(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("+1000000", 64)]
    [InlineData("+10000000", 128)]
    [InlineData("+100000000", 256)]
    [InlineData("+1000000000", 512)]
    [InlineData("+1111111", 127)]
    [InlineData("+11111111", 255)]
    [InlineData("+111111111", 511)]
    [InlineData("+1111111111", 1023)]
    public void TryParseBinary_ByteBoundaries_ExplicitPositive(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("-1000000", -64)]
    [InlineData("-10000000", -128)]
    [InlineData("-100000000", -256)]
    [InlineData("-1000000000", -512)]
    [InlineData("-1111111", -127)]
    [InlineData("-11111111", -255)]
    [InlineData("-111111111", -511)]
    [InlineData("-1111111111", -1023)]
    [InlineData("-11111111111", -2047)]
    public void TryParseBinary_ByteBoundaries_Negative(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1000000000000000", 32768)]
    [InlineData("1111111111111101", 65533)]
    [InlineData("1111111111111110", 65534)]
    [InlineData("1111111111111111", 65535)]
    [InlineData("10000000000000000", 65536)]
    [InlineData("10000000000000001", 65537)]
    [InlineData("10000000000000010", 65538)]
    [InlineData("11111111111111111", 131071)]
    public void TryParseBinary_TwoByteBoundaries(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1000000000000000.", 32768)]
    [InlineData("1111111111111101.0", 65533)]
    [InlineData("+1111111111111110", 65534)]
    [InlineData("-1111111111111111", -65535)]
    [InlineData("10000000000000000.", 65536)]
    [InlineData("10000000000000000.0", 65536)]
    [InlineData("-10000000000000000.0", -65536)]
    [InlineData("+10000000000000001", 65537)]
    [InlineData("10000000000000010.00", 65538)]
    [InlineData("11111111111111111.000000000000", 131071)]
    public void TryParseBinary_TwoByteBoundaries_VariousFormats(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1001100110011000001101110101110110001100011011011100100", 21616517498418916L)]
    [InlineData("100110011001100000110111010111011000110001101101110010011", 86466069993675667L)]
    [InlineData("101010101010101010101010101010101010101010101010101010101010101", 6148914691236517205L)]
    [InlineData("1001100110011000001101110101110110001100011011011100100.", 21616517498418916L)]
    [InlineData("-100110011001100000110111010111011000110001101101110010011.0", -86466069993675667L)]
    [InlineData("+101010101010101010101010101010101010101010101010101010101010101.", 6148914691236517205L)]
    public void TryParseBinary_LargeNumbers(string input, long expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out var output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Fact]
    public void TryParseBinary_LoopTest_GrowthPattern()
    {
        double growthSpeed = 1.01;  // Adjust for test speed
        for (long i = 1; i > 0; i = (long)(i * growthSpeed) + 1)
        {
            BigFloat val = (BigFloat)i;
            string binaryBits = Convert.ToString(i, 2);

            // Test positive number
            Assert.True(BigFloat.TryParseBinary(binaryBits, out var output));
            Assert.Equal(val, output);

            // Test negative number
            Assert.True(BigFloat.TryParseBinary("-" + binaryBits, out output));
            Assert.Equal((BigFloat)(-i), output);

            // Test with explicit plus sign
            Assert.True(BigFloat.TryParseBinary("+" + binaryBits, out output));
            Assert.Equal((BigFloat)i, output);

            // Test with leading '-0'
            Assert.True(BigFloat.TryParseBinary("-0" + binaryBits, out output));
            Assert.Equal((BigFloat)(-i), output);

            // Test with trailing '.'
            Assert.True(BigFloat.TryParseBinary("+" + binaryBits + ".", out output));
            Assert.Equal((BigFloat)i, output);

            // Test with trailing '.0'
            Assert.True(BigFloat.TryParseBinary("-0" + binaryBits + ".0", out output));
            Assert.Equal((BigFloat)(-i), output);
        }
    }

    #endregion

    #region TryParseBinary Guard Bit Separator Tests

    [Fact]
    public void TryParseBinary_GuardBitSeparator_TrailingPipe()
    {
        Assert.True(BigFloat.TryParseBinary("1000000000000000.|", out var output));
        Assert.Equal(new BigFloat((ulong)32768 << BigFloat.GuardBits, 0, true), output);
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeBeforeDot()
    {
        Assert.True(BigFloat.TryParseBinary("1000000000000000|.", out var output));
        Assert.Equal(new BigFloat((ulong)32768 << BigFloat.GuardBits, 0, true), output);
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeInMiddle()
    {
        Assert.True(BigFloat.TryParseBinary("100000000000000|0.", out var output));
        Assert.True(output.EqualsZeroExtended(32768));
        BigFloat val = new BigFloat((BigInteger)32768 << (BigFloat.GuardBits-1), 1, valueIncludesGuardBits: true);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeWithFractional()
    {
        Assert.True(BigFloat.TryParseBinary("100000000000000|0.0", out var output));
        BigFloat val = new BigFloat((BigInteger)32768 << (BigFloat.GuardBits - 1), 1, valueIncludesGuardBits: true);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_FractionalWithGuardBits()
    {
        Assert.True(BigFloat.TryParseBinary("10000000000.0000|00", out var output));
        BigFloat val = BigFloat.CreateWithPrecisionFromValue((ulong)32768 << (BigFloat.GuardBits - 1), true, 0, binaryScaler:-4);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterFirstBit()
    {
        Assert.True(BigFloat.TryParseBinary("1|000000000000000.", out var output));
        BigFloat val = BigFloat.CreateWithPrecisionFromValue((ulong)1 << BigFloat.GuardBits, true, 0, binaryScaler: 15);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterDot()
    {
        Assert.True(BigFloat.TryParseBinary("1.|000000000000000", out var output));
        BigFloat val = BigFloat.CreateWithPrecisionFromValue((ulong)1 << BigFloat.GuardBits, true, 0, binaryScaler: 0);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterDotWithFractional()
    {
        Assert.True(BigFloat.TryParseBinary("1|.000000000000000", out var output));
        BigFloat val = BigFloat.CreateWithPrecisionFromValue((ulong)1 << BigFloat.GuardBits, true, 0, binaryScaler: 0);
        Assert.Equal(0, output.CompareTotalOrderBitwise(val));
    }

    #endregion

    #region TryParseBinary BigIntegerTools Tests

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1.", 1)]
    [InlineData("-0", 0)]
    [InlineData("+0", 0)]
    [InlineData(".0", 0)]
    [InlineData(".1", 0)]
    [InlineData("00", 0)]
    [InlineData("01", 1)]
    [InlineData("10", 2)]
    [InlineData("11", 3)]
    public void TryParseBinary_BigIntegerTools_BasicValues(string input, int expected)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("+00", 0)]
    [InlineData("+01", 1)]
    [InlineData("+10", 2)]
    [InlineData("+11", 3)]
    [InlineData("-00", 0)]
    [InlineData("-01", -1)]
    [InlineData("-10", -2)]
    [InlineData("-11", -3)]
    public void TryParseBinary_BigIntegerTools_SignedValues(string input, int expected)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(".00", 0)]
    [InlineData(".01", 0)]
    [InlineData(".10", 0)]
    [InlineData(".11", 0)]
    [InlineData("0.0", 0)]
    [InlineData("0.1", 0)]
    [InlineData("1.0", 1)]
    [InlineData("1.1", 1)]
    public void TryParseBinary_BigIntegerTools_FractionalValues(string input, int expected)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("00.", 0)]
    [InlineData("01.", 1)]
    [InlineData("10.", 2)]
    [InlineData("11.", 3)]
    [InlineData("+00.", 0)]
    [InlineData("+01.", 1)]
    [InlineData("+10.", 2)]
    [InlineData("+11.", 3)]
    public void TryParseBinary_BigIntegerTools_TrailingDot(string input, int expected)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger output));
        Assert.Equal(expected, output);
    }

    [Fact]
    public void TryParseBinary_BasicFunctionality()
    {
        string input = "10100010010111";
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger result));
        BigInteger expected = 10391;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10100010010111", "1010001001100", 1)]
    [InlineData("-10100010010111", "-1010001001100", 1)]
    [InlineData("10100010010110", "1010001001011", 1)]
    [InlineData("-10100010010110", "-1010001001011", 1)]
    [InlineData("10100010010110", "101000100110", 2)]
    [InlineData("-10100010010110", "-101000100110", 2)]
    [InlineData("101000100101011", "1010001001011", 2)]
    [InlineData("-101000100101011", "-1010001001011", 2)]
    public void RightShiftWithRound_BinaryRepresentation(string input, string expectedOutput, int shift)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger inputValue));
        Assert.True(BigIntegerTools.TryParseBinary(expectedOutput, out BigInteger expectedValue));
        
        BigInteger result = BigIntegerTools.RoundingRightShift(inputValue, shift);
        Assert.Equal(expectedValue, result);
    }

    #endregion

    #region TryParseHex Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("-.")]
    [InlineData("1-")]
    [InlineData("0x")]
    [InlineData("-0x")]
    [InlineData("0.0.")]
    [InlineData("+.0.")]
    [InlineData("1.01.")]
    [InlineData(".G1")]
    [InlineData("2.G1")]
    [InlineData("0h-ABCD")]
    public void TryParseHex_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(BigFloat.TryParseHex(input, out _));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("--1", 1)]
    [InlineData("++1", 1)]
    [InlineData("-+1", -1)]
    [InlineData("-+0x55", -0x55)]
    [InlineData("0x-5", -0x5)]
    [InlineData("-0x5", -0x5)]
    [InlineData("0x0", 0)]
    [InlineData("F", 15)]
    [InlineData("-1", -1)]
    [InlineData("-F", -15)]
    [InlineData("00", 0)]
    [InlineData("80", 128)]
    [InlineData("FF", 255)]
    [InlineData("+00", 0)]
    [InlineData("-11", -17)]
    [InlineData("-FF", -255)]
    [InlineData("0.0", 0)]
    public void TryParseHex_ValidInput_ParsesCorrectly(string input, int expected)
    {
        Assert.True(BigFloat.TryParseHex(input, out BigFloat output));
        Assert.Equal(expected, output);
    }

    #endregion

    #region Zero Parsing Tests

    [Theory]
    [InlineData(".0000", "0.0000")]
    [InlineData("0.000", "0.000")]
    [InlineData("00.00", "0.00")]
    [InlineData("-.0000", "0.0000")]
    [InlineData("-0.000", "0.000")]
    [InlineData("+.000000", "0.000000")]
    [InlineData("0", "0")]
    [InlineData("000", "0")]
    [InlineData("0.0000000000000000000", "0.0000000000000000000")]
    [InlineData("0.0000000000000000000000000000000000000", "0.0000000000000000000000000000000000000")]
    public void Parse_ZeroFormats_ReturnsExpectedString(string input, string expected)
    {
        var result = BigFloat.Parse(input);
        Assert.Equal(expected, result.ToString());
    }

    #endregion
}