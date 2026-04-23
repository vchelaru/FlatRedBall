using AnimationEditor.Core.Data;
using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AESettingsSaveRoundTripTests
{
    // ── Serialization helpers ─────────────────────────────────────────────────

    private static string Serialize(AESettingsSave s)
    {
        var xs = new XmlSerializer(typeof(AESettingsSave));
        using var sw = new StringWriter();
        xs.Serialize(sw, s);
        return sw.ToString();
    }

    private static AESettingsSave Deserialize(string xml)
    {
        var xs = new XmlSerializer(typeof(AESettingsSave));
        using var sr = new StringReader(xml);
        return (AESettingsSave)xs.Deserialize(sr)!;
    }

    // ── Guides ────────────────────────────────────────────────────────────────

    [Fact]
    public void HorizontalGuides_RoundTrip_PreservesValuesAndOrder()
    {
        var s = new AESettingsSave();
        s.HorizontalGuides.Add(10.5f);
        s.HorizontalGuides.Add(25.0f);
        s.HorizontalGuides.Add(100f);

        var loaded = Deserialize(Serialize(s));

        Assert.Equal(3,     loaded.HorizontalGuides.Count);
        Assert.Equal(10.5f, loaded.HorizontalGuides[0]);
        Assert.Equal(25.0f, loaded.HorizontalGuides[1]);
        Assert.Equal(100f,  loaded.HorizontalGuides[2]);
    }

    [Fact]
    public void VerticalGuides_RoundTrip_PreservesValuesAndOrder()
    {
        var s = new AESettingsSave();
        s.VerticalGuides.Add(50f);
        s.VerticalGuides.Add(75.25f);

        var loaded = Deserialize(Serialize(s));

        Assert.Equal(2,      loaded.VerticalGuides.Count);
        Assert.Equal(50f,    loaded.VerticalGuides[0]);
        Assert.Equal(75.25f, loaded.VerticalGuides[1]);
    }

    [Fact]
    public void HorizontalAndVertical_RoundTrip_StoredIndependently()
    {
        var s = new AESettingsSave();
        s.HorizontalGuides.Add(10f);
        s.VerticalGuides.Add(20f);

        var loaded = Deserialize(Serialize(s));

        Assert.Single(loaded.HorizontalGuides);
        Assert.Single(loaded.VerticalGuides);
        Assert.Equal(10f, loaded.HorizontalGuides[0]);
        Assert.Equal(20f, loaded.VerticalGuides[0]);
    }

    // ── Expanded nodes ────────────────────────────────────────────────────────

    [Fact]
    public void ExpandedNodes_RoundTrip_PreservesAllNames()
    {
        var s = new AESettingsSave();
        s.ExpandedNodes.Add("Walk");
        s.ExpandedNodes.Add("Run");
        s.ExpandedNodes.Add("Idle");

        var loaded = Deserialize(Serialize(s));

        Assert.Equal(3, loaded.ExpandedNodes.Count);
        Assert.Contains("Walk", loaded.ExpandedNodes);
        Assert.Contains("Run",  loaded.ExpandedNodes);
        Assert.Contains("Idle", loaded.ExpandedNodes);
    }

    [Fact]
    public void ExpandedNodes_RoundTrip_PreservesInsertionOrder()
    {
        var s = new AESettingsSave();
        s.ExpandedNodes.Add("C");
        s.ExpandedNodes.Add("A");
        s.ExpandedNodes.Add("B");

        var loaded = Deserialize(Serialize(s));

        Assert.Equal("C", loaded.ExpandedNodes[0]);
        Assert.Equal("A", loaded.ExpandedNodes[1]);
        Assert.Equal("B", loaded.ExpandedNodes[2]);
    }

    // ── UnitType / grid settings ──────────────────────────────────────────────

    [Fact]
    public void UnitType_RoundTrip()
    {
        var s = new AESettingsSave { UnitType = UnitType.TextureCoordinate };
        Assert.Equal(UnitType.TextureCoordinate, Deserialize(Serialize(s)).UnitType);
    }

    [Fact]
    public void UnitType_SpriteSheet_RoundTrip()
    {
        var s = new AESettingsSave { UnitType = UnitType.SpriteSheet };
        Assert.Equal(UnitType.SpriteSheet, Deserialize(Serialize(s)).UnitType);
    }

    [Fact]
    public void SnapToGrid_True_RoundTrip()
    {
        var s = new AESettingsSave { SnapToGrid = true };
        Assert.True(Deserialize(Serialize(s)).SnapToGrid);
    }

    [Fact]
    public void GridSize_NonDefault_RoundTrip()
    {
        var s = new AESettingsSave { GridSize = 32 };
        Assert.Equal(32, Deserialize(Serialize(s)).GridSize);
    }

    [Fact]
    public void GridSize_DefaultIs16()
    {
        Assert.Equal(16, new AESettingsSave().GridSize);
    }

    // ── AnimationChainSettings ────────────────────────────────────────────────

    [Fact]
    public void AnimationChainSettings_RoundTrip_NameAndUnitType()
    {
        var s = new AESettingsSave();
        s.AnimationChainSettings.Add(new AnimationChainSettingSave
        {
            Name     = "Walk",
            UnitType = UnitType.SpriteSheet
        });

        var loaded = Deserialize(Serialize(s));

        Assert.Single(loaded.AnimationChainSettings);
        Assert.Equal("Walk",           loaded.AnimationChainSettings[0].Name);
        Assert.Equal(UnitType.SpriteSheet, loaded.AnimationChainSettings[0].UnitType);
    }

    [Fact]
    public void AnimationChainSettings_MultipleEntries_RoundTrip()
    {
        var s = new AESettingsSave();
        s.AnimationChainSettings.Add(new AnimationChainSettingSave { Name = "A", UnitType = UnitType.Pixel });
        s.AnimationChainSettings.Add(new AnimationChainSettingSave { Name = "B", UnitType = UnitType.TextureCoordinate });

        var loaded = Deserialize(Serialize(s));

        Assert.Equal(2, loaded.AnimationChainSettings.Count);
        Assert.Equal("A", loaded.AnimationChainSettings[0].Name);
        Assert.Equal("B", loaded.AnimationChainSettings[1].Name);
    }

    // ── Empty round-trip ──────────────────────────────────────────────────────

    [Fact]
    public void EmptySettings_RoundTrip_AllCollectionsEmpty()
    {
        var loaded = Deserialize(Serialize(new AESettingsSave()));

        Assert.Empty(loaded.HorizontalGuides);
        Assert.Empty(loaded.VerticalGuides);
        Assert.Empty(loaded.ExpandedNodes);
        Assert.Empty(loaded.AnimationChainSettings);
    }

    // ── Combined ─────────────────────────────────────────────────────────────

    [Fact]
    public void AllFieldsPopulated_RoundTrip_IntegrityCheck()
    {
        var s = new AESettingsSave
        {
            UnitType    = UnitType.TextureCoordinate,
            SnapToGrid  = true,
            GridSize    = 8,
            OffsetMultiplier = 2f
        };
        s.HorizontalGuides.Add(50f);
        s.VerticalGuides.Add(75f);
        s.ExpandedNodes.Add("Walk");
        s.AnimationChainSettings.Add(new AnimationChainSettingSave { Name = "Walk", UnitType = UnitType.Pixel });

        var loaded = Deserialize(Serialize(s));

        Assert.Equal(UnitType.TextureCoordinate, loaded.UnitType);
        Assert.True(loaded.SnapToGrid);
        Assert.Equal(8,  loaded.GridSize);
        Assert.Single(loaded.HorizontalGuides);
        Assert.Single(loaded.VerticalGuides);
        Assert.Single(loaded.ExpandedNodes);
        Assert.Single(loaded.AnimationChainSettings);
    }
}
