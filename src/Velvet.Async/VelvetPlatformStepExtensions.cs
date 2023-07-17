namespace Velvet.Async;

public static class VelvetPlatformStepExtensions
{
    public static IEnumerable<VelvetPlatformLoopStep> IncludedSteps(this VelvetPlatformStepMask mask) {
        for (int i = 0; i < 8; i++) {
            VelvetPlatformStepMask step = (VelvetPlatformStepMask)(1 << i);
            bool b = MaskMatches(mask, step);
            if (b)
                yield return (VelvetPlatformLoopStep)i;
        }
    }

    public static bool HasStep(this VelvetPlatformStepMask mask, VelvetPlatformLoopStep step) =>
        (mask & (VelvetPlatformStepMask)(1 << (int)step)) != VelvetPlatformStepMask.None;

    public static bool MaskMatches(this VelvetPlatformStepMask mask, VelvetPlatformStepMask step) =>
        (mask & step) != VelvetPlatformStepMask.None;

    public static VelvetPlatformStepMask ToMask(this VelvetPlatformLoopStep step) =>
        (VelvetPlatformStepMask)(1 << (int)step);

    public static VelvetPlatformStepMask ToMask(this IEnumerable<VelvetPlatformLoopStep> steps) =>
        steps.Aggregate(VelvetPlatformStepMask.None, (m, s) => m | s.ToMask());
}