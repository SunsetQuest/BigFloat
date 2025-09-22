// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok were used in the development of this library.

// Ignore Spelling: Comparers Bitwise Ulp

namespace BigFloatLibrary.Tests;

//todo: add some tests to ensure "myBigFloat+1" equals "bigFloat++"
public class ComparerTests
{
    /// <summary>
    /// Target time for each test. Time based on release mode on 16 core x64 CPU.
    /// </summary>

#if DEBUG
    private const int MaxDegreeOfParallelism = 1;
    private const long sqrtBruteForceStoppedAt = 262144;
    private const long inverseBruteForceStoppedAt = 262144;
#else
    readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;
    const long sqrtBruteForceStoppedAt = 524288;
    const long inverseBruteForceStoppedAt = 524288 * 1;
#endif

    private const int RAND_SEED = 22;// new Random().Next();
    private static readonly Random _rand = new(RAND_SEED);

    private static BigFloat BF(decimal d) => (BigFloat)d;

    public sealed class EqualityAndHash
    {
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(1.0, 1.00)]
        [InlineData(2.5, 2.50)]
        [InlineData(-3.125, -3.1250)]
        [InlineData(123456789.0, 123456789.0000)]
        [InlineData(0.0000001, 0.00000010)]
        public void Equal_values_have_same_hash(double a, double b)
        {
            var x = (BigFloat)(decimal)a;
            var y = (BigFloat)(decimal)b;

            Assert.True(x.Equals(y));
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }

        [Fact]
        public void Zero_hash_is_zero()
        {
            var z1 = BF(0m);
            var z2 = default(BigFloat);
            Assert.True(z1.Equals(z2));
            Assert.Equal(0, z1.GetHashCode());
            Assert.Equal(0, z2.GetHashCode());
        }

        [Fact]
        public void Dictionary_key_collapses_equal_values()
        {
            var a = BF(2.5m);
            var b = BF(2.50m);

            var dict = new Dictionary<BigFloat, int>
            {
                [a] = 1,
                [b] = 2
            };

            Assert.Single(dict);
            Assert.Equal(2, dict[a]);
        }

        [Theory]
        [InlineData(1.0, 2.0)]
        [InlineData(-5.0, -4.0)]
        [InlineData(0.125, 0.25)]
        public void Unequal_values_may_have_different_hashes(double a, double b)
        {
            var x = (BigFloat)(decimal)a;
            var y = (BigFloat)(decimal)b;

            Assert.False(x.Equals(y));
            // Do NOT assert hash inequality; collisions are allowed.
            _ = x.GetHashCode();
            _ = y.GetHashCode();
        }
    }

    public sealed class CompareTo_ValueOrdering
    {
        [Theory]
        [InlineData(1.0, 1.00)]
        [InlineData(2.5, 2.50)]
        [InlineData(-3.125, -3.1250)]
        public void CompareTo_zero_when_numerically_equal(double a, double b)
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
        public void CompareTo_orders_properly(double a, double b)
        {
            var x = (BigFloat)(decimal)a;
            var y = (BigFloat)(decimal)b;
            Assert.True(x.CompareTo(y) < 0);
            Assert.True(y.CompareTo(x) > 0);
        }

        [Fact]
        public void CompareTo_consistent_with_Equals()
        {
            var a = BF(123.456m);
            var b = BF(123.4560m);
            var c = BF(123.457m);

            Assert.Equal(0, a.CompareTo(b));
            Assert.True(a.Equals(b));

            Assert.NotEqual(0, a.CompareTo(c));
            Assert.False(a.Equals(c));
        }
    }

    public sealed class TotalOrderComparer_Semantics
    {
        [Theory]
        [InlineData(2.5, 2.50)]
        [InlineData(1.0, 1.00)]
        [InlineData(-7.75, -7.750)]
        public void Zero_extension_ties_compare_equal(double a, double b)
        {
            var x = (BigFloat)(decimal)a;
            var y = (BigFloat)(decimal)b;

            // CompareTotalOrder collapses zero-extensions
            Assert.Equal(0, x.CompareTotalPreorder(y));
            Assert.Equal(0, BigFloat.CompareTotalOrderBitwise(in x, in y));
        }

        [Theory]
        [InlineData(-1.0, 0.0)]
        [InlineData(0.0, 1.0)]
        [InlineData(0.5, 1.0)]
        public void Orders_by_sign_then_magnitude_when_different(double a, double b)
        {
            var x = (BigFloat)(decimal)a;
            var y = (BigFloat)(decimal)b;
            Assert.True(x.CompareTotalPreorder(y) < 0);
            Assert.True(y.CompareTotalPreorder(x) > 0);
        }

        [Fact]
        public void TotalOrderComparer_is_deterministic_for_sort()
        {
            var data = new List<BigFloat>
            {
                BF(2.5m), BF(2.50m), BF(-1m), BF(0m), BF(3m), BF(3.0m)
            };

            data.Sort(BigFloat.TotalOrderComparer.Instance);

            // Sorting twice yields identical sequence
            var copy = new List<BigFloat>(data);
            copy.Sort(BigFloat.TotalOrderComparer.Instance);
            Assert.Equal(data, copy);
        }
    }

    public sealed class BitwiseEquality
    {
        [Fact]
        public void Bitwise_equal_implies_numeric_equal_and_same_hash()
        {
            var a = BF(42m);
            var b = a; // exact copy

            Assert.True(a.IsBitwiseEqual(b));
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void If_bitwise_diff_but_numeric_equal_then_hashes_match()
        {
            var a = BF(2.5m);
            var b = BF(2.50m);

            // Depending on your constructor, these may or may not be bitwise-equal.
            if (!a.IsBitwiseEqual(b))
            {
                Assert.True(a.Equals(b));
                Assert.Equal(a.GetHashCode(), b.GetHashCode());
                Assert.Equal(0, a.CompareTotalPreorder(b));
            }
        }
    }

    public sealed class CollectionsInterplay
    {
        [Fact]
        public void HashSet_uses_numeric_equality()
        {
            HashSet<BigFloat> set = [BF(1.25m), BF(1.250m)];
            Assert.Single(set);
            Assert.Contains(BF(1.25m), set);
        }

        [Fact]
        public void SortedSet_uses_ValueComparer_by_default_example()
        {
            var set = new SortedSet<BigFloat>(BigFloat.ValueComparer.Instance)
            {
                BF(2.5m), BF(2.50m), BF(-1m), BF(0m)
            };
            // 2.5 and 2.50 collapse as equal under ValueComparer/CompareTo
            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void SortedSet_with_TotalOrderComparer_is_deterministic()
        {
            var set = new SortedSet<BigFloat>(BigFloat.TotalOrderComparer.Instance)
            {
                BF(2.5m), BF(2.50m), BF(-1m), BF(0m)
            };
            // 2.5 and 2.50 compare equal under your current total-order,
            // so they collapse to one element here as well.
            Assert.Equal(3, set.Count);
        }
    }

    public sealed class UlpComparers
    {
        [Fact]
        public void CompareUlp_zero_tolerance_matches_CompareTo()
        {
            var a = BF(10m);
            var b = BF(10.0m);
            Assert.Equal(0, a.CompareUlp(b, 0));
            Assert.Equal(0, a.CompareTo(b));
        }

        [Fact]
        public void UlpToleranceComparer_collapses_small_differences()
        {
            var x = BF(1.0m);
            var y = BF(1.0m); // construct a second value; small differences would be tolerated
            var cmp = new BigFloat.UlpToleranceComparer(ulps: 1, includeGuardBits: true);

            Assert.Equal(0, cmp.Compare(x, y));
        }
    }
}
