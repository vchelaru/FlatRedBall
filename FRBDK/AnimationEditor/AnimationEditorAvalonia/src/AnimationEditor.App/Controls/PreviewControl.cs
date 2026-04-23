using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
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
using FilePath = FlatRedBall.IO.FilePath;

namespace AnimationEditor.App.Controls;

/// <summary>
/// Animated sprite preview panel. Plays the selected AnimationChain at runtime speed
/// (one frame = FrameLength seconds). When a single frame is selected, shows that
/// frame statically with optional onion-skin overlay.
/// </summary>
public class PreviewControl : Control
{
    // ── Animation state ───────────────────────────────────────────────────────
    private readonly DispatcherTimer _timer;
    private readonly AnimationEditor.Core.CommandsAndState.PlaybackController _playback = new();

    // ── Bitmap cache ──────────────────────────────────────────────────────────
    private readonly Dictionary<string, SKBitmap?> _bitmapCache =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Camera ────────────────────────────────────────────────────────────────
    private float _zoom = 1f;
    private float _panX, _panY;

    // ── Settings ──────────────────────────────────────────────────────────────
    private bool _showOnionSkin;
    private bool _showGuides;

    // ── Pan drag ──────────────────────────────────────────────────────────────
    private bool  _isPanning;
    private Point _lastMousePt;

    // ── Public properties ─────────────────────────────────────────────────────

    public bool ShowOnionSkin
    {
        get => _showOnionSkin;
        set { _showOnionSkin = value; InvalidateVisual(); }
    }

    public bool ShowGuides
    {
        get => _showGuides;
        set { _showGuides = value; InvalidateVisual(); }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public PreviewControl()
    {
        ClipToBounds = true;
        Focusable    = true;

        SelectedState.Self.SelectionChanged               += () => Dispatcher.UIThread.InvokeAsync(OnSelectionChanged);
        ApplicationEvents.Self.AnimationChainsChanged     += () => Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
        ApplicationEvents.Self.AchxLoaded                += _ => Dispatcher.UIThread.InvokeAsync(OnSelectionChanged);
        AppCommands.Self.RefreshAnimationFrameDisplayRequested += () => Dispatcher.UIThread.InvokeAsync(InvalidateVisual);

        _playback.FrameIndexChanged += _ => Dispatcher.UIThread.InvokeAsync(InvalidateVisual);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void OnTimerTick(object? sender, EventArgs e)
    {
        // Only advance when the whole chain is playing (no specific frame pinned)
        if (SelectedState.Self.SelectedFrame is not null) return;
        _playback.Advance(0.016);
    }

    // ── State reset ───────────────────────────────────────────────────────────

    private void OnSelectionChanged()
    {
        _playback.SetChain(SelectedState.Self.SelectedChain);
        InvalidateVisual();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetZoomPercent(int pct)
    {
        _zoom = Math.Clamp(pct / 100f, 0.05f, 32f);
        InvalidateVisual();
    }

    // ── Bitmap cache helpers ──────────────────────────────────────────────────

    private SKBitmap? GetBitmap(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_bitmapCache.TryGetValue(path, out var cached)) return cached;
        try
        {
            var bm = SKBitmap.Decode(path);
            _bitmapCache[path] = bm;
            return bm;
        }
        catch
        {
            _bitmapCache[path] = null;
            return null;
        }
    }

    private string? ResolveTexturePath(AnimationFrameSave? frame)
    {
        if (frame is null || string.IsNullOrEmpty(frame.TextureName)) return null;
        if (string.IsNullOrEmpty(ProjectManager.Self.FileName))       return null;
        string achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);
        string full = new FilePath(achxFolder + frame.TextureName).FullPath;
        return File.Exists(full) ? full : null;
    }

    // ── Avalonia rendering ────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var chain         = SelectedState.Self.SelectedChain;
        var selectedFrame = SelectedState.Self.SelectedFrame;

        AnimationFrameSave? displayFrame = null;
        AnimationFrameSave? onionFrame   = null;

        if (selectedFrame is not null)
        {
            displayFrame = selectedFrame;
            if (_showOnionSkin && chain is not null && chain.Frames.Count > 1)
            {
                int idx     = chain.Frames.IndexOf(selectedFrame);
                int prevIdx = (idx - 1 + chain.Frames.Count) % chain.Frames.Count;
                if (prevIdx != idx)
                    onionFrame = chain.Frames[prevIdx];
            }
        }
        else if (chain is not null && chain.Frames.Count > 0)
        {
            int idx  = Math.Clamp(_playback.CurrentFrameIndex, 0, chain.Frames.Count - 1);
            displayFrame = chain.Frames[idx];
        }

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // Pre-fill bitmap cache synchronously before handing off to render thread
        string? texPath   = ResolveTexturePath(displayFrame);
        string? onionPath = ResolveTexturePath(onionFrame);
        GetBitmap(texPath);
        GetBitmap(onionPath);

        ctx.Custom(new DrawOp(
            new RenderSnapshot(
                displayFrame, onionFrame, _zoom, _panX, _panY, _showGuides,
                texPath, onionPath, (float)w, (float)h),
            _bitmapCache));
    }

