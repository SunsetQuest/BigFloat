// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class GetIntegralValueTests
{
    [Fact]
    public void GetIntegralValue_ReturnsBigIntegerForIntegerValues()
    {
        BigInteger largeValue = BigInteger.Parse("987654321012345678901234567890");
        var value = new BigFloat(largeValue);

        var integral = value.GetIntegralValue();

        Assert.Equal(largeValue, integral);
    }

    [Theory]
    [InlineData("123.75", 123)]
    [InlineData("-9876.5", -9876)]
    [InlineData("0.25", 0)]
    public void GetIntegralValue_TruncatesFractionalValues(string input, long expectedInteger)
    {
        var value = new BigFloat(input);

        var integral = value.GetIntegralValue();

        Assert.Equal(new BigInteger(expectedInteger), integral);
    }
}
