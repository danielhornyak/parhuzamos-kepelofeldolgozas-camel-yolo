using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Normalizálás: pixelértékek [0,1] tartományba képzése.
/// Képlet (4.3): N(x,y,c) = I(x,y,c) / 255.0
/// Egyúttal BGR → RGB sorrendcsere is megtörténik (a loader BGR-t ad, a YOLO RGB-t vár).
/// </summary>
public class NormalizeStep : IPreprocessor
{
    private const float Inv255 = 1f / 255f;

    public unsafe FrameData Process(FrameData input)
    {
        byte[] srcBuffer = input.PixelData;
        int byteCount = srcBuffer.Length;
        int pixelCount = byteCount / 3;
        float[] dstBuffer = new float[byteCount];

        fixed (byte* srcPtr = srcBuffer)
        fixed (float* dstPtr = dstBuffer)
        {
            // Pixelenként három byte → három float; egyúttal BGR → RGB csere.
            for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * 3;
                dstPtr[idx]     = srcPtr[idx + 2] * Inv255; // R (forrás 2. byte-ja)
                dstPtr[idx + 1] = srcPtr[idx + 1] * Inv255; // G (forrás 1. byte-ja)
                dstPtr[idx + 2] = srcPtr[idx]     * Inv255; // B (forrás 0. byte-ja)
            }
        }

        return new FrameData
        {
            Width = input.Width,
            Height = input.Height,
            FrameIndex = input.FrameIndex,
            PixelData = Array.Empty<byte>(),
            NormalizedData = dstBuffer
        };
    }
}
