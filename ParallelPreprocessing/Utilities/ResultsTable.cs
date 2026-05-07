namespace ParallelPreprocessing.Utilities;

public static class ResultsTable
{
    public static void Print(List<BenchmarkResult> results)
    {
        const string separator = "+--------------------+--------+---------+-------------+-----------------+----------+";
        const string header =    "| Modell             | Keret  | Szalak  | Ido (ms)    | Keret/atlag(ms) | Gyorsulas|";

        Console.WriteLine(separator);
        Console.WriteLine(header);
        Console.WriteLine(separator);

        foreach (var r in results)
        {
            Console.WriteLine(
                $"| {r.ModelName,-18} | {r.FrameCount,6} | {r.ThreadCount,7} | {r.ElapsedMs,11:F2} | {r.PerFrameMs,15:F4} | {r.Speedup,7:F2}x |");
        }

        Console.WriteLine(separator);
    }
}
