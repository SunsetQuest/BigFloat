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
        var y = BigFloat.BitIncrement(x);
        var z = BigFloat.BitDecrement(y);

        Assert.Equal(x._mantissa, z._mantissa);
        Assert.Equal(x.SizeWithGuardBits, z.SizeWithGuardBits);
        Assert.Equal(x, z);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(-10, 1)]
    public void DecrementThenIncrement_RestoresOriginal(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.BitDecrement(x);
        var z = BigFloat.BitIncrement(y);

        Assert.Equal(x, z);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(3, 5)]
    [InlineData(long.MaxValue, 0)]
    [InlineData(uint.MaxValue, 0)]
    [InlineData(-1, 4)]
    [InlineData(-0x200000000000, -45)]
    public void BitIncrement_MonotonicEqual(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.BitIncrement(x);
        Assert.True(y == x, "BitIncrement usually does not impact compares - unless it causes the top GuardBit to set.");
    }

    [Theory]
    [InlineData(int.MaxValue, 0)]
    public void BitIncrement_MonotonicIncrease(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.BitIncrement(x);
        Assert.True(y > x, "BitIncrement usually does not impact compares - unless it causes the top GuardBit to set, like this one.");
    }

    [Theory]
    [InlineData(long.MaxValue, 0)]
    [InlineData(3, 0)]
    [InlineData(3L << 32, 0)]
    [InlineData(-1, 4)]
    public void GuardBitDecrement_MonotonicDecrease(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.GuardBitDecrement(x);
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
            ? BigFloat.BitIncrement(x)
            : BigFloat.BitDecrement(x);

        Assert.Equal(new BigInteger(expectedMantissa), y._mantissa);
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

        var inc = BigFloat.BitIncrement(x);
        Assert.Equal(big + BigInteger.One, inc._mantissa);
        Assert.Equal(oldSize, inc.SizeWithGuardBits);

        var dec = BigFloat.BitDecrement(x);
        Assert.Equal(big - BigInteger.One, dec._mantissa);
        Assert.Equal(oldSize, dec.SizeWithGuardBits + 1);
    }

    [Theory]
    [InlineData(5L << 32, 1)]
    public void GuardBitIncrement_AdjustsMantissaByGuardDelta(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.GuardBitIncrement(x);

        long delta = (long)(y._mantissa - x._mantissa);
        Assert.Equal(1L << BigFloat.GuardBits, delta);
        Assert.Equal(x.SizeWithGuardBits, y.SizeWithGuardBits);
    }

    [Theory]
    [InlineData(5L << 32, 1)]
    public void GuardBitDecrement_AdjustsMantissaByGuardDelta(long m, int scale)
    {
        var x = new BigFloat(new BigInteger(m), scale + BigFloat.GuardBits, true);
        var y = BigFloat.GuardBitDecrement(x);

        long delta = (long)(x._mantissa - y._mantissa);
        Assert.Equal(1L << BigFloat.GuardBits, delta);
        Assert.Equal(x.SizeWithGuardBits, y.SizeWithGuardBits);
    }
}
