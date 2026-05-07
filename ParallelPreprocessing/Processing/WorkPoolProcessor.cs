using System.Collections.Concurrent;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 3. modell: Work Pool feldolgozás - ConcurrentQueue + worker szálak.
/// Dinamikus terheléselosztás: a szálak menet közben veszik ki a feladatokat a sorból.
/// A szálszámot az Environment.ProcessorCount határozza meg.
/// </summary>
public static class WorkPoolProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        var queue = new ConcurrentQueue<FrameData>(frames);
        var results = new ConcurrentBag<FrameData>();
        int workerCount = Environment.ProcessorCount;

        var workers = new Thread[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            workers[i] = new Thread(() =>
            {
                while (queue.TryDequeue(out var frame))
                {
                    var processed = pipeline.Execute(frame);
                    results.Add(processed);
                }
            });
            workers[i].Start();
        }

        foreach (var worker in workers)
            worker.Join();

        return results.OrderBy(f => f.FrameIndex).ToArray();
    }
}
