using System.Collections.Concurrent;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 3. modell: Work Pool feldolgozás — ConcurrentQueue + worker szálak.
/// Dinamikus terheléselosztás: a képkockák egy közös, szálbiztos sorba kerülnek.
/// A worker szálak ebből a sorból (TryDequeue) veszik ki a következő elemet,
/// amíg a sor ki nem ürül.
/// </summary>
public static class WorkPoolProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        int frameCount = frames.Count;
        var results = new FrameData[frameCount];
        int workerCount = Environment.ProcessorCount;

        // 1. A feladatsor (queue) feltöltése az összes frame-mel
        var queue = new ConcurrentQueue<FrameData>(frames);

        var workers = new Thread[workerCount];
        for (int w = 0; w < workerCount; w++)
        {
            workers[w] = new Thread(() =>
            {
                // 2. Minden worker addig vesz ki elemet a queue-ból, amíg van benne
                while (queue.TryDequeue(out var frame))
                {
                    // 3. Feldolgozás és az eredmény elmentése a megfelelő indexre
                    results[frame.FrameIndex] = pipeline.Execute(frame);
                }
            });
            workers[w].Start();
        }

        // 4. Megvárjuk, amíg az összes szál végez
        foreach (var worker in workers)
        {
            worker.Join();
        }

        return results;
    }
}
