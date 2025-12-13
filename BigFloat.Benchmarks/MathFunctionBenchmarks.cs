using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Math")]
public class MathFunctionBenchmarks
{
    [Params(64, 256, 1024)]
    public int OperandBits { get; set; }

    private BigFloatLibrary.BigFloat _positive;
    private BigFloatLibrary.BigFloat _unitRange;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 3571);
        int mantissaBits = OperandBits + BigFloatLibrary.BigFloat.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 6);

        _positive = BigFloatLibrary.BigFloat.RandomWithMantissaBits(mantissaBits, -exponent, exponent, logarithmic: true, rand: rand).Abs() + new BigFloatLibrary.BigFloat(1);
        _unitRange = BigFloatLibrary.BigFloat.RandomInRange(new BigFloatLibrary.BigFloat(-4), new BigFloatLibrary.BigFloat(4), logarithmic: false, rand: rand);
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatLibrary.BigFloat Sqrt() => BigFloatLibrary.BigFloat.Sqrt(_positive, OperandBits);

    [Benchmark]
    public BigFloatLibrary.BigFloat Pow() => BigFloatLibrary.BigFloat.Pow(_positive, 5);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public double Log2() => BigFloatLibrary.BigFloat.Log2(_positive);

    [Benchmark]
    public BigFloatLibrary.BigFloat Sin() => BigFloatLibrary.BigFloat.Sin(_unitRange);

    [Benchmark]
    public BigFloatLibrary.BigFloat Cos() => BigFloatLibrary.BigFloat.Cos(_unitRange);
}
