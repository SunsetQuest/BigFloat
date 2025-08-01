﻿// Copyright Ryan Scott White. 2020-2025
// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// Starting 2/25, ChatGPT/Claude/GitHub Copilot/Grok were used in the development of this library.

using BigFloatLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using static BigFloatLibrary.BigFloat;

#pragma warning disable IDE0051  // Ignore unused private members
//#pragma warning disable CS0162 // Ignore unreachable code

namespace PlaygroundAndShowCase;

public static class Showcase
{
    ////////////// Play Area & Examples //////////////
    public static void Main()
    {
        //////////////////// TEST AREA //////////////////// 
        

        //Console.WriteLine(ceil0.ToBinaryString(false));
        //Console.WriteLine(ceil1.ToBinaryString(false));
         
        /////// Author experimentation area - Please make sure to comment this top area out! ///////
        // Console.WriteLine("2320000 -> " + BigFloat.Parse("2320000"));
        // Console.WriteLine("232XXXX -> " + BigFloat.Parse("232XXXX"));
        // Console.WriteLine("232XX -> " + BigFloat.Parse("232XX"));
        // Console.WriteLine("232X.X -> " + BigFloat.Parse("232X.X"));
        // Console.WriteLine("232 -> " + BigFloat.Parse("232"));
        // Console.WriteLine("23.2 -> " + BigFloat.Parse("23.2"));
        // Console.WriteLine("2.32 -> " + BigFloat.Parse("2.32"));
        // Console.WriteLine("0.232 -> " + BigFloat.Parse("0.232"));
        // Console.WriteLine("0.00232 -> " + BigFloat.Parse("0.00232"));
        // Console.WriteLine("0.00000000232 -> " + BigFloat.Parse("0.00232"));
        // Console.WriteLine("0.000000000232 -> " + BigFloat.Parse("0.00000000000232"));
        // Console.WriteLine("0.0000000000000000000000000000232 -> " + BigFloat.Parse("0.0000000000000000000000000000232"));

        // BigFloat randNumber = BigFloat.RandomWithMantissaBits(1024, 0, 0);
        // Console.WriteLine($"The number {randNumber} is {randNumber.Size} bits.");

        // BigFloat x = new("1.1234567890123456"); Console.WriteLine(ConstantVisualization.GetContinuedFraction(x));
        // Console.WriteLine(ConstantVisualization.GetConstantInfo("Pi"));
        // var allConstants = BigFloat.Constants.WithConfig(precisionInBits: 20000).GetAll();
        // Console.WriteLine(ConstantVisualization.CreateComparisonTable(allConstants));

        // CastingFromFloatAndDouble_Stuff(); return;
        // Constant_Stuff(); return;
        // Inverse_Benchmark(); return;
        // NthRoot_Benchmark(); return;
        // NthRoot_Benchmark2(); return;
        // Pow_Stuff(); return;
        // Pow_Benchmark(); return;
        // Pow_Stuff4(); return;
        // PowMostSignificantBits_Benchmark(); return;
        // ToStringHexScientific_Stuff(); return;
        // Compare_Stuff(); return;
        // GeneratePi_Benchmark(); return;
        // Parse_Stuff(); return;
        // TryParse_Stuff(); return;
        // TruncateToAndRound_Stuff(); return;
        // Remainder_Stuff(); return;
        // Sqrt_Stuff(); return;


        //////////////////// Initializing and Basic Arithmetic: ////////////////////
        // Initialize BigFloat numbers
        BigFloat a = new("123456789.012345678901234"); // Initialize by String
        BigFloat b = new(1234.56789012345678);         // Initialize by Double

        // Basic arithmetic
        BigFloat sum = a + b;
        BigFloat difference = a - b;
        BigFloat product = a * b;
        BigFloat quotient = a / b;

        // Display results
        Console.WriteLine($"Sum: {sum}");
        // Output: Sum: 123458023.5802358024

        Console.WriteLine($"Difference: {difference}");
        // Output: Difference: 123455554.4444555554

        Console.WriteLine($"Product: {product}");
        // Output: Product: 152415787532.39

        Console.WriteLine($"Quotient: {quotient}");
        // Output: Quotient: 100000.000000000


        //////////////////// Working with Mathematical Constants: ////////////////////
        // Access constants like Pi or E from Constants
        Dictionary<string, BigFloat> bigConstants = Constants.WithConfig(precisionInBits: 1000).GetAll();
        BigFloat pi = bigConstants["Pi"];
        BigFloat e = bigConstants["E"];

        Console.WriteLine($"e to 1000 binary digits: {e.ToString()}");
        // Output:
        // e to 1000 binary digits: 2.718281828459045235360287471352662497757247093699959574966967
        // 627724076630353547594571382178525166427427466391932003059921817413596629043572900334295
        // 260595630738132328627943490763233829880753195251019011573834187930702154089149934884167
        // 509244761460668082264800168477411853742345442437107539077744992070

        // Use Pi in a calculation (Area of a circle with r = 100)
        BigFloat radius = new("100.0000000000000000");
        BigFloat area = pi * radius * radius;

        Console.WriteLine($"Area of the circle: {area}");
        // Output: Area of the circle: 31415.9265358979324


        //////////////////// Precision Manipulation: ////////////////////
        // Initialize a number with high precision
        BigFloat preciseNumber = new("123.45678901234567890123");
        BigFloat morePreciseNumber = ExtendPrecision(preciseNumber, bitsToAdd: 50);

        Console.WriteLine($"Extend Precision result: {morePreciseNumber}");
        // Output: Extend Precision result: 123.45678901234567890123000000000102788

        // Initialize an integer with custom precision
        BigFloat c = IntWithAccuracy(10, 100);
        Console.WriteLine($"Int with specified accuracy: {c}");
        // Output: Int with specified accuracy: 10.000000000000000000000000000000


        //////////////////// Comparing Numbers: ////////////////////
        // Initialize two BigFloat numbers

        BigFloat num1 = new("12345.6790");
        BigFloat num2 = new("12345.6789");

        // Lets compare the numbers that are not equal...
        bool areEqual = num1 == num2;
        bool isFirstBigger = num1 > num2;

        Console.WriteLine($"Are the numbers equal? {areEqual}");
        // Output: Are the numbers equal? False

        Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
        // Output: Is the first number bigger? True

        // Due to the nuances of decimal to binary conversion, we end up with some undesired rounding.
        BigFloat num3 = new("12345.6789");
        BigFloat num4 = new("12345.67896");

        areEqual = num3 == num4;
        isFirstBigger = num3 > num4;

        Console.WriteLine($"Are the numbers equal? {areEqual}");
        // Output: Are the numbers equal? True

        Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
        // Output: Is the first number bigger? False


        //////////////////// Handling Very Large or Small Exponents: ////////////////////
        // Creating a large number 
        BigFloat largeNumber = new("1234e+7");

        Console.WriteLine($"Large Number: {largeNumber}");
        // Output: Large Number: 1.234e+10

        Console.WriteLine($"Large Number: {BigFloat.ToStringDecimal(largeNumber, digitMaskingForm: true)}");
        // Output: Large Number: 1234XXXXXXX

        // Creating a very large number
        BigFloat veryLargeNumber = new("1234e+300");

        Console.WriteLine($"Large Number: {veryLargeNumber}");
        // Output: Large Number: 123 * 10^301

        // Creating a very small number 
        BigFloat smallNumber = new("1e-300");

        Console.WriteLine($"Small Number: {smallNumber}");
        // Output: Small Number: 1e-300

        BigFloat num5 = new("12121212.1212");
        BigFloat num6 = new("1234");
        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 12121212.1212 * 1234 = 1.496e+10

        num5 = new("12121212.1212");
        num6 = new("3");
        BigFloat result = num5 * num6;
        Console.WriteLine($"{num5} * {num6} = {result}");
        // Output: 12121212.1212 * 3 = 4e+7

        num5 = new("121212.1212");
        num6 = new("1234567");

        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 121212.1212 * 1234567 = 1.496445e+11

        Console.WriteLine($"GetPrecision: {num6.Precision}");


        // Output: GetPrecision: 21
    }

