using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.PostProcessing
{
    #region XML Docs
    /// <summary>
    /// Post processing class which can be used to create a bloom (glow) effect.
    /// </summary>
    #endregion
    public class Bloom : PostProcessingEffectBase
    {
        #region Fields

        #region Blurring Parameters

        private int mBlurSampleCount = 9;

        private float mBlurStandardDeviation = 4.0f;

        private float mSampleScale = 1.0f;

        private string mTechniqueHorizontal = "BlurHorizontalHi";
        private string mTechniqueVertical = "BlurVerticalHi";
        private EffectQuality mBlurQuality = EffectQuality.High;

        private float[] mSampleWeights;
        private Vector2[] mSampleOffsetsHor;
        private Vector2[] mSampleOffsetsVer;

        #endregion

        #region Bloom Parameters

        private EffectParameter mParamBloomThreshold;
        private EffectParameter mParamBaseIntensity;
        private EffectParameter mParamBloomIntensity;
        private EffectParameter mParamBaseSaturation;
        private EffectParameter mParamBloomSaturation;

        // When the final textures (the base and the brightening) are combined, one will
        // be the original and one will be taken directly from a render target.
        // If the user is using a camera that is showing a split-screen view, then the two
        // will combine improperly.  To fix that, this parameter is used to modify the texture
        // that the shader samples at.
        private EffectParameter mHorizontalSampleMultiplier;
        private EffectParameter mVerticalSampleMultiplier;

        private float mBloomThreshold = 0.25f;
        private float mBaseIntensity = 1.0f;
        private float mBloomIntensity = 1.25f;
        private float mBaseSaturation = 1.0f;
        private float mBloomSaturation = 1.0f;

        #endregion

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Controls the strength of the Gaussian blur used in the blooming algorithm
        /// (the standard deviation of the normal curve used for sampling nearby coordinates).  Default value is 4.
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
        /// Controls the strength of the linear blur used in the blooming algorithm
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
        /// Gets or sets the quality of the blur used in the blooming algorithm
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

        #region XML Docs
        /// <summary>
        /// Gets or sets the bloom threshold - only pixels brighter than the
        /// bloom threshold will be bloomed
        /// </summary>
        #endregion
        public float BloomThreshold
        {
            get { return mBloomThreshold; }
            set { mBloomThreshold = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the intensity of the base image
        /// </summary>
        #endregion
        public float BaseIntensity
        {
            get { return mBaseIntensity; }
            set { mBaseIntensity = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the intensity of the bloomed image. Default is 1.
        /// </summary>
        #endregion
        public float BloomIntensity
        {
            get { return mBloomIntensity; }
            set { mBloomIntensity = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the saturation of the base image
        /// </summary>
        #endregion
        public float BaseSaturation
        {
            get { return mBaseSaturation; }
            set { mBaseSaturation = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the saturation of the bloomed image
        /// </summary>
        #endregion
        public float BloomSaturation
        {
            get { return mBloomSaturation; }
            set { mBloomSaturation = value; }
        }

        #endregion

        #region Constuctor

        #region XML Docs
        /// <summary>
        /// Internal Constructor (so this class can't be instantiated externally)
        /// </summary>
        #endregion
        internal Bloom()
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
            return "Bloom";
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
            //mEffect = FlatRedBallServices.Load<Effect>(@"Assets\Shaders\PostProcessing\Bloom");
            // Otherwise, keep the following line uncommented so the .xnb files in the
            // resources are used.
            mEffect = FlatRedBallServices.mResourceContentManager.Load<Effect>("Bloom");

            // Get effect parameters
            mParamBloomThreshold = mEffect.Parameters["threshold"];
            mParamBaseIntensity = mEffect.Parameters["baseIntensity"];
            mParamBloomIntensity = mEffect.Parameters["bloomIntensity"];
            mParamBaseSaturation = mEffect.Parameters["baseSaturation"];
            mParamBloomSaturation = mEffect.Parameters["bloomSaturation"];

            mHorizontalSampleMultiplier = mEffect.Parameters["horizontalSampleMultiplier"];
            mVerticalSampleMultiplier = mEffect.Parameters["verticalSampleMultiplier"];

            // Set the sampling parameters (for bloom)
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
            mParamBloomThreshold.SetValue(mBloomThreshold);
            mParamBaseIntensity.SetValue(mBaseIntensity);
            mParamBloomIntensity.SetValue(mBloomIntensity);
            mParamBaseSaturation.SetValue(mBaseSaturation);
            mParamBloomSaturation.SetValue(mBloomSaturation);

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
            mHorizontalSampleMultiplier.SetValue(1);
            mVerticalSampleMultiplier.SetValue(1);

            #region Extract

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques["BloomExtract"];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref screenTexture, ref baseRectangle);

            #endregion

            #region Horizontal Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueHorizontal];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref mTexture, ref baseRectangle);

            #endregion

            #region Vertical Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueVertical];

            // Draw the vertical pass
            DrawToTexture(camera, ref mTexture, clearColor, ref mTexture, ref baseRectangle);

            #endregion

            #region Combine

            // The two textures may be of different sizes if using a Camera that doesn't
            // fill up the entire screen.
            mHorizontalSampleMultiplier.SetValue(screenTexture.Width / (float)mTexture.Width);
            mVerticalSampleMultiplier.SetValue(screenTexture.Height / (float)mTexture.Height);

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques["BloomCombine"];

            // Set the bloom texture
            FlatRedBallServices.GraphicsDevice.Textures[1] = mTexture;

            // Draw the bloom combine
            DrawToTexture(camera, ref mTexture, clearColor, ref screenTexture, ref baseRectangle);

            #endregion
        }

        internal override void DrawToCameraRenderTarget(Camera camera,
                                        ref Texture2D texture,
                                        Color clearColor,
                                        ref Rectangle sourceRectangle)
        {
            // Set the effect parameters
            SetEffectParameters(camera);
            mHorizontalSampleMultiplier.SetValue(1);
            mVerticalSampleMultiplier.SetValue(1);

            #region Extract

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques["BloomExtract"];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref texture, ref sourceRectangle);

            #endregion

            #region Horizontal Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueHorizontal];

            // Draw the horizontal pass
            DrawToTexture(camera, ref mTexture, clearColor, ref mTexture, ref sourceRectangle);

            #endregion

            #region Vertical Blur

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques[mTechniqueVertical];

            // Draw the vertical pass
            DrawToTexture(camera, ref mTexture, clearColor, ref mTexture, ref sourceRectangle);


            #endregion

            #region Combine

            // The two textures may be of different sizes if using a Camera that doesn't
            // fill up the entire screen.
            mHorizontalSampleMultiplier.SetValue(texture.Width / (float)mTexture.Width);
            mVerticalSampleMultiplier.SetValue(texture.Height / (float)mTexture.Height);

            // Set the effect technique
            mEffect.CurrentTechnique = mEffect.Techniques["BloomCombine"];

            // Set the bloom texture
            FlatRedBallServices.GraphicsDevice.Textures[1] = mTexture;

#if XNA4
            throw new NotImplementedException();
            // Unreachable code that should be put back in when the exception above is removed
//            DrawToCurrentTarget(camera, ref texture, clearColor, ref sourceRectangle);

#else
            // Draw the bloom combine
            camera.mRenderTargetTexture.SetOnDevice();
            DrawToCurrentTarget(camera, ref texture, clearColor, ref sourceRectangle);

#endif
            #endregion
        }

        #endregion
    }
}
