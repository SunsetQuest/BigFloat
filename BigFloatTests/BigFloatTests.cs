// Copyright Ryan Scott White. 2020, 2021, 2022, 2023, 2024, 2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Written by human hand - unless noted. This may change in the future. Code written by Ryan Scott White unless otherwise noted.

using BigFloatLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace BigFloatTests;

// Arrange, Act, Assert

// important notes:
//  Equals() only compares the bits that are in precision.
//  A "tick" is considered an increment of a BigFloats current precision data. An increment of 123<<3 would 1<<3.
//  A number is considered in tolerance for any number within +/- one half tick. So, 123<<3 can represent 122.5<<3 (inclusive) to 123.5<<3 (exclusive)

[TestClass]
public class BigFloatTests
{
    /// <summary>
    /// Target time for each test. Time based on release mode on 16 core x64 CPU.
    /// </summary>
    private readonly int TestTargetInMillseconds = 500;

#if DEBUG
    const int MaxDegreeOfParallelism = 1;
    const long sqrtBruteForceStoppedAt = 262144;
    const long inverseBruteForceStoppedAt = 262144;
#else
    int MaxDegreeOfParallelism = Environment.ProcessorCount;
    const long sqrtBruteForceStoppedAt = 524288;
    const long inverseBruteForceStoppedAt = 524288;

#endif

    private const int RAND_SEED = 22;// new Random().Next();


    [TestMethod]
    public void Verify_Misc()
    {
        // Make sure that a "1" bit never rounds to zero in a ToString or OutOfPrecision operation.
        for (int i = 0; i > -1073; i--)
        {
            // issues at BigFloat(1, -1071).ToString() and BigFloat(1, -8).ToString()
            BigFloat value = new(1, i);
            IsFalse(new BigFloat(value.ToString()).IsOutOfPrecision, "0.000...1->BigFloat->String->BigInteger should never be zero.");
            IsFalse(value.IsOutOfPrecision, "0.000...1->BigFloat->OutOfPrecision should never be zero.");
        }

        IsTrue(new BigFloat(1, -8) % 1 == 0); // 0.00390625 % 01
        IsTrue((new BigFloat(1, -1074) % 1) == 0);   // 0   == 0.000...001
        IsFalse(new BigFloat(0, 0) == new BigFloat(-4503599627370496, -52));  // 0 != -1.0

        BigFloat temp = new(ulong.MaxValue);
        temp++;
        IsTrue(temp == new BigFloat(1, 64));
        IsTrue(temp == new BigFloat("18446744073709551616"));

        // Very simple test of GetHashCode();
        HashSet<int> hashSet = [];
        for (int i = -100; i < 100; i++)
        {
            BigFloat bf = new(i);
            int hash = bf.GetHashCode();
            IsTrue(hashSet.Add(hash), "Duplicate found with only 200 values.");
        }
    }

    [TestMethod]
    public void Verify_DebuggerDisplay()
    {
        BigFloat bf;

        bf = new BigFloat(-14566005701624942, 96, true);
        //1154037866912041841546539185052621408946880512 or  33bfb47ba4446e000000000000000000000000
        // 14566005701624942 << 96  or 33bfb47ba4446e << 96
        //is -268695379354069438191721957422006272 but only 16 precision digits so -2686953793540694e+20, 
        //Correct: -2686953793540694e+20, -0x33BFB4:7BA4446E[22+32=54], << 96
        IsTrue(bf.DebuggerDisplay == "-2686953793540694e+20, -0x33BFB4:7BA4446E[22+32=54], << 96");

        bf = new BigFloat(BigInteger.Parse("0CC404B845BBB924A88E39E", NumberStyles.AllowHexSpecifier), 96, true);
        // 246924491699516410027369374 x 18446744073709551616(for 64 up-shift) = 4554952903911797705753984222769658845550608384
        //is -4554952903911797705753984222769658845550608384 but only first ____ precision digits
        //Correct: 45549529039117977057539842e+20,  0x0CC404B845BBB92:4A88E39E[56+32=88], << 96
        IsTrue(bf.DebuggerDisplay == "45549529039117977057539842e+20,  0x0CC404B845BBB92:4A88E39E[56+32=88], << 96");
    }

    [TestMethod]
    public void Verify_BitwiseComplementOperator()
    {
        BigFloat a = BigFloat.ParseBinary("10.111");
        BigFloat expectedAns = BigFloat.ParseBinary(" 1.000 11111111111111111111111111111111", includesHiddenBits: 32);
        IsTrue(~a == expectedAns);

        _ = BigFloat.TryParseBinary("1100110110110", out a);
        _ = BigFloat.TryParseBinary("  11001001001.11111111111111111111111111111111", out expectedAns, includesHiddenBits: 32);
        IsTrue(~a == expectedAns);

        _ = BigFloat.TryParseBinary("11001001001.11111111111111111111111111111111", out a, includesHiddenBits: 32);
        _ = BigFloat.TryParseBinary("  110110110", out expectedAns);
        IsTrue(~a == expectedAns);

        _ = BigFloat.TryParseBinary("1100110110110", out a);
        _ = BigFloat.TryParseBinary("    110110110", out expectedAns);
        IsTrue(~~a == expectedAns);
    }

    [TestMethod]
    public void Verify_BigConstants_Pi()
    {
        _ = TestTargetInMillseconds switch
        {
            >= 141 => 8000,
            >= 15 => 4000,
            >= 8 => 3000,
            _ => 2000,
        };
        int MAX_INT = 2000;
        BigFloat.BigConstants bigConstants = new(MAX_INT);
        BigFloat pi200ref = bigConstants.Pi;
        BigFloat pi200gen = BigFloat.BigConstants.GeneratePi(MAX_INT);
        IsTrue(pi200ref == pi200gen, $"We got some Pi in our face. The generated pi does not match the literal constant Pi.");
        IsTrue(pi200ref == BigFloat.BigConstants.GeneratePi(0), $"Issue with BigConstants.GeneratePi(0)   ");
        IsTrue(pi200ref == BigFloat.BigConstants.GeneratePi(1), $"Issue with BigConstants.GeneratePi(1)   ");
        IsTrue(pi200ref == BigFloat.BigConstants.GeneratePi(2), $"Issue with BigConstants.GeneratePi(2)   ");
        for (int i = 3; i < MAX_INT; i *= 3)
        {
            IsTrue(pi200ref == BigFloat.BigConstants.GeneratePi(i), $"Issue with BigConstants.GeneratePi({i})   ");
        }
    }

    [TestMethod]
    public void Verify_BigConstants_GenerateArrayOfCommonConstants()
    {
        BigFloat[] bigFloats1000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        BigFloat[] bigFloats2000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        for (int i = 0; i < bigFloats1000.Length; i++)
        {
            BigFloat bf1000 = bigFloats1000[i];
            BigFloat bf2000 = bigFloats2000[i];
            IsTrue(bf1000 == bf2000, $"Issue with GenerateArrayOfCommonConstants");
        }
    }

