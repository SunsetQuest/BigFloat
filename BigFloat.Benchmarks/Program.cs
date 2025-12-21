using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace BigFloatLibrary.Benchmarks;

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


}
