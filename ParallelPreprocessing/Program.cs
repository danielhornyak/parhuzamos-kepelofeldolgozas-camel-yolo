using ParallelPreprocessing.Preprocessing;
using ParallelPreprocessing.Processing;
using ParallelPreprocessing.Utilities;

// === Konfiguráció ===
// Az első parancssori argumentum a videó elérési útja, ha nincs, akkor az alapértelmezett.
string videoPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test_video.mp4");

const int targetWidth  = 640;
const int targetHeight = 480;
const float cropRatio  = 0.30f;

Console.WriteLine("=== Párhuzamos Képelőfeldolgozási Modul - Benchmark ===");
Console.WriteLine($"CPU magok száma: {Environment.ProcessorCount}");
Console.WriteLine($"Cél felbontás:    {targetWidth}x{targetHeight}");
Console.WriteLine($"Kivágási arány:   {cropRatio:P0}");
Console.WriteLine();

// Videó információk lekérdezése betöltés nélkül.
var (srcW, srcH, fps, totalFrames) = VideoFrameLoader.GetVideoInfo(videoPath);
Console.WriteLine($"Forrás videó: {videoPath}");
Console.WriteLine($"Felbontás: {srcW}x{srcH}, FPS: {fps:F1}, Összes frame: {totalFrames}");
Console.WriteLine();

// Az összes frame betöltése a memóriába — a benchmark így csak a feldolgozást méri.
Console.WriteLine("Az összes frame betöltése a videóból...");
var frames = VideoFrameLoader.LoadFrames(videoPath);
Console.WriteLine();

var pipeline = new PreprocessingPipeline(targetWidth, targetHeight, cropRatio);
var results = new List<BenchmarkResult>();

// 1. Soros modell — ez lesz a referencia gyorsulás-számításhoz.
Console.Write("Soros modell mérése...");
var seqResult = BenchmarkRunner.Run(
    "Soros",
    SequentialProcessor.Process,
    frames, pipeline,
    threadCount: 1);
results.Add(seqResult);
Console.WriteLine($" {seqResult.ElapsedMs:F2} ms");

// 2. Statikus Task modell (Parallel.ForEach + Partitioner)
Console.Write("Statikus Task mérése...");
var staticResult = BenchmarkRunner.Run(
    "Statikus Task",
    StaticTaskProcessor.Process,
    frames, pipeline,
    threadCount: Environment.ProcessorCount,
    baselineAvgMs: seqResult.ElapsedMs);
results.Add(staticResult);
Console.WriteLine($" {staticResult.ElapsedMs:F2} ms");

// 3. Work Pool modell (atomic counter + worker szálak)
Console.Write("Work Pool mérése...");
var poolResult = BenchmarkRunner.Run(
    "Work Pool",
    WorkPoolProcessor.Process,
    frames, pipeline,
    threadCount: Environment.ProcessorCount,
    baselineAvgMs: seqResult.ElapsedMs);
results.Add(poolResult);
Console.WriteLine($" {poolResult.ElapsedMs:F2} ms");

Console.WriteLine();
ResultsTable.Print(results);
Console.WriteLine();
Console.WriteLine("=== Benchmark befejezve ===");
