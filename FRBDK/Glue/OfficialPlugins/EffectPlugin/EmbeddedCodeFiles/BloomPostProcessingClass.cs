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
    SpriteBatch _spriteBatch;

    public float Threshold = .6f;
    public float StrengthMultiplier = 1f;
    public float RadiusMultiplier = 1f;
    public bool UseLuminance = false;
    public float Intensity = 0.25f;
    public float Saturation = 1.3f;
    public float Quality = .5f;

    public bool PreserveContents = true;
    RenderTarget2D _bloomRenderTarget2DMip0;
    RenderTarget2D _bloomRenderTarget2DMip1;
    RenderTarget2D _bloomRenderTarget2DMip2;
    RenderTarget2D _bloomRenderTarget2DMip3;
    RenderTarget2D _bloomRenderTarget2DMip4;
    RenderTarget2D _bloomRenderTarget2DMip5;

    Effect _bloomEffect;

    EffectPass _bloomPassExtract;
    EffectPass _bloomPassExtractLuminance;
    EffectPass _bloomPassDownsample;
    EffectPass _bloomPassUpsample;
    EffectPass _bloomPassUpsampleLuminance;

    EffectParameter _bloomParameterScreenTexture;
    EffectParameter _bloomHalfPixelParameter;
    EffectParameter _bloomDownsampleOffsetParameter;
    EffectParameter _bloomUpsampleOffsetParameter;
    EffectParameter _bloomStrengthParameter;
    EffectParameter _bloomThresholdParameter;

    Effect _bloomCombineEffect;

    EffectTechnique _bloomTechniqueCombine;
    EffectTechnique _bloomTechniqueSaturate;

    EffectParameter _bloomCombineBLTextureParameter;
    EffectParameter _bloomCombineBLBaseTextureParameter;
    EffectParameter _bloomCombineBLIntensityParameter;
    EffectParameter _bloomCombineBLSaturationParameter;

    float _bloomRadius1 = 1.0f;
    float _bloomRadius2 = 1.0f;
    float _bloomRadius3 = 1.0f;
    float _bloomRadius4 = 1.0f;
    float _bloomRadius5 = 1.0f;

    float _bloomStrength1 = 1.0f;
    float _bloomStrength2 = 1.0f;
    float _bloomStrength3 = 1.0f;
    float _bloomStrength4 = 1.0f;
    float _bloomStrength5 = 1.0f;

    public float BloomStrengthMultiplier = 1.0f;

    float _radiusMultiplier = 1.0f;

    public bool BloomUseLuminance = true;
    public int BloomDownsamplePasses = 5;

    public BloomPresets BloomPreset
    {
        get { return _bloomPreset; }
        set
        {
            if (_bloomPreset == value) return;

            _bloomPreset = value;
            SetBloomPreset(_bloomPreset);
        }
    }
    BloomPresets _bloomPreset;


    Texture2D BloomScreenTexture { set { _bloomParameterScreenTexture.SetValue(value); } }

    Vector2 HalfPixel
    {
        get { return _halfPixel; }
        set
        {
            if (value != _halfPixel)
            {
                _halfPixel = value;
                _bloomHalfPixelParameter.SetValue(_halfPixel);
            }
        }
    }
    Vector2 _halfPixel;

    float BloomStrength
    {
        get { return _bloomStrength; }
        set
        {
            if (Math.Abs(_bloomStrength - value) > 0.001f)
            {
                _bloomStrength = value;
                _bloomStrengthParameter.SetValue(_bloomStrength * BloomStrengthMultiplier);
            }

        }
    }
    float _bloomStrength;

    public float BloomThreshold
    {
        get { return _bloomThreshold; }
        set
        {
            if (Math.Abs(_bloomThreshold - value) > 0.001f)
            {
                _bloomThreshold = value;
                _bloomThresholdParameter.SetValue(_bloomThreshold);
            }
        }
    }
    float _bloomThreshold;

    Vector2 BloomInverseResolution
    {
        get { return _bloomInverseResolution; }
        set
        {
            if (value != _bloomInverseResolution)
                _bloomInverseResolution = value;
        }
    }
    Vector2 _bloomInverseResolution;

    public float BloomStreakLength
    {
        get { return _bloomStreakLength; }
        set
        {
            if (Math.Abs(_bloomStreakLength - value) > 0.001f)
                _bloomStreakLength = value;
        }
    }
    float _bloomStreakLength;

    float BloomRadius
    {
        get { return _bloomRadius; }
        set
        {
            if (Math.Abs(_bloomRadius - value) > 0.001f)
                _bloomRadius = value;
        }
    }
    float _bloomRadius;

    #endregion

    public ReplaceClassName(Effect effect)
    {
        _spriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

        InitializeEffects(effect, effect);

        PostProcessingHelper.BaseWidth = 320;
        PostProcessingHelper.BaseHeight = 180;
        PostProcessingHelper.PresentationWidth = FlatRedBallServices.Game.Window.ClientBounds.Width;
        PostProcessingHelper.PresentationHeight = FlatRedBallServices.Game.Window.ClientBounds.Height;

        ApplySettings();
    }

    void UpdateDownsampleOffsetParameter()
    {
        var offset = _bloomInverseResolution;
        offset.X *= _bloomStreakLength;
        _bloomDownsampleOffsetParameter.SetValue(offset);
    }

    void UpdateUpsampleOffsetParameter()
    {
        var offset = _bloomInverseResolution;
        offset.X *= _bloomStreakLength;
        offset *= _bloomRadius * _radiusMultiplier;
        _bloomUpsampleOffsetParameter.SetValue(offset);
    }

    void InitializeEffects(Effect bloomEffect, Effect bloomCombine)
    {
        if (_bloomEffect == null || _bloomEffect.IsDisposed)
        {
            _bloomEffect = bloomEffect;
            _bloomHalfPixelParameter = _bloomEffect.Parameters["HalfPixel"];
            _bloomDownsampleOffsetParameter = _bloomEffect.Parameters["DownsampleOffset"];
            _bloomUpsampleOffsetParameter = _bloomEffect.Parameters["UpsampleOffset"];
            _bloomStrengthParameter = _bloomEffect.Parameters["Strength"];
            _bloomThresholdParameter = _bloomEffect.Parameters["Threshold"];

            _bloomParameterScreenTexture = _bloomEffect.Parameters["LinearSampler+ScreenTexture"];

            _bloomPassExtract = _bloomEffect.Techniques["Extract"].Passes[0];
            _bloomPassExtractLuminance = _bloomEffect.Techniques["ExtractLuminance"].Passes[0];
            _bloomPassDownsample = _bloomEffect.Techniques["Downsample"].Passes[0];
            _bloomPassUpsample = _bloomEffect.Techniques["Upsample"].Passes[0];
            _bloomPassUpsampleLuminance = _bloomEffect.Techniques["UpsampleLuminance"].Passes[0];
        }

        if (_bloomCombineEffect == null || _bloomCombineEffect.IsDisposed)
        {
            _bloomCombineEffect = bloomCombine;

            _bloomTechniqueCombine = _bloomCombineEffect.Techniques["BloomCombine"];
            _bloomTechniqueSaturate = _bloomCombineEffect.Techniques["BloomSaturate"];

            _bloomCombineBLTextureParameter = _bloomCombineEffect.Parameters["BloomTexture"];
            _bloomCombineBLBaseTextureParameter = _bloomCombineEffect.Parameters["BaseTexture"];
            _bloomCombineBLIntensityParameter = _bloomCombineEffect.Parameters["BloomIntensity"];
            _bloomCombineBLSaturationParameter = _bloomCombineEffect.Parameters["BloomSaturation"];
        }
    }

    void ApplySettings()
    {
        int width = (int)(FlatRedBallServices.ClientWidth * Quality);
        int height = (int)(FlatRedBallServices.ClientHeight * Quality);

        var usage = PreserveContents ? RenderTargetUsage.PreserveContents : RenderTargetUsage.DiscardContents;

        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip0, width, height, SurfaceFormat.HalfVector4);
        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip1, width / 2, height / 2, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip2, width / 4, height / 4, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip3, width / 8, height / 8, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip4, width / 16, height / 16, SurfaceFormat.HalfVector4, usage);
        PostProcessingHelper.CreateRenderTarget(ref _bloomRenderTarget2DMip5, width / 32, height / 32, SurfaceFormat.HalfVector4, usage);

        SetBloomPreset(this.BloomPreset);
        _radiusMultiplier = RadiusMultiplier;
        BloomThreshold = Threshold;
        BloomStrengthMultiplier = StrengthMultiplier;
        BloomUseLuminance = UseLuminance;
        _bloomCombineBLIntensityParameter.SetValue(Intensity);
        _bloomCombineBLSaturationParameter.SetValue(Saturation);
    }

    void SetBloomPreset(BloomPresets preset)
    {
        switch (preset)
        {
            case BloomPresets.Wide:
                {
                    _bloomStrength1 = 0.5f;
                    _bloomStrength2 = 1;
                    _bloomStrength3 = 2;
                    _bloomStrength4 = 1;
                    _bloomStrength5 = 2;
                    _bloomRadius5 = 4.0f;
                    _bloomRadius4 = 4.0f;
                    _bloomRadius3 = 2.0f;
                    _bloomRadius2 = 2.0f;
                    _bloomRadius1 = 1.0f;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.SuperWide:
                {
                    _bloomStrength1 = 0.9f;
                    _bloomStrength2 = 1;
                    _bloomStrength3 = 1;
                    _bloomStrength4 = 2;
                    _bloomStrength5 = 6;
                    _bloomRadius5 = 4.0f;
                    _bloomRadius4 = 2.0f;
                    _bloomRadius3 = 2.0f;
                    _bloomRadius2 = 2.0f;
                    _bloomRadius1 = 2.0f;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Focussed:
                {
                    _bloomStrength1 = 0.8f;
                    _bloomStrength2 = 1;
                    _bloomStrength3 = 1;
                    _bloomStrength4 = 1;
                    _bloomStrength5 = 2;
                    _bloomRadius5 = 4.0f;
                    _bloomRadius4 = 2.0f;
                    _bloomRadius3 = 2.0f;
                    _bloomRadius2 = 2.0f;
                    _bloomRadius1 = 2.0f;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Small:
                {
                    _bloomStrength1 = 0.8f;
                    _bloomStrength2 = 1;
                    _bloomStrength3 = 1;
                    _bloomStrength4 = 1;
                    _bloomStrength5 = 1;
                    _bloomRadius5 = 1;
                    _bloomRadius4 = 1;
                    _bloomRadius3 = 1;
                    _bloomRadius2 = 1;
                    _bloomRadius1 = 1;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 5;
                    break;
                }
            case BloomPresets.Cheap:
                {
                    _bloomStrength1 = 0.8f;
                    _bloomStrength2 = 2;
                    _bloomRadius2 = 2;
                    _bloomRadius1 = 2;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 2;
                    break;
                }
            case BloomPresets.One:
                {
                    _bloomStrength1 = 4f;
                    _bloomStrength2 = 1;
                    _bloomStrength3 = 1;
                    _bloomStrength4 = 1;
                    _bloomStrength5 = 2;
                    _bloomRadius5 = 1.0f;
                    _bloomRadius4 = 1.0f;
                    _bloomRadius3 = 1.0f;
                    _bloomRadius2 = 1.0f;
                    _bloomRadius1 = 1.0f;
                    BloomStreakLength = 1;
                    BloomDownsamplePasses = 5;
                    break;
                }
        }
    }

    public void Apply(Texture2D source)
    {
        var device = FlatRedBallServices.GraphicsDevice;

        device.RasterizerState = RasterizerState.CullNone;
        device.BlendState = BlendState.Opaque;

        var output = device.GetRenderTargets().FirstOrDefault().RenderTarget as RenderTarget2D;

        // EXTRACT
        // We extract the bright values which are above the Threshold and save them to Mip0
        device.SetRenderTarget(_bloomRenderTarget2DMip0);

        BloomScreenTexture = source;

        int clientScale = (int)Math.Ceiling((float)PostProcessingHelper.PresentationHeight / PostProcessingHelper.BaseHeight);

        // According to Miguel this is no longer needed
        //HalfPixel = new Vector2(1.0f / PostProcessingHelper.PresentationWidth, 1.0f / PostProcessingHelper.PresentationHeight) / clientScale;
        HalfPixel = Vector2.Zero;
        BloomInverseResolution = new Vector2(1.0f / PostProcessingHelper.PresentationWidth, 1.0f / PostProcessingHelper.PresentationHeight);

        device.SamplerStates[0] = SamplerState.LinearClamp;

        if (BloomUseLuminance) _bloomPassExtractLuminance.Apply();
        else _bloomPassExtract.Apply();
        QuadRenderer.RenderFitViewport();

        // Now downsample to the next lower mip texture
        if (BloomDownsamplePasses > 0)
        {
            //DOWNSAMPLE TO MIP1
            device.SetRenderTarget(_bloomRenderTarget2DMip1);

            BloomScreenTexture = _bloomRenderTarget2DMip0;
            UpdateDownsampleOffsetParameter();

            //Pass
            _bloomPassDownsample.Apply();
            QuadRenderer.RenderFitViewport();

            if (BloomDownsamplePasses > 1)
            {
                //Our input resolution is halved, so our inverse 1/res. must be doubled
                HalfPixel *= 2;
                BloomInverseResolution *= 2;

                //DOWNSAMPLE TO MIP2
                device.SetRenderTarget(_bloomRenderTarget2DMip2);

                BloomScreenTexture = _bloomRenderTarget2DMip1;
                UpdateDownsampleOffsetParameter();

                //Pass
                _bloomPassDownsample.Apply();
                QuadRenderer.RenderFitViewport();

                if (BloomDownsamplePasses > 2)
                {
                    HalfPixel *= 2;
                    BloomInverseResolution *= 2;

                    //DOWNSAMPLE TO MIP3
                    device.SetRenderTarget(_bloomRenderTarget2DMip3);

                    BloomScreenTexture = _bloomRenderTarget2DMip2;
                    UpdateDownsampleOffsetParameter();

                    //Pass
                    _bloomPassDownsample.Apply();
                    QuadRenderer.RenderFitViewport();

                    if (BloomDownsamplePasses > 3)
                    {
                        HalfPixel *= 2;
                        BloomInverseResolution *= 2;

                        //DOWNSAMPLE TO MIP4
                        device.SetRenderTarget(_bloomRenderTarget2DMip4);

                        BloomScreenTexture = _bloomRenderTarget2DMip3;
                        UpdateDownsampleOffsetParameter();

                        //Pass
                        _bloomPassDownsample.Apply();
                        QuadRenderer.RenderFitViewport();

                        if (BloomDownsamplePasses > 4)
                        {
                            HalfPixel *= 2;
                            BloomInverseResolution *= 2;

                            //DOWNSAMPLE TO MIP5
                            device.SetRenderTarget(_bloomRenderTarget2DMip5);

                            BloomScreenTexture = _bloomRenderTarget2DMip4;
                            UpdateDownsampleOffsetParameter();

                            //Pass
                            _bloomPassDownsample.Apply();
                            QuadRenderer.RenderFitViewport();

                            ChangeBlendState();

                            //UPSAMPLE TO MIP4
                            device.SetRenderTarget(_bloomRenderTarget2DMip4);

                            BloomScreenTexture = _bloomRenderTarget2DMip5;
                            BloomStrength = _bloomStrength5;
                            BloomRadius = _bloomRadius5;
                            UpdateUpsampleOffsetParameter();

                            if (BloomUseLuminance) _bloomPassUpsampleLuminance.Apply();
                            else _bloomPassUpsample.Apply();
                            QuadRenderer.RenderFitViewport();

                            HalfPixel /= 2;
                            BloomInverseResolution /= 2;
                        }

                        ChangeBlendState();

                        //UPSAMPLE TO MIP3
                        device.SetRenderTarget(_bloomRenderTarget2DMip3);

                        BloomScreenTexture = _bloomRenderTarget2DMip4;
                        BloomStrength = _bloomStrength4;
                        BloomRadius = _bloomRadius4;
                        UpdateUpsampleOffsetParameter();

                        if (BloomUseLuminance) _bloomPassUpsampleLuminance.Apply();
                        else _bloomPassUpsample.Apply();
                        QuadRenderer.RenderFitViewport();

                        HalfPixel /= 2;
                        BloomInverseResolution /= 2;
                    }

                    ChangeBlendState();

                    //UPSAMPLE TO MIP2
                    device.SetRenderTarget(_bloomRenderTarget2DMip2);

                    BloomScreenTexture = _bloomRenderTarget2DMip3;
                    BloomStrength = _bloomStrength3;
                    BloomRadius = _bloomRadius3;
                    UpdateUpsampleOffsetParameter();

                    if (BloomUseLuminance) _bloomPassUpsampleLuminance.Apply();
                    else _bloomPassUpsample.Apply();
                    QuadRenderer.RenderFitViewport();

                    HalfPixel /= 2;
                    BloomInverseResolution /= 2;
                }

                ChangeBlendState();

                //UPSAMPLE TO MIP1
                device.SetRenderTarget(_bloomRenderTarget2DMip1);

                BloomScreenTexture = _bloomRenderTarget2DMip2;
                BloomStrength = _bloomStrength2;
                BloomRadius = _bloomRadius2;
                UpdateUpsampleOffsetParameter();

                if (BloomUseLuminance) _bloomPassUpsampleLuminance.Apply();
                else _bloomPassUpsample.Apply();
                QuadRenderer.RenderFitViewport();

                HalfPixel /= 2;
                BloomInverseResolution /= 2;
            }

            ChangeBlendState();

            //UPSAMPLE TO MIP0
            device.SetRenderTarget(_bloomRenderTarget2DMip0);

            BloomScreenTexture = _bloomRenderTarget2DMip1;
            BloomStrength = _bloomStrength1;
            BloomRadius = _bloomRadius1;
            UpdateUpsampleOffsetParameter();

            if (BloomUseLuminance) _bloomPassUpsampleLuminance.Apply();
            else _bloomPassUpsample.Apply();
            QuadRenderer.RenderFitViewport();
        }

        device.SamplerStates[1] = SamplerState.LinearClamp;

        // Combine base image and bloom image.
        _bloomCombineEffect.CurrentTechnique = _bloomTechniqueCombine;
        _bloomCombineBLBaseTextureParameter.SetValue(source);

        FlatRedBallServices.GraphicsDevice.SetRenderTarget(output);
        DrawFullscreenQuadRT(_bloomRenderTarget2DMip0, output, _bloomCombineEffect);

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
        var spriteBatch = _spriteBatch;
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

        static GraphicsDevice _device;

        static VertexPositionTexture[] _fitViewportVertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1))
        };

        static VertexPositionTexture[] _customVertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 1))
        };

        static VertexPositionColorTexture[] _fitViewportColorVertices = new VertexPositionColorTexture[]
        {
            new VertexPositionColorTexture(new Vector3(-1, 1, 0), Color.White, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1, 1, 0), Color.White, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1, -1, 0), Color.White, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(-1, -1, 0), Color.White, new Vector2(0, 1))
        };

        static VertexPositionColorTexture[] _customColorVertices = new VertexPositionColorTexture[]
        {
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(0, 0, 0), Color.White, new Vector2(0, 1))
        };

        static short[] _indices = { 0, 1, 2, 2, 3, 0 };
        static ushort[] _bufferedIndices = { 0, 1, 2, 2, 3, 0 };

        static VertexBuffer _vertexBuffer;
        static VertexBuffer _vertexColorBuffer;
        static IndexBuffer _indexBuffer;

        public static bool UseVertexColor { get; set; }

        static QuadRenderer()
        {
            _device = FlatRedBallServices.GraphicsDevice;

            _vertexBuffer = new VertexBuffer(_device, typeof(VertexPositionTexture), _fitViewportVertices.Length, BufferUsage.None);

            _vertexColorBuffer = new VertexBuffer(_device, typeof(VertexPositionColorTexture), _fitViewportColorVertices.Length, BufferUsage.None);

            _indexBuffer = new IndexBuffer(_device, typeof(ushort), _bufferedIndices.Length, BufferUsage.None);
            _indexBuffer.SetData(_bufferedIndices);
        }

        public static void SetBuffers(bool useCustomVertices)
        {
            _device.Indices = _indexBuffer;

            if (UseVertexColor)
            {
                if (useCustomVertices)
                    _vertexColorBuffer.SetData(_customColorVertices);
                else
                    _vertexColorBuffer.SetData(_fitViewportColorVertices);

                _device.SetVertexBuffer(_vertexColorBuffer);

            }
            else
            {
                if (useCustomVertices)
                    _vertexBuffer.SetData(_customVertices);
                else
                    _vertexBuffer.SetData(_fitViewportVertices);

                _device.SetVertexBuffer(_vertexBuffer);
            }
        }

        public static void UnsetBuffers()
        {
            _device.Indices = null;
            _device.SetVertexBuffer(null);
        }

        public static void UpdateCustomVerticesPosition(int destinationWidth, int destinationHeight)
        {
            float viewportWidth = _device.Viewport.Width;
            float viewportHeight = _device.Viewport.Height;
            float horizontalPosition = 1f + ((destinationWidth - viewportWidth) / viewportWidth);
            float verticalPosition = -1f - ((destinationHeight - viewportHeight) / viewportHeight);

            if (UseVertexColor)
            {
                _customColorVertices[0].Position.X = -1f;
                _customColorVertices[0].Position.Y = 1f;

                _customColorVertices[1].Position.X = horizontalPosition;
                _customColorVertices[1].Position.Y = 1f;

                _customColorVertices[2].Position.X = horizontalPosition;
                _customColorVertices[2].Position.Y = verticalPosition;

                _customColorVertices[3].Position.X = -1f;
                _customColorVertices[3].Position.Y = verticalPosition;
            }
            else
            {
                _customVertices[0].Position.X = -1f;
                _customVertices[0].Position.Y = 1f;

                _customVertices[1].Position.X = horizontalPosition;
                _customVertices[1].Position.Y = 1f;

                _customVertices[2].Position.X = horizontalPosition;
                _customVertices[2].Position.Y = verticalPosition;

                _customVertices[3].Position.X = -1f;
                _customVertices[3].Position.Y = verticalPosition;
            }
        }

        public static void UpdateCustomVerticesPosition(Vector2 topLeft, Vector2 bottomRight)
        {
            if (UseVertexColor)
            {
                _customColorVertices[0].Position.X = topLeft.X;
                _customColorVertices[0].Position.Y = topLeft.Y;

                _customColorVertices[1].Position.X = bottomRight.X;
                _customColorVertices[1].Position.Y = topLeft.Y;

                _customColorVertices[2].Position.X = bottomRight.X;
                _customColorVertices[2].Position.Y = bottomRight.Y;

                _customColorVertices[3].Position.X = topLeft.X;
                _customColorVertices[3].Position.Y = bottomRight.Y;
            }
            else
            {
                _customVertices[0].Position.X = topLeft.X;
                _customVertices[0].Position.Y = topLeft.Y;

                _customVertices[1].Position.X = bottomRight.X;
                _customVertices[1].Position.Y = topLeft.Y;

                _customVertices[2].Position.X = bottomRight.X;
                _customVertices[2].Position.Y = bottomRight.Y;

                _customVertices[3].Position.X = topLeft.X;
                _customVertices[3].Position.Y = bottomRight.Y;
            }
        }

        public static void UpdateCustomVerticesTexCoord(Vector2 topLeft, Vector2 bottomRight)
        {
            if (UseVertexColor)
            {
                _customColorVertices[0].TextureCoordinate.X = topLeft.X;
                _customColorVertices[0].TextureCoordinate.Y = topLeft.Y;

                _customColorVertices[1].TextureCoordinate.X = bottomRight.X;
                _customColorVertices[1].TextureCoordinate.Y = topLeft.Y;

                _customColorVertices[2].TextureCoordinate.X = bottomRight.X;
                _customColorVertices[2].TextureCoordinate.Y = bottomRight.Y;

                _customColorVertices[3].TextureCoordinate.X = topLeft.X;
                _customColorVertices[3].TextureCoordinate.Y = bottomRight.Y;
            }
            else
            {
                _customVertices[0].TextureCoordinate.X = topLeft.X;
                _customVertices[0].TextureCoordinate.Y = topLeft.Y;

                _customVertices[1].TextureCoordinate.X = bottomRight.X;
                _customVertices[1].TextureCoordinate.Y = topLeft.Y;

                _customVertices[2].TextureCoordinate.X = bottomRight.X;
                _customVertices[2].TextureCoordinate.Y = bottomRight.Y;

                _customVertices[3].TextureCoordinate.X = topLeft.X;
                _customVertices[3].TextureCoordinate.Y = bottomRight.Y;
            }
        }

        public static void RenderBuffered()
        {
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
        }

        public static void RenderFitViewport()
        {
            if (UseVertexColor)
                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _fitViewportColorVertices, 0, 4, _indices, 0, 2);
            else
                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _fitViewportVertices, 0, 4, _indices, 0, 2);
        }

        public static void RenderCustom()
        {
            if (UseVertexColor)
                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _customColorVertices, 0, 4, _indices, 0, 2);
            else
                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _customVertices, 0, 4, _indices, 0, 2);
        }
    }

    #endregion

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

