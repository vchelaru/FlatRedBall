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

public enum CrtModeOption
{
    Full,
    SoftScans,
    NoScans,
    NoScansNoCurvature,
    Disabled
}

internal class ReplaceClassName : IPostProcess
{
    #region Fields/Properties

    public bool IsEnabled { get; set; } = true;

    RenderTarget2D mIntermediatePass;

    Effect mEffect;

    EffectTechnique mEffectTechniqueBase;
    EffectTechnique mEffectTechniqueBaseAndSmoothing;
    EffectTechnique mEffectTechniqueCrtBaseAndSmoothingPass1;
    EffectTechnique mEffectTechniqueCrtScan;
    EffectTechnique mEffectTechniqueCrtScanCa;
    EffectTechnique mEffectTechniqueCrtSmoothingPass2;

    EffectTechnique mBaseTechniqueToUse;
    EffectTechnique mScanTechniqueToUse;

    EffectParameter mOriginalSizeParameter;
    EffectParameter mOutputSizeParameter;
    EffectParameter mPixelWidthParameter;
    EffectParameter mPixelHeightParameter;

    EffectParameter mExposureParameter;
    EffectParameter mVibranceParameter;

    EffectParameter mSmoothingWeightBParameter;
    EffectParameter mSmoothingWeightSParameter;

    EffectParameter mCrtSmoothingWeightP1BParameter;
    EffectParameter mCrtSmoothingWeightP1HParameter;
    EffectParameter mCrtSmoothingWeightP1VParameter;
    EffectParameter mCrtSmoothingWeightP2BParameter;
    EffectParameter mCrtSmoothingWeightP2HParameter;
    EffectParameter mCrtSmoothingWeightP2VParameter;

    EffectParameter mScanMaskStrenghtParameter;
    EffectParameter mScanScaleParameter;
    EffectParameter mScanKernelShapeParameter;
    EffectParameter mScanBrightnessBoostParameter;

    EffectParameter mWarpXParameter;
    EffectParameter mWarpYParameter;

    EffectParameter mCaRedOffsetParameter;
    EffectParameter mCaBlueOffsetParameter;

    SpriteBatch mSpriteBatch;

    bool mNeedsApplySettings;

    #endregion

    #region Exposed settings

    /// <summary>
    /// Sets the <see cref="CrtMode"/> to display. If disabled this
    /// effect will still apply a mild smoothing filter if enabled.
    /// </summary>
    public CrtModeOption CrtMode
    {
        get { return mCrtMode; }
        set
        {
            if (mCrtMode == value) return;

            mCrtMode = value;
            mNeedsApplySettings = true;
        }
    }
    CrtModeOption mCrtMode = CrtModeOption.Full;

    /// <summary>
    /// Whether to apply smoothing filter when <see cref="CrtMode"/> is <see cref="CrtModeOption.Disabled"/>.
    /// </summary>
    public bool IsSmoothingFilterEnabled
    {
        get { return mIsSmoothingFilterEnabled; }
        set
        {
            if (mIsSmoothingFilterEnabled == value) return;

            mIsSmoothingFilterEnabled = value;
            mNeedsApplySettings = true;
        }
    }
    bool mIsSmoothingFilterEnabled = true;

    /// <summary>
    /// Whether to apply chromatic aberration effect.
    /// </summary>
    public bool IsChromaticAberrationEnabled
    {
        get { return mIsChromaticAberrationEnabled; }
        set
        {
            if (mIsChromaticAberrationEnabled == value) return;

            mIsChromaticAberrationEnabled = value;
            mNeedsApplySettings = true;
        }
    }
    bool mIsChromaticAberrationEnabled = true;

    /// <summary>
    /// Level of exposure for the output image.
    /// </summary>
    public float Exposure
    {
        get { return mExposure; }
        set
        {
            if (mExposure == value) return;

            mExposure = value;
            mNeedsApplySettings = true;
        }
    }
    float mExposure = 1.00f;

    /// <summary>
    /// Level of vibrance for the output image.
    /// </summary>
    public float Vibrance
    {
        get { return mVibrance; }
        set
        {
            if (mVibrance == value) return;

            mVibrance = value;
            mNeedsApplySettings = true;
        }
    }
    float mVibrance = 0.18f;

