using AnimationEditor.Core;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Full round-trip tests for .achx file serialization using
/// <see cref="AnimationChainListSave.FromFile"/> and
/// <see cref="AnimationChainListSave.Save"/>.
/// XML root element is <c>&lt;AnimationChainArraySave&gt;</c> (XmlType attribute on the class).
/// </summary>
[Collection("SequentialSingletons")]
public class AchxSerializationTests
{
    // ── Chain / Frame basics ──────────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_PreservesChainName()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/test.achx";
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "WalkRight" });

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.Equal("WalkRight", loaded.AnimationChains[0].Name);
    }

    [Fact]
    public void SaveThenLoad_PreservesFrameTextureName()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/tex.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Idle" };
        chain.Frames.Add(new AnimationFrameSave { TextureName = "hero_sheet.png" });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.Equal("hero_sheet.png", loaded.AnimationChains[0].Frames[0].TextureName);
    }

    [Fact]
    public void SaveThenLoad_PreservesFrameUvCoordinates()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/uv.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Run" };
        chain.Frames.Add(new AnimationFrameSave
        {
            LeftCoordinate = 0.25f,
            RightCoordinate = 0.5f,
            TopCoordinate = 0.0f,
            BottomCoordinate = 0.5f
        });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);
        var frame = loaded.AnimationChains[0].Frames[0];

        Assert.Equal(0.25f, frame.LeftCoordinate);
        Assert.Equal(0.5f, frame.RightCoordinate);
        Assert.Equal(0.0f, frame.TopCoordinate);
        Assert.Equal(0.5f, frame.BottomCoordinate);
    }

    [Fact]
    public void SaveThenLoad_PreservesFrameLength()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/fl.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Anim" };
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0.25f });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.Equal(0.25f, loaded.AnimationChains[0].Frames[0].FrameLength);
    }

    // ── FlipHorizontal / FlipVertical ─────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_WhenFlipHorizontalTrue_Preserved()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/flip.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Flip" };
        chain.Frames.Add(new AnimationFrameSave { FlipHorizontal = true });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.True(loaded.AnimationChains[0].Frames[0].FlipHorizontal);
    }

    [Fact]
    public void SaveThenLoad_WhenFlipVerticalTrue_Preserved()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/flipV.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "FlipV" };
        chain.Frames.Add(new AnimationFrameSave { FlipVertical = true });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.True(loaded.AnimationChains[0].Frames[0].FlipVertical);
    }

    // ── RelativeX / RelativeY ─────────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_PreservesNonZeroRelativeXY()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/rel.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Offset" };
        chain.Frames.Add(new AnimationFrameSave { RelativeX = 5f, RelativeY = -3f });
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);
        var frame = loaded.AnimationChains[0].Frames[0];

        Assert.Equal(5f, frame.RelativeX);
        Assert.Equal(-3f, frame.RelativeY);
    }

    // ── Shape data ────────────────────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_PreservesAxisAlignedRectangleInFrame()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/rect.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "WithRect" };
        var frame = TestHelpers.MakeFrame();
        var rect = new AxisAlignedRectangleSave { Name = "HitBox", ScaleX = 8, ScaleY = 8, X = 2, Y = -1 };
        frame.ShapeCollectionSave!.AxisAlignedRectangleSaves.Add(rect);
        chain.Frames.Add(frame);
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);
        var loadedRect = loaded.AnimationChains[0].Frames[0]
            .ShapeCollectionSave?.AxisAlignedRectangleSaves[0];

        Assert.NotNull(loadedRect);
        Assert.Equal("HitBox", loadedRect!.Name);
        Assert.Equal(8f, loadedRect.ScaleX);
        Assert.Equal(2f, loadedRect.X);
    }

    [Fact]
    public void SaveThenLoad_PreservesCircleInFrame()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/circle.achx";
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "WithCircle" };
        var frame = TestHelpers.MakeFrame();
        var circle = new CircleSave { Name = "AttackRange", Radius = 20, X = 1, Y = 2 };
        frame.ShapeCollectionSave!.CircleSaves.Add(circle);
        chain.Frames.Add(frame);
        acls.AnimationChains.Add(chain);

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);
        var loadedCircle = loaded.AnimationChains[0].Frames[0]
            .ShapeCollectionSave?.CircleSaves[0];

        Assert.NotNull(loadedCircle);
        Assert.Equal("AttackRange", loadedCircle!.Name);
        Assert.Equal(20f, loadedCircle.Radius);
    }

    // ── Multiple chains ───────────────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_MultipleChains_AllPreserved()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/multi.achx";
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Idle" });
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Run" });
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Jump" });

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.Equal(3, loaded.AnimationChains.Count);
        Assert.Contains(loaded.AnimationChains, c => c.Name == "Idle");
        Assert.Contains(loaded.AnimationChains, c => c.Name == "Run");
        Assert.Contains(loaded.AnimationChains, c => c.Name == "Jump");
    }

    [Fact]
    public void SaveThenLoad_MultipleChains_OrderPreserved()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/order.achx";
        var acls = new AnimationChainListSave();
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta" };
        foreach (var n in names)
            acls.AnimationChains.Add(new AnimationChainSave { Name = n });

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        for (int i = 0; i < names.Length; i++)
            Assert.Equal(names[i], loaded.AnimationChains[i].Name);
    }

    // ── Empty file ────────────────────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_EmptyAcls_LoadsWithNoChains()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/empty.achx";
        var acls = new AnimationChainListSave();

        acls.Save(path);
        var loaded = AnimationChainListSave.FromFile(path);

        Assert.Empty(loaded.AnimationChains);
    }

    // ── XML structure ─────────────────────────────────────────────────────────

    [Fact]
    public void Save_XmlRootElementIsAnimationChainArraySave()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/rootcheck.achx";
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Test" });

        acls.Save(path);
        var xml = File.ReadAllText(path);

        Assert.Contains("AnimationChainArraySave", xml);
    }

    [Fact]
    public void Save_EachChainSerializesAsAnimationChainElement()
    {
        TestHelpers.SetupFreshAcls();
        using var dir = new TestHelpers.TempDir();
        var path = dir.Path + "/chainelem.achx";
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Run" });

        acls.Save(path);
        var xml = File.ReadAllText(path);

        // Per [XmlElementAttribute("AnimationChain")], chains serialize as <AnimationChain>
        Assert.Contains("<AnimationChain>", xml);
    }
}
