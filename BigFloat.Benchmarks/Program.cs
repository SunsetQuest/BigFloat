using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace BigFloat.Benchmarks;

public static class Program
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
    }
}