    // ── Pointer events ────────────────────────────────────────────────────────

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        float factor  = e.Delta.Y > 0 ? 1.25f : 0.8f;
        var   pt      = e.GetPosition(this);
        float oldZoom = _zoom;
        _zoom  = Math.Clamp(_zoom * factor, 0.05f, 32f);
        _panX  = (float)(pt.X - (_zoom / oldZoom) * (pt.X - _panX));
        _panY  = (float)(pt.Y - (_zoom / oldZoom) * (pt.Y - _panY));
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed ||
            (props.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            _isPanning    = true;
            _lastMousePt  = e.GetPosition(this);
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isPanning) return;
        var pt = e.GetPosition(this);
        _panX      += (float)(pt.X - _lastMousePt.X);
        _panY      += (float)(pt.Y - _lastMousePt.Y);
        _lastMousePt = pt;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
        e.Pointer.Capture(null);
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private record RenderSnapshot(
        AnimationFrameSave? Frame,
        AnimationFrameSave? OnionFrame,
        float  Zoom,
        float  PanX, float PanY,
        bool   ShowGuides,
        string? TexturePath,
        string? OnionTexturePath,
        float  Width, float Height);

    private sealed class DrawOp : ICustomDrawOperation
    {
        private readonly RenderSnapshot              _snap;
        private readonly Dictionary<string, SKBitmap?> _cache;

        public DrawOp(RenderSnapshot snap, Dictionary<string, SKBitmap?> cache)
        {
            _snap  = snap;
            _cache = cache;
        }

        public Rect Bounds => new(0, 0, _snap.Width, _snap.Height);
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => true;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext ctx)
        {
            var feature = ctx.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (feature is null) return;
            using var lease = feature.Lease();
            RenderSk(lease.SkCanvas);
        }

        private void RenderSk(SKCanvas canvas)
        {
            canvas.Clear(new SKColor(30, 30, 30));

            // Origin in screen space (frame center drawn here)
            float cx = _snap.Width  / 2 + _snap.PanX;
            float cy = _snap.Height / 2 + _snap.PanY;

            // Onion skin (below main frame)
            if (_snap.OnionFrame is not null    &&
                _snap.OnionTexturePath is not null &&
                _cache.TryGetValue(_snap.OnionTexturePath, out var onionBm) && onionBm is not null)
            {
                DrawFrame(canvas, _snap.OnionFrame, onionBm, cx, cy, _snap.Zoom, alpha: 0.4f);
            }

            // Main frame
            if (_snap.Frame is not null      &&
                _snap.TexturePath is not null &&
                _cache.TryGetValue(_snap.TexturePath, out var bm) && bm is not null)
            {
                DrawFrame(canvas, _snap.Frame, bm, cx, cy, _snap.Zoom, alpha: 1.0f);
            }

            // Origin guide lines
            if (_snap.ShowGuides)
            {
                using var gp = new SKPaint
                {
                    Color       = new SKColor(100, 200, 100, 160),
                    StrokeWidth = 1f,
                    IsAntialias = false
                };
                canvas.DrawLine(cx, 0,             cx,           _snap.Height, gp);
                canvas.DrawLine(0,  cy,             _snap.Width,  cy,           gp);
            }
        }

        private static void DrawFrame(
            SKCanvas canvas, AnimationFrameSave frame, SKBitmap bm,
            float cx, float cy, float zoom, float alpha)
        {
            int tw = bm.Width, th = bm.Height;
            int sx = (int)(frame.LeftCoordinate   * tw);
            int sy = (int)(frame.TopCoordinate    * th);
            int sw = (int)Math.Max(1, (frame.RightCoordinate  - frame.LeftCoordinate)  * tw);
            int sh = (int)Math.Max(1, (frame.BottomCoordinate - frame.TopCoordinate)   * th);

            var src = SKRectI.Create(sx, sy, sw, sh);
            float dw = sw * zoom;
            float dh = sh * zoom;
            float dx = cx - dw / 2;
            float dy = cy - dh / 2;
            var dst = SKRect.Create(dx, dy, dw, dh);

            using var paint = new SKPaint
            {
                FilterQuality = zoom >= 1 ? SKFilterQuality.None : SKFilterQuality.Low,
                Color         = new SKColor(255, 255, 255, (byte)(255 * alpha))
            };

            bool flip = frame.FlipHorizontal || frame.FlipVertical;
            if (flip)
            {
                canvas.Save();
                canvas.Scale(
                    frame.FlipHorizontal ? -1f : 1f,
                    frame.FlipVertical   ? -1f : 1f,
                    cx, cy);
            }

            canvas.DrawBitmap(bm, src, dst, paint);

            if (flip) canvas.Restore();

            // Outline rect
            using var op = new SKPaint
            {
                Color       = new SKColor(255, 255, 255, (byte)(200 * alpha)),
                StrokeWidth = 1f,
                IsStroke    = true
            };
            canvas.DrawRect(dst, op);
        }
    }
}
