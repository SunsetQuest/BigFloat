// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Diagnostics;
using System.Numerics;
using Xunit.Sdk;

using static BigFloatLibrary.TestsShared;

namespace BigFloatLibrary.Tests;
/// <summary>
/// Tests for mathematical operations including Pow, NthRoot, and arithmetic operations
/// </summary>
public class MathOperationsTests
{
    #region Pow Tests

    [Theory]
    [InlineData("0.5", 2, "0.25")]
    [InlineData("1.5", 2, "2.25")]
    [InlineData("2.5", 2, "6.25")]
    [InlineData("3.5", 2, "12.25")]
    [InlineData("0.5", 3, "0.125")]
    [InlineData("1.5", 3, "3.375")]
    [InlineData("2.5", 3, "15.625")]
    [InlineData("3.5", 3, "42.875")]
    [InlineData("0.5", 4, "0.0625")]
    [InlineData("1.5", 4, "5.0625")]
    [InlineData("2.5", 4, "39.0625")]
    [InlineData("3.5", 4, "150.0625")]
    public void Pow_FractionalBases_IntegerPowers(string baseStr, int power, string expectedStr)
    {
        var baseValue = BigFloat.Parse(baseStr);
        var expected = BigFloat.Parse(expectedStr);
        var result = BigFloat.Pow(baseValue, power);
        
        Assert.True(result.EqualsZeroExtended(expected));
    }

    [Theory]
    [InlineData("3.000", 0, "1.00")]
    [InlineData("3.000", 1, "3.00")]
    [InlineData("3.000", 2, "9.00")]
    [InlineData("-3.000", 0, "1.00")]
    [InlineData("-3.000", 1, "-3.00")]
    [InlineData("-3.000", 2, "9.00")]
    public void Pow_SmallPowers_ReturnsExact(string baseStr, int power, string expectedStr)
    {
        var baseValue = new BigFloat(baseStr);
        var expected = new BigFloat(expectedStr);
        var result = BigFloat.Pow(baseValue, power);
        
        Assert.True(result.EqualsZeroExtended(expected));
    }

    [Theory]
    [InlineData("3.000", -1, "0.3333")]
    [InlineData("3.000", -2, "0.1111")]
    [InlineData("-3.000", -1, "-0.3333")]
    [InlineData("-3.000", -2, "0.1111")]
    public void Pow_NegativePowers_ReturnsApproximate(string baseStr, int power, string expectedApprox)
    {
        var baseValue = new BigFloat(baseStr);
        var expected = new BigFloat(expectedApprox);
        var result = BigFloat.Pow(baseValue, power);
        
        Assert.True(result.EqualsUlp(expected));
    }

    [Fact]
    public void Pow_LargerPowers()
    {
        var three = new BigFloat("3.000");
        var result = BigFloat.Pow(three, 3);
        var expected = new BigFloat("27.0");
        
        Assert.Equal(expected, result);
        
        var negativeResult = BigFloat.Pow(three, -3);
        Assert.NotEqual(expected, negativeResult);
    }

    #endregion

    #region NthRoot Tests


    [Fact]
    public void NthRoot_RandomValues_AccurateResults2()
    {
        for (int i = BigFloat.GuardBits; i < 3000; i++) //i = (int)(i * 1.111111) + 1)
        {
            for (int root = 1; root < 35; root = (int)(root * 1.111111) + 1)
            {
                BigFloat answer = BigFloat.RandomWithMantissaBits(
                    mantissaBits: i,
                    minBinaryExponent: -300,
                    maxBinaryExponent: 300,
                    logarithmic: true,
                    _rand);

                BigFloat toTest = BigFloat.Pow(answer, root);
                BigFloat result = BigFloat.NthRoot(toTest, root);

                if (!answer.EqualsUlp(result, 3, true))
                {
                    result = BigFloat.NthRoot(toTest, root);
                }

                Assert.True(answer.EqualsUlp(result, 3, true),
                    $"Failed with input({toTest}) and root({root}) with a result of {result} but answer is {answer}");

                Assert.True((toTest.SizeWithGuardBits - result.SizeWithGuardBits) < 32,
                    $"Size difference too big with input({toTest}) and root({root}) with a result of {result} but answer is {answer}");
            }
        }
    }

