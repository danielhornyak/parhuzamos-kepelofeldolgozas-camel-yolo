namespace ParallelPreprocessing.Models;

/// <summary>
/// Egyetlen videó-képkocka adatait reprezentálja a pipeline-on keresztül.
/// A nyers BGR pixel-tömb (PixelData) a Crop és Resize lépésekben él;
/// a NormalizeStep után már csak a [0,1] tartományba képzett float tömb
/// (NormalizedData) hordozza az értékeket, a PixelData felszabadul.
/// </summary>
public class FrameData
{
    /// <summary>A képkocka aktuális szélessége (pixel).</summary>
    public int Width { get; set; }

    /// <summary>A képkocka aktuális magassága (pixel).</summary>
    public int Height { get; set; }

    /// <summary>A képkocka sorszáma az eredeti videóban (0-tól indul).</summary>
    public int FrameIndex { get; set; }

    /// <summary>Nyers BGR pixel-adatok (3 byte/pixel). Normalizálás után üres.</summary>
    public byte[] PixelData { get; set; } = Array.Empty<byte>();

    /// <summary>Normalizált RGB értékek [0,1] tartományban (3 float/pixel).</summary>
    public float[]? NormalizedData { get; set; }
}
