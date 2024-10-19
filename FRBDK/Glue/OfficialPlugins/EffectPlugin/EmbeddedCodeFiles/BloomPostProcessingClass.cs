using FlatRedBall;
using FlatRedBall.Graphics.PostProcessing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaceNamespace;

#region Enums

public enum BloomPresets
{
    Wide,
    Focussed,
    Small,
    SuperWide,
    Cheap,
    One
};

#endregion

internal class ReplaceClassName : IPostProcess
{
    #region Fields/Properties

    public bool IsEnabled { get; set; } = true;
    SpriteBatch mSpriteBatch;

    RenderTarget2D mBloomRenderTarget2DMip0;
    RenderTarget2D mBloomRenderTarget2DMip1;
    RenderTarget2D mBloomRenderTarget2DMip2;
    RenderTarget2D mBloomRenderTarget2DMip3;
    RenderTarget2D mBloomRenderTarget2DMip4;
    RenderTarget2D mBloomRenderTarget2DMip5;

    Effect mBloomEffect;

    EffectPass mBloomPassExtract;
    EffectPass mBloomPassExtractLuminance;
    EffectPass mBloomPassDownsample;
    EffectPass mBloomPassUpsample;
    EffectPass mBloomPassUpsampleLuminance;

    EffectParameter mBloomParameterScreenTexture;
    EffectParameter mBloomHalfPixelParameter;
    EffectParameter mBloomDownsampleOffsetParameter;
    EffectParameter mBloomUpsampleOffsetParameter;
    EffectParameter mBloomStrengthParameter;
    EffectParameter mBloomThresholdParameter;

    Effect mBloomCombineEffect;

    EffectTechnique mBloomTechniqueCombine;
    EffectTechnique mBloomTechniqueSaturate;

    EffectParameter mBloomCombineBLTextureParameter;
    EffectParameter mBloomCombineBLBaseTextureParameter;
    EffectParameter mBloomCombineBLIntensityParameter;
    EffectParameter mBloomCombineBLSaturationParameter;

    float mBloomRadius1 = 1.0f;
    float mBloomRadius2 = 1.0f;
    float mBloomRadius3 = 1.0f;
    float mBloomRadius4 = 1.0f;
    float mBloomRadius5 = 1.0f;

    float mBloomStrength1 = 1.0f;
    float mBloomStrength2 = 1.0f;
    float mBloomStrength3 = 1.0f;
    float mBloomStrength4 = 1.0f;
    float mBloomStrength5 = 1.0f;

    float mRadiusMultiplier = 1.0f;

    bool mNeedsCreateRenderTargets;
    bool mNeedsApplySettings;

    #endregion

    #region Exposed settings

    public BloomPresets BloomPreset
    {
        get { return mBloomPreset; }
        set
        {
            if (mBloomPreset == value) return;

            mBloomPreset = value;
            mNeedsApplySettings = true;
        }
    }
    BloomPresets mBloomPreset;

    public float Threshold
    {
        get { return mThreshold; }
        set
        {
            if (mThreshold == value) return;

            mThreshold = value;
            mNeedsApplySettings = true;
        }
    }
    float mThreshold = .6f;

    public float StrengthMultiplier
    {
        get { return mStrengthMultiplier; }
        set
        {
            if (mStrengthMultiplier == value) return;

            mStrengthMultiplier = value;
            mNeedsApplySettings = true;
        }
    }
    float mStrengthMultiplier = 1f;

    public float RadiusMultiplier
    {
        get { return mRadiusMultiplier; }
        set
        {
            if (mRadiusMultiplier == value) return;

            mRadiusMultiplier = value;
            mNeedsApplySettings = true;
        }
    }
    float mRadiusMultiplier = 1f;

    public bool UseLuminance
    {
        get { return mUseLuminance; }
        set
        {
            if (mUseLuminance == value) return;

            mUseLuminance = value;
            mNeedsApplySettings = true;
        }
    }
    bool mUseLuminance = false;

