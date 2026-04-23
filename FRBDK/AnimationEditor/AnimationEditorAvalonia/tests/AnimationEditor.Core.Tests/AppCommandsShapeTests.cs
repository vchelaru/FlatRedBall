using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsShapeTests
{
    // ── AddAxisAlignedRectangle ───────────────────────────────────────────────

    [Fact]
    public void AddAxisAlignedRectangle_AddsRectangleToFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddAxisAlignedRectangle(frame);

        Assert.Single(frame.ShapeCollectionSave!.AxisAlignedRectangleSaves);
    }

    [Fact]
    public void AddAxisAlignedRectangle_SetsDefaultScale8()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddAxisAlignedRectangle(frame);

        var rect = frame.ShapeCollectionSave!.AxisAlignedRectangleSaves[0];
        Assert.Equal(8, rect.ScaleX);
        Assert.Equal(8, rect.ScaleY);
    }

    [Fact]
    public void AddAxisAlignedRectangle_GeneratesUniqueNamesForMultipleRects()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddAxisAlignedRectangle(frame);
        AppCommands.Self.AddAxisAlignedRectangle(frame);

        var names = frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Select(r => r.Name).ToList();
        Assert.Equal(2, names.Distinct().Count());
    }

    [Fact]
    public void AddAxisAlignedRectangle_SetsSelectedRectangle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddAxisAlignedRectangle(frame);

        Assert.NotNull(SelectedState.Self.SelectedRectangle);
        Assert.Same(
            frame.ShapeCollectionSave!.AxisAlignedRectangleSaves[0],
            SelectedState.Self.SelectedRectangle);
    }

    [Fact]
    public void AddAxisAlignedRectangle_PositionMatchesFrameRelativeXY()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        frame.RelativeX = 10f;
        frame.RelativeY = -5f;
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddAxisAlignedRectangle(frame);

        var rect = frame.ShapeCollectionSave!.AxisAlignedRectangleSaves[0];
        Assert.Equal(10f, rect.X);
        Assert.Equal(-5f, rect.Y);
    }

    // ── AddCircle ────────────────────────────────────────────────────────────

    [Fact]
    public void AddCircle_AddsCircleToFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddCircle(frame);

        Assert.Single(frame.ShapeCollectionSave!.CircleSaves);
    }

    [Fact]
    public void AddCircle_SetsDefaultRadius8()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddCircle(frame);

        Assert.Equal(8, frame.ShapeCollectionSave!.CircleSaves[0].Radius);
    }

    [Fact]
    public void AddCircle_GeneratesUniqueNamesForMultipleCircles()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddCircle(frame);
        AppCommands.Self.AddCircle(frame);

        var names = frame.ShapeCollectionSave!.CircleSaves.Select(c => c.Name).ToList();
        Assert.Equal(2, names.Distinct().Count());
    }

    [Fact]
    public void AddCircle_SetsSelectedCircle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddCircle(frame);

        Assert.NotNull(SelectedState.Self.SelectedCircle);
        Assert.Same(frame.ShapeCollectionSave!.CircleSaves[0], SelectedState.Self.SelectedCircle);
    }

    [Fact]
    public void AddCircle_PositionMatchesFrameRelativeXY()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        frame.RelativeX = 3f;
        frame.RelativeY = -7f;
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.AddCircle(frame);

        var circle = frame.ShapeCollectionSave!.CircleSaves[0];
        Assert.Equal(3f, circle.X);
        Assert.Equal(-7f, circle.Y);
    }

    [Fact]
    public void AddCircle_UniqueNameNotConflictWithExistingRectangleName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Attack", 1);
        var frame = chain.Frames[0];
        SelectedState.Self.SelectedFrame = frame;

        // Add a rect with the default circle name to force a conflict
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(
            new AxisAlignedRectangleSave { Name = "CircleInstance" });

        AppCommands.Self.AddCircle(frame);

        Assert.NotEqual("CircleInstance", frame.ShapeCollectionSave!.CircleSaves[0].Name);
    }

    // ── DeleteAxisAlignedRectangle ───────────────────────────────────────────

    [Fact]
    public void DeleteAxisAlignedRectangle_RemovesFromOwnerFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        AppCommands.Self.DeleteAxisAlignedRectangle(rect, frame);

        Assert.Empty(frame.ShapeCollectionSave!.AxisAlignedRectangleSaves);
    }

    [Fact]
    public void DeleteAxisAlignedRectangle_WhenNotOwnedByFrame_DoesNotRemoveFromOtherFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 2);
        var frame1 = chain.Frames[0];
        var frame2 = chain.Frames[1];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frame1.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        // Call with wrong owner (frame2 doesn't own rect)
        AppCommands.Self.DeleteAxisAlignedRectangle(rect, frame2);

        Assert.Single(frame1.ShapeCollectionSave!.AxisAlignedRectangleSaves);
    }

    [Fact]
    public void DeleteAxisAlignedRectangle_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "Box" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.DeleteAxisAlignedRectangle(rect, frame);
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── DeleteCircle ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteCircle_RemovesCircleFromOwnerFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        AppCommands.Self.DeleteCircle(circle, frame);

        Assert.Empty(frame.ShapeCollectionSave!.CircleSaves);
    }

    [Fact]
    public void DeleteCircle_WhenNotOwnedByFrame_DoesNotThrow()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 2);
        var frame1 = chain.Frames[0];
        var frame2 = chain.Frames[1];
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        frame1.ShapeCollectionSave!.CircleSaves.Add(circle);

        // Call with wrong owner - should not remove and should not throw
        AppCommands.Self.DeleteCircle(circle, frame2);

        Assert.Single(frame1.ShapeCollectionSave!.CircleSaves);
    }

    [Fact]
    public void DeleteCircle_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "Ring", Radius = 5 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.DeleteCircle(circle, frame);
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── MatchRectangleToFrame / MatchCircleToFrame ───────────────────────────

    [Fact]
    public void MatchRectangleToFrame_SetsRectangleXFromFrameRelativeX()
    {
        TestHelpers.SetupFreshAcls();
        var frame = TestHelpers.MakeFrame();
        frame.RelativeX = 12f;
        frame.RelativeY = -4f;
        var rect = new AxisAlignedRectangleSave();

        AppCommands.Self.MatchRectangleToFrame(rect, frame);

        Assert.Equal(12f, rect.X);
    }

    [Fact]
    public void MatchRectangleToFrame_SetsRectangleYFromFrameRelativeY()
    {
        TestHelpers.SetupFreshAcls();
        var frame = TestHelpers.MakeFrame();
        frame.RelativeX = 3f;
        frame.RelativeY = -9f;
        var rect = new AxisAlignedRectangleSave();

        AppCommands.Self.MatchRectangleToFrame(rect, frame);

        Assert.Equal(-9f, rect.Y);
    }

    [Fact]
    public void MatchCircleToFrame_SetsCircleXFromFrameRelativeX()
    {
        TestHelpers.SetupFreshAcls();
        var frame = TestHelpers.MakeFrame();
        frame.RelativeX = 7f;
        frame.RelativeY = 0f;
        var circle = new CircleSave();

        AppCommands.Self.MatchCircleToFrame(circle, frame);

        Assert.Equal(7f, circle.X);
    }

    [Fact]
    public void MatchCircleToFrame_SetsCircleYFromFrameRelativeY()
    {
        TestHelpers.SetupFreshAcls();
        var frame = TestHelpers.MakeFrame();
        frame.RelativeX = 0f;
        frame.RelativeY = 15f;
        var circle = new CircleSave();

        AppCommands.Self.MatchCircleToFrame(circle, frame);

        Assert.Equal(15f, circle.Y);
    }
}
