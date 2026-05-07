namespace ParallelPreprocessing.Models;

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameIndex { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
    public float[]? NormalizedData { get; set; }
}
