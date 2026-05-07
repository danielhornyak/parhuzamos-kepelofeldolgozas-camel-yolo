using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 1. modell: Soros feldolgozás - egyetlen szálon, szekvenciálisan.
/// Referencia alap a párhuzamos modellek összehasonlításához.
/// </summary>
public static class SequentialProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        var results = new FrameData[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            results[i] = pipeline.Execute(frames[i]);
        }
        return results;
    }
}
