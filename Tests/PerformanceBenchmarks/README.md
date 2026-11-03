# Rendering benchmarks

Run on Windows with a working graphics session:

```powershell
dotnet run --project Tests/PerformanceBenchmarks --configuration Release -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false -- 5
```

Each workload gets one untimed warmup. The following five iterations measure parsing/drawing construction on the STA thread, with a full collection before each iteration. Output is JSON Lines with the median elapsed milliseconds, median allocated bytes on that thread, a SHA-256 hash of the resulting 300 by 200 premultiplied pixels, an additional 900 by 600 output hash, and an aggregate hash/count of native bitmap drawings. The latter checks every filter bitmap at its full raster resolution. Allocations measure cumulative managed allocation, not peak memory, worker-thread bookkeeping or native WPF/GPU memory. Elapsed time includes completion of bounded numeric worker loops. Hash generation, final rasterization and explicit collections are outside the timed section. Native filter rasterization performed while constructing a drawing remains inside it.

Workloads cover empty inheritance chains, color matrices, separable/nonseparable blends, a linearRGB filter chain, and full loads versus redraws of the existing tiger, Acid1 and car samples. Samples resolve from the repository rather than downloaded assets. Run baseline and candidate serially with other builds/tests stopped. Keep framework, iteration count, sampling scale, host and graphics session the same. Compare pixel hashes before accepting a timing improvement. Results vary with the machine and workload; no timing threshold is used in CI.

To benchmark only car loading/redrawing, append `car` after the iteration count:

```powershell
dotnet run --project Tests/PerformanceBenchmarks --configuration Release -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false -- 5 car
```

See [performance notes](../../Docs/Performance.md) for the measured changes and remaining tradeoffs.
