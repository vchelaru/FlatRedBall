using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Tests for the conditional-serialization helpers on <see cref="AnimationFrameSave"/>.
///
/// Key implementation notes:
/// - <c>ShouldSerializeFlipHorizontal()</c> and <c>ShouldSerializeFlipVertical()</c> are METHODS —
///   XmlSerializer honors them as conditional gates (elements are omitted when false).
/// - <c>ShouldSerializeRelativeX()</c> and <c>ShouldSerializeRelativeY()</c> are METHODS —
///   XmlSerializer honors them (elements omitted when value is 0).
/// - <c>ShouldSerializeShapeCollectionSave</c> is a PROPERTY (expression body, no parentheses) —
///   XmlSerializer does NOT honor it as a method-based gate, so ShapeCollectionSave serializes
///   whenever non-null, regardless of whether any shapes are present.
/// </summary>
public class AnimationFrameSaveConditionalSerializationTests
{
    // ── ShouldSerializeFlipHorizontal() method behavior ───────────────────────

    [Fact]
    public void ShouldSerializeFlipHorizontal_WhenFalse_ReturnsFalse()
    {
        var frame = new AnimationFrameSave { FlipHorizontal = false };

        Assert.False(frame.ShouldSerializeFlipHorizontal());
    }

    [Fact]
    public void ShouldSerializeFlipHorizontal_WhenTrue_ReturnsTrue()
    {
        var frame = new AnimationFrameSave { FlipHorizontal = true };

        Assert.True(frame.ShouldSerializeFlipHorizontal());
    }

    // ── ShouldSerializeFlipVertical() method behavior ─────────────────────────

    [Fact]
    public void ShouldSerializeFlipVertical_WhenFalse_ReturnsFalse()
    {
        var frame = new AnimationFrameSave { FlipVertical = false };

        Assert.False(frame.ShouldSerializeFlipVertical());
    }

    [Fact]
    public void ShouldSerializeFlipVertical_WhenTrue_ReturnsTrue()
    {
        var frame = new AnimationFrameSave { FlipVertical = true };

        Assert.True(frame.ShouldSerializeFlipVertical());
    }

    // ── ShouldSerializeRelativeX() method behavior ────────────────────────────

    [Fact]
    public void ShouldSerializeRelativeX_WhenZero_ReturnsFalse()
    {
        var frame = new AnimationFrameSave { RelativeX = 0f };

        Assert.False(frame.ShouldSerializeRelativeX());
    }

    [Fact]
    public void ShouldSerializeRelativeX_WhenNonZero_ReturnsTrue()
    {
        var frame = new AnimationFrameSave { RelativeX = 5f };

        Assert.True(frame.ShouldSerializeRelativeX());
    }

    [Fact]
    public void ShouldSerializeRelativeX_WhenNegative_ReturnsTrue()
    {
        var frame = new AnimationFrameSave { RelativeX = -3.5f };

        Assert.True(frame.ShouldSerializeRelativeX());
    }

    // ── ShouldSerializeRelativeY() method behavior ────────────────────────────

    [Fact]
    public void ShouldSerializeRelativeY_WhenZero_ReturnsFalse()
    {
        var frame = new AnimationFrameSave { RelativeY = 0f };

        Assert.False(frame.ShouldSerializeRelativeY());
    }

    [Fact]
    public void ShouldSerializeRelativeY_WhenNonZero_ReturnsTrue()
    {
        var frame = new AnimationFrameSave { RelativeY = 7f };

        Assert.True(frame.ShouldSerializeRelativeY());
    }

    // ── XML output: FlipHorizontal conditional serialization ──────────────────

    [Fact]
    public void Serialization_WhenFlipHorizontalFalse_OmitsElementFromXml()
    {
        var frame = new AnimationFrameSave { FlipHorizontal = false };

        var xml = SerializeFrame(frame);

        Assert.DoesNotContain("FlipHorizontal", xml);
    }

    [Fact]
    public void Serialization_WhenFlipHorizontalTrue_IncludesElementInXml()
    {
        var frame = new AnimationFrameSave { FlipHorizontal = true };

        var xml = SerializeFrame(frame);

        Assert.Contains("FlipHorizontal", xml);
        Assert.Contains("true", xml);
    }

    // ── XML output: FlipVertical conditional serialization ────────────────────

    [Fact]
    public void Serialization_WhenFlipVerticalFalse_OmitsElementFromXml()
    {
        var frame = new AnimationFrameSave { FlipVertical = false };

        var xml = SerializeFrame(frame);

        Assert.DoesNotContain("FlipVertical", xml);
    }

    [Fact]
    public void Serialization_WhenFlipVerticalTrue_IncludesElementInXml()
    {
        var frame = new AnimationFrameSave { FlipVertical = true };

        var xml = SerializeFrame(frame);

        Assert.Contains("FlipVertical", xml);
    }

    // ── XML output: RelativeX/Y conditional serialization ────────────────────

    [Fact]
    public void Serialization_WhenRelativeXZero_OmitsElementFromXml()
    {
        var frame = new AnimationFrameSave { RelativeX = 0f };

        var xml = SerializeFrame(frame);

        Assert.DoesNotContain("RelativeX", xml);
    }

