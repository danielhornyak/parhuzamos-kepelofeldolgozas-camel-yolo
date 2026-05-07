using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// Geometriai előfeldolgozás: felső 30%-os függőleges vágás (C30).
/// Képlet (4.1): I_crop(x,y) = I(x, y + floor(c*H))
/// ahol 0 ≤ x < W, 0 ≤ y < H(1-c), c = kivágási arány
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
        int cropRows = (int)Math.Floor(_cropRatio * input.Height);
        int newHeight = input.Height - cropRows;
        int bytesPerRow = input.Width * 3; // RGB
        byte[] croppedPixels = new byte[newHeight * bytesPerRow];

        // Felső cropRows sor elhagyása, alsó rész másolása
        Buffer.BlockCopy(
            input.PixelData, cropRows * bytesPerRow,
            croppedPixels, 0,
            newHeight * bytesPerRow);

        return new FrameData
        {
            Width = input.Width,
            Height = newHeight,
            FrameIndex = input.FrameIndex,
            PixelData = croppedPixels
        };
    }
}
