using AnimationEditor.Core;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class ObjectFinderTests
{
    // ── GetAnimationFrameContaining(AxisAlignedRectangleSave) ─────────────────

    [Fact]
    public void GetAnimationFrameContaining_Rectangle_ReturnsOwningFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "HitBox" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        var result = ObjectFinder.Self.GetAnimationFrameContaining(rect);

        Assert.Same(frame, result);
    }

    [Fact]
    public void GetAnimationFrameContaining_Rectangle_WhenNotPresent_ReturnsNull()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Run", 2);
        var orphanRect = new AxisAlignedRectangleSave { Name = "Orphan" };

        var result = ObjectFinder.Self.GetAnimationFrameContaining(orphanRect);

        Assert.Null(result);
    }

    [Fact]
    public void GetAnimationFrameContaining_Rectangle_FindsRectInSecondChainSecondFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Chain1", 2);
        var chain2 = TestHelpers.MakeChain(acls, "Chain2", 3);
        var targetFrame = chain2.Frames[2];
        var rect = new AxisAlignedRectangleSave { Name = "DeepBox" };
        targetFrame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        var result = ObjectFinder.Self.GetAnimationFrameContaining(rect);

        Assert.Same(targetFrame, result);
    }

    [Fact]
    public void GetAnimationFrameContaining_Rectangle_WhenFrameHasNoShapeCollection_StillSearches()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        var frameWithRect = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frameWithRect.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        var result = ObjectFinder.Self.GetAnimationFrameContaining(rect);

        Assert.Same(frameWithRect, result);
    }

    // ── GetAnimationFrameContaining(CircleSave) ───────────────────────────────

    [Fact]
    public void GetAnimationFrameContaining_Circle_ReturnsOwningFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "Halo", Radius = 12 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        var result = ObjectFinder.Self.GetAnimationFrameContaining(circle);

        Assert.Same(frame, result);
    }

    [Fact]
    public void GetAnimationFrameContaining_Circle_WhenNotPresent_ReturnsNull()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Jump", 2);
        var orphan = new CircleSave { Name = "NotInAnyFrame", Radius = 5 };

        var result = ObjectFinder.Self.GetAnimationFrameContaining(orphan);

        Assert.Null(result);
    }

    [Fact]
    public void GetAnimationFrameContaining_Circle_FindsCircleInNonFirstPosition()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Attack", 4);
        var targetFrame = chain.Frames[3];
        var circle = new CircleSave { Name = "DeepCircle", Radius = 8 };
        targetFrame.ShapeCollectionSave!.CircleSaves.Add(circle);

        var result = ObjectFinder.Self.GetAnimationFrameContaining(circle);

        Assert.Same(targetFrame, result);
    }

    // ── GetAnimationChainContaining(AnimationFrameSave) ───────────────────────

    [Fact]
    public void GetAnimationChainContaining_Frame_ReturnsOwningChain()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 2);
        var frame = chain.Frames[1];

        var result = ObjectFinder.Self.GetAnimationChainContaining(frame);

        Assert.Same(chain, result);
    }

    [Fact]
    public void GetAnimationChainContaining_Frame_WhenNotPresent_ReturnsNull()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Walk", 2);
        var orphan = TestHelpers.MakeFrame("ghost.png");

        var result = ObjectFinder.Self.GetAnimationChainContaining(orphan);

        Assert.Null(result);
    }

    [Fact]
    public void GetAnimationChainContaining_Frame_FindsFrameInSecondChain()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Chain1", 3);
        var chain2 = TestHelpers.MakeChain(acls, "Chain2", 2);
        var targetFrame = chain2.Frames[0];

        var result = ObjectFinder.Self.GetAnimationChainContaining(targetFrame);

        Assert.Same(chain2, result);
    }

    [Fact]
    public void GetAnimationChainContaining_Frame_DoesNotConfuseFramesAcrossChains()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain1 = TestHelpers.MakeChain(acls, "Chain1", 2);
        var chain2 = TestHelpers.MakeChain(acls, "Chain2", 2);

        Assert.Same(chain1, ObjectFinder.Self.GetAnimationChainContaining(chain1.Frames[0]));
        Assert.Same(chain2, ObjectFinder.Self.GetAnimationChainContaining(chain2.Frames[1]));
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAnimationFrameContaining_Rectangle_WhenAclsEmpty_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();
        var rect = new AxisAlignedRectangleSave { Name = "LonelyRect" };

        var result = ObjectFinder.Self.GetAnimationFrameContaining(rect);

        Assert.Null(result);
    }

    [Fact]
    public void GetAnimationChainContaining_Frame_WhenAclsEmpty_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();
        var frame = TestHelpers.MakeFrame();

        var result = ObjectFinder.Self.GetAnimationChainContaining(frame);

        Assert.Null(result);
    }
}
