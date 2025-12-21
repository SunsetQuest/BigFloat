using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        BigFloat.TryParse(_input.AsSpan(), out BigFloat value);
        return value;
    }
}

