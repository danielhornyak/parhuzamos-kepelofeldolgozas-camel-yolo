using System.Collections.Concurrent;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 2. modell: Statikus Task feldolgozás - Parallel.ForEach + Partitioner.
/// A képkockákat fix blokkokban osztja szét a szálak között.
/// Pl.: 1. szál: frame 1-25, 2. szál: frame 26-50, stb.
/// </summary>
public static class StaticTaskProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        var results = new FrameData[frames.Count];

        Parallel.ForEach(
            Partitioner.Create(0, frames.Count),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    results[i] = pipeline.Execute(frames[i]);
                }
            });

        return results;
    }
}
