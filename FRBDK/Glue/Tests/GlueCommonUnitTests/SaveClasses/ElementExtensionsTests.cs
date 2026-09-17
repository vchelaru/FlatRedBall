using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueCommonUnitTests.Parsing;

namespace GlueCommonUnitTests.SaveClasses;

// Several of these methods read the shared static ObjectFinderCore.Self/AvailableAssetTypesCore.Self/
// TypeResolutionCore.Self/PluginManagerCore.Self, so this can't run concurrently with any other test
// class that swaps them out.
[Collection(nameof(ObjectFinderCoreCollection))]
public class ElementExtensionsTests
{
    enum TestEnum { First = 0, Second = 1, Third = 2 }

    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();
    readonly FakeTypeResolutionCore _typeResolution = new();
    readonly FakePluginManagerCore _plugins = new();

    public ElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
        TypeResolutionCore.Self = _typeResolution;
        PluginManagerCore.Self = _plugins;
        _finder.GlueProject = new GlueProjectSave();
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
    #region DoesMemberNeedToBeSetByContainer (IElement / ScreenSave)

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_MemberSetByContainer_ReturnsTrue()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite", SetByContainer = true });

        Assert.True(entity.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_MemberNotSetByContainerAndNoBase_ReturnsFalse()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite", SetByContainer = false });

        Assert.False(entity.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_MemberMissingAndNoBase_ReturnsFalse()
    {
        var entity = new EntitySave();

        Assert.False(entity.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_SetByContainerOnBaseEntity_ReturnsTrue()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite", SetByContainer = true });
        _finder.AddElement("Entities\\Base", baseEntity);
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };

        Assert.True(derived.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_InheritsFromFrbType_ReturnsFalse()
    {
        var entity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "FlatRedBall.Sprite" };

        Assert.False(entity.DoesMemberNeedToBeSetByContainer("Sprite"));
    }

    [Fact]
    public void DoesMemberNeedToBeSetByContainer_ScreenOverload_UsesSameLogic()
    {
        var screen = new ScreenSave();
        screen.NamedObjects.Add(new NamedObjectSave { InstanceName = "Layer", SetByContainer = true });

        Assert.True(screen.DoesMemberNeedToBeSetByContainer("Layer"));
    }

    #endregion

    #region ReactToRenamedReferencedFile

    [Fact]
    public void ReactToRenamedReferencedFile_MatchingSourceFile_RenamesAndReturnsTrue()
    {
        var nos = new NamedObjectSave { SourceFile = "Old.scnx" };
        var entity = new EntitySave();
        entity.NamedObjects.Add(nos);

        Assert.True(entity.ReactToRenamedReferencedFile("Old.scnx", "New.scnx"));
        Assert.Equal("New.scnx", nos.SourceFile);
    }

    [Fact]
    public void ReactToRenamedReferencedFile_NoMatch_ReturnsFalseAndLeavesSourceFile()
    {
        var nos = new NamedObjectSave { SourceFile = "Other.scnx" };
        var entity = new EntitySave();
        entity.NamedObjects.Add(nos);

        Assert.False(entity.ReactToRenamedReferencedFile("Old.scnx", "New.scnx"));
        Assert.Equal("Other.scnx", nos.SourceFile);
    }

    #endregion

    #region PostLoadInitialize

    [Fact]
    public void PostLoadInitialize_RemovesNullValuedInstructionsFromNamedObjects()
    {
        var nos = new NamedObjectSave();
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "X", Value = null });
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Y", Value = 1f });
        var entity = new EntitySave();
        entity.NamedObjects.Add(nos);

        entity.PostLoadInitialize();

        Assert.Single(nos.InstructionSaves);
        Assert.Equal("Y", nos.InstructionSaves[0].Member);
    }

    [Fact]
    public void PostLoadInitialize_ReachesContainedNamedObjects()
    {
        var contained = new NamedObjectSave();
        contained.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "X", Value = null });
        var list = new NamedObjectSave();
        list.ContainedObjects.Add(contained);
        var entity = new EntitySave();
        entity.NamedObjects.Add(list);

        entity.PostLoadInitialize();

