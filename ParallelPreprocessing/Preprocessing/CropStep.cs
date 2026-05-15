using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Geometriai előfeldolgozás: a kép felső c arányú sávjának eldobása (default c=0.30).
/// Képlet (4.1): I_crop(x,y) = I(x, y + floor(c*H)), ahol 0 ≤ x &lt; W, 0 ≤ y &lt; H(1-c)
/// </summary>
public class CropStep : IPreprocessor
{
    private readonly float _cropRatio;

    public CropStep(float cropRatio = 0.30f)
    {
        _cropRatio = cropRatio;
    }

    public FrameData Process(FrameData input)
    {
        int srcW = input.Width;
        int srcH = input.Height;
        int cropRows = (int)Math.Floor(_cropRatio * srcH); // levágandó felső sorok
        int dstH = srcH - cropRows;
        int rowBytes = srcW * 3; // BGR — 3 byte/pixel
        byte[] dstBuffer = new byte[dstH * rowBytes];

        Buffer.BlockCopy(
            input.PixelData, cropRows * rowBytes,
            dstBuffer,       0,
            dstH * rowBytes);

        return new FrameData
        {
            Width = srcW,
            Height = dstH,
            FrameIndex = input.FrameIndex,
            PixelData = dstBuffer
        };
    }
}
