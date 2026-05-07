using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

public class PreprocessingPipeline
{
    private readonly List<IPreprocessor> _steps;

    public PreprocessingPipeline(int targetWidth, int targetHeight, float cropRatio = 0.30f)
    {
        _steps = new List<IPreprocessor>
        {
            new CropStep(cropRatio),
            new ResizeStep(targetWidth, targetHeight),
            new NormalizeStep()
        };
    }

    public FrameData Execute(FrameData frame)
    {
        foreach (var step in _steps)
            frame = step.Process(frame);
        return frame;
    }
}