    public float Intensity
    {
        get { return mIntensity; }
        set
        {
            if (mIntensity == value) return;

            mIntensity = value;
            mNeedsApplySettings = true;
        }
    }
    float mIntensity = 0.25f;

    public float Saturation
    {
        get { return mSaturation; }
        set
        {
            if (mSaturation == value) return;

            mSaturation = value;
            mNeedsApplySettings = true;
        }
    }
    float mSaturation = 1.3f;

    public float Quality
    {
        get { return mQuality; }
        set
        {
            if (mQuality == value) return;

            mQuality = value;
            mNeedsCreateRenderTargets = true;
        }
    }
    float mQuality = .5f;

    public bool PreserveContents
    {
        get { return mPreserveContents; }
        set
        {
            if (mPreserveContents == value) return;

            mPreserveContents = value;
            mNeedsCreateRenderTargets = true;
        }
    }
    bool mPreserveContents = true;

    #endregion

    #region Preset dependent fields/properties (not public)

    float BloomStrength
    {
        get { return mBloomStrength; }
        set
        {
            if (Math.Abs(mBloomStrength - value) > 0.001f)
            {
                mBloomStrength = value;
                mBloomStrengthParameter.SetValue(mBloomStrength * mStrengthMultiplier);
            }

        }
    }
    float mBloomStrength;

    float BloomRadius
    {
        get { return mBloomRadius; }
        set
        {
            if (Math.Abs(mBloomRadius - value) > 0.001f)
                mBloomRadius = value;
        }
    }
    float mBloomRadius;

    float BloomStreakLength
    {
        get { return mBloomStreakLength; }
        set
        {
            if (Math.Abs(mBloomStreakLength - value) > 0.001f)
                mBloomStreakLength = value;
        }
    }
    float mBloomStreakLength;

    int mBloomDownsamplePasses = 5;

    #endregion

    #region Utility fields/properties (not public)

    Texture2D BloomScreenTexture { set { mBloomParameterScreenTexture.SetValue(value); } }

    Vector2 HalfPixel
    {
        get { return mHalfPixel; }
        set
        {
            if (value != mHalfPixel)
            {
                mHalfPixel = value;
                mBloomHalfPixelParameter.SetValue(mHalfPixel);
            }
        }
    }
    Vector2 mHalfPixel;

    float BloomThreshold
    {
        get { return mBloomThreshold; }
        set
        {
            if (Math.Abs(mBloomThreshold - value) > 0.001f)
            {
                mBloomThreshold = value;
                mBloomThresholdParameter.SetValue(mBloomThreshold);
            }
        }
    }
    float mBloomThreshold;

    Vector2 BloomInverseResolution
    {
        get { return mBloomInverseResolution; }
        set
        {
            if (value != mBloomInverseResolution)
                mBloomInverseResolution = value;
        }
    }
    Vector2 mBloomInverseResolution;

    #endregion

    public ReplaceClassName(Effect effect)
    {
        mSpriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

        InitializeEffects(effect, effect);

        PostProcessingHelper.BaseWidth = 320;
        PostProcessingHelper.BaseHeight = 180;

        ApplySettings();
        // todo - may need to re-create render targets if the user changes the Quality property 
        CreateRenderTargets();
        FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>
        {
            CreateRenderTargets();
        };
    }

    void UpdateDownsampleOffsetParameter()
    {
        var offset = mBloomInverseResolution;
        offset.X *= mBloomStreakLength;
        mBloomDownsampleOffsetParameter.SetValue(offset);
    }

    void UpdateUpsampleOffsetParameter()
    {
        var offset = mBloomInverseResolution;
        offset.X *= mBloomStreakLength;
        offset *= mBloomRadius * mRadiusMultiplier;
        mBloomUpsampleOffsetParameter.SetValue(offset);
    }

