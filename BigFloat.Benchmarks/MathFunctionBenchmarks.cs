using BenchmarkDotNet.Attributes;
namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Math")]
internal class MathFunctionBenchmarks
{
    [Params(64, 256, 1024)]
    public int OperandBits { get; set; }

    private BigFloat _positive;
    private BigFloat _unitRange;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 3571);
        int mantissaBits = OperandBits + BigFloat.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 6);

        _positive = BigFloat.RandomWithMantissaBits(mantissaBits, -exponent, exponent, logarithmic: true, rand: rand).Abs() + new BigFloat(1);
        _unitRange = BigFloat.RandomInRange(new BigFloat(-4), new BigFloat(4), logarithmic: false, rand: rand);
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat Sqrt() => BigFloat.Sqrt(_positive, OperandBits);

    [Benchmark]
    public BigFloat Pow() => BigFloat.Pow(_positive, 5);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public double Log2() => BigFloat.Log2(_positive);

    [Benchmark]
    public BigFloat Sin() => BigFloat.Sin(_unitRange);

    [Benchmark]
    public BigFloat Cos() => BigFloat.Cos(_unitRange);
}
