using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

#if !WINDOWS_PHONE
using Microsoft.Xna.Framework.Storage;
#endif

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// An ordered collection of PostProcessingEffects.
    /// </summary>
    /// <remarks>
    /// Each camera in FlatRedBall contains a PostProcessingEffectCollection, so
    /// this class is not usually instantiated outside of the FlatRedBall engine.
    /// </remarks>
    #endregion
    public class PostProcessingEffectCollection
    {
        #region Fields

        internal List<PostProcessingEffectBase> mEffectCombineOrder;

        internal Bloom mBloom;

        internal Blur mBlur;

        internal DirectionalBlur mDirectionalBlur;

        internal RadialBlur mRadialBlur;

        internal Pixellate mPixellate;

        //internal Dictionary<SurfaceFormat, RenderTargetPair> mRenderTargets;

        #endregion

        #region Properties

        #region Effect accessors

        #region XML Docs
        /// <summary>
        /// Gets or sets the list of effects, which defines the order
        /// in which post-processing effects are combined.
        /// </summary>
        #endregion
        public List<PostProcessingEffectBase> EffectCombineOrder
        {
            get { return mEffectCombineOrder; }
            set { mEffectCombineOrder = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the bloom effect
        /// </summary>
        #endregion
        public Bloom Bloom
        {
            get { return mBloom; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the blur effect
        /// </summary>
        #endregion
        public Blur Blur
        {
            get { return mBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the directional blur effect
        /// </summary>
        #endregion
        public DirectionalBlur DirectionalBlur
        {
            get { return mDirectionalBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the radial blur effect
        /// </summary>
        #endregion
        public RadialBlur RadialBlur
        {
            get { return mRadialBlur; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the pixellate effect
        /// </summary>
        #endregion
        public Pixellate Pixellate
        {
            get { return mPixellate; }
        }

        #endregion

        #endregion

        #region Public Methods

        public void InitializeEffects()
        {
            // Create the render target set
            //mRenderTargets = new Dictionary<SurfaceFormat, RenderTargetPair>();

            // Load all effects
            mBloom = new Bloom();
            mBlur = new Blur();
            mDirectionalBlur = new DirectionalBlur();
            mRadialBlur = new RadialBlur();
            mPixellate = new Pixellate();

            // Create the combine order list
            mEffectCombineOrder = new List<PostProcessingEffectBase>();

            // Add effects to combine order list in default order
            mEffectCombineOrder.Add(mBlur);
            mEffectCombineOrder.Add(mDirectionalBlur);
            mEffectCombineOrder.Add(mRadialBlur);
            mEffectCombineOrder.Add(mBloom);
            mEffectCombineOrder.Add(mPixellate);

            // Initialize all effects
            mBloom.InitializeEffect();
            mBlur.InitializeEffect();
            mDirectionalBlur.InitializeEffect();
            mRadialBlur.InitializeEffect();
            mPixellate.InitializeEffect();
        }

        #endregion
    }
}