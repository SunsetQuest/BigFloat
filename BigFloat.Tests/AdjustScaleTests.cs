// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class AdjustScaleTests
{
    [Fact]
    public void AdjustScale_Static_ShiftsScaleAndPreservesPrecision()
    {
        var original = new BigFloat(6, binaryScaler: 3);

        var adjusted = BigFloat.AdjustScale(original, -2);

        Assert.Equal(original.Size, adjusted.Size);
        Assert.Equal(original.Scale - 2, adjusted.Scale);
        Assert.True(adjusted.EqualsUlp(new BigFloat(6, binaryScaler: 1), 0, true));
    }

    [Fact]
    public void AdjustScale_Instance_ShiftsScaleUpwards()
    {
        var original = new BigFloat(7, binaryScaler: -4);

        var adjusted = original.AdjustScale(5);

        Assert.Equal(original.Size, adjusted.Size);
        Assert.Equal(original.Scale + 5, adjusted.Scale);
        Assert.True(adjusted.EqualsUlp(new BigFloat(7, binaryScaler: 1), 0, true));
    }

    [Fact]
    public void AdjustScale_Static_Overflow_Throws()
    {
        var largeScale = new BigFloat(BigInteger.One, int.MaxValue);

        Assert.Throws<OverflowException>(() => BigFloat.AdjustScale(largeScale, 1));
    }

    [Fact]
    public void AdjustScale_Instance_Underflow_Throws()
    {
        var smallScale = new BigFloat(BigInteger.One, int.MinValue);

        Assert.Throws<OverflowException>(() => smallScale.AdjustScale(-1));
    }
}
