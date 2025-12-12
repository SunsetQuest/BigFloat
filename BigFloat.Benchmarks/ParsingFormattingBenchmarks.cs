using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("IO")]
public class ParsingFormattingBenchmarks
{
    [Params(64, 256, 1024)]
    public int OperandBits { get; set; }

    private BigFloatNumber _value;
    private string _decimalString = string.Empty;
    private string _hexString = string.Empty;
    private string _binaryString = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(OperandBits * 1867);
        int mantissaBits = OperandBits + BigFloatNumber.GuardBits;
        int exponent = Math.Max(OperandBits / 2, 6);

        _value = BigFloatNumber.RandomWithMantissaBits(mantissaBits, -exponent, exponent, logarithmic: true, rand: rand);

        _decimalString = _value.ToString();
        _hexString = _value.ToString("X");
        _binaryString = _value.ToString("B");
    }

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatNumber ParseDecimal() => BigFloatNumber.Parse(_decimalString);

    [Benchmark]
    public BigFloatNumber ParseHex() => BigFloatNumber.Parse(_hexString);

    [Benchmark]
    public BigFloatNumber ParseBinary() => BigFloatNumber.Parse(_binaryString);

    [Benchmark]
    public string FormatDecimal() => _value.ToString();

    [Benchmark]
    [BenchmarkCategory("CI")]
    public string FormatHex() => _value.ToString("X");

    [Benchmark]
    public string FormatBinary() => _value.ToString("B");
}
