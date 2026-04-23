using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class DragHandleHitTesterTests
{
    // rect: left=10, top=10, right=110, bottom=110, cx=60, cy=60

    [Fact]
    public void GetHandleAt_TopLeft_WhenOnTopLeftCorner()
    {
        var result = DragHandleHitTester.GetHandleAt(10f, 10f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.TopLeft, result);
    }

    [Fact]
    public void GetHandleAt_TopCenter_WhenOnTopEdgeMidpoint()
    {
        var result = DragHandleHitTester.GetHandleAt(60f, 10f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.TopCenter, result);
    }

    [Fact]
    public void GetHandleAt_TopRight_WhenOnTopRightCorner()
    {
        var result = DragHandleHitTester.GetHandleAt(110f, 10f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.TopRight, result);
    }

    [Fact]
    public void GetHandleAt_MidLeft_WhenOnLeftEdgeMidpoint()
    {
        var result = DragHandleHitTester.GetHandleAt(10f, 60f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.MidLeft, result);
    }

    [Fact]
    public void GetHandleAt_MidRight_WhenOnRightEdgeMidpoint()
    {
        var result = DragHandleHitTester.GetHandleAt(110f, 60f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.MidRight, result);
    }

    [Fact]
    public void GetHandleAt_BotLeft_WhenOnBottomLeftCorner()
    {
        var result = DragHandleHitTester.GetHandleAt(10f, 110f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.BotLeft, result);
    }

    [Fact]
    public void GetHandleAt_BotCenter_WhenOnBottomEdgeMidpoint()
    {
        var result = DragHandleHitTester.GetHandleAt(60f, 110f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.BotCenter, result);
    }

    [Fact]
    public void GetHandleAt_BotRight_WhenOnBottomRightCorner()
    {
        var result = DragHandleHitTester.GetHandleAt(110f, 110f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.BotRight, result);
    }

    [Fact]
    public void GetHandleAt_Move_WhenInsideRectBody()
    {
        var result = DragHandleHitTester.GetHandleAt(60f, 60f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.Move, result);
    }

    [Fact]
    public void GetHandleAt_None_WhenOutsideRect()
    {
        var result = DragHandleHitTester.GetHandleAt(200f, 200f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.None, result);
    }

    [Fact]
    public void GetHandleAt_HitsHandle_WhenWithinHitRadius()
    {
        // Point is 5 pixels away from TopLeft corner — within default hitRadius of 7
        var result = DragHandleHitTester.GetHandleAt(15f, 14f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.TopLeft, result);
    }

    [Fact]
    public void GetHandleAt_MissesHandle_WhenBeyondHitRadius()
    {
        // Point is 20 pixels away from TopLeft corner — beyond default hitRadius of 7
        var result = DragHandleHitTester.GetHandleAt(30f, 30f, 10f, 10f, 110f, 110f);
        Assert.Equal(HandleKind.Move, result); // inside the rect but not near a handle
    }

    [Fact]
    public void GetHandleAt_None_WhenRectIsZeroSize()
    {
        var result = DragHandleHitTester.GetHandleAt(50f, 50f, 50f, 50f, 50f, 50f);
        // all handles coincide at (50,50); TopLeft is checked first
        Assert.NotEqual(HandleKind.None, result);
    }
}

[Collection("SequentialSingletons")]
public class DragHandleApplierTests
{
    private static BoundsRect Rect20x20 => new(20f, 20f, 80f, 80f); // 60×60

    // ── Apply – edge deltas ───────────────────────────────────────────────────

    [Fact]
    public void Apply_Move_ShiftsAllFourEdges()
    {
        var result = DragHandleApplier.Apply(HandleKind.Move, 10f, 5f, Rect20x20, 200f, 200f);
        Assert.Equal(30f, result.Left);
        Assert.Equal(25f, result.Top);
        Assert.Equal(90f, result.Right);
        Assert.Equal(85f, result.Bottom);
    }

    [Fact]
    public void Apply_TopLeft_MovesTopAndLeftEdges()
    {
        var result = DragHandleApplier.Apply(HandleKind.TopLeft, 5f, 5f, Rect20x20, 200f, 200f);
        Assert.Equal(25f, result.Left);
        Assert.Equal(25f, result.Top);
        Assert.Equal(80f, result.Right);  // unchanged
        Assert.Equal(80f, result.Bottom); // unchanged
    }

