// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using BigFloatLibrary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using static BigFloatLibrary.BigFloat;

namespace PlaygroundAndShowCase;

public static class TestingArea
{
    public static void Constant_Stuff()
    {
        BigFloat PiFromGenerated = Constants.GeneratePi(20000);
        Console.WriteLine($"Pi(generated): {PiFromGenerated}");

        // Default constants with standard precision
        BigFloat PiFromDefault = BigFloat.Constants.Pi;
        Console.WriteLine($"Pi(default): {PiFromDefault}");

        // Get all constants at once with configured precision
        var allConstants = BigFloat.Constants.WithConfig(precisionInBits: 20000).GetAll();
        allConstants.TryGetValue("Pi", out BigFloat piFromConfig);
        Console.WriteLine($"Pi(using configured precision): {piFromConfig}");

        // Default constants by name
        var PiFromConstantName = BigFloat.Constants.Get("Pi");
        Console.WriteLine($"Pi(by name): {PiFromConstantName}");

        // Other constants with default precision
        Console.WriteLine($"Pi: {allConstants["Pi"]}");
        Console.WriteLine($"E: {allConstants["E"]}");
        Console.WriteLine($"Sqrt2: {allConstants["Sqrt2"]}");
        Console.WriteLine($"Golden Ratio: {allConstants["GoldenRatio"]}");
    }

    //////////////  Pow() Play Area & Examples //////////////
    public static void Pow_Stuff()
    {
        //// BigFloat.Zero  BigFloat.One
        //AreEqual(BigFloat.Pow(BigFloat.Zero, 0), 1, $"Failed on: 0^0");
        //AreEqual(BigFloat.Pow(BigFloat.One, 0), 1, $"Failed on: 1^0");
        //AreEqual(BigFloat.Pow(0, 0), 1, $"Failed on: 0^0");
        //AreEqual(BigFloat.Pow(1, 0), 1, $"Failed on: 1^0");
        //AreEqual(BigFloat.Pow(2, 0), 1, $"Failed on: 2^0");
        //AreEqual(BigFloat.Pow(3, 0), 1, $"Failed on: 3^0");

        //AreEqual(BigFloat.Pow(BigFloat.Zero, 1), 0, $"Failed on: 0^1");
        //AreEqual(BigFloat.Pow(BigFloat.One, 1), 1, $"Failed on: 1^1");
        //AreEqual(BigFloat.Pow(0, 1), 0, $"Failed on: 0^1");
        //AreEqual(BigFloat.Pow(1, 1), 1, $"Failed on: 1^1");
        //AreEqual(BigFloat.Pow(2, 1), 2, $"Failed on: 2^1");
        //AreEqual(BigFloat.Pow(3, 1), 3, $"Failed on: 3^1");

        //AreEqual(BigFloat.Pow(BigFloat.Zero, 2), 0, $"Failed on: 0^2");
        //AreEqual(BigFloat.Pow(BigFloat.One, 2), 1, $"Failed on: 1^2");
        AreEqual(Pow(0, 2), 0, $"Failed on: 0^2");
        AreEqual(Pow(1, 2), 1, $"Failed on: 1^2");
        AreEqual(Pow(2, 2), 4, $"Failed on: 2^2");
        AreEqual(Pow(3, 2), 9, $"Failed on: 3^2");

        //AreEqual(BigFloat.Pow(BigFloat.Zero, 3), 0, $"Failed on: 0^3");
        //AreEqual(BigFloat.Pow(BigFloat.One, 3), 1, $"Failed on: 1^3");
        AreEqual(Pow(0, 3), 0, $"Failed on: 0^3");
        AreEqual(Pow(1, 3), 1, $"Failed on: 1^3");
        AreEqual(Pow(2, 3), 8, $"Failed on: 2^3");
        AreEqual(Pow(3, 3), 27, $"Failed on: 3^3");

        for (int k = 3; k < 20; k++)
        {
            for (double i = 1; i < 20; i = 1 + (i * 1.7))
            {
                BigFloat exp2 = (BigFloat)double.Pow(i, k);
                BigFloat res2 = Pow((BigFloat)i, k);
                AreEqual(res2, exp2, $"Failed on: {i}^{k}, result:{res2} exp:{exp2}");
            }
        }

        BigFloat tbf = new(255, 0);
        _ = Pow(tbf, 3);
        tbf = new BigFloat(256, 0);
        _ = Pow(tbf, 3);
        tbf = new BigFloat(511, 0);
        _ = Pow(tbf, 3);
        tbf = new BigFloat(512, 0);
        _ = Pow(tbf, 3);
    }

