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

    SpriteBatch _spriteBatch;

    bool _needsApplySettings;

    #endregion

    #region Exposed settings

    public CrtModeOption CrtMode
    {
        get { return _crtMode; }
        set
        {
            if (_crtMode == value) return;

            _crtMode = value;
            _needsApplySettings = true;
        }
    }
    CrtModeOption _crtMode = CrtModeOption.Full;

    public bool IsSmoothingFilterEnabled
    {
        get { return _isSmoothingFilterEnabled; }
        set
        {
            if (_isSmoothingFilterEnabled == value) return;

            _isSmoothingFilterEnabled = value;
            _needsApplySettings = true;
        }
    }
    bool _isSmoothingFilterEnabled = true;

    public bool IsChromaticAberrationEnabled
    {
        get { return _isChromaticAberrationEnabled; }
        set
        {
            if (_isChromaticAberrationEnabled == value) return;

            _isChromaticAberrationEnabled = value;
            _needsApplySettings = true;
        }
    }
    bool _isChromaticAberrationEnabled = true;

    public float Exposure
    {
        get { return _exposure; }
        set
        {
            if (_exposure == value) return;

            _exposure = value;
            _needsApplySettings = true;
        }
    }
    float _exposure = 1.00f;

    public float Vibrance
    {
        get { return _vibrance; }
        set
        {
            if (_vibrance == value) return;

            _vibrance = value;
            _needsApplySettings = true;
        }
    }
    float _vibrance = 0.18f;

    public float SmoothingWeightB
    {
        get { return _smoothingWeightB; }
        set
        {
            if (_smoothingWeightB == value) return;

            _smoothingWeightB = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightB = 0.68f;

    public float SmoothingWeightS
    {
        get { return _smoothingWeightS; }
        set
        {
            if (_smoothingWeightS == value) return;

            _smoothingWeightS = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightS = 0.32f;

    public float SmoothingWeightP1B
    {
        get { return _smoothingWeightP1B; }
        set
        {
            if (_smoothingWeightP1B == value) return;

            _smoothingWeightP1B = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP1B = 0.7f;

    public float SmoothingWeightP1H
    {
        get { return _smoothingWeightP1H; }
        set
        {
            if (_smoothingWeightP1H == value) return;

            _smoothingWeightP1H = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP1H = 0.15f;

    public float SmoothingWeightP1V
    {
        get { return _smoothingWeightP1V; }
        set
        {
            if (_smoothingWeightP1V == value) return;

            _smoothingWeightP1V = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP1V = 0.15f;

    public float SmoothingWeightP2B
    {
        get { return _smoothingWeightP2B; }
        set
        {
            if (_smoothingWeightP2B == value) return;

            _smoothingWeightP2B = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP2B = 0.3f;

    public float SmoothingWeightP2H
    {
        get { return _smoothingWeightP2H; }
        set
        {
            if (_smoothingWeightP2H == value) return;

            _smoothingWeightP2H = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP2H = 0.5f;

    public float SmoothingWeightP2V
    {
        get { return _smoothingWeightP2V; }
        set
        {
            if (_smoothingWeightP2V == value) return;

            _smoothingWeightP2V = value;
            _needsApplySettings = true;
        }
    }
    float _smoothingWeightP2V = 0.2f;

    public float ScanMaskStrenght
    {
        get { return _scanMaskStrenght; }
        set
        {
            if (_scanMaskStrenght == value) return;

            _scanMaskStrenght = value;
            _needsApplySettings = true;
        }
    }
    float _scanMaskStrenght = 0.525f;

    public float ScanScale
    {
        get { return _scanScale; }
        set
        {
            if (_scanScale == value) return;

            _scanScale = value;
            _needsApplySettings = true;
        }
    }
    float _scanScale = -8.0f;

    public float ScanKernelShape
    {
        get { return _scanKernelShape; }
        set
        {
            if (_scanKernelShape == value) return;

            _scanKernelShape = value;
            _needsApplySettings = true;
        }
    }
    float _scanKernelShape = 2.0f;

    public float ScanBrightnessBoost
    {
        get { return _scanBrightnessBoost; }
        set
        {
            if (_scanBrightnessBoost == value) return;

            _scanBrightnessBoost = value;
            _needsApplySettings = true;
        }
    }
    float _scanBrightnessBoost = 1.11f;

    public float WarpX
    {
        get { return _warpX; }
        set
        {
            if (_warpX == value) return;

            _warpX = value;
            _needsApplySettings = true;
        }
    }
    float _warpX = 0.01f;

    public float WarpY
    {
        get { return _warpY; }
        set
        {
            if (_warpY == value) return;

            _warpY = value;
            _needsApplySettings = true;
        }
    }
    float _warpY = 0.02f;

    public float ChromaticAberrationRedOffset
    {
        get { return _caRedOffset; }
        set
        {
            if (_caRedOffset == value) return;

            _caRedOffset = value;
            _needsApplySettings = true;
        }
    }
    float _caRedOffset = 0.0006f;

    public float ChromaticAberrationBlueOffset
    {
        get { return _caBlueOffset; }
        set
        {
            if (_caBlueOffset == value) return;

            _caBlueOffset = value;
            _needsApplySettings = true;
        }
    }
    float _caBlueOffset = 0.0006f;

    #endregion

    public ReplaceClassName(Effect effect)
    {
        PostProcessingHelper.BaseWidth = 320;
        PostProcessingHelper.BaseHeight = 180;

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
            ref _intermediatePass,
            FlatRedBallServices.Game.Window.ClientBounds.Width,
            FlatRedBallServices.Game.Window.ClientBounds.Height,
            SurfaceFormat.HalfVector4);
    }

    void ApplySettings()
    {
        _originalSizeParameter.SetValue(new Vector2(PostProcessingHelper.BaseWidth, PostProcessingHelper.BaseHeight));
        _outputSizeParameter.SetValue(new Vector2(FlatRedBallServices.Game.Window.ClientBounds.Width, FlatRedBallServices.Game.Window.ClientBounds.Height));
        _pixelWidthParameter.SetValue(1f / FlatRedBallServices.Game.Window.ClientBounds.Width);
        _pixelHeightParameter.SetValue(1f / FlatRedBallServices.Game.Window.ClientBounds.Height);

        _exposureParameter.SetValue(_exposure);
        _vibranceParameter.SetValue(_vibrance);

        _smoothingWeightBParameter.SetValue(_smoothingWeightB);
        _smoothingWeightSParameter.SetValue(_smoothingWeightS);

        _crtSmoothingWeightP1BParameter.SetValue(_smoothingWeightP1B);
        _crtSmoothingWeightP1HParameter.SetValue(_smoothingWeightP1H);
        _crtSmoothingWeightP1VParameter.SetValue(_smoothingWeightP1V);
        _crtSmoothingWeightP2BParameter.SetValue(_smoothingWeightP2B);
        _crtSmoothingWeightP2HParameter.SetValue(_smoothingWeightP2H);
        _crtSmoothingWeightP2VParameter.SetValue(_smoothingWeightP2V);

        float scanMaskStrenght = _scanMaskStrenght;
        float scanBrightnessBoost = _scanBrightnessBoost;

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
        _scanScaleParameter.SetValue(_scanScale);
        _scanKernelShapeParameter.SetValue(_scanKernelShape);
        _scanBrightnessBoostParameter.SetValue(scanBrightnessBoost);

        if (CrtMode == CrtModeOption.NoScansNoCurvature)
        {
            _warpXParameter.SetValue(0f);
            _warpYParameter.SetValue(0f);
        }
        else
        {
            _warpXParameter.SetValue(_warpX);
            _warpYParameter.SetValue(_warpY);
        }

        _caRedOffsetParameter.SetValue(_caRedOffset);
        _caBlueOffsetParameter.SetValue(_caBlueOffset);

        _baseTechniqueToUse = IsSmoothingFilterEnabled ? _effectTechniqueBaseAndSmoothing : _effectTechniqueBase;
        _scanTechniqueToUse = IsChromaticAberrationEnabled ? _effectTechniqueCrtScanCa : _effectTechniqueCrtScan;

        _needsApplySettings = false;
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
        if (_needsApplySettings)
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
