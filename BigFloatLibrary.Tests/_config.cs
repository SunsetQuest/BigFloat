namespace BigFloatLibrary.Tests;

internal static class _config
{
    /// <summary>
    /// For long running methods only, This specifies the target time in milliseconds.
    /// </summary>
    public const int TestTargetInMilliseconds = 100;

#if DEBUG
    public const int MaxDegreeOfParallelism = 1;
    public const long SqrtBruteForceStoppedAt = 262144;
    public const long InverseBruteForceStoppedAt = 262144;
#else
    public static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;
    public const long SqrtBruteForceStoppedAt = 524288;
    public const long InverseBruteForceStoppedAt = 524288 * 1;
#endif

    public const int RAND_SEED = 22;
    public static readonly Random random = new(RAND_SEED);
}
