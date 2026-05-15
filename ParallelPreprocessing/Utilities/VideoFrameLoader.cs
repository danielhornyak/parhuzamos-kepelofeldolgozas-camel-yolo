using System.Runtime.InteropServices;
using OpenCvSharp;
using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Utilities;

/// <summary>
/// Videófájlból tölti be a képkockákat FrameData formátumba (OpenCvSharp / VideoCapture).
/// A pixeladat BGR sorrendben kerül a memóriába (ahogy az OpenCV adja).
/// </summary>
public static class VideoFrameLoader
{
    /// <summary>
    /// Az összes (vagy a megadott darabszámú) frame betöltése a memóriába.
    /// </summary>
    /// <param name="videoPath">A videófájl elérési útja.</param>
    /// <param name="maxFrames">Maximum betöltendő frame-ek száma (0 = összes).</param>
    /// <returns>FrameData lista nyers BGR byte tömbökkel.</returns>
    public static List<FrameData> LoadFrames(string videoPath, int maxFrames = 0)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"A videófájl nem található: {videoPath}");

        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Nem sikerült megnyitni a videót: {videoPath}");

        int totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        double fps = capture.Get(VideoCaptureProperties.Fps);
        int srcW = (int)capture.Get(VideoCaptureProperties.FrameWidth);
        int srcH = (int)capture.Get(VideoCaptureProperties.FrameHeight);

        int limit = maxFrames > 0 ? Math.Min(maxFrames, totalFrames) : totalFrames;
        long perFrameBytes = (long)srcW * srcH * 3;
        double totalMemMb = (double)perFrameBytes * limit / (1024 * 1024);

        Console.WriteLine($"  Videó:      {Path.GetFileName(videoPath)}");
        Console.WriteLine($"  Felbontás:  {srcW}x{srcH}, FPS: {fps:F1}, Összes frame: {totalFrames}");
        Console.WriteLine($"  Betöltendő: {limit} frame (~{totalMemMb:F0} MB memória)");

        var frames = new List<FrameData>(limit);
        using var mat = new Mat();
        int frameIndex = 0;
        int progressStep = Math.Max(1, limit / 10); // 10%-os lépésközzel írunk ki haladást

        while (frameIndex < limit && capture.Read(mat))
        {
            if (mat.Empty())
                break;

            int byteCount = mat.Rows * mat.Cols * 3;
            byte[] pixelData = new byte[byteCount];
            // OpenCV natív bufferből másolás a managed tömbbe.
            Marshal.Copy(mat.Data, pixelData, 0, byteCount);

            frames.Add(new FrameData
            {
                Width = mat.Cols,
                Height = mat.Rows,
                FrameIndex = frameIndex,
                PixelData = pixelData
            });

            frameIndex++;

            if (frameIndex % progressStep == 0)
            {
                Console.Write($"\r  Betöltés: {frameIndex}/{limit} ({100 * frameIndex / limit}%)");
            }
        }

        Console.WriteLine(
            $"\r  Betöltve: {frames.Count} frame, memória: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        return frames;
    }

    /// <summary>
    /// Streameli a frame-eket a videóból egyenként — nem tölti be az összeset előre.
    /// Hasznos, ha kevés a memória, és a feldolgozás is folyamszerű (pl. soros pipeline).
    /// </summary>
    public static IEnumerable<FrameData> StreamFrames(string videoPath, int maxFrames = 0)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"A videófájl nem található: {videoPath}");

        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Nem sikerült megnyitni a videót: {videoPath}");

        int totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        int limit = maxFrames > 0 ? Math.Min(maxFrames, totalFrames) : totalFrames;

        using var mat = new Mat();
        int frameIndex = 0;

        while (frameIndex < limit && capture.Read(mat))
        {
            if (mat.Empty())
                yield break;

            int byteCount = mat.Rows * mat.Cols * 3;
            byte[] pixelData = new byte[byteCount];
            Marshal.Copy(mat.Data, pixelData, 0, byteCount);

            yield return new FrameData
            {
                Width = mat.Cols,
                Height = mat.Rows,
                FrameIndex = frameIndex,
                PixelData = pixelData
            };

            frameIndex++;
        }
    }

    /// <summary>
    /// Visszaadja a videó alapinformációit a teljes betöltés nélkül.
    /// </summary>
    public static (int width, int height, double fps, int totalFrames) GetVideoInfo(string videoPath)
    {
        using var capture = new VideoCapture(videoPath);
        return (
            (int)capture.Get(VideoCaptureProperties.FrameWidth),
            (int)capture.Get(VideoCaptureProperties.FrameHeight),
            capture.Get(VideoCaptureProperties.Fps),
            (int)capture.Get(VideoCaptureProperties.FrameCount)
        );
    }
}