    public static void Pow_Stuff4()
    {
        for (BigFloat val = 777; val < 778; val++)
        {
            for (int exp = 5; exp < 6; exp++)
            {
                BigFloat res = Pow(val, exp);
                double correct = Math.Pow((double)val, exp);
                Console.WriteLine($"{val,3}^{exp,2} = {res,8} ({res.RoundedMantissa,4} << {res.Scale,2})  Correct: {correct,8}");
            }
        }
    }

    public static void ToStringHexScientific_Stuff()
    {
        new BigFloat("1.8814224e11").DebugPrint("1.8814224e11"); //18814224____
        new BigFloat("-10000e4").DebugPrint("-10000e4");
        Console.WriteLine(new BigFloat("-32769").ToStringHexScientific(showInTwosComplement: true));
        Console.WriteLine(new BigFloat("-32769").ToStringHexScientific());
        new BigFloat("-32769").DebugPrint(); //100000000

        Console.WriteLine(new BigFloat("8814224057326597640e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597642e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597646e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597648e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597650e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597652e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597654e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597656e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597658e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597660e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597662e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597664e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597666e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597668e16").ToStringHexScientific());

        //10000000_
        new BigFloat("1.0000000e+8").DebugPrint(); //100000000
        new BigFloat("10000000e1").DebugPrint();   //10000000000

        Console.WriteLine(new BigFloat("10000e4").ToStringHexScientific());

        new BigFloat("1.0000000e+8").DebugPrint();
        new BigFloat("10000000e1").DebugPrint();
    }

    public static void Compare_Stuff()
    {
        AreEqual(new BigFloat("1.0e-1"), new BigFloat("0.10"), $"Failed on: 3^0");
        AreEqual(new BigFloat("0.10e2"), new BigFloat("10."), $"Failed on: 3^0");
        AreEqual(new BigFloat("0.0010e2"), new BigFloat("0.10"), $"Failed on: 3^0");
        AreEqual(new BigFloat("1.00e0"), new BigFloat("1.00"), $"Failed on: 3^0");
        AreEqual(new BigFloat("10.0e-1"), new BigFloat("1.00"), $"Failed on: 3^0");
        AreEqual(new BigFloat("100.0e-2"), new BigFloat("1.000"), $"Failed on: 3^0");
        AreEqual(new BigFloat("300.0e-2"), new BigFloat("3.000"), $"Failed on: 3^0");
        AreEqual(new BigFloat("300.0e-2"), new BigFloat("0.03000e+2"), $"Failed on: 3^0");
        AreEqual(new BigFloat("1.0000000e+8"), new BigFloat("10000000e1"), $"Failed on: 3^0");
        AreEqual(new BigFloat("10000e4").ToStringHexScientific(), "17D7 << 14");
    }

    public static void Parse_Stuff()
    {
        BigFloat aa = Parse("0b100000.0");
        BigFloat bb = Parse("0b100.0");
        BigFloat rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b100000");
        bb = Parse("0b100.0");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b100000.0");
        bb = Parse("0b100");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b10000.0");
        bb = Parse("0b1000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b1000.00000");
        bb = Parse("0b10000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b1000.0");
        bb = Parse("0b10000.000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b1.0000");
        bb = Parse("0b10000000.");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = Parse("0b1000.00000000000000000000000000");
        bb = Parse("0b10000.0000000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");
    }

    public static void TryParse_Stuff()
    {
        _ = BigInteger.TryParse("15.0",
                NumberStyles.AllowDecimalPoint,
                null, out BigInteger number);

        Console.WriteLine(number.ToString());

        //BigFloat bf0 = new BigFloat("1.1"); Console.WriteLine(bf0.DebuggerDisplay);
        BigFloat bf0 = new("-1.1"); bf0.DebugPrint("");
        //BigFloat bf0 = new BigFloat("-1.1"); Console.WriteLine(bf0.DebuggerDisplay);
    }

