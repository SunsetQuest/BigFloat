using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Math")]
public class MathFunctionBenchmarks
{
    [Params(64, 256, 1024)]
    public int OperandBits { get; set; }

    private BigFloatNumber _positive;
    private BigFloatNumber _unitRange;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 3571);
        int mantissaBits = OperandBits + BigFloatNumber.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 6);

        _positive = BigFloatNumber.RandomWithMantissaBits(mantissaBits, -exponent, exponent, logarithmic: true, rand: rand).Abs() + new BigFloatNumber(1);
        _unitRange = BigFloatNumber.RandomInRange(new BigFloatNumber(-4), new BigFloatNumber(4), logarithmic: false, rand: rand);
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatNumber Sqrt() => BigFloatNumber.Sqrt(_positive, OperandBits);

    [Benchmark]
    public BigFloatNumber Pow() => BigFloatNumber.Pow(_positive, 5);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public double Log2() => BigFloatNumber.Log2(_positive);

    [Benchmark]
    public BigFloatNumber Sin() => BigFloatNumber.Sin(_unitRange);

    [Benchmark]
    public BigFloatNumber Cos() => BigFloatNumber.Cos(_unitRange);
}
