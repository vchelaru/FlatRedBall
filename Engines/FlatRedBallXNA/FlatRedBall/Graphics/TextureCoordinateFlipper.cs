using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics
{
    /// <summary>
    /// Pure UV-corner permutation logic shared by all texture-flip combinations (horizontal, vertical, diagonal).
    /// </summary>
    /// <remarks>
    /// Diagonal, horizontal, and vertical flips are all reflections (as opposed to rotations), so they can be
    /// composed by swapping which corner's texture coordinate lands on which quad corner. Horizontal and vertical
    /// commute with each other, but diagonal does not commute with either - flips are always applied in the fixed
    /// order diagonal, then horizontal, then vertical, so combinations round-trip consistently.
    /// </remarks>
    public static class TextureCoordinateFlipper
    {
        public static (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft) Flip(
            Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft,
            bool flipDiagonal, bool flipHorizontal, bool flipVertical)
        {
            if (flipDiagonal)
            {
                (topRight, bottomLeft) = (bottomLeft, topRight);
            }

            if (flipHorizontal)
            {
                (topLeft, topRight) = (topRight, topLeft);
                (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
            }

            if (flipVertical)
            {
                (topLeft, bottomLeft) = (bottomLeft, topLeft);
                (topRight, bottomRight) = (bottomRight, topRight);
            }

            return (topLeft, topRight, bottomRight, bottomLeft);
        }
    }
}