    static void Constant_Stuff()
    {
        BigFloat PiFromGenerated = Constants.GeneratePi(20000);
        Console.WriteLine($"Pi(generated): {PiFromGenerated}");

        // Default constants with standard precision
        BigFloat PiFromDefault = BigFloat.Constants.Fundamental.Pi;
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

    private static void Inverse_Benchmark()
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        //Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)19; // Binary 0001

        Console.WriteLine();

        BigInteger valToTest = BigInteger.Parse("170000");

        if (true)
        {
            bool stoppedAready = false;
            Stopwatch perfTimerClassic = new(), perfTimerNew = new();
            double totalSpeedup = 0;
            int totalCount = 0;
            long divideBy = 160 * 100 * Stopwatch.Frequency / 1000000000;
            //long perfTimerTotal1 = 0, perfTimerTotal2 = 0;
            for (int i = 0; i < 9700; i++)
            {
                for (int k = 0; k < 12800; k++)
                {
                    valToTest += 1 + (valToTest / 23); //(valToTest >> 5); //(valToTest / 100003); 23 127 251 503 997 7727  100003
                    int valLenth = (int)valToTest.GetBitLength();
                    if (valLenth < 0) continue;
                    if (valLenth > 10000 && !stoppedAready)
                    {
                        DisplayStatus(valToTest, perfTimerClassic, perfTimerNew, ref totalSpeedup, ref totalCount, divideBy);
                        _ = Console.ReadKey();
                        stoppedAready = true;
                    }

                    BenchmarkInverseHelper(valToTest, perfTimerClassic, perfTimerNew, k, valLenth);
                }
                //perfTimerTotal1 += perfTimerClassic.ElapsedTicks; perfTimerTotal2 += perfTimerNew.ElapsedTicks;
                DisplayStatus(valToTest, perfTimerClassic, perfTimerNew, ref totalSpeedup, ref totalCount, divideBy);
            }

            static void DisplayStatus(BigInteger valToTest, Stopwatch perfTimerClassic, Stopwatch perfTimerNew, ref double totalSpeedup, ref int totalCount, long divideBy)
            {
                double thisTotal = (double)perfTimerClassic.ElapsedTicks / perfTimerNew.ElapsedTicks;
                totalSpeedup += thisTotal;
                totalCount++;
                Console.WriteLine($"[{valToTest.GetBitLength(),4}] Ticks: {perfTimerClassic.ElapsedTicks / divideBy,4} " +
                    $"-> {perfTimerNew.ElapsedTicks / divideBy,4} ({(float)thisTotal,-12}) " +
                    $"(Total: {totalSpeedup}/{totalCount} -> {(float)totalSpeedup / totalCount,-12})");
            }
        }

        static void BenchmarkInverseHelper(BigInteger valToTest, Stopwatch perfTimerClassic, Stopwatch perfTimerNew, int k, int valLen)
        {
            BigInteger xInvTst = 0, xInvRes = 0;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (k % 2 == 0)
            {
                perfTimerClassic.Start();
                //xInvTst = BigIntegerTools.InverseOther(valToTest, valLen);
                perfTimerClassic.Stop();

                perfTimerNew.Start();
                xInvRes = BigIntegerTools.Inverse(valToTest, valLen);
                perfTimerNew.Stop();
            }
            else
            {
                perfTimerNew.Start();
                xInvRes = BigIntegerTools.Inverse(-valToTest, valLen);
                perfTimerNew.Stop();

                perfTimerClassic.Start();
                //xInvTst = BigIntegerTools.InverseOther(-valToTest, valLen);
                perfTimerClassic.Stop();
            }

            if (xInvRes != xInvTst)
            {
                Console.WriteLine($"{xInvRes} != \r\n{xInvTst} with input {valToTest}");

                if (xInvRes.GetBitLength() != valLen)
                {
                    Console.WriteLine($"Result:  [{xInvRes.GetBitLength()}] != [{valLen,-4}]");
                }

                if (xInvTst.GetBitLength() != valLen)
                {
                    Console.WriteLine($"Other: [{xInvTst.GetBitLength()}] != [{valLen,-4}]");
                }

                int correctBits = BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
                if (xInvRes.GetBitLength() - correctBits > 0)
                {
                    Console.WriteLine($"CorrectBits:[{correctBits}] of [{xInvRes.GetBitLength()}] x:{valToTest}");
                }
            }
        }
    }

