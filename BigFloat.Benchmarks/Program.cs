using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BigFloatLibrary;

BenchmarkRunner.Run<FormattingBenchmarks>();
BenchmarkRunner.Run<ParsingBenchmarks>();

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
        return _largeValue.TryFormat(_buffer, out _, ReadOnlySpan<char>.Empty, provider: null);
    }
}

[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private readonly string _input = "-12345678901234567890.12345678901234567890";

    [Benchmark]
    public BigFloat ParseWithString() => BigFloat.Parse(_input);

    [Benchmark]
    public BigFloat ParseWithSpan()
    {
        BigFloat.TryParse(_input.AsSpan(), out var value);
        return value;
    }
}
