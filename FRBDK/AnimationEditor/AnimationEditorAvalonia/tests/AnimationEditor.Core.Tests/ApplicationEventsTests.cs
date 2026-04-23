using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class ApplicationEventsTests
{
    // ── AnimationChainsChanged ────────────────────────────────────────────────

    [Fact]
    public void RaiseAnimationChainsChanged_FiresEvent()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.AnimationChainsChanged += () => fired = true;

        ApplicationEvents.Self.RaiseAnimationChainsChanged();

        Assert.True(fired);
    }

    [Fact]
    public void RaiseAnimationChainsChanged_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        // No subscribers — should silently succeed
        var ex = Record.Exception(() => ApplicationEvents.Self.RaiseAnimationChainsChanged());

        Assert.Null(ex);
    }

    [Fact]
    public void RaiseAnimationChainsChanged_NotifiesMultipleSubscribers()
    {
        TestHelpers.SetupFreshAcls();
        int count = 0;
        ApplicationEvents.Self.AnimationChainsChanged += () => count++;
        ApplicationEvents.Self.AnimationChainsChanged += () => count++;

        ApplicationEvents.Self.RaiseAnimationChainsChanged();

        Assert.Equal(2, count);
    }

    // ── AfterAxisAlignedRectangleChanged ──────────────────────────────────────

    [Fact]
    public void RaiseAfterAxisAlignedRectangleChanged_PassesRectangleToHandler()
    {
        TestHelpers.SetupFreshAcls();
        AxisAlignedRectangleSave? received = null;
        ApplicationEvents.Self.AfterAxisAlignedRectangleChanged += r => received = r;
        var rect = new AxisAlignedRectangleSave { Name = "TestRect" };

        ApplicationEvents.Self.RaiseAfterAxisAlignedRectangleChanged(rect);

        Assert.Same(rect, received);
    }

    [Fact]
    public void RaiseAfterAxisAlignedRectangleChanged_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() =>
            ApplicationEvents.Self.RaiseAfterAxisAlignedRectangleChanged(
                new AxisAlignedRectangleSave()));

        Assert.Null(ex);
    }

    // ── AfterCircleChanged ────────────────────────────────────────────────────

    [Fact]
    public void RaiseAfterCircleChanged_PassesCircleToHandler()
    {
        TestHelpers.SetupFreshAcls();
        CircleSave? received = null;
        ApplicationEvents.Self.AfterCircleChanged += c => received = c;
        var circle = new CircleSave { Name = "TestCircle", Radius = 10 };

        ApplicationEvents.Self.RaiseAfterCircleChanged(circle);

        Assert.Same(circle, received);
    }

    [Fact]
    public void RaiseAfterCircleChanged_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() =>
            ApplicationEvents.Self.RaiseAfterCircleChanged(new CircleSave()));

        Assert.Null(ex);
    }

    // ── AchxLoaded ────────────────────────────────────────────────────────────

    [Fact]
    public void CallAchxLoaded_PassesFileNameToHandler()
    {
        TestHelpers.SetupFreshAcls();
        string? received = null;
        ApplicationEvents.Self.AchxLoaded += f => received = f;

        ApplicationEvents.Self.CallAchxLoaded("C:/Game/hero.achx");

        Assert.Equal("C:/Game/hero.achx", received);
    }

    [Fact]
    public void CallAchxLoaded_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() =>
            ApplicationEvents.Self.CallAchxLoaded("some.achx"));

        Assert.Null(ex);
    }

    // ── AfterZoomChange ───────────────────────────────────────────────────────

    [Fact]
    public void CallAfterZoomChange_FiresEvent()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.AfterZoomChange += () => fired = true;

        ApplicationEvents.Self.CallAfterZoomChange();

        Assert.True(fired);
    }

    [Fact]
    public void CallAfterZoomChange_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() => ApplicationEvents.Self.CallAfterZoomChange());

        Assert.Null(ex);
    }

    // ── WireframePanning ──────────────────────────────────────────────────────

    [Fact]
    public void CallAfterWireframePanning_FiresEvent()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.WireframePanning += () => fired = true;

        ApplicationEvents.Self.CallAfterWireframePanning();

        Assert.True(fired);
    }

    [Fact]
    public void CallAfterWireframePanning_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() => ApplicationEvents.Self.CallAfterWireframePanning());

        Assert.Null(ex);
    }

    // ── WireframeTextureChange ────────────────────────────────────────────────

    [Fact]
    public void CallWireframeTextureChange_FiresEvent()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.WireframeTextureChange += () => fired = true;

        ApplicationEvents.Self.CallWireframeTextureChange();

        Assert.True(fired);
    }

    [Fact]
    public void CallWireframeTextureChange_WithNoSubscribers_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        var ex = Record.Exception(() => ApplicationEvents.Self.CallWireframeTextureChange());

        Assert.Null(ex);
    }
}
