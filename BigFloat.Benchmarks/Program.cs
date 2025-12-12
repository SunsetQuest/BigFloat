using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
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
            .AddExporter(MarkdownExporter.GitHub)
            .WithArtifactsPath(artifactsPath)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

        BenchmarkRunner.Run<FormattingBenchmarks>();
        BenchmarkRunner.Run<ParsingBenchmarks>();
    }


    [MemoryDiagnoser]
    public class FormattingBenchmarks
    {
        private readonly BigFloatLibrary.BigFloat _largeValue = BigFloatLibrary.BigFloat.Parse("-12345678901234567890.12345678901234567890", guardBitsIncluded: 16);
        private readonly char[] _buffer = new char[512];

        [Benchmark]
        public string FormatWithToString() => _largeValue.ToString();

        [Benchmark]
        public bool FormatWithTryFormat()
        {
            return _largeValue.TryFormat(_buffer, out _, ReadOnlySpan<char>.Empty, provider: null);
        }
    }

    [MemoryDiagnoser]
    public class ParsingBenchmarks
    {
        private readonly string _input = "-12345678901234567890.12345678901234567890";

        [Benchmark]
        public BigFloatLibrary.BigFloat ParseWithString() => BigFloatLibrary.BigFloat.Parse(_input);

        [Benchmark]
        public BigFloatLibrary.BigFloat ParseWithSpan()
        {
            BigFloatLibrary.BigFloat.TryParse(_input.AsSpan(), out var value);
            return value;
        }
    }
}
