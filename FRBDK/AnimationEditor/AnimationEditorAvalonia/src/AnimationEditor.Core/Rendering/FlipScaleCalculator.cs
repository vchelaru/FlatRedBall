namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Computes the (scaleX, scaleY) factors needed to flip a rendered frame.
/// Each factor is +1 (no flip) or -1 (mirror around the pivot).
/// Extracted from <c>PreviewControl.DrawFrame</c> so the decision logic can
/// be unit-tested without a SkiaSharp canvas.
/// </summary>
public static class FlipScaleCalculator
{
    /// <summary>
    /// Returns the scale factors to pass to <c>canvas.Scale(sx, sy, pivotX, pivotY)</c>
    /// before rendering a frame.
    /// </summary>
    public static (float ScaleX, float ScaleY) Compute(bool flipHorizontal, bool flipVertical)
        => (flipHorizontal ? -1f : 1f, flipVertical ? -1f : 1f);

    /// <summary>
    /// Returns <c>true</c> when any flip is active (i.e. a Save/Scale/Restore is required).
    /// </summary>
    public static bool IsFlipped(bool flipHorizontal, bool flipVertical)
        => flipHorizontal || flipVertical;
}
