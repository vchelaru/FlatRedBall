using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class SelectedStateTests
{
    // ── SelectedChain ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectedChain_WhenSet_ClearsSelectedFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        SelectedState.Self.SelectedFrame = chain.Frames[0];

        // Setting chain should clear frame
        var otherChain = TestHelpers.MakeChain(acls, "Idle");
        SelectedState.Self.SelectedChain = otherChain;

        Assert.Null(SelectedState.Self.SelectedFrame);
    }

    [Fact]
    public void SelectedChain_WhenSet_ClearsSelectedRectangle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);
        SelectedState.Self.SelectedRectangle = rect;

        var otherChain = TestHelpers.MakeChain(acls, "Idle");
        SelectedState.Self.SelectedChain = otherChain;

        Assert.Null(SelectedState.Self.SelectedRectangle);
    }

    [Fact]
    public void SelectedChain_WhenSet_ClearsSelectedCircle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);
        SelectedState.Self.SelectedCircle = circle;

        var otherChain = TestHelpers.MakeChain(acls, "Idle");
        SelectedState.Self.SelectedChain = otherChain;

        Assert.Null(SelectedState.Self.SelectedCircle);
    }

    [Fact]
    public void SelectedChain_WhenSet_FiresSelectionChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run");
        bool fired = false;
        SelectedState.Self.SelectionChanged += () => fired = true;

        SelectedState.Self.SelectedChain = chain;

        Assert.True(fired);
    }

    [Fact]
    public void SelectedChain_WhenSetToNull_StillFiresSelectionChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run");
        SelectedState.Self.SelectedChain = chain;

        bool fired = false;
        SelectedState.Self.SelectionChanged += () => fired = true;
        SelectedState.Self.SelectedChain = null;

        Assert.True(fired);
    }

    // ── SelectedFrame ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectedFrame_WhenSet_AutoFindsParentChain()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 2);
        var frame = chain.Frames[1];

        SelectedState.Self.SelectedFrame = frame;

        Assert.Same(chain, SelectedState.Self.SelectedChain);
    }

    [Fact]
    public void SelectedFrame_WhenSet_ClearsSelectedRectangle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);
        SelectedState.Self.SelectedRectangle = rect;

        SelectedState.Self.SelectedFrame = frame;

        Assert.Null(SelectedState.Self.SelectedRectangle);
    }

    [Fact]
    public void SelectedFrame_WhenSet_ClearsSelectedCircle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);
        SelectedState.Self.SelectedCircle = circle;

        SelectedState.Self.SelectedFrame = frame;

        Assert.Null(SelectedState.Self.SelectedCircle);
    }

    [Fact]
    public void SelectedFrame_WhenSet_FiresSelectionChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        bool fired = false;
        SelectedState.Self.SelectionChanged += () => fired = true;

        SelectedState.Self.SelectedFrame = chain.Frames[0];

        Assert.True(fired);
    }

    [Fact]
    public void SelectedFrame_WhenFrameNotInAnyChain_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var standaloneFrame = TestHelpers.MakeFrame();

        // Should not throw even if frame isn't in ACLS
        SelectedState.Self.SelectedFrame = standaloneFrame;

        Assert.Same(standaloneFrame, SelectedState.Self.SelectedFrame);
    }

    // ── SelectedRectangle / SelectedCircle mutual exclusion ───────────────────

    [Fact]
    public void SelectedRectangle_WhenSet_ClearsSelectedCircle()
    {
        TestHelpers.SetupFreshAcls();
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        SelectedState.Self.SelectedCircle = circle;

        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        SelectedState.Self.SelectedRectangle = rect;

        Assert.Null(SelectedState.Self.SelectedCircle);
        Assert.Same(rect, SelectedState.Self.SelectedRectangle);
    }

    [Fact]
    public void SelectedCircle_WhenSet_ClearsSelectedRectangle()
    {
        TestHelpers.SetupFreshAcls();
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        SelectedState.Self.SelectedRectangle = rect;

        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        SelectedState.Self.SelectedCircle = circle;

        Assert.Null(SelectedState.Self.SelectedRectangle);
        Assert.Same(circle, SelectedState.Self.SelectedCircle);
    }

    [Fact]
    public void SelectedRectangle_WhenSet_FiresSelectionChanged()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        SelectedState.Self.SelectionChanged += () => fired = true;
        var rect = new AxisAlignedRectangleSave { Name = "Box" };

        SelectedState.Self.SelectedRectangle = rect;

        Assert.True(fired);
    }

    [Fact]
    public void SelectedCircle_WhenSet_FiresSelectionChanged()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        SelectedState.Self.SelectionChanged += () => fired = true;
        var circle = new CircleSave { Name = "Ring", Radius = 5 };

        SelectedState.Self.SelectedCircle = circle;

        Assert.True(fired);
    }

    // ── SelectedShape ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectedShape_WhenRectangleSelected_ReturnsRectangle()
    {
        TestHelpers.SetupFreshAcls();
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        SelectedState.Self.SelectedRectangle = rect;

        Assert.Same(rect, SelectedState.Self.SelectedShape);
    }

    [Fact]
    public void SelectedShape_WhenCircleSelected_ReturnsCircle()
    {
        TestHelpers.SetupFreshAcls();
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        SelectedState.Self.SelectedCircle = circle;

        Assert.Same(circle, SelectedState.Self.SelectedShape);
    }

    [Fact]
    public void SelectedShape_WhenNothingSelected_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();

        Assert.Null(SelectedState.Self.SelectedShape);
    }

    // ── SelectedTextureName ───────────────────────────────────────────────────

    [Fact]
    public void SelectedTextureName_WhenFrameSelectedWithTexture_ReturnsFrameTextureName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        chain.Frames[0].TextureName = "hero_run.png";
        SelectedState.Self.SelectedFrame = chain.Frames[0];

        Assert.Equal("hero_run.png", SelectedState.Self.SelectedTextureName);
    }

    [Fact]
    public void SelectedTextureName_WhenChainSelectedAndHasFrames_ReturnsFirstFrameTexture()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 2);
        chain.Frames[0].TextureName = "sheet.png";
        chain.Frames[1].TextureName = "other.png";
        SelectedState.Self.SelectedChain = chain;

        Assert.Equal("sheet.png", SelectedState.Self.SelectedTextureName);
    }

    [Fact]
    public void SelectedTextureName_WhenNothingSelected_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();

        Assert.Null(SelectedState.Self.SelectedTextureName);
    }

    // ── SelectedFrames / multi-select ─────────────────────────────────────────

    [Fact]
    public void SelectedFrames_WhenSelectedNodesContainFrames_ReturnsThoseFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var f0 = chain.Frames[0];
        var f2 = chain.Frames[2];
        SelectedState.Self.SelectedNodes = new List<object> { f0, f2 };

        var frames = SelectedState.Self.SelectedFrames;

        Assert.Contains(f0, frames);
        Assert.Contains(f2, frames);
    }

    [Fact]
    public void SelectedFrames_WhenNodesEmpty_FallsBackToSingleSelectedFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 2);
        SelectedState.Self.SelectedNodes = new List<object>();
        SelectedState.Self.SelectedFrame = chain.Frames[1];

        var frames = SelectedState.Self.SelectedFrames;

        Assert.Contains(chain.Frames[1], frames);
    }

    [Fact]
    public void SelectedFrames_WhenNothingSelected_ReturnsEmptyEnumerable()
    {
        TestHelpers.SetupFreshAcls();
        SelectedState.Self.SelectedNodes = new List<object>();

        var frames = SelectedState.Self.SelectedFrames;

        Assert.Empty(frames);
    }
}
