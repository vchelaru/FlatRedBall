using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;

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

    #region GetAdditionsNeededForChangingType

    static (List<PropertyValuePair> values, List<CustomVariable> variables, List<StateSave> states, List<StateSaveCategory> categories)
        GetAdditions(string oldType, string newType)
    {
        var values = new List<PropertyValuePair>();
        var variables = new List<CustomVariable>();
        var states = new List<StateSave>();
        var categories = new List<StateSaveCategory>();
        NamedObjectSaveElementExtensions.GetAdditionsNeededForChangingType(oldType, newType, values, variables, states, categories);
        return (values, variables, states, categories);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_UnknownOldOrNewType_AddsNothing()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        old.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        _finder.AddElement("Entities\\Old", old);

        var (values, variables, states, categories) = GetAdditions("Entities\\Old", "Entities\\Missing");

        Assert.Empty(values);
        Assert.Empty(variables);
        Assert.Empty(states);
        Assert.Empty(categories);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_VariableMissingFromNewType_Added()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var speed = new CustomVariable { Name = "Speed", Type = "float" };
        old.CustomVariables.Add(speed);
        _finder.AddElement("Entities\\Old", old);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });

        var (_, variables, _, _) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Same(speed, Assert.Single(variables));
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_VariableWithDifferentTypeInNewType_Added()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        old.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        _finder.AddElement("Entities\\Old", old);
        var @new = new EntitySave { Name = "Entities\\New" };
        @new.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "int" });
        _finder.AddElement("Entities\\New", @new);

        var (_, variables, _, _) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Single(variables);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_VariableDefinedOnNewTypesBase_NotAdded()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        old.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        _finder.AddElement("Entities\\Old", old);
        var newBase = new EntitySave { Name = "Entities\\NewBase" };
        newBase.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        _finder.AddElement("Entities\\NewBase", newBase);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New", BaseEntity = "Entities\\NewBase" });

        var (_, variables, _, _) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Empty(variables);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_InterfacesLostOnSwitch_AddedAsValuesToSet()
    {
        _finder.AddElement("Entities\\Old", new EntitySave
        {
            Name = "Entities\\Old",
            ImplementsIClickable = true,
            ImplementsIVisible = true,
            ImplementsIWindow = true,
            ImplementsITiledTileMetadata = true
        });
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New", ImplementsIVisible = true });

        var (values, _, _, _) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Equal(
            new[] { "ImplementsIClickable", "ImplementsIWindow", "ImplementsITiledTileMetadata" },
            values.Select(item => item.Property));
        Assert.All(values, item => Assert.Equal(true, item.Value));
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_ScreenToScreen_SkipsInterfaceComparison()
    {
        _finder.AddElement("Screens\\Old", new ScreenSave { Name = "Screens\\Old" });
        _finder.AddElement("Screens\\New", new ScreenSave { Name = "Screens\\New" });

        var (values, _, _, _) = GetAdditions("Screens\\Old", "Screens\\New");

        Assert.Empty(values);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_UncategorizedStateMissingFromNewType_Added()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var idle = new StateSave { Name = "Idle" };
        old.States.Add(idle);
        old.States.Add(new StateSave { Name = "Running" });
        _finder.AddElement("Entities\\Old", old);
        var @new = new EntitySave { Name = "Entities\\New" };
        @new.States.Add(new StateSave { Name = "Running" });
        _finder.AddElement("Entities\\New", @new);

        var (_, _, states, _) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Same(idle, Assert.Single(states));
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_CategoryMissingFromNewType_AddedWithItsStates()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var category = new StateSaveCategory { Name = "Movement" };
        category.States.Add(new StateSave { Name = "Walk" });
        old.StateCategoryList.Add(category);
        _finder.AddElement("Entities\\Old", old);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });

        var (_, _, _, categories) = GetAdditions("Entities\\Old", "Entities\\New");

        var needed = categories.First(item => item.Name == "Movement");
        Assert.NotSame(category, needed);
        Assert.Equal("Walk", Assert.Single(needed.States).Name);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_StateMissingFromExistingCategory_AddsCategoryWithJustThatState()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var oldCategory = new StateSaveCategory { Name = "Movement" };
        oldCategory.States.Add(new StateSave { Name = "Walk" });
        oldCategory.States.Add(new StateSave { Name = "Run" });
        old.StateCategoryList.Add(oldCategory);
        _finder.AddElement("Entities\\Old", old);
        var @new = new EntitySave { Name = "Entities\\New" };
        var newCategory = new StateSaveCategory { Name = "Movement" };
        newCategory.States.Add(new StateSave { Name = "Walk" });
        @new.StateCategoryList.Add(newCategory);
        _finder.AddElement("Entities\\New", @new);

        var (_, _, _, categories) = GetAdditions("Entities\\Old", "Entities\\New");

        var needed = Assert.Single(categories);
        Assert.Equal("Movement", needed.Name);
        Assert.Equal("Run", Assert.Single(needed.States).Name);
    }

    [Fact]
    public void GetAdditionsNeededForChangingType_CategoryFullyPresentInNewType_NotAdded()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var oldCategory = new StateSaveCategory { Name = "Movement" };
        oldCategory.States.Add(new StateSave { Name = "Walk" });
        old.StateCategoryList.Add(oldCategory);
        _finder.AddElement("Entities\\Old", old);
        var @new = new EntitySave { Name = "Entities\\New" };
        var newCategory = new StateSaveCategory { Name = "Movement" };
        newCategory.States.Add(new StateSave { Name = "Walk" });
        @new.StateCategoryList.Add(newCategory);
        _finder.AddElement("Entities\\New", @new);

        var (_, _, _, categories) = GetAdditions("Entities\\Old", "Entities\\New");

        Assert.Empty(categories);
    }

    #endregion

    #region GetMessageWhySwitchMightCauseProblems

    [Fact]
    public void GetMessageWhySwitchMightCauseProblems_NothingMissing_ReturnsNull()
    {
        _finder.AddElement("Entities\\Old", new EntitySave { Name = "Entities\\Old" });
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\New" };

        Assert.Null(nos.GetMessageWhySwitchMightCauseProblems("Entities\\Old"));
    }

    [Fact]
    public void GetMessageWhySwitchMightCauseProblems_MissingVariable_NamesVariableAndType()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        old.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        _finder.AddElement("Entities\\Old", old);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\New" };

        var message = nos.GetMessageWhySwitchMightCauseProblems("Entities\\Old");

        Assert.Contains("Entities\\New is missing the following variables", message);
        Assert.Contains("Speed (float)", message);
    }

    [Fact]
    public void GetMessageWhySwitchMightCauseProblems_MissingInterface_NamesProperty()
    {
        _finder.AddElement("Entities\\Old", new EntitySave { Name = "Entities\\Old", ImplementsIClickable = true });
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\New" };

        var message = nos.GetMessageWhySwitchMightCauseProblems("Entities\\Old");

        Assert.Contains("is missing the following properties", message);
        Assert.Contains("ImplementsIClickable", message);
    }

    [Fact]
    public void GetMessageWhySwitchMightCauseProblems_MissingUncategorizedState_NamesState()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        old.States.Add(new StateSave { Name = "Idle" });
        _finder.AddElement("Entities\\Old", old);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\New" };

        var message = nos.GetMessageWhySwitchMightCauseProblems("Entities\\Old");

        Assert.Contains("is missing the following states", message);
        Assert.Contains("Idle (Uncategorized)", message);
    }

    [Fact]
    public void GetMessageWhySwitchMightCauseProblems_MissingCategoryAndCategorizedState_NamesBoth()
    {
        var old = new EntitySave { Name = "Entities\\Old" };
        var category = new StateSaveCategory { Name = "Movement" };
        category.States.Add(new StateSave { Name = "Walk" });
        old.StateCategoryList.Add(category);
        old.StateCategoryList.Add(new StateSaveCategory { Name = "Empty" });
        _finder.AddElement("Entities\\Old", old);
        _finder.AddElement("Entities\\New", new EntitySave { Name = "Entities\\New" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\New" };

        var message = nos.GetMessageWhySwitchMightCauseProblems("Entities\\Old");

        Assert.Contains("Walk (in category Movement)", message);
        Assert.Contains("Empty (Category) is missing", message);
    }

    #endregion

    #region SetVariable

    [Fact]
    public void SetVariable_ExistingInstruction_SetsValueInPlace()
    {
        var nos = new NamedObjectSave();
        var existing = new CustomVariableInNamedObject { Member = "X", Type = "float", Value = 1f };
        nos.InstructionSaves.Add(existing);

        nos.SetVariable("X", 2f);

        Assert.Same(existing, Assert.Single(nos.InstructionSaves));
        Assert.Equal(2f, existing.Value);
    }

    [Fact]
    public void SetVariable_VariableDefinedOnAssetTypeInfo_AddsInstructionWithDefinedType()
    {
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Sprite" } };
        ati.VariableDefinitions.Add(new VariableDefinition { Name = "Alpha", Type = "float" });
        _availableAssetTypes.AddAssetType(ati);
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };

        nos.SetVariable("Alpha", 0.5f);

        var instruction = Assert.Single(nos.InstructionSaves);
        Assert.Equal("Alpha", instruction.Member);
        Assert.Equal("float", instruction.Type);
        Assert.Equal(0.5f, instruction.Value);
    }

    [Fact]
    public void SetVariable_VariableDefinedOnSourceEntity_AddsInstructionWithEntityVariableType()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "int" });
        _finder.AddElement("Entities\\Player", entity);
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };

        // Pass a float so the instruction's type can only have come from the entity's variable.
        nos.SetVariable("Speed", 3f);

        var instruction = Assert.Single(nos.InstructionSaves);
        Assert.Equal("int", instruction.Type);
        Assert.Equal(3f, instruction.Value);
    }

    [Fact]
    public void SetVariable_VariableDefinedOnSourceEntitysBase_AddsInstructionWithBaseVariableType()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "int" });
        _finder.AddElement("Entities\\Base", baseEntity);
        _finder.AddElement("Entities\\Player", new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Base" });
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Player" };

        nos.SetVariable("Speed", 3f);

        Assert.Equal("int", Assert.Single(nos.InstructionSaves).Type);
    }

    [Fact]
    public void SetVariable_UnknownVariable_AddsInstructionTypedFromValue()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.Entity, SourceClassType = "Entities\\Missing" };

        nos.SetVariable("Anything", "text");

        var instruction = Assert.Single(nos.InstructionSaves);
        Assert.Equal("Anything", instruction.Member);
        Assert.Equal("string", instruction.Type);
        Assert.Equal("text", instruction.Value);
    }

    #endregion
}

[CollectionDefinition(nameof(ObjectFinderCoreCollection), DisableParallelization = true)]
public class ObjectFinderCoreCollection
{
}
