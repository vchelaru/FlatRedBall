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

    public bool IsEnabled { get; set; } = true;

    RenderTarget2D _intermediatePass;

    Effect _effect;

    EffectTechnique _effectTechniqueBase;
    EffectTechnique _effectTechniqueBaseAndSmoothing;
    EffectTechnique _effectTechniqueCrtBaseAndSmoothingPass1;
    EffectTechnique _effectTechniqueCrtScan;
    EffectTechnique _effectTechniqueCrtScanCa;
    EffectTechnique _effectTechniqueCrtSmoothingPass2;

    EffectTechnique _baseTechniqueToUse;
    EffectTechnique _scanTechniqueToUse;

    EffectParameter _originalSizeParameter;
    EffectParameter _outputSizeParameter;
    EffectParameter _pixelWidthParameter;
    EffectParameter _pixelHeightParameter;

    EffectParameter _exposureParameter;
    EffectParameter _vibranceParameter;

    EffectParameter _smoothingWeightBParameter;
    EffectParameter _smoothingWeightSParameter;

    EffectParameter _crtSmoothingWeightP1BParameter;
    EffectParameter _crtSmoothingWeightP1HParameter;
    EffectParameter _crtSmoothingWeightP1VParameter;
    EffectParameter _crtSmoothingWeightP2BParameter;
    EffectParameter _crtSmoothingWeightP2HParameter;
    EffectParameter _crtSmoothingWeightP2VParameter;

    EffectParameter _scanMaskStrenghtParameter;
    EffectParameter _scanScaleParameter;
    EffectParameter _scanKernelShapeParameter;
    EffectParameter _scanBrightnessBoostParameter;

    EffectParameter _warpXParameter;
    EffectParameter _warpYParameter;

    EffectParameter _caRedOffsetParameter;
    EffectParameter _caBlueOffsetParameter;

    public float Exposure { get; set; } = 1.00f;
    public float Vibrance { get; set; } = 0.18f;
    public float ScanBrightnessBoost { get; set; } = 1.11f;
    public CrtModeOption CrtMode { get; set; } = CrtModeOption.Full;
    public bool IsSmoothingFilterEnabled { get; set; } = true;
    public bool IsChromaticAberrationEnabled { get; set; } = true;

    SpriteBatch _spriteBatch;


    public ReplaceClassName(Effect effect)
    {
        PostProcessingHelper.BaseWidth = 320;
        PostProcessingHelper.BaseHeight = 180;
        PostProcessingHelper.PresentationWidth = FlatRedBallServices.Game.Window.ClientBounds.Width;
        PostProcessingHelper.PresentationHeight = FlatRedBallServices.Game.Window.ClientBounds.Height;

        _spriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

        if (_effect == null || _effect.IsDisposed)
        {
            _effect = effect;

            _effectTechniqueBase = _effect.Techniques["Base"];
            _effectTechniqueBaseAndSmoothing = _effect.Techniques["BaseAndSmoothing"];
            _effectTechniqueCrtBaseAndSmoothingPass1 = _effect.Techniques["CrtBaseAndSmoothingPass1"];
            _effectTechniqueCrtScan = _effect.Techniques["CrtScan"];
            _effectTechniqueCrtScanCa = _effect.Techniques["CrtScanCa"];
            _effectTechniqueCrtSmoothingPass2 = _effect.Techniques["CrtSmoothingPass2"];

            _originalSizeParameter = _effect.Parameters["OriginalSize"];
            _outputSizeParameter = _effect.Parameters["OutputSize"];
            _pixelWidthParameter = _effect.Parameters["PixelWidth"];
            _pixelHeightParameter = _effect.Parameters["PixelHeight"];

            _exposureParameter = _effect.Parameters["Exposure"];
            _vibranceParameter = _effect.Parameters["Vibrance"];

            _smoothingWeightBParameter = _effect.Parameters["SmoothingWeightB"];
            _smoothingWeightSParameter = _effect.Parameters["SmoothingWeightS"];

            _crtSmoothingWeightP1BParameter = _effect.Parameters["CrtSmoothingWeightP1B"];
            _crtSmoothingWeightP1HParameter = _effect.Parameters["CrtSmoothingWeightP1H"];
            _crtSmoothingWeightP1VParameter = _effect.Parameters["CrtSmoothingWeightP1V"];
            _crtSmoothingWeightP2BParameter = _effect.Parameters["CrtSmoothingWeightP2B"];
            _crtSmoothingWeightP2HParameter = _effect.Parameters["CrtSmoothingWeightP2H"];
            _crtSmoothingWeightP2VParameter = _effect.Parameters["CrtSmoothingWeightP2V"];

            _scanMaskStrenghtParameter = _effect.Parameters["ScanMaskStrenght"];
            _scanScaleParameter = _effect.Parameters["ScanScale"];
            _scanKernelShapeParameter = _effect.Parameters["ScanKernelShape"];
            _scanBrightnessBoostParameter = _effect.Parameters["ScanBrightnessBoost"];

            _warpXParameter = _effect.Parameters["WarpX"];
            _warpYParameter = _effect.Parameters["WarpY"];

            _caRedOffsetParameter = _effect.Parameters["CaRedOffset"];
            _caBlueOffsetParameter = _effect.Parameters["CaBlueOffset"];
        }

        PostProcessingHelper.CreateRenderTarget(
            ref _intermediatePass,
            PostProcessingHelper.PresentationWidth,
            PostProcessingHelper.PresentationHeight,
            SurfaceFormat.HalfVector4);
        ApplySettings();
    }

    void ApplySettings()
    {

        _originalSizeParameter.SetValue(new Vector2(PostProcessingHelper.BaseWidth, PostProcessingHelper.BaseHeight));
        _outputSizeParameter.SetValue(new Vector2(PostProcessingHelper.PresentationWidth, PostProcessingHelper.PresentationHeight));
        _pixelWidthParameter.SetValue(1f / PostProcessingHelper.PresentationWidth);
        _pixelHeightParameter.SetValue(1f / PostProcessingHelper.PresentationHeight);

        _exposureParameter.SetValue(Exposure);
        _vibranceParameter.SetValue(Vibrance);

        _smoothingWeightBParameter.SetValue(0.68f);
        _smoothingWeightSParameter.SetValue(0.32f);

        _crtSmoothingWeightP1BParameter.SetValue(0.7f);
        _crtSmoothingWeightP1HParameter.SetValue(0.15f);
        _crtSmoothingWeightP1VParameter.SetValue(0.15f);
        _crtSmoothingWeightP2BParameter.SetValue(0.3f);
        _crtSmoothingWeightP2HParameter.SetValue(0.5f);
        _crtSmoothingWeightP2VParameter.SetValue(0.2f);

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

        _scanMaskStrenghtParameter.SetValue(scanMaskStrenght);
        _scanScaleParameter.SetValue(-8.0f);
        _scanKernelShapeParameter.SetValue(2.0f);
        _scanBrightnessBoostParameter.SetValue(scanBrightnessBoost);

        if (CrtMode == CrtModeOption.NoScansNoCurvature)
        {
            _warpXParameter.SetValue(0f);
            _warpYParameter.SetValue(0f);
        }
        else
        {
            _warpXParameter.SetValue(0.01f);
            _warpYParameter.SetValue(0.02f);
        }

        _caRedOffsetParameter.SetValue(0.0006f);
        _caBlueOffsetParameter.SetValue(0.0006f);

        _baseTechniqueToUse = IsSmoothingFilterEnabled ? _effectTechniqueBaseAndSmoothing : _effectTechniqueBase;
        _scanTechniqueToUse = IsChromaticAberrationEnabled ? _effectTechniqueCrtScanCa : _effectTechniqueCrtScan;
    }

    public void Apply(Texture2D source)
    {
        // This could be smarter to only apply when it changes, but for now this will do:
        ApplySettings();

        var device = FlatRedBallServices.GraphicsDevice;
        var output = device.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
        var spriteBatch = _spriteBatch;

        if (CrtMode == CrtModeOption.Disabled)
        {
            // Base ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.Clear(Color.Transparent);

            _effect.CurrentTechnique = _baseTechniqueToUse;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, _effect);
            spriteBatch.Draw(source, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////
        }
        else
        {
            // Base and smoothing pass 1 ///////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.Clear(Color.Transparent);

            _effect.CurrentTechnique = _effectTechniqueCrtBaseAndSmoothingPass1;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, _effect);
            spriteBatch.Draw(source, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////

            // Scan ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.SetRenderTarget(_intermediatePass);
            device.Clear(Color.Transparent);

            _effect.CurrentTechnique = _scanTechniqueToUse;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, _effect);
            spriteBatch.Draw(output, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////

            // Smoothing pass 2 ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////
            device.SetRenderTarget(output);
            device.Clear(Color.Transparent);

            _effect.CurrentTechnique = _effectTechniqueCrtSmoothingPass2;
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, null, _effect);
            spriteBatch.Draw(_intermediatePass, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            spriteBatch.End();

            ////////////////////////////////////////////////////////////////////////////////////////////////
        }


    }


    internal static class PostProcessingHelper
    {
        internal static int BaseWidth { get; set; }
        internal static int BaseHeight { get; set; }

        internal static int PresentationWidth { get; set; }
        internal static int PresentationHeight { get; set; }

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
}
