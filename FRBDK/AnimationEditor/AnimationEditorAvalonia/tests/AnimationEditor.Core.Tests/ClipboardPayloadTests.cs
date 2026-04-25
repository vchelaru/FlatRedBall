using AnimationEditor.Core.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class ClipboardPayloadTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    static AnimationChainSave MakeChain(string name) => new AnimationChainSave { Name = name };

    static AnimationFrameSave MakeFrame(string tex) => new AnimationFrameSave
    {
        TextureName      = tex,
        LeftCoordinate   = 0f,
        RightCoordinate  = 1f,
        TopCoordinate    = 0f,
        BottomCoordinate = 1f,
        FrameLength      = 0.1f,
        ShapeCollectionSave = new ShapeCollectionSave()
    };

    // ── AnimationChainSave list ───────────────────────────────────────────

    [Fact]
    public void RoundTrip_ChainList_RestoresNames()
    {
        var chains = new List<AnimationChainSave> { MakeChain("Walk"), MakeChain("Run") };
        var text   = ClipboardPayload.Serialize(chains);

        bool ok = ClipboardPayload.TryDeserialize(text,
            out var gotChains, out _, out _, out _);

        Assert.True(ok);
        Assert.NotNull(gotChains);
        Assert.Equal(2, gotChains!.Count);
        Assert.Equal("Walk", gotChains[0].Name);
        Assert.Equal("Run",  gotChains[1].Name);
    }

    [Fact]
    public void Serialize_ChainList_StartsWithCorrectPrefix()
    {
        var text = ClipboardPayload.Serialize(new List<AnimationChainSave> { MakeChain("X") });
        Assert.StartsWith("List<AnimationChainSave>:", text);
    }

    // ── AnimationFrameSave list ───────────────────────────────────────────

    [Fact]
    public void RoundTrip_FrameList_RestoresTextureNames()
    {
        var frames = new List<AnimationFrameSave> { MakeFrame("a.png"), MakeFrame("b.png") };
        var text   = ClipboardPayload.Serialize(frames);

        bool ok = ClipboardPayload.TryDeserialize(text,
            out _, out var gotFrames, out _, out _);

        Assert.True(ok);
        Assert.NotNull(gotFrames);
        Assert.Equal("a.png", gotFrames![0].TextureName);
        Assert.Equal("b.png", gotFrames![1].TextureName);
    }

    [Fact]
    public void Serialize_FrameList_StartsWithCorrectPrefix()
    {
        var text = ClipboardPayload.Serialize(new List<AnimationFrameSave> { MakeFrame("x.png") });
        Assert.StartsWith("List<AnimationFrameSave>:", text);
    }

    // ── AxisAlignedRectangleSave ──────────────────────────────────────────

    [Fact]
    public void RoundTrip_Rectangle_RestoresProperties()
    {
        var rect = new AxisAlignedRectangleSave { Name = "HitBox", ScaleX = 8, ScaleY = 16, X = 1, Y = 2 };
        var text = ClipboardPayload.Serialize(rect);

        bool ok = ClipboardPayload.TryDeserialize(text,
            out _, out _, out var gotRect, out _);

        Assert.True(ok);
        Assert.NotNull(gotRect);
        Assert.Equal("HitBox", gotRect!.Name);
        Assert.Equal(8f,  gotRect.ScaleX, precision: 4);
        Assert.Equal(16f, gotRect.ScaleY, precision: 4);
    }

    [Fact]
    public void Serialize_Rectangle_StartsWithCorrectPrefix()
    {
        var text = ClipboardPayload.Serialize(new AxisAlignedRectangleSave { Name = "R" });
        Assert.StartsWith("AxisAlignedRectangleSave:", text);
    }

    // ── CircleSave ────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_Circle_RestoresProperties()
    {
        var circle = new CircleSave { Name = "Sensor", Radius = 12, X = 3, Y = -4 };
        var text   = ClipboardPayload.Serialize(circle);

        bool ok = ClipboardPayload.TryDeserialize(text,
            out _, out _, out _, out var gotCircle);

        Assert.True(ok);
        Assert.NotNull(gotCircle);
        Assert.Equal("Sensor", gotCircle!.Name);
        Assert.Equal(12f, gotCircle.Radius, precision: 4);
    }

    [Fact]
    public void Serialize_Circle_StartsWithCorrectPrefix()
    {
        var text = ClipboardPayload.Serialize(new CircleSave { Name = "C" });
        Assert.StartsWith("CircleSave:", text);
    }

    // ── TryDeserialize — error cases ──────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no colon here")]
    public void TryDeserialize_InvalidInput_ReturnsFalse(string? input)
    {
        bool ok = ClipboardPayload.TryDeserialize(input,
            out var c, out var f, out var r, out var ci);
        Assert.False(ok);
        Assert.Null(c);
        Assert.Null(f);
        Assert.Null(r);
        Assert.Null(ci);
    }

    [Fact]
    public void TryDeserialize_UnknownTypeName_ReturnsFalse()
    {
        bool ok = ClipboardPayload.TryDeserialize("UnknownType:<xml/>",
            out _, out _, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryDeserialize_MalformedXml_ReturnsFalse()
    {
        bool ok = ClipboardPayload.TryDeserialize("CircleSave:<<<NOT XML>>>",
            out _, out _, out _, out _);
        Assert.False(ok);
    }

    // ── Output discriminator: only the correct out param is set ──────────

    [Fact]
    public void TryDeserialize_ChainList_OnlyChainsIsPopulated()
    {
        var text = ClipboardPayload.Serialize(new List<AnimationChainSave> { MakeChain("A") });
        ClipboardPayload.TryDeserialize(text, out var chains, out var frames, out var rect, out var circle);
        Assert.NotNull(chains);
        Assert.Null(frames);
        Assert.Null(rect);
        Assert.Null(circle);
    }

    [Fact]
    public void TryDeserialize_FrameList_OnlyFramesIsPopulated()
    {
        var text = ClipboardPayload.Serialize(new List<AnimationFrameSave> { MakeFrame("a.png") });
        ClipboardPayload.TryDeserialize(text, out var chains, out var frames, out var rect, out var circle);
        Assert.Null(chains);
        Assert.NotNull(frames);
        Assert.Null(rect);
        Assert.Null(circle);
    }
}
