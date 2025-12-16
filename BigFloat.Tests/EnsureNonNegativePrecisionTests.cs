using System;
using Xunit;

namespace BigFloatLibrary.Tests;

public class EnsureNonNegativePrecisionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    public void IntegerConstructors_ThrowWhenPrecisionIsNegative(int binaryPrecision)
    {
        Assert.Throws<OverflowException>(() => new BigFloat(1, binaryPrecision: binaryPrecision));
        Assert.Throws<OverflowException>(() => new BigFloat(1L, binaryPrecision: binaryPrecision));
        Assert.Throws<OverflowException>(() => new BigFloat(1UL, binaryPrecision: binaryPrecision));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-9)]
    public void FloatingConstructors_ThrowWhenPrecisionIsNegative(int binaryPrecision)
    {
        Assert.Throws<OverflowException>(() => new BigFloat(1.0, binaryPrecision: binaryPrecision));
        Assert.Throws<OverflowException>(() => new BigFloat(1.0f, binaryPrecision: binaryPrecision));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-12)]
    public void DecimalConstructor_ThrowsWhenPrecisionIsNegative(int addedBinaryPrecision)
    {
        Assert.Throws<OverflowException>(() => new BigFloat(1.0m, addedBinaryPrecision: addedBinaryPrecision));
    }
}
