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
        double? baselineAvgMs = null)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        var result = processor();
        sw.Stop();
        double elapsedMs = sw.Elapsed.TotalMilliseconds;

        result = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        double avgPerFrame = elapsedMs / frameCount;
        double speedup = baselineAvgMs.HasValue ? baselineAvgMs.Value / elapsedMs : 1.0;

        return new BenchmarkResult(modelName, frameCount, threadCount, elapsedMs, avgPerFrame, speedup);
    }

    public static BenchmarkResult Run(
        string modelName,
        Func<List<FrameData>, PreprocessingPipeline, FrameData[]> processor,
        List<FrameData> frames,
        PreprocessingPipeline pipeline,
        int threadCount,
        double? baselineAvgMs = null)
    {
        return Run(modelName, () => processor(frames, pipeline), frames.Count, threadCount, baselineAvgMs);
    }
}
