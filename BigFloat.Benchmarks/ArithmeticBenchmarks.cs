using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Core")]
public class ArithmeticBenchmarks
{
    [Params(32, 256, 1024, 4096)]
    public int OperandBits { get; set; }

    private BigFloatLibrary.BigFloat _a;
    private BigFloatLibrary.BigFloat _b;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 7919);
        _a = CreateRandomValue(rand);
        _b = CreateRandomValue(rand);

        if (_b.IsZero)
        {
            _b = new BigFloatLibrary.BigFloat(1, binaryScaler: 0);
        }
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatLibrary.BigFloat Addition() => _a + _b;

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatLibrary.BigFloat Subtraction() => _a - _b;

    [Benchmark]
    public BigFloatLibrary.BigFloat Multiplication() => _a * _b;

    [Benchmark]
    public BigFloatLibrary.BigFloat Division() => _a / _b;

    private BigFloatLibrary.BigFloat CreateRandomValue(Random rand)
    {
        int mantissaBits = OperandBits + BigFloatLibrary.BigFloat.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 8);
        return BigFloatLibrary.BigFloat.RandomWithMantissaBits(
            mantissaBits,
            -exponent,
            exponent,
            logarithmic: true,
            rand: rand);
    }
}