    [Fact]
    public void NthRoot_RandomValues_AccurateResults()
    {
        RunBudgeted(TestTargetInMilliseconds, RAND_SEED, (rand, iter) =>
        {
            int mantissaBits = (int)LogUniform(rand, BigFloat.GuardBits, 2999);
            int root = (int)LogUniform(rand, 1, 34);

            BigFloat answer = BigFloat.RandomWithMantissaBits(
                mantissaBits: mantissaBits,
                minBinaryExponent: -300,
                maxBinaryExponent: 300,
                logarithmic: true,
                rand);

            BigFloat toTest = BigFloat.Pow(answer, root);
            BigFloat result = BigFloat.NthRoot(toTest, root);

            if (!answer.EqualsUlp(result, 3, true))
                throw new XunitException(
                    $"EqualsUlp failed: seed={RAND_SEED}, iter={iter}, bits={mantissaBits}, root={root}, " +
                    $"result={result}, answer={answer}, input={toTest}");

            int sizeDiff = toTest.SizeWithGuardBits - result.SizeWithGuardBits;
            if (sizeDiff >= 32)
                throw new XunitException(
                    $"Size diff too big: seed={RAND_SEED}, iter={iter}, bits={mantissaBits}, root={root}, " +
                    $"sizeDiff={sizeDiff}, result={result}, answer={answer}, input={toTest}");
        });
    }

    [Fact]
    public void Verify_NthRoot()
    {
        const long minA = 2, maxA = 999;   // answer < 1000
        const int minE = 1, maxE = 199;   // e < 200

        RunBudgeted(10, RAND_SEED, (rand, iter) =>
        {
            long a = LogUniform(rand, minA, maxA);
            int e = (int)LogUniform(rand, minE, maxE);

            BigInteger lo = BigInteger.Pow(a, e);
            BigInteger hi = BigInteger.Pow(a + 1, e);

            BigInteger x = BigIntegerTools.RandomBigInteger(lo, hi, rand);
            BigInteger root = BigIntegerTools.NthRoot(x, e);

            if (root != a)
                throw new XunitException($"NthRoot failed: seed={RAND_SEED}, iter={iter}, a={a}, e={e}");
        });
    }

    private static void RunBudgeted(int targetMs, int seed, Action<Random, int> testCase)
    {
        int n = CalibrateIterations(targetMs, seed, testCase);
        var r = new Random(seed);
        for (int i = 0; i < n; i++) testCase(r, i);
    }

    private static int CalibrateIterations(int targetMs, int seed, Action<Random, int> testCase)
    {
        // Short window keeps this light but still adapts to machine speed.
        int windowMs = int.Clamp(targetMs, 10, 50);
        var r = new Random((int)(seed ^ 0x9E37_79B9));
        var sw = Stopwatch.StartNew();

        int n = 0;
        while (sw.ElapsedMilliseconds < windowMs)
            testCase(r, n++);

        double msPer = sw.Elapsed.TotalMilliseconds / Math.Max(1, n);
        return Math.Max(1, (int)Math.Round(targetMs / msPer));
    }

    private static long LogUniform(Random r, long minInclusive, long maxInclusive)
    {
        double logMin = Math.Log(minInclusive);
        double logMax = Math.Log(maxInclusive);
        long v = (long)Math.Round(Math.Exp(logMin + r.NextDouble() * (logMax - logMin)));
        return v < minInclusive ? minInclusive : (v > maxInclusive ? maxInclusive : v);
    }

    [Theory]
    [InlineData("7777777777777777777777777777777777777777777777777777777777777777", 7, "1340494621.5142142784634135012228258256621009971950248327657|6045823859")]
    [InlineData("7777777777777777777777777777777777777777777777777777777777777777", 4, "9391044157537525.19591975149938555692792588485605707185903878276688969|9549582798593")]
    [InlineData("77777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777", 7, "1862611236825425192.5326420663234462718496133629936707812842460267769993007449764005342755106890750175013920585641604590068868|74051982282")]
    [InlineData("7777777777777777777777777777777", 3, "19813073175.8770993405594|9316958138")]
    [InlineData("7777777777777777777777777777777", 7, "25880.89921")]
    public void NthRoot_SpecificValues(string valueStr, int root, string expectedStr)
    {
        var value = new BigFloat(valueStr);
        var expected = new BigFloat(expectedStr);
        var result = BigFloat.NthRoot(value, root);
        
        // Using a relaxed comparison since these are complex calculations
        var difference = BigFloat.Abs(result - expected);
        var relativeDifference = difference / expected;
        
        Assert.True(relativeDifference < new BigFloat("0.0000001"), 
            $"NthRoot({valueStr}, {root}) = {result}, expected {expectedStr}");
    }

