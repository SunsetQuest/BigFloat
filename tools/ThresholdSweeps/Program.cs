using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace ThresholdSweeps;

internal static class Program
{
    private static readonly int[] MultiplicationSizes = new[] { 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144 };
    private static readonly int[] DivisionSizes = new[] { 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144 };

    private const int MultiplyIterations = 300;
    private const int DivisionIterations = 200;

    public static void Main()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < 2; i++)
        {
            sb.Clear();
            sb.AppendLine($"# Threshold sweep ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription})");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            var rng = Random.Shared;

            sb.AppendLine("## Karatsuba vs schoolbook multiplication");
            sb.AppendLine("Bit length | Schoolbook mean (ms) | Karatsuba mean (ms)");
            sb.AppendLine("---|---:|---:");

            foreach (int bits in MultiplicationSizes)
            {
                var (aLimbs, bLimbs) = CreateOperands(bits, rng);
                double karatsuba, schoolbook;
                if (rng.Next(2) == 0)
                {
                    karatsuba = Time(() => MultiplyKaratsuba(aLimbs, bLimbs), MultiplyIterations);
                    schoolbook = Time(() => MultiplySchoolbook(aLimbs, bLimbs), MultiplyIterations);
                }
                else
                {
                    schoolbook = Time(() => MultiplySchoolbook(aLimbs, bLimbs), MultiplyIterations);
                    karatsuba = Time(() => MultiplyKaratsuba(aLimbs, bLimbs), MultiplyIterations);
                }


                sb.AppendLine($"{bits} | {schoolbook:F3} | {karatsuba:F3}");
            }

            sb.AppendLine();
            sb.AppendLine("## Burnikel–Ziegler-style division vs basic shift-subtract");
            sb.AppendLine("Bit length | Shift/subtract mean (ms) | DivRem mean (ms)");
            sb.AppendLine("---|---:|---:");

            foreach (int bits in DivisionSizes)
            {
                var dividend = CreateBigInteger(bits, rng);
                var divisor = CreateBigInteger(bits - 3, rng) | 1; // ensure non-zero, smaller divisor

                double shiftSubtract, divRem;
                if (rng.Next(2) == 0)
                {
                    divRem = Time(() => BigInteger.DivRem(dividend, divisor, out _), DivisionIterations * 3);
                    shiftSubtract = Time(() => ShiftSubtractDivide(dividend, divisor), DivisionIterations);
                }
                else
                {
                    shiftSubtract = Time(() => ShiftSubtractDivide(dividend, divisor), DivisionIterations);
                    divRem = Time(() => BigInteger.DivRem(dividend, divisor, out _), DivisionIterations * 3);
                }


                sb.AppendLine($"{bits} | {shiftSubtract:F3} | {divRem:F3}");
            }
        }

        string output = sb.ToString();
        Console.WriteLine(output);

        string tfm = typeof(Program).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "unknown";
        string fileName = $"threshold-sweeps-{tfm.Replace(".", string.Empty).Replace(",", string.Empty).Replace("=", "-").Replace(" ", "-")}.md";
        string outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "benchmarks", fileName));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, output);
    }

    private static double Time(Action action, int iterations)
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds*1000) / iterations;
    }

    private static (uint[] A, uint[] B) CreateOperands(int bits, Random rng)
    {
        return (ToLimbs(CreateBigInteger(bits, rng)), ToLimbs(CreateBigInteger(bits - 1, rng)));
    }

    private static BigInteger CreateBigInteger(int bits, Random rng)
    {
        int byteCount = (bits + 7) / 8;
        byte[] bytes = new byte[byteCount + 1];
        rng.NextBytes(bytes);
        int excessBits = (8 * byteCount) - bits;
        if (excessBits > 0)
        {
            byte mask = (byte)((1 << (8 - excessBits)) - 1);
            bytes[^2] &= mask;
        }

        bytes[^2] |= 0x80; // ensure the number stays close to the requested width
        bytes[^1] = 0; // positive
        return new BigInteger(bytes);
    }

    private static uint[] ToLimbs(BigInteger value)
    {
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        int limbCount = (bytes.Length + 3) / 4;
        uint[] limbs = new uint[limbCount];
        Buffer.BlockCopy(bytes, 0, limbs, 0, bytes.Length);
        return Trim(limbs);
    }

    private static BigInteger FromLimbs(uint[] limbs)
    {
        limbs = Trim(limbs);
        byte[] bytes = new byte[limbs.Length * 4 + 1];
        Buffer.BlockCopy(limbs, 0, bytes, 0, limbs.Length * 4);
        return new BigInteger(bytes);
    }

    private static uint[] Trim(uint[] limbs)
    {
        int last = limbs.Length - 1;
        while (last > 0 && limbs[last] == 0)
        {
            last--;
        }

        if (last == limbs.Length - 1)
        {
            return limbs;
        }

        uint[] trimmed = new uint[last + 1];
        Array.Copy(limbs, trimmed, trimmed.Length);
        return trimmed;
    }

    private static BigInteger MultiplySchoolbook(uint[] a, uint[] b)
    {
        uint[] result = new uint[a.Length + b.Length];

        for (int i = 0; i < a.Length; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < b.Length; j++)
            {
                ulong existing = result[i + j];
                ulong product = (ulong)a[i] * b[j] + existing + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            if (carry != 0)
            {
                result[i + b.Length] += (uint)carry;
            }
        }

        return FromLimbs(result);
    }

    private static BigInteger MultiplyKaratsuba(uint[] a, uint[] b)
    {
        int n = Math.Max(a.Length, b.Length);
        if (n <= 32)
        {
            return MultiplySchoolbook(a, b);
        }

        int m = (n + 1) / 2;
        var (aLow, aHigh) = Split(a, m);
        var (bLow, bHigh) = Split(b, m);

        BigInteger z0 = MultiplyKaratsuba(aLow, bLow);
        BigInteger z2 = MultiplyKaratsuba(aHigh, bHigh);
        BigInteger z1 = MultiplyKaratsuba(Add(aLow, aHigh), Add(bLow, bHigh)) - z2 - z0;

        BigInteger result = (z2 << (64 * m)) + (z1 << (32 * m)) + z0;
        return result;
    }

    private static (uint[] Low, uint[] High) Split(uint[] limbs, int split)
    {
        uint[] low = new uint[Math.Min(split, limbs.Length)];
        Array.Copy(limbs, 0, low, 0, low.Length);

        if (limbs.Length <= split)
        {
            return (low, Array.Empty<uint>());
        }

        int highLen = limbs.Length - split;
        uint[] high = new uint[highLen];
        Array.Copy(limbs, split, high, 0, highLen);
        return (Trim(low), Trim(high));
    }

    private static uint[] Add(uint[] a, uint[] b)
    {
        int len = Math.Max(a.Length, b.Length);
        uint[] result = new uint[len + 1];
        ulong carry = 0;
        for (int i = 0; i < result.Length - 1; i++)
        {
            ulong left = i < a.Length ? a[i] : 0;
            ulong right = i < b.Length ? b[i] : 0;
            ulong sum = left + right + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        result[^1] = (uint)carry;
        return Trim(result);
    }

    private static BigInteger ShiftSubtractDivide(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
        {
            return BigInteger.Zero;
        }

        BigInteger quotient = BigInteger.Zero;
        int shift = (int)numerator.GetBitLength() - (int)denominator.GetBitLength();
        if (shift < 0)
        {
            return BigInteger.Zero;
        }

        BigInteger shiftedDivisor = denominator << shift;
        for (int i = shift; i >= 0; i--)
        {
            if (numerator >= shiftedDivisor)
            {
                numerator -= shiftedDivisor;
                quotient |= BigInteger.One << i;
            }
            shiftedDivisor >>= 1;
        }

        return quotient;
    }
}
