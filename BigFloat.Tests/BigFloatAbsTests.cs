using Xunit;
using BigFloatLibrary;

namespace BigFloatLibrary.Tests;

public class BigFloatAbsTests
{
    [Fact]
    public void Verify_ABS()
    {
        Assert.Equal(new BigFloat(5), BigFloatMath.Abs(new BigFloat(-5)));
        Assert.Equal(new BigFloat(5), BigFloatMath.Abs(new BigFloat(5)));
        Assert.Equal(BigFloat.Zero, BigFloatMath.Abs(BigFloat.Zero));
    }
}

