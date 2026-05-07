using System.Diagnostics;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Utilities;

public record BenchmarkResult(
    string ModelName,
    int FrameCount,
    int ThreadCount,
    double ElapsedMs,
    double PerFrameMs,
    double Speedup);

public static class BenchmarkRunner
{
    public static BenchmarkResult Run(
        string modelName,
        Func<List<FrameData>, PreprocessingPipeline, FrameData[]> processor,
        List<FrameData> frames,
        PreprocessingPipeline pipeline,
        int threadCount,
        double? baselineAvgMs = null)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Egyetlen mérés
        var sw = Stopwatch.StartNew();
        var result = processor(frames, pipeline);
        sw.Stop();
        double elapsedMs = sw.Elapsed.TotalMilliseconds;

        // Eredmény felszabadítása
        result = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        double avgPerFrame = elapsedMs / frames.Count;
        double speedup = baselineAvgMs.HasValue ? baselineAvgMs.Value / elapsedMs : 1.0;

        return new BenchmarkResult(modelName, frames.Count, threadCount, elapsedMs, avgPerFrame, speedup);
    }
}
