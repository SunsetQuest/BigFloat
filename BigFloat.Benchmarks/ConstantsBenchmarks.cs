using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Constants")]
public class ConstantsBenchmarks
{
    [Params(128, 512, 2048)]
    public int PrecisionBits { get; set; }

    [IterationSetup(Target = nameof(Pi))]
    public void WarmPi() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.Pi, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatNumber Pi() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.Pi, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(E))]
    public void WarmE() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.E, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloatNumber E() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.E, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(Gamma))]
    public void WarmGamma() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloatNumber Gamma() => BigFloatNumber.Constants.GetConstant(ConstantsCatalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);
}
