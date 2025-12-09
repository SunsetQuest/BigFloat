// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.Numerics;
using BigFloatLibrary;
using Xunit;

namespace BigFloatLibrary.Tests;

public class GenericMathInterfaceTests
{
    [Fact]
    public void GenericDotProductWorksForBigFloatAndDouble()
    {
        static T Dot<T>(ReadOnlySpan<T> values, ReadOnlySpan<T> weights) where T : INumberBase<T>
        {
            if (values.Length != weights.Length)
                throw new ArgumentException("Lengths must match", nameof(weights));

            T acc = T.Zero;
            for (int i = 0; i < values.Length; i++)
            {
                acc += values[i] * weights[i];
            }

            return acc;
        }

        BigFloat[] bigFloats = [new BigFloat(1.5), new BigFloat(2), new BigFloat(3)];
        double[] doubles = [1.5, 2, 3];
        BigFloat[] bigFloatWeights = [new BigFloat(2), new BigFloat(2), new BigFloat(2)];
        double[] doubleWeights = [2, 2, 2];

        BigFloat bfResult = Dot<BigFloat>(bigFloats, bigFloatWeights);
        double doubleResult = Dot<double>(doubles, doubleWeights);

        Assert.Equal(new BigFloat(13), bfResult);
        Assert.Equal(13, doubleResult);
    }

    [Fact]
    public void TryConvertFromCheckedRejectsNonFinite()
    {
        bool success = BigFloat.TryConvertFromChecked(double.NaN, out BigFloat result);

        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryConvertToSaturatingProducesInfinityWhenTooLargeForDouble()
    {
        BigFloat huge = new(BigInteger.One << 2000);

        bool success = BigFloat.TryConvertToSaturating(huge, out double converted);

        Assert.True(success);
        Assert.True(double.IsPositiveInfinity(converted));
    }
}