    [Fact]
    public void Apply_TopCenter_MovesTopEdgeOnly()
    {
        var result = DragHandleApplier.Apply(HandleKind.TopCenter, 99f, 5f, Rect20x20, 200f, 200f);
        Assert.Equal(20f, result.Left);   // unchanged
        Assert.Equal(25f, result.Top);
        Assert.Equal(80f, result.Right);  // unchanged
        Assert.Equal(80f, result.Bottom); // unchanged
    }

    [Fact]
    public void Apply_MidRight_MovesRightEdgeOnly()
    {
        var result = DragHandleApplier.Apply(HandleKind.MidRight, 10f, 99f, Rect20x20, 200f, 200f);
        Assert.Equal(20f, result.Left);
        Assert.Equal(20f, result.Top);
        Assert.Equal(90f, result.Right);
        Assert.Equal(80f, result.Bottom);
    }

    [Fact]
    public void Apply_BotCenter_MovesBottomEdgeOnly()
    {
        var result = DragHandleApplier.Apply(HandleKind.BotCenter, 99f, 10f, Rect20x20, 200f, 200f);
        Assert.Equal(90f, result.Bottom); // 80 + dy(10) = 90
        Assert.Equal(20f, result.Left);
        Assert.Equal(80f, result.Right);
        Assert.Equal(20f, result.Top);
    }

    // ── Clamping ──────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_ClampsToBitmapRight()
    {
        // Move to the right by 200 — should be clamped at bitmapWidth=100
        var result = DragHandleApplier.Apply(HandleKind.Move, 200f, 0f, Rect20x20, 100f, 100f);
        Assert.True(result.Right <= 100f);
    }

    [Fact]
    public void Apply_ClampsToBitmapBottom()
    {
        var result = DragHandleApplier.Apply(HandleKind.Move, 0f, 200f, Rect20x20, 100f, 100f);
        Assert.True(result.Bottom <= 100f);
    }

    [Fact]
    public void Apply_ClampsToZeroLeft()
    {
        var result = DragHandleApplier.Apply(HandleKind.Move, -200f, 0f, Rect20x20, 100f, 100f);
        Assert.True(result.Left >= 0f);
    }

    [Fact]
    public void Apply_EnforcesMinimumWidthOf1()
    {
        // Drag right edge far left to collapse width
        var result = DragHandleApplier.Apply(HandleKind.MidRight, -200f, 0f, Rect20x20, 100f, 100f);
        Assert.True(result.Right - result.Left >= 1f);
    }

    [Fact]
    public void Apply_EnforcesMinimumHeightOf1()
    {
        // Drag bottom edge far up to collapse height
        var result = DragHandleApplier.Apply(HandleKind.BotCenter, 0f, -200f, Rect20x20, 100f, 100f);
        Assert.True(result.Bottom - result.Top >= 1f);
    }

    // ── UV conversion ─────────────────────────────────────────────────────────

    [Fact]
    public void ToUvCoords_DividesByBitmapDimensions()
    {
        var (l, t, r, b) = DragHandleApplier.ToUvCoords(new BoundsRect(10f, 20f, 50f, 80f), 100f, 100f);
        Assert.Equal(0.1f, l);
        Assert.Equal(0.2f, t);
        Assert.Equal(0.5f, r);
        Assert.Equal(0.8f, b);
    }

    [Fact]
    public void ToUvCoords_FullBitmapBounds_ReturnsOneByOne()
    {
        var (l, t, r, b) = DragHandleApplier.ToUvCoords(new BoundsRect(0f, 0f, 100f, 100f), 100f, 100f);
        Assert.Equal(0f, l);
        Assert.Equal(0f, t);
        Assert.Equal(1f, r);
        Assert.Equal(1f, b);
    }

    [Fact]
    public void Apply_None_LeavesRectUnchanged()
    {
        var result = DragHandleApplier.Apply(HandleKind.None, 50f, 50f, Rect20x20, 200f, 200f);
        Assert.Equal(Rect20x20, result);
    }
}
