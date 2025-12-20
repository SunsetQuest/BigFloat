using BenchmarkDotNet.Attributes;
namespace BigFloat.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Constants")]
internal class ConstantsBenchmarks
{
    [Params(128, 512, 2048)]
    public int PrecisionBits { get; set; }

    [IterationSetup(Target = nameof(Pi))]
    public void WarmPi() => BigFloat.Constants.GetConstant(BigFloat.Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloat Pi() => BigFloat.Constants.GetConstant(BigFloat.Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(E))]
    public void WarmE() => BigFloat.Constants.GetConstant(BigFloat.Catalog.E, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloat E() => BigFloat.Constants.GetConstant(BigFloat.Catalog.E, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(Gamma))]
    public void WarmGamma() => BigFloat.Constants.GetConstant(BigFloat.Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloat Gamma() => BigFloat.Constants.GetConstant(BigFloat.Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);
}
