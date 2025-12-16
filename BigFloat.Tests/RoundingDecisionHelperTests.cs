using System.Numerics;
using System.Reflection;

namespace BigFloatLibrary.Tests;

public class RoundingDecisionHelperTests
{
    [Fact]
    public void WouldRoundUp_UsesGuardBitsForStaticAndInstance()
    {
        BigInteger mantissa = (BigInteger.One << (BigFloat.GuardBits + 1)) | (BigInteger.One << (BigFloat.GuardBits - 1));
        bool expected = WouldRoundAwayFromZero(mantissa, BigFloat.GuardBits);

        Assert.Equal(expected, BigFloat.WouldRoundUp(mantissa));

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);
        Assert.Equal(expected, value.WouldRoundUp());
    }

    [Fact]
    public void WouldRoundUp_NegativeMantissaWithClearedGuardBit_DoesNotRound()
    {
        BigInteger mantissa = -(BigInteger.One << (BigFloat.GuardBits + 1));
        bool expected = WouldRoundAwayFromZero(mantissa, BigFloat.GuardBits);

        Assert.Equal(expected, BigFloat.WouldRoundUp(mantissa));

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);
        Assert.Equal(expected, value.WouldRoundUp());
    }

    [Fact]
    public void WouldRoundUp_RespectsCustomBitRemovalAcrossSigns()
    {
        const int bitsToRemove = 4;
        BigInteger positiveMantissa = new(0b100110);
        var positive = new BigFloat(positiveMantissa, binaryScaler: 0, valueIncludesGuardBits: true);
        bool expectedPositive = WouldRoundAwayFromZero(positiveMantissa, bitsToRemove);
        Assert.Equal(expectedPositive, positive.WouldRoundUp(bitsToRemove));

        BigInteger negativeMantissa = -positiveMantissa;
        var negative = new BigFloat(negativeMantissa, binaryScaler: 0, valueIncludesGuardBits: true);
        bool expectedNegative = WouldRoundAwayFromZero(negativeMantissa, bitsToRemove);
        Assert.Equal(expectedNegative, negative.WouldRoundUp(bitsToRemove));
    }

    [Fact]
    public void GetRoundedMantissa_PublicAndPrivateMatchRoundingShift()
    {
        BigInteger mantissa = (BigInteger.One << (BigFloat.GuardBits + 2)) | (BigInteger.One << (BigFloat.GuardBits - 1));
        BigInteger expected = BigIntegerTools.RoundingRightShift(mantissa, BigFloat.GuardBits);

        Assert.Equal(expected, InvokePrivateRoundedMantissa(mantissa));

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);
        Assert.Equal(expected, value.RoundedMantissa);
    }

    [Fact]
    public void GetRoundedMantissa_CarryPropagationUpdatesSize()
    {
        BigInteger mantissa = (BigInteger.One << (BigFloat.GuardBits + 3)) - 1;
        int originalSize = (int)BigInteger.Abs(mantissa).GetBitLength();

        int expectedSize = originalSize;
        BigInteger expectedRounded = BigIntegerTools.RoundingRightShift(mantissa, BigFloat.GuardBits, ref expectedSize);

        (BigInteger rounded, int updatedSize) = InvokePrivateRoundedMantissaWithSize(mantissa, originalSize);

        Assert.Equal(expectedRounded, rounded);
        Assert.Equal(expectedSize, updatedSize);
        Assert.True(updatedSize > originalSize - BigFloat.GuardBits, "Carry should increase size when rounding overflows retained bits.");
    }

    [Fact]
    public void GetRoundedMantissa_PublicMethodRoundsDownWhenBelowMidpoint()
    {
        const int magnitude = 42;
        BigInteger mantissa = ((BigInteger)magnitude << BigFloat.GuardBits)
            | (BigInteger.One << (BigFloat.GuardBits - 2));

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);

        Assert.Equal(magnitude, value.GetRoundedMantissa());
    }

    [Fact]
    public void GetRoundedMantissa_PublicMethodRoundsUpAtHalfway()
    {
        const int magnitude = 42;
        BigInteger mantissa = ((BigInteger)magnitude << BigFloat.GuardBits)
            | (BigInteger.One << (BigFloat.GuardBits - 1));

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);

        Assert.Equal(magnitude + 1, value.GetRoundedMantissa());
    }

    [Fact]
    public void GetRoundedMantissa_PublicMethodRoundsUpWhenAboveMidpoint()
    {
        const int magnitude = 42;
        BigInteger mantissa = ((BigInteger)magnitude << BigFloat.GuardBits)
            | (BigInteger.One << (BigFloat.GuardBits - 1))
            | BigInteger.One;

        var value = new BigFloat(mantissa, binaryScaler: 0, valueIncludesGuardBits: true);

        Assert.Equal(magnitude + 1, value.GetRoundedMantissa());
    }

    private static BigInteger InvokePrivateRoundedMantissa(BigInteger mantissa)
    {
        MethodInfo? method = typeof(BigFloat).GetMethod(
            "GetRoundedMantissa",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(BigInteger)],
            modifiers: null);

        Assert.NotNull(method);
        return (BigInteger)method!.Invoke(null, [mantissa])!;
    }

    private static (BigInteger rounded, int updatedSize) InvokePrivateRoundedMantissaWithSize(BigInteger mantissa, int size)
    {
        MethodInfo? method = typeof(BigFloat).GetMethod(
            "GetRoundedMantissa",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(BigInteger), typeof(int).MakeByRefType()],
            modifiers: null);

        Assert.NotNull(method);

        object[] args = { mantissa, size };
        BigInteger rounded = (BigInteger)method!.Invoke(null, args)!;
        return (rounded, (int)args[1]!);
    }

    private static bool WouldRoundAwayFromZero(BigInteger mantissa, int bitsToRemove)
    {
        BigInteger truncated = BigInteger.Abs(mantissa) >> bitsToRemove;
        if (mantissa.Sign < 0)
        {
            truncated = -truncated;
        }

        BigInteger rounded = BigIntegerTools.RoundingRightShift(mantissa, bitsToRemove);
        return rounded != truncated;
    }
}
