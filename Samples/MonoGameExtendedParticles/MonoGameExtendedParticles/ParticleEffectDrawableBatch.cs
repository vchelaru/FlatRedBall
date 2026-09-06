using System;
using FlatRedBall;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Particles;

namespace MonoGameExtendedParticles
{
    /// <summary>
    /// Draws a MonoGame.Extended ParticleEffect inside FlatRedBall's render pipeline.
    /// Add an instance to the engine with SpriteManager.AddDrawableBatch, or to a Layer
    /// with SpriteManager.AddToLayer.
    /// </summary>
    /// <remarks>
    /// MonoGame.Extended simulates and draws particles in SpriteBatch coordinates, where
    /// +Y points down the screen. FlatRedBall's world is +Y up. Rather than flipping Y in
    /// the transform matrix (which would also mirror every particle texture and reverse
    /// particle rotation), this class keeps the simulation in MonoGame.Extended's own
    /// Y-down space and converts at the boundary - see <see cref="Trigger(float, float)"/>.
    /// The practical consequence is that modifiers written against screen space, such as
    /// LinearGravityModifier with a direction of Vector2.UnitY, behave the way they read.
    /// </remarks>
    public class ParticleEffectDrawableBatch : PositionedObject, IDrawableBatch
    {
        readonly ParticleEffect particleEffect;
        readonly SpriteBatch spriteBatch;

        /// <summary>
        /// The blend state used when drawing particles. Additive suits fire, sparks, and
        /// magic; AlphaBlend suits smoke and dust.
        /// </summary>
        /// <remarks>
        /// MonoGame.Extended premultiplies particle color by opacity only when the device
        /// blend state is AlphaBlend, and writes opacity into the alpha channel otherwise,
        /// so this choice changes how a given effect fades, not just how it composites.
        /// </remarks>
        public BlendState BlendState { get; set; } = BlendState.Additive;

        /// <summary>
        /// The wrapped effect, exposed so emitters and modifiers can be adjusted at runtime.
        /// </summary>
        public ParticleEffect ParticleEffect => particleEffect;

        /// <summary>
        /// FlatRedBall calls Update every frame when this is true, which is where the
        /// particle simulation is advanced.
        /// </summary>
        public bool UpdateEveryFrame => true;

        public ParticleEffectDrawableBatch(ParticleEffect particleEffect)
        {
            this.particleEffect = particleEffect ?? throw new ArgumentNullException(nameof(particleEffect));
            spriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);
        }

        /// <summary>
        /// Emits a burst of particles at a position in FlatRedBall world coordinates.
        /// </summary>
        public void Trigger(float worldX, float worldY)
        {
            // Negating Y moves the position from FlatRedBall's Y-up world into the Y-down
            // space the particle simulation runs in.
            particleEffect.Trigger(new Vector2(worldX, -worldY));
        }

        /// <summary>
        /// Advances the particle simulation. FlatRedBall calls this during its draw phase,
        /// so it must not change render targets.
        /// </summary>
        public void Update()
        {
            particleEffect.Update(TimeManager.SecondDifference);
        }

        public void Draw(Camera camera)
        {
            // FlatRedBall does not preserve render state across IDrawableBatches, so every
            // piece of state this batch depends on is set here rather than once at startup.
            spriteBatch.Begin(
                blendState: BlendState,
                samplerState: SamplerState.LinearClamp,
                transformMatrix: BuildTransform(camera));

            spriteBatch.Draw(particleEffect);

            spriteBatch.End();
        }

        /// <summary>
        /// Builds the matrix that maps the particle simulation's Y-down space onto the
        /// camera's viewport, honoring camera position and zoom.
        /// </summary>
        /// <remarks>
        /// Camera roll (RotationZ) is not handled. Effects on a rotating camera need an
        /// additional Matrix.CreateRotationZ between the translation and scale below.
        /// </remarks>
        Matrix BuildTransform(Camera camera)
        {
            var destination = camera.DestinationRectangle;
            var zoom = camera.CurrentZoom;

            // this.X/this.Y are in FlatRedBall world coordinates, so Y is negated here for
            // the same reason it is in Trigger.
            return
                Matrix.CreateTranslation(this.X - camera.X, camera.Y - this.Y, 0) *
                Matrix.CreateScale(zoom, zoom, 1) *
                Matrix.CreateTranslation(destination.Width / 2f, destination.Height / 2f, 0);
        }

        public void Destroy()
        {
            particleEffect.Dispose();
            spriteBatch.Dispose();
        }
    }
}