    [Fact]
    public void Serialization_WhenRelativeXNonZero_IncludesElementInXml()
    {
        var frame = new AnimationFrameSave { RelativeX = 4.5f };

        var xml = SerializeFrame(frame);

        Assert.Contains("RelativeX", xml);
    }

    [Fact]
    public void Serialization_WhenRelativeYZero_OmitsElementFromXml()
    {
        var frame = new AnimationFrameSave { RelativeY = 0f };

        var xml = SerializeFrame(frame);

        Assert.DoesNotContain("RelativeY", xml);
    }

    [Fact]
    public void Serialization_WhenRelativeYNonZero_IncludesElementInXml()
    {
        var frame = new AnimationFrameSave { RelativeY = -2f };

        var xml = SerializeFrame(frame);

        Assert.Contains("RelativeY", xml);
    }

    // ── ShouldSerializeShapeCollectionSave: property, not method ─────────────
    //
    // IMPORTANT: ShouldSerializeShapeCollectionSave is a C# property (expression body =>),
    // not a parameterless method. XmlSerializer only honors ShouldSerialize* gating
    // when the member is a method. Because it is a property, XmlSerializer does NOT
    // call it as a conditional gate — ShapeCollectionSave serializes whenever non-null,
    // even when no shapes are present.

    [Fact]
    public void ShouldSerializeShapeCollectionSave_IsAProperty_NotAMethod()
    {
        var frameType = typeof(AnimationFrameSave);
        var method = frameType.GetMethod("ShouldSerializeShapeCollectionSave");
        var property = frameType.GetProperty("ShouldSerializeShapeCollectionSave");

        Assert.Null(method);    // No parameterless method — XmlSerializer can't find it
        Assert.NotNull(property); // It's a property
    }

    [Fact]
    public void Serialization_WhenShapeCollectionSaveIsNull_OmitsElement()
    {
        var frame = new AnimationFrameSave { ShapeCollectionSave = null };

        var xml = SerializeFrame(frame);

        Assert.DoesNotContain("ShapeCollectionSave", xml);
    }

    [Fact]
    public void Serialization_WhenShapeCollectionSaveIsNonNullButEmpty_IncludesElement()
    {
        // Because ShouldSerializeShapeCollectionSave is a property (not a method),
        // XmlSerializer does NOT use it as a gate. A non-null but empty
        // ShapeCollectionSave WILL appear in the output XML.
        var frame = new AnimationFrameSave
        {
            ShapeCollectionSave = new ShapeCollectionSave()
        };

        var xml = SerializeFrame(frame);

        Assert.Contains("ShapeCollectionSave", xml);
    }

    [Fact]
    public void ShouldSerializeShapeCollectionSave_Property_WhenNullReturnsFalse()
    {
        var frame = new AnimationFrameSave { ShapeCollectionSave = null };

        Assert.False(frame.ShouldSerializeShapeCollectionSave);
    }

    [Fact]
    public void ShouldSerializeShapeCollectionSave_Property_WhenHasRects_ReturnsTrue()
    {
        var frame = new AnimationFrameSave
        {
            ShapeCollectionSave = new ShapeCollectionSave()
        };
        frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(
            new AxisAlignedRectangleSave { Name = "Box" });

        Assert.True(frame.ShouldSerializeShapeCollectionSave);
    }

    [Fact]
    public void ShouldSerializeShapeCollectionSave_Property_WhenHasCircles_ReturnsTrue()
    {
        var frame = new AnimationFrameSave
        {
            ShapeCollectionSave = new ShapeCollectionSave()
        };
        frame.ShapeCollectionSave.CircleSaves.Add(
            new CircleSave { Name = "Ring", Radius = 5 });

        Assert.True(frame.ShouldSerializeShapeCollectionSave);
    }

    [Fact]
    public void ShouldSerializeShapeCollectionSave_Property_WhenEmptyCollection_ReturnsFalse()
    {
        // The property correctly returns false for an empty collection,
        // but XmlSerializer doesn't call it as a method so it doesn't prevent serialization.
        var frame = new AnimationFrameSave
        {
            ShapeCollectionSave = new ShapeCollectionSave() // non-null, all lists empty
        };

        Assert.False(frame.ShouldSerializeShapeCollectionSave);
    }

    // ── RightCoordinate / BottomCoordinate non-zero defaults ─────────────────

    [Fact]
    public void DefaultFrame_RightCoordinateIsOne()
    {
        var frame = new AnimationFrameSave();

        Assert.Equal(1f, frame.RightCoordinate);
    }

    [Fact]
    public void DefaultFrame_BottomCoordinateIsOne()
    {
        var frame = new AnimationFrameSave();

        Assert.Equal(1f, frame.BottomCoordinate);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string SerializeFrame(AnimationFrameSave frame)
    {
        var serializer = new XmlSerializer(typeof(AnimationFrameSave));
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        serializer.Serialize(writer, frame);
        return sb.ToString();
    }
}
