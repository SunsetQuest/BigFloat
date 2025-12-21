// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

namespace BigFloatLibrary.Tests;

public class ToNearestIntTests
{
    private static int ExpectedHalfAwayFromZero(double value)
    {
        int truncated = (int)Math.Truncate(value);
        double fractional = value - truncated;
        double magnitude = Math.Abs(fractional);

        if (magnitude >= 0.5)
        {
            return truncated + Math.Sign(value);
        }

        return truncated;
    }

    [Fact]
    public void ToNearestInt_CoversSmallIntegersAcrossFractions()
    {
        double[] offsets = [-0.75, -0.5, -0.25, 0.0, 0.25, 0.5, 0.75];

        for (int whole = -100; whole <= 100; whole++)
        {
            foreach (double offset in offsets)
            {
                double value = whole + offset;
                var bf = new BigFloat(value);

                int expected = ExpectedHalfAwayFromZero(value);
                int actual = BigFloat.ToNearestInt(bf);

                Assert.Equal(expected, actual);
            }
        }
    }

    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(-0.5, -1)]
    [InlineData(1.5, 2)]
    [InlineData(-1.5, -2)]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -3)]
    [InlineData(123456.5, 123457)]
    [InlineData(-123456.5, -123457)]
    public void ToNearestInt_TieBreaksAwayFromZero(double value, int expected)
    {
        var bf = new BigFloat(value);

        int rounded = BigFloat.ToNearestInt(bf);

        Assert.Equal(expected, rounded);
    }

    [Theory]
    [InlineData(1500000000.4, 1500000000)]
    [InlineData(1500000000.6, 1500000001)]
    [InlineData(-1500000000.4, -1500000000)]
    [InlineData(-1500000000.6, -1500000001)]
    [InlineData(0.0, 0)]
    [InlineData(0.25, 0)]
    [InlineData(-0.25, 0)]
    [InlineData(0.499999999, 0)]
    [InlineData(-0.499999999, 0)]
    [InlineData(0.75, 1)]
    [InlineData(-0.75, -1)]
    public void ToNearestInt_HandlesLargeAndTinyMagnitudes(double value, int expected)
    {
        var bf = new BigFloat(value);

        int rounded = BigFloat.ToNearestInt(bf);

        Assert.Equal(expected, rounded);
    }
}
