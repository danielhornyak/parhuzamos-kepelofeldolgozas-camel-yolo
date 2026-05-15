using System.Collections.Concurrent;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 2. modell: Statikus Task feldolgozás — Parallel.ForEach + Partitioner.
/// A képkockákat fix, egybefüggő blokkokban osztja szét a szálak között
/// (pl. 1. szál: frame 1–25, 2. szál: frame 26–50, ...). Egyszerű, alacsony overhead,
/// de érzékeny a frame-enkénti feldolgozási idő szórására (terhelés-egyenetlenség).
/// </summary>
public static class StaticTaskProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        int frameCount = frames.Count;
        var results = new FrameData[frameCount];

        Parallel.ForEach(
            Partitioner.Create(0, frameCount),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            range =>
            {
                // Egy szál egy egybefüggő [start, end) tartományt dolgoz fel.
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    results[i] = pipeline.Execute(frames[i]);
                }
            });

        return results;
    }
}
