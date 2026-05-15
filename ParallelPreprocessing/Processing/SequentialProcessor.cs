using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 1. modell: Soros feldolgozás — egyetlen szálon, szekvenciálisan.
/// </summary>
public static class SequentialProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        int frameCount = frames.Count;
        var results = new FrameData[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            results[i] = pipeline.Execute(frames[i]);
        }

        return results;
    }
}
