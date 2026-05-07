using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;
using ParallelPreprocessing.Processing;
using ParallelPreprocessing.Utilities;

// === Konfiguráció ===
string videoPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test_video.mp4");

int targetWidth = 640;
int targetHeight = 480;
float cropRatio = 0.30f;

Console.WriteLine("=== Párhuzamos Képelőfeldolgozási Modul - Benchmark ===");
Console.WriteLine($"CPU magok száma: {Environment.ProcessorCount}");
Console.WriteLine($"Cél felbontás:    {targetWidth}x{targetHeight}");
Console.WriteLine($"Kivágási arány:   {cropRatio:P0}");
Console.WriteLine();

// Videó információk lekérdezése
var (srcWidth, srcHeight, fps, totalFrames) = VideoFrameLoader.GetVideoInfo(videoPath);
Console.WriteLine($"Forrás videó: {videoPath}");
Console.WriteLine($"Felbontás: {srcWidth}x{srcHeight}, FPS: {fps:F1}, Összes frame: {totalFrames}");
Console.WriteLine();

// Az ÖSSZES frame betöltése a videóból
Console.WriteLine("Az összes frame betöltése a videóból...");
var frames = VideoFrameLoader.LoadFrames(videoPath);
Console.WriteLine();

var pipeline = new PreprocessingPipeline(targetWidth, targetHeight, cropRatio);
var results = new List<BenchmarkResult>();

// 1. Soros modell (referencia)
Console.Write("Soros modell mérése...");
var seqResult = BenchmarkRunner.Run(
    "Soros",
    (f, p) => SequentialProcessor.Process(f, p),
    frames, pipeline,
    threadCount: 1);
results.Add(seqResult);
Console.WriteLine($" {seqResult.ElapsedMs:F2} ms");

// 2. Statikus Task modell (Parallel.ForEach)
Console.Write("Statikus Task mérése...");
var staticResult = BenchmarkRunner.Run(
    "Statikus Task",
    (f, p) => StaticTaskProcessor.Process(f, p),
    frames, pipeline,
    threadCount: Environment.ProcessorCount,
    baselineAvgMs: seqResult.ElapsedMs);
results.Add(staticResult);
Console.WriteLine($" {staticResult.ElapsedMs:F2} ms");

// 3. Work Pool modell (ConcurrentQueue)
Console.Write("Work Pool mérése...");
var poolResult = BenchmarkRunner.Run(
    "Work Pool",
    (f, p) => WorkPoolProcessor.Process(f, p),
    frames, pipeline,
    threadCount: Environment.ProcessorCount,
    baselineAvgMs: seqResult.ElapsedMs);
results.Add(poolResult);
Console.WriteLine($" {poolResult.ElapsedMs:F2} ms");

Console.WriteLine();
ResultsTable.Print(results);
Console.WriteLine();
Console.WriteLine("=== Benchmark befejezve ===");
