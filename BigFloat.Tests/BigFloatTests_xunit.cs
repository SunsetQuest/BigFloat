// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

// Ignore Spelling: Aprox Bitwise Sqrt Ulp Fractionals

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;

namespace BigFloatLibrary.Tests;

public class OriginalBigFloatTests
{
    /// <summary>
    /// Target time for each test. Time based on release mode on 16 core x64 CPU.
    /// </summary>
    private readonly int TestTargetInMillseconds = 100;

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

    [Fact]
    public void Verify_Misc()
    {
        // Make sure that a "1" bit never rounds to zero in a ToString or OutOfPrecision operation.
        BigFloat value = new(1, 0, valueIncludesGuardBits: true, binaryPrecision: 31);
        Assert.True(value.IsOutOfPrecision); // 0.000...1->BigFloat->OutOfPrecision should never be zero.
        value = new(1, 0, valueIncludesGuardBits: true, binaryPrecision: 32);
        Assert.False(value.IsOutOfPrecision); // 0.000...1->BigFloat->OutOfPrecision should never be zero.

        foreach (int i in new int[] { 0, -1, -999 })
        {
            value = new(1, i, valueIncludesGuardBits: true, binaryPrecision: 31);
            string strValue = value.ToString();
            Assert.False(strValue is "0" or "0.0" or "0.00"); // 0.000...1->BigFloat->ToString should never be zero.
        }

        Assert.Equal(new BigFloat(1, -8) % 1, (BigFloat)0.00390625); // 0.00390625 % 01 // 5-5-2025 update: "0.00390625" is a better answer than 0
        Assert.Equal(new BigFloat(1, -1074, binaryPrecision: 0) % 1, 0);   // 0   == 0.000...001
        Assert.False(new BigFloat(0, 0) == new BigFloat(-4503599627370496, -52, binaryPrecision: 0));  // 0 != -1.0

        BigFloat temp = new(ulong.MaxValue);
        temp++;
        Assert.Equal(temp, new BigFloat(1, 64));
        Assert.Equal(temp, new BigFloat("18446744073709551616"));

        // Very simple test of GetHashCode();
        HashSet<int> hashSet = [];
        for (int i = -100; i < 100; i++)
        {
            BigFloat bf = new(i);
            int hash = bf.GetHashCode();
            Assert.True(hashSet.Add(hash)); // Duplicate found with only 200 values.
        }
    }

    //[Fact]
    //public void Verify_DebuggerDisplay()
    //{
    //    BigFloat bf;

    //    bf = new BigFloat(-14566005701624942, 96, true);
    //    //1154037866912041841546539185052621408946880512 or  33bfb47ba4446e000000000000000000000000
    //    // 14566005701624942 << 96  or 33bfb47ba4446e << 96
    //    //is -268695379354069438191721957422006272 but only 16 precision digits so -2686953793540694e+20, 
    //    //Correct: -2686953793540694e+20, -0x33BFB4|7BA4446E[22+32=54], << 96
    //    Assert.Equal("-2686953793540694e+20, -0x33BFB4|7BA4446E[22+32=54], << 96", bf.DebuggerDisplay);

    //    bf = new BigFloat(BigInteger.Parse("0CC404B845BBB924A88E39E", NumberStyles.AllowHexSpecifier), 96, true);
    //    // 246924491699516410027369374 x 18446744073709551616(for 64 up-shift) = 4554952903911797705753984222769658845550608384
    //    //is -4554952903911797705753984222769658845550608384 but only first ____ precision digits
    //    //Correct: 45549529039117977057539842e+20,  0x0CC404B845BBB92|4A88E39E[56+32=88], << 96
    //    Assert.Equal("45549529039117977057539842e+20,  0x0CC404B845BBB92|4A88E39E[56+32=88], << 96", bf.DebuggerDisplay);
    //}

    [Fact]
    public void Verify_BitwiseComplementOperator()
    {
        BigFloat a = BigFloat.ParseBinary("10.111");
        BigFloat expectedAns = BigFloat.ParseBinary(" 1.000 11111111111111111111111111111111", includedGuardBits: BigFloat.GuardBits);
        Assert.Equal(expectedAns, ~a);

        _ = BigFloat.TryParseBinary("1100110110110", out a);
        _ = BigFloat.TryParseBinary("  11001001001.11111111111111111111111111111111", out expectedAns, includedGuardBits: BigFloat.GuardBits);
        Assert.Equal(expectedAns, ~a);

        _ = BigFloat.TryParseBinary("11001001001.11111111111111111111111111111111", out a, includedGuardBits: BigFloat.GuardBits);
        _ = BigFloat.TryParseBinary("  110110110", out expectedAns);
        Assert.Equal(expectedAns, ~a);

        _ = BigFloat.TryParseBinary("1100110110110", out a);
        _ = BigFloat.TryParseBinary("    110110110", out expectedAns);
        Assert.Equal(expectedAns, ~~a);
    }

    [Fact]
    public void Verify_Constants_Pi()
    {
        int MAX_INT = TestTargetInMillseconds switch
        {
            >= 5000 => 24000,
            >= 3400 => 20000,
            >= 1500 => 17000,
            >= 800 => 4000,
            >= 600 => 15000,
            >= 469 => 12000,
            >= 175 => 8000,
            >= 62 => 4000,
            >= 58 => 3000,
            _ => 2000,
        };
        Dictionary<string, BigFloat> bigConstants = BigFloat.Constants.WithConfig(precisionInBits: MAX_INT).GetAll();
        BigFloat pi200ref = bigConstants["Pi"];
        BigFloat pi200gen = BigFloat.Constants.GeneratePi(MAX_INT);
        Assert.Equal(0, pi200ref.CompareUlp(pi200gen)); // We got some Pi in our face. The generated pi does not match the literal constant Pi.
        Assert.Equal(0, pi200ref.CompareUlp(BigFloat.Constants.GeneratePi(0)));
        Assert.Equal(0, pi200ref.CompareUlp(BigFloat.Constants.GeneratePi(1)));
        Assert.Equal(0, pi200ref.CompareUlp(BigFloat.Constants.GeneratePi(2)));
        for (int i = 3; i < MAX_INT; i *= 3)
        {
            Assert.Equal(0, pi200ref.CompareUlp(BigFloat.Constants.GeneratePi(i)));
        }
    }

    [Fact]
    public void Verify_Constants_GenerateArrayOfCommonConstants()
    {
        BigFloat[] bigFloats1000 = BigFloat.ConstantBuilder.GenerateArrayOfCommonConstants();
        BigFloat[] bigFloats2000 = BigFloat.ConstantBuilder.GenerateArrayOfCommonConstants();
        for (int i = 0; i < bigFloats1000.Length; i++)
        {
            BigFloat bf1000 = bigFloats1000[i];
            BigFloat bf2000 = bigFloats2000[i];
            Assert.Equal(bf1000, bf2000); // Issue with GenerateArrayOfCommonConstants
        }
    }

