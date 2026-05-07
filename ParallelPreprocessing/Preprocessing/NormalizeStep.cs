using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Normalizálás: pixelértékek [0,1] tartományba képzése.
/// Képlet (4.3): N(x,y,c) = I(x,y,c) / 255.0
/// Optimalizált: nem tartja meg a byte[] PixelData-t, csak a float[] eredményt.
/// </summary>
public class NormalizeStep : IPreprocessor
{
    public FrameData Process(FrameData input)
    {
        float[] normalized = new float[input.PixelData.Length];

        for (int i = 0; i < input.PixelData.Length; i++)
        {
            normalized[i] = input.PixelData[i] / 255.0f;
        }

        return new FrameData
        {
            Width = input.Width,
            Height = input.Height,
            FrameIndex = input.FrameIndex,
            PixelData = Array.Empty<byte>(), // Felszabadítjuk - már nem kell
            NormalizedData = normalized
        };
    }
}
