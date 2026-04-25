using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>All tests that mutate singleton state run in the same xUnit collection to prevent parallelism.</summary>
[Collection("SequentialSingletons")]
public class AppCommandsChainTests
{
    // ── AddAnimationChain ────────────────────────────────────────────────────

    [Fact]
    public void AddAnimationChain_AddsChainToAcls()
    {
        var acls = TestHelpers.SetupFreshAcls();

        AppCommands.Self.AddAnimationChain();

        Assert.Single(acls.AnimationChains);
    }

    [Fact]
    public void AddAnimationChain_UsesUniqueNamesWhenCalledMultipleTimes()
    {
        var acls = TestHelpers.SetupFreshAcls();

        AppCommands.Self.AddAnimationChain();
        AppCommands.Self.AddAnimationChain();
        AppCommands.Self.AddAnimationChain();

        var names = acls.AnimationChains.Select(c => c.Name).ToList();
        Assert.Equal(3, names.Distinct().Count());
    }

    [Fact]
    public void AddAnimationChain_SetsSelectedChain()
    {
        TestHelpers.SetupFreshAcls();

        AppCommands.Self.AddAnimationChain();

        Assert.NotNull(SelectedState.Self.SelectedChain);
    }

    [Fact]
    public void AddAnimationChain_FiresAnimationChainsChanged()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.AddAnimationChain();
            Assert.True(fired, "AnimationChainsChanged was not raised.");
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    [Fact]
    public void AddAnimationChain_WhenAclsIsNull_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        ProjectManager.Self.AnimationChainListSave = null;

