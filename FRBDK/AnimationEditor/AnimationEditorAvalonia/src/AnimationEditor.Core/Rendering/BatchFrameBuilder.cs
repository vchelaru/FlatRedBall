using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Builds a batch of new frames to append to a chain (F12 — Add Multiple Frames).
/// All UV arithmetic is isolated here so it can be unit-tested independently.
/// </summary>
public static class BatchFrameBuilder
{
    /// <summary>
    /// Result produced by <see cref="BuildBatch"/>.
    /// </summary>
    public sealed class BatchResult
    {
        /// <summary>The newly created frames (not yet added to any chain).</summary>
        public IReadOnlyList<AnimationFrameSave> Frames { get; init; } = [];

        /// <summary>
        /// <c>true</c> when <see cref="incrementUV"/> was <c>true</c> and at least one
        /// frame's auto-incremented coordinates would have exceeded [0, 1].
        /// The frames are still created but the caller may want to warn the user.
        /// </summary>
        public bool ExceededTextureBounds { get; init; }
    }

    /// <summary>
    /// Creates <paramref name="count"/> frames, optionally auto-incrementing the UV
    /// cell position row-by-row based on the last frame already in the chain.
    /// </summary>
    /// <param name="lastFrame">
    ///     The last frame already in the chain, used as the copy-from source.
    ///     <c>null</c> is allowed (all frames get default UV 0/1/0/1, FrameLength 0.1 s).
    /// </param>
    /// <param name="count">Number of frames to create (must be ≥ 1).</param>
    /// <param name="incrementUV">
    ///     <c>true</c> → advance the UV cell left-to-right, wrapping to the next row;
    ///     <c>false</c> → each new frame copies the UV of <paramref name="lastFrame"/>.
    /// </param>
    public static BatchResult BuildBatch(
        AnimationFrameSave? lastFrame,
        int count,
        bool incrementUV)
    {
        if (count <= 0)
            return new BatchResult { Frames = [] };

        float frameWidth  = lastFrame != null ? lastFrame.RightCoordinate  - lastFrame.LeftCoordinate  : 1f;
        float frameHeight = lastFrame != null ? lastFrame.BottomCoordinate - lastFrame.TopCoordinate : 1f;

        // Guard: degenerate cell sizes fall back to full-texture
        if (frameWidth  <= 0f) frameWidth  = 1f;
        if (frameHeight <= 0f) frameHeight = 1f;

        float framesPerRow = 1f / frameWidth; // may be fractional — we'll round below

        var result = new List<AnimationFrameSave>(count);
        bool exceeded = false;

        // Running position — starts at the last frame's position
        float currentLeft   = lastFrame?.LeftCoordinate   ?? 0f;
        float currentTop    = lastFrame?.TopCoordinate    ?? 0f;
        float currentRight  = lastFrame?.RightCoordinate  ?? 1f;
        float currentBottom = lastFrame?.BottomCoordinate ?? 1f;

        for (int i = 0; i < count; i++)
        {
            if (incrementUV && lastFrame != null)
            {
                // Advance to the next cell
                int currentCellInRow = RoundToInt(currentLeft / frameWidth);
                int totalCellsPerRow = RoundToInt(framesPerRow);

                if (currentCellInRow + 1 < totalCellsPerRow)
                {
                    // Move right within the same row
                    currentLeft  = (currentCellInRow + 1) * frameWidth;
                    currentRight = currentLeft + frameWidth;
                }
                else
                {
                    // Wrap to start of next row
                    currentLeft   = 0f;
                    currentRight  = frameWidth;
                    currentTop    += frameHeight;
                    currentBottom += frameHeight;
                }

                if (currentBottom > 1f + 1e-5f)
                    exceeded = true;
            }

            var frame = new AnimationFrameSave
            {
                TextureName      = lastFrame?.TextureName      ?? string.Empty,
                FrameLength      = lastFrame?.FrameLength      ?? 0.1f,
                LeftCoordinate   = currentLeft,
                RightCoordinate  = currentRight,
                TopCoordinate    = currentTop,
                BottomCoordinate = currentBottom,
                ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
            };

            result.Add(frame);
        }

        return new BatchResult { Frames = result, ExceededTextureBounds = exceeded };
    }

    private static int RoundToInt(float value) => (int)Math.Round(value);
}