    void InitializeEffects(Effect bloomEffect, Effect bloomCombine)
    {
        if (mBloomEffect == null || mBloomEffect.IsDisposed)
        {
            mBloomEffect = bloomEffect;
            mBloomHalfPixelParameter = mBloomEffect.Parameters["HalfPixel"];
            mBloomDownsampleOffsetParameter = mBloomEffect.Parameters["DownsampleOffset"];
            mBloomUpsampleOffsetParameter = mBloomEffect.Parameters["UpsampleOffset"];
            mBloomStrengthParameter = mBloomEffect.Parameters["Strength"];
            mBloomThresholdParameter = mBloomEffect.Parameters["Threshold"];

            mBloomParameterScreenTexture = mBloomEffect.Parameters["LinearSampler+ScreenTexture"];

            mBloomPassExtract = mBloomEffect.Techniques["Extract"].Passes[0];
            mBloomPassExtractLuminance = mBloomEffect.Techniques["ExtractLuminance"].Passes[0];
            mBloomPassDownsample = mBloomEffect.Techniques["Downsample"].Passes[0];
            mBloomPassUpsample = mBloomEffect.Techniques["Upsample"].Passes[0];
            mBloomPassUpsampleLuminance = mBloomEffect.Techniques["UpsampleLuminance"].Passes[0];
        }

        if (mBloomCombineEffect == null || mBloomCombineEffect.IsDisposed)
        {
            mBloomCombineEffect = bloomCombine;

            mBloomTechniqueCombine = mBloomCombineEffect.Techniques["BloomCombine"];
            mBloomTechniqueSaturate = mBloomCombineEffect.Techniques["BloomSaturate"];

            mBloomCombineBLTextureParameter = mBloomCombineEffect.Parameters["BloomTexture"];
            mBloomCombineBLBaseTextureParameter = mBloomCombineEffect.Parameters["BaseTexture"];
            mBloomCombineBLIntensityParameter = mBloomCombineEffect.Parameters["BloomIntensity"];
            mBloomCombineBLSaturationParameter = mBloomCombineEffect.Parameters["BloomSaturation"];
        }
    }

    void ApplySettings()
    {
        SetBloomPreset(mBloomPreset);
        BloomThreshold = mThreshold;
        mBloomCombineBLIntensityParameter.SetValue(mIntensity);
        mBloomCombineBLSaturationParameter.SetValue(mSaturation);

        mNeedsApplySettings = false;
    }