    [Theory]
    [InlineData(10000000000000000UL, 2, "100000000.000000000|000000000000000")]
    [InlineData(10000000000000000UL, 3, "215443.46900318837|217592935665193")]
    [InlineData(10000000000000000UL, 4, "10000.000000000000|000000000000000")]
    [InlineData(10000000000000000UL, 5, "1584.8931924611134|852021013733915")]
    [InlineData(10000000000000000UL, 6, "464.15888336127788|924100763509194")]
    [InlineData(1000000000000000UL, 2, "31622776.60168379|3319988935444327")]
    [InlineData(1000000000000000UL, 3, "100000.0000000000|0000000000000000")]
    [InlineData(1000000000000000UL, 4, "5623.413251903490|8039495103977648")]
    [InlineData(1000000000000000UL, 5, "1000.000000000000|0000000000000000")]
    [InlineData(1000000000000000UL, 6, "316.2277660168379|3319988935444327")]
    [InlineData(100000000UL, 2, "10000.0000000000000|00000000000")]
    [InlineData(100000000UL, 3, "464.158883361277889241|00763509194465")]
    [InlineData(100000000UL, 4, "100.00000000000000000|00000000000")]
    [InlineData(100000000UL, 5, "39.810717055349725077|02523050877")]
    [InlineData(100000000UL, 6, "21.544346900318837217|59293566519")]
    [InlineData(100UL, 2, "10.0000000000000000000|0000000000")]
    [InlineData(100UL, 3, "4.6415888336127|7889241007")]
    [InlineData(100UL, 4, "3.1622776601683793319988935444327185|33719555139")]
    [InlineData(100UL, 5, "2.51188643150958011108|50320677993")]
    [InlineData(100UL, 6, "2|.1544346900318837217|59293566519")]
    [InlineData(10UL, 2, "3.1622776601683793319988935444327185|33719555139")]
    [InlineData(10UL, 3, "2.1544346900318837217|59293566519")]
    [InlineData(10UL, 4, "1.7782794100389228012254211951926848|4473579052")]
    [InlineData(10UL, 5, "1.5848931924611134852021|0137339150")]
    [InlineData(10UL, 6, "1.46779926762206954092|0517114816")]
    public void NthRoot_ULongValues_ExpectedPrecision(ulong value, int root, string expectedStr)
    {
        var input = new BigFloat(value);
        var result = BigFloat.NthRoot(input, root);
        
        // Parse expected value, handling the pipe separator for precision indication
        var cleanExpected = expectedStr.Replace("|", "");
        var expected = new BigFloat(cleanExpected,0, 32);
        
        // Use ULP comparison for these calculated values
        Assert.True(result.EqualsUlp(expected, 2), 
            $"NthRoot({value}, {root}) = {result}, expected approximately {cleanExpected}");
    }

    #endregion

    #region Increment and Decrement Tests

    [Theory]
    [InlineData("2.00000000000", "3.00000000000")]
    [InlineData("1.00000000000", "2.00000000000")]
    [InlineData("0.00000000000", "1.00000000000")]
    [InlineData("-1.00000000000", "0.00000000000")]
    [InlineData("-2.00000000000", "-1.00000000000")]
    [InlineData("-9.00000000000", "-8.00000000000")]
    public void Increment_ReturnsExpectedValue(string initial, string expected)
    {
        var inputVal = new BigFloat(initial);
        var expectedVal = new BigFloat(expected);
        inputVal++;
        Assert.Equal(expectedVal, inputVal);
    }

    [Theory]
    [InlineData("2.00000000000", "1.00000000000")]
    [InlineData("1.00000000000", "0.00000000000")]
    [InlineData("0.00000000000", "-1.00000000000")]
    [InlineData("-1.00000000000", "-2.00000000000")]
    [InlineData("-8.00000000000", "-9.00000000000")]
    public void Decrement_ReturnsExpectedValue(string initial, string expected)
    {
        var inputVal = new BigFloat(initial);
        var expectedVal = new BigFloat(expected);
        inputVal--;
        Assert.Equal(expectedVal, inputVal);
    }

    #endregion

    #region Cast Tests

    [Fact]
    public void Cast_BigFloatToDouble()
    {
        double result = (double)new BigFloat(123);
        Assert.Equal(123, (int)result);

        for (double d = -2.345; d < 12.34; d = 0.1 + (d * 1.007))
        {
            result = (double)new BigFloat(d);
            Assert.Equal(d, result);
        }
    }

    [Fact]
    public void Cast_DoubleToBigFloat_LargeNumber()
    {
        var str = "179769000000000006323030492138942643493033036433685336215410983289126434148906289940615299632196609445533816320312774433484859900046491141051651091672734470972759941382582304802812882753059262973637182942535982636884444611376868582636745405553206881859340916340092953230149901406738427651121855107737424232448.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        
        var a = new BigFloat(str);
        var d = (BigFloat)(double)179769000000000006323030492138942643493033036433685336215410983289126434148906289940615299632196609445533816320312774433484859900046491141051651091672734470972759941382582304802812882753059262973637182942535982636884444611376868582636745405553206881859340916340092953230149901406738427651121855107737424232448.0;
        
        Assert.True(a.EqualsUlp(d));
    }

