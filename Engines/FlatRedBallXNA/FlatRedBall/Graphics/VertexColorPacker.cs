using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics
{
    /// <summary>
    /// Packs a Sprite's unpacked (signed float, per-channel) vertex color into the packed uint used
    /// by the unsigned-normalized-byte4 GPU vertex color format.
    /// </summary>
    public static class VertexColorPacker
    {
        public static uint Pack(Vector4 color, ColorOperation colorOperation)
        {
            if (colorOperation == ColorOperation.AddSubtract)
            {
                // RGB store a signed [-1, 1] value in an unsigned-normalized byte, using the standard
                // bias/scale trick (same as tangent-space normal maps): encode (v * 0.5 + 0.5) here,
                // decode (v * 2 - 1) in the AddSubtract pixel shader technique. Alpha is not part of
                // the signed trick, so it packs the same as every other ColorOperation.
                return
                    ((uint)(255 * (color.X * 0.5f + 0.5f))) +
                    (((uint)(255 * (color.Y * 0.5f + 0.5f))) << 8) +
                    (((uint)(255 * (color.Z * 0.5f + 0.5f))) << 16) +
                    (((uint)(255 * color.W)) << 24);
            }

            return
                ((uint)(255 * color.X)) +
                (((uint)(255 * color.Y)) << 8) +
                (((uint)(255 * color.Z)) << 16) +
                (((uint)(255 * color.W)) << 24);
        }
    }
}