    private void CreateRenderTargets()
    {
        var usage = mPreserveContents ? RenderTargetUsage.PreserveContents : RenderTargetUsage.DiscardContents;
        int width = (int)(FlatRedBallServices.ClientWidth * Quality);
        int height = (int)(FlatRedBallServices.ClientHeight * Quality);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip0, width, height, SurfaceFormat.HalfVector4);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip1, width / 2, height / 2, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip2, width / 4, height / 4, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip3, width / 8, height / 8, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip4, width / 16, height / 16, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref mBloomRenderTarget2DMip5, width / 32, height / 32, SurfaceFormat.HalfVector4, usage);

        mNeedsCreateRenderTargets = false;
    }

    void SetBloomPreset(BloomPresets preset)
    {
        switch (preset)
        {
            case BloomPresets.Wide:
                {
                    mBloomStrength1 = 0.5f;
                    mBloomStrength2 = 1;
                    mBloomStrength3 = 2;
                    mBloomStrength4 = 1;
                    mBloomStrength5 = 2;
                    mBloomRadius5 = 4.0f;
                    mBloomRadius4 = 4.0f;
                    mBloomRadius3 = 2.0f;
                    mBloomRadius2 = 2.0f;
                    mBloomRadius1 = 1.0f;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.SuperWide:
                {
                    mBloomStrength1 = 0.9f;
                    mBloomStrength2 = 1;
                    mBloomStrength3 = 1;
                    mBloomStrength4 = 2;
                    mBloomStrength5 = 6;
                    mBloomRadius5 = 4.0f;
                    mBloomRadius4 = 2.0f;
                    mBloomRadius3 = 2.0f;
                    mBloomRadius2 = 2.0f;
                    mBloomRadius1 = 2.0f;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Focussed:
                {
                    mBloomStrength1 = 0.8f;
                    mBloomStrength2 = 1;
                    mBloomStrength3 = 1;
                    mBloomStrength4 = 1;
                    mBloomStrength5 = 2;
                    mBloomRadius5 = 4.0f;
                    mBloomRadius4 = 2.0f;
                    mBloomRadius3 = 2.0f;
                    mBloomRadius2 = 2.0f;
                    mBloomRadius1 = 2.0f;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Small:
                {
                    mBloomStrength1 = 0.8f;
                    mBloomStrength2 = 1;
                    mBloomStrength3 = 1;
                    mBloomStrength4 = 1;
                    mBloomStrength5 = 1;
                    mBloomRadius5 = 1;
                    mBloomRadius4 = 1;
                    mBloomRadius3 = 1;
                    mBloomRadius2 = 1;
                    mBloomRadius1 = 1;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Cheap:
                {
                    mBloomStrength1 = 0.8f;
                    mBloomStrength2 = 2;
                    mBloomRadius2 = 2;
                    mBloomRadius1 = 2;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 2;
                    break;
                }
            case BloomPresets.One:
                {
                    mBloomStrength1 = 4f;
                    mBloomStrength2 = 1;
                    mBloomStrength3 = 1;
                    mBloomStrength4 = 1;
                    mBloomStrength5 = 2;
                    mBloomRadius5 = 1.0f;
                    mBloomRadius4 = 1.0f;
                    mBloomRadius3 = 1.0f;
                    mBloomRadius2 = 1.0f;
                    mBloomRadius1 = 1.0f;
                    BloomStreakLength = 1;
                    mBloomDownsamplePasses = 5;
                    break;
                }
        }
    }

    public void Apply(Texture2D source, RenderTarget2D target)
    {
        var device = FlatRedBallServices.GraphicsDevice;

        var oldRt = device.GetRenderTargets().FirstOrDefault().RenderTarget as RenderTarget2D;
        device.SetRenderTarget(target);
        Apply(source);
        device.SetRenderTarget(oldRt);
    }

    public void Apply(Texture2D source)
    {
        if (mNeedsCreateRenderTargets)
            CreateRenderTargets();

        if (mNeedsApplySettings)
            ApplySettings();

        var device = FlatRedBallServices.GraphicsDevice;

        device.RasterizerState = RasterizerState.CullNone;
        device.BlendState = BlendState.Opaque;

        var output = device.GetRenderTargets().FirstOrDefault().RenderTarget as RenderTarget2D;

        // EXTRACT
        // We extract the bright values which are above the Threshold and save them to Mip0
        device.SetRenderTarget(mBloomRenderTarget2DMip0);

        BloomScreenTexture = source;

        int clientScale = (int)Math.Ceiling((float)FlatRedBallServices.Game.Window.ClientBounds.Height / PostProcessingHelper.BaseHeight);

        // According to Miguel this is no longer needed
        //HalfPixel = new Vector2(1.0f / PostProcessingHelper.PresentationWidth, 1.0f / PostProcessingHelper.PresentationHeight) / clientScale;
        HalfPixel = Vector2.Zero;
        BloomInverseResolution = new Vector2(1.0f / FlatRedBallServices.Game.Window.ClientBounds.Width, 1.0f / FlatRedBallServices.Game.Window.ClientBounds.Height);

        // Miguel 19/10/2024
        // When Vic migrated the .fx files to FRB he merged Bloom.fx and BloomCombine.fx into one,
        // resulting in a rearrange of the sampler states order. BloomCombine.fx sampler states
        // became r0 and r1 while Bloom.fx became r2. This broke the clamping and, as a consecuence,
        // bloom bleed from the opposite sides of the screen. Fixed by ensuring all three sampler states
        // are set to clamp mode:
        device.SamplerStates[0] = SamplerState.LinearClamp;
        device.SamplerStates[1] = SamplerState.LinearClamp;
        device.SamplerStates[2] = SamplerState.LinearClamp;

        if (mUseLuminance) mBloomPassExtractLuminance.Apply();
        else mBloomPassExtract.Apply();
        QuadRenderer.RenderFitViewport();

        // Now downsample to the next lower mip texture
        if (mBloomDownsamplePasses > 0)
        {
            //DOWNSAMPLE TO MIP1
            device.SetRenderTarget(mBloomRenderTarget2DMip1);

            BloomScreenTexture = mBloomRenderTarget2DMip0;
            UpdateDownsampleOffsetParameter();

            //Pass
            mBloomPassDownsample.Apply();
            QuadRenderer.RenderFitViewport();

            if (mBloomDownsamplePasses > 1)
            {
                //Our input resolution is halved, so our inverse 1/res. must be doubled
                HalfPixel *= 2;
                BloomInverseResolution *= 2;

                //DOWNSAMPLE TO MIP2
                device.SetRenderTarget(mBloomRenderTarget2DMip2);

                BloomScreenTexture = mBloomRenderTarget2DMip1;
                UpdateDownsampleOffsetParameter();

                //Pass
                mBloomPassDownsample.Apply();
                QuadRenderer.RenderFitViewport();

                if (mBloomDownsamplePasses > 2)
                {
                    HalfPixel *= 2;
                    BloomInverseResolution *= 2;

                    //DOWNSAMPLE TO MIP3
                    device.SetRenderTarget(mBloomRenderTarget2DMip3);

                    BloomScreenTexture = mBloomRenderTarget2DMip2;
                    UpdateDownsampleOffsetParameter();

                    //Pass
                    mBloomPassDownsample.Apply();
                    QuadRenderer.RenderFitViewport();

                    if (mBloomDownsamplePasses > 3)
                    {
                        HalfPixel *= 2;
                        BloomInverseResolution *= 2;

                        //DOWNSAMPLE TO MIP4
                        device.SetRenderTarget(mBloomRenderTarget2DMip4);

                        BloomScreenTexture = mBloomRenderTarget2DMip3;
                        UpdateDownsampleOffsetParameter();

                        //Pass
                        mBloomPassDownsample.Apply();
                        QuadRenderer.RenderFitViewport();

                        if (mBloomDownsamplePasses > 4)
                        {
                            HalfPixel *= 2;
                            BloomInverseResolution *= 2;

                            //DOWNSAMPLE TO MIP5
                            device.SetRenderTarget(mBloomRenderTarget2DMip5);

                            BloomScreenTexture = mBloomRenderTarget2DMip4;
                            UpdateDownsampleOffsetParameter();

                            //Pass
                            mBloomPassDownsample.Apply();
                            QuadRenderer.RenderFitViewport();

                            ChangeBlendState();

                            //UPSAMPLE TO MIP4
                            device.SetRenderTarget(mBloomRenderTarget2DMip4);

                            BloomScreenTexture = mBloomRenderTarget2DMip5;
                            BloomStrength = mBloomStrength5;
                            BloomRadius = mBloomRadius5;
                            UpdateUpsampleOffsetParameter();

                            if (mUseLuminance) mBloomPassUpsampleLuminance.Apply();
                            else mBloomPassUpsample.Apply();
                            QuadRenderer.RenderFitViewport();

                            HalfPixel /= 2;
                            BloomInverseResolution /= 2;
                        }

                        ChangeBlendState();

                        //UPSAMPLE TO MIP3
                        device.SetRenderTarget(mBloomRenderTarget2DMip3);

                        BloomScreenTexture = mBloomRenderTarget2DMip4;
                        BloomStrength = mBloomStrength4;
                        BloomRadius = mBloomRadius4;
                        UpdateUpsampleOffsetParameter();

                        if (mUseLuminance) mBloomPassUpsampleLuminance.Apply();
                        else mBloomPassUpsample.Apply();
                        QuadRenderer.RenderFitViewport();

                        HalfPixel /= 2;
                        BloomInverseResolution /= 2;
                    }

                    ChangeBlendState();

                    //UPSAMPLE TO MIP2
                    device.SetRenderTarget(mBloomRenderTarget2DMip2);

                    BloomScreenTexture = mBloomRenderTarget2DMip3;
                    BloomStrength = mBloomStrength3;
                    BloomRadius = mBloomRadius3;
                    UpdateUpsampleOffsetParameter();

                    if (mUseLuminance) mBloomPassUpsampleLuminance.Apply();
                    else mBloomPassUpsample.Apply();
                    QuadRenderer.RenderFitViewport();

                    HalfPixel /= 2;
                    BloomInverseResolution /= 2;
                }

                ChangeBlendState();

                //UPSAMPLE TO MIP1
                device.SetRenderTarget(mBloomRenderTarget2DMip1);

                BloomScreenTexture = mBloomRenderTarget2DMip2;
                BloomStrength = mBloomStrength2;
                BloomRadius = mBloomRadius2;
                UpdateUpsampleOffsetParameter();

                if (mUseLuminance) mBloomPassUpsampleLuminance.Apply();
                else mBloomPassUpsample.Apply();
                QuadRenderer.RenderFitViewport();

                HalfPixel /= 2;
                BloomInverseResolution /= 2;
            }

            ChangeBlendState();

            //UPSAMPLE TO MIP0
            device.SetRenderTarget(mBloomRenderTarget2DMip0);

            BloomScreenTexture = mBloomRenderTarget2DMip1;
            BloomStrength = mBloomStrength1;
            BloomRadius = mBloomRadius1;
            UpdateUpsampleOffsetParameter();

            if (mUseLuminance) mBloomPassUpsampleLuminance.Apply();
            else mBloomPassUpsample.Apply();
            QuadRenderer.RenderFitViewport();
        }

        // Combine base image and bloom image.
        mBloomCombineEffect.CurrentTechnique = mBloomTechniqueCombine;
        mBloomCombineBLBaseTextureParameter.SetValue(source);

        FlatRedBallServices.GraphicsDevice.SetRenderTarget(output);
        DrawFullscreenQuadRT(mBloomRenderTarget2DMip0, output, mBloomCombineEffect);

    }

    void ChangeBlendState()
    {
        FlatRedBallServices.GraphicsDevice.BlendState = BlendState.AlphaBlend;
    }

    /// <summary>
    /// Helper for drawing a texture into a render target, using
    /// a custom shader to apply postprocessing effects.
    /// </summary>
    void DrawFullscreenQuadRT(Texture2D texture, RenderTarget2D renderTarget, Effect effect)
    {
        var device = FlatRedBallServices.GraphicsDevice;
        var spriteBatch = mSpriteBatch;
        //device.SetRenderTarget(renderTarget);
        //device.Clear(Color.Transparent);
        spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, effect);
        spriteBatch.Draw(texture, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height), Color.White);
        spriteBatch.End();
        //device.SetRenderTarget(null);
    }

    #region Quad Renderer

    public static class QuadRenderer
    {
        // This are the vertex numbers 
        // and positions if drawing as 
        // a fullscreen quad:
        //    0--------------1
        //    |-1,1       1,1|
        //    |              |
        //    |              |
        //    |              |
        //    |-1,-1     1,-1|
        //    3--------------2
        //
        // This are the vertex numbers 
        // and texture coordinates for 
        // a textured quad:
        //    0--------------1
        //    |0,0        1,0|
        //    |              |
        //    |              |
        //    |              |
        //    |0,1        1,1|
        //    3--------------2
        ///////////////////////////////

        static GraphicsDevice mDevice;

        static VertexPositionTexture[] mFitViewportVertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1))
        };

        static VertexPositionTexture[] mCustomVertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 1))
        };

        static VertexPositionColorTexture[] mFitViewportColorVertices = new VertexPositionColorTexture[]
        {
            new VertexPositionColorTexture(new Vector3(-1, 1, 0), Color.White, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1, 1, 0), Color.White, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1, -1, 0), Color.White, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(-1, -1, 0), Color.White, new Vector2(0, 1))
        };

        static VertexPositionColorTexture[] mCustomColorVertices = new VertexPositionColorTexture[]
        {
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(0, 1))
        };

        static short[] mIndices = { 0, 1, 2, 2, 3, 0 };
        static ushort[] mBufferedIndices = { 0, 1, 2, 2, 3, 0 };

        static VertexBuffer mVertexBuffer;
        static VertexBuffer mVertexColorBuffer;
        static IndexBuffer mIndexBuffer;

        public static bool UseVertexColor { get; set; }

        static QuadRenderer()
        {
            mDevice = FlatRedBallServices.GraphicsDevice;

            mVertexBuffer = new VertexBuffer(mDevice, typeof(VertexPositionTexture), mFitViewportVertices.Length, BufferUsage.None);

            mVertexColorBuffer = new VertexBuffer(mDevice, typeof(VertexPositionColorTexture), mFitViewportColorVertices.Length, BufferUsage.None);

            mIndexBuffer = new IndexBuffer(mDevice, typeof(ushort), mBufferedIndices.Length, BufferUsage.None);
            mIndexBuffer.SetData(mBufferedIndices);
        }

        public static void SetBuffers(bool useCustomVertices)
        {
            mDevice.Indices = mIndexBuffer;

            if (UseVertexColor)
            {
                if (useCustomVertices)
                    mVertexColorBuffer.SetData(mCustomColorVertices);
                else
                    mVertexColorBuffer.SetData(mFitViewportColorVertices);

                mDevice.SetVertexBuffer(mVertexColorBuffer);

            }
            else
            {
                if (useCustomVertices)
                    mVertexBuffer.SetData(mCustomVertices);
                else
                    mVertexBuffer.SetData(mFitViewportVertices);

                mDevice.SetVertexBuffer(mVertexBuffer);
            }
        }

        public static void UnsetBuffers()
        {
            mDevice.Indices = null;
            mDevice.SetVertexBuffer(null);
        }

        public static void UpdateCustomVerticesPosition(int destinationWidth, int destinationHeight)
        {
            float viewportWidth = mDevice.Viewport.Width;
            float viewportHeight = mDevice.Viewport.Height;
            float horizontalPosition = 1f + ((destinationWidth - viewportWidth) / viewportWidth);
            float verticalPosition = -1f - ((destinationHeight - viewportHeight) / viewportHeight);

            if (UseVertexColor)
            {
                mCustomColorVertices[0].Position.X = -1f;
                mCustomColorVertices[0].Position.Y = 1f;

                mCustomColorVertices[1].Position.X = horizontalPosition;
                mCustomColorVertices[1].Position.Y = 1f;

                mCustomColorVertices[2].Position.X = horizontalPosition;
                mCustomColorVertices[2].Position.Y = verticalPosition;

                mCustomColorVertices[3].Position.X = -1f;
                mCustomColorVertices[3].Position.Y = verticalPosition;
            }
            else
            {
                mCustomVertices[0].Position.X = -1f;
                mCustomVertices[0].Position.Y = 1f;

                mCustomVertices[1].Position.X = horizontalPosition;
                mCustomVertices[1].Position.Y = 1f;

                mCustomVertices[2].Position.X = horizontalPosition;
                mCustomVertices[2].Position.Y = verticalPosition;

                mCustomVertices[3].Position.X = -1f;
                mCustomVertices[3].Position.Y = verticalPosition;
            }
        }

        public static void UpdateCustomVerticesPosition(Vector2 topLeft, Vector2 bottomRight)
        {
            if (UseVertexColor)
            {
                mCustomColorVertices[0].Position.X = topLeft.X;
                mCustomColorVertices[0].Position.Y = topLeft.Y;

                mCustomColorVertices[1].Position.X = bottomRight.X;
                mCustomColorVertices[1].Position.Y = topLeft.Y;

                mCustomColorVertices[2].Position.X = bottomRight.X;
                mCustomColorVertices[2].Position.Y = bottomRight.Y;

                mCustomColorVertices[3].Position.X = topLeft.X;
                mCustomColorVertices[3].Position.Y = bottomRight.Y;
            }
            else
            {
                mCustomVertices[0].Position.X = topLeft.X;
                mCustomVertices[0].Position.Y = topLeft.Y;

                mCustomVertices[1].Position.X = bottomRight.X;
                mCustomVertices[1].Position.Y = topLeft.Y;

                mCustomVertices[2].Position.X = bottomRight.X;
                mCustomVertices[2].Position.Y = bottomRight.Y;

                mCustomVertices[3].Position.X = topLeft.X;
                mCustomVertices[3].Position.Y = bottomRight.Y;
            }
        }

        public static void UpdateCustomVerticesTexCoord(Vector2 topLeft, Vector2 bottomRight)
        {
            if (UseVertexColor)
            {
                mCustomColorVertices[0].TextureCoordinate.X = topLeft.X;
                mCustomColorVertices[0].TextureCoordinate.Y = topLeft.Y;

                mCustomColorVertices[1].TextureCoordinate.X = bottomRight.X;
                mCustomColorVertices[1].TextureCoordinate.Y = topLeft.Y;

                mCustomColorVertices[2].TextureCoordinate.X = bottomRight.X;
                mCustomColorVertices[2].TextureCoordinate.Y = bottomRight.Y;

                mCustomColorVertices[3].TextureCoordinate.X = topLeft.X;
                mCustomColorVertices[3].TextureCoordinate.Y = bottomRight.Y;
            }
            else
            {
                mCustomVertices[0].TextureCoordinate.X = topLeft.X;
                mCustomVertices[0].TextureCoordinate.Y = topLeft.Y;

                mCustomVertices[1].TextureCoordinate.X = bottomRight.X;
                mCustomVertices[1].TextureCoordinate.Y = topLeft.Y;

                mCustomVertices[2].TextureCoordinate.X = bottomRight.X;
                mCustomVertices[2].TextureCoordinate.Y = bottomRight.Y;

                mCustomVertices[3].TextureCoordinate.X = topLeft.X;
                mCustomVertices[3].TextureCoordinate.Y = bottomRight.Y;
            }
        }

        public static void RenderBuffered()
        {
            mDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
        }

        public static void RenderFitViewport()
        {
            if (UseVertexColor)
                mDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mFitViewportColorVertices, 0, 4, mIndices, 0, 2);
            else
                mDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mFitViewportVertices, 0, 4, mIndices, 0, 2);
        }

        public static void RenderCustom()
        {
            if (UseVertexColor)
                mDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mCustomColorVertices, 0, 4, mIndices, 0, 2);
            else
                mDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mCustomVertices, 0, 4, mIndices, 0, 2);
        }
    }

    #endregion

    #region PostProcessingHelper

    internal static class PostProcessingHelper
    {
        internal static int BaseWidth { get; set; }
        internal static int BaseHeight { get; set; }

        static GraphicsDevice GraphicsDevice => FlatRedBallServices.GraphicsDevice;

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height)
        {
            CreateRenderTarget(ref renderTarget, width, height, GraphicsDevice.DisplayMode.Format, RenderTargetUsage.DiscardContents);
        }

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat)
        {
            CreateRenderTarget(ref renderTarget, width, height, surfaceFormat, RenderTargetUsage.DiscardContents);
        }

        internal static void CreateRenderTarget(ref RenderTarget2D renderTarget, int width, int height, SurfaceFormat surfaceFormat, RenderTargetUsage renderTargetUsage)
        {
            if (renderTarget == null
                || renderTarget.Width != width
                || renderTarget.Height != height
                || renderTarget.Format != surfaceFormat
                || renderTarget.RenderTargetUsage != renderTargetUsage)
            {
                if (renderTarget != null)
                    renderTarget.Dispose();

                lock (GraphicsDevice)
                {
                    renderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, surfaceFormat, (DepthFormat)0, 0, renderTargetUsage);
                }
            }
        }
    }

    #endregion
}

