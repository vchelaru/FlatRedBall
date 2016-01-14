using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.PostProcessing
{
    /// <summary>
    /// Quality of an effect - used for effect with varying quality levels
    /// </summary>
    public enum EffectQuality
    {
        #region XML Docs
        /// <summary>
        /// High quality - uses 9 samples in horizontal and vertical directions
        /// </summary>
        #endregion
        High,
        #region XML Docs
        /// <summary>
        /// Medium quality - uses 7 samples in horizontal and vertical directions
        /// </summary>
        #endregion
        Medium,
        #region XML Docs
        /// <summary>
        /// Low quality - uses 5 samples in horizontal and vertical directions
        /// </summary>
        #endregion
        Low
    }

    #region XML Docs
    /// <summary>
    /// Base class for post-processing effects
    /// </summary>
    #endregion
    public class PostProcessingEffectBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// Whether or not this effect is enabled
        /// </summary>
        #endregion
        protected bool mEnabled = false;

        #region XML Docs
        /// <summary>
        /// The effect for this post processing effect
        /// </summary>
        #endregion
        protected Effect mEffect;

        #region XML Docs
        /// <summary>
        /// The surface format for the render target for this effect
        /// (default is SurfaceFormat.Color)
        /// </summary>
        #endregion
        protected SurfaceFormat mRenderTargetFormat = SurfaceFormat.Color;

        #region XML Docs
        /// <summary>
        /// The texture that was rendered by the render target
        /// </summary>
        #endregion
        public Texture2D mTexture;

        #region XML Docs
        /// <summary>
        /// The screen rectangle
        /// </summary>
        #endregion
        protected Rectangle mScreenRectangle;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Enables or disables this effect
        /// </summary>
        #endregion
        public bool Enabled
        {
            get { return mEnabled; }
            set 
            {
                if (value != mEnabled)
                {
                    mEnabled = value;
                    if (mEnabled)
                    {
                        InitializeEffect();
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets the shader used to render this effect
        /// </summary>
        #endregion
        public Effect Effect
        {
            get { return mEffect; }
            set { mEffect = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the texture that was rendered
        /// </summary>
        #endregion
        public Texture2D Texture
        {
            get { return mTexture; }
        }

        #endregion

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates a basic post-processing effect
        /// </summary>
        #endregion
        public PostProcessingEffectBase()
        { }

        #region XML Docs
        /// <summary>
        /// Loads the specified effect as a default post-processing effect
        /// </summary>
        #endregion
        public PostProcessingEffectBase(String effectPath, String contentManagerName)
        {
            InitializeEffect();
            mEffect = FlatRedBallServices.Load<Effect>(effectPath, contentManagerName);
            mEnabled = true;
        }

        #region XML Docs
        /// <summary>
        /// Loads the specified effect as a default post-processing effect
        /// </summary>
        #endregion
        public PostProcessingEffectBase(String effectPath)
        {
            InitializeEffect();
            mEffect = FlatRedBallServices.Load<Effect>(effectPath);
            mEnabled = true;
        }

        #region XML Docs
        /// <summary>
        /// Uses the specified effect as a default post-processing effect
        /// </summary>
        #endregion
        public PostProcessingEffectBase(Effect effect)
        {
            InitializeEffect();
            mEffect = effect;
            mEnabled = true;
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Reports this effect's name as a string
        /// </summary>
        /// <returns>The effect's name as a string</returns>
        #endregion
        public override string ToString()
        {
            return "Basic Post-processing Effect";
        }

        #region XML Docs
        /// <summary>
        /// Update the post processing effect
        /// </summary>
        #endregion
        public virtual void Update()
        {
        }

        #endregion

        #region Internal Methods

        #region XML Docs
        /// <summary>
        /// Loads the effect and initializes values in the shader
        /// Base method sets the screen rectangle to full-screen
        /// </summary>
        #endregion
        public virtual void InitializeEffect()
        {
            UpdateToScreenSize();
        }

        internal void UpdateToScreenSize()
        {
            // Initialize the screen rectangle to full-screen
            mScreenRectangle = new Rectangle(
                FlatRedBallServices.GraphicsDevice.Viewport.X,
                FlatRedBallServices.GraphicsDevice.Viewport.Y,
                FlatRedBallServices.mGraphics.PreferredBackBufferWidth,
                FlatRedBallServices.mGraphics.PreferredBackBufferHeight);

            // Ensure the render targets exist
            Renderer.InitializeRenderTargets(mRenderTargetFormat);
        }

        #region XML Docs
        /// <summary>
        /// Sets the effect's parameters before drawing
        /// (override and implement this in your own effects)
        /// </summary>
        #endregion
        protected virtual void SetEffectParameters(Camera camera)
        {
        }

        protected void DrawToTexture(
            Camera camera,
            ref Texture2D destinationTexture,
            Color clearColor,
            ref Texture2D screenTexture,
            ref Rectangle sourceRectangle)
        {
            // Start the render target

#if WINDOWS_PHONE
            throw new NotImplementedException();
#else
            //camera.mRenderTargetTextures[0].SetOnDevice();
            Renderer.PostProcessRenderTargets[(int)mRenderTargetFormat].SwitchTarget();
#endif

            FlatRedBallServices.GraphicsDevice.Clear(clearColor);

#if XNA4
            // Start the sprite batch
            PostProcessingManager.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
#else
            // Start the sprite batch
            PostProcessingManager.SpriteBatch.Begin(
                SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
            // Draw using the current effect and technique
            mEffect.Begin();
#endif
            


            for (int i = 0; i < mEffect.CurrentTechnique.Passes.Count; i++)
            {
#if !XNA4
                mEffect.CurrentTechnique.Passes[i].Begin();
#endif
                PostProcessingManager.SpriteBatch.Draw(
                    screenTexture,
                    sourceRectangle,
                    sourceRectangle, // Use null to draw the entire source Rectangle
                    Color.White);
#if !XNA4
                mEffect.CurrentTechnique.Passes[i].End();
#endif
            }
#if !XNA4
            mEffect.End();
#endif
            // End the sprite batch
            PostProcessingManager.SpriteBatch.End();

#if WINDOWS_PHONE
            throw new NotImplementedException();
#else
            // Resolve this render target and get the texture
            Renderer.PostProcessRenderTargets[(int)mRenderTargetFormat].SwitchTarget(ref destinationTexture);
            //FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, null);
            //destinationTexture = camera.mRenderTargetTextures[0].RenderTarget.GetTexture();
#endif
        }

        internal void DrawToCurrentTarget(Camera camera,
                                        ref Texture2D texture,
                                        Color clearColor,
                                        ref Rectangle sourceRectangle)
        {
            SetEffectParameters(camera);
            FlatRedBallServices.GraphicsDevice.Clear(clearColor);

#if XNA4
            PostProcessingManager.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
#else
            // Start the sprite batch

            PostProcessingManager.SpriteBatch.Begin(
                SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
#endif

#if !XNA4
            // Draw using the current effect and technique
            mEffect.Begin();
#endif

            for (int i = 0; i < mEffect.CurrentTechnique.Passes.Count; i++)
            {
#if !XNA4                
                mEffect.CurrentTechnique.Passes[i].Begin();
#endif

                PostProcessingManager.SpriteBatch.Draw(
                    texture,
                    sourceRectangle,
                    sourceRectangle, // Use null to draw the entire source Rectangle
                    Color.White);

#if !XNA4                   
                mEffect.CurrentTechnique.Passes[i].End();
#endif
            }


#if !XNA4  
            mEffect.End();
#endif
            // End the sprite batch
            PostProcessingManager.SpriteBatch.End();

        }

        /// <summary>
        /// This function MUST be overridden for any effects that make passes with multiple techniques.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="texture"></param>
        /// <param name="clearColor"></param>
        /// <param name="sourceRectangle"></param>
        internal virtual void DrawToCameraRenderTarget(Camera camera,
                                        ref Texture2D texture,
                                        Color clearColor,
                                        ref Rectangle sourceRectangle)
        {
#if XNA4
            throw new NotImplementedException();
#else
            SetEffectParameters(camera);
            camera.mRenderTargetTexture.SetOnDevice();
            FlatRedBallServices.GraphicsDevice.Clear(clearColor);

            // Start the sprite batch
            PostProcessingManager.SpriteBatch.Begin(
                SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);

            // Draw using the current effect and technique
            mEffect.Begin();
            for (int i = 0; i < mEffect.CurrentTechnique.Passes.Count; i++)
            {
                mEffect.CurrentTechnique.Passes[i].Begin();

                PostProcessingManager.SpriteBatch.Draw(
                    texture,
                    sourceRectangle,
                    sourceRectangle, // Use null to draw the entire source Rectangle
                    Color.White);

                mEffect.CurrentTechnique.Passes[i].End();
            }
            mEffect.End();

            // End the sprite batch
            PostProcessingManager.SpriteBatch.End();
#endif
        }

        #region XML Docs
        /// <summary>
        /// Draws the effect using the drawn screen texture
        /// Base method draws screen texture with current effect to current screen rectangle
        /// </summary>
        /// <param name="camera">The camera to use when drawing.</param>
        /// <param name="screenTexture">The screen texture.</param>
        /// <param name="baseRectangle">The rectangle to draw to.</param>
        /// <param name="clearColor">The background color.</param>
        #endregion
        public virtual void Draw(
            Camera camera,
            ref Texture2D screenTexture,
            ref Rectangle baseRectangle,
            Color clearColor)
        {
            SetEffectParameters(camera);
            DrawToTexture(camera, ref mTexture, clearColor, ref screenTexture, ref baseRectangle);
        }

        #region XML Docs
        /// <summary>
        /// Draws this effect if it is enabled (helper method)
        /// </summary>
        /// <param name="camera">The camera to use when drawing</param>
        /// <param name="clearColor">The color to use as transparent</param>
        /// <param name="screenTexture">The screen texture</param>
        /// <param name="baseRectangle">The rectangle to draw to</param>
        #endregion
        public void DrawIfEnabled(
            Camera camera,
            ref Texture2D screenTexture,
            ref Rectangle baseRectangle,
            Color clearColor)
        {
            if (mEnabled)
            {
                this.Draw(camera, ref screenTexture, ref baseRectangle, clearColor);
            }
        }

        #endregion
    }
}
