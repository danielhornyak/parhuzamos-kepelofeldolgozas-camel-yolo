using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Átméretezés bilineáris interpolációval a célméretekre.
/// Képlet (4.2): a kimeneti pixel a 4 szomszédos forrás-pixel súlyozott átlaga.
/// Teljesítménykritikus lépés — unsafe pointerekkel, kibontott csatorna-ciklussal
/// és sor-szintű címszámítás-kiemeléssel optimalizálva.
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

    public unsafe FrameData Process(FrameData input)
    {
        int srcW = input.Width;
        int srcH = input.Height;
        int dstW = _targetWidth;
        int dstH = _targetHeight;
        byte[] srcBuffer = input.PixelData;
        byte[] dstBuffer = new byte[dstW * dstH * 3];

        // A skálázási tényező a kimenetből a bemenetbe mutat.
        float scaleX = (float)srcW / dstW;
        float scaleY = (float)srcH / dstH;
        int srcStride = srcW * 3; // egy forrássor mérete byte-ban
        int dstStride = dstW * 3; // egy célsor mérete byte-ban

        fixed (byte* srcPtr = srcBuffer)
        fixed (byte* dstPtr = dstBuffer)
        {
            // Külső ciklus: kimeneti sorokon végig — a sorszintű mennyiségek itt számolódnak ki.
            for (int dy = 0; dy < dstH; dy++)
            {
                float fy = dy * scaleY;
                int y0 = (int)fy;
                int y1 = y0 + 1 < srcH ? y0 + 1 : srcH - 1;
                float weightY = fy - y0;
                float invWeightY = 1f - weightY;

                // Két forrás-sor pointer kiemelése: ez O(dstW) szorzást spórol meg soronként.
                byte* rowTop = srcPtr + y0 * srcStride;
                byte* rowBot = srcPtr + y1 * srcStride;
                byte* dstRow = dstPtr + dy * dstStride;

                // Belső ciklus: kimeneti oszlopokon végig.
                for (int dx = 0; dx < dstW; dx++)
                {
                    float fx = dx * scaleX;
                    int x0 = (int)fx;
                    int x1 = x0 + 1 < srcW ? x0 + 1 : srcW - 1;
                    float weightX = fx - x0;
                    float invWeightX = 1f - weightX;

                    // A 4 sarokpixel byte-eltolása (3 byte/pixel — BGR).
                    int off00 = x0 * 3;
                    int off10 = x1 * 3;

                    // Bilineáris súlyok — egyszer számolva, mind a 3 csatornára közösen.
                    float w00 = invWeightX * invWeightY;
                    float w10 = weightX   * invWeightY;
                    float w01 = invWeightX * weightY;
                    float w11 = weightX   * weightY;

                    byte* outPx = dstRow + dx * 3;

                    // c=0 csatorna (B)
                    float v0 = rowTop[off00]     * w00 + rowTop[off10]     * w10
                             + rowBot[off00]     * w01 + rowBot[off10]     * w11;
                    // c=1 csatorna (G)
                    float v1 = rowTop[off00 + 1] * w00 + rowTop[off10 + 1] * w10
                             + rowBot[off00 + 1] * w01 + rowBot[off10 + 1] * w11;
                    // c=2 csatorna (R)
                    float v2 = rowTop[off00 + 2] * w00 + rowTop[off10 + 2] * w10
                             + rowBot[off00 + 2] * w01 + rowBot[off10 + 2] * w11;

                    // Kerekítés és byte tartományba szorítás. A +0.5f miatt az értékek
                    // [0, 255.5] közöttiek; a clamp védi a határt.
                    outPx[0] = ToByte(v0);
                    outPx[1] = ToByte(v1);
                    outPx[2] = ToByte(v2);
                }
            }
        }

        return new FrameData
        {
            Width = dstW,
            Height = dstH,
            FrameIndex = input.FrameIndex,
            PixelData = dstBuffer
        };
    }

    // Inline-olható segéd: kerekítés + clamp egy lépésben.
    private static byte ToByte(float value)
    {
        int v = (int)(value + 0.5f);
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }
}
