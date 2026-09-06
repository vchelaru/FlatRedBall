using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExtendedParticles
{
    /// <summary>
    /// Builds a soft round particle texture in code so the sample needs no content
    /// pipeline. A real game would load a .png instead, either through
    /// FlatRedBallServices.Load or through the ContentManager that
    /// ParticleEffect.FromFile takes.
    /// </summary>
    public static class ParticleTexture
    {
        public static Texture2D CreateSoftCircle(GraphicsDevice graphicsDevice, int diameter)
        {
            var texture = new Texture2D(graphicsDevice, diameter, diameter);
            var pixels = new Color[diameter * diameter];
            var radius = diameter / 2f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    var distance = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(radius, radius));

                    // Falls off to fully transparent at the edge, squared to keep the
                    // center bright rather than muddy.
                    var falloff = Math.Clamp(1 - (distance / radius), 0f, 1f);
                    var alpha = falloff * falloff;

                    pixels[y * diameter + x] = Color.White * alpha;
                }
            }

            texture.SetData(pixels);
            return texture;
        }
    }
}
