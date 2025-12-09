namespace BigFloatLibrary;

/// <summary>
/// Ambient, opt-in convenience layer for running a block of BigFloat operations
/// with a shared accuracy budget, rounding hints, and constants configuration.
/// </summary>
public sealed class BigFloatContext : IDisposable
{
    private static readonly AsyncLocal<BigFloatContext?> s_current = new();
    private readonly BigFloatContext? _previous;
    private bool _disposed;

    private BigFloatContext(
        int? requestedAccuracyBits,
        BigFloatContextRounding roundingMode,
        int? constantsPrecisionBits,
        bool? constantsCutOnTrailingZero,
        bool? constantsUseExternalFiles)
    {
        RequestedAccuracyBits = requestedAccuracyBits;
        RoundingMode = roundingMode;
        ConstantsPrecisionBits = constantsPrecisionBits;
        ConstantsCutOnTrailingZero = constantsCutOnTrailingZero;
        ConstantsUseExternalFiles = constantsUseExternalFiles;

        _previous = s_current.Value;
        s_current.Value = this;
    }

    /// <summary>
    /// The currently active context for the executing thread/async flow.
    /// </summary>
    public static BigFloatContext? Current => s_current.Value;

    /// <summary>
    /// Requested binary accuracy (fractional bit budget) to enforce when adjusting values.
    /// When null, values retain their existing accuracy unless modified explicitly.
    /// </summary>
    public int? RequestedAccuracyBits { get; }

    /// <summary>
    /// Rounding guidance applied when shrinking accuracy.
    /// </summary>
    public BigFloatContextRounding RoundingMode { get; }

    /// <summary>
    /// Optional precision override for constants fetched through <see cref="Constants"/>.
    /// </summary>
    public int? ConstantsPrecisionBits { get; }

    /// <summary>
    /// Optional trailing-zero cut policy for constants fetched through <see cref="Constants"/>.
    /// </summary>
    public bool? ConstantsCutOnTrailingZero { get; }

    /// <summary>
    /// Optional toggle that controls whether constants use external resource files when available.
    /// </summary>
    public bool? ConstantsUseExternalFiles { get; }

    /// <summary>
    /// Creates and activates a context with the specified accuracy and optional rounding/constants configuration.
    /// </summary>
    /// <param name="accuracyBits">Requested binary accuracy for values produced under this context.</param>
    /// <param name="roundingMode">Rounding guidance when the context must shrink accuracy.</param>
    /// <param name="constantsPrecisionBits">Optional precision to apply when retrieving constants.</param>
    /// <param name="constantsCutOnTrailingZero">Optional flag to cut precision when constants end in trailing zeros.</param>
    /// <param name="constantsUseExternalFiles">Optional flag to toggle loading constant tables from external files.</param>
    public static BigFloatContext WithAccuracy(
        int accuracyBits,
        BigFloatContextRounding roundingMode = BigFloatContextRounding.Default,
        int? constantsPrecisionBits = null,
        bool? constantsCutOnTrailingZero = null,
        bool? constantsUseExternalFiles = null)
    {
        return new BigFloatContext(
            accuracyBits,
            roundingMode,
            constantsPrecisionBits,
            constantsCutOnTrailingZero,
            constantsUseExternalFiles);
    }

    /// <summary>
    /// Creates and activates a context configured only for constants.
    /// </summary>
    /// <param name="constantsPrecisionBits">Optional precision to apply when retrieving constants.</param>
    /// <param name="constantsCutOnTrailingZero">Optional flag to cut precision when constants end in trailing zeros.</param>
    /// <param name="constantsUseExternalFiles">Optional flag to toggle loading constant tables from external files.</param>
    public static BigFloatContext ForConstants(
        int? constantsPrecisionBits = null,
        bool? constantsCutOnTrailingZero = null,
        bool? constantsUseExternalFiles = null)
    {
        return new BigFloatContext(
            requestedAccuracyBits: null,
            BigFloatContextRounding.Default,
            constantsPrecisionBits,
            constantsCutOnTrailingZero,
            constantsUseExternalFiles);
    }

    /// <summary>
    /// Restores the prior context when the current scope is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (s_current.Value == this)
        {
            s_current.Value = _previous;
        }

        _disposed = true;
    }

    /// <summary>
    /// Runs a BigFloat-producing delegate under this context and adjusts the result to the configured accuracy.
    /// </summary>
    public BigFloat Run(Func<BigFloat> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var scope = EnsureActive();
        BigFloat result = work();
        return Apply(result);
    }

    /// <summary>
    /// Runs a delegate under this context for callers that need ambient access via <see cref="Current"/>.
    /// </summary>
    public void Run(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var scope = EnsureActive();
        work();
    }

    /// <summary>
    /// Runs a delegate under this context and, when the return type is <see cref="BigFloat"/>,
    /// adjusts the result to the configured accuracy.
    /// </summary>
    public T Run<T>(Func<T> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var scope = EnsureActive();
        T result = work();
        return result switch
        {
            BigFloat value => (T)(object)Apply(value),
            _ => result
        };
    }

    /// <summary>
    /// Adjusts a value according to the context's requested accuracy and rounding mode.
    /// </summary>
    public BigFloat Apply(BigFloat value)
    {
        if (RequestedAccuracyBits is int targetAccuracy)
        {
            int delta = targetAccuracy - value.Accuracy;
            if (delta != 0)
            {
                bool roundWhenShrinking = RoundingMode != BigFloatContextRounding.TowardZero;
                value = BigFloat.AdjustAccuracy(value, delta, roundWhenShrinking);
            }
        }

        return value;
    }

    /// <summary>
    /// Adjusts a value using the currently active context, if any.
    /// </summary>
    public static BigFloat ApplyCurrent(BigFloat value)
    {
        return Current?.Apply(value) ?? value;
    }

    /// <summary>
    /// A constants provider honoring the context configuration.
    /// </summary>
    public BigFloat.Constants.WithPrecision Constants
    {
        get
        {
            if (ConstantsPrecisionBits is null && ConstantsCutOnTrailingZero is null && ConstantsUseExternalFiles is null)
            {
                return BigFloat.Constants.WithConfig();
            }

            return BigFloat.Constants.WithConfig(
                precisionInBits: ConstantsPrecisionBits ?? BigFloat.Constants.DefaultPrecisionInBits,
                cutOnTrailingZero: ConstantsCutOnTrailingZero ?? BigFloat.Constants.DefaultCutOnTrailingZeroSetting,
                useExternalFiles: ConstantsUseExternalFiles ?? BigFloat.Constants.DefaultUseExternalFiles);
        }
    }

    private ReentryScope? EnsureActive()
    {
        if (ReferenceEquals(Current, this))
        {
            return null;
        }

        return new ReentryScope(this);
    }

    private sealed class ReentryScope : IDisposable
    {
        private readonly BigFloatContext _context;
        private readonly BigFloatContext? _prior;
        private bool _disposed;

        public ReentryScope(BigFloatContext context)
        {
            _context = context;
            _prior = s_current.Value;
            s_current.Value = context;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (s_current.Value == _context)
            {
                s_current.Value = _prior;
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Rounding guidance for <see cref="BigFloatContext"/>.
/// </summary>
public enum BigFloatContextRounding
{
    /// <summary>
    /// Default BigFloat rounding semantics when shrinking accuracy.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Avoid rounding up when shrinking accuracy (truncate toward zero).
    /// </summary>
    TowardZero = 1,
}
