using System.Diagnostics;

namespace BigFloatLibrary;

public static class TestsShared
{

    /// <summary>
    /// For long running methods only, This specifies the target time in milliseconds.
    /// </summary>
    public const int TestTargetInMilliseconds = 100;
    public const int RAND_SEED = 0x51C0_F00D;
    public static readonly Random _rand = new(RAND_SEED);

    public static void RunBudgeted(int targetMs, int seed, Action<Random, int> testCase)
    {
        int n = CalibrateIterations(targetMs, seed, testCase);
        var r = new Random(seed);
        for (int i = 0; i < n; i++) testCase(r, i);
    }

    public static int CalibrateIterations(int targetMs, int seed, Action<Random, int> testCase)
    {
        // Short window keeps this light but still adapts to machine speed.
        int windowMs = int.Clamp(targetMs, 10, 50);
        var r = new Random((int)(seed ^ 0x30)); //0x9E37_79B9
        var sw = Stopwatch.StartNew();

        int n = 0;
        while (sw.ElapsedMilliseconds < windowMs)
            testCase(r, n++);

        double msPer = sw.Elapsed.TotalMilliseconds / Math.Max(1, n);
        return Math.Max(1, (int)Math.Round(targetMs / msPer));
    }

    public static long LogUniform(Random r, long minInclusive, long maxInclusive)
    {
        double logMin = Math.Log(minInclusive);
        double logMax = Math.Log(maxInclusive);
        long v = (long)Math.Round(Math.Exp(logMin + r.NextDouble() * (logMax - logMin)));
        return v < minInclusive ? minInclusive : (v > maxInclusive ? maxInclusive : v);
    }
}
