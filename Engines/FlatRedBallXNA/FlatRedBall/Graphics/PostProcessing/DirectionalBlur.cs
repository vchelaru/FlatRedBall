using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Post processing to implement directional blur.
    /// </summary>
    #endregion
    public class DirectionalBlur : PostProcessingEffectBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The number of samples used in the blur filtering (static for now)
        /// </summary>
        #endregion
        private int mBlurSampleCount = 9;

        private float mBlurStandardDeviation = 2.0f;

        private float mSampleScale = 30.0f;

        private float mAngle = 0f;

        private string mTechnique = "BlurHi";

        private EffectQuality mBlurQuality = EffectQuality.High;

        private float[] mSampleWeights;
        private Vector2[] mSampleOffsets;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Controls the strength of the Gaussian blur
        /// (the standard deviation of the normal curve used for sampling nearby coordinates)
        /// </summary>
        #endregion
        public float GaussianStrength
        {
            get { return mBlurStandardDeviation; }
            set
            {
                if (mBlurStandardDeviation != value)
                {
                    mBlurStandardDeviation = value;
                    SetSampleParameters();
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the length of the directional blur (in pixels)
        /// </summary>
        #endregion
        public float Length
        {
            get { return mSampleScale; }
            set
            {
                if (mSampleScale != value)
                {
                    mSampleScale = value;
                    SetSampleParameters();
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the angle of the directional blur
        /// </summary>
        #endregion
        public float Angle
        {
            get { return mAngle; }
            set
            {
                mAngle = value;
                SetSampleParameters();
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the quality of the blur
        /// </summary>
        #endregion
        public EffectQuality Quality
        {
            get { return mBlurQuality; }
            set
            {
                if (mBlurQuality != value)
                {
                    mBlurQuality = value;

                    #region Set technique names

                    switch (mBlurQuality)
                    {
                        case EffectQuality.High:
                            mTechnique = "BlurHi";
                            mBlurSampleCount = 9;
                            break;
                        case EffectQuality.Medium:
                            mTechnique = "BlurMed";
                            mBlurSampleCount = 7;
                            break;
                        case EffectQuality.Low:
                            mTechnique = "BlurLow";
                            mBlurSampleCount = 5;
                            break;
                        default:
                            mTechnique = "BlurHi";
                            mBlurSampleCount = 9;
                            break;
                    }

                    #endregion

                    // Set the effect technique
                    mEffect.CurrentTechnique = mEffect.Techniques[mTechnique];

                    // calculate new sample values
                    SetSampleParameters();
                }
            }
        }

        #endregion

        #region Constuctor

        #region XML Docs
        /// <summary>
        /// Internal Constructor (so this class can't be instantiated externally)
        /// </summary>
        #endregion
        internal DirectionalBlur()
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
            return "DirectionalBlur";
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
            //mEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\DirectionalBlur");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>("DirectionalBlur");

            // Set the sampling parameters
            SetSampleParameters();

            // Call the base initialization method
            base.InitializeEffect();
        }

        #region XML Docs
        /// <summary>
        /// Calculates and sets the sampling parameters in the shader
        /// </summary>
        #endregion
        internal void SetSampleParameters()
        {
            float[] sampleWeights = new float[mBlurSampleCount];
            Vector2[] sampleOffsets = new Vector2[mBlurSampleCount];
            Vector2 pixelSize = PostProcessingManager.PixelSize;
            // Direction is negated on y axis to account for
            // texture coordinate y values being inverted with respect to normal values
            // Direction is then inverted so it blurs in the direction of the angle specified,
            // instead of sampling in that direction (which results in a blur in the opposite
            // direction).
            // This results in only the x value being negated.
            Vector2 direction = new Vector2(
                -(float)System.Math.Cos((double)mAngle),
                (float)System.Math.Sin((double)mAngle));
            float stepLength = mSampleScale / (float)mBlurSampleCount;

            // Calculate values using normal (gaussian) distribution
            float weightSum = 0f;
            for (int i = 0; i < mBlurSampleCount; i++)
            {
                // Get weight
                sampleWeights[i] =
                    1f / (((float)System.Math.Sqrt(2.0 * System.Math.PI) / mBlurStandardDeviation) *
                    (float)System.Math.Pow(System.Math.E,
                        System.Math.Pow((double)i, 2.0) /
                        (2.0 * System.Math.Pow((double)mBlurStandardDeviation, 2.0))));

                // Add to total weight value (for normalization)
                weightSum += sampleWeights[i];

                // Get offset
                sampleOffsets[i] =
                    (((float)i * stepLength * direction) + (Vector2.One * .5f)) * pixelSize;
            }

            // Normalize sample weights
            for (int i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= weightSum;
            }

            // Set parameters in shader
            mSampleWeights = sampleWeights;
            mSampleOffsets = sampleOffsets;
        }

        protected override void SetEffectParameters(Camera camera)
        {
            mEffect.Parameters["sampleWeights"].SetValue(mSampleWeights);
            mEffect.Parameters["sampleOffsets"].SetValue(mSampleOffsets);
        }

        #region XML Docs
        /// <summary>
        /// Draws the effect
        /// </summary>
        /// <param name="screenTexture">The screen texture</param>
        /// <param name="baseRectangle">The rectangle to draw to</param>
        /// <param name="clearColor">The background color</param>
        #endregion
        public override void Draw(
            Camera camera,
            ref Texture2D screenTexture,
            ref Rectangle baseRectangle,
            Color clearColor)
        {
            // Set the effect parameters
            SetEffectParameters(camera);

            #region Directional Blur

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref screenTexture, ref baseRectangle);

            #endregion
        }

        #endregion
    }
}
