using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Post processing effect which can be used to perform a radial blur.
    /// </summary>
    #endregion
    public class RadialBlur : PostProcessingEffectBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The number of samples used in the blur filtering (static for now)
        /// </summary>
        #endregion
        private int mBlurSampleCount = 9;

        private float mBlurStandardDeviation = 2.0f;

        private float mSampleScale = 20f;
        private Vector2 mRadialSource = new Vector2(.5f, .5f);

        private EffectParameter mParamSampleScale;
        private EffectParameter mParamRadialSource;

        private string mTechnique = "BlurHi";

        private EffectQuality mBlurQuality = EffectQuality.High;

        private float[] mSampleWeights;

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
            set { mSampleScale = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the x value of the point from which the blur originates (in UV coordinates)
        /// </summary>
        #endregion
        public float CenterX
        {
            get { return mRadialSource.X; }
            set { mRadialSource.X = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the y value of the point from which the blur originates (in UV coordinates)
        /// </summary>
        #endregion
        public float CenterY
        {
            get { return mRadialSource.Y; }
            set { mRadialSource.Y = value; }
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
        internal RadialBlur()
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
            return "RadialBlur";
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
            // Call the base initialization method
            base.InitializeEffect();

            // Load the effect
            // If modifying the shaders uncomment the following file:
            //mEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\RadialBlur");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>("RadialBlur");

            // Get effect parameters
            mParamRadialSource = mEffect.Parameters["radialSource"];
            mParamSampleScale = mEffect.Parameters["sampleScale"];

            // Set the sampling parameters
            SetSampleParameters();
        }

        #region XML Docs
        /// <summary>
        /// Calculates and sets the sampling parameters in the shader
        /// </summary>
        #endregion
        internal void SetSampleParameters()
        {
            float[] sampleWeights = new float[mBlurSampleCount];

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
            }

            // Normalize sample weights
            for (int i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= weightSum;
            }

            // Set parameters in shader
            mSampleWeights = sampleWeights;
        }

        protected override void SetEffectParameters(Camera camera)
        {
            mParamRadialSource.SetValue(mRadialSource);
            mParamSampleScale.SetValue(mSampleScale);

            mEffect.Parameters["sampleWeights"].SetValue(mSampleWeights);
        }

        #endregion
    }
}
