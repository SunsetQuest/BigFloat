using BenchmarkDotNet.Attributes;
namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Core")]
internal class IntegerArithmeticBenchmarks
{
    [Params(-4, -3, -2, -1, 1, 2, 3, 4, 7, 16, 123456789)]
    public int Factor { get; set; }

    [Params(32, 256, 1024)]
    public int OperandBits { get; set; }
    
    private BigFloat _value;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 48611 + Factor);
        int mantissaBits = OperandBits + BigFloat.GuardBits;
        _value = BigFloat.RandomWithMantissaBits(
            mantissaBits,
            -OperandBits,
            OperandBits,
            logarithmic: true,
            rand: rand);

        if (_value.IsZero)
        {
            _value = new BigFloat(1, binaryScaler: OperandBits / 4);
        }
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat MultiplyByInt() => _value * Factor;

    [Benchmark]
    public BigFloat DivideByInt() => _value / Factor;
}
