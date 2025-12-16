// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class TruncateByAndRoundTests
{
    [Fact]
    public void TruncateByAndRound_InstanceOverloadRoundsMantissaAcrossPrecisionLevels()
    {
        BigFloat baseValue = new(new BigInteger(0b1011011), binaryScaler: 3, valueIncludesGuardBits: true);
        BigFloat extended = baseValue.AdjustPrecision(8);

        const int bitsToRemove = 5;

        int expectedSize = extended.SizeWithGuardBits;
        BigInteger expectedMantissa = BigIntegerTools.RoundingRightShift(extended.RawMantissa, bitsToRemove, ref expectedSize);
        int expectedScale = extended.Scale + bitsToRemove;

        BigFloat result = extended.TruncateByAndRound(bitsToRemove);

        Assert.Equal(expectedMantissa, result.RawMantissa);
        Assert.Equal(expectedScale, result.Scale);
        Assert.Equal(expectedSize, result.SizeWithGuardBits);
        Assert.True(result.EqualsZeroExtended(BigFloat.TruncateByAndRound(extended, bitsToRemove)));
    }

    [Fact]
    public void TruncateByAndRound_WithReducedPrecisionMaintainsScaleAndRoundingForNegatives()
    {
        BigFloat highPrecision = new(new BigInteger(-0b101101100101), binaryScaler: 5, valueIncludesGuardBits: true);
        BigFloat reduced = highPrecision.AdjustPrecision(-3);

        const int bitsToRemove = 4;

        int expectedSize = reduced.SizeWithGuardBits;
        BigInteger expectedMantissa = BigIntegerTools.RoundingRightShift(reduced.RawMantissa, bitsToRemove, ref expectedSize);
        int expectedScale = reduced.Scale + bitsToRemove;

        BigFloat result = reduced.TruncateByAndRound(bitsToRemove);

        Assert.Equal(expectedMantissa, result.RawMantissa);
        Assert.Equal(expectedScale, result.Scale);
        Assert.Equal(expectedSize, result.SizeWithGuardBits);
        Assert.Equal(BigFloat.TruncateByAndRound(reduced, bitsToRemove), result);
    }
}
