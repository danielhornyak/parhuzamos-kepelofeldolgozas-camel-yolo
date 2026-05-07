using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Átméretezés bilineáris interpolációval a célméretekre.
/// Képlet (4.2) alapján.
/// </summary>
public class ResizeStep : IPreprocessor
{
    private readonly int _targetWidth;
    private readonly int _targetHeight;

    public ResizeStep(int targetWidth, int targetHeight)
    {
        _targetWidth = targetWidth;
        _targetHeight = targetHeight;
    }

    public FrameData Process(FrameData input)
    {
        int srcW = input.Width;
        int srcH = input.Height;
        int dstW = _targetWidth;
        int dstH = _targetHeight;
        byte[] src = input.PixelData;
        byte[] dst = new byte[dstW * dstH * 3];

        float scaleX = (float)srcW / dstW;
        float scaleY = (float)srcH / dstH;

        for (int oy = 0; oy < dstH; oy++)
        {
            float fy = oy * scaleY;
            int y0 = (int)fy;
            int y1 = Math.Min(y0 + 1, srcH - 1);
            float dy = fy - y0;

            for (int ox = 0; ox < dstW; ox++)
            {
                float fx = ox * scaleX;
                int x0 = (int)fx;
                int x1 = Math.Min(x0 + 1, srcW - 1);
                float dx = fx - x0;

                int srcIdx00 = (y0 * srcW + x0) * 3;
                int srcIdx10 = (y0 * srcW + x1) * 3;
                int srcIdx01 = (y1 * srcW + x0) * 3;
                int srcIdx11 = (y1 * srcW + x1) * 3;
                int dstIdx = (oy * dstW + ox) * 3;

                for (int c = 0; c < 3; c++)
                {
                    float val =
                        src[srcIdx00 + c] * (1 - dx) * (1 - dy) +
                        src[srcIdx10 + c] * dx * (1 - dy) +
                        src[srcIdx01 + c] * (1 - dx) * dy +
                        src[srcIdx11 + c] * dx * dy;
                    dst[dstIdx + c] = (byte)Math.Clamp(val + 0.5f, 0, 255);
                }
            }
        }

        return new FrameData
        {
            Width = dstW,
            Height = dstH,
            FrameIndex = input.FrameIndex,
            PixelData = dst
        };
    }
}