    private static void NthRoot_Benchmark()
    {
        Stopwatch sw = new();
        Stopwatch swBase = new();
        Random random = new(6);
        ulong runCount = 0;
        //Parallel.For(0, 1000000, i => 
        for (int i = 0; i < 10000000; i++)
        {
            long totalTime = 0, totalTimeBase = 0;
            //////// Generate a random non-zero BigInteger for the value ////////
            BigInteger val = BigIntegerTools.RandomBigInteger(minBitLength: 50, maxBitLength: 10000);

            //////// Generate a random nth root ////////
            int n = random.Next(3, 400);

            int outputBits = (int)(BigInteger.Log(val, 2) / n) + 1;
            if (outputBits is < 1 or > 85)
            {
                continue;
            }

            //////// Let run our algorithm and benchmark it. ////////
            sw.Start();
            BigInteger result = BigIntegerTools.NewtonNthRoot(val, n);
            sw.Stop();

            swBase.Start();
            //BigInteger result2 = BigIntegerTools.NewtonNthRoot_Draft(val, n);
            swBase.Stop();

            Interlocked.Increment(ref runCount);

            // |---------------|----------------|
            bool isTooSmall = BigInteger.Pow(result, n - 1) > val;
            bool isTooLarge = val >= BigInteger.Pow(result + 2, n);

            if (isTooSmall || isTooLarge)
            {
                BigInteger answer = NthRootBisection(val, n, out _);
                BigInteger diff = answer - result;
                Console.WriteLine($"MissBy:{diff,2}  val: {val}^(1/{n}) Ans:{answer}[{outputBits}] != Res:{result} valBits: {BigIntegerTools.ToBinaryString(answer)}");
            }

            _ = Interlocked.Add(ref totalTime, sw.ElapsedTicks);
            _ = Interlocked.Add(ref totalTimeBase, swBase.ElapsedTicks);

            if (i % 1000 == 0 && totalTime > 0)
            {
                if (i == 5000) { totalTime = 0; totalTimeBase = 0; runCount = 0; sw.Reset(); swBase.Reset(); Console.WriteLine("RESET"); }
                Console.WriteLine($"i:{i} AvgTime:{(float)totalTime / runCount} Speed-up: {(float)totalTimeBase / totalTime}X");
            }
        }
        //); // Parallel.For ends here

        // Verify nthRoot by Bisection
        // source: https://www.codeproject.com/Tips/831816/The--Method-and-Calculating-Nth-Roots, Cryptonite, 2014
        static BigInteger NthRootBisection(BigInteger value, int root, out BigInteger remainder)
        {
            //special conditions
            if (value < 2)
            {
                if (value < 0)
                {
                    throw new Exception("value must be a positive integer");
                }

                remainder = 0;
                return value;
            }
            if (root < 2)
            {
                if (root < 1)
                {
                    throw new Exception("root must be greater than or equal to 1");
                }

                remainder = 0;
                return value;
            }

            //set the upper and lower limits
            BigInteger upperbound = value;
            BigInteger lowerbound = 0;

            while (true)
            {
                BigInteger nval = (upperbound + lowerbound) / 2;
                BigInteger tstsq = BigInteger.Pow(nval, root);
                if (tstsq > value)
                {
                    upperbound = nval;
                }

                if (tstsq < value)
                {
                    lowerbound = nval;
                }
                if (tstsq == value)
                {
                    lowerbound = nval;
                    break;
                }
                if (lowerbound == upperbound - 1)
                {
                    break;
                }
            }
            remainder = value - BigInteger.Pow(lowerbound, root);
            return lowerbound;
        }
    }

