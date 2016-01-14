using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Post processing effect which can be used to blur the screen.
    /// </summary>
    #endregion
    public class Blur : PostProcessingEffectBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The number of samples used in the blur filtering (static for now)
        /// </summary>
        #endregion
        private int mBlurSampleCount = 9;

        private float mBlurStandardDeviation = 1.0f;

        private float mSampleScale = 1.0f;

        private string mTechniqueHorizontal = "BlurHorizontalHi";
        private string mTechniqueVertical = "BlurVerticalHi";
        private EffectQuality mBlurQuality = EffectQuality.High;

        private float[] mSampleWeights;
        private Vector2[] mSampleOffsetsHor;
        private Vector2[] mSampleOffsetsVer;

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
        /// Controls the strength of the linear blur
        /// (Scales the sampling points to nearer or farther away)
        /// </summary>
        #endregion
        public float LinearStrength
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
                            mTechniqueHorizontal = "BlurHorizontalHi";
                            mTechniqueVertical = "BlurVerticalHi";
                            mBlurSampleCount = 9;
                            break;
                        case EffectQuality.Medium:
                            mTechniqueHorizontal = "BlurHorizontalMed";
                            mTechniqueVertical = "BlurVerticalMed";
                            mBlurSampleCount = 7;
                            break;
                        case EffectQuality.Low:
                            mTechniqueHorizontal = "BlurHorizontalLow";
                            mTechniqueVertical = "BlurVerticalLow";
                            mBlurSampleCount = 5;
                            break;
                        default:
                            mTechniqueHorizontal = "BlurHorizontalHi";
                            mTechniqueVertical = "BlurVerticalHi";
                            mBlurSampleCount = 9;
                            break;
                    }

                    #endregion

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
        internal Blur()
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
            return "Blur";
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
            //mEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\Blur");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>("Blur");

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
            int sampleMid = (mBlurSampleCount / 2);
            float[] sampleWeights = new float[mBlurSampleCount];
            Vector2[] sampleOffsetsHor = new Vector2[mBlurSampleCount];
            Vector2[] sampleOffsetsVer = new Vector2[mBlurSampleCount];
            Vector2 pixelSize = PostProcessingManager.PixelSize;

            // Calculate values using normal (gaussian) distribution
            float weightSum = 0f;
            for (int i = 0; i < mBlurSampleCount; i++)
            {
                // Get weight
                sampleWeights[i] =
                    1f / (((float)System.Math.Sqrt(2.0 * System.Math.PI) / mBlurStandardDeviation) *
                    (float)System.Math.Pow(System.Math.E,
                        System.Math.Pow((double)(i - sampleMid), 2.0) /
                        (2.0 * System.Math.Pow((double)mBlurStandardDeviation, 2.0))));

                // Add to total weight value (for normalization)
                weightSum += sampleWeights[i];

                // Get offset
                sampleOffsetsHor[i] = (new Vector2(
                    (float)(i - sampleMid) * 2.0f * mSampleScale + 0.5f, 0.5f)) * pixelSize;
                sampleOffsetsVer[i] = (new Vector2(
                    0.5f, (float)(i - sampleMid) * 2.0f * mSampleScale + 0.5f)) * pixelSize;
            }

            // Normalize sample weights
            for (int i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= weightSum;
            }

            // Set parameters in shader
            mSampleWeights = sampleWeights;
            mSampleOffsetsHor = sampleOffsetsHor;
            mSampleOffsetsVer = sampleOffsetsVer;
        }

        protected override void SetEffectParameters(Camera camera)
        {
            mEffect.Parameters["sampleWeights"].SetValue(mSampleWeights);
            mEffect.Parameters["sampleOffsetsHor"].SetValue(mSampleOffsetsHor);
            mEffect.Parameters["sampleOffsetsVer"].SetValue(mSampleOffsetsVer);
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

            #region Horizontal Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueHorizontal];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref screenTexture, ref baseRectangle);

            #endregion

            #region Vertical Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueVertical];

            // Draw the vertical pass
            DrawToTexture(camera, ref mTexture, clearColor, ref mTexture, ref baseRectangle);

            #endregion
        }

        internal override void DrawToCameraRenderTarget(Camera camera,
                                        ref Texture2D texture,
                                        Color clearColor,
                                        ref Rectangle sourceRectangle)
        {

            // Set the effect parameters
            SetEffectParameters(camera);

            #region Horizontal Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueHorizontal];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref texture, ref sourceRectangle);

            #endregion

            #region Vertical Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueVertical];

#if XNA4
            throw new NotImplementedException();
            // Unreachable code, but uncomment once the NotImplementedException is removed:
            //DrawToCurrentTarget(camera, ref mTexture, clearColor, ref sourceRectangle);

#else
            // Draw the vertical pass
            DrawToCurrentTarget(camera, ref mTexture, clearColor, ref sourceRectangle);
            camera.mRenderTargetTexture.SetOnDevice();

#endif

            #endregion
        }
        #endregion
    }
}
