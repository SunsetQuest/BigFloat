using BenchmarkDotNet.Attributes;

namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
public class FormattingBenchmarks
{
    private readonly BigFloat _largeValue = BigFloat.Parse("-12345678901234567890.12345678901234567890", guardBitsIncluded: 16);
    private readonly char[] _buffer = new char[512];

    [Benchmark]
    public string FormatWithToString() => _largeValue.ToString();

    [Benchmark]
    public bool FormatWithTryFormat()
    {
        return _largeValue.TryFormat(_buffer, out _, [], provider: null);
    }
}
