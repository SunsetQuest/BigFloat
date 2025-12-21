using BenchmarkDotNet.Attributes;

namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
internal class ParsingBenchmarks
{
    private readonly string _input = "-12345678901234567890.12345678901234567890";

    [Benchmark]
    public BigFloat ParseWithString() => BigFloat.Parse(_input);

    [Benchmark]
    public BigFloat ParseWithSpan()
    {
        if (BigFloat.TryParse(_input.AsSpan(), out BigFloat value))
        {
            return value;
        }

        throw new FormatException("Failed to parse input as BigFloat.");
    }
}
