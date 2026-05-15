namespace ParallelPreprocessing.Utilities;

/// <summary>
/// Egyszerű ASCII táblázat-megjelenítő a benchmark eredményekhez.
/// </summary>
public static class ResultsTable
{
    private const string Separator =
        "+--------------------+--------+---------+-------------+-----------------+----------+";
    private const string Header =
        "| Modell             | Keret  | Szalak  | Ido (ms)    | Keret/atlag(ms) | Gyorsulas|";

    public static void Print(List<BenchmarkResult> results)
    {
        Console.WriteLine(Separator);
        Console.WriteLine(Header);
        Console.WriteLine(Separator);

        foreach (var r in results)
        {
            Console.WriteLine(
                $"| {r.ModelName,-18} | {r.FrameCount,6} | {r.ThreadCount,7} | " +
                $"{r.ElapsedMs,11:F2} | {r.PerFrameMs,15:F4} | {r.Speedup,7:F2}x |");
        }

        Console.WriteLine(Separator);
    }
}
