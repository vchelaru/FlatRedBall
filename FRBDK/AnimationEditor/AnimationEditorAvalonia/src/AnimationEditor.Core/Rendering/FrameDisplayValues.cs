using FlatRedBall.Content.AnimationChain;
using System;

namespace AnimationEditor.Core.Rendering
{
    /// <summary>
    /// Pure conversion helpers: UV texture coordinates → display values
    /// (pixel integers or sprite-sheet tile indices) shown in the property inspector.
    /// All methods are stateless and easily unit-tested.
    /// </summary>
    public static class FrameDisplayValues
    {
        // ── UV → pixel ────────────────────────────────────────────────────────

        /// <summary>Pixel X of the frame's left edge.</summary>
        public static int GetPixelX(AnimationFrameSave frame, int textureWidth)
            => (int)Math.Round(frame.LeftCoordinate * textureWidth);

        /// <summary>Pixel Y of the frame's top edge.</summary>
        public static int GetPixelY(AnimationFrameSave frame, int textureHeight)
            => (int)Math.Round(frame.TopCoordinate * textureHeight);

        /// <summary>Pixel width of the frame region (minimum 1).</summary>
        public static int GetPixelWidth(AnimationFrameSave frame, int textureWidth)
            => Math.Max(1, (int)Math.Round((frame.RightCoordinate - frame.LeftCoordinate) * textureWidth));

        /// <summary>Pixel height of the frame region (minimum 1).</summary>
        public static int GetPixelHeight(AnimationFrameSave frame, int textureHeight)
            => Math.Max(1, (int)Math.Round((frame.BottomCoordinate - frame.TopCoordinate) * textureHeight));

        // ── UV → sprite-sheet tile index ──────────────────────────────────────

        /// <summary>
        /// Zero-based tile column index for the frame's left edge.
        /// Truncates to an integer (same behaviour as the old WinForms tool).
        /// </summary>
        public static int GetTileX(AnimationFrameSave frame, int cellWidth, int textureWidth)
        {
            if (cellWidth <= 0 || textureWidth <= 0) return 0;
            return (int)(frame.LeftCoordinate * textureWidth / cellWidth);
        }

        /// <summary>
        /// Zero-based tile row index for the frame's top edge.
        /// </summary>
        public static int GetTileY(AnimationFrameSave frame, int cellHeight, int textureHeight)
        {
            if (cellHeight <= 0 || textureHeight <= 0) return 0;
            return (int)(frame.TopCoordinate * textureHeight / cellHeight);
        }
    }
}
