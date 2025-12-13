using BenchmarkDotNet.Attributes;
using BigFloatLibrary;

namespace BigFloatLibrary.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Constants")]
public class ConstantsBenchmarks
{
    [Params(128, 512, 2048)]
    public int PrecisionBits { get; set; }

    [IterationSetup(Target = nameof(Pi))]
    public void WarmPi() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    [BenchmarkCategory("CI")]
    public BigFloatLibrary.BigFloat Pi() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.Pi, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(E))]
    public void WarmE() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.E, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloatLibrary.BigFloat E() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.E, PrecisionBits, useExternalFiles: false);

    [IterationSetup(Target = nameof(Gamma))]
    public void WarmGamma() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);

    [Benchmark]
    public BigFloatLibrary.BigFloat Gamma() => BigFloatLibrary.BigFloat.Constants.GetConstant(BigFloatLibrary.BigFloat.Catalog.EulerMascheroniConstant, PrecisionBits, useExternalFiles: false);
}
