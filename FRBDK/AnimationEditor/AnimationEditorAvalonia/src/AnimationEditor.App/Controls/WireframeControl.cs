using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using FlatRedBall.Content.AnimationChain;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FilePath = FlatRedBall.IO.FilePath;

namespace AnimationEditor.App.Controls;

/// <summary>
/// Avalonia + SkiaSharp wireframe editor.
/// Replaces ImageRegionSelectionControl + WireframeManager from the WinForms port.
/// <para>
/// Coordinate systems:
///   Texture-space — pixel coords (0,0)→(W,H) inside the loaded bitmap.
///   Screen-space  — pixel coords within the control bounds (origin = top-left of control).
///   Transform: screenX = panX + textureX * zoom
/// </para>
/// </summary>
public class WireframeControl : Control
{
    // ── Inner types ───────────────────────────────────────────────────────────


    private sealed class FrameRect
    {
        public AnimationFrameSave Frame = null!;
        public SKRect Bounds;       // texture-space pixel coords
        public bool IsSelected;
    }

    /// <summary>
    /// Immutable snapshot of all rendering state, captured on the UI thread
    /// so the render thread can read it safely.
    /// </summary>
    private sealed class RenderSnapshot
    {
        public SKBitmap? Bitmap;
        public float PanX, PanY, Zoom;
        public bool ShowGrid;
        public int GridSize;
        public List<(SKRect Bounds, bool IsSelected)> Frames = new();
        public SKRect? SelectedHandleBounds;    // null → no handles drawn
        public bool ShowPreview;
        public SKRect PreviewRect;
        public double Width, Height;
    }

    private sealed class DrawOp : ICustomDrawOperation
    {
        private readonly RenderSnapshot _s;

        public DrawOp(RenderSnapshot s) { _s = s; Bounds = new Rect(0, 0, s.Width, s.Height); }

        public Rect Bounds { get; }
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext ctx)
        {
            var lease = ctx.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
            if (lease is null) return;
            using (lease)
                RenderSk(lease.SkCanvas, _s);
        }

        // ── Static rendering logic ────────────────────────────────────────────

        private static void RenderSk(SKCanvas canvas, RenderSnapshot s)
        {
            canvas.Clear(new SKColor(30, 30, 30));

            if (s.Bitmap != null)
            {
                var dest = new SKRect(
                    s.PanX, s.PanY,
                    s.PanX + s.Bitmap.Width * s.Zoom,
                    s.PanY + s.Bitmap.Height * s.Zoom);

                // Texture image — point sampling when zoomed ≥ 1× for pixel-art fidelity
                using var imgPaint = new SKPaint();
                imgPaint.FilterQuality = s.Zoom >= 1f ? SKFilterQuality.None : SKFilterQuality.Low;
                canvas.DrawBitmap(s.Bitmap, dest, imgPaint);

                // Outline around whole texture
                using var outlinePaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 160),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f
                };
                canvas.DrawRect(dest, outlinePaint);