    private static void NthRoot_Benchmark2()
    {
        Stopwatch timer = Stopwatch.StartNew();
        BigFloat result = NthRoot(new BigFloat((ulong)3 << 60, -60), 3);
        Console.WriteLine($"NthRoot {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");

        result = NthRoot(new BigFloat((BigInteger)3 << 200, -200), 3);
        Console.WriteLine($"NthRoot {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");

        timer.Stop();
        timer.Reset();

        for (int i = 2; i >= 0; i--)
        {
            for (int m = 7; m < 300; m *= 31)
            {
                for (int e = 5; e < 10; e++)
                {
                    BigFloat bf = new((ulong)m << 60, -60);
                    //timer = Stopwatch.StartNew();
                    timer.Restart();
                    timer.Start();
                    BigFloat result2 = NthRoot(bf, e);
                    timer.Stop();
                    if (i == 0)
                    {
                        Console.WriteLine($"{m}^(1/{e}) = {result2}  correct:{double.Pow((double)bf, 1 / (double)e)}  ticks {timer.ElapsedTicks}");
                    }
                }
            }
        }

        Console.WriteLine(NthRoot(100000000000, 5));
    }


    //////////////  Pow() Play Area & Examples //////////////
    private static void Pow_Stuff()
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

    private static void Pow_Benchmark()
    {
        Stopwatch timer = new();
        timer.Start();

        int countNotExact = 0, wayLow = 0, minus2 = 0, minus1 = 0, plus1 = 0, plus2 = 0, plus3 = 0, wayHigh = 0, total = 0;
        //for (BigInteger val = 3; val < 3000; val += 1)
        //int b = 0;
        for (int b = 0; b < 99999999; b += 7)
        {
            //timer.Reset();
            for (BigInteger val = BigInteger.One << b; val < BigInteger.One << (b + 1); val += BigInteger.Max(BigInteger.One, BigInteger.One << (b - 6 + 1)))
            //for (int v = b * 1000; v < (b + 1) * 1000; v ++)
            //Parallel.For(b * 1000, (b + 1) * 1000, v =>
            {
                //BigInteger val = (BigInteger)v * (BigInteger)1 + 2;
                int valSize = (int)val.GetBitLength();
                uint maxPowSearch = 18000;// (uint)(3502.0 / (valSize - 0)) + 1;
                //for (BigInteger val = 3; val < (BigInteger)float.MaxValue; val = (BigInteger)((double)val * 2.13 + 1))
                //for (BigInteger val = (((BigInteger)1) <<b); val < (((BigInteger)1) << (b+1)); val = (BigInteger)((double)val * 1.02 + 1))
                //for (uint exp = 2; exp < maxPowSearch; exp++)
                for (uint exp = 1; exp < maxPowSearch; exp = (uint)((exp * 1.3) + 1))
                {
                    //if ((valSize * exp) >= 3502)
                    //    continue;
                    timer.Start();
                    //BigInteger res = BigFloat.PowAccurate(val, valSize, /*(int)*/exp, out int shifted);
                    (BigInteger res, int shifted) = BigIntegerTools.PowMostSignificantBitsApprox(val, (int)exp, valSize,extraAccurate: false);
                    timer.Stop();
                    //BigInteger ans = 0; int shiftedAns =0;
                    //BigInteger ans = BigIntegerTools.PowMostSignificantBits(val, (int)exp, out int shiftedAns, valSize);
                    BigInteger ans = PowAccurate(val, (int)exp, out int shiftedAns, valSize);
                    //BigInteger exact = BigInteger.Pow(val, (int)exp);
                    if (res != ans || shifted != shiftedAns)
                    {
                        //Console.WriteLine($"{res} != {ans}  {val}^{exp}");
                        //Console.WriteLine($"Results for {ToBinaryString(val)}[{val.GetBitLength()}]^{exp}....\r\n" +
                        //    $"  Result: {ToBinaryString(res)}[{res.GetBitLength()}] << {shifted})  OR  {res} \r\n" +
                        //    $"  Answer: {ToBinaryString(ans)}[{ans.GetBitLength()}] << {shiftedAns}\r\n" +
                        //    $"  Exact:  {ToBinaryString(exact)}[{exact.GetBitLength()}]");
                        if ((res >> 1) + 1 == ans)
                        {
                            minus1++;
                            Console.WriteLine("resolved !!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 1 == ans)
                        { 
                            plus1++;
                            Console.WriteLine($"plus1:{plus1} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res + 1 == ans)
                        {
                            minus1++;
                            Console.WriteLine($"minus1:{minus1} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 2 == ans)
                        {
                            plus2++;
                            Console.WriteLine($"plus2:{plus2} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 3 == ans)
                        {
                            plus3++;
                            Console.WriteLine($"plus3:{plus3} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res + 2 == ans)
                        {
                            minus2++;
                            Console.WriteLine($"minus2:{minus2} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res > ans)
                        {
                            wayHigh++;
                            Console.WriteLine($"wayHigh({res - ans}):{wayHigh} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res < ans)
                        {
                            wayLow++;
                            Console.WriteLine($"wayLow({res - ans}):{wayLow} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (shifted != shiftedAns)
                        {
                            Console.WriteLine($"shifted({shifted}) != shiftedAns({shiftedAns}) ?????????????? m= val={val} pow={exp}");
                        }
                        else
                        {
                            Console.WriteLine($"???????????????????????? m= val={val} pow={exp}");
                        }
                        countNotExact++;
                    }
                    total++;
                    if (total % (1 << 26) == 0)
                    {
                        Console.WriteLine($"b={b,3}  val={val} exp={exp}  maxPowSearch={maxPowSearch}");
                    }
                }
            }//);
            //if (countNotExact > 0)
            {
                //UInt128 count =  ((UInt128)1 << b);
                Console.WriteLine($"bits: {b,3} countNotExact:{countNotExact}  wayLow:{wayLow}  -2:{minus2}  -1:{minus1}  +1:{plus1}  +2:{plus2}  +3:{plus3}  wayHigh:{wayHigh}   total:{total}  ticks: {timer.ElapsedTicks / total}");
                countNotExact = 0;
                wayLow = 0;
                minus2 = 0;
                minus1 = 0;
                plus1 = 0;
                plus2 = 0;
                plus3 = 0;
                wayHigh = 0;
                total = 0;
            }
            //if (m % 1024 == 0)
            //    Console.WriteLine(m);
        }
        // For testing only (SLOWWWWWW)
        static BigInteger PowAccurate(BigInteger value, int exp, out int shifted, int size)
        {
            if (size != value.GetBitLength())
            {
                throw new Exception("size != value.GetBitLength()");
            }
            BigInteger res = BigInteger.Pow(value, exp);
            shifted = (int)(res.GetBitLength() - value.GetBitLength());
            if (value == 0) return BigInteger.One;
            if (exp == 0) return BigInteger.Zero;
            //return BigIntegerTools.RightShiftWithRound(res, shifted);
            (BigInteger result, bool carry) = (BigIntegerTools.RightShiftWithRoundAndCarry(res, shifted));
            if (carry)
                shifted++;
            return result;
        }
    }

    private static void Pow_Stuff4()
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

    private static void PowMostSignificantBits_Benchmark()
    {
        Stopwatch timer = Stopwatch.StartNew();
        long errorTotal = 0; // too high or low by 2 or more

        //_ = Parallel.For(2, 8192, bitSize =>
        for (int bitSize = 2; bitSize < 1026; bitSize += 1)
        {

            int exp = 0;
            BigInteger miss = 0;
            long correctBits = 0;
            BigInteger ans = 0;

            long counter = 0;
            long oneTooHi = 0; // too high by 1
            long oneTooLo = 0; // too low  by 1
            for (int valTries = 0; valTries < 10; valTries++)
            {
                BigInteger val = GenerateRandomBigInteger(bitSize);
                int valSize = (int)val.GetBitLength();
                for (int expSize = 2; expSize < 12; expSize++)
                {
                    int wantedBits;
                    //int wantedBits = valSize;  
                    for (wantedBits = 1; wantedBits < valSize + 2; wantedBits++)
                    {
                        exp = (int)GenerateRandomBigInteger(expSize);

                        if ((long)exp * Math.Max(valSize, wantedBits) >= int.MaxValue)
                        {
                            continue;
                        }

                        //if (/*exp < 3 ||*/ val < 3 /*|| wantedBits < 3*/)
                        //{
                        //    continue;
                        //}

                        // Answer Setup version 1
                        //BigInteger ans = BigFloat.PowMostSignificantBits(val, valSize, exp, out int shiftedAns, wantedBits, true);

                        // Answer Setup version 2
                        BigInteger p = BigInteger.Pow(val, exp);
                        int shiftedAns = Math.Max(0, (int)(p.GetBitLength() - Math.Min(wantedBits, valSize)));
                        (ans, bool overflowed) = BigIntegerTools.RightShiftWithRoundAndCarry(p, shiftedAns);
                        if (overflowed)
                        {
                            shiftedAns++;
                        }

                        if (val.IsZero)
                        {
                            shiftedAns = 0;
                        }

                        // Result Setup
                        timer.Restart();
                        (BigInteger res,int shiftedRes) = BigIntegerTools.PowMostSignificantBitsApprox(val, exp, valSize, wantedBits, false);
                        timer.Stop();
                        //int needToShiftAgainBy2 = (int)(res.GetBitLength() - wantedBits);
                        //res = BigFloat.RightShiftWithRound(res, needToShiftAgainBy2); shifted += needToShiftAgainBy2;
                        //BigInteger res4 = BigFloat.Pow4(val, valSize, exp, out int shifted4, workSize);

                        if (shiftedAns < 0)
                        {
                            Console.WriteLine($"Error - depending on the mode a negative shiftedAns is not supported ");
                        }

                        if (shiftedAns - shiftedRes == 1)  // res did not round up
                        {
                            ans <<= 1;
                        }
                        else if (shiftedAns - shiftedRes == -1)  // ans did not round up
                        {
                            res <<= 1;
                        }
                        else if (shiftedRes != shiftedAns)
                        {
                            if (shiftedRes > shiftedAns + 1)
                            {
                                ans = BigIntegerTools.RoundingRightShift(ans, shiftedRes - shiftedAns);
                            }
                            else
                            {
                                Console.WriteLine($"ERRORRRRR - Answer should always be at least 2 bits larger");
                            }
                        }

                        miss = ans - res;
                        correctBits = ans.GetBitLength() - miss.GetBitLength();
                        //Console.Write($", {valSize}, {(val.IsEven?1:0)}, {exp}, {(exp % 2 == 0 ? 1 : 0)}, {expSize}, wantedBits:{wantedBits}, got:{correctBits} ({ans.GetBitLength()} - {diff.GetBitLength()})  miss:({wantedBits- correctBits})\r\n");

                        _ = Interlocked.Increment(ref counter);

                        if (BigInteger.Abs(miss) > 1)
                        {
                            Console.Write($"!!!!!!! diff:{miss,-2}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            _ = Interlocked.Increment(ref errorTotal);
                        }
                        else if (miss > 0)
                        {
                            //Console.Write($"OK but ans 1 too Low, valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            _ = Interlocked.Increment(ref oneTooLo);
                        }
                        else if (miss < 0)
                        {
                            Console.Write($"OK but ans 1 too High,  valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            _ = Interlocked.Increment(ref oneTooHi);
                        }
                    }
                    if (val % 8192 == 777)
                    {
                        Console.Write($"sz:{bitSize,3},diff:{miss,-3}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-9}[{expSize,-2}] wantedBits:{wantedBits,-4}" +
                                                            $" got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} count:{counter,-7}(Lo({(float)oneTooLo / counter,-4:F5} hi{(float)oneTooHi /*/ (float)counter*/,-1})" +
                                                            $" time:{timer.ElapsedTicks,3} val:{val}\r\n");
                    }
                }
            }

            //if (errorTotal > 0 || oneTooLo > 0 || oneTooHi > 0)
            //    Console.WriteLine($"errorTotal:{errorTotal} oneTooLo:{oneTooLo} oneTooHi:{oneTooHi}");
        }
        //);


        static BigInteger GenerateRandomBigInteger(int maxNumberOfBits)
        {
            byte[] data = new byte[(maxNumberOfBits / 8) + 1];
            Random.Shared.NextBytes(data);
            data[^1] >>= 8 - (maxNumberOfBits % 8);
            return new(data, true);
        }
    }

    private static void ToStringHexScientific_Stuff()
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

    private static void Compare_Stuff()
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

    private static void GeneratePi_Benchmark()
    {
        for (int i = 400000; i <= 400000; i += 4)
        {
            Stopwatch timer = Stopwatch.StartNew();
            BigFloat res2 = Constants.GeneratePi(i);
            timer.Stop();
            Console.WriteLine($"{i,4} {res2} {timer.ElapsedTicks}");
        }
        Console.WriteLine($"perfect: 3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679821480865132823066470938446095505822317253594081284811174502841027019385211055596446229");
    }

    private static void Parse_Stuff()
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

    private static void TryParse_Stuff()
    {
        _ = BigInteger.TryParse("15.0",
                NumberStyles.AllowDecimalPoint,
                null, out BigInteger number);

        Console.WriteLine(number.ToString());

        //BigFloat bf0 = new BigFloat("1.1"); Console.WriteLine(bf0.DebuggerDisplay);
        BigFloat bf0 = new("-1.1"); bf0.DebugPrint("");
        //BigFloat bf0 = new BigFloat("-1.1"); Console.WriteLine(bf0.DebuggerDisplay);
    }

    private static void TruncateToAndRound_Stuff()
    {
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 1));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 2));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)0b111, 3));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)(-0b111), 1));
        Console.WriteLine(BigIntegerTools.TruncateToAndRound((BigInteger)(-0b1000), 1));
    }

    private static void Remainder_Stuff()
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

    private static void Sqrt_Stuff()
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

    private static void CastingFromFloatAndDouble_Stuff()
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
