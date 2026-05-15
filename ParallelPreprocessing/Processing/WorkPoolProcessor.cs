using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;

namespace ParallelPreprocessing.Processing;

/// <summary>
/// 3. modell: Work Pool feldolgozás — atomic counter + worker szálak.
/// Dinamikus terheléselosztás: a szálak menet közben veszik ki a következő frame indexét
/// (Interlocked.Increment), így nem kell ConcurrentQueue/Bag és a végső rendezés is elmarad.
/// Az eredménytömb előre allokált, közvetlenül az indexre írunk — szálbiztos, mert
/// minden index pontosan egy szálé.
/// </summary>
public static class WorkPoolProcessor
{
    public static FrameData[] Process(List<FrameData> frames, PreprocessingPipeline pipeline)
    {
        int frameCount = frames.Count;
        var results = new FrameData[frameCount];
        int workerCount = Environment.ProcessorCount;
        int nextIndex = -1; // a counter; az első Increment 0-t ad

        var workers = new Thread[workerCount];
        for (int w = 0; w < workerCount; w++)
        {
            workers[w] = new Thread(() =>
            {
                // Minden worker addig vesz fel feladatot, amíg van.
                while (true)
                {
                    int idx = Interlocked.Increment(ref nextIndex);
                    if (idx >= frameCount) break;
                    results[idx] = pipeline.Execute(frames[idx]);
                }
            });
            workers[w].Start();
        }

        foreach (var worker in workers)
            worker.Join();

        return results;
    }
}
