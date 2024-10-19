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

    public float Exposure { get; set; } = 1.00f;
    public float Vibrance { get; set; } = 0.18f;
    public float ScanBrightnessBoost { get; set; } = 1.11f;
    public CrtModeOption CrtMode { get; set; } = CrtModeOption.Full;
    public bool IsSmoothingFilterEnabled { get; set; } = true;
    public bool IsChromaticAberrationEnabled { get; set; } = true;

    SpriteBatch mSpriteBatch;

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

        FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>
        {
            CreateRenderTargets();
        };

        ApplySettings();
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

        mExposureParameter.SetValue(Exposure);
        mVibranceParameter.SetValue(Vibrance);

        mSmoothingWeightBParameter.SetValue(0.68f);
        mSmoothingWeightSParameter.SetValue(0.32f);

        mCrtSmoothingWeightP1BParameter.SetValue(0.7f);
        mCrtSmoothingWeightP1HParameter.SetValue(0.15f);
        mCrtSmoothingWeightP1VParameter.SetValue(0.15f);
        mCrtSmoothingWeightP2BParameter.SetValue(0.3f);
        mCrtSmoothingWeightP2HParameter.SetValue(0.5f);
        mCrtSmoothingWeightP2VParameter.SetValue(0.2f);

        float scanMaskStrenght = 0.525f;
        float scanBrightnessBoost = ScanBrightnessBoost;

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
        mScanScaleParameter.SetValue(-8.0f);
        mScanKernelShapeParameter.SetValue(2.0f);
        mScanBrightnessBoostParameter.SetValue(scanBrightnessBoost);

        if (CrtMode == CrtModeOption.NoScansNoCurvature)
        {
            mWarpXParameter.SetValue(0f);
            mWarpYParameter.SetValue(0f);
        }
        else
        {
            mWarpXParameter.SetValue(0.01f);
            mWarpYParameter.SetValue(0.02f);
        }

        mCaRedOffsetParameter.SetValue(0.0006f);
        mCaBlueOffsetParameter.SetValue(0.0006f);

        mBaseTechniqueToUse = IsSmoothingFilterEnabled ? mEffectTechniqueBaseAndSmoothing : mEffectTechniqueBase;
        mScanTechniqueToUse = IsChromaticAberrationEnabled ? mEffectTechniqueCrtScanCa : mEffectTechniqueCrtScan;
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
        // This could be smarter to only apply when it changes, but for now this will do:
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