    public static void TruncateToAndRound_Stuff()
    {
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 1));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 2));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 3));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)(-0b111), 1));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)(-0b1000), 1));
    }

    public static void Remainder_Stuff()
    {
        //BigFloat bf = new BigFloat(1, -8);
        BigFloat bf = new("0.00390625");
        BigFloat res = Remainder(bf, new BigFloat("1.00000000")); // i.e. "bf % 1;"
        // Answer of 0.00390625 % 1 is:
        //   0.00390625 or 0.0039063 or 0.003906 or 0.00391 or 0.0039 or 0.004 or 0.00 or or 0.0 or or 0

        AreEqual(res, new BigFloat("0.004"), "0.004");
        AreEqual(res, new BigFloat("0.0039"), "0.0039");
        AreEqual(res, new BigFloat("0.00391"), "0.00391");
        AreEqual(res, new BigFloat("0.003906"), "0.003906");
        AreEqual(res, new BigFloat("0.0039063"), "0.0039063");
        AreEqual(res, new BigFloat("0.00390625"), "0.00390625");

        IsFalse(res == 0, "(Int)0");
        IsFalse(res == new BigFloat("0"), "0");
        IsFalse(res == new BigFloat("0.0"), "0.0");
        IsFalse(res == new BigFloat("0.00"), "0.00");

        IsFalse(res == new BigFloat("0.003"), "0.003");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.0038"), "0.0038");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.00390"), "0.00390");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.003905"), "0.003905");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.0039062"), "0.0039062");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.00390624"), "0.00390624");   // values that are a smudge to small

        IsFalse(res == new BigFloat("0.005"), "0.005");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.0040"), "0.0040");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.00392"), "0.00392");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.003907"), "0.003907");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.0039064"), "0.0039064");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.00390626"), "0.00390626");   // values that are a smudge to large
    }

    public static void Sqrt_Stuff()
    {
        BigFloat inpParam = new("2.0000000000000000000000000000000");
        BigFloat fnOutput = Sqrt(inpParam);
        BigFloat expected = new("1.4142135623730950488016887242097");
        Console.WriteLine($"Calculate Sqrt( {inpParam} )\r\n" +
                          $"              = {fnOutput}\r\n" +
                          $"      expected: {expected}\r\n");
        BigFloat a = new(1);
        Console.WriteLine(inpParam < a);
    }

    public static void CastingFromFloatAndDouble_Stuff()
    {
        Console.WriteLine((BigFloat)(float)1.0);
        Console.WriteLine((BigFloat)1.0);
        Console.WriteLine((BigFloat)(float)0.5);
        Console.WriteLine((BigFloat)0.5);
        Console.WriteLine((BigFloat)(float)0.25);
        Console.WriteLine((BigFloat)2.0);
        Console.WriteLine((BigFloat)(float)2.0);

        Console.WriteLine("(BigFloat)1     (Double->BigFloat->String) -> " + (BigFloat)1);
        Console.WriteLine("(BigFloat)1.0   (Double->BigFloat->String) -> " + (BigFloat)1.0);
        Console.WriteLine("(BigFloat)1.01  (Double->BigFloat->String) -> " + (BigFloat)1.01);
        Console.WriteLine("(BigFloat)1.99  (Double->BigFloat->String) -> " + (BigFloat)1.99);
        Console.WriteLine("(BigFloat)2.00  (Double->BigFloat->String) -> " + (BigFloat)2.00);
        Console.WriteLine("(BigFloat)2.99  (Double->BigFloat->String) -> " + (BigFloat)2.99);
        Console.WriteLine("(BigFloat)3.00  (Double->BigFloat->String) -> " + (BigFloat)3.00);
        Console.WriteLine("(BigFloat)3.99  (Double->BigFloat->String) -> " + (BigFloat)3.99);
        Console.WriteLine("(BigFloat)4.00  (Double->BigFloat->String) -> " + (BigFloat)4.00);
        Console.WriteLine("(BigFloat)4.99  (Double->BigFloat->String) -> " + (BigFloat)4.99);
        Console.WriteLine("(BigFloat)5.00  (Double->BigFloat->String) -> " + (BigFloat)5.00);
        Console.WriteLine("(BigFloat)5.99  (Double->BigFloat->String) -> " + (BigFloat)5.99);
        Console.WriteLine("(BigFloat)6.00  (Double->BigFloat->String) -> " + (BigFloat)6.00);
        Console.WriteLine("(BigFloat)6.99  (Double->BigFloat->String) -> " + (BigFloat)6.99);
        Console.WriteLine("(BigFloat)7.00  (Double->BigFloat->String) -> " + (BigFloat)7.00);
        Console.WriteLine("(BigFloat)7.99  (Double->BigFloat->String) -> " + (BigFloat)7.99);
        Console.WriteLine("(BigFloat)8.00  (Double->BigFloat->String) -> " + (BigFloat)8.00);
        Console.WriteLine("(BigFloat)8.99  (Double->BigFloat->String) -> " + (BigFloat)8.99);
        Console.WriteLine("(BigFloat)9.00  (Double->BigFloat->String) -> " + (BigFloat)9.00);
        Console.WriteLine("(BigFloat)9.9   (Double->BigFloat->String) -> " + (BigFloat)9.9);
        Console.WriteLine("(BigFloat)10.0  (Double->BigFloat->String) -> " + (BigFloat)10.0);
        Console.WriteLine("(BigFloat)10.1  (Double->BigFloat->String) -> " + (BigFloat)10.1);
        Console.WriteLine("(BigFloat)99.9  (Double->BigFloat->String) -> " + (BigFloat)99.9);
        Console.WriteLine("(BigFloat)100.0 (Double->BigFloat->String) -> " + (BigFloat)100.0);

        Console.WriteLine($"Double: {(double)1234567.8901234567890123} --> BigFloat: {(BigFloat)(double)1234567.8901234567890123}");
        Console.WriteLine($"Double: {(double)887102364.1} --> BigFloat: {(BigFloat)(double)887102364.1}");
        Console.WriteLine($"Double: {(double)887102364.01} --> BigFloat: {(BigFloat)(double)887102364.01}");
        Console.WriteLine($"Double: {(double)887102364.001} --> BigFloat: {(BigFloat)(double)887102364.001}");
        //Console.WriteLine("887102364.0 --> " + (BigFloat)(float) 887102364.0);

        Console.WriteLine($"Double: {(double)9156478887102364.0} --> BigFloat: {(BigFloat)(double)9156478887102364.0}");

        Console.WriteLine($"Double: {(double)887102364.001} --> BigFloat: {(BigFloat)(double)887102364.001}");
        Console.WriteLine($"Double: {(double)9156478887102364.0} --> BigFloat: {(BigFloat)(double)9156478887102364.0}");
        Console.WriteLine($"Float:  {(long)9156478887102364.0} --> BigFloat: {(BigFloat)(long)9156478887102364.0}");

        Console.WriteLine($"Float:  {(long)(float)9156478887102364.0} --> BigFloat: {(BigFloat)(float)9156478887102364.0}");
        Console.WriteLine("9156478887102364.0 --> " + (BigFloat)(float)9156478887102364.0);

        for (double i = 0.000000000000001; i < 9999999999999999.9; i *= 1.1)
        {
            BigFloat bfi = (BigFloat)i;
            double bfd = (double)bfi;
            Console.WriteLine(i.ToString() + " => " + bfi.ToString() + " => " + bfd.ToString());
        }

        Console.WriteLine((BigFloat)0.000000012300000000000000);
        Console.WriteLine((BigFloat)0.011111);
        Console.WriteLine((BigFloat)0.11111111111);
        Console.WriteLine((BigFloat)0.1111111111111);
        Console.WriteLine((BigFloat)(float)0.11111);
        Console.WriteLine((BigFloat)(float)0.1111111);
        Console.WriteLine((BigFloat)(float)0.111111111);
        Console.WriteLine((BigFloat)(float)0.11111111111);
        Console.WriteLine((BigFloat)(float)0.1111111111111);
    }

    private static void CheckIt(BigFloat inputVal, BigFloat output, BigFloat expect)
    {
        Console.WriteLine($"{(output.ToString() == expect.ToString() ? "YES!" : "NO! ")}  Sqrt({inputVal}) " +
            $"\r\n  was      {output + " [" + output.Size + "]",20} " +
            $"\r\n  expected {expect + " [" + expect.Size + "]",20} ");
    }

    private static void PrintStringAsBigFloatAndAsDouble(string origStingValue)
    {
        Console.WriteLine($"Input As String:  {origStingValue,22}");
        Console.WriteLine($" ->Double  ->Str: {double.Parse(origStingValue).ToString("G17", CultureInfo.InvariantCulture),22}");
        bool success = TryParse(origStingValue, out BigFloat bf);
        if (success)
        {
            Console.WriteLine($" ->BigFloat->Str: {bf,22}");
        }
        else
        {
            Console.WriteLine($" ->BigFloat->Str: FAIL");
        }

        Console.WriteLine();
    }

    [DebuggerHidden]
    private static void IsFalse(bool val, string msg = null)
    {
        IsTrue(!val, msg);
    }

    [DebuggerHidden]
    private static void IsTrue(bool val, string msg = null)
    {
        if (!val)
        {
            Fail(msg);
        }
    }

    [DebuggerHidden]
    private static void AreEqual<T>(T val1, T val2, string msg = null) where T : IEquatable<T>
    {
        if (!val1.Equals(val2))
        {
            Fail(msg);
        }
    }


    [DebuggerHidden]
    private static void Fail(string msg)
    {
        Console.WriteLine(msg ?? "Failed");

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}
