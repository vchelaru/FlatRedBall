using FlatRedBall.Glue.Elements;

namespace GlueCommonUnitTests.Elements;

public class AssetTypeInfoTests
{
    [Fact]
    public void RuntimeTypeName_QualifiedTypeWithNamespace_ReturnsLastSegment()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" }
        };

        Assert.Equal("Sprite", ati.RuntimeTypeName);
    }

    [Fact]
    public void RuntimeTypeName_QualifiedTypeWithoutNamespace_ReturnsItUnchanged()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" }
        };

        Assert.Equal("Sprite", ati.RuntimeTypeName);
    }

    [Fact]
    public void RuntimeTypeName_EmptyQualifiedType_ReturnsNull()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "" }
        };

        Assert.Null(ati.RuntimeTypeName);
    }

    [Fact]
    public void RuntimeTypeName_PlatformFuncSet_TakesPriorityOverQualifiedType()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType
            {
                QualifiedType = "FlatRedBall.Sprite",
                PlatformFunc = _ => "PlatformSpecificSprite"
            }
        };

        Assert.Equal("PlatformSpecificSprite", ati.RuntimeTypeName);
    }

    [Fact]
    public void IsInstantiatedInAddToManagers_FirstEntryStartsWithThisEquals_ReturnsTrue()
    {
        var ati = new AssetTypeInfo { AddToManagersMethod = new List<string> { "this = new Layer()" } };

        Assert.True(ati.IsInstantiatedInAddToManagers);
    }

    [Fact]
    public void IsInstantiatedInAddToManagers_FirstEntryDoesNotStartWithThisEquals_ReturnsFalse()
    {
        var ati = new AssetTypeInfo { AddToManagersMethod = new List<string> { "this.AddToManagers()" } };

        Assert.False(ati.IsInstantiatedInAddToManagers);
    }

    [Fact]
    public void IsInstantiatedInAddToManagers_EmptyList_ReturnsFalse()
    {
        var ati = new AssetTypeInfo { AddToManagersMethod = new List<string>() };

        Assert.False(ati.IsInstantiatedInAddToManagers);
    }

    [Fact]
    public void IsInstantiatedInAddToManagers_NullList_ReturnsFalse()
    {
        var ati = new AssetTypeInfo { AddToManagersMethod = null };

        Assert.False(ati.IsInstantiatedInAddToManagers);
    }

    [Fact]
    public void IsMatchTo_SameExtensionAndType_ReturnsTrue()
    {
        var first = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Microsoft.Xna.Framework.Graphics.Texture2D" }
        };
        var second = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Microsoft.Xna.Framework.Graphics.Texture2D" }
        };

        Assert.True(first.IsMatchTo(second));
    }

    [Fact]
    public void IsMatchTo_DifferentExtension_ReturnsFalse()
    {
        var first = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Microsoft.Xna.Framework.Graphics.Texture2D" }
        };
        var second = new AssetTypeInfo
        {
            Extension = "jpg",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Microsoft.Xna.Framework.Graphics.Texture2D" }
        };

        Assert.False(first.IsMatchTo(second));
    }

    [Fact]
    public void ToString_ReturnsFriendlyName()
    {
        var ati = new AssetTypeInfo { FriendlyName = "Sprite" };

        Assert.Equal("Sprite", ati.ToString());
    }
}
