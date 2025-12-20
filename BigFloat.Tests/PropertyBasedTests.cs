// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace BigFloatLibrary.Tests;

internal static class BigFloatGenerators
{
    private static Gen<BigFloat> CreateBigFloatGenerator(
        bool allowNegative,
        int minBinaryExponent,
        int maxBinaryExponent,
        int minMantissaBits = 48,
        int maxMantissaBits = 160)
    {
        int lowerMantissa = Math.Max(BigFloat.GuardBits, minMantissaBits);
        return from mantissaBits in Gen.Choose(lowerMantissa, maxMantissaBits)
               from exponent in Gen.Choose(minBinaryExponent, maxBinaryExponent)
               from negative in allowNegative ? Gen.Elements(true, false) : Gen.Constant(false)
               select BigFloat.RandomWithMantissaBits(
                          mantissaBits,
                          exponent - 1,
                          exponent + 1,
                          logarithmic: true,
                          _config._rand) * (negative ? -1 : 1);
    }

    public static Gen<BigFloat> ModerateMagnitudeGen() =>
        CreateBigFloatGenerator(allowNegative: true, minBinaryExponent: -64, maxBinaryExponent: 64);

    public static Gen<BigFloat> PositiveSmallMagnitudeGen() =>
        CreateBigFloatGenerator(allowNegative: false, minBinaryExponent: -8, maxBinaryExponent: 32);
}

internal static class ModerateBigFloatArbitrary
{
    public static Arbitrary<BigFloat> ModerateBigFloat() =>
        Arb.From(BigFloatGenerators.ModerateMagnitudeGen());
}

internal static class PositiveSmallBigFloatArbitrary
{
    public static Arbitrary<BigFloat> PositiveSmallBigFloat() =>
        Arb.From(BigFloatGenerators.PositiveSmallMagnitudeGen());
}

public class PropertyBasedTests
{

    [Property(MaxTest = 30, Arbitrary = new[] { typeof(ModerateBigFloatArbitrary) })]
    public bool ToStringThenParsePreservesValue(BigFloat value)
    {
        if (value.BinaryExponent is < -32 or > 32)
        {
            return true;
        }

        string formatted = value.ToString();
        BigFloat reparsed = BigFloat.Parse(formatted);

        BigFloat delta = BigFloat.Abs(reparsed - value);
        BigFloat scale = BigFloat.Max(BigFloat.Abs(value), (BigFloat)1);
        bool ulpClose = reparsed.EqualsUlp(value, 12, ulpScopeIncludeGuardBits: true);
        if (delta.IsStrictZero)
        {
            return true;
        }

        BigFloat relative = delta / scale;
        return ulpClose || relative < new BigFloat("1e-5");
    }

    [Property(MaxTest = 40, Arbitrary = new[] { typeof(ModerateBigFloatArbitrary) })]
    public bool NeutralElementsHold(BigFloat value)
    {
        bool addZero = (value + (BigFloat)0).EqualsUlp(value, 2, ulpScopeIncludeGuardBits: true);
        bool multiplyOne = (value * (BigFloat)1).EqualsUlp(value, 2, ulpScopeIncludeGuardBits: true);
        bool subtractSelf = (value - value).EqualsUlp((BigFloat)0, 2, ulpScopeIncludeGuardBits: true);

        return addZero && multiplyOne && subtractSelf;
    }

    [Property(MaxTest = 30, Arbitrary = new[] { typeof(ModerateBigFloatArbitrary) })]
    public bool AdditionAssociativityWithinTolerance(BigFloat a, BigFloat b, BigFloat c)
    {
        BigFloat left = (a + b) + c;
        BigFloat right = a + (b + c);

        return left.EqualsUlp(right, 4, ulpScopeIncludeGuardBits: true);
    }

    [Property(MaxTest = 25, Arbitrary = new[] { typeof(PositiveSmallBigFloatArbitrary) })]
    public bool ExpLogRoundTripStaysClose(BigFloat input)
    {
        if (input <= (BigFloat)0)
        {
            return true;
        }

        if (input.BinaryExponent is < -24 or > 24)
        {
            return true;
        }

        double log2 = BigFloat.Log2(input);
        if (double.IsInfinity(log2) || double.IsNaN(log2))
        {
            return true;
        }

        BigFloat roundTrip = (BigFloat)Math.Pow(2.0, log2);
        BigFloat delta = BigFloat.Abs(roundTrip - input);
        BigFloat scale = BigFloat.Max(BigFloat.Abs(input), (BigFloat)1);

        return (delta / scale) < new BigFloat("1e-8");
    }

    [Property(MaxTest = 25, Arbitrary = new[] { typeof(PositiveSmallBigFloatArbitrary) })]
    public bool SqrtSquareRoundTripStaysClose(BigFloat input)
    {
        BigFloat squared = BigFloat.Pow(BigFloat.Sqrt(input), 2);
        return input.EqualsUlp(squared, 6, ulpScopeIncludeGuardBits: true);
    }
}