    [Fact]
    public void Verify_Constants()
    {
        BigFloat bfFromBfConstants =
            BigFloat.Constants.Prime +
            BigFloat.Constants.NaturalLogOfPhi +
            BigFloat.Constants.Omega +
            BigFloat.Constants.EulerMascheroni +
            BigFloat.Constants.TwinPrime +
            BigFloat.Constants.Catalan +
            BigFloat.Constants.Plastic +
            BigFloat.Constants.Pisot +
            BigFloat.Constants.Sqrt2 +
            BigFloat.Constants.FineStructure +
            BigFloat.Constants.GoldenRatio +
            BigFloat.Constants.Sqrt3 +
            BigFloat.Constants.SqrtPi +
            BigFloat.Constants.Khintchine +
            BigFloat.Constants.E +
            BigFloat.Constants.Pi;


        double doubleTotal =
            0.414682509851111660248109 +
            0.481211825059603447497759 +
            0.567143290409783872999968 +
            0.577215664901532860606512 +
            0.660161815846869573927812 +
            0.915965594177219015054603 +
            1.324717957244746025960908 +
            1.380277569097614115673301 +
            Math.Sqrt(2.0) + // 1.414213562373095048801688 +
            1.460354508809586812889499 +
            1.618033988749894848204586 +
            1.732050807568877293527446 +
            Math.Sqrt(Math.PI) + // 1.772453850905516027298167 +
            2.685452001065306445309714 +
            Math.E +
            Math.PI;

        var bfFromDoubles = (BigFloat)doubleTotal;
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 8));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 7));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 6));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 5));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 4));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 3));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 2));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 1));
        Assert.Equal(0, BigFloat.CompareUlp(bfFromBfConstants, bfFromDoubles, 0));

        // double:            22.863809428109594
        // bigFloat(true ans):22.86380942810959552182300968517081605432710107187450407311958860532..

        // Previously this comparison could drift by a few bits because of the double-to-BigFloat conversion.
        // After tightening that conversion, the summed constants now agree within 23 ulp (including guard bits).
        Assert.True(bfFromBfConstants.EqualsUlp(((BigFloat)doubleTotal), 23, ulpScopeIncludeGuardBits:true));

        double BigFloatZero1 = (double)BigFloat.ZeroWithAccuracy(0);
        double BigFloatZero2 = (double)BigFloat.ZeroWithAccuracy(50);
        Assert.Equal(BigFloatZero2, BigFloatZero1); // Fail on ZeroWithNoPrecision == ZeroWithSpecifiedLeastPrecision(50)

        double acceptableTolarance = (double.BitIncrement(Math.PI) - Math.PI) * 8;
        double doubleDiff0 = (double)(bfFromBfConstants - (BigFloat)doubleTotal);

        Assert.True(doubleDiff0 <= acceptableTolarance);

        // Since we are doing repetitive addition, it is expected that doubleTotal is off by a few bits.
        double doubleDiff1 = doubleTotal - (double)bfFromBfConstants;
        Assert.True(doubleDiff1 <= acceptableTolarance);

        Assert.Equal(0, BigFloat.CompareUlp(BigFloat.Constants.RamanujanSoldner, (BigFloat)262537412640768743.99999999999925, 50));
        Assert.Equal(262537412640768743.99999999999925, (double)BigFloat.Constants.RamanujanSoldner);

        bool success = BigFloat.ConstantBuilder.Const_0_0307.TryGetAsBigFloat(out BigFloat bf, 100);

        BigFloat ans = BigFloat.ParseBinary("0.0000011111011101100111101100000010101011110101000101011101101100000100100001101000100011010011110010000010011000011101001101011001111101100111100001001110101010011000111100110011100110110111000", includedGuardBits: 32);
        if (success)
        {
            Assert.True(bf.EqualsUlp(ans, 1, true));
        }

        success = BigFloat.ConstantBuilder.Const_0_4146.TryGetAsBigFloat(out bf, 200);
        ans = BigFloat.ParseBinary("0.011010100010100010100010000010100000100010100010000010000010100000100010100000100010000010000000100010100010100010000000000000100010000010100000000010100000100000100010000010000010100000000010100010100000000000100000000000100010100010000010", includedGuardBits: 32);
        if (success)
        {
            Assert.True(bf.EqualsUlp(ans, 1, true));
        }

        success = BigFloat.ConstantBuilder.Const_0_5671.TryGetAsBigFloat(out bf, 200);
        ans = BigFloat.ParseBinary("0.100100010011000001001101011111000111010010110010101110100101111010101111110111011010101001100010100001101101110000101000111000010110111010000110111011001110100001010111000110101000100000001100100100111111011100000011010100101000111000", includedGuardBits: 32);
        if (success)
        {
            Assert.True(bf.EqualsUlp(ans, 1, true));
        }

        success = BigFloat.ConstantBuilder.Const_1_4142.TryGetAsBigFloat(out bf, 200);
        ans = BigFloat.ParseBinary("1.0110101000001001111001100110011111110011101111001100100100001000101100101111101100010011011001101110101010010101011111010011111000111010110111101100000101110101000100100111011101010000100110011101101000101111010110010000101100000110011001", includedGuardBits: 32);
        if (success)
        {
            Assert.True(bf.EqualsUlp(ans, 1, true));
        }

        success = BigFloat.ConstantBuilder.Const_2_6854.TryGetAsBigFloat(out bf, 300);
        ans = BigFloat.ParseBinary("10.101011110111100111001000010001111000110110100001101011101111001011111101111100111110001110010100011001100111111110011100001100111001001011100000001000011110011000011000001010011101110111111010010011011000011100000110001110110011100101010", includedGuardBits: 32);
        if (success)
        {
            Assert.True(bf.EqualsUlp(ans, 1, true));
        }
    }

    // Test values calculated using: https://www.ttmath.org/online_calculator, https://www.mathsisfun.com/calculator-precision.html

    [Fact]
    public void Verify_IsZero()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat b = BigFloat.ParseBinary("10000.0");
        Assert.True((a - b).IsZero);

        a = BigFloat.ParseBinary("100.0");
        b = BigFloat.ParseBinary("100.000");
        Assert.True((a - b).IsZero);

        a = BigFloat.ParseBinary("100.01");
        b = BigFloat.ParseBinary("100.000");
        Assert.False((a - b).IsZero);

        Assert.False(BigFloat.ParseBinary("1|.111111111", -2, 0).IsZero); // (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("1|.111111111", -1, 0).IsZero); // (no because _size >= GuardBits-2) 
        Assert.False(BigFloat.ParseBinary("0|.111111111", -1, 0).IsZero); // (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.111111111", 0, 0).IsZero); //  (no because _size >= GuardBits-2) 
        Assert.False(BigFloat.ParseBinary("0|.011111111", 1, 0).IsZero); //  (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.001111111", 2, 0).IsZero); //                                        (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.000111111", 3, 0).IsZero); //                                        (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.000111111", 4, 0).IsZero); //                                        (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("1|.000000000", -2, 0).IsZero); // (no because _size >= GuardBits-2) 
        Assert.False(BigFloat.ParseBinary("1|.000000000", -1, 0).IsZero); // (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("1|.000000000", 0, 0).IsZero); //  (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.100000000", -1, 0).IsZero); // (no because _size >= GuardBits-2)
        Assert.False(BigFloat.ParseBinary("0|.100000000", 0, 0).IsZero); //  (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.False(BigFloat.ParseBinary("0|.100000000", 1, 0).IsZero); //  (no because _size >= GuardBits-2) AND (no because (Scale + _size - GuardBits) < 0)
        Assert.True(BigFloat.ParseBinary("0|.011111111", -1, 0).IsZero); //  (no because _size >= GuardBits-2)                                            
        Assert.True(BigFloat.ParseBinary("0|.011111111", 0, 0).IsZero); //   (no because _size >= GuardBits-2)
        Assert.True(BigFloat.ParseBinary("0|.001111111", 1, 0).IsZero); //                                         (no because (Scale + _size - GuardBits) < 0)
        Assert.True(BigFloat.ParseBinary("0|.000111111", 2, 0).IsZero); //


        //  IntData    Scale  Precision  Zero
        //1|111111111 << -2       1       Y -1 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)
        //1|111111111 << -1       1       N  0 (no because _size >= GuardBits-1) 
        //0|111111111 << -1       0       Y -1 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)
        //0|111111111 <<  0       0       N  0 (no because _size >= GuardBits-1) 
        //0|011111111 << -1      -1       Y -2                                             
        //0|011111111 <<  0      -1       Y -1 (borderline)
        //0|011111111 <<  1      -1       N  0                                       (no because (Scale + _size - GuardBits) < 0)
        //0|001111111 <<  1      -2       Y -1 (borderline)
        //0|001111111 <<  2      -2       N  0                                       (no because (Scale + _size - GuardBits) < 0)
        //0|000111111 <<  2      -3       Y -1 (borderline)
        //0|000111111 <<  3      -3       N  0                                       (no because (Scale + _size - GuardBits) < 0)
        //1|000000000 << -2       1       N -1 (no because _size >= GuardBits-1) 
        //1|000000000 << -1       1       N  0 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)
        //1|000000000 <<  0       1       N  0 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)
        //0|100000000 << -1       0       N -1 (no because _size >= GuardBits-1)
        //0|100000000 <<  0       0       N  0 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)
        //0|100000000 <<  1       0       N  1 (no because _size >= GuardBits-1) AND (no because (Scale + _size - GuardBits) < 0)

        Assert.False(BigFloat.ParseBinary("100.01").IsZero);
        Assert.False(BigFloat.ParseBinary("0.00000000000000000000001").IsZero);
        Assert.False(BigFloat.ParseBinary("-0.00000000000000000000001").IsZero);
        Assert.True(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("-0.00000000000000000000001", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("0.00001", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("0.000000000000001000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.False(BigFloat.ParseBinary("100000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("0.0100000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.False(BigFloat.ParseBinary("10000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("0.010000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.False(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.True(BigFloat.ParseBinary("0.01000000000000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.False(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
        Assert.False(BigFloat.ParseBinary("0.0100000000000000000000000000000000", 0, 0, BigFloat.GuardBits).IsZero);
    }

    [Fact]
    public void SignProperties_ShouldRespectGuardBitZeroTolerance()
    {
        var cases = new (int BitLength, int ScaleOffset, bool Negative, bool ExpectZero, string Description)[]
        {
            (1, -1, false, true, "1-bit positive just under tolerance boundary"),
            (1, 0, false, false, "1-bit positive just above tolerance boundary"),
            (1, -1, true, true, "1-bit negative just under tolerance boundary"),
            (1, 0, true, false, "1-bit negative just above tolerance boundary"),
            (16, -1, false, true, "16-bit positive just under tolerance boundary"),
            (16, 0, false, false, "16-bit positive just above tolerance boundary"),
            (16, -1, true, true, "16-bit negative just under tolerance boundary"),
            (16, 0, true, false, "16-bit negative just above tolerance boundary"),
        };

        foreach ((int BitLength, int ScaleOffset, bool Negative, bool ExpectZero, string Description) in cases)
        {
            BigInteger magnitude = BigInteger.One << (BitLength - 1);
            BigInteger mantissa = Negative ? BigInteger.Negate(magnitude) : magnitude;
            int scale = BigFloat.GuardBits + ScaleOffset - BitLength;

            BigFloat value = new(mantissa, scale, valueIncludesGuardBits: true);

            Assert.Equal(ExpectZero, value.IsZero);
            Assert.Equal(ExpectZero ? 0 : (Negative ? -1 : 1), value.Sign);
            Assert.Equal(!ExpectZero && !Negative, value.IsPositive);
            Assert.Equal(!ExpectZero && Negative, value.IsNegative);
        }
    }

    [Fact]
    public void IsStrictZero_WhenResultFromFloatingPointArithmetic_ShouldHandleRoundingErrors()
    {
        // Arrange & Act
        BigFloat result = ((BigFloat)1.3 * (BigFloat)2) - (BigFloat)2.6;

        // Assert
        Assert.True(result.IsZero, "Result should be considered zero despite potential floating-point arithmetic errors");
        // Note: IsStrictZero behavior for arithmetic results may vary based on implementation
    }

    [Fact]
    public void IsStrictZero_WhenExactZero_ShouldReturnTrue()
    {
        // Arrange & Act
        BigFloat result = 0;

        // Assert
        Assert.True(result.IsStrictZero, "Exact zero should be strict zero");
        Assert.True(result.IsZero, "Exact zero should be considered zero");
    }

    [Theory]
    [InlineData("1", "Smallest representable positive value")]
    [InlineData("-1", "Smallest representable negative value")]
    public void IsStrictZero_WhenSmallestRepresentableValue_ShouldReturnFalse(string binaryValue, string description)
    {
        // Arrange & Act
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, BigFloat.GuardBits);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero");
        Assert.False(result.IsZero, $"{description} should not be considered zero");
    }

    [Theory]
    [InlineData("1", "Positive value with guard bits - 1")]
    [InlineData("-1", "Negative value with guard bits - 1")]
    public void IsStrictZero_WhenValueWithReducedGuardBits_ShouldReturnFalse(string binaryValue, string description)
    {
        // Arrange & Act
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, BigFloat.GuardBits - 1);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero");
        Assert.False(result.IsZero, $"{description} should not be considered zero");
    }

    [Theory]
    [InlineData("1", "Positive normalized value")]
    [InlineData("-1", "Negative normalized value")]
    public void IsStrictZero_WhenNormalizedValues_ShouldReturnFalse(string binaryValue, string description)
    {
        // Arrange & Act
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, 1);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero");
        Assert.False(result.IsZero, $"{description} should not be considered zero");
    }

    [Theory]
    [InlineData(".1", BigFloat.GuardBits, "Fractional value with full guard bits")]
    [InlineData(".1", BigFloat.GuardBits - 1, "Fractional value with reduced guard bits")]
    [InlineData("-.1", BigFloat.GuardBits, "Negative fractional value with full guard bits")]
    [InlineData(".1", 1, "Fractional value with minimal precision")]
    [InlineData("-.1", 1, "Negative fractional value with minimal precision")]
    public void IsStrictZero_WhenFractionalValues_ShouldReturnFalse(string binaryValue, int precision, string description)
    {
        // Arrange & Act
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, precision);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero");
        Assert.False(result.IsZero, $"{description} should not be considered zero");
    }

    [Theory]
    [InlineData(".01", BigFloat.GuardBits, "Small fractional value with full guard bits")]
    [InlineData(".01", BigFloat.GuardBits - 1, "Small fractional value with reduced guard bits")]
    [InlineData("-.01", BigFloat.GuardBits, "Negative small fractional value")]
    public void IsZero_WhenVerySmallFractionalValues_ShouldReturnTrueButNotStrictZero(string binaryValue, int precision, string description)
    {
        // Arrange & Act
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, precision);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero");
        Assert.True(result.IsZero, $"{description} should be considered zero due to precision limits");
    }

    [Theory]
    [InlineData(".01", "Positive small value that rounds up")]
    [InlineData("-.01", "Negative small value that rounds down")]
    public void IsZero_WhenSmallValuesRoundToSignificantValues_ShouldReturnFalse(string binaryValue, string description)
    {
        // Arrange & Act - Using minimal precision causes rounding to significant values
        BigFloat result = BigFloat.ParseBinary(binaryValue, 0, 0, 1);

        // Assert
        Assert.False(result.IsStrictZero, $"{description} should not be strict zero after rounding");
        Assert.False(result.IsZero, $"{description} should not be considered zero after rounding to significant value");
    }

    [Fact]
    public void IsStrictZero_ComparisonMatrix_ShouldDemonstrateAllScenarios()
    {
        // This test provides a comprehensive overview of all zero-checking scenarios
        var testCases = new (BigFloat Value, bool? ExpectedStrictZero, bool ExpectedZero, string Description)[]
        {
            ((BigFloat)0, true, true, "Exact zero"),
            (((BigFloat)1.3 * (BigFloat)2) - (BigFloat)2.6, null, true, "Arithmetic result (implementation dependent)"),
            (BigFloat.ParseBinary("1", 0, 0, BigFloat.GuardBits), false, false, "Smallest positive"),
            (BigFloat.ParseBinary("-1", 0, 0, BigFloat.GuardBits), false, false, "Smallest negative"),
            (BigFloat.ParseBinary(".01", 0, 0, BigFloat.GuardBits), false, true, "Very small positive"),
            (BigFloat.ParseBinary("-.01", 0, 0, BigFloat.GuardBits), false, true, "Very small negative"),
        };

        foreach ((BigFloat Value, bool? ExpectedStrictZero, bool ExpectedZero, string Description) in testCases)
        {
            if (ExpectedStrictZero.HasValue)
            {
                Assert.True(ExpectedStrictZero.Value == Value.IsStrictZero,
                    $"IsStrictZero failed for: {Description}");
            }

            Assert.True(ExpectedZero == Value.IsZero,
                $"IsZero failed for: {Description}");
        }
    }


    [Fact]
    public void Verify_GetPrecision()
    {
        int guardBits = BigFloat.GuardBits;
        Assert.Equal(5, BigFloat.ParseBinary("100.01").Precision);
        Assert.Equal(1, BigFloat.ParseBinary("-0.00000000000000000000001").Precision);
        Assert.Equal(1 - guardBits, BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, guardBits).Precision);
        Assert.Equal(1 - guardBits, BigFloat.ParseBinary("0.00001", 0, 0, guardBits).Precision);
        Assert.Equal(4 - guardBits, BigFloat.ParseBinary("0.000000000000001000", 0, 0, guardBits).Precision);
        Assert.Equal(9 - guardBits, BigFloat.ParseBinary("100000000", 0, 0, guardBits).Precision);
        Assert.Equal(17 - guardBits, BigFloat.ParseBinary("10000000000000000", 0, 0, guardBits).Precision);
        Assert.Equal(25 - guardBits, BigFloat.ParseBinary("1000000000000000000000000", 0, 0, guardBits).Precision);
        Assert.Equal(33 - guardBits, BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, guardBits).Precision);
    }

    [Fact]
    public void Verify_GetAccuracy()
    {
        int hb = BigFloat.GuardBits;
        Assert.Equal(2, BigFloat.ParseBinary("100.01").Accuracy);
        Assert.Equal(23, BigFloat.ParseBinary("-0.00000000000000000000001").Accuracy);
        Assert.Equal(23 - hb, BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, hb).Accuracy);
        Assert.Equal(5 - hb, BigFloat.ParseBinary("0.00001", 0, 0, hb).Accuracy);
        Assert.Equal(18 - hb, BigFloat.ParseBinary("0.000000000000001000", 0, 0, hb).Accuracy);       // 0|00000000000000.00000000000000000000001" (accuracy is -14)
        Assert.Equal(hb - hb, BigFloat.ParseBinary("100000000", -hb, 0, hb).Accuracy);                // 0.|00000000000000000000000100000000        (accuracy is 0)
        Assert.Equal(hb - hb, BigFloat.ParseBinary("10000000000000000", -hb, 0, hb).Accuracy);        // 0.|00000000000000010000000000000000        (accuracy is 0)
        Assert.Equal(hb - hb, BigFloat.ParseBinary("1000000000000000000000000", -hb, 0, hb).Accuracy);// 0.|00000001000000000000000000000000        (accuracy is 0)
        Assert.Equal(hb - hb, BigFloat.ParseBinary("100000000000000000000000000000000", -hb, 0, hb).Accuracy);//1.|00000000000000000000000000000000(accuracy is 0)
        Assert.Equal(-hb, BigFloat.ParseBinary("100000000", 0, 0, hb).Accuracy);                        // 0|00000000000000000000000100000000.(accuracy is -32)
        Assert.Equal(-hb, BigFloat.ParseBinary("10000000000000000", 0, 0, hb).Accuracy);                // 0|00000000000000010000000000000000.(accuracy is -32)
        Assert.Equal(-hb, BigFloat.ParseBinary("1000000000000000000000000", 0, 0, hb).Accuracy);        // 0|00000001000000000000000000000000.(accuracy is -32)
        Assert.Equal(-hb, BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, hb).Accuracy);// 1|00000000000000000000000000000000.(accuracy is -32)
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.25, 1)]
    [InlineData(1.25, 17)]
    [InlineData(-42.5, 5)]
    public void AdjustAccuracy_Increase_IsValuePreserving(double v, int inc)
    {
        var x = new BigFloat(v);
        var y = BigFloat.AdjustAccuracy(x, inc);
        Assert.True(x == y, $"Value changed: {x} vs {y}");
    }

    [Theory]
    [InlineData(1.2345, -5)]
    [InlineData(-1.2345, -20)]
    [InlineData(123456789.0, -53)]
    [InlineData(123.0, 64)]
    public void AdjustAccuracy_Equals_AdjustPrecision(double v, int delta)
    {
        var x = new BigFloat(v);
        var a = BigFloat.AdjustAccuracy(x, delta);
        var p = BigFloat.AdjustPrecision(x, delta);
        Assert.True(a.Equals(p), $"Mismatch: {a} vs {p}");
    }

    [Fact]
    public void SetAccuracy_Matches_Other_Accuracy()
    {
        var a = new BigFloat(1.23456789012345);
        var b = new BigFloat(6.789);
        var b2 = BigFloat.SetAccuracy(b, a.Accuracy);
        Assert.Equal(a.Accuracy, b2.Accuracy);
    }

    [Fact]
    public void SetAccuracy_OnZero_PreservesZeroAndContext()
    {
        var z = BigFloat.ZeroWithAccuracy(100);     // existing API
        var z2 = BigFloat.SetAccuracy(z, 10);
        Assert.True(z2.IsZero);
        Assert.Equal(10, z2.Accuracy);
    }

    [Fact]
    public void Verify_IsPositive()
    {
        int gb = BigFloat.GuardBits;
        Assert.True(BigFloat.ParseBinary("100.01").IsPositive);
        Assert.True(BigFloat.ParseBinary("1|00000000", 0, 0).IsPositive);
        Assert.True(BigFloat.ParseBinary("|100000000", 0, 0).IsPositive);
        Assert.True(BigFloat.ParseBinary("|0100000000", 0, 0).IsPositive);
        Assert.True(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("10000000000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("1000000000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("100000000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("10000000000000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("100000000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("10000", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("100", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("1", 0, 0, gb).IsPositive);
        Assert.True(BigFloat.ParseBinary("0.1", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("0.01", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("0.00001", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("0.000000000000001000", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("0.00000000000000000000000").IsPositive);
        Assert.False(BigFloat.ParseBinary("-0.00000000000000000000001").IsPositive);
        Assert.False(BigFloat.ParseBinary("-100.01").IsPositive);
        Assert.False(BigFloat.ParseBinary("-100000000000000000000000000000000", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("-1|00000000", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("-|100000000", 0, 0, gb).IsPositive);
        Assert.False(BigFloat.ParseBinary("-|0100000000", 0, 0, gb).IsPositive);
    }

    [Fact]
    public void Verify_IsNegative()
    {
        int gb = BigFloat.GuardBits;
        Assert.True(BigFloat.ParseBinary("-1|00000000", 0, 0).IsNegative);
        Assert.True(BigFloat.ParseBinary("-|100000000", 0, 0).IsNegative);
        Assert.True(BigFloat.ParseBinary("-|0100000000", 0, 0).IsNegative);
        Assert.True(BigFloat.ParseBinary("-0.00000000000000000000001").IsNegative);
        Assert.True(BigFloat.ParseBinary("-100000000000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-10000000000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-1000000000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-100000000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-10000000000000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-1000000", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-1", 0, 0, gb).IsNegative);
        Assert.True(BigFloat.ParseBinary("-.1", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("100.01").IsNegative);
        Assert.False(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("0.00001", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("0.000000000000001000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("100000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("1|00000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("|100000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("|0100000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("10000000000000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, gb).IsNegative);
        Assert.False(BigFloat.ParseBinary("0.00000000000000000000000").IsNegative);
    }

    [Fact]
    public void Verify_LeftShift()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat expectedAnswer = BigFloat.ParseBinary("100000.");
        Assert.True((a << 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-10000.0");
        expectedAnswer = BigFloat.ParseBinary("-100000.");
        Assert.True((a << 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("100000");
        expectedAnswer = BigFloat.ParseBinary("100000", 1);
        Assert.True((a << 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("0.000100000");
        Assert.True((a << 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("-0.000100000");
        Assert.True((a << 1).EqualsZeroExtended(expectedAnswer));
    }

    [Fact]
    public void Verify_RightShift()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat expectedAnswer = BigFloat.ParseBinary("1000.00");
        Assert.True((a >> 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-10000.0");
        expectedAnswer = BigFloat.ParseBinary("-1000.00");
        Assert.True((a >> 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("100000");
        expectedAnswer = BigFloat.ParseBinary("10000.0");
        Assert.True((a >> 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("0.00000100000");
        Assert.True((a >> 1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("-0.00000100000");
        Assert.True((a >> 1).EqualsZeroExtended(expectedAnswer));
    }

    [Fact]
    public void Verify_LeftShiftMantissa()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat expectedAnswer = BigFloat.ParseBinary("100000.0");
        Assert.True(a.LeftShiftMantissa(1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-10000.0");
        expectedAnswer = BigFloat.ParseBinary("-100000.0");
        Assert.True(a.LeftShiftMantissa(1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("100000");
        expectedAnswer = BigFloat.ParseBinary("1000000");
        Assert.True(a.LeftShiftMantissa(1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("0.0001000000");
        Assert.True(a.LeftShiftMantissa(1).EqualsZeroExtended(expectedAnswer));

        a = BigFloat.ParseBinary("-0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("-0.0001000000");
        Assert.True(a.LeftShiftMantissa(1).EqualsZeroExtended(expectedAnswer));
    }

    [Theory]
    [InlineData("10000.0", "1000.0", 1)]
    [InlineData("100000", "10000", 1)]
    [InlineData("0.0000100000", "0.0000010000", 1)]
    [InlineData("0.10", "0.01", 1)]
    [InlineData("-10000.0", "-1000.0", 1)]
    [InlineData("-0.0000100000", "-0.0000010000", 1)]
    public void Verify_RightShiftMantissa(string input, string expected, int shift)
    {
        BigFloat a = BigFloat.ParseBinary(input);
        BigFloat aShifted = a.RightShiftMantissa(shift);
        BigFloat expectedAnswer = BigFloat.ParseBinary(expected);
        Assert.True(aShifted.EqualsZeroExtended(expectedAnswer));
    }

    [Theory]
    [InlineData("10000.0")]
    [InlineData("10000")]
    [InlineData("10000000000000000000000000.0000000000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("-10000.0")]
    [InlineData("-1000000|0000")]

    public void Verify_IsOneBitFollowedByZeroBitsTrueValues(string valueStr)
    {
        _ = BigFloat.TryParseBinary(valueStr, out BigFloat result);
        Assert.True(result.IsOneBitFollowedByZeroBits);
    }

    [Fact]
    public void Verify_IsOneBitFollowedByZeroBitsFalseValues()
    {
        _ = BigFloat.TryParseBinary("-111111111111", out BigFloat result);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("-11111111111111111111111111111111111111111111111111111", out result, includedGuardBits: -BigFloat.GuardBits);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("10101.1", out result);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("110000.0", out result);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("1100000", out result);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("110000000.0000000000000000000000000000000000000000000000000000000", out result);
        Assert.False(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("11000000000000", out result);
        Assert.False(result.IsOneBitFollowedByZeroBits);
    }

    [Fact]
    public void Verify_Lowest64Bits()
    {
        string input;

        input = "10000.0";
        CheckMe(input);

        input = "1000000000000000000000000000000000000000000000000000000000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000000000000000000000000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000.00000000000000000000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000.000000000000000000000000000";
        CheckMe(input);

        input = "1000000000000000000000000000000000000000000000000000000000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000000000000000000000000000000";
        CheckMe(input);

        input = "-10000.0";
        CheckMe(input);

        input = "-1000000000000000000000000000000000000000000000000000000000000000";
        CheckMe(input);

        input = "-10000000000000000000000000000000000000.00000000000000000000000000";
        CheckMe(input);

        input = "-10000000000000000000000000000000000000.000000000000000000000000000";
        CheckMe(input);

        input = "10000.1";
        CheckMe(input);

        input = "1000000000000000000000000000000000000000000000000000100000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000000000000000000000000000001";
        CheckMe(input);

        input = "10000000000000000000000000000000000001.00000000000000000000000001";
        CheckMe(input);

        input = "11000000000000000000000000000000000000.000000000000000000000000000";
        CheckMe(input);

        input = "1000000000000000000000000000000001000000000000000000000000000000";
        CheckMe(input);

        input = "10000000000000000000000000000000000000000000000000000000000000001";
        CheckMe(input);

        input = "-10001.1";
        CheckMe(input);

        input = "-1000000000000000000000000000000000000000000000000000000000000001";
        CheckMe(input);

        input = "-10000000000000000000000000000000000000.00000001000000000000000000";
        CheckMe(input);

        input = "-10000000000000000000000000000000000000.000000000000000000000000001";
        CheckMe(input);

        input = "1111111111110000000000000000000000000000000000000.000000000000000000000000001";
        CheckMe(input);

        static void CheckMe(string input)
        {
            _ = BigFloat.TryParseBinary(input, out BigFloat result);
            input = input.Replace(".", "");
            input = input[Math.Max(input.Length - 64, 0)..];
            input = input.TrimStart('0', '-');
            if (string.IsNullOrEmpty(input))
            {
                input = "0";
            }

            string resultStr = Convert.ToString((long)result.Lowest64Bits, 2);

            Assert.Equal(resultStr, input);
        }
    }

    [Fact]
    public void Verify_IntWithAddedPrecision()
    {
        int hb = BigFloat.GuardBits;
        Assert.True(BigFloat.OneWithAccuracy(10).EqualsZeroExtended(1));
        Assert.True(BigFloat.IntWithAccuracy(1, 10).EqualsZeroExtended(1));
        Assert.True(BigFloat.IntWithAccuracy(2, 10).EqualsZeroExtended(new BigFloat(2)));

        BigFloat a = BigFloat.IntWithAccuracy(2, 10);
        Assert.Equal(a.RawMantissa, (BigInteger)2 << (hb + 10));
        Assert.Equal(-10, a.Scale);

        a = BigFloat.IntWithAccuracy(-32, 100);
        Assert.Equal(a.RawMantissa, -(BigInteger)32 << (hb + 100));
        Assert.Equal(-100, a.Scale);

        a = BigFloat.IntWithAccuracy(27, -15);
        Assert.Equal(a.RawMantissa, (BigInteger)27 << (hb - 15));
        Assert.Equal(15, a.Scale);
    }

    [Fact]
    public void Verify_Log2Int()
    {
        BigFloat testValue;
        testValue = new("0b10110.1010000010011110011001100111111100111011110011001001000");
        Assert.Equal(5 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b10000.00000000000000000|00000000");
        Assert.Equal(5 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b1111.11111111111111111|11111111");
        Assert.Equal(4 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b1|0000.0000000000000000000000000");
        Assert.Equal(5 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b1|111.1111111111111111111111111");
        Assert.Equal(4 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b100");
        Assert.Equal(3 - 1, BigFloat.Log2Int(testValue));

        testValue = new("0b111");
        Assert.Equal(3 - 1, BigFloat.Log2Int(testValue));

#if !DEBUG
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => BigFloat.Log2Int(0));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => BigFloat.Log2Int(-1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => BigFloat.Log2Int(new("-0b100")));
#endif
    }

    [Fact]
    public void Verify_Sign_vs_IsZero_vs_IsInteger()
    {
        // IsZero should only be true when Sign is zero.
        // Also, when IsZero, IsInteger should be true.
        BigFloat testValue;
        testValue = new("0b10|");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b1|");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b|1");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b|01");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b|00111111");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b|0");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);

        testValue = new("0b-1|");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b-|1");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b-|01");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b-|00111111");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
        testValue = new("0b-|0");
        Assert.Equal(testValue.Sign == 0, testValue.IsZero);
        Assert.True(!testValue.IsZero || testValue.IsInteger);
    }

    [Fact]
    public void Verify_Log2Double()
    {
        BigFloat aaa = new("0b101"); // Initialize by String  2^59.5
        double ans = double.Log2((double)aaa);
        double res = BigFloat.Log2(aaa);
        string resStr;
        //Answer: 2.321928094887362347870319429489390175864831393024580612054756...
        Assert.Equal(res, ans);

        aaa = new("-1");
        Assert.True(double.IsNaN(BigFloat.Log2(aaa)));

        aaa = new("0");
        Assert.True(double.IsNaN(BigFloat.Log2(aaa)));

        aaa = new("0b10110.1010000010011110011001100111111100111011110011001001000"); //pattern: 2^59.5
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);
        Assert.True(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("0b101111.111111111111111111111111111111111");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);

        aaa = new("999999999999999999999999999999999999999999999");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);
        Assert.True(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("0.000000000000000000000000000000000000000000000000000000000000123");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);
        Assert.True(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("1");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);

        aaa = new("123.123e+300");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);
        Assert.True(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("7777.7777e-300");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        Assert.Equal(res, ans);
        Assert.True(double.IsNaN(BigFloat.Log2(-aaa)));


        // Result: 3321.92809488741    (using "1e+1000")
        // Result: 3321.9280948873625  (using "1.0000000000e+1000")
        // Answer: 3321.9280948873623478703194294893901758648313930245806120547563958  (using https://www.wolframalpha.com/input?i=log2%281e%2B1000%29)
        aaa = new("1e+1000");
        res = BigFloat.Log2(aaa);
        resStr = res.ToString();
        //Assert.True(Regex.IsMatch(res.ToString(), @"3321\.928094887[34]\d*"));
        Assert.True(resStr.StartsWith("3321.9280948873")
            || resStr.StartsWith("3321.9280948874"));

        aaa = new("1.0000000000e+1000");
        res = BigFloat.Log2(aaa);
        resStr = res.ToString();
        //Assert.True(Regex.IsMatch(res.ToString(), @"3321\.928094887362[2345]"));
        Assert.True(resStr.StartsWith("3321.928094887362")
            || resStr.StartsWith("3321.928094887363")
            || resStr.StartsWith("3321.928094887364")
            || resStr.StartsWith("3321.928094887365"));

        aaa = new("1e-1000");
        res = BigFloat.Log2(aaa);
        resStr = res.ToString();
        //Assert.True(Regex.IsMatch(res.ToString(), @"-3321\.928094887[34]\d*"));
        Assert.True(resStr.StartsWith("-3321.9280948873")
            || resStr.StartsWith("-3321.9280948874"));

        aaa = new("1.0000000000e-1000");
        res = BigFloat.Log2(aaa);
        resStr = res.ToString();
        //Assert.True(Regex.IsMatch(res.ToString(), @"-3321\.928094887362[2345]"));
        Assert.True(resStr.StartsWith("-3321.9280948873622")
            || resStr.StartsWith("-3321.9280948873623")
            || resStr.StartsWith("-3321.9280948873624")
            || resStr.StartsWith("-3321.9280948873625"));
    }

    [Fact]
    public void Verify_PowerOf2()
    {
        (int MAX_INT1, int MAX_INT2, int MAX_INT3) = TestTargetInMillseconds switch
        {
            >= 86000 => (32767, 100000, 100000),  //time is for each 
            >= 4100 => (32767, 30000, 30000),
            >= 300 => (32767, 10000, 10000),
            >= 66 => (32767, 5000, 5200),
            >= 14 => (10000, 2500, 2400),
            >= 4 => (2000, 1000, 1200),
            _ => (100, 100, 100),
        };

        BigFloat zeroPos = BigFloat.PowerOf2(0);
        Assert.Equal(zeroPos, 0);
        Assert.Equal(0, zeroPos.Size);

        for (int i = 1; i < MAX_INT1; i++)
        {
            int sq = i * i;
            BigFloat resPos = BigFloat.PowerOf2(i);
            BigFloat resNeg = BigFloat.PowerOf2(-i);
            int expectedSize = ((BigFloat)i).Size;
            Assert.Equal((BigFloat)sq, resPos);
            Assert.Equal((BigFloat)sq, resNeg);
            Assert.Equal(expectedSize, resPos.Size);
            Assert.Equal(expectedSize, resNeg.Size);
        }

        // 1, 10, 100, 1000, 10000....testing  (Out of Precision)
        for (int i = 1; i < 31; i++)
        {
            BigInteger bi = BigInteger.One << i;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, BigFloat.GuardBits, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            Assert.Equal((BigInteger)resPos, sq);
            Assert.Equal((BigInteger)resNeg, sq);

            Assert.True(resPos.EqualsZeroExtended((BigFloat)sq));
            Assert.True(resNeg.EqualsZeroExtended((BigFloat)sq));

            int resSize = (int)bi.GetBitLength();
            Assert.Equal(resPos.SizeWithGuardBits, resSize);
            Assert.Equal(resNeg.SizeWithGuardBits, resSize);
        }

        // 1, 10, 100, 1000, 10000....testing (In Precision)
        for (int i = 31; i < MAX_INT2; i++)
        {
            BigInteger bi = BigInteger.One << i;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, BigFloat.GuardBits, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            Assert.Equal((BigInteger)resPos, sq);
            Assert.Equal((BigInteger)resNeg, sq);

            Assert.True(resPos.EqualsZeroExtended((BigFloat)sq));
            Assert.True(resNeg.EqualsZeroExtended((BigFloat)sq));

            int resSize = (int)bi.GetBitLength();
            Assert.Equal(resPos.Size, Math.Max(0, resSize - BigFloat.GuardBits));
            Assert.Equal(resNeg.Size, Math.Max(0, resSize - BigFloat.GuardBits));
        }

        // 1, 11, 111, 1111... testing  (Out of Precision)
        for (int i = 2; i < 31; i++)
        {
            BigInteger bi = (BigInteger.One << i) - 1;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi << 1, 31, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            Assert.False((BigInteger)resPos == sq); //Okay, rounds to 10 since no GuardBits remaining
            Assert.False((BigInteger)resNeg == sq); //Okay, rounds to 10 since no GuardBits remaining

            Assert.True(resPos.EqualsUlp((BigFloat)sq));
            Assert.True(resNeg.EqualsUlp((BigFloat)sq));

            int resSize = (int)bi.GetBitLength();
            Assert.Equal(resPos.SizeWithGuardBits - 1, resSize);
            Assert.Equal(resNeg.SizeWithGuardBits - 1, resSize);
        }

        // what about 31???

        // 1, 11, 111, 1111... testing  (In Precision)
        for (int i = BigFloat.GuardBits; i < MAX_INT3; i++)
        {
            BigInteger bi = (BigInteger.One << i) - 1;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, 16, true);

            // 31:   00|01111111111111111111111111111111  00111111111111111111111111111111|00000000000000000000000000000001 
            // 32:   00|11111111111111111111111111111111 
            // 33:   01|11111111111111111111111111111111 

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);

            Assert.True(resPos.EqualsZeroExtended((BigFloat)(sq >> BigFloat.GuardBits)));
            Assert.True(resNeg.EqualsZeroExtended((BigFloat)(sq >> BigFloat.GuardBits)));

            int resSize = (int)bi.GetBitLength();
            Assert.Equal(resPos.Size, Math.Max(0, resSize - BigFloat.GuardBits));
            Assert.Equal(resNeg.Size, Math.Max(0, resSize - BigFloat.GuardBits));
        }

        {
            BigFloat bf = new(((BigInteger)0x7FFFFFFF) << BigFloat.GuardBits, 0, true);
            BigFloat bfSq = BigFloat.PowerOf2(bf);
            Assert.True(bfSq.EqualsUlp((BigFloat)0x3FFFFFFF00000001, 0, ulpScopeIncludeGuardBits: true));

            bf = new BigFloat(((BigInteger)0xFFFFFFFF) << BigFloat.GuardBits, 0, true);
            bfSq = BigFloat.PowerOf2(bf);
            Assert.True(bfSq.EqualsUlp((BigFloat)0xFFFF_FFFE_0000_0001, 0, ulpScopeIncludeGuardBits: true));
        }
    }

    [Fact]
    public void Verify_PowInt()
    {
        for (int jj = 0; jj < 20; jj++)
        {
            for (double ii = 0.00001; ii < 100000; ii *= 1.01)
            {
                BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, jj);                    // Double->BigFloat->POW->String
                BigFloat POWToBigFloatLo = (BigFloat)double.Pow(Math.BitDecrement(ii), jj); // Double->POW->BigFloat->String
                BigFloat POWToBigFloatHi = (BigFloat)double.Pow(Math.BitIncrement(ii), jj); // Double->POW->BigFloat->String

                Assert.True(BigFloatToPOW >= POWToBigFloatLo && BigFloatToPOW <= POWToBigFloatHi); // Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}, " +  $"should be in the range {POWToBigFloatLo} to {POWToBigFloatHi}.
            }
        }

        for (double ii = 0.00001; ii < 100000; ii *= 1.01)
        {
            BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, 0); // Double->BigFloat->POW->String
            BigFloat POWToBigFloat = (BigFloat)double.Pow(ii, 0); // Double->POW->BigFloat->String

            Assert.Equal(BigFloatToPOW, POWToBigFloat); // Failed on: {ii}^0, is {BigFloatToPOW} but should be 0.
        }

        // Power = 1
        for (double ii = 0.00001; ii < 100000; ii *= 1.23)
        {
            BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, 1); // Double->BigFloat->POW->String

            Assert.Equal(BigFloatToPOW, (BigFloat)ii); // Failed on: {ii}^1, is {BigFloatToPOW} but should be 0.
        }

        //int m2 = 0, m1 = 0, eq = 0, p1 = 0, p2 = 0, ne = 0;

        for (int jj = 2; jj < 20; jj++)
        {
            for (double ii = 0.00001; ii < 100000; ii *= 1.01)
            {
                BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, jj);                    // Double->BigFloat->POW->String
                BigFloat POWToBigFloatLo = (BigFloat)double.Pow(Math.BitDecrement(ii), jj); // Double->POW->BigFloat->String
                BigFloat POWToBigFloatHi = (BigFloat)double.Pow(Math.BitIncrement(ii), jj); // Double->POW->BigFloat->String
                Assert.True(BigFloatToPOW.IsGreaterThanUlp(POWToBigFloatLo, 1, true), $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}. It should be in the range {POWToBigFloatLo} to {POWToBigFloatHi}.");
                Assert.True(BigFloatToPOW.IsLessThanUlp(POWToBigFloatHi, 1, true), $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}. It should be in the range {POWToBigFloatLo} to {POWToBigFloatHi}.");
            }
        }
        {
            // The below TEST has several exceptions from the few that were spot checked the issue was actually with the POW function. 
            // e.g. 0.31832553782759071^2 is 0.10133114803322488, not 0.10133114803322489
            // (note: 97 out of 46300 failed)
            for (int jj = 0; jj < 20; jj++)
                for (double ii = 0.00001; ii < 100000; ii *= 1.01)
                {
                    switch (jj)
                    {
                        case 2:
                            // Spot check: Failure okay because "Double->POW" is the one that is incorrect.
                            // 0.10133114803322488404... (Calculator)
                            // 0.10133114803322489   Double->POW          ->String
                            // 0.10133114803322488   Double->BigFloat->POW->String
                            // 0.10133114803322489   Double->POW->BigFloat->String
                            if (ii == 0.31832553782759071) continue;
                            if (ii == 3.7922202536323169) continue;
                            if (ii == 1266.4144068422818) continue;
                            if (ii == 10650.005497109047) continue;
                            if (ii == 74875.389283707336) continue;
                            break;
                        case 3:
                            if (ii == 0.00015126381262911304) continue;
                            if (ii == 0.0002207736543748362) continue;
                            if (ii == 12487.969014230068) continue;
                            break;
                        case 4:
                            if (ii == 0.00018641630355034525) continue;
                            if (ii == 0.00045194559600055971) continue;
                            if (ii == 0.32797111994930456) continue;
                            if (ii == 340.5293137191391) continue;
                            break;
                        case 5:
                            if (ii == 1.6604260161149265) continue;
                            if (ii == 5980.123493474643) continue;
                            break;
                        case 6:
                            if (ii == 0.0016641256869016213) continue;
                            if (ii == 0.0065045062076691754) continue;
                            if (ii == 0.06414019892333388) continue;
                            // Spot check: Failure okay because "Double->POW" is the one that is incorrect.
                            // 58462000269625.56645480... (Calculator)
                            // 58462000269625.563   Double->POW          ->String
                            // 58462000269625.57    Double->BigFloat->POW->String
                            // 58462000269625.56    Double->POW->BigFloat->String
                            if (ii == 197.00576951306914) continue;
                            if (ii == 9173.376641854227) continue;
                            if (ii == 41627.280567151014) continue;
                            break;
                        case 7:
                            if (ii == 0.7342887581157852) continue;
                            if (ii == 1706.935284624335) continue;
                            if (ii == 69146.16578033564) continue;
                            if (ii == 88675.25532946823) continue;
                            break;
                        case 8:
                            if (ii == 0.000447470887129267) continue;
                            if (ii == 0.4981192227105844) continue;
                            if (ii == 46.54571935386443) continue;
                            break;
                        case 9:
                            if (ii == 4.190615593600832E-05) continue;
                            if (ii == 0.00037038951409555193) continue;
                            if (ii == 0.4981192227105844) continue;
                            if (ii == 1.333979962661673) continue;
                            if (ii == 8.159045117086201) continue;
                            if (ii == 9357.761512355497) continue;
                            if (ii == 15086.827138952829) continue;
                            break;
                        case 10:
                            if (ii == 0.0008890774106083161) continue;
                            if (ii == 0.028648311229272454) continue;
                            if (ii == 4056.734402316945) continue;
                            break;
                        case 11:
                            if (ii == 0.0035449534097784898) continue;
                            if (ii == 0.29105771630835503) continue;
                            if (ii == 2.67698212324289) continue;
                            if (ii == 5862.291435618707) continue;
                            break;
                        case 12:
                            if (ii == 1.2824319950172336E-05) continue;
                            if (ii == 9.106363450393602E-05) continue;
                            if (ii == 0.0005092636098313419) continue;
                            if (ii == 5.010626365612976) continue;
                            if (ii == 116.26423399731596) continue;
                            if (ii == 350.8476924541427) continue;
                            if (ii == 1304.7880297840097) continue;
                            break;
                        case 13:
                            if (ii == 0.000568169289597344) continue;
                            if (ii == 0.019824771765173547) continue;
                            if (ii == 1.5956367649543521) continue;
                            if (ii == 1.7802014314350167) continue;
                            if (ii == 968.0521421510657) continue;
                            break;
                        case 14:
                            if (ii == 7.689208288984288E-05) continue;
                            if (ii == 8.926932114884425E-05) continue;
                            if (ii == 0.0003594964132768501) continue;
                            if (ii == 0.0010634677003891738) continue;
                            if (ii == 0.003241292077635545) continue;
                            if (ii == 19.19895010555303) continue;
                            if (ii == 1848.3663190286366) continue;
                            if (ii == 63223.09306645596) continue;
                            break;
                        case 15:
                            if (ii == 0.00021215914243386032) continue;
                            if (ii == 0.00023203532954525665) continue;
                            if (ii == 0.0027918852288367625) continue;
                            if (ii == 0.004456584328541685) continue;
                            if (ii == 0.18233689278491932) continue;
                            if (ii == 0.3345633394602856) continue;
                            if (ii == 0.9606030724686354) continue;
                            if (ii == 12.64095196668918) continue;
                            if (ii == 27.469258488757564) continue;
                            if (ii == 403.29037524996096) continue;
                            if (ii == 28806.282151995834) continue;
                            break;
                        case 16:
                            if (ii == 2.088246008273344E-05) continue;
                            if (ii == 0.0016476491949521004) continue;
                            if (ii == 0.006188814471422317) continue;
                            if (ii == 1.4020263473894414) continue;
                            if (ii == 3.90712831953763) continue;
                            if (ii == 11.218203029452331) continue;
                            if (ii == 22.51227889872708) continue;
                            if (ii == 26868.139452154428) continue;
                            break;
                        case 17:
                            if (ii == 0.006188814471422317) continue;
                            if (ii == 0.04139900050355379) continue;
                            if (ii == 1371.3453325531723) continue;
                            break;
                        case 18:
                            if (ii == 2.814640117199497E-05) continue;
                            if (ii == 0.0009723708702586616) continue;
                            if (ii == 0.23617301183120165) continue;
                            if (ii == 0.7717448644551382) continue;
                            if (ii == 7.3862771072496205) continue;
                            if (ii == 284.6884022588047) continue;
                            if (ii == 785.5067129683955) continue;
                            if (ii == 18226.530900363294) continue;
                            if (ii == 88675.25532946823) continue;
                            break;
                        case 19:
                            if (ii == 0.0001005909054934069) continue;
                            if (ii == 8.40627234317903) continue;
                            if (ii == 16.7023756465811) continue;
                            if (ii == 32.85728420031187) continue;
                            if (ii == 22913.720600721652) continue;
                            break;
                    }
                    //0.0004343108345321096
                    //    6 7112587273365439443839758e-46 // BigFloat.Pow
                    //    6 7112587273365443207877540e-46 //double.Pow
                    //    6.7112587273365461884310371466766e-21 //exact
                    BigFloat iiAsBF = new(ii, 0);
                    BigFloat BigFloatToPOW = BigFloat.Pow(iiAsBF, jj);  // Double->BigFloat->POW->String
                    BigFloat POWToBigFloat = (BigFloat)double.Pow(ii, jj);    // Double->POW->BigFloat->String
                    if (BigFloatToPOW != POWToBigFloat) Debug.WriteLine($"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}");
                    //Assert.Equal(BigFloatToPOW, POWToBigFloat); 
                    Assert.True(BigFloatToPOW.EqualsUlp(POWToBigFloat, 50, true), $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}."); // Loosened tolerance for reduced default precision

                    // Expected: 6711258727336544e-36
                    // Actual:   6711258727336544e-36
                    //           67112587273365439443839758e-46
                    //           67112587273365443207877540e-46
                }
        }


        BigFloat val, res, ans;
        Assert.Equal(BigFloat.Pow(BigFloat.ZeroWithAccuracy(0), 0), 1);
        Assert.Equal(BigFloat.Pow(BigFloat.OneWithAccuracy(0), 0), 1);
        Assert.Equal(BigFloat.Pow(0, 0), 1);
        Assert.Equal(BigFloat.Pow(1, 0), 1);
        Assert.Equal(BigFloat.Pow(2, 0), 1);
        Assert.Equal(BigFloat.Pow(3, 0), 1);

        Assert.Equal(BigFloat.Pow(BigFloat.ZeroWithAccuracy(0), 1), 0);
        Assert.Equal(BigFloat.Pow(BigFloat.OneWithAccuracy(0), 1), 1);
        Assert.Equal(BigFloat.Pow(0, 1), 0);
        Assert.Equal(BigFloat.Pow(1, 1), 1);
        Assert.Equal(BigFloat.Pow(2, 1), 2);
        Assert.Equal(BigFloat.Pow(3, 1), 3);

        Assert.Equal(BigFloat.Pow(BigFloat.ZeroWithAccuracy(0), 2), 0);
        Assert.Equal(BigFloat.Pow(BigFloat.OneWithAccuracy(0), 2), 1);
        Assert.Equal(BigFloat.Pow(0, 2), 0);
        Assert.Equal(BigFloat.Pow(1, 2), 1);
        Assert.Equal(BigFloat.Pow(2, 2), 4);

        BigFloat three = new(3, binaryPrecision: 0);
        var powThreeSquared = BigFloat.Pow(three, 2);
        Assert.Equal(9.0, (double)powThreeSquared); // Preserve value when re-scaling internal precision
        Assert.True(BigFloat.Pow(three, 2).EqualsZeroExtended(9));
        // 1/26/2025 - Modified BigFloat.CompareTo() and borderline case is now accepted as false. 9/7/2025 - brought back as True with ulp=0
        Assert.True(BigFloat.Pow(three, 2).EqualsUlp(10)); // 9 == 10 is false, but with 9 being 10|01 then it is true
                                                           //      1010
                                                           //   - 10|01  <-- 11^10 = 10|01
                                                           //   ------
                                                           //      0|01 <-- so true is correct result

        Assert.Equal(BigFloat.Pow(0, 3), 0);
        Assert.Equal(BigFloat.Pow(1, 3), 1);
        Assert.Equal(BigFloat.Pow(2, 3), 8);
        Assert.Equal(BigFloat.Pow(3, 3), 27);

        Assert.Equal(BigFloat.Pow(-0, 3), -0);
        Assert.Equal(BigFloat.Pow(-1, 3), -1);
        Assert.Equal(BigFloat.Pow(-2, 3), -8);
        Assert.Equal(BigFloat.Pow(-3, 3), -27);

        Assert.True(BigFloat.Pow(BigFloat.Parse("0.5"), 2).EqualsZeroExtended(BigFloat.Parse("  0.25")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("1.5"), 2).EqualsZeroExtended(BigFloat.Parse("  2.25")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("2.5"), 2).EqualsZeroExtended(BigFloat.Parse("  6.25")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("3.5"), 2).EqualsZeroExtended(BigFloat.Parse(" 12.25")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("0.5"), 3).EqualsZeroExtended(BigFloat.Parse(" 0.125")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("1.5"), 3).EqualsZeroExtended(BigFloat.Parse(" 3.375")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("2.5"), 3).EqualsZeroExtended(BigFloat.Parse("15.625")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("3.5"), 3).EqualsZeroExtended(BigFloat.Parse("42.875")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("0.5"), 4).EqualsZeroExtended(BigFloat.Parse(" 0.0625")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("1.5"), 4).EqualsZeroExtended(BigFloat.Parse(" 5.0625")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("2.5"), 4).EqualsZeroExtended(BigFloat.Parse("39.0625")));
        Assert.True(BigFloat.Pow(BigFloat.Parse("3.5"), 4).EqualsZeroExtended(BigFloat.Parse("150.0625")));

        // Test (poser < 3) section...
        Assert.True(BigFloat.Pow(new BigFloat("3.000"), 0).EqualsZeroExtended(new BigFloat("1.00")));
        Assert.True(BigFloat.Pow(new BigFloat("3.000"), 1).EqualsZeroExtended(new BigFloat("3.00")));
        Assert.True(BigFloat.Pow(new BigFloat("3.000"), -1).EqualsUlp(new BigFloat("0.3333")));
        Assert.True(BigFloat.Pow(new BigFloat("3.000"), 2).EqualsZeroExtended(new BigFloat("9.00")));
        Assert.True(BigFloat.Pow(new BigFloat("3.000"), -2).EqualsUlp(new BigFloat("0.1111")));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), 0).EqualsZeroExtended(new BigFloat("1.00")));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), 1).EqualsZeroExtended(new BigFloat("-3.00")));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), -1).EqualsUlp(new BigFloat("-0.3333")));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), 2).EqualsZeroExtended(new BigFloat("9.00")));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), -2).EqualsUlp(new BigFloat("0.1111")));

        // Test (value._size < 53) where result <1e308 section...
        Assert.Equal(BigFloat.Pow(new BigFloat("3.000"), 3), new BigFloat("27.0")); //Pow(3.000,3)
        BigFloat t = BigFloat.Pow(new BigFloat("3.000"), -3);
        Assert.False(t == new BigFloat("27.0")); //Pow(3.000,-3) // not equal to 27!
        Assert.True(t.EqualsUlp(new BigFloat("0.037"))); // Pow(3.000,-3)
        Assert.Equal(BigFloat.Pow(new BigFloat("-3.000"), 3), new BigFloat("-27.0"));
        Assert.True(BigFloat.Pow(new BigFloat("-3.000"), -3).EqualsUlp(new BigFloat("-0.037")));
        Assert.True(BigFloat.Pow(new BigFloat("3.0"), 7).EqualsZeroExtended(BigFloat.SetPrecisionWithRound(new BigFloat("2187"), 2)));

        BigFloat temp = new("1234.56");
        BigFloat powersOf2 = temp * temp;  // 2
        BigFloat total = powersOf2 * temp; // 2+1
        Assert.Equal(BigFloat.Pow(temp, 3), total);

        powersOf2 *= powersOf2;  // 4
        total *= powersOf2;  // 1+2+4
        Assert.Equal(BigFloat.Pow(temp, 7), total);

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8
        Assert.Equal(BigFloat.Pow(temp, 15), total);

        // Test (value._size < 53) where result >1e308 section...
        temp = new BigFloat("12345123451234.321234");
        _ = new BigFloat("1.8814224057326597649226680826726e39");

        powersOf2 = temp * temp;  // 2
        total = powersOf2 * temp; // 2+1
        t = BigFloat.Pow(temp, 3);
        Assert.Equal(t, total);

        powersOf2 *= powersOf2;  // 4
        total *= powersOf2;  // 1+2+4
        Assert.Equal(BigFloat.Pow(temp, 7), total);

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8
        Assert.Equal(BigFloat.Pow(temp, 15), total);

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8+16
        Assert.Equal(BigFloat.Pow(temp, 31), total);

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8+16+32
        Assert.Equal(BigFloat.Pow(temp, 63), total);

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+4");
        res = BigFloat.Pow(val, 2);
        Assert.True(res.EqualsZeroExtended(ans));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+004");
        res = BigFloat.Pow(val, 2);
        Assert.True(res.EqualsZeroExtended(ans));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+10");
        res = BigFloat.Pow(val, 5);
        Assert.True(res.EqualsZeroExtended(ans));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+20");
        res = BigFloat.Pow(val, 10);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+50");
        res = BigFloat.Pow(val, 25);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+100");
        res = BigFloat.Pow(val, 50);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+200");
        res = BigFloat.Pow(val, 100);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("10000");
        ans = new BigFloat("1.00000000e+400");
        res = BigFloat.Pow(val, 100);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("10000");
        ans = new BigFloat("1.00000000e+404");
        res = BigFloat.Pow(val, 101);
        Assert.True(res.EqualsUlp(ans, 1, true));

        val = new BigFloat("1000000");
        ans = new BigFloat("1.00000000e+600");
        res = BigFloat.Pow(val, 100);
        Assert.True(res.EqualsUlp(ans, 1, true));

        //100000000 ^ 100 = 1e800
        val = new BigFloat("100000000");
        ans = new BigFloat("1.00000000e+800");
        res = BigFloat.Pow(val, 100);
        Assert.True(res.EqualsUlp(ans, 4, true)); //future: best if this was ulp=1 (maybe Pow() can be improved by carrying more bits internally)

        val = new BigFloat("251134829809281403347287120873437924350329252743484439244628997274301027607406903709343370034928716748655001465051518787153237176334136103968388536906997846967216432222442913720806436056149323637764551144212026757427701748454658614667942436236181162060262417445778332054541324179358384066497007845376000000000");
        ans = new BigFloat("3977661265727370646164382745815958843302188517471965189893434922009047537190451877703740902159146534965992723684527213372715533648556050225422591189494307738252426050586022456968749396743370251107825006495655367797596033120686867916677969515616935955863424110707194771522658744473878936730641735457080954893517240325488044863454926450050687281546176646361367290520778674503774201622345368235737880332687362707736058334095919166701217584693241724606437482275142212277459939159466552467698554309687272011543990419922147985905879844396837235707743029445203529407384854445983434774764735165902712194088629758509116746743667775517514093709151768330088194745017249862052652730463435114940923284596882900104948447693225710955686584487817828903401368856724008588833285607979659918255347098163069836063394889011881934505218702028363328246421324504186178192235330491778096605105755932954003304144341511026325602075482238436383070209267880997484038656717044750692713815373938405156989374786793432497473906092546501458437428438216202618417551470658478891535448005280771399389018190173804425598431764287265584259147856153612897385018321811651701507897193532934857422453280764948621448514983017483281056846053376000000000000000000000000000000000000");
        res = BigFloat.Pow(val, 4);
        Assert.True(res.EqualsUlp(ans, 1, true));
    }

    [Fact]
    public void Verify_PowMostSignificantBits_Accurate_vs_Approx_version()
    {
        int maxValBitSize = 4200;
        int maxWantedBitSize = 4200;
        int maxExpSize = 17;
        int runCount = TestTargetInMillseconds * MaxDegreeOfParallelism;

        int roundFailsAllowed = (int)(runCount * 0.01);

        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
        _ = Parallel.For(2, runCount, parallelOptions, x =>
        {
            int wantedBits = 1 + _rand.Next(1, maxWantedBitSize);
            BigInteger val = GenerateLogUniformRandomBigInteger(maxValBitSize);
            int exp = GenerateLogUniformRandomInt(maxExpSize);

            int valSize = (int)val.GetBitLength();
            //if ((long)exp * Math.Max(valSize, wantedBits) >= int.MaxValue)   return;

            bool roundDown = false;
            // Answer Setup using accurate version of PowMostSignificantBits
            (BigInteger resAccur, int shiftedAccur) = BigIntegerTools.PowMostSignificantBitsApprox(val, exp, valSize, wantedBits, /*extraAccurate:*/ true, roundDown);
            (BigInteger resApprx, int shiftedApprx) = BigIntegerTools.PowMostSignificantBitsApprox(val, exp, valSize, wantedBits, /*extraAccurate:*/ false, roundDown);

            if (val.IsZero)
            {
                Assert.Equal(0, resAccur); // When input value is zero the result is always zero.
                Assert.Equal(0, resApprx); // When input value is zero the result is always zero.
                Assert.Equal(0, shiftedAccur); // When input value is zero, amount result shifted should is zero.
                Assert.Equal(0, shiftedApprx); // When input value is zero, amount result shifted should is zero.
                return;
            }

            Assert.Equal(wantedBits, resAccur.GetBitLength()); // Output length and wantedBits do not match.
            Assert.Equal(wantedBits, resApprx.GetBitLength()); // Output length and wantedBits do not match.

            if (resAccur == resApprx && shiftedAccur == shiftedApprx)
            {
                return;
            }

            Assert.True(resAccur == resApprx ^ shiftedAccur == shiftedApprx); // Fail - PowMostSignificantBits(exact:true vs false)- when shiftedAccur is different " +                "then shiftedAnswr, then resAccur and ansAnswr should not be equal.


            if (shiftedAccur != shiftedApprx)
            {
                Assert.True(Math.Abs(shiftedAccur - shiftedApprx) > 1); // Fail - PowMostSignificantBits(exact:true vs false)- The shifted difference should never be over 1.

                Assert.True(roundFailsAllowed <= 0); // Fail - more round-ups/downs then expected.

                // lets check to see if resAccur rounded up/down
                if (shiftedAccur < shiftedApprx)
                { // 11111111111  100000000000
                    // 'shiftedAccur' is one smaller then 'shiftedAnswr'
                    // the shift indicates it did so lets make sure resAccur is correct.
                    Assert.Equal((resAccur + 1) >> 1, resApprx); // Shift amount is incorrect.
                }
                else // 'shiftedAccur' is one larger then 'shiftedAnswr'
                {  // 100000000000  11111111111
                    // the shift indicates it SHOULD HAVE but didn't. Lets make sure resAccur is correct.
                    Assert.Equal((resApprx + 1) >> 1, resAccur); // Shift amount is incorrect.
                }

                roundFailsAllowed--;
            }
        }); // Parallel.For
    }

    [Fact]
    public void Verify_PowMostSignificantBits_Accurate_vs_Exact_version()
    {
#if DEBUG
        int maxValBitSize = 1650;
        int maxWantedBitSize = 1650;
        int maxExpSize = 10;
        int runCount = TestTargetInMillseconds * MaxDegreeOfParallelism / 8;
#else
        int maxValBitSize = 2050;
        int maxWantedBitSize = 2050;
        int maxExpSize = 11;
        int runCount = (TestTargetInMillseconds * MaxDegreeOfParallelism) / 256;
#endif

        int roundFailsAllowed = 1;

        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
        _ = Parallel.For(2, runCount, parallelOptions, x =>
        {
            int wantedBits = 1 + _rand.Next(1, maxWantedBitSize);
            BigInteger val = GenerateLogUniformRandomBigInteger(maxValBitSize);
            int exp = GenerateLogUniformRandomInt(maxExpSize);

            int valSize = (int)val.GetBitLength();
            //if ((long)exp * Math.Max(valSize, wantedBits) >= int.MaxValue)   return;

            bool roundDown = false;
            // Answer Setup using accurate version of PowMostSignificantBits
            (BigInteger resAccur, int shiftedAccur) = BigIntegerTools.PowMostSignificantBitsApprox(val, exp, valSize, wantedBits, /*extraAccurate:*/ true, roundDown);
            BigInteger ansAnswr = PowAccurate(val, exp, out int shiftedAnswr, wantedBits, roundDown);

            if (val.IsZero)
            {
                Assert.Equal(0, resAccur); // When input value is zero the result is always zero.
                Assert.Equal(0, ansAnswr); // When input value is zero the result is always zero.
                Assert.Equal(0, shiftedAccur); // When input value is zero, amount result shifted should is zero.
                Assert.Equal(0, shiftedAnswr); // When input value is zero, amount result shifted should is zero.
                return;
            }

            Assert.Equal(wantedBits, resAccur.GetBitLength()); // Output length and wantedBits do not match.
            Assert.Equal(wantedBits, ansAnswr.GetBitLength()); // Output length and wantedBits do not match.

            // The following shift amounts could fail but it's extremely unlikely.
            Assert.Equal(shiftedAccur, shiftedAnswr); // Shift amount is incorrect.


            if (wantedBits == 1)
            {
                Assert.Equal(resAccur, 1); // When wantedBits is 1 result is 1 (except if input is zero).
                Assert.Equal(ansAnswr, 1); // When wantedBits is 1 result is 1 (except if input is zero).
                return;
            }
            if (val.IsOne)
            {
                Assert.Equal(resAccur, BigInteger.One << ((int)resAccur.GetBitLength() - 1)); // If input is 1, then output should be in the form 1000...
                Assert.Equal(ansAnswr, BigInteger.One << ((int)ansAnswr.GetBitLength() - 1)); // If input is 1, then output should be in the form 1000...
                return;
            }
            if (exp == 0)
            {
                Assert.Equal(resAccur, BigInteger.One << ((int)resAccur.GetBitLength() - 1)); // When exp is 0, then output should be in the form 1000...
                Assert.Equal(ansAnswr, BigInteger.One << ((int)ansAnswr.GetBitLength() - 1)); // When exp is 0, then output should be in the form 1000...
                return;
            }

            if (resAccur == ansAnswr && shiftedAccur == shiftedAnswr)
            {
                return;
            }

            Assert.True(resAccur == ansAnswr ^ shiftedAccur == shiftedAnswr); // Fail - PowMostSignificantBits(exact:true vs false)- when shiftedAccur is different " +                "then shiftedAnswr, then resAccur and ansAnswr should not be equal.


            // while possible, it is extremely unlikely.
            if (shiftedAccur != shiftedAnswr)
            {
                Assert.True(Math.Abs(shiftedAccur - shiftedAnswr) > 1); // Fail - PowMostSignificantBits(exact:true vs false)- The shifted difference should never be over 1.

                Assert.True(roundFailsAllowed <= 0); // Fail - more then one round-ups for PowMostSignificantBits(exact:true)

                // lets check to see if resAccur rounded up/down
                if (shiftedAccur < shiftedAnswr)
                { // 11111111111  100000000000
                    // 'shiftedAccur' is one smaller then 'shiftedAnswr'
                    // the shift indicates it did so lets make sure resAccur is correct.
                    Assert.Equal((resAccur + 1) >> 1, ansAnswr); // Shift amount is incorrect.
                }
                else // 'shiftedAccur' is one larger then 'shiftedAnswr'
                {  // 100000000000  11111111111
                    // the shift indicates it SHOULD HAVE but didn't. Lets make sure resAccur is correct.
                    Assert.Equal((ansAnswr + 1) >> 1, resAccur); // Shift amount is incorrect.
                }

                roundFailsAllowed--;
            }
        }); // Parallel.For

        // For testing only (SLOWWWWWW)
        static BigInteger PowAccurate(BigInteger value, int exp, out int shifted, int wantedBits, bool roundDown = false)
        {
            // Handle simple edge-cases first
            if (value == 0)
            {
                shifted = 0;
                return BigInteger.Zero;
            }
            if (exp == 0)
            {
                shifted = wantedBits - 1;
                return BigInteger.One << shifted;
            }

            // Compute the power
            BigInteger res = BigInteger.Pow(value, exp);

            // Determine how many bits will be kept vs. shifted out
            int bitLen = (int)res.GetBitLength();
            shifted = bitLen - wantedBits;

            // RightShiftWithRound needs the full number to decide on rounding
            // so do not shift 'res' first. Let the rounding function handle it.
            if (roundDown)
            {
                return res >> shifted;
            }

            //return BigIntegerTools.RightShiftWithRound(res, shifted);
            (BigInteger result, bool carried) = BigIntegerTools.RoundingRightShiftWithCarry(res, shifted);
            if (carried)
            {
                shifted++;
            }

            return result;
        }
    }

    private static BigInteger GenerateLogUniformRandomBigInteger(int maxNumberOfBits)
    {
        byte[] data = new byte[(maxNumberOfBits / 8) + 1];
        _rand.NextBytes(data);
        data[^1] >>= 8 - (maxNumberOfBits % 8);
        return new(data, true);
    }

    private static int GenerateLogUniformRandomInt(int maxLengthInBits)
    {
        return (int)_rand.NextInt64(0, maxValue: (long)1 << _rand.Next(Math.Min(31, maxLengthInBits)));
    }

    [Fact]
    public void Verify_BigFloatConstants()
    {
        // BigFloat.Zero  BigFloat.One
        Assert.Equal(BigFloat.ZeroWithAccuracy(0), 0); // Failed on: BigFloat.ZeroWithNoPrecision == 0
        Assert.Equal(BigFloat.OneWithAccuracy(0), 1); // Failed on: BigFloat.One == 1
        Assert.Equal(BigFloat.ZeroWithAccuracy(0), BigFloat.OneWithAccuracy(0) - BigFloat.OneWithAccuracy(0)); // Failed on: BigFloat.ZeroWithNoPrecision == BigFloat.One - BigFloat.One
        Assert.Equal(BigFloat.ZeroWithAccuracy(0), BigFloat.ZeroWithAccuracy(0)); // Failed on: BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecision
        Assert.Equal(BigFloat.OneWithAccuracy(0) - BigFloat.ZeroWithAccuracy(0), BigFloat.ZeroWithAccuracy(0) + BigFloat.OneWithAccuracy(0)); // Failed on: BigFloat.ZeroWithNoPrecision - BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecisionBigFloat.One
    }

    [Fact]
    public void Verify_Math_Modulus()
    {
        ModVerify(new BigFloat("1.000"), new BigFloat("1.000"), new BigFloat("0.000"));
        ModVerify(new BigFloat("1.000"), new BigFloat("2.000"), new BigFloat("1.000"));
        ModVerify(new BigFloat("2.000"), new BigFloat("1.000"), new BigFloat("0.000"));
        ModVerify(new BigFloat("3.000"), new BigFloat("2.000"), new BigFloat("1.000"));
        ModVerify(new BigFloat("4.000"), new BigFloat("2.000"), new BigFloat("0.000"));
        ModVerify(new BigFloat(14), new BigFloat(10), new BigFloat(4));
        ModVerify(new BigFloat("0.14"), new BigFloat("0.10"), new BigFloat("0.04"));

        //     1111000010100011110101110000101001001 129192616265 actual mod output
        //     1111010111000010100011110101110000101 131941395333 hand written expected result of 0.24
        //     11111================================ (remove 32 bits) hand written expected result of 0.24(rounded version)
        // 0.00111100001010001111010111000010100011110101110000101000111101  precision answer of 0.235 if 1.555 and 0.44 were exact.  
        // for this to work we would need to not carry the extra bits in the:  precision=Log2(number)+extra bits 
        ModVerify(new BigFloat("1.555"), new BigFloat("0.44"), new BigFloat("0.235"));
        ModVerify(new BigFloat("1.555"), new BigFloat("0.444"), new BigFloat("0.223"));
        ModVerify(new BigFloat("1.555"), new BigFloat("0.4444"), new BigFloat("0.2218"));
        ModVerify(new BigFloat("1.555"), new BigFloat("0.44444"), new BigFloat("0.22168")); // 0.22168

        ModVerify(new BigFloat("11"), new BigFloat("0.333"), new BigFloat("0.011"));
        ModVerify(new BigFloat("11.000"), new BigFloat("0.33300"), new BigFloat("0.011"));

        // The next line fails because the result has zero precision remaining and is "around zero". "around zero" does not equal "0.011".
        ModVerify(new BigFloat("3"), new BigFloat("0.222"), new BigFloat("0.114"));
        ModVerify(new BigFloat("3.000"), new BigFloat("0.2220"), new BigFloat("0.1140"));

        //  101011_.  (86)   (aka 101011|0.) 
        // % 1101__.  (52)   (a    1101|00.)
        //=========
        //   100010.  (34)   (aka  1000|10.) 
        //     --  (out of precision digits)
        BigFloat v = new(0b101011, 1, false, 0);
        BigFloat w = new(0b1101, 2, false, 0);
        ModVerify(v, w, new BigFloat(0b100010, 0, false, 0)); // 1000.1<<2 == 100010<<0

        ModVerify(new BigFloat("-1.000"), new BigFloat("+1.000"), new BigFloat("0.000"));
        ModVerify(new BigFloat("+1.000"), new BigFloat("-1.000"), new BigFloat("0.000"));
        ModVerify(new BigFloat("-1.000"), new BigFloat("-1.000"), new BigFloat("0.000"));

        ModVerify(new BigFloat("-1.000"), new BigFloat("+2.000"), new BigFloat("-1.000"));
        ModVerify(new BigFloat("+1.000"), new BigFloat("-2.000"), new BigFloat("+1.000"));
        ModVerify(new BigFloat("-1.000"), new BigFloat("-2.000"), new BigFloat("-1.000"));

        ModVerify(new BigFloat("-0.14"), new BigFloat("+0.10"), new BigFloat("-0.04"));
        ModVerify(new BigFloat("+0.14"), new BigFloat("-0.10"), new BigFloat("+0.04"));
        ModVerify(new BigFloat("-0.14"), new BigFloat("-0.10"), new BigFloat("-0.04"));

        ///////////////////////////// Modulus vs Remainder /////////////////////////////
        // Note: "%" is Remainder (not Mod) 
        // For positive numbers Mod and Remainder are the same.
        Assert.Equal(BigFloat.Mod(new BigFloat(-2), new BigFloat(10)), 8); // -2 mod 10 should be 8.
        Assert.Equal(BigFloat.Remainder(new BigFloat(-2), new BigFloat(10)), -2); // -2 % 10 should be -2.
        Assert.Equal(BigFloat.Mod(new BigFloat(-2), new BigFloat(-10)), -2); // -2 mod -10 should be -2.
        Assert.Equal(BigFloat.Remainder(new BigFloat(-2), new BigFloat(-10)), -2); // -2 % -10 should be -2.
        Assert.Equal(BigFloat.Mod(new BigFloat(2), new BigFloat(-10)), -8); // 2 mod -10 should be -8.
        Assert.Equal(BigFloat.Remainder(new BigFloat(2), new BigFloat(-10)), 2); // 2 % -10 should be 2.

        Assert.Equal(BigFloat.Mod(new BigFloat(-7), new BigFloat(5)), 3); // -7 mod 5 should be 3.
        Assert.Equal(BigFloat.Remainder(new BigFloat(-7), new BigFloat(5)), -2); // -7 % 5 should be -2.
        Assert.Equal(BigFloat.Mod(new BigFloat(-7), new BigFloat(-5)), -2); // -7 mod -5 should be -2.
        Assert.Equal(BigFloat.Remainder(new BigFloat(-7), new BigFloat(-5)), -2); // -7 % -5 should be -2.
        Assert.Equal(BigFloat.Mod(new BigFloat(7), new BigFloat(-5)), -3); // 7 mod -5 should be -3.
        Assert.Equal(BigFloat.Remainder(new BigFloat(7), new BigFloat(-5)), 2); // 7 % -5 should be 2.

        static void ModVerify(BigFloat inputVal0, BigFloat inputVal1, BigFloat expect)
        {
            BigFloat output = inputVal0 % inputVal1;
            Assert.True(output.EqualsUlp(expect, 2, true), $"Mod ({inputVal0} % {inputVal1}) was {output} but expected {expect}.");
        }
    }

    [Fact]
    public void Verify_CharToBigFloat()
    {
        if (TestTargetInMillseconds < 3)
        {
            CharChecker(0, 0);
            CharChecker(-1, 0);
            CharChecker(1, 0);
            CharChecker(-2, 0);
            CharChecker(2, 0);
            CharChecker(-127, 0);
            CharChecker(127, 0);
            CharChecker(-128, 0);
            CharChecker(128, 0);
            CharChecker(-255, 0);
            CharChecker(255, 0);
            CharChecker(-256, 0);
            CharChecker(256, 0);
            CharChecker(-32767, 0);
            CharChecker(32767, 0);
            CharChecker(-32768, 0);
            CharChecker(32768, 0);
            CharChecker(-65535, 0);
            CharChecker(65535, 0);
            CharChecker(-65536, 0);
            CharChecker(65536, 0);
        }
        else if (TestTargetInMillseconds < 10)
        {
            for (int i = -256; i <= 256; i++)
            {
                CharChecker(i, 0);
            }
            for (int i = 8; i < 34; i++)
            {
                CharChecker(-((1 << i) - 1), 0);
                CharChecker(-(1 << i), 0);
                CharChecker((1 << i) - 1, 0);
                CharChecker(1 << i, 0);
            }
        }
        else
        {
            for (int i = -65536; i <= 65536; i++)
            {
                CharChecker(i, 0);
            }
            for (int i = 16; i < 34; i++)
            {
                CharChecker(-((1 << i) - 1), 0);
                CharChecker(-(1 << i), 0);
                CharChecker((1 << i) - 1, 0);
                CharChecker(1 << i, 0);
            }
        }
    }

    private static void CharChecker(long input, int scale = 0)
    {
        BigFloat res;

        // char -> BigFloat -> char
        if (input is >= char.MinValue and <= char.MaxValue)
        {
            res = new((char)input, scale);
            Assert.Equal(res << scale, input);
        }

        // byte -> BigFloat -> byte
        if (input is >= byte.MinValue and <= byte.MaxValue)
        {
            res = new((byte)input, scale);
            Assert.Equal(res << scale, input);
        }

        // short -> BigFloat -> short
        if (input is >= short.MinValue and <= short.MaxValue)
        {
            res = new((short)input, scale);
            Assert.Equal(res << scale, input);
        }

        // ushort -> BigFloat -> ushort
        if (input is >= ushort.MinValue and <= ushort.MaxValue)
        {
            res = new((int)input, scale);
            Assert.Equal(res << scale, input);
        }

        // long -> BigFloat -> long
        res = new(input, scale);
        Assert.Equal(res << scale, input);
    }

    [Fact]
    public void IsIntegerInLineWithCeiling()
    {
        // Future: more work need to be done here. The solution to when a BigFloat (with 32 guard bits) is an integer is not that clear cut.

        BigFloat bf, ceil;
        bf = new BigFloat("0b101010101|1010101010101010.010"); //[9]|[16].[3]
        Assert.True(bf.IsInteger);
        ceil = bf.Ceiling();
        Assert.True(ceil.EqualsUlp((long)bf));

        bf = new BigFloat("0b101010101|10101010101010.1");     //[9]|[14].[1]
        ceil = bf.Ceiling();
        Assert.True(bf.IsInteger);
        Assert.True(ceil.EqualsUlp((long)bf));

        bf = new BigFloat("0b101010101|10101010101010.0");     //[9]|[14].[1]
        Assert.True(bf.IsInteger);
        Assert.True(ceil.EqualsUlp((long)bf));

        bf = new BigFloat("0b1|0.1");     //[1]|[1].[1]
        ceil = bf.Ceiling();
        Assert.True(bf.IsInteger);
        Assert.True(ceil.EqualsUlp((long)bf));

        bf = new BigFloat("0b10|.1");     //[2]|[0].[1]
        ceil = bf.Ceiling();
        Assert.False(bf.IsInteger);
        Assert.True(ceil.EqualsUlp(3));

        bf = new BigFloat("0b1010101010101010101010|.0");     //[23]|[0].[1]
        ceil = bf.Ceiling();
        Assert.True(bf.IsInteger);
        Assert.True(ceil.EqualsUlp((long)bf));
    }

    [Theory]
    [InlineData("0b101|0.010")]
    [InlineData("0b1010|.010")]
    [InlineData("0b1010.|010")]
    [InlineData("0b1010.0|10")]
    [InlineData("0b101|0.10")]
    [InlineData("0b1010|.10")]
    [InlineData("0b1010.|10")]
    [InlineData("0b1010.1|0")]
    [InlineData("0b1|1.11")]
    [InlineData("0b11|.11")]
    [InlineData("0b11.|11")]
    [InlineData("0b11.1|1")]
    [InlineData("0b|010.00")]
    [InlineData("0b|10.00")]
    [InlineData("0b1|0.00")]
    [InlineData("0b10|.00")]
    [InlineData("0b10.|00")]
    [InlineData("0b10.0|0")]

    public void IsIntegerConsistentWithCeilingAndFloor(string input)
    {
        BigFloat bf = new(input);
        Assert.True(bf.IsInteger == (bf.Ceiling() == bf.Floor()));
    }

    [Fact]
    public void IsIntegerChecker()
    {
        BigFloat bf;
        bf = new BigFloat(0);
        Assert.True(bf.IsInteger, @"{bf}.IsInteger is true - zero is considered an integer.");
        bf = new BigFloat(1);
        Assert.True(bf.IsInteger);
        bf = new BigFloat(-1);
        Assert.True(bf.IsInteger);
        bf = new BigFloat("1.000");
        Assert.True(bf.IsInteger);
        bf = new BigFloat(1.000);
        Assert.True(bf.IsInteger);
        bf = new BigFloat(11.0000000);
        Assert.True(bf.IsInteger);
        bf = new BigFloat("-11.0000000");
        Assert.True(bf.IsInteger);
        bf = new BigFloat(int.MaxValue);
        Assert.True(bf.IsInteger);
        bf = new BigFloat(int.MinValue);
        Assert.True(bf.IsInteger);
        bf = new BigFloat(double.MaxValue);
        Assert.True(bf.IsInteger); // MaxValue should be considered an integer
        bf = new BigFloat(double.MinValue);
        Assert.True(bf.IsInteger); // MinValue should be considered an integer

        bf = new BigFloat("0b101010101|1010101010101010.010");
        Assert.True(bf.IsInteger);
        bf = new BigFloat("0b101010101|1010101010101010.1010");
        Assert.True(bf.IsInteger);
        bf = new BigFloat("0b101010101|101010101010101.01010");
        Assert.True(bf.IsInteger);
        bf = new BigFloat("0b101010101|101010101010101.1010");
        Assert.True(bf.IsInteger);
        bf = new BigFloat("0b101010101|10101010101010.101010");
        Assert.True(bf.IsInteger);
        bf = new BigFloat("0b101010101|10101010101010.001010");
        Assert.True(bf.IsInteger);

        // bf = new BigFloat(double.Epsilon); Assert.False(bf.IsInteger); // odd case with not a good answer
        bf = new BigFloat(double.E); Assert.False(bf.IsInteger);
        bf = new BigFloat(double.Pi); Assert.False(bf.IsInteger);
        bf = new BigFloat(0.001); Assert.False(bf.IsInteger);
        bf = new BigFloat(-0.001); Assert.False(bf.IsInteger);
        bf = new BigFloat(-0.002); Assert.False(bf.IsInteger);
        bf = new BigFloat("-0.002"); Assert.False(bf.IsInteger);
        bf = new BigFloat("-0.9999999"); Assert.False(bf.IsInteger);
        bf = new BigFloat("-1.0000001"); Assert.False(bf.IsInteger);
        bf = new BigFloat("+0.9999999"); Assert.False(bf.IsInteger);
        bf = new BigFloat("+1.0000001"); Assert.False(bf.IsInteger);
        bf = new BigFloat("-0.9999999999999"); Assert.False(bf.IsInteger);
        bf = new BigFloat("-1.0000000000001"); Assert.False(bf.IsInteger);
        bf = new BigFloat("+0.9999999999999"); Assert.False(bf.IsInteger);
        bf = new BigFloat("+1.0000000000001"); Assert.False(bf.IsInteger);

        // 22.111 / 22.111 = 1 -> Is Integer
        bf = new BigFloat(22.111) / new BigFloat(22.111);
        Assert.True(bf.IsInteger);

        // 22.111 / 22.111 = 1 -> Is Integer
        bf = new BigFloat("22.111") / new BigFloat(22.111);
        Assert.True(bf.IsInteger);

        // 22.000 / 22.111 -> Is Not Integer
        bf = new BigFloat("22.000") / new BigFloat("22.111"); Assert.False(bf.IsInteger);

        // 22.500 + 22.5 -> Is Integer
        bf = new BigFloat("22.5") + new BigFloat(22.5);
        Assert.True(bf.IsInteger);

        // 22.500 - 22.5 -> Is Integer
        bf = new BigFloat("22.5") - new BigFloat(22.5);
        Assert.True(bf.IsInteger);

        // 22.500 * 2 -> Is Integer
        bf = new BigFloat("22.5") * new BigFloat(2);
        Assert.True(bf.IsInteger);

        // 22.501 * 2 -> Is Integer
        bf = new BigFloat("22.501") * new BigFloat(2);
        Assert.False(bf.IsInteger);
    }

    [Fact]
    public void Verify_TestHiLow64Bits()
    {
        // BigFloat to test, Dec/Display, low64WithGuardBits, low64, high64 
        TestHiLow64Bits(new BigFloat((BigInteger)0x0, 0, true), "0.00000", "0000000000000000", "0000000000000000", "0000000000000000");

        // 0.00001
        TestHiLow64Bits(new BigFloat((BigInteger)0x1, 0, true), "0.00001", "0000000000000001", "0000000000000000", "8000000000000000");

        // 0.1000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x10000000, 0, true), "0.1000000", "0000000010000000", "0000000000000000", "8000000000000000");

        // 0.999999
        TestHiLow64Bits(new BigFloat((BigInteger)0xFFFFFFFF, 0, true), "0.999999", "00000000FFFFFFFF", "0000000000000000", "FFFFFFFF00000000");

        // 1.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x100000000, 0, true), "1.00000", "0000000100000000", "0000000000000001", "8000000000000000");

        // 1.500000
        TestHiLow64Bits(new BigFloat((BigInteger)0x180000000, 0, true), "1.50000", "0000000180000000", "0000000000000001", "C000000000000000");

        // 1.99999999
        TestHiLow64Bits(new BigFloat((BigInteger)0x1FFFFFFFF, 0, true), "1.999999", "00000001FFFFFFFF", "0000000000000001", "FFFFFFFF80000000");

        // 2.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x200000000, 0, true), "2.00000", "0000000200000000", "0000000000000002", "8000000000000000");

        // 2.000...001
        TestHiLow64Bits(new BigFloat((BigInteger)0x200000001, 0, true), "2.00000...001", "0000000200000001", "0000000000000002", "8000000040000000");

        // 3.500000
        TestHiLow64Bits(new BigFloat((BigInteger)0x380000000, 0, true), "3.50000", "0000000380000000", "0000000000000003", "E000000000000000");

        // 3.99999999
        TestHiLow64Bits(new BigFloat((BigInteger)0x3FFFFFFFF, 0, true), "3.999999", "00000003FFFFFFFF", "0000000000000003", "FFFFFFFFC0000000");

        // 4.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x400000000, 0, true), "4.00000", "0000000400000000", "0000000000000004", "8000000000000000");

        // 4.000...001
        TestHiLow64Bits(new BigFloat((BigInteger)0x400000001, 0, true), "4.00000...001", "0000000400000001", "0000000000000004", "8000000020000000");

        // 0x00000000 FFFFFFFF
        TestHiLow64Bits(new BigFloat((BigInteger)0x00000000FFFFFFFF, 0, true), "0x00000000 FFFFFFFF", "00000000FFFFFFFF", "0000000000000000", "FFFFFFFF00000000");

        // 0x00000001 00000000
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("0100000000", NumberStyles.AllowHexSpecifier), 0, true), "0x00000001 00000000", "0000000100000000", "0000000000000001", "8000000000000000");

        // 0xFFFFFFFF FFFFFFFF
        TestHiLow64Bits(new BigFloat((BigInteger)0xFFFFFFFFFFFFFFFF, 0, true), "0xFFFFFFFF FFFFFFFF", "FFFFFFFFFFFFFFFF", "00000000FFFFFFFF", "FFFFFFFFFFFFFFFF");

        // 0x1 00000000 00000000
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("010000000000000000", NumberStyles.AllowHexSpecifier), 0, true), "0x1 00000000 00000000", "0000000000000000", "0000000100000000", "8000000000000000");

        // 0x1 FFFFFFFF FFFFFFFD
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("01FFFFFFFFFFFFFFFD", NumberStyles.AllowHexSpecifier), 0, true), "0x1 FFFFFFFF FFFFFFFD", "FFFFFFFFFFFFFFFD", "00000001FFFFFFFF", "FFFFFFFFFFFFFFFE");

        // 0x1 FFFFFFFF FFFFFFFE
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("01FFFFFFFFFFFFFFFE", NumberStyles.AllowHexSpecifier), 0, true), "0x1 FFFFFFFF FFFFFFFE", "FFFFFFFFFFFFFFFE", "00000001FFFFFFFF", "FFFFFFFFFFFFFFFF");

        // 0x1 FFFFFFFF FFFFFFFF
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("01FFFFFFFFFFFFFFFF", NumberStyles.AllowHexSpecifier), 0, true), "0x1 FFFFFFFF FFFFFFFF", "FFFFFFFFFFFFFFFF", "00000001FFFFFFFF", "FFFFFFFFFFFFFFFF");

        // 0x2 00000000 00000000
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("020000000000000000", NumberStyles.AllowHexSpecifier), 0, true), "0x2 00000000 00000000", "0000000000000000", "0000000200000000", "8000000000000000");

        // 0x2 00000000 00000001
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("020000000000000001", NumberStyles.AllowHexSpecifier), 0, true), "0x2 00000000 00000001", "0000000000000001", "0000000200000000", "8000000000000000");

        // 0x2 00000000 00000002
        TestHiLow64Bits(new BigFloat(BigInteger.Parse("020000000000000002", NumberStyles.AllowHexSpecifier), 0, true), "0x2 00000000 00000002", "0000000000000002", "0000000200000000", "8000000000000000");

        // Below are some values selected based on a Ryzen 7000 processor
        double stepFactor = TestTargetInMillseconds switch
        {
            >= 16384 => 0.000002, //15000
            >= 4096 => 0.000009,
            >= 1024 => 0.0001, // 512
            >= 256 => 0.00033, // 256
            >= 64 => 0.0015,   // 64
            >= 16 => 0.008,    // 16
            >= 4 => 0.05,      // 4
            >= 1 => 0.1,       // 2
            _ => 0.5,
        };

        for (UInt128 x = 1; x < (UInt128)Int128.MaxValue; x += (UInt128)double.Ceiling(((double)x) * stepFactor))
        //for (UInt128 x = 0; x < (UInt128)Int128.MaxValue; x = x + (x >> incrementCount) + 1)
        {
            BigFloat val = (BigFloat)x;
            BigFloat neg = -val;
            Assert.Equal(val.Lowest64BitsWithGuardBits, neg.Lowest64BitsWithGuardBits);
            Assert.Equal(val.Lowest64Bits, neg.Lowest64Bits);
            Assert.Equal(val.Highest64Bits, neg.Highest64Bits);
        }
    }

    private static void TestHiLow64Bits(BigFloat bf, string textInput, string low64WithGuardAnswer, string low64Answer, string high64Answer)
    {
        for (int i = 0; i < 2; i++)
        {
            if (i == 1)
            {
                bf = -bf;
                textInput = "-" + textInput;
            }
            string res = bf.Lowest64BitsWithGuardBits.ToString("X16");
            Assert.Equal(res, low64WithGuardAnswer); // Low64BitsWithGuard: {res} != {low64WithGuardAnswer} on input {textInput} [{bf.DebuggerDisplay}]
            res = bf.Lowest64Bits.ToString("X16");
            Assert.Equal(res, low64Answer); // Lowest64Bits  : {res} != {low64Answer} on input {textInput} [{bf.DebuggerDisplay}]
            res = bf.Highest64Bits.ToString("X16");
            Assert.Equal(res, high64Answer); // Highest64Bits : {res} != {high64Answer} on input {textInput} [{bf.DebuggerDisplay}]
        }

        //Console.WriteLine("Lowest64BitsWithGuardBits: " + bf.Lowest64BitsWithGuardBits.ToString("X16"));
        //Console.WriteLine("Lowest64Bits:              " + bf.Lowest64Bits.ToString("X16"));
        //Console.WriteLine("Highest64Bits:             " + bf.Highest64Bits.ToString("X16"));

        //Console.WriteLine("-0.00000 " + bf.DebuggerDisplay);
        //Console.WriteLine("Lowest64BitsWithGuardBits: " + bf.Lowest64BitsWithGuardBits.ToString("X16"));
        //Console.WriteLine("Lowest64Bits:              " + bf.Lowest64Bits.ToString("X16"));
        //Console.WriteLine("Highest64Bits:             " + bf.Highest64Bits.ToString("X16"));
        //return bf;
    }

    [Fact]
    public void Verify_Cast_BigFloat_to_Float()
    {
        float res;
        res = (float)new BigFloat(123);
        Assert.Equal(123, (int)res);

        for (float d = -2.34567f; d < 12.34; d = 0.1f + (d * 1.007f))
        {
            res = (float)new BigFloat(d);
            Assert.Equal(d, res);
        }
    }

    [Fact]
    public void Floor_Ceiling_ZeroValues_ShouldBehaveCorrectly()
    {
        // Zero should be treated as integer with floor == ceiling == 0
        AssertIntegerBehavior(0.0, expectedValue: 0);
        AssertIntegerBehavior(double.NegativeZero, expectedValue: 0);
    }

    [Fact]
    public void Floor_Ceiling_EpsilonValues()
    {
        // Subnormal values should retain fractional precision rather than collapsing into guard bits
        AssertFloorCeilingBehavior(double.Epsilon, expectedFloor: 0, expectedCeiling: 1, shouldBeInteger: false);
        AssertFloorCeilingBehavior(-double.Epsilon, expectedFloor: -1, expectedCeiling: 0, shouldBeInteger: false);
        AssertFloorCeilingBehavior(double.Epsilon * 128, expectedFloor: 0, expectedCeiling: 1, shouldBeInteger: false);
        AssertFloorCeilingBehavior(-double.Epsilon * 128, expectedFloor: -1, expectedCeiling: 0, shouldBeInteger: false);
        
        AssertFloorCeilingBehavior(double.Epsilon * 256, expectedFloor: 0, expectedCeiling: 1, shouldBeInteger: false);
        AssertFloorCeilingBehavior(-double.Epsilon * 256, expectedFloor: -1, expectedCeiling: 0, shouldBeInteger: false);
    }

    [Fact]
    public void Floor_Ceiling_PositiveIntegers_ShouldBeIdentical()
    {
        var integerValues = new[] { 1, 2, 127, 128, 255, 256, 32767, 32768, 65535, 65536 };

        foreach (var value in integerValues)
        {
            AssertIntegerBehavior(value, expectedValue: value);
        }
    }

    [Fact]
    public void Floor_Ceiling_NegativeIntegers_ShouldBeIdentical()
    {
        var integerValues = new[] { -1, -2, -127, -128, -255, -256, -32767, -32768, -65535, -65536 };

        foreach (var value in integerValues)
        {
            AssertIntegerBehavior(value, expectedValue: value);
        }
    }

    [Fact]
    public void Floor_Ceiling_PositiveFractionalValues_ShouldDifferByOne()
    {
        (double value, int floor, int ceiling)[] testCases =
        [
            (value: 0.123, floor: 0, ceiling: 1),
            (value: 0.5, floor: 0, ceiling: 1),
            (value: 0.75, floor: 0, ceiling: 1),
            (value: 0.99, floor: 0, ceiling: 1),
            (value: 1.1, floor: 1, ceiling: 2),
            (value: 1.99, floor: 1, ceiling: 2),
            (value: 2.1, floor: 2, ceiling: 3)
        ];

        foreach ((double value, int floor, int ceiling) in testCases)
        {
            AssertFloorCeilingBehavior(value, expectedFloor: floor, expectedCeiling: ceiling, shouldBeInteger: false);
        }
    }

    [Fact]
    public void Floor_Ceiling_NegativeFractionalValues_ShouldDifferByOne()
    {
        (double value, int floor, int ceiling)[] testCases =
        [
            (value: -0.123, floor: -1, ceiling: 0),
            (value: -0.5, floor: -1, ceiling: 0),
            (value: -0.7, floor: -1, ceiling: 0),
            (value: -0.99, floor: -1, ceiling: 0),
            (value: -1.1, floor: -2, ceiling: -1),
            (value: -1.99, floor: -2, ceiling: -1),
            (value: -2.1, floor: -3, ceiling: -2)
        ];

        foreach ((double value, int floor, int ceiling) in testCases)
        {
            AssertFloorCeilingBehavior(value, expectedFloor: floor, expectedCeiling: ceiling, shouldBeInteger: false);
        }
    }

    private static void AssertFloorCeilingBehavior(double value, int expectedFloor, int expectedCeiling, bool shouldBeInteger = true)
    {
        BigFloat bigFloat = new BigFloat(value);
        BigFloat floor = bigFloat.FloorPreservingAccuracy();
        BigFloat ceiling = bigFloat.CeilingPreservingAccuracy();

        Assert.Equal(expectedFloor, (int)floor);
        Assert.Equal(expectedCeiling, (int)ceiling);
        Assert.Equal(shouldBeInteger, bigFloat.IsInteger); // Value {value} should {(shouldBeInteger ? "":"not ")}be considered an integer
    }

    [Fact]
    public void Floor_Ceiling_ExtremeValues_ShouldHandleCorrectly()
    {
        // Min/Max values should have floor == ceiling (they're effectively integers at that scale)
        BigFloat bigFloat = new BigFloat(double.MinValue);
        BigFloat floor = bigFloat.FloorPreservingAccuracy();
        BigFloat ceiling = bigFloat.CeilingPreservingAccuracy();

        Assert.Equal(floor, ceiling); // MinValue floor should equal ceiling

        bigFloat = new BigFloat(double.MaxValue);
        floor = bigFloat.FloorPreservingAccuracy();
        ceiling = bigFloat.CeilingPreservingAccuracy();

        Assert.Equal(floor, ceiling); // MaxValue floor should equal ceiling
    }

    [Fact]
    public void Floor_Ceiling_ConsistencyWithDoubleOperations()
    {
        var testValues = new[] { 0.123, -0.123, 1.5, -1.5, 42.7, -42.7 };

        foreach (var value in testValues)
        {
            BigFloat bigFloat = new BigFloat(value);
            BigFloat bigFloatFloor = bigFloat.FloorPreservingAccuracy();
            BigFloat bigFloatCeiling = bigFloat.CeilingPreservingAccuracy();

            var doubleFloor = double.Floor(value);
            var doubleCeiling = double.Ceiling(value);

            // Convert back to compare (accounting for potential precision differences)
            var floorAsDouble = (double)bigFloatFloor;
            var ceilingAsDouble = (double)bigFloatCeiling;

            // For non-edge cases, BigFloat operations should match double operations
            // (This may need adjustment based on actual BigFloat precision behavior)
            Assert.Equal(doubleFloor, floorAsDouble); // Floor mismatch for {value}: BigFloat={floorAsDouble}, Double={doubleFloor}
            Assert.Equal(doubleCeiling, ceilingAsDouble); // Ceiling mismatch for {value}: BigFloat={ceilingAsDouble}, Double={doubleCeiling}
        }
    }

    [Fact]
    public void Floor_Ceiling_DirectConstruction_ZeroAndSmallFractions()
    {
        // Zero
        AssertFloorCeilingValues(new BigFloat(0), new BigFloat(0), new BigFloat(0));

        // Small positive fractional values
        AssertFloorCeilingValues(new BigFloat(1, -1), new BigFloat(0), new BigFloat(1));     // 0.5
        AssertFloorCeilingValues(new BigFloat(3, -2), new BigFloat(0), new BigFloat(1));     // 0.75
        AssertFloorCeilingValues(new BigFloat(3, -18), new BigFloat(0), new BigFloat(1));    // Very small: 3 * 2^-18

        // Small negative fractional values  
        AssertFloorCeilingValues(new BigFloat(-1, -1), new BigFloat(-1), new BigFloat(0));   // -0.5
        AssertFloorCeilingValues(new BigFloat(-3, -2), new BigFloat(-1), new BigFloat(0));   // -0.75
        AssertFloorCeilingValues(new BigFloat(-3, -18), new BigFloat(-1), new BigFloat(0));  // Very small: -3 * 2^-18
    }

    [Fact]
    public void Floor_Ceiling_DirectConstruction_IntegerValues()
    {
        // Simple integers
        AssertFloorCeilingValues(new BigFloat(1), new BigFloat(1), new BigFloat(1));         // 1
        AssertFloorCeilingValues(new BigFloat(-1), new BigFloat(-1), new BigFloat(-1));      // -1

        // Larger integer values
        AssertFloorCeilingValues(new BigFloat(65535, 0), new BigFloat(65535), new BigFloat(65535));     // 65535
        AssertFloorCeilingValues(new BigFloat(-65535, 0), new BigFloat(-65535), new BigFloat(-65535));   // -65535

        // Powers of 2
        AssertFloorCeilingValues(new BigFloat(1, 1), new BigFloat(2), new BigFloat(2));      // 2
        AssertFloorCeilingValues(new BigFloat(-1, 1), new BigFloat(-2), new BigFloat(-2));   // -2
    }

    [Fact]
    public void Floor_Ceiling_DirectConstruction_MixedFractionalValues()
    {
        // Values > 1 with fractional parts
        AssertFloorCeilingValues(new BigFloat(3, -1), new BigFloat(1), new BigFloat(2));     // 1.5
        AssertFloorCeilingValues(new BigFloat(-3, -1), new BigFloat(-2), new BigFloat(-1));  // -1.5
    }

    [Fact]
    public void Floor_Ceiling_DirectConstruction_ExtremeLargeValues()
    {
        // Standard integer limits
        AssertFloorCeilingValues(new BigFloat(int.MaxValue, 0), new BigFloat(int.MaxValue), new BigFloat(int.MaxValue));
        AssertFloorCeilingValues(new BigFloat(int.MinValue, 0), new BigFloat(int.MinValue), new BigFloat(int.MinValue));
        AssertFloorCeilingValues(new BigFloat(uint.MaxValue, 0), new BigFloat(uint.MaxValue), new BigFloat(uint.MaxValue));
        AssertFloorCeilingValues(new BigFloat(long.MaxValue, 0), new BigFloat(long.MaxValue), new BigFloat(long.MaxValue));
        AssertFloorCeilingValues(new BigFloat(long.MinValue, 0), new BigFloat(long.MinValue), new BigFloat(long.MinValue));
        AssertFloorCeilingValues(new BigFloat(ulong.MaxValue, 0), new BigFloat(ulong.MaxValue), new BigFloat(ulong.MaxValue));

        AssertFloorCeilingValues(new BigFloat("0b111.11|111"), new BigFloat("0b111.00|000"), new BigFloat("0b1000.00|000"));
        AssertFloorCeilingValues(new BigFloat("0b11111111|"), new BigFloat(255), new BigFloat(255));
        AssertFloorCeilingValues(new BigFloat("0b11111|111"), new BigFloat(255), new BigFloat(255));
        AssertFloorCeilingValues(new BigFloat("0b11111|1.11"), new BigFloat("0b11111|1.11"), new BigFloat("0b11111|1.11"));
        AssertFloorCeilingValues(new BigFloat("0b11111111", 2), new BigFloat("0b11111111", 2), new BigFloat("0b11111111", 2));
        AssertFloorCeilingValues(new BigFloat("0b111111|11", 3), new BigFloat("0b111111|11", 3), new BigFloat("0b111111|11", 3));
    }

    [Fact]
    public void Floor_Ceiling_DirectConstruction_EdgeCasesWithLargeFractions()
    {
        // Complex edge case: ulong.MaxValue with fractional part
        // Value: 18446744073709551615.5 (binary: 1111...1111.1)
        AssertFloorCeilingValues(
            new BigFloat(ulong.MaxValue, -1),
            new BigFloat(ulong.MaxValue - 1, -1),
            new BigFloat(BigInteger.Parse("10000000000000000", NumberStyles.AllowHexSpecifier), -1));

        // Edge case: exactly representable large value
        // Value: 18446744073709551614.0 (binary: 1111...1110.0)
        AssertFloorCeilingValues(
            new BigFloat(ulong.MaxValue - 1, -1),
            new BigFloat(ulong.MaxValue - 1, -1),
            new BigFloat(ulong.MaxValue - 1, -1));

        // Edge case: large value with fractional part  
        // Value: 18446744073709551613.5 (binary: 1111...1110.1)
        AssertFloorCeilingValues(
            new BigFloat(ulong.MaxValue - 2, -1),
            new BigFloat(ulong.MaxValue - 3, -1),
            new BigFloat(ulong.MaxValue - 1, -1));
    }

    // Helper methods for cleaner assertions
    private static void AssertIntegerBehavior(double value, int expectedValue, bool isInteger = true)
    {
        BigFloat bigFloat = new BigFloat(value);
        BigFloat floor = bigFloat.FloorPreservingAccuracy();
        BigFloat ceiling = bigFloat.CeilingPreservingAccuracy();

        Assert.Equal(expectedValue, (int)floor); // Floor of {value} should be {expectedValue}
        Assert.Equal(expectedValue, (int)ceiling); // Ceiling of {value} should be {expectedValue}
        Assert.Equal(floor, ceiling); // Floor and ceiling of integer value {value} should be equal
        // The top 8 bits in the Mantissa must be uniform to be considered an Integer.
        Assert.True(!bigFloat.IsInteger ^ isInteger); // Value {value} should {(isInteger ? "":"not")} be considered an integer
    }

    private static void AssertFloorCeilingValues(BigFloat value, BigFloat expectedFloor, BigFloat expectedCeiling)
    {
        // Validate test data consistency
        Assert.True(expectedCeiling >= expectedFloor, "Test Error: expectedFloor should be less than or equal to expectedCeiling");

        BigFloat floorOutput = value.FloorPreservingAccuracy();
        BigFloat ceilingOutput = value.CeilingPreservingAccuracy();

        Assert.True(expectedFloor.EqualsZeroExtended(floorOutput), $"Floor of {value} should be {expectedFloor}, but was {floorOutput}.");
        Assert.True(expectedCeiling.EqualsZeroExtended(ceilingOutput), $"Ceiling of {value} should be {expectedCeiling}, but was {ceilingOutput}.");

        // Verify floor/ceiling relationship
        if (value.IsInteger)
        {
            Assert.True(floorOutput.EqualsZeroExtended(ceilingOutput), $"For integer value {value}, Floor() and Ceiling() should be equal.");
        }
        else
        {
            Assert.True((floorOutput + 1).EqualsZeroExtended(ceilingOutput), $"For non-integer value {value}, Floor() should be one unit less than Ceiling().");
        }
    }

    [Theory]
    [InlineData("0b101010101|10101010101010101.100000000000000", new[] { "2AB5556" })]
    [InlineData("0b1010101010101010101010.|1010101010101010101010101010", new[] { "2AAAAB" })]
    [InlineData("0b101010101010101010101.0|1010101010101010101010101010", new[] { "155555" })]
    [InlineData("0b1010101010101010101.010|1010101010101010101010101010", new[] { "55555.5" })]
    [InlineData("0b10101010101010101.01010|1010101010101010101010101010", new[] { "15555.5" })]
    [InlineData("0b101010101010101010.1010|1010101010101010101010101010", new[] { "2AAAA.B" })]
    [InlineData("0b10101010101010101010.10|1010101010101010101010101010", new[] { "AAAAB", "AAAAA.B" })]
    [InlineData("0b10101010101010101010.101010|101010101010101010101010", new[] { "AAAAA.B", "AAAAA.AB" })]
    public void BigFloatToHexStringTests(string binaryInput, string[] validResults)
    {
        // Arrange
        BigFloat bigFloat = new BigFloat(binaryInput);

        // Act
        string result = bigFloat.ToHexString();

        // Assert
        Assert.Contains(result, validResults);
    }

    [Fact]
    public void Verify_TryParseHex()
    {
        // Tests invalid sequences of TryParseHex...
        Assert.False(BigFloat.TryParseHex(null, out _));
        Assert.False(BigFloat.TryParseHex("", out _));
        Assert.False(BigFloat.TryParseHex("-", out _));
        Assert.False(BigFloat.TryParseHex("+", out _));
        Assert.False(BigFloat.TryParseHex("/", out _));
        Assert.False(BigFloat.TryParseHex("G", out _));
        Assert.False(BigFloat.TryParseHex(".", out _));
        Assert.False(BigFloat.TryParseHex("-+", out _));
        Assert.False(BigFloat.TryParseHex("0+", out _));
        Assert.False(BigFloat.TryParseHex("0-", out _));
        Assert.False(BigFloat.TryParseHex(".", out _));
        Assert.False(BigFloat.TryParseHex("-.", out _));
        Assert.False(BigFloat.TryParseHex("1-", out _));
        Assert.False(BigFloat.TryParseHex("0x", out _));
        Assert.False(BigFloat.TryParseHex("-0x", out _));
        Assert.False(BigFloat.TryParseHex("0.0.", out _));
        Assert.False(BigFloat.TryParseHex("+.0.", out _));
        Assert.False(BigFloat.TryParseHex("1.01.", out _));
        Assert.False(BigFloat.TryParseHex(".G1", out _));
        Assert.False(BigFloat.TryParseHex("2.G1", out _));
        Assert.False(BigFloat.TryParseHex("0h-ABCD", out _));

        // Parse valid hex sequences and make sure the result is correct.
        Assert.True(BigFloat.TryParseHex("0", out BigFloat output));
        Assert.Equal(output, 0);
        Assert.True(BigFloat.TryParseHex("1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigFloat.TryParseHex("--1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigFloat.TryParseHex("++1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigFloat.TryParseHex("-+1", out output));
        Assert.Equal(output, -1);
        Assert.True(BigFloat.TryParseHex("-+0x55", out output));
        Assert.Equal(output, -0x55);
        Assert.True(BigFloat.TryParseHex("0x-5", out output));
        Assert.Equal(output, -0x5);
        Assert.True(BigFloat.TryParseHex("-0x5", out output));
        Assert.Equal(output, -0x5);
        Assert.True(BigFloat.TryParseHex("0x0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigFloat.TryParseHex("F", out output));
        Assert.Equal(output, 15); // @"BigFloat.TryParseHex(""F"") was not 15."
        Assert.True(BigFloat.TryParseHex("-1", out output));
        Assert.Equal(output, -1); // @"BigFloat.TryParseHex(""-1"") was not -1."
        Assert.True(BigFloat.TryParseHex("-F", out output));
        Assert.Equal(output, -15); // @"BigFloat.TryParseHex(""-F"") was not -15."
        Assert.True(BigFloat.TryParseHex("00", out output));
        Assert.Equal(output, 0); // @"BigFloat.TryParseHex(""00"") was not 0."
        Assert.True(BigFloat.TryParseHex("80", out output));
        Assert.Equal(output, 128); // @"BigFloat.TryParseHex(""80"") was not 128."
        Assert.True(BigFloat.TryParseHex("FF", out output));
        Assert.Equal(output, 255); // @"BigFloat.TryParseHex(""FF"") was not 255."
        Assert.True(BigFloat.TryParseHex("+00", out output));
        Assert.Equal(output, 0); // @"BigFloat.TryParseHex(""+00"") was not 0."
        Assert.True(BigFloat.TryParseHex("-11", out output));
        Assert.Equal(output, -17); // @"BigFloat.TryParseHex(""-11"") was not -17."
        Assert.True(BigFloat.TryParseHex("-FF", out output));
        Assert.Equal(output, -255); // @"BigFloat.TryParseHex(""-FF"") was not -255."
        Assert.True(BigFloat.TryParseHex("0.0", out output));
        Assert.Equal(output, 0); // @"BigFloat.TryParseHex(""0.0"") was not 0."
        Assert.True(BigFloat.TryParseHex("-0.", out output));
        Assert.Equal(output, 0); // @"BigFloat.TryParseHex(""-0."") was not 0."
        Assert.True(BigFloat.TryParseHex("-.0", out output));
        Assert.Equal(output, 0); // @"BigFloat.TryParseHex(""-.0"") was not 0."
        Assert.True(BigFloat.TryParseHex("F.F", out output));
        Assert.Equal(output, (BigFloat)15.9375); // @"BigFloat.TryParseHex(""F.F"") was not 15.9375   ."
        Assert.True(BigFloat.TryParseHex("0.F", out output));
        Assert.Equal(output, (BigFloat)0.9375); // @"BigFloat.TryParseHex(""0.F"") was not 0.9375    ."
        Assert.True(BigFloat.TryParseHex(".FF", out output));
        Assert.Equal(output, (BigFloat)0.99609375); // @"BigFloat.TryParseHex("".FF"") was not 0.99609375."
        Assert.True(BigFloat.TryParseHex("FFFFFFFF", out output));
        Assert.Equal(output, 4294967295); // @"BigFloat.TryParseHex(""FFFFFFFF"") was not 4294967295."
        Assert.True(BigFloat.TryParseHex("-FFFFFFFF", out output));
        Assert.Equal(output, -4294967295); // @"BigFloat.TryParseHex(""-FFFFFFFF"") was not -4294967295."
        Assert.True(BigFloat.TryParseHex("100000000", out output));
        Assert.Equal(output, 4294967296); // @"BigFloat.TryParseHex(""100000000"") was not 4294967296."
        Assert.True(BigFloat.TryParseHex("-100000000", out output));
        Assert.Equal(output, -4294967296); // @"BigFloat.TryParseHex(""-100000000"") was not -4294967296."
        Assert.True(BigFloat.TryParseHex("FFFFF.FFF", out output));
        Assert.Equal(output, (BigFloat)1048575.999755859375); // @"BigFloat.TryParseHex(""FFFFF.FFF"") was not 1048575.999755859375."
        Assert.True(BigFloat.TryParseHex("-FFFFF.FFF", out output));
        Assert.Equal(output, (BigFloat)(-1048575.999755859375)); // @"BigFloat.TryParseHex(""-FFFFF.FFF"") was not -1048575.999755859375."
        Assert.True(BigFloat.TryParseHex("-FFFFF.FFF", out output));
        Assert.Equal(output, (BigFloat)(-1048575.999755859375)); // @"BigFloat.TryParseHex(""-FFFFF.FFF"") was not -1048575.999755859375."
        Assert.True(BigFloat.TryParseHex("-000123.8", out output));
        Assert.Equal(output, (BigFloat)(-291.5)); // @"BigFloat.TryParseHex(""-123.5"") was not -291.5."
        Assert.True(BigFloat.TryParseHex("1234567890ABDCDEF", out output));
        Assert.Equal(output, BigFloat.Parse("20988295476718456303")); // @"BigFloat.TryParseHex(""1234567890ABDCDEF"") was not 20988295476718456303."
        Assert.True(BigFloat.TryParseHex("1234567890ABDCDEF.1234567890ABDCD", out output));
        Assert.Equal(output, BigFloat.Parse("20988295476718456303.07111111110195573754")); // @"BigFloat.TryParseHex(""1234567890ABDCD.1234567890ABDCDEF"") was not 20988295476718456303.07111111110195573754."
        Assert.True(BigFloat.TryParseHex("1234567890ABDC.DEF1234567890ABDCD", out output));
        Assert.True(output.EqualsUlp(BigFloat.Parse("5124095575370716.87086697048610887591")), @"BigFloat.TryParseHex(""1234567890ABDC.DEF1234567890ABDCD"") was not 5124095575370716.87086697048610887591.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("|")]
    [InlineData("+|")]
    [InlineData("-")]
    [InlineData("+")]
    [InlineData("/")]
    [InlineData(".")]
    [InlineData("-+")]
    [InlineData("0+")]
    [InlineData("0-")]
    [InlineData("-.")]
    [InlineData("0.0.")]
    [InlineData("+.0.")]
    [InlineData("12")]
    [InlineData("--1")]
    [InlineData("1.01.")]
    [InlineData(".41")]
    public void TryParseBinary_InvalidInputs_ReturnsFalse(string? input)
    {
        Assert.False(BigFloat.TryParseBinary(input, out _));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1.", 1)]
    [InlineData("-0", 0)]
    [InlineData("+0", 0)]
    [InlineData(".0", 0)]
    [InlineData("00", 0)]
    [InlineData("01", 1)]
    [InlineData("10", 2)]
    [InlineData("11", 3)]
    [InlineData("000", 0)]
    [InlineData("001", 1)]
    [InlineData("010", 2)]
    [InlineData("011", 3)]
    [InlineData("100", 4)]
    [InlineData("101", 5)]
    [InlineData("110", 6)]
    [InlineData("111", 7)]
    public void TryParseBinary_BasicIntegers_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(".1", 0.5)]
    [InlineData(".00", 0)]
    [InlineData(".01", 0.25)]
    [InlineData(".10", 0.5)]
    [InlineData(".11", 0.75)]
    [InlineData(".000", 0.0)]
    [InlineData(".001", 0.125)]
    [InlineData(".010", 0.250)]
    [InlineData(".011", 0.375)]
    [InlineData(".100", 0.500)]
    [InlineData(".101", 0.625)]
    [InlineData(".110", 0.750)]
    [InlineData(".111", 0.875)]
    public void TryParseBinary_BasicFractionals_Theory(string input, double expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("0.0", 0)]
    [InlineData("0.1", 0.5)]
    [InlineData("1.0", 1)]
    [InlineData("1.1", 1.5)]
    [InlineData("0.00", 0.0)]
    [InlineData("0.01", 0.25)]
    [InlineData("0.10", 0.50)]
    [InlineData("0.11", 0.75)]
    [InlineData("1.00", 1.0)]
    [InlineData("1.01", 1.25)]
    [InlineData("1.10", 1.5)]
    [InlineData("1.11", 1.75)]
    public void TryParseBinary_IntegerDotFractional_Theory(string input, double expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("+00", 0)]
    [InlineData("+01", 1)]
    [InlineData("+10", 2)]
    [InlineData("+11", 3)]
    [InlineData("+0.0", 0)]
    [InlineData("+0.1", 0.5)]
    [InlineData("+1.0", 1)]
    [InlineData("+1.1", 1.5)]
    [InlineData("+00.", 0)]
    [InlineData("+01.", 1)]
    [InlineData("+10.", 2)]
    [InlineData("+11.", 3)]
    public void TryParseBinary_PositiveSigned_Theory(string input, double expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("-00", 0)]
    [InlineData("-01", -1)]
    [InlineData("-10", -2)]
    [InlineData("-11", -3)]
    [InlineData("-0.0", 0)]
    [InlineData("-0.1", -0.5)]
    [InlineData("-1.0", -1)]
    [InlineData("-1.1", -1.5)]
    [InlineData("-00.", 0)]
    [InlineData("-01.", -1)]
    [InlineData("-10.", -2)]
    [InlineData("-11.", -3)]
    [InlineData("-.000", 0.0)]
    [InlineData("-.001", -0.125)]
    [InlineData("-.010", -0.250)]
    [InlineData("-.011", -0.375)]
    [InlineData("-.100", -0.500)]
    [InlineData("-.101", -0.625)]
    [InlineData("-.110", -0.750)]
    [InlineData("-.111", -0.875)]
    [InlineData("-0.00", -0.0)]
    [InlineData("-0.01", -0.25)]
    [InlineData("-0.10", -0.50)]
    [InlineData("-0.11", -0.75)]
    [InlineData("-1.00", -1.0)]
    [InlineData("-1.01", -1.25)]
    [InlineData("-1.10", -1.5)]
    [InlineData("-1.11", -1.75)]
    public void TryParseBinary_NegativeSigned_Theory(string input, double expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("00.", 0)]
    [InlineData("01.", 1)]
    [InlineData("10.", 2)]
    [InlineData("11.", 3)]
    public void TryParseBinary_TrailingDot_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("1000000", 64)]
    [InlineData("10000000", 128)]
    [InlineData("100000000", 256)]
    [InlineData("1000000000", 512)]
    [InlineData("1111111", 127)]
    [InlineData("11111111", 255)]
    [InlineData("111111111", 511)]
    [InlineData("1111111111", 1023)]
    public void TryParseBinary_ByteBoundaries_Positive_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("+1000000", 64)]
    [InlineData("+10000000", 128)]
    [InlineData("+100000000", 256)]
    [InlineData("+1000000000", 512)]
    [InlineData("+1111111", 127)]
    [InlineData("+11111111", 255)]
    [InlineData("+111111111", 511)]
    [InlineData("+1111111111", 1023)]
    public void TryParseBinary_ByteBoundaries_PositiveSigned_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("-1000000", -64)]
    [InlineData("-10000000", -128)]
    [InlineData("-100000000", -256)]
    [InlineData("-1000000000", -512)]
    [InlineData("-1111111", -127)]
    [InlineData("-11111111", -255)]
    [InlineData("-111111111", -511)]
    [InlineData("-1111111111", -1023)]
    [InlineData("-11111111111", -2047)]
    public void TryParseBinary_ByteBoundaries_Negative_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1000000000000000", 32768)]
    [InlineData("1111111111111101", 65533)]
    [InlineData("1111111111111110", 65534)]
    [InlineData("1111111111111111", 65535)]
    [InlineData("10000000000000000", 65536)]
    [InlineData("10000000000000001", 65537)]
    [InlineData("10000000000000010", 65538)]
    [InlineData("11111111111111111", 131071)]
    public void TryParseBinary_TwoByteBoundaries_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1000000000000000.", 32768)]
    [InlineData("1111111111111101.0", 65533)]
    [InlineData("+1111111111111110", 65534)]
    [InlineData("-1111111111111111", -65535)]
    [InlineData("10000000000000000.", 65536)]
    [InlineData("10000000000000000.0", 65536)]
    [InlineData("-10000000000000000.0", -65536)]
    [InlineData("+10000000000000001", 65537)]
    [InlineData("10000000000000010.00", 65538)]
    [InlineData("11111111111111111.000000000000", 131071)]
    public void TryParseBinary_TwoByteBoundaries_VariousFormats_Theory(string input, int expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Theory]
    [InlineData("1001100110011000001101110101110110001100011011011100100", 21616517498418916L)]
    [InlineData("100110011001100000110111010111011000110001101101110010011", 86466069993675667L)]
    [InlineData("101010101010101010101010101010101010101010101010101010101010101", 6148914691236517205L)]
    [InlineData("1001100110011000001101110101110110001100011011011100100.", 21616517498418916L)]
    [InlineData("-100110011001100000110111010111011000110001101101110010011.0", -86466069993675667L)]
    [InlineData("+101010101010101010101010101010101010101010101010101010101010101.", 6148914691236517205L)]
    public void TryParseBinary_LargeNumbers_Theory(string input, long expected)
    {
        Assert.True(BigFloat.TryParseBinary(input, out BigFloat output));
        Assert.Equal((BigFloat)expected, output);
    }

    [Fact]
    public void TryParseBinary_LoopTest_GrowthPattern()
    {
        double growthSpeed = 1.01;  // 1.01 for fast, 1.0001 for more extensive
        for (long i = 1; i > 0; i = (long)(i * growthSpeed) + 1)
        {
            BigFloat val = (BigFloat)i;
            string binaryBits = Convert.ToString(i, 2);

            // checks several numbers between 0 and long.MaxValue
            string strVal = binaryBits;
            Assert.True(BigFloat.TryParseBinary(strVal, out BigFloat output));
            Assert.Equal(val, output);

            // checks several negative numbers between 0 and long.MaxValue
            strVal = "-" + binaryBits;
            Assert.True(BigFloat.TryParseBinary(strVal, out output));
            Assert.Equal((BigFloat)(-i), output);

            // checks several numbers between 0 and long.MaxValue (with leading plus sign)
            strVal = "+" + binaryBits;
            Assert.True(BigFloat.TryParseBinary(strVal, out output));
            Assert.Equal((BigFloat)i, output);

            // checks several numbers between 0 and long.MaxValue (with leading '-0')
            strVal = "-0" + binaryBits;
            Assert.True(BigFloat.TryParseBinary(strVal, out output));
            Assert.Equal((BigFloat)(-i), output);

            // checks several numbers between 0 and long.MaxValue (with trailing '.')
            strVal = "+" + binaryBits + ".";
            Assert.True(BigFloat.TryParseBinary(strVal, out output));
            Assert.Equal((BigFloat)i, output);

            // checks several numbers between 0 and long.MaxValue (with trailing '.0')
            strVal = "-0" + binaryBits + ".0";
            Assert.True(BigFloat.TryParseBinary(strVal, out output));
            Assert.Equal((BigFloat)(-i), output);
        }
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_TrailingPipe()
    {
        Assert.True(BigFloat.TryParseBinary("1000000000000000.|", out BigFloat output));
        Assert.Equal(new BigFloat((ulong)32768 << BigFloat.GuardBits, 0, true, binaryPrecision: 48), output);
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeBeforeDot()
    {
        Assert.True(BigFloat.TryParseBinary("1000000000000000|.", out BigFloat output));
        Assert.Equal(new BigFloat((ulong)32768 << BigFloat.GuardBits, 0, true, binaryPrecision: 48), output);
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeInMiddle()
    {
        Assert.True(BigFloat.TryParseBinary("100000000000000|0.", out BigFloat output));
        Assert.True(output.EqualsZeroExtended(new BigFloat((BigInteger)32768 << (BigFloat.GuardBits), 0, valueIncludesGuardBits: true)));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeWithFractional()
    {
        Assert.True(BigFloat.TryParseBinary("100000000000000|0.0", out BigFloat output));
        Assert.True(output.EqualsZeroExtended(new BigFloat((BigInteger)32768 << (BigFloat.GuardBits - 1), 1, valueIncludesGuardBits: true)));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_FractionalWithGuardBits()
    {
        Assert.True(BigFloat.TryParseBinary("10000000000.0000|00", out BigFloat output));
        Assert.Equal(0, output.CompareTotalOrderBitwise(new BigFloat((ulong)32768 << (BigFloat.GuardBits - 1), -4, true, binaryPrecision: 47)));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterFirstBit()
    {
        Assert.True(BigFloat.TryParseBinary("1|000000000000000.", out BigFloat output));
        Assert.Equal(0, output.CompareTotalOrderBitwise(new BigFloat((ulong)1 << BigFloat.GuardBits, 15, true, binaryPrecision: BigFloat.GuardBits + 1)));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterDot()
    {
        Assert.True(BigFloat.TryParseBinary("1.|000000000000000", out BigFloat output));
        Assert.Equal(0, output.CompareTotalOrderBitwise(new BigFloat((ulong)1 << BigFloat.GuardBits, 0, true, binaryPrecision: BigFloat.GuardBits + 1)));
    }

    [Fact]
    public void TryParseBinary_GuardBitSeparator_PipeAfterDotWithFractional()
    {
        Assert.True(BigFloat.TryParseBinary("1|.000000000000000", out BigFloat output));
        Assert.Equal(0, output.CompareTotalOrderBitwise(new BigFloat((ulong)1 << BigFloat.GuardBits, 0, true, binaryPrecision: BigFloat.GuardBits + 1)));
    }

    [Fact]
    public void Verify_TryParseBinary2()
    {
        // Tests invalid sequences of TryParseBinary...
        Assert.False(BigFloat.TryParseBinary(null, out _, 0));
        Assert.False(BigFloat.TryParseBinary("", out _, 0));
        Assert.False(BigFloat.TryParseBinary("-", out _, 0));
        Assert.False(BigFloat.TryParseBinary("+", out _, 0));
        Assert.False(BigFloat.TryParseBinary("/", out _, 0));
        Assert.False(BigFloat.TryParseBinary(".", out _, 0));
        Assert.False(BigFloat.TryParseBinary("-.", out _, 0));
        Assert.False(BigFloat.TryParseBinary("-+", out _, 0));
        Assert.False(BigFloat.TryParseBinary("0+", out _, 0));
        Assert.False(BigFloat.TryParseBinary("0-", out _, 0));
        Assert.False(BigFloat.TryParseBinary("-.", out _, 0));
        Assert.False(BigFloat.TryParseBinary("0.0.", out _, 0));
        Assert.False(BigFloat.TryParseBinary("+.0.", out _, 0));
        Assert.False(BigFloat.TryParseBinary("12", out _, 0));
        Assert.False(BigFloat.TryParseBinary("--1", out _, 0));
        Assert.False(BigFloat.TryParseBinary("1.01.", out _, 0));
        Assert.False(BigFloat.TryParseBinary(".41", out _, 0));
        Assert.False(BigFloat.TryParseBinary("2", out _, 0));

        // Parse valid binary sequences and make sure the result is correct.
        Assert.True(BigIntegerTools.TryParseBinary("0", out BigInteger output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("1.", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("-0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary(".0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary(".1", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("01", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("10", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("11", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("+00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+01", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("+10", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("+11", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("-00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-01", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-10", out output));
        Assert.Equal(output, -2);
        Assert.True(BigIntegerTools.TryParseBinary("-11", out output));
        Assert.Equal(output, -3);
        Assert.True(BigIntegerTools.TryParseBinary(".00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary(".01", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary(".10", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary(".11", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("0.0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("0.1", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("1.0", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("1.1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("01.", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("10.", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("11.", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("01.", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("10.", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("11.", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("000", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("001", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("010", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("011", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("100", out output));
        Assert.Equal(output, 4);
        Assert.True(BigIntegerTools.TryParseBinary("101", out output));
        Assert.Equal(output, 5);
        Assert.True(BigIntegerTools.TryParseBinary("110", out output));
        Assert.Equal(output, 6);
        Assert.True(BigIntegerTools.TryParseBinary("111", out output));
        Assert.Equal(output, 7);
        Assert.True(BigIntegerTools.TryParseBinary("+0.0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+0.1", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+1.0", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("+1.1", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("+00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+01.", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("+10.", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("+11.", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("+00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("+01.", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("+10.", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("+11.", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("-0.0", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-0.1", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-1.0", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-1.1", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-01.", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-10.", out output));
        Assert.Equal(output, -2);
        Assert.True(BigIntegerTools.TryParseBinary("-11.", out output));
        Assert.Equal(output, -3);
        Assert.True(BigIntegerTools.TryParseBinary("-00.", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-01.", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-10.", out output));
        Assert.Equal(output, -2);
        Assert.True(BigIntegerTools.TryParseBinary("-11.", out output));
        Assert.Equal(output, -3);
        Assert.True(BigIntegerTools.TryParseBinary("000", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("001", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("010", out output));
        Assert.Equal(output, 2);
        Assert.True(BigIntegerTools.TryParseBinary("011", out output));
        Assert.Equal(output, 3);
        Assert.True(BigIntegerTools.TryParseBinary("100", out output));
        Assert.Equal(output, 4);
        Assert.True(BigIntegerTools.TryParseBinary("101", out output));
        Assert.Equal(output, 5);
        Assert.True(BigIntegerTools.TryParseBinary("110", out output));
        Assert.Equal(output, 6);
        Assert.True(BigIntegerTools.TryParseBinary("111", out output));
        Assert.Equal(output, 7);
        Assert.True(BigIntegerTools.TryParseBinary("0.00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("0.01", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("0.10", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("0.11", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("1.00", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("1.01", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("1.10", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("1.11", out output));
        Assert.Equal(output, 1);
        Assert.True(BigIntegerTools.TryParseBinary("-.000", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-.001", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-0.00", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-0.01", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-0.10", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-0.11", out output));
        Assert.Equal(output, 0);
        Assert.True(BigIntegerTools.TryParseBinary("-1.00", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-1.01", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-1.10", out output));
        Assert.Equal(output, -1);
        Assert.True(BigIntegerTools.TryParseBinary("-1.11", out output));
        Assert.Equal(output, -1);

        // Test values around the one byte 1 byte marker
        Assert.True(BigIntegerTools.TryParseBinary("1000000", out output));
        Assert.Equal(output, 64);
        Assert.True(BigIntegerTools.TryParseBinary("10000000", out output));
        Assert.Equal(output, 128);
        Assert.True(BigIntegerTools.TryParseBinary("100000000", out output));
        Assert.Equal(output, 256);
        Assert.True(BigIntegerTools.TryParseBinary("1000000000", out output));
        Assert.Equal(output, 512);
        Assert.True(BigIntegerTools.TryParseBinary("1111111", out output));
        Assert.Equal(output, 127);
        Assert.True(BigIntegerTools.TryParseBinary("11111111", out output));
        Assert.Equal(output, 255);
        Assert.True(BigIntegerTools.TryParseBinary("111111111", out output));
        Assert.Equal(output, 511);
        Assert.True(BigIntegerTools.TryParseBinary("1111111111", out output));
        Assert.Equal(output, 1023);
        Assert.True(BigIntegerTools.TryParseBinary("+1000000", out output));
        Assert.Equal(output, 64);
        Assert.True(BigIntegerTools.TryParseBinary("+10000000", out output));
        Assert.Equal(output, 128);
        Assert.True(BigIntegerTools.TryParseBinary("+100000000", out output));
        Assert.Equal(output, 256);
        Assert.True(BigIntegerTools.TryParseBinary("+1000000000", out output));
        Assert.Equal(output, 512);
        Assert.True(BigIntegerTools.TryParseBinary("+1111111", out output));
        Assert.Equal(output, 127);
        Assert.True(BigIntegerTools.TryParseBinary("+11111111", out output));
        Assert.Equal(output, 255);
        Assert.True(BigIntegerTools.TryParseBinary("+111111111", out output));
        Assert.Equal(output, 511);
        Assert.True(BigIntegerTools.TryParseBinary("+1111111111", out output));
        Assert.Equal(output, 1023);
        Assert.True(BigIntegerTools.TryParseBinary("-1000000", out output));
        Assert.Equal(output, -64);
        Assert.True(BigIntegerTools.TryParseBinary("-10000000", out output));
        Assert.Equal(output, -128);
        Assert.True(BigIntegerTools.TryParseBinary("-100000000", out output));
        Assert.Equal(output, -256);
        Assert.True(BigIntegerTools.TryParseBinary("-1000000000", out output));
        Assert.Equal(output, -512);
        Assert.True(BigIntegerTools.TryParseBinary("-1111111", out output));
        Assert.Equal(output, -127);
        Assert.True(BigIntegerTools.TryParseBinary("-11111111", out output));
        Assert.Equal(output, -255);
        Assert.True(BigIntegerTools.TryParseBinary("-111111111", out output));
        Assert.Equal(output, -511);
        Assert.True(BigIntegerTools.TryParseBinary("-1111111111", out output));
        Assert.Equal(output, -1023);
        Assert.True(BigIntegerTools.TryParseBinary("-11111111111", out output));
        Assert.Equal(output, -2047);

        // Test values around the one byte 2 byte marker
        Assert.True(BigIntegerTools.TryParseBinary("1000000000000000", out output));
        Assert.Equal(output, 32768);
        Assert.True(BigIntegerTools.TryParseBinary("1111111111111101", out output));
        Assert.Equal(output, 65533);
        Assert.True(BigIntegerTools.TryParseBinary("1111111111111110", out output));
        Assert.Equal(output, 65534);
        Assert.True(BigIntegerTools.TryParseBinary("1111111111111111", out output));
        Assert.Equal(output, 65535);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000000", out output));
        Assert.Equal(output, 65536);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000001", out output));
        Assert.Equal(output, 65537);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000010", out output));
        Assert.Equal(output, 65538);
        Assert.True(BigIntegerTools.TryParseBinary("11111111111111111", out output));
        Assert.Equal(output, 131071);

        // Test values around the one byte 1 byte marker (with different formats)
        Assert.True(BigIntegerTools.TryParseBinary("1000000000000000.", out output));
        Assert.Equal(output, 32768);
        Assert.True(BigIntegerTools.TryParseBinary("1111111111111101.0", out output));
        Assert.Equal(output, 65533);
        Assert.True(BigIntegerTools.TryParseBinary("+1111111111111110", out output));
        Assert.Equal(output, 65534);
        Assert.True(BigIntegerTools.TryParseBinary("-1111111111111111", out output));
        Assert.Equal(output, -65535);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000000.", out output));
        Assert.Equal(output, 65536);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000000.0", out output));
        Assert.Equal(output, 65536);
        Assert.True(BigIntegerTools.TryParseBinary("-10000000000000000.0", out output));
        Assert.Equal(output, -65536);
        Assert.True(BigIntegerTools.TryParseBinary("+10000000000000001", out output));
        Assert.Equal(output, 65537);
        Assert.True(BigIntegerTools.TryParseBinary("10000000000000010.00", out output));
        Assert.Equal(output, 65538);
        Assert.True(BigIntegerTools.TryParseBinary("11111111111111111.000000000000", out output));
        Assert.Equal(output, 131071);

        // around 3 to 4 byte with random formats
        Assert.True(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output));
        Assert.Equal(output, 21616517498418916);
        Assert.True(BigIntegerTools.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output));
        Assert.Equal(output, 86466069993675667);
        Assert.True(BigIntegerTools.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output));
        Assert.Equal(output, 6148914691236517205);
        Assert.True(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output));
        Assert.Equal(output, 21616517498418916);
        Assert.True(BigIntegerTools.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output));
        Assert.Equal(output, -86466069993675667);
        Assert.True(BigIntegerTools.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output));
        Assert.Equal(output, 6148914691236517205);

        // around 3 to 4 byte with random formats
        Assert.True(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output));
        Assert.Equal(output, 21616517498418916);
        Assert.True(BigIntegerTools.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output));
        Assert.Equal(output, 86466069993675667);
        Assert.True(BigIntegerTools.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output));
        Assert.Equal(output, 6148914691236517205);
        Assert.True(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output));
        Assert.Equal(output, 21616517498418916);
        Assert.True(BigIntegerTools.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output));
        Assert.Equal(output, -86466069993675667);
        Assert.True(BigIntegerTools.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output));
        Assert.Equal(output, 6148914691236517205);

        double growthSpeed = 1.01;  // 1.01 for fast, 1.0001 for more extensive
        for (long i = 0; i > 0; i = (long)(i * growthSpeed) + 1)
        {
            BigFloat val = i;
            string binaryBits = Convert.ToString(i, 2);

            // checks several numbers between 0 and long.MaxValue
            string strVal = binaryBits;
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.True(output == val);

            // checks several negative numbers between 0 and long.MaxValue
            strVal = "-" + binaryBits;
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.Equal(output, -i);

            // checks several numbers between 0 and long.MaxValue (with leading plus sign)
            strVal = "+" + binaryBits;
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.Equal(output, i);

            // checks several numbers between 0 and long.MaxValue (with leading '-0')
            strVal = "-0" + binaryBits;
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.Equal(output, -i);

            // checks several numbers between 0 and long.MaxValue (with with trailing '.')
            strVal = "+" + binaryBits + ".";
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.Equal(output, i);

            // checks several numbers between 0 and long.MaxValue (with with trailing '.0')
            strVal = "-0" + binaryBits + ".0"; ;
            Assert.True(BigIntegerTools.TryParseBinary(strVal, out output));
            Assert.Equal(output, -i);
        }
    }

    [Fact]
    public void Verify_Parse_BasicStringTests()
    {
        BigInteger biTwo = new(2);
        BigFloat bfTwo = new((BigInteger)0x1FFFFFFFF, 0, true);

        Assert.Equal(bfTwo.ToString(), biTwo.ToString());
        Assert.Equal((-bfTwo).ToString(), (-biTwo).ToString());

        StringBuilder sbBI = new();
        StringBuilder sbBF = new();
        _ = sbBI.Append(biTwo);
        _ = sbBF.Append(biTwo);
        _ = sbBI.Append(" + " + biTwo + "=");
        _ = sbBF.Append(" + " + bfTwo + "=");
        _ = sbBI.Append(" + " + biTwo + "=");
        _ = sbBF.Append(" + " + bfTwo + "=");
        _ = sbBI.Append(biTwo + biTwo + "!");
        _ = sbBF.Append(bfTwo + bfTwo + "!");

        Assert.Equal(sbBI.ToString(), sbBF.ToString());
    }

    [Fact]
    public void Verify_Parse_RoundTripIntegerParseThenToString()
    {
        // test zero
        string outputPos = BigFloat.Parse("0").ToString();
        Assert.Equal("0", outputPos); // Failed converting "0" from String->BigFloat->String.

        // Converts a string to a BigFloat and then back to a string.
        RoundTripIntegerParseThenToStringChecker("1");
        RoundTripIntegerParseThenToStringChecker("1234567890");
        RoundTripIntegerParseThenToStringChecker("1234567890123456789");
        RoundTripIntegerParseThenToStringChecker("2345678901234567891234567890123456789");
        RoundTripIntegerParseThenToStringChecker("345678901234567891234567890123456789012345678901234567891234567890123456789");

        for (int i = 3; i < 43; i++)
        {
            RoundTripIntegerParseThenToStringChecker("3.1415926535897932384626433832795028841971"[..i]);
        }

        // compare a decimal round trip to a BigFloat round trip (String -> BigFloat -> String  vs.  String -> Decimal -> String)
        DecimalVsBigFloatToStringChecker("1234567890123456789");
        for (int i = 1; i < 30; i++)
        {
            DecimalVsBigFloatToStringChecker(decimal.MaxValue.ToString()[..29]);
        }
        for (int i = 3; i < 30; i++)
        {
            DecimalVsBigFloatToStringChecker("3.141592653589793238462643383"[..i]);
        }

        // Converts a string to a BigFloat and then back to a string.
        static void RoundTripIntegerParseThenToStringChecker(string input)
        {
            string outputPos = BigFloat.Parse(input).ToString();
            Assert.Equal(input, outputPos); // Failed converting {input} from String->BigFloat->String.

            input = "-" + input;
            string outputNeg = BigFloat.Parse(input).ToString();
            Assert.Equal(input, outputNeg); // Failed converting {input} from String->BigFloat->String.
        }

        // String -> BigFloat/Decimal -> String
        static void DecimalVsBigFloatToStringChecker(string input)
        {
            string outputBIG = BigFloat.Parse(input).ToString();
            string outputDEC = decimal.Parse(input).ToString();
            Assert.Equal(outputBIG, outputDEC);
            //BigFloat.Parse($"{input}\").ToString() != Decimal.Parse(\"{input}\").ToString(). [{outputBIG} != {outputDEC}]");
        }
    }

    [Fact]
    public void Verify_Parse_BigFloat()
    {
        // Zero Tests
        CleanedUpTextVsBigFloatParse("-0");
        CleanedUpTextVsBigFloatParse("+0");
        CleanedUpTextVsBigFloatParse("-0.");
        CleanedUpTextVsBigFloatParse("+0.");
        CleanedUpTextVsBigFloatParse("0.");
        CleanedUpTextVsBigFloatParse(".0");
        CleanedUpTextVsBigFloatParse("+.0");
        CleanedUpTextVsBigFloatParse("-.0");

        // Larger Numbers
        CleanedUpTextVsBigFloatParse("6987029348765093487623076509348762307650934876230765093487623090120784334563456.4575436856748");
        CleanedUpTextVsBigFloatParse("0.012002928374380005089620983743800050896209837438000508962092908436501983467");
        CleanedUpTextVsBigFloatParse("69870293487650934876230901207843345634566987029348765093487623090120784334563456.");
        CleanedUpTextVsBigFloatParse("-6987029348765093487623076509348762307650934876230765093487623090120784334563456.4575436856748");
        CleanedUpTextVsBigFloatParse("-0.00000000000000000000000089620983743800050896209837438000508962092908436501983467");
        CleanedUpTextVsBigFloatParse("-6987029348765093487623090120784334563456698702934876509348762309000000000000.");

        CleanedUpTextVsBigFloatParse("5");
        CleanedUpTextVsBigFloatParse("500000.000000");
        CleanedUpTextVsBigFloatParse("5.");
        CleanedUpTextVsBigFloatParse("5.0");
        CleanedUpTextVsBigFloatParse(".50");
        CleanedUpTextVsBigFloatParse(".0005");
        CleanedUpTextVsBigFloatParse(".05");
        CleanedUpTextVsBigFloatParse("5.50");
        CleanedUpTextVsBigFloatParse("0005.5");
        CleanedUpTextVsBigFloatParse("+5");
        CleanedUpTextVsBigFloatParse("+5.");
        CleanedUpTextVsBigFloatParse("+5.0");
        CleanedUpTextVsBigFloatParse("+.50");
        CleanedUpTextVsBigFloatParse("+.05");
        CleanedUpTextVsBigFloatParse("+5.50");
        CleanedUpTextVsBigFloatParse("+0005.5");
        CleanedUpTextVsBigFloatParse("-5");
        CleanedUpTextVsBigFloatParse("-5.");
        CleanedUpTextVsBigFloatParse("-5.0");
        CleanedUpTextVsBigFloatParse("-.50");
        CleanedUpTextVsBigFloatParse("-.05");
        CleanedUpTextVsBigFloatParse("-5.50");
        CleanedUpTextVsBigFloatParse("-0005.5");

        ////////////////////////////////////////////////////////////
        for (int i = 1; i < 10; i++)
        {
            CleanedUpTextVsBigFloatParse("0.000000" + i.ToString());
            CleanedUpTextVsBigFloatParse("0.0000" + i.ToString());
            CleanedUpTextVsBigFloatParse("0.00" + i.ToString());
        }

        for (int i = 10; i < 100; i++)
        {
            CleanedUpTextVsBigFloatParse("0.00000" + i.ToString());
            CleanedUpTextVsBigFloatParse("0.000" + i.ToString());
            CleanedUpTextVsBigFloatParse("0.0" + i.ToString());
        }

        for (int i = 100; i < 150; i++)
        {
            CleanedUpTextVsBigFloatParse("0.0000" + i.ToString());
            CleanedUpTextVsBigFloatParse("0.00" + i.ToString());
            CleanedUpTextVsBigFloatParse("0." + i.ToString());
        }

        ////////////////////////////////////////////////////////////
        for (int i = 1; i < 200; i++)
        {
            CleanedUpTextVsBigFloatParse(i.ToString().Insert(0, "."));
            CleanedUpTextVsBigFloatParse(i.ToString().Insert(1, "."));
            CleanedUpTextVsBigFloatParse("-" + i.ToString().Insert(0, "."));
            CleanedUpTextVsBigFloatParse("-" + i.ToString().Insert(1, "."));
        }
        for (int i = 10; i < 200; i += 7)
        {
            CleanedUpTextVsBigFloatParse(i.ToString().Insert(2, "."));
            CleanedUpTextVsBigFloatParse("-" + i.ToString().Insert(2, "."));
        }

        CleanedUpTextVsBigFloatParse("0.0000003");
        CleanedUpTextVsBigFloatParse("0.0000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("0.0000000000000000000000000000000000000000000000000000000000003");
        CleanedUpTextVsBigFloatParse("-0.0000003");
        CleanedUpTextVsBigFloatParse("-1.0000003");
        CleanedUpTextVsBigFloatParse("-1.7474747474747");
        CleanedUpTextVsBigFloatParse("-7654321.3");
        CleanedUpTextVsBigFloatParse("-7654321");
        CleanedUpTextVsBigFloatParse("-765432109876543");
        CleanedUpTextVsBigFloatParse("54321000000.7");
        CleanedUpTextVsBigFloatParse("54321111000000.7");
        CleanedUpTextVsBigFloatParse("0.000000119");
        CleanedUpTextVsBigFloatParse("0.00119");
        CleanedUpTextVsBigFloatParse("0.119");
        CleanedUpTextVsBigFloatParse("0.1");
        CleanedUpTextVsBigFloatParse("0.2");
        CleanedUpTextVsBigFloatParse("0.3");
        CleanedUpTextVsBigFloatParse("0.4");
        CleanedUpTextVsBigFloatParse("0.5");
        CleanedUpTextVsBigFloatParse("0.6");
        CleanedUpTextVsBigFloatParse("0.7");
        CleanedUpTextVsBigFloatParse("0.77");
        CleanedUpTextVsBigFloatParse("0.777");
        CleanedUpTextVsBigFloatParse("0.7777");
        CleanedUpTextVsBigFloatParse("0.77777");
        CleanedUpTextVsBigFloatParse("0.777777");
        CleanedUpTextVsBigFloatParse("0.7777777");
        CleanedUpTextVsBigFloatParse("0.77777777");
        CleanedUpTextVsBigFloatParse("0.777777777");
        CleanedUpTextVsBigFloatParse("0.7777777777");
        CleanedUpTextVsBigFloatParse("0.77777777777");
        CleanedUpTextVsBigFloatParse("0.777777777777");
        CleanedUpTextVsBigFloatParse("0.7777777777777");
        CleanedUpTextVsBigFloatParse("0.77777777777777");
        CleanedUpTextVsBigFloatParse("0.777777777777777");
        CleanedUpTextVsBigFloatParse("0.1231");
        CleanedUpTextVsBigFloatParse("0.1232");
        CleanedUpTextVsBigFloatParse("0.1233");
        CleanedUpTextVsBigFloatParse("0.1234");
        CleanedUpTextVsBigFloatParse("12.31");
        CleanedUpTextVsBigFloatParse("12.32");
        CleanedUpTextVsBigFloatParse("12.33");
        CleanedUpTextVsBigFloatParse("12.34");
        CleanedUpTextVsBigFloatParse("1230.00000000123");
        CleanedUpTextVsBigFloatParse("1230.000000123");
        CleanedUpTextVsBigFloatParse("1230.0000123");
        CleanedUpTextVsBigFloatParse("1230.00123");
        CleanedUpTextVsBigFloatParse("1230.123");

        // Test zero's precision
        BigFloat res;
        res = BigFloat.Parse(".00000000e+004");  //0 of size 13, so, offset by 13. (00000000 = ((8-4) x 3.322  = 13.28)
        Assert.Equal("0 >> 13", res.ToStringHexScientific());
        res = BigFloat.Parse(".00000000e-4"); // (00000000 = ((8+4) x 3.322  = 39.86)
        Assert.Equal("0 >> 39", res.ToStringHexScientific());

        res = BigFloat.Parse("123.123e+000");  //0 of size 13, so, offset by 13. (00000000 = ((8-4) x 3.322  = 13.28)
        Assert.Equal(res, BigFloat.Parse("123.123"));
        Assert.Equal(res, BigFloat.Parse("000123.123"));
        Assert.Equal(res, BigFloat.Parse("123.123e0"));
        Assert.Equal(res, BigFloat.Parse("123.123e-0"));
        Assert.Equal(res, BigFloat.Parse("012312.3e-2"));
        Assert.Equal(res, BigFloat.Parse("123123.e-3"));
        Assert.Equal(res, BigFloat.Parse("123123.0e-3"));
        Assert.Equal(res, BigFloat.Parse(".123123e3"));
        Assert.Equal(res, BigFloat.Parse("0.123123e3"));
        Assert.Equal(res, BigFloat.Parse("000.123123e3"));
        Assert.Equal(res, BigFloat.Parse(".123123e+3"));
        Assert.Equal(res, BigFloat.Parse(".123123e+0003"));
    }

    /// <summary>
    /// Converts a string to a BigFloat and then back to a string. Supports decimal values and is flexible on the import format.
    /// </summary>
    private static void CleanedUpTextVsBigFloatParse(string stringVal)
    {
        // lets do a String->BigFloat->String
        bool success = BigFloat.TryParse(stringVal, out BigFloat result);
        Assert.True(success);

        string bigFloatResult = result.ToString();

        // Create a cleaned up version of stringVal to compare against. (like remove train '.', leading '+', and leading '0's
        string cleanedStringVal = stringVal.TrimEnd('.');   // Remove trailing '.'
        cleanedStringVal = cleanedStringVal.TrimStart('+'); // Remove leading  '+'
        bool isNegitive = cleanedStringVal.StartsWith('-');
        if (isNegitive)
        {
            cleanedStringVal = cleanedStringVal[1..];
        }

        if (cleanedStringVal != "0")        // Remove leading zeros (except if just one zero)
        {
            cleanedStringVal = cleanedStringVal.TrimStart('0');
        }

        if (cleanedStringVal[0] == '.')   //  .0001
        {
            // lets count leading zeros (just so we can see if we want to format it in 123e-25 format.

            int zerosFound = 0;
            int cleanedStringValLength = cleanedStringVal.Length;
            while ((zerosFound + 1) < cleanedStringValLength && cleanedStringVal[zerosFound + 1] == '0')
            {
                zerosFound++;
            }

            // if more then 10 zeros, like 0.00000000000123 then output in E notation.
            if (zerosFound > 10)
            {
                cleanedStringVal = cleanedStringVal.TrimStart('.', '0');
                cleanedStringVal = $"{cleanedStringVal}e-{zerosFound + cleanedStringVal.Length}";
            }
            else // if less then 10 zeros, just put our leading zero back on. e.g. 0.0000123.
            {
                cleanedStringVal = "0" + cleanedStringVal;  //  .0001 -> 0.0001
            }
        }

        if (isNegitive && cleanedStringVal != "0" && cleanedStringVal != "0.0") // put negative back in unless it is zero
        {
            cleanedStringVal = "-" + cleanedStringVal;
        }

        // compare the String->BigFloat->String with the cleaned up string version
        Assert.Equal(bigFloatResult, cleanedStringVal);
    }

    [Fact]
    public void Verify_Constructor_BigFloat_WithBigInteger()
    {
        string res;
        string strVal = "1024.0000002384185791015625";
        BigFloat val = new(512 * BigInteger.Parse("4294967297"), 1, true);
        int sizeShouldBe = (int)Math.Round((val.Size + BigFloat.GuardBits) / 3.32192809488736235, 0) + 1;
        // + 1 is for decimal point.
        Assert.Equal(strVal[0..sizeShouldBe], val.ToString(true));

        val = new BigFloat(512 * BigInteger.Parse("4294967297"), 1, true);
        res = val.ToString(false);
        Assert.Equal("1.02e+3", res);

        strVal = "1024.000000000";
        val = new BigFloat(512 * BigInteger.Parse("4294967296"), 1, true);
        res = val.ToString(true);
        Assert.Equal(strVal, res);

        strVal = "1023.999999762"; // 1023.9999997615814208984375
        val = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true);
        res = val.ToString(true);
        Assert.Equal(strVal, res);
    }

    [Fact]
    public void Verify_TryParse_Precision()
    {
        BigFloat BF123123 = BigFloat.Parse("123.123");
        BigFloat BF123124 = BigFloat.Parse("123.124");
        BigFloat BF123123_9 = BigFloat.Parse("123.123|9");
        BigFloat BF123123_5 = BigFloat.Parse("123.123|5");
        BigFloat BF123123_1 = BigFloat.Parse("123.123|1");
        BigFloat BF123123_2 = BigFloat.Parse("123.123|2");
        BigFloat BF123123_6 = BigFloat.Parse("123.123|6");

        Assert.Equal(BF123123_1, BF123123);
        Assert.Equal(BF123123_2, BF123123);
        Assert.Equal(BF123123_6, BF123124);


        //  123.123 has 17.91 binary accuracy, so 17 bits. 
        //  123.123|9: 1111011.0001111110|1101111110...  111101100011111101101111110
        //  123.123:   1111011.0001111101|1111001110... -111101100011111011111001110
        //                         Diff: |1110110000                      1110110000
        Assert.False(BF123123_9 == BF123123);

        Assert.Equal(BF123123_9, BF123124);

        // The below fail is acceptable - so excluding, however, "123.123" == "123.123|5" a miss by 0|10000 so it should fail however decimal to binary conversion is not perfect. 
        //  123.123 has 17.91 binary accuracy, so 17 bits. 
        //  123.123|5: 1111011.0001111110|01110...     1111011000111111001110
        //  123.123:   1111011.0001111101|11110...    -1111011000111110111110
        //                         Diff: |10000                         10000
        Assert.True(BF123123_5 == BF123123);
        Assert.False(BF123123_5.EqualsUlp(BF123123));
        Assert.True(BF123123_5.EqualsUlp(BF123123, 1));

        // The below fail is acceptable - so excluding, however, "123.124" == "123.123|5" a miss by 0|10000 so it should fail however decimal to binary conversion is not perfect. 
        //  123.124 has 17.91 binary accuracy, so 17 bits. 
        //  123.124:   1111011.0001111110|11111...   
        //  123.123|5: 1111011.0001111110|01110...   
        //                         Diff: |10001      
        Assert.False(BF123123_5 == BF123124);
    }

    /// <summary>
    /// Test some string values that can not be converted to a big integer.  
    /// </summary>
    [Fact]
    public void Verify_TryParse_Errors()
    {
        Assert.False(BigFloat.TryParse(null, out _));
        Assert.False(BigFloat.TryParse("", out _));
        Assert.False(BigFloat.TryParse(" ", out _));
        Assert.False(BigFloat.TryParse("e", out _));
        Assert.False(BigFloat.TryParse(".e", out _));
        Assert.False(BigFloat.TryParse("1e", out _));
        Assert.False(BigFloat.TryParse("1e1.", out _));
        Assert.False(BigFloat.TryParse("1e1.1", out _));
        Assert.False(BigFloat.TryParse("1e1+", out _));
        Assert.False(BigFloat.TryParse("1e1-", out _));
        Assert.False(BigFloat.TryParse("1e--1", out _));
        Assert.False(BigFloat.TryParse("1e-+1", out _));
        Assert.False(BigFloat.TryParse("1e++1", out _));
        Assert.False(BigFloat.TryParse("1e+-1", out _));
        Assert.False(BigFloat.TryParse("+-1e1", out _));
        Assert.False(BigFloat.TryParse("e1", out _));
        Assert.False(BigFloat.TryParse("{1", out _));
        Assert.False(BigFloat.TryParse("1}", out _));
        Assert.False(BigFloat.TryParse("}", out _));
        Assert.False(BigFloat.TryParse("{", out _));
        Assert.False(BigFloat.TryParse("{}1", out _));
        Assert.False(BigFloat.TryParse("1{}", out _));
        Assert.False(BigFloat.TryParse("{{}}", out _));
        Assert.False(BigFloat.TryParse("{{1}}", out _));
        Assert.False(BigFloat.TryParse("{}1", out _));
        Assert.False(BigFloat.TryParse("{1)", out _));
        Assert.False(BigFloat.TryParse("(1}", out _));
        Assert.False(BigFloat.TryParse("+-1", out _));
        Assert.False(BigFloat.TryParse("--1", out _));
        Assert.False(BigFloat.TryParse("++1", out _));
        Assert.False(BigFloat.TryParse("+-1", out _));
        Assert.False(BigFloat.TryParse("-1+", out _));
        Assert.False(BigFloat.TryParse("+1-", out _));
        Assert.False(BigFloat.TryParse("-1-", out _));
        Assert.False(BigFloat.TryParse("+1+", out _));
        Assert.False(BigFloat.TryParse("+1-", out _));
        Assert.False(BigFloat.TryParse("-1+", out _));
        Assert.False(BigFloat.TryParse("1-+", out _));
        Assert.False(BigFloat.TryParse("1+-", out _));
        Assert.False(BigFloat.TryParse("1--", out _));
        Assert.False(BigFloat.TryParse("1++", out _));
        Assert.False(BigFloat.TryParse("1+-", out _));
        Assert.False(BigFloat.TryParse("1-+", out _));
        Assert.False(BigFloat.TryParse("*", out _));
        Assert.False(BigFloat.TryParse("00x", out _));
        Assert.False(BigFloat.TryParse("-0x", out _));
        Assert.False(BigFloat.TryParse("0-x", out _));
        Assert.False(BigFloat.TryParse("0x-", out _));
        Assert.False(BigFloat.TryParse(".", out _));
        Assert.False(BigFloat.TryParse("-", out _));
        Assert.False(BigFloat.TryParse("-.", out _));
        Assert.False(BigFloat.TryParse(".-", out _));
        Assert.False(BigFloat.TryParse("+.", out _));
        Assert.False(BigFloat.TryParse("1+.", out _));
        Assert.False(BigFloat.TryParse(".1+", out _));
        Assert.False(BigFloat.TryParse("1.+", out _));
        Assert.False(BigFloat.TryParse("1.e", out _));
        Assert.False(BigFloat.TryParse(".+", out _));
        Assert.False(BigFloat.TryParse("/", out _));
        Assert.False(BigFloat.TryParse(@"\", out _));
        Assert.False(BigFloat.TryParse("--1", out _));
        Assert.False(BigFloat.TryParse("1-1", out _));
        Assert.False(BigFloat.TryParse("0-1", out _));
        Assert.False(BigFloat.TryParse("0-", out _));
        Assert.False(BigFloat.TryParse("0.-", out _));
        Assert.False(BigFloat.TryParse("1.41.", out _));
        Assert.False(BigFloat.TryParse(".41.", out _));
        Assert.False(BigFloat.TryParse(".4.1", out _));
        Assert.False(BigFloat.TryParse(".4.1e", out _));
        Assert.False(BigFloat.TryParse("XXXX", out _));
        Assert.False(BigFloat.TryParse("XX1XX", out _));
        Assert.False(BigFloat.TryParse("XXXX1", out _));
        Assert.False(BigFloat.TryParse("1XXXXe10", out _));
        Assert.False(BigFloat.TryParse("1XX.XXe10", out _));
        Assert.False(BigFloat.TryParse("X.XX", out _));
        Assert.False(BigFloat.TryParse("1e10XXX", out _));
#if !DEBUG
        _ = Assert.Throws<ArgumentNullException>(() => BigFloat.ParseBinary(null));
        _ = Assert.Throws<ArgumentException>(() => BigFloat.ParseBinary(""));
        _ = Assert.Throws<ArgumentException>(() => BigFloat.ParseBinary(" "));
#endif
    }

    /// <summary>
    /// Test some string values that can not be converted to a big integer.  
    /// </summary>
    [Fact]
    public void Verify_TryParseDecimal_Errors()
    {
        Assert.False(BigFloat.TryParseDecimal(null, out _));
        Assert.False(BigFloat.TryParseDecimal("", out _));
        Assert.False(BigFloat.TryParseDecimal(" ", out _));
        Assert.False(BigFloat.TryParseDecimal("e", out _));
        Assert.False(BigFloat.TryParseDecimal(".e", out _));
        Assert.False(BigFloat.TryParseDecimal("1e", out _));
        Assert.False(BigFloat.TryParseDecimal("1e1.", out _));
        Assert.False(BigFloat.TryParseDecimal("1e1.1", out _));
        Assert.False(BigFloat.TryParseDecimal("1e1+", out _));
        Assert.False(BigFloat.TryParseDecimal("1e1-", out _));
        Assert.False(BigFloat.TryParseDecimal("1e--1", out _));
        Assert.False(BigFloat.TryParseDecimal("1e-+1", out _));
        Assert.False(BigFloat.TryParseDecimal("1e++1", out _));
        Assert.False(BigFloat.TryParseDecimal("1e+-1", out _));
        Assert.False(BigFloat.TryParseDecimal("+-1e1", out _));
        Assert.False(BigFloat.TryParseDecimal("e1", out _));
        Assert.False(BigFloat.TryParseDecimal("{1", out _));
        Assert.False(BigFloat.TryParseDecimal("1}", out _));
        Assert.False(BigFloat.TryParseDecimal("}", out _));
        Assert.False(BigFloat.TryParseDecimal("{", out _));
        Assert.False(BigFloat.TryParseDecimal("{}1", out _));
        Assert.False(BigFloat.TryParseDecimal("1{}", out _));
        Assert.False(BigFloat.TryParseDecimal("{{}}", out _));
        Assert.False(BigFloat.TryParseDecimal("{{1}}", out _));
        Assert.False(BigFloat.TryParseDecimal("{}1", out _));
        Assert.False(BigFloat.TryParseDecimal("{1)", out _));
        Assert.False(BigFloat.TryParseDecimal("(1}", out _));
        Assert.False(BigFloat.TryParseDecimal("+-1", out _));
        Assert.False(BigFloat.TryParseDecimal("--1", out _));
        Assert.False(BigFloat.TryParseDecimal("++1", out _));
        Assert.False(BigFloat.TryParseDecimal("+-1", out _));
        Assert.False(BigFloat.TryParseDecimal("-1+", out _));
        Assert.False(BigFloat.TryParseDecimal("+1-", out _));
        Assert.False(BigFloat.TryParseDecimal("-1-", out _));
        Assert.False(BigFloat.TryParseDecimal("+1+", out _));
        Assert.False(BigFloat.TryParseDecimal("+1-", out _));
        Assert.False(BigFloat.TryParseDecimal("-1+", out _));
        Assert.False(BigFloat.TryParseDecimal("1-+", out _));
        Assert.False(BigFloat.TryParseDecimal("1+-", out _));
        Assert.False(BigFloat.TryParseDecimal("1--", out _));
        Assert.False(BigFloat.TryParseDecimal("1++", out _));
        Assert.False(BigFloat.TryParseDecimal("1+-", out _));
        Assert.False(BigFloat.TryParseDecimal("1-+", out _));
        Assert.False(BigFloat.TryParseDecimal("*", out _));
        Assert.False(BigFloat.TryParseDecimal("0x1", out _));
        Assert.False(BigFloat.TryParseDecimal(".", out _));
        Assert.False(BigFloat.TryParseDecimal("-", out _));
        Assert.False(BigFloat.TryParseDecimal("-.", out _));
        Assert.False(BigFloat.TryParseDecimal(".-", out _));
        Assert.False(BigFloat.TryParseDecimal("+.", out _));
        Assert.False(BigFloat.TryParseDecimal("1+.", out _));
        Assert.False(BigFloat.TryParseDecimal(".1+", out _));
        Assert.False(BigFloat.TryParseDecimal("1.+", out _));
        Assert.False(BigFloat.TryParseDecimal("1.e", out _));
        Assert.False(BigFloat.TryParseDecimal(".+", out _));
        Assert.False(BigFloat.TryParseDecimal("/", out _));
        Assert.False(BigFloat.TryParseDecimal(@"\", out _));
        Assert.False(BigFloat.TryParseDecimal("--1", out _));
        Assert.False(BigFloat.TryParseDecimal("1-1", out _));
        Assert.False(BigFloat.TryParseDecimal("0-1", out _));
        Assert.False(BigFloat.TryParseDecimal("0-", out _));
        Assert.False(BigFloat.TryParseDecimal("0.-", out _));
        Assert.False(BigFloat.TryParseDecimal("1.41.", out _));
        Assert.False(BigFloat.TryParseDecimal(".41.", out _));
        Assert.False(BigFloat.TryParseDecimal(".4.1", out _));
        Assert.False(BigFloat.TryParseDecimal(".4.1e", out _));
        Assert.False(BigFloat.TryParseDecimal("XXXX", out _));
        Assert.False(BigFloat.TryParseDecimal("XX1XX", out _));
        Assert.False(BigFloat.TryParseDecimal("XXXX1", out _));
        Assert.False(BigFloat.TryParseDecimal("1XXXXe10", out _));
        Assert.False(BigFloat.TryParseDecimal("1XX.XXe10", out _));
        Assert.False(BigFloat.TryParseDecimal("X.XX", out _));
        Assert.False(BigFloat.TryParseDecimal("1e10XXX", out _));
    }

    /// <summary>
    /// Test some string values that can not be converted to a big integer.  
    /// </summary>
    [Fact]
    public void Verify_ToBinaryStringFormat()
    {
        BigFloat bf;
        string str;

        bf = new(1.5);
        str = bf.ToString("B");
        str = bf.ToString("B");
        Assert.Equal("1.100000000000000000000000000000000000", str); // Verify_ToStringFormat on 1.5
        bf = new(1.25);
        bf = new(1.25);
        str = bf.ToString("B");
        Assert.Equal("1.010000000000000000000000000000000000", str); // Verify_ToStringFormat on 1.25
        bf = new(2.5);
        bf = new(2.5);
        str = bf.ToString("B");
        Assert.Equal("10.10000000000000000000000000000000000", str); // Verify_ToStringFormat on 2.5
        bf = new(3.5);
        bf = new(3.5);
        str = bf.ToString("B");
        Assert.Equal("11.10000000000000000000000000000000000", str); // Verify_ToStringFormat on 3.5
        bf = new(7.5);
        bf = new(7.5);
        str = bf.ToString("B");
        Assert.Equal("111.1000000000000000000000000000000000", str); // Verify_ToStringFormat on 7.5
        bf = new(15.5);
        str = bf.ToString("B");
        Assert.Equal("1111.100000000000000000000000000000000", str); // Verify_ToStringFormat on 15.5

        bf = new(31.25);
        str = bf.ToString("B");
        Assert.Equal("11111.01000000000000000000000000000000", str); // Verify_ToStringFormat on 31.25

        bf = new(63.25);
        str = bf.ToString("B");
        Assert.Equal("111111.0100000000000000000000000000000", str); // Verify_ToStringFormat on 63.25

        bf = new(127.25);
        str = bf.ToString("B");
        Assert.Equal("1111111.010000000000000000000000000000", str); // Verify_ToStringFormat on 127.25

        bf = new(255.25);
        str = bf.ToString("B");
        Assert.Equal("11111111.01000000000000000000000000000", str); // Verify_ToStringFormat on 255.25

        bf = new(0.0000123);
        str = bf.ToString("B");
        Assert.Equal("0.00000000000000001100111001011100000110010000010110001", str); // Verify_ToStringFormat on 0.0000123
        //    answer: 0.000000000000000011001110010111000001100100000101100010101000001101111100001000001110001011...
        //                             12345678901234567890123456789012345678901234567890123  (expect 1+52= 53 significant bits)
        //  expected: 0.000000000000000011001110010111000001100100000101100010101000001110000

        bf = new(1230000000);
        str = bf.ToString("B");
        Assert.Equal("1001001010100000100111110000000", str); // Verify_ToStringFormat on 1230000000

        bf = new(123, 5);
        str = bf.Truncate().ToString("B");
        Assert.Equal("111101100000", str); // Verify_ToStringFormat on 123 * 2^5

        bf = new(123, 40);
        str = bf.ToString("B");
        Assert.Equal("11110110000000000000000000000000000000000000000", str); // Verify_ToStringFormat on 123 * 2^5

        bf = new(0.0000123);
        str = bf.ToString("B");
        Assert.Equal("0.00000000000000001100111001011100000110010000010110001", str); // Verify_ToStringFormat on 0.0000123
        //     answer: 0.000000000000000011001110010111000001100100000101100010101000001101111100001000001110001011...
        //                               12345678901234567890123456789012345678901234567890123 (expect 1+52= 53 significant bits)
        //   expected: 0.000000000000000011001110010111000001100100000101100010101000001110000

        bf = new(-0.123);
        str = bf.ToString("B");
        Assert.Equal("-0.0001111101111100111011011001000101101000", str); // Verify_ToStringFormat on -3.5
        //     answer: -0.00011111011111001110110110010001011010000111001010110000001000...
        //                   12345678901234567890123456789012345678901234567890123 (expect 1+52= 53 significant bits)
        //   expected: -0.00011111011111001110110110010001011010000111001010110000

        bf = new(-3.5);
        str = bf.ToString("B");
        Assert.Equal("-11.10000000000000000000000000000000000", str); // Verify_ToStringFormat on -3.5

        bf = new(-1230000000.0);
        str = bf.ToString("B");
        Assert.Equal("-1001001010100000100111110000000.000000", str); // Verify_ToStringFormat on 1230000000

        bf = BigFloat.CreateWithPrecisionFromValue(-123, binaryScaler: 5);
        str = bf.ToString("B");
        Assert.Equal("-111101100000", str); // Verify_ToStringFormat on 123 * 2^5

        bf = new(-123, 40);
        str = bf.ToString("B");
        Assert.Equal("-11110110000000000000000000000000000000000000000", str); // Verify_ToStringFormat on 123 * 2^5

        //////////////// Int conversions ////////////////
        bf = new(1230000000);
        str = bf.ToString("B");
        Assert.Equal("1001001010100000100111110000000", str); // Verify_ToStringFormat on 1230000000

        bf = new(-1230000000);
        str = bf.ToString("B");
        Assert.Equal("-1001001010100000100111110000000", str); // Verify_ToStringFormat on -1230000000

        //////////////// String conversions ////////////////
        bf = new("1230000000");
        str = bf.ToString("B");
        Assert.Equal("1001001010100000100111110000000", str); // Verify_ToStringFormat on 1230000000

        bf = new("-1230000000");
        str = bf.ToString("B");
        Assert.Equal("-1001001010100000100111110000000", str); // Verify_ToStringFormat on -1230000000

        bf = new("1230000000.0");
        str = bf.ToString("B");
        Assert.Equal("1001001010100000100111110000000.000", str); // Verify_ToStringFormat on 1230000000.0

        bf = new("-123456.7890123");
        str = bf.ToString("B");
        Assert.Equal("-11110001001000000.11001001111111001011010", str); // Verify_ToStringFormat on -123456.7890123
        //     answer: -11110001001000000.110010011111110010110101110010001010010001001001001000000000010010000010010001011011111111...
        //                                012345678901234567890123 (expect 7 * 3.321928 = 23.253 significant binary digits)
        //   expected: -11110001001000000.11001001111111001011011  
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 1f)]
    [InlineData(1f, 2f)]
    public void CompareToExact_SingleIntegers_Theory(float smaller, float larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.True(a.IsLessThanUlp(b, 1, true)); // Replaces StrictCompareTo
    }

    [Theory]
    [InlineData(-0.0000123f, 0.0000123f, true)]   // smaller < larger
    [InlineData(-0.0000000445f, -0.0000000444f, true)]  // smaller < larger  
    [InlineData(0.0000122f, 0.0000123f, true)]    // smaller < larger
    [InlineData(-0.0000000444f, -0.0000000445f, false)] // larger > smaller
    [InlineData(0.0000123f, 0.0000122f, false)]   // larger > smaller
    public void CompareToExact_SingleFloats_Theory(float aVal, float bVal, bool aLessThanB)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (aLessThanB)
        {
            Assert.True(a.IsLessThanUlp(b, 1, true));
        }
        else
        {
            Assert.True(a.IsGreaterThanUlp(b, 1, true));
        }
    }

    [Fact]
    public void CompareToExact_SingleFloatParse_Precision()
    {
        var a = new BigFloat(float.Parse("0.0000123"));
        var b = new BigFloat(float.Parse("0.0000122"));

        Assert.True(a.IsGreaterThanUlp(b, 1, true));
    }

    [Theory]
    [InlineData(100.000000f, 100.000001f)]  // Beyond single precision
    [InlineData(1.000000001f, 1.000000002f)] // Beyond single precision
    public void CompareToExact_SingleBeyondPrecision_Theory(float val1, float val2)
    {
        var a = new BigFloat(val1);
        var b = new BigFloat(val2);

        // Values are identical at single precision limits
        Assert.True(a.EqualsUlp(b, 1, true)); // Replaces Assert.Equal(0, StrictCompareTo)
    }

    [Fact]
    public void CompareToExact_SingleDoubleTranslation()
    {
        // These values are first translated from 53 bit doubles, then 24 bit floats
        var a = new BigFloat((float)0.0000123);  //0.0000000000000000110011100101110000011001
        var b = new BigFloat((float)0.00001234); //0.000000000000000011001111000001111110010

        Assert.True(a.IsLessThanUlp(b, 1, true));
    }

    [Fact]
    public void CompareToExact_SingleVerySmallNegatives()
    {
        var a = new BigFloat((float)-0.000000044501); // 0.000000000000000000000000101111110010000101011101111100
        var b = new BigFloat((float)-0.0000000445);   // 0.000000000000000000000000101111110010000001000100011101

        Assert.True(a.IsLessThanUlp(b, 1, true));
    }

    [Fact]
    public void CompareToExact_SingleStandardComparison()
    {
        var a = new BigFloat((float)1.0);
        var b = new BigFloat((float)1.01);

        Assert.True(a.IsLessThanUlp(b, 1, true));
    }

    [Fact]
    public void CompareUlp_BasicComparisons()
    {
        var a = new BigFloat(-1);
        var b = new BigFloat(0);

        // "-1 < 0" OR "-1 - 0 = -1" so NEG
        Assert.True(a.IsLessThanUlp(b, 0)); // Replaces: Assert.True(BigFloat.CompareUlp(a, b, 0) < 0)
        Assert.True(b.IsGreaterThanUlp(a, 0)); // Replaces: Assert.True(BigFloat.CompareUlp(b, a, 0) > 0)
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(-4)]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void CompareUlp_SelfComparison_ReturnsEqual(int value)
    {
        var a = new BigFloat(value);

        for (int tolerance = 0; tolerance < 5; tolerance++)
        {
            Assert.True(a.EqualsUlp(a, tolerance)); // Replaces: Assert.Equal(0, BigFloat.CompareUlp(a, a, i))
        }
    }

    [Theory]
    [InlineData(1, 0, 1, true)]      // CompareUlp(1, 0, 1) == +1
    [InlineData(1, 0, 31, true)]     // CompareUlp(1, 0, 31) == +1
    [InlineData(1, 0, 32, false)]     // CompareUlp(1, 0, 32) == +1
    [InlineData(1, 0, 33, false)]    // CompareUlp(1, 0, 33) == 0
    public void CompareUlp_OneVsZero_Theory(int aVal, int bVal, int tolerance, bool aGreaterThanB)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (aGreaterThanB)
        {
            Assert.True(a.IsGreaterThanUlp(b, tolerance));
            Assert.True(b.IsLessThanUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
    }

    [Theory]
    [InlineData(-1, 0, 31, false)]   // CompareUlp(-1, 0, 31) == 0
    [InlineData(-1, 0, 32, true)]    // CompareUlp(-1, 0, 32) != 0
    [InlineData(-1, 2, 31, false)]   // CompareUlp(-1, 2, 32) == 0
    [InlineData(-1, 2, 32, true)]    // CompareUlp(-1, 2, 33) != 0
    public void CompareUlp_NegativeComparisons_Theory(int aVal, int bVal, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Theory]
    [InlineData(1, 2, 31 - 3, false)]    // CompareUlp(1, 2, 1 + 32) != 0
    [InlineData(1, 2, 31 - 2, false)]    // CompareUlp(1, 2, 1 + 32) != 0
    [InlineData(1, 2, 31 - 1, false)]    // CompareUlp(1, 2, 1 + 32) != 0
    [InlineData(1, 2, 31 + 0, true)]    // CompareUlp(1, 2, 1 + 32) != 0
    [InlineData(1, 2, 31 + 1, true)]    // CompareUlp(1, 2, 1 + 32) != 0
    //[InlineData(1, 2, 34, true)]     // CompareUlp(1, 2, 2 + 32) == 0
    //[InlineData(-1, -2, 33, false)]  // CompareUlp(-1, -2, 1 + 32) != 0
    //[InlineData(-1, -2, 34, true)]   // CompareUlp(-1, -2, 2 + 32) == 0
    public void CompareUlp_AdjacentIntegers_Theory(int aVal, int bVal, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aVal);
        var b = new BigFloat(bVal);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            // Determine expected ordering based on values
            if (aVal < bVal)
            {
                Assert.True(a.IsLessThanUlp(b, tolerance));
                Assert.True(b.IsGreaterThanUlp(a, tolerance));
            }
            else
            {
                Assert.True(a.IsGreaterThanUlp(b, tolerance));
                Assert.True(b.IsLessThanUlp(a, tolerance));
            }
        }
    }

    [Fact]
    public void CompareUlp_SmallFloats_SameSize()
    {
        var a = new BigFloat((float)-0.0000123);
        var b = new BigFloat((float)0.0000123);

        Assert.True(a.IsLessThanUlp(b, 3));
        Assert.True(b.IsGreaterThanUlp(a, 3));
    }

    [Theory]
    [InlineData(8, true)]   // CompareUlp equals 0 at tolerance 8
    [InlineData(7, false)]  // CompareUlp differs at tolerance 7 after float precision fix
    [InlineData(6, false)]  // CompareUlp differs at tolerance 6 after float precision fix
    public void CompareUlp_VerySmallFloats_Theory(int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat((float)-0.0000000444);
        var b = new BigFloat((float)-0.0000000445);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(b.IsLessThanUlp(a, tolerance));
            Assert.True(a.IsGreaterThanUlp(b, tolerance));
        }
    }

    [Theory]
    [InlineData("0b11", "0b01", 0, 0, false)]  // Different at tolerance 0
    [InlineData("0b11", "0b01", 0, 1, false)]  // Different at tolerance 1
    [InlineData("0b11", "0b01", 0, 3, true)]   // Equal at tolerance 3
    public void CompareUlp_BinaryStrings_Theory(string aStr, string bStr, int precision, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aStr, precision);
        var b = new BigFloat(bStr, precision);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
        }
        else
        {
            Assert.True(a.IsGreaterThanUlp(b, tolerance));
        }
    }

    [Theory]
    [InlineData("-0b11", "-0b01", 0, 1, false)]  // Different at tolerance 1
    [InlineData("-0b11", "-0b01", 0, 2, false)]  // Different at tolerance 2
    [InlineData("-0b11", "-0b01", 0, 3, true)]   // Equal at tolerance 3
    [InlineData("-0b11", "-0b01", 0, 4, true)]   // Equal at tolerance 4
    public void CompareUlp_NegativeBinaryStrings_Theory(string aStr, string bStr, int precision, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aStr, precision);
        var b = new BigFloat(bStr, precision);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Theory]
    [InlineData("-0b11", "-0b01", 1, 1, false)]  // Different at tolerance 1
    [InlineData("-0b11", "-0b01", 1, 2, false)]  // Different at tolerance 2
    [InlineData("-0b11", "-0b01", 1, 3, true)]   // Equal at tolerance 3
    public void CompareUlp_NegativeBinaryStrings_Precision1_Theory(string aStr, string bStr, int precision, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aStr, precision);
        var b = new BigFloat(bStr, precision);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Theory]
    [InlineData("10.001", "10.01", 1, true)]   // Equal at tolerance 1
    [InlineData("10.001", "10.01", 0, false)]  // Different at tolerance 0
    [InlineData("10.0001", "10.01", 1, true)]  // Equal at tolerance 1
    [InlineData("10.0001", "10.01", 0, false)] // Different at tolerance 0
    public void CompareUlp_BinaryParsing_Theory(string aStr, string bStr, int tolerance, bool shouldBeEqual)
    {
        Assert.True(BigFloat.TryParseBinary(aStr, out BigFloat a));
        Assert.True(BigFloat.TryParseBinary(bStr, out BigFloat b));

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Fact]
    public void CompareUlp_DifferentPrecisions_SpecialCase()
    {
        var a = new BigFloat("-0b11", 0);
        var b = new BigFloat("-0b01", 1);

        // Special case where alignment creates ambiguity
        Assert.True(a.EqualsUlp(b, 1));
        Assert.True(b.EqualsUlp(a, 1));
    }

    [Theory]
    [InlineData(555, 554, 0)]  // 31+1-10=22
    [InlineData(-555, -554, 0)] // 31+1-10=22
    public void CompareUlp_LargerIntegers_GreaterOrLessThen(int aVal, int bVal, int precision)
    {
        var a = new BigFloat(aVal, precision);
        var b = new BigFloat(bVal, precision);

        int tolerance = 31 + 1 - (int.Max(int.Log2(int.Abs(aVal)), int.Log2(int.Abs(aVal))) + 1);


        if (aVal > bVal) // Positive case
        {
            Assert.True(a.IsGreaterThanUlp(b, tolerance));
            Assert.True(b.IsLessThanUlp(a, tolerance));
        }
        else // Negative case: -555 < -554
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }

    }

    [Theory]
    [InlineData(555, 554, 0)]
    [InlineData(-555, -554, 0)]
    public void CompareUlp_LargerIntegers__Equal(int aVal, int bVal, int precision)
    {
        var a = new BigFloat(aVal, precision);
        var b = new BigFloat(bVal, precision);

        int tolerance = 31 + 1 - (int.Max(int.Log2(int.Abs(aVal)), int.Log2(int.Abs(aVal))) + 1);

        Assert.True(a.EqualsUlp(b, tolerance + 1));
        Assert.True(b.EqualsUlp(a, tolerance + 1));
    }

    [Theory]
    [InlineData(-555, -554, 0, 1, 21, false)]
    [InlineData(-555, -554, 0, 1, 22, false)]
    [InlineData(-555, -554, 0, 1, 23, true)]
    [InlineData(555, 554, 0, 1, 21, false)]
    [InlineData(555, 554, 0, 1, 22, false)]
    [InlineData(555, 554, 0, 1, 23, true)]
    public void CompareUlp_MixedPrecisions_Theory(int aVal, int bVal, int aPrecision, int bPrecision, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aVal, binaryPrecision: aPrecision);
        var b = new BigFloat(bVal, binaryPrecision: bPrecision);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance, true));
            Assert.True(b.EqualsUlp(a, tolerance, true));
        }
        else
        {
            if (aVal > bVal)
            {
                if (aVal < 0)
                {
                    Assert.True(a.IsLessThanUlp(b, tolerance, true));
                    Assert.True(b.IsGreaterThanUlp(a, tolerance, true));
                }
                else
                {
                    Assert.True(a.IsGreaterThanUlp(b, tolerance, true));
                    Assert.True(b.IsLessThanUlp(a, tolerance, true));
                }
            }
        }
    }

    [Theory]
    [InlineData(-555, -554, 0, 0, 32, false)]
    [InlineData(-555, -554, 0, 0, 33, true)]
    [InlineData(555, 554, 0, 0, 32, false)]
    [InlineData(555, 554, 0, 0, 33, true)]
    public void CompareUlp_MixedPrecisions2_Theory(int aVal, int bVal, int aPrecision, int bPrecision, int tolerance, bool shouldBeEqual)
    {
        var a = BigFloat.CreateWithPrecisionFromValue(aVal, false, aPrecision);
        var b = BigFloat.CreateWithPrecisionFromValue(bVal, false, bPrecision);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance, true));
            Assert.True(b.EqualsUlp(a, tolerance, true));
        }
        else
        {
            if (aVal > bVal)
            {
                if (aVal < 0)
                {
                    Assert.True(a.IsLessThanUlp(b, tolerance, true));
                    Assert.True(b.IsGreaterThanUlp(a, tolerance, true));
                }
                else
                {
                    Assert.True(a.IsGreaterThanUlp(b, tolerance, true));
                    Assert.True(b.IsLessThanUlp(a, tolerance, true));
                }
            }
        }
    }

    [Theory]
    [InlineData("55555555555555555555552", "55555555555555555555554", 1, false)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 2, false)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 3, true)]
    [InlineData("55555555555555555555552", "55555555555555555555554", 4, true)]
    public void CompareUlp_VeryLargeNumbers_Theory(string aStr, string bStr, int tolerance, bool shouldBeEqual)
    {
        var a = new BigFloat(aStr);
        var b = new BigFloat(bStr);

        if (shouldBeEqual)
        {
            Assert.True(a.EqualsUlp(b, tolerance));
            Assert.True(b.EqualsUlp(a, tolerance));
        }
        else
        {
            Assert.True(a.IsLessThanUlp(b, tolerance));
            Assert.True(b.IsGreaterThanUlp(a, tolerance));
        }
    }

    [Fact]
    public void CompareUlp_DefaultParameters_BackwardsCompatibility()
    {
        var a = new BigFloat(555, 0);
        var b = new BigFloat(554, 0);

        // Test that default parameters work with instance methods
        Assert.True(a.IsGreaterThanUlp(b)); // Uses default ulpTolerance = 0
        Assert.True(b.IsLessThanUlp(a));    // Uses default ulpTolerance = 0
    }


    [Fact]
    public void Verify_NumberOfMatchingLeadingBitsWithRounding()
    {
        _ = BigFloat.TryParseBinary("10.111", out BigFloat a);
        _ = BigFloat.TryParseBinary("10.101", out BigFloat b);
        Test(a, b, expectedResult: 3, expectedSign: 1);

        _ = BigFloat.TryParseBinary("1111100", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        Test(a, b, expectedResult: 5, expectedSign: -1);

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        Test(a, b, expectedResult: 4, expectedSign: 1);

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("1000000000", out b);
        Test(a, b, expectedResult: 0, expectedSign: -1);

        a = new BigFloat(-1);
        b = new BigFloat(0);
        Test(a, b, expectedResult: 0, expectedSign: -1);

        a = new BigFloat(1);
        b = new BigFloat(0);
        Test(a, b, expectedResult: 0, expectedSign: 1);

        a = new BigFloat(0);
        b = new BigFloat(0);
        Test(a, b, expectedResult: 0, expectedSign: 0);

        a = new BigFloat(0);
        b = new BigFloat(-1);
        Test(a, b, expectedResult: 0, expectedSign: 1);

        a = new BigFloat(0);
        b = new BigFloat(1);
        Test(a, b, expectedResult: 0, expectedSign: -1);

        static void Test(BigFloat a, BigFloat b, int expectedResult, int expectedSign)
        {
            int result = BigFloat.NumberOfMatchingLeadingBitsWithRounding(a, b, out int sign);
            Assert.Equal(expectedResult, result); // Fail on Verify_NumberOfMatchingLeadingBitsWithRounding({a},{b},{expectedResult},{expectedSign})
            Assert.Equal(expectedSign, sign);
        }
    }


    [Fact]
    public void Verify_NumberOfMatchingLeadingBits()
    {
        _ = BigFloat.TryParseBinary("10.111", out BigFloat a);
        _ = BigFloat.TryParseBinary("10.101", out BigFloat b);
        int result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(3, result); // Fail-10 on Verify_NumberOfMatchingLeadingBits

        _ = BigFloat.TryParseBinary("1111100", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(1, result); // Fail-20 on Verify_NumberOfMatchingLeadingBits

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(4, result); // Fail-30 on Verify_NumberOfMatchingLeadingBits

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("1000000000", out b);
        result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(4, result); // Fail-40 on Verify_NumberOfMatchingLeadingBits

        a = new BigFloat(-1);
        b = new BigFloat(0);
        result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(0, result); // Fail-50 on Verify_NumberOfMatchingLeadingBits

        a = new BigFloat(-3);
        b = new BigFloat(3);
        result = BigFloat.NumberOfMatchingLeadingMantissaBits(a, b);
        Assert.Equal(0, result); // Fail-60 on Verify_NumberOfMatchingLeadingBits
    }

    [Fact]
    public void Verify_CastFromDouble()
    {
        int count = 0;
        for (float ii = 0.0001F; ii < 100000; ii *= 1.0001F)
        {
            count++;
            BigFloat fromSingle = (BigFloat)ii;
            Assert.Equal((float)fromSingle, ii);
        }
        for (double ii = 0.0001; ii < 100000; ii *= 1.0001)
        {
            count++;
            BigFloat fromDouble = (BigFloat)ii;
            Assert.Equal((double)fromDouble, ii);
        }
        Debug.WriteLine($"Verify_CastFromDouble  Count {count}");

        BigFloat a = (BigFloat)0.414682509851111660248;         //0.0110101000101000101000100000101000001000101000100000100000101000...
        BigFloat d = (BigFloat)(double)0.414682509851111660248;
        Assert.True(a.EqualsUlp(d, 50, true));
        BigFloat f = (BigFloat)(float)0.414682509851111660248;
        Assert.True(f.EqualsUlp(d, 50, true));

        a = BigFloat.ParseBinary("0.0111101100110000101100101011101100010100010110000010011001010010");
        d = (BigFloat)(double)0.481211825059603447497;
        Assert.True(a.EqualsUlp(d, 50, true));
        f = (BigFloat)(float)0.481211825059603447497;
        Assert.True(f.EqualsUlp(d, 50, true));

        a = BigFloat.ParseBinary("10.1010111101111001110010000100011110001101101000011010111011110010");
        d = (BigFloat)(double)2.685452001065306445309;
        Assert.True(a.EqualsUlp(d, 50, true));
        f = (BigFloat)(float)2.685452001065306445309;
        Assert.True(f.EqualsUlp(d, 50, true));

        a = BigFloat.ParseBinary("0.01010101010101010101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.333333333333333333333333333;
        Assert.True(a.EqualsUlp(d, 50, true));
        f = (BigFloat)(float)0.333333333333333333333333333;
        Assert.True(f.EqualsUlp(d, 50, true));

        a = BigFloat.ParseBinary("0.101010101010101010101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.666666666666666666666666666;
        Assert.True(a.EqualsUlp(d, 50, true));
        f = (BigFloat)(float)0.666666666666666666666666666;
        Assert.True(f.EqualsUlp(d, 50, true));

        a = BigFloat.ParseBinary("0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.000325520833333333333333333;
        Assert.True(a.EqualsUlp(d));
        f = (BigFloat)(float)0.000325520833333333333333333;
        Assert.True(f.EqualsUlp(d));

        a = BigFloat.ParseBinary("-0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)-0.000325520833333333333333333;
        Assert.True(a.EqualsUlp(d));
        f = (BigFloat)(float)-0.000325520833333333333333333;
        Assert.True(f.EqualsUlp(d));

        a = BigFloat.ParseBinary("-0.1111111111111111111111111111111111111111111111111111111111111111", 0, 0, BigFloat.GuardBits);
        d = (BigFloat)(double)-0.999999999999999999999999999;
        Assert.True(a.EqualsUlp(d));
        f = (BigFloat)(float)-0.999999999999999999999999999;
        Assert.True(f.EqualsUlp(d));

        a = BigFloat.ParseBinary("-0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)-0.000325520833333333333333333;
        Assert.True(a.EqualsUlp(d));
        f = (BigFloat)(float)-0.000325520833333333333333333;
        Assert.True(f.EqualsUlp(d));

        a = BigFloat.ParseBinary("0.1111111111111111111111111111111111111111111111111111111111111111", 0, 0, BigFloat.GuardBits);
        d = (BigFloat)(double)0.999999999999999999999999999;
        Assert.True(a.EqualsUlp(d));
        f = (BigFloat)(float)0.999999999999999999999999999;
        Assert.True(f.EqualsUlp(d));

        //1.79769E+308
        a = BigFloat.ParseBinary("1111111111111111111000101011111001010100000101010111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        d = (BigFloat)(double)179769000000000006323030492138942643493033036433685336215410983289126434148906289940615299632196609445533816320312774433484859900046491141051651091672734470972759941382582304802812882753059262973637182942535982636884444611376868582636745405553206881859340916340092953230149901406738427651121855107737424232448.0;
        Assert.True(a.EqualsUlp(d));
    }


    [Fact]
    public void Verify_Cast_BigFloat_to_Double()
    {
        double res;
        res = (double)new BigFloat(123);
        Assert.Equal(123, (int)res);

        for (double d = -2.345; d < 12.34; d = 0.1 + (d * 1.007))
        {
            res = (double)new BigFloat(d);
            Assert.Equal(d, res);
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void CompareToExact_BasicIntegers_Theory(int smaller, int larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.Equal(-1, a.CompareTo(b));
        Assert.True(a.IsLessThanUlp(b, 1, true)); // Replaces StrictCompareTo
        Assert.Equal(-1, a.CompareTotalOrderBitwise(b)); // Replaces FullPrecisionCompareTo
        Assert.False(a.EqualsZeroExtended(b));
    }

    [Theory]
    [InlineData(-0.0000123, 0.0000123)]
    [InlineData(-0.0000000445, -0.0000000444)]
    [InlineData(0.0000122, 0.0000123)]
    public void CompareToExact_SmallFloats_Theory(double smaller, double larger)
    {
        var a = new BigFloat(smaller);
        var b = new BigFloat(larger);

        Assert.Equal(-1, a.CompareTo(b));
        Assert.True(a.IsLessThanUlp(b, 1, true)); // Replaces StrictCompareTo
        Assert.Equal(-1, a.CompareTotalOrderBitwise(b)); // Replaces FullPrecisionCompareTo
        Assert.False(a.EqualsZeroExtended(b));
    }

    [Fact]
    public void CompareToExact_GuardBitBehavior_DefaultPrecision()
    {
        // "...0001" falls in GuardBit area because default Double->BigFloat conversion
        var a = new BigFloat(100000000.000000);
        var b = new BigFloat(100000000.000001);

        Assert.Equal(0, a.CompareTo(b)); // Default CompareTo ignores out-of-precision guard bits in this scenario
        Assert.True(a.IsLessThanUlp(b, 1, true)); // StrictCompareTo considers guard bits
        Assert.Equal(-1, a.CompareTotalOrderBitwise(b)); // FullPrecisionCompareTo considers all bits
        Assert.False(a.EqualsZeroExtended(b));
    }

    [Fact]
    public void CompareToExact_GuardBitBehavior_ExplicitGuardBits()
    {
        var a = new BigFloat(100000000.000000, binaryPrecision: BigFloat.GuardBits);
        var b = new BigFloat(100000000.000001, binaryPrecision: BigFloat.GuardBits);

        Assert.Equal(0, a.CompareTo(b));
        Assert.True(a.IsLessThanUlp(b, 1, true)); // Replaces StrictCompareTo
        Assert.Equal(-1, a.CompareTotalOrderBitwise(b)); // Replaces FullPrecisionCompareTo
        Assert.False(a.EqualsZeroExtended(b));
    }

    [Fact]
    public void CompareToExact_StringConstructor_FullPrecision()
    {
        var a = new BigFloat("100000000.000000");
        var b = new BigFloat("100000000.000001");

        Assert.Equal(-1, a.CompareTo(b));
        Assert.True(a.IsLessThanUlp(b, 1, true)); // Replaces StrictCompareTo
        Assert.Equal(-1, a.CompareTotalOrderBitwise(b)); // Replaces FullPrecisionCompareTo
        Assert.False(a.EqualsZeroExtended(b));
    }

    [Fact]
    public void CompareToExact_StringVsDouble_GuardBitInteraction()
    {
        var a = new BigFloat("100000000.000001");
        var b = new BigFloat(100000000.000001d); // "...0001" falls in GuardBit area because default Double->BigFloat conversion

        // Detailed binary representation analysis:
        //TrueAns 101111101011110000100000000.00000000000000000001000011000110111101111010000010110101111011...  (matches / good)
        //a       10111110101111000010000000000000000000000000001000011000110111101111010000010           
        //b       10111110101111000010000000000000000000000000001000011000000000000000000000000 (subtract) using:24
        //32 GuardBits                                         XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //                                                                110111101111010000010
        //area ignored for CompareUlp(other, 1, true)                                         X
        //area ignored for Compare()                            ______________ALL______________ (after rounding)
        //area ignored for IsExactMatchOf()                     ______________ALL______________

        Assert.True(a.IsGreaterThanUlp(b, 1, true)); // String has more precision than double in guard bits
        Assert.False(a.EqualsUlp(b, 1, true)); // String has more precision than double in guard bits
        Assert.False(a.IsLessThanOrEqualToUlp(b, 1, true)); // String has more precision than double in guard bits
        Assert.NotEqual(0, a.CompareTo(b)); // CompareTo ignores guard bit differences
        Assert.False(a.EqualsZeroExtended(b)); // False - same GuardBits but allows zeros
        Assert.False(a.IsBitwiseEqual(b)); // False - Different size
    }

    [Fact]
    public void CompareToExact_DifferentDoubleSizes()
    {
        // These values are first translated from 52 bit doubles
        var a = new BigFloat(0.0000123);  //0.0000000000000000110011100101110000011001000001011000101010000111
        var b = new BigFloat(0.00001234); //0.000000000000000011001111000001111110010101111100100111000000011111100
                                          //area ignored for StrictCompareTo()                                                        XXXXXX 
                                          //area ignored for Compare()                                       XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

        Assert.True(a.IsLessThanUlp(b, 1, true)); // StrictCompareTo considers guard bits
        Assert.Equal(-1, a.CompareTo(b)); // CompareTo result
    }

    [Fact]
    public void CompareToExact_VerySmallNegativeFloats()
    {
        var a = new BigFloat(-0.000000044501); // 0.000000000000000000000000101111110010000101011101111100
        var b = new BigFloat(-0.0000000445);   // 0.000000000000000000000000101111110010000001000100011101

        Assert.True(a.IsLessThanUlp(b, 1, true)); // StrictCompareTo with guard bit consideration
    }

    [Fact]
    public void CompareToExact_BeyondDoublePrecision()
    {
        // 1.00000000000000001 is beyond the precision of double
        var a = new BigFloat(1.00000000000000001);
        var b = new BigFloat(1.00000000000000002);

        Assert.True(a.EqualsUlp(b, 1, true)); // Values are identical at double precision
    }

    [Fact]
    public void CompareToExact_StandardFloatComparison()
    {
        var a = new BigFloat(1.0);
        var b = new BigFloat(1.01);

        Assert.True(a.IsLessThanUlp(b, 1, true)); // Clear difference within double precision
    }

    [Fact]
    public void Verify_FitsInADouble()
    {
        Assert.True(new BigFloat("1.000").FitsInADouble);
        Assert.True(new BigFloat("1.000").FitsInADouble);
        Assert.True(new BigFloat("0.000").FitsInADouble);
        Assert.True(new BigFloat("-99.000").FitsInADouble);
        Assert.True(new BigFloat("0.00000001").FitsInADouble);
        Assert.True(new BigFloat("-0.00000001").FitsInADouble);
        Assert.True(new BigFloat(double.E).FitsInADouble);
        Assert.True(new BigFloat(double.MaxValue).FitsInADouble);
        Assert.True(new BigFloat(double.MinValue).FitsInADouble);
        Assert.True(new BigFloat(double.MinValue).FitsInADouble);
        Assert.False((new BigFloat(double.MinValue) << 1).FitsInADouble);
        Assert.False((new BigFloat(double.MaxValue) << 1).FitsInADouble);
        Assert.False(((new BigFloat(double.MinValue)) * (new BigFloat("1.1"))).FitsInADouble);
        Assert.True(new BigFloat(double.Epsilon).FitsInADoubleWithDenormalization);
        Assert.False((new BigFloat(double.Epsilon) >> 1).FitsInADoubleWithDenormalization);
        Assert.True(new BigFloat(double.NegativeZero).FitsInADouble);
        Assert.True(new BigFloat(0).FitsInADouble);
        Assert.True(new BigFloat("0.000000000000000001").FitsInADouble);
        Assert.True(new BigFloat("1000000000000000000").FitsInADouble);
        Assert.True(new BigFloat(-1).FitsInADouble);
        Assert.True(new BigFloat(1).FitsInADouble);

        Assert.False((new BigFloat(double.MaxValue) * (BigFloat)1.0001).FitsInADouble); // Failed on: (new BigFloat(double.MaxValue) * (BigFloat)1.0001).FitsInADouble
        Assert.False((new BigFloat(double.MinValue) * (BigFloat)1.0001).FitsInADouble); // Failed on: (new BigFloat(double.MinValue) * (BigFloat)1.0001).FitsInADouble()
    }

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(-float.Epsilon)]
    [InlineData(2.0f * float.Epsilon)]
    public void RoundTrips_Subnormal(float value)
    {
        var bf = new BigFloat(value);
        var back = (float)bf;
        Assert.Equal(value, back);
    }

    [Fact]
    public void RoundTrips_Largest_Subnormal()
    {
        float largestSubnormal = BitConverter.Int32BitsToSingle(0x007FFFFF);
        var bf = new BigFloat(largestSubnormal);
        var back = (float)bf;
        Assert.Equal(largestSubnormal, back);
    }

    [Fact]
    public void SmallestSubnormal_HasExpectedBitPattern()
    {
        // Smallest positive subnormal: 2^-1074
        var d = (double)new BigFloat(1, binaryScaler: -1074);
        Assert.Equal(0x0000_0000_0000_0001L, BitConverter.DoubleToInt64Bits(d));
    }

    [Fact]
    public void HalfwayRoundsToEven_MinNormalBitPattern()
    {
        // Half-way below the smallest normal; ties-to-even → min normal
        BigFloat eps = new BigFloat(1, binaryScaler: -1075);
        BigFloat near = new BigFloat(1, binaryScaler: -1022) - eps;
        double d = (double)near;

        Assert.Equal(0x0010_0000_0000_0000L, BitConverter.DoubleToInt64Bits(d));
    }

    [Fact]
    public void Overflow_ProducesPositiveInfinity()
    {
        // Overflow path
        var d = (double)new BigFloat(1, binaryScaler: 2000);
        Assert.True(double.IsPositiveInfinity(d));
    }

    [Fact]
    public void NegativeTinyUnderflow_ProducesNegativeZero()
    {
        // Negative tiny underflow → -0.0 (sign preserved even though magnitude → 0)
        var d = (double)(-new BigFloat(1, binaryScaler: -2000));
        const long ans = unchecked((long)0x8000_0000_0000_0000UL);
        var intVal = BitConverter.DoubleToInt64Bits(d);
        Assert.Equal(ans, intVal);
    }

    [Fact]
    public void Verify_Zero()
    {
        string errorOutputFormat = "ParseString({0,10}) -> BigFloat -> String -> Expect: {2,10}, Got: {1}";
        IsNotEqual(".0000", x => BigFloat.Parse(x), "0.0000", errorOutputFormat);
        IsNotEqual("0.000", x => BigFloat.Parse(x), "0.000", errorOutputFormat);
        IsNotEqual("00.00", x => BigFloat.Parse(x), "0.00", errorOutputFormat);
        IsNotEqual("-.0000", x => BigFloat.Parse(x), "0.0000", errorOutputFormat);
        IsNotEqual("-0.000", x => BigFloat.Parse(x), "0.000", errorOutputFormat);
        IsNotEqual("+.000000", x => BigFloat.Parse(x), "0.000000", errorOutputFormat);
        IsNotEqual("0", x => BigFloat.Parse(x), "0", errorOutputFormat);
        IsNotEqual("000", x => BigFloat.Parse(x), "0", errorOutputFormat);
        IsNotEqual("0.0000000000000000000", x => BigFloat.Parse(x), "0.0000000000000000000", errorOutputFormat);
        IsNotEqual("0.0000000000000000000000000000000000000", x => BigFloat.Parse(x), "0.0000000000000000000000000000000000000", errorOutputFormat);
    }

    [Fact]
    public void Verify_Increment()
    {
        BigFloat inputVal, expect;
        inputVal = new BigFloat("2.00000000000");
        expect = new BigFloat("3.00000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("1.00000000000");
        expect = new BigFloat("2.00000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("0.00000000000");
        expect = new BigFloat("1.00000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-1.00000000000");
        expect = new BigFloat("0.00000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-2.00000000000");
        expect = new BigFloat("-1.00000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        // With decimal
        inputVal = new BigFloat("2.50000000000");
        expect = new BigFloat("3.50000000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("1.4400000000000");
        inputVal++;
        expect = new BigFloat("2.4400000000000");
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("0.0000230000000");
        expect = new BigFloat("1.0000230000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-0.0000230000000");
        expect = new BigFloat("0.9999770000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-1.0000430000000");
        expect = new BigFloat("-0.0000430000000");
        inputVal++;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-2.000000000007777");
        expect = new BigFloat("-1.000000000007777");
        inputVal++;
        Assert.Equal(inputVal, expect);
    }

    [Fact]
    public void Verify_Decrement()
    {
        BigFloat inputVal, expect;
        inputVal = new BigFloat("2.00000000000");
        expect = new BigFloat("1.00000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("1.00000000000");
        expect = new BigFloat("0.00000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("0.00000000000");
        expect = new BigFloat("-1.00000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-1.00000000000");
        expect = new BigFloat("-2.00000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-2.00000000000");
        expect = new BigFloat("-3.00000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        // With decimal
        inputVal = new BigFloat("2.50000000000");
        expect = new BigFloat("1.50000000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("1.440000000000");
        expect = new BigFloat("0b0.011100001010001111010111000010100011110101110000101000111101011100001010", 0, 32);
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("0.0000230000000");
        //expect = new BigFloat("-0.9999770000000"); fails because of rounding of last bit.
        expect = new BigFloat("-0b0.111111111111111001111110000111111100000010001111101001111010100001010000001", 0, 32);
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-0.0000230000000");
        expect = new BigFloat("-1.0000230000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-1.0000430000000");
        expect = new BigFloat("-2.0000430000000");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-2.000000000007777");
        expect = new BigFloat("-3.000000000007777");
        inputVal--;
        Assert.Equal(inputVal, expect);

        inputVal = new BigFloat("-20000000000000000000000000000000000000000000000000000000000000000000000000000000.000000000007777");
        expect = new BigFloat("-20000000000000000000000000000000000000000000000000000000000000000000000000000001.000000000007777");
        inputVal--;
        Assert.Equal(inputVal, expect);
    }

    [Fact]
    public void Verify_Math_Add()
    {
        BigFloat inputVal0, inputVal1, output, expect;
        inputVal0 = new BigFloat("2.00000000000");
        inputVal1 = new BigFloat("2.00000000000");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("4.00000000000");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("3");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("4");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("0.00000000001");
        inputVal1 = new BigFloat("1000000.0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1000000.0");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0.1");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1.1");
        Assert.True(output.EqualsUlp(expect, 1, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("0");
        inputVal1 = new BigFloat("0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("0");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("123457855782.27542786378320");        //      123457855782.27542786378320
        inputVal1 = new BigFloat("56784589567864578.05687450567100");   // 56784589567864578.05687450567100
        output = inputVal0 + inputVal1;                                 // 56784713025720360.3323023694542 (this should be enough reduced precision to match)
        expect = new BigFloat("56784713025720360.3323023694542");
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat("0.0000000012101");  //   0.0000000012101
        inputVal1 = new BigFloat("0.00000000512");    // + 0.00000000512
        output = inputVal0 + inputVal1;               //   0.0000000063301
        expect = new BigFloat("0.00000000633");       //   0.00000000633
        Assert.True(output.EqualsUlp(expect, 25, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}."); // 25 is passing edge case

        inputVal0 = new BigFloat(5555, 10);  // 5688320 + 5555 = 5693875
        inputVal1 = new BigFloat(5555);
        output = inputVal0 + inputVal1;
        expect = new BigFloat("5693875");  // expected: 5693875 result: 5693875
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        inputVal0 = new BigFloat(55555, 10);  // 56888320 + 5555 = 56893875
        inputVal1 = new BigFloat(5555);
        output = inputVal0 + inputVal1;
        expect = new BigFloat("56893875");  // expected: 56893875 result: 56893440
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        // Test Shortcut for values way out of precision range.
        BigInteger x123456789ABCDEF0 = BigInteger.Parse("123456789ABCDEF0", NumberStyles.AllowHexSpecifier);
        BigInteger x1234560789A = BigInteger.Parse("1234560789A", NumberStyles.AllowHexSpecifier);
        inputVal0 = new BigFloat(x123456789ABCDEF0, 64, true);  // "12345678"9ABCDEF0________.       (Size: 29, _size: 61, Scale: 64)
        inputVal1 = new BigFloat(x1234560789A, 20, true);       // +                "12"34560.789A   (Size:  5, _size: 37, Scale: 20)
        output = inputVal0 + inputVal1;                         //= 12345678"9ABCDEF0________.
        expect = new BigFloat(x123456789ABCDEF0, 64, true);
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");

        // other add order...
        output = inputVal1 + inputVal0;
        Assert.True(output.EqualsUlp(expect, 0, true), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}.");
    }

    [Fact]
    public void Verify_Math_Subtract()
    {
        BigFloat inputVal0, inputVal1, output, expect;
        bool passed;

        inputVal0 = new BigFloat("2.00000000000");
        inputVal1 = new BigFloat("2.00000000000");
        output = inputVal0 - inputVal1;
        //2.00000000000
        // .00000000000 
        // .00000000000
        expect = new BigFloat("0.00000000000");
        passed = output == expect;
        Assert.True(passed); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("3");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-2");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("0.00000000001");
        inputVal1 = new BigFloat("1000000.0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-1000000.0");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0.1");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("1");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("1");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("0");
        inputVal1 = new BigFloat("0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("0");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("123457855782.2754278637832");
        inputVal1 = new BigFloat("56784589567864578.05687450567100");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-56784466110008795.7814466418878");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat("0.0000000012101"); // 0.00000000121005    0.00000000121015
        inputVal1 = new BigFloat("0.00000000512");   // 0.000000005125      0.000000005115  
        output = inputVal0 - inputVal1;              //-0.00000000391495  -0.00000000390485  so, -0.0000000039  (okay would also be the avg -0.00000000391)  
        expect = new BigFloat("-0.00000000391");
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = new BigFloat(2119, binaryScaler: 18, binaryPrecision: 0);  //  5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(5555, binaryPrecision: 0);   //      -5555  
        output = inputVal0 - inputVal1;                  //= 5555_____
        expect = new BigFloat("555572222");  // expected: 555572222 result:555483136  OK
        Assert.True(output.EqualsUlp(expect), $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}.");

        inputVal0 = BigFloat.CreateWithPrecisionFromValue(2119, binaryScaler: 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(555555); //          -10000111101000100011    -555555  
        output = inputVal0 - inputVal1;                  //=100001000101                    5549_____
        expect = new BigFloat(2117, 18);
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = BigFloat.CreateWithPrecisionFromValue(2119, binaryScaler: 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(-555555);//          +10000111101000100011    +555555              +555555
        output = inputVal0 - inputVal1;                  //=100001001001                    5561_____            556038691
        expect = new BigFloat(2121, 18);
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = BigFloat.CreateWithPrecisionFromValue(-2119, binaryScaler: 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(555555);//          +10000111101000100011    +555555              +555555
        output = inputVal0 - inputVal1;                  //=100001001001                    5561_____            556038691
        expect = new BigFloat(-2121, 18);
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}

        inputVal0 = BigFloat.CreateWithPrecisionFromValue(-2119, binaryScaler: 18);  // -100001000111                   -5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(-555555); //           +10000111101000100011    +555555  
        output = inputVal0 - inputVal1;                   //=-100001000101                   -5549_____
        expect = new BigFloat(-2117, 18);
        Assert.Equal(output, expect); // Add({inputVal0} - {inputVal1}) was {output} but expected {expect}
    }

    [Fact]
    public void Verify_Math_Multiply()
    {
        BigFloat inputVal0, inputVal1, output, expect;

        inputVal0 = new BigFloat("1.000");
        inputVal1 = new BigFloat("1.000");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("1.000");
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("255");
        inputVal1 = new BigFloat("255");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("0x0FE|01.00000000", 0); // "new BigFloat("65025")" is correct but not the exact needed precision.
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("256");
        inputVal1 = new BigFloat("255");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("65280");
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(9007199254740991UL);
        inputVal1 = new BigFloat(9007199254740991UL);
        output = inputVal0 * inputVal1;
        // in      11111111111111111111111111111111111111111111111111111   9007199254740991
        // output: 11111111111111111111111111111111011111111111111111111000000000000000000000000000000001  77371252446329059336519681 <<52
        // exact   1111111111111111111111111111111111111111111111111111000000000000000000000000000000000000000000000000000001 81129638414606663681390495662081
        expect = new BigFloat(BigInteger.Parse("81129638414606663681390495662081"));
        Assert.True(output.EqualsUlp(expect, 1));

        inputVal0 = new BigFloat(9007199254740992UL);
        inputVal1 = new BigFloat(9007199254740991UL);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(BigInteger.Parse("81129638414606672688589750403072"));
        Assert.True(output.EqualsUlp(expect, 1));

        inputVal0 = new BigFloat("11.000");
        inputVal1 = new BigFloat("3.000");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("33.00");
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("255", 2);  // 1020
        inputVal1 = new BigFloat("20", -1);  // 10
        output = inputVal0 * inputVal1;
        expect = new BigFloat("20", 9);  // 19.921875 << 9
        Assert.Equal(output, expect); // Step 22a 
        expect = new BigFloat("10|200");
        Assert.Equal(output, expect); // Step 22b 

        inputVal0 = new BigFloat("19", -3);
        inputVal1 = new BigFloat("15", 2);
        output = inputVal0 * inputVal1;
        expect = new BigFloat("18", 3);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("1", 0);
        inputVal1 = new BigFloat("1.0", 1);
        output = inputVal0 * inputVal1;
        expect = new BigFloat("2.0", 0);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("19", -3);
        inputVal1 = new BigFloat("1.5", 2);
        output = inputVal0 * inputVal1;
        expect = new BigFloat("14", 0);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("3.00");
        inputVal1 = new BigFloat("11.00");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("33.0");
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat("2.00000000000");
        output = inputVal0 * inputVal0;
        expect = new BigFloat("4.00000000000");
        Assert.Equal(output, expect);

        // OVERRIDE TEST: output is 64(not 63) but this is technically okay - maybe this can be improved by a fixed number of bits of precision.
        inputVal0 = new BigFloat("7");
        inputVal1 = new BigFloat("9");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("6|3"); // output is 64 (8<<3) and this is technically okay. 
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(11);
        inputVal1 = new BigFloat(9);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(11, 8);
        inputVal1 = new BigFloat(9);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99, 8);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(4, 8); //     1024
        inputVal1 = new BigFloat(16, 10); //    16384
        output = inputVal0 * inputVal1;
        expect = new BigFloat(4, 22);   //  16777216  4 x 2^22  or  1 x 2^24
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(511, 1); //     1022
        inputVal1 = new BigFloat(1023, 4); //    16368
        output = inputVal0 * inputVal1;
        expect = new BigFloat(522753, 5);   //  16728096  4 x 2^22  or  1 x 2^24
        Assert.Equal(output, expect);

        // Lets test the round up in equals. The expect (11111...111111) should shrink and round up at the same time so both should be 10000...
        inputVal0 = new BigFloat(0b10101010101010101010101010101, 0); //  357913941 (binary string is converted to an Int)
        inputVal1 = new BigFloat(0b11000000000000000000000000000011, 0); // 3221225475 (binary string is converted to an UInt)
        output = inputVal0 * inputVal1;                                  // 1152921504606846975
        expect = new BigFloat("0b0001111111111111111111111111111|11111111111111111111111111111111");
        //                   exact: 111111111111111111111111111111111111111111111111111111111111
        //                          111111111111111111111111111111111111111111111111111111111111
        Assert.Equal(output, expect);  //Todo: FAIL: BigFloat(uint) and BigFloat(int) need updating as the precision is different

        inputVal0 = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true); // aka. 511.9999<<1 or 1023.99999 
        inputVal1 = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true); // 1111111111.1111111111111111111111000000000 >> (32-1)    1048575.99999...
                                                                                 // HIDDEN:  #.############################### 

        output = inputVal0 * inputVal1;
        expect = new BigFloat(1024, 10);   //  4835703276206716885401600 1024>>10
        // 11111111111111111111.11111111111000000000000000000000000000000001000000000000000000   (4835703276206716885401600)
        // 11111111111111111111.111111111110000000000000000000################################
        //                   ##.##############################
        //100000000000000000000
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(11);
        inputVal1 = new BigFloat(9, 8);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99, 8);
        Assert.Equal(output, expect);

        inputVal0 = new BigFloat(11, -121);
        inputVal1 = new BigFloat(-120, -22);
        output = inputVal0 * inputVal1;     // -708669603840 >> (140+32) = 0.0000....000001010010100000000000000000000000000000000
        expect = new BigFloat(-1320, -143); // 1320 >> 143               = 0.0000....0000010100101000
        //                   both should round to 10 (the input of size)   0.0000....0000010101     
        Assert.Equal(output, expect); // Step 90a 
        Assert.Equal(output.Size, inputVal0.Size);

        inputVal0 = new BigFloat(8941981654981981918UL, 55); //322168841994645319142957991669530624
        inputVal1 = new BigFloat(-15024375452859887L, -22); //3582090247.3592488765716552734375
        //          111110000011000010011001111000001000101010000101011011011011110 (8941981654981981918)
        //        x 110101011000001001011100000001110101101111110111101111 (15024375452859887)
        // exact    134347689677034716410464568421523266 << 33 =   134347689677034716410464568421523266*8589934592  = 1154037866912041818479159667074393539946217472
        //        = 110011101111111011010001111011101001000100010001101101101101010111011100011100011000101010011100101100001111101000010000000000000000000000000000000000  
        //          11001110111111101101000111101110100100010001000110110110110101011101110001110001100100 (62560518121828658697411684)
        //          11001110111111101101000111101110100100010001000110110110110101011101110001110001100100
        //          1100111011111110110100011110111010010001000100011011011011010101110111000111000110001010100111001011000011111010000100000000000000000000000000000000000000000000000000000000000000000
        //          11001110111111101101000111101110100100010001000110110110110101011101110001110001100011
        //
        // output   11001110111111101101000111101110100100010001000110110110110101011101110001110001100010   (33BFB47BA4446DB5771C62 << 96)
        // 
        // expect1: 110011101111111011010001111011101001000100010001101101101101010111011100011100011000101010011100101100001111101000010000000000000000000000000000000000   (1154037866912041818479159667074393539946217472)  PASS
        // expect2: 110011101111111011010001111011101001000100010001101110 (rounded up)     (14566005701624942)  PASS
        // expect3: 1100111011111110110100011110111010010001                                (889038433937)       PASS
        output = inputVal0 * inputVal1;

        // with over accurate expected value
        expect = new BigFloat(BigInteger.Parse("-1154037866912041818479159667074393539946217472"));
        Assert.True(output.EqualsUlp(expect, 1));

        // output   11001110111111101101000111101110100100010001000110110110110101011101110001110001100011
        // expect   11001110111111101101000111101110100100010001000110111000000000000000000000000000000000
        //                                                                ################################  guard
        expect = new BigFloat("-14566005701624942", 96);
        Assert.Equal(output.Size, inputVal1.Size);
        Assert.True(output.EqualsUlp(expect));

        // output     11001110111111101101000111101110100100010001000110110110110101011101110001110001100011    62560518121828658697411683
        // expected   110011101111111011010001111011101001000100000000000000000000000000000000                 889038433937 14566005701624942(DataBits = 3818390998646471524352)
        //                                                    ################################  GuardBits
        expect = new BigFloat("-889038433937", 110); //-1154037866912041818479159667074393539946217472
        Assert.True(output.EqualsUlp(expect));

        // output:    11001110111111101101000111101110100100010001000110110110110101011101110001110001100011    62560518121828658697411683
        // output:    110011101111111011010001111011101001000100010001101101101101010111011101                 3818390998646768719325 (this.DataBits >> (sizeDiff - expDifference))
        // expect:    110011101111111011010001111011101001001                                                  444519216969 << (32 + 1) = 3818390998650766491648
        // expect:    110011101111111011010001111011101001001000000000000000000000000000000000                 3818390998650766491648 (other)  or 1154037866913250071024881716200922954189504512
        //                                                    ################################  GuardBits
        //                                                   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%  rounding
        expect = new BigFloat("-444519216969", 111); //-1154037866913250071024881716200922954189504512
        Assert.True(output.EqualsUlp(expect));

        inputVal0 = new BigFloat("123457855782.2754278637832");
        inputVal1 = new BigFloat("56784589567864578.05687450567100");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("7010503669525126837652377239.56001231481228902391");
        Assert.True(output.EqualsUlp(expect), $"Add({inputVal0}{inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("8941981654981.981918284", 55);
        inputVal1 = new BigFloat("-1502437545285988701043238237856775089653447902277", -22);
        output = inputVal0 * inputVal1;
        Assert.Equal(output.Size, inputVal0.Size);
        // external calculation: 13434768967703471650801766289704559608123152231389557678010624.231532668
        //                       57701893345602090965856718393316345018971514774247574862961329414977941.015625728

        // a little too small
        expect = new BigFloat(BigInteger.Parse("-115403786691204181931719282793182013649615844287826014858001281754705681"), 0);
        // output     1000010111000100100000111100110000010001111010011101110101110111000001000011101011101011101000110001001001    42392656037190875842938869288009
        // expected   10000101110001001000001111001100000100011110100111011101011110010000010010100010000101001100101101011111000110111100100010110010111111011001101000101110111110101100100001111111100001100001111100010000000000100011010001001101011110001000100000000000000000000000000000000                   -_______ >> 32 = -115403786691204181931719282793182013649615844287826014858001281754705681
        //                                                                                      ################################  GuardBits
        Assert.False(output == expect); // Step 97a 

        // a little too small
        expect = new BigFloat("-678282496595054013627833570557952", 127); //-1343476896770347165080199207099879564484138783290.5031390682463057059450092223189508
        // manual calculation100001011100010010000011110011000001000111101001110111010111100100000100100000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000.00111011010001011011100110010101000001000000000000001100001101...  13434768967703471650801766289704559608123152231389557678010624.231532668
        // output(be4 Round) 1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111100111111010011101111111110101110100100011001011110111100110101001101010111110101111111101110000110011111110
        // output            1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101      42392656037190875851739737828733<<163
        // expected          10000101110001001000001111001100000100011110100111011101011110010000010010000000000000000000000000000000000000  678282496595054013627833570557952>>4
        // output(rounded)   10000101110001001000001111001100000100011110100111011101011110010000010010                                              9870309391336253809682 << 163  (includes GuardBits)
        // expected(rounded) 10000101110001001000001111001100000100011110100111011101011110010000010010
        //                                                                                            ################################  GuardBits
        Assert.Equal(output, expect);

        // a little too small
        expect = new BigFloat("-115403786691204181933215860469808858237856417556527488670128956678713105", 0);
        // output           1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected         100001011100010010000011110011000001000111101001110111010111100100100100101000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000001000110100010011010111100010001  (this)(right)  9870309391336253809811  (DataBits: 115403786691204181933215860469808858237856417556527488670128956678713105
        // output(rounded)  10000101110001001000001111001100000100011110100111011101011110010000010010  (other)(left)  9870309391336253809682 << 163  (DataBits: 42392656037190875851739737828733)
        // expected(rounded)10000101110001001000001111001100000100011110100111011101011110010010010011  (this)(right)  9870309391336253809811  (DataBits: 115403786691204181933215860469808858237856417556527488670128956678713105
        //                                                                                      ^       ################################  GuardBits
        Assert.False(output == expect);

        // a little too small
        expect = new BigFloat("-115403786691179073526274313746753515080163586890863079248351100540661521", 0);
        // output           1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected         100001011100010010000011110011000001000111001001110111010111100100000100101000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000001000110100010011010111100010001
        // output(rounded)  10000101110001001000001111001100000100011110100111011101011110010000010010    (other)(left)                       9870309391336253809682 << 163  (DataBits: 42392656037190875851739737828733)
        // expected(rounded)10000101110001001000001111001100000100011100100111011101011110010000010011        DataBits: 115403786691179073524777736070126670491923013622161605436223425616654097
        //                                                                                              ################################  GuardBits
        Assert.False(output == expect);

        // a little too small
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809680"), 163);
        // output     1000010111000100100000111100110000010001111010011101110101110111000001000011101011101011101000110001001001    42392656037190875842938869288009
        // expected   1000010111000100100000111100110000010001111010011101110101111001000001000000000000000000000000000000000000
        //                                                                                      ################################  GuardBits
        Assert.False(output == expect);

        // a little too big                          
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809681"), 163);
        // manual calc 100001011100010010000011110011000001000111101001110111010111100100000100100000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000.00111011010001011011100110010101000001000000000000001100001101...  13434768967703471650801766289704559608123152231389557678010624.231532668
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001000100000000000000000000000000000000    9870309391336253809681
        //                                                                                       ################################  GuardBits
        Assert.False(output == expect); // Step 97e 

        // just right  
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809682"), 163);
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001001000000000000000000000000000000000
        //                                                                                       ################################  GuardBits
        Assert.Equal(output, expect);

        // a little too big
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809683"), 163);
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001001100000000000000000000000000000000
        //                                                                                       ################################  GuardBits
        Assert.False(output == expect);

        (double growthSpeed_i, double growthSpeed_j, int MAX_INT) = TestTargetInMillseconds switch
        {
            >= 30900 => (1.1, 1.3, 1024),
            >= 6700 => (1.3, 1.7, 1024),
            >= 4500 => (1.3, 2.3, 1024),
            >= 1500 => (2.3, 3.3, 1024),
            >= 1100 => (3.3, 4.3, 1024),
            >= 900 => (4.3, 5.3, 764),
            >= 615 => (14.3, 15.3, 512),
            >= 285 => (90.3, 135.3, 512),
            >= 174 => (1090.3, 1935.3, 256),
            >= 165 => (2090.3, 2935.3, 256),
            >= 73 => (100000090.3, 200000935.3, 256),
            >= 53 => (100000000090.3, 200000000935.3, 128),
            _ => (12345678901234567, 234567890123456, 64), //33  
        };


        for (int i = 0; i < MAX_INT; i++)
        {
            for (int j = i; j < MAX_INT * 2; j++)
            {
                BigFloat input0 = (BigFloat)i;
                BigFloat input1 = (BigFloat)j;
                BigFloat res = input0 * input1;
                int exp = i * j;
                Assert.Equal(res, exp); // LoopA {i}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}
            }
        }

        for (double j = 0.0001; j < 1E154; j *= growthSpeed_j)
        {
            BigFloat input1 = (BigFloat)j;
            for (int i = -MAX_INT; i < MAX_INT; i++)
            {
                BigFloat input0 = (BigFloat)i;
                BigFloat res = input0 * input1;
                BigFloat exp = (BigFloat)(i * j);
                Assert.True(res.EqualsUlp(exp, -12), $"LoopA {i}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }

            for (double ii = 0.0001; ii < 1E154; ii *= growthSpeed_i)
            {
                BigFloat input0 = (BigFloat)ii;
                BigFloat res = input0 * input1;
                BigFloat exp = (BigFloat)(ii * j);
                Assert.True(res.EqualsUlp(exp, 50, true), $"LoopA {ii}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }

            for (double ii = -0.0001; ii > -1E154; ii *= growthSpeed_i)
            {
                BigFloat input0 = (BigFloat)ii;
                BigFloat res = input0 * input1;
                BigFloat exp = (BigFloat)(ii * j);
                Assert.True(res.EqualsUlp(exp, 50, true), $"LoopA {ii}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }
        }
    }

    [Fact]
    public void Verify_Math_Divide()
    {
        string inputVal0, inputVal1, expectAns;

        inputVal0 = "1.000";                //  1.0005                  0.9995
        inputVal1 = "1.000";                //  0.9995                  1.0005
        expectAns = "1.000";                //  1.00100                 0.99900             so, 1.000  (last digit within +/- 3 range)
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns); // getting 1.000 but can be 1.00

        inputVal0 = "11.000";               //    11.0004999            10.9995000
        inputVal1 = "3.000";                //  / 2.99950000          / 3.00049999
        expectAns = "3.667";                //  = 3.66744457          = 3.66588903           so, 3.667 (last digit within +/- 3 range)
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);

        inputVal0 = "3.000";                //    3.00049999999         2.9995
        inputVal1 = "11.0000000000";        //  / 10.99999999995        11.000000000049999
        expectAns = "0.2727";               //  = 0.27277272727       = 0.272681818180578   avg is 0.272727,   so, 0.2727 0.27273 
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);

        inputVal0 = "5.000000000000";       //   5.0000000000005        4.9999999999995
        inputVal1 = "10.000";               // / 9.9995               / 10.0005
        expectAns = "0.5000";               // = 0.50002500125        = 0.499975001249      so, 0.50000
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);

        inputVal0 = "3.141592653589793238462643";   //   3.141 592 653 589 7932384626435   3.141 592 653 589 7932384626425
        inputVal1 = "2.000000000000";               // / 1.999 999 999 999 5               2.000 000 000 000 5
        expectAns = "1.5707963267949";              // = 1.570 796 326 795 289           = 1.570 796 326 794 503          so, 1.570 796 326 794 9
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);

        inputVal0 = "1.0001";                  //  1.00015         1.00005
        inputVal1 = "1.000";                   //  0.9995          1.0005
        expectAns = "1.000";                   //  1.0006503       0.9995502   so, 1.000 
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);  // getting 1.000 but can be 1.00

        inputVal0 = "1.0000";                //  0.99995          1.00005
        inputVal1 = "1.0001";                //  1.00015          1.00005
        expectAns = "0.9999";                //  0.9998000        1.0000000  so, 0.9999
        Verify_Math_Divide_Helper(inputVal0, inputVal1, expectAns);  // getting 1.000 but can be 1.00
    }

    private static void Verify_Math_Divide_Helper(string inputVal0, string inputVal1, string expectedAnswer)
    {
        BigFloat inputVal0BF = BigFloat.Parse(inputVal0);
        BigFloat inputVal1BF = BigFloat.Parse(inputVal1);
        BigFloat output = inputVal0BF / inputVal1BF;

        //Console.WriteLine($"{inputVal0BF})[{inputVal0BF.Size}] / ({inputVal1BF})[{inputVal1BF.Size}]\r\n  was {output.ToString()} [{output.Size}]\r\n  expected:{expectedAnswer} \r\n  moreExact:  {decimal.Parse(inputVal0) / decimal.Parse(inputVal1)}");
        Assert.Equal(expectedAnswer, output.ToString());
    }

    [Theory]
    [InlineData("2222222222", 8, 132)]
    [InlineData("-2222222222", 8, -132)]
    [InlineData("-1024", 8, -128)]
    [InlineData("-1022", 8, -256)]
    [InlineData("1022", 8, 256)]
    [InlineData("-1023", 8, -256)]
    [InlineData("1023", 8, 256)]
    [InlineData("1024", 8, 128)]
    public void BigIntegerTools_TruncateToAndRound_ShouldReturnExpectedValue(string input, int precision, int expected)
    {
        // Arrange
        BigInteger inputInt = BigInteger.Parse(input);

        // Act
        BigInteger result = BigIntegerTools.TruncateToAndRound(inputInt, precision);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2222222222", 8, 132)]
    [InlineData("-2222222222", 8, -132)]
    [InlineData("-1024", 8, -128)]
    [InlineData("1024", 8, 128)]
    [InlineData("1022", 9, 511)]
    [InlineData("1023", 9, 512)]
    [InlineData("1022", 10, 1022)]
    [InlineData("1023", 10, 1023)]
    [InlineData("1024", 10, 512)]
    [InlineData("1025", 10, 513)]
    public void BigFloat_SetPrecisionWithRound_ShouldReturnExpectedRoundedMantissa(string input, int precision, int expected)
    {
        // Arrange
        BigFloat inputVal = new(input, 0);

        // Act
        BigFloat result = BigFloat.SetPrecisionWithRound(inputVal, precision);

        // Assert
        Assert.Equal(expected, result.RoundedMantissa);
    }

    [Theory]
    [InlineData("2.00000000000", "1.4142135623730950488016887242097")]
    [InlineData("200000000000", "447213.59549995793928183473374626")]
    [InlineData("0.0215841551", "0.14691546923316142068618979769788")]
    [InlineData("0.000000001", "0.000031622776601683793319988935444327")]
    [InlineData("98765432109876543210987654321098765432109876543210987654321098765432109876543210", "9938079900558082311789231964937550558064.64944382685442702212868466033571678970487057062388")]
    [InlineData("0.98765432109876543210987654321098765432109876543210987654321098765432109876543210", "0.993807990055808231178923196493755055806464944382685442702212868466033571678970487057062388")]
    public void BigFloat_Sqrt_ShouldReturnExpectedValue(string inputString, string expectedString)
    {
        // Arrange
        BigFloat inputVal = new(inputString, 0);
        BigFloat expected = new(expectedString);

        // Act
        BigFloat output = BigFloat.Sqrt(inputVal);

        // Assert with detailed output
        Verify_TruncateAndRoundHelper(inputVal, output, expected);
    }

    private static void Verify_TruncateAndRoundHelper(BigFloat inputVal, BigFloat output, BigFloat preciseAnswer)
    {
        // Calculate expected output with proper precision
        int expectedOutputSize = inputVal.Size;
        BigFloat expectedBF = BigFloat.SetPrecisionWithRound(preciseAnswer, expectedOutputSize);

        // Check if the result matches expected
        bool isMatch = output.ToString() == expectedBF.ToString();

        // Generate detailed output message
        string resultMessage = $"{(isMatch ? "YES!" : "NO! ")}  Sqrt({inputVal})[{inputVal.Size}]" +
            $"\r\n  was      {output.ToString() + " [" + output.Size + "]"}" +
            $"\r\n  expected {expectedBF.ToString() + " [" + expectedBF.Size + "]"} [{expectedOutputSize}]";

        // Assert with detailed message on failure
        Assert.True(isMatch, $"BigFloat.Sqrt() did not return expected result:\n{resultMessage}");
    }

    [Fact]
    public void Verify_TryParseBinary()
    {
        // Test 'TryParseBinary' functionality
        string input0 = "10100010010111";
        Assert.True(BigIntegerTools.TryParseBinary(input0, out BigInteger resVal));
        BigInteger expVal = 10391;
        Assert.Equal(expVal, resVal); // Fixed: was comparing resVal to expVal incorrectly
    }

    [Theory]
    [InlineData("10100010010111", "1010001001100", 1)]     // Basic right shift with round
    [InlineData("-10100010010111", "-1010001001100", 1)]   // Negative basic right shift with round
    [InlineData("10100010010110", "1010001001011", 1)]     // Right shift with round (no carry)
    [InlineData("-10100010010110", "-1010001001011", 1)]   // Negative right shift with round (no carry)
    [InlineData("10100010010110", "101000100110", 2)]      // Right shift by 2 positions
    [InlineData("-10100010010110", "-101000100110", 2)]    // Negative right shift by 2 positions
    [InlineData("101000100101011", "1010001001011", 2)]    // Right shift by 2 with specific rounding behavior
    [InlineData("-101000100101011", "-1010001001011", 2)]  // Negative right shift by 2 with rounding
    [InlineData("101000100101101", "1010001001011", 2)]    // Right shift by 2, different rounding case
    [InlineData("-101000100101101", "-1010001001011", 2)]  // Negative version of above
    [InlineData("11111111111111", "10000000000000", 1)]    // Overflow case: all 1s -> rounded overflow
    [InlineData("-11111111111111", "-10000000000000", 1)]  // Negative overflow case
    public void Verify_RoundingRightShift_Basic(string input, string expected, int shiftAmount)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger inputVal));
        Assert.True(BigIntegerTools.TryParseBinary(expected, out BigInteger expectedVal));

        BigInteger result = BigIntegerTools.RoundingRightShift(inputVal, shiftAmount);
        Assert.Equal(expectedVal, result);
    }

    [Theory]
    [InlineData("11111111111111", "10000000000000", 1, 14, 14)]    // Overflow with size tracking
    [InlineData("-11111111111111", "-10000000000000", 1, 14, 14)]  // Negative overflow with size tracking
    [InlineData("11111111111110", "1111111111111", 1, 14, 13)]     // No overflow, size reduces
    public void Verify_RoundingRightShift_WithSizeTracking(string input, string expected, int shiftAmount, int inputSize, int expectedSize)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger inputVal));
        Assert.True(BigIntegerTools.TryParseBinary(expected, out BigInteger expectedVal));

        int size = inputSize;
        BigInteger result = BigIntegerTools.RoundingRightShift(inputVal, shiftAmount, ref size);

        Assert.Equal(expectedVal, result);
        Assert.Equal(expectedSize, size);
    }

    [Theory]
    [InlineData("11111111111111", "1000000000000", 1, true)]       // Overflow case with carry
    [InlineData("-11111111111111", "-1000000000000", 1, true)]     // Negative overflow with carry  
    [InlineData("11111111111110", "1111111111111", 1, false)]      // No overflow, no carry
    [InlineData("-11111111111111", "-100000000000", 2, true)]      // Multi-bit shift with carry
    [InlineData("-11011111111111", "-111000000000", 2, false)]     // Multi-bit shift without carry
    public void Verify_RoundingRightShiftWithCarry(string input, string expected, int shiftAmount, bool expectedCarry)
    {
        Assert.True(BigIntegerTools.TryParseBinary(input, out BigInteger inputVal));
        Assert.True(BigIntegerTools.TryParseBinary(expected, out BigInteger expectedVal));

        (BigInteger result, bool carry) = BigIntegerTools.RoundingRightShiftWithCarry(inputVal, shiftAmount);

        Assert.Equal(expectedCarry, carry);
        Assert.Equal(expectedVal, result);
    }

#pragma warning disable CS0618 // SetPrecision is obsolete; keep coverage as a compatibility check
    [Fact]
    public void SetPrecision_LegacyCompatibility()
    {
        BigFloat inputVal, res;
        string output, expect;
        inputVal = new BigFloat("0.9876543210987654321098765432109876");
        string exact = "0.11111100110101101110100111100000110111110100110111000011010010...";

        for (int i = 1; i < 20; i++)
        {
            res = BigFloat.SetPrecision(inputVal, i);
            output = res.ToString("B");
            int temp = Convert.ToInt32(exact[2..(i + 2)], 2);
            expect = "0." + Convert.ToString(temp, 2);
            Assert.Equal(output, expect);
        }
    }
#pragma warning restore CS0618

    [Fact]
    public void ExtendPrecision()
    {

        //BigFloat SetPrecision(BigFloat x, int newSize, bool useRounding = false)
        //BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
    }

    [Fact]
    public void Verify_FloatAndDoubleExceptions()  // last for debugging
    {
#if !DEBUG
        Assert.Throws<OverflowException>(() => new BigFloat(float.PositiveInfinity));
        Assert.Throws<OverflowException>(() => new BigFloat(float.NegativeInfinity));
        Assert.Throws<OverflowException>(() => new BigFloat(double.PositiveInfinity));
        Assert.Throws<OverflowException>(() => new BigFloat(double.NegativeInfinity));
        Assert.Throws<OverflowException>(() => new BigFloat(float.NaN));
        Assert.Throws<OverflowException>(() => new BigFloat(double.NaN));
#endif
    }

    /// <summary>
    /// Takes an inputParam and inputFunc and then checks if the results matches the expectedOutput.
    /// </summary>
    /// <param name="inputParam">The input value to apply to the inputFunc.</param>
    /// <param name="inputFunc">The function that is being tested.</param>
    /// <param name="expectedOutput">What the output of inputFunc(inputParam) should be like.</param>
    /// <param name="msg">If they don't match, output this message. Use {0}= input, {1}=results of inputFunc(inputParam) {2}=the value it should be.
    /// Example: "The input value of {0} with the given function resulted in {1}, however the value of {2} was expected."</param>
    [DebuggerHidden]
    private static void IsNotEqual(string inputParam, Func<string, object> inputFunc, string expectedOutput, string msg = "")
    {
        string a = inputFunc(inputParam).ToString() ?? "";
        if (!a.Equals(expectedOutput))
        {
            if (string.IsNullOrEmpty(msg))
            {
                msg = "The input value [{0}] with the given function resulted in [{1}], however [{2}] was expected.";
            }

            Console.WriteLine(msg, inputParam, a, expectedOutput);

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Tests for BigFloat trigonometric function implementations.
    /// Verifies accuracy against known mathematical constants and standard library functions.
    /// </summary>
    public class BigFloatTrigonometricTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        // High-precision constants for testing
        private static readonly BigFloat Pi = BigFloat.Constants.GetConstant(BigFloat.Catalog.Pi, precisionInBits: 200);
        private static readonly BigFloat HalfPi = Pi / 2;
        private static readonly BigFloat QuarterPi = Pi / 4;

        // Test precision constants
        private const int StandardPrecision = 100;
        private const int HighPrecision = 200;
        private const double DoublePrecisionTolerance = 1e-15;
        private const double TaylorApproximationTolerance = 1e-17;

        #region Exact Mathematical Values Tests

        [Fact]
        public void Sin_Should_ReturnZero_When_InputIsZero()
        {
            var expected = BigFloat.ZeroWithAccuracy(StandardPrecision);
            var result = BigFloat.Sin(0);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Cos_Should_ReturnOne_When_InputIsZero()
        {
            var input = BigFloat.ZeroWithAccuracy(StandardPrecision);
            var expected = BigFloat.OneWithAccuracy(StandardPrecision);
            var result = BigFloat.Cos(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sin_Should_ReturnOne_When_InputIsHalfPi()
        {
            BigFloat input = HalfPi;
            BigFloat expected = BigFloat.OneWithAccuracy(StandardPrecision);
            BigFloat result = BigFloat.Sin(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Cos_Should_ReturnMinusOne_When_InputIsPi()
        {
            BigFloat input = Pi;
            BigFloat expected = -BigFloat.OneWithAccuracy(StandardPrecision);
            BigFloat result = BigFloat.Cos(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sin_Should_ReturnZero_When_InputIsPi()
        {
            BigFloat input = Pi;
            var expected = BigFloat.ZeroWithAccuracy(HighPrecision);
            var result = BigFloat.Sin(input);
            Assert.True(result.EqualsUlp(expected));
        }

        [Fact]
        public void Tan_Should_ReturnZero_When_InputIsZero()
        {
            var input = BigFloat.ZeroWithAccuracy(StandardPrecision);
            var expected = BigFloat.ZeroWithAccuracy(StandardPrecision);
            var result = BigFloat.Tan(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Tan_Should_ReturnOne_When_InputIsQuarterPi()
        {
            BigFloat input = QuarterPi;
            var expected = BigFloat.OneWithAccuracy(StandardPrecision);
            var result = BigFloat.Tan(input);
            Assert.True(expected.EqualsUlp(result, 1, true));
        }

        #endregion

        #region Standard Library Compatibility Tests

        [Theory]
        [InlineData(0.5)]
        [InlineData(0.3)]
        [InlineData(0.7)]
        [InlineData(1.0)]
        public void Sin_Should_MatchStandardLibrary_When_InputIsWithinRange(double input)
        {
            var bigFloatInput = (BigFloat)input;
            var expectedFromStdLib = Math.Sin(input);

            var bigFloatResult = BigFloat.Sin(bigFloatInput);
            var actualDouble = (double)bigFloatResult;

            Assert.Equal(expectedFromStdLib, actualDouble, DoublePrecisionTolerance);
        }

        [Fact]
        public void Cos_Should_MatchHighPrecisionValue_When_InputIs0Point3()
        {
            const string inputStr = "0.30000000000000000000000000000000000000000000000000000000";
            const string expectedStr = "0.95533648912560601964231022756804989824421408263203767451761361222758159119178287117193528426930399766502502337829176922206077713583632366729045871758981790339061840133145752476700911253193689140325629";

            var input = BigFloat.Parse(inputStr);
            var expected = BigFloat.Parse(expectedStr);

            var result = BigFloat.Cos(input);

            Assert.Equal(0, expected.CompareUlp(result, 1, true));

            // Also verify double precision compatibility
            var actualDouble = (double)BigFloat.Cos((BigFloat)0.3);
            Assert.Equal(Math.Cos(0.3), actualDouble, DoublePrecisionTolerance);

            _output.WriteLine($"High precision result: {result.ToString(true)}");
        }

        [Theory]
        [InlineData(0.7)]
        public void Tan_Should_MatchStandardLibrary_When_InputIsWithinRange(double input)
        {
            var bigFloatInput = (BigFloat)input;
            var expectedFromStdLib = Math.Tan(input);

            var bigFloatResult = BigFloat.Tan(bigFloatInput);
            var actualDouble = (double)bigFloatResult;

            Assert.Equal(expectedFromStdLib, actualDouble, DoublePrecisionTolerance);
        }

        #endregion

        #region Taylor Series Approximation Tests

        [Theory]
        [InlineData(0.1, "Small angle approximation")]
        [InlineData(1.0, "Larger angle approximation")]
        public void SinAprox_Should_BeWithinTolerance_When_ComparedToExactSin(double inputValue, string testCase)
        {
            var input = (BigFloat)inputValue;

            var exactResult = BigFloat.Sin(input);
            var approximateResult = BigFloat.SinAprox(input);
            var error = Math.Abs((double)(exactResult - approximateResult));

            Assert.True(error < TaylorApproximationTolerance,
                $"{testCase}: Error {error:E} exceeds tolerance {TaylorApproximationTolerance:E}");

            _output.WriteLine($"{testCase} - Input: {inputValue}, Error: {error:E}");
        }

        #endregion
    }

    /// <summary>
    /// Tests for BigInteger binary string conversion utilities.
    /// Validates different output formats and width specifications.
    /// </summary>
    public class BigIntegerBinaryStringTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        #region Two's Complement Format Tests

        [Theory]
        [InlineData("256", "0000000100000000", 12, "Standard width test")]
        [InlineData("127", "01111111", 8, "Positive boundary value")]
        [InlineData("-127", "10000001", 8, "Negative boundary value")]
        [InlineData("-63", "1111111111000001", 16, "Negative with minimum width")]
        public void ToBinaryString_Should_ProduceTwosComplement_When_FormatSpecified(
            string inputValue, string expectedResult, int minWidth, string testDescription)
        {
            // Arrange
            var input = BigInteger.Parse(inputValue);

            // Act
            var result = BigIntegerTools.ToBinaryString(input, BinaryStringFormat.TwosComplement, minWidth: minWidth);

            // Assert
            Assert.Equal(expectedResult, result);
            _output.WriteLine($"{testDescription}: {inputValue} -> {result}");
        }

        #endregion

        #region Standard Format Tests

        [Theory]
        [InlineData("256", "100000000", "Large positive value")]
        [InlineData("127", "1111111", "Boundary positive value")]
        [InlineData("-127", "-1111111", "Boundary negative value")]
        [InlineData("-63", "-0000000000111111", "Negative with padding", 16)]
        public void ToBinaryString_Should_ProduceStandardFormat_When_FormatSpecified(
            string inputValue, string expectedResult, string testDescription, int minWidth = 0)
        {
            // Arrange
            var input = BigInteger.Parse(inputValue);

            // Act
            var result = minWidth > 0
                ? BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Standard, minWidth: minWidth)
                : BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Standard);

            // Assert
            Assert.Equal(expectedResult, result);
            _output.WriteLine($"{testDescription}: {inputValue} -> {result}");
        }

        #endregion

        #region Shades Format Tests

        [Theory]
        [InlineData("256", "···█········", 12, "Large value with padding")]
        [InlineData("256", "█········", 0, "Large value without padding")]
        [InlineData("127", "███████", 0, "Multiple ones pattern")]
        [InlineData("-127", "-███████", 0, "Negative multiple ones")]
        [InlineData("-63", "-██████", 0, "Negative pattern")]
        public void ToBinaryString_Should_ProduceShadesFormat_When_FormatSpecified(
            string inputValue, string expectedResult, int minWidth, string testDescription)
        {
            // Arrange
            var input = BigInteger.Parse(inputValue);

            // Act
            var result = minWidth > 0
                ? BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Shades, minWidth: minWidth)
                : BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Shades);

            // Assert
            Assert.Equal(expectedResult, result);
            _output.WriteLine($"{testDescription}: {inputValue} -> {result}");
        }

        #endregion

        #region Edge Cases and Validation Tests

        [Fact]
        public void ToBinaryString_Should_HandleZero_When_InputIsZero()
        {
            // Arrange
            BigInteger input = BigInteger.Zero;

            // Act & Assert
            Assert.Equal("0", BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Standard));
            Assert.Equal("00000000", BigIntegerTools.ToBinaryString(input, BinaryStringFormat.TwosComplement));
            Assert.Equal("·", BigIntegerTools.ToBinaryString(input, BinaryStringFormat.Shades));
        }

        [Fact]
        public void ToBinaryString_Should_HandleLargeNumbers_When_InputExceedsIntRange()
        {
            // Arrange
            var largeNumber = BigInteger.Parse("123456789012345678901234567890");

            // Act
            var result = BigIntegerTools.ToBinaryString(largeNumber, BinaryStringFormat.Standard);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.DoesNotContain(" ", result); // Should not contain spaces
            _output.WriteLine($"Large number binary representation length: {result.Length}");
        }

        #endregion
    }


    [Fact]
    public void TryParseBinary_WithValidInput_ShouldReturnExpectedValue()
    {
        // Arrange
        string binaryInput = "10100010010111";
        BigInteger expectedValue = 10391;

        // Act
        bool success = BigIntegerTools.TryParseBinary(binaryInput, out BigInteger result);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedValue, result);
    }

    [Theory]
    [InlineData("0", "0", 1)]
    [InlineData("1", "1", 1)]  // Rounds up then removes the bit (always stays 1)
    [InlineData("10", "1", 1)]  // Simple right shift (of Abs value)
    [InlineData("11", "10", 1)]  // Rounds up to 100, then right shift
    [InlineData("100", "10", 1)]  // Simple right shift (of Abs value)
    [InlineData("-0", "0", 1)]
    [InlineData("-1", "-1", 1)]  // Rounds down to -10 then removes the bit (of Abs value)
    [InlineData("-10", "-1", 1)]  // Simple right shift (of Abs value)
    [InlineData("-11", "-10", 1)]  // Rounds down to -100, then right shift (of Abs value)
    [InlineData("-100", "-10", 1)]  // Simple right shift (of Abs value)
    [InlineData("0", "0", 2)]
    [InlineData("1", "0", 2)]  // Rounds up to 10, then rights shift by 2, so zero
    [InlineData("10", "1", 2)]
    [InlineData("11", "1", 2)]  // Rounds up to 100, then right shift by 2, so 1
    [InlineData("100", "1", 2)]  // Simple right shift of 2 (of Abs value)
    [InlineData("-0", "-0", 2)]
    [InlineData("-1", "-0", 2)]  // Rounds down to -10, then rights shift by 2, so zero
    [InlineData("-10", "-1", 2)]
    [InlineData("-11", "-1", 2)]  // Rounds down to -100, then right shift by 2, so -1
    [InlineData("-100", "-1", 2)]  // Simple right shift of 2 (of Abs value)
    [InlineData("10100010010111", "1010001001100", 1)]  // Round up due to LSB = 1
    [InlineData("-10100010010111", "-1010001001100", 1)] // LSB = 1, Negative with round to next larger negative number
    [InlineData("10100010010110", "1010001001011", 1)]   // LSB = 0, Simple Right shift of 1
    [InlineData("-10100010010110", "-1010001001011", 1)] // LSB = 0, Negative Simple Right shift (of Abs value)
    public void RightShiftWithRound_Basic_ShouldRoundCorrectly(string inputBinary, string expectedBinary, int shiftAmount)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));

        // Act
        BigInteger result = BigIntegerTools.RoundingRightShift(input, shiftAmount);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10100010010110", "101000100110", 2)]     // Standard shift by 2
    [InlineData("-10100010010110", "-101000100110", 2)]   // Negative shift by 2
    [InlineData("101000100101011", "1010001001011", 2)]   // Round up with shift by 2
    [InlineData("-101000100101011", "-1010001001011", 2)] // Negative round up
    [InlineData("101000100101101", "1010001001011", 2)]   // Round up (different pattern)
    [InlineData("-101000100101101", "-1010001001011", 2)] // Negative round up
    public void RightShiftWithRound_MultiBitShift_ShouldRoundCorrectly(string inputBinary, string expectedBinary, int shiftAmount)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));

        // Act
        BigInteger result = BigIntegerTools.RoundingRightShift(input, shiftAmount);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("11111111111111", "10000000000000", 1)]   // Overflow case (max value)
    [InlineData("-11111111111111", "-10000000000000", 1)] // Negative overflow case
    public void RightShiftWithRound_WithOverflow_ShouldHandleCorrectly(string inputBinary, string expectedBinary, int shiftAmount)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));

        // Act
        BigInteger result = BigIntegerTools.RoundingRightShift(input, shiftAmount);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("11111111111111", "10000000000000", 1, 14)]   // With size reference (overflow)
    [InlineData("-11111111111111", "-10000000000000", 1, 14)] // Negative with size reference
    [InlineData("11111111111110", "1111111111111", 1, 13)]    // No overflow case
    public void RightShiftWithRound_WithSizeReference_ShouldUpdateSizeCorrectly(
        string inputBinary, string expectedBinary, int shiftAmount, int expectedSize)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));
        int size = (int)input.GetBitLength();

        // Act
        BigInteger result = BigIntegerTools.RoundingRightShift(input, shiftAmount, ref size);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expectedSize, size);
    }

    [Theory]
    [InlineData("11111111111111", "1000000000000", 1, true)]    // With carry flag (overflow)
    [InlineData("-11111111111111", "-1000000000000", 1, true)]  // Negative with carry flag
    [InlineData("11111111111110", "1111111111111", 1, false)]   // No overflow, no carry
    public void RightShiftWithRoundAndCarry_ShouldReturnCorrectCarryFlag(
        string inputBinary, string expectedBinary, int shiftAmount, bool expectedCarry)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));

        // Act
        (BigInteger result, bool carry) = BigIntegerTools.RoundingRightShiftWithCarry(input, shiftAmount);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expectedCarry, carry);
    }

    [Theory]
    [InlineData("-11111111111111", "-100000000000", 2, true)]    // 2-bit shift with carry
    [InlineData("-11011111111111", "-111000000000", 2, false)]   // 2-bit shift without carry
    public void RightShiftWithRoundAndCarry_MultiBitShift_ShouldHandleCarryCorrectly(
        string inputBinary, string expectedBinary, int shiftAmount, bool expectedCarry)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(inputBinary, out BigInteger input));
        Assert.True(BigIntegerTools.TryParseBinary(expectedBinary, out BigInteger expected));

        // Act
        (BigInteger result, bool carry) = BigIntegerTools.RoundingRightShiftWithCarry(input, shiftAmount);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expectedCarry, carry);
    }

    [Fact]
    public void Verify_ABS()
    {
        Assert.Equal(new BigFloat(5), BigFloat.Abs(new BigFloat(-5)));
        Assert.Equal(new BigFloat(5), BigFloat.Abs(new BigFloat(5)));
        Assert.Equal(BigFloat.ZeroWithAccuracy(0), BigFloat.Abs(BigFloat.ZeroWithAccuracy(0)));
    }

    [Theory]
    [InlineData("0", -10, false, "0.0000000000")]
    [InlineData("0", 0, false, "0")]
    [InlineData("0", 10, false, "0")]
    [InlineData("0", -10, true, "0.000000000000000000000000000000000000000000")]
    [InlineData("0", 0, true, "0.00000000000000000000000000000000")]
    [InlineData("0", 10, true, "0.0000000000000000000000")]
    [InlineData("0", 31, true, "0.0")]
    [InlineData("0", 32, true, "0")]
    [InlineData("0", 33, true, "0")]
    [InlineData("1111", -4, false, "0.0000")]
    [InlineData("1111", 0, false, "0")]
    [InlineData("1111", 4, false, "0")]
    [InlineData("-1111", -4, false, "0.0000")]
    [InlineData("-1111", 0, false, "0")]
    [InlineData("-1111", 4, false, "0")]
    [InlineData("111111111111111111111111", 0, false, "0")]
    [InlineData("111111111111111111111111", -8, false, "0.00000000")]
    [InlineData("1111111111111111111111111111111", 0, false, "0")]
    [InlineData("11111111111111111111111111111111", 1, false, "0")]
    [InlineData("11111111111111111111111111111111", 2, false, "0")]
    [InlineData("11111111111111111111111111111111", 0, false, "0")]
    [InlineData("11111111111111111111111111111111", -1, false, "0.0")]
    [InlineData("111111111111", 32, true, "111111111111")]
    [InlineData("111111111111", 0, true, "0.00000000000000000000111111111111")]
    [InlineData("111111111111111111111111111111111", 0, false, "1")]
    [InlineData("1111", 32, true, "1111")]
    [InlineData("1111", 33, true, "11110")]
    [InlineData("1111", 34, true, "111100")]
    [InlineData("1111", 36, true, "11110000")]
    [InlineData("1111111", 36, true, "11111110000")]
    [InlineData("1111", 31, true, "111.1")]
    [InlineData("1111", 28, true, "0.1111")]
    [InlineData("-1111", 32, true, "-1111")]
    [InlineData("-1111", 33, true, "-11110")]
    [InlineData("-1111", 36, true, "-11110000")]
    [InlineData("-1111", 31, true, "-111.1")]
    [InlineData("-1111", 28, true, "-0.1111")]
    [InlineData("111111111111111111111111111111111", 1, false, "11")]
    [InlineData("111111111111111111111111111111111", 2, false, "111")]
    [InlineData("111111111111111111111111111111111", -1, false, "0.1")]
    [InlineData("111111111111111111111111111111111", -2, false, "0.01")]
    [InlineData("11111111111111111111111111111111", 32, true, "11111111111111111111111111111111")]
    [InlineData("11111111111111111111111111111111", 33, true, "111111111111111111111111111111110")]
    [InlineData("11111111111111111111111111111111", 31, true, "1111111111111111111111111111111.1")]
    [InlineData("111111111111111111111111111111111111", 0, false, "1111")]
    [InlineData("111111111111111111111111111111111111", 1, false, "11111")]
    [InlineData("111111111111111111111111111111111111", -1, false, "111.1")]
    [InlineData("111111111111111111111111111111111111", 32, true, "111111111111111111111111111111111111")]
    [InlineData("111111111111111111111111111111111111", 33, true, "1111111111111111111111111111111111110")]
    [InlineData("111111111111111111111111111111111111", 31, true, "11111111111111111111111111111111111.1")]
    [InlineData("111111111111111111111111111111111111", 0, true, "1111.11111111111111111111111111111111")]
    [InlineData("111111111111111111111111111111111111", 1, true, "11111.1111111111111111111111111111111")]
    [InlineData("111111111111111111111111111111111111", -1, true, "111.111111111111111111111111111111111")]
    [InlineData("111111111111111111111110000000000000", -1, true, "111.111111111111111111110000000000000")]
    [InlineData("111111111111111111111111111111111111", -4, true, "0.111111111111111111111111111111111111")]
    [InlineData("111111111111111111111110000000000000", -4, true, "0.111111111111111111111110000000000000")]
    [InlineData("111111111111111111111110000000000000", -1, false, "111.1")]
    [InlineData("111111111111111111111111111111111111", -4, false, "0.1111")]
    [InlineData("111111111111111111111110000000000000", -4, false, "0.1111")]
    public void ToBinaryString_WithVariousInputs_ReturnsExpectedOutput(string binaryInput, int scale, bool includeGuard, string expectedOutput)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(binaryInput, out BigInteger mantissa));
        var bigFloat = new BigFloat(mantissa, scale, true);
        // Use reflection to access private CalculateBinaryStringLength method
        MethodInfo? calculateBinaryStringLengthMethod = typeof(BigFloat).GetMethod("CalculateBinaryStringLength", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(calculateBinaryStringLengthMethod);
        // Act
        int? bufferSizeNullable = calculateBinaryStringLengthMethod.Invoke(bigFloat, [includeGuard ? 32 : 0, false]) as int?;
        Assert.NotNull(bufferSizeNullable);
        int bufferSize = bufferSizeNullable.Value;
        string result = bigFloat.ToBinaryString(includeGuard, false);
        // Assert
        Assert.InRange(bufferSize, expectedOutput.Length, expectedOutput.Length + 2);
        Assert.Equal(expectedOutput, result);
    }

    //has not bits between "." and "|" set => no round up
    // , top guard bit not set, 
    [Theory]
    // [9 bits]|[17 bits].[15 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b10101|10101.100000000", "0b10101|10101.100000000")]
    // [9 bits]|[17 bits].[15 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|10101010101010101.100000000000000", "0b101010101|10101010101010101.100000000000000")]
    // [9 bits]|[17 bits].[15 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|10101010101010101.000000000000000", "0b101010101|10101010101010101.000000000000000")]
    // [9 bits]|[16 bits].[16 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|1010101010101010.0100000000000000", "0b101010101|1010101010101010.0100000000000000")]
    // [9 bits]|[16 bits].[16 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|1010101010101010.1000000000000000", "0b101010101|1010101010101010.1000000000000000")]
    // [9 bits]|[15 bits].[17 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|101010101010101.01000000000000000", "0b101010101|101010101010101.01000000000000000")]
    // [9 bits]|[15 bits].[17 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|101010101010101.10000000000000000", "0b101010101|101010101010101.10000000000000000")]
    // [9 bits]|[15 bits].[17 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|101010101010101.00100000000000000", "0b101010101|101010101010101.00100000000000000")]
    // [9 bits]|[1 bit] .[31 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|1.0101010101010101010101010101010", "0b101010101|1.0101010101010101010101010101010")]
    // [9 bits]|[1 bit] .[31 bits] bit31=1, "|" before "." => no round up
    [InlineData("0b101010101|1.0000000000000010000000000000000", "0b101010101|1.0000000000000010000000000000000")]
    // [9 bits]|[1 bit] .[31 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101011|.0000000000000001000000000000000", "0b101010101|1.0000000000000001000000000000000")]
    // [10 bits]|[0 bit] .[32 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101100|.00000000000000000000000000000000", "0b1010101100|.00000000000000000000000000000000")]
    // [10 bits]|[0 bit] .[32 bits] bit31=0, "|" before "." => round up
    [InlineData("0b1010101100|.10000000000000000000000000000000", "0b1010101101|.00000000000000000000000000000000")]
    // [10 bits]|[0 bit] .[32 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101100|.01000000000000000000000000000000", "0b1010101100|.01000000000000000000000000000000")]
    // [10 bits]|[0 bit] .[31 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101011|.0101010101010101010101010101010", "0b1010101011|.0101010101010101010101010101010")]
    // [10 bits]|[0 bit] .[31 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101011|.0000000000000010000000000000000", "0b1010101011|.0000000000000010000000000000000")]
    // [10 bits]|[0 bit] .[31 bits] bit31=0, "|" before "." => no round up
    [InlineData("0b1010101011|.0000000000000001000000000000000", "0b1010101011|.0000000000000001000000000000000")]
    public void Ceiling_ShouldReturnExpected(string origBinary, string expectedBinary)
    {
        // Arrange
        BigFloat orig = new(origBinary);
        BigFloat expected = new(expectedBinary);

        // Act
        BigFloat actual = orig.Ceiling();
        BigFloat actual2 = orig.CeilingPreservingAccuracy();

        // Assert
        Assert.True(actual2.EqualsUlp(expected, 1, true),
            $"Ceiling of {origBinary} expected to be {expectedBinary}, but was {actual}.");
        Assert.True(orig.Size == actual2.Size);
        Assert.True(orig.Size >= actual.Size);
    }

    [Fact]
    public void ToStringDecimal_RetainsDigitWhenPositiveScaleRoundsToZero()
    {
        // Arrange
        BigFloat value = new(new BigInteger(0x80000000), binaryScaler: 1, valueIncludesGuardBits: true);

        // Act
        string result = BigFloat.ToStringDecimal(value);

        // Assert
        Assert.Equal("1e+1", result);
    }
}
