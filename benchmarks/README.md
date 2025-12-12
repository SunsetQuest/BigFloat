# BigFloat benchmarks

This solution contains a BenchmarkDotNet suite under `BigFloat.Benchmarks`. The suite focuses on arithmetic, math functions, parsing/formatting, and constant retrieval at multiple operand sizes.

## Running benchmarks

- Full run: `dotnet run -c Release -p BigFloat.Benchmarks`
- CI/quick sweep (shorter, uses `[BenchmarkCategory("CI")]` and a ShortRun/Dry job):
  `dotnet run -c Release -p BigFloat.Benchmarks -- --job short --anyCategories CI`

Benchmark artifacts are written to `benchmarks/artifacts/` via BenchmarkDotNet's output, and curated baseline reports live in `benchmarks/baselines/`.
