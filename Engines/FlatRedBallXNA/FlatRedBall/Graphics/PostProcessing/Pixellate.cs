using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Performs a pixellate effect on the screen - effectively reducing the resolution
    /// without recreating render targets.
    /// </summary>
    /// <remarks>
    /// This effect was used frequently on the Super Nintendo, such as during
    /// transitions.
    /// </remarks>
    #endregion
    public class Pixellate : PostProcessingEffectBase
    {
        #region Fields

        private float mStrength = 10.0f;
        private EffectParameter mParamStrength;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the strength of the pixellation (pixel size)
        /// </summary>
        public float Strength
        {
            get { return mStrength; }
            set { mStrength = value; }
        }

        #endregion

        #region Constuctor

        #region XML Docs
        /// <summary>
        /// Internal Constructor (so this class can't be instantiated externally)
        /// </summary>
        #endregion
        internal Pixellate()
        { }

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
            return "Pixellate";
        }

        #endregion

        #region Internal Methods

        #region XML Docs
        /// <summary>
        /// Loads the effect
        /// </summary>
        #endregion
        public override void InitializeEffect()
        {
            // Load the effect
            // If modifying the shaders uncomment the following file:
            //mEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\Pixellate");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>("Pixellate");

            // Get and initialize the effect parameter
            mParamStrength = mEffect.Parameters["strength"];

            // Call the base initialization method
            base.InitializeEffect();
        }

        protected override void SetEffectParameters(Camera camera)
        {
            mParamStrength.SetValue(mStrength);

#if !XNA4
            mEffect.CommitChanges();
#endif
        }

        #endregion
    }
}
