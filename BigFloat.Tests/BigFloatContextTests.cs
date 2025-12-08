using Xunit;

namespace BigFloatLibrary.Tests;

public class BigFloatContextTests
{
    [Fact]
    public void WithAccuracyAppliesRequestedBudget()
    {
        using var ctx = BigFloatContext.WithAccuracy(64);
        BigFloat seed = BigFloat.OneWithAccuracy(8);

        BigFloat doubled = ctx.Run(() => seed + seed);

        Assert.Equal(64, doubled.Accuracy);
        Assert.Same(ctx, BigFloatContext.Current);
    }

    [Fact]
    public void NestedContextsRestorePrevious()
    {
        using var outer = BigFloatContext.WithAccuracy(32);
        Assert.Same(outer, BigFloatContext.Current);

        using (var inner = BigFloatContext.WithAccuracy(48))
        {
            Assert.Same(inner, BigFloatContext.Current);
            BigFloat scoped = inner.Run(() => BigFloat.OneWithAccuracy(16));
            Assert.Equal(48, scoped.Accuracy);
        }

        Assert.Same(outer, BigFloatContext.Current);
    }

    [Fact]
    public void ConstantsConfigurationHonorsRequestedPrecision()
    {
        BigFloat defaultPi = BigFloat.Constants.Get(BigFloat.Catalog.Pi);

        using var ctx = BigFloatContext.ForConstants(constantsPrecisionBits: 64);
        BigFloat scopedPi = ctx.Constants.Get(BigFloat.Catalog.Pi);

        Assert.True(scopedPi.SizeWithGuardBits < defaultPi.SizeWithGuardBits);
        Assert.True(scopedPi.SizeWithGuardBits >= BigFloat.GuardBits);
    }
}
