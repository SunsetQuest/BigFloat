// Copyright Ryan Scott White. 2020, 2021, 2022, 2023, 2024

// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// This struct was written by human hand. This may change soon.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using BigFloatLibrary;
using static BigFloatLibrary.BigFloat;
//using static BigFloatLibrary.BigFloat;


#pragma warning disable IDE0051  // Ignore unused private members
//#pragma warning disable CS0162 // Ignore unreachable code

namespace ShowCase2;

public static class Showcase
{
    
    //////////////  BigConstants Play Area & Examples //////////////
        static volatile uint lowestScore = 99999999;
    public static void Main()
    {

        //BigInteger valToTest = BigInteger.Parse("12312312312312312312");
        //{
        //    int valLen = (int)valToTest.GetBitLength();
        //    BigInteger xInvTst = (((BigInteger)1 << (valLen * 2 + 10)) / valToTest) >> 10;
        //    BigInteger xInvRes = BigIntegerTools.InverseBigInteger(valToTest, valLen);
        //    Console.WriteLine($"missed last {xInvRes.GetBitLength() - BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()} of {xInvRes.GetBitLength()} (Correct:{BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()})");
        //}

        //valToTest = BigInteger.Parse("12312312312312312312123123");
        //{
        //    int valLen = (int)valToTest.GetBitLength();
        //    BigInteger xInvTst = (((BigInteger)1 << (valLen * 2 + 10)) / valToTest) >> 10;
        //    BigInteger xInvRes = BigIntegerTools.InverseBigInteger(valToTest, valLen);
        //    Console.WriteLine($"missed last {xInvRes.GetBitLength() - BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()} of {xInvRes.GetBitLength()} (Correct:{BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()})");
        //}

        //valToTest = BigInteger.Parse("12312312123123123123123123123123121231231231231231231231231212312312312312312312312312123123");
        //{
        //    int valLen = (int)valToTest.GetBitLength();
        //    BigInteger xInvTst = (((BigInteger)1 << (valLen * 2 + 10)) / valToTest) >> 10;
        //    BigInteger xInvRes = BigIntegerTools.InverseBigInteger(valToTest, valLen);
        //    Console.WriteLine($"missed last {xInvRes.GetBitLength() - BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()} of {xInvRes.GetBitLength()} (Correct:{BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count()})");
        //}


        // LEFT OFF HERE WITH ADDING INVERT FUNCTION
        Console.WriteLine();
        Stopwatch perfTimer1 = new(), perfTimer2 = new();
        long perfTimerTotal1 = 0, perfTimerTotal2 = 0;
        //BigInteger valToTest = BigInteger.Parse("17000000000000000000");
        BigInteger valToTest = BigInteger.Parse("17000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        //BigInteger valToTest = BigInteger.Parse("54423786437031527763697912279787023177007175773396983622816024035711543468387091978597056733234710315130549820241594446393756345612424802270801471745336113250193667718316040253941103853841621774234813857891663076076715068586343817309246770086732294883423415223673860597326831387240313092477650074951316685417593007167891273031148391605579769517420776570212699556110145652641739426307621148603418506166450038891744322262956504929671467272484497540453925091672914077693364778107681126835011311994757409261811224974104348895160386664369615480529461919450491798311706809949739817420025767850441857925");
        int OFFSET = 0;
        BigInteger xInvTst = 0, xInvRes = 0;
        //xInvRes = BigIntegerTools.InverseBigInteger(0xFF, 7);
        //xInvTst = BigIntegerTools.InverseBigIntegerClassic(0xFF, 7);
        //xInvRes = BigIntegerTools.InverseBigInteger(0xFFFFFFFF, 32);
        //xInvRes = BigIntegerTools.InverseBigIntegerClassic(0xFFFFFFFF, 32);
        //xInvRes = BigIntegerTools.InverseBigInteger(0xFFFFFFFFFUL, 32 + 4);
        //xInvRes = BigIntegerTools.InverseBigIntegerClassic(0xFFFFFFFFFUL, 32 + 4);
        //xInvRes = BigIntegerTools.InverseBigInteger(0xFFFFFFFFFFFFFFFFUL, 64);
        //xInvRes = BigIntegerTools.InverseBigIntegerClassic(0xFFFFFFFFFFFFFFFFUL, 64);
        //xInvRes = BigIntegerTools.InverseBigInteger(BigInteger.Parse("1777777777777777777777"), 70);
        //xInvRes = BigIntegerTools.InverseBigIntegerClassic(BigInteger.Parse("1777777777777777777777"), 70);
        //xInvRes = BigIntegerTools.InverseBigInteger(BigInteger.Parse("1777777777777777777777777777777777777777777"), 140);
        //xInvRes = BigIntegerTools.InverseBigIntegerClassic(BigInteger.Parse("1777777777777777777777777777777777777777777"), 140);


        if (true)
        for (int i = 0; i < 9700; i++)
        {
            int valLen = 0;
            perfTimerTotal1 += perfTimer1.ElapsedTicks; perfTimerTotal2 += perfTimer2.ElapsedTicks; perfTimer1.Reset(); perfTimer2.Reset();
            for (int k = 0; k < 12800; k++)
            {
                valToTest += 1 + (valToTest / 127); //(valToTest >> 5); //(valToTest / 100003); 127 251 503 997 7727  100003
                valLen = (int)valToTest.GetBitLength() + OFFSET;
                if (valLen < 0) continue;

                GC.Collect();
                if (k % 2 == 0)
                {
                    perfTimer1.Start();
                    xInvTst = BigIntegerTools.InverseBigIntegerClassic(valToTest, valLen);
                    perfTimer1.Stop();

                    perfTimer2.Start();
                    xInvRes = BigIntegerTools.InverseBigInteger(valToTest, valLen);
                    perfTimer2.Stop();
                }
                else
                {
                    perfTimer2.Start();
                    xInvRes = BigIntegerTools.InverseBigInteger(valToTest, valLen);
                    perfTimer2.Stop();

                    perfTimer1.Start();
                    xInvTst = BigIntegerTools.InverseBigIntegerClassic(valToTest, valLen);
                    perfTimer1.Stop();
                }


                if (xInvRes != xInvTst)
                {
                    Console.WriteLine($"Input: {valToTest}\r\ni:{i}-k:{k,3} {xInvRes} != {xInvTst} ");

                    if (xInvRes.GetBitLength() != valLen)
                        Console.WriteLine($"i:{i}-k:{k,3} {xInvRes.GetBitLength()} != {valLen,-4}  {xInvTst} on Result");

                    if (xInvTst.GetBitLength() != valLen)
                        Console.WriteLine($"i:{i}-k:{k,3} {xInvTst.GetBitLength()} != {valLen,-4}  {xInvTst} on Classic");

                    int correctBits = BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
                    if ((xInvRes.GetBitLength() - correctBits) > 0)
                        Console.WriteLine($"i:{i}-k: CorrectBits:{correctBits} of {xInvRes.GetBitLength()}. x: {valToTest}");
                    Console.ReadKey();
                }

            }
            long divideBy = (160 * 100 * Stopwatch.Frequency) / 1000000000;
            int correct = BigIntegerTools.ToBinaryString(xInvRes).Zip(BigIntegerTools.ToBinaryString(xInvTst), (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
            Console.WriteLine($"[{valLen,4}] Ticks: {perfTimer1.ElapsedTicks / divideBy,4} -> {perfTimer2.ElapsedTicks / divideBy,4} ({(float)perfTimer1.ElapsedTicks / perfTimer2.ElapsedTicks,-12}) (Total: {(float)perfTimerTotal1 / perfTimerTotal2,-12}) Missed {xInvRes.GetBitLength() - correct,4} of {xInvRes.GetBitLength()}");
        }















        // FindAdjustmentsForMethodToResolveIssue(); return;
        // NewtonNthRootPerformance(); return;
        // NthRoot_DRAFT_Stuff(); return;

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
        // Output: Sum: 123458023.5802358023581

        Console.WriteLine($"Difference: {difference}");
        // Output: Difference: 123455554.4444555554443

        Console.WriteLine($"Product: {product}");
        // Output: Product: 152415787532.38838

        Console.WriteLine($"Quotient: {quotient}");
        // Output: Quotient: 99999.99999999999


        //////////////////// Working with Mathematical Constants: ////////////////////
        // Access constants like Pi or E from BigConstants
        BigFloat.BigConstants bigConstants = new(
            requestedAccuracyInBits: 1000,
            onInsufficientBitsThenSetToZero: true,
            cutOnTrailingZero: true);
        BigFloat pi = bigConstants.Pi;
        BigFloat e = bigConstants.E;

        Console.WriteLine($"e to 1000 binary digits: {e.ToString()}");
        // Output:
        // e to 1000 binary digits: 2.71828182845904523536028747135266249775724709369995957496696
        // 76277240766303535475945713821785251664274274663919320030599218174135966290435729003342
        // 95260595630738132328627943490763233829880753195251019011573834187930702154089149934884
        // 1675092447614606680822648001684774118537423454424371075390777449920696

        // Use Pi in a calculation (Area of a circle with r = 100)
        BigFloat radius = new("100.0000000000000000");
        BigFloat area = pi * radius * radius;

        Console.WriteLine($"Area of the circle: {area}");
        // Output: Area of the circle: 31415.92653589793238


        //////////////////// Precision Manipulation: ////////////////////
        // Initialize a number with high precision
        BigFloat preciseNumber = new("123.45678901234567890123");
        BigFloat morePreciseNumber = BigFloat.ExtendPrecision(preciseNumber, bitsToAdd: 50);

        Console.WriteLine($"Extend Precision result: {morePreciseNumber}");
        // Output: Extend Precision result: 123.45678901234567890122999999999787243

        // Initialize an integer with custom precision
        BigFloat c = BigFloat.IntWithAccuracy(10, 100);
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
        // Output: Large Number: 123XXXXXXXX

        // Creating a very large number
        BigFloat veryLargeNumber = new("1234e+300");

        Console.WriteLine($"Large Number: {veryLargeNumber}");
        // Output: Large Number: 123 * 10^301

        // Creating a very small number 
        BigFloat smallNumber = new("1e-300");

        Console.WriteLine($"Small Number: {smallNumber}");
        // Output: Small Number: 0.00000000000000000000000000000000000000000000000000000000000000
        // 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000
        // 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000
        // 000000000000000000000000000000000000000000000000000000000000000001

        BigFloat num5 = new("12121212.1212");
        BigFloat num6 = new("1234");
        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 12121212.1212 * 1234 = 1496XXXXXXX  (rounded up from 14957575757)

        num5 = new("12121212.1212");
        num6 = new("3");
        BigFloat result = num5 * num6;
        Console.WriteLine($"12121212.1212 * 3 = 36363636.3636");
        Console.WriteLine($"{num5} * {num6} = {result}");
        // Output: 12121212.1212 * 3 = 0XXXXXXXX
        // Optimal Output:              4XXXXXXX

        num5 = new("121212.1212");
        num6 = new("1234567");

        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 121212.1212 * 1234567 = 149644XXXXXX

        Console.WriteLine($"GetPrecision: {num6.GetPrecision}");
        // Output: GetPrecision: 21


        ///////////////////////////////////////////////////
        //////////////////// TEST AREA ////////////////////
        ///////////////////////////////////////////////////

        //BigConstant_Stuff();
        //BigConstant_Stuff2();
        //Pow_Stuff();
        //PowMostSignificantBits_Stuff();
        //Pow_Stuff3();
        //Pow_Stuff4();
        //ToStringHexScientific_Stuff();
        //Compare_Stuff();
        //Unknown_Stuff();
        //GeneratePi_Stuff();
        //Parse_Stuff();
        //TruncateToAndRound_Stuff();
        //Remainder_Stuff();
        //TryParse_Stuff();
        //Sqrt_Stuff();
        //CastingFromFloatAndDouble_Stuff();
        //NthRoot_DRAFT_Stuff();
    }

    private static void FindAdjustmentsForMethodToResolveIssue()
    {
        UInt128 aa = UInt128.Parse("1234567890123456789012345678901");
        UInt128 bb = UInt128.Parse("2345678901234567890123456789012");
        BigInteger ans = (((BigInteger)aa * (BigInteger)bb) >> 128); //(aa>>37) * (bb>>37);
                                                                     //UInt128 ans = (aa>>37) * (bb>>37);
        UInt128 res = Int128Tools.MultHi((UInt128)aa, bb);
        Console.WriteLine($"aa  {BigIntegerTools.BigIntegerToBinaryString((BigInteger)aa)}[{((BigInteger)aa).GetBitLength()}]");
        Console.WriteLine($"onl 1110011010101011111101101001100000110010111110001110111110111010110001011010011111001110011001000101010000010010101000001101100011100100001010101100111011000101011...");
        Console.WriteLine($"ans {BigIntegerTools.BigIntegerToBinaryString((BigInteger)ans)}[{((BigInteger)ans).GetBitLength()}]");
        Console.WriteLine($"res {BigIntegerTools.BigIntegerToBinaryString((BigInteger)res)}[{((BigInteger)res).GetBitLength()}]");

        long tryCount = 0;
        Parallel.For(-15, 17, aaa =>
        {
            //for (int aaa = -21; aaa < 21; aaa++){
            for (int bbb = 0; bbb < 4; bbb++)
                for (int ccc = -32; ccc < 1; ccc++)
                    for (int ddd = -16; ddd < -5; ddd++)
                        for (int eee = 0; eee < 5; eee++)
                            for (int fff = 0; fff < 1; fff++)
                                for (int ggg = 0; ggg < 1; ggg++)
                                {
                                    tryCount++;
                                    //Config p = new Config(
                                    //    aaa,
                                    //    bbb,
                                    //    ccc,
                                    //    ddd,
                                    //    eee,
                                    //    fff,
                                    //    ggg );

                                    uint totalmiss = 0;

                                    for (int cons1 = 1; cons1 < 18; cons1++)
                                    //for (int cons2 = 1; cons2 < 7; cons2 *= 3)
                                    {
                                        BigInteger temp = ((BigInteger)aa * cons1);
                                        UInt128 a2 = (UInt128)(temp << (127 - (int)BigInteger.Log2(temp)));
                                        BigInteger ans2 = temp;

                                        int next = 3;
                                        for (int i = 2; i < 10000; i++)
                                        {
                                            //Console.WriteLine($"Power " + i);

                                            ans2 *= a2;
                                            ans2 >>= (int)ans2.GetBitLength() - 256; //lets not let ans2 get too big.
                                            BigInteger tempAns = ans2 >> (int)ans2.GetBitLength() - 128;

                                            if (i == next)
                                            {
                                                next = (int)(i * 1.34);

                                                BigInteger res2 = Int128Tools.PowerFast(a2, i/*, p*/);

                                                BigInteger miss = (res2 - tempAns);// * (100 / int.Log2(i)) ;

                                                totalmiss += (uint)miss.GetBitLength();
                                                //Console.WriteLine($"res {BigFloat.BigIntegerToBinaryString((BigInteger)res2)}[{((BigInteger)res2).GetBitLength()}]");
                                                //Console.WriteLine($"ans {BigFloat.BigIntegerToBinaryString((BigInteger)tempAns)}[{((BigInteger)tempAns).GetBitLength()}]");
                                                //Console.WriteLine($"dif {miss}");
                                            }
                                        }
                                    }
                                    if (aaa == 0) totalmiss--;
                                    if (bbb == 0) totalmiss--;
                                    if (ccc == 0) totalmiss--;
                                    if (ddd == 0) totalmiss--;
                                    if (eee == 0) totalmiss--;
                                    //if (fff == 0) totalmiss--;
                                    if (ggg == 0) totalmiss--;

                                    if (totalmiss < lowestScore)
                                    {
                                        lowestScore = totalmiss;
                                        //Console.WriteLine($"totalmiss {totalmiss} a{p.a,3} b{p.b,3} c{p.c,3} d{p.d,3} e{p.e,3} f{p.f,3} g{p.g} ");
                                    }
                                }
        });
    }

    private static void BigConstant_Stuff() //added to test
    {
        BigFloat.BigConstants bigConstants = new(4000);
        BigFloat pi200ref = bigConstants.Pi;   // 3.141592653589793238462643383279502884197169...
        BigFloat pi200gen = BigFloat.BigConstants.GeneratePi(4000);
        Console.WriteLine(pi200ref == pi200gen);

        for (int i = 0; i < 500; i++)
            Console.WriteLine(pi200ref == BigFloat.BigConstants.GeneratePi(i));

        BigFloat.BigConstants c = new(10);
        Console.WriteLine(c);
        Console.WriteLine(c.Pi);
    }

    private static void BigConstant_Stuff2() //added to test
    {
        BigFloat[] bigFloats1000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        BigFloat[] bigFloats2000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        for (int i = 0; i < bigFloats1000.Length; i++)
        {
            BigFloat bf1000 = bigFloats1000[i];
            BigFloat bf2000 = bigFloats2000[i];
            if (bf1000 != bf2000)
                Console.WriteLine(bf2000 - bf1000);
        }
    }



    //////////////  NthRoot Play Area & Examples //////////////


    static long NewtonNthRootPerformance()
    {
        Stopwatch sw = new();
        Stopwatch swBase = new();
        Random random = new(5);
        long totalTime = 0;

        //Parallel.For(0, 1000000, i => 
        for (int i = 0; i < 1000000; i++)
        {
            //////// Generate a random non-zero BigInteger for the value ////////
            int bits = 0;
            while (bits == 0)
            {
                bits = random.Next(5/*minBitLength*/, 1000/*maxBitLength*/);
            }
            byte[] bytes = new byte[(bits + 7) / 8];
            random.NextBytes(bytes);
            // For the top byte, place a leading 1-bit then down-shift to achieve desired length.
            bytes[^1] = (byte)((0x80 | bytes[^1]) >> (7 - (bits - 1) % 8));
            BigInteger val = new(bytes, true);

            //////// Generate a random nth root ////////
            int n = random.Next(3, 400);

            int outputBits = (int)BigInteger.Log2(val) / n + 1;
            if (outputBits < 48
            && outputBits > 1)
            {

                //////// Let run our algorithm and benchmark it. ////////
                sw.Restart();
                BigInteger result = NewtonNthRoot(ref val, n);
                sw.Stop();

                //////// Lets make sure it is correct. ////////
                swBase.Restart();
                BigInteger answer = NthRoot(val, n, out BigInteger remainder);
                swBase.Stop();
                bool fail = answer != result;
                if (fail)
                {
                    Console.WriteLine($"Bits: {bits,2} n:{n,3} Ticks: {sw.ElapsedTicks}/{swBase.ElapsedTicks} {(fail ? $"FAIL: Ans:{answer} != Res:{result}" : "")} ");
                }

                //////// bottom bits ////////



                /////// Lets check bottom 64 bit
                //ulong low64Bits = (ulong)(val & ulong.MaxValue);
                //ulong total = low64Bits;
                //for (int jj = 1; jj < n; jj++)
                //    total *= low64Bits;

                //if ((ulong)(answer & ulong.MaxValue) != total)
                //    Console.WriteLine($"{answer} (ulong)answer != {total} total");

                Interlocked.Add(ref totalTime, sw.ElapsedTicks); //totalTime += sw.ElapsedTicks;
            }
        }
        //);
        return totalTime;
    }

    // Verify nthRoot
    // https://www.codeproject.com/Tips/831816/The-Bisection-Method-and-Calculating-Nth-Roots, 2014
    public static BigInteger NthRoot(BigInteger value, int root, out BigInteger remainder)
    {
        //special conditions
        if (value < 2)
        {
            if (value < 0) throw new Exception("value must be a positive integer");
            remainder = 0;
            return value;
        }
        if (root < 2)
        {
            if (root < 1) throw new Exception("root must be greater than or equal to 1");
            remainder = 0;
            return value;
        }

        //set the upper and lower limits
        BigInteger upperbound = value;
        BigInteger lowerbound = 0;

        while (true)
        {
            var nval = (upperbound + lowerbound) / 2;
            var tstsq = BigInteger.Pow(nval, root);
            if (tstsq > value) upperbound = nval;
            if (tstsq < value)
            {
                lowerbound = nval;
            }
            if (tstsq == value)
            {
                lowerbound = nval;
                break;
            }
            if (lowerbound == upperbound - 1) break;
        }
        remainder = value - BigInteger.Pow(lowerbound, root);
        return lowerbound;
    }


private static void NthRoot_DRAFT_Stuff()
    {
        //                                                                                                                                                                                        7777777777777777777777777777777777777777777777777777777777777777                   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("7777777777777777777777777777777777777777777777777777777777777777"), 7)}"); Console.WriteLine($"x^(1/7):1340494621.514214278463413501222825825662100997195024832765760458|23859");        //Console.WriteLine($"ANS:  1100111011001010100110000110100100101101000010110010100101000.10001000010110110011101011111111010101010100001110111010010101110001010111001110100001011011110100111101000100010101001000111110110000011001101111110111110110100011011010001101100011100111010110101110000000001010101010101011111001011011110101010111011111001010011100001101000101010000010010001001001110010010001100001110000001110011010010101110100011011001110010011110001000111101101000100011011110100011000110111101001100000|011");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("7777777777777777777777777777777777777777777777777777777777777777"), 4)}"); Console.WriteLine($"x^(1/4):9391044157537525.19591975149938555692792588485605707185903878278)");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("77777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777"), 7)}"); Console.WriteLine($"x^(1/7):1862611236825425192.5326420663234462718496133629936707812842460267769993007449764005342755106890750175013920585641604590068868740|51982282");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("7777777777777777777777777777777"), 3)}");    Console.WriteLine($"x^(1/3):19813073175.87709934055949316958|138 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("7777777777777777777777777777777"), 7)}");    Console.WriteLine($"x^(1/7):25880.89921337705525458987063396|056 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("7777777777777777777777777777777"), 55)}");   Console.WriteLine($"x^(1/55:3.644617186032180086485625982525|169 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("77777777777777777777777777777777"), 3)}");   Console.WriteLine($"x^(1/3):42685972166.249808508213684454449|731");   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 2)}");   Console.WriteLine($"x^(1/2):8944271909999158.7856366946749251|049");   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 3)}");   Console.WriteLine($"x^(1/3):43088693800.637674435185871330387|009");   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 4)}");   Console.WriteLine($"x^(1/4):94574160.900317581330169611988722|");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 5)}");   Console.WriteLine($"x^(1/5):2402248.8679628624664841997871983|");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 6)}");   Console.WriteLine($"x^(1/6):207578.16311124268746614482713121|");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 7)}");   Console.WriteLine($"x^(1/7):36106.407876409947138175505843180|");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("80000000000000000000000000000000"), 8)}");   Console.WriteLine($"x^(1/8):9724.9247246607303150644442684673|");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 2)}");    Console.WriteLine($"x^(1/2):1000000000000000.000000000000000|0");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 3)}");    Console.WriteLine($"x^(1/3):10000000000.00000000000000000000|0");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 4)}");    Console.WriteLine($"x^(1/4):31622776.60168379331998893544432|7");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 5)}");    Console.WriteLine($"x^(1/5):1000000.000000000000000000000000|0");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 6)}");    Console.WriteLine($"x^(1/6):100000.0000000000000000000000000|0");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 7)}");    Console.WriteLine($"x^(1/7):19306.97728883250167007074799840|2");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 8)}");    Console.WriteLine($"x^(1/8):5623.413251903490803949510397764|8");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 9)}");    Console.WriteLine($"x^(1/9):2154.434690031883721759293566519|4");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 10)}");   Console.WriteLine($"x^(1/10):1000.000000000000000000000000000|0");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 11)}");   Console.WriteLine($"x^(1/11):533.6699231206309658153694194942|9");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 12)}");   Console.WriteLine($"x^(1/12):316.2277660168379331998893544432|7");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000000000"), 13)}");   Console.WriteLine($"x^(1/13):203.0917620904735720992124668860|1");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 2)}");        Console.WriteLine($"x^(1/2):10000000000000.0000000000000|00000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 3)}");        Console.WriteLine($"x^(1/3):464158883.361277889241007635|09194");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 4)}");        Console.WriteLine($"x^(1/4):3162277.66016837933199889354|44327");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 5)}");        Console.WriteLine($"x^(1/5):158489.319246111348520210137|33915");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 6)}");        Console.WriteLine($"x^(1/6):21544.3469003188372175929356|65194");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 7)}");        Console.WriteLine($"x^(1/7):5179.47467923121113475517467|79610");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000000"), 8)}");        Console.WriteLine($"x^(1/8):1778.27941003892280122542119|51927");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 2)}");         Console.WriteLine($"x^(1/2):3162277660168.3793319988935|444327");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 3)}");         Console.WriteLine($"x^(1/3):215443469.00318837217592935|665194");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 4)}");         Console.WriteLine($"x^(1/4):1778279.4100389228012254211|951927");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 5)}");         Console.WriteLine($"x^(1/5):100000.00000000000000000000|000000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 6)}");         Console.WriteLine($"x^(1/6):14677.992676220695409205171|148169");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 7)}");         Console.WriteLine($"x^(1/7):3727.5937203149401661724906|094730");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000000"), 8)}");         Console.WriteLine($"x^(1/8):1333.5214321633240256759317|152953");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 2)}");          Console.WriteLine($"x^(1/2):1000000000000.000000000000|0000000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 3)}");          Console.WriteLine($"x^(1/3):100000000.0000000000000000|0000000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 4)}");          Console.WriteLine($"x^(1/4):1000000.000000000000000000|0000000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 5)}");          Console.WriteLine($"x^(1/5):63095.73444801932494343601|3662234");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 6)}");          Console.WriteLine($"x^(1/6):10000.00000000000000000000|0000000");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000000"), 7)}");          Console.WriteLine($"x^(1/7):2682.695795279725747698802|6806276");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 2)}");           Console.WriteLine($"x^(1/2):316227766016.837933199889|35444327");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 3)}");           Console.WriteLine($"x^(1/3):46415888.3361277889241007|63509194");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 4)}");           Console.WriteLine($"x^(1/4):562341.325190349080394951|03977648");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 5)}");           Console.WriteLine($"x^(1/5):39810.7170553497250770252|30508775");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 6)}");           Console.WriteLine($"x^(1/6):6812.92069057961285497988|17963002");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 7)}");           Console.WriteLine($"x^(1/7):1930.69772888325016700707|47998402");      
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000000"), 8)}");           Console.WriteLine($"x^(1/8):749.894209332455827302184|27561514");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000"), 2)}");            Console.WriteLine($"x^(1/2):100000000000.00000000000|000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000"), 3)}");            Console.WriteLine($"x^(1/3):21544346.900318837217592|93566519");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000"), 4)}");            Console.WriteLine($"x^(1/4):316227.76601683793319988|935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000"), 5)}");            Console.WriteLine($"x^(1/5):25118.864315095801110850|320677993");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("10000000000000000000000"), 6)}");            Console.WriteLine($"x^(1/6):4641.5888336127788924100|763509194");  
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000"), 3)}");             Console.WriteLine($"x^(1/3):10000000.00000000000000|0000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000"), 4)}");             Console.WriteLine($"x^(1/4):177827.9410038922801225|4211951927");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000"), 5)}");             Console.WriteLine($"x^(1/5):15848.93192461113485202|1013733915");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("1000000000000000000000"), 6)}");             Console.WriteLine($"x^(1/6):3162.277660168379331998|8935444327");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000"), 2)}");              Console.WriteLine($"x^(1/2):10000000000.0000000000|00000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000"), 3)}");              Console.WriteLine($"x^(1/3):4641588.83361277889241|00763509194");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000"), 4)}");              Console.WriteLine($"x^(1/4):100000.000000000000000|00000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000"), 5)}");              Console.WriteLine($"x^(1/5):10000.0000000000000000|00000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(BigFloat.Parse("100000000000000000000"), 6)}");              Console.WriteLine($"x^(1/6):2154.43469003188372175|92935665194");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000000, 2)}");                                 Console.WriteLine($"x^(1/2):3162277660.1683793319|988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000000, 3)}");                                 Console.WriteLine($"x^(1/3):2154434.6900318837217|592935665194");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000000, 4)}");                                 Console.WriteLine($"x^(1/4):56234.132519035008039|495103977648");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000000, 5)}");                                 Console.WriteLine($"x^(1/5):6309.5734448019424943|436013662234");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000000, 6)}");                                 Console.WriteLine($"x^(1/6):1467.7992676220795409|205171148169");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000000, 2)}");                                  Console.WriteLine($"x^(1/2):1000000000.000000000|0000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000000, 3)}");                                  Console.WriteLine($"x^(1/3):1000000.000000000000|0000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000000, 4)}");                                  Console.WriteLine($"x^(1/4):31622.77660168389331|9988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000000, 5)}");                                  Console.WriteLine($"x^(1/5):3981.071705534972507|7025230508775");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000000, 6)}");                                  Console.WriteLine($"x^(1/6):1000.000000000000000|0000000000000");   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000000, 2)}");                                   Console.WriteLine($"x^(1/2):316227766.016838933|19988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000000, 3)}");                                   Console.WriteLine($"x^(1/3):464158.883361278889|24100763509194");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000000, 4)}");                                   Console.WriteLine($"x^(1/4):17782.7941003892280|12254211951927");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000000, 5)}");                                   Console.WriteLine($"x^(1/5):2511.88643150958011|10850320677993");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000000, 6)}");                                   Console.WriteLine($"x^(1/6):681.292069057961285|49798817963002");   
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000, 2)}");                                    Console.WriteLine($"x^(1/2):100000000.00000000|000000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000, 3)}");                                    Console.WriteLine($"x^(1/3):215443.46900318937|217592935665194");  
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000, 4)}");                                    Console.WriteLine($"x^(1/4):10000.000000000000|000000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000, 5)}");                                    Console.WriteLine($"x^(1/5):1584.8931924611134|852021013733915");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000000, 6)}");                                    Console.WriteLine($"x^(1/6):464.15888336127788|924100763509194"); 
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000, 2)}");                                     Console.WriteLine($"x^(1/2):31622776.60168379|3319988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000, 3)}");                                     Console.WriteLine($"x^(1/3):100000.0000000000|0000000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000, 4)}");                                     Console.WriteLine($"x^(1/4):5623.413251903490|8039495103977648");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000, 5)}");                                     Console.WriteLine($"x^(1/5):1000.000000000000|0000000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000000, 6)}");                                     Console.WriteLine($"x^(1/6):316.2277660168379|3319988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000, 2)}");                                      Console.WriteLine($"x^(1/2):10000000.0000000|00000000000000000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000, 3)}");                                      Console.WriteLine($"x^(1/3):46415.8883361278|88924100763509194");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000, 4)}");                                      Console.WriteLine($"x^(1/4):3162.27766016837|93319988935444327");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000, 5)}");                                      Console.WriteLine($"x^(1/5):630.957344480194|24943436013662234");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000000, 6)}");                                      Console.WriteLine($"x^(1/6):215.443469003189|37217592935665194");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000, 2)}");                                       Console.WriteLine($"x^(1/2):3162277.6601683|8");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000, 3)}");                                       Console.WriteLine($"x^(1/3):21544.346900318|8");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000, 4)}");                                       Console.WriteLine($"x^(1/4):1778.2794100389|2");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000, 5)}");                                       Console.WriteLine($"x^(1/5):398.10717055349|7");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000000, 6)}");                                       Console.WriteLine($"x^(1/6):146.77992676220|7");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000, 2)}");                                        Console.WriteLine($"x^(1/2):1000000.000000|00");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000, 3)}");                                        Console.WriteLine($"x^(1/3):100000.0000000|00");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000, 4)}");                                        Console.WriteLine($"x^(1/4):1000.000000000|00");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000, 5)}");                                        Console.WriteLine($"x^(1/5):251.1886431509|58");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000000, 6)}");                                        Console.WriteLine($"x^(1/6):100.0000000000|00");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000, 2)}");                                         Console.WriteLine($"x^(1/2):316227.766016|838");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000, 3)}");                                         Console.WriteLine($"x^(1/3):4641.58883361|278");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000, 4)}");                                         Console.WriteLine($"x^(1/4):562.341325190|349");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000, 5)}");                                         Console.WriteLine($"x^(1/5):158.489319246|111");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000000, 6)}");                                         Console.WriteLine($"x^(1/6):68.1292069057|961");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000, 2)}");                                          Console.WriteLine($"x^(1/2):100000.00000|0000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000, 3)}");                                          Console.WriteLine($"x^(1/3):2154.4346900|3188");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000, 4)}");                                          Console.WriteLine($"x^(1/4):316.22776601|6838");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000, 5)}");                                          Console.WriteLine($"x^(1/5):100.00000000|0000");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000000, 6)}");                                          Console.WriteLine($"x^(1/6):46.415888336|1278");       
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000, 2)}");                                           Console.WriteLine($"x^(1/2):31622.7766|016838");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000, 3)}");                                           Console.WriteLine($"x^(1/3):1000.000000|00000");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000, 4)}");                                           Console.WriteLine($"x^(1/4):177.8279410|03892");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000, 5)}");                                           Console.WriteLine($"x^(1/5):63.0957344|480193");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000000, 6)}");                                           Console.WriteLine($"x^(1/6):31.6227766|016838");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000, 2)}");                                             Console.WriteLine($"x^(1/2):10000.0000|0");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000, 3)}");                                             Console.WriteLine($"x^(1/3):464.15888|34");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000, 4)}");                                             Console.WriteLine($"x^(1/4):100.000000|0");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000, 5)}");                                             Console.WriteLine($"x^(1/5):39.810717|06");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000000, 6)}");                                             Console.WriteLine($"x^(1/6):21.5443469|0");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000, 2)}");                                              Console.WriteLine($"x^(1/2):3162.277|66");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000, 3)}");                                              Console.WriteLine($"x^(1/3):215.44346|9 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000, 4)}");                                              Console.WriteLine($"x^(1/4):56.23413|252");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000, 5)}");                                              Console.WriteLine($"x^(1/5):25.118864|32");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000000, 6)}");                                              Console.WriteLine($"x^(1/6):14.677992|68");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000, 2)}");                                               Console.WriteLine($"x^(1/2):1000.000|00 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000, 3)}");                                               Console.WriteLine($"x^(1/3):100.0000|00 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000, 4)}");                                               Console.WriteLine($"x^(1/4):31.6227|766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000, 5)}");                                               Console.WriteLine($"x^(1/5):15.848931|92");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000000, 6)}");                                               Console.WriteLine($"x^(1/6):10.00000|0  ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000, 2)}");                                                Console.WriteLine($"x^(1/2):316.22|7766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000, 3)}");                                                Console.WriteLine($"x^(1/3):46.415|88834");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000, 4)}");                                                Console.WriteLine($"x^(1/4):17.7827|941 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000, 5)}");                                                Console.WriteLine($"x^(1/5):10.0000|000 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100000, 6)}");                                                Console.WriteLine($"x^(1/6):6.8129|20691");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000, 2)}");                                                 Console.WriteLine($"x^(1/2):100.00|0000 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000, 3)}");                                                 Console.WriteLine($"x^(1/3):21.544|3469 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000, 4)}");                                                 Console.WriteLine($"x^(1/4):10.000|0000 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000, 5)}");                                                 Console.WriteLine($"x^(1/5):6.309|573445");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10000, 6)}");                                                 Console.WriteLine($"x^(1/6):4.641|588834");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000, 2)}");                                                  Console.WriteLine($"x^(1/2):31.6|227766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000, 3)}");                                                  Console.WriteLine($"x^(1/3):10.00|00000 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000, 4)}");                                                  Console.WriteLine($"x^(1/4):5.62|3413252");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000, 5)}");                                                  Console.WriteLine($"x^(1/5):3.98|1071706");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(1000, 6)}");                                                  Console.WriteLine($"x^(1/6):3.16|227766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100, 2)}");                                                   Console.WriteLine($"x^(1/2):10.0|000000 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100, 3)}");                                                   Console.WriteLine($"x^(1/3):4.6|41588834");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100, 4)}");                                                   Console.WriteLine($"x^(1/4):3.1|6227766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100, 5)}");                                                   Console.WriteLine($"x^(1/5):2.51|1886432");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(100, 6)}");                                                   Console.WriteLine($"x^(1/6):2.15|443469 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10, 2)}");                                                    Console.WriteLine($"x^(1/2):3.|16227766 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10, 3)}");                                                    Console.WriteLine($"x^(1/3):2.1|5443469 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10, 4)}");                                                    Console.WriteLine($"x^(1/4):1.7|7827941 ");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10, 5)}");                                                    Console.WriteLine($"x^(1/5):1.5|84893192");
        Console.WriteLine($"result: {BigFloat.NthRoot_INCOMPLETE_DRAFT_10(10, 6)}");                                                    Console.WriteLine($"x^(1/6):1.4|67799268");





















































































        Stopwatch timer = Stopwatch.StartNew();
        BigFloat result = BigFloat.NthRoot_INCOMPLETE_DRAFT9(new BigFloat((ulong)3 << 60, -60), 3);
        Console.WriteLine($"NthRootDRAFT {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");

        result = BigFloat.NthRoot_INCOMPLETE_DRAFT9(new BigFloat((BigInteger)3 << 200, -200), 3);
        Console.WriteLine($"NthRootDRAFT {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");

        timer.Stop();
        timer.Reset();

        for (int i = 2; i >= 0; i--)
            for (int m = 7; m < 300; m *= 31)
                for (int e = 5; e < 10; e++)
                {
                    BigFloat bf = new((ulong)(m) << 60, -60);
                    //timer = Stopwatch.StartNew();
                    timer.Restart();
                    timer.Start();
                    BigFloat result2 = BigFloat.NthRoot_INCOMPLETE_DRAFT9(bf, e);
                    timer.Stop();
                    if (i == 0) Console.WriteLine($"{m}^(1/{e}) = {result2}  correct:{double.Pow((double)bf, 1 / (double)e)}  ticks {timer.ElapsedTicks}");
                }

        Console.WriteLine(BigFloat.NthRoot_INCOMPLETE_DRAFT_BF(100000000000, 5));
    }

    //////////////  Pow() Play Area & Examples //////////////
    private static void Pow_Stuff()
    {


        //// BigFloat.Zero  BigFloat.One
        //IsTrue(BigFloat.Pow(BigFloat.Zero, 0) == 1, $"Failed on: 0^0");
        //IsTrue(BigFloat.Pow(BigFloat.One, 0) == 1, $"Failed on: 1^0");
        //IsTrue(BigFloat.Pow(0, 0) == 1, $"Failed on: 0^0");
        //IsTrue(BigFloat.Pow(1, 0) == 1, $"Failed on: 1^0");
        //IsTrue(BigFloat.Pow(2, 0) == 1, $"Failed on: 2^0");
        //IsTrue(BigFloat.Pow(3, 0) == 1, $"Failed on: 3^0");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 1) == 0, $"Failed on: 0^1");
        //IsTrue(BigFloat.Pow(BigFloat.One, 1) == 1, $"Failed on: 1^1");
        //IsTrue(BigFloat.Pow(0, 1) == 0, $"Failed on: 0^1");
        //IsTrue(BigFloat.Pow(1, 1) == 1, $"Failed on: 1^1");
        //IsTrue(BigFloat.Pow(2, 1) == 2, $"Failed on: 2^1");
        //IsTrue(BigFloat.Pow(3, 1) == 3, $"Failed on: 3^1");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 2) == 0, $"Failed on: 0^2");
        //IsTrue(BigFloat.Pow(BigFloat.One, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(0, 2) == 0, $"Failed on: 0^2");
        IsTrue(BigFloat.Pow(1, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(2, 2) == 4, $"Failed on: 2^2");
        IsTrue(BigFloat.Pow(3, 2) == 9, $"Failed on: 3^2");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 3) == 0, $"Failed on: 0^3");
        //IsTrue(BigFloat.Pow(BigFloat.One, 3) == 1, $"Failed on: 1^3");
        IsTrue(BigFloat.Pow(0, 3) == 0, $"Failed on: 0^3");
        IsTrue(BigFloat.Pow(1, 3) == 1, $"Failed on: 1^3");
        IsTrue(BigFloat.Pow(2, 3) == 8, $"Failed on: 2^3");
        IsTrue(BigFloat.Pow(3, 3) == 27, $"Failed on: 3^3");



        for (int k = 3; k < 20; k++)
        {
            for (double i = 1; i < 20; i = 1 + (i * 1.7))
            {
                BigFloat exp2 = (BigFloat)double.Pow(i, k);
                BigFloat res2 = BigFloat.Pow((BigFloat)i, k);
                IsTrue(res2 == exp2, $"Failed on: {i}^{k}, result:{res2} exp:{exp2}");
            }
        }

        BigFloat tbf = new(255, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(256, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(511, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(512, 0);
        _ = BigFloat.Pow(tbf, 3);
    }

    private static void PowMostSignificantBits_Stuff()
    {
        Stopwatch timer = Stopwatch.StartNew();
        long errorTotal = 0; // too high or low by 2 or more

        Parallel.For(2, 8192, bitSize =>
        //for (int bitSize = 2; bitSize < 1026; bitSize += 1)
        {

            int exp = 0;
            BigInteger miss = 0;
            long correctBits = 0;
            BigInteger ans = 0;

            long counter = 0;
            long oneTooHi = 0; // too high by 1
            long oneTooLo = 0; // too low  by 1
            Random random = new(bitSize + 1);
            for (int valTries = 0; valTries < 10; valTries++)
            {
                BigInteger val = GenerateRandomBigInteger(bitSize);
                int valSize = (int)val.GetBitLength();
                for (int expSize = 2; expSize < 12; expSize++)
                {
                    int wantedBits;
                    //int wantedBits = valSize;  
                    for (wantedBits = 1; wantedBits < (valSize + 2); wantedBits++)
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
                        bool overflowed = BigIntegerTools.RightShiftWithRoundWithCarryDownsize(out ans, p, shiftedAns);
                        if (overflowed) shiftedAns++;
                        if (val.IsZero) shiftedAns = 0;

                        // Result Setup
                        timer.Restart();
                        BigInteger res = BigIntegerTools.PowMostSignificantBits(val, exp, out int shiftedRes, valSize, wantedBits, false);
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
                                ans = BigIntegerTools.RightShiftWithRound(ans, shiftedRes - shiftedAns);
                            }
                            else
                            {
                                Console.WriteLine($"ERRORRRRR - Answer should always be at least 2 bits larger");
                            }
                        }

                        miss = ans - res;
                        correctBits = ans.GetBitLength() - miss.GetBitLength();
                        //Console.Write($", {valSize}, {(val.IsEven?1:0)}, {exp}, {(exp % 2 == 0 ? 1 : 0)}, {expSize}, wantedBits:{wantedBits}, got:{correctBits} ({ans.GetBitLength()} - {diff.GetBitLength()})  miss:({wantedBits- correctBits})\r\n");

                        Interlocked.Increment(ref counter);

                        if (BigInteger.Abs(miss) > 1)
                        {
                            Console.Write($"!!!!!!! diff:{miss,-2}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref errorTotal);
                        }
                        else if (miss > 0)
                        {
                            //Console.Write($"OK but ans 1 too Low, valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref oneTooLo);
                        }
                        else if (miss < 0)
                        {
                            Console.Write($"OK but ans 1 too High,  valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref oneTooHi);
                        }
                    }
                    if (val % 8192 == 777)
                        Console.Write($"sz:{bitSize,3},diff:{miss,-3}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-9}[{expSize,-2}] wantedBits:{wantedBits,-4}" +
                                        $" got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} count:{counter,-7}(Lo({(float)oneTooLo / counter,-4:F5} hi{(float)(oneTooHi /*/ (float)counter*/),-1})" +
                                        $" time:{(timer.ElapsedTicks),3} val:{val}\r\n");

                }
            }

            //if (errorTotal > 0 || oneTooLo > 0 || oneTooHi > 0)
            //    Console.WriteLine($"errorTotal:{errorTotal} oneTooLo:{oneTooLo} oneTooHi:{oneTooHi}");
        });


        static BigInteger GenerateRandomBigInteger(int maxNumberOfBits)
        {
            byte[] data = new byte[(maxNumberOfBits / 8) + 1];
            Random.Shared.NextBytes(data);
            data[^1] >>= 8 - (maxNumberOfBits % 8);
            return new(data, true);
        }
    }

    private static void Pow_Stuff3()
    {
        Stopwatch timer = new();
        timer.Start();

        int countNotExact = 0, wayLow = 0, minus2 = 0, minus1 = 0, plus1 = 0, plus2 = 0, plus3 = 0, wayHigh = 0, total = 0;
        //for (BigInteger val = 3; val < 3000; val += 1)
        //int b = 0;
        for (int b = 0; b < 99999999; b += 7)
        {
            timer.Reset();
            for (BigInteger val = BigInteger.One << b; val < (BigInteger.One << (b + 1)); val += BigInteger.Max(BigInteger.One, BigInteger.One << (b - 6 + 1)))
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
                    BigInteger res = BigIntegerTools.PowMostSignificantBits(val, (int)exp, out int shifted, valSize);
                    timer.Stop();
                    //BigInteger ans = 0; int shiftedAns =0;
                    BigInteger ans = BigIntegerTools.PowMostSignificantBits(val, (int)exp, out int shiftedAns, valSize);
                    //BigInteger ans = BigFloat.PowAccurate(val, valSize, exp, out int shiftedAns);
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
                            Console.WriteLine($"wayHigh:{wayHigh} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res < ans)
                        {
                            wayLow++;
                            Console.WriteLine($"wayLow:{wayLow} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else
                        {
                            Console.WriteLine($"??????????? shifted({shifted}) != shiftedAns({shiftedAns}) ?????????????? m= val={val} pow={exp}");
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
    }

    private static void Pow_Stuff4()
    {
        for (BigFloat val = 777; val < 778; val++)
            for (int exp = 5; exp < 6; exp++)
            {
                BigFloat res = BigFloat.Pow(val, exp);
                double correct = Math.Pow((double)val, exp);
                Console.WriteLine($"{val,3}^{exp,2} = {res,8} ({res.Int,4} << {res.Scale,2})  Correct: {correct,8}");
            }
    }

    private static void ToStringHexScientific_Stuff()
    {
        //todo: test
        //  "251134829809281403347287120873437924350329252743484439244628997274301027607406903709343370034928716748655001465051518787153237176334136103968388536906997846967216432222442913720806436056149323637764551144212026757427701748454658614667942436236181162060262417445778332054541324179358384066497007845376000000000, 0x596:82F00000[11+32=43],  << 1014"
        //  with BigFloat Pow(BigFloat value, 4)

        (new BigFloat("1.8814224e11")).DebugPrint("1.8814224e11"); //18814224____
        (new BigFloat("-10000e4")).DebugPrint("-10000e4");
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
        IsTrue(new BigFloat("1.0e-1") == new BigFloat("0.10"), $"Failed on: 3^0");
        IsTrue(new BigFloat("0.10e2") == new BigFloat("10."), $"Failed on: 3^0");
        IsTrue(new BigFloat("0.0010e2") == new BigFloat("0.10"), $"Failed on: 3^0");
        IsTrue(new BigFloat("1.00e0") == new BigFloat("1.00"), $"Failed on: 3^0");
        IsTrue(new BigFloat("10.0e-1") == new BigFloat("1.00"), $"Failed on: 3^0");
        IsTrue(new BigFloat("100.0e-2") == new BigFloat("1.000"), $"Failed on: 3^0");
        IsTrue(new BigFloat("300.0e-2") == new BigFloat("3.000"), $"Failed on: 3^0");
        IsTrue(new BigFloat("300.0e-2") == new BigFloat("0.03000e+2"), $"Failed on: 3^0");
        IsTrue(new BigFloat("1.0000000e+8") == new BigFloat("10000000e1"), $"Failed on: 3^0");
        IsTrue(new BigFloat("10000e4").ToStringHexScientific() == "17D7 << 14");
    }

    private static void GeneratePi_Stuff()
    {
        for (int i = 400000; i <= 400000; i += 4)
        {
            Stopwatch timer = Stopwatch.StartNew();
            BigFloat res2 = BigFloat.BigConstants.GeneratePi(i);
            timer.Stop();
            Console.WriteLine($"{i,4} {res2.ToString("")} {timer.ElapsedTicks}");
        }
        Console.WriteLine($"perfect: 3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679821480865132823066470938446095505822317253594081284811174502841027019385211055596446229");
    }

    private static void Parse_Stuff()
    {
        BigFloat aa = BigFloat.Parse("0b100000.0");
        BigFloat bb = BigFloat.Parse("0b100.0");
        BigFloat rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b100000");
        bb = BigFloat.Parse("0b100.0");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b100000.0");
        bb = BigFloat.Parse("0b100");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b10000.0");
        bb = BigFloat.Parse("0b1000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.00000");
        bb = BigFloat.Parse("0b10000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.0");
        bb = BigFloat.Parse("0b10000.000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1.0000");
        bb = BigFloat.Parse("0b10000000.");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.00000000000000000000000000");
        bb = BigFloat.Parse("0b10000.0000000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");
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
        BigFloat res = BigFloat.Remainder(bf, new BigFloat("1.00000000")); // i.e. "bf % 1;"
        // Answer of 0.00390625 % 1 is:
        //   0.00390625 or 0.0039063 or 0.003906 or 0.00391 or 0.0039 or 0.004 or 0.00 or or 0.0 or or 0

        IsTrue(res == new BigFloat("0.004"), "0.004");
        IsTrue(res == new BigFloat("0.0039"), "0.0039");
        IsTrue(res == new BigFloat("0.00391"), "0.00391");
        IsTrue(res == new BigFloat("0.003906"), "0.003906");
        IsTrue(res == new BigFloat("0.0039063"), "0.0039063");
        IsTrue(res == new BigFloat("0.00390625"), "0.00390625");

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

    private static void Sqrt_Stuff()
    {
        BigFloat inpParam = new("2.0000000000000000000000000000000");
        BigFloat fnOutput = BigFloat.Sqrt(inpParam);
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
        bool success = BigFloat.TryParse(origStingValue, out BigFloat bf);
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

    private static void IsNotEqual(object v1, object v2, string msg = null)
    {
        if (!v1.Equals(v2))
        {
            if (msg != null)
            {
                Console.WriteLine(msg, v1, v2);
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }

    /// <summary>
    /// Takes an inputParam and inputFunc and then checks if the results matches the expectedOutput.
    /// </summary>
    /// <param name="inputParam">The input value to apply to the inputFunc.</param>
    /// <param name="inputFunc">The function that is being tested.</param>
    /// <param name="expectedOutput">What the output of inputFunc(inputParam) should be like.</param>
    /// <param name="msg">If they don't match, output this message. Use {0}= input, {1}=results of inputFunc(inputParam) {2}=the value it should be.
    /// Example: "The input value of {0} with the given function resulted in {1}, however the value of {2} was expected."</param>
    //[DebuggerHidden]
    private static void IsNotEqual(string inputParam, Func<string, object> inputFunc, string expectedOutput, string msg = null)
    {
        string a = inputFunc(inputParam).ToString();
        if (!a.Equals(expectedOutput))
        {
            if (msg != null)
            {
                Console.WriteLine(msg, inputParam, a, expectedOutput);
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
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
    private static void Fail(string msg)
    {
        Console.WriteLine(msg ?? "Failed");

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}
