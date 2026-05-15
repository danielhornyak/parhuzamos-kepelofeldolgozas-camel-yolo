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
        Func<FrameData[]> processor,
        int frameCount,
        int threadCount,
        double? baselineMs = null) 
    {
        // 1) Warmup — eldobjuk az első futást, hogy a JIT végezzen az optimalizációval.
        var warmupResult = processor();
        warmupResult = null;
        ForceGc();

        var sw = Stopwatch.StartNew();
        var runResult = processor();
        sw.Stop();

        double elapsedMs = sw.Elapsed.TotalMilliseconds;
        runResult = null;

        double perFrameMs = elapsedMs / frameCount;
        double speedup = baselineMs.HasValue ? baselineMs.Value / elapsedMs : 1.0;

        return new BenchmarkResult(modelName, frameCount, threadCount, elapsedMs, perFrameMs, speedup);
    }

    /// <summary>
    /// Kényelmi overload: a feldolgozó (frames, pipeline) szignatúrájára van szabva.
    /// </summary>
    public static BenchmarkResult Run(
        string modelName,
        Func<List<FrameData>, PreprocessingPipeline, FrameData[]> processor,
        List<FrameData> frames,
        PreprocessingPipeline pipeline,
        int threadCount,
        double? baselineMs = null)
    {
        return Run(modelName, () => processor(frames, pipeline), frames.Count, threadCount, baselineMs);
    }

    // GC kényszerített tisztítás — a mérések közötti zaj csökkentésére.
    private static void ForceGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