        Assert.Empty(contained.InstructionSaves);
    }

    [Fact]
    public void PostLoadInitialize_NonEnumCustomVariable_LeavesDefaultValue()
    {
        var variable = new CustomVariable { Name = "Health", Type = "float", DefaultValue = 3f };
        var entity = new EntitySave();
        entity.CustomVariables.Add(variable);

        entity.PostLoadInitialize();

        Assert.Equal(3f, variable.DefaultValue);
    }

    #endregion

    #region FixAllTypes / FixEnumerationValues / ConvertEnumerationValuesToInts

    // One item of each kind an element owns, so a regression in any of the four dispatch loops shows up.
    static EntitySave BuildEntityWithOneOfEach(out NamedObjectSave nos, out StateSave state, out CustomVariable variable, out ReferencedFileSave file)
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "X", Type = "float", Value = 1 });
        entity.NamedObjects.Add(nos);
        state = new StateSave { Name = "Alive" };
        state.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave { Member = "Health", Type = "float", Value = 2 });
        entity.States.Add(state);
        variable = new CustomVariable { Name = "Health", Type = "float", DefaultValue = 3 };
        entity.CustomVariables.Add(variable);
        file = new ReferencedFileSave { Name = "Player.png" };
        file.Properties.Add(new PropertySave { Name = "Scale", Type = "float", Value = 4 });
        entity.ReferencedFiles.Add(file);
        return entity;
    }

    [Fact]
    public void FixAllTypes_Element_FixesNamedObjectsStatesVariablesAndFiles()
    {
        _typeResolution.AddType("float", typeof(float));
        var entity = BuildEntityWithOneOfEach(out var nos, out var state, out var variable, out var file);

        entity.FixAllTypes();

        Assert.Equal(1f, nos.InstructionSaves[0].Value);
        Assert.Equal(2f, state.InstructionSaves[0].Value);
        Assert.Equal(3f, variable.DefaultValue);
        Assert.Equal(4f, file.Properties[0].Value);
    }

    [Fact]
    public void FixAllTypes_Element_ReachesPluginSeamForPreferredDisplayer()
    {
        var entity = new EntitySave();
        var variable = new CustomVariable
        {
            Name = "Speed",
            Type = "float",
            VariableDefinition = new VariableDefinition { PreferredDisplayerName = "SliderDisplay" },
        };
        entity.CustomVariables.Add(variable);

        entity.FixAllTypes();

        Assert.Same(variable, Assert.Single(_plugins.DisplayerRequests));
    }

    [Fact]
    public void FixEnumerationValues_ConvertsIntsOnNamedObjectsStatesAndVariables()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var entity = new EntitySave();
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Mode", Type = "TestEnum", Value = 1 });
        entity.NamedObjects.Add(nos);
        var state = new StateSave { Name = "Alive" };
        state.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave { Member = "Mode", Type = "TestEnum", Value = 2 });
        entity.States.Add(state);
        var variable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = 1 };
        entity.CustomVariables.Add(variable);

        entity.FixEnumerationValues();

        Assert.Equal(TestEnum.Second, nos.InstructionSaves[0].Value);
        Assert.Equal(TestEnum.Third, state.InstructionSaves[0].Value);
        Assert.Equal(TestEnum.Second, variable.DefaultValue);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_ConvertsEnumsOnNamedObjectsStatesAndVariables()
    {
        var entity = new EntitySave();
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        nos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Mode", Type = "TestEnum", Value = TestEnum.Second });
        entity.NamedObjects.Add(nos);
        var state = new StateSave { Name = "Alive" };
        state.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave { Member = "Mode", Type = "TestEnum", Value = TestEnum.Third });
        entity.States.Add(state);
        var variable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = TestEnum.Second };
        entity.CustomVariables.Add(variable);

        entity.ConvertEnumerationValuesToInts();

        Assert.Equal(1, nos.InstructionSaves[0].Value);
        Assert.Equal(2, state.InstructionSaves[0].Value);
        Assert.Equal(1, variable.DefaultValue);
    }

    #endregion

    #region GetReferencedFileSaveRecursively / GetReferencedFileSaveByInstanceName(Recursively)

    EntitySave AddBaseEntityWithFile(string fileName, out ReferencedFileSave rfs)
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        rfs = new ReferencedFileSave { Name = fileName };
        baseEntity.ReferencedFiles.Add(rfs);
        _finder.GlueProject.Entities.Add(baseEntity);
        return baseEntity;
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_ByName_FoundOnInstance_ReturnsIt()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Player.png" };
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.ReferencedFiles.Add(rfs);

        Assert.Same(rfs, entity.GetReferencedFileSaveRecursively("Entities/Player/Player.png"));
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_ByName_FoundOnBase_ReturnsIt()
    {
        AddBaseEntityWithFile("Entities/Base/Base.png", out var rfs);
        var entity = new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Base" };

        Assert.Same(rfs, entity.GetReferencedFileSaveRecursively("Entities/Base/Base.png"));
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_ByName_NotFound_ReturnsNull()
    {
        var entity = new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Missing" };

        Assert.Null(entity.GetReferencedFileSaveRecursively("Nope.png"));
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_ByFilePath_FoundOnBase_ReturnsIt()
    {
        // The lookup compares the rfs name against FilePath.FullPath, which FilePath standardizes per
        // OS (on Linux a "c:/..." string is relative and gets the cwd prefixed), so build the rfs name
        // from the same FilePath rather than a literal.
        var filePath = new FilePath("c:/content/Entities/Base/Base.png");
        AddBaseEntityWithFile(filePath.FullPath, out var rfs);
        var entity = new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Base" };

        Assert.Same(rfs, ((IElement)entity).GetReferencedFileSaveRecursively(filePath));
    }

    [Fact]
    public void GetReferencedFileSaveByInstanceName_MatchesGeneratedInstanceName()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Player Sheet.png" };
        var entity = new EntitySave();
        entity.ReferencedFiles.Add(rfs);

        Assert.Same(rfs, entity.GetReferencedFileSaveByInstanceName("PlayerSheet"));
        Assert.Null(entity.GetReferencedFileSaveByInstanceName("playersheet"));
        Assert.Same(rfs, entity.GetReferencedFileSaveByInstanceName("playersheet", caseSensitive: false));
    }

    [Fact]
    public void GetReferencedFileSaveByInstanceName_EmptyName_ReturnsNull()
    {
        var entity = new EntitySave();
        entity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Entities/Player/Player.png" });

        Assert.Null(entity.GetReferencedFileSaveByInstanceName(""));
    }

    [Fact]
    public void GetReferencedFileSaveByInstanceNameRecursively_FoundOnBase_ReturnsIt()
    {
        AddBaseEntityWithFile("Entities/Base/BaseSheet.png", out var rfs);
        var entity = new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Base" };

        Assert.Same(rfs, entity.GetReferencedFileSaveByInstanceNameRecursively("BaseSheet"));
    }

    [Fact]
    public void GetReferencedFileSaveByInstanceNameRecursively_NotFound_ReturnsNull()
    {
        AddBaseEntityWithFile("Entities/Base/BaseSheet.png", out _);
        var entity = new EntitySave { Name = "Entities\\Player", BaseEntity = "Entities\\Base" };

        Assert.Null(entity.GetReferencedFileSaveByInstanceNameRecursively("Nope"));
    }

    #endregion
}
