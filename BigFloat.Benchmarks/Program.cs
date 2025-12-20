using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;

namespace BigFloat.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        string artifactsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "benchmarks", "artifacts");

        IConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .AddLogger(ConsoleLogger.Default)
            .AddColumn(StatisticColumn.P0, StatisticColumn.P50, StatisticColumn.P90)
            .WithArtifactsPath(artifactsPath)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

        BenchmarkRunner.Run<FormattingBenchmarks>();
        BenchmarkRunner.Run<ParsingBenchmarks>();
    }


    [MemoryDiagnoser]
    internal class FormattingBenchmarks
    {
        private readonly BigFloat _largeValue = BigFloat.Parse("-12345678901234567890.12345678901234567890", guardBitsIncluded: 16);
        private readonly char[] _buffer = new char[512];

        [Benchmark]
        public string FormatWithToString() => _largeValue.ToString();

        [Benchmark]
        public bool FormatWithTryFormat()
        {
            return _largeValue.TryFormat(_buffer, out _, [], provider: null);
        }
    }

    [MemoryDiagnoser]
    internal class ParsingBenchmarks
    {
        private readonly string _input = "-12345678901234567890.12345678901234567890";

        [Benchmark]
        public BigFloat ParseWithString() => BigFloat.Parse(_input);

        [Benchmark]
        public BigFloat ParseWithSpan()
        {
            BigFloat.TryParse(_input.AsSpan(), out BigFloat value);
            return value;
        }
    }
}
