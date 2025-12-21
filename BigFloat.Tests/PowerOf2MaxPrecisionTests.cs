namespace BigFloatLibrary.Tests;

public class PowerOf2MaxPrecisionTests
{
    [Fact]
    public void UsesFullPrecisionWhenOutputFits()
    {
        BigFloat value = new(System.Numerics.BigInteger.One << BigFloat.GuardBits, 0, true);
        BigFloat baseline = BigFloat.PowerOf2(value);

        BigFloat constrained = BigFloat.PowerOf2(value, 4);

        Assert.Equal(baseline, constrained);
        Assert.Equal(baseline.SizeWithGuardBits, constrained.SizeWithGuardBits);
    }

    [Fact]
    public void ReducesPrecisionWhenOutputTooLarge()
    {
        BigFloat value = new(System.Numerics.BigInteger.One << BigFloat.GuardBits, 0, true);

        BigFloat trimmed = BigFloat.PowerOf2(value, 0);

        Assert.NotEqual(BigFloat.PowerOf2(value), trimmed);
        Assert.Equal((BigFloat)0.25, trimmed);
    }
}
