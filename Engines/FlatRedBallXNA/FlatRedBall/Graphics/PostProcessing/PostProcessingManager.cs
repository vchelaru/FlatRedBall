using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Static class providing access to post processing classes including the out-of-the-box
    /// post processing features.
    /// </summary>
    #endregion
    public static class PostProcessingManager
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The SpiteBatch used to render the resulting texture
        /// after post pocessing is performed.  This uses the Camera's
        /// RenderTagetTexture for the given Camera
        /// </summary>
        #endregion
        private static SpriteBatch mSpriteBatch;

        private static bool mIsInitialized = false;

        #region Shared parameters

        private static Effect mSharedParametersEffect;

        private static EffectParameter mPixelSize;

        #endregion

        #endregion

        #region Properties

        // Vic says:  This used to be public...why?
        internal static SpriteBatch SpriteBatch
        {
            get { return mSpriteBatch; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the PostPocessingManager has been initialized yet.
        /// </summary>
        #endregion
        public static bool IsInitialized
        {
            get { return mIsInitialized; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the currently set pixel size
        /// </summary>
        #endregion
        public static Vector2 PixelSize
        {
            get {
                try
                {
                    return mPixelSize.GetValueVector2();
                }
                catch 
                {
                    return
                        Vector2.One /
                        (new Vector2(
                        (float)FlatRedBallServices.ClientWidth,
                        (float)FlatRedBallServices.ClientHeight));
                }
            }
        }

        #region Effect accessors for default camera

#if !XNA4

        #region XML Docs
        /// <summary>
        /// Gets or sets the list of effects, which defines the order
        /// in which post-processing effects are combined.
        /// </summary>
        #endregion
        public static List<PostProcessingEffectBase> EffectCombineOrder
        {
            get { return SpriteManager.Camera.PostProcessing.mEffectCombineOrder; }
            set { SpriteManager.Camera.PostProcessing.mEffectCombineOrder = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the bloom effect
        /// </summary>
        #endregion
        public static Bloom Bloom
        {
            get { return SpriteManager.Camera.PostProcessing.mBloom; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the blur effect
        /// </summary>
        #endregion
        public static Blur Blur
        {
            get { return SpriteManager.Camera.PostProcessing.mBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the directional blur effect
        /// </summary>
        #endregion
        public static DirectionalBlur DirectionalBlur
        {
            get { return SpriteManager.Camera.PostProcessing.mDirectionalBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the radial blur effect
        /// </summary>
        #endregion
        public static RadialBlur RadialBlur
        {
            get { return SpriteManager.Camera.PostProcessing.mRadialBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the pixellate effect
        /// </summary>
        #endregion
        public static Pixellate Pixellate
        {
            get { return SpriteManager.Camera.PostProcessing.mPixellate; }
        }
#endif
        #endregion

        #endregion

        #region Constructor / Initialization

        static PostProcessingManager()
        {
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Loads all effects and initializes effect variables
        /// </summary>
        /// <remarks>
        /// This method is automatically called when FlatRedBall is Initialized if
        /// post processing is suppoted in the current build of the engine. 
        /// </remarks>
        #endregion
        internal static void InitializeEffects()
        {
#if !MONOGAME
            // Create the sprite batch
            mSpriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

            // Shared parameters
            #region Get and initialize shared parameters

            // Get the effect

            // If modifying the shaders uncomment the following file:
            //mSharedParametersEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\Blur");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mSharedParametersEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>(@"Blur");

            // Get shared effect parameters
            mPixelSize = mSharedParametersEffect.Parameters["pixelSize"];

            // Initialize shared effect parameters
            mPixelSize.SetValue(new Vector2(
                1f / (float)FlatRedBallServices.ClientWidth,
                1f / (float)FlatRedBallServices.ClientHeight));

            // Commit
#if !XNA4
            mSharedParametersEffect.CommitChanges();
#endif
            #endregion
#endif
            mIsInitialized = true;
        }

        #endregion

        #region Internal Methods

        internal static void RefreshPostProcessingSurfaceSizes()
        {

#if XNA4
            throw new NotImplementedException();
#else
            for (int c = 0; c < SpriteManager.Cameras.Count; c++)
            {                
                for (int i = 0; i < SpriteManager.Cameras[c].PostProcessing.EffectCombineOrder.Count; i++)
                {
                    Camera camera = SpriteManager.Cameras[c];
                    camera.PostProcessing.EffectCombineOrder[i].UpdateToScreenSize();
                }
            }
#endif

        }

        public static void Update()
        {
            
#if XNA4
            throw new NotImplementedException();
#else
            for (int c = 0; c < SpriteManager.Cameras.Count; c++)
            {
                for (int i = 0; i < SpriteManager.Cameras[c].PostProcessing.EffectCombineOrder.Count; i++)
                {
                    SpriteManager.Cameras[c].PostProcessing.EffectCombineOrder[i].Update();
                }
            }
#endif
        }

        internal static void DrawPostProcessing(Camera camera)
        {

#if !XNA4
            #region Draw Effects

            // Get the camera's clear color (with alpha set to 0)
            // This will ensure color bleeding from the background
            // produces the correct results
            Color clearColor = new Color(
                camera.BackgroundColor.R,
                camera.BackgroundColor.G,
                camera.BackgroundColor.B,
                0
                );

            // The last effect drawn
            PostProcessingEffectBase lastEffect = null;

            //Rectangle destinationRectangle = new Rectangle(0, 0, camera.mDestinationRectangle.Width, camera.mDestinationRectangle.Height);
            // Vic says:  doing this to avoid the New call.  Kyle, this should result in the same thing.  Is it ok?
            Rectangle destinationRectangle = camera.mDestinationRectangle;
            destinationRectangle.X = 0;
            destinationRectangle.Y = 0;

            // Draw the effects
            int effectCombineOrderCount = camera.PostProcessing.mEffectCombineOrder.Count;
            int lastEffectIndex = -1;

            //Determine the index of the last effect
            for (int i = effectCombineOrderCount - 1; i >= 0; --i)
            {
                if (camera.PostProcessing.mEffectCombineOrder[i].Enabled)
                {
                    lastEffectIndex = i;
                    break;
                }
            }

            //This section now only draws the effects leading up to (but not including) the lastEffect. lastEffect is
            //drawn directly to the final camera texture rather than being copied later on.
            if (lastEffectIndex != -1)
            {
                //This can be skipped if there isn't an effect that is enabled
                for (int i = 0; i < lastEffectIndex; i++)
                {
                    PostProcessingEffectBase effectBase = camera.PostProcessing.mEffectCombineOrder[i];
                    if (lastEffect == null)
                    {
                        // Render with the screen texture
                        Texture2D cameraTexture = camera.mRenderTargetTextures[(int)camera.RenderOrder[0]].Texture;
                        if (effectBase.Enabled)
                        {
                            effectBase.Draw(camera, ref cameraTexture, ref destinationRectangle, clearColor);
                        }
                    }
                    else
                    {
                        // Render with the accumulated texture
                        effectBase.DrawIfEnabled(camera, ref lastEffect.mTexture, ref destinationRectangle, clearColor);
                    }

                    // Get the last rendered effect
                    if (effectBase.Enabled)
                    {
                        lastEffect = effectBase;
                    }
                }
            }
            #endregion

            #region Draw final render to camera texture

            // Check if target is large enough
            if (camera.mRenderTargetTexture.Width < camera.mDestinationRectangle.Width ||
                camera.mRenderTargetTexture.Height < camera.mDestinationRectangle.Height)
            {
                camera.mRenderTargetTexture.SetSize(camera.mDestinationRectangle.Width, camera.mDestinationRectangle.Height);

                if (camera.mRenderTargetTextures != null)
                {
                    foreach (RenderTargetTexture rtt in camera.mRenderTargetTextures.Values)
                    {
                        if (rtt.Width != camera.mDestinationRectangle.Width ||
                            rtt.Height != camera.mDestinationRectangle.Height)
                        {
                            rtt.SetSize(camera.mDestinationRectangle.Width, camera.mDestinationRectangle.Height);
                        }
                    }
                }

            }

            // Set and clear the render target
//            FlatRedBallServices.GraphicsDevice.SetRenderTarget(0, camera.mRenderTargetTexture.RenderTarget);


            if (lastEffectIndex == -1)
            {
                camera.mRenderTargetTexture.SetOnDevice();
                FlatRedBallServices.GraphicsDevice.Clear(clearColor);
                //If no PostProcessing was done, just copy directly from the Renderer's RenderTarget
                Renderer.mSpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

                if (camera.DrawsToScreen)
                {
                    Renderer.mSpriteBatch.Draw(
                        camera.mRenderTargetTextures[(int)camera.RenderOrder[0]].Texture,
                        destinationRectangle,
                        destinationRectangle,
                        Color.White);
                }

                // End the sprite batch
                Renderer.mSpriteBatch.End();
            }
            else
            {
                //If there is PostProcessing to do, have it rendered to the camera's final texture
                PostProcessingEffectBase finalEffect = camera.PostProcessing.mEffectCombineOrder[lastEffectIndex];

                if (lastEffect == null)
                {
                    finalEffect.DrawToCameraRenderTarget(
                        camera,
                        ref camera.mRenderTargetTextures[(int)camera.RenderOrder[0]].Texture,
                        Color.White,
                        ref destinationRectangle);
                }
                else
                {
                    finalEffect.DrawToCameraRenderTarget(
                        camera,
                        ref lastEffect.mTexture,
                        Color.White,
                        ref destinationRectangle);
                }
            }

            /*
            // Start the sprite batch
            Renderer.mSpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            if (lastEffect == null)
            {
                if (camera.DrawsToScreen)
                {
                    Renderer.mSpriteBatch.Draw(
                        camera.mRenderTargetTextures[(int)camera.RenderOrder[0]].Texture,
                        destinationRectangle,
                        destinationRectangle,
                        Color.White);
                }
            }
            else
            {
                Renderer.mSpriteBatch.Draw(
                    lastEffect.mTexture,
                    destinationRectangle,
                    destinationRectangle,
                    Color.White);
            }

            // Draw to screen


            // End the sprite batch
            Renderer.mSpriteBatch.End();
            */
            // Get the rendered texture
            camera.mRenderTargetTexture.ResolveTexture();

            #endregion

#endif
        }

        #endregion
    }
}
