using OpenCvSharp;
using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Utilities;

/// <summary>
/// Videófájlból tölti be a képkockákat FrameData formátumba.
/// A videót frame-ekre bontja OpenCvSharp (VideoCapture) segítségével.
/// </summary>
public static class VideoFrameLoader
{
    /// <summary>
    /// Betölti a megadott számú frame-et a videóból.
    /// </summary>
    /// <param name="videoPath">A videófájl elérési útja</param>
    /// <param name="maxFrames">Maximum betöltendő frame-ek száma (0 = összes)</param>
    /// <returns>Frame-ek listája nyers RGB byte tömbökkel</returns>
    public static List<FrameData> LoadFrames(string videoPath, int maxFrames = 0)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"A videófájl nem található: {videoPath}");

        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Nem sikerült megnyitni a videót: {videoPath}");

        int totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        double fps = capture.Get(VideoCaptureProperties.Fps);
        int width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
        int height = (int)capture.Get(VideoCaptureProperties.FrameHeight);

        int limit = maxFrames > 0 ? Math.Min(maxFrames, totalFrames) : totalFrames;
        long perFrameBytes = (long)width * height * 3;
        double totalMemMB = (double)perFrameBytes * limit / (1024 * 1024);

        Console.WriteLine($"  Videó: {Path.GetFileName(videoPath)}");
        Console.WriteLine($"  Felbontás: {width}x{height}, FPS: {fps:F1}, Összes frame: {totalFrames}");
        Console.WriteLine($"  Betöltendő: {limit} frame (~{totalMemMB:F0} MB memória)");

        var frames = new List<FrameData>(limit);
        using var mat = new Mat();
        int frameIndex = 0;
        int progressStep = Math.Max(1, limit / 10); // 10%-onként kiír

        while (frameIndex < limit && capture.Read(mat))
        {
            if (mat.Empty())
                break;

            // BGR -> RGB konverzió és byte tömbbe másolás
            using var rgbMat = new Mat();
            Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);

            byte[] pixelData = new byte[rgbMat.Rows * rgbMat.Cols * 3];
            System.Runtime.InteropServices.Marshal.Copy(rgbMat.Data, pixelData, 0, pixelData.Length);

            frames.Add(new FrameData
            {
                Width = rgbMat.Cols,
                Height = rgbMat.Rows,
                FrameIndex = frameIndex,
                PixelData = pixelData
            });

            frameIndex++;

            if (frameIndex % progressStep == 0)
            {
                Console.Write($"\r  Betöltés: {frameIndex}/{limit} ({100 * frameIndex / limit}%)");
            }
        }

        Console.WriteLine($"\r  Betöltve: {frames.Count} frame, memória: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        return frames;
    }

    /// <summary>
    /// Visszaadja a videó alapinformációit betöltés nélkül.
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
