using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetContainer/GetReferencedElement/GetContainerType/NamedObjectSaveToString/GetDefiningNamedObjectSave
// all read the shared static ObjectFinderCore.Self, so this can't run concurrently with any other test
// class that swaps it out - hence the shared collection (see ObjectFinderCoreCollection below).
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();

    public NamedObjectSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
    }

    [Fact]
    public void GetContainer_GlueProjectNull_ReturnsNull()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave();

        Assert.Null(nos.GetContainer());
    }

    [Fact]
    public void GetContainer_GlueProjectSet_ReturnsContainerFromFinder()
    {
        _finder.GlueProject = new GlueProjectSave();
        var container = new EntitySave { Name = "Entities\\Player" };
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, container);

        Assert.Same(container, nos.GetContainer());
    }

    [Fact]
    public void GetReferencedElement_NullInstance_Throws()
    {
        NamedObjectSave instance = null;

        Assert.Throws<ArgumentNullException>(() => instance.GetReferencedElement());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetReferencedElement_NoSourceClassType_ReturnsNull(string sourceClassType)
    {
        var nos = new NamedObjectSave { SourceClassType = sourceClassType };

        Assert.Null(nos.GetReferencedElement());
    }

    [Fact]
    public void GetReferencedElement_KnownSourceClassType_ReturnsEntitySave()
    {
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        _finder.AddElement("Entities\\Enemy", entity);
        var nos = new NamedObjectSave { SourceClassType = "Entities\\Enemy" };

        Assert.Same(entity, nos.GetReferencedElement());
    }

    [Fact]
    public void GetContainerType_NoContainer_ReturnsNone()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave();

        Assert.Equal(ContainerType.None, nos.GetContainerType());
    }

    [Fact]
    public void GetContainerType_EntityContainer_ReturnsEntity()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, new EntitySave { Name = "Entities\\Player" });

        Assert.Equal(ContainerType.Entity, nos.GetContainerType());
    }

    [Fact]
    public void GetContainerType_ScreenContainer_ReturnsScreen()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave();
        _finder.SetContainer(nos, new ScreenSave { Name = "Screens\\GameScreen" });

        Assert.Equal(ContainerType.Screen, nos.GetContainerType());
    }

    [Fact]
    public void NamedObjectSaveToString_Uncontained_IncludesUncontainedSuffix()
    {
        _finder.GlueProject = null;
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite", InstanceName = "SpriteInstance" };

        var result = NamedObjectSaveElementExtensions.NamedObjectSaveToString(nos);

        Assert.Equal("Sprite SpriteInstance (Uncontained)", result);
    }

    [Fact]
    public void NamedObjectSaveToString_Contained_IncludesContainerName()
    {
        _finder.GlueProject = new GlueProjectSave();
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite", InstanceName = "SpriteInstance" };
        var container = new EntitySave { Name = "Entities\\Player" };
        _finder.SetContainer(nos, container);

        var result = NamedObjectSaveElementExtensions.NamedObjectSaveToString(nos);

        Assert.Equal($"Sprite SpriteInstance in {container}", result);
    }

    [Fact]
    public void GetDefiningNamedObjectSave_NotDefinedByBase_ReturnsInstance()
    {
        var nos = new NamedObjectSave { DefinedByBase = false };
        var container = new EntitySave { Name = "Entities\\Player" };

        Assert.Same(nos, nos.GetDefiningNamedObjectSave(container));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBaseWithNoBaseElement_Throws()
    {
        var nos = new NamedObjectSave { DefinedByBase = true, InstanceName = "SpriteInstance" };
        var container = new EntitySave { Name = "Entities\\Derived", BaseEntity = "" };

        Assert.Throws<Exception>(() => nos.GetDefiningNamedObjectSave(container));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBase_FindsMatchInBaseElement()
    {
        var baseNos = new NamedObjectSave { InstanceName = "SpriteInstance", SetByDerived = true };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(baseNos);
        _finder.AddElement("Entities\\Base", baseEntity);

        var derivedNos = new NamedObjectSave { InstanceName = "SpriteInstance", DefinedByBase = true };
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.Same(baseNos, derivedNos.GetDefiningNamedObjectSave(derivedEntity));
    }

    [Fact]
    public void GetDefiningNamedObjectSave_DefinedByBase_NoMatchAnywhere_ReturnsNull()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        _finder.AddElement("Entities\\Base", baseEntity);

        var derivedNos = new NamedObjectSave { InstanceName = "SpriteInstance", DefinedByBase = true };
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.Null(derivedNos.GetDefiningNamedObjectSave(derivedEntity));
    }

    [Fact]
    public void InheritsFrom_DirectBase_ReturnsTrue()
    {
        var instance = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(instance.InheritsFrom("Entities\\Base"));
    }

    [Fact]
    public void InheritsFrom_IndirectBase_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base", BaseEntity = "Entities\\Root" };
        _finder.AddElement("Entities\\Base", baseEntity);
        var instance = new EntitySave { BaseEntity = "Entities\\Base" };

        Assert.True(instance.InheritsFrom("Entities\\Root"));
    }

    [Fact]
    public void InheritsFrom_NoMatch_ReturnsFalse()
    {
        var instance = new EntitySave { BaseEntity = "" };

        Assert.False(instance.InheritsFrom("Entities\\Root"));
    }

    [Fact]
    public void IsICollidableRecursive_NotEntitySave_ReturnsFalse()
    {
        IElement screen = new ScreenSave { Name = "Screens\\GameScreen" };

        Assert.False(screen.IsICollidableRecursive());
    }

    [Fact]
    public void IsICollidableRecursive_ImplementsDirectly_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsICollidable = true };

        Assert.True(((IElement)entity).IsICollidableRecursive());
    }

    [Fact]
    public void IsICollidableRecursive_BaseImplements_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsICollidable = false };
        var baseEntity = new EntitySave { ImplementsICollidable = true };
        _finder.SetBaseElements(entity, new List<GlueElement> { baseEntity });

        Assert.True(((IElement)entity).IsICollidableRecursive());
    }

    [Fact]
    public void IsICollidableRecursive_NeitherImplements_ReturnsFalse()
    {
        var entity = new EntitySave { ImplementsICollidable = false };
        var baseEntity = new EntitySave { ImplementsICollidable = false };
        _finder.SetBaseElements(entity, new List<GlueElement> { baseEntity });

        Assert.False(((IElement)entity).IsICollidableRecursive());
    }

    [Fact]
    public void CanBeInList_MatchingSourceClassType_ReturnsTrue()
    {
        var instance = new NamedObjectSave { SourceClassType = "Sprite" };
        var listNos = new NamedObjectSave { SourceClassGenericType = "Sprite" };

        Assert.True(instance.CanBeInList(listNos));
    }

    [Fact]
    public void CanBeInList_MatchingInstanceType_ReturnsTrue()
    {
        // InstanceType is computed; for SourceType.Entity it's SourceClassType with the "Entities\"
        // path stripped, so this is the one case where InstanceType and SourceClassType differ.
        var instance = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Sprite" };
        var listNos = new NamedObjectSave { SourceClassGenericType = "Sprite" };

        Assert.True(instance.CanBeInList(listNos));
    }

    [Fact]
    public void CanBeInList_EntityInheritsFromListType_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        _finder.AddElement("Entities\\Derived", entity);
        _finder.AddElement("Entities\\Base", baseEntity);

        var instance = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Derived" };
        var listNos = new NamedObjectSave { SourceClassGenericType = "Entities\\Base" };

        Assert.True(instance.CanBeInList(listNos));
    }

    [Fact]
    public void CanBeInList_NoMatch_ReturnsFalse()
    {
        var instance = new NamedObjectSave { SourceClassType = "Sprite" };
        var listNos = new NamedObjectSave { SourceClassGenericType = "Circle" };

        Assert.False(instance.CanBeInList(listNos));
    }

    [Fact]
    public void IsCollidableOrCollidableList_ListOfCollidableEntity_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsICollidable = true };
        _finder.AddElement("Entities\\Enemy", entity);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Entities\\Enemy"
        };

        Assert.True(nos.IsCollidableOrCollidableList());
    }

    [Fact]
    public void IsCollidableOrCollidableList_ListOfNonCollidableEntity_ReturnsFalse()
    {
        var entity = new EntitySave { ImplementsICollidable = false };
        _finder.AddElement("Entities\\NonCollidable", entity);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Entities\\NonCollidable"
        };

        Assert.False(nos.IsCollidableOrCollidableList());
    }

    [Fact]
    public void IsCollidableOrCollidableList_ShapeCollectionRuntimeType_ReturnsTrue()
    {
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "ShapeCollection" } };
        _availableAssetTypes.AddAssetType(ati);

        var nos = new NamedObjectSave
        {
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "ShapeCollection"
        };

        Assert.True(nos.IsCollidableOrCollidableList());
    }

    [Fact]
    public void IsCollidableOrCollidableList_CollidableEntityInstance_ReturnsTrue()
    {
        var entity = new EntitySave { ImplementsICollidable = true };
        _finder.AddElement("Entities\\Enemy", entity);

        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Enemy" };

        Assert.True(nos.IsCollidableOrCollidableList());
    }

    [Fact]
    public void IsCollidableOrCollidableList_NoMatch_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite" };

        Assert.False(nos.IsCollidableOrCollidableList());
    }

    #region GetNamedObjectRecursively

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetNamedObjectRecursively_NoName_ReturnsNull(string name)
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });

        Assert.Null(entity.GetNamedObjectRecursively(name));
    }

    [Fact]
    public void GetNamedObjectRecursively_FoundOnContainer_ReturnsIt()
    {
        var nos = new NamedObjectSave { InstanceName = "Sprite" };
        var entity = new EntitySave();
        entity.NamedObjects.Add(nos);

        Assert.Same(nos, entity.GetNamedObjectRecursively("Sprite"));
    }

    [Fact]
    public void GetNamedObjectRecursively_FoundInContainedList_ReturnsIt()
    {
        var contained = new NamedObjectSave { InstanceName = "Bullet1" };
        var list = new NamedObjectSave { InstanceName = "Bullets" };
        list.ContainedObjects.Add(contained);
        var entity = new EntitySave();
        entity.NamedObjects.Add(list);

        Assert.Same(contained, entity.GetNamedObjectRecursively("Bullet1"));
    }

    [Fact]
    public void GetNamedObjectRecursively_NotFoundAndNoBase_ReturnsNull()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });

        Assert.Null(entity.GetNamedObjectRecursively("Missing"));
    }

    [Fact]
    public void GetNamedObjectRecursively_EntityBaseHasIt_ReturnsBaseObject()
    {
        var baseNos = new NamedObjectSave { InstanceName = "Sprite" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(baseNos);
        _finder.AddElement("Entities\\Base", baseEntity);
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.Same(baseNos, derived.GetNamedObjectRecursively("Sprite"));
    }

    [Fact]
    public void GetNamedObjectRecursively_EntityBaseMissing_ReturnsNull()
    {
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Gone" };

        Assert.Null(derived.GetNamedObjectRecursively("Sprite"));
    }

    [Fact]
    public void GetNamedObjectRecursively_ScreenBaseHasIt_ReturnsBaseObject()
    {
        var baseNos = new NamedObjectSave { InstanceName = "Layer" };
        var baseScreen = new ScreenSave { Name = "Screens\\Base" };
        baseScreen.NamedObjects.Add(baseNos);
        _finder.AddElement("Screens\\Base", baseScreen);
        var derived = new ScreenSave { Name = "Screens\\Derived", BaseScreen = "Screens\\Base" };

        Assert.Same(baseNos, derived.GetNamedObjectRecursively("Layer"));
    }

    [Fact]
    public void GetNamedObjectRecursively_ScreenBaseMissing_ReturnsNull()
    {
        var derived = new ScreenSave { Name = "Screens\\Derived", BaseScreen = "Screens\\Gone" };

        Assert.Null(derived.GetNamedObjectRecursively("Layer"));
    }

    [Fact]
    public void GetNamedObjectRecursively_TwoLevelsUp_ReturnsGrandparentObject()
    {
        var rootNos = new NamedObjectSave { InstanceName = "Sprite" };
        var root = new EntitySave { Name = "Entities\\Root" };
        root.NamedObjects.Add(rootNos);
        var middle = new EntitySave { Name = "Entities\\Middle", BaseEntity = "Entities\\Root" };
        _finder.AddElement("Entities\\Root", root);
        _finder.AddElement("Entities\\Middle", middle);
        var leaf = new EntitySave { Name = "Entities\\Leaf", BaseEntity = "Entities\\Middle" };

        Assert.Same(rootNos, leaf.GetNamedObjectRecursively("Sprite"));
    }

    [Fact]
    public void GetNamedObjectRecursively_DerivedShadowsBase_ReturnsDerivedObject()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var derivedNos = new NamedObjectSave { InstanceName = "Sprite" };
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        derived.NamedObjects.Add(derivedNos);

        Assert.Same(derivedNos, derived.GetNamedObjectRecursively("Sprite"));
    }

    #endregion

    #region DoesMemberNeedToBeSetByContainer (NamedObjectSave)

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_NotEntity_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite" };

        Assert.False(nos.DoesMemberNeedToBeSetByContainer("X"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_EntityNotFound_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Gone" };

        Assert.False(nos.DoesMemberNeedToBeSetByContainer("X"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_EntityMemberSetByContainer_ReturnsTrue()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite", SetByContainer = true });
        _finder.AddElement("Entities\\Player", entity);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };

        Assert.True(nos.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_EntityMemberNotSetByContainer_ReturnsFalse()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite", SetByContainer = false });
        _finder.AddElement("Entities\\Player", entity);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };

        Assert.False(nos.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    #endregion

    #region GetIsScalableEntity

    [Fact]
    public void GetIsScalableEntity_NotEntity_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite" };

        Assert.False(nos.GetIsScalableEntity());
    }

    [Fact]
    public void GetIsScalableEntity_EntityWithNoSourceClassType_ReturnsFalse()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "" };

        Assert.False(nos.GetIsScalableEntity());
    }

    [Fact]
    public void GetIsScalableEntity_EntityWithScaleXAndScaleY_ReturnsTrue()
    {
        var entity = new EntitySave { Name = "Entities\\Box" };
        entity.CustomVariables.Add(new CustomVariable { Name = "ScaleX" });
        entity.CustomVariables.Add(new CustomVariable { Name = "ScaleY" });
        _finder.AddElement("Entities\\Box", entity);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Box" };

        Assert.True(nos.GetIsScalableEntity());
    }

    [Fact]
    public void GetIsScalableEntity_EntityWithOnlyScaleX_ReturnsFalse()
    {
        var entity = new EntitySave { Name = "Entities\\Box" };
        entity.CustomVariables.Add(new CustomVariable { Name = "ScaleX" });
        _finder.AddElement("Entities\\Box", entity);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Box" };

        Assert.False(nos.GetIsScalableEntity());
    }

    [Fact]
    public void GetIsScalableEntity_ScaleVariablesOnBase_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "ScaleX" });
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "ScaleY" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        _finder.AddElement("Entities\\Derived", derived);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Derived" };

        Assert.True(nos.GetIsScalableEntity());
    }

    #endregion
}

[CollectionDefinition(nameof(ObjectFinderCoreCollection), DisableParallelization = true)]
public class ObjectFinderCoreCollection
{
}
