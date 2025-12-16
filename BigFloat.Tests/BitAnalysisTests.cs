using System.Reflection;
using BigFloatLibrary;
using Xunit;

namespace BigFloatLibrary.Tests;

public class BitAnalysisTests
{
    private static readonly MethodInfo GetBitLengthMethod = typeof(BigFloat)
        .GetMethod("GetBitLength", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(1UL, 1)]
    [InlineData(2UL, 2)]
    [InlineData(0b1011UL, 4)]
    [InlineData(0x8000_0000_0000_0000UL, 64)]
    public void GetBitLength_UsesExpectedCounts(ulong value, int expected)
    {
        int bitLength = (int)GetBitLengthMethod.Invoke(null, new object[] { value })!;

        Assert.Equal(expected, bitLength);
    }

    [Theory]
    [InlineData(20, 1, 17, 0, 2, -1)]
    [InlineData(17, 0, 20, 1, 2, 1)]
    public void NumberOfMatchingLeadingBitsWithRounding_RespectsScaleAlignment(
        int aValue,
        int aScale,
        int bValue,
        int bScale,
        int expectedMatches,
        int expectedSign)
    {
        var a = new BigFloat(aValue, aScale);
        var b = new BigFloat(bValue, bScale);

        int matches = BigFloat.NumberOfMatchingLeadingBitsWithRounding(a, b, out int sign);

        Assert.Equal(expectedMatches, matches);
        Assert.Equal(expectedSign, sign);
    }

    [Theory]
    [InlineData(48, 20, 2)]
    [InlineData(20, 34, 3)]
    public void NumberOfMatchingLeadingMantissaBits_SplitsOnSizeDifference(int aValue, int bValue, int expected)
    {
        var a = new BigFloat(aValue);
        var b = new BigFloat(bValue);

        int matches = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);

        Assert.Equal(expected, matches);
    }
}
