using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// Several of these methods read the shared static ObjectFinderCore.Self/AvailableAssetTypesCore.Self,
// so this can't run concurrently with any other test class that swaps them out.
[Collection(nameof(ObjectFinderCoreCollection))]
public class ElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();

    public ElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
    }

    [Fact]
    public void GetCustomVariableRecursively_FoundOnInstance_ReturnsIt()
    {
        var variable = new CustomVariable { Name = "Health" };
        var entity = new EntitySave();
        entity.CustomVariables.Add(variable);

        Assert.Same(variable, entity.GetCustomVariableRecursively("Health"));
    }

    [Fact]
    public void GetCustomVariableRecursively_FoundOnBase_ReturnsIt()
    {
        var variable = new CustomVariable { Name = "Health" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(variable);
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(variable, entity.GetCustomVariableRecursively("Health"));
    }

    [Fact]
    public void GetCustomVariableRecursively_NotFound_ReturnsNull()
    {
        var entity = new EntitySave();

        Assert.Null(entity.GetCustomVariableRecursively("Health"));
    }

    [Fact]
    public void GetCustomVariablesToBeSetByDerived_IncludesOwnAndBaseSetByDerivedVariables()
    {
        var baseVariable = new CustomVariable { Name = "BaseVar", SetByDerived = true };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(baseVariable);
        _finder.AddElement("Entities\\Base", baseEntity);

        var ownVariable = new CustomVariable { Name = "OwnVar", SetByDerived = true };
        var notSetByDerived = new CustomVariable { Name = "NotSetByDerived", SetByDerived = false };
        var entity = new EntitySave { BaseObject = "Entities\\Base" };
        entity.CustomVariables.Add(ownVariable);
        entity.CustomVariables.Add(notSetByDerived);

        var result = entity.GetCustomVariablesToBeSetByDerived();

        Assert.Equal(new[] { baseVariable, ownVariable }, result);
    }

    [Fact]
    public void ContainsCustomVariable_Present_ReturnsTrue()
    {
        var entity = new EntitySave();
        entity.CustomVariables.Add(new CustomVariable { Name = "Health" });

        Assert.True(entity.ContainsCustomVariable("Health"));
    }

    [Fact]
    public void ContainsCustomVariable_Absent_ReturnsFalse()
    {
        var entity = new EntitySave();

        Assert.False(entity.ContainsCustomVariable("Health"));
    }

    [Fact]
    public void ContainsCustomVariableRecursively_OnBase_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "Health" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.True(entity.ContainsCustomVariableRecursively("Health"));
    }

    [Fact]
    public void ContainsCustomVariableRecursively_NeitherHasIt_ReturnsFalse()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.False(entity.ContainsCustomVariableRecursively("Health"));
    }

    [Fact]
    public void GetEventsOnVariable_ReturnsOnlyMatchingEvents()
    {
        var matching = new EventResponseSave { SourceVariable = "Health" };
        var nonMatching = new EventResponseSave { SourceVariable = "Position" };
        var entity = new EntitySave();
        entity.Events.Add(matching);
        entity.Events.Add(nonMatching);

        var result = entity.GetEventsOnVariable("Health");

        Assert.Equal(new[] { matching }, result);
    }

    [Fact]
    public void GetState_Uncategorized_FindsByName()
    {
        var state = new StateSave { Name = "Idle" };
        var entity = new EntitySave();
        entity.States.Add(state);

        Assert.Same(state, entity.GetState("Idle"));
    }

    [Fact]
    public void GetState_InCategory_FindsByNameAndCategory()
    {
        var state = new StateSave { Name = "Walking" };
        var category = new StateSaveCategory { Name = "Movement" };
        category.States.Add(state);
        var entity = new EntitySave();
        entity.StateCategoryList.Add(category);

        Assert.Same(state, entity.GetState("Walking", "Movement"));
    }

    [Fact]
    public void GetStateRecursively_NotOnInstance_FindsOnBase()
    {
        var state = new StateSave { Name = "Idle" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.States.Add(state);
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(state, entity.GetStateRecursively("Idle"));
    }

    [Fact]
    public void GetUncategorizedStateRecursively_NotOnInstance_FindsOnBase()
    {
        var state = new StateSave { Name = "Idle" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.States.Add(state);
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(state, entity.GetUncategorizedStateRecursively("Idle"));
    }

    [Fact]
    public void GetUncategorizedStatesRecursively_OwnStatesPresent_ReturnsOwnStates()
    {
        var entity = new EntitySave();
        var state = new StateSave { Name = "Idle" };
        entity.States.Add(state);

        Assert.Same(entity.States, entity.GetUncategorizedStatesRecursively());
    }

    [Fact]
    public void GetUncategorizedStatesRecursively_NoOwnStates_FallsBackToBase()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.States.Add(new StateSave { Name = "Idle" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(baseEntity.States, entity.GetUncategorizedStatesRecursively());
    }

    [Fact]
    public void GetStateCategoryRecursively_NotOnInstance_FindsOnBase()
    {
        var category = new StateSaveCategory { Name = "Movement" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.StateCategoryList.Add(category);
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(category, entity.GetStateCategoryRecursively("Movement"));
    }

    [Fact]
    public void DefinesCategoryEnumRecursive_VariableState_ChecksUncategorizedStates()
    {
        var entity = new EntitySave();
        entity.States.Add(new StateSave { Name = "Idle" });

        Assert.True(entity.DefinesCategoryEnumRecursive("VariableState"));
    }

    [Fact]
    public void DefinesCategoryEnumRecursive_NamedCategory_FoundOnBase()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.StateCategoryList.Add(new StateSaveCategory { Name = "Movement" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.True(entity.DefinesCategoryEnumRecursive("Movement"));
    }

    [Fact]
    public void GetAllNamedObjectsRecurisvely_IncludesOwnAndBaseNamedObjects()
    {
        var ownNos = new NamedObjectSave { InstanceName = "Own" };
        var entity = new EntitySave();
        entity.NamedObjects.Add(ownNos);

        var baseNos = new NamedObjectSave { InstanceName = "FromBase" };
        var baseEntity = new EntitySave();
        baseEntity.NamedObjects.Add(baseNos);
        _finder.SetBaseElements(entity, new List<GlueElement> { baseEntity });

        var result = entity.GetAllNamedObjectsRecurisvely().ToList();

        Assert.Equal(new[] { ownNos, baseNos }, result);
    }

    [Fact]
    public void GetAllReferencedFileSavesRecursively_IncludesOwnAndBaseFiles()
    {
        var ownFile = new ReferencedFileSave { Name = "Own.png" };
        var entity = new EntitySave { BaseObject = "Entities\\Base" };
        entity.ReferencedFiles.Add(ownFile);

        var baseFile = new ReferencedFileSave { Name = "Base.png" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.ReferencedFiles.Add(baseFile);
        _finder.AddElement("Entities\\Base", baseEntity);

        var result = entity.GetAllReferencedFileSavesRecursively().ToList();

        Assert.Equal(new[] { ownFile, baseFile }, result);
    }

    [Fact]
    public void GetQualifiedName_CombinesProjectNameAndBackslashPath()
    {
        var entity = new EntitySave { Name = "Entities\\Sub\\Player" };

        Assert.Equal("MyProject.Entities.Sub.Player", entity.GetQualifiedName("MyProject"));
    }

    [Theory]
    [InlineData("Entities\\Base", true)]
    [InlineData("Screens\\Base", true)]
    [InlineData("SomeFrbType", false)]
    [InlineData(null, false)]
    public void InheritsFromElement_ChecksBaseElementPrefix(string baseElement, bool expected)
    {
        var entity = new EntitySave { BaseObject = baseElement };

        Assert.Equal(expected, entity.InheritsFromElement());
    }

    [Fact]
    public void InheritsFromEntity_ScreenSave_AlwaysFalse()
    {
        IElement screen = new ScreenSave { BaseObject = "Entities\\Base" };

        Assert.False(screen.InheritsFromEntity());
    }

    [Fact]
    public void InheritsFromEntity_EntityWithEntityBase_ReturnsTrue()
    {
        IElement entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.True(entity.InheritsFromEntity());
    }

    [Fact]
    public void InheritsFromFrbType_ScreenSave_AlwaysFalse()
    {
        IElement screen = new ScreenSave { BaseObject = null };

        Assert.False(screen.InheritsFromFrbType());
    }

    [Fact]
    public void InheritsFromFrbType_NoBaseElement_ReturnsFalse()
    {
        IElement entity = new EntitySave { BaseObject = "" };

        Assert.False(entity.InheritsFromFrbType());
    }

    [Fact]
    public void InheritsFromFrbType_BaseElementIsFrbType_ReturnsTrue()
    {
        IElement entity = new EntitySave { BaseObject = "Sprite" };

        Assert.True(entity.InheritsFromFrbType());
    }

    [Fact]
    public void GetAssetTypeInfo_ScreenSave_ReturnsScreenAti()
    {
        var screenAti = new AssetTypeInfo();
        _availableAssetTypes.Screen = screenAti;
        IElement screen = new ScreenSave();

        Assert.Same(screenAti, screen.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_EntityWithBaseEntity_RecursesToBase()
    {
        // The base entity has no BaseElement of its own, so its own GetAssetTypeInfo() call falls into
        // the "no BaseElement" branch and resolves the ATI by matching its Name.
        var baseAti = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } };
        var baseEntity = new EntitySave { Name = "Sprite" };
        _finder.AddElement("Entities\\Base", baseEntity);
        _availableAssetTypes.AddAssetType(baseAti);

        IElement entity = new EntitySave { BaseObject = "Entities\\Base" };

        Assert.Same(baseAti, entity.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_NoBaseElement_MatchesByElementName()
    {
        var ati = new AssetTypeInfo { QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } };
        _availableAssetTypes.AddAssetType(ati);
        IElement entity = new EntitySave { Name = "Sprite", BaseObject = null };

        Assert.Same(ati, entity.GetAssetTypeInfo());
    }
}
