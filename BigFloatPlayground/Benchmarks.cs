// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using BigFloatLibrary;
using System.Diagnostics;
using System.Numerics;
using static BigFloatLibrary.BigFloat;

namespace PlaygroundAndShowCase;

public static class Benchmarks
{
    public static void Inverse_Benchmark()
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

    public static void NthRoot_Benchmark()
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
            BigInteger result = BigIntegerTools.NthRoot(val, n);
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

    public static void NthRoot_Benchmark2()
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

    //////////////  Pow() Benchmarks //////////////
    public static void Pow_Benchmark()
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
                for (uint exp = 3; exp < maxPowSearch; exp++)
                {
                    //timer.Restart();
                    total++;
                    if (total % 1000000 == 0)
                    {
                        Console.Write($"TOTAL:{total,-10} err:{countNotExact,-7}({(float)countNotExact / total,5:F9}%)  ({wayLow},{minus2},{minus1} - {plus1},{plus2},{plus3},{wayHigh})\r\n");
                    }

                    BigInteger testValue = BigInteger.Pow(val, (int)exp);
                    int outputSize = (int)(exp * valSize);

                    // Answer Setup version 1
                    int keep = (int)Math.Min(outputSize / 5, 600000000);
                    //int ansShiftedBy = (int)Math.Max(0, (int)(outputSize - keep));
                    //BigInteger ans = BigFloat.RightShiftWithRound(testValue, ansShiftedBy);
                    //(BigInteger ans, int ansShiftedBy) = BigFloat.PowMostSignificantBits(val, valSize, (int)exp, outputSize, true);

                    // Answer Setup version 2
                    int shifted; BigInteger ans;
                    //fastPowMostSignificantBits(val, valSize, exp, wantedBits, out ans, out shifted);

                    //// Answer Setup version 3
                    //ans = BigFloat.PowMostSignificantBits(BigInteger, BigInteger, BigInteger, int, out int, int) {var testing}
                    //BigInteger ans = BigInteger.Pow(val, (int)exp);
                    int removeBits = outputSize - keep;
                    if (removeBits > 0)
                    {
                        (ans, bool overflowed) = BigIntegerTools.RoundingRightShiftWithCarry(testValue, removeBits);
                        shifted = Math.Max(0, removeBits);
                        if (overflowed)
                        {
                            shifted++;
                        }
                    }
                    else
                    {
                        ans = testValue;
                        shifted = 0;
                    }

                    // Result Setup
                    // timer.Restart();
                    (BigInteger res, int shiftedRes) = BigIntegerTools.PowMostSignificantBitsApprox(val, (int)exp, valSize, keep, false);
                    // timer.Stop();

                    // Let ensure if they overflow they both overflow the same amount.
                    //BigInteger diff = res - ans;
                    long miss;
                    //int correctBits = 0;
                    BigInteger miss2;

                    if (shifted > outputSize)
                    {
                        Console.WriteLine($"{ans} is too small (by a lot) shifted by {shifted}  size:{valSize} exp:{exp}");
                        continue;
                    }

                    if (shiftedRes < shifted - 1 || shiftedRes > shifted + 1)
                    {
                        Console.WriteLine($"shiftedRes({shiftedRes}) is out of range {shifted}");
                    }

                    // try remove 1 more bit
                    if (shiftedRes == shifted - 1)
                    {
                        res >>= 1;
                    }

                    // try add 1 more bit
                    if (shiftedRes == shifted + 1)
                    {
                        ans >>= 1;
                    }

                    miss2 = ans - res;
                    int keep2 = (int)Math.Min(outputSize / 8, 600000000);
                    int removeBits2 = outputSize - keep2;
                    if (removeBits2 > 0)
                    {
                        miss2 >>= removeBits2;
                    }
                    else
                    {
                        Console.WriteLine($"outputSize is out of range {outputSize}");
                    }

                    miss = (long)miss2;

                    switch (miss)
                    {
                        case > 2:
                            //Console.WriteLine($" res is too small! val:{val.ToString()} exp:{exp}");
                            wayLow++;
                            break;
                        case 2:
                            plus2++;
                            break;
                        case 1:
                            plus1++;
                            break;
                        case 0:
                            break;
                        case -1:
                            minus1++;
                            break;
                        case -2:
                            minus2++;
                            break;
                        case -3:
                            plus3++;
                            break;
                        default:
                            //Console.WriteLine($" res is too high! val:{val.ToString()} exp:{exp}");
                            wayHigh++;
                            break;
                    }

                    if (miss != 0)
                    {
                        countNotExact++;
                    }

                    //int needToShiftAgainBy2 = (int)(res.GetBitLength() - keep);
                    //res = BigFloat.RightShiftWithRound(res, needToShiftAgainBy2); shifted += needToShiftAgainBy2;
                }
            }
        }

        timer.Stop();
        Console.WriteLine();
        Console.WriteLine($"({(float)countNotExact / (float)total,1:F2}%) countNotExact:{countNotExact} Total:{total}  ({wayLow},{minus2},{minus1} - {plus1},{plus2},{plus3},{wayHigh})");
        Console.WriteLine($"Total Time: {timer.ElapsedMilliseconds}");
    }

    public static void PowMostSignificantBits_Benchmark()
    {
        Stopwatch timer = new();
        int errorTotal = 0;
        BigInteger miss = 0;
        int exp = 0;
        long correctBits = 0;
        long counter = 0;
        long oneTooHi = 0; // too high by 1
        long oneTooLo = 0; // too low  by 1
        for (int bitSize = 8; bitSize < 9; bitSize++)
        {
            BigInteger val; // = (BigInteger)long.MaxValue;// (BigInteger)419;// BigInteger.Pow(2, i) - 1 + i;
            long errorCount = 0;

            int expSize = bitSize / 2;
            if (bitSize < 12)
            {
                expSize = 4;
            }

            for (int i = 0; i < 100; i++)
            {
                val = GenerateRandomBigInteger(bitSize);
                int valSize = (int)val.GetBitLength();

                long correctBits2 = 0;

                for (int y = 0; y < 10; y++)
                {
                    BigInteger diff;
                    long errorCount2 = 0;

                    for (exp = 3; exp < 8; exp++)
                    {
                        int workSize = valSize * exp;

                        int workSize2 = (int)GenerateRandomBigInteger(expSize) * 8;
                        workSize2 = (int)Math.Min(workSize2, valSize * exp);

                        int wantedBits = Math.Max(0, workSize - workSize2);

                        (BigInteger res, int shiftedRes) = BigIntegerTools.PowMostSignificantBitsApprox(val, exp, valSize, wantedBits, false);

                        BigInteger answer;

                        int keep = Math.Min((int)GenerateRandomBigInteger(expSize), valSize) + 8;

                        // Answer Setup version 2
                        answer = BigInteger.Pow(val, exp);
                        int shifted = (int) answer.GetBitLength() - keep;
                        answer >>= shifted;
                        //answer = BigFloat.RightShiftWithRound(answer, shifted);

                        // Remove the unneeded bits that have become 0
                        int needToShiftAgain = (int)(answer.GetBitLength() - wantedBits);
                        answer >>= needToShiftAgain;
                        shifted += needToShiftAgain;

                        // Sometimes the lower level PowMostSignificantBits rounds up an extra bit, let compensate it here.
                        if (shiftedRes == shifted - 1)
                        {
                            res >>= 1;
                        }
                        else if (shiftedRes == shifted + 1)
                        {
                            answer >>= 1;
                        }

                        diff = answer - res;
                        correctBits2 = answer.GetBitLength() - diff.GetBitLength();
                        errorCount2++;
                    }

                    _ = Interlocked.Add(ref errorCount, errorCount2);
                    _ = Interlocked.Add(ref correctBits, correctBits2);
                }

                if (val % 256 == 99)
                {
                    Console.WriteLine($"count:{counter,-6} exp:{exp,-2} diff:{miss,-5}[{miss.GetBitLength()}] valSize:{valSize,-4} bits:{bitSize,-2} {val.IsEven}");
                }
            }

            int wantedBits3 = 100;
            BigInteger val3 = (BigInteger)long.MaxValue;// (BigInteger)419;// BigInteger.Pow(2, i) - 1 + i;
            int valSize3 = (int)val3.GetBitLength();

            for (exp = 3; exp < 8; exp++)
            {
                // Answer answer version 1
                //BigInteger ans = BigFloat.PowMostSignificantBits(val, valSize, exp, out int shifted, workSize, false);

                // Answer Setup version 2
                BigInteger p = BigInteger.Pow(val3, exp);
                int shifted = Math.Max(0, (int)(p.GetBitLength() - Math.Min(wantedBits3, valSize3)));
                (BigInteger answer, bool overflowed) = BigIntegerTools.RoundingRightShiftWithCarry(p, shifted);
                if (overflowed)
                {
                    shifted++;
                }

                if (val3.IsZero)
                {
                    shifted = 0;
                }

                // Result Setup
                timer.Restart();
                (BigInteger res, int shiftedRes) = BigIntegerTools.PowMostSignificantBitsApprox(val3, exp, valSize3, wantedBits3, false);
                timer.Stop();
                //int needToShiftAgainBy2 = (int)(res.GetBitLength() - wantedBits);
                //res = BigFloat.RightShiftWithRound(res, needToShiftAgainBy2); shifted += needToShiftAgainBy2;
                //BigInteger res4 = BigFloat.Pow4(val, valSize, exp, out int shifted4, workSize);

                if (shifted < 0)
                {
                    Console.WriteLine($"Error - depending on the mode a negative shiftedAns is not supported ");
                }

                if (shifted - shiftedRes == 1)  // res did not round up
                {
                    answer <<= 1;
                }
                else if (shifted - shiftedRes == -1)  // answer did not round up
                {
                    res <<= 1;
                }
                else if (shiftedRes != shifted)
                {
                    if (shiftedRes > shifted + 1)
                    {
                        answer = BigIntegerTools.RoundingRightShift(answer, shiftedRes - shifted);
                    }
                    else
                    {
                        Console.WriteLine($"ERRORRRRR - Answer should always be at least 2 bits larger");
                    }
                }

                miss = answer - res;
                correctBits = answer.GetBitLength() - miss.GetBitLength();
                //Console.Write($", {valSize}, {(val.IsEven?1:0)}, {exp}, {(exp % 2 == 0 ? 1 : 0)}, {expSize}, wantedBits:{wantedBits}, got:{correctBits} ({answer.GetBitLength()} - {diff.GetBitLength()})  miss:({wantedBits- correctBits})\r\n");

                _ = Interlocked.Increment(ref counter);

                if (BigInteger.Abs(miss) > 1)
                {
                    Console.Write($"!!!!!!! diff:{miss,-2}[{miss.GetBitLength()}] valSize:{valSize3,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits3,-4}got:{correctBits,-4}ansSz:{answer.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val3}\r\n");
                    _ = Interlocked.Increment(ref errorTotal);
                }
                else if (miss > 0)
                {
                    //Console.Write($"OK but answer 1 too Low, valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{answer.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                    _ = Interlocked.Increment(ref oneTooLo);
                }
                else if (miss < 0)
                {
                    Console.Write($"OK but ans 1 too High,  valSize:{valSize3,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits3,-4}got:{correctBits,-4}ansSz:{answer.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val3}\r\n");
                    _ = Interlocked.Increment(ref oneTooHi);
                }
            }
            if (errorTotal > 0 || oneTooLo > 0 || oneTooHi > 0)
            {
                Console.WriteLine($"errorTotal:{errorTotal} oneTooLo:{oneTooLo} oneTooHi:{oneTooHi}");
            }

            long correctBitsAvg = correctBits / counter;
            Console.Write($"bits:{bitSize,-2} {val3.IsEven,-5} total:{errorCount,-8} correct:{correctBitsAvg,-4} exact:{(float)errorCount / counter} err({(float)errorTotal / (float)counter}, {errorTotal})\r\n");
        }

        static BigInteger GenerateRandomBigInteger(int maxNumberOfBits)
        {
            byte[] data = new byte[(maxNumberOfBits / 8) + 1];
            Random.Shared.NextBytes(data);
            data[^1] >>= 8 - (maxNumberOfBits % 8);
            return new(data, true);
        }
    }

    public static void GeneratePi_Benchmark()
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
}
