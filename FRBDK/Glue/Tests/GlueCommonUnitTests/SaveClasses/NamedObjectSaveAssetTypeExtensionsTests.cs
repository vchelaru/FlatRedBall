using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetAssetTypeInfo reads the shared static AvailableAssetTypesCore.Self (and, on the IsEntireFile
// path, ObjectFinderCore.Self via GetContainer), so this can't run concurrently with any other test
// class that swaps either out - hence the shared collection (see ObjectFinderCoreCollection in
// NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectSaveAssetTypeExtensionsTests
{
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();
    readonly FakeObjectFinderCore _finder = new();

    public NamedObjectSaveAssetTypeExtensionsTests()
    {
        AvailableAssetTypesCore.Self = _availableAssetTypes;
        ObjectFinderCore.Self = _finder;
    }

    [Fact]
    public void GetAssetTypeInfo_NullInstance_Throws()
    {
        NamedObjectSave instance = null;

        Assert.Throws<ArgumentNullException>(() => instance.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_SourceTypeEntity_ReturnsNull()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity };

        Assert.Null(nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_EmptyClassType_ReturnsNull()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "" };

        Assert.Null(nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_PositionedObjectListSourceClassType_ReturnsCommonPositionedObjectList()
    {
        var positionedObjectList = new AssetTypeInfo { Extension = "" };
        _availableAssetTypes.PositionedObjectList = positionedObjectList;

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Math.PositionedObjectList<Sprite>"
        };

        Assert.Same(positionedObjectList, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_ClassTypeMatchesKnownRuntimeType_ReturnsMatch()
    {
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" } };
        _availableAssetTypes.AddAssetType(ati);

        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };

        Assert.Same(ati, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_ClassTypeUnknownButSourceClassTypeMatches_ReturnsMatch()
    {
        // A file-sourced NOS is the case where ClassType (derived from SourceName's parenthesized
        // text) and SourceClassType genuinely differ - for a FlatRedBallType NOS they're the same
        // field, so this branch can't be exercised that way.
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" } };
        _availableAssetTypes.AddAssetType(ati);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.File,
            SourceName = "Sprite (Unknown)",
            SourceClassType = "FlatRedBall.Sprite"
        };

        Assert.Same(ati, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_NoMatchAndIsList_ReturnsCommonPositionedObjectList()
    {
        var positionedObjectList = new AssetTypeInfo();
        _availableAssetTypes.PositionedObjectList = positionedObjectList;

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            // Deliberately the bare "PositionedObjectList<T>" (not "FlatRedBall.Math."-prefixed) -
            // that prefix is the earlier fast-path (see the previous test). This exercises IsList's
            // own check instead. SourceClassGenericType keeps ClassType from resolving to null (the
            // "<T>" substitution needs a generic type to substitute in).
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Sprite"
        };

        Assert.Same(positionedObjectList, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_NoMatchAndNotList_ReturnsNull()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Unknown" };

        Assert.Null(nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_IsEntireFile_MatchingRuntimeType_ReturnsFileAssetTypeInfo()
    {
        var fileAti = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Graphics.Texture2D" }
        };
        _availableAssetTypes.AddAssetType(fileAti);

        var container = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "sprite.png" };
        container.ReferencedFiles.Add(rfs);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.File,
            // ClassType (= InstanceType, derived from this) must match fileAti.RuntimeTypeName,
            // which strips the namespace off QualifiedType ("...Texture2D" -> "Texture2D").
            SourceName = "Entire File (Texture2D)",
            SourceFile = "sprite.png"
        };
        _finder.GlueProject = new GlueProjectSave();
        _finder.SetContainer(nos, container);

        Assert.Same(fileAti, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_IsEntireFile_MismatchedRuntimeType_FallsBackToNormalResolution()
    {
        var fileAti = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Graphics.Texture2D" }
        };
        var fallbackAti = new AssetTypeInfo
        {
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "SomeOtherType" }
        };
        _availableAssetTypes.AddAssetType(fileAti);
        _availableAssetTypes.AddAssetType(fallbackAti);

        var container = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "sprite.png" };
        container.ReferencedFiles.Add(rfs);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.File,
            SourceName = "Entire File (SomeOtherType)",
            SourceFile = "sprite.png"
        };
        _finder.GlueProject = new GlueProjectSave();
        _finder.SetContainer(nos, container);

        Assert.Same(fallbackAti, nos.GetAssetTypeInfo());
    }

    [Fact]
    public void CanBeInShapeCollection_MatchingFrbType_ReturnsTrue()
    {
        var circle = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Circle" } };
        _availableAssetTypes.Circle = circle;
        _availableAssetTypes.AddAssetType(circle);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Circle"
        };

        Assert.True(nos.CanBeInShapeCollection());
    }

    [Fact]
    public void CanBeInShapeCollection_NonShapeFrbType_ReturnsFalse()
    {
        var sprite = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } };
        _availableAssetTypes.AddAssetType(sprite);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Sprite"
        };

        Assert.False(nos.CanBeInShapeCollection());
    }

    [Fact]
    public void CanBeInShapeCollection_NotFrbType_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };

        Assert.False(nos.CanBeInShapeCollection());
    }

    [Fact]
    public void ShouldInstantiateInConstructor_ListInstantiatedNotByBase_ReturnsTrue()
    {
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            Instantiate = true,
            InstantiatedByBase = false
        };

        Assert.True(nos.ShouldInstantiateInConstructor());
    }

    [Fact]
    public void ShouldInstantiateInConstructor_ShapeCollectionInstantiatedNotByBase_ReturnsTrue()
    {
        var shapeCollection = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "ShapeCollection" } };
        _availableAssetTypes.ShapeCollection = shapeCollection;
        _availableAssetTypes.AddAssetType(shapeCollection);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "ShapeCollection",
            Instantiate = true,
            InstantiatedByBase = false
        };

        Assert.True(nos.ShouldInstantiateInConstructor());
    }

    [Fact]
    public void ShouldInstantiateInConstructor_NotInstantiated_ReturnsFalse()
    {
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            Instantiate = false,
            InstantiatedByBase = false
        };

        Assert.False(nos.ShouldInstantiateInConstructor());
    }

    [Fact]
    public void ShouldInstantiateInConstructor_InstantiatedByBase_ReturnsFalse()
    {
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            Instantiate = true,
            InstantiatedByBase = true
        };

        Assert.False(nos.ShouldInstantiateInConstructor());
    }

    [Fact]
    public void ShouldInstantiateInConstructor_NotListAndNotShapeCollection_ReturnsFalse()
    {
        // Give the NOS a resolvable, non-ShapeCollection AssetTypeInfo so the comparison isn't a
        // vacuous null == null (AvailableAssetTypesCore.Self.ShapeCollection is unset/null here).
        var sprite = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } };
        _availableAssetTypes.AddAssetType(sprite);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Sprite",
            Instantiate = true,
            InstantiatedByBase = false
        };

        Assert.False(nos.ShouldInstantiateInConstructor());
    }

    #region GetContainedListItemAssetTypeInfo

    [Fact]
    public void GetContainedListItemAssetTypeInfo_NullInstance_Throws()
    {
        NamedObjectSave instance = null;

        Assert.Throws<ArgumentNullException>(() => instance.GetContainedListItemAssetTypeInfo());
    }

    [Fact]
    public void GetContainedListItemAssetTypeInfo_NotAList_Throws()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };

        Assert.Throws<InvalidOperationException>(() => nos.GetContainedListItemAssetTypeInfo());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetContainedListItemAssetTypeInfo_NoGenericType_ReturnsNull(string genericType)
    {
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = genericType
        };

        Assert.Null(nos.GetContainedListItemAssetTypeInfo());
    }

    [Fact]
    public void GetContainedListItemAssetTypeInfo_KnownGenericType_ReturnsItsAssetTypeInfo()
    {
        var spriteAti = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" } };
        _availableAssetTypes.AddAssetType(spriteAti);
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "FlatRedBall.Sprite"
        };

        Assert.Same(spriteAti, nos.GetContainedListItemAssetTypeInfo());
    }

    [Fact]
    public void GetContainedListItemAssetTypeInfo_UnknownGenericType_ReturnsNull()
    {
        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Entities\\Player"
        };

        Assert.Null(nos.GetContainedListItemAssetTypeInfo());
    }

    #endregion
}
