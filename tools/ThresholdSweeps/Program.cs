using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using BigFloatLibrary;

namespace ThresholdSweeps;

internal static class Program
{
    private static readonly int[] MultiplicationSizes = new[] { 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144 };
    private static readonly int[] DivisionEqualSizes = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
    private static readonly (int NumeratorBits, int DenominatorBits)[] DivisionUnbalancedSizes = new[]
    {
        (1000, 10),
        (2048, 32),
        (4096, 64)
    };

    private const int MultiplyIterations = 300;
    private const int DivisionIterations = 120;

    private static readonly Func<BigFloat, BigFloat, BigFloat> DivideSmallNumbers = CreateDivideDelegate(nameof(DivideSmallNumbers));
    private static readonly Func<BigFloat, BigFloat, BigFloat> DivideStandard = CreateDivideDelegate(nameof(DivideStandard));
    private static readonly Func<BigFloat, BigFloat, BigFloat> DivideLargeNumbers = CreateDivideDelegate(nameof(DivideLargeNumbers));

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
            sb.AppendLine("## Division (BigFloat variants)");
            AppendDivisionEqualSizeSweep(sb, rng);
            AppendDivisionUnbalancedSweep(sb, rng);
            AppendDivisionRandomSweep(sb, rng, count: 8);
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

    private static void AppendDivisionEqualSizeSweep(StringBuilder sb, Random rng)
    {
        sb.AppendLine("### Equal-size operands");
        sb.AppendLine("Bit length | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)");
        sb.AppendLine("---|---:|---:|---:");

        var results = new List<DivisionResult>(DivisionEqualSizes.Length);

        foreach (int bits in DivisionEqualSizes)
        {
            DivisionResult result = BenchmarkDivision(bits, bits, rng);
            results.Add(result);
            sb.AppendLine($"{bits} | {result.SmallMean:F3} | {result.StandardMean:F3} | {result.LargeMean:F3}");
        }

        sb.AppendLine();
        sb.AppendLine(RenderCrossoverSummary(results, includeHeader: true));
        sb.AppendLine();
    }

    private static void AppendDivisionUnbalancedSweep(StringBuilder sb, Random rng)
    {
        sb.AppendLine("### Unbalanced operands");
        sb.AppendLine("Numerator bits | Denominator bits | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)");
        sb.AppendLine("---|---|---:|---:|---:");

        foreach ((int numeratorBits, int denominatorBits) in DivisionUnbalancedSizes)
        {
            DivisionResult result = BenchmarkDivision(numeratorBits, denominatorBits, rng);
            sb.AppendLine($"{numeratorBits} | {denominatorBits} | {result.SmallMean:F3} | {result.StandardMean:F3} | {result.LargeMean:F3}");
        }

        sb.AppendLine();
    }

    private static void AppendDivisionRandomSweep(StringBuilder sb, Random rng, int count)
    {
        sb.AppendLine("### Random operand sizes");
        sb.AppendLine("Case | Numerator bits | Denominator bits | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)");
        sb.AppendLine("---|---|---|---:|---:|---:");

        for (int i = 0; i < count; i++)
        {
            int numeratorBits = rng.Next(32, 4097);
            int denominatorBits = rng.Next(16, 2049);
            DivisionResult result = BenchmarkDivision(numeratorBits, denominatorBits, rng);
            sb.AppendLine($"{i + 1} | {numeratorBits} | {denominatorBits} | {result.SmallMean:F3} | {result.StandardMean:F3} | {result.LargeMean:F3}");
        }

        sb.AppendLine();
    }

    private static DivisionResult BenchmarkDivision(int numeratorBits, int denominatorBits, Random rng)
    {
        BigFloat numerator = CreateBigFloat(numeratorBits, rng);
        BigFloat denominator = CreateBigFloat(denominatorBits, rng);

        _ = DivideSmallNumbers(numerator, denominator);
        _ = DivideStandard(numerator, denominator);
        _ = DivideLargeNumbers(numerator, denominator);

        int iterations = GetDivisionIterations(numeratorBits, denominatorBits);

        double smallMean = Time(() => DivideSmallNumbers(numerator, denominator), iterations);
        double standardMean = Time(() => DivideStandard(numerator, denominator), iterations);
        double largeMean = Time(() => DivideLargeNumbers(numerator, denominator), iterations);

        return new DivisionResult(numeratorBits, denominatorBits, smallMean, standardMean, largeMean);
    }

    private static string RenderCrossoverSummary(IReadOnlyList<DivisionResult> results, bool includeHeader)
    {
        int? smallToStandard = FindCrossover(results, result => result.SmallMean, result => result.StandardMean);
        int? standardToLarge = FindCrossover(results, result => result.StandardMean, result => result.LargeMean);

        var sb = new StringBuilder();
        if (includeHeader)
        {
            sb.AppendLine("Crossover summary:");
        }

        sb.AppendLine($"- Small vs standard: {(smallToStandard is null ? "no crossover in sweep" : $"{smallToStandard} bits")}.");
        sb.AppendLine($"- Standard vs large/BZ: {(standardToLarge is null ? "no crossover in sweep" : $"{standardToLarge} bits")}.");
        return sb.ToString();
    }

    private static int? FindCrossover(IReadOnlyList<DivisionResult> results, Func<DivisionResult, double> left, Func<DivisionResult, double> right)
    {
        foreach (DivisionResult result in results)
        {
            if (right(result) <= left(result))
            {
                return result.NumeratorBits;
            }
        }

        return null;
    }

    private static int GetDivisionIterations(int numeratorBits, int denominatorBits)
    {
        int maxBits = Math.Max(numeratorBits, denominatorBits);
        return maxBits switch
        {
            >= 4096 => 30,
            >= 2048 => 50,
            >= 1024 => 80,
            _ => DivisionIterations
        };
    }

    private static Func<BigFloat, BigFloat, BigFloat> CreateDivideDelegate(string name)
    {
        MethodInfo? method = typeof(BigFloat).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        if (method is null)
        {
            throw new InvalidOperationException($"Unable to locate BigFloat.{name} for threshold sweep.");
        }

        return (Func<BigFloat, BigFloat, BigFloat>)method.CreateDelegate(typeof(Func<BigFloat, BigFloat, BigFloat>));
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

    private static BigFloat CreateBigFloat(int bits, Random rng)
    {
        return new BigFloat(CreateBigInteger(bits, rng));
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

    private sealed record DivisionResult(int NumeratorBits, int DenominatorBits, double SmallMean, double StandardMean, double LargeMean);

}
