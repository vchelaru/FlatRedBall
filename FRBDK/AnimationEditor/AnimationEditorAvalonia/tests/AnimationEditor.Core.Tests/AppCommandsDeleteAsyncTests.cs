using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System.Threading.Tasks;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Tests for the async delete-with-confirmation operations in AppCommands.
/// The <see cref="AppCommands.ConfirmAsync"/> delegate is overridden per-test
/// to control whether the user "confirms" or "cancels" the dialog.
/// </summary>
[Collection("SequentialSingletons")]
public class AppCommandsDeleteAsyncTests
{
    // ── AskToDeleteAnimationChains ────────────────────────────────────────────

    [Fact]
    public async Task AskToDeleteAnimationChains_WhenConfirmed_DeletesChains()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chainA = TestHelpers.MakeChain(acls, "A");
        var chainB = TestHelpers.MakeChain(acls, "B");
        var chainC = TestHelpers.MakeChain(acls, "C");

        await AppCommands.Self.AskToDeleteAnimationChains(
            new List<AnimationChainSave> { chainA, chainC });

        Assert.Single(acls.AnimationChains);
        Assert.Equal(chainB, acls.AnimationChains[0]);
    }

    [Fact]
    public async Task AskToDeleteAnimationChains_WhenCancelled_DoesNotDeleteAnyChains()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(false);
        TestHelpers.MakeChain(acls, "A");
        TestHelpers.MakeChain(acls, "B");

        await AppCommands.Self.AskToDeleteAnimationChains(
            new List<AnimationChainSave> { acls.AnimationChains[0] });

        Assert.Equal(2, acls.AnimationChains.Count);
    }

    [Fact]
    public async Task AskToDeleteAnimationChains_WhenConfirmed_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "X");
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            await AppCommands.Self.AskToDeleteAnimationChains(
                new List<AnimationChainSave> { chain });
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── AskToDeleteFrames ─────────────────────────────────────────────────────

    [Fact]
    public async Task AskToDeleteFrames_WhenConfirmed_DeletesFramesFromSelectedChain()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        SelectedState.Self.SelectedChain = chain;
        var frameToDelete = chain.Frames[1];

        await AppCommands.Self.AskToDeleteFrames(
            new List<AnimationFrameSave> { frameToDelete });

        Assert.Equal(2, chain.Frames.Count);
        Assert.DoesNotContain(frameToDelete, chain.Frames);
    }

    [Fact]
    public async Task AskToDeleteFrames_WhenCancelled_DoesNotDeleteAnyFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(false);
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        SelectedState.Self.SelectedChain = chain;

        await AppCommands.Self.AskToDeleteFrames(
            new List<AnimationFrameSave> { chain.Frames[0] });

        Assert.Equal(3, chain.Frames.Count);
    }

    [Fact]
    public async Task AskToDeleteFrames_WhenConfirmed_DeletesMultipleFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "Run", 4);
        SelectedState.Self.SelectedChain = chain;
        var toDelete = new List<AnimationFrameSave> { chain.Frames[0], chain.Frames[2] };

        await AppCommands.Self.AskToDeleteFrames(toDelete);

        Assert.Equal(2, chain.Frames.Count);
    }

    [Fact]
    public async Task AskToDeleteFrames_WhenConfirmed_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "Run", 2);
        SelectedState.Self.SelectedChain = chain;
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            await AppCommands.Self.AskToDeleteFrames(
                new List<AnimationFrameSave> { chain.Frames[0] });
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── AskToDeleteRectangles ─────────────────────────────────────────────────

    [Fact]
    public async Task AskToDeleteRectangles_WhenConfirmed_RemovesRectangleFromFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "HitBox" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        await AppCommands.Self.AskToDeleteRectangles(
            new List<AxisAlignedRectangleSave> { rect });

        Assert.Empty(frame.ShapeCollectionSave!.AxisAlignedRectangleSaves);
    }

    [Fact]
    public async Task AskToDeleteRectangles_WhenCancelled_DoesNotRemoveRectangle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(false);
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "HitBox" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        await AppCommands.Self.AskToDeleteRectangles(
            new List<AxisAlignedRectangleSave> { rect });

        Assert.Single(frame.ShapeCollectionSave!.AxisAlignedRectangleSaves);
    }

    [Fact]
    public async Task AskToDeleteRectangles_ConfirmMessageContainsRectangleName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        string? capturedMessage = null;
        AppCommands.Self.ConfirmAsync = (msg, _) =>
        {
            capturedMessage = msg;
            return Task.FromResult(false);
        };
        var chain = TestHelpers.MakeChain(acls, "Walk", 1);
        var frame = chain.Frames[0];
        var rect = new AxisAlignedRectangleSave { Name = "BodyCollision" };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);

        await AppCommands.Self.AskToDeleteRectangles(
            new List<AxisAlignedRectangleSave> { rect });

        Assert.NotNull(capturedMessage);
        Assert.Contains("BodyCollision", capturedMessage);
    }

    // ── AskToDeleteCircles ────────────────────────────────────────────────────

    [Fact]
    public async Task AskToDeleteCircles_WhenConfirmed_RemovesCircleFromFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(true);
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "AttackRadius", Radius = 10 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        await AppCommands.Self.AskToDeleteCircles(
            new List<CircleSave> { circle });

        Assert.Empty(frame.ShapeCollectionSave!.CircleSaves);
    }

    [Fact]
    public async Task AskToDeleteCircles_WhenCancelled_DoesNotRemoveCircle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.ConfirmAsync = (_, __) => Task.FromResult(false);
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "AttackRadius", Radius = 10 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        await AppCommands.Self.AskToDeleteCircles(
            new List<CircleSave> { circle });

        Assert.Single(frame.ShapeCollectionSave!.CircleSaves);
    }

    [Fact]
    public async Task AskToDeleteCircles_ConfirmMessageContainsCircleName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        string? capturedMessage = null;
        AppCommands.Self.ConfirmAsync = (msg, _) =>
        {
            capturedMessage = msg;
            return Task.FromResult(false);
        };
        var chain = TestHelpers.MakeChain(acls, "Jump", 1);
        var frame = chain.Frames[0];
        var circle = new CircleSave { Name = "DetectionArea", Radius = 20 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);

        await AppCommands.Self.AskToDeleteCircles(
            new List<CircleSave> { circle });

        Assert.NotNull(capturedMessage);
        Assert.Contains("DetectionArea", capturedMessage);
    }
}