    /// <summary>
    /// Amount of weight for the center pixel when
    /// blurring with the smoothing filter.
    /// </summary>
    public float SmoothingWeightB
    {
        get { return mSmoothingWeightB; }
        set
        {
            if (mSmoothingWeightB == value) return;

            mSmoothingWeightB = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightB = 0.68f;

    /// <summary>
    /// Amount of weight for the adjacent pixels when
    /// blurring with the smoothing filter.
    /// </summary>
    public float SmoothingWeightS
    {
        get { return mSmoothingWeightS; }
        set
        {
            if (mSmoothingWeightS == value) return;

            mSmoothingWeightS = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightS = 0.32f;

    /// <summary>
    /// Amount of weight for the center pixel when
    /// blurring with the CRT mode at pass 1.
    /// </summary>
    public float SmoothingWeightP1B
    {
        get { return mSmoothingWeightP1B; }
        set
        {
            if (mSmoothingWeightP1B == value) return;

            mSmoothingWeightP1B = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP1B = 0.7f;

    /// <summary>
    /// Amount of weight for the adjacent horizontal
    /// pixels when blurring with the CRT mode at pass 1.
    /// </summary>
    public float SmoothingWeightP1H
    {
        get { return mSmoothingWeightP1H; }
        set
        {
            if (mSmoothingWeightP1H == value) return;

            mSmoothingWeightP1H = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP1H = 0.15f;

    /// <summary>
    /// Amount of weight for the adjacent vertical
    /// pixels when blurring with the CRT mode at pass 1.
    /// </summary>
    public float SmoothingWeightP1V
    {
        get { return mSmoothingWeightP1V; }
        set
        {
            if (mSmoothingWeightP1V == value) return;

            mSmoothingWeightP1V = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP1V = 0.15f;

    /// <summary>
    /// Amount of weight for the center pixel
    /// when blurring with the CRT mode at pass 2.
    /// </summary>
    public float SmoothingWeightP2B
    {
        get { return mSmoothingWeightP2B; }
        set
        {
            if (mSmoothingWeightP2B == value) return;

            mSmoothingWeightP2B = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP2B = 0.3f;

    /// <summary>
    /// Amount of weight for the adjacent horizontal
    /// pixels when blurring with the CRT mode at pass 2.
    /// </summary>
    public float SmoothingWeightP2H
    {
        get { return mSmoothingWeightP2H; }
        set
        {
            if (mSmoothingWeightP2H == value) return;

            mSmoothingWeightP2H = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP2H = 0.5f;

    /// <summary>
    /// Amount of weight for the adjacent vertical
    /// pixels when blurring with the CRT mode at pass 2.
    /// </summary>
    public float SmoothingWeightP2V
    {
        get { return mSmoothingWeightP2V; }
        set
        {
            if (mSmoothingWeightP2V == value) return;

            mSmoothingWeightP2V = value;
            mNeedsApplySettings = true;
        }
    }
    float mSmoothingWeightP2V = 0.2f;

    /// <summary>
    /// Strenght for the scan mask.
    /// </summary>
    public float ScanMaskStrenght
    {
        get { return mScanMaskStrenght; }
        set
        {
            if (mScanMaskStrenght == value) return;

            mScanMaskStrenght = value;
            mNeedsApplySettings = true;
        }
    }
    float mScanMaskStrenght = 0.525f;

    /// <summary>
    /// Scale of the scan gaussian effect.
    /// </summary>
    public float ScanScale
    {
        get { return mScanScale; }
        set
        {
            if (mScanScale == value) return;

            mScanScale = value;
            mNeedsApplySettings = true;
        }
    }
    float mScanScale = -8.0f;

    /// <summary>
    /// Shape of the blur filter kernel.
    /// </summary>
    public float ScanKernelShape
    {
        get { return mScanKernelShape; }
        set
        {
            if (mScanKernelShape == value) return;

            mScanKernelShape = value;
            mNeedsApplySettings = true;
        }
    }
    float mScanKernelShape = 2.0f;

    /// <summary>
    /// Brightness boost to compensate for scan darkness.
    /// </summary>
    public float ScanBrightnessBoost
    {
        get { return mScanBrightnessBoost; }
        set
        {
            if (mScanBrightnessBoost == value) return;

            mScanBrightnessBoost = value;
            mNeedsApplySettings = true;
        }
    }
    float mScanBrightnessBoost = 1.11f;

    /// <summary>
    /// Amount of horizontal curvature.
    /// </summary>
    public float WarpX
    {
        get { return mWarpX; }
        set
        {
            if (mWarpX == value) return;

            mWarpX = value;
            mNeedsApplySettings = true;
        }
    }
    float mWarpX = 0.01f;

    /// <summary>
    /// Amount of vertical curvature.
    /// </summary>
    public float WarpY
    {
        get { return mWarpY; }
        set
        {
            if (mWarpY == value) return;

            mWarpY = value;
            mNeedsApplySettings = true;
        }
    }
    float mWarpY = 0.02f;

    /// <summary>
    /// Amount of red channel distortion for chromatic aberration.
    /// </summary>
    public float ChromaticAberrationRedOffset
    {
        get { return mCaRedOffset; }
        set
        {
            if (mCaRedOffset == value) return;

            mCaRedOffset = value;
            mNeedsApplySettings = true;
        }
    }
    float mCaRedOffset = 0.0006f;

    /// <summary>
    /// Amount of blue channel distortion for chromatic aberration.
    /// </summary>
    public float ChromaticAberrationBlueOffset
    {
        get { return mCaBlueOffset; }
        set
        {
            if (mCaBlueOffset == value) return;

            mCaBlueOffset = value;
            mNeedsApplySettings = true;
        }
    }
    float mCaBlueOffset = 0.0006f;

    #endregion

    public ReplaceClassName(Effect effect)
    {
        PostProcessingHelper.BaseWidth = 320;
        PostProcessingHelper.BaseHeight = 180;

        mSpriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

        if (mEffect == null || mEffect.IsDisposed)
        {
            mEffect = effect;

            mEffectTechniqueBase = mEffect.Techniques["Base"];
            mEffectTechniqueBaseAndSmoothing = mEffect.Techniques["BaseAndSmoothing"];
            mEffectTechniqueCrtBaseAndSmoothingPass1 = mEffect.Techniques["CrtBaseAndSmoothingPass1"];
            mEffectTechniqueCrtScan = mEffect.Techniques["CrtScan"];
            mEffectTechniqueCrtScanCa = mEffect.Techniques["CrtScanCa"];
            mEffectTechniqueCrtSmoothingPass2 = mEffect.Techniques["CrtSmoothingPass2"];

            mOriginalSizeParameter = mEffect.Parameters["OriginalSize"];
            mOutputSizeParameter = mEffect.Parameters["OutputSize"];
            mPixelWidthParameter = mEffect.Parameters["PixelWidth"];
            mPixelHeightParameter = mEffect.Parameters["PixelHeight"];

            mExposureParameter = mEffect.Parameters["Exposure"];
            mVibranceParameter = mEffect.Parameters["Vibrance"];

            mSmoothingWeightBParameter = mEffect.Parameters["SmoothingWeightB"];
            mSmoothingWeightSParameter = mEffect.Parameters["SmoothingWeightS"];

            mCrtSmoothingWeightP1BParameter = mEffect.Parameters["CrtSmoothingWeightP1B"];
            mCrtSmoothingWeightP1HParameter = mEffect.Parameters["CrtSmoothingWeightP1H"];
            mCrtSmoothingWeightP1VParameter = mEffect.Parameters["CrtSmoothingWeightP1V"];
            mCrtSmoothingWeightP2BParameter = mEffect.Parameters["CrtSmoothingWeightP2B"];
            mCrtSmoothingWeightP2HParameter = mEffect.Parameters["CrtSmoothingWeightP2H"];
            mCrtSmoothingWeightP2VParameter = mEffect.Parameters["CrtSmoothingWeightP2V"];

            mScanMaskStrenghtParameter = mEffect.Parameters["ScanMaskStrenght"];
            mScanScaleParameter = mEffect.Parameters["ScanScale"];
            mScanKernelShapeParameter = mEffect.Parameters["ScanKernelShape"];
            mScanBrightnessBoostParameter = mEffect.Parameters["ScanBrightnessBoost"];

            mWarpXParameter = mEffect.Parameters["WarpX"];
            mWarpYParameter = mEffect.Parameters["WarpY"];

            mCaRedOffsetParameter = mEffect.Parameters["CaRedOffset"];
            mCaBlueOffsetParameter = mEffect.Parameters["CaBlueOffset"];
        }

        CreateRenderTargets();
        ApplySettings();

        FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>
        {
            CreateRenderTargets();
            ApplySettings();
        };
    }

    private void CreateRenderTargets()
    {
        PostProcessingHelper.CreateRenderTarget(
            ref mIntermediatePass,
            FlatRedBallServices.Game.Window.ClientBounds.Width,
            FlatRedBallServices.Game.Window.ClientBounds.Height,
            SurfaceFormat.HalfVector4);
    }

    void ApplySettings()
    {
        mOriginalSizeParameter.SetValue(new Vector2(PostProcessingHelper.BaseWidth, PostProcessingHelper.BaseHeight));
        mOutputSizeParameter.SetValue(new Vector2(FlatRedBallServices.Game.Window.ClientBounds.Width, FlatRedBallServices.Game.Window.ClientBounds.Height));
        mPixelWidthParameter.SetValue(1f / FlatRedBallServices.Game.Window.ClientBounds.Width);
        mPixelHeightParameter.SetValue(1f / FlatRedBallServices.Game.Window.ClientBounds.Height);

        mExposureParameter.SetValue(mExposure);
        mVibranceParameter.SetValue(mVibrance);

        mSmoothingWeightBParameter.SetValue(mSmoothingWeightB);
        mSmoothingWeightSParameter.SetValue(mSmoothingWeightS);

        mCrtSmoothingWeightP1BParameter.SetValue(mSmoothingWeightP1B);
        mCrtSmoothingWeightP1HParameter.SetValue(mSmoothingWeightP1H);
        mCrtSmoothingWeightP1VParameter.SetValue(mSmoothingWeightP1V);
        mCrtSmoothingWeightP2BParameter.SetValue(mSmoothingWeightP2B);
        mCrtSmoothingWeightP2HParameter.SetValue(mSmoothingWeightP2H);
        mCrtSmoothingWeightP2VParameter.SetValue(mSmoothingWeightP2V);

        float scanMaskStrenght = mScanMaskStrenght;
        float scanBrightnessBoost = mScanBrightnessBoost;

        if (CrtMode == CrtModeOption.SoftScans)
        {
            scanMaskStrenght *= .58f;
            scanBrightnessBoost = 1f + (scanBrightnessBoost - 1f) * .4f;
        }
        else if (CrtMode == CrtModeOption.NoScans || CrtMode == CrtModeOption.NoScansNoCurvature)
        {
            scanMaskStrenght = 0f;
            scanBrightnessBoost = 1f;
        }

        mScanMaskStrenghtParameter.SetValue(scanMaskStrenght);
        mScanScaleParameter.SetValue(mScanScale);
        mScanKernelShapeParameter.SetValue(mScanKernelShape);
        mScanBrightnessBoostParameter.SetValue(scanBrightnessBoost);

        if (CrtMode == CrtModeOption.NoScansNoCurvature)
        {
            mWarpXParameter.SetValue(0f);
            mWarpYParameter.SetValue(0f);
        }
        else
        {
            mWarpXParameter.SetValue(mWarpX);
            mWarpYParameter.SetValue(mWarpY);
        }

        mCaRedOffsetParameter.SetValue(mCaRedOffset);
        mCaBlueOffsetParameter.SetValue(mCaBlueOffset);

        mBaseTechniqueToUse = IsSmoothingFilterEnabled ? mEffectTechniqueBaseAndSmoothing : mEffectTechniqueBase;
        mScanTechniqueToUse = IsChromaticAberrationEnabled ? mEffectTechniqueCrtScanCa : mEffectTechniqueCrtScan;

        mNeedsApplySettings = false;
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
        if (mNeedsApplySettings)
            ApplySettings();

        var device = FlatRedBallServices.GraphicsDevice;
        var output = device.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
        var spriteBatch = mSpriteBatch;

        if (CrtMode == CrtModeOption.Disabled)
        {
            // Base ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.Clear(Color.Transparent);

            mEffect.CurrentTechnique = mBaseTechniqueToUse;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, mEffect);
            spriteBatch.Draw(source, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////
        }
        else
        {
            // Base and smoothing pass 1 ///////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.Clear(Color.Transparent);

            mEffect.CurrentTechnique = mEffectTechniqueCrtBaseAndSmoothingPass1;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, mEffect);
            spriteBatch.Draw(source, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////

            // Scan ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.SetRenderTarget(mIntermediatePass);
            device.Clear(Color.Transparent);

            mEffect.CurrentTechnique = mScanTechniqueToUse;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, mEffect);
            spriteBatch.Draw(output, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////

            // Smoothing pass 2 ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.SetRenderTarget(output);
            device.Clear(Color.Transparent);

            mEffect.CurrentTechnique = mEffectTechniqueCrtSmoothingPass2;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, mEffect);
            spriteBatch.Draw(mIntermediatePass, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }

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
