using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure-logic calculations for the "Adjust All Frame Time" dialog (A17).
/// </summary>
public static class FrameTimeScaler
{
    /// <summary>
    /// Scales every frame's <c>FrameLength</c> so the chain's total duration
    /// equals <paramref name="targetTotalDuration"/> while keeping the original
    /// ratios between frames.
    ///
    /// No-op when <paramref name="frames"/> is empty or when the current total
    /// duration is zero (all frames have FrameLength = 0).
    /// </summary>
    public static void ApplyKeepProportional(
        IList<AnimationFrameSave> frames,
        float targetTotalDuration)
    {
        if (frames.Count == 0) return;

        float currentTotal = frames.Sum(f => f.FrameLength);
        if (currentTotal == 0f) return;

        float scale = targetTotalDuration / currentTotal;
        foreach (var frame in frames)
            frame.FrameLength *= scale;
    }

    /// <summary>
    /// Sets every frame's <c>FrameLength</c> to the same value:
    /// <paramref name="targetTotalDuration"/> / <paramref name="frames"/>.Count.
    ///
    /// No-op when <paramref name="frames"/> is empty.
    /// </summary>
    public static void ApplySetAllSame(
        IList<AnimationFrameSave> frames,
        float targetTotalDuration)
    {
        if (frames.Count == 0) return;

        float perFrame = targetTotalDuration / frames.Count;
        foreach (var frame in frames)
            frame.FrameLength = perFrame;
    }
}
