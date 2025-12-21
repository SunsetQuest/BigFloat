// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

namespace BigFloatLibrary.Tests;

public class SetPrecisionInstanceTests
{
#pragma warning disable CS0618 // SetPrecision is obsolete; keep coverage as a compatibility check
    [Fact]
    public void SetPrecision_InstanceExtendsAndTruncatesWithoutRounding()
    {
        BigFloat value = new(3, binaryScaler: -2, valueIncludesGuardBits: false, binaryPrecision: 2);
        int originalSize = value.Size;

        BigFloat expanded = value.SetPrecision(originalSize + 3);
        Assert.Equal(originalSize + 3, expanded.Size);
        Assert.Equal(0, value.CompareTo(expanded));

        BigFloat reduced = value.SetPrecision(originalSize - 1);
        Assert.Equal(originalSize - 1, reduced.Size);
        Assert.Equal("0.1", reduced.ToString("B"));
    }
#pragma warning restore CS0618

    [Fact]
    public void SetPrecisionWithRound_InstanceRoundsWhenShrinkingAndPreservesValueWhenGrowing()
    {
        BigFloat value = new(3, binaryScaler: -2, valueIncludesGuardBits: false, binaryPrecision: 2);
        int originalSize = value.Size;

        BigFloat rounded = value.SetPrecisionWithRound(originalSize - 1);
        Assert.Equal(BigFloat.OneWithAccuracy(0), rounded);

        BigFloat extended = value.SetPrecisionWithRound(originalSize + 2);
        Assert.Equal(originalSize + 2, extended.Size);
        Assert.Equal(0, value.CompareTo(extended));
    }
}
