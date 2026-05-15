using System.Diagnostics;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Utilities;

/// <summary>
/// Egy benchmark futás eredménye.
/// </summary>
/// <param name="ModelName">A mért modell neve (Soros, Statikus Task, Work Pool).</param>
/// <param name="FrameCount">A feldolgozott képkockák száma.</param>
/// <param name="ThreadCount">A használt szálak száma.</param>
/// <param name="ElapsedMs">A teljes futási idő ezredmásodpercben (átlag, ha több mérés volt).</param>
/// <param name="PerFrameMs">Átlagos idő egy képkockára (ms).</param>
/// <param name="Speedup">Gyorsulás a soros referenciához képest (1.0 = nincs).</param>
public record BenchmarkResult(
    string ModelName,
    int FrameCount,
    int ThreadCount,
    double ElapsedMs,
    double PerFrameMs,
    double Speedup);

/// <summary>
/// Benchmark futtató: warmup + több mérés átlaga, GC kényszerített ürítéssel a mérések előtt.
/// A warmup futás kihagyásával eltüntetjük a JIT-fordítás (Tier 0 → Tier 1) zaját.
/// </summary>
public static class BenchmarkRunner
{
    // Ennyi mérést végzünk és átlagolunk a megbízhatóság érdekében.
    private const int MeasuredRuns = 3;

    /// <summary>
    /// Általános benchmark egy paraméter nélküli delegate-re.
    /// </summary>
    public static BenchmarkResult Run(
        string modelName,
        Func<FrameData[]> processor,
        int frameCount,
        int threadCount,
        double? baselineAvgMs = null)
    {
        // 1) Warmup — eldobjuk az első futást, hogy a JIT végezzen az optimalizációval.
        var warmupResult = processor();
        warmupResult = null; // hint a GC felé
        ForceGc();

        // 2) Több éles mérés — a legrövidebb és legmegbízhatóbb az átlag.
        double totalMs = 0;
        for (int run = 0; run < MeasuredRuns; run++)
        {
            ForceGc();
            var sw = Stopwatch.StartNew();
            var runResult = processor();
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
            runResult = null;
        }

        double elapsedMs = totalMs / MeasuredRuns;
        double perFrameMs = elapsedMs / frameCount;
        double speedup = baselineAvgMs.HasValue ? baselineAvgMs.Value / elapsedMs : 1.0;

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
        double? baselineAvgMs = null)
    {
        return Run(modelName, () => processor(frames, pipeline), frames.Count, threadCount, baselineAvgMs);
    }

    // GC kényszerített tisztítás — a mérések közötti zaj csökkentésére.
    private static void ForceGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