                // Grid overlay
                if (s.ShowGrid && s.GridSize > 0)
                    DrawGrid(canvas, s, dest);
            }

            // Frame region rectangles
            using var frameFill = new SKPaint { Style = SKPaintStyle.Fill };
            using var frameStroke = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

            foreach (var (bounds, isSelected) in s.Frames)
            {
                var sr = ToScreen(bounds, s);
                if (isSelected)
                {
                    frameFill.Color = new SKColor(80, 160, 255, 45);
                    frameStroke.Color = new SKColor(80, 160, 255, 230);
                }
                else
                {
                    frameFill.Color = new SKColor(80, 160, 255, 18);
                    frameStroke.Color = new SKColor(80, 160, 255, 120);
                }
                canvas.DrawRect(sr, frameFill);
                canvas.DrawRect(sr, frameStroke);
            }

            // Resize handles on selected frame
            if (s.SelectedHandleBounds.HasValue)
                DrawHandles(canvas, ToScreen(s.SelectedHandleBounds.Value, s));

            // Magic-wand / grid-snap preview rectangle
            if (s.ShowPreview)
            {
                using var pvPaint = new SKPaint
                {
                    Color = new SKColor(255, 220, 0, 180),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4f, 3f }, 0f)
                };
                canvas.DrawRect(ToScreen(s.PreviewRect, s), pvPaint);
            }
        }

        private static void DrawGrid(SKCanvas canvas, RenderSnapshot s, SKRect textureDest)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 35),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.5f
            };
            float step = s.GridSize * s.Zoom;

            for (float x = textureDest.Left + step; x < textureDest.Right; x += step)
                canvas.DrawLine(x, textureDest.Top, x, textureDest.Bottom, paint);

            for (float y = textureDest.Top + step; y < textureDest.Bottom; y += step)
                canvas.DrawLine(textureDest.Left, y, textureDest.Right, y, paint);
        }

        private static void DrawHandles(SKCanvas canvas, SKRect sr)
        {
            const float Hs = 5f;
            using var fill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
            using var stroke = new SKPaint { Color = SKColors.DodgerBlue, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

            foreach (var pt in HandlePoints(sr))
            {
                var hr = new SKRect(pt.X - Hs, pt.Y - Hs, pt.X + Hs, pt.Y + Hs);
                canvas.DrawRect(hr, fill);
                canvas.DrawRect(hr, stroke);
            }
        }

        private static IEnumerable<SKPoint> HandlePoints(SKRect r)
        {
            float cx = r.MidX, cy = r.MidY;
            yield return new SKPoint(r.Left, r.Top);
            yield return new SKPoint(cx, r.Top);
            yield return new SKPoint(r.Right, r.Top);
            yield return new SKPoint(r.Left, cy);
            yield return new SKPoint(r.Right, cy);
            yield return new SKPoint(r.Left, r.Bottom);
            yield return new SKPoint(cx, r.Bottom);
            yield return new SKPoint(r.Right, r.Bottom);
        }

        private static SKRect ToScreen(SKRect r, RenderSnapshot s)
        {
            var (l, t, rr, b) = WireframeTransform.TextureRectToScreen(
                r.Left, r.Top, r.Right, r.Bottom, s.PanX, s.PanY, s.Zoom);
            return new SKRect(l, t, rr, b);
        }
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    private SKBitmap? _bitmap;
    private string? _loadedTexturePath;
    private InspectableImage? _inspectableImage;

    private float _zoom = 1f;
    private float _panX, _panY;

    private bool _showGrid;
    private int _gridSize = 16;

    private readonly List<FrameRect> _frameRects = new();

    // Mouse / drag state
    private bool _isPanning;
    private Point _panAnchor;
    private float _panAnchorX, _panAnchorY;

    private FrameRect? _draggingRect;
    private HandleKind _draggingHandle;
    private SKPoint _dragStartWorld;
    private SKRect _dragStartBounds;

    // Preview rectangle (magic wand / grid snap hover)
    private bool _showPreview;
    private SKRect _previewRect;

    // Per-texture saved camera position (texture path → panX, panY, zoom)
    private readonly Dictionary<string, (float px, float py, float z)> _cameraByTexture = new();

    // ── Public properties ─────────────────────────────────────────────────────

    /// <summary>Absolute path of the currently displayed texture, or null.</summary>
    public string? LoadedTexturePath => _loadedTexturePath;

    /// <summary>Pixel dimensions of the loaded bitmap (0×0 when nothing is loaded).</summary>
    public (int Width, int Height) BitmapSize =>
        _bitmap is null ? (0, 0) : (_bitmap.Width, _bitmap.Height);

    /// <summary>Current zoom factor (1.0 = 100 %).</summary>
    public float Zoom => _zoom;

    private bool _isMagicWandMode;

    /// <summary>When true, mouse clicks perform a flood-fill to set/create the frame region.</summary>
    public bool IsMagicWandMode
    {
        get => _isMagicWandMode;
        set
        {
            _isMagicWandMode = value;
            if (value && _bitmap != null)
                _inspectableImage ??= new InspectableImage(_bitmap);
            if (!value)
                _showPreview = false;
            InvalidateVisual();
        }
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired after a frame's UV coords have been updated by dragging a handle.</summary>
    public event Action<AnimationFrameSave>? FrameRegionChanged;

    /// <summary>
    /// Fired when the user ctrl+clicks to add a new frame
    /// (minX, minY, maxX, maxY in texture pixel coords).
    /// </summary>
    public event Action<int, int, int, int>? FrameCreatedFromRegion;

    // ── Constructor ───────────────────────────────────────────────────────────

    public WireframeControl()
    {
        ClipToBounds = true;
        Focusable = true;

        // Wire into Core event system
        SelectedState.Self.SelectionChanged += () => Dispatcher.UIThread.InvokeAsync(RefreshAll);
        AppCommands.Self.RefreshWireframeRequested += () => Dispatcher.UIThread.InvokeAsync(RefreshAll);
        ApplicationEvents.Self.AchxLoaded += _ => Dispatcher.UIThread.InvokeAsync(RefreshAll);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Load a PNG from disk and show it. Pass null to clear the view.
    /// Saves the camera position for the old texture and restores it for the new one.
    /// </summary>
    public void LoadTexture(string? filePath)
    {
        // Normalise path for comparison
        string? norm = string.IsNullOrEmpty(filePath) ? null : new FilePath(filePath).Standardized;
        Console.WriteLine($"[Wireframe] LoadTexture: filePath={filePath ?? "(null)"}, norm={norm ?? "(null)"}, _loadedTexturePath={_loadedTexturePath ?? "(null)"}, fileExists={norm != null && File.Exists(norm)}");

        if (_loadedTexturePath == norm) { Console.WriteLine("[Wireframe] LoadTexture: skipped (same path)"); return; }

        // Save camera for the texture we're leaving
        if (_loadedTexturePath != null)
            _cameraByTexture[_loadedTexturePath] = (_panX, _panY, _zoom);

        _loadedTexturePath = norm;
        _bitmap?.Dispose();
        _bitmap = null;
        _inspectableImage = null;

        if (norm != null && File.Exists(norm))
        {
            _bitmap = SKBitmap.Decode(norm);

            if (_isMagicWandMode)
                _inspectableImage = new InspectableImage(_bitmap);

            if (_cameraByTexture.TryGetValue(norm, out var cam))
            {
                (_panX, _panY, _zoom) = (cam.px, cam.py, cam.z);
            }
            else
            {
                CenterTexture();
            }
        }

        RefreshFramesInternal();
    }

    /// <summary>
    /// Rebuild the displayed frame rectangles from SelectedState
    /// (must be called on the UI thread).
    /// </summary>
    public void RefreshFrames() => RefreshFramesInternal();

    /// <summary>Re-detect the current texture from the selection, reload it, and refresh frames.</summary>
    public void RefreshAll()
    {
        var path = DetermineTexturePath();
        Console.WriteLine($"[Wireframe] RefreshAll: DetermineTexturePath={path ?? "(null)"}");
        LoadTexture(path);
    }

    /// <summary>Set zoom by whole-number percentage (e.g. 100 = 1× fit).</summary>
    public void SetZoomPercent(int percent)
    {
        float newZoom = Math.Clamp(percent / 100f, WireframeTransform.MinZoom, WireframeTransform.MaxZoom);
        float cx = (float)Bounds.Width / 2;
        float cy = (float)Bounds.Height / 2;
        (_panX, _panY, _zoom) = WireframeTransform.ZoomToward(cx, cy, newZoom / _zoom, _panX, _panY, _zoom);
        InvalidateVisual();
    }

    /// <summary>Toggle the grid overlay and update the grid cell size.</summary>
    public void SetGrid(bool show, int cellSize)
    {
        _showGrid = show;
        _gridSize = cellSize;
        InvalidateVisual();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        // Build a snapshot on the UI thread; the render thread reads it immutably.
        var snap = new RenderSnapshot
        {
            Bitmap = _bitmap,
            PanX = _panX,
            PanY = _panY,
            Zoom = _zoom,
            ShowGrid = _showGrid,
            GridSize = _gridSize,
            Width = Bounds.Width,
            Height = Bounds.Height,
            ShowPreview = _showPreview,
            PreviewRect = _previewRect,
        };

        foreach (var fr in _frameRects)
            snap.Frames.Add((fr.Bounds, fr.IsSelected));

        var sel = _frameRects.FirstOrDefault(f => f.IsSelected);
        if (sel != null)
            snap.SelectedHandleBounds = sel.Bounds;

        ctx.Custom(new DrawOp(snap));
    }

    // ── Mouse input ───────────────────────────────────────────────────────────

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        float factor = e.Delta.Y > 0 ? 1.25f : 1f / 1.25f;
        ZoomToward((float)pos.X, (float)pos.Y, factor);
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var props = e.GetCurrentPoint(this).Properties;
        var pos = e.GetPosition(this);
        bool isAlt = (e.KeyModifiers & KeyModifiers.Alt) != 0;
        bool isCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;

        // Middle-mouse or Alt+left → pan
        if (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && isAlt))
        {
            StartPan(pos);
            e.Pointer.Capture(this);
            return;
        }

        if (!props.IsLeftButtonPressed) return;

        // 1. Hit-test resize handles on the selected frame
        if (!isCtrl)
        {
            var (hitFrame, hitHandle) = HitTestHandle(pos);
            if (hitHandle != HandleKind.None)
            {
                _draggingRect = hitFrame;
                _draggingHandle = hitHandle;
                _dragStartWorld = ScreenToTexture((float)pos.X, (float)pos.Y);
                _dragStartBounds = hitFrame!.Bounds;
                e.Pointer.Capture(this);
                return;
            }
        }

        if (_bitmap is null) return;

        var world = ScreenToTexture((float)pos.X, (float)pos.Y);

        // 2. Magic-wand mode
        if (_isMagicWandMode && _inspectableImage != null)
        {
            _inspectableImage.GetOpaqueWandBounds(
                (int)world.X, (int)world.Y,
                out int minX, out int minY, out int maxX, out int maxY);

            if (maxX >= minX && maxY >= minY)
            {
                if (isCtrl)
                    FrameCreatedFromRegion?.Invoke(minX, minY, maxX, maxY);
                else
                    ApplyRegionToSelectedFrame(minX, minY, maxX, maxY);
            }
            return;
        }

        // 3. Snap-to-grid Ctrl+click → create frame
        if (_showGrid && _gridSize > 0 && isCtrl)
        {
            int gx = GridSnapper.Snap(world.X, _gridSize);
            int gy = GridSnapper.Snap(world.Y, _gridSize);
            FrameCreatedFromRegion?.Invoke(gx, gy, gx + _gridSize, gy + _gridSize);
            return;
        }

        // 4. Click on an unselected frame to select it
        TrySelectFrameAtPoint(world);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_isPanning)
        {
            (_panX, _panY) = WireframeTransform.Pan(
                _panAnchorX, _panAnchorY,
                (float)_panAnchor.X, (float)_panAnchor.Y,
                (float)pos.X, (float)pos.Y);
            InvalidateVisual();
            return;
        }

        if (_draggingRect != null)
        {
            ApplyHandleDrag(pos);
            return;
        }

        // Update hover preview for magic-wand / grid-snap
        UpdatePreview(pos);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }

        if (_draggingRect != null)
        {
            FrameRegionChanged?.Invoke(_draggingRect.Frame);
            _draggingRect = null;
            _draggingHandle = HandleKind.None;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_showPreview) { _showPreview = false; InvalidateVisual(); }
    }

    // ── Mouse helpers ─────────────────────────────────────────────────────────

    private void StartPan(Point pos)
    {
        _isPanning = true;
        _panAnchor = pos;
        _panAnchorX = _panX;
        _panAnchorY = _panY;
    }

    private void ZoomToward(float sx, float sy, float factor)
    {
        (_panX, _panY, _zoom) = WireframeTransform.ZoomToward(sx, sy, factor, _panX, _panY, _zoom);
        InvalidateVisual();
    }

    private void ApplyHandleDrag(Point pos)
    {
        if (_draggingRect is null || _bitmap is null) return;

        var world = ScreenToTexture((float)pos.X, (float)pos.Y);
        float dx = world.X - _dragStartWorld.X;
        float dy = world.Y - _dragStartWorld.Y;
        var startBounds = new BoundsRect(_dragStartBounds.Left, _dragStartBounds.Top,
                                         _dragStartBounds.Right, _dragStartBounds.Bottom);

        var nb = DragHandleApplier.Apply(_draggingHandle, dx, dy, startBounds,
                                         _bitmap.Width, _bitmap.Height);

        _draggingRect.Bounds = new SKRect(nb.Left, nb.Top, nb.Right, nb.Bottom);

        // Write UV coords back to the frame
        var (l, t, r, b) = DragHandleApplier.ToUvCoords(nb, _bitmap.Width, _bitmap.Height);
        var f = _draggingRect.Frame;
        f.LeftCoordinate   = l;
        f.RightCoordinate  = r;
        f.TopCoordinate    = t;
        f.BottomCoordinate = b;

        InvalidateVisual();
    }

    private void UpdatePreview(Point pos)
    {
        if (_bitmap is null) { ClearPreview(); return; }

        var world = ScreenToTexture((float)pos.X, (float)pos.Y);

        if (_isMagicWandMode && _inspectableImage != null)
        {
            _inspectableImage.GetOpaqueWandBounds(
                (int)world.X, (int)world.Y,
                out int minX, out int minY, out int maxX, out int maxY);

            bool found = maxX >= minX && maxY >= minY;
            _showPreview = found;
            if (found) _previewRect = new SKRect(minX, minY, maxX, maxY);
            InvalidateVisual();
        }
        else if (_showGrid && _gridSize > 0)
        {
            int gx = GridSnapper.Snap(world.X, _gridSize);
            int gy = GridSnapper.Snap(world.Y, _gridSize);
            _showPreview = true;
            _previewRect = new SKRect(gx, gy, gx + _gridSize, gy + _gridSize);
            InvalidateVisual();
        }
        else
        {
            ClearPreview();
        }
    }

    private void ClearPreview()
    {
        if (_showPreview) { _showPreview = false; InvalidateVisual(); }
    }

    private (FrameRect? frame, HandleKind handle) HitTestHandle(Point pos)
    {
        var sel = _frameRects.FirstOrDefault(f => f.IsSelected);
        if (sel is null) return (null, HandleKind.None);

        var sr = ToScreen(sel.Bounds);

        var kind = DragHandleHitTester.GetHandleAt(
            (float)pos.X, (float)pos.Y,
            sr.Left, sr.Top, sr.Right, sr.Bottom);

        return kind == HandleKind.None ? (null, HandleKind.None) : (sel, kind);
    }

    private void TrySelectFrameAtPoint(SKPoint worldPt)
    {
        foreach (var fr in _frameRects)
        {
            if (fr.Bounds.Contains(worldPt))
            {
                SelectedState.Self.SelectedFrame = fr.Frame;
                return;
            }
        }
    }

    private void ApplyRegionToSelectedFrame(int minX, int minY, int maxX, int maxY)
    {
        if (SelectedState.Self.SelectedFrame is null || _bitmap is null) return;
        var frame = SelectedState.Self.SelectedFrame;
        float w = _bitmap.Width, h = _bitmap.Height;
        frame.LeftCoordinate   = minX / w;
        frame.RightCoordinate  = maxX / w;
        frame.TopCoordinate    = minY / h;
        frame.BottomCoordinate = maxY / h;
        RefreshFramesInternal();
        FrameRegionChanged?.Invoke(frame);
    }

    // ── Coordinate transforms ─────────────────────────────────────────────────

    private SKPoint ScreenToTexture(float sx, float sy)
    {
        var (tx, ty) = WireframeTransform.ScreenToTexture(sx, sy, _panX, _panY, _zoom);
        return new SKPoint(tx, ty);
    }

    private SKRect ToScreen(SKRect r)
    {
        var (l, t, rr, b) = WireframeTransform.TextureRectToScreen(
            r.Left, r.Top, r.Right, r.Bottom, _panX, _panY, _zoom);
        return new SKRect(l, t, rr, b);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshFramesInternal()
    {
        _frameRects.Clear();

        if (_bitmap is null) { InvalidateVisual(); return; }

        var selectedFrame  = SelectedState.Self.SelectedFrame;
        var selectedChain  = SelectedState.Self.SelectedChain;
        var selectedChains = SelectedState.Self.SelectedChains;

        string? achxFolder = string.IsNullOrEmpty(ProjectManager.Self.FileName)
            ? null
            : FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);

        IEnumerable<AnimationFrameSave> framesToShow;
        if (selectedFrame != null)
            framesToShow = new[] { selectedFrame };
        else if (selectedChains?.Count > 0)
            framesToShow = selectedChains.SelectMany(c => c.Frames);
        else if (selectedChain?.Frames != null)
            framesToShow = selectedChain.Frames;
        else
            framesToShow = Array.Empty<AnimationFrameSave>();

        float w = _bitmap.Width;
        float h = _bitmap.Height;

        foreach (var frame in framesToShow)
        {
            if (string.IsNullOrEmpty(frame.TextureName)) continue;

            // Filter to frames that use the currently shown texture
            if (achxFolder != null && _loadedTexturePath != null)
            {
                var fp = new FilePath(achxFolder + frame.TextureName);
                if (!fp.Equals(new FilePath(_loadedTexturePath))) continue;
            }

            _frameRects.Add(new FrameRect
            {
                Frame = frame,
                Bounds = new SKRect(
                    frame.LeftCoordinate   * w,
                    frame.TopCoordinate    * h,
                    frame.RightCoordinate  * w,
                    frame.BottomCoordinate * h),
                IsSelected = frame == selectedFrame
            });
        }

        InvalidateVisual();
    }

    private void CenterTexture()
    {
        if (_bitmap is null) return;
        float ctrlW = (float)(Bounds.Width  > 0 ? Bounds.Width  : 800);
        float ctrlH = (float)(Bounds.Height > 0 ? Bounds.Height : 600);
        (_panX, _panY, _zoom) = WireframeTransform.CenterFit(_bitmap.Width, _bitmap.Height, ctrlW, ctrlH);
    }

    private string? DetermineTexturePath()
    {
        string? textureName = SelectedState.Self.SelectedFrame?.TextureName
                           ?? SelectedState.Self.SelectedChain?.Frames?.FirstOrDefault()?.TextureName;

        Console.WriteLine($"[Wireframe] DetermineTexturePath: SelectedFrame={SelectedState.Self.SelectedFrame?.TextureName ?? "(null)"}, textureName={textureName ?? "(null)"}, FileName={ProjectManager.Self.FileName ?? "(null)"}");

        if (string.IsNullOrEmpty(textureName))
            return null;

        // If no ACHX is saved yet, the texture path is already absolute.
        if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            return textureName;

        return FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName) + textureName;
    }
}
