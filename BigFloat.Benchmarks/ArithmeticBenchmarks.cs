using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Core")]
public class ArithmeticBenchmarks
{
    [Params(32, 256, 1024, 4096)]
    public int OperandBits { get; set; }

    private BigFloatNumber _a;
    private BigFloatNumber _b;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 7919);
        _a = CreateRandomValue(rand);
        _b = CreateRandomValue(rand);

        if (_b.IsZero)
        {
            _b = new BigFloatNumber(1, binaryScaler: 0);
        }
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatNumber Addition() => _a + _b;

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatNumber Subtraction() => _a - _b;

    [Benchmark]
    public BigFloatNumber Multiplication() => _a * _b;

    [Benchmark]
    public BigFloatNumber Division() => _a / _b;

    private BigFloatNumber CreateRandomValue(Random rand)
    {
        int mantissaBits = OperandBits + BigFloatNumber.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 8);
        return BigFloatNumber.RandomWithMantissaBits(
            mantissaBits,
            -exponent,
            exponent,
            logarithmic: true,
            rand: rand);
    }
}
