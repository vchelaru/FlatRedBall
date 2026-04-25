using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure-logic calculations for the "Adjust Offsets" dialog (A16).
/// No UI, no singletons — fully testable.
/// </summary>
public static class AdjustOffsetCalculator
{
    /// <summary>
    /// Justify (Bottom) — sets each frame's <c>RelativeY</c> so the frame's bottom
    /// pixel edge aligns to y = 0, then divides by <paramref name="offsetMultiplier"/>
    /// to match the preview panel's scale.
    ///
    /// Formula:  RelativeY = (textureHeight * (bottom − top)) / 2 / offsetMultiplier
    ///
    /// FRB convention: positive Y is up, so placing the bottom edge at 0 requires
    /// an upward offset equal to half the sprite's pixel height.
    /// </summary>
    /// <param name="frames">The frames to adjust (modified in place).</param>
    /// <param name="textureHeightPixels">Height of the source texture in pixels.</param>
    /// <param name="offsetMultiplier">
    ///     Preview offset multiplier (PL12, default 1.0).
    ///     Pass 1.0 when no multiplier is active.
    /// </param>
    public static void ApplyJustifyBottom(
        IEnumerable<AnimationFrameSave> frames,
        float textureHeightPixels,
        float offsetMultiplier = 1f)
    {
        if (offsetMultiplier == 0f) offsetMultiplier = 1f; // guard divide-by-zero

        foreach (var frame in frames)
        {
            float spriteHeightInTexels = textureHeightPixels * (frame.BottomCoordinate - frame.TopCoordinate);
            frame.RelativeY = (spriteHeightInTexels / 2f) / offsetMultiplier;
        }
    }

    /// <summary>
    /// Adjust All — applies a delta to, or overwrites, <c>RelativeX</c>/<c>RelativeY</c>
    /// of every frame in <paramref name="frames"/>.
    /// </summary>
    /// <param name="frames">The frames to adjust (modified in place).</param>
    /// <param name="deltaX">
    ///     Relative: added to existing <c>RelativeX</c>.
    ///     Absolute: written as the new <c>RelativeX</c>.
    ///     <c>null</c> = leave X unchanged.
    /// </param>
    /// <param name="deltaY">
    ///     Relative: added to existing <c>RelativeY</c>.
    ///     Absolute: written as the new <c>RelativeY</c>.
    ///     <c>null</c> = leave Y unchanged.
    /// </param>
    /// <param name="relative">
    ///     <c>true</c>  → values are added to the current offset (relative mode).<br/>
    ///     <c>false</c> → values overwrite the current offset (absolute mode).
    /// </param>
    public static void ApplyAdjustAll(
        IEnumerable<AnimationFrameSave> frames,
        float? deltaX,
        float? deltaY,
        bool relative)
    {
        foreach (var frame in frames)
        {
            if (deltaX.HasValue)
                frame.RelativeX = relative ? frame.RelativeX + deltaX.Value : deltaX.Value;

            if (deltaY.HasValue)
                frame.RelativeY = relative ? frame.RelativeY + deltaY.Value : deltaY.Value;
        }
    }
}