    [TestMethod]
    public void Verify_BigConstants()
    {
        BigFloat.BigConstants bigConstants = new(200);
        BigFloat bigFloatTotal =
            bigConstants.PrimeConstant +
            bigConstants.NaturalLogarithm +
            bigConstants.OmegaConstant +
            bigConstants.EulerMascheroniConstant +
            bigConstants.LemniscateConstant +
            bigConstants.TwinPrimeConstant +
            bigConstants.CatalanConstant +
            bigConstants.PlasticNumber +
            bigConstants.PisotsConstant +
            bigConstants.Sqrt2 +
            bigConstants.FineStructureConstant +
            bigConstants.GoldenRatio +
            bigConstants.TheodorusConstant_Sqrt3 +
            bigConstants.Sqrt_Pi +
            bigConstants.KhintchinesConstant +
            bigConstants.E +
            bigConstants.Pi;

        double doubleTotal =
            0.414682509851111660248 +
            0.481211825059603447497 +
            0.567143290409783872999 +
            0.577215664901532860606 +
            0.599070117367796103719 +
            .6601618158468695739278 +
            0.915965594177219015054 +
            1.324717957244746025960 +
            1.380277569097614115673 +
            Math.Sqrt(2.0) +
            1.460354508809586812889 +
            1.618033988749894848204 +
            1.732050807568877293527 +
            Math.Sqrt(Math.PI) +
            2.685452001065306445309 +
            Math.E +
            Math.PI;

        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(bigFloatTotal, (BigFloat)doubleTotal, 2) == 0, "Fail on Verify_BigConstants");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(bigFloatTotal, (BigFloat)doubleTotal, 1) == 0, "Fail on Verify_BigConstants");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(bigFloatTotal, (BigFloat)doubleTotal, 0) == 0, "Fail on Verify_BigConstants");

        // double:            23.462879545477389
        // (BigFloat)double:  23.46287954547739, 
        // (double)bigFloat:  23.462879545477392
        // bigFloat(true ans):23.462879545477391625..

        // We got lucky that these matched since doubleTotal can be off by a bit or two. (i.e. OK to fail)
        BigFloat BigFloatdiff = bigFloatTotal - (BigFloat)doubleTotal;
        IsTrue(bigFloatTotal == (BigFloat)doubleTotal, "Fail on Verify_BigConstants");


        // following does not pass because of limitations of double. A number that is out-of-precision and Zero cannot be differentiated. 
        double BigFloatZeroToDoubleZero1 = (double)BigFloat.ZeroWithNoPrecision;
        double BigFloatZeroToDoubleZero2 = (double)BigFloat.ZeroWithSpecifiedLeastPrecision(50);

        double doubleDiff0 = (double)(bigFloatTotal - (BigFloat)doubleTotal);
        //IsTrue(doubleDiff0 <= acceptableTolarance, "Fail on Verify_BigConstants");

        double doubleDiff1 = doubleTotal - (double)bigFloatTotal;
        double acceptableTolarance = (double.BitIncrement(Math.PI) - Math.PI) * 8;

        // Since we are doing repetitive addition, it is expected that doubleTotal is off by a few bits.
        IsTrue(doubleDiff1 <= acceptableTolarance, "Fail on Verify_BigConstants");

        IsTrue(bigConstants.RamanujanSoldnerConstant == (BigFloat)262537412640768743.99999999999925, "Fail on Verify_BigConstants");
        IsTrue((double)bigConstants.RamanujanSoldnerConstant == 262537412640768743.99999999999925, "Fail on Verify_BigConstants");

        bool success = BigFloat.BigConstantBuilder.Const_0_030725404776.TryGetAsBigFloat(out BigFloat bf, 100);
        IsTrue(success && bf.CompareToExact(BigFloat.ParseBinary("0.00000111110111011001111011000000101010111101010001010111011011000001001000011010001000110100111100100000100110000111010011010100111111")) == 0);

        success = BigFloat.BigConstantBuilder.Const_0_414682509851.TryGetAsBigFloat(out bf, 200);
        IsTrue(success && bf.CompareToExact(BigFloat.ParseBinary("0.0110101000101000101000100000101000001000101000100000100000101000001000101000001000100000100000001000101000101000100000000000001000100000101000000000101000001000001000100000100000101000000000101000101000000000001000000000001000100001111")) == 0);

        success = BigFloat.BigConstantBuilder.Const_0_567143290409.TryGetAsBigFloat(out bf, 200);
        IsTrue(success && bf.CompareToExact(BigFloat.ParseBinary("0.1001000100110000010011010111110001110100101100101011101001011110101011111101110110101010011000101000011011011100001010001110000101101110100001101110110011101000010101110001101010001000000011001001001111110111000000110101001001100110")) == 0);

        success = BigFloat.BigConstantBuilder.Const_1_414213562373.TryGetAsBigFloat(out bf, 200);
        IsTrue(success && bf.CompareToExact(BigFloat.ParseBinary("1.0110101000001001111001100110011111110011101111001100100100001000101100101111101100010011011001101110101010010101011111010011111000111010110111101100000101110101000100100111011101010000100110011101101000101111010110010000101011110101")) == 0);

        success = BigFloat.BigConstantBuilder.Const_2_685452001065.TryGetAsBigFloat(out bf, 200);
        IsTrue(success && bf.CompareToExact(BigFloat.ParseBinary("10.1010111101111001110010000100011110001101101000011010111011110010111111011111001111100011100101000110011001111111100111000011001110010010111000000010000111100110000110000010100111011101111110100100110110000111000001100011101011111100")) == 0);
    }

    //[TestMethod]
    //public void Verify_NthRoot_DRAFT()
    //{
    //    // TODO: More tests and ranges needed!  Bugs still present.  In a DRAFT State.
    //    // fails on large numbers like 100000000000000000000
    //    //                           

    //    for (int i = 0; i < 4; i++)
    //    {
    //        BigFloat bf_in = new("1" + string.Concat(System.Linq.Enumerable.Repeat("00000", i)));
    //        BigFloat bf_out = new("1" + new string('0', i));
    //        IsTrue(BigFloat.NthRootDRAFT7(bf_in, 5) == bf_out, $"Answer of {bf_in}^(1/5) is {bf_out}");
    //    }
    //    for (int i = 0; i < 4; i++)
    //    {
    //        BigFloat bf_in = new("1" + string.Concat(System.Linq.Enumerable.Repeat("000", i)));
    //        BigFloat bf_out = new("1" + new string('0', i));
    //        IsTrue(BigFloat.NthRootDRAFT7(bf_in, 3) == bf_out, $"Answer of {bf_in}^(1/5) is {bf_out}");
    //    }

    //    IsTrue(BigFloat.NthRootDRAFT7(100000000000, 2) == BigFloat.Parse("316227.7660168"), $"Answer of val^(1/2) is 316227.76601683793319");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000000, 3) == BigFloat.Parse("4641.588833612"), $"Answer of val^(1/3) is 4641.5888336127788924");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000000, 4) == BigFloat.Parse("562.3413251903"), $"Answer of val^(1/4) is 562.34132519034908039");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000000, 5) == BigFloat.Parse("158.4893192461"), $"Answer of val^(1/5) is 158.48931924611134852");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000000, 2) == BigFloat.Parse("100000.0000000"), $"Answer of val^(1/2) is 100000.00000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000000, 3) == BigFloat.Parse("2154.434690032"), $"Answer of val^(1/3) is 2154.4346900318837217");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000000, 4) == BigFloat.Parse("316.2277660168"), $"Answer of val^(1/4) is 316.22776601683793320");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000000, 5) == BigFloat.Parse("100.0000000000"), $"Answer of val^(1/5) is 100.00000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000000, 2) == BigFloat.Parse("31622.77660168"), $"Answer of val^(1/2) is 31622.776601683793319");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000000, 3) == BigFloat.Parse("1000.000000000"), $"Answer of val^(1/3) is 1000.0000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000000, 4) == BigFloat.Parse("177.8279410038"), $"Answer of val^(1/4) is 177.82794100389228012");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000000, 5) == BigFloat.Parse("63.09573444801"), $"Answer of val^(1/5) is 63.095734448019324943");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000, 2) == BigFloat.Parse("10000.00000000"), $"Answer of val^(1/2) is 10000.000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000, 3) == BigFloat.Parse("464.1588833612"), $"Answer of val^(1/3) is 464.15888336127788924");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000, 4) == BigFloat.Parse("100.0000000000"), $"Answer of val^(1/4) is 100.00000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(100000000, 5) == BigFloat.Parse("39.81071705534"), $"Answer of val^(1/5) is 39.810717055349725077");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000, 2) == BigFloat.Parse("3162.277660168"), $"Answer of val^(1/2) is 3162.2776601683793319");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000, 3) == BigFloat.Parse("215.4434690032"), $"Answer of val^(1/3) is 215.44346900318837217");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000, 4) == BigFloat.Parse("56.23413251903"), $"Answer of val^(1/4) is 56.234132519034908039");
    //    IsTrue(BigFloat.NthRootDRAFT7(10000000, 5) == BigFloat.Parse("25.11886431509"), $"Answer of val^(1/5) is 25.118864315095801111");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000, 2) == BigFloat.Parse("1000.000000000"), $"Answer of val^(1/2) is 1000.0000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000, 3) == BigFloat.Parse("100.0000000000"), $"Answer of val^(1/3) is 100.00000000000000000");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000, 4) == BigFloat.Parse("31.62277660168"), $"Answer of val^(1/4) is 31.622776601683793320");
    //    IsTrue(BigFloat.NthRootDRAFT7(1000000, 5) == BigFloat.Parse("15.84893192461"), $"Answer of val^(1/5) is 15.848931924611134852");
    //}

    [TestMethod]
    public void Verify_IsZero()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat b = BigFloat.ParseBinary("10000.0");
        IsTrue((a - b).IsZero);

        a = BigFloat.ParseBinary("100.0");
        b = BigFloat.ParseBinary("100.000");
        IsTrue((a - b).IsZero);

        a = BigFloat.ParseBinary("100.01");
        b = BigFloat.ParseBinary("100.000");
        IsFalse((a - b).IsZero);

        IsFalse(BigFloat.ParseBinary("1:111111111", -2, 0).IsZero); // (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("1:111111111", -1, 0).IsZero); // (no because _size >= ExtraHiddenBits-2) 
        IsFalse(BigFloat.ParseBinary("0:111111111", -1, 0).IsZero); // (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("0:111111111", 0, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2) 
        IsTrue(BigFloat.ParseBinary("0:011111111", -1, 0).IsZero); // (no because _size >= ExtraHiddenBits-2)                                            
        IsTrue(BigFloat.ParseBinary("0:011111111", 0, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2)
        IsFalse(BigFloat.ParseBinary("0:011111111", 1, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsTrue(BigFloat.ParseBinary("0:001111111", 1, 0).IsZero); //                                               (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("0:001111111", 2, 0).IsZero); //                                              (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsTrue(BigFloat.ParseBinary("0:000111111", 2, 0).IsZero); //
        IsFalse(BigFloat.ParseBinary("0:000111111", 3, 0).IsZero); //                                              (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("1:000000000", -2, 0).IsZero); // (no because _size >= ExtraHiddenBits-2) 
        IsFalse(BigFloat.ParseBinary("1:000000000", -1, 0).IsZero); // (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("1:000000000", 0, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("0:100000000", -1, 0).IsZero); // (no because _size >= ExtraHiddenBits-2)
        IsFalse(BigFloat.ParseBinary("0:100000000", 0, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        IsFalse(BigFloat.ParseBinary("0:100000000", 1, 0).IsZero); //  (no because _size >= ExtraHiddenBits-2) AND (no because (Scale + _size - ExtraHiddenBits) < 0)


        //  IntData    Scale  Precision  Zero
        //1:111111111 << -2       1       Y -1 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        //1:111111111 << -1       1       N  0 (no because _size >= ExtraHiddenBits-1) 
        //0:111111111 << -1       0       Y -1 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        //0:111111111 <<  0       0       N  0 (no because _size >= ExtraHiddenBits-1) 
        //0:011111111 << -1      -1       Y -2                                             
        //0:011111111 <<  0      -1       Y -1 (borderline)
        //0:011111111 <<  1      -1       N  0                                             (no because (Scale + _size - ExtraHiddenBits) < 0)
        //0:001111111 <<  1      -2       Y -1 (borderline)
        //0:001111111 <<  2      -2       N  0                                             (no because (Scale + _size - ExtraHiddenBits) < 0)
        //0:000111111 <<  2      -3       Y -1 (borderline)
        //0:000111111 <<  3      -3       N  0                                             (no because (Scale + _size - ExtraHiddenBits) < 0)
        //1:000000000 << -2       1       N -1 (no because _size >= ExtraHiddenBits-1) 
        //1:000000000 << -1       1       N  0 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        //1:000000000 <<  0       1       N  0 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        //0:100000000 << -1       0       N -1 (no because _size >= ExtraHiddenBits-1)
        //0:100000000 <<  0       0       N  0 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)
        //0:100000000 <<  1       0       N  1 (no because _size >= ExtraHiddenBits-1) AND (no because (Scale + _size - ExtraHiddenBits) < 0)

        IsFalse(BigFloat.ParseBinary("100.01").IsZero);
        IsFalse(BigFloat.ParseBinary("0.00000000000000000000001").IsZero);
        IsFalse(BigFloat.ParseBinary("-0.00000000000000000000001").IsZero);
        IsTrue(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("-0.00000000000000000000001", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("0.00001", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("0.000000000000001000", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("100000000", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("10000000000000000", 0, 0, 32).IsZero);
        IsTrue(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, 32).IsZero);
        IsFalse(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, 32).IsZero);
    }

    [TestMethod]
    public void Verify_IsStrictZero()
    {
        BigFloat result = ((BigFloat)1.3 * (BigFloat)2) - (BigFloat)2.6;
        IsFalse(result.IsStrictZero);
        IsTrue(result.IsZero);

        result = 0;
        IsTrue(result.IsStrictZero);
        IsTrue(result.IsZero);

        result = BigFloat.ParseBinary("1", 0, 0, 32);
        IsFalse(result.IsStrictZero);
        IsTrue(result.IsZero);

        result = BigFloat.ParseBinary("-1", 0, 0, 32);
        IsFalse(result.IsStrictZero);
        IsTrue(result.IsZero);
    }

    [TestMethod]
    public void Verify_GetPrecision()
    {
        //public int GetPrecision => _size - ExtraHiddenBits;
        IsTrue(BigFloat.ParseBinary("100.01").Precision == 5);
        IsTrue(BigFloat.ParseBinary("-0.00000000000000000000001").Precision == 1);
        IsTrue(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, 32).Precision == 1 - 32);
        IsTrue(BigFloat.ParseBinary("0.00001", 0, 0, 32).Precision == 1 - 32);
        IsTrue(BigFloat.ParseBinary("0.000000000000001000", 0, 0, 32).Precision == 4 - 32);
        IsTrue(BigFloat.ParseBinary("100000000", 0, 0, 32).Precision == 9 - 32);
        IsTrue(BigFloat.ParseBinary("10000000000000000", 0, 0, 32).Precision == 17 - 32);
        IsTrue(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, 32).Precision == 25 - 32);
        IsTrue(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, 32).Precision == 33 - 32);
    }

    [TestMethod]
    public void Verify_GetAccuracy()
    {
        //public int GetPrecision => _size - ExtraHiddenBits;
        IsTrue(BigFloat.ParseBinary("100.01").Accuracy == 2);
        IsTrue(BigFloat.ParseBinary("-0.00000000000000000000001").Accuracy == 23);
        IsTrue(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, 32).Accuracy == 23 - 32);
        IsTrue(BigFloat.ParseBinary("0.00001", 0, 0, 32).Accuracy == 5 - 32);
        IsTrue(BigFloat.ParseBinary("0.000000000000001000", 0, 0, 32).Accuracy == 18 - 32);     // 0|00000000000000.00000000000000000000001" (accuracy is -14)
        IsTrue(BigFloat.ParseBinary("100000000", 0, 0, 32).Accuracy == 32 - 32);                //0.|00000000000000000000000100000000        (accuracy is 0)
        IsTrue(BigFloat.ParseBinary("10000000000000000", 0, 0, 32).Accuracy == 32 - 32);        //0.|00000000000000010000000000000000        (accuracy is 0)
        IsTrue(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, 32).Accuracy == 32 - 32);//0.|00000001000000000000000000000000        (accuracy is 0)
        IsTrue(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, 32).Accuracy == 32 - 32);//1.|00000000000000000000000000000000(accuracy is 0)
    }

    [TestMethod]
    public void Verify_IsPositive()
    {
        //public bool IsPositive => Sign > 0;
        IsTrue(BigFloat.ParseBinary("100.01").IsPositive);
        IsTrue(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, 32).IsPositive);
        IsTrue(BigFloat.ParseBinary("10000000000000000000000000000000", 0, 0, 32).IsPositive);
        IsTrue(BigFloat.ParseBinary("1000000000000000000000000000000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("100000000000000000000000000000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("-100.01").IsPositive);
        IsFalse(BigFloat.ParseBinary("0.00001", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("0.000000000000001000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("100000000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("10000000000000000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, 32).IsPositive);
        IsFalse(BigFloat.ParseBinary("0.00000000000000000000000").IsPositive);
        IsFalse(BigFloat.ParseBinary("-0.00000000000000000000001").IsPositive);
        IsFalse(BigFloat.ParseBinary("-100000000000000000000000000000000", 0, 0, 32).IsPositive);
    }

    [TestMethod]
    public void Verify_IsNegative()
    {
        //public bool IsNegative => Sign < 0;
        IsTrue(BigFloat.ParseBinary("-0.00000000000000000000001").IsNegative);
        IsTrue(BigFloat.ParseBinary("-100000000000000000000000000000000", 0, 0, 32).IsNegative);
        IsTrue(BigFloat.ParseBinary("-10000000000000000000000000000000", 0, 0, 32).IsNegative);
        IsTrue(BigFloat.ParseBinary("-1000000000000000000000000000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("-100000000000000000000000000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("100.01").IsNegative);
        IsFalse(BigFloat.ParseBinary("0.00000000000000000000001", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("0.00001", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("0.000000000000001000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("100000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("10000000000000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("1000000000000000000000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("100000000000000000000000000000000", 0, 0, 32).IsNegative);
        IsFalse(BigFloat.ParseBinary("0.00000000000000000000000").IsNegative);
    }

    [TestMethod]
    public void Verify_LeftShift()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat expectedAnswer = BigFloat.ParseBinary("100000.0");
        IsTrue((a << 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("-10000.0");
        expectedAnswer = BigFloat.ParseBinary("-100000.0");
        IsTrue((a << 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("100000");
        expectedAnswer = BigFloat.ParseBinary("1000000");
        IsTrue((a << 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("0.000100000");
        IsTrue((a << 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("-0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("-0.000100000");
        IsTrue((a << 1).CompareToExact(expectedAnswer) == 0);
    }

    [TestMethod]
    public void Verify_RightShift()
    {
        BigFloat a = BigFloat.ParseBinary("10000.0");
        BigFloat expectedAnswer = BigFloat.ParseBinary("1000.0");
        IsTrue((a >> 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("-10000.0");
        expectedAnswer = BigFloat.ParseBinary("-1000.0");
        IsTrue((a >> 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("100000");
        expectedAnswer = BigFloat.ParseBinary("10000");
        IsTrue((a >> 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("0.00000100000");
        IsTrue((a >> 1).CompareToExact(expectedAnswer) == 0);

        a = BigFloat.ParseBinary("-0.0000100000");
        expectedAnswer = BigFloat.ParseBinary("-0.00000100000");
        IsTrue((a >> 1).CompareToExact(expectedAnswer) == 0);
    }

    [TestMethod]
    public void Verify_IsOneBitFollowedByZeroBits()
    {
        _ = BigFloat.TryParseBinary("10000.0", out BigFloat result);
        IsTrue(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("10000", out result);
        IsTrue(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("10000000000000000000000000.0000000000000000000000000000000000000000000000000000000000000000000000", out result);
        IsTrue(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("10101.1", out result);
        IsFalse(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("110000.0", out result);
        IsFalse(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("1100000", out result);
        IsFalse(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("110000000.0000000000000000000000000000000000000000000000000000000", out result);
        IsFalse(result.IsOneBitFollowedByZeroBits);

        _ = BigFloat.TryParseBinary("11000000000000", out result);
        IsFalse(result.IsOneBitFollowedByZeroBits);
    }

    [TestMethod]
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
            if (input == "")
            {
                input = "0";
            }

            string resultStr = Convert.ToString((long)result.Lowest64Bits, 2);

            IsTrue(resultStr == input);
        }
    }

    [TestMethod]
    public void Verify_IntWithAddedPrecision()
    {
        IsTrue(BigFloat.OneWithAccuracy(10).CompareToExact(BigFloat.One) == 0);
        IsTrue(BigFloat.IntWithAccuracy(1, 10).CompareToExact(BigFloat.One) == 0);
        IsTrue(BigFloat.IntWithAccuracy(2, 10).CompareToExact(new BigFloat(2)) == 0);

        BigFloat a = BigFloat.IntWithAccuracy(2, 10);
        IsTrue(a.DataBits == (BigInteger)2 << (32 + 10));
        IsTrue(a.Scale == -10);

        a = BigFloat.IntWithAccuracy(-32, 100);
        IsTrue(a.DataBits == -(BigInteger)32 << (32 + 100));
        IsTrue(a.Scale == -100);

        a = BigFloat.IntWithAccuracy(27, -15);
        IsTrue(a.DataBits == (BigInteger)27 << (32 - 15));
        IsTrue(a.Scale == 15);
    }

    [TestMethod]
    public void Verify_Inverse()
    {
        IsTrue(BigFloat.Inverse(new BigFloat("1.000")) == new BigFloat("1.000"), $"Failed on: Inverse(1.000)");
        IsTrue(BigFloat.Inverse(new BigFloat("2.000")) == new BigFloat("0.5000"), $"Failed on: Inverse(2.000)");
        // To-Do: reviewed this and it should pass - we need to update the compare function
        IsTrue(BigFloat.Inverse(new BigFloat("3.000")) == new BigFloat("0.3333"), $"Failed on: Inverse(3.000)");
        IsTrue(BigFloat.Inverse(new BigFloat("0.5000")) == new BigFloat("2.000"), $"Failed on: Inverse(0.5000)");
        IsTrue(BigFloat.Inverse(new BigFloat("0.3333")) == new BigFloat("3.000"), $"Failed on: Inverse(0.3333)");

        BigFloat a = new("0.33333333333333");
        BigFloat b = new("3.0000000000000"); // 3.0000000000000
        // a0:  0.01010101010101010101010101010101010101010101001011111100110110...   0.333333333333325
        // a1:  0.01010101010101010101010101010101010101010101010111001101011011...   0.333333333333335
        // a2:  11.0000000000000000000000000000000000000000000 1010100 3.00000000000007500
        // a3:  10.1111111111111111111111111111111111111111111 1101111 2.99999999999998500
        // a4:   0.0000000000000000000000000000000000000000000 1100101 0.00000000000009
        // b1:  10.1111111111111111111111111111111111111111111 1000111 2.99999999999995
        // b2:  11.0000000000000000000000000000000000000000000 0111000 3.00000000000005
        // b3:   0.0000000000000000000000000000000000000000000 1110000 0.0000000000001
        // true, by a small margin, because a4 == b3
        IsTrue(BigFloat.Inverse(a) == b, $"Failed on: Inverse(0.33333333333333)");

        a = new BigFloat("-0.333333333333333333333333333");
        b = new BigFloat("-3.00000000000000000000000000"); //3.00000000000000000000000000
        // a0: 0.01010101010101010101010101010101010101010101010101010101010101010101010101010101010101010 00100 0.3333333333333333333333333325
        // a1: 0.01010101010101010101010101010101010101010101010101010101010101010101010101010101010101010 11000 0.3333333333333333333333333335
        // a2:   11.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000 10010 3.0000000000000000000000000075 (0.3333333333333333333333333325)
        // a3:   10.11111111111111111111111111111111111111111111111111111111111111111111111111111111111111 11100 2.9999999999999999999999999985 (0.3333333333333333333333333335)
        // a4:    0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000 10110 0.000000000000000000000000009
        // b1:   11.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000 01100 3.000000000000000000000000005
        // b2:   10.11111111111111111111111111111111111111111111111111111111111111111111111111111111111111 10011 2.999999999999999999999999995
        // b3:    0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000 11000 0.00000000000000000000000001
        // would be true, by a small margin, because a4 == b3 
        // BUT 0.333333333333333333333333333 is stored with hidden bits being 0x00000000 so it comes up false
        IsTrue(BigFloat.Inverse(a) == b, $"Failed on: Inverse(-0.333333333333333333333333333)");

        b = new BigFloat("-3.0000"); // more true since too small (allows for larger tolerance)
        IsTrue(BigFloat.Inverse(a) == b, $"Failed on: Inverse(-0.333333333333333333333333333)");

        b = new BigFloat("-3.000000000000000000000000000"); // false
        IsTrue(BigFloat.Inverse(a) == b, $"Failed on: Inverse(-0.333333333333333333333333333)");

        IsTrue(BigFloat.Inverse(new BigFloat("7.9697706335180071911585875567198e-26")) == new BigFloat("12547412541514775369202510"), $"Failed on: Inverse(0.5000)");
    }

    [TestMethod]
    public void Verify_Log2Double()
    {
        BigFloat aaa = new("0b101"); // Initialize by String  2^59.5
        double ans = double.Log2((double)aaa);
        double res = BigFloat.Log2(aaa);
        //Answer: 2.321928094887362347870319429489390175864831393024580612054756...
        IsTrue(res == ans);

        aaa = new("-1");
        ans = double.Log2((double)aaa);
        IsTrue(double.IsNaN(BigFloat.Log2(aaa)));

        aaa = new("0");
        ans = double.Log2((double)aaa);
        IsTrue(double.IsNaN(BigFloat.Log2(aaa)));

        aaa = new("0b10110.1010000010011110011001100111111100111011110011001001000"); //pattern: 2^59.5
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);
        IsTrue(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("0b101111.111111111111111111111111111111111");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);

        aaa = new("999999999999999999999999999999999999999999999");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);
        IsTrue(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("0.000000000000000000000000000000000000000000000000000000000000123");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);
        IsTrue(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("1");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);

        aaa = new("123.123e+300");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);
        IsTrue(double.IsNaN(BigFloat.Log2(-aaa)));

        aaa = new("7777.7777e-300");
        ans = double.Log2((double)aaa);
        res = BigFloat.Log2(aaa);
        IsTrue(res == ans);
        IsTrue(double.IsNaN(BigFloat.Log2(-aaa)));


        // Result: 3321.92809488741    (using "1e+1000")
        // Result: 3321.9280948873625  (using "1.0000000000e+1000")
        // Answer: 3321.9280948873623478703194294893901758648313930245806120547563958  (using https://www.wolframalpha.com/input?i=log2%281e%2B1000%29)

        aaa = new("1e+1000");
        res = BigFloat.Log2(aaa);
        IsTrue(Regex.IsMatch(res.ToString(), @"3321\.928094887[34]\d*"));

        aaa = new("1.0000000000e+1000");
        res = BigFloat.Log2(aaa);
        IsTrue(Regex.IsMatch(res.ToString(), @"3321\.928094887362[2345]"));

        aaa = new("1e-1000");
        res = BigFloat.Log2(aaa);
        IsTrue(Regex.IsMatch(res.ToString(), @"-3321\.928094887[34]\d*"));

        aaa = new("1.0000000000e-1000");
        res = BigFloat.Log2(aaa);
        IsTrue(Regex.IsMatch(res.ToString(), @"-3321\.928094887362[2345]"));


    }


    //[TestMethod]
    //[ExpectedException(typeof(ArgumentException))]
    //public void Verify_Log2__ShouldFail1()
    //{
    //    BigFloat aaa = new("-1");

    //    double res = BigFloat.Log2(aaa);
    //}

    //[TestMethod]
    //[ExpectedException(typeof(ArgumentException))]
    //public void Verify_Log2_ShouldFail2()
    //{
    //    BigFloat aaa = new("0");
    //    double res = BigFloat.Log2(aaa);
    //}




    [TestMethod]
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
        IsTrue(zeroPos == 0);
        IsTrue(zeroPos.Size == 0);

        for (int i = 1; i < MAX_INT1; i++)
        {
            int sq = i * i;
            BigFloat resPos = BigFloat.PowerOf2(i);
            BigFloat resNeg = BigFloat.PowerOf2(-i);
            //int sqBitCt = (sizeof(uint)*8) - BitOperations.LeadingZeroCount((uint)sq);
            int sqBitCt = (int)BigInteger.Log2(i) + 1;
            IsTrue(resPos == sq);
            IsTrue(resNeg == sq);
            IsTrue(resPos.Size == sqBitCt);
            IsTrue(resNeg.Size == sqBitCt);
        }

        // 1, 10, 100, 1000, 10000....testing  (Out of Precision)
        for (int i = 1; i < 31; i++)
        {
            BigInteger bi = BigInteger.One << i;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, 32, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            IsTrue((BigInteger)resPos == sq);
            IsTrue((BigInteger)resNeg == sq);
            // next two would be false since resPos and resNeg are both out of precision.
            IsFalse(resPos == (BigFloat)sq);
            IsFalse(resNeg == (BigFloat)sq);

            int resSize = (int)bi.GetBitLength();
            IsTrue(resPos.SizeWithHiddenBits == resSize);
            IsTrue(resNeg.SizeWithHiddenBits == resSize);
        }

        // 1, 10, 100, 1000, 10000....testing (In Precision)
        for (int i = 31; i < MAX_INT2; i++)
        {
            BigInteger bi = BigInteger.One << i;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, 32, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            IsTrue((BigInteger)resPos == sq);
            IsTrue((BigInteger)resNeg == sq);
            IsTrue(resPos == (BigFloat)sq);
            IsTrue(resNeg == (BigFloat)sq);

            int resSize = (int)bi.GetBitLength();
            IsTrue(resPos.Size == Math.Max(0, resSize - BigFloat.ExtraHiddenBits));
            IsTrue(resNeg.Size == Math.Max(0, resSize - BigFloat.ExtraHiddenBits));
        }

        // 1, 11, 111, 1111... testing  (Out of Precision)
        for (int i = 2; i < 31; i++)
        {
            BigInteger bi = (BigInteger.One << i) - 1;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi << 1, 31, true);

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);
            IsFalse((BigInteger)resPos == sq);
            IsFalse((BigInteger)resNeg == sq);
            IsFalse(resPos == (BigFloat)sq);
            IsFalse(resNeg == (BigFloat)sq);

            int resSize = (int)bi.GetBitLength();
            IsTrue(resPos.SizeWithHiddenBits - 1 == resSize);
            IsTrue(resNeg.SizeWithHiddenBits - 1 == resSize);
        }

        // what about 31???

        // 1, 11, 111, 1111... testing  (In Precision)
        for (int i = 32; i < MAX_INT3; i++)
        {
            BigInteger bi = (BigInteger.One << i) - 1;
            BigInteger sq = bi * bi;

            BigFloat bf = new(bi, 16, true);

            // 31:   00:01111111111111111111111111111111  00111111111111111111111111111111|00000000000000000000000000000001 
            // 32:   00:11111111111111111111111111111111 
            // 33:   01:11111111111111111111111111111111 

            BigFloat resPos = BigFloat.PowerOf2(bf);
            BigFloat resNeg = BigFloat.PowerOf2(-bf);

            IsTrue(resPos == (BigFloat)(sq >> 32));
            IsTrue(resNeg == (BigFloat)(sq >> 32));

            int resSize = (int)bi.GetBitLength();
            IsTrue(resPos.Size == Math.Max(0, resSize - BigFloat.ExtraHiddenBits));
            IsTrue(resNeg.Size == Math.Max(0, resSize - BigFloat.ExtraHiddenBits));
        }

        {
            BigFloat bf = new(0x7FFFFFFF, 32, true);
            BigFloat bfSq = BigFloat.PowerOf2(bf);
            IsFalse(bfSq == (BigFloat)0x3FFFFFFF00000001);  // false because 0:7FFFFFFE it out of precision.

            bf = new BigFloat(0xFFFFFFFF, 32, true);
            bfSq = BigFloat.PowerOf2(bf);
            IsTrue(bfSq == (BigFloat)0xFFFFFFFE00000001);
        }
    }

    [TestMethod]
    public void Verify_PowInt()
    {
        for (int jj = 0; jj < 20; jj++)
        {
            for (double ii = 0.00001; ii < 100000; ii *= 1.01)
            {
                BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, jj);                    // Double->BigFloat->POW->String
                BigFloat POWToBigFloatLo = (BigFloat)double.Pow(Math.BitDecrement(ii), jj); // Double->POW->BigFloat->String
                BigFloat POWToBigFloatHi = (BigFloat)double.Pow(Math.BitIncrement(ii), jj); // Double->POW->BigFloat->String

                IsTrue(BigFloatToPOW >= POWToBigFloatLo && BigFloatToPOW <= POWToBigFloatHi,
                    $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}, " +
                    $"should be in the range {POWToBigFloatLo} to {POWToBigFloatHi}.");
            }
        }

        for (double ii = 0.00001; ii < 100000; ii *= 1.01)
        {
            BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, 0); // Double->BigFloat->POW->String
            BigFloat POWToBigFloat = (BigFloat)double.Pow(ii, 0); // Double->POW->BigFloat->String

            IsTrue(BigFloatToPOW == POWToBigFloat, $"Failed on: {ii}^0, is {BigFloatToPOW} but should be 0.");
        }

        // Power = 1
        for (double ii = 0.00001; ii < 100000; ii *= 1.23)
        {
            BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, 1); // Double->BigFloat->POW->String

            IsTrue(BigFloatToPOW == (BigFloat)ii, $"Failed on: {ii}^1, is {BigFloatToPOW} but should be 0.");
        }

        //int m2 = 0, m1 = 0, eq = 0, p1 = 0, p2 = 0, ne = 0;

        for (int jj = 2; jj < 20; jj++)
        {
            for (double ii = 0.00001; ii < 100000; ii *= 1.01)
            {
                BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, jj);                    // Double->BigFloat->POW->String
                BigFloat POWToBigFloatLo = (BigFloat)double.Pow(Math.BitDecrement(ii), jj); // Double->POW->BigFloat->String
                BigFloat POWToBigFloatHi = (BigFloat)double.Pow(Math.BitIncrement(ii), jj); // Double->POW->BigFloat->String

                //if (!miss.IsZero)
                //    ne++;
                //if (BigFloatToPOW < POWToBigFloatLo)
                //    m2++;
                //if (BigFloatToPOW == POWToBigFloatLo)
                //    m1++;
                //if (BigFloatToPOW > POWToBigFloatLo & BigFloatToPOW < POWToBigFloatHi)
                //    eq++;
                //if (BigFloatToPOW == POWToBigFloatHi)
                //    p1++;
                //if (BigFloatToPOW > POWToBigFloatHi)
                //    p2++;

                IsTrue(BigFloatToPOW > POWToBigFloatLo && BigFloatToPOW < POWToBigFloatHi,
                    $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}, " +
                    $"should be in the range {POWToBigFloatLo} to {POWToBigFloatHi}.");
            }
        }
        { }
        //// The below TEST has several exceptions from the few that were spot checked the issue was actually with the POW function. 
        //// e.g. 0.31832553782759071^2 is 0.10133114803322488, not 0.10133114803322489
        //// (note: 97 out of 46300 failed)
        //for (int jj = 0; jj < 20; jj++)
        //    for (double ii = 0.00001; ii < 100000; ii *= 1.01)
        //    {
        //        switch (jj)
        //        {
        //            case 2:
        //                // Spot check: Failure okay because "Double->POW" is the one that is incorrect.
        //                // 0.10133114803322488404... (Calculator)
        //                // 0.10133114803322489   Double->POW          ->String
        //                // 0.10133114803322488   Double->BigFloat->POW->String
        //                // 0.10133114803322489   Double->POW->BigFloat->String
        //                if (ii == 0.31832553782759071) continue; 
        //                if (ii == 3.7922202536323169) continue;                                            
        //                if (ii == 1266.4144068422818) continue;
        //                if (ii == 10650.005497109047) continue;
        //                if (ii == 74875.389283707336) continue;
        //                break;
        //            case 3:
        //                if (ii == 0.00015126381262911304) continue;
        //                if (ii == 0.0002207736543748362) continue;
        //                if (ii == 12487.969014230068) continue;
        //                break;
        //            case 4:
        //                if (ii == 0.00018641630355034525) continue;
        //                if (ii == 0.00045194559600055971) continue;
        //                if (ii == 0.32797111994930456) continue;
        //                if (ii == 340.5293137191391) continue;
        //                break;
        //            case 5:
        //                if (ii == 1.6604260161149265) continue;
        //                if (ii == 5980.123493474643) continue;
        //                break;
        //            case 6:
        //                if (ii == 0.0016641256869016213) continue;
        //                if (ii == 0.0065045062076691754) continue;
        //                if (ii == 0.06414019892333388) continue;
        //                // Spot check: Failure okay because "Double->POW" is the one that is incorrect.
        //                // 58462000269625.56645480... (Calculator)
        //                // 58462000269625.563   Double->POW          ->String
        //                // 58462000269625.57    Double->BigFloat->POW->String
        //                // 58462000269625.56    Double->POW->BigFloat->String
        //                if (ii == 197.00576951306914) continue;
        //                if (ii == 9173.376641854227) continue;
        //                if (ii == 41627.280567151014) continue;
        //                break;
        //            case 7:
        //                if (ii == 0.7342887581157852) continue;
        //                if (ii == 1706.935284624335) continue;
        //                if (ii == 69146.16578033564) continue;
        //                if (ii == 88675.25532946823) continue;
        //                break;
        //            case 8:
        //                if (ii == 0.000447470887129267) continue;
        //                if (ii == 0.4981192227105844) continue;
        //                if (ii == 46.54571935386443) continue;
        //                break;
        //            case 9:
        //                if (ii == 4.190615593600832E-05) continue;
        //                if (ii == 0.00037038951409555193) continue;
        //                if (ii == 0.4981192227105844) continue;
        //                if (ii == 1.333979962661673) continue;
        //                if (ii == 8.159045117086201) continue;
        //                if (ii == 9357.761512355497) continue;
        //                if (ii == 15086.827138952829) continue;
        //                break;
        //            case 10:
        //                if (ii == 0.0008890774106083161) continue;
        //                if (ii == 0.028648311229272454) continue;
        //                if (ii == 4056.734402316945) continue;
        //                break;
        //            case 11:
        //                if (ii == 0.0035449534097784898) continue;
        //                if (ii == 0.29105771630835503) continue;
        //                if (ii == 2.67698212324289) continue;
        //                if (ii == 5862.291435618707) continue;
        //                break;
        //            case 12:
        //                if (ii == 1.2824319950172336E-05) continue;
        //                if (ii == 9.106363450393602E-05) continue;
        //                if (ii == 0.0005092636098313419) continue;
        //                if (ii == 5.010626365612976) continue;
        //                if (ii == 116.26423399731596) continue;
        //                if (ii == 350.8476924541427) continue;
        //                if (ii == 1304.7880297840097) continue;
        //                break;
        //            case 13:
        //                if (ii == 0.000568169289597344) continue;
        //                if (ii == 0.019824771765173547) continue;
        //                if (ii == 1.5956367649543521) continue;
        //                if (ii == 1.7802014314350167) continue;
        //                if (ii == 968.0521421510657) continue;
        //                break;
        //            case 14:
        //                if (ii == 7.689208288984288E-05) continue;
        //                if (ii == 8.926932114884425E-05) continue;
        //                if (ii == 0.0003594964132768501) continue;
        //                if (ii == 0.0010634677003891738) continue;
        //                if (ii == 0.003241292077635545) continue;
        //                if (ii == 19.19895010555303) continue;
        //                if (ii == 1848.3663190286366) continue;
        //                if (ii == 63223.09306645596) continue;
        //                break;
        //            case 15:
        //                if (ii == 0.00021215914243386032) continue;
        //                if (ii == 0.00023203532954525665) continue;
        //                if (ii == 0.0027918852288367625) continue;
        //                if (ii == 0.004456584328541685) continue;
        //                if (ii == 0.18233689278491932) continue;
        //                if (ii == 0.3345633394602856) continue;
        //                if (ii == 0.9606030724686354) continue;
        //                if (ii == 12.64095196668918) continue;
        //                if (ii == 27.469258488757564) continue;
        //                if (ii == 403.29037524996096) continue;
        //                if (ii == 28806.282151995834) continue;
        //                break;
        //            case 16:
        //                if (ii == 2.088246008273344E-05) continue;
        //                if (ii == 0.0016476491949521004) continue;
        //                if (ii == 0.006188814471422317) continue;
        //                if (ii == 1.4020263473894414) continue;
        //                if (ii == 3.90712831953763) continue;
        //                if (ii == 11.218203029452331) continue;
        //                if (ii == 22.51227889872708) continue;
        //                if (ii == 26868.139452154428) continue;
        //                break;
        //            case 17:
        //                if (ii == 0.006188814471422317) continue;
        //                if (ii == 0.04139900050355379) continue;
        //                if (ii == 1371.3453325531723) continue;
        //                break;
        //            case 18:
        //                if (ii == 2.814640117199497E-05) continue;
        //                if (ii == 0.0009723708702586616) continue;
        //                if (ii == 0.23617301183120165) continue;
        //                if (ii == 0.7717448644551382) continue;
        //                if (ii == 7.3862771072496205) continue;
        //                if (ii == 284.6884022588047) continue;
        //                if (ii == 785.5067129683955) continue;
        //                if (ii == 18226.530900363294) continue;
        //                if (ii == 88675.25532946823) continue;
        //                break;
        //            case 19:
        //                if (ii == 0.0001005909054934069) continue;
        //                if (ii == 8.40627234317903) continue;
        //                if (ii == 16.7023756465811) continue;
        //                if (ii == 32.85728420031187) continue;
        //                if (ii == 22913.720600721652) continue;
        //                break;
        //        }
        //        //0.0004343108345321096
        //        //    6 7112587273365439443839758e-46 // BigFloat.Pow
        //        //    6 7112587273365443207877540e-46 //double.Pow
        //        //    6.7112587273365461884310371466766e-21 //exact
        //        BigFloat BigFloatToPOW = BigFloat.Pow((BigFloat)ii, jj);  // Double->BigFloat->POW->String
        //        BigFloat POWToBigFloat = (BigFloat)double.Pow(ii, jj);    // Double->POW->BigFloat->String
        //        if (BigFloatToPOW != POWToBigFloat) Debug.WriteLine($"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}");
        //        IsTrue(BigFloatToPOW == POWToBigFloat, $"Failed on: {ii}^{jj}, BigFloatToPOW:{BigFloatToPOW}");
        //    }



        BigFloat val, res, ans;

        IsTrue(BigFloat.Pow(BigFloat.ZeroWithNoPrecision, 0) == 1, $"Failed on: 0^0");
        IsTrue(BigFloat.Pow(BigFloat.One, 0) == 1, $"Failed on: 1^0");
        IsTrue(BigFloat.Pow(0, 0) == 1, $"Failed on: 0^0");
        IsTrue(BigFloat.Pow(1, 0) == 1, $"Failed on: 1^0");
        IsTrue(BigFloat.Pow(2, 0) == 1, $"Failed on: 2^0");
        IsTrue(BigFloat.Pow(3, 0) == 1, $"Failed on: 3^0");

        IsTrue(BigFloat.Pow(BigFloat.ZeroWithNoPrecision, 1) == 0, $"Failed on: 0^1");
        IsTrue(BigFloat.Pow(BigFloat.One, 1) == 1, $"Failed on: 1^1");
        IsTrue(BigFloat.Pow(0, 1) == 0, $"Failed on: 0^1");
        IsTrue(BigFloat.Pow(1, 1) == 1, $"Failed on: 1^1");
        IsTrue(BigFloat.Pow(2, 1) == 2, $"Failed on: 2^1");
        IsTrue(BigFloat.Pow(3, 1) == 3, $"Failed on: 3^1");

        IsTrue(BigFloat.Pow(BigFloat.ZeroWithNoPrecision, 2) == 0, $"Failed on: 0^2");
        IsTrue(BigFloat.Pow(BigFloat.One, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(0, 2) == 0, $"Failed on: 0^2");
        IsTrue(BigFloat.Pow(1, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(2, 2) == 4, $"Failed on: 2^2");

        IsTrue(BigFloat.Pow(3, 2) == 8, $"Failed on: 3^2");  // Min:1100^2=10010000 Max(exclusive):1110^2=11000100
        IsTrue(BigFloat.Pow(3, 2) == 9, $"Failed on: 3^2");
        // 1/26/2025 - Modified BigFloat.CompareTo() and borderline case is now accepted as false. 
        IsTrue(BigFloat.Pow(3, 2) == 10, $"Failed on: 3^2");  // does (10|01. == 1010.|00)?  1001-1010=00|01, so less then 00|1, so true 
        IsFalse(7 == 8, $"Failed on: 7 == 8");
        IsFalse(9 == 10, $"Failed on: 9 == 10");

        IsTrue(BigFloat.Pow(0, 3) == 0, $"Failed on: 0^3");
        IsTrue(BigFloat.Pow(1, 3) == 1, $"Failed on: 1^3");
        IsTrue(BigFloat.Pow(2, 3) == 8, $"Failed on: 2^3");
        IsTrue(BigFloat.Pow(3, 3) == 27, $"Failed on: 3^3");


        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 2) == BigFloat.Parse("  0.25"), $"Failed on: 0.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 2) == BigFloat.Parse("  2.25"), $"Failed on: 1.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 2) == BigFloat.Parse("  6.25"), $"Failed on: 2.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 2) == BigFloat.Parse(" 12.25"), $"Failed on: 3.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 3) == BigFloat.Parse(" 0.125"), $"Failed on: 0.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 3) == BigFloat.Parse(" 3.375"), $"Failed on: 1.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 3) == BigFloat.Parse("15.625"), $"Failed on: 2.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 3) == BigFloat.Parse("42.875"), $"Failed on: 3.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 4) == BigFloat.Parse(" 0.0625"), $"Failed on: 0.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 4) == BigFloat.Parse(" 5.0625"), $"Failed on: 1.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 4) == BigFloat.Parse("39.0625"), $"Failed on: 2.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 4) == BigFloat.Parse("150.0625"), $"Failed on: 3.5^4");

        // Test (poser < 3) section...
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), 0) == new BigFloat("1.00"), $"Failed on: Pow(3.000,0)");
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), 1) == new BigFloat("3.00"), $"Failed on: Pow(3.000,1)");
        // To-Do: reviewed this and it should pass - we need to update the compare function
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), -1) == new BigFloat("0.3333"), $"Failed on: Pow(3.000,-1)");
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), 2) == new BigFloat("9.00"), $"Failed on: Pow(3.000,2)");
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), -2) == new BigFloat("0.1111"), $"Failed on: Pow(3.000,2)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), 0) == new BigFloat("1.00"), $"Failed on: Pow(-3.000,0)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), 1) == new BigFloat("-3.00"), $"Failed on: Pow(-3.000,1)");
        // To-Do: reviewed this and it should pass - we need to update the compare function
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), -1) == new BigFloat("-0.3333"), $"Failed on: Pow(-3.000,-1)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), 2) == new BigFloat("9.00"), $"Failed on: Pow(-3.000,2)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), -2) == new BigFloat("0.1111"), $"Failed on: Pow(-3.000,2)");

        // Test (value._size < 53) where result <1e308 section...
        IsTrue(BigFloat.Pow(new BigFloat("3.000"), 3) == new BigFloat("27.0"), $"Failed on: Pow(3.000,3)");
        BigFloat t = BigFloat.Pow(new BigFloat("3.000"), -3);
        IsFalse(t == new BigFloat("27.0"), $"Failed on: Pow(3.000,-3)"); // not equal to 27!
        IsTrue(t == new BigFloat("0.037"), $"Failed on: Pow(3.000,-3)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), 3) == new BigFloat("-27.0"), $"Failed on: Pow(-3.000,3)");
        IsTrue(BigFloat.Pow(new BigFloat("-3.000"), -3) == new BigFloat("-0.037"), $"Failed on: Pow(-3.000,-3)");
        _ = BigFloat.Pow(new BigFloat("3.000"), -3);
        _ = BigFloat.SetPrecisionWithRound(new BigFloat("2187"), 2);
        IsTrue(BigFloat.Pow(new BigFloat("3.0"), 7) == BigFloat.SetPrecisionWithRound(new BigFloat("2187"), 2), $"Failed on: Pow(3.0,7)");

        BigFloat temp = new("1234.56");
        BigFloat powersOf2 = temp * temp;  // 2
        BigFloat total = powersOf2 * temp; // 2+1
        IsTrue(BigFloat.Pow(temp, 3) == total, $"Failed on: Pow(1234.56, 3)");

        powersOf2 *= powersOf2;  // 4
        total *= powersOf2;  // 1+2+4
        IsTrue(BigFloat.Pow(temp, 7) == total, $"Failed on: Pow(1234.56, 7)");

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8
        IsTrue(BigFloat.Pow(temp, 15) == total, $"Failed on: Pow(1234.56, 15)");

        // Test (value._size < 53) where result >1e308 section...
        temp = new BigFloat("12345123451234.321234");
        _ = new BigFloat("1.8814224057326597649226680826726e39");

        powersOf2 = temp * temp;  // 2
        total = powersOf2 * temp; // 2+1
        t = BigFloat.Pow(temp, 3);
        IsTrue(t == total, $"Failed on: Pow(12345123451234.321234, 3)");

        powersOf2 *= powersOf2;  // 4
        total *= powersOf2;  // 1+2+4
        IsTrue(BigFloat.Pow(temp, 7) == total, $"Failed on: Pow(12345123451234.321234, 7)");

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8
        IsTrue(BigFloat.Pow(temp, 15) == total, $"Failed on: Pow(12345123451234.321234, 15)");

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8+16
        IsTrue(BigFloat.Pow(temp, 31) == total, $"Failed on: Pow(12345123451234.321234, 31)");

        powersOf2 *= powersOf2; // 8
        total *= powersOf2;  // 1+2+4+8+16+32
        IsTrue(BigFloat.Pow(temp, 63) == total, $"Failed on: Pow(12345123451234.321234, 63)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+4");
        res = BigFloat.Pow(val, 2);
        IsTrue(res == ans, $"Failed on: Pow(100, 2)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+004");
        res = BigFloat.Pow(val, 2);
        IsTrue(res == ans, $"Failed on: Pow(100, 2)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+10");
        res = BigFloat.Pow(val, 5);
        IsTrue(res == ans, $"Failed on: Pow(100, 5)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+20");
        res = BigFloat.Pow(val, 10);
        IsTrue(res == ans, $"Failed on: Pow(100, 10)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+50");
        res = BigFloat.Pow(val, 25);
        IsTrue(res == ans, $"Failed on: Pow(100, 25)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+100");
        res = BigFloat.Pow(val, 50);
        IsTrue(res == ans, $"Failed on: Pow(100, 50)");

        val = new BigFloat("100");
        ans = new BigFloat("1.00000000e+200");
        res = BigFloat.Pow(val, 100);
        IsTrue(res == ans, $"Failed on: Pow(100, 100)");

        val = new BigFloat("10000");
        ans = new BigFloat("1.00000000e+400");
        res = BigFloat.Pow(val, 100);

        IsTrue(res == ans, $"Failed on: Pow(10000, 100)");
        val = new BigFloat("10000");
        ans = new BigFloat("1.00000000e+404");
        res = BigFloat.Pow(val, 101);
        IsTrue(res == ans, $"Failed on: Pow(10000, 101)");

        val = new BigFloat("1000000");
        ans = new BigFloat("1.00000000e+600");
        res = BigFloat.Pow(val, 100);
        IsTrue(res == ans, $"Failed on: Pow(1000000, 100)");

        //100000000 ^ 100 = 1e800
        val = new BigFloat("100000000");
        ans = new BigFloat("1.00000000e+800");
        res = BigFloat.Pow(val, 100);
        IsTrue(res == ans, $"Failed on: Pow(100000000, 100)");
    }

    [TestMethod]
    private void Verify_PowMostSignificantBitsUsingAccurteVersion()
    {

        (int maxBitSize, int maxTries, int maxExpSize) = TestTargetInMillseconds switch
        {
            >= 16627 => (2050, 1, 10),
            >= 2242 => (1026, 1, 11),
            >= 1678 => (1026, 1, 10),
            >= 753 => (768, 1, 10),
            >= 615 => (768, 1, 9),
            >= 540 => (514, 1, 10),
            >= 456 => (768, 1, 8),
            >= 147 => (514, 1, 9),
            >= 115 => (514, 1, 8),
            >= 35 => (258, 1, 8),
            >= 11 => (194, 1, 7),
            >= 6 => (150, 1, 6),
            >= 3 => (140, 1, 6),
            >= 2 => (98, 1, 6),
            >= 1 => (70, 1, 6),
            _ => (70, 1, 5),        // up to 69 value bit size, 1 try on average for each size, up to 4 bits.
        };

        _ = Parallel.For(2, maxBitSize, bitSize =>
        //for (int bitSize = 2; bitSize < maxBitSize; bitSize ++)
        {
            for (int valTries = 0; valTries < maxTries; valTries++)
            {
                for (int expSize = 2; expSize < maxExpSize; expSize++) // e.g. 8 = 511 max
                {
                    BigInteger val = GenerateRandomBigInteger(bitSize);
                    int valSize = (int)val.GetBitLength();
                    int wantedBits;
                    for (wantedBits = 1; wantedBits < (valSize + 2); wantedBits++)
                    {
                        int exp = (int)GenerateRandomBigInteger(expSize);

                        if ((long)exp * Math.Max(valSize, wantedBits) >= int.MaxValue)
                        {
                            continue;
                        }

                        // Answer Setup using accurate version of PowMostSignificantBits
                        BigInteger ans = BigIntegerTools.PowMostSignificantBits(val, exp, out int shiftedAns, valSize, wantedBits, true);

                        // Result Setup
                        BigInteger res = BigIntegerTools.PowMostSignificantBits(val, exp, out int shiftedRes, valSize, wantedBits, false);

                        if (shiftedAns < 0)
                        {
                            Fail("Fail - depending on the mode, a negative shiftedAns is not supported.");
                        }
                        else if (shiftedAns - shiftedRes == 1)  // ans rounded up to larger size - this is allowed but only by one
                        {
                            ans <<= 1;
                        }
                        else if (shiftedAns - shiftedRes == -1)  // ans rounded down
                        {
                            // should not happen 
                            Fail("Specification does not allow rounding down, so size can never be smaller then answers's size.");
                            res <<= 1;
                        }
                        else if (shiftedRes != shiftedAns)
                        {
                            Fail("'Shifted' should not be greater then 1.");
                        }

                        BigInteger miss = ans - res;
                        // a miss of 0 or 1(a round-up)
                        if (miss > 1)
                        {
                            Fail("Specification allows for round ups.");
                        }
                        else if (miss < 0)
                        {
                            Fail("Specification does not allow rounding down so it should be less then actual.");
                        }
                    }
                }
            }
        });
    }

    [TestMethod]
    private void Verify_PowMostSignificantBitsUsingFullCalculation()
    {
        (int maxBitSize, int maxTries, int maxExpSize) = TestTargetInMillseconds switch
        {
            > 269291 => (1026, 1, 10),
            > 101587 => (768, 1, 10),
            > 27232 => (768, 1, 9),
            > 19366 => (514, 1, 10),
            > 8901 => (768, 1, 8),
            > 6963 => (514, 1, 9),
            > 1662 => (514, 1, 8),
            > 736 => (386, 1, 8),
            > 367 => (258, 1, 8),
            > 30 => (194, 1, 7),
            > 5 => (150, 1, 6),
            > 4 => (140, 1, 6),
            > 3 => (120, 1, 6),
            > 2 => (98, 1, 6),
            > 1 => (70, 1, 6),
            > 0 => (70, 1, 5),
            _ => (66, 1, 5),         // up to 65 value bit size, 1 try on average for each size, up to 4 bits.
        };

        _ = Parallel.For(2, maxBitSize, bitSize =>
        //for (int bitSize = 2; bitSize < maxBitSize; bitSize ++)
        {
            for (int valTries = 0; valTries < maxTries; valTries++)
            {
                for (int expSize = 2; expSize < maxExpSize; expSize++) // e.g. 8 = 511 max
                {
                    BigInteger val = GenerateRandomBigInteger(bitSize);
                    int valSize = (int)val.GetBitLength();
                    int wantedBits;
                    //int wantedBits = valSize;  
                    for (wantedBits = 1; wantedBits < (valSize + 2); wantedBits++)
                    {
                        int exp = (int)GenerateRandomBigInteger(expSize);

                        if ((long)exp * Math.Min(wantedBits, valSize) >= int.MaxValue)
                        {
                            continue;
                        }

                        // Answer Setup using Pow (this is slow for large numbers)
                        BigInteger p = BigInteger.Pow(val, exp);
                        int shiftedAns = Math.Max(0, (int)(p.GetBitLength() - Math.Min(wantedBits, valSize)));
                        bool overflowed = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out BigInteger ans, p, shiftedAns);
                        if (overflowed)
                        {
                            shiftedAns++;
                        }

                        if (val.IsZero)
                        {
                            shiftedAns = 0;
                        }

                        // Result Setup
                        BigInteger res = BigIntegerTools.PowMostSignificantBits(val, exp, out int shiftedRes, valSize, wantedBits, false);

                        if (shiftedAns < 0)
                        {
                            Fail("Fail - depending on the mode, a negative shiftedAns is not supported.");
                        }
                        else if (shiftedAns - shiftedRes == 1)  // ans rounded up to larger size - this is allowed but only by one
                        {
                            ans <<= 1;
                        }
                        else if (shiftedAns - shiftedRes == -1)  // ans rounded down
                        {
                            Fail("Specification does not allow rounding down, so size can never be smaller then answers's size.");
                            res <<= 1;
                        }
                        else if (shiftedRes != shiftedAns)
                        {
                            Fail("'Shifted' should not be greater then 1.");
                        }

                        BigInteger miss = ans - res;
                        // a miss of 0 or 1(a round-up)
                        if (miss > 1)
                        {
                            Fail("Specification allows for round ups.");
                        }
                        else if (miss < 0)
                        {
                            Fail("Specification does not allow rounding down so it should be less then actual.");
                        }
                    }
                }
            }
        });
    }

    private static BigInteger GenerateRandomBigInteger(int maxNumberOfBits)
    {
        byte[] data = new byte[(maxNumberOfBits / 8) + 1];
        Random.Shared.NextBytes(data);
        data[^1] >>= 8 - (maxNumberOfBits % 8);
        return new(data, true);
    }

    [TestMethod]
    public void Verify_BigFloatConstants()
    {
        // BigFloat.Zero  BigFloat.One
        IsTrue(BigFloat.ZeroWithNoPrecision == 0, $"Failed on: BigFloat.ZeroWithNoPrecision == 0");
        IsTrue(BigFloat.One == 1, $"Failed on: BigFloat.One == 1");
        IsTrue(BigFloat.ZeroWithNoPrecision == BigFloat.One - BigFloat.One, $"Failed on: BigFloat.ZeroWithNoPrecision == BigFloat.One - BigFloat.One");
        IsTrue(BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecision, $"Failed on: BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecision");
        IsTrue(BigFloat.One - BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecision + BigFloat.One, $"Failed on: BigFloat.ZeroWithNoPrecision - BigFloat.ZeroWithNoPrecision == BigFloat.ZeroWithNoPrecision + BigFloat.One");
    }

    [TestMethod]
    public void Verify_Math_Modulus()
    {
        ModVerify__True(new BigFloat("1.000"), new BigFloat("1.000"), new BigFloat("0.000"));
        ModVerify__True(new BigFloat("1.000"), new BigFloat("2.000"), new BigFloat("1.000"));
        ModVerify__True(new BigFloat("2.000"), new BigFloat("1.000"), new BigFloat("0.000"));
        ModVerify__True(new BigFloat("3.000"), new BigFloat("2.000"), new BigFloat("1.000"));
        ModVerify__True(new BigFloat("4.000"), new BigFloat("2.000"), new BigFloat("0.000"));
        ModVerify__True(new BigFloat(14), new BigFloat(10), new BigFloat(4));
        ModVerify__True(new BigFloat("0.14"), new BigFloat("0.10"), new BigFloat("0.04"));

        //     1111000010100011110101110000101001001 129192616265 actual mod output
        //     1111010111000010100011110101110000101 131941395333 hand written expected result of 0.24
        //     11111================================ (remove 32 bits) hand written expected result of 0.24(rounded version)
        // 0.00111100001010001111010111000010100011110101110000101000111101  precision answer of 0.235 if 1.555 and 0.44 were exact.  
        // for this to work we would need to not carry the extra bits in the:  precision=Log2(number)+extra bits 
        ModVerify__True(new BigFloat("1.555"), new BigFloat("0.44"), new BigFloat("0.235"));
        ModVerify__True(new BigFloat("1.555"), new BigFloat("0.444"), new BigFloat("0.223"));
        ModVerify__True(new BigFloat("1.555"), new BigFloat("0.4444"), new BigFloat("0.2218"));
        ModVerify__True(new BigFloat("1.555"), new BigFloat("0.44444"), new BigFloat("0.2217"));

        // The next line fails because the result has zero precision remaining and is "around zero". "around zero" does not equal "0.011".
        ModVerify_False(new BigFloat("11"), new BigFloat("0.333"), new BigFloat("0.011"));
        ModVerify__True(new BigFloat("11.000"), new BigFloat("0.333"), new BigFloat("0.011"));

        // The next line is true because the result has zero precision remaining and is "around zero". "around zero" equals "0"("around zero" also)
        ModVerify__True(new BigFloat("11"), new BigFloat("0.333"), new BigFloat("0"));

        // The next line fails because the result has zero precision remaining and is "around zero". "around zero" does not equal "0.011".
        ModVerify_False(new BigFloat("3"), new BigFloat("0.222"), new BigFloat("0.114"));
        ModVerify__True(new BigFloat("3.000"), new BigFloat("0.222"), new BigFloat("0.114"));

        // The next line is true because the result has zero precision remaining and is "around zero". "around zero" equals "0"("around zero" also)
        ModVerify__True(new BigFloat("3"), new BigFloat("0.222"), new BigFloat("0"));

        //  101011_.  (86)   (aka 101011|0.) 
        // % 1101__.  (52)   (a    1101|00.)
        //=========
        //   100010.  (34)   (aka  1000|10.) 
        //     --  (out of precision digits)
        BigFloat v = new(0b101011, 1);
        BigFloat w = new(0b1101, 2);
        ModVerify__True(v, w, new BigFloat(0b100010, 0)); // 1000.1<<2 == 100010<<0
        ModVerify__True(v, w, new BigFloat(0b10001, 1));  // 1000.1<<2 ==  10001<<1
        ModVerify_False(v, w, new BigFloat(0b1000, 2));   // 1000.1<<2 ==   1000<<2 or 1001!=1000  (if we do not round up)
        // 1/26/2025 - Modified BigFloat.CompareTo() and borderline case is now accepted as false. 
        ModVerify_False(v, w, new BigFloat(0b1001, 2));   // 1000.1<<2 ==   1001<<2 or 1001==1001  (if we do     round up) 
        
        // Below two tests are the same as above two tests.
        // IsFalse(v % w == new BigFloat(0b1000, 2));  //reverse order:  1000<<2 == 1000.1<<2 or 1000!=1001  (if we do not round up)
        // IsFalse(v % w == new BigFloat(0b1001, 2));  //reverse order:  1001<<2 == 1000.1<<2 or 1001=1001  (if we do     round up)
        ModVerify__True(v, w, new BigFloat(0b100, 3));    // 1000.1<<2 ==    100<<3 
        ModVerify__True(v, w, new BigFloat(0b100011, 0)); // 1000.1<<2 == 100011<<0 
        // 1/26/2025 - Modified BigFloat.CompareTo() and borderline case is now accepted as false. 
        ModVerify_False(v, w, new BigFloat(0b10010, 1));  // 1000.1<<2 ==  10010<<1 ("1000|10 == 10010|0." can be considered 1001==1001 but questionable)
        // Below test is the same as above test.
        //ModVerify__True(v, w, new BigFloat(0b1001, 2));   // 1000.1<<2 !=   1001<<2 ("1000|10 == 1001|00." can be considered 1001==1001 but questionable)
        ModVerify_False(v, w, new BigFloat(0b100000, 0)); // 1000.1<<2 != 100000<<0
        ModVerify_False(v, w, new BigFloat(0b011111, 0)); // 1000.1<<2 !=  11111<<0
        ModVerify_False(v, w, new BigFloat(0b1000, 2));   // 1000.1<<2 !=   1000<<2
         
        ModVerify__True(new BigFloat("-1.000"), new BigFloat("+1.000"), new BigFloat("0.000"));
        ModVerify__True(new BigFloat("+1.000"), new BigFloat("-1.000"), new BigFloat("0.000"));
        ModVerify__True(new BigFloat("-1.000"), new BigFloat("-1.000"), new BigFloat("0.000"));

        ModVerify__True(new BigFloat("-1.000"), new BigFloat("+2.000"), new BigFloat("-1.000"));
        ModVerify__True(new BigFloat("+1.000"), new BigFloat("-2.000"), new BigFloat("+1.000"));
        ModVerify__True(new BigFloat("-1.000"), new BigFloat("-2.000"), new BigFloat("-1.000"));

        ModVerify__True(new BigFloat("-0.14"), new BigFloat("+0.10"), new BigFloat("-0.04"));
        ModVerify__True(new BigFloat("+0.14"), new BigFloat("-0.10"), new BigFloat("+0.04"));
        ModVerify__True(new BigFloat("-0.14"), new BigFloat("-0.10"), new BigFloat("-0.04"));

        ///////////////////////////// Modulus vs Remainder /////////////////////////////
        // Note: "%" is Remainder (not Mod) 
        // For positive numbers Mod and Remainder are the same.
        IsTrue(BigFloat.Mod(new BigFloat(-2), new BigFloat(10)) == 8, $"-2 mod 10 should be 8.");
        IsTrue(BigFloat.Remainder(new BigFloat(-2), new BigFloat(10)) == -2, $"-2 % 10 should be -2.");
        IsTrue(BigFloat.Mod(new BigFloat(-2), new BigFloat(-10)) == -2, $"-2 mod -10 should be -2.");
        IsTrue(BigFloat.Remainder(new BigFloat(-2), new BigFloat(-10)) == -2, $"-2 % -10 should be -2.");
        IsTrue(BigFloat.Mod(new BigFloat(2), new BigFloat(-10)) == -8, $"2 mod -10 should be -8.");
        IsTrue(BigFloat.Remainder(new BigFloat(2), new BigFloat(-10)) == 2, $"2 % -10 should be 2.");

        IsTrue(BigFloat.Mod(new BigFloat(-7), new BigFloat(5)) == 3, $"-7 mod 5 should be 3.");
        IsTrue(BigFloat.Remainder(new BigFloat(-7), new BigFloat(5)) == -2, $"-7 % 5 should be -2.");
        IsTrue(BigFloat.Mod(new BigFloat(-7), new BigFloat(-5)) == -2, $"-7 mod -5 should be -2.");
        IsTrue(BigFloat.Remainder(new BigFloat(-7), new BigFloat(-5)) == -2, $"-7 % -5 should be -2.");
        IsTrue(BigFloat.Mod(new BigFloat(7), new BigFloat(-5)) == -3, $"7 mod -5 should be -3.");
        IsTrue(BigFloat.Remainder(new BigFloat(7), new BigFloat(-5)) == 2, $"7 % -5 should be 2.");

        static void ModVerify__True(BigFloat inputVal0, BigFloat inputVal1, BigFloat expect)
        {
            BigFloat output = inputVal0 % inputVal1;
            IsTrue(output.CompareTo(expect) == 0, $"Mod ({inputVal0} % {inputVal1}) was {output} but expected {expect}.");
        }

        static void ModVerify_False(BigFloat inputVal0, BigFloat inputVal1, BigFloat expect)
        {
            BigFloat output = inputVal0 % inputVal1;
            IsFalse(output.CompareTo(expect) == 0, $"Mod ({inputVal0} % {inputVal1}) should not have been {output}.");
        }
    }

    [TestMethod]
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
            IsTrue((res << scale) == input);
        }

        // byte -> BigFloat -> byte
        if (input is >= byte.MinValue and <= byte.MaxValue)
        {
            res = new((byte)input, scale);
            IsTrue((res << scale) == input);
        }

        // short -> BigFloat -> short
        if (input is >= short.MinValue and <= short.MaxValue)
        {
            res = new((short)input, scale);
            IsTrue((res << scale) == input);
        }

        // ushort -> BigFloat -> ushort
        if (input is >= ushort.MinValue and <= ushort.MaxValue)
        {
            res = new((int)input, scale);
            IsTrue((res << scale) == input);
        }

        // long -> BigFloat -> long
        res = new(input, scale);
        IsTrue((res << scale) == input);
    }

    [TestMethod]
    public void IsIntegerChecker()
    {
        BigFloat bf;
        bf = new BigFloat(0); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(1); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(-1); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat("1.000"); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(1.000); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(11.0000000); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat("-11.0000000"); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(int.MaxValue); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(int.MinValue); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(double.MaxValue); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");
        bf = new BigFloat(double.MinValue); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        bf = new BigFloat(double.E); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat(double.Epsilon); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat(double.Pi); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat(0.001); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat(-0.001); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat(-0.002); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("-0.002"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("-0.9999999"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("-1.0000001"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("+0.9999999"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("+1.0000001"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("-0.9999999999999"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("-1.0000000000001"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("+0.9999999999999"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
        bf = new BigFloat("+1.0000000000001"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");

        // 22.111 / 22.111 = 1 -> Is Integer
        bf = new BigFloat(22.111) / new BigFloat(22.111); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        // 22.111 / 22.111 = 1 -> Is Integer
        bf = new BigFloat("22.111") / new BigFloat(22.111); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        // 22.000 / 22.111 -> Is Not Integer
        bf = new BigFloat("22.000") / new BigFloat("22.111"); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");

        // 22.500 + 22.5 -> Is Integer
        bf = new BigFloat("22.5") + new BigFloat(22.5); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        // 22.500 - 22.5 -> Is Integer
        bf = new BigFloat("22.5") - new BigFloat(22.5); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        // 22.500 * 2 -> Is Integer
        bf = new BigFloat("22.5") * new BigFloat(2); IsTrue(bf.IsInteger, $"{bf}.IsInteger reported as false but should be true.");

        // 22.501 * 2 -> Is Integer
        bf = new BigFloat("22.501") * new BigFloat(2); IsFalse(bf.IsInteger, $"{bf}.IsInteger reported as true but should be false.");
    }

    [TestMethod]
    public void Verify_TestHiLow64Bits()
    {
        //                                                       Dec/Display,  low64WithHidden,       low64,              high64 
        TestHiLow64Bits(new BigFloat((BigInteger)0x0, 0, true), "0.00000", "0000000000000000", "0000000000000000", "0000000000000000");

        // 0.00001
        TestHiLow64Bits(new BigFloat((BigInteger)0x1, 0, true), "0.00001", "0000000000000001", "0000000000000000", "8000000000000000");

        //  0.1000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x10000000, 0, true), "0.1000000", "0000000010000000", "0000000000000000", "8000000000000000");

        // .999999
        TestHiLow64Bits(new BigFloat((BigInteger)0xFFFFFFFF, 0, true), "0.999999", "00000000FFFFFFFF", "0000000000000000", "FFFFFFFF00000000");

        //  1.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x100000000, 0, true), "1.00000", "0000000100000000", "0000000000000001", "8000000000000000");

        //  1.500000
        TestHiLow64Bits(new BigFloat((BigInteger)0x180000000, 0, true), "1.50000", "0000000180000000", "0000000000000001", "C000000000000000");

        // 1.99999999
        TestHiLow64Bits(new BigFloat((BigInteger)0x1FFFFFFFF, 0, true), "1.999999", "00000001FFFFFFFF", "0000000000000001", "FFFFFFFF80000000");

        //  2.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x200000000, 0, true), "2.00000", "0000000200000000", "0000000000000002", "8000000000000000");

        //  2.000...001
        TestHiLow64Bits(new BigFloat((BigInteger)0x200000001, 0, true), "2.00000...001", "0000000200000001", "0000000000000002", "8000000040000000");

        //  3.500000
        TestHiLow64Bits(new BigFloat((BigInteger)0x380000000, 0, true), "3.50000", "0000000380000000", "0000000000000003", "E000000000000000");

        // 3.99999999
        TestHiLow64Bits(new BigFloat((BigInteger)0x3FFFFFFFF, 0, true), "3.999999", "00000003FFFFFFFF", "0000000000000003", "FFFFFFFFC0000000");

        //  4.000000
        TestHiLow64Bits(new BigFloat((BigInteger)0x400000000, 0, true), "4.00000", "0000000400000000", "0000000000000004", "8000000000000000");

        //  4.000...001
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
            IsTrue(val.Lowest64BitsWithHiddenBits == neg.Lowest64BitsWithHiddenBits);
            IsTrue(val.Lowest64Bits == neg.Lowest64Bits);
            IsTrue(val.Highest64Bits == neg.Highest64Bits);
        }

    }

    private static void TestHiLow64Bits(BigFloat bf, string textInput, string low64WithHiddenAnswer, string low64Answer, string high64Answer)
    {
        for (int i = 0; i < 2; i++)
        {
            if (i == 1)
            {
                bf = -bf;
                textInput = "-" + textInput;
            }
            string res = bf.Lowest64BitsWithHiddenBits.ToString("X16");
            IsTrue(res == low64WithHiddenAnswer, $"Low64BitsWithHidden: {res} != {low64WithHiddenAnswer} on input {textInput} [{bf.DebuggerDisplay}]");
            res = bf.Lowest64Bits.ToString("X16");
            IsTrue(res == low64Answer, $"Lowest64Bits   : {res} != {low64Answer} on input {textInput} [{bf.DebuggerDisplay}]");
            res = bf.Highest64Bits.ToString("X16");
            IsTrue(res == high64Answer, $"Highest64Bits  : {res} != {high64Answer} on input {textInput} [{bf.DebuggerDisplay}]");
        }

        //Console.WriteLine("Lowest64BitsWithHiddenBits: " + bf.Lowest64BitsWithHiddenBits.ToString("X16"));
        //Console.WriteLine("Lowest64Bits:               " + bf.Lowest64Bits.ToString("X16"));
        //Console.WriteLine("Highest64Bits:              " + bf.Highest64Bits.ToString("X16"));

        //Console.WriteLine("-0.00000 " + bf.DebuggerDisplay);
        //Console.WriteLine("Lowest64BitsWithHiddenBits: " + bf.Lowest64BitsWithHiddenBits.ToString("X16"));
        //Console.WriteLine("Lowest64Bits:               " + bf.Lowest64Bits.ToString("X16"));
        //Console.WriteLine("Highest64Bits:              " + bf.Highest64Bits.ToString("X16"));
        //return bf;
    }

    [TestMethod]
    public void Verify_Cast_BigFloat_to_Float()
    {
        float res;
        res = (float)new BigFloat(123);
        IsTrue((int)res == 123);

        for (float d = -2.34567f; d < 12.34; d = 0.1f + (d * 1.007f))
        {
            res = (float)new BigFloat(d);
            IsTrue(d == res);
        }
    }

    [TestMethod]
    public void Verify_Floor()
    {
        FloorCeilingCheckerDouble(0);
        FloorCeilingCheckerDouble(double.NegativeZero);       // should accept as 0.0
        FloorCeilingCheckerDouble(double.Epsilon);
        FloorCeilingCheckerDouble(-double.Epsilon);
        FloorCeilingCheckerDouble(double.Epsilon * 2);
        FloorCeilingCheckerDouble(-double.Epsilon * 2);
        FloorCeilingCheckerDouble(double.Epsilon * 3);
        FloorCeilingCheckerDouble(-double.Epsilon * 3);
        FloorCeilingCheckerDouble(0.123);
        FloorCeilingCheckerDouble(-0.123);
        FloorCeilingCheckerDouble(0.5);
        FloorCeilingCheckerDouble(-0.5);
        FloorCeilingCheckerDouble(0.75);
        FloorCeilingCheckerDouble(0.75);
        FloorCeilingCheckerDouble(-0.7);
        FloorCeilingCheckerDouble(-0.7);
        FloorCeilingCheckerDouble(0.99);
        FloorCeilingCheckerDouble(0.99);
        FloorCeilingCheckerDouble(-0.99);
        FloorCeilingCheckerDouble(-0.99);
        FloorCeilingCheckerDouble(1);
        FloorCeilingCheckerDouble(1);
        FloorCeilingCheckerDouble(-1);
        FloorCeilingCheckerDouble(-1);
        FloorCeilingCheckerDouble(1.1);
        FloorCeilingCheckerDouble(-1.1);
        FloorCeilingCheckerDouble(1.99);
        FloorCeilingCheckerDouble(1.99);
        FloorCeilingCheckerDouble(-1.99);
        FloorCeilingCheckerDouble(-1.99);
        FloorCeilingCheckerDouble(2);
        FloorCeilingCheckerDouble(-2);
        FloorCeilingCheckerDouble(2.1);
        FloorCeilingCheckerDouble(-2.1);
        FloorCeilingCheckerDouble(-127);
        FloorCeilingCheckerDouble(127);
        FloorCeilingCheckerDouble(-128);
        FloorCeilingCheckerDouble(128);
        FloorCeilingCheckerDouble(255);
        FloorCeilingCheckerDouble(-255);
        FloorCeilingCheckerDouble(255);
        FloorCeilingCheckerDouble(-255);
        FloorCeilingCheckerDouble(-32767);
        FloorCeilingCheckerDouble(32767);
        FloorCeilingCheckerDouble(-32768);
        FloorCeilingCheckerDouble(32768);
        FloorCeilingCheckerDouble(-65535);
        FloorCeilingCheckerDouble(65535);
        FloorCeilingCheckerDouble(-65536);
        FloorCeilingCheckerDouble(65536);
        FloorCeilingCheckerDouble(double.MinValue);
        FloorCeilingCheckerDouble(double.MaxValue);

        FloorCeilingChecker(new BigFloat(0), new BigFloat(0), new BigFloat(0));         // 0

        FloorCeilingChecker(new BigFloat(1, -1), new BigFloat(0), new BigFloat(1));     // .1
        FloorCeilingChecker(new BigFloat(-1, -1), new BigFloat(-1), new BigFloat(0));   //-.1

        FloorCeilingChecker(new BigFloat(3, -2), new BigFloat(0), new BigFloat(1));     // .11
        FloorCeilingChecker(new BigFloat(-3, -2), new BigFloat(-1), new BigFloat(0));   //-.11

        FloorCeilingChecker(new BigFloat(65535, 0), new BigFloat(65535), new BigFloat(65535));     // .1111111111111111
        FloorCeilingChecker(new BigFloat(-65535, 0), new BigFloat(-65535), new BigFloat(-65535));   //-.1111111111111111

        FloorCeilingChecker(new BigFloat(3, -18), new BigFloat(0), new BigFloat(1));     // .000000000000000011
        FloorCeilingChecker(new BigFloat(-3, -18), new BigFloat(-1), new BigFloat(0));   //-.000000000000000011

        FloorCeilingChecker(new BigFloat(1), new BigFloat(1), new BigFloat(1));         // 1
        FloorCeilingChecker(new BigFloat(-1), new BigFloat(-1), new BigFloat(-1));      //-1

        FloorCeilingChecker(new BigFloat(3, -1), new BigFloat(1), new BigFloat(2));     // 1.1
        FloorCeilingChecker(new BigFloat(-3, -1), new BigFloat(-2), new BigFloat(-1));  //-1.1

        FloorCeilingChecker(new BigFloat(1, 1), new BigFloat(2), new BigFloat(2));      // 1 << 1
        FloorCeilingChecker(new BigFloat(-1, 1), new BigFloat(-2), new BigFloat(-2));   //-1 << 1

        FloorCeilingChecker(new BigFloat(int.MaxValue, 0), new BigFloat(int.MaxValue), new BigFloat(int.MaxValue));    // 0x7fffffff
        FloorCeilingChecker(new BigFloat(int.MinValue, 0), new BigFloat(int.MinValue), new BigFloat(int.MinValue));    //-0x80000000 

        FloorCeilingChecker(new BigFloat(uint.MaxValue, 0), new BigFloat(uint.MaxValue), new BigFloat(uint.MaxValue));  // 0xffffffff

        FloorCeilingChecker(new BigFloat(long.MaxValue, 0), new BigFloat(long.MaxValue), new BigFloat(long.MaxValue));
        FloorCeilingChecker(new BigFloat(long.MinValue, 0), new BigFloat(long.MinValue), new BigFloat(long.MinValue));

        FloorCeilingChecker(new BigFloat(ulong.MaxValue, 0), new BigFloat(ulong.MaxValue), new BigFloat(ulong.MaxValue));

        FloorCeilingChecker(new BigFloat(ulong.MaxValue, -1), new BigFloat(ulong.MaxValue - 1, -1), new BigFloat(BigInteger.Parse("10000000000000000", NumberStyles.AllowHexSpecifier), -1));
        // Value: 1111111111111111111111111111111.1|00000000000000000000000000000000
        // Floor: 1111111111111111111111111111111.0|00000000000000000000000000000000
        // Ceil: 10000000000000000000000000000000.0|00000000000000000000000000000000

        FloorCeilingChecker(new BigFloat(ulong.MaxValue - 1, -1), new BigFloat(ulong.MaxValue - 1, -1), new BigFloat(ulong.MaxValue - 1, -1));
        // Value: 1111111111111111111111111111111.0|00000000000000000000000000000000
        // Floor: 1111111111111111111111111111111.0|00000000000000000000000000000000
        // Ceil:  1111111111111111111111111111111.0|00000000000000000000000000000000

        FloorCeilingChecker(new BigFloat(ulong.MaxValue - 2, -1), new BigFloat(ulong.MaxValue - 3, -1), new BigFloat(ulong.MaxValue - 1, -1));
        // Value: 1111111111111111111111111111110.1|00000000000000000000000000000000
        // Floor: 1111111111111111111111111111110.0|00000000000000000000000000000000
        // Ceil:  1111111111111111111111111111111.0|00000000000000000000000000000000

        static void FloorCeilingChecker(BigFloat val, BigFloat manualValueForFloor, BigFloat manualValueForCeiling)
        {
            if (manualValueForCeiling < manualValueForFloor)
            {
                throw new Exception("Test Error, floor should be equal to or less then manualValueForCeiling.");
            }

            BigFloat floorBI = val.Floor();
            IsTrue(floorBI == manualValueForFloor, $"BigInteger.Floor() ({floorBI}) does not match ({manualValueForFloor})");

            BigFloat ceilingBI = val.Ceiling();
            IsTrue(ceilingBI == manualValueForCeiling, $"BigInteger.Ceiling() ({ceilingBI}) does not match ({manualValueForCeiling})");

            // Compare Ceiling() vs Floor() - except for integers, ceiling should be larger by one

            if (val.IsInteger)
            {
                IsTrue(floorBI == ceilingBI, $"For integers (like {val}) Floor() and Ceiling() should match.");
            }
            else
            {
                IsTrue((floorBI + 1) == ceilingBI, $"For non-integers, ({val}).Floor() should be one unit less then ({val}).Ceiling()).");
            }
        }
    }

    private static void FloorCeilingCheckerDouble(double value)
    {
        BigFloat res = new(value);

        // doubles.Floor() should match BigFloat.Floor()
        BigFloat floorBI = res.Floor();
        double floorDub = double.Floor(value);

        BigFloat floorBackToBI = (BigFloat)floorDub;
        IsTrue(floorBI == floorBackToBI, $"BigInteger.Floor() ({floorBI}) does not match Double.Floor() ({floorDub})");

        double floorBackToDub = (double)floorBI;
        IsTrue(floorBackToDub == floorDub, $"BigInteger.Floor() ({floorBI}) does not match Double.Floor() ({floorDub})");

        // doubles.Ceiling() should match BigFloat.Ceiling()
        BigFloat ceilingBI = res.Ceiling();
        double ceilingDub = double.Ceiling(value);

        BigFloat ceilingBackToBI = (BigFloat)ceilingDub;
        IsTrue(ceilingBI == ceilingBackToBI, $"BigInteger.Ceiling() ({ceilingBI}) does not match Double.Ceiling() ({ceilingDub})");

        double ceilingBackToDub = (double)ceilingBI;
        IsTrue(ceilingBackToDub == ceilingDub, $"BigInteger.Ceiling() ({ceilingBI}) does not match Double.Ceiling() ({ceilingDub})");

        // Compare Ceiling() vs Floor() - except for integers, ceiling should be larger by one
        if (res.IsInteger)
        {
            IsTrue(floorBI == ceilingBI, $"For integers (like {res}) Floor() and Ceiling() should match.");
        }
        else
        {
            IsTrue((floorBI + 1) == ceilingBI, $"For non-integers, ({res}).Floor() should be one unit less then ({res}).Ceiling()).");
        }
    }

    [TestMethod]
    public void Verify_TryParseHex()
    {
        // Tests invalid sequences of TryParseHex...
        IsFalse(BigFloat.TryParseHex(null, out _), @"BigFloat.TryParseHex(null) reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("", out _), @"BigFloat.TryParseHex("""") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("-", out _), @"BigFloat.TryParseHex(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("+", out _), @"BigFloat.TryParseHex(""+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("/", out _), @"BigFloat.TryParseHex(""/"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("G", out _), @"BigFloat.TryParseHex(""G"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex(".", out _), @"BigFloat.TryParseHex(""."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("-+", out _), @"BigFloat.TryParseHex(""-+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0+", out _), @"BigFloat.TryParseHex(""0+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0-", out _), @"BigFloat.TryParseHex(""0-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex(".", out _), @"BigFloat.TryParseHex(""."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("-.", out _), @"BigFloat.TryParseHex(""-."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("1-", out _), @"BigFloat.TryParseHex(""1-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0x", out _), @"BigFloat.TryParseHex(""0x"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("-0x", out _), @"BigFloat.TryParseHex(""-0x"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0.0.", out _), @"BigFloat.TryParseHex(""0.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("+.0.", out _), @"BigFloat.TryParseHex(""+.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("--1", out _), @"BigFloat.TryParseHex(""--1"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("1.01.", out _), @"BigFloat.TryParseHex(""1.01."") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex(".G1", out _), @"BigFloat.TryParseHex("".G1"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("2.G1", out _), @"BigFloat.TryParseHex(""2.G1"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0h-ABCD", out _), @"BigFloat.TryParseHex(""0h-ABCD"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("-+0x55", out _), @"BigFloat.TryParseHex(""-+0x55"") reported True but should be False.");
        IsFalse(BigFloat.TryParseHex("0x0", out _), @"BigFloat.TryParseHex(""0x0"") reported True but should be False.");

        // Parse valid hex sequences and make sure the result is correct.
        IsTrue(BigFloat.TryParseHex("0", out BigFloat output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""0"") was not 0.");
        IsTrue(BigFloat.TryParseHex("1", out output)); IsTrue(output == 1, @"BigFloat.TryParseHex(""1"") was not 1.");
        IsTrue(BigFloat.TryParseHex("F", out output)); IsTrue(output == 15, @"BigFloat.TryParseHex(""F"") was not 15.");
        IsTrue(BigFloat.TryParseHex("-1", out output)); IsTrue(output == -1, @"BigFloat.TryParseHex(""-1"") was not -1.");
        IsTrue(BigFloat.TryParseHex("-F", out output)); IsTrue(output == -15, @"BigFloat.TryParseHex(""-F"") was not -15.");
        IsTrue(BigFloat.TryParseHex("00", out output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""00"") was not 0.");
        IsTrue(BigFloat.TryParseHex("80", out output)); IsTrue(output == 128, @"BigFloat.TryParseHex(""80"") was not 128.");
        IsTrue(BigFloat.TryParseHex("FF", out output)); IsTrue(output == 255, @"BigFloat.TryParseHex(""FF"") was not 255.");
        IsTrue(BigFloat.TryParseHex("+00", out output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""+00"") was not 0.");
        IsTrue(BigFloat.TryParseHex("-11", out output)); IsTrue(output == -17, @"BigFloat.TryParseHex(""-11"") was not -17.");
        IsTrue(BigFloat.TryParseHex("-FF", out output)); IsTrue(output == -255, @"BigFloat.TryParseHex(""-FF"") was not -255.");
        IsTrue(BigFloat.TryParseHex("0.0", out output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""0.0"") was not 0.");
        IsTrue(BigFloat.TryParseHex("-0.", out output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""-0."") was not 0.");
        IsTrue(BigFloat.TryParseHex("-.0", out output)); IsTrue(output == 0, @"BigFloat.TryParseHex(""-.0"") was not 0.");
        IsTrue(BigFloat.TryParseHex("F.F", out output)); IsTrue(output == (BigFloat)15.9375, @"BigFloat.TryParseHex(""F.F"") was not 15.9375   .");
        IsTrue(BigFloat.TryParseHex("0.F", out output)); IsTrue(output == (BigFloat)0.9375, @"BigFloat.TryParseHex(""0.F"") was not 0.9375    .");
        IsTrue(BigFloat.TryParseHex(".FF", out output)); IsTrue(output == (BigFloat)0.99609375, @"BigFloat.TryParseHex("".FF"") was not 0.99609375.");
        IsTrue(BigFloat.TryParseHex("FFFFFFFF", out output)); IsTrue(output == 4294967295, @"BigFloat.TryParseHex(""FFFFFFFF"") was not 4294967295.");
        IsTrue(BigFloat.TryParseHex("-FFFFFFFF", out output)); IsTrue(output == -4294967295, @"BigFloat.TryParseHex(""-FFFFFFFF"") was not -4294967295.");
        IsTrue(BigFloat.TryParseHex("100000000", out output)); IsTrue(output == 4294967296, @"BigFloat.TryParseHex(""100000000"") was not 4294967296.");
        IsTrue(BigFloat.TryParseHex("-100000000", out output)); IsTrue(output == -4294967296, @"BigFloat.TryParseHex(""-100000000"") was not -4294967296.");
        IsTrue(BigFloat.TryParseHex("FFFFF.FFF", out output)); IsTrue(output == (BigFloat)1048575.999755859375, @"BigFloat.TryParseHex(""FFFFF.FFF"") was not 1048575.999755859375.");
        IsTrue(BigFloat.TryParseHex("-FFFFF.FFF", out output)); IsTrue(output == (BigFloat)(-1048575.999755859375), @"BigFloat.TryParseHex(""-FFFFF.FFF"") was not -1048575.999755859375.");
        IsTrue(BigFloat.TryParseHex("-FFFFF.FFF", out output)); IsTrue(output == (BigFloat)(-1048575.999755859375), @"BigFloat.TryParseHex(""-FFFFF.FFF"") was not -1048575.999755859375.");
        IsTrue(BigFloat.TryParseHex("-000123.8", out output)); IsTrue(output == (BigFloat)(-291.5), @"BigFloat.TryParseHex(""-123.5"") was not -291.5.");
        IsTrue(BigFloat.TryParseHex("1234567890ABDCDEF", out output)); IsTrue(output == BigFloat.Parse("20988295476718456303"), @"BigFloat.TryParseHex(""1234567890ABDCDEF"") was not 20988295476718456303.");
        IsTrue(BigFloat.TryParseHex("1234567890ABDCDEF.1234567890ABDCD", out output)); IsTrue(output == BigFloat.Parse("20988295476718456303.07111111110195573754"), @"BigFloat.TryParseHex(""1234567890ABDCD.1234567890ABDCDEF"") was not 20988295476718456303.07111111110195573754.");
        IsTrue(BigFloat.TryParseHex("1234567890ABDC.DEF1234567890ABDCD", out output)); IsTrue(output == BigFloat.Parse("5124095575370716.87086697048610887591"), @"BigFloat.TryParseHex(""1234567890ABDC.DEF1234567890ABDCD"") was not 5124095575370716.87086697048610887591.");
    }

    [TestMethod]
    public void Verify_TryParseBinary()
    {
        // Tests invalid sequences of TryParseBinary...
        IsFalse(BigFloat.TryParseBinary(null, out _), @"BigFloat.TryParseBinary(null) reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("", out _), @"BigFloat.TryParseBinary("""") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("|", out _), @"BigFloat.TryParseBinary(""|"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("+|", out _), @"BigFloat.TryParseBinary(""+|"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-", out _), @"BigFloat.TryParseBinary(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("+", out _), @"BigFloat.TryParseBinary(""+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("/", out _), @"BigFloat.TryParseBinary(""/"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary(".", out _), @"BigFloat.TryParseBinary(""."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-+", out _), @"BigFloat.TryParseBinary(""-+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0+", out _), @"BigFloat.TryParseBinary(""0+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0-", out _), @"BigFloat.TryParseBinary(""0-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-.", out _), @"BigFloat.TryParseBinary(""-."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0.0.", out _), @"BigFloat.TryParseBinary(""0.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("+.0.", out _), @"BigFloat.TryParseBinary(""+.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("12", out _), @"BigFloat.TryParseBinary(""12"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("--1", out _), @"BigFloat.TryParseBinary(""--1"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("1.01.", out _), @"BigFloat.TryParseBinary(""1.01."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary(".41", out _), @"BigFloat.TryParseBinary("".41"") reported True but should be False.");

        // Parse valid binary sequences and make sure the result is correct.
        IsTrue(BigFloat.TryParseBinary("0", out BigFloat output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("1", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("1.", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("-0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("+0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary(".0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary(".1", out output)); IsTrue(output == (BigFloat)0.5);
        IsTrue(BigFloat.TryParseBinary("00", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("01", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("10", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("11", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("+00", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("+01", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("+10", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("+11", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("-00", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("-01", out output)); IsTrue(output == -1);
        IsTrue(BigFloat.TryParseBinary("-10", out output)); IsTrue(output == -2);
        IsTrue(BigFloat.TryParseBinary("-11", out output)); IsTrue(output == -3);
        IsTrue(BigFloat.TryParseBinary(".00", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary(".01", out output)); IsTrue(output == (BigFloat)0.25);
        IsTrue(BigFloat.TryParseBinary(".10", out output)); IsTrue(output == (BigFloat)0.5);
        IsTrue(BigFloat.TryParseBinary(".11", out output)); IsTrue(output == (BigFloat)0.75);
        IsTrue(BigFloat.TryParseBinary("0.0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("0.1", out output)); IsTrue(output == (BigFloat)0.5);
        IsTrue(BigFloat.TryParseBinary("1.0", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("1.1", out output)); IsTrue(output == (BigFloat)1.5);
        IsTrue(BigFloat.TryParseBinary("00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("01.", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("10.", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("11.", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("01.", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("10.", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("11.", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("000", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("001", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("010", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("011", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("100", out output)); IsTrue(output == 4);
        IsTrue(BigFloat.TryParseBinary("101", out output)); IsTrue(output == 5);
        IsTrue(BigFloat.TryParseBinary("110", out output)); IsTrue(output == 6);
        IsTrue(BigFloat.TryParseBinary("111", out output)); IsTrue(output == 7);
        IsTrue(BigFloat.TryParseBinary("+0.0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("+0.1", out output)); IsTrue(output == (BigFloat)0.5);
        IsTrue(BigFloat.TryParseBinary("+1.0", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("+1.1", out output)); IsTrue(output == (BigFloat)1.5);
        IsTrue(BigFloat.TryParseBinary("+00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("+01.", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("+10.", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("+11.", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("+00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("+01.", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("+10.", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("+11.", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("-0.0", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("-0.1", out output)); IsTrue(output == (BigFloat)(-0.5));
        IsTrue(BigFloat.TryParseBinary("-1.0", out output)); IsTrue(output == -1);
        IsTrue(BigFloat.TryParseBinary("-1.1", out output)); IsTrue(output == (BigFloat)(-1.5));
        IsTrue(BigFloat.TryParseBinary("-00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("-01.", out output)); IsTrue(output == -1);
        IsTrue(BigFloat.TryParseBinary("-10.", out output)); IsTrue(output == -2);
        IsTrue(BigFloat.TryParseBinary("-11.", out output)); IsTrue(output == -3);
        IsTrue(BigFloat.TryParseBinary("-00.", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("-01.", out output)); IsTrue(output == -1);
        IsTrue(BigFloat.TryParseBinary("-10.", out output)); IsTrue(output == -2);
        IsTrue(BigFloat.TryParseBinary("-11.", out output)); IsTrue(output == -3);
        IsTrue(BigFloat.TryParseBinary("000", out output)); IsTrue(output == 0);
        IsTrue(BigFloat.TryParseBinary("001", out output)); IsTrue(output == 1);
        IsTrue(BigFloat.TryParseBinary("010", out output)); IsTrue(output == 2);
        IsTrue(BigFloat.TryParseBinary("011", out output)); IsTrue(output == 3);
        IsTrue(BigFloat.TryParseBinary("100", out output)); IsTrue(output == 4);
        IsTrue(BigFloat.TryParseBinary("101", out output)); IsTrue(output == 5);
        IsTrue(BigFloat.TryParseBinary("110", out output)); IsTrue(output == 6);
        IsTrue(BigFloat.TryParseBinary("111", out output)); IsTrue(output == 7);
        IsTrue(BigFloat.TryParseBinary(".000", out output)); IsTrue(output == (BigFloat)0.0);
        IsTrue(BigFloat.TryParseBinary(".001", out output)); IsTrue(output == (BigFloat)0.125);
        IsTrue(BigFloat.TryParseBinary(".010", out output)); IsTrue(output == (BigFloat)0.250);
        IsTrue(BigFloat.TryParseBinary(".011", out output)); IsTrue(output == (BigFloat)0.375);
        IsTrue(BigFloat.TryParseBinary(".100", out output)); IsTrue(output == (BigFloat)0.500);
        IsTrue(BigFloat.TryParseBinary(".101", out output)); IsTrue(output == (BigFloat)0.625);
        IsTrue(BigFloat.TryParseBinary(".110", out output)); IsTrue(output == (BigFloat)0.750);
        IsTrue(BigFloat.TryParseBinary(".111", out output)); IsTrue(output == (BigFloat)0.875);
        IsTrue(BigFloat.TryParseBinary("0.00", out output)); IsTrue(output == (BigFloat)0.0);
        IsTrue(BigFloat.TryParseBinary("0.01", out output)); IsTrue(output == (BigFloat)0.25);
        IsTrue(BigFloat.TryParseBinary("0.10", out output)); IsTrue(output == (BigFloat)0.50);
        IsTrue(BigFloat.TryParseBinary("0.11", out output)); IsTrue(output == (BigFloat)0.75);
        IsTrue(BigFloat.TryParseBinary("1.00", out output)); IsTrue(output == (BigFloat)1.0);
        IsTrue(BigFloat.TryParseBinary("1.01", out output)); IsTrue(output == (BigFloat)1.25);
        IsTrue(BigFloat.TryParseBinary("1.10", out output)); IsTrue(output == (BigFloat)1.5);
        IsTrue(BigFloat.TryParseBinary("1.11", out output)); IsTrue(output == (BigFloat)1.75);
        IsTrue(BigFloat.TryParseBinary("-.000", out output)); IsTrue(output == (BigFloat)0.0);
        IsTrue(BigFloat.TryParseBinary("-.001", out output)); IsTrue(output == (BigFloat)(-0.125));
        IsTrue(BigFloat.TryParseBinary("-.010", out output)); IsTrue(output == (BigFloat)(-0.250));
        IsTrue(BigFloat.TryParseBinary("-.011", out output)); IsTrue(output == (BigFloat)(-0.375));
        IsTrue(BigFloat.TryParseBinary("-.100", out output)); IsTrue(output == (BigFloat)(-0.500));
        IsTrue(BigFloat.TryParseBinary("-.101", out output)); IsTrue(output == (BigFloat)(-0.625));
        IsTrue(BigFloat.TryParseBinary("-.110", out output)); IsTrue(output == (BigFloat)(-0.750));
        IsTrue(BigFloat.TryParseBinary("-.111", out output)); IsTrue(output == (BigFloat)(-0.875));
        IsTrue(BigFloat.TryParseBinary("-0.00", out output)); IsTrue(output == (BigFloat)(-0.0));
        IsTrue(BigFloat.TryParseBinary("-0.01", out output)); IsTrue(output == (BigFloat)(-0.25));
        IsTrue(BigFloat.TryParseBinary("-0.10", out output)); IsTrue(output == (BigFloat)(-0.50));
        IsTrue(BigFloat.TryParseBinary("-0.11", out output)); IsTrue(output == (BigFloat)(-0.75));
        IsTrue(BigFloat.TryParseBinary("-1.00", out output)); IsTrue(output == (BigFloat)(-1.0));
        IsTrue(BigFloat.TryParseBinary("-1.01", out output)); IsTrue(output == (BigFloat)(-1.25));
        IsTrue(BigFloat.TryParseBinary("-1.10", out output)); IsTrue(output == (BigFloat)(-1.5));
        IsTrue(BigFloat.TryParseBinary("-1.11", out output)); IsTrue(output == (BigFloat)(-1.75));

        // Test values around the one byte 1 byte marker
        IsTrue(BigFloat.TryParseBinary("1000000", out output)); IsTrue(output == (BigFloat)64);
        IsTrue(BigFloat.TryParseBinary("10000000", out output)); IsTrue(output == (BigFloat)128);
        IsTrue(BigFloat.TryParseBinary("100000000", out output)); IsTrue(output == (BigFloat)256);
        IsTrue(BigFloat.TryParseBinary("1000000000", out output)); IsTrue(output == (BigFloat)512);
        IsTrue(BigFloat.TryParseBinary("1111111", out output)); IsTrue(output == (BigFloat)127);
        IsTrue(BigFloat.TryParseBinary("11111111", out output)); IsTrue(output == (BigFloat)255);
        IsTrue(BigFloat.TryParseBinary("111111111", out output)); IsTrue(output == (BigFloat)511);
        IsTrue(BigFloat.TryParseBinary("1111111111", out output)); IsTrue(output == (BigFloat)1023);
        IsTrue(BigFloat.TryParseBinary("+1000000", out output)); IsTrue(output == (BigFloat)64);
        IsTrue(BigFloat.TryParseBinary("+10000000", out output)); IsTrue(output == (BigFloat)128);
        IsTrue(BigFloat.TryParseBinary("+100000000", out output)); IsTrue(output == (BigFloat)256);
        IsTrue(BigFloat.TryParseBinary("+1000000000", out output)); IsTrue(output == (BigFloat)512);
        IsTrue(BigFloat.TryParseBinary("+1111111", out output)); IsTrue(output == (BigFloat)127);
        IsTrue(BigFloat.TryParseBinary("+11111111", out output)); IsTrue(output == (BigFloat)255);
        IsTrue(BigFloat.TryParseBinary("+111111111", out output)); IsTrue(output == (BigFloat)511);
        IsTrue(BigFloat.TryParseBinary("+1111111111", out output)); IsTrue(output == (BigFloat)1023);
        IsTrue(BigFloat.TryParseBinary("-1000000", out output)); IsTrue(output == (BigFloat)(-64));
        IsTrue(BigFloat.TryParseBinary("-10000000", out output)); IsTrue(output == (BigFloat)(-128));
        IsTrue(BigFloat.TryParseBinary("-100000000", out output)); IsTrue(output == (BigFloat)(-256));
        IsTrue(BigFloat.TryParseBinary("-1000000000", out output)); IsTrue(output == (BigFloat)(-512));
        IsTrue(BigFloat.TryParseBinary("-1111111", out output)); IsTrue(output == (BigFloat)(-127));
        IsTrue(BigFloat.TryParseBinary("-11111111", out output)); IsTrue(output == (BigFloat)(-255));
        IsTrue(BigFloat.TryParseBinary("-111111111", out output)); IsTrue(output == (BigFloat)(-511));
        IsTrue(BigFloat.TryParseBinary("-1111111111", out output)); IsTrue(output == (BigFloat)(-1023));
        IsTrue(BigFloat.TryParseBinary("-11111111111", out output)); IsTrue(output == (BigFloat)(-2047));

        // Test values around the one byte 2 byte marker
        IsTrue(BigFloat.TryParseBinary("1000000000000000", out output)); IsTrue(output == (BigFloat)32768);
        IsTrue(BigFloat.TryParseBinary("1111111111111101", out output)); IsTrue(output == (BigFloat)65533);
        IsTrue(BigFloat.TryParseBinary("1111111111111110", out output)); IsTrue(output == (BigFloat)65534);
        IsTrue(BigFloat.TryParseBinary("1111111111111111", out output)); IsTrue(output == (BigFloat)65535);
        IsTrue(BigFloat.TryParseBinary("10000000000000000", out output)); IsTrue(output == (BigFloat)65536);
        IsTrue(BigFloat.TryParseBinary("10000000000000001", out output)); IsTrue(output == (BigFloat)65537);
        IsTrue(BigFloat.TryParseBinary("10000000000000010", out output)); IsTrue(output == (BigFloat)65538);
        IsTrue(BigFloat.TryParseBinary("11111111111111111", out output)); IsTrue(output == (BigFloat)131071);

        // Test values around the one byte 1 byte marker (with different formats)
        IsTrue(BigFloat.TryParseBinary("1000000000000000.", out output)); IsTrue(output == (BigFloat)32768);
        IsTrue(BigFloat.TryParseBinary("1111111111111101.0", out output)); IsTrue(output == (BigFloat)65533);
        IsTrue(BigFloat.TryParseBinary("+1111111111111110", out output)); IsTrue(output == (BigFloat)65534);
        IsTrue(BigFloat.TryParseBinary("-1111111111111111", out output)); IsTrue(output == (BigFloat)(-65535));
        IsTrue(BigFloat.TryParseBinary("10000000000000000.", out output)); IsTrue(output == (BigFloat)65536);
        IsTrue(BigFloat.TryParseBinary("10000000000000000.0", out output)); IsTrue(output == (BigFloat)65536);
        IsTrue(BigFloat.TryParseBinary("-10000000000000000.0", out output)); IsTrue(output == (BigFloat)(-65536));
        IsTrue(BigFloat.TryParseBinary("+10000000000000001", out output)); IsTrue(output == (BigFloat)65537);
        IsTrue(BigFloat.TryParseBinary("10000000000000010.00", out output)); IsTrue(output == (BigFloat)65538);
        IsTrue(BigFloat.TryParseBinary("11111111111111111.000000000000", out output)); IsTrue(output == (BigFloat)131071);

        // around 3 to 4 byte with random formats
        IsTrue(BigFloat.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output)); IsTrue(output == (BigFloat)21616517498418916);
        IsTrue(BigFloat.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output)); IsTrue(output == (BigFloat)86466069993675667);
        IsTrue(BigFloat.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output)); IsTrue(output == (BigFloat)6148914691236517205);
        IsTrue(BigFloat.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output)); IsTrue(output == (BigFloat)21616517498418916);
        IsTrue(BigFloat.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output)); IsTrue(output == (BigFloat)(-86466069993675667));
        IsTrue(BigFloat.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output)); IsTrue(output == (BigFloat)6148914691236517205);

        // around 3 to 4 byte with random formats
        IsTrue(BigFloat.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output)); IsTrue(output == (BigFloat)21616517498418916);
        IsTrue(BigFloat.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output)); IsTrue(output == (BigFloat)86466069993675667);
        IsTrue(BigFloat.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output)); IsTrue(output == (BigFloat)6148914691236517205);
        IsTrue(BigFloat.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output)); IsTrue(output == (BigFloat)21616517498418916);
        IsTrue(BigFloat.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output)); IsTrue(output == (BigFloat)(-86466069993675667));
        IsTrue(BigFloat.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output)); IsTrue(output == (BigFloat)6148914691236517205);

        double growthSpeed = 1.01;  // 1.01 for fast, 1.0001 for more extensive
        for (long i = 0; i > 0; i = (long)(i * growthSpeed) + 1)
        {
            BigFloat val = (BigFloat)i;
            string binaryBits = Convert.ToString(i, 2);

            // checks several numbers between 0 and long.MaxValue
            string strVal = binaryBits;
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == val);

            // checks several negative numbers between 0 and long.MaxValue
            strVal = "-" + binaryBits;
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == (BigFloat)(-i));

            // checks several numbers between 0 and long.MaxValue (with leading plus sign)
            strVal = "+" + binaryBits;
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == (BigFloat)i);

            // checks several numbers between 0 and long.MaxValue (with leading '-0')
            strVal = "-0" + binaryBits;
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == (BigFloat)(-i));

            // checks several numbers between 0 and long.MaxValue (with with trailing '.')
            strVal = "+" + binaryBits + ".";
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == (BigFloat)i);

            // checks several numbers between 0 and long.MaxValue (with with trailing '.0')
            strVal = "-0" + binaryBits + ".0"; ;
            IsTrue(BigFloat.TryParseBinary(strVal, out output));
            IsTrue(output == (BigFloat)(-i));
        }

        IsTrue(BigFloat.TryParseBinary("1000000000000000.|", out output));
        IsTrue(output == new BigFloat((ulong)32768 << 32, 0, true));

        IsTrue(BigFloat.TryParseBinary("1000000000000000|.", out output));
        IsTrue(output == new BigFloat((ulong)32768 << 32, 0, true));

        IsTrue(BigFloat.TryParseBinary("100000000000000|0.", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)32768 << 31, 1, true)) == 0);

        IsTrue(BigFloat.TryParseBinary("100000000000000|0.0", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)32768 << 31, 1, true)) == 0);

        IsTrue(BigFloat.TryParseBinary("10000000000.0000|00", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)32768 << 31, -4, true)) == 0);

        IsTrue(BigFloat.TryParseBinary("1|000000000000000.", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)1 << 32, 15, true)) == 0);

        IsTrue(BigFloat.TryParseBinary("1.|000000000000000", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)1 << 32, 0, true)) == 0);

        IsTrue(BigFloat.TryParseBinary("1|.000000000000000", out output));
        IsTrue(output.CompareToExact(new BigFloat((ulong)1 << 32, 0, true)) == 0);
    }

    [TestMethod]
    public void Verify_TryParseBinary2()
    {
        // Tests invalid sequences of TryParseBinary...
        IsFalse(BigFloat.TryParseBinary(null, out _, 0), @"BigFloat.TryParseBinary(null) reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("", out _, 0), @"BigFloat.TryParseBinary("""") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-", out _, 0), @"BigFloat.TryParseBinary(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("+", out _, 0), @"BigFloat.TryParseBinary(""+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("/", out _, 0), @"BigFloat.TryParseBinary(""/"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary(".", out _, 0), @"BigFloat.TryParseBinary(""."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-+", out _, 0), @"BigFloat.TryParseBinary(""-+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0+", out _, 0), @"BigFloat.TryParseBinary(""0+"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0-", out _, 0), @"BigFloat.TryParseBinary(""0-"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("-.", out _, 0), @"BigFloat.TryParseBinary(""-."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("0.0.", out _, 0), @"BigFloat.TryParseBinary(""0.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("+.0.", out _, 0), @"BigFloat.TryParseBinary(""+.0."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("12", out _, 0), @"BigFloat.TryParseBinary(""12"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("--1", out _, 0), @"BigFloat.TryParseBinary(""--1"") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary("1.01.", out _, 0), @"BigFloat.TryParseBinary(""1.01."") reported True but should be False.");
        IsFalse(BigFloat.TryParseBinary(".41", out _, 0), @"BigFloat.TryParseBinary("".41"") reported True but should be False.");

        // Parse valid binary sequences and make sure the result is correct.
        IsTrue(BigIntegerTools.TryParseBinary("0", out BigInteger output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("1", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("1.", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("-0", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+0", out output)); IsTrue(output == 0);
        // IsTrue(BigIntegerTools.TryParseBinary(".0", out output)); IsTrue(output == 0); This can go either way for BigInteger
        // IsTrue(BigIntegerTools.TryParseBinary(".1", out output)); IsTrue(output == 0); This can go either way for BigInteger
        IsTrue(BigIntegerTools.TryParseBinary("00", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("01", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("10", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("11", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("+00", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+01", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("+10", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("+11", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("-00", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-01", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-10", out output)); IsTrue(output == -2);
        IsTrue(BigIntegerTools.TryParseBinary("-11", out output)); IsTrue(output == -3);
        //IsTrue(BigIntegerTools.TryParseBinary(".00", out output)); IsTrue(output == 0); This can go either way for BigInteger
        //IsTrue(BigIntegerTools.TryParseBinary(".01", out output)); IsTrue(output == 0); This can go either way for BigInteger
        //IsTrue(BigIntegerTools.TryParseBinary(".10", out output)); IsTrue(output == 0); This can go either way for BigInteger
        //IsTrue(BigIntegerTools.TryParseBinary(".11", out output)); IsTrue(output == 0); This can go either way for BigInteger
        IsTrue(BigIntegerTools.TryParseBinary("0.0", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("0.1", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("1.0", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("1.1", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("01.", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("10.", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("11.", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("01.", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("10.", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("11.", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("000", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("001", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("010", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("011", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("100", out output)); IsTrue(output == 4);
        IsTrue(BigIntegerTools.TryParseBinary("101", out output)); IsTrue(output == 5);
        IsTrue(BigIntegerTools.TryParseBinary("110", out output)); IsTrue(output == 6);
        IsTrue(BigIntegerTools.TryParseBinary("111", out output)); IsTrue(output == 7);
        IsTrue(BigIntegerTools.TryParseBinary("+0.0", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+0.1", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+1.0", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("+1.1", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("+00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+01.", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("+10.", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("+11.", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("+00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("+01.", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("+10.", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("+11.", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("-0.0", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-0.1", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-1.0", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-1.1", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-01.", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-10.", out output)); IsTrue(output == -2);
        IsTrue(BigIntegerTools.TryParseBinary("-11.", out output)); IsTrue(output == -3);
        IsTrue(BigIntegerTools.TryParseBinary("-00.", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-01.", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-10.", out output)); IsTrue(output == -2);
        IsTrue(BigIntegerTools.TryParseBinary("-11.", out output)); IsTrue(output == -3);
        IsTrue(BigIntegerTools.TryParseBinary("000", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("001", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("010", out output)); IsTrue(output == 2);
        IsTrue(BigIntegerTools.TryParseBinary("011", out output)); IsTrue(output == 3);
        IsTrue(BigIntegerTools.TryParseBinary("100", out output)); IsTrue(output == 4);
        IsTrue(BigIntegerTools.TryParseBinary("101", out output)); IsTrue(output == 5);
        IsTrue(BigIntegerTools.TryParseBinary("110", out output)); IsTrue(output == 6);
        IsTrue(BigIntegerTools.TryParseBinary("111", out output)); IsTrue(output == 7);
        IsTrue(BigIntegerTools.TryParseBinary("0.00", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("0.01", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("0.10", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("0.11", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("1.00", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("1.01", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("1.10", out output)); IsTrue(output == 1);
        IsTrue(BigIntegerTools.TryParseBinary("1.11", out output)); IsTrue(output == 1);
        //IsTrue(BigIntegerTools.TryParseBinary("-.000", out output)); IsTrue(output == 0);
        //IsTrue(BigIntegerTools.TryParseBinary("-.001", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-0.00", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-0.01", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-0.10", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-0.11", out output)); IsTrue(output == 0);
        IsTrue(BigIntegerTools.TryParseBinary("-1.00", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-1.01", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-1.10", out output)); IsTrue(output == -1);
        IsTrue(BigIntegerTools.TryParseBinary("-1.11", out output)); IsTrue(output == -1);

        // Test values around the one byte 1 byte marker
        IsTrue(BigIntegerTools.TryParseBinary("1000000", out output)); IsTrue(output == 64);
        IsTrue(BigIntegerTools.TryParseBinary("10000000", out output)); IsTrue(output == 128);
        IsTrue(BigIntegerTools.TryParseBinary("100000000", out output)); IsTrue(output == 256);
        IsTrue(BigIntegerTools.TryParseBinary("1000000000", out output)); IsTrue(output == 512);
        IsTrue(BigIntegerTools.TryParseBinary("1111111", out output)); IsTrue(output == 127);
        IsTrue(BigIntegerTools.TryParseBinary("11111111", out output)); IsTrue(output == 255);
        IsTrue(BigIntegerTools.TryParseBinary("111111111", out output)); IsTrue(output == 511);
        IsTrue(BigIntegerTools.TryParseBinary("1111111111", out output)); IsTrue(output == 1023);
        IsTrue(BigIntegerTools.TryParseBinary("+1000000", out output)); IsTrue(output == 64);
        IsTrue(BigIntegerTools.TryParseBinary("+10000000", out output)); IsTrue(output == 128);
        IsTrue(BigIntegerTools.TryParseBinary("+100000000", out output)); IsTrue(output == 256);
        IsTrue(BigIntegerTools.TryParseBinary("+1000000000", out output)); IsTrue(output == 512);
        IsTrue(BigIntegerTools.TryParseBinary("+1111111", out output)); IsTrue(output == 127);
        IsTrue(BigIntegerTools.TryParseBinary("+11111111", out output)); IsTrue(output == 255);
        IsTrue(BigIntegerTools.TryParseBinary("+111111111", out output)); IsTrue(output == 511);
        IsTrue(BigIntegerTools.TryParseBinary("+1111111111", out output)); IsTrue(output == 1023);
        IsTrue(BigIntegerTools.TryParseBinary("-1000000", out output)); IsTrue(output == (-64));
        IsTrue(BigIntegerTools.TryParseBinary("-10000000", out output)); IsTrue(output == (-128));
        IsTrue(BigIntegerTools.TryParseBinary("-100000000", out output)); IsTrue(output == (-256));
        IsTrue(BigIntegerTools.TryParseBinary("-1000000000", out output)); IsTrue(output == (-512));
        IsTrue(BigIntegerTools.TryParseBinary("-1111111", out output)); IsTrue(output == (-127));
        IsTrue(BigIntegerTools.TryParseBinary("-11111111", out output)); IsTrue(output == (-255));
        IsTrue(BigIntegerTools.TryParseBinary("-111111111", out output)); IsTrue(output == (-511));
        IsTrue(BigIntegerTools.TryParseBinary("-1111111111", out output)); IsTrue(output == (-1023));
        IsTrue(BigIntegerTools.TryParseBinary("-11111111111", out output)); IsTrue(output == (-2047));

        // Test values around the one byte 2 byte marker
        IsTrue(BigIntegerTools.TryParseBinary("1000000000000000", out output)); IsTrue(output == 32768);
        IsTrue(BigIntegerTools.TryParseBinary("1111111111111101", out output)); IsTrue(output == 65533);
        IsTrue(BigIntegerTools.TryParseBinary("1111111111111110", out output)); IsTrue(output == 65534);
        IsTrue(BigIntegerTools.TryParseBinary("1111111111111111", out output)); IsTrue(output == 65535);
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000000", out output)); IsTrue(output == 65536);
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000001", out output)); IsTrue(output == 65537);
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000010", out output)); IsTrue(output == 65538);
        IsTrue(BigIntegerTools.TryParseBinary("11111111111111111", out output)); IsTrue(output == 131071);

        // Test values around the one byte 1 byte marker (with different formats)
        IsTrue(BigIntegerTools.TryParseBinary("1000000000000000.", out output)); IsTrue(output == 32768);
        IsTrue(BigIntegerTools.TryParseBinary("1111111111111101.0", out output)); IsTrue(output == 65533);
        IsTrue(BigIntegerTools.TryParseBinary("+1111111111111110", out output)); IsTrue(output == 65534);
        IsTrue(BigIntegerTools.TryParseBinary("-1111111111111111", out output)); IsTrue(output == (-65535));
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000000.", out output)); IsTrue(output == 65536);
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000000.0", out output)); IsTrue(output == 65536);
        IsTrue(BigIntegerTools.TryParseBinary("-10000000000000000.0", out output)); IsTrue(output == (-65536));
        IsTrue(BigIntegerTools.TryParseBinary("+10000000000000001", out output)); IsTrue(output == 65537);
        IsTrue(BigIntegerTools.TryParseBinary("10000000000000010.00", out output)); IsTrue(output == 65538);
        IsTrue(BigIntegerTools.TryParseBinary("11111111111111111.000000000000", out output)); IsTrue(output == 131071);

        // around 3 to 4 byte with random formats
        IsTrue(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output)); IsTrue(output == 21616517498418916);
        IsTrue(BigIntegerTools.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output)); IsTrue(output == 86466069993675667);
        IsTrue(BigIntegerTools.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output)); IsTrue(output == 6148914691236517205);
        IsTrue(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output)); IsTrue(output == 21616517498418916);
        IsTrue(BigIntegerTools.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output)); IsTrue(output == (-86466069993675667));
        IsTrue(BigIntegerTools.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output)); IsTrue(output == 6148914691236517205);

        // around 3 to 4 byte with random formats
        IsTrue(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100", out output)); IsTrue(output == 21616517498418916);
        IsTrue(BigIntegerTools.TryParseBinary("100110011001100000110111010111011000110001101101110010011", out output)); IsTrue(output == 86466069993675667);
        IsTrue(BigIntegerTools.TryParseBinary("101010101010101010101010101010101010101010101010101010101010101", out output)); IsTrue(output == 6148914691236517205);
        IsTrue(BigIntegerTools.TryParseBinary("1001100110011000001101110101110110001100011011011100100.", out output)); IsTrue(output == 21616517498418916);
        IsTrue(BigIntegerTools.TryParseBinary("-100110011001100000110111010111011000110001101101110010011.0", out output)); IsTrue(output == (-86466069993675667));
        IsTrue(BigIntegerTools.TryParseBinary("+101010101010101010101010101010101010101010101010101010101010101.", out output)); IsTrue(output == 6148914691236517205);

        double growthSpeed = 1.01;  // 1.01 for fast, 1.0001 for more extensive
        for (long i = 0; i > 0; i = (long)(i * growthSpeed) + 1)
        {
            BigFloat val = i;
            string binaryBits = Convert.ToString(i, 2);

            // checks several numbers between 0 and long.MaxValue
            string strVal = binaryBits;
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == val);

            // checks several negative numbers between 0 and long.MaxValue
            strVal = "-" + binaryBits;
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == (-i));

            // checks several numbers between 0 and long.MaxValue (with leading plus sign)
            strVal = "+" + binaryBits;
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == i);

            // checks several numbers between 0 and long.MaxValue (with leading '-0')
            strVal = "-0" + binaryBits;
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == (-i));

            // checks several numbers between 0 and long.MaxValue (with with trailing '.')
            strVal = "+" + binaryBits + ".";
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == i);

            // checks several numbers between 0 and long.MaxValue (with with trailing '.0')
            strVal = "-0" + binaryBits + ".0"; ;
            IsTrue(BigIntegerTools.TryParseBinary(strVal, out output));
            IsTrue(output == (-i));
        }
    }

    [TestMethod]
    public void Verify_Parse_BasicStringTests()
    {
        BigInteger biTwo = new(2);
        BigFloat bfTwo = new((BigInteger)0x1FFFFFFFF, 0, true);

        IsTrue(bfTwo.ToString() == biTwo.ToString());
        IsTrue((-bfTwo).ToString() == (-biTwo).ToString());

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

        IsTrue(sbBI.ToString() == sbBF.ToString(), $"""The BigInt output "{sbBI}" did not match the BigFloat output "{sbBF}".""");
    }

    [TestMethod]
    public void Verify_Parse_RoundTripIntegerParseThenToString()
    {
        // test zero
        string outputPos = BigFloat.Parse("0").ToString();
        IsTrue("0" == outputPos, $"Failed converting \"0\" from String->BigFloat->String. The output was \"{outputPos}\".");

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
            IsTrue(input == outputPos, $"Failed converting \"{input}\" from String->BigFloat->String. The output was \"{outputPos}\".");

            input = "-" + input;
            string outputNeg = BigFloat.Parse(input).ToString();
            IsTrue(input == outputNeg, $"Failed converting \"{input}\" from String->BigFloat->String. The output was \"{outputNeg}\".");
        }

        // String -> BigFloat/Decimal -> String
        static void DecimalVsBigFloatToStringChecker(string input)
        {
            string outputBIG = BigFloat.Parse(input).ToString();
            string outputDEC = decimal.Parse(input).ToString();
            IsTrue(outputBIG == outputDEC, $"BigFloat.Parse(\"{input}\").ToString() != Decimal.Parse(\"{input}\").ToString(). [{outputBIG} != {outputDEC}]");
        }
    }

    [TestMethod]
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
        }

        for (int i = 10; i < 100; i++)
        {
            CleanedUpTextVsBigFloatParse("0.00000" + i.ToString());
        }

        for (int i = 100; i < 150; i++)
        {
            CleanedUpTextVsBigFloatParse("0.0000" + i.ToString());
        }
        ////////////////////////////////////////////////////////////
        for (int i = 1; i < 10; i++)
        {
            CleanedUpTextVsBigFloatParse("0.0000" + i.ToString());
        }

        for (int i = 10; i < 100; i++)
        {
            CleanedUpTextVsBigFloatParse("0.000" + i.ToString());
        }

        for (int i = 100; i < 150; i++)
        {
            CleanedUpTextVsBigFloatParse("0.00" + i.ToString());
        }
        ////////////////////////////////////////////////////////////
        for (int i = 1; i < 10; i++)
        {
            CleanedUpTextVsBigFloatParse("0.00" + i.ToString());
        }

        for (int i = 10; i < 100; i++)
        {
            CleanedUpTextVsBigFloatParse("0.0" + i.ToString());
        }

        for (int i = 100; i < 150; i++)
        {
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
        IsTrue(res.ToStringHexScientific() == "0 >> 13");
        res = BigFloat.Parse(".00000000e-4"); // (00000000 = ((8+4) x 3.322  = 39.86)
        IsTrue(res.ToStringHexScientific() == "0 >> 39");

        res = BigFloat.Parse("123.123e+000");  //0 of size 13, so, offset by 13. (00000000 = ((8-4) x 3.322  = 13.28)
        IsTrue(res == BigFloat.Parse("123.123"));
        IsTrue(res == BigFloat.Parse("000123.123"));
        IsTrue(res == BigFloat.Parse("123.123e0"));
        IsTrue(res == BigFloat.Parse("123.123e-0"));
        IsTrue(res == BigFloat.Parse("012312.3e-2"));
        IsTrue(res == BigFloat.Parse("123123.e-3"));
        IsTrue(res == BigFloat.Parse("123123.0e-3"));
        IsTrue(res == BigFloat.Parse(".123123e3"));
        IsTrue(res == BigFloat.Parse("0.123123e3"));
        IsTrue(res == BigFloat.Parse("000.123123e3"));
        IsTrue(res == BigFloat.Parse(".123123e+3"));
        IsTrue(res == BigFloat.Parse(".123123e+0003"));
    }

    /// <summary>
    /// Converts a string to a BigFloat and then back to a string. Supports decimal values and is flexible on the import format.
    /// </summary>
    private static void CleanedUpTextVsBigFloatParse(string stringVal)
    {
        // lets do a String->BigFloat->String
        bool success = BigFloat.TryParse(stringVal, out BigFloat result);

        IsTrue(success, $"Failed converting String[{stringVal}] to BigFloat in BigFloat.TryParse");

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

        if (isNegitive && cleanedStringVal != "0" && cleanedStringVal != "0.0")   // put negative back in unless it is zero
        {
            cleanedStringVal = "-" + cleanedStringVal;
        }

        // lets do a String->Double->String (just for debug error output)
        int digits = stringVal.Length - stringVal.IndexOf('.') - 1;
        string doubleResult = double.Parse(stringVal).ToString("0." + new string('#', digits));

        // compare the String->BigFloat->String with the cleaned up string version
        IsTrue(bigFloatResult == cleanedStringVal, $"Failed converting String[{stringVal}]->BigFloat->ToString[{bigFloatResult}]. For reference: Double->ToString->\"{doubleResult}\".");
    }

    [TestMethod]
    public void Verify_Constructor_BigFloat_WithBigInteger()
    {
        string strVal = "1024.0000002384185791015625";
        BigFloat test1 = new(512 * BigInteger.Parse("4294967297"), 1, true);
        int sizeShouldBe = (int)Math.Round((test1.Size + BigFloat.ExtraHiddenBits) / 3.32192809488736235, 0) + 1;
        // + 1 is for decimal point.
        IsTrue(strVal[0..sizeShouldBe] == test1.ToString(true));

        test1 = new BigFloat(512 * BigInteger.Parse("4294967297"), 1, true);
        IsTrue("102X" == test1.ToString(false));

        strVal = "1024.000000000";
        test1 = new BigFloat(512 * BigInteger.Parse("4294967296"), 1, true);
        IsTrue(strVal == test1.ToString(true));

        strVal = "1023.999999762"; // 1023.9999997615814208984375
        test1 = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true);
        IsTrue(strVal == test1.ToString(true));
    }

    /// <summary>
    /// Test some string values that can not be converted to a big integer.  
    /// </summary>
    [TestMethod]
    //[ExpectedException(typeof(ArgumentNullException))]
    //[ExpectedException(typeof(ArgumentException))]

    public void Verify_TryParse_Errors()
    {
        IsFalse(BigFloat.TryParse("", out _), @"BigFloat.TryParse("""") reported True but should be False.");
        IsFalse(BigFloat.TryParse(" ", out _), @"BigFloat.TryParse("" "") reported True but should be False.");
        IsFalse(BigFloat.TryParse("*", out _), @"BigFloat.TryParse(""*"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("0x1", out _), @"BigFloat.TryParse(""0x1"") reported True but should be False.");
        IsFalse(BigFloat.TryParse(".", out _), @"BigFloat.TryParse(""."") reported True but should be False.");
        IsFalse(BigFloat.TryParse("-", out _), @"BigFloat.TryParse(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("-.", out _), @"BigFloat.TryParse(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse(".-", out _), @"BigFloat.TryParse(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("+.", out _), @"BigFloat.TryParse(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse(".+", out _), @"BigFloat.TryParse(""-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("/", out _), @"BigFloat.TryParse(""/"") reported True but should be False.");
        IsFalse(BigFloat.TryParse(@"\", out _), @"BigFloat.TryParse(""\"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("--1", out _), @"BigFloat.TryParse(""--1"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("1-1", out _), @"BigFloat.TryParse(""1-1"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("0-1", out _), @"BigFloat.TryParse(""0-1"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("0-", out _), @"BigFloat.TryParse(""0-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("0.-", out _), @"BigFloat.TryParse(""0.-"") reported True but should be False.");
        IsFalse(BigFloat.TryParse("1.41.", out _), @"BigFloat.TryParse(""1.41."") reported True but should be False.");
        IsFalse(BigFloat.TryParse(".41.", out _), @"BigFloat.TryParse("".41."") reported True but should be False.");
#if !DEBUG
        _ = Assert.ThrowsException<ArgumentNullException>(() => BigFloat.ParseBinary(null));
        _ = Assert.ThrowsException<ArgumentException>(() => BigFloat.ParseBinary(""));
        _ = Assert.ThrowsException<ArgumentException>(() => BigFloat.ParseBinary(" "));
#endif
    }

    /// <summary>
    /// Test some string values that can not be converted to a big integer.  
    /// </summary>
    [TestMethod]
    public void Verify_ToBinaryStringFormat()
    {
        BigFloat bi;
        string str;

        bi = new(1.5);
        str = bi.ToString("B");
        IsTrue(str == "1.1000000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 1.5");

        bi = new(1.25);
        str = bi.ToString("B");
        IsTrue(str == "1.0100000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 1.25");

        bi = new(2.5);
        str = bi.ToString("B");
        IsTrue(str == "10.100000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 2.5");

        bi = new(3.5);
        str = bi.ToString("B");
        IsTrue(str == "11.100000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 3.5");

        bi = new(7.5);
        str = bi.ToString("B");
        IsTrue(str == "111.10000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 7.5");

        bi = new(15.5);
        str = bi.ToString("B");
        IsTrue(str == "1111.1000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 15.5");

        bi = new(31.25);
        str = bi.ToString("B");
        IsTrue(str == "11111.010000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 31.25");

        bi = new(63.25);
        str = bi.ToString("B");
        IsTrue(str == "111111.01000000000000000000000000000000000000000000000", "Verify_ToStringFormat on 63.25");

        bi = new(127.25);
        str = bi.ToString("B");
        IsTrue(str == "1111111.0100000000000000000000000000000000000000000000", "Verify_ToStringFormat on 127.25");

        bi = new(255.25);
        str = bi.ToString("B");
        IsTrue(str == "11111111.010000000000000000000000000000000000000000000", "Verify_ToStringFormat on 255.25");

        bi = new(0.0000123);
        str = bi.ToString("B");
        IsTrue(str == "0.000000000000000011001110010111000001100100000101100010101000001110000", "Verify_ToStringFormat on 0.0000123");
        //     answer: 0.000000000000000011001110010111000001100100000101100010101000001101111100001000001110001011...
        //                               12345678901234567890123456789012345678901234567890123  (expect 1+52= 53 significant bits)
        //   expected: 0.000000000000000011001110010111000001100100000101100010101000001110000

        bi = new(1230000000);
        str = bi.ToString("B");
        IsTrue(str == "1001001010100000100111110000000", "Verify_ToStringFormat on 1230000000");

        bi = new(123, 5);
        str = bi.ToString("B");
        IsTrue(str == "111101100000", "Verify_ToStringFormat on 123 * 2^5");

        bi = new(123, 40);
        str = bi.ToString("B");
        IsTrue(str == "11110110000000000000000000000000000000000000000", "Verify_ToStringFormat on 123 * 2^5");

        bi = new(0.0000123);
        str = bi.ToString("B");
        IsTrue(str == "0.000000000000000011001110010111000001100100000101100010101000001110000", "Verify_ToStringFormat on 0.0000123");
        //     answer: 0.000000000000000011001110010111000001100100000101100010101000001101111100001000001110001011...
        //                               12345678901234567890123456789012345678901234567890123 (expect 1+52= 53 significant bits)
        //   expected: 0.000000000000000011001110010111000001100100000101100010101000001110000

        bi = new(-0.123);
        str = bi.ToString("B");
        IsTrue(str == "-0.00011111011111001110110110010001011010000111001010110000", "Verify_ToStringFormat on -3.5");
        //     answer: -0.00011001110010111000001100100000101100010101000001101111100001000001110001011...
        //                   12345678901234567890123456789012345678901234567890123 (expect 1+52= 53 significant bits)
        //   expected: -0.00011111011111001110110110010001011010000111001010110000

        bi = new(-3.5);
        str = bi.ToString("B");
        IsTrue(str == "-11.100000000000000000000000000000000000000000000000000", "Verify_ToStringFormat on -3.5");

        bi = new(-1230000000.0);
        str = bi.ToString("B");
        IsTrue(str == "-1001001010100000100111110000000.0000000000000000000000", "Verify_ToStringFormat on 1230000000");

        bi = new(-123, 5);
        str = bi.ToString("B");
        IsTrue(str == "-111101100000", "Verify_ToStringFormat on 123 * 2^5");

        bi = new(-123, 40);
        str = bi.ToString("B");
        IsTrue(str == "-11110110000000000000000000000000000000000000000", "Verify_ToStringFormat on 123 * 2^5");

        //////////////// Int conversions ////////////////
        bi = new(1230000000);
        str = bi.ToString("B");
        IsTrue(str == "1001001010100000100111110000000", "Verify_ToStringFormat on 1230000000");

        bi = new(-1230000000);
        str = bi.ToString("B");
        IsTrue(str == "-1001001010100000100111110000000", "Verify_ToStringFormat on -1230000000");

        //////////////// String conversions ////////////////
        bi = new("1230000000");
        str = bi.ToString("B");
        IsTrue(str == "1001001010100000100111110000000", "Verify_ToStringFormat on 1230000000");

        bi = new("-1230000000");
        str = bi.ToString("B");
        IsTrue(str == "-1001001010100000100111110000000", "Verify_ToStringFormat on -1230000000");

        bi = new("1230000000.0");
        str = bi.ToString("B");
        IsTrue(str == "1001001010100000100111110000000.000", "Verify_ToStringFormat on 1230000000.0");

        bi = new("-123456.7890123");
        str = bi.ToString("B");
        IsTrue(str == "-11110001001000000.11001001111111001011011", "Verify_ToStringFormat on -123456.7890123");
        //     answer: -11110001001000000.110010011111110010110101110010001010010001001001001000000000010010000010010001011011111111...
        //                                012345678901234567890123 (expect 7 * 3.321928 = 23.253 significant binary digits)
        //   expected: -11110001001000000.11001001111111001011011  
    }


    [TestMethod]
    public void Verify_Equals_Byte()
    {
        BigFloat aBigFloat;
        byte bByte;

        aBigFloat = new BigFloat(byte.MinValue);
        bByte = byte.MinValue;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-26 on VerifyEquals(byte.MinValue)");

        aBigFloat = new BigFloat(byte.MinValue + 1);
        bByte = byte.MinValue + 1;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-28 on VerifyEquals(byte.MinValue)");

        aBigFloat = new BigFloat(byte.MinValue + 1);
        bByte = byte.MinValue;
        IsFalse(aBigFloat.Equals(bByte), $"Fail-30 on VerifyEquals(byte.MinValue)");

        aBigFloat = new BigFloat((byte)2);
        bByte = 2;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-16 on VerifyEquals((byte)2)");

        aBigFloat = new BigFloat(byte.MaxValue);
        bByte = byte.MaxValue;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-20 on VerifyEquals(byte.MaxValue)");

        aBigFloat = new BigFloat(byte.MaxValue);
        bByte = byte.MaxValue;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-20 on VerifyEquals(byte.MaxValue)");

        aBigFloat = new BigFloat(byte.MaxValue - 1);
        bByte = byte.MaxValue - 1;
        IsTrue(aBigFloat.Equals(bByte), $"Fail-22 on VerifyEquals(byte.MaxValue)");

        aBigFloat = new BigFloat(byte.MaxValue - 1);
        bByte = byte.MaxValue;
        IsFalse(aBigFloat.Equals(bByte), $"Fail-24 on VerifyEquals(byte.MaxValue)");

        aBigFloat = new BigFloat((byte)0);
        bByte = 1;
        IsFalse(aBigFloat.Equals(bByte), $"Fail-33 on VerifyEquals((byte)1)");

        aBigFloat = new BigFloat(256);
        bByte = 0;
        IsFalse(aBigFloat.Equals(bByte), $"Fail-34 on VerifyEquals (byte)256 should be 0");
    }

    [TestMethod]
    public void Verify_Equals_Int()
    {
        BigFloat aBigFloat;
        int bInt;

        aBigFloat = new BigFloat(0);
        bInt = 0;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-10 on VerifyEquals(0)");
        IsFalse(aBigFloat.Equals(1), $"Fail-10 on VerifyEquals(1)");
        IsFalse(aBigFloat.Equals(-1), $"Fail-10 on VerifyEquals(-1)");

        aBigFloat = new BigFloat(1);
        bInt = 1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-12 on VerifyEquals(1)");
        IsFalse(aBigFloat.Equals(2), $"Fail-12 on VerifyEquals(2)");
        IsFalse(aBigFloat.Equals(0), $"Fail-12 on VerifyEquals(0)");
        IsFalse(aBigFloat.Equals(-1), $"Fail-12 on VerifyEquals(-1)");

        aBigFloat = new BigFloat(-1);
        bInt = -1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-14 on VerifyEquals(-1)");
        IsFalse(aBigFloat.Equals(-2), $"Fail-14 on VerifyEquals(-2)");
        IsFalse(aBigFloat.Equals(0), $"Fail-14 on VerifyEquals(0)");
        IsFalse(aBigFloat.Equals(1), $"Fail-14 on VerifyEquals(1)");

        aBigFloat = new BigFloat(2);
        bInt = 2;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-16 on VerifyEquals(2)");

        aBigFloat = new BigFloat(-2);
        bInt = -2;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-18 on VerifyEquals(-2)");

        aBigFloat = new BigFloat(int.MaxValue);
        bInt = int.MaxValue;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-20 on VerifyEquals(int.MaxValue)");

        aBigFloat = new BigFloat(int.MaxValue - 1);
        bInt = int.MaxValue - 1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-22 on VerifyEquals(int.MaxValue)");

        aBigFloat = new BigFloat(int.MaxValue - 1);
        bInt = int.MaxValue;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-24 on VerifyEquals(int.MaxValue)");

        aBigFloat = new BigFloat(int.MinValue);
        bInt = int.MinValue;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-26 on VerifyEquals(int.MinValue)");

        aBigFloat = new BigFloat(int.MinValue + 1);
        bInt = int.MinValue + 1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-28 on VerifyEquals(int.MinValue)");

        aBigFloat = new BigFloat(int.MinValue + 1);
        bInt = int.MinValue;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-30 on VerifyEquals(int.MinValue)");

        aBigFloat = new BigFloat(-1);
        bInt = 1;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-32 on VerifyEquals((int)-1)");

        aBigFloat = new BigFloat(0);
        bInt = 1;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-33 on VerifyEquals((int)1)");

        aBigFloat = new BigFloat(4294967296);
        bInt = 0;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-34 on VerifyEquals((int)0)");
    }

    [TestMethod]
    public void Verify_Equals_UInt()
    {
        BigFloat aBigFloat;
        uint bInt;

        aBigFloat = new BigFloat(uint.MinValue);
        bInt = uint.MinValue;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-26 on VerifyEquals(uint.MinValue)");
        IsFalse(aBigFloat.Equals(-1));
        IsFalse(aBigFloat.Equals(1));

        aBigFloat = new BigFloat(uint.MinValue + 1);
        bInt = uint.MinValue + 1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-28 on VerifyEquals(uint.MinValue)");
        IsFalse(aBigFloat.Equals(-1));
        IsFalse(aBigFloat.Equals(0));
        IsFalse(aBigFloat.Equals(2));

        aBigFloat = new BigFloat(uint.MinValue + 1);
        bInt = uint.MinValue;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-30 on VerifyEquals(uint.MinValue)");

        aBigFloat = new BigFloat((uint)2);
        bInt = 2;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-16 on VerifyEquals((uint)2)");

        aBigFloat = new BigFloat(uint.MaxValue);
        bInt = uint.MaxValue;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-20 on VerifyEquals(uint.MaxValue)");

        aBigFloat = new BigFloat(uint.MaxValue);
        bInt = uint.MaxValue;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-20 on VerifyEquals(uint.MaxValue)");

        aBigFloat = new BigFloat(uint.MaxValue - 1);
        bInt = uint.MaxValue - 1;
        IsTrue(aBigFloat.Equals(bInt), $"Fail-22 on VerifyEquals(uint.MaxValue)");

        aBigFloat = new BigFloat(uint.MaxValue - 1);
        bInt = uint.MaxValue;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-24 on VerifyEquals(uint.MaxValue)");

        aBigFloat = new BigFloat((uint)0);
        bInt = 1;
        IsFalse(aBigFloat.Equals(bInt), $"Fail-33 on VerifyEquals((uint)1)");
    }

    [TestMethod]
    public void Verify_Equals_Long()
    {
        BigFloat aBigFloat;
        long bLong;

        aBigFloat = new BigFloat((long)0);
        bLong = 0;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-10 on VerifyEquals(0)");

        aBigFloat = new BigFloat((long)1);
        bLong = 1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-12 on VerifyEquals(1)");

        aBigFloat = new BigFloat((long)-1);
        bLong = -1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-14 on VerifyEquals(-1)");

        aBigFloat = new BigFloat((long)2);
        bLong = 2;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-16 on VerifyEquals(2)");

        aBigFloat = new BigFloat((long)-2);
        bLong = -2;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-18 on VerifyEquals(-2)");

        aBigFloat = new BigFloat(long.MaxValue);
        bLong = long.MaxValue;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-20 on VerifyEquals(long.MaxValue)");

        aBigFloat = new BigFloat(long.MaxValue - 1);
        bLong = long.MaxValue - 1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-22 on VerifyEquals(long.MaxValue)");

        aBigFloat = new BigFloat(long.MaxValue - 1);
        bLong = long.MaxValue;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-24 on VerifyEquals(long.MaxValue)");

        aBigFloat = new BigFloat(long.MinValue);
        bLong = long.MinValue;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-26 on VerifyEquals(long.MinValue)");

        aBigFloat = new BigFloat(long.MinValue + 1);
        bLong = long.MinValue + 1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-28 on VerifyEquals(long.MinValue)");

        aBigFloat = new BigFloat(long.MinValue + 1);
        bLong = long.MinValue;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-30 on VerifyEquals(long.MinValue)");

        aBigFloat = new BigFloat((long)-1);
        bLong = 1;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-32 on VerifyEquals((long)-1)");

        aBigFloat = new BigFloat((long)0);
        bLong = 1;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-33 on VerifyEquals((long)1)");

        aBigFloat = new BigFloat(0xFF, 8);
        bLong = 0xFF00;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-40 on VerifyEquals((long)1)");

        aBigFloat = new BigFloat(0xFF, 8);
        bLong = 0xFFFF;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-42 on VerifyEquals((long)1)");
    }

    [TestMethod]
    public void Verify_Equals_ULong()
    {
        BigFloat aBigFloat;
        ulong bLong;

        aBigFloat = new BigFloat(ulong.MinValue);
        bLong = ulong.MinValue;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-26 on VerifyEquals(ulong.MinValue)");

        aBigFloat = new BigFloat(ulong.MinValue + 1);
        bLong = ulong.MinValue + 1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-28 on VerifyEquals(ulong.MinValue)");

        aBigFloat = new BigFloat(ulong.MinValue + 1);
        bLong = ulong.MinValue;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-30 on VerifyEquals(ulong.MinValue)");

        aBigFloat = new BigFloat((ulong)2);
        bLong = 2;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-16 on VerifyEquals((ulong)2)");

        aBigFloat = new BigFloat(ulong.MaxValue);
        bLong = ulong.MaxValue;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-20 on VerifyEquals(ulong.MaxValue)");

        aBigFloat = new BigFloat(ulong.MaxValue);
        bLong = ulong.MaxValue - 1;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-21 on VerifyEquals(ulong.MaxValue)");

        aBigFloat = new BigFloat(ulong.MaxValue - 1);
        bLong = ulong.MaxValue - 1;
        IsTrue(aBigFloat.Equals(bLong), $"Fail-22 on VerifyEquals(ulong.MaxValue)");

        aBigFloat = new BigFloat(ulong.MaxValue - 1);
        bLong = ulong.MaxValue;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-24 on VerifyEquals(ulong.MaxValue)");

        aBigFloat = new BigFloat((ulong)0);
        bLong = 1;
        IsFalse(aBigFloat.Equals(bLong), $"Fail-33 on VerifyEquals((ulong)1)");
    }

    [TestMethod]
    public void Verify_ToString_LessThenOne()
    {
        string actual;
        actual = new BigFloat(0.0000123).ToString(); AreEqual("0.000012300000000000001", actual, false, $"Fail-1 on Double->BigFloat->ToString");
        actual = new BigFloat(0.000123).ToString(); AreEqual("0.00012300000000000001", actual, false, $"Fail-2 on Double->BigFloat->ToString");
        actual = new BigFloat(0.00123).ToString(); AreEqual("0.0012300000000000000", actual, false, $"Fail-3 on Double->BigFloat->ToString");
        actual = new BigFloat(0.0123).ToString(); AreEqual("0.012300000000000000", actual, false, $"Fail-4 on Double->BigFloat->ToString");
        actual = new BigFloat(0.123).ToString(); AreEqual("0.12300000000000000", actual, false, $"Fail-5 on Double->BigFloat->ToString");
    }

    [TestMethod]
    public void Verify_ToString_GreaterThenOne_WithFraction()
    {
        string actual;
        actual = new BigFloat(1.23).ToString(); AreEqual("1.2300000000000000", actual, false, $"Fail-6 on Double->BigFloat->ToString");
        actual = new BigFloat(12.3).ToString(); AreEqual("12.300000000000001", actual, false, $"Fail-7 on Double->BigFloat->ToString");
        actual = new BigFloat(123.0).ToString(); AreEqual("123.00000000000000", actual, false, $"Fail-8 on Double->BigFloat->ToString");
        actual = new BigFloat(1230.0).ToString(); AreEqual("1230.0000000000000", actual, false, $"Fail-9 on Double->BigFloat->ToString");
        actual = new BigFloat(12300.0).ToString(); AreEqual("12300.000000000000", actual, false, $"Fail-A on Double->BigFloat->ToString");
        actual = new BigFloat(123000.0).ToString(); AreEqual("123000.00000000000", actual, false, $"Fail-B on Double->BigFloat->ToString");
        actual = new BigFloat(1230000.0).ToString(); AreEqual("1230000.0000000000", actual, false, $"Fail-C on Double->BigFloat->ToString");
        actual = new BigFloat(12300000.0).ToString(); AreEqual("12300000.000000000", actual, false, $"Fail-D on Double->BigFloat->ToString");
        actual = new BigFloat(123000000.0).ToString(); AreEqual("123000000.00000000", actual, false, $"Fail-E on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000.0).ToString(); AreEqual("1230000000.0000000", actual, false, $"Fail-F on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000.0).ToString(); AreEqual("12300000000.000000", actual, false, $"Fail-G on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000.0).ToString(); AreEqual("123000000000.00000", actual, false, $"Fail-H on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000.0).ToString(); AreEqual("1230000000000.0000", actual, false, $"Fail-I on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000000.0).ToString(); AreEqual("12300000000000.000", actual, false, $"Fail-J on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000.0).ToString(); AreEqual("123000000000000.00", actual, false, $"Fail-K on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000000.0).ToString(); AreEqual("1230000000000000.0", actual, false, $"Fail-L on Double->BigFloat->ToString");
    }

    [TestMethod]
    public void Verify_ToString_WholeNumbers()
    {
        string actual;
        // Note: We have includeOutOfPrecisionBits set to true so we expect to see 32 bits of out of precision bits here. 
        actual = BigFloat.ToStringDecimal(new BigFloat(12300000000000000.0), true);                    AreEqual("12300000000000000.000000000", actual, false, $"Fail-M on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000.0), true);                   AreEqual("123000000000000000.00000000", actual, false, $"Fail-N on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000.0), true);                  AreEqual("1230000000000000000.0000000", actual, false, $"Fail-O on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(12300000000000000000.0), true);                 AreEqual("12300000000000000000.000000", actual, false, $"Fail-P on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000.0), true);                AreEqual("123000000000000000000.00000", actual, false, $"Fail-Q on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000.0), true);               AreEqual("1230000000000000000000.0000", actual, false, $"Fail-R on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000000.0), true);             AreEqual("123000000000000002097152.00", actual, false, $"Fail-S on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000000.0), true);            AreEqual("1229999999999999920308224.0", actual, false, $"Fail-T on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(12300000000000000000000000.0), true);           AreEqual("12300000000000000276824064" , actual, false, $"Fail-U on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000000000.0), true);          AreEqual("12299999999999999847327334X", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000000000.0), true);         AreEqual("12300000000000000190924718XX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000000000000.0), true);       AreEqual("12300000000000000300875881XXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000000000000.0), true);      AreEqual("12299999999999999597188439XXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(12300000000000000000000000000000.0), true);     AreEqual("12299999999999999597188439XXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000000000000000.0), true);    AreEqual("12300000000000000497908365XXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000000000000000.0), true);   AreEqual("12300000000000000137620394XXXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(12300000000000000000000000000000000.0), true);  AreEqual("12300000000000000425850771XXXXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(123000000000000000000000000000000000.0), true); AreEqual("12299999999999999503513567XXXXXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = BigFloat.ToStringDecimal(new BigFloat(1230000000000000000000000000000000000.0), true); AreEqual("12299999999999999503513567e+11", actual, false, $"Fail-V on Double->BigFloat->ToString");

        actual = new BigFloat(12300000000000000.0).ToString();              AreEqual("1230000000000000X", actual, false, $"Fail-M on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000000.0).ToString();             AreEqual("12300000000000000X", actual, false, $"Fail-N on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000000000.0).ToString();            AreEqual("1230000000000000XXX", actual, false, $"Fail-O on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000000000000.0).ToString();           AreEqual("1230000000000000XXXX", actual, false, $"Fail-P on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000000000.0).ToString();          AreEqual("12300000000000000XXXX", actual, false, $"Fail-Q on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000000000000.0).ToString();         AreEqual("1230000000000000XXXXXX", actual, false, $"Fail-R on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000000000000000.0).ToString();        AreEqual("1230000000000000XXXXXXX", actual, false, $"Fail-S on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000000000000.0).ToString();       AreEqual("12300000000000000XXXXXXX", actual, false, $"Fail-T on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000000000000000.0).ToString();      AreEqual("1230000000000000XXXXXXXXX", actual, false, $"Fail-U on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000000000000000000.0).ToString();     AreEqual("1230000000000000XXXXXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000000000000000.0).ToString();    AreEqual("12300000000000000XXXXXXXXXX", actual, false, $"Fail-W on Double->BigFloat->ToString");
        actual = new BigFloat(1230000000000000000000000000.0).ToString();   AreEqual("12300000000000000e+11", actual, false, $"Fail-W on Double->BigFloat->ToString");
        actual = new BigFloat(12300000000000000000000000000.0).ToString();  AreEqual("1230000000000000e+13", actual, false, $"Fail-W on Double->BigFloat->ToString");
        actual = new BigFloat(123000000000000000000000000000.0).ToString(); AreEqual("12300000000000000e+13", actual, false, $"Fail-W on Double->BigFloat->ToString");

        actual = new BigFloat(99990000000000000.0).ToString(); AreEqual("9999000000000000X", actual, false, $"Fail-M on Double->BigFloat->ToString");
        actual = new BigFloat(999900000000000000.0).ToString(); AreEqual("9999000000000000XX", actual, false, $"Fail-N on Double->BigFloat->ToString");
        actual = new BigFloat(9999000000000000000.0).ToString(); AreEqual("999900000000000XXXX", actual, false, $"Fail-O on Double->BigFloat->ToString");
        actual = new BigFloat(99990000000000000000.0).ToString(); AreEqual("9999000000000000XXXX", actual, false, $"Fail-P on Double->BigFloat->ToString");
        actual = new BigFloat(999900000000000000000.0).ToString(); AreEqual("9999000000000000XXXXX", actual, false, $"Fail-Q on Double->BigFloat->ToString");
        actual = new BigFloat(9999000000000000000000.0).ToString(); AreEqual("999900000000000XXXXXXX", actual, false, $"Fail-R on Double->BigFloat->ToString");
        AreEqual(99990000000000000000000.0, 9999000000000001e+7);
        actual = new BigFloat(99990000000000000000000.0).ToString();    AreEqual("9999000000000001XXXXXXX", actual, false, $"Fail-S on Double->BigFloat->ToString");
        actual = new BigFloat(999900000000000000000000.0).ToString();   AreEqual("9999000000000000XXXXXXXX", actual, false, $"Fail-T on Double->BigFloat->ToString");
        actual = new BigFloat(9999000000000000000000000.0).ToString();  AreEqual("999900000000000XXXXXXXXXX", actual, false, $"Fail-U on Double->BigFloat->ToString");
        actual = new BigFloat(99990000000000000000000000.0).ToString(); AreEqual("9999000000000000XXXXXXXXXX", actual, false, $"Fail-V on Double->BigFloat->ToString");
        AreEqual(999900000000000000000000000.0, 9999000000000001e+11);
        actual = new BigFloat(999900000000000000000000000.0).ToString(); AreEqual("9999000000000001e+11", actual, false, $"Fail-W on Double->BigFloat->ToString");
        actual = new BigFloat(9999000000000000000000000000.0).ToString(); AreEqual("999900000000000e+13", actual, false, $"Fail-W on Double->BigFloat->ToString");

        for (int i = 1; i < 2883; i++)
        {
            float floatVal = float.Parse(i.ToString() + "00000000000000.0");
            actual = new BigFloat(floatVal).ToString();
            IsTrue(actual.Contains("9X") || actual.Contains("0X") || actual.Contains("1X"));
        }

        for (int i = 2883; i < 10000; i++)
        {
            float floatVal = float.Parse(i.ToString() + "00000000000000.0");
            actual = new BigFloat(floatVal).ToString();
            IsTrue(actual.Contains("e+"));
        }

        for (int i = 1; i < 10000; i++)
        {
            double doubleVal = double.Parse(i.ToString() + "000000000000000000000000000.0");
            actual = new BigFloat(doubleVal).ToString();
            IsTrue(actual.Contains("e+"));
        }
    }

    [TestMethod]
    public void Verify_CompareTo()
    {
        BigFloat a, b;

        a = new BigFloat(0);
        b = new BigFloat(0);
        IsFalse(a < b, $"Fail-8a on VerifyCompareTo");
        IsFalse(b > a, $"Fail-8b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-8c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-8d on VerifyCompareTo");
        IsTrue(a == b, $"Fail-8e on VerifyCompareTo");
        IsFalse(a != b, $"Fail-8f on VerifyCompareTo");

        a = new BigFloat(-1);
        b = new BigFloat(0);
        IsTrue(a < b, $"Fail-10a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-10b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-10c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-10d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-10e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-10f on VerifyCompareTo");

        a = new BigFloat(0);
        b = new BigFloat(1);
        IsTrue(a < b, $"Fail-20a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-20b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-20c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-20d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-20e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-20f on VerifyCompareTo");

        a = new BigFloat(1);
        b = new BigFloat(2);
        IsTrue(a < b, $"Fail-30a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-30b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-30c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-30d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-30e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-30f on VerifyCompareTo");

        a = new BigFloat(-2);
        b = new BigFloat(-1);
        IsTrue(a < b, $"Fail-31a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-31b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-31c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-31d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-31e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-31f on VerifyCompareTo");

        // Negative 
        a = new BigFloat(-0.0000123);
        b = new BigFloat(0.0000123);
        IsTrue(a < b, $"Fail-40a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-40b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-40c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-40d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-40e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-40f on VerifyCompareTo");

        a = new BigFloat(-0.0000000445);
        b = new BigFloat(-0.0000000444);
        IsTrue(a < b, $"Fail-50a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-50b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-50c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-50d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-50e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-50f on VerifyCompareTo");

        a = new BigFloat(0.0000122);
        b = new BigFloat(0.0000123);
        IsTrue(a < b, $"Fail-60a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-60b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-60c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-60d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-60e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-60f on VerifyCompareTo");

        a = new BigFloat(100000000.000000);
        b = new BigFloat(100000000.000001);
        IsTrue(a < b, $"Fail-80a on VerifyCompareTo");
        IsFalse(b < a, $"Fail-80aa on VerifyCompareTo");
        IsTrue(b > a, $"Fail-80b on VerifyCompareTo");
        IsFalse(a > b, $"Fail-80bb on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-80c on VerifyCompareTo");
        IsFalse(b <= a, $"Fail-80cc on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-80d on VerifyCompareTo");
        IsFalse(a >= b, $"Fail-80dd on VerifyCompareTo");
        IsFalse(a == b, $"Fail-80e on VerifyCompareTo");
        IsFalse(b == a, $"Fail-80ee on VerifyCompareTo");
        IsTrue(a != b, $"Fail-80f on VerifyCompareTo");
        IsTrue(b != a, $"Fail-80ff on VerifyCompareTo");

        // Zero ranges
        a = new BigFloat(-1.0000000);
        b = new BigFloat(0.0000000);
        IsTrue(a < b, $"Fail-90a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-90b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-90c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-90d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-90e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-90f on VerifyCompareTo");

        a = new BigFloat(0.0000000);
        b = new BigFloat(1.0000000);
        IsTrue(a < b, $"Fail-100a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-100b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-100c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-100d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-100e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-100f on VerifyCompareTo");

        a = new BigFloat(-0.0000001);
        b = new BigFloat(0.0000000);
        IsTrue(a < b, $"Fail-110a on VerifyCompareTo");
        IsTrue(b > a, $"Fail-110b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-110c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-110d on VerifyCompareTo");
        IsFalse(a == b, $"Fail-110e on VerifyCompareTo");
        IsTrue(a != b, $"Fail-110f on VerifyCompareTo");

        a = new BigFloat(0.0000000);
        b = new BigFloat(0.0000000);
        IsFalse(a < b, $"Fail-120a on VerifyCompareTo");
        IsFalse(b > a, $"Fail-120b on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-120c on VerifyCompareTo");
        IsTrue(b >= a, $"Fail-120d on VerifyCompareTo");
        IsTrue(a == b, $"Fail-120e on VerifyCompareTo");
        IsFalse(a != b, $"Fail-120f on VerifyCompareTo");

        a = new BigFloat(0.00000);
        b = new BigFloat(0.0000000);
        IsTrue(a == b, $"Fail-130a on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-130b on VerifyCompareTo");
        IsTrue(a >= b, $"Fail-130c on VerifyCompareTo");
        IsFalse(a < b, $"Fail-130d on VerifyCompareTo");
        IsFalse(a > b, $"Fail-130e on VerifyCompareTo");
        IsFalse(a != b, $"Fail-130f on VerifyCompareTo");

        a = new BigFloat(0.000001000);
        b = new BigFloat(0.000001);
        IsTrue(a == b, $"Fail-140a on VerifyCompareTo");
        IsTrue(a <= b, $"Fail-140b on VerifyCompareTo");
        IsTrue(a >= b, $"Fail-140c on VerifyCompareTo");
        IsFalse(a < b, $"Fail-140d on VerifyCompareTo");
        IsFalse(a > b, $"Fail-140e on VerifyCompareTo");
        IsFalse(a != b, $"Fail-140f on VerifyCompareTo");
    }

    [TestMethod]
    public void Verify_CompareToExact_On_Single()
    {
        BigFloat a, b;

        // Integers
        a = new BigFloat((float)-1);
        b = new BigFloat((float)0);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-10a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)0);
        b = new BigFloat((float)1);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-20a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)1);
        b = new BigFloat((float)2);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-30a on Verify_CompareToExact_On_Single");

        // Floats of same size (should be same as VerifyCompareTo)
        a = new BigFloat((float)-0.0000123);
        b = new BigFloat((float)0.0000123);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-40a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)-0.0000000444);
        b = new BigFloat((float)-0.0000000445);
        IsTrue(a.CompareToExact(b) > 0, $"Fail-50a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)0.0000123);
        b = new BigFloat((float)0.0000122);
        IsTrue(a.CompareToExact(b) > 0, $"Fail-60a on Verify_CompareToExact_On_Single");

        a = new BigFloat(float.Parse("0.0000123"));
        b = new BigFloat(float.Parse("0.0000122"));
        IsTrue(a.CompareToExact(b) > 0, $"Fail-65 on Verify_CompareToExact_On_Single");

        // 1.000000001 is beyond the precision of single
        a = new BigFloat((float)100.000000);
        b = new BigFloat((float)100.000001);
        IsTrue(a.CompareToExact(b) == 0, $"Fail-80a on Verify_CompareToExact_On_Single");

        // These values are first translated from 53 bit doubles, then 24 bit floats
        a = new BigFloat((float)0.0000123);  //0.0000000000000000110011100101110000011001
        b = new BigFloat((float)0.00001234); //0.000000000000000011001111000001111110010
        IsTrue(a.CompareToExact(b) < 0, $"Fail-40a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)-0.000000044501); // 0.000000000000000000000000101111110010000101011101111100
        b = new BigFloat((float)-0.0000000445);   // 0.000000000000000000000000101111110010000001000100011101
        IsTrue(a.CompareToExact(b) < 0, $"Fail-50a on Verify_CompareToExact_On_Single");

        // 1.000000001 is beyond the precision of single
        a = new BigFloat((float)1.000000001);
        b = new BigFloat((float)1.000000002);
        IsTrue(a.CompareToExact(b) == 0, $"Fail-55a on Verify_CompareToExact_On_Single");

        a = new BigFloat((float)1.0);
        b = new BigFloat((float)1.01);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-60a on Verify_CompareToExact_On_Single");
    }

    [TestMethod]
    public void CompareToIgnoringLeastSigBitsFast()
    {
        BigFloat a, b;

        a = new BigFloat(-1);
        b = new BigFloat(0);
        // "-1 < 0" OR "-1 - 0 = -1" so NEG
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) < 0, $"Fail-10 on CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) > 0, $"Fail-20 on CompareToIgnoringLeastSigBitsFast");

        for (a = -5; a < 5; a++)
        {
            for (int i = 0; i < 5; i++)
            {
                IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, a, i) == 0, $"Fail-30 on CompareToIgnoringLeastSigBitsFast");
            }
        }


        // an unclear answer here that either way would be acceptable. (so checking for >= 0)
        // Case 1: "1" would right shift and then round away from zero. So it would be +1(greater).
        // Case 2: 1 - 0 = 1 --> but then ignore one bit -->  so both are zero(equal)
        a = new BigFloat(1);
        b = new BigFloat(0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) >= 0, $"Fail-70 on CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) <= 0, $"Fail-60 on CompareToIgnoringLeastSigBitsFast");

        // same questionable item as above. It certainly should not be the incorrect sign, though.
        a = new BigFloat(-1);
        b = new BigFloat(0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) <= 0, $"Fail-80 on CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) >= 0, $"Fail-90 on CompareToIgnoringLeastSigBitsFast");


        a = new BigFloat(0);
        b = new BigFloat(1);
        // 0|0000...0000  - 1|0000...0000 = -1|0000...0000
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-100 on CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-110 on CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-1);
        b = new BigFloat(0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-120 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-130 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(2);
        b = new BigFloat(1);
        //10 - 1 = 1 ===> ignore bottom bit, is zero or equal
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-140 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-150 on Verify_CompareToIgnoringLeastSigBitsFast");


        a = new BigFloat(-1);
        b = new BigFloat(-2);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-160 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-710 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-1);
        b = new BigFloat(2);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) < 0, $"Fail-180 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) > 0, $"Fail-190 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(2); // 10 -> .10
        b = new BigFloat(1); // -1 -> .01
                             //  1 -> .01 -> 0
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-140 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(1); //  1 -> .01
        b = new BigFloat(2); //-10 -> .10
                             // -1 -> .01 -> 0
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-150 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-1);
        b = new BigFloat(-2);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-160 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-161 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-1);
        b = new BigFloat(2);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) < 0, $"Fail-180 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) > 0, $"Fail-181 on Verify_CompareToIgnoringLeastSigBitsFast");

        // Floats of same size
        a = new BigFloat((float)-0.0000123);
        b = new BigFloat((float)0.0000123);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) < 0, $"Fail-200 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 3) > 0, $"Fail-200 on Verify_CompareToIgnoringLeastSigBitsFast");

        // Floats of same size                    0.00000000000000000000000010111110101100100101000011110111010010...(answer from https://www.exploringbinary.com/binary-converter)
        a = new BigFloat((float)-0.0000000444);//0.000000000000000000000000101111101011001001010001                 (init via float)
        //                                        0.00000000000000000000000010111111001000000100010001110110101100...(answer from https://www.exploringbinary.com/binary-converter)
        b = new BigFloat((float)-0.0000000445);//0.000000000000000000000000101111110010000001000100                 (init via float)
        //                                                                             HiddenBits  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) > 0, $"Fail-210 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) < 0, $"Fail-210 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat((double)-0.0000000444);
        b = new BigFloat((double)-0.0000000445);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) > 0, $"Fail-220 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) < 0, $"Fail-220 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat("0b11", 0);
        b = new BigFloat("0b01", 0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) > 0, $"Fail-330 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) > 0, $"Fail-300 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-310 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-320 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat("-0b11", 0);
        b = new BigFloat("-0b01", 0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) < 0, $"Fail-330 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) < 0, $"Fail-330 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-340 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-350 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat("-0b11", 1);
        b = new BigFloat("-0b01", 1);
        // -11_ - -1_ -->(Line up) -> -11 - -1 --> Sub --> -10 --> (remove one bit ) --> -1
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) < 0, $"Fail-360 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) > 0, $"Fail-360 on Verify_CompareToIgnoringLeastSigBitsFast");
        // -11_ - -1_ -->(Line up) -> -11 - -1 --> Sub --> -10 --> (remove two bits) --> -.1 -> Rounds to -1
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-370 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-370 on Verify_CompareToIgnoringLeastSigBitsFast");
        // -11_ - -1_ -->(Line up) -> -11 - -1 --> Sub --> -10 --> (remove three bits) --> -.01 -> Rounds to 0
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-380 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 3) == 0, $"Fail-380 on Verify_CompareToIgnoringLeastSigBitsFast");

        _ = BigFloat.TryParseBinary("10.001", out a); // 10.0001000.. 10.001011111
        _ = BigFloat.TryParseBinary("10.01", out b);  //              10.0010000.. 10.010111111 
                                                      // overlap  10.001000000 to 10.001011111
                                                      // miss     10.001100000 to 10.010111111   and  10.000100000 to 10.001011110
                                                      // (10.001, 10.01) => (10.01, 10.01) => 0
                                                      // The following two tests are extreme edge cases that can go either way. Since the compare function only checks in-precision bits.
                                                      // 10.001 rounds to 10.01, which equals 10.01.However, if we subtract them, we get 0:1, which would round to 1:0, letting us know the difference when rounded, is 1 and not equal.
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-371 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-371 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-381 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 3) == 0, $"Fail-381 on Verify_CompareToIgnoringLeastSigBitsFast");

        _ = BigFloat.TryParseBinary("10.0001", out a);
        _ = BigFloat.TryParseBinary("10.01", out b);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) == -1, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) == 1, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-361 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-371 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-371 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-381 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 3) == 0, $"Fail-381 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat("-0b11", 0);
        b = new BigFloat("-0b01", 1);
        //Line up    11
        //           1_  Are these equal? can be either way
        // case for no : 11 rounds to 3 and 1_ is (0.100000... to 10.011111..)
        // case for yes: 11 == 1_ because the _ is considered unknown.
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) == 0, $"Fail-394 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) == 0, $"Fail-396 on Verify_CompareToIgnoringLeastSigBitsFast");

        //Line up    11
        //           1_  Are these equal if we ignore the bottom bit, yes
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-390 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-392 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(555, 0);
        b = new BigFloat(554, 0);
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) > 0, $"Fail-400 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) < 0, $"Fail-400 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-400 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-400 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-555, 0);
        b = new BigFloat(-554, 0);
        // -1000101011.0 - -1000101010.0 = 1.0 --> Shift 0 --> 1
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) < 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) > 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");
        // -1000101011.0 - -1000101010.0 = 1.0 --> Shift 1 --> 0.1 ---> Round --> 1
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) == 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) == 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");
        // -1000101011.0 - -1000101010.0 = 1.0 --> Shift 2 --> 0.01 ---> Round --> 0
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-410 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(-555, 0); //  -555    -1000101011
        b = new BigFloat(-554, 1); // -1108   -1000101010_
        // -555 - -1108 = pos
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) > 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) < 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) > 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) < 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 20) == 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 20) == 0, $"Fail-420 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat(555, 0);  //  555    1000101011
        b = new BigFloat(554, 1);  // 1108   1000101010_
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) < 0, $"Fail-430 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) > 0, $"Fail-430 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) < 0, $"Fail-430 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) > 0, $"Fail-430 on Verify_CompareToIgnoringLeastSigBitsFast");

        a = new BigFloat("55555555555555555555552");
        b = new BigFloat("55555555555555555555554");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 0) < 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 0) > 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 1) < 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 1) > 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 2) == 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 2) == 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(a, b, 3) == 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
        IsTrue(BigFloat.CompareToIgnoringLeastSigBits(b, a, 3) == 0, $"Fail-440 on Verify_CompareToIgnoringLeastSigBitsFast");
    }


    [TestMethod]
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
            IsTrue(result == expectedResult, $"Fail on Verify_NumberOfMatchingLeadingBitsWithRounding({a},{b},{expectedResult},{expectedSign})");
            IsTrue(sign == expectedSign);
        }
    }


    [TestMethod]
    public void Verify_NumberOfMatchingLeadingBits()
    {
        _ = BigFloat.TryParseBinary("10.111", out BigFloat a);
        _ = BigFloat.TryParseBinary("10.101", out BigFloat b);
        int result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 3, $"Fail-10 on Verify_NumberOfMatchingLeadingBits");

        _ = BigFloat.TryParseBinary("1111100", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 1, $"Fail-20 on Verify_NumberOfMatchingLeadingBits");

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("10000000", out b);
        result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 4, $"Fail-30 on Verify_NumberOfMatchingLeadingBits");

        _ = BigFloat.TryParseBinary("10001000", out a);
        _ = BigFloat.TryParseBinary("1000000000", out b);
        result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 4, $"Fail-40 on Verify_NumberOfMatchingLeadingBits");

        a = new BigFloat(-1);
        b = new BigFloat(0);
        result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 0, $"Fail-50 on Verify_NumberOfMatchingLeadingBits");

        a = new BigFloat(-3);
        b = new BigFloat(3);
        result = BigFloat.NumberOfMatchingLeadingBits(a, b);
        IsTrue(result == 0, $"Fail-60 on Verify_NumberOfMatchingLeadingBits");
    }

    [TestMethod]
    public void Verify_CastFromDouble()
    {


        int count = 0;
        for (float ii = 0.0001F; ii < 100000; ii *= 1.0001F)
        {
            count++;
            BigFloat fromSingle = (BigFloat)ii;
            IsTrue((float)fromSingle == ii);
        }
        for (double ii = 0.0001; ii < 100000; ii *= 1.0001)
        {
            count++;
            BigFloat fromDouble = (BigFloat)ii;
            IsTrue((double)fromDouble == ii);
        }
        Debug.WriteLine($"Verify_CastFromDouble  Count {count}");

        BigFloat a = (BigFloat)0.414682509851111660248;         //0.0110101000101000101000100000101000001000101000100000100000101000...
        BigFloat d = (BigFloat)(double)0.414682509851111660248;
        IsTrue(a == d);
        BigFloat f = (BigFloat)(float)0.414682509851111660248;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("0.0111101100110000101100101011101100010100010110000010011001010010");
        d = (BigFloat)(double)0.481211825059603447497;
        IsTrue(a == d);
        f = (BigFloat)(float)0.481211825059603447497;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("10.1010111101111001110010000100011110001101101000011010111011110010");
        d = (BigFloat)(double)2.685452001065306445309;
        IsTrue(a == d);
        f = (BigFloat)(float)2.685452001065306445309;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("0.01010101010101010101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.333333333333333333333333333;
        IsTrue(a == d);
        f = (BigFloat)(float)0.333333333333333333333333333;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("0.101010101010101010101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.666666666666666666666666666;
        IsTrue(a == d);
        f = (BigFloat)(float)0.666666666666666666666666666;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)0.000325520833333333333333333;
        IsTrue(a == d);
        f = (BigFloat)(float)0.000325520833333333333333333;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("-0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)-0.000325520833333333333333333;
        IsTrue(a == d);
        f = (BigFloat)(float)-0.000325520833333333333333333;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("-0.1111111111111111111111111111111111111111111111111111111111111111", 0, 0, 32);
        d = (BigFloat)(double)-0.999999999999999999999999999;
        IsTrue(a == d);
        f = (BigFloat)(float)-0.999999999999999999999999999;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("-0.00000000000101010101010101010101010101010101010101010101010101");
        d = (BigFloat)(double)-0.000325520833333333333333333;
        IsTrue(a == d);
        f = (BigFloat)(float)-0.000325520833333333333333333;
        IsTrue(f == d);

        a = BigFloat.ParseBinary("0.1111111111111111111111111111111111111111111111111111111111111111", 0, 0, 32);
        d = (BigFloat)(double)0.999999999999999999999999999;
        IsTrue(a == d);
        f = (BigFloat)(float)0.999999999999999999999999999;
        IsTrue(f == d);

        //1.79769E+308
        a = BigFloat.ParseBinary("1111111111111111111000101011111001010100000101010111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        d = (BigFloat)(double)179769000000000006323030492138942643493033036433685336215410983289126434148906289940615299632196609445533816320312774433484859900046491141051651091672734470972759941382582304802812882753059262973637182942535982636884444611376868582636745405553206881859340916340092953230149901406738427651121855107737424232448.0;
        IsTrue(a == d);
    }


    [TestMethod]
    public void Verify_Cast_BigFloat_to_Double()
    {
        double res;
        res = (double)new BigFloat(123);
        IsTrue((int)res == 123);

        for (double d = -2.345; d < 12.34; d = 0.1 + (d * 1.007))
        {
            res = (double)new BigFloat(d);
            IsTrue(d == res);
        }
    }

    [TestMethod]
    public void Verify_CompareToExact_On_Doubles()
    {
        BigFloat a, b;

        // Integers
        a = new BigFloat(-1);
        b = new BigFloat(0);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-10 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(0);
        b = new BigFloat(1);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-20 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(1);
        b = new BigFloat(2);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-30 on Verify_CompareToExact_With_Doubles");

        // Floats of same size (should be same as VerifyCompareTo)
        a = new BigFloat(-0.0000123);
        b = new BigFloat(0.0000123);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-40 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(-0.0000000445);
        b = new BigFloat(-0.0000000444);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-50 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(0.0000122);
        b = new BigFloat(0.0000123);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-60 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(100000000.000000);
        b = new BigFloat(100000000.000001);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-70 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat("100000000.000000");
        b = new BigFloat("100000000.000001");
        IsTrue(a.CompareToExact(b) < 0, $"Fail-80 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat("100000000.000001");
        b = new BigFloat(100000000.000001d);
        //TrueAns101111101011110000100000000.00000000000000000001000011000110111101111010000010110101111011...  (matches / good)
        //a      1011111010111100001000000000000000000000000000100001100011011110111101000001011          450359962737054103599627 (5f5e100000010c6f7a0b)
        //a                                                     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //b      1011111010111100001000000000000000000000000000100001100000000000000000000000000000000  28823037615171462162808832 (17d7840000004300000000)
        //b                                                           XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //b      1011111010111100001000000000000000000000000000100001100000000000000000000000000000000  28823037615171462162808832 (17d7840000004300000000)
        //area ignored for CompareToExact()                                                     XXXXXX
        //area ignored for Compare()                            XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        IsFalse(a.CompareToExact(b) == 0, $"Fail-80 on Verify_CompareToExact_With_Doubles");
        IsTrue(a.CompareTo(b) == 0, $"Fail-80 on Verify_CompareToExact_With_Doubles");

        // Floats of different sizes 
        // These values are first translated from 52 bit doubles
        a = new BigFloat(0.0000123);  //0.0000000000000000110011100101110000011001
        b = new BigFloat(0.00001234); //0.000000000000000011001111000001111110010
        IsTrue(a.CompareToExact(b) < 0, $"Fail-90 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(-0.000000044501); // 0.000000000000000000000000101111110010000101011101111100
        b = new BigFloat(-0.0000000445);   // 0.000000000000000000000000101111110010000001000100011101
        IsTrue(a.CompareToExact(b) < 0, $"Fail-100 on Verify_CompareToExact_With_Doubles");

        // 1.00000000000000001 is beyond the precision of double
        a = new BigFloat(1.00000000000000001);
        b = new BigFloat(1.00000000000000002);
        IsTrue(a.CompareToExact(b) == 0, $"Fail-110 on Verify_CompareToExact_With_Doubles");

        a = new BigFloat(1.0);
        b = new BigFloat(1.01);
        IsTrue(a.CompareToExact(b) < 0, $"Fail-120 on Verify_CompareToExact_With_Doubles");
    }

    [TestMethod]
    public void Verify_FitsInADouble()
    {
        IsTrue(new BigFloat("1.000").FitsInADouble(), $"Failed on: FitsInADouble(1.000)");
        IsTrue(new BigFloat("0.000").FitsInADouble(), $"Failed on: FitsInADouble(0.000)");
        IsTrue(new BigFloat("-99.000").FitsInADouble(), $"Failed on: FitsInADouble(-99.000)");
        IsTrue(new BigFloat("0.00000001").FitsInADouble(), $"Failed on: FitsInADouble(0.00000001)");
        IsTrue(new BigFloat("-0.00000001").FitsInADouble(), $"Failed on: FitsInADouble(-0.00000001)");
        IsTrue(new BigFloat(double.MaxValue).FitsInADouble(), $"Failed on: FitsInADouble(double.MaxValue)");
        IsTrue(new BigFloat(double.MinValue).FitsInADouble(), $"Failed on: FitsInADouble(double.MinValue)");
        IsTrue(new BigFloat(double.E).FitsInADouble());
        IsTrue(new BigFloat(double.Epsilon).FitsInADouble());
        IsTrue(new BigFloat(double.NegativeZero).FitsInADouble());
        IsTrue(new BigFloat(0).FitsInADouble());
        IsTrue(new BigFloat("0.000000000000000001").FitsInADouble());
        IsTrue(new BigFloat("1000000000000000000").FitsInADouble());
        IsTrue(new BigFloat(-1).FitsInADouble());
        IsTrue(new BigFloat(1).FitsInADouble());

        IsFalse((new BigFloat(double.MaxValue) * (BigFloat)1.0001).FitsInADouble(), $"Failed on: (new BigFloat(double.MaxValue) * (BigFloat)1.0001).FitsInADouble()");
        IsFalse((new BigFloat(double.MinValue) * (BigFloat)1.0001).FitsInADouble(), $"Failed on: (new BigFloat(double.MinValue) * (BigFloat)1.0001).FitsInADouble()");

        // Below checked in Verify_FloatAndDoubleExceptions
        //  IsFalse(new BigFloat(double.NaN).FitsInADouble()); 
        //  IsFalse(new BigFloat(double.NegativeInfinity).FitsInADouble());
        //  IsFalse(new BigFloat(double.PositiveInfinity).FitsInADouble());
    }

    [TestMethod]
    public void Verify_CompareTo_BigInteger()
    {
        BigFloat a;
        BigInteger b;

        for (int i = -5; i < 5; i++)
        {
            a = new BigFloat(i);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) == 0, $"Fail-8a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) == 0, $"Fail-8a on Verify_CompareTo_BigInteger");
        }

        for (double i = -5; i < 5; i++)
        {
            a = new BigFloat(i);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) == 0, $"Fail-9a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) == 0, $"Fail-9b on Verify_CompareTo_BigInteger");
        }

        for (int i = -5; i < 5; i++)
        {
            a = new BigFloat(i + 1);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) > 0, $"Fail-10a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) > 0, $"Fail-10b on Verify_CompareTo_BigInteger");
        }

        for (double i = -5; i < 5; i++)
        {
            a = new BigFloat(i + 1.0);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) > 0, $"Fail-11a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) > 0, $"Fail-11b on Verify_CompareTo_BigInteger");
        }

        for (int i = -5; i < 5; i++)
        {
            a = new BigFloat(i - 1);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) < 0, $"Fail-12a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) < 0, $"Fail-12b on Verify_CompareTo_BigInteger");
        }

        for (double i = -5; i < 5; i++)
        {
            a = new BigFloat(i - 1.0);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) < 0, $"Fail-13a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) < 0, $"Fail-13b on Verify_CompareTo_BigInteger");
        }

        for (long i = long.MinValue >> 1; i < (long.MaxValue >> 2); i += long.MaxValue >> 3)
        {
            a = new BigFloat(i);
            b = new BigInteger(i);
            IsTrue(a.CompareTo((object)b) == 0, $"Fail-14a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) == 0, $"Fail-14b on Verify_CompareTo_BigInteger");
            a = new BigFloat(i);
            b = new BigInteger(i + 1);
            IsTrue(a.CompareTo((object)b) < 0, $"Fail-15a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) < 0, $"Fail-16b on Verify_CompareTo_BigInteger");
            a = new BigFloat(i);
            b = new BigInteger(i - 1);
            IsTrue(a.CompareTo((object)b) > 0, $"Fail-16a on Verify_CompareTo_BigInteger");
            IsTrue(a.CompareTo(b) > 0, $"Fail-16b on Verify_CompareTo_BigInteger");
        }
    }

    [TestMethod]
    public void Verify_Zero()
    {
        string errorOutputFormat = "ParseString({0,10}) -> BigFloat -> String -> Expect: {2,10}, Got: {1}";
        IsNotEqual(".0000", (string x) => BigFloat.Parse(x), "0.0000", errorOutputFormat);
        IsNotEqual("0.000", (string x) => BigFloat.Parse(x), "0.000", errorOutputFormat);
        IsNotEqual("00.00", (string x) => BigFloat.Parse(x), "0.00", errorOutputFormat);
        IsNotEqual("-.0000", (string x) => BigFloat.Parse(x), "0.0000", errorOutputFormat);
        IsNotEqual("-0.000", (string x) => BigFloat.Parse(x), "0.000", errorOutputFormat);
        IsNotEqual("+.000000", (string x) => BigFloat.Parse(x), "0.000000", errorOutputFormat);
        IsNotEqual("0", (string x) => BigFloat.Parse(x), "0", errorOutputFormat);
        IsNotEqual("000", (string x) => BigFloat.Parse(x), "0", errorOutputFormat);
        IsNotEqual("0.0000000000000000000", (string x) => BigFloat.Parse(x), "0.0000000000000000000", errorOutputFormat);
        IsNotEqual("0.0000000000000000000000000000000000000", (string x) => BigFloat.Parse(x), "0.0000000000000000000000000000000000000", errorOutputFormat);
    }

    [TestMethod]
    public void Verify_Increment()
    {
        BigFloat inputVal, expect;
        inputVal = new BigFloat("2.00000000000");
        expect = new BigFloat("3.00000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("1.00000000000");
        expect = new BigFloat("2.00000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("0.00000000000");
        expect = new BigFloat("1.00000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-1.00000000000");
        expect = new BigFloat("0.00000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-2.00000000000");
        expect = new BigFloat("-1.00000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        // With decimal
        inputVal = new BigFloat("2.50000000000");
        expect = new BigFloat("3.50000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("1.440000000000");
        expect = new BigFloat("2.4400000000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("0.0000230000000");
        expect = new BigFloat("1.0000230000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-0.0000230000000");
        expect = new BigFloat("0.999977");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-1.0000430000000");
        expect = new BigFloat("-0.0000430000000");
        inputVal++;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-2.000000000007777");
        expect = new BigFloat("-1.000000000007777");
        inputVal++;
        IsTrue(inputVal == expect);
    }

    [TestMethod]
    public void Verify_Decrement()
    {
        BigFloat inputVal, expect;
        inputVal = new BigFloat("2.00000000000");
        expect = new BigFloat("1.00000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("1.00000000000");
        expect = new BigFloat("0.00000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("0.00000000000");
        expect = new BigFloat("-1.00000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-1.00000000000");
        expect = new BigFloat("-2.00000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-2.00000000000");
        expect = new BigFloat("-3.00000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        // With decimal
        inputVal = new BigFloat("2.50000000000");
        expect = new BigFloat("1.50000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("1.440000000000");
        expect = new BigFloat("0.4400000000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("0.0000230000000");
        expect = new BigFloat("-0.999977");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-0.0000230000000");
        expect = new BigFloat("-1.0000230000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-1.0000430000000");
        expect = new BigFloat("-2.0000430000000");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-2.000000000007777");
        expect = new BigFloat("-3.000000000007777");
        inputVal--;
        IsTrue(inputVal == expect);

        inputVal = new BigFloat("-20000000000000000000000000000000000000000000000000000000000000000000000000000000.000000000007777");
        expect = new BigFloat("-20000000000000000000000000000000000000000000000000000000000000000000000000000001.000000000007777");
        inputVal--;
        IsTrue(inputVal == expect);
    }

    [TestMethod]
    public void Verify_Math_Add()
    {
        BigFloat inputVal0, inputVal1, output, expect;
        inputVal0 = new BigFloat("2.00000000000");
        inputVal1 = new BigFloat("2.00000000000");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("4.00000000000");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("3");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("4");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0.00000000001");
        inputVal1 = new BigFloat("1000000.0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1000000.0");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0.1");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("1");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0");
        inputVal1 = new BigFloat("0");
        output = inputVal0 + inputVal1;
        expect = new BigFloat("0");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("123457855782.27542786378320");        //      123457855782.27542786378320
        inputVal1 = new BigFloat("56784589567864578.05687450567100");   // 56784589567864578.05687450567100
        output = inputVal0 + inputVal1;                                 // 56784713025720360.3323023694542 (this should be enough reduced precision to match)
        expect = new BigFloat("56784713025720360.3323023694542");
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0.0000000012101");  //   0.0000000012101
        inputVal1 = new BigFloat("0.00000000512");    // + 0.00000000512
        output = inputVal0 + inputVal1;               //   0.0000000063301
        expect = new BigFloat("0.00000000633");       //   0.00000000633
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(5555, 10);  // 5688320 + 5555 = 5693875
        inputVal1 = new BigFloat(5555);
        output = inputVal0 + inputVal1;
        expect = new BigFloat("5693875");  // expected: 5693875 result: 5693875
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(55555, 10);  // 56888320 + 5555 = 56893875
        inputVal1 = new BigFloat(5555);
        output = inputVal0 + inputVal1;
        expect = new BigFloat("56893875");  // expected: 56893875 result: 56893440
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        // Test Shortcut for values way out of precision range.
        BigInteger x123456789ABCDEF0 = BigInteger.Parse("123456789ABCDEF0", NumberStyles.AllowHexSpecifier);
        BigInteger x1234560789A = BigInteger.Parse("1234560789A", NumberStyles.AllowHexSpecifier);
        inputVal0 = new BigFloat(x123456789ABCDEF0, 64, true);  // "12345678"9ABCDEF0________.       (Size: 29, _size: 61, Scale: 64)
        inputVal1 = new BigFloat(x1234560789A, 20, true);       // +                "12"34560.789A   (Size:  5, _size: 37, Scale: 20)
        output = inputVal0 + inputVal1;                         //= 12345678"9ABCDEF0________.
        expect = new BigFloat(x123456789ABCDEF0, 64, true);
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        // other add order...
        output = inputVal1 + inputVal0;
        IsTrue(output == expect, $"Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");
    }

    [TestMethod]
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
        IsTrue(passed, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("3");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-2");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0.00000000001");
        inputVal1 = new BigFloat("1000000.0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-1000000.0");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0.1");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("1");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1");
        inputVal1 = new BigFloat("0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("1");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0");
        inputVal1 = new BigFloat("0");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("0");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("123457855782.2754278637832");
        inputVal1 = new BigFloat("56784589567864578.05687450567100");
        output = inputVal0 - inputVal1;
        expect = new BigFloat("-56784466110008795.7814466418878");
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("0.0000000012101"); // 0.00000000121005    0.00000000121015
        inputVal0.DebugPrint();
        inputVal1 = new BigFloat("0.00000000512");   // 0.000000005125      0.000000005115  
        inputVal1.DebugPrint();
        output = inputVal0 - inputVal1;              //-0.00000000391495  -0.00000000390485  so, -0.0000000039  (okay would also be the avg -0.00000000391)  
        output.DebugPrint();
        expect = new BigFloat("-0.00000000391");
        expect.DebugPrint();
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(2119, 18);  //  5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(5555);   //      -5555  
        output = inputVal0 - inputVal1;                  //= 5555_____
        expect = new BigFloat("555572222");  // expected: 555572222 result:555483136  OK
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(2119, 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(555555); //          -10000111101000100011    -555555  
        output = inputVal0 - inputVal1;                  //=100001000101                    5549_____
        expect = new BigFloat(2117, 18);
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(2119, 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(-555555);//          +10000111101000100011    +555555              +555555
        output = inputVal0 - inputVal1;                  //=100001001001                    5561_____            556038691
        expect = new BigFloat(2121, 18);
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(-2119, 18);  // 100001000111                    5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(555555);//          +10000111101000100011    +555555              +555555
        output = inputVal0 - inputVal1;                  //=100001001001                    5561_____            556038691
        expect = new BigFloat(-2121, 18);
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(-2119, 18);  // -100001000111                   -5555_____ (stored as 555483136)
        inputVal1 = new BigFloat(-555555); //           +10000111101000100011    +555555  
        output = inputVal0 - inputVal1;                   //=-100001000101                   -5549_____
        expect = new BigFloat(-2117, 18);
        IsTrue(output == expect, $"Add({inputVal0} - {inputVal1}) was {output} but expected {expect}");

    }

    [TestMethod]
    public void Verify_Math_Multiply()
    {
        BigFloat inputVal0, inputVal1, output, expect;

        inputVal0 = new BigFloat("1.000");
        inputVal1 = new BigFloat("1.000");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("1.000");
        IsTrue(output == expect, $"Step 10: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("255");
        inputVal1 = new BigFloat("255");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("65025");
        IsTrue(output == expect, $"Step 11: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("256");
        inputVal1 = new BigFloat("255");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("65280");
        IsTrue(output == expect, $"Step 12: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(9007199254740991UL);
        inputVal1 = new BigFloat(9007199254740991UL);
        output = inputVal0 * inputVal1;
        // in      11111111111111111111111111111111111111111111111111111   9007199254740991
        // output: 11111111111111111111111111111111011111111111111111111000000000000000000000000000000001  77371252446329059336519681 <<52
        // exact   1111111111111111111111111111111111111111111111111111000000000000000000000000000000000000000000000000000001 81129638414606663681390495662081
        expect = new BigFloat("81129638414606663681390495662081");
        IsTrue(output == expect, $"Step 11: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(9007199254740992UL);
        inputVal1 = new BigFloat(9007199254740991UL);
        output = inputVal0 * inputVal1;
        expect = new BigFloat("81129638414606672688589750403072");
        IsTrue(output == expect, $"Step 12: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("11.000");
        inputVal1 = new BigFloat("3.000");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("33.00");
        IsTrue(output == expect, $"Step 20: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("255", 2);  // 1020
        inputVal1 = new BigFloat("20", -1);  // 10
        output = inputVal0 * inputVal1;
        expect = new BigFloat("20", 9);  // 19.921875 << 9
        IsTrue(output == expect, $"Step 22a: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");
        expect = new BigFloat("10200");
        IsTrue(output == expect, $"Step 22b: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("19", -3);  // 2.375
        inputVal1 = new BigFloat("15", 2);  //  60
        output = inputVal0 * inputVal1;
        expect = new BigFloat("18", 3);  // 142.5
        IsTrue(output == expect, $"Step 24: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("1", 0);
        inputVal1 = new BigFloat("1.0", 1);
        output = inputVal0 * inputVal1;
        expect = new BigFloat("2.0", 0);
        IsTrue(output == expect, $"Step 25: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("19", -3);  // 2.375
        inputVal1 = new BigFloat("1.5", 2);  //  6.0
        output = inputVal0 * inputVal1;
        expect = new BigFloat("14", 0);  // 142.5
        IsTrue(output == expect, $"Step 26: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("3.00");
        inputVal1 = new BigFloat("11.00");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("33.0");
        IsTrue(output == expect, $"Step 30: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("2.00000000000");
        inputVal1 = new BigFloat("2.00000000000");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("4.00000000000");
        IsTrue(output == expect, $"Step 40: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // OVERRIDE TEST: output is 64(not 63) but this is technically okay - maybe this can be improved by a fixed number of bits of precision.
        inputVal0 = new BigFloat("7");
        inputVal1 = new BigFloat("9");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("63"); // output is 64 (8<<3) and this is technically okay. 
        IsTrue(output == expect, $"Step 50: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(11);
        inputVal1 = new BigFloat(9);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99);
        IsTrue(output == expect, $"Step 60: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(11, 8);
        inputVal1 = new BigFloat(9);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99, 8);
        IsTrue(output == expect, $"Step 70: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(4, 8); //     1024
        inputVal1 = new BigFloat(16, 10); //    16384
        output = inputVal0 * inputVal1;
        expect = new BigFloat(4, 22);   //  16777216  4 x 2^22  or  1 x 2^24
        IsTrue(output == expect, $"Step 71: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(511, 1); //     1022
        inputVal1 = new BigFloat(1023, 4); //    16368
        output = inputVal0 * inputVal1;
        expect = new BigFloat(522753, 5);   //  16728096  4 x 2^22  or  1 x 2^24
        IsTrue(output == expect, $"Step 72: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // Lets test the round up in equals. The expect (11111...111111) should shrink and round up at the same time so both should be 10000...
        inputVal0 = new BigFloat(0b10101010101010101010101010101, 0);
        inputVal1 = new BigFloat(0b11000000000000000000000000000011, 0);  // exact: 111111111111111111111111111111111111111111111111111111111111
        output = inputVal0 * inputVal1;
        //expect = new BigFloat(0b00100000000000000000000000000000, 31);   
        expect = new BigFloat(0b000111111111111111111111111111111111111111111111111111111111111, 0);
        IsTrue(output == expect, $"Step 72: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true); // aka. 511.9999<<1 or 1023.99999 
        inputVal1 = new BigFloat(512 * BigInteger.Parse("4294967295"), 1, true); // 1111111111.1111111111111111111111000000000 >> (32-1)    1048575.99999...
                                                                                 // HIDDEN:  #.############################### 

        output = inputVal0 * inputVal1;
        expect = new BigFloat(1024, 10);   //  4835703276206716885401600 1024>>10
        // 11111111111111111111.11111111111000000000000000000000000000000001000000000000000000   (4835703276206716885401600)
        // 11111111111111111111.111111111110000000000000000000################################
        //                   ##.##############################
        //100000000000000000000
        IsTrue(output == expect, $"Step 72: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(11);
        inputVal1 = new BigFloat(9, 8);
        output = inputVal0 * inputVal1;
        expect = new BigFloat(99, 8);
        IsTrue(output == expect, $"Step 80: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat(11, -121);
        inputVal1 = new BigFloat(-120, -22);
        output = inputVal0 * inputVal1;     // -708669603840 >> (140+32) = 0.0000....000001010010100000000000000000000000000000000
        expect = new BigFloat(-1320, -143); // 1320 >> 143               = 0.0000....0000010100101000
        //                   both should round to 10 (the input of size)   0.0000....0000010101     
        IsTrue(output == expect, $"Step 90a: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");
        IsTrue(output.Size == inputVal0.Size, $"Step 90b: Multiply ({inputVal0} * {inputVal1}) output size was {output.Size} bits but expected {inputVal1.Size} bits");

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
        expect = new BigFloat("-1154037866912041818479159667074393539946217472", 0);
        IsTrue(output == expect, $"Step 93: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // output   11001110111111101101000111101110100100010001000110110110110101011101110001110001100011
        // expect   11001110111111101101000111101110100100010001000110111000000000000000000000000000000000
        //                                                                ################################  hidden
        expect = new BigFloat("-14566005701624942", 96);
        IsTrue(output.Size == inputVal1.Size, $"Step 92a: Multiply ({inputVal0} * {inputVal1}) output size was {output.Size} bits but expected {inputVal1.Size} bits");
        IsTrue(output == expect, $"Step 92b: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // output     11001110111111101101000111101110100100010001000110110110110101011101110001110001100011    62560518121828658697411683
        // expected   110011101111111011010001111011101001000100000000000000000000000000000000                 889038433937 14566005701624942(_int = 3818390998646471524352)
        //                                                    ################################  hidden
        expect = new BigFloat("-889038433937", 110); //-1154037866912041818479159667074393539946217472
        IsTrue(output == expect, $"Step 94: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // output:    11001110111111101101000111101110100100010001000110110110110101011101110001110001100011    62560518121828658697411683
        // output:    110011101111111011010001111011101001000100010001101101101101010111011101                 3818390998646768719325 (this.DataBits >> (sizeDiff - expDifference))
        // expect:    110011101111111011010001111011101001001                                                  444519216969 << (32 + 1) = 3818390998650766491648
        // expect:    110011101111111011010001111011101001001000000000000000000000000000000000                 3818390998650766491648 (other)  or 1154037866913250071024881716200922954189504512
        //                                                    ################################  hidden
        //                                                   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%  rounding
        expect = new BigFloat("-444519216969", 111); //-1154037866913250071024881716200922954189504512
        IsTrue(output == expect, $"Step 95: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("123457855782.2754278637832");
        inputVal1 = new BigFloat("56784589567864578.05687450567100");
        output = inputVal0 * inputVal1;
        expect = new BigFloat("7010503669525126837652377239.56001231481228902391");
        IsTrue(output == expect, $"Step 96: Add({inputVal0} + {inputVal1}) was {output} but expected {expect}");

        inputVal0 = new BigFloat("8941981654981.981918284", 55);
        inputVal1 = new BigFloat("-1502437545285988701043238237856775089653447902277", -22);
        output = inputVal0 * inputVal1;
        IsTrue(output.Size == inputVal0.Size, $"Step 97e: Multiply ({inputVal0} * {inputVal1}) output size was {output.Size} bits but expected {inputVal1.Size} bits");
        // external calculation: 13434768967703471650801766289704559608123152231389557678010624.231532668
        //                       57701893345602090965856718393316345018971514774247574862961329414977941.015625728

        // a little too small
        expect = new BigFloat(BigInteger.Parse("-115403786691204181931719282793182013649615844287826014858001281754705681"), 0);
        // output     1000010111000100100000111100110000010001111010011101110101110111000001000011101011101011101000110001001001    42392656037190875842938869288009
        // expected   10000101110001001000001111001100000100011110100111011101011110010000010010100010000101001100101101011111000110111100100010110010111111011001101000101110111110101100100001111111100001100001111100010000000000100011010001001101011110001000100000000000000000000000000000000                   -_______ >> 32 = -115403786691204181931719282793182013649615844287826014858001281754705681
        //                                                                                      ################################  hidden
        IsFalse(output == expect, $"Step 97a: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // a little too small
        expect = new BigFloat("-678282496595054013627833570557952", 127); //-1343476896770347165080199207099879564484138783290.5031390682463057059450092223189508
        // manual calculation100001011100010010000011110011000001000111101001110111010111100100000100100000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000.00111011010001011011100110010101000001000000000000001100001101...  13434768967703471650801766289704559608123152231389557678010624.231532668
        // output(be4 Round) 1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111100111111010011101111111110101110100100011001011110111100110101001101010111110101111111101110000110011111110
        // output            1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101      42392656037190875851739737828733<<163
        // expected          10000101110001001000001111001100000100011110100111011101011110010000010010000000000000000000000000000000000000  678282496595054013627833570557952>>4
        // output(rounded)   10000101110001001000001111001100000100011110100111011101011110010000010010                                              9870309391336253809682 << 163  (includes hidden)
        // expected(rounded) 10000101110001001000001111001100000100011110100111011101011110010000010010
        //                                                                                            ################################  hidden
        IsTrue(output == expect, $"Step 97b: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

        // a little too small
        expect = new BigFloat("-115403786691204181933215860469808858237856417556527488670128956678713105", 0);
        // output           1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected         100001011100010010000011110011000001000111101001110111010111100100100100101000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000001000110100010011010111100010001  (this)(right)  9870309391336253809811  (_int: 115403786691204181933215860469808858237856417556527488670128956678713105
        // output(rounded)  10000101110001001000001111001100000100011110100111011101011110010000010010  (other)(left)  9870309391336253809682 << 163  (_int: 42392656037190875851739737828733)
        // expected(rounded)10000101110001001000001111001100000100011110100111011101011110010010010011  (this)(right)  9870309391336253809811  (_int: 115403786691204181933215860469808858237856417556527488670128956678713105
        //                                                                                      ^       ################################  hidden
        IsFalse(output == expect, $"Step 97c: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

        // a little too small
        expect = new BigFloat("-115403786691179073526274313746753515080163586890863079248351100540661521", 0);
        // output           1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected         100001011100010010000011110011000001000111001001110111010111100100000100101000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000001000110100010011010111100010001
        // output(rounded)  10000101110001001000001111001100000100011110100111011101011110010000010010    (other)(left)                       9870309391336253809682 << 163  (_int: 42392656037190875851739737828733)
        // expected(rounded)10000101110001001000001111001100000100011100100111011101011110010000010011        _int: 115403786691179073524777736070126670491923013622161605436223425616654097
        //                                                                                              ################################  hidden
        IsFalse(output == expect, $"Step 97d: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

        // a little too small
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809680"), 163);
        // output     1000010111000100100000111100110000010001111010011101110101110111000001000011101011101011101000110001001001    42392656037190875842938869288009
        // expected   1000010111000100100000111100110000010001111010011101110101111001000001000000000000000000000000000000000000
        //                                                                                      ################################  hidden
        IsFalse(output == expect, $"Step 97d: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

        // a little too big                          
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809681"), 163);
        // manual calc 100001011100010010000011110011000001000111101001110111010111100100000100100000100001010011001011010111110001101111001000101100101111110110011010001011101111101011001000011111111000011000011111000100000000.00111011010001011011100110010101000001000000000000001100001101...  13434768967703471650801766289704559608123152231389557678010624.231532668
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001000100000000000000000000000000000000    9870309391336253809681
        //                                                                                       ################################  hidden
        IsFalse(output == expect, $"Step 97e: Multiply ({inputVal0} * {inputVal1}) was {output} but expected {expect}");

        // just right  
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809682"), 163);
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001001000000000000000000000000000000000
        //                                                                                       ################################  hidden
        IsTrue(output == expect, $"Step 97f: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

        // a little too big
        expect = new BigFloat(BigInteger.Parse("-9870309391336253809683"), 163);
        // output  n   1000010111000100100000111100110000010001111010011101110101111001000001001000001000010100110010110101111101    42392656037190875851739737828733<<163
        // expected    1000010111000100100000111100110000010001111010011101110101111001000001001100000000000000000000000000000000
        //                                                                                       ################################  hidden
        IsFalse(output == expect, $"Step 97f: Multiply ({inputVal0} * {inputVal1}) was {output} but this should not be equal to {expect}");

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
                IsTrue(res == exp, $"LoopA {i}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
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
                IsTrue(res == exp, $"LoopA {i}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }

            for (double ii = 0.0001; ii < 1E154; ii *= growthSpeed_i)
            {
                BigFloat input0 = (BigFloat)ii;
                BigFloat res = input0 * input1;
                BigFloat exp = (BigFloat)(ii * j);
                IsTrue(res == exp, $"LoopB {ii}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }

            for (double ii = -0.0001; ii > -1E154; ii *= growthSpeed_i)
            {
                BigFloat input0 = (BigFloat)ii;
                BigFloat res = input0 * input1;
                BigFloat exp = (BigFloat)(ii * j);
                IsTrue(res == exp, $"LoopB {ii}-{j}: Multiply ({input0} * {input1}) was {res} but expected {exp}");
            }
        }
    }

    [TestMethod]
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

        decimal moreExact = decimal.Parse(inputVal0) / decimal.Parse(inputVal1);
        BigFloat output = inputVal0BF / inputVal1BF;

        IsTrue(output.ToString() == expectedAnswer, $"({inputVal0BF})[{inputVal0BF.Size}] / ({inputVal1BF})[{inputVal1BF.Size}]" +
            $"\r\n  was          {output.ToString() + " [" + output.Size + "]",20} " +
            $"\r\n  expected {expectedAnswer} " +
            $"\r\n  exact       {moreExact}");
    }

    [TestMethod]
    public void Verify_Math_Sqrt()
    {
        BigFloat inputVal, valExpect, valResult;
        inputVal = new BigFloat("2.00000000000", 0);
        BigFloat val = BigFloat.Sqrt(inputVal);
        string output, expect;

        inputVal = new BigFloat("49");
        output = BigFloat.Sqrt(inputVal).ToString();
        expect = "7.0";
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        output = val.ToString();
        //        2.00000000000                            // 10.000000000000000000000000000000000000
        expect = "1.41421356237";
        //        1.41421356237 is best, but 1.414213562373 are acceptable 
        //        1.4142135623730950488016887242097           1.0110101000001001111001100110011111110 0111011110011001001000010
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("200000000000");
        output = BigFloat.Sqrt(inputVal).ToString();
        //        447213.595499                    // okay  1101101001011101101.100110000111001010011111010110011100110011111010111011111111010111000110110000010001101000..
        expect = "447213.595500";                  // best  1101101001011101101.100110000111001010110000001000001100010010011011101001011110001101010011111101111100111011...
        //        447213.59549995793928183473374626   exact 1101101001011101101.100110000111001010101111011011000001111001011011111110110110010000111100001011000010100010...
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("0.0215841551");
        output = BigFloat.Sqrt(inputVal).ToString();
        //          215841551
        expect = "0.146915469";  //146915469(best) or 146915469[1-5](okay)
        //        0.14691546923316142068618979769788 //more precision on another system
        //       0.0215841551
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("0.000000001");
        output = BigFloat.Sqrt(inputVal).ToString();
        expect = "0.00003";
        //        0.000031622776601683793319988935444327 //more precision on another system
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("98765432109876543210987654321098765432109876543210987654321098765432109876543210");
        output = BigFloat.Sqrt(inputVal).ToString();
        //        9876543210987654321098765432109876543210 9876543210987654321098765432109876543210
        expect = "9938079900558082311789231964937550558064.6494438268544270221286846603357167897049";
        //        9938079900558082311789231964937550558064.64944382685442702212868466033571678970487057062388 //more precision on another system
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("0.98765432109876543210987654321098765432109876543210987654321098765432109876543210");
        output = BigFloat.Sqrt(inputVal).ToString();
        //        0.98765432109876543210987654321098765432109876543210987654321098765432109876543210
        expect = "0.99380799005580823117892319649375505580646494438268544270221286846603357167897049";
        //        0.993807990055808231178923196493755055806464944382685442702212868466033571678970487057062388 //more precision on another system
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");

        inputVal = new BigFloat("23466207109390852182562229134844879465209207461285119050842725537452100070948111321244695716285488004820807390076326731850692667100714992415364312032227360362070027890120698082826669803953958791443305257455513984934956578611336998676672804562842121688708383087759159988954760747537602550118269197135294250359262819649936574767063922.01945122219918131773");
        valResult = BigFloat.Sqrt(inputVal);
        output = valResult.ToString();
        expect = "4844193132957319671709340941797445984823847916300558971839295375794565129018082952319997856028164815858716997110257694105883395223009099123324505808832930067526997557.67942784382363710559954840909262062040844976811740121716293669295087565452542991248498736784872402435202193650709055844125266714299502864068900000000000000000000000000000000000000000000";
        // exact: 4844193132957319671709340941797445984823847916300558971839295375794565129018082952319997856028164815858716997110257694105883395223009099123324505808832930067526997557.67942784382363710559954840909262062040844976811740121716293669295087565452542991248498736784872402435202193650709055844125266714299502864068899999999999999999999999999999999999999999999949320041843783
        IsTrue(output == expect, $"Sqrt({inputVal}) was {output} but expected {expect}");
        valExpect = new BigFloat("4844193132957319671709340941797445984823847916300558971839295375794565129018082952319997856028164815858716997110257694105883395223009099123324505808832930067526997557.679427843823637105599548409092620620408449768117401217162936692950875654525429912484987367848724024352021936507090558441252667142995028640689");
        IsTrue(valResult == valExpect, $"Sqrt({inputVal}) was {output} but expected {expect}");
    }


    [TestMethod]
    public void Verify_NewtonPlusSqrt_Common_Fails()
    {
        BigInteger temp;

        temp = BigInteger.Zero;
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        // Covered under the Verify_NewtonPlusSqrt_Brute_Force.
        //temp = BigInteger.Parse("4");
        //temp = BigInteger.Parse("15");

        temp = BigInteger.Parse("4503599761588224");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("144838757784765629");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("4332296397072526994426");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("197120777410685286861723419348662720446983624468633941814867274161329731855");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp}");

        temp = BigInteger.Parse("568596673212406235539046204653574092779927528240990410308704492396460002250762210750361428976428226236231439839658527797601369407516351767038245884193317345297332639084030064798571562357325869523334241343457316070779167053230802243325508166810051970793029610805741205436326081505118688824250695502334609951573043497313485532249651668225178016840421469883558806604285808368323341479822807033581983205344039371458847573584264752900519079048915398981011181623230780288048890300634904517965949212204068737098107941104189698397409567698012505870983478073274795514339034169365773220230830720004807771185986855895045516625461415107861041712612909415282678933430993639015017973044506087648363018729847627791605903425315716867660373283634463025466898314552226041199227082594230096271404048950683874734785456093637644080259923926309749113053869920861252071824902816983919198943333352747567573205971722594392368757236981129345325357188283747755165211300361598909057331888013394644712040562578111312045020011010991816673666598318790155764029251076097495451756886293433937287357398346137053436703441717477280156524220517544784355270939618075800550360950261327034177704356076938995007627050124084857312674697721104308993667110188079684610462970532770811430407577836576516525347984663742735894854393178929653419127586692542189167843310656882269150136996004592038958466538491947533727965371911271931474363560773806290903082464818620463658436801310707825375068269725362589748265742091637498362621850566516960404061662197346676810108676564947944053523430298498377965136876382874888755098780874762932647407939626869067245075305802595524491063488862288004615122286564843300578758861996842723888825626998296253860396832728618268698513999105663450448455591453911622931238399649235353903164331408715648544484198462915062815114371906052899024506631206302172485246425734102246282006790515841269176368354750944457205740865919396101592164443131178363631180186481154637375276513183701970126748221898210963776797851581007115533834804644343822008468228860094391251339086238465678711921921284967851031385031907419916767858003766595842244869449534509496748434950293981618989570054257983701817992679002284807232891636649497294174834055117140618871480275671453660871756346174812413763546753450026532970380652053777318975717905464523846316531635812393163893169715944468523807676593337865609836993849998617745398537861679170415965793630853028066627328675239723557343598380919099547947117124600794798069455211709821342706038292764227379487605537218318247566029210526464224355667262491214506233857732726704711260425183953510241304266161977666456048204419815901425751876965458230836272990822362376876246567929545106942228163519038240884565231374383262437933921682301042259394323384010986971478111167473189572624136900260842462563632164438243406573308292515335842446029470620153455750269492648502995752698839327318101471308195682068310941060522835002396271185878639534383332814526736730627737675311440925963840167979468823970109167743381899347507787042067457538898702507081788658375961558876519917419284295948484882059236624597063874422807420548386494638142661819447373680561474726020600212494965888772592905717450857077427720367172542629011169439451164789672353614751341788973143362728269876015374623199859749885177199865505249137720400675981797711570054912715025084920361018230684483048532553044351189092760249689620742581712189291998116260173085751887604161409277198749738705418188423325214210791436123865095895924870449787198921748406270195538419589929583571270220608276936976047435099002481081568006269184926746075413902095833213112716082651523466102709809765278868521868323119552163151078268298543092909181764094354421443739939102266618363082870756251734285570963705511926675021687571952918187727056741057433095113623046731136780044772629021222742048526819043564191009305901298547848411807561630976907033779660614988066747079217915126796269532241434824559034294551034567089930561187776063042962098376001055021975735594097154610662494227608391831324251819834849972727806898988047156642000444156374099020509496337413749154812935620746943658043566102396889370920743841762392784731892754856936207021618371850662875222101060948251861698244863970080362619038241610264649305147250851826137983717857787292617945678440789654915586664827700527372511102728500979579398643014513213916697791050137689464063898748385088697075494961738641464073679536486848774189038880714063768855876078617505553790225908239315278348914126700695635803110855185780815614549031475503058796620205024302283909168380777135676289136283202774805599924261375761188156533898405212485642815583559044372013559333161812445651874731406291358975389500184031405897796170361475824855865416996816101180577179211466498528422630318977338028015876113173714172700603294777292007011893140268292139031620419978838233874410683183389592330610012298019178543866221491941389015906603598594523857857828887538768270002115444064670246189471105302854904357142640940815690815734212236064510656653497518955499351963564298916187123224379625736828158748021096975654285608875084594416617349440558333862887122384581582621428905712789260430593638211814462714113337065254852187384002227801421778675360047475402608367224990354216772160982391869523318958043157276234232215594952081263397029345447393215331410760682924198274455705944715191010466822858771476567511651447291833709417664994052378535962785039458646535298796251823002497493739453107285588189345055064989468830654731943635755096663118764896699043462398498063855211918516506112449276535417359689692816934359580186655249122981543576662853443920537700756370071176061687748929211550413374463270060822541705443889568085981718411771212910543081641827053808927056261093615521865229815551648410013951889654447956505928765294673621144189425512678052272847811478536361635041037247376079729955183288034878518775882046345262042085840029108259221965326603011451402215189442292326949370373918443853583166196123334746715570385322755860325668018143340905234499599661770553181685425109623262543554842563091462152389684151335425459512757268182388248548523806002139043631636292746232049543450881373125665610482569105547103522944485323651945010132405329295466906186502963615807071377352577596402482171032894689185345703931237197465495582572835620049008345615416894296528929647879711273709072418138224125107429443618724331758238114288955335876006203475145403711957920226577816782199167434890841283968630429187388429297294594396082808367568391279858533434198593052588130438526062765357196625443946092250933148070571533443620438382567098056574401350203053743912618224443564964963443981705183424089707049132420709512811684110954808225750117291699279190814324766521785636930646857218211339050239728030407504252402281110555437220027611206852489981972335663826111509206274876677013594436818577024753697807160957110918683574700041515759818141929214696536737555054156463211781351270996437552588723233229968280241742959860248850145435451592696399504097017400658966435059320799049397167451625262640117821825408096184107986323793568374539075737342874384546075573443054746055797583589009325216800125861872312825727671724348718906475191691504698280148131491140967107650139462912226865980464987962788562821048754369500679830978859725690019966684659376992815514045818430491026939697467871051042324292828993612736362860526073881154128878164128599611551530542310658302503782893657116944936194648228308865028057607312143533193693276156368189379813543149482685350792790900828570877584724011390068030679428412329640096515459755446277254206795751336660534427014290278841915158071763997616948849295574651034991146490119782815733533690996854686822010136733628846720978622539379847979030187244911605202840143861781982888178795784849924776497393293251973778927673416094397274685411386366555150802162781628659406215666823246623165293809466708313281347029191907077218303008657606493723248678087195932972884404760267321772756889788034941676516109268379862672702988397543495729736053680904777823723687628080981561711050363322368414855010665896948734225989419949098919172314701355903983867077527964924292955188146625777356737268809184825688808727316372675679905612856708689910533140809777396520507481327364392903800382176843167097065576641359796355206255921794399479538490606924139256862692724352340257725289913772940861331250854604953474112127163651856327913992832600123879966765945027720897535126324356794186885712704393607569538708105218104098849209423758863143894085086521731190291213196696379305734158272203101824107379076013035076712234971563844659606120406749074695967924573761014310011944566766342932466508424800571668641713486290921461220902791729023092771013548720715052229192246483610110598593104826970969087806572025329199569944106409193771552798814229945092476861583524722722759112066195996832270583763580479784998191784157314172850935658197344181379452332323193615353475333080892522126019108669481315395243573395324665363237446275972016556276331960182906303806899636009927546780543980346488212445140028771356388091942552442803773188008085351584825331255325030728733841409046107296730339245186862892760570481181616153002140782747878123422642967197887773195118140973221908721712441299887200866209083986770666824257088245624210982913142488708791987999144692110825246610047377251697461317512198538491122770376600660555409811367120173388378864060067155455090713541086467734664217720993609926519612776700477174101888188993458884880678252772213470425870780423917552701421891594456827968463820740935159076965202859200886098119890641044112881824831639132512603547695921402281940932405417994997938702814783404646751173368769430287371382741376186138486209145164636826028569305579088510904269435098378605169160732461767439018571424555136824347744527082607528180785762004430782108476943616681422686369572684280347505608117430283967292298696789525942300497917954715161971484652463862582725536721104119418568020996161818601764632840636654512368265608739854877931293276921822972877637476652414189723163275512609800132204020956071479807078747845492246682750495407588012902148137602445392176004165882375624372971093548185310117781416021878766399749320798339753778763002071834838306505750050805540210201136326020621556855005961134616936531575783426741040255892005909751600522657015249622142126076979990690329503176083139283356214159398656550117855920963884388077339193608835643128361137119861234131388516866300141373858812640966673370208976799490665174350806186760932933142007086682804757500330217466112164002695550502935300774079412928586340785624656954700151339024544642095334737169097624334788472340640760991267874048831593849530914907470681401965682331359610472406029801405712218159877957333208758173834527023282453958732806282317437672436010620079619244570903695496127956956370537136100022623247986844149235756854737785008277115265923928165200225322336346459556753143202165172227589300527336518245297791085001356912548092099238245360700904251961806593017791798456151653909623459064981589910213870726571579248087879927563066384322856888956287583958965883356240509710864472300769026240045989417656762302396004755336300292521981524065962669490439929857862990319598994639527337122261314730682010056463040758759229889469623898712489719894803269114736500294656194415411869308406146183676054911313131576490647259320992587418054623431837487101604749635114422516499029219293563309281873120759088798895107585724608028263483751331564960049361692704843923785240777313723141453380005341554361639106359775060438287533523856932311422822902626821353904546321484240526585044350171088733439015781558642861065686090670546148571755491286609432057060043971666742286929123667582156169592438262264874070917970547323393649441643765658590865329264618839034301964877180176616403723280116891858186445030814169336508840411566050522180360021655721892781045068966149022578480880921209467602646549684170791500075860710762219336741044349075645200291919578682567332551703896463617259078795321898551974712455647120157245246302781579972253305371481572681034639914639300323011324097949748477310566362105763912344548012887103371826153953590397376115194569981433472808913562082278719594578119855935945569601702547936472782015206840585788231913151015093662983988417437045652418696833683466810782825130439414165774425720297653926967414456360568582558597443506721014596982536734082869106415898445244563135907319363754872172732968447400156320191672297712593724792947325051014248915874773651986723659919087113447245598165982260966270996855626015566461178715461446100650588392938743410774620631872835529431975415586482202897851113697427399684025977623533827867513660088600035366734999866788911160860417088431720747173113244497590518629084473281861361283399287991613303988316391104279108152188690308248779942580210521290025393058453299589362878325391924052007947284168720206307284670229558925312818188165898373997681761608045055177236603993020863977914314891842571099458801905126172967955860909914918248496128402431735678905186029032850506351096008393724932390617611975528997095896345764537644314187545496811248642037001876864656020866004278021777761041348263458447185585845471066467853418222600902676527198074498128418084033054178895168187506127544181648175505145324458999126697899163798209934081");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("3069442491521288882209912198117542685828244445905690595315578257576026925040003666991889175682390308936323311867038463231490746054685241129120234622000159636586371418712969328339618031154071244120791357576614510041502582933263867565326846833636258354336203659164264481477059261848349918717073176414771008286700543434325474444449281438222251793946891850998116797072652530702407793152000333188429567050812235397506192494548530491291118797409487643364978182787322328448530443362866083337031188757267098345726710434955949268377737390836920451119678791106540362808087964617692057966401075660464587924580608108398968250310775222716116115842124693003674389514501976582298711620937002974383619569023753418480367573767467109933985464413077005010973797700627360476030427905268125619163045870901316889850082561993497291168719068022883738752815867042916842718745541070994482039679939849686799177261959589642760853561386332687951825763396656135637366729975121257639567151942569081995138008253909141260694278219504247960643385039649932729611760156050269813026751925974222070054700377476339563378936752904193074400715127888611166019389931458008378123152360189049273713791950489010801943196670458698643779919030483832749037450457276224893004054165699247691719595414641725657814253155441724928627136906173465867833310562416506605962220212469680743631195003521103634050821337603560290588301701596040865889503083855843394986557884944945556100592904055449870116267151820289881744593737669145921716277253162110565422264986362809375302374514493691147107428092182615316212264678504594536231491812159439899029724642495322419784462312727216082735303318714472464191610452544028257508441799798931762864864715988018364678947840698415064282872338622653781614921843768477383401228088830160953321885774522233387402007516375459644536393505823116638100179333621321400095017611673388059841225689604612490060851053572095213627696433778410111379957067154748095781374936736446450881740690435307309763773395445323295974928890549503947240983765416438316292872409377918246887291176323662563458868858131452555217996657788702611398299641850553371541552427242564221750854980931902962012028686635547627516089966407286156561459813332159539037150419561327879678933483017892715686205036514841614923381597298214857688978080050309895836002372073116267988189697095225265014606447202687121366873422812010046203954155446406512176497398958902199187683934954505035177942270807666234260421784563099426565436213900626105750326879387331585783621735516159883149795298060197599003338404202019864250034904388846601666103456534799784513665424523093761686490311179806863884227268082845554028485641071881186667339630420990536348033811374573518685174498776199153952807285764157483964672560582276713403379743570822639933025397481655098408886282660023171993551106296385527170701183133777672093213188738741694934044983293582924711799227280747933936423952460643954522511189432838833992318133436093402215845142128199633596734427838667731079213697705985167700880000426168834509392875445367485856653591422112711060014993723631726287025250240528933012106273768378351138500715338161989228016080116459136734216724127503205822742783332823682994022186248905883173293824075574351709860643483188378501317242065384232733643703800477007671245506506448819409132040977872161817999289756235480194523522496361207573434686161257665534400673589353120197460876437504167055219100215867602411096460412229429079285626829687313455227071378556727057753271604205993856464497871129467229025429547074215802004078323994886093651496186600662753953467748733605822112764756889732419163666430200039786738275552439772076955582989558501826698699205244307930848801928945717786923638908737879755698526580886002753749119517824311199565239535459743495944328212082804476388179843223579058132576774434884138498280414395932009691578799908458223727862462162517348267792744797120556934452160358567208951294773492750488980597538249497239502805023275074583865176885646664231279436272375097671515831792480205586694323948514674819096158241914861084086730146230038029178925903687272115333804362120993281043507953301957264579715851946913744157249804950757134088225209081331103699928306372835734613537073408836283565648493538290450219281361986271155570178995901695603037671788303630750211538625719029474791732114270982436148244502475225987625453208430820423770243728495708993385732000987834633973038968332320682403779372533185282387350736003580916668556752016226984103964817708169764367301733647432713197376702796924072209926215015200903604895412233023052040791250206901139981495026716929835988480330322955182770954165349145460676151947470528933066854293732077126911806037025514262502175217341119266516935266970430435686586687221511704044830195876803951216886191712932361996208153966591637741058854115931984726926395471877234990804321123897818679943442005331229659361006123821003682184718600964631416525432858662586103439119832901378590593488273914589529522529168098354134329484105776576392869271043003588742911347032098816967613528209855168254288156486216138860890485199621231868083353369428916387229362205336191575119399643960239395501449162348659292602006106445609881542323651673071183576950962378623267489699018803623453089373045430854799441436465182350062274762844819766839146628622899174925631396268622996552154198489741293389968601416787938546938413000179187947660271807946308136433080654777244653137705506524778275973917002809188019744865561074857914262025366775107914959037947935316055300044685467269655480316211607691656997277221963296026571589420385702910093313956234014290580287470710740800775121991739227925815442621575357604714760366161961606852305033257363574874222642286664956773235526288930766467143544775887655158853924584028117436526969518661333800848727404137269125708724992245320013527102903032439454937686146731236236644251899955560076677364560719237051001636472139620263613990376764707323762058262951223961863679442545168037107687223160686841088157051652125145116511027972190682608974557596973280729330951543221824468877897232543355065149890320924240623526500984311167436811828770741517261326174152190194196160834894411398301516653057903996098096777350577210537813268640448546835519494688128611437628716320798087066624978464673815910444900519121937861886812690948622113194530651737717787102024458087792007794523529513738876821246810329817159823426901579562127006149066292928826792007136959549093470296877960511054614153437428961563399076934000170041285968076539471692045791436764662441026128845038670405435646801492180091809627139564644805409289569951372982624195987330279311945725064850280462255023304846430029411342844166104267344590648173000271835390536016874950730343758331221972309317221251083873750004791284489673470854881554429194332430130208106028264843456123903540591507890627066290509050419481208501414288030965549577962623556319068798301492261251326113621001381146549654755095578415942651955697937940689009059973557256823887040416614910041698289281109611472423623331742817915753811190461438227782075653120276478594477526225339946255836104882044580743650137870239234187836464406352370893081394437977780821196100379020558313651776799202087439820892790623511128549831346687683224237311167958904514270092980552226154995550845480935380293112607479761340761547283114189620347225879629121828399336942595360072519727479517809110113801510868580800944756922402988679535268864215546651144216923869194161620309679353954274724119563407615509947186724322271491339801422012052910420364096711158184569256957243892984936667615461215962807480664364324872427309070603063714655408970108816175305871355129023247072211288258896111680582765033139170345652688583109693191303861932309889301880553965895178485256684858090531178681802660545578070532890263020044650399744838839835926984266373841008645128966784789801878328782877224312920128429094887468565913728766636896482868230658705412665604959760522984666270503028367851555525820263015204327140619576239426253160534212233365371114016087577998805966396947967209534939295499686464591363743189264038467458309394783583068318447551901762866282068169491414836212699794010057728196877366801100127798605063880906462985085114675537");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("65785897164448191381343511924499180834109624990100754110346692714720833794182365156704520567066494452568598038317099669513516096681618601973599684423150328823149127367318079223120613816366038825604373268484954782110429835105286425333570541703714024151532084137071597682869259489221172755742364472724201391713876228852531646764843076854104581946061772267221757994736893926938160629380056553372248214368909504039212458266423980657106363733011856061121104369672147994640441778258160341955435109744447134256187215376894548013860157067815014854327054401293768815922507649024668392519266407522576716874831315048731693618952003216023606499644720147295275387516119527423680540664128864272031313852922193652901872732314717832642396584320613044874876038709312185088372090147112187657869779392688566837197222873583961493636492878943080433802748761360310302723190158716092892744929758873685478975276800670405063542783787175166169602615999466339596354504219501105673891354573209038920435930403019087822420187428656762642041998808872161199084947727133936559102364496735902940200177931856797143955996184320324843305825519761471929663872341396515529983727108459536090173169742307430044945248663781699303787795654384125310492475708658293822181665360");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("62362385618440558416947016669824906883813410011053641792946485497339721329431721439415220243379222918320766404281447700590021891155050265114385592812878735466325208888593348836073537528761371853341648068552950252434358013341985391327537178373087155201768599524019380757016544450988505261122166827623821157388989385416619683557220268832127834874073559435984144961254858185346608192576658054546623242322471196316756094923811160906264371131392381779320586622639793335662752727861825331393639498034265911346139282451973559648859868126280816483538209855193358838624452241451974573654740521140261037836393493195199235018766811286482795967197478000411576883345741508867320043804212855588444336753360390022381829058493004310313059457501659510131594527812649374332829267428993258110432996432011983126655315265920566458028478121477367097244510948421854062842203915600005914824");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt failed for the input: {temp}");

        temp = BigInteger.Parse("105304873411458465237823109840830461532297664641861909345335983651620697961501875992724194244954865033669992328452594774452926688664213255617325573108242413961035542821302346985592911344125656037430417857923745187297258902871436992889265964566408383242965076143762182878415696697321419732252675577742829165177250146537026239309456512322120322660658657500735460842080634302497836781370951570897343811129414613636887318514067098255516234127025764803768697992519955839169838829708639416834170285096476387342434255498654317390346575985930158243128857324460324557274293835174709332634562827897313");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp} - failed with round up");

        temp = BigInteger.Parse("4158778369644239253144735435452135103052920040777068394326845749479928668137113273641891938414472777155402871106641843416775631184591761508954289137159646589762108986911327059013268970766061512895207495793891523453588504760273949467854779946180645436561957963870029200019077271901983439750690458982602101659773464708927882431821592294995304801341476291809490825735434728904816304538249047761794496268396415473815044527283546569535324502176951155143748970420841309367267113066246134610984581041136088216956464090974746565616023221");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"Failed for huge sqrt({temp}) - failed with round up");

        temp = BigInteger.Parse("179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp} - Failed for value Double.Max");

        temp = BigInteger.Parse("179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858369");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp} - Failed for value Double.Max + 1");

        temp = BigInteger.Parse("179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986050859197892009348423098673397576100060199749986929562362761314132383880020795324759350467498370284959417336104341480439649700322878491181830478233599");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp} - Failed for value Double.Max + some");

        temp = BigInteger.Parse("179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986050859197892009348423098673397576100060199749986929562362761314132383880020795324759350467498370284959417336104341480439649700322878491181830478233600");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp} - Failed for value Double.Max + some");

        temp = BigInteger.Parse("17494584706016591027735461995965655369485392135483279753178087315506247479908101322432716538350151127201566210005644237814513392249452453615066147272228907663966390734640115862609428708808030883561312370933224354989584163634780158683901786449438459917087336832199985240528014645163631305415749573655211490978631716429164715576326122339425754435169992953750485069221610238394718337618921655783782041008005224393274487002390986157125495569904504979630450742020277163243700439394100971116982469820853805921150898151992772979321237326399758133");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp}");

        temp = BigInteger.Parse("324869344822123891204500737190540217603582230298827943613070138634574543931529836644908557280814426421865640009546187334173413368040022188428404427615158419933534601057247685980219135338184905291081445059428169291870657858169275815840222956732487620761154654410650902413711236901782615917737119907905229234946211961080658388960638760959363844640743773892304002116832698921887645232477218304189719735593244966041503279433593532306881416962517923413587821230750081023226603959650598328121575017362314407084534778367861380310792005727284136781374900887396549343");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp}");

        temp = BigInteger.Parse("628585829943043711774780150124486302296386743285745807915546421548369772944349205519895063878854598279327388017081748928549003526040710666991887335679085821326590372431911569248580138566784523769649934299594659147063327786855481652768517809939447427274571843102808731405570448808757614071");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp}");

        temp = BigInteger.Parse("469219801800293764373197355969328553831984974596843971042368711922664472663701981746713137411270711303034626199044091413698918166643890203860091306664994072502482932661931411083539271868071588269998735494868914134645646190292788569954038367952474854129663");
        IsTrue(IsSqrt(temp, BigIntegerTools.NewtonPlusSqrt(temp)), $"NewtonPlusSqrt Failed for the input: {temp}");
    }

    /// <summary>
    /// Sqrt - Verification 2: Brute Force testing (starting at 0)
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_Brute_Force()
    {
        Parallel.For(0, sqrtBruteForceStoppedAt, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (x, s) =>
        {
            BigInteger root = BigIntegerTools.NewtonPlusSqrt(x);
            BigInteger lowerBound = root * root;
            BigInteger upperBound = lowerBound + (2 * root) + 1;
            IsFalse(x < lowerBound || x >= upperBound, $"In: {Math.Sqrt(x)} !!!!! {(lowerBound > x ? "Lo" : "Hi")}  In:{root}^2={x}  xShouldBe: {Math.Sqrt(x)}");
        });
    }

    /// <summary>
    /// Sqrt - Verification 3: 2^n + [-5 to +5] Testing
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_2_Pow_n_Testing()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int s = 0; s < 32; s++)
        {
            Parallel.For((s * 512) + 8, (s * 512) + 512 + 8, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (x, s) =>
            {
                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Stop();
                }

                for (long i = -5; i < 6; i++)
                {
                    BigInteger testVal = BigInteger.Pow(2, x) + i;

                    BigInteger root = BigIntegerTools.NewtonPlusSqrt(testVal);

                    BigInteger lowerBound = root * root;
                    BigInteger upperBound = lowerBound + (2 * root) + 1;

                    IsFalse(testVal < lowerBound || testVal >= upperBound, $"testVal: 2^{x} + {i} failed.");
                }
            });
        }
    }

    /// <summary>
    /// Sqrt - Verification 4: 11111[n]00000[n] Testing
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_11110000()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int startAt = BitOperations.Log2(sqrtBruteForceStoppedAt) - 1;

        Parallel.For(startAt, 1000, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (length, s) =>
        {
            if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
            {
                s.Stop();
            }

            for (int i = 1; i <= length; i++)
            {
                BigInteger v = ((BigInteger.One << i) - 1) << (length - i);
                BigInteger root = BigIntegerTools.NewtonPlusSqrt(v);

                BigInteger lowerBound = root * root;
                BigInteger upperBound = lowerBound + (2 * root) + 1;

                IsFalse(v < lowerBound || v >= upperBound, $"failed: {i} 0's  {length - i} 1's");
            }
        });
    }

    /// <summary>
    /// Sqrt - Verification 5: 1010101010101... Testing 
    /// example: 1, 10, 101, 1010, 10101....
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_10101010()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int startAt = BitOperations.Log2(sqrtBruteForceStoppedAt) - 1;

        Parallel.For(startAt, 10000, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (length, s) =>
        {
            if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
            {
                s.Stop();
            }

            BigInteger v = 1;
            for (int i = 2; i < length; i += 2)
            {
                v = (v << 2) + 1;
            }
            if ((length & 1) == 0)
            {
                v <<= 1;
            }

            BigInteger root = BigIntegerTools.NewtonPlusSqrt(v);

            BigInteger lowerBound = root * root;
            BigInteger upperBound = lowerBound + (2 * root) + 1;

            IsFalse(v < lowerBound || v >= upperBound, $"Failed on a '10101010101..' test with length {length}");
        });
    }

    /// <summary>
    /// Sqrt - Verification 6: n^2 -[0,1] Testing
    /// note: n^2 some overlap here with the "n^[2,3,5,6,7] + [-2,-1,0,1,2] Testing"
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_Pow2()
    {
        Stopwatch sw = Stopwatch.StartNew();
        BigInteger c = (BigInteger)Math.Sqrt(sqrtBruteForceStoppedAt);

        while (sw.ElapsedMilliseconds < TestTargetInMillseconds)
        {
            Parallel.For(0, 1024, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (x, s) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    BigInteger valToTest = (2 * (c + x)) - i;
                    BigInteger root = BigIntegerTools.NewtonPlusSqrt(valToTest);

                    BigInteger lowerBound = root * root;
                    BigInteger upperBound = lowerBound + (2 * root) + 1;

                    IsFalse(valToTest < lowerBound || valToTest >= upperBound, $"Failed on {c + x}^2 - {i}");
                }

                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Stop();
                }

            });

            c += 1024;
        }
    }

    /// <summary>
    /// Sqrt - Verification 7: Random number testing...
    /// </summary>
    [TestMethod]
    public void Verify_NewtonPlusSqrt_RandomNumberTesting()
    {
        int randomMinBitSize = -1;
        int randomMaxBitSize = 5000;

        Stopwatch sw = Stopwatch.StartNew();

        Parallel.For(0, MaxDegreeOfParallelism, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (p, s) =>
        {
            Random r = new(p + RAND_SEED);
            int counter = 0;
            while (true)
            {
                //int bitLenRangeBeg = (int)Math.Log2(4e34) + 10;//BitOperations.Log2((ulong)BruteForceStoppedAt)-1; //(int)Math.Log2(4e254) -3;
                //int bitLenRangeEnd = (int)Math.Log2(4e34) + 12; //1e308

                int bitLenBeg = (randomMinBitSize >= 0) ? randomMinBitSize : (BitOperations.Log2(sqrtBruteForceStoppedAt) - 1); //(int)Math.Log2(4e254) -3;
                int bitLenEnd = randomMaxBitSize;

                int bitLen = r.Next(bitLenBeg, bitLenEnd) + 1;
                int byteCt = (bitLen + 7) / 8;
                byte[] bytes = new byte[byteCt];
                r.NextBytes(bytes);
                bytes[byteCt - 1] |= 0x80;
                bytes[byteCt - 1] >>= 7 - ((bitLen - 1) % 8);
                BigInteger x = new(bytes, true, false);
                //x += counter + p; // a little extra randomness; can cause bitLen to go over.

                if (x < sqrtBruteForceStoppedAt)
                {
                    continue;
                }

                BigInteger a01 = BigIntegerTools.NewtonPlusSqrt(x);

                BigInteger lowerBound = a01 * a01;
                BigInteger upperBound = lowerBound + (2 * a01) + 1;

                int offby = 0;
                bool failed = lowerBound > x || upperBound <= x;
                if (failed)
                {
                    for (int i = -32; i < 32; i++)
                    {
                        if (x >= ((a01 + i) * (a01 + i))) //is high
                        {
                            offby = i;
                        }
                    }
                }

                IsFalse(failed, $"Failed on random number check with {x}. It is off by {offby}.");

                if (counter++ % 0x1000000 == 0)
                {
                    Debug.WriteLine($"Status {string.Format("{0:T}", DateTime.Now)}: thread:{p}\tCount:{counter}\t2^{x.GetBitLength() - 1}/{(double)x}");
                }

                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Break();
                    break;
                }
            }
        });
    }

#if !DEBUG
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Verify_NewtonPlusSqrt_ShouldFail1()
    {
        _ = BigIntegerTools.NewtonPlusSqrt(-1);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Verify_NewtonPlusSqrt_ShouldFail2()
    {
        BigInteger input = (BigInteger)double.MinValue + (BigInteger)double.MinValue;
        _ = BigIntegerTools.NewtonPlusSqrt(input);
    }
#endif

    private static bool IsSqrt(BigInteger n, BigInteger root)
    {
        //source: https://github.com/pilotMike/Euler-Challenges-v2/blob/962f981c87e394773507bc00a708fdae202aa61c/EulerTools/Extensions/MyExtensions.cs  Michael DiLeo 2015
        BigInteger lowerBound = root * root;
        BigInteger upperBound = lowerBound + root + root + 1;
        return n >= lowerBound && n < upperBound;
    }

    /// <summary>
    /// Inverse Verification 1: Common Fails
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_Common_Fails()
    {
        CheckInverse(BigInteger.MinusOne);
        CheckInverse(BigInteger.One);
        CheckInverse(BigInteger.Parse("645939712405401427719711254063813468156213415200850699928240890973548642930906671628067939561389768742610260274947216253535958539046510117181847336215500441547828988008499863283321060888285180958716586132295468008708718750886004174750090339672254391327068510418655854996295608654912181466562066392576946408280444645720652219224611465788921115672007871684057345123697713619795572111218976868165977088388076018155579127963692414645217516226582443109944361333778959142017020766702585614084557002374143856684835403030114576877613125361001081612661505866535992828793261026218710006019409384286248553489144084836908483354058059174031935080941095970669550752672290188012935344941625941224776598832005105461709062131845129678963155310027422917876006618051141488519807701539489712459454171376535906228081656842506129847531133807702931600418505954342868857145344054108555281592568541709103974802015219413388401920300146704419785634503745727784608594651862819775429285667691480255596598355137918884688681242925903869182365424076667891828794970711995156501553646245103285321272836088502175303126383961643724861657121768832605051542254287022038928115325910288733210686961346257986675795521419117484112569337949140264990"));
        CheckInverse(BigInteger.Parse("14226342718751118987907712656792939014609116305038288508984965665170435278580333690269830569305522770533115648153109977677970505910625922454536026835852861332513459837041790011369159613511987064990374692209153941946410250817507783093926309366818470453091487613497831683214972282433680630982532763190683188379014716931664367628856712864496549434590849436602433350062473844636337430852410107263030344416723319870714159741005261604289621571405370996571482202189946969590725396299242462841573118206347715965805077151815088089898712040656448748652946583617809438541519496216169721658653723720561561601101228727064752442615455482001090437223561687360017043925134663500743462770265751193548606236574887993897708234798015778243132964667428178856994844952952767372869586443371631986024216806390351458153350155473742698605698036672929293892925483318864445513221152207924537215494391086982099603955047363639252292415150243702042020253673557543258256080860766767245166366206504042341743664890169812601231127032334294569486704846172949043552949441316944714028840689338784401867179801203613271182276331013888454830352391740343025859921596673531204457761831252242526173047406966821405710834640264664514325319700094788810229600338339048034248472906440829567003568352904106454211600272772441256937357011526350840028766974271849878883968029793821029979280359368212174059092944193932724591216533306907868949240183508100414983415165523499662280683154859830743035090387199495274469840495806979622786301340"));
        CheckInverse(BigInteger.Parse("41597037944448288110263031477097654167384303281785501659348547438859712932895265129174863537442219208764808367754224325676189013224863230815238211318298805347258792422345044074873622426170281290814220505198280942180680987026072516738908213582080426073114716009829990895938425085364287895467011390686208508278438730461229031670604488583132047013295327251056792004212211984418619663447584994978851293935131110818345158735090825798050014339956039806087253671405307536505969146032086905214682999371273072327848485190337222112654049779316736398006552350156349884603007360240860067257909482396786549588732357562118172137668037090856181026987902018187123355906466294875863290720195176549809520330510535389796132984892059640168902171480943753536264315689507100966013546691280187870571451205108217441077590372742568828587495458227141473387320433587383078311146940539855321485599293540008356160519179316373740003907476816065356834101152604366223141056328388148703861540389801990631343518586799040845656241392934834522495698513979794700246917693292354190436213407849292690244331619453139129528311878463071451903744767274814238809831756377838973380878611727693240233305958509793663676407892016477218846901984357526107274350245333587892891647258020839563922086967136639138149549163501535002313407476514168371528265958304129029559790809555296315581870799758675251593555164438335668433357530966606494565841539517226231078555238897938082287091553434761476447011358899223501505465429569334182561228540892303214811082283526723697168379678479540727712438070861914927293109710911106707261295892041586638725763206110753540190896772169081260784380"));
        CheckInverse(BigInteger.Parse("20671763530409241324943099770758453374656073519208971837161323019736968598213040550063501802843843456188225260341175120190769282417510043673943805824640880119134633219211469954969974507848904555803334435197882765457136716437770296957048078656754075697050046118996825841746205252785206097369685430981035558594791602963045501531421510566506194772075275382440945200939540465695659828408838690926414042171936271541955906046169138710632745679881280602279076605388037666825339189737803887302307968158073837560120798580516365956420797937652142269159027921716745566978929345859123514450786114694292421480838182220087427088137015527356463359164029442792965441472949808342336219707591834015713549019248836633995886752934832102060114592550617810155254165659790003713516074868150437007539368900584118148822975181129863262810822587766092176976232054649447450476816188080046345755470758840829614272451921461411169209034550634190302697887369075179799790792706543812224247601683087038546841649124042977325954599894194019670455284451378580405359721112229130305343154584037097021290350012473718157459368363791"));
    }

    /// <summary>
    /// Inverse Verification 2: Brute Force testing (starting at 0)
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_Brute_Force()
    {
        Parallel.For(2, inverseBruteForceStoppedAt, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (valToTest, s) =>
        {
            CheckInverse(valToTest);
        });
    }

    /// <summary>
    /// Inverse Verification 3: 2^n + [-5 to +5] Testing
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_2_Pow_n_Testing()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int s = 0; s < 32; s++)
        {
            Parallel.For((s * 512) + 8, (s * 512) + 512 + 8, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (x, s) =>
            {
                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Stop();
                }

                for (long i = -5; i < 6; i++)
                {
                    BigInteger valToTest = BigInteger.Pow(2, x) + i;
                    CheckInverse(valToTest);
                }
            });
        }
    }

    /// <summary>
    /// Inverse Verification 4: 11111[n]00000[n] Testing
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_11110000()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int startAt = BitOperations.Log2(inverseBruteForceStoppedAt) - 1;

        Parallel.For(startAt, 1000, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (length, s) =>
        {
            if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
            {
                s.Stop();
            }

            for (int i = 1; i <= length; i++)
            {
                BigInteger valToTest = ((BigInteger.One << i) - 1) << (length - i);
                CheckInverse(valToTest);
            }
        });
    }

    /// <summary>
    /// Inverse Verification 5: 1010101010101... Testing 
    /// example: 1, 10, 101, 1010, 10101....
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_10101010()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int startAt = BitOperations.Log2(inverseBruteForceStoppedAt) - 1;

        Parallel.For(startAt, 10000, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (length, s) =>
        {
            if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
            {
                s.Stop();
            }

            BigInteger valToTest = 1;
            for (int i = 2; i < length; i += 2)
            {
                valToTest = (valToTest << 2) + 1;
            }
            if ((length & 1) == 0)
            {
                valToTest <<= 1;
            }
            CheckInverse(valToTest);

        });
    }

    /// <summary>
    // Inverse Verification 6: n^2 -[0,1] Testing
    //note: n^2 some overlap here with the "n^[2,3,5,6,7] + [-2,-1,0,1,2] Testing"
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_Pow2()
    {
        Stopwatch sw = Stopwatch.StartNew();
        BigInteger c = (BigInteger)Math.Sqrt(inverseBruteForceStoppedAt);

        while (sw.ElapsedMilliseconds < TestTargetInMillseconds)
        {
            Parallel.For(0, 1024, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (x, s) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    BigInteger valToTest = (2 * (c + x)) - i;
                    CheckInverse(valToTest);
                }

                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Stop();
                }
            });

            c += 1024;
        }
    }

    /// <summary>
    // Inverse Verification 7: Random number testing...
    /// </summary>
    [TestMethod]
    public void Verify_Inverse_RandomNumberTesting()
    {
        int randomMinBitSize = -1;
        int randomMaxBitSize = 5000;

        Stopwatch sw = Stopwatch.StartNew();

        Parallel.For(0, MaxDegreeOfParallelism, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, (p, s) =>
        {
            Random r = new(p + RAND_SEED);
            int counter = 0;
            while (true)
            {
                //int bitLenRangeBeg = (int)Math.Log2(4e34) + 10;//BitOperations.Log2((ulong)BruteForceStoppedAt)-1; //(int)Math.Log2(4e254) -3;
                //int bitLenRangeEnd = (int)Math.Log2(4e34) + 12; //1e308

                int bitLenBeg = (randomMinBitSize >= 0) ? randomMinBitSize : (BitOperations.Log2(inverseBruteForceStoppedAt) - 1); //(int)Math.Log2(4e254) -3;
                int bitLenEnd = randomMaxBitSize;

                int bitLen = r.Next(bitLenBeg, bitLenEnd) + 1;
                int byteCt = (bitLen + 7) / 8;
                byte[] bytes = new byte[byteCt];
                r.NextBytes(bytes);
                bytes[byteCt - 1] |= 0x80;
                bytes[byteCt - 1] >>= 7 - ((bitLen - 1) % 8);
                BigInteger valToTest = new(bytes, true, false);

                CheckInverse(valToTest);

                if (counter++ % 0x1000000 == 0)
                {
                    Debug.WriteLine($"Status {string.Format("{0:T}", DateTime.Now)}: thread:{p}\tCount:{counter}\t2^{valToTest.GetBitLength() - 1}/{(double)valToTest}");
                }

                if (sw.ElapsedMilliseconds > TestTargetInMillseconds)
                {
                    s.Break();
                    break;
                }
            }
        });
    }

#if !DEBUG
    [TestMethod]
    [ExpectedException(typeof(DivideByZeroException))]
    public void Verify_Inverse_ShouldFailOnZeroInput()
    {
        _ = BigIntegerTools.Inverse(0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Verify_Inverse_ShouldFailOnNegativePrecision()
    {
        _ = BigIntegerTools.Inverse(1,-1);
    }
#endif

    private static bool CheckInverse(BigInteger x)
    {
        BigInteger xInvRes = BigIntegerTools.Inverse(x);
        BigInteger xInvTst = BigIntegerTools.InverseClassic(x);

        int correctBits = 0;
        bool success = xInvRes == xInvTst;
        StringBuilder sb = new();

        if (!success)
        {
            sb.AppendLine($"Inverse Fail with input {x}");
            sb.AppendLine($"  Result: {xInvRes} !=");
            sb.AppendLine($"  Answer: {xInvTst}");

            if (xInvRes.GetBitLength() != x.GetBitLength())
            {
                sb.AppendLine($"  Result length incorrect:  [{xInvRes.GetBitLength()}] != [{x.GetBitLength(),-4}]");
            }

            if (xInvTst.GetBitLength() != x.GetBitLength())
            {
                sb.AppendLine($"  Classic length incorrect: [{xInvTst.GetBitLength()}] != [{x.GetBitLength(),-4}]");
            }

            correctBits = BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
            if (xInvRes.GetBitLength() - correctBits > 0)
            {
                sb.AppendLine($"  incorrect bits:[{xInvRes.GetBitLength()-correctBits}]  CorrectBits:[{correctBits}] of [{xInvRes.GetBitLength()}]");
            }
        }

        IsTrue(success, sb.ToString());

        return success;
    }


    [TestMethod]
    public void Verify_TruncateToAndRound()
    {
        BigInteger inputInt, retInt;
        BigFloat inputVal, retBF;

        inputInt = BigInteger.Parse("2222222222");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8); //should be 132
        if (retInt != 132)
        {
            Console.WriteLine($"TrunkAndRnd10 - Should be 132 but got {retInt}");
        }

        inputInt = BigInteger.Parse("-2222222222");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);
        if (retInt != -132)
        {
            Console.WriteLine($"TrunkAndRnd20 - Should be -132 but got {retInt}");
        }

        inputInt = BigInteger.Parse("-1024");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);
        if (retInt != -128)
        {
            Console.WriteLine($"TrunkAndRnd30 - Should be -128 but got {retInt}");
        }

        inputInt = BigInteger.Parse("-1022");               // -1111111110           
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);    // -100000000(-256)
        if (retInt != -256)
        {
            Console.WriteLine($"TrunkAndRnd40 - Should be -256 but got {retInt}");
        }

        inputInt = BigInteger.Parse("1022");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);
        if (retInt != 256)
        {
            Console.WriteLine($"TrunkAndRnd45 - Should be 256 but got {retInt}");
        }

        inputInt = BigInteger.Parse("-1023");               // -1111111111           
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);    // -100000000(-256)
        if (retInt != -256)
        {
            Console.WriteLine($"TrunkAndRnd50 - Should be -256 but got {retInt}");
        }

        inputInt = BigInteger.Parse("1023");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);
        if (retInt != 256)
        {
            Console.WriteLine($"TrunkAndRnd55 - Should be 256 but got {retInt}");
        }

        inputInt = BigInteger.Parse("-1024");              // -1000000000      
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);   // -10000000(-128)
        if (retInt != -128)
        {
            Console.WriteLine($"TrunkAndRnd60 - Should be -128 but got {retInt}");
        }

        inputInt = BigInteger.Parse("1024");
        retInt = BigIntegerTools.TruncateToAndRound(inputInt, 8);
        if (retInt != 128)
        {
            Console.WriteLine($"TrunkAndRnd65 - Should be 128 but got {retInt}");
        }

        inputVal = new BigFloat("2222222222", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 8);
        if (retBF.UnscaledValue != 132)
        {
            Console.WriteLine($"TrunkAndRnd70 - Should be 132 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("-2222222222", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 8);
        if (retBF.UnscaledValue != -132)
        {
            Console.WriteLine($"TrunkAndRnd80 - Should be -132 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("-1024", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 8);
        if (retBF.UnscaledValue != -128)
        {
            Console.WriteLine($"TrunkAndRnd90 - Should be -128 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1024", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 8);
        if (retBF.UnscaledValue != 128)
        {
            Console.WriteLine($"TrunkAndRnd100 - Should be 128 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1022", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 9);
        if (retBF.UnscaledValue != 511)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 511 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1023", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 9);
        if (retBF.UnscaledValue != 512)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 512 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1022", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 10);
        if (retBF.UnscaledValue != 1022)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 1022 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1023", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 10);
        if (retBF.UnscaledValue != 1023)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 1023 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1024", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 10);
        if (retBF.UnscaledValue != 256)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 256 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("1025", 0);
        retBF = BigFloat.SetPrecisionWithRound(inputVal, 10);
        if (retBF.UnscaledValue != 257)
        {
            Console.WriteLine($"TrunkAndRnd110 - Should be 257 but got {retBF.UnscaledValue}");
        }

        inputVal = new BigFloat("2.00000000000", 0);
        BigFloat output = BigFloat.Sqrt(inputVal);
        BigFloat expect = new("1.4142135623730950488016887242097");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);

        inputVal = new BigFloat("200000000000");
        output = BigFloat.Sqrt(inputVal);
        expect = new BigFloat("447213.59549995793928183473374626");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);

        inputVal = new BigFloat("0.0215841551");
        output = BigFloat.Sqrt(inputVal);
        expect = new BigFloat("0.14691546923316142068618979769788");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);

        inputVal = new BigFloat("0.000000001");
        output = BigFloat.Sqrt(inputVal);
        expect = new BigFloat("0.000031622776601683793319988935444327");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);

        inputVal = new BigFloat("98765432109876543210987654321098765432109876543210987654321098765432109876543210");
        output = BigFloat.Sqrt(inputVal);
        expect = new BigFloat("9938079900558082311789231964937550558064.64944382685442702212868466033571678970487057062388");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);

        inputVal = new BigFloat("0.98765432109876543210987654321098765432109876543210987654321098765432109876543210");
        output = BigFloat.Sqrt(inputVal);
        expect = new BigFloat("0.993807990055808231178923196493755055806464944382685442702212868466033571678970487057062388");
        Verify_TruncateAndRoundHelper(inputVal, output, expect);
    }

    private static void Verify_TruncateAndRoundHelper(BigFloat inputVal, BigFloat output, BigFloat preciseAnswer)
    {
        //int expectedOutputSize = inputVal.Size - ((inputVal < output)?1:0); This version is more correct because it will shrink the out of precision area.
        int expectedOutputSize = inputVal.Size;
        BigFloat expectedBF = BigFloat.SetPrecisionWithRound(preciseAnswer, expectedOutputSize);

        Console.WriteLine($"{((output.ToString() == preciseAnswer.ToString()) ? "YES!" : "NO! ")}  Sqrt({inputVal})[{inputVal.Size}]" +
            $"\r\n  was      {output.ToString() + " [" + output.Size + "]",20} " +
            $"\r\n  expected {expectedBF.ToString() + " [" + expectedBF.Size + "]",20} [{expectedOutputSize}]");
    }

    [TestMethod]
    public void Verify_RightShiftWithRound()
    {
        BigInteger expVal;
        string expect, input0;
        int size;
        bool carry;

        // test 'TryParseBinary'
        input0 = "10100010010111";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out BigInteger resVal));
        expVal = 10391;
        IsTrue(resVal == expVal, $"TryParseBinary({input0},_) was {resVal} but expected {expVal}");

        // test 'RightShiftWithRound'
        input0 = "10100010010111";
        expect = "1010001001100";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out BigInteger inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "-10100010010111";
        expect = "-1010001001100";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "10100010010110";
        expect = "1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "-10100010010110";
        expect = "-1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "10100010010110";
        expect = "101000100110";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "-10100010010110";
        expect = "-101000100110";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "101000100101011";
        expect = "1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "-101000100101011";
        expect = "-1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "101000100101101";
        expect = "1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound'
        input0 = "-101000100101101";
        expect = "-1010001001011";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 2);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "11111111111111";
        expect = "10000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "-11111111111111";
        expect = "-10000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1);
        IsTrue(resVal == expVal, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "11111111111111";
        expect = "10000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1, ref size);
        IsTrue(resVal == expVal && size == 14, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "-11111111111111";
        expect = "-10000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1, ref size);
        IsTrue(resVal == expVal && size == 14, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' 
        input0 = "11111111111110";
        expect = "1111111111111";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        resVal = BigIntegerTools.RightShiftWithRound(inpVal, 1, ref size);
        IsTrue(resVal == expVal && size == 13, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "11111111111111";
        expect = "1000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        carry = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out resVal, inpVal, 1, size);
        IsTrue(resVal == expVal && carry, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "-11111111111111";
        expect = "-1000000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        carry = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out resVal, inpVal, 1, size);
        IsTrue(resVal == expVal && carry, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' 
        input0 = "11111111111110";
        expect = "1111111111111";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        carry = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out resVal, inpVal, 1, size);
        IsTrue(resVal == expVal && !carry, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' (with overflow)
        input0 = "-11111111111111";
        expect = "-100000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        carry = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out resVal, inpVal, 2, size);
        IsTrue(resVal == expVal && carry, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");

        // test 'RightShiftWithRound' 
        input0 = "-11011111111111";
        expect = "-111000000000";
        IsTrue(BigIntegerTools.TryParseBinary(input0, out inpVal));
        IsTrue(BigIntegerTools.TryParseBinary(expect, out expVal));
        size = (int)inpVal.GetBitLength();
        carry = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out resVal, inpVal, 2, size);
        IsTrue(resVal == expVal && !carry, $"RightShiftWithRound({inpVal}) was {resVal} but expected {expect}");
    }

    [TestMethod]
    public void SetPrecision()
    {
        //BigFloat inputVal, res;
        //string output, expect;
        //inputVal = new BigFloat("0.9876543210987654321098765432109876");
        //string exact = "0.11111100110101101110100111100000110111110100110111000011010010...";

        //for (int i = 1; i < 6; i++)
        //{
        //    res = BigFloat.SetPrecision(inputVal, i);
        //    output = res.ToString("B");
        //    int temp = Convert.ToInt32(exact[2..(i + 2)], 2);
        //    int mostSigBigInRemovedSection = (int)exact[i + 2] - 48;
        //    expect = Convert.ToString(temp + mostSigBigInRemovedSection, 2).Insert(1, ".");
        //    IsTrue(output == expect, $"SetPrecision({inputVal}) was {output} but expected {expect}");
        //}
        //for (int i = 6; i < 20; i++)
        //{
        //    res = BigFloat.SetPrecision(inputVal, i);
        //    output = res.ToString("B");
        //    int temp = Convert.ToInt32(exact[2..(i + 2)], 2);
        //    int mostSigBigInRemovedSection = (int)exact[i + 2] - 48;
        //    expect = "0." + Convert.ToString(temp + mostSigBigInRemovedSection, 2);
        //    IsTrue(output == expect, $"SetPrecision({inputVal}) was {output} but expected {expect}");
        //}

        //res = BigFloat.SetPrecision(inputVal, 11);
        //output = res.ToString("B");
        //expect = "0.11111100111";
        //IsTrue(output == expect, $"SetPrecision({inputVal}) was {output} but expected {expect}");
    }

    [TestMethod]
    public void ExtendPrecision()
    {

        //BigFloat SetPrecision(BigFloat x, int newSize, bool useRounding = false)
        //BigFloat ExtendPrecision(BigFloat x, int bitsToAdd)
    }

    [TestMethod]
    public void Verify_FloatAndDoubleExceptions()  // last for debugging
    {
#if !DEBUG
        ThrowsException<OverflowException>(() => new BigFloat(float.PositiveInfinity));
        ThrowsException<OverflowException>(() => new BigFloat(float.NegativeInfinity));
        ThrowsException<OverflowException>(() => new BigFloat(double.PositiveInfinity));
        ThrowsException<OverflowException>(() => new BigFloat(double.NegativeInfinity));
        ThrowsException<OverflowException>(() => new BigFloat(float.NaN));
        ThrowsException<OverflowException>(() => new BigFloat(double.NaN));
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
}

