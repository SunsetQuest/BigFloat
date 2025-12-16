// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using Xunit;

namespace BigFloatLibrary.Tests;

public class InvalidInitializationTests
{
    [Theory]
    [InlineData(double.NaN, "Value is NaN")]
    [InlineData(double.PositiveInfinity, "Value is infinity")]
    [InlineData(double.NegativeInfinity, "Value is infinity")]
    public void DoubleConstructorRejectsUnsupportedValues(double value, string reason)
    {
        OverflowException ex = Assert.Throws<OverflowException>(() => new BigFloat(value));

        Assert.Equal($"Invalid BigFloat initialization: {reason}", ex.Message);
    }

    [Fact]
    public void DecimalConstructorRejectsNegativePrecision()
    {
        OverflowException ex = Assert.Throws<OverflowException>(() => new BigFloat(1m, addedBinaryPrecision: -1));

        Assert.Equal("Invalid BigFloat initialization: binaryPrecision (-1) cannot be negative.", ex.Message);
    }

    [Fact]
    public void FactoryMethodRejectsNegativeRequestedPrecision()
    {
        OverflowException ex = Assert.Throws<OverflowException>(() => BigFloat.CreateWithPrecisionFromValue(1L, adjustBinaryPrecision: -2));

        Assert.Equal("Invalid BigFloat initialization: binaryPrecision (-1) cannot be negative.", ex.Message);
    }
}
