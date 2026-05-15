using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// A pipeline egy lépésének közös szerződése: egy FrameData-ból egy újat ad vissza.
/// Az implementációk szálbiztosnak tervezettek (állapotmentesek vagy csak readonly mezők).
/// </summary>
public interface IPreprocessor
{
    FrameData Process(FrameData input);
}
