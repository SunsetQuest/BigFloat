using System.Numerics;

namespace BigFloatLibrary.Tests;

public class BitAdjustTests
{
    [Theory]
    [InlineData(10, 0)]
    [InlineData(-10, 2)]
    public void IncrementThenDecrement_RestoresOriginal(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits + BigFloat.GuardBits, true);
        var y = BigFloat.NextUp(x);
        var z = BigFloat.NextDown(y);

        Assert.Equal(x.RawMantissa, z.RawMantissa);
        Assert.Equal(x.SizeWithGuardBits, z.SizeWithGuardBits);
        Assert.Equal(x, z);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(-10, 1)]
    public void DecrementThenIncrement_RestoresOriginal(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.NextDown(x);
        var z = BigFloat.NextUp(y);

        Assert.Equal(x, z);
    }

    [Theory]
    [InlineData(-2, -1)]
    [InlineData(-1, 4)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(3, 5)]
    [InlineData(long.MaxValue, 0)]
    [InlineData(uint.MaxValue, 0)]
    [InlineData(-0x200000000000, -45)]
    [InlineData(int.MaxValue, 0)]
    public void BitIncrement_MonotonicEqual(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.NextUp(x);
        Assert.True(y == x, "Incrementing the GuardBit is not large enough to the value to not be equal.");
    }

    [Theory]
    [InlineData(long.MaxValue, 0)]
    [InlineData(3, 0)]
    [InlineData(3L << 32, 0)]
    [InlineData(-1, 4)]
    public void GuardBitDecrement_MonotonicDecrease(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.NextDownExtended(x);
        Assert.True(y < x, "BitDecrement should be strictly smaller");
    }

    [Theory]
    // m.expectedMant crosses a power-of-two boundary, so size jumps by +1 or –1  
    [InlineData(7, 0, 8, 1)]
    [InlineData(9, 0, 8, 0)]
    [InlineData(8, 0, 7, -1)]
    [InlineData(-0x200000000000, -45, -0x1FFFFFFFFFFF, -1)]
    [InlineData(-0x1FFFFFFFFFFF, -45, -0x200000000000, 1)]
    public void PowerOfTwo_BoundaryAdjustsSize(long m, int scale, long expectedMantissa, int sizeDelta)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        int oldSize = x.SizeWithGuardBits;

        var y = m < expectedMantissa
            ? BigFloat.NextUp(x)
            : BigFloat.NextDown(x);

        Assert.Equal(new BigInteger(expectedMantissa), y.RawMantissa);
        Assert.Equal(oldSize + sizeDelta, y.SizeWithGuardBits);
    }

    [Fact]
    public void SlowPath_BigMantissa_WorksCorrectly()
    {
        // force size >= 63 so we hit the BigInteger path  
        int threshold = 63;
        var big = BigInteger.One << (BigFloat.GuardBits + threshold);
        var x = new BigFloat(big, 0, true);
        int oldSize = x.SizeWithGuardBits;

        var inc = BigFloat.NextUp(x);
        Assert.Equal(big + BigInteger.One, inc.RawMantissa);
        Assert.Equal(oldSize, inc.SizeWithGuardBits);

        var dec = BigFloat.NextDown(x);
        Assert.Equal(big - BigInteger.One, dec.RawMantissa);
        Assert.Equal(oldSize, dec.SizeWithGuardBits + 1);
    }

    [Theory]
    [InlineData(5L << 32, 1)]
    public void GuardBitIncrement_AdjustsMantissaByGuardDelta(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.NextUpExtended(x);

        long delta = (long)(y.RawMantissa - x.RawMantissa);
        Assert.Equal(1L << BigFloat.GuardBits, delta);
        Assert.Equal(x.SizeWithGuardBits, y.SizeWithGuardBits);
    }

    [Theory]
    [InlineData(5L << 32, 1)]
    public void GuardBitDecrement_AdjustsMantissaByGuardDelta(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.NextDownExtended(x);

        long delta = (long)(x.RawMantissa - y.RawMantissa);
        Assert.Equal(1L << BigFloat.GuardBits, delta);
        Assert.Equal(x.SizeWithGuardBits, y.SizeWithGuardBits);
    }
}