        // Should silently do nothing
        AppCommands.Self.AddAnimationChain();
    }

    // ── MoveChain ────────────────────────────────────────────────────────────

    [Fact]
    public void MoveChain_Delta1_MovesChainDown()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        var chainB = TestHelpers.MakeChain(acls, "B");

        AppCommands.Self.MoveChain(chainA, +1);

        Assert.Equal(chainB, acls.AnimationChains[0]);
        Assert.Equal(chainA, acls.AnimationChains[1]);
    }

    [Fact]
    public void MoveChain_DeltaNeg1_MovesChainUp()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        var chainB = TestHelpers.MakeChain(acls, "B");

        AppCommands.Self.MoveChain(chainB, -1);

        Assert.Equal(chainB, acls.AnimationChains[0]);
        Assert.Equal(chainA, acls.AnimationChains[1]);
    }

    [Fact]
    public void MoveChain_AtBottom_DoesNotMoveBelowEnd()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        var chainB = TestHelpers.MakeChain(acls, "B");

        AppCommands.Self.MoveChain(chainB, +1); // already at end

        Assert.Equal(chainA, acls.AnimationChains[0]);
        Assert.Equal(chainB, acls.AnimationChains[1]);
    }

    [Fact]
    public void MoveChain_AtTop_DoesNotMoveAboveStart()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        TestHelpers.MakeChain(acls, "B");

        AppCommands.Self.MoveChain(chainA, -1); // already at top

        Assert.Equal(chainA, acls.AnimationChains[0]);
    }

    // ── MoveChainToTop / MoveChainToBottom ───────────────────────────────────

    [Fact]
    public void MoveChainToTop_MovesChainToFirstPosition()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "A");
        TestHelpers.MakeChain(acls, "B");
        var chainC = TestHelpers.MakeChain(acls, "C");

        AppCommands.Self.MoveChainToTop(chainC);

        Assert.Equal(chainC, acls.AnimationChains[0]);
    }

    [Fact]
    public void MoveChainToBottom_MovesChainToLastPosition()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        TestHelpers.MakeChain(acls, "B");
        TestHelpers.MakeChain(acls, "C");

        AppCommands.Self.MoveChainToBottom(chainA);

        Assert.Equal(chainA, acls.AnimationChains[2]);
    }

    // ── FlipChainHorizontally ────────────────────────────────────────────────

    [Fact]
    public void FlipChainHorizontally_TogglesFlipHorizontalOnAllFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 3);
        foreach (var f in chain.Frames) f.FlipHorizontal = false;

        AppCommands.Self.FlipChainHorizontally(chain);

        Assert.All(chain.Frames, f => Assert.True(f.FlipHorizontal));
    }

    [Fact]
    public void FlipChainHorizontally_TogglesBack_WhenCalledTwice()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 2);

        AppCommands.Self.FlipChainHorizontally(chain);
        AppCommands.Self.FlipChainHorizontally(chain);

        Assert.All(chain.Frames, f => Assert.False(f.FlipHorizontal));
    }

    [Fact]
    public void FlipChainHorizontally_MixedFlags_TogglesEachIndividually()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk", 2);
        chain.Frames[0].FlipHorizontal = true;
        chain.Frames[1].FlipHorizontal = false;

        AppCommands.Self.FlipChainHorizontally(chain);

        Assert.False(chain.Frames[0].FlipHorizontal);
        Assert.True(chain.Frames[1].FlipHorizontal);
    }

    // ── FlipChainVertically ──────────────────────────────────────────────────

    [Fact]
    public void FlipChainVertically_TogglesFlipVerticalOnAllFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump", 2);
        foreach (var f in chain.Frames) f.FlipVertical = false;

        AppCommands.Self.FlipChainVertically(chain);

        Assert.All(chain.Frames, f => Assert.True(f.FlipVertical));
    }

    // ── InvertFrameOrder ─────────────────────────────────────────────────────

    [Fact]
    public void InvertFrameOrder_ReversesFrameSequence()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var original = chain.Frames.ToList();

        AppCommands.Self.InvertFrameOrder(chain);

        Assert.Equal(original[2], chain.Frames[0]);
        Assert.Equal(original[1], chain.Frames[1]);
        Assert.Equal(original[0], chain.Frames[2]);
    }

    [Fact]
    public void InvertFrameOrder_OddFrameCount_MiddleFrameStaysMiddle()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 5);
        var middle = chain.Frames[2];

        AppCommands.Self.InvertFrameOrder(chain);

        Assert.Equal(middle, chain.Frames[2]);
    }

    [Fact]
    public void InvertFrameOrder_ThenInvertAgain_RestoresOriginalOrder()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 4);
        var original = chain.Frames.ToList();

        AppCommands.Self.InvertFrameOrder(chain);
        AppCommands.Self.InvertFrameOrder(chain);

        Assert.Equal(original, chain.Frames.ToList());
    }

    // ── SetAllFrameLengths ───────────────────────────────────────────────────

    [Fact]
    public void SetAllFrameLengths_AssignsDurationToEveryFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Idle", 4);

        AppCommands.Self.SetAllFrameLengths(chain, 0.25f);

        Assert.All(chain.Frames, f => Assert.Equal(0.25f, f.FrameLength));
    }

    [Fact]
    public void SetAllFrameLengths_ZeroDuration_IsAllowed()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Idle", 2);

        AppCommands.Self.SetAllFrameLengths(chain, 0f);

        Assert.All(chain.Frames, f => Assert.Equal(0f, f.FrameLength));
    }

    // ── DuplicateChain ───────────────────────────────────────────────────────

    [Fact]
    public void DuplicateChain_CreatesDeepCopyWithAllFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "Walk", 3);

        var copy = AppCommands.Self.DuplicateChain(source);

        Assert.NotNull(copy);
        Assert.Equal(3, copy!.Frames.Count);
        Assert.NotSame(source, copy);
        Assert.NotSame(source.Frames[0], copy.Frames[0]);
    }

    [Fact]
    public void DuplicateChain_CopiedFramesHaveSameTextureName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "Walk", 2);
        source.Frames[0].TextureName = "alpha.png";
        source.Frames[1].TextureName = "beta.png";

        var copy = AppCommands.Self.DuplicateChain(source);

        Assert.Equal("alpha.png", copy!.Frames[0].TextureName);
        Assert.Equal("beta.png", copy.Frames[1].TextureName);
    }

    [Fact]
    public void DuplicateChain_WithFlipH_TogglesFlipHorizontalOnAllCopiedFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "WalkLeft", 2);
        source.Frames[0].FlipHorizontal = false;
        source.Frames[1].FlipHorizontal = true;

        var copy = AppCommands.Self.DuplicateChain(source, flipH: true);

        Assert.True(copy!.Frames[0].FlipHorizontal);   // false → toggled → true
        Assert.False(copy.Frames[1].FlipHorizontal);   // true  → toggled → false
    }

    [Fact]
    public void DuplicateChain_WithFlipV_TogglesFlipVerticalOnAllCopiedFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "JumpUp", 2);
        foreach (var f in source.Frames) f.FlipVertical = false;

        var copy = AppCommands.Self.DuplicateChain(source, flipV: true);

        Assert.All(copy!.Frames, f => Assert.True(f.FlipVertical));
    }

    [Fact]
    public void DuplicateChain_CopiesShapesFromFrames()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "Attack", 1);
        source.Frames[0].ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(
            new AxisAlignedRectangleSave { Name = "HitBox", ScaleX = 5, ScaleY = 5 });

        var copy = AppCommands.Self.DuplicateChain(source);

        Assert.Single(copy!.Frames[0].ShapeCollectionSave!.AxisAlignedRectangleSaves);
        Assert.Equal("HitBox", copy.Frames[0].ShapeCollectionSave!.AxisAlignedRectangleSaves[0].Name);
    }

    [Fact]
    public void DuplicateChain_CopiedChainNameIsUnique()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "Walk");

        var copy1 = AppCommands.Self.DuplicateChain(source);
        var copy2 = AppCommands.Self.DuplicateChain(source);

        Assert.NotEqual(copy1!.Name, copy2!.Name);
    }

    [Fact]
    public void DuplicateChain_WithCustomName_UsesProvidedName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "WalkLeft");

        var copy = AppCommands.Self.DuplicateChain(source, newName: "WalkRight");

        Assert.Equal("WalkRight", copy!.Name);
    }

    [Fact]
    public void DuplicateChain_AddsToAcls()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var source = TestHelpers.MakeChain(acls, "Walk");

        AppCommands.Self.DuplicateChain(source);

        Assert.Equal(2, acls.AnimationChains.Count);
    }

    [Fact]
    public void DuplicateChain_WhenAclsIsNull_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();
        ProjectManager.Self.AnimationChainListSave = null;
        var orphan = new AnimationChainSave { Name = "Orphan" };

        var result = AppCommands.Self.DuplicateChain(orphan);

        Assert.Null(result);
    }

    // ── SortAnimationsAlphabetically ─────────────────────────────────────────

    [Fact]
    public void SortAnimationsAlphabetically_SortsByNameAscending()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "Zebra");
        TestHelpers.MakeChain(acls, "Alpha");
        TestHelpers.MakeChain(acls, "Mango");

        AppCommands.Self.SortAnimationsAlphabetically();

        Assert.Equal("Alpha", acls.AnimationChains[0].Name);
        Assert.Equal("Mango", acls.AnimationChains[1].Name);
        Assert.Equal("Zebra", acls.AnimationChains[2].Name);
    }

    [Fact]
    public void SortAnimationsAlphabetically_AlreadySorted_IsIdempotent()
    {
        var acls = TestHelpers.SetupFreshAcls();
        TestHelpers.MakeChain(acls, "A");
        TestHelpers.MakeChain(acls, "B");
        TestHelpers.MakeChain(acls, "C");

        AppCommands.Self.SortAnimationsAlphabetically();

        Assert.Equal("A", acls.AnimationChains[0].Name);
        Assert.Equal("B", acls.AnimationChains[1].Name);
        Assert.Equal("C", acls.AnimationChains[2].Name);
    }

    // ── DeleteAnimationChains ────────────────────────────────────────────────

    [Fact]
    public void DeleteAnimationChains_RemovesSpecifiedChains()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "A");
        var chainB = TestHelpers.MakeChain(acls, "B");
        var chainC = TestHelpers.MakeChain(acls, "C");

        AppCommands.Self.DeleteAnimationChains(new List<AnimationChainSave> { chainA, chainC });

        Assert.Single(acls.AnimationChains);
        Assert.Equal(chainB, acls.AnimationChains[0]);
    }

    [Fact]
    public void DeleteAnimationChains_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "X");
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.DeleteAnimationChains(new List<AnimationChainSave> { chain });
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── FlipFrameHorizontally (F09) ──────────────────────────────────────────

    [Fact]
    public void FlipFrameHorizontally_TogglesFlipHorizontalOnFrame()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipHorizontal = false };

        AppCommands.Self.FlipFrameHorizontally(frame);

        Assert.True(frame.FlipHorizontal);
    }

    [Fact]
    public void FlipFrameHorizontally_TogglesBackWhenCalledTwice()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipHorizontal = false };

        AppCommands.Self.FlipFrameHorizontally(frame);
        AppCommands.Self.FlipFrameHorizontally(frame);

        Assert.False(frame.FlipHorizontal);
    }

    [Fact]
    public void FlipFrameHorizontally_DoesNotAffectFlipVertical()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipHorizontal = false, FlipVertical = true };

        AppCommands.Self.FlipFrameHorizontally(frame);

        Assert.True(frame.FlipVertical);
    }

    [Fact]
    public void FlipFrameHorizontally_RaisesAnimationChainsChanged()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave();
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.FlipFrameHorizontally(frame);
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── FlipFrameVertically (F10) ────────────────────────────────────────────

    [Fact]
    public void FlipFrameVertically_TogglesFlipVerticalOnFrame()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipVertical = false };

        AppCommands.Self.FlipFrameVertically(frame);

        Assert.True(frame.FlipVertical);
    }

    [Fact]
    public void FlipFrameVertically_TogglesBackWhenCalledTwice()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipVertical = false };

        AppCommands.Self.FlipFrameVertically(frame);
        AppCommands.Self.FlipFrameVertically(frame);

        Assert.False(frame.FlipVertical);
    }

    [Fact]
    public void FlipFrameVertically_DoesNotAffectFlipHorizontal()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { FlipVertical = false, FlipHorizontal = true };

        AppCommands.Self.FlipFrameVertically(frame);

        Assert.True(frame.FlipHorizontal);
    }

    [Fact]
    public void FlipFrameVertically_RaisesAnimationChainsChanged()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave();
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.FlipFrameVertically(frame);
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── RenameChain (TV07) ────────────────────────────────────────────────────

    [Fact]
    public void RenameChain_UniqueNewName_ReturnsTrueAndUpdatesName()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "OldName");

        bool result = AppCommands.Self.RenameChain(chain, "NewName");

        Assert.True(result);
        Assert.Equal("NewName", chain.Name);
    }

    [Fact]
    public void RenameChain_SameName_ReturnsTrueWithoutChange()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk");

        bool result = AppCommands.Self.RenameChain(chain, "Walk");

        Assert.True(result);
        Assert.Equal("Walk", chain.Name);
    }

    [Fact]
    public void RenameChain_DuplicateNameExistsOnOtherChain_ReturnsFalseAndNameUnchanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "Walk");
        var chainB = TestHelpers.MakeChain(acls, "Run");

        bool result = AppCommands.Self.RenameChain(chainA, "Run");

        Assert.False(result);
        Assert.Equal("Walk", chainA.Name);
        Assert.Equal("Run",  chainB.Name);
    }

    [Fact]
    public void RenameChain_UniqueNewName_RaisesAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "A");
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.RenameChain(chain, "B");
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    [Fact]
    public void RenameChain_DuplicateName_DoesNotRaiseAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chainA = TestHelpers.MakeChain(acls, "Walk");
        TestHelpers.MakeChain(acls, "Run");
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.RenameChain(chainA, "Run");
            Assert.False(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    // ── RenameFrame / SetFrameTextureName (TV08) ───────────────────────────────

    [Fact]
    public void RenameFrame_SetsTextureNameOnFrame()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new FlatRedBall.Content.AnimationChain.AnimationFrameSave
            { TextureName = "old.png" };

        AppCommands.Self.RenameFrame(frame, "new.png");

        Assert.Equal("new.png", frame.TextureName);
    }

    [Fact]
    public void RenameFrame_RaisesAnimationChainsChanged()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new FlatRedBall.Content.AnimationChain.AnimationFrameSave();
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.RenameFrame(frame, "hero.png");
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    [Fact]
    public void RenameFrame_EmptyString_SetsEmptyTextureName()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new FlatRedBall.Content.AnimationChain.AnimationFrameSave
            { TextureName = "something.png" };

        AppCommands.Self.RenameFrame(frame, string.Empty);

        Assert.Equal(string.Empty, frame.TextureName);
    }
}
