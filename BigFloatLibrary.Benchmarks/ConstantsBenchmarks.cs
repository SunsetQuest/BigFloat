using BenchmarkDotNet.Attributes;
using static BigFloatLibrary.BigFloat;
namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Constants")]
public class ConstantsBenchmarks
{
    [Params(128, 512, 2048)]
    public int PrecisionBits { get; set; }

    [IterationSetup(Target = nameof(Pi))]
    public void WarmPi() => Constants.GetConstant(Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat Pi() => Constants.GetConstant(Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(E))]
    public void WarmE() => Constants.GetConstant(Catalog.E, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloat E() => Constants.GetConstant(Catalog.E, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(Gamma))]
    public void WarmGamma() => Constants.GetConstant(Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloat Gamma() => Constants.GetConstant(Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);
}
