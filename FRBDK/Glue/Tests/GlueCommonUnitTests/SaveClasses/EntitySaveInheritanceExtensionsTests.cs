using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// These methods read the shared static ObjectFinderCore.Self/AvailableAssetTypesCore.Self, so this
// can't run concurrently with any other test class that swaps them out.
[Collection(nameof(ObjectFinderCoreCollection))]
public class EntitySaveInheritanceExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();

    public EntitySaveInheritanceExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
    }

    [Fact]
    public void GetRootBaseEntitySave_NoBaseEntity_ReturnsSelf()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.Same(entity, entity.GetRootBaseEntitySave());
    }

    [Fact]
    public void GetRootBaseEntitySave_InheritsFromFrbType_ReturnsSelf()
    {
        var entity = new EntitySave { BaseEntity = "Sprite" };

        Assert.Same(entity, entity.GetRootBaseEntitySave());
    }

    [Fact]
    public void GetRootBaseEntitySave_ChainOfEntities_ReturnsTopmostBase()
    {
        var root = new EntitySave { Name = "Entities\\Root", BaseEntity = "" };
        _finder.AddElement("Entities\\Root", root);
        var middle = new EntitySave { Name = "Entities\\Middle", BaseEntity = "Entities\\Root" };
        _finder.AddElement("Entities\\Middle", middle);
        var leaf = new EntitySave { BaseEntity = "Entities\\Middle" };

        Assert.Same(root, leaf.GetRootBaseEntitySave());
    }

    [Fact]
    public void GetImplementsIWindowRecursively_OwnPropertyTrue_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsIWindow = true };

        Assert.True(entity.GetImplementsIWindowRecursively());
    }

    [Fact]
    public void GetImplementsIWindowRecursively_InheritedFromBase_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsIWindow = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetImplementsIWindowRecursively());
    }

    [Fact]
    public void GetImplementsIVisibleRecursively_NeitherOwnNorBase_ReturnsFalse()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.False(entity.GetImplementsIVisibleRecursively());
    }

    [Fact]
    public void GetImplementsIClickableRecursively_OwnPropertyTrue_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsIClickable = true };

        Assert.True(entity.GetImplementsIClickableRecursively());
    }

    [Fact]
    public void GetImplementsITiledTileMetadataRecursively_InheritedFromBase_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsITiledTileMetadata = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetImplementsITiledTileMetadataRecursively());
    }

    [Fact]
    public void GetInheritsFromIWindow_NoBaseEntity_ReturnsFalse()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.False(entity.GetInheritsFromIWindow());
    }

    [Fact]
    public void GetInheritsFromIWindow_BaseImplementsDirectly_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsIWindow = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetInheritsFromIWindow());
    }

    [Fact]
    public void GetInheritsFromITiledTileMetadata_BaseImplementsDirectly_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsITiledTileMetadata = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetInheritsFromITiledTileMetadata());
    }

    [Fact]
    public void GetHasImplementsCollidableProperty_NoBaseEntity_ReturnsTrue()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.True(entity.GetHasImplementsCollidableProperty());
    }

    [Fact]
    public void GetHasImplementsCollidableProperty_FrbBaseImplementsICollidable_ReturnsFalse()
    {
        var ati = new AssetTypeInfo
        {
            ImplementsICollidable = true,
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" }
        };
        _availableAssetTypes.AddAssetType(ati);
        var entity = new EntitySave { BaseEntity = "Sprite" };

        Assert.False(entity.GetHasImplementsCollidableProperty());
    }

    [Fact]
    public void GetHasImplementsCollidableProperty_FrbBaseDoesNotImplementICollidable_ReturnsTrue()
    {
        var ati = new AssetTypeInfo
        {
            ImplementsICollidable = false,
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" }
        };
        _availableAssetTypes.AddAssetType(ati);
        var entity = new EntitySave { BaseEntity = "Sprite" };

        Assert.True(entity.GetHasImplementsCollidableProperty());
    }

    [Fact]
    public void GetInheritsFromIVisible_FrbBaseHasVisibleProperty_ReturnsTrue()
    {
        var ati = new AssetTypeInfo
        {
            HasVisibleProperty = true,
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" }
        };
        _availableAssetTypes.AddAssetType(ati);
        var entity = new EntitySave { BaseEntity = "Sprite" };

        Assert.True(entity.GetInheritsFromIVisible());
    }

    [Fact]
    public void GetInheritsFromIVisible_EntityBaseImplementsIt_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsIVisible = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetInheritsFromIVisible());
    }

    [Fact]
    public void GetInheritsFromIClickable_NoBaseEntity_ReturnsFalse()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.False(entity.GetInheritsFromIClickable());
    }

    [Fact]
    public void GetInheritsFromIClickable_BaseImplementsRecursively_ReturnsTrue()
    {
        var root = new EntitySave { Name = "Entities\\Root", ImplementsIClickable = true };
        _finder.AddElement("Entities\\Root", root);
        var baseEntity = new EntitySave { Name = "Entities\\Base", BaseEntity = "Entities\\Root" };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetInheritsFromIClickable());
    }

    [Fact]
    public void GetInheritsFromIWindowOrIClickable_BaseImplementsIClickable_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", ImplementsIClickable = true };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(entity.GetInheritsFromIWindowOrIClickable());
    }

    [Fact]
    public void GetInheritsFromIWindowOrIClickable_NeitherImplemented_ReturnsFalse()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.False(entity.GetInheritsFromIWindowOrIClickable());
    }

    [Fact]
    public void BaseElements_ReturnsFullChainMostDerivedFirst()
    {
        var root = new EntitySave { Name = "Entities\\Root" };
        _finder.AddElement("Entities\\Root", root);
        var middle = new EntitySave { Name = "Entities\\Middle", BaseObject = "Entities\\Root" };
        _finder.AddElement("Entities\\Middle", middle);
        IElement leaf = new EntitySave { BaseObject = "Entities\\Middle" };

        var result = leaf.BaseElements().ToList();

        Assert.Equal(new IElement[] { middle, root }, result);
    }

    [Fact]
    public void BaseElements_NoBaseElement_ReturnsEmpty()
    {
        IElement leaf = new EntitySave { BaseObject = "" };

        Assert.Empty(leaf.BaseElements());
    }

    [Fact]
    public void GetAllBaseEntities_ReturnsFullChain()
    {
        var root = new EntitySave { Name = "Entities\\Root" };
        _finder.AddElement("Entities\\Root", root);
        var middle = new EntitySave { Name = "Entities\\Middle", BaseEntity = "Entities\\Root" };
        _finder.AddElement("Entities\\Middle", middle);
        var leaf = new EntitySave { BaseEntity = "Entities\\Middle" };

        var result = leaf.GetAllBaseEntities();

        Assert.Equal(new[] { middle, root }, result);
    }

    [Fact]
    public void GetAllBaseEntities_NoBaseEntity_ReturnsEmpty()
    {
        var entity = new EntitySave { BaseEntity = "" };

        Assert.Empty(entity.GetAllBaseEntities());
    }
}
