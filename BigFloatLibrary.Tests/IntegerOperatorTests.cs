// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class IntegerOperatorTests
{
    public static IEnumerable<object[]> SmallIntFactors()
    {
        int[] factors = [-4, -3, -2, -1, 1, 2, 3, 4];
        string[] values = ["0.5", "-1.5", "3.25", "1e30", "-1e-40"];

        foreach (string value in values)
        {
            foreach (int factor in factors)
            {
                yield return new object[] { value, factor };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SmallIntFactors))]
    public void Multiply_IntMatchesBigInteger(string value, int factor)
    {
        BigFloat original = new BigFloat(value);
        BigFloat viaInt = original * factor;
        BigFloat viaBigInteger = original * new BigFloat(new BigInteger(factor));

        Assert.True(viaInt.EqualsZeroExtended(viaBigInteger));
    }

    [Theory]
    [MemberData(nameof(SmallIntFactors))]
    public void Divide_IntMatchesBigInteger(string value, int factor)
    {
        if (factor == 0)
        {
            return; // unreachable but keeps data generator symmetric
        }

        BigFloat original = new BigFloat(value);
        BigFloat viaInt = original / factor;
        BigFloat viaBigInteger = original / new BigFloat(new BigInteger(factor));

        Assert.True(viaInt.EqualsZeroExtended(viaBigInteger));
    }

    [Theory]
    [InlineData("5", 3, "2")]
    [InlineData("3", 5, "-2")]
    [InlineData("-1.5", -1, "-0.5")]
    [InlineData("-1.5", 2, "-3.5")]
    [InlineData("1", 1, "0")]
    [InlineData("0", -4, "4")]
    [InlineData("0.25", -1, "1.25")]
    public void Subtract_IntProducesExpectedValueAndSign(string value, int subtrahend, string expected)
    {
        BigFloat original = new BigFloat(value);

        BigFloat result = original - subtrahend;
        BigFloat expectedValue = new BigFloat(expected);

        Assert.True(result.EqualsUlp(expectedValue, 4, true));
        Assert.Equal(expectedValue.Sign, result.Sign);
    }

    [Theory]
    [InlineData("1e-2000", int.MaxValue)]
    [InlineData("-1e-2000", int.MinValue)]
    public void DivisionAndMultiplication_HandleExtremeScales(string value, int factor)
    {
        BigFloat original = new BigFloat(value);

        BigFloat multiplied = original * factor;
        BigFloat multipliedReference = original * new BigFloat(new BigInteger(factor));
        Assert.True(multiplied.EqualsZeroExtended(multipliedReference));

        BigFloat divided = original / factor;
        BigFloat dividedReference = original / new BigFloat(new BigInteger(factor));
        Assert.True(divided.EqualsZeroExtended(dividedReference));
    }

#if !DEBUG
    [Fact]
    public void Division_ByZeroIntThrows()
    {
        BigFloat numerator = new BigFloat(1);
        Assert.Throws<DivideByZeroException>(() => numerator / 0);
    }
#endif
}