    #endregion

    #region Miscellaneous Tests

    [Fact]
    public void ToString_SmallValues_NeverRoundsToZero()
    {
        // Ensure that a "1" bit never rounds to zero in ToString or OutOfPrecision operations
        for (int i = 0; i > -1073; i--)
        {
            BigFloat value = new(1, i, addedBinaryPrecision: 0);
            
            // String->BigFloat should never be zero
            Assert.False(new BigFloat(value.ToString()).IsOutOfPrecision);
            
            // OutOfPrecision should never be zero for non-zero values
            Assert.False(value.IsOutOfPrecision);
        }
    }

    [Theory]
    [InlineData("0", -10, false, "0.000",true)]
    [InlineData("0", 0, false, "0", true)]
    [InlineData("0", 10, false, "0e+3", true)]
    [InlineData("0", -10, true, "0.000", true)] // 12.6
    [InlineData("0", 0, true, "0", true)]       // 9.6
    [InlineData("0", 10, true, "0e+3", true)]   // (10)/log2(10) -> 3 zeros
    [InlineData("0", 30, true, "0e+9", true)]   // (30)/log2(10) -> 9 zeros
    [InlineData("0", 32, true, "0e+10", true)]  // (32)/log2(10) -> 9-10 zeros
    [InlineData("0", 33, true, "0e+10", true)]  // (32)/log2(10) -> 10 zeros

    [InlineData("1111", -4, false, "69.4", false)]
    [InlineData("1111", 0, false, "1111", false)]
    [InlineData("1111", 4, false, "1.778e+4", false)]
    [InlineData("-1111", -4, false, "-69.4", false)]
    [InlineData("-1111", 0, false, "-1111", false)]
    [InlineData("-1111", 4, false, "-1.778e+4", false)]

    [InlineData("1111", -4, true, "0.0", true)]
    [InlineData("1111", 0, true, "0", true)]
    [InlineData("1111", 4, true, "0e+1", true)]
    [InlineData("-1111", -4, true, "0.0", true)]
    [InlineData("-1111", 0, true, "0", true)]
    [InlineData("-1111", 4, true, "0e+1", true)]
    public void OutOfPrecision_ZeroValues(string mantissaStr, int scale, bool includesGuardBits, string expectedToString, bool isOutOfPrecision)
    {
        var mantissa = BigInteger.Parse(mantissaStr);
        var value = new BigFloat(mantissa, scale, includesGuardBits);
        
        Assert.True(isOutOfPrecision == value.IsOutOfPrecision);
        var stringValue = value.ToString();
        Assert.Equal(expectedToString, stringValue);
    }

    [Theory]
    [InlineData("1111111111111111111111111111111111", 1, true, "5.1740143034193250868056e+23")]
    [InlineData("1111111111111111111111111111111111", 2, true, "1.03480286068386501736111e+24")]
    [InlineData("1111111111111111111111111111111111", -1, true, "129350357585483127170139")]
    [InlineData("111111111111", 0, false, "111111111111")]
    [InlineData("111111111111", -32, true, "0.0000000060")] // 10 digits go into the guard bits
    [InlineData("1111111", 4, false, "1.777778e+7")]
    [InlineData("1111", 0, false, "1111")]
    [InlineData("1111", 1, false, "2.22e+3")] // or 2.222e+3
    [InlineData("1111", 2, false, "4.44e+3")] // or 4.444e+3
    [InlineData("1111", 4, false, "1.778e+4")]
    [InlineData("1111", -1, false, "556")]
    [InlineData("1111", -4, false, "69.4")]  
    [InlineData("-1111", 0, false, "-1111")]
    [InlineData("-1111", 1, false, "-2.22e+3")]
    [InlineData("-1111", 4, false, "-1.778e+4")]
    [InlineData("-1111", -1, false, "-556")]
    [InlineData("-1111", -4, false, "-69.4")] 
    public void ToString_NonZeroValues(string mantissaStr, int scale, bool includesGuardBits, string expectedToString)
    {
        var mantissa = BigInteger.Parse(mantissaStr);
        var value = new BigFloat(mantissa, scale, includesGuardBits);
        string valueStr = value.ToString();
        Assert.Equal(expectedToString, valueStr);
    }

    #endregion
}
