using BenchmarkDotNet.Attributes;
namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Core")]
public class ArithmeticBenchmarks
{
    [Params(32, 256, 1024, 4096)]
    public int OperandBits { get; set; }

    private BigFloat _a;
    private BigFloat _b;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 7919);
        _a = CreateRandomValue(rand);
        _b = CreateRandomValue(rand);

        if (_b.IsZero)
        {
            _b = new BigFloat(1, binaryScaler: 0);
        }
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat Addition() => _a + _b;

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat Subtraction() => _a - _b;

    [Benchmark]
    public BigFloat Multiplication() => _a * _b;

    [Benchmark]
    public BigFloat Division() => _a / _b;

    private BigFloat CreateRandomValue(Random rand)
    {
        int mantissaBits = OperandBits + BigFloat.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 8);
        return BigFloat.RandomWithMantissaBits(
            mantissaBits,
            -exponent,
            exponent,
            logarithmic: true,
            rand: rand);
    }
}
