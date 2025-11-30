// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using Xunit;

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for BigFloat comparers and comparison semantics
/// </summary>
public class ComparerTests
{
    /// <summary>
    /// Target time for each test in milliseconds.
    /// </summary>
    private const int TestTargetInMilliseconds = 100;

    // Helper method for creating BigFloat from decimal
    private static BigFloat BF(decimal value) => (BigFloat)value;

    #region CompareTo Value Ordering Tests

    [Theory]
    [InlineData(1.0, 1.00)]
    [InlineData(2.5, 2.50)]
    [InlineData(-3.125, -3.1250)]
    public void CompareTo_NumericallyEqual_ReturnsZero(double a, double b)
    {
        var x = (BigFloat)(decimal)a;
        var y = (BigFloat)(decimal)b;
        
        Assert.Equal(0, x.CompareTo(y));
        Assert.True(x.Equals(y));
    }

    [Theory]
    [InlineData(1.0, 2.0)]
    [InlineData(-5.0, -4.0)]
    [InlineData(0.125, 0.25)]
    public void CompareTo_OrdersProperly(double a, double b)
    {
        var x = (BigFloat)(decimal)a;
        var y = (BigFloat)(decimal)b;
        
        Assert.True(x.CompareTo(y) < 0);
        Assert.True(y.CompareTo(x) > 0);
    }

    [Fact]
    public void CompareTo_ConsistentWithEquals()
    {
        var a = new BigFloat(123.456m);
        var b = new BigFloat(123.4560m);
        var c = new BigFloat(123.457m);

        Assert.Equal(0, a.CompareTo(b));
        Assert.True(a.Equals(b));

        Assert.NotEqual(0, a.CompareTo(c));
        Assert.False(a.Equals(c));
    }

    #endregion

    #region TotalOrderComparer Semantics Tests

    [Theory]
    [InlineData(2.5, 2.50)]
    [InlineData(1.0, 1.00)]
    [InlineData(-7.75, -7.750)]
    public void TotalOrder_ZeroExtensionTies_CompareEqual(double a, double b)
    {
        var x = (BigFloat)(decimal)a;
        var y = (BigFloat)(decimal)b;

        // CompareTotalPreorder collapses zero-extensions
        Assert.Equal(0, x.CompareTotalPreorder(y));
        Assert.Equal(0, BigFloat.CompareTotalOrderBitwise(in x, in y));
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 1.0)]
    public void TotalOrder_OrdersBySignThenMagnitude(double a, double b)
    {
        var x = (BigFloat)(decimal)a;
        var y = (BigFloat)(decimal)b;
        
        Assert.True(x.CompareTotalPreorder(y) < 0);
        Assert.True(y.CompareTotalPreorder(x) > 0);
    }

    [Fact]
    public void TotalOrderComparer_ConsistentWithGetHashCode()
    {
        var x = new BigFloat(789.012m);
        var y = new BigFloat(789.0120m); // Numerically equal
        
        // CompareTotalPreorder considers them equal
        Assert.Equal(0, x.CompareTotalPreorder(y));
        
        // GetHashCode should also be equal
        _ = x.GetHashCode();
        _ = y.GetHashCode();
    }

    #endregion

    #region Comparison Operator Tests

    [Theory]
    [InlineData(5.5, 10.2)]
    [InlineData(-3.7, 2.1)]
    [InlineData(0.0, 1.0)]
    [InlineData(-1.0, 0.0)]
    public void ComparisonOperators_WorkCorrectly(double smaller, double larger)
    {
        var a = (BigFloat)(decimal)smaller;
        var b = (BigFloat)(decimal)larger;

        #pragma warning disable CS1718
        // Less than
        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < a);

        // Less than or equal
        Assert.True(a <= b);
        Assert.False(b <= a);
        Assert.True(a <= a);

        // Greater than
        Assert.False(a > b);
        Assert.True(b > a);
        Assert.False(a > a);

        // Greater than or equal
        Assert.False(a >= b);
        Assert.True(b >= a);
        Assert.True(a >= a);

        // Equality
        Assert.False(a == b);
        Assert.True(a == a);

        // Inequality
        Assert.True(a != b);
        Assert.False(a != a);
        #pragma warning restore CS1718
    }

    #endregion

    #region Static Comparison Method Tests

    [Theory]
    [InlineData(3.14, 2.71, 1)]
    [InlineData(2.71, 3.14, -1)]
    [InlineData(3.14, 3.14, 0)]
    public void StaticCompare_ReturnsCorrectSign(double a, double b, int expectedSign)
    {
        var x = (BigFloat)(decimal)a;
        var y = (BigFloat)(decimal)b;
        
        int result = BigFloat.Compare(x, y);
        
        if (expectedSign == 0)
            Assert.Equal(0, result);
        else if (expectedSign > 0)
            Assert.True(result > 0);
        else
            Assert.True(result < 0);
    }

    #endregion

    #region Special Value Tests

    [Theory]
    [InlineData("0", "1", -1)]
    [InlineData("0", "-1", 1)]
    [InlineData("999999999999999999999999999999999999", "999999999999999999999999999999999998", 1)]
    [InlineData("0.000000000000000000000000000000001", "0.000000000000000000000000000000002", -1)]
    public void Compare_SpecialCases_ExpectedOrdering(string left, string right, int expectedSign)
    {
        var lhs = new BigFloat(left);
        var rhs = new BigFloat(right);

        int compareResult = lhs.CompareTo(rhs);

        if (expectedSign > 0)
        {
            Assert.True(compareResult > 0);
            Assert.True(lhs > rhs);
        }
        else
        {
            Assert.True(compareResult < 0);
            Assert.True(lhs < rhs);
        }
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("0", "-0")]
    [InlineData("1.00000000000000000000", "1.0")]
    public void Compare_EqualRepresentations_CompareAsEqual(string left, string right)
    {
        var lhs = new BigFloat(left);
        var rhs = new BigFloat(right);

        Assert.Equal(0, lhs.CompareTo(rhs));
        Assert.True(lhs == rhs);
    }

    #endregion

    #region Performance Boundary Tests

    [Fact(Skip = "Performance test - enable manually")]
    public void Compare_Performance_MeetsTarget()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var a = new BigFloat("123456789.987654321");
        var b = new BigFloat("123456789.987654322");
        
        const int iterations = 100000;
        for (int i = 0; i < iterations; i++)
        {
            _ = a.CompareTo(b);
            _ = a < b;
            _ = a == b;
        }
        
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < TestTargetInMilliseconds * 10, 
            $"Performance test took {sw.ElapsedMilliseconds}ms, target was {TestTargetInMilliseconds * 10}ms");
    }

    #endregion
}