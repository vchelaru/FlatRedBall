using System;
using System.Collections.Generic;
using System.Threading;
using FlatRedBall.Graphics.PostProcessing;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Performance.Measurement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Effect = Microsoft.Xna.Framework.Graphics.Effect;
using ShapeManager = FlatRedBall.Math.Geometry.ShapeManager;

namespace FlatRedBall.Graphics;

/// <summary>
/// Static class responsible for rendering content to the cameras on screen.
/// </summary> 
/// <remarks>This class is called by <see cref="FlatRedBallServices.Draw()"/></remarks>
public static class Renderer
{
    #region Fields / properties

    #region Core
    static internal IGraphicsDeviceService Graphics { get { return _graphics; } }
    static IGraphicsDeviceService _graphics;

    static internal GraphicsDevice GraphicsDevice { get { return _graphics.GraphicsDevice; } }

    public static Texture2D Texture
    {
        set
        {
            if (value != _texture)
            {
                ForceSetTexture(value);
            }
        }
    }
    static Texture2D _texture;

    static void ForceSetTexture(Texture2D value)
    {
        _texture = value;
        _effectManager.ParameterCurrentTexture.SetValue(_texture);
    }

    public static Texture2D TextureOnDevice
    {
        set
        {
            if (value != _texture)
            {
                _texture = value;
                GraphicsDevice.Textures[0] = _texture;
            }
        }
    }

    /// <summary>
    /// Sets the blend operation on the graphics device if the set value differs from the current value.
    /// If the two values are the same, then the property doesn't do anything.
    /// </summary>
    public static BlendOperation BlendOperation
    {
        get { return _blendOperation; }
        set
        {
            if (value != _blendOperation)
            {
                _blendOperation = value;
                ForceSetBlendOperation();
            }
        }
    }
    static BlendOperation _blendOperation;

    public static void ForceSetBlendOperation()
    {
        switch (_blendOperation)
        {
            case BlendOperation.Add:
                _graphics.GraphicsDevice.BlendState = BlendState.Additive;
                break;
            case BlendOperation.Regular:
                _graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                break;
            case BlendOperation.NonPremultipliedAlpha:
                _graphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                break;
            case BlendOperation.Modulate:
                {
                    var blendState = new BlendState();
                    blendState.AlphaSourceBlend = Blend.DestinationColor;
                    blendState.ColorSourceBlend = Blend.DestinationColor;

                    blendState.AlphaDestinationBlend = Blend.Zero;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
                    blendState.ColorBlendFunction = BlendFunction.Add;

                    _graphics.GraphicsDevice.BlendState = blendState;

                }
                break;
            case BlendOperation.SubtractAlpha:
                {
                    // Vic says 12/19/2020
                    // This took me a while to figure out, so I'll document what I learned.
                    // For alpha, the operation is:
                    // ResultAlpha = (SourceAlpha * Blend.AlphaSourceBlend) {BlendFunc} (DestinationAlpha * Blend.AlphaDestblend)
                    // where:
                    // ResultAlpha is the resulting pixel alpha after the operation occurs
                    // SourceAlpha is the alpha of the pixel on the sprite that is being drawn
                    // DestinationAlpha is the alpha of the pixel on the surface before the pixel is drawn, which is the result alpha from a previous operation
                    // In this case we want to subtract the sprite being drawn.
                    // To subtract the sprite that is being drawn, which is the SourceSprite, we need to do a ReverseSubtract
                    // so that the Source is being subtracted.
                    // We want to use Blend.One on both so that the values being used are the pixel values on source and dest.
                    // Keep in mind that since we're making a texture, we need this texture to be premultiplied, so we
                    // need to multiply the destination color by the inverse source alpha, so that if alpha is 0, we preserve the color, otherwise we
                    // darken it to premult

                    var blendState = new BlendState();

                    blendState.ColorSourceBlend = Blend.Zero;
                    blendState.ColorBlendFunction = BlendFunction.Add;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;

                    blendState.AlphaSourceBlend = Blend.One;
                    blendState.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                    blendState.AlphaDestinationBlend = Blend.One;

                    _graphics.GraphicsDevice.BlendState = blendState;
                }
                break;

            case BlendOperation.Modulate2X:
                {
                    var blendState = new BlendState();

                    blendState.AlphaSourceBlend = Blend.DestinationColor;
                    blendState.ColorSourceBlend = Blend.DestinationColor;

                    blendState.AlphaDestinationBlend = Blend.SourceColor;
                    blendState.ColorDestinationBlend = Blend.SourceColor;

                    _graphics.GraphicsDevice.BlendState = blendState;
                }
                break;

            default:
                throw new NotImplementedException("Blend operation not implemented: " + _blendOperation);
        }
    }

    public static TextureAddressMode TextureAddressMode
    {
        get { return _textureAddressMode; }
        set
        {
            if (value != _textureAddressMode)
            {
                ForceSetTextureAddressMode(value);
            }
        }
    }
    static TextureAddressMode _textureAddressMode;

    public static void ForceSetTextureAddressMode(TextureAddressMode value)
    {
        _textureAddressMode = value;
        FlatRedBallServices.GraphicsOptions.ForceRefreshSamplerState(0);
        FlatRedBallServices.GraphicsOptions.ForceRefreshSamplerState(1);
    }

    public static bool IsInRendering { get; set; }

    public static RenderMode CurrentRenderMode { get { return _currentRenderMode; } }
    static RenderMode _currentRenderMode = RenderMode.Default;

    #endregion

    #region Cameras and layers

    /// <summary>
    /// Gets the default camera (SpriteManager.Camera).
    /// </summary>
    static public Camera Camera { get { return SpriteManager.Camera; } }

    /// <summary>
    /// Gets the list of cameras (SpriteManager.Cameras).
    /// </summary>
    static public PositionedObjectList<Camera> Cameras { get { return SpriteManager.Cameras; } }

    static LayerCameraSettings _oldCameraLayerSettings = new LayerCameraSettings();

    /// <summary>
    /// Returns the layer currently being rendered. Can be used in IDrawableBatches and debug code.
    /// </summary>
    public static Layer CurrentLayer { get; private set; }

    internal static string CurrentLayerName
    {
        get
        {
            if (CurrentLayer != null)
            {
                return CurrentLayer.Name;
            }
            else
            {
                return "Unlayered";
            }
        }
    }

    #endregion

    #region Geometry

    public static VertexBuffer VertexBuffer
    {
        set
        {
            if (value != _vertexBuffer && value != null)
            {
                _vertexBuffer = value;
                GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            }
            else
            {
                _vertexBuffer = null;
            }
        }
    }
    static VertexBuffer _vertexBuffer;

    public static IndexBuffer IndexBuffer
    {
        set
        {
            if (value != _indexBuffer && value != null)
            {
                _indexBuffer = value;
                GraphicsDevice.Indices = _indexBuffer;
            }
            else
            {
                _indexBuffer = null;
            }
        }
    }
    static IndexBuffer _indexBuffer;

    static List<FillVertexLogic> _fillVertexLogics = new List<FillVertexLogic>();

    static List<VertexPositionColorTexture[]> _spriteVertices = new List<VertexPositionColorTexture[]>();
    static List<VertexPositionColorTexture[]> _zBufferedSpriteVertices = new List<VertexPositionColorTexture[]>();
    static List<VertexPositionColorTexture[]> _textVertices = new List<VertexPositionColorTexture[]>();
    static List<VertexPositionColor[]> _shapeVertices = new List<VertexPositionColor[]>();

    // Vertex arrays
    static VertexPositionColorTexture[] _vertexArray;

    static List<int> _vertsPerVertexBuffer = new List<int>(4);

    #endregion

    #region Render breaks

    internal static List<RenderBreak> RenderBreaks = new List<RenderBreak>();
    static List<RenderBreak> _spriteRenderBreaks = new List<RenderBreak>();
    static List<RenderBreak> _zBufferedSpriteRenderBreaks = new List<RenderBreak>();
    static List<RenderBreak> _textRenderBreaks = new List<RenderBreak>();

    /// <summary>
    /// Tells the renderer to record and keep track of render breaks so they
    /// can be used when optimizing rendering. This value defaults to false
    /// </summary>
    public static bool RecordRenderBreaks
    {
        get { return _recordRenderBreaks; }
        set
        {
            _recordRenderBreaks = value;

            if (_recordRenderBreaks && LastFrameRenderBreakList == null)
            {
                LastFrameRenderBreakList = new List<RenderBreak>();
            }

            if (!_recordRenderBreaks && LastFrameRenderBreakList != null)
            {
                LastFrameRenderBreakList.Clear();
            }
        }
    }
    static bool _recordRenderBreaks;

    /// <summary>
    /// Contains the list of render breaks from the previous frame.
    /// This is updated every time FlatRedBall is drawn.
    /// </summary>
    public static List<RenderBreak> LastFrameRenderBreakList
    {
        get
        {
#if DEBUG
            if (_recordRenderBreaks == false)
            {
                throw new InvalidOperationException($"You must set {nameof(RecordRenderBreaks)} to true before getting LastFrameRenderBreakList");
            }
#endif
            return _lastFrameRenderBreakList;
        }
        private set { _lastFrameRenderBreakList = value; }
    }
    static List<RenderBreak> _lastFrameRenderBreakList;

    #endregion

    #region Effects and color operations

    public static Effect Effect
    {
        get { return _effect; }
        set
        {
            _effect = value;
            _effectManager.Effect = _effect;
        }
    }
    static Effect _effect;
    static CustomEffectManager _effectManager = new CustomEffectManager();

    public static Effect ExternalEffect
    {
        get { return _externalEffect; }
        set
        {
            _externalEffect = value;
            ExternalEffectManager.Effect = _externalEffect;
        }
    }
    static Effect _externalEffect;
    public static CustomEffectManager ExternalEffectManager { get; } = new CustomEffectManager();

    static BasicEffect _basicEffect;
    static BasicEffect _wireframeEffect;
    static Effect _currentEffect; // This stores the current effect in use

    /// <summary>
    /// Sets the color operation on the graphics device if the set value differs from the current value.
    /// This is public so that IDrawableBatches can set the color operation.
    /// </summary>
    public static ColorOperation ColorOperation
    {
        get
        {
            return _colorOperation;
        }
        set
        {
            if (_colorOperation != value)
            {
                ForceSetColorOperation(value);
            }
        }
    }
    static internal ColorOperation _colorOperation = ColorOperation.Texture;

    public static void ForceSetColorOperation(ColorOperation value)
    {
        _colorOperation = value;

        var technique = _effectManager.GetVertexColorTechniqueFromColorOperation(value);

        if (technique == null)
        {
            string errorString =
                "Could not find a technique for " + value.ToString() +
                ", filter: " + FlatRedBallServices.GraphicsOptions.TextureFilter +
                " in the current shader. If using a custom shader verify that" +
                " this pixel shader technique exists.";
            throw new Exception(errorString);
        }
        else
        {
            if (_currentEffect == null)
            {
                _currentEffect = _effect;
            }

            _currentEffect.CurrentTechnique = technique;
        }
    }

    /// <summary>
    /// When this is enabled texture colors will be translated to linear space before 
    /// any other shader operations are performed. This is useful for games with 
    /// lighting and other special shader effects. If the colors are left in gamma 
    /// space the shader calculations will crush the colors and not look like natural 
    /// lighting. Delinearization must be done by the developer in the last render 
    /// step when rendering to the screen. This technique is called gamma correction.
    /// Disabled by default.
    /// </summary>
    public static bool LinearizeTextures { get; set; }

    #endregion

    #region Sorting and culling

    static List<Sprite> _visibleSprites = new List<Sprite>();
    static List<Text> _visibleTexts = new List<Text>();
    static List<IDrawableBatch> _visibleBatches = new List<IDrawableBatch>();

    /// <summary>
    /// Controls whether the renderer will refresh the sorting of its internal lists in the next draw call.
    /// </summary>
    public static bool UpdateSorting { get { return _updateSorting; } set { _updateSorting = value; } }
    static bool _updateSorting = true;

    public static IComparer<Sprite> SpriteComparer { get { return _spriteComparer; } set { _spriteComparer = value; } }
    static IComparer<Sprite> _spriteComparer;

    public static IComparer<Text> TextComparer { get { return _textComparer; } set { _textComparer = value; } }
    static IComparer<Text> _textComparer;

    public static IComparer<IDrawableBatch> DrawableBatchComparer { get { return _drawableBatchComparer; } set { _drawableBatchComparer = value; } }
    static IComparer<IDrawableBatch> _drawableBatchComparer;

    static List<int> _batchZBreaks = new List<int>(10);

    #endregion

    #region Render targets and postprocessing

    public static Dictionary<int, SurfaceFormat> RenderModeFormats;
    public static SwapChain SwapChain { get; set; }
    public static List<IPostProcess> GlobalPostProcesses { get; private set; } = new List<IPostProcess>();

    #endregion

    #region Debugging

    static int _numberOfSpritesDrawn;
    static int _fillVBListCallsThisFrame;
    static int _renderBreaksAllocatedThisFrame;

    #endregion

    #region Performance

    /// <summary>
    /// Controls whether the renderer will update the drawable batches in the next draw call.
    /// </summary>
    public static bool UpdateDrawableBatches { get { return _updateDrawableBatches; } set { _updateDrawableBatches = value; } }
    static bool _updateDrawableBatches = true;

    #endregion

    #endregion

    #region Constructor and initialization

    static Renderer()
    {
        _vertexArray = new VertexPositionColorTexture[6000];
        SetNumberOfThreadsToUse(1);
    }

    internal static void Initialize(IGraphicsDeviceService graphics)
    {
        // Make sure the device isn't null
        if (graphics.GraphicsDevice == null)
        {
            throw new NullReferenceException("The GraphicsDevice is null. Are you calling FlatRedBallServices.InitializeFlatRedBall from the Game's constructor?  If so, you need to call it in the Initialize or LoadGraphicsContent method.");
        }

        _graphics = graphics;
        InitializeEffect();
        ForceSetBlendOperation();
    }

    static void InitializeEffect()
    {
        // Create render mode formats dictionary
        RenderModeFormats = new Dictionary<int, SurfaceFormat>(10);
        RenderModeFormats.Add((int)RenderMode.Color, SurfaceFormat.Color);
        RenderModeFormats.Add((int)RenderMode.Default, SurfaceFormat.Color);

        // Set the initial viewport
        var viewport = _graphics.GraphicsDevice.Viewport;
        viewport.Width = FlatRedBallServices.ClientWidth;
        viewport.Height = FlatRedBallServices.ClientHeight;
        _graphics.GraphicsDevice.Viewport = viewport;

        // Basic effect
        _basicEffect = new BasicEffect(_graphics.GraphicsDevice);
        _basicEffect.Alpha = 1.0f;
        _basicEffect.AmbientLightColor = new Vector3(1f, 1f, 1f);
        _basicEffect.World = Matrix.Identity;

        _wireframeEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice);

        BlendOperation = BlendOperation.Regular;

        var depthStencilState = new DepthStencilState();
        depthStencilState.DepthBufferEnable = false;
        depthStencilState.DepthBufferWriteEnable = false;

        _graphics.GraphicsDevice.DepthStencilState = depthStencilState;

        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
    }

    #endregion

    #region Main drawing

    public static void Draw()
    {
        Draw(null);
    }

    // This is public for those who want to have more control over how FRB renders.
    public static void Draw(Section section)
    {
        PostProcessLogic.DrawWithPostProcessing(
            GlobalPostProcesses,
            SwapChain,
            () => DrawInternal(section));
    }

    static void DrawInternal(Section section)
    {
        if (section != null)
        {
            Section.GetAndStartContextAndTime("Start of Renderer.Draw");
        }

        IsInRendering = true;

        // Drawing should only occur if the window actually has pixels.
        // Using ClientBounds causes memory to be allocated. We can just
        // use the FlatRedBallService's value which gets updated whenever
        // the resolution changes.
        if (FlatRedBallServices.mClientWidth == 0 ||
            FlatRedBallServices.mClientHeight == 0)
        {
            IsInRendering = false;
            return;
        }

#if DEBUG
        if (SpriteManager.Cameras.Count <= 0)
        {
            NullReferenceException exception = new NullReferenceException(
                "There are no cameras to render, did you forget to add a camera to the SpriteManager?");
            throw exception;
        }
        if (_graphics == null || _graphics.GraphicsDevice == null)
        {
            NullReferenceException exception = new NullReferenceException(
                "Renderer's GraphicsDeviceManager is null.  Did you forget to call FlatRedBallServices.Initialize?");
            throw exception;
        }
#endif

        #region Reset the debugging and profiling information

        _fillVBListCallsThisFrame = 0;
        _renderBreaksAllocatedThisFrame = 0;
        if (_lastFrameRenderBreakList != null)
        {
            _lastFrameRenderBreakList.Clear();
        }

        _numberOfSpritesDrawn = 0;

        #endregion


        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Render Cameras");
        }

        #region Loop through all cameras (viewports)

        // Note: It may be more efficient to do this loop at each point
        // there is a camera reference to avoid passing geometry multiple times.
        // Addition: The noted idea may not be faster due to render target swapping.

        for (int i = 0; i < SpriteManager.Cameras.Count; i++)
        {
            var camera = SpriteManager.Cameras[i];

            if(camera.DrawsWorld == false && camera.DrawsCameraLayer == false)
            {
                continue;
            }

            lock (_graphics.GraphicsDevice)
            {
                if (section != null)
                {
                    string cameraName = camera.Name;
                    if (string.IsNullOrEmpty(cameraName))
                    {
                        cameraName = "at index " + i;
                    }
                    Section.GetAndStartContextAndTime("Render camera " + cameraName);
                }

                DrawCamera(camera, section, RenderMode.Default);
                //DrawCamera(camera, section, RenderMode.Default);

                if (section != null)
                {
                    Section.EndContextAndTime();
                }
            }
        }

        #endregion

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("End of Render");
        }

        IsInRendering = false;

        Screens.ScreenManager.Draw();

        if (section != null)
        {
            Section.EndContextAndTime();
        }
    }

    #endregion

    #region Camera and layers drawing

    public static void DrawCamera(Camera camera, Section section = null, RenderMode renderMode = RenderMode.Default)
    {

        PostProcessLogic.DrawWithPostProcessing(
            camera.PostProcesses, camera.SwapChain,
            () => DrawCameraInternal(camera, section, RenderMode.Default));
    }

    static void DrawCameraInternal(Camera camera, Section section, RenderMode renderMode)
    { 
        if (section != null)
        {
            Section.GetAndStartContextAndTime("Start of camera draw");
        }

        PrepareForDrawScene(camera, renderMode);

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Draw UnderAllLayer");
        }

        if (camera.DrawsWorld && !SpriteManager.UnderAllDrawnLayer.IsEmpty)
        {
            var layer = SpriteManager.UnderAllDrawnLayer;
            RenderTarget2D lastRenderTarget = null;
            var lastViewport = GraphicsDevice.Viewport;
            DrawIndividualLayer(camera, RenderMode.Default, layer, section, ref lastRenderTarget, ref lastViewport);

            if (lastRenderTarget != null)
            {
                GraphicsDevice.SetRenderTarget(null);
            }
        }

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Draw Unlayered");
        }

        DrawUnlayeredObjects(camera, renderMode, section);

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Draw Regular Layers");
        }

        // Draw layers. This method will check internally for
        // the camera's DrawsWorld and DrawsCameraLayers properties.
        DrawLayers(camera, renderMode, section);

        if (section != null)
        {
            Section.EndContextAndTime();
        }
    }

    static void PrepareForDrawScene(Camera camera, RenderMode renderMode)
    {
        _currentRenderMode = renderMode;

        // Set the viewport for the current camera
        var viewport = camera.GetViewport();

        _graphics.GraphicsDevice.Viewport = viewport;

        #region Clear the viewport

        if (renderMode == RenderMode.Default || renderMode == RenderMode.Color)
        {
            // Vic says:  This code used to be:
            // if (!mUseRenderTargets && camera.BackgroundColor.A == 0)
            // Why prevent color clearing only when we aren't using render targets?  Don't know, 
            // so I changed this in June 

            // UPDATE:
            // It seems that removing the !mUseRenderTargets just makes the background purple...no change
            // happens in tems of things being able to be drawn.  Not sure why, but I'll update the docs to
            // indicate that you can't use RenderTargets and have stuff drawn before FRB

            if (camera.BackgroundColor.A == 0)
            {
                if (camera.ClearsDepthBuffer)
                {
                    // Clearing to a transparent color, so just clear depth
                    _graphics.GraphicsDevice.Clear(ClearOptions.DepthBuffer, camera.BackgroundColor, 1, 0);
                }
            }
            else
            {
                if (camera.ClearsDepthBuffer)
                {
                    _graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, camera.BackgroundColor, 1, 0);
                }
                else
                {
                    _graphics.GraphicsDevice.Clear(ClearOptions.Target, camera.BackgroundColor, 1, 0);
                }
            }
        }
        else if (renderMode == RenderMode.Depth)
        {
            if (camera.ClearsDepthBuffer)
            {
                _graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1, 0);
            }
        }
        else
        {
            if (camera.ClearsDepthBuffer)
            {
                Color colorToClearTo = Color.Transparent;

                _graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, colorToClearTo, 1, 0);
            }
        }

        #endregion

        #region Set device settings for rendering

        // Let's force texture address mode just in case someone screwed
        // with it outside of the rendering, like when using render states.
        ForceSetTextureAddressMode(TextureAddressMode.Clamp);

        #endregion

        #region Set camera values on the current effect

        _currentEffect = _effect;
        camera.SetDeviceViewAndProjection(_currentEffect, false);

        #endregion
    }

    static void DrawIndividualLayer(Camera camera, RenderMode renderMode, Layer layer, Section section,
        ref RenderTarget2D lastRenderTarget, ref Viewport lastViewport)
    {
        bool hasLayerModifiedCamera = false;

        if (layer.Visible)
        {
            CurrentLayer = layer;

            if (section != null)
            {
                string layerName = "No Layer";

                if (layer != null)
                {
                    layerName = layer.Name;
                }

                Section.GetAndStartContextAndTime("Layer: " + layerName);
            }

            bool didSetRenderTarget = layer.RenderTarget != lastRenderTarget;

            if (didSetRenderTarget)
            {
                lastRenderTarget = layer.RenderTarget;

                if (layer.RenderTarget != null)
                {
                    lastViewport = GraphicsDevice.Viewport;
                }

                GraphicsDevice.SetRenderTarget(layer.RenderTarget);

                if (layer.RenderTarget == null)
                {
                    GraphicsDevice.Viewport = lastViewport;
                }

                if (layer.RenderTarget != null)
                {
                    _graphics.GraphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 1, 0);
                }
            }

            // No need to clear depth buffer if it's a render target
            if (!didSetRenderTarget)
            {
                ClearBackgroundForLayer(camera);
            }

            #region Set view and projection

            // Store the camera's FieldOfView in the oldFieldOfView and set the
            // camera's FieldOfView to the layer's OverridingFieldOfView if necessary.
            _oldCameraLayerSettings.SetFromCamera(camera);

            var oldPosition = camera.Position;
            var oldUpVector = camera.UpVector;

            if (layer.LayerCameraSettings != null)
            {
                layer.LayerCameraSettings.ApplyValuesToCamera(camera, SetCameraOptions.PerformZRotation, null, layer.RenderTarget);
                hasLayerModifiedCamera = true;
            }

            camera.SetDeviceViewAndProjection(_currentEffect, layer.RelativeToCamera);

            #endregion

            if (renderMode == RenderMode.Default)
            {
                if (layer.mZBufferedSprites.Count > 0)
                {
                    DrawZBufferedSprites(camera, layer.mZBufferedSprites);
                }

                // Draw the camera's layer
                DrawMixed(layer.mSprites, layer.mSortType,
                    layer.mTexts, layer.mBatches, layer.RelativeToCamera, camera, section);

                // Draw shapes
                DrawShapes(camera,
                    layer.mSpheres,
                    layer.mCubes,
                    layer.mRectangles,
                    layer.mCircles,
                    layer.mPolygons,
                    layer.mLines,
                    layer.mCapsule2Ds,
                    layer);
            }

            // Set the camera's field of view back.
            // Vic asks: What if the user wants to have a wacky field of view?
            // Does that mean that this will regulate it on layers? This is something
            // that may need to be fixed in the future, but it seems rare and will bloat
            // the visible property list considerably. Let's leave it like this for now
            // to establish a pattern then if the time comes to change this we'll be comfortable
            // with the overriding field of view pattern so a better decision can be made.
            if (hasLayerModifiedCamera)
            {
                // Use the render target here, because it may not have been unset yet.
                _oldCameraLayerSettings.ApplyValuesToCamera(camera, SetCameraOptions.ApplyMatrix, layer.LayerCameraSettings, layer.RenderTarget);
                camera.Position = oldPosition;
                camera.UpVector = oldUpVector;
            }

            if (section != null)
            {
                Section.EndContextAndTime();
            }
        }
    }

    static void DrawUnlayeredObjects(Camera camera, RenderMode renderMode, Section section)
    {
        CurrentLayer = null;

        if (section != null)
        {
            Section.GetAndStartContextAndTime("Draw above shapes");
        }

        #region Draw shapes if UnderEverything

        if (camera.DrawsWorld && renderMode == RenderMode.Default && camera.DrawsShapes &&
            ShapeManager.ShapeDrawingOrder == ShapeDrawingOrder.UnderEverything)
        {
            DrawShapes(
                camera,
                ShapeManager.mSpheres,
                ShapeManager.mCubes,
                ShapeManager.mRectangles,
                ShapeManager.mCircles,
                ShapeManager.mPolygons,
                ShapeManager.mLines,
                ShapeManager.mCapsule2Ds,
                null
                );
        }

        #endregion

        #region Draw Z-buffered sprites and mixed

        // Only draw the rest if in default rendering mode
        if (renderMode == RenderMode.Default)
        {
            if (camera.DrawsWorld)
            {
                if (section != null)
                {
                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("Draw Z Buffered Sprites");
                }

                if (SpriteManager.ZBufferedSpritesWriteable.Count != 0)
                {
                    // Draw the Z-buffered sprites
                    DrawZBufferedSprites(camera, SpriteManager.ZBufferedSpritesWriteable);
                }

                foreach (var drawableBatch in SpriteManager.mZBufferedDrawableBatches)
                {
                    drawableBatch.Draw(camera);
                }

                if (section != null)
                {
                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("Draw Ordered objects");
                }

                // Draw the OrderedByDistanceFromCamera objects (Sprites, Texts, DrawableBatches)
                DrawOrderedByDistanceFromCamera(camera, section);
            }
        }

        #endregion

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Draw below shapes");
        }

        #region Draw shapes if OverEverything

        if (camera.DrawsWorld &&
            renderMode == RenderMode.Default &&
            camera.DrawsShapes &&
            ShapeManager.ShapeDrawingOrder == ShapeDrawingOrder.OverEverything)
        {
            DrawShapes(
                camera,
                ShapeManager.mSpheres,
                ShapeManager.mCubes,
                ShapeManager.mRectangles,
                ShapeManager.mCircles,
                ShapeManager.mPolygons,
                ShapeManager.mLines,
                ShapeManager.mCapsule2Ds,
                null
                );
        }

        #endregion

        if (section != null)
        {
            Section.EndContextAndTime();
        }
    }

    static void DrawLayers(Camera camera, RenderMode renderMode, Section section)
    {
        RenderTarget2D lastRenderTarget = null;
        var lastViewport = GraphicsDevice.Viewport;

        #region Draw world layers

        if (camera.DrawsWorld)
        {
            // These layers are still considered in the "world" because all cameras can see them
            for (int i = 0; i < SpriteManager.LayersWriteable.Count; i++)
            {
                var layer = SpriteManager.LayersWriteable[i];
                DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget, ref lastViewport);
            }
        }

        #endregion

        #region Draw camera layers

        if (camera.DrawsCameraLayer)
        {
            int layerCount = camera.Layers.Count;

            for (int i = 0; i < layerCount; i++)
            {
                var layer = camera.Layers[i];
                DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget, ref lastViewport);
            }
        }

        #endregion

        #region Last, draw the top layer

        if (camera.DrawsWorld && !SpriteManager.TopLayer.IsEmpty)
        {
            var layer = SpriteManager.TopLayer;
            DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget, ref lastViewport);
        }

        #endregion

        if (lastRenderTarget != null)
        {
            _graphics.GraphicsDevice.SetRenderTarget(null);
        }
    }

    static void DrawMixed(SpriteList spriteListUnfiltered, SortType sortType,
        PositionedObjectList<Text> textListUnfiltered, List<IDrawableBatch> batches,
        bool relativeToCamera, Camera camera, Section section)
    {
        if (section != null)
        {
            Section.GetAndStartContextAndTime("Start of Draw Mixed");
        }

        DrawMixedStart(camera);

        int spriteIndex = 0;
        int textIndex = 0;
        int batchIndex = 0;

        // The sort values can represent different
        // things depending on the sortType argument.
        // They can either represent pure Z values or they
        // can represent distance from the camera (squared).
        // The problem is that a larger Z means closer to the
        // camera, but a larger distance means further from the
        // camera. Therefore, to fix this problem if these values
        // represent distance from camera, they will be multiplied by
        // negative 1.
        var nextSpriteSortValue = new SortValues { PrimarySortValue = float.PositiveInfinity };
        var nextTextSortValue = new SortValues { PrimarySortValue = float.PositiveInfinity };
        var nextBatchSortValue = new SortValues { PrimarySortValue = float.PositiveInfinity };

        if (section != null)
        {
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Sort Lists");
        }

        // Vic asks - why do we sort before identifying visible objects?
        // The reason is because we want to sort the actual lists that are
        // passed in so that the next time sorting is called, the lists are 
        // already sorted (or nearly so), making each subsequent sort faster.
        if (_updateSorting)
        {
            SortAllLists(spriteListUnfiltered, sortType, textListUnfiltered, batches, relativeToCamera, camera);
        }

        _visibleSprites.Clear();
        _visibleTexts.Clear();
        _visibleBatches.Clear();

        if (batches != null)
        {
            for (int i = 0; i < batches.Count; i++)
            {
                var shouldAdd = true;
                var batch = batches[i];

                if (batch is IVisible asIVisible)
                {
                    shouldAdd = asIVisible.AbsoluteVisible;
                }
                if (shouldAdd)
                {
                    _visibleBatches.Add(batch);
                }
            }
        }

        for (int i = 0; i < spriteListUnfiltered.Count; i++)
        {
            var sprite = spriteListUnfiltered[i];

            bool isVisible = sprite.AbsoluteVisible &&
                (sprite.ColorOperation == ColorOperation.InterpolateColor || sprite.Alpha > .0001) &&
                camera.IsSpriteInView(sprite, relativeToCamera);

            if (isVisible)
            {
                _visibleSprites.Add(sprite);
            }
        }

        for (int i = 0; i < textListUnfiltered.Count; i++)
        {
            var text = textListUnfiltered[i];

            if (text.AbsoluteVisible && text.Alpha > .0001 && camera.IsTextInView(text, relativeToCamera))
            {
                _visibleTexts.Add(text);
            }
        }

        int indexOfNextSpriteToReposition = 0;

        GetNextZValuesByCategory(_visibleSprites, sortType, _visibleTexts, _visibleBatches, camera, ref spriteIndex, ref textIndex,
            ref nextSpriteSortValue, ref nextTextSortValue, ref nextBatchSortValue);

        int numberToDraw = 0;

        // This is used as a temporary variable for Z or distance from camera
        var sortingValue = new SortValues();

        Section performDrawingSection = null;
        if (section != null)
        {
            Section.EndContextAndTime();
            performDrawingSection = Section.GetAndStartContextAndTime("Perform Drawing");
        }

        while (spriteIndex < _visibleSprites.Count || textIndex < _visibleTexts.Count ||
            (batchIndex < _visibleBatches.Count))
        {
            #region Only 1 array remains to be drawn so finish it off completely

            #region Draw texts

            if (spriteIndex >= _visibleSprites.Count && (batchIndex >= _visibleBatches.Count) &&
                textIndex < _visibleTexts.Count)
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw Texts");
                }

                if (sortType == SortType.DistanceAlongForwardVector)
                {
                    int temporaryCount = _visibleTexts.Count;

                    for (int i = textIndex; i < temporaryCount; i++)
                    {
                        _visibleTexts[i].Position = _visibleTexts[i].mOldPosition;
                    }
                }

                // Texts: draw all texts from textIndex to numberOfVisibleTexts - textIndex
                DrawTexts(_visibleTexts, textIndex, _visibleTexts.Count - textIndex, camera, section);
                break;
            }

            #endregion

            #region Draw Sprites

            else if (textIndex >= _visibleTexts.Count && (batchIndex >= _visibleBatches.Count) &&
                spriteIndex < _visibleSprites.Count)
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw Sprites");
                }

                numberToDraw = _visibleSprites.Count - spriteIndex;

                if (sortType == SortType.DistanceAlongForwardVector)
                {
                    int temporaryCount = _visibleSprites.Count;

                    for (int i = indexOfNextSpriteToReposition; i < temporaryCount; i++)
                    {
                        _visibleSprites[i].Position = _visibleSprites[i].mOldPosition;
                        indexOfNextSpriteToReposition++;
                    }
                }

                PrepareSprites(
                    _spriteVertices, _spriteRenderBreaks,
                    _visibleSprites, spriteIndex, numberToDraw
                    );

                DrawSprites(
                    _spriteVertices, _spriteRenderBreaks,
                    _visibleSprites, spriteIndex,
                    numberToDraw, camera);

                break;
            }

            #endregion

            #region Draw drawable batches

            else if (spriteIndex >= _visibleSprites.Count && textIndex >= _visibleTexts.Count &&
                batchIndex < _visibleBatches.Count)
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw IDrawableBatches");
                }

                // Only drawable batches remain so draw them all
                while (batchIndex < _visibleBatches.Count)
                {
                    var batchAtIndex = _visibleBatches[batchIndex];

                    if (_recordRenderBreaks)
                    {
                        // Even though we aren't using a RenderBreak here, we should record a render break
                        // for this batch as it does cause rendering to be interrupted:
                        var renderBreak = new RenderBreak();
#if DEBUG
                        renderBreak.ObjectCausingBreak = batchAtIndex;
#endif
                        renderBreak.LayerName = CurrentLayerName;
                        LastFrameRenderBreakList.Add(renderBreak);
                    }

                    batchAtIndex.Draw(camera);
                    batchIndex++;
                }

                FixRenderStatesAfterBatchDraw();
                break;
            }

            #endregion

            #endregion

            #region More than 1 list remains so find which group of objects to render

            #region Sprites

            else if (nextSpriteSortValue <= nextTextSortValue && nextSpriteSortValue <= nextBatchSortValue && spriteIndex < _visibleSprites.Count)
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw Sprites");
                }

                // The next furthest object is a sprite. Find how many to draw.

                #region Count how many sprites to draw and store it in numberToDraw

                numberToDraw = 0;

                sortingValue.SecondarySortValue = 0;

                if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                {
                    sortingValue.PrimarySortValue = _visibleSprites[spriteIndex + numberToDraw].Position.Z;

                    if (sortType == SortType.ZSecondaryParentY)
                    {
                        sortingValue.SecondarySortValue = -_visibleSprites[spriteIndex + numberToDraw].TopParent.Y;
                    }
                }
                else
                {
                    sortingValue.PrimarySortValue = -(camera.Position - _visibleSprites[spriteIndex + numberToDraw].Position).LengthSquared();
                }

                while (sortingValue <= nextTextSortValue &&
                       sortingValue <= nextBatchSortValue)
                {
                    numberToDraw++;

                    if (spriteIndex + numberToDraw == _visibleSprites.Count)
                    {
                        break;
                    }

                    sortingValue.SecondarySortValue = 0;

                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                    {
                        sortingValue.PrimarySortValue = _visibleSprites[spriteIndex + numberToDraw].Position.Z;

                        if (sortType == SortType.ZSecondaryParentY)
                        {
                            sortingValue.SecondarySortValue = -_visibleSprites[spriteIndex + numberToDraw].TopParent.Y;
                        }
                    }
                    else
                    {
                        sortingValue.PrimarySortValue = -(camera.Position - _visibleSprites[spriteIndex + numberToDraw].Position).LengthSquared();
                    }
                }

                #endregion

                if (sortType == SortType.DistanceAlongForwardVector)
                {
                    for (int i = indexOfNextSpriteToReposition; i < numberToDraw + spriteIndex; i++)
                    {
                        _visibleSprites[i].Position = _visibleSprites[i].mOldPosition;
                        indexOfNextSpriteToReposition++;
                    }
                }

                PrepareSprites(
                    _spriteVertices, _spriteRenderBreaks,
                    _visibleSprites, spriteIndex,
                    numberToDraw);

                DrawSprites(
                    _spriteVertices, _spriteRenderBreaks,
                    _visibleSprites, spriteIndex,
                    numberToDraw, camera);

                // numberToDraw represents a range so increase spriteIndex by that amount
                spriteIndex += numberToDraw;

                if (spriteIndex >= _visibleSprites.Count)
                {
                    nextSpriteSortValue.PrimarySortValue = float.PositiveInfinity;
                }
                else
                {
                    nextSpriteSortValue.SecondarySortValue = 0;

                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                    {
                        nextSpriteSortValue.PrimarySortValue = _visibleSprites[spriteIndex].Position.Z;

                        if (sortType == SortType.ZSecondaryParentY)
                        {
                            nextSpriteSortValue.SecondarySortValue = -_visibleSprites[spriteIndex].TopParent.Y;
                        }
                    }
                    else
                    {
                        nextSpriteSortValue.PrimarySortValue = -(camera.Position - _visibleSprites[spriteIndex].Position).LengthSquared();
                    }
                }
            }

            #endregion

            #region Texts

            else if (nextTextSortValue <= nextSpriteSortValue && nextTextSortValue <= nextBatchSortValue) // Draw texts
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw Texts");
                }

                numberToDraw = 0;

                if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector)
                    sortingValue.PrimarySortValue = _visibleTexts[textIndex + numberToDraw].Position.Z;
                else
                    sortingValue.PrimarySortValue = -(camera.Position - _visibleTexts[textIndex + numberToDraw].Position).LengthSquared();

                while (sortingValue <= nextSpriteSortValue &&
                       sortingValue <= nextBatchSortValue)
                {
                    numberToDraw++;

                    if (textIndex + numberToDraw == _visibleTexts.Count)
                    {
                        break;
                    }

                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector)
                        sortingValue.PrimarySortValue = _visibleTexts[textIndex + numberToDraw].Position.Z;
                    else
                        sortingValue.PrimarySortValue = -(camera.Position - _visibleTexts[textIndex + numberToDraw].Position).LengthSquared();

                }

                if (sortType == SortType.DistanceAlongForwardVector)
                {
                    for (int i = textIndex; i < textIndex + numberToDraw; i++)
                    {
                        _visibleTexts[i].Position = _visibleTexts[i].mOldPosition;
                    }
                }

                DrawTexts(_visibleTexts, textIndex, numberToDraw, camera, section);

                textIndex += numberToDraw;

                if (textIndex == _visibleTexts.Count)
                {
                    nextTextSortValue.PrimarySortValue = float.PositiveInfinity;
                }
                else
                {
                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                        nextTextSortValue.PrimarySortValue = _visibleTexts[textIndex].Position.Z;
                    else
                        nextTextSortValue.PrimarySortValue = -(camera.Position - _visibleTexts[textIndex].Position).LengthSquared();
                }
            }

            #endregion

            #region Batches

            else if (nextBatchSortValue <= nextSpriteSortValue && nextBatchSortValue <= nextTextSortValue)
            {
                if (section != null)
                {
                    if (Section.Context != performDrawingSection)
                    {
                        Section.EndContextAndTime();
                    }

                    Section.GetAndStartMergedContextAndTime("Draw IDrawableBatches");
                }

                while (nextBatchSortValue <= nextSpriteSortValue && nextBatchSortValue <= nextTextSortValue && batchIndex < _visibleBatches.Count)
                {
                    var batchAtIndex = _visibleBatches[batchIndex];

                    if (_recordRenderBreaks)
                    {
                        // Even though we aren't using a RenderBreak here, we should record a render break
                        // for this batch as it does cause rendering to be interrupted:
                        var renderBreak = new RenderBreak();
#if DEBUG
                        renderBreak.ObjectCausingBreak = batchAtIndex;
#endif
                        renderBreak.LayerName = CurrentLayerName;
                        LastFrameRenderBreakList.Add(renderBreak);
                    }

                    batchAtIndex.Draw(camera);
                    batchIndex++;

                    nextBatchSortValue.SecondarySortValue = 0;

                    if (batchIndex == _visibleBatches.Count)
                    {
                        nextBatchSortValue.PrimarySortValue = float.PositiveInfinity;
                    }
                    else
                    {
                        batchAtIndex = _visibleBatches[batchIndex];

                        if (sortType == SortType.Z || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                        {
                            nextBatchSortValue.PrimarySortValue = batchAtIndex.Z;

                            if (sortType == SortType.ZSecondaryParentY)
                            {
                                nextBatchSortValue.SecondarySortValue = -batchAtIndex.Y;
                            }
                        }
                        else if (sortType == SortType.DistanceAlongForwardVector)
                        {
                            var vectorDifference = new Vector3(
                            batchAtIndex.X - camera.X,
                            batchAtIndex.Y - camera.Y,
                            batchAtIndex.Z - camera.Z);

                            float firstDistance;
                            var forwardVector = camera.RotationMatrix.Forward;

                            Vector3.Dot(ref vectorDifference, ref forwardVector, out firstDistance);

                            nextBatchSortValue.PrimarySortValue = -firstDistance;
                        }
                        else
                        {
                            nextBatchSortValue.PrimarySortValue = -(batchAtIndex.Z * batchAtIndex.Z);
                        }
                    }
                }

                FixRenderStatesAfterBatchDraw();
            }

            #endregion

            #endregion
        }

        if (section != null)
        {
            // Hop up a level
            if (Section.Context != performDrawingSection)
            {
                Section.EndContextAndTime();
            }

            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("End of Draw Mixed");
        }

        // Return the position of any objects not drawn
        if (sortType == SortType.DistanceAlongForwardVector)
        {
            for (int i = indexOfNextSpriteToReposition; i < _visibleSprites.Count; i++)
            {
                _visibleSprites[i].Position = _visibleSprites[i].mOldPosition;
            }
        }

        Texture = null;
        TextureOnDevice = null;

        if (section != null)
        {
            Section.EndContextAndTime();
        }
    }

    static void DrawOrderedByDistanceFromCamera(Camera camera, Section section)
    {
        if (SpriteManager.OrderedByDistanceFromCameraSprites.Count != 0 ||
            SpriteManager.WritableDrawableBatchesList.Count != 0 ||
            TextManager.mDrawnTexts.Count != 0)
        {
            CurrentLayer = null;

            DrawMixed(
                SpriteManager.OrderedByDistanceFromCameraSprites,
                SpriteManager.OrderedSortType, TextManager.mDrawnTexts,
                SpriteManager.WritableDrawableBatchesList,
                false, camera, section);
        }
    }

    static void ClearBackgroundForLayer(Camera camera)
    {
        if (camera.ClearsDepthBuffer)
        {
            var clearColor = Color.Transparent;
            _graphics.GraphicsDevice.Clear(ClearOptions.DepthBuffer, clearColor, 1, 0);
        }
    }

    static void FixRenderStatesAfterBatchDraw()
    {
        FlatRedBallServices.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        FlatRedBallServices.GraphicsOptions.TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
        ForceSetBlendOperation();
        _currentEffect = _effect;
    }

    #endregion

    #region  Drawing helpers

    static void DrawMixedStart(Camera camera)
    {
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (camera.ClearsDepthBuffer)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }
    }

    static void PrepareSprites(
        List<VertexPositionColorTexture[]> spriteVertices,
        List<RenderBreak> renderBreaks,
        IList<Sprite> spritesToDraw, int startIndex, int numberOfVisible)
    {
        // Make sure there are enough vertex arrays
        int numberOfVertexArrays = 1 + (numberOfVisible / 1000);
        while (spriteVertices.Count < numberOfVertexArrays)
        {
            spriteVertices.Add(new VertexPositionColorTexture[6000]);
        }

        // Clear the render breaks
        renderBreaks.Clear();

        FillVertexList(spritesToDraw, spriteVertices,
            renderBreaks, startIndex, numberOfVisible);

        _renderBreaksAllocatedThisFrame += renderBreaks.Count;

        if (_recordRenderBreaks)
        {
            LastFrameRenderBreakList.AddRange(renderBreaks);
        }
    }

    static int DrawSprites(
        List<VertexPositionColorTexture[]> spriteVertices,
        List<RenderBreak> renderBreaks,
        IList<Sprite> spritesToDraw, int startIndex, int numberOfVisibleSprites, Camera camera)
    {
        // Prepare device settings

        // TODO: turn off cull mode
        var oldTextureAddressMode = TextureAddressMode;
        if (spritesToDraw.Count > 0)
            TextureAddressMode = spritesToDraw[0].TextureAddressMode;

        // numberToRender * 2 represents how many triangles. Therefore we only want to use the number of visible Sprites.
        DrawVertexList<VertexPositionColorTexture>(camera, spriteVertices, renderBreaks,
            numberOfVisibleSprites * 2, PrimitiveType.TriangleList, 6000);

        TextureAddressMode = oldTextureAddressMode;

        return numberOfVisibleSprites;
    }

    static void DrawTexts(List<Text> texts, int startIndex, int numToDraw, Camera camera, Section section)
    {
        if (TextManager.UseNativeTextRendering)
        {
            TextManager.DrawTexts(texts, startIndex, numToDraw, camera);
        }
        else
        {
            #region Bitmap font rendering

            int totalVertices = 0;

            for (int i = startIndex; i < startIndex + numToDraw; i++)
            {
                if (!texts[i].RenderOnTexture)
                    totalVertices += texts[i].VertexCount;
            }

            if (totalVertices == 0)
            {
                return;
            }

            var oldTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;

            if (TextManager.FilterTexts == false)
            {
                FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter.Point;
            }

            int numberOfVertexBuffers = 1 + (totalVertices / 6000);

            _textRenderBreaks.Clear();

            // If there are not enough vertex buffers to hold all of the vertices for drawing the texts, add more.
            while (_textVertices.Count < numberOfVertexBuffers)
            {
                _textVertices.Add(new VertexPositionColorTexture[6000]);
            }

            int numberToRender = FillVertexList(texts, _textVertices, camera, _textRenderBreaks, startIndex, numToDraw, totalVertices);
            _renderBreaksAllocatedThisFrame += _textRenderBreaks.Count;

            if (_recordRenderBreaks)
            {
                LastFrameRenderBreakList.AddRange(_textRenderBreaks);
            }

            DrawVertexList<VertexPositionColorTexture>(camera, _textVertices, _textRenderBreaks,
                totalVertices / 3, PrimitiveType.TriangleList, 6000);

            FlatRedBallServices.GraphicsOptions.TextureFilter = oldTextureFilter;

            #endregion
        }
    }

    static void DrawShapes(Camera camera,
        PositionedObjectList<Sphere> spheres,
        PositionedObjectList<AxisAlignedCube> cubes,
        PositionedObjectList<AxisAlignedRectangle> rectangles,
        PositionedObjectList<Circle> circles,
        PositionedObjectList<Polygon> polygons,
        PositionedObjectList<Line> lines,
        PositionedObjectList<Capsule2D> capsule2Ds,
        Layer layer)
    {
        _vertsPerVertexBuffer.Clear();

        var oldTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;

        // August 28, 2023 - Why do we use linear filtering for polygons?
        // This won't make the lines anti-aliased. It just blends the
        // textels. This doesn't seem necessary, and is just a waste of performance.
        //FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter.Linear;

        if (layer == null)
        {
            // Reset the camera as it may have been set differently by layers
            camera.SetDeviceViewAndProjection(_basicEffect, false);
            camera.SetDeviceViewAndProjection(_effect, false);
        }
        else
        {
            camera.SetDeviceViewAndProjection(_basicEffect, layer.RelativeToCamera);
            camera.SetDeviceViewAndProjection(_effect, layer.RelativeToCamera);
        }

        ForceSetColorOperation(ColorOperation.Color);

        #region Count the number of vertices needed to draw the various shapes

        const int numberOfSphereSlices = 4;
        const int numberOfSphereVertsPerSlice = 17;
        int verticesToDraw = 0;

        // Rectangles require 5 points since the last is repeated
        for (int i = 0; i < rectangles.Count; i++)
        {
            if (rectangles[i].AbsoluteVisible)
            {
                verticesToDraw += 5;
            }
        }

        // Add all the vertices for circles
        for (int i = 0; i < circles.Count; i++)
        {
            if (circles[i].AbsoluteVisible)
            {
                verticesToDraw += ShapeManager.NumberOfVerticesForCircles;
            }
        }

        // Add all the vertices for the polygons
        verticesToDraw += ShapeManager.GetTotalPolygonVertexCount(polygons);

        // Add all the vertices for the lines
        verticesToDraw += lines.Count * 2;

        // Add all the vertices for the Capsule2D's
        verticesToDraw += capsule2Ds.Count * ShapeManager.NumberOfVerticesForCapsule2Ds;

        // Add the vertices for AxisAlignedCubes
        verticesToDraw += cubes.Count * 16; // 16 points needed to draw a cube

        verticesToDraw += spheres.Count * numberOfSphereSlices * numberOfSphereVertsPerSlice;

        #endregion

        // If nothing is being drawn, exit the function now
        if (verticesToDraw != 0)
        {
            #region Make sure that there are enough vertex buffers created to hold how many vertices are needed

            // Each vertex buffer holds 6000 vertices. This is common throughout FRB rendering.
            int numberOfVertexBuffers = 1 + (verticesToDraw / 6000);

            while (_shapeVertices.Count < numberOfVertexBuffers)
            {
                _shapeVertices.Add(new VertexPositionColor[6000]);
            }

            #endregion

            int vertexBufferNum = 0;
            int vertNum = 0;

            RenderBreaks.Clear();

            var renderBreak = new RenderBreak(0, null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
            renderBreak.ObjectCausingBreak = "ShapeManager";
#endif
            RenderBreaks.Add(renderBreak);

            int renderBreakNumber = 0;

            int verticesLeftToDraw = verticesToDraw;

            #region Fill the vertex array with the rectangle vertices

            for (int i = 0; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];

                if (rectangle.AbsoluteVisible)
                {
                    // Since there are many kinds of shapes, check the vert num before each shape
                    if (vertNum + 5 > 6000)
                    {
                        vertexBufferNum++;
                        verticesLeftToDraw -= (vertNum);
                        _vertsPerVertexBuffer.Add(vertNum);
                        vertNum = 0;
                    }

                    var buffer = _shapeVertices[vertexBufferNum];

                    buffer[vertNum + 0].Position = new Vector3(rectangle.Left, rectangle.Top, rectangle.Z);
                    buffer[vertNum + 0].Color.PackedValue = rectangle.Color.PackedValue;

                    buffer[vertNum + 1].Position = new Vector3(rectangle.Right, rectangle.Top, rectangle.Z);
                    buffer[vertNum + 1].Color.PackedValue = rectangle.Color.PackedValue;

                    buffer[vertNum + 2].Position = new Vector3(rectangle.Right, rectangle.Bottom, rectangle.Z);
                    buffer[vertNum + 2].Color.PackedValue = rectangle.Color.PackedValue;

                    buffer[vertNum + 3].Position = new Vector3(rectangle.Left, rectangle.Bottom, rectangle.Z);
                    buffer[vertNum + 3].Color.PackedValue = rectangle.Color.PackedValue;

                    buffer[vertNum + 4] = buffer[vertNum + 0];

                    vertNum += 5;
                    RenderBreaks.Add(
                            new RenderBreak(6000 * vertexBufferNum + vertNum,
                            null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp));
                    renderBreakNumber++;
                }
            }
            #endregion

            #region Fill the vertex array with the circle vertices

            Circle circle;

            // Loop through all of the circles
            for (int i = 0; i < circles.Count; i++)
            {
                circle = circles[i];

                if (circle.AbsoluteVisible)
                {
                    if (vertNum + ShapeManager.NumberOfVerticesForCircles > 6000)
                    {
                        vertexBufferNum++;
                        verticesLeftToDraw -= (vertNum);
                        _vertsPerVertexBuffer.Add(vertNum);
                        vertNum = 0;
                    }

                    for (int pointNumber = 0; pointNumber < ShapeManager.NumberOfVerticesForCircles; pointNumber++)
                    {
                        float angle = pointNumber * 2 * 3.1415928f / (ShapeManager.NumberOfVerticesForCircles - 1);

                        _shapeVertices[vertexBufferNum][vertNum + pointNumber].Position =
                            new Vector3(
                                circle.Radius * (float)System.Math.Cos(angle) + circle.X,
                                circle.Radius * (float)System.Math.Sin(angle) + circle.Y,
                                circle.Z);

                        _shapeVertices[vertexBufferNum][vertNum + pointNumber].Color.PackedValue = circle.mPremultipliedColor.PackedValue;
                    }

                    vertNum += ShapeManager.NumberOfVerticesForCircles;

                    renderBreak =
                        new RenderBreak(6000 * vertexBufferNum + vertNum,
                        null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                    renderBreak.ObjectCausingBreak = "Circle";
#endif
                    RenderBreaks.Add(renderBreak);
                    renderBreakNumber++;
                }
            }
            #endregion

            #region Fill the vertex array with the Capsule2D vertices

            Capsule2D capsule2D;
            int numberOfVerticesPerHalf = ShapeManager.NumberOfVerticesForCapsule2Ds / 2;

            // This is the distance from the center of the Capsule to the center of the endpoint
            float endPointCenterDistanceX = 0;
            float endPointCenterDistanceY = 0;

            // Loop through all of the Capsule2Ds
            for (int i = 0; i < capsule2Ds.Count; i++)
            {
                if (vertNum + ShapeManager.NumberOfVerticesForCapsule2Ds > 6000)
                {
                    vertexBufferNum++;
                    verticesLeftToDraw -= (vertNum);
                    _vertsPerVertexBuffer.Add(vertNum);
                    vertNum = 0;
                }

                capsule2D = capsule2Ds[i];

                endPointCenterDistanceX = (float)(System.Math.Cos(capsule2D.RotationZ) * (capsule2D.mScale - capsule2D.mEndpointRadius));
                endPointCenterDistanceY = (float)(System.Math.Sin(capsule2D.RotationZ) * (capsule2D.mScale - capsule2D.mEndpointRadius));

                // First draw one half, then the draw the other half
                for (int pointNumber = 0; pointNumber < numberOfVerticesPerHalf; pointNumber++)
                {
                    float angle = capsule2D.RotationZ + -1.5707963f + pointNumber * 3.1415928f / (numberOfVerticesPerHalf - 1);

                    _shapeVertices[vertexBufferNum][vertNum + pointNumber].Position =
                        new Vector3(
                            endPointCenterDistanceX + capsule2D.mEndpointRadius * (float)System.Math.Cos(angle) + capsule2D.X,
                            endPointCenterDistanceY + capsule2D.mEndpointRadius * (float)System.Math.Sin(angle) + capsule2D.Y,
                            capsule2D.Z);

                    _shapeVertices[vertexBufferNum][vertNum + pointNumber].Color.PackedValue = capsule2D.Color.PackedValue;
                }

                vertNum += numberOfVerticesPerHalf;

                for (int pointNumber = 0; pointNumber < numberOfVerticesPerHalf; pointNumber++)
                {
                    float angle = capsule2D.RotationZ + 1.5707963f + pointNumber * 3.1415928f / (numberOfVerticesPerHalf - 1);

                    _shapeVertices[vertexBufferNum][vertNum + pointNumber].Position =
                        new Vector3(
                            -endPointCenterDistanceX + capsule2D.mEndpointRadius * (float)System.Math.Cos(angle) + capsule2D.X,
                            -endPointCenterDistanceY + capsule2D.mEndpointRadius * (float)System.Math.Sin(angle) + capsule2D.Y,
                            capsule2D.Z);

                    _shapeVertices[vertexBufferNum][vertNum + pointNumber].Color.PackedValue = capsule2D.Color.PackedValue;
                }

                vertNum += numberOfVerticesPerHalf;

                _shapeVertices[vertexBufferNum][vertNum] =
                    _shapeVertices[vertexBufferNum][vertNum - 2 * numberOfVerticesPerHalf];

                vertNum++;

                renderBreak =
                    new RenderBreak(6000 * vertexBufferNum + vertNum,
                    null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                renderBreak.ObjectCausingBreak = "Capsule";
#endif
                RenderBreaks.Add(renderBreak);
                renderBreakNumber++;
            }

            #endregion

            #region Fill the vertex array with the polygon vertices

            for (int i = 0; i < polygons.Count; i++)
            {
                var polygon = polygons[i];

                if (polygon.Points != null && polygon.Points.Count > 1)
                {
                    // If this polygon knocks us into the next vertex buffer, then set the data for this one, then move on.
                    if (vertNum + polygon.Points.Count > 6000)
                    {
                        vertexBufferNum++;
                        verticesLeftToDraw -= (vertNum);
                        _vertsPerVertexBuffer.Add(vertNum);
                        vertNum = 0;
                    }

                    polygon.Vertices.CopyTo(_shapeVertices[vertexBufferNum], vertNum);
                    vertNum += polygon.Vertices.Length;

                    renderBreak =
                        new RenderBreak(6000 * vertexBufferNum + vertNum,
                        null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                    renderBreak.ObjectCausingBreak = "Polygon";
#endif
                    RenderBreaks.Add(renderBreak);
                    renderBreakNumber++;
                }
            }

            #endregion

            #region Fill the vertex array with the line vertices

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // If this line knocks us into the next vertex buffer, then set the data for this one, then move on.
                if (vertNum + 2 > 6000)
                {
                    vertexBufferNum++;
                    verticesLeftToDraw -= vertNum;
                    _vertsPerVertexBuffer.Add(vertNum);
                    vertNum = 0;
                }

                // Add the line points
                _shapeVertices[vertexBufferNum][vertNum + 0].Position =
                    line.Position +
                    Vector3.Transform(new Vector3(
                        (float)line.RelativePoint1.X,
                        (float)line.RelativePoint1.Y,
                        (float)line.RelativePoint1.Z),
                        line.RotationMatrix);

                _shapeVertices[vertexBufferNum][vertNum + 0].Color.PackedValue = line.Color.PackedValue;

                _shapeVertices[vertexBufferNum][vertNum + 1].Position =
                    line.Position +
                    Vector3.Transform(new Vector3(
                        (float)line.RelativePoint2.X,
                        (float)line.RelativePoint2.Y,
                        (float)line.RelativePoint2.Z),
                        line.RotationMatrix);

                _shapeVertices[vertexBufferNum][vertNum + 1].Color.PackedValue = line.Color.PackedValue;

                // Increment the vertex number past this line
                vertNum += 2;

                // Add a render break
                renderBreak =
                    new RenderBreak(6000 * vertexBufferNum + vertNum,
                    null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                renderBreak.ObjectCausingBreak = "Line";
#endif
                RenderBreaks.Add(renderBreak);
                renderBreakNumber++;
            }

            #endregion

            #region Fill the vertex array with the AxisAlignedCube pieces

            for (int i = 0; i < cubes.Count; i++)
            {
                var cube = cubes[i];

                const int numberOfPoints = 16;

                if (vertNum + numberOfPoints > 6000)
                {
                    vertexBufferNum++;
                    verticesLeftToDraw -= vertNum;
                    _vertsPerVertexBuffer.Add(vertNum);
                    vertNum = 0;
                }

                // We can do the top/bottom all in one pass
                for (int cubeVertIndex = 0; cubeVertIndex < 16; cubeVertIndex++)
                {
                    _shapeVertices[vertexBufferNum][vertNum + cubeVertIndex].Position = new Vector3(
                        ShapeManager.UnscaledCubePoints[cubeVertIndex].X * cube.mScaleX + cube.Position.X,
                        ShapeManager.UnscaledCubePoints[cubeVertIndex].Y * cube.mScaleY + cube.Position.Y,
                        ShapeManager.UnscaledCubePoints[cubeVertIndex].Z * cube.mScaleZ + cube.Position.Z);

                    _shapeVertices[vertexBufferNum][vertNum + cubeVertIndex].Color = cube.mColor;

                    if (cubeVertIndex == 9 || cubeVertIndex == 11 || cubeVertIndex == 13 || cubeVertIndex == 15)
                    {
                        renderBreak =
                                new RenderBreak(6000 * vertexBufferNum + vertNum + cubeVertIndex + 1,
                                null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                        renderBreak.ObjectCausingBreak = "Cube";
#endif
                        RenderBreaks.Add(renderBreak);
                        renderBreakNumber++;
                    }
                }

                vertNum += numberOfPoints;
            }

            #endregion

            #region Fill the vertex array with the Sphere pieces

            for (int sphereIndex = 0; sphereIndex < spheres.Count; sphereIndex++)
            {
                if (vertNum + numberOfSphereSlices * numberOfSphereVertsPerSlice > 6000)
                {
                    vertexBufferNum++;
                    verticesLeftToDraw -= vertNum;
                    _vertsPerVertexBuffer.Add(vertNum);
                    vertNum = 0;
                }

                var sphere = spheres[sphereIndex];

                for (int sliceNumber = 0; sliceNumber < numberOfSphereSlices; sliceNumber++)
                {
                    float sliceAngle = sliceNumber * 3.1415928f / numberOfSphereSlices;
                    var rotationMatrix = Matrix.CreateRotationY(sliceAngle);

                    for (int pointNumber = 0; pointNumber < numberOfSphereVertsPerSlice; pointNumber++)
                    {
                        float angle = pointNumber * 2 * 3.1415928f / (numberOfSphereVertsPerSlice - 1);

                        var newPosition = new Vector3(
                                sphere.Radius * (float)System.Math.Cos(angle),
                                sphere.Radius * (float)System.Math.Sin(angle), 0);

                        MathFunctions.TransformVector(ref newPosition, ref rotationMatrix);

                        newPosition += sphere.Position;

                        _shapeVertices[vertexBufferNum][vertNum + pointNumber].Position = newPosition;

                        _shapeVertices[vertexBufferNum][vertNum + pointNumber].Color.PackedValue = sphere.Color.PackedValue;
                    }

                    vertNum += numberOfSphereVertsPerSlice;

                    renderBreak = new RenderBreak(6000 * vertexBufferNum + vertNum,
                        null, ColorOperation.Color, BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
                    renderBreak.ObjectCausingBreak = "Sphere";
#endif
                    RenderBreaks.Add(renderBreak);
                }
            }

            #endregion

            int vertexBufferIndex = 0;
            int renderBreakIndexForRendering = 0;

            for (; vertexBufferIndex < _vertsPerVertexBuffer.Count; vertexBufferIndex++)
            {
                DrawVertexList<VertexPositionColor>(camera, _shapeVertices, RenderBreaks,
                    _vertsPerVertexBuffer[vertexBufferIndex], PrimitiveType.LineStrip,
                    6000, vertexBufferIndex, ref renderBreakIndexForRendering);
            }

            DrawVertexList<VertexPositionColor>(camera, _shapeVertices, RenderBreaks,
                verticesLeftToDraw, PrimitiveType.LineStrip,
                6000, vertexBufferIndex, ref renderBreakIndexForRendering);
        }
    }

    static void DrawZBufferedSprites(Camera camera, SpriteList listToRender)
    {
        // A note about how Z-buffered rendering works with alpha.
        // FRB shaders use a clip() function, which prevents a pixel
        // from being processed. In this case the FRB shader clips
        // based off of the Alpha in the Sprite. If the alpha is
        // essentially 0, then the pixel is not rendered and it
        // does not modify the depth buffer.

        if (SpriteManager.ZBufferedSortType == SortType.Texture)
        {
            listToRender.SortTextureInsertion();
        }

        // Set device settings for drawing Z-buffered sprites
        _visibleSprites.Clear();

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (camera.ClearsDepthBuffer)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        // Currently Z-buffered Sprites are all drawn. Performance improvement possible here by culling.

        lock (listToRender)
        {
            for (int i = 0; i < listToRender.Count; i++)
            {
                var sprite = listToRender[i];

                if (sprite.AbsoluteVisible && sprite.Alpha > .0001f)
                {
                    _visibleSprites.Add(sprite);
                }
            }
        }

        // Draw
        PrepareSprites(
            _zBufferedSpriteVertices, _zBufferedSpriteRenderBreaks,
            _visibleSprites, 0, _visibleSprites.Count);

        DrawSprites(
            _zBufferedSpriteVertices, _zBufferedSpriteRenderBreaks,
            _visibleSprites, 0,
            _visibleSprites.Count, camera);
    }

    public static void DrawVertexList<T>(Camera camera, List<T[]> vertexList, List<RenderBreak> renderBreaks,
        int numberOfPrimitives, PrimitiveType primitiveType, int verticesPerVertexBuffer) where T : struct, IVertexType
    {
        int throwAway = 0;
        DrawVertexList(camera, vertexList, renderBreaks, numberOfPrimitives, primitiveType, verticesPerVertexBuffer, 0, ref throwAway);
    }

    public static void DrawVertexList<T>(Camera camera, List<T[]> vertexList, List<RenderBreak> renderBreaks,
        int numberOfPrimitives, PrimitiveType primitiveType, int verticesPerVertexBuffer, int vbIndex, ref int renderBreakIndex) where T : struct, IVertexType
    {
        bool startedOnNonZeroVBIndex = vbIndex != 0;

        if (numberOfPrimitives == 0) return;

        var oldTextureAddressMode = TextureAddressMode;

        #region Get the verticesPerPrimitive and extraVertices according to the PrimitiveType

        int verticesPerPrimitive = 1;

        // Some primitive types, like LineStrip, require 1 extra vertex for the initial point.
        // That is, to draw 3 lines, 4 points are needed. This variable is used for that.
        int extraVertices = 0;

        switch (primitiveType)
        {
            case PrimitiveType.TriangleList:
                verticesPerPrimitive = 3;
                break;
            case PrimitiveType.LineStrip:
                verticesPerPrimitive = 1;
                extraVertices = 1;
                break;
        }

        #endregion

        int drawToOnThisVB = 0;
        int drawnOnThisVB = 0;
        int totalDrawn = 0;
        int VBOn = vbIndex;
        int numPasses = 0;
        int numberOfPrimitivesPerVertexBuffer = verticesPerVertexBuffer / verticesPerPrimitive;
        var effectToUse = _currentEffect;

        if (primitiveType == PrimitiveType.LineStrip)
        {
            _basicEffect.LightingEnabled = false;
            _basicEffect.VertexColorEnabled = true;
            effectToUse = _basicEffect;
        }

        if (renderBreaks.Count != 0)
        {
            renderBreaks[renderBreakIndex].SetStates();
            renderBreakIndex++;
        }

        #region Move through all of the VBs and draw them

        while (totalDrawn < numberOfPrimitives)
        {
            numPasses++;

            if (startedOnNonZeroVBIndex)
            {
                drawToOnThisVB = System.Math.Min(numberOfPrimitivesPerVertexBuffer, (numberOfPrimitives));
            }
            else
            {
                drawToOnThisVB = System.Math.Min(numberOfPrimitivesPerVertexBuffer, (numberOfPrimitives - (numberOfPrimitivesPerVertexBuffer * VBOn)));
            }

            if (drawToOnThisVB < 0)
            {
                drawToOnThisVB = numberOfPrimitives;
            }

            if (renderBreakIndex < renderBreaks.Count && renderBreaks[renderBreakIndex].ItemNumber < numberOfPrimitivesPerVertexBuffer * VBOn + drawToOnThisVB)
            {
                drawToOnThisVB = renderBreaks[renderBreakIndex].ItemNumber - (numberOfPrimitivesPerVertexBuffer * VBOn);
                var currentTechnique = effectToUse.CurrentTechnique;

                foreach (EffectPass pass in currentTechnique.Passes)
                {
                    pass.Apply();

                    if (drawToOnThisVB != drawnOnThisVB)
                    {
                        _graphics.GraphicsDevice.DrawUserPrimitives<T>(
                            primitiveType,
                            vertexList[VBOn],
                            verticesPerPrimitive * drawnOnThisVB,
                            drawToOnThisVB - drawnOnThisVB - extraVertices,
                            vertexList[VBOn][0].VertexDeclaration);
                    }
                }

                renderBreaks[renderBreakIndex - 1].Cleanup();
                renderBreaks[renderBreakIndex].SetStates();

                renderBreakIndex++;
            }
            else
            {
                var currentTechnique = effectToUse.CurrentTechnique;

                foreach (EffectPass pass in currentTechnique.Passes)
                {
                    pass.Apply();

                    if (drawToOnThisVB - extraVertices != drawnOnThisVB)
                    {
                        try
                        {
                            _graphics.GraphicsDevice.DrawUserPrimitives<T>(
                                primitiveType,
                                vertexList[VBOn],
                                verticesPerPrimitive * drawnOnThisVB,
                                drawToOnThisVB - drawnOnThisVB - extraVertices);

                        }
                        catch (Exception e)
                        {
                            int m = 3;
                        }
                    }
                }
                renderBreaks[renderBreakIndex - 1].Cleanup();
            }

            totalDrawn += drawToOnThisVB - drawnOnThisVB;
            drawnOnThisVB = drawToOnThisVB;

            if (drawToOnThisVB == numberOfPrimitivesPerVertexBuffer && totalDrawn < numberOfPrimitives)
            {
                VBOn++;
                drawnOnThisVB = 0;
            }
        }

        if (TextureAddressMode != oldTextureAddressMode)
            TextureAddressMode = oldTextureAddressMode;

        #endregion
    }

    #endregion

    #region Vertex buffer helpers

    internal static void SetNumberOfThreadsToUse(int numberOfUpdaters)
    {
        _fillVertexLogics.Clear();

        for (int i = 0; i < numberOfUpdaters; i++)
        {
            var logic = new FillVertexLogic();
            _fillVertexLogics.Add(logic);
        }
    }

    static void FillVertexList(IList<Sprite> sprites,
        List<VertexPositionColorTexture[]> vertexLists,
        List<RenderBreak> renderBreaks, int firstSprite,
        int numberToDraw)
    {
        _fillVBListCallsThisFrame++;

        // If the array is empty, then we just exit.
        if (sprites.Count == 0) return;

        // Clear the places where batching breaks occur.
        renderBreaks.Clear();

        int vertexBufferNum = 0;
        int vertNumForRenderBreaks = 0;
        int vertexBufferNumForRenderBreaks = 0;
        var arrayAtIndex = vertexLists[vertexBufferNum];
        var renderBreak = new RenderBreak(firstSprite, sprites[firstSprite]);

        renderBreaks.Add(renderBreak);

        int renderBreakNumber = 0;
        bool addedNewVertexBuffer = false;

        for (int i = firstSprite; i < firstSprite + numberToDraw; i++)
        {
            var sprite = sprites[i];

            #region If the sprite is different from the last render break, break the batch

            if (renderBreaks[renderBreakNumber].DiffersFrom(sprite) || addedNewVertexBuffer)
            {
                renderBreak = new RenderBreak(2000 * vertexBufferNumForRenderBreaks + vertNumForRenderBreaks / 3, sprite);

                // Mark where the break occurred
                renderBreaks.Add(renderBreak);
                renderBreakNumber++;
            }

            #endregion

            vertNumForRenderBreaks += 6;

            if (vertNumForRenderBreaks == 6000)
            {
                vertexBufferNumForRenderBreaks++;
                vertNumForRenderBreaks = 0;
                addedNewVertexBuffer = true;
            }
            else
            {
                addedNewVertexBuffer = false;
            }
        }

        float ratio = 1 / ((float)_fillVertexLogics.Count);
        int spriteCount = (int)(ratio * numberToDraw);

        for (int i = 0; i < _fillVertexLogics.Count; i++)
        {
            _fillVertexLogics[i].SpriteList = sprites;
            _fillVertexLogics[i].VertexLists = vertexLists;
            _fillVertexLogics[i].StartIndex = firstSprite + spriteCount * i;
            _fillVertexLogics[i].FirstSpriteInAllSimultaneousLogics = firstSprite;

            if (i == _fillVertexLogics.Count - 1)
            {
                _fillVertexLogics[i].Count = (firstSprite + numberToDraw) - _fillVertexLogics[i].StartIndex;
            }
            else
            {
                _fillVertexLogics[i].Count = spriteCount;
            }
        }

        if (_fillVertexLogics.Count == 1)
        {
            // If there's only 1 vertex logic, no need to make it async, that causes memory allocations.
            // Maybe look at multithread fixes for memory allocations too but...at least let's make the default
            // case not allocate:
            _fillVertexLogics[0].FillVertexListSync(null);
        }
        else
        {
            for (int i = 0; i < _fillVertexLogics.Count; i++)
            {
                _fillVertexLogics[i].FillVertexList();
            }

            for (int i = 0; i < _fillVertexLogics.Count; i++)
            {
                _fillVertexLogics[i].Wait();
            }
        }
    }

    static int FillVBList(List<Text> texts,
        List<DynamicVertexBuffer> vertexBufferList, Camera camera,
        List<RenderBreak> renderBreaks, int firstText,
        int numToDraw, int numberOfVertices)
    {
        #region If the array is empty or if all texts are empty, just exit

        bool toExit = true;

        if (texts.Count != 0)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                var text = texts[i];
                if (text.VertexCount != 0)
                {
                    toExit = false;
                    break;
                }
            }
        }

        if (toExit)
            return 0;

        #endregion

        renderBreaks.Clear();
        int vertexBufferNum = 0;

        // Grab the first vertex buffer
        var vertexBuffer = vertexBufferList[vertexBufferNum];
        int vertNum = 0;

        var renderBreak = new RenderBreak(firstText, texts[firstText].Font.Texture, texts[firstText].ColorOperation,
            BlendOperation.Regular, TextureAddressMode.Clamp);
#if DEBUG
        renderBreak.ObjectCausingBreak = texts[firstText];
#endif
        renderBreaks.Add(renderBreak);

        int renderBreakNumber = 0;

        #region Calculate verticesLeftToRender

        int verticesLeftToRender = 0;

        for (int i = firstText; i < firstText + numToDraw; i++)
        {
            var text = texts[i];

            if (text.AbsoluteVisible)
                verticesLeftToRender += text.VertexCount;
        }

        #endregion

        for (int i = firstText; i < firstText + numToDraw; i++)
        {
            #region If the Text is different from the last RenderBreak, break the batch

            var text = texts[i];

            if (renderBreaks[renderBreakNumber].DiffersFrom(text))
            {
                renderBreak = new RenderBreak(2000 * vertexBufferNum + vertNum / 3,
                    text.Font.Texture, text.ColorOperation, text.BlendOperation, TextureAddressMode.Clamp);
#if DEBUG
                renderBreak.ObjectCausingBreak = text;
#endif
                renderBreaks.Add(renderBreak);
                renderBreakNumber++;
            }

            #endregion

            int verticesLeftOnThisText = text.VertexCount;

            #region If this text will fit on the current vertex buffer, copy over the info

            if (vertNum + verticesLeftOnThisText < 6000)
            {
                for (int textVertex = 0; textVertex < text.VertexCount; textVertex++)
                {
                    _vertexArray[vertNum] = text.VertexArray[textVertex];
                    vertNum++;
                }

                verticesLeftToRender -= text.VertexCount;

                if (vertNum == 6000 &&
                    ((i + 1 < firstText + numToDraw) ||
                     verticesLeftToRender != 0))
                {
#if MONODROID
                    vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(mVertexArray);
#else
                    vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(_vertexArray, 0, vertNum, SetDataOptions.Discard);
#endif
                    vertexBufferNum++;
                    vertNum = 0;
                }
            }

            #endregion

            #region The text won't fit on this vertex buffer, so copy some over so break the text up

            else
            {
                int textVertexIndexOn = 0;

                while (verticesLeftOnThisText > 0)
                {
                    int numberToCopy = System.Math.Min(verticesLeftToRender, 6000 - vertNum);

                    for (int numberCopied = 0; numberCopied < numberToCopy; numberCopied++)
                    {
                        _vertexArray[vertNum] = text.VertexArray[textVertexIndexOn];
                        vertNum++;
                        verticesLeftToRender--;
                        textVertexIndexOn++;
                    }

                    // Now write the verts if there is more to draw after this
                    if (vertNum == 6000 &&
                        ((i + 1 < firstText + numToDraw) ||
                         verticesLeftToRender != 0))
                    {
#if MONODROID
                        vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(mVertexArray);
#else
                        vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(_vertexArray, 0, vertNum, SetDataOptions.Discard);
#endif
                        vertexBufferNum++;
                        vertNum = 0;
                        verticesLeftOnThisText -= 6000;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            #endregion
        }

        GraphicsDevice.SetVertexBuffer(null);

#if MONODROID
        vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(mVertexArray);
#else
        vertexBufferList[vertexBufferNum].SetData<VertexPositionColorTexture>(_vertexArray, 0, vertNum, SetDataOptions.Discard);
#endif

        return vertNum / 3 + vertexBufferNum * 2000;
    }

    static int FillVertexList(IList<Text> texts,
        List<VertexPositionColorTexture[]> vertexLists, Camera camera,
        List<RenderBreak> renderBreaks, int firstText,
        int numToDraw, int numberOfVertices)
    {
        #region If the array is empty or if all texts are empty, just exit

        bool toExit = true;

        if (texts.Count != 0)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                var text = texts[i];
                if (text.AbsoluteVisible && text.VertexCount != 0)
                {
                    toExit = false;
                    break;
                }
            }
        }

        if (toExit)
            return 0;

        #endregion

        renderBreaks.Clear();
        int vertexBufferNum = 0;
        int vertNum = 0;

        var renderBreak = new RenderBreak(firstText, texts[firstText], 0);

        renderBreaks.Add(renderBreak);

        int renderBreakNumber = 0;

        #region Calculate verticesLeftToRender

        int verticesLeftToRender = 0;

        for (int i = firstText; i < firstText + numToDraw; i++)
        {
            var text = texts[i];

            if (text.AbsoluteVisible)
                verticesLeftToRender += text.VertexCount;
        }

        #endregion

        for (int i = firstText; i < firstText + numToDraw; i++)
        {
            var text = texts[i];

            if (text.AbsoluteVisible == false || text.VertexCount == 0)
                continue;

            #region If the Text is different from the last RenderBreak, break the batch

            if (renderBreaks[renderBreakNumber].DiffersFrom(text))
            {
                renderBreak = new RenderBreak(2000 * vertexBufferNum + vertNum / 3, text, 0);
                renderBreaks.Add(renderBreak);
                renderBreakNumber++;
            }

            #endregion

            int verticesLeftOnThisText = text.VertexCount;

            #region If this text will fit on the current vertex buffer, copy over the info

            if (vertNum + verticesLeftOnThisText < 6000)
            {
                Array.Copy(text.VertexArray, 0, vertexLists[vertexBufferNum], vertNum, text.VertexCount);

                AddRenderBreaksForTextureSwitches(text, renderBreaks, ref renderBreakNumber, vertNum / 3, 0, int.MaxValue);

                vertNum += text.VertexCount;

                verticesLeftToRender -= text.VertexCount;

                if (vertNum == 6000 &&
                    ((i + 1 < firstText + numToDraw) ||
                     verticesLeftToRender != 0))
                {
                    vertexBufferNum++;
                    vertNum = 0;
                }
            }

            #endregion

            #region The text won't fit on this vertex buffer, so copy some over so break the text up

            else
            {
                int textVertexIndexOn = 0;

                while (verticesLeftOnThisText > 0)
                {
                    int numberToCopy = System.Math.Min(verticesLeftOnThisText, 6000 - vertNum);

                    int relativeIndexToStartAt = text.VertexCount - verticesLeftOnThisText;

                    AddRenderBreaksForTextureSwitches(text, renderBreaks, ref renderBreakNumber, vertNum / 3,
                        relativeIndexToStartAt / 3, numberToCopy / 3 + relativeIndexToStartAt);

                    // This can be sped up using Array.Copy. Do this sometime.
                    for (int numberCopied = 0; numberCopied < numberToCopy; numberCopied++)
                    {
                        vertexLists[vertexBufferNum][vertNum] = texts[i].VertexArray[textVertexIndexOn];
                        vertNum++;
                        verticesLeftToRender--;
                        textVertexIndexOn++;
                    }

                    // Now write the verts if there is more to draw after this
                    if (vertNum == 6000 &&
                        ((i + 1 < firstText + numToDraw) ||
                         verticesLeftToRender != 0))
                    {
                        vertexBufferNum++;
                        vertNum = 0;
                        verticesLeftOnThisText -= textVertexIndexOn;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            #endregion
        }

        return vertNum / 3 + vertexBufferNum * 2000;
    }

    static void AddRenderBreaksForTextureSwitches(Text text, List<RenderBreak> renderBreaks, ref int renderBreakNumber,
        int startIndex, int minimumTriangleIndex, int maximumTriangleIndex)
    {
        for (int i = 0; i < text.mInternalTextureSwitches.Count; i++)
        {
            Microsoft.Xna.Framework.Point point = text.mInternalTextureSwitches[i];

            if (point.X >= minimumTriangleIndex && point.X < maximumTriangleIndex)
            {
                var renderBreakToAdd = new RenderBreak(startIndex + point.X, text, point.Y);
                renderBreaks.Add(renderBreakToAdd);
                renderBreakNumber++;
            }
        }
    }

    #endregion

    #region Sorting

    static void SortAllLists(SpriteList spriteList, SortType sortType, PositionedObjectList<Text> textList, List<IDrawableBatch> batches, bool relativeToCamera, Camera camera)
    {
        StoreOldPositionsForDistanceAlongForwardVectorSort(spriteList, sortType, textList, batches, camera);

        #region Sort the sprite list and get the number of visible sprites in numberOfVisibleSprites

        if (spriteList != null && spriteList.Count != 0)
        {
            lock (spriteList)
            {
                switch (sortType)
                {
                    case SortType.None:
                        break;

                    case SortType.Z:
                    case SortType.DistanceAlongForwardVector:
                        // Sorting ascending means everything will be drawn back to front. This
                        // is slower but necessary for translucent objects.
                        // Sorting descending means everything will be drawn back to front. This
                        // is faster but will cause problems for translucency.
                        spriteList.SortZInsertionAscending();
                        break;

                    case SortType.DistanceFromCamera:
                        spriteList.SortCameraDistanceInsersionDescending(camera);
                        break;

                    case SortType.ZSecondaryParentY:
                        spriteList.SortZInsertionAscending();
                        spriteList.SortParentYInsertionDescendingOnZBreaks();
                        break;

                    case SortType.CustomComparer:
                        if (_spriteComparer != null)
                        {
                            spriteList.Sort(_spriteComparer);
                        }
                        else
                        {
                            spriteList.SortZInsertionAscending();
                        }
                        break;

                    case SortType.Texture:
                        spriteList.SortTextureInsertion();
                        break;

                    default:
                        break;
                }
            }
        }

        #endregion

        #region Sort the text list

        if (textList != null && textList.Count != 0)
        {
            switch (sortType)
            {
                case SortType.None:
                    break;

                case SortType.Z:
                case SortType.DistanceAlongForwardVector:
                    textList.SortZInsertionAscending();
                    break;

                case SortType.DistanceFromCamera:
                    textList.SortCameraDistanceInsersionDescending(camera);
                    break;

                case SortType.CustomComparer:
                    if (_textComparer != null)
                    {
                        textList.Sort(_textComparer);
                    }
                    else
                    {
                        textList.SortZInsertionAscending();
                    }
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Sort the batches

        if (batches != null && batches.Count != 0)
        {
            switch (sortType)
            {
                case SortType.None:
                    break;

                case SortType.Z:
                    // Z serves as the radius if using SortType.DistanceFromCamera.
                    // If Z represents actual Z or radius, the larger the value the
                    // further away from the camera the object will be.
                    SortBatchesZInsertionAscending(batches);
                    break;

                case SortType.DistanceAlongForwardVector:
                    batches.Sort(new BatchForwardVectorSorter(camera));
                    break;

                case SortType.ZSecondaryParentY:
                    SortBatchesZInsertionAscending(batches);
                    // Even though the sort type is by parent, IDB doesn't have a Parent object,
                    // so we'll just rely on Y. May need to revisit this if it causes problems.
                    SortBatchesYInsertionDescendingOnZBreaks(batches);
                    break;

                case SortType.CustomComparer:
                    if (_drawableBatchComparer != null)
                    {
                        batches.Sort(_drawableBatchComparer);
                    }
                    else
                    {
                        SortBatchesZInsertionAscending(batches);
                    }
                    break;
            }
        }

        #endregion
    }

    static void StoreOldPositionsForDistanceAlongForwardVectorSort(PositionedObjectList<Sprite> spriteList, SortType sortType, PositionedObjectList<Text> textList, List<IDrawableBatch> batches, Camera camera)
    {
        #region If DistanceAlongForwardVector store old values

        // If the objects are using SortType.DistanceAlongForwardVector
        // then store the old positions, then rotate the objects by the matrix that
        // moves the forward vector to the Z = -1 vector (the camera's inverse rotation
        // matrix)
        if (sortType == SortType.DistanceAlongForwardVector)
        {
            var inverseRotationMatrix = camera.RotationMatrix;
            Matrix.Invert(ref inverseRotationMatrix, out inverseRotationMatrix);

            int temporaryCount = spriteList.Count;

            for (int i = 0; i < temporaryCount; i++)
            {
                spriteList[i].mOldPosition = spriteList[i].Position;

                spriteList[i].Position -= camera.Position;
                Vector3.Transform(ref spriteList[i].Position,
                    ref inverseRotationMatrix, out spriteList[i].Position);
            }

            temporaryCount = textList.Count;

            for (int i = 0; i < temporaryCount; i++)
            {
                textList[i].mOldPosition = textList[i].Position;

                textList[i].Position -= camera.Position;
                Vector3.Transform(ref textList[i].Position,
                    ref inverseRotationMatrix, out textList[i].Position);
            }

            temporaryCount = batches.Count;
        }

        #endregion
    }

    static void SortBatchesZInsertionAscending(IList<IDrawableBatch> batches)
    {
        if (batches.Count == 1 || batches.Count == 0)
            return;

        int whereBatchBelongs;

        for (int i = 1; i < batches.Count; i++)
        {
            if (batches[i].Z < batches[i - 1].Z)
            {
                if (i == 1)
                {
                    batches.Insert(0, batches[i]);
                    batches.RemoveAt(i + 1);
                    continue;
                }

                for (whereBatchBelongs = i - 2; whereBatchBelongs > -1; whereBatchBelongs--)
                {
                    if (batches[i].Z >= batches[whereBatchBelongs].Z)
                    {
                        batches.Insert(whereBatchBelongs + 1, batches[i]);
                        batches.RemoveAt(i + 1);
                        break;
                    }
                    else if (whereBatchBelongs == 0 && batches[i].Z < batches[0].Z)
                    {
                        batches.Insert(0, batches[i]);
                        batches.RemoveAt(i + 1);
                        break;
                    }
                }
            }
        }
    }

    static void SortBatchesYInsertionDescendingOnZBreaks(List<IDrawableBatch> batches)
    {
        GetBatchZBreaks(batches, _batchZBreaks);

        _batchZBreaks.Insert(0, 0);
        _batchZBreaks.Add(batches.Count);

        for (int i = 0; i < _batchZBreaks.Count - 1; i++)
        {
            SortBatchInsertionDescending(batches, _batchZBreaks[i], _batchZBreaks[i + 1]);
        }
    }

    static void GetBatchZBreaks(List<IDrawableBatch> batches, List<int> zBreaks)
    {
        zBreaks.Clear();

        if (batches.Count == 0 || batches.Count == 1)
            return;

        for (int i = 1; i < batches.Count; i++)
        {
            if (batches[i].Z != batches[i - 1].Z)
                zBreaks.Add(i);
        }
    }

    static void SortBatchInsertionDescending(List<IDrawableBatch> batches, int firstObject, int lastObjectExclusive)
    {
        int whereObjectBelongs;

        float yAtI;
        float yAtIMinusOne;

        for (int i = firstObject + 1; i < lastObjectExclusive; i++)
        {
            yAtI = batches[i].Y;
            yAtIMinusOne = batches[i - 1].Y;

            if (yAtI > yAtIMinusOne)
            {
                if (i == firstObject + 1)
                {
                    batches.Insert(firstObject, batches[i]);
                    batches.RemoveAt(i + 1);
                    continue;
                }

                for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                {
                    if (yAtI <= (batches[whereObjectBelongs]).Y)
                    {
                        batches.Insert(whereObjectBelongs + 1, batches[i]);
                        batches.RemoveAt(i + 1);
                        break;
                    }
                    else if (whereObjectBelongs == firstObject && yAtI > (batches[firstObject]).Y)
                    {
                        batches.Insert(firstObject, batches[i]);
                        batches.RemoveAt(i + 1);
                        break;
                    }
                }
            }
        }
    }

    static void GetNextZValuesByCategory(List<Sprite> spriteList, SortType sortType, List<Text> textList, List<IDrawableBatch> batches,
        Camera camera, ref int spriteIndex, ref int textIndex, ref SortValues nextSpriteSortValue, ref SortValues nextTextSortValue, ref SortValues nextBatchSortValue)
    {
        #region Find out the initial Z values of the 3 categories of objects to know which to render first

        if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY ||
            // Custom comparers define how objects sort within the category, but outside we just have to rely on Z
            sortType == SortType.CustomComparer)
        {
            lock (spriteList)
            {
                int spriteNumber = 0;

                while (spriteNumber < spriteList.Count)
                {
                    nextSpriteSortValue.PrimarySortValue = spriteList[spriteNumber].Z;
                    nextSpriteSortValue.SecondarySortValue = sortType == SortType.ZSecondaryParentY ? -spriteList[spriteNumber].TopParent.Y : 0;
                    spriteIndex = spriteNumber;
                    break;
                }
            }

            if (textList != null && textList.Count != 0)
                nextTextSortValue.PrimarySortValue = textList[0].Position.Z;
            else if (textList != null)
                textIndex = textList.Count;

            if (batches != null && batches.Count != 0)
            {
                if (sortType == SortType.Z || sortType == SortType.ZSecondaryParentY || sortType == SortType.CustomComparer)
                {
                    // The Z value of the current batch is used. Batches are always visible to this code.
                    nextBatchSortValue.PrimarySortValue = batches[0].Z;

                    // Y value is the sorting value, so use that.
                    nextBatchSortValue.SecondarySortValue = sortType == SortType.ZSecondaryParentY ? -batches[0].Y : 0;
                }
                else
                {
                    var vectorDifference = new Vector3(
                        batches[0].X - camera.X,
                        batches[0].Y - camera.Y,
                        batches[0].Z - camera.Z);

                    float firstDistance;
                    var forwardVector = camera.RotationMatrix.Forward;
                    Vector3.Dot(ref vectorDifference, ref forwardVector, out firstDistance);
                    nextBatchSortValue.PrimarySortValue = -firstDistance;
                }
            }
        }

        else if (sortType == SortType.Texture)
        {
            throw new Exception("Sorting based on texture is not supported on non z-buffered Sprites");
        }

        else if (sortType == SortType.DistanceFromCamera)
        {
            // Code duplication to prevent tight-loop if statements
            lock (spriteList)
            {
                int spriteNumber = 0;
                while (spriteNumber < spriteList.Count)
                {
                    nextSpriteSortValue.PrimarySortValue =
                        -(camera.Position - spriteList[spriteNumber].Position).LengthSquared();
                    spriteIndex = spriteNumber;
                    break;
                }
            }

            if (textList != null && textList.Count != 0)
            {
                nextTextSortValue.PrimarySortValue = -(camera.Position - textList[0].Position).LengthSquared();
            }
            else if (textList != null)
            {
                textIndex = textList.Count;
            }

            // The Z value of the current batch is used. Batches are always visible to this code.
            if (batches != null && batches.Count != 0)
            {
                // Working with squared length, so use that here.
                nextBatchSortValue.PrimarySortValue = -(batches[0].Z * batches[0].Z);
            }
        }

        #endregion
    }

    struct SortValues
    {
        public float PrimarySortValue;
        public float SecondarySortValue;

        public override string ToString()
        {
            return $"{PrimarySortValue} ({SecondarySortValue})";
        }

        // Overloading the '<' operator
        public static bool operator <(SortValues left, SortValues right)
        {
            return left.PrimarySortValue < right.PrimarySortValue ||
                (left.PrimarySortValue == right.PrimarySortValue && left.SecondarySortValue < right.SecondarySortValue);
        }

        public static bool operator >(SortValues left, SortValues right)
        {
            return left.PrimarySortValue > right.PrimarySortValue ||
                (left.PrimarySortValue == right.PrimarySortValue && left.SecondarySortValue > right.SecondarySortValue);
        }

        public static bool operator <=(SortValues left, SortValues right)
        {
            return (left.PrimarySortValue == right.PrimarySortValue && left.SecondarySortValue == right.SecondarySortValue) || left < right;
        }

        public static bool operator >=(SortValues left, SortValues right)
        {
            return (left.PrimarySortValue == right.PrimarySortValue && left.SecondarySortValue == right.SecondarySortValue) || left > right;
        }
    }

    #endregion

    #region Postprocessing

    /// <summary>
    /// Creates a SwapChain instance matching the game's resolution which automatically adjusts when the game window resizes.
    /// </summary>
    public static void CreateDefaultSwapChain()
    {
        SwapChain = new PostProcessing.SwapChain(
            FlatRedBallServices.Game.Window.ClientBounds.Width,
            FlatRedBallServices.Game.Window.ClientBounds.Height);

        FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>
        {
            SwapChain.UpdateRenderTargetSize(
                FlatRedBallServices.Game.Window.ClientBounds.Width,
                FlatRedBallServices.Game.Window.ClientBounds.Height);
        };
    }


    #endregion

    #region Other

    public static void Update()
    {
    }

    internal static void UpdateDependencies()
    {
    }

    public static new String ToString()
    {
        return String.Format(
            "Number of RenderBreaks allocated: %d\nNumber of Sprites drawn: %d",
            _renderBreaksAllocatedThisFrame, _numberOfSpritesDrawn);
    }

    #endregion
}

#region Enums

/// <summary>
/// Rendering modes available in FlatRedBall
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Default rendering mode. Uses embedded effects in models.
    /// </summary>
    Default,
    /// <summary>
    /// Color rendering mode. Renders color values for a model (does not
    /// include lighting information). Effect technique: RenderColor.
    /// </summary>
    Color,
    /// <summary>
    /// Normals rendering mode. Renders normals. Effect technique: RenderNormals.
    /// </summary>
    Normals,
    /// <summary>
    /// Depth rendering mode. Renders depth. Effect technique: RenderDepth.
    /// </summary>
    Depth,
    /// <summary>
    /// Position rendering mode. Renders position. Effect technique: RenderPosition.
    /// </summary>
    Position
}

#endregion

#region FillVertexLogic class

class FillVertexLogic
{
    public IList<Sprite> SpriteList;
    public List<VertexPositionColorTexture[]> VertexLists;
    public int StartIndex;
    public int Count;
    public int FirstSpriteInAllSimultaneousLogics;

    ManualResetEvent _manualResetEvent;

    public FillVertexLogic()
    {
        _manualResetEvent = new ManualResetEvent(false);
    }

    public void Reset()
    {
        _manualResetEvent.Reset();
    }

    public void Wait()
    {
        _manualResetEvent.WaitOne();
    }

    public void FillVertexList()
    {
        Reset();
        ThreadPool.QueueUserWorkItem(FillVertexListSync);
    }

    internal void FillVertexListSync(object notUsed)
    {
        int vertNum = 0;
        int vertexBufferNum = 0;
        int lastIndexExclusive = StartIndex + Count;
        var arrayAtIndex = VertexLists[vertexBufferNum];

        for (int unadjustedI = StartIndex; unadjustedI < lastIndexExclusive; unadjustedI++)
        {
            int i = unadjustedI - FirstSpriteInAllSimultaneousLogics;

            vertNum = (i * 6) % 6000;
            vertexBufferNum = i / 1000;
            arrayAtIndex = VertexLists[vertexBufferNum];

            var spriteAtIndex = SpriteList[unadjustedI];

            #region The Sprite doesn't have stored vertices (default) so we have to create them now

            if (spriteAtIndex.mAutomaticallyUpdated)
            {
                spriteAtIndex.UpdateVertices();

                #region Set the color
#if IOS
                // If the Sprite's Texture is null, it will behave as if it's got its ColorOperation set to Color instead of Texture
                if (spriteAtIndex.ColorOperation == ColorOperation.Texture && spriteAtIndex.Texture != null)
                {
                    // If we are using the texture color, we want to ignore the Sprite's RGB values.
                    // The W component is Alpha, so we'll use full values for the others.
                    var value = (uint)(255 * spriteAtIndex.mVertices[3].Color.W);
                    arrayAtIndex[vertNum + 0].Color.PackedValue =
                        value +
                        (value << 8) +
                        (value << 16) +
                        (value << 24);
                }
                else
                {
                    // If we are using the texture color, we 
                    arrayAtIndex[vertNum + 0].Color.PackedValue =
                        ((uint)(255 * spriteAtIndex.mVertices[3].Color.X)) +
                        (((uint)(255 * spriteAtIndex.mVertices[3].Color.Y)) << 8) +
                        (((uint)(255 * spriteAtIndex.mVertices[3].Color.Z)) << 16) +
                        (((uint)(255 * spriteAtIndex.mVertices[3].Color.W)) << 24);
                }

                arrayAtIndex[vertNum + 1].Color.PackedValue =
                    arrayAtIndex[vertNum + 0].Color.PackedValue;

                arrayAtIndex[vertNum + 2].Color.PackedValue =
                    arrayAtIndex[vertNum + 0].Color.PackedValue;

                arrayAtIndex[vertNum + 5].Color.PackedValue =
                    arrayAtIndex[vertNum + 0].Color.PackedValue;
#else
                arrayAtIndex[vertNum + 0].Color.PackedValue =
                    ((uint)(255 * spriteAtIndex.mVertices[3].Color.X)) +
                    (((uint)(255 * spriteAtIndex.mVertices[3].Color.Y)) << 8) +
                    (((uint)(255 * spriteAtIndex.mVertices[3].Color.Z)) << 16) +
                    (((uint)(255 * spriteAtIndex.mVertices[3].Color.W)) << 24);

                arrayAtIndex[vertNum + 1].Color.PackedValue =
                    ((uint)(255 * spriteAtIndex.mVertices[0].Color.X)) +
                    (((uint)(255 * spriteAtIndex.mVertices[0].Color.Y)) << 8) +
                    (((uint)(255 * spriteAtIndex.mVertices[0].Color.Z)) << 16) +
                    (((uint)(255 * spriteAtIndex.mVertices[0].Color.W)) << 24);

                arrayAtIndex[vertNum + 2].Color.PackedValue =
                    ((uint)(255 * spriteAtIndex.mVertices[1].Color.X)) +
                    (((uint)(255 * spriteAtIndex.mVertices[1].Color.Y)) << 8) +
                    (((uint)(255 * spriteAtIndex.mVertices[1].Color.Z)) << 16) +
                    (((uint)(255 * spriteAtIndex.mVertices[1].Color.W)) << 24);

                arrayAtIndex[vertNum + 5].Color.PackedValue =
                    ((uint)(255 * spriteAtIndex.mVertices[2].Color.X)) +
                    (((uint)(255 * spriteAtIndex.mVertices[2].Color.Y)) << 8) +
                    (((uint)(255 * spriteAtIndex.mVertices[2].Color.Z)) << 16) +
                    (((uint)(255 * spriteAtIndex.mVertices[2].Color.W)) << 24);
#endif
                #endregion

                arrayAtIndex[vertNum + 0].Position = spriteAtIndex.mVertices[3].Position;
                arrayAtIndex[vertNum + 0].TextureCoordinate = spriteAtIndex.mVertices[3].TextureCoordinate;

                arrayAtIndex[vertNum + 1].Position = spriteAtIndex.mVertices[0].Position;
                arrayAtIndex[vertNum + 1].TextureCoordinate = spriteAtIndex.mVertices[0].TextureCoordinate;

                arrayAtIndex[vertNum + 2].Position = spriteAtIndex.mVertices[1].Position;
                arrayAtIndex[vertNum + 2].TextureCoordinate = spriteAtIndex.mVertices[1].TextureCoordinate;

                arrayAtIndex[vertNum + 3] = arrayAtIndex[vertNum + 0];
                arrayAtIndex[vertNum + 4] = arrayAtIndex[vertNum + 2];

                arrayAtIndex[vertNum + 5].Position = spriteAtIndex.mVertices[2].Position;
                arrayAtIndex[vertNum + 5].TextureCoordinate = spriteAtIndex.mVertices[2].TextureCoordinate;

                if (spriteAtIndex.FlipHorizontal)
                {
                    arrayAtIndex[vertNum + 0].TextureCoordinate = arrayAtIndex[vertNum + 5].TextureCoordinate;
                    arrayAtIndex[vertNum + 5].TextureCoordinate = arrayAtIndex[vertNum + 3].TextureCoordinate;
                    arrayAtIndex[vertNum + 3].TextureCoordinate = arrayAtIndex[vertNum + 0].TextureCoordinate;

                    arrayAtIndex[vertNum + 2].TextureCoordinate = arrayAtIndex[vertNum + 1].TextureCoordinate;
                    arrayAtIndex[vertNum + 1].TextureCoordinate = arrayAtIndex[vertNum + 4].TextureCoordinate;
                    arrayAtIndex[vertNum + 4].TextureCoordinate = arrayAtIndex[vertNum + 2].TextureCoordinate;
                }

                if (spriteAtIndex.FlipVertical)
                {
                    arrayAtIndex[vertNum + 0].TextureCoordinate = arrayAtIndex[vertNum + 1].TextureCoordinate;
                    arrayAtIndex[vertNum + 1].TextureCoordinate = arrayAtIndex[vertNum + 3].TextureCoordinate;
                    arrayAtIndex[vertNum + 3].TextureCoordinate = arrayAtIndex[vertNum + 0].TextureCoordinate;

                    arrayAtIndex[vertNum + 2].TextureCoordinate = arrayAtIndex[vertNum + 5].TextureCoordinate;
                    arrayAtIndex[vertNum + 5].TextureCoordinate = arrayAtIndex[vertNum + 4].TextureCoordinate;
                    arrayAtIndex[vertNum + 4].TextureCoordinate = arrayAtIndex[vertNum + 2].TextureCoordinate;
                }
            }

            #endregion

            else
            {
                arrayAtIndex[vertNum + 0] = spriteAtIndex.mVerticesForDrawing[3];
                arrayAtIndex[vertNum + 1] = spriteAtIndex.mVerticesForDrawing[0];
                arrayAtIndex[vertNum + 2] = spriteAtIndex.mVerticesForDrawing[1];
                arrayAtIndex[vertNum + 3] = spriteAtIndex.mVerticesForDrawing[3];
                arrayAtIndex[vertNum + 4] = spriteAtIndex.mVerticesForDrawing[1];
                arrayAtIndex[vertNum + 5] = spriteAtIndex.mVerticesForDrawing[2];
            }
        }

        _manualResetEvent.Set();
    }
}

#endregion
