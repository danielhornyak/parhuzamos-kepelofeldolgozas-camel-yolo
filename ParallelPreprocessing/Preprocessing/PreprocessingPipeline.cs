using ParallelPreprocessing.Models;

namespace ParallelPreprocessing.Preprocessing;

/// <summary>
/// A három előfeldolgozási lépést (Crop → Resize → Normalize) láncoló pipeline.
/// A lépéseket tömbben tárolja és sima for ciklussal futtatja — gyorsabb hívási útvonal,
/// mint a List+foreach kombináció (nincs enumerátor, JIT könnyebben inline-ol).
/// </summary>
public class PreprocessingPipeline
{
    private readonly IPreprocessor[] _steps;

    public PreprocessingPipeline(int targetWidth, int targetHeight, float cropRatio = 0.30f)
    {
        _steps = new IPreprocessor[]
        {
            new CropStep(cropRatio),
            new ResizeStep(targetWidth, targetHeight),
            new NormalizeStep()
        };
    }

    public FrameData Execute(FrameData frame)
    {
        var steps = _steps;
        for (int i = 0; i < steps.Length; i++)
        {
            frame = steps[i].Process(frame);
        }
        return frame;
    }
}
