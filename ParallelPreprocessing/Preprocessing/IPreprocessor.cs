using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

public interface IPreprocessor
{
    FrameData Process(FrameData input);
}
