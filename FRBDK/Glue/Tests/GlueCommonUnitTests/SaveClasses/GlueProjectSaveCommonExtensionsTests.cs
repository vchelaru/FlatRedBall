using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Controls;
using GlueCommonUnitTests.Parsing;

namespace GlueCommonUnitTests.SaveClasses;

// These fan out to element-level logic that reads the shared static ObjectFinderCore.Self/
// AvailableAssetTypesCore.Self/TypeResolutionCore.Self/PluginManagerCore.Self/ErrorReportingCore.Self,
// so this can't run concurrently with any other test class that swaps them out - hence the shared
// collection (see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class GlueProjectSaveCommonExtensionsTests
{
    enum TestEnum { First = 0, Second = 1, Third = 2 }

    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();
    readonly FakeTypeResolutionCore _typeResolution = new();
    readonly FakePluginManagerCore _plugins = new();
    readonly FakeErrorReportingCore _errors = new();
    readonly GlueProjectSave _project = new();

    public GlueProjectSaveCommonExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
        TypeResolutionCore.Self = _typeResolution;
        PluginManagerCore.Self = _plugins;
        ErrorReportingCore.Self = _errors;
        _finder.GlueProject = _project;
    }

    EntitySave AddEntity(string name, string? baseEntity = null)
    {
        var entity = new EntitySave { Name = name, BaseEntity = baseEntity };
        _project.Entities.Add(entity);
        _finder.AddElement(name, entity);
        return entity;
    }

    ScreenSave AddScreen(string name)
    {
        var screen = new ScreenSave { Name = name };
        _project.Screens.Add(screen);
        _finder.AddElement(name, screen);
        return screen;
    }

    static NamedObjectSave EntityInstance(string instanceName, string entityName, string? currentState = null) =>
        new()
        {
            InstanceName = instanceName,
            SourceType = SourceType.Entity,
            SourceClassType = entityName,
            CurrentState = currentState,
        };

    #region RemoveInvalidStatesFromNamedObjects

    [Fact]
    public void RemoveInvalidStatesFromNamedObjects_MissingState_ClearsStateAndReports()
    {
        AddEntity("Entities\\Enemy");
        var level = AddScreen("Screens\\Level1");
        var nos = EntityInstance("EnemyInstance", "Entities\\Enemy", "Missing");
        level.NamedObjects.Add(nos);

        _project.RemoveInvalidStatesFromNamedObjects(showPopupsOnFixedErrors: true);

        Assert.Null(nos.CurrentState);
        var message = Assert.Single(_errors.Messages);
        Assert.Contains("EnemyInstance", message);
        Assert.Contains("Screens\\Level1", message);
        Assert.Contains("Missing", message);
    }

    [Fact]
    public void RemoveInvalidStatesFromNamedObjects_MissingState_NoPopups_ClearsSilently()
    {
        var container = AddEntity("Entities\\Spawner");
        AddEntity("Entities\\Enemy");
        var nos = EntityInstance("EnemyInstance", "Entities\\Enemy", "Missing");
        container.NamedObjects.Add(nos);

        _project.RemoveInvalidStatesFromNamedObjects(showPopupsOnFixedErrors: false);

        Assert.Null(nos.CurrentState);
        Assert.Empty(_errors.Messages);
    }

    [Fact]
    public void RemoveInvalidStatesFromNamedObjects_StateExists_LeavesIt()
    {
        var enemy = AddEntity("Entities\\Enemy");
        enemy.States.Add(new StateSave { Name = "Alive" });
        var level = AddScreen("Screens\\Level1");
        var nos = EntityInstance("EnemyInstance", "Entities\\Enemy", "Alive");
        level.NamedObjects.Add(nos);

        _project.RemoveInvalidStatesFromNamedObjects(showPopupsOnFixedErrors: true);

        Assert.Equal("Alive", nos.CurrentState);
        Assert.Empty(_errors.Messages);
    }

    [Fact]
    public void RemoveInvalidStatesFromNamedObjects_StateOnBaseEntity_LeavesIt()
    {
        var baseEnemy = AddEntity("Entities\\BaseEnemy");
        baseEnemy.States.Add(new StateSave { Name = "Alive" });
        AddEntity("Entities\\Enemy", "Entities\\BaseEnemy");
        var level = AddScreen("Screens\\Level1");
        var nos = EntityInstance("EnemyInstance", "Entities\\Enemy", "Alive");
        level.NamedObjects.Add(nos);

        _project.RemoveInvalidStatesFromNamedObjects(showPopupsOnFixedErrors: true);

        Assert.Equal("Alive", nos.CurrentState);
    }

    [Fact]
    public void RemoveInvalidStatesFromNamedObjects_UnknownEntityOrNonEntitySource_LeavesState()
    {
        var level = AddScreen("Screens\\Level1");
        var unknownEntity = EntityInstance("Ghost", "Entities\\Nope", "Missing");
        var fileSource = new NamedObjectSave { InstanceName = "Sprite", SourceType = SourceType.File, SourceClassType = "Sprite", CurrentState = "Missing" };
        level.NamedObjects.Add(unknownEntity);
        level.NamedObjects.Add(fileSource);

        _project.RemoveInvalidStatesFromNamedObjects(showPopupsOnFixedErrors: true);

        Assert.Equal("Missing", unknownEntity.CurrentState);
        Assert.Equal("Missing", fileSource.CurrentState);
        Assert.Empty(_errors.Messages);
    }

    #endregion

    #region PostLoadInitialize

    [Fact]
    public void PostLoadInitialize_ReachesScreensAndEntities_NoErrors()
    {
        var screenNos = new NamedObjectSave();
        screenNos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "X", Value = null });
        AddScreen("Screens\\Level1").NamedObjects.Add(screenNos);
        var entityNos = new NamedObjectSave();
        entityNos.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "Y", Value = null });
        AddEntity("Entities\\Player").NamedObjects.Add(entityNos);

        _project.PostLoadInitialize(out var errors);

        Assert.Null(errors);
        Assert.Empty(screenNos.InstructionSaves);
        Assert.Empty(entityNos.InstructionSaves);
    }

    #endregion

    #region FixReferencedFileSaveContentPipelineSettings

    ReferencedFileSave FileNeedingPipeline(string name)
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { Extension = "fbx", MustBeAddedToContentPipeline = true });
        return new ReferencedFileSave { Name = name, UseContentPipeline = false };
    }

    [Fact]
    public void FixReferencedFileSaveContentPipelineSettings_SetsUseContentPipelineOnScreensEntitiesAndGlobalFiles()
    {
        var screenFile = FileNeedingPipeline("Screens/Level1/Model.fbx");
        AddScreen("Screens\\Level1").ReferencedFiles.Add(screenFile);
        var entityFile = new ReferencedFileSave { Name = "Entities/Player/Model.fbx" };
        AddEntity("Entities\\Player").ReferencedFiles.Add(entityFile);
        var globalFile = new ReferencedFileSave { Name = "GlobalContent/Model.fbx" };
        _project.GlobalFiles.Add(globalFile);

        _project.FixReferencedFileSaveContentPipelineSettings();

        Assert.True(screenFile.UseContentPipeline);
        Assert.True(entityFile.UseContentPipeline);
        Assert.True(globalFile.UseContentPipeline);
    }

    [Fact]
    public void FixReferencedFileSaveContentPipelineSettings_PipelineOptionalOrUnknownType_LeavesFalse()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { Extension = "png", MustBeAddedToContentPipeline = false });
        var optional = new ReferencedFileSave { Name = "GlobalContent/Sprite.png" };
        var unknown = new ReferencedFileSave { Name = "GlobalContent/Data.xyz" };
        _project.GlobalFiles.Add(optional);
        _project.GlobalFiles.Add(unknown);

        _project.FixReferencedFileSaveContentPipelineSettings();

        Assert.False(optional.UseContentPipeline);
        Assert.False(unknown.UseContentPipeline);
    }

    #endregion

    #region AllElements

    [Fact]
    public void AllElements_YieldsScreensThenEntities()
    {
        var entity = AddEntity("Entities\\Player");
        var screen = AddScreen("Screens\\Level1");

        Assert.Equal(new GlueElement[] { screen, entity }, _project.AllElements());
    }

    [Fact]
    public void AllElements_EmptyProject_YieldsNothing()
    {
        Assert.Empty(_project.AllElements());
    }

    #endregion

    #region FixAllTypesPostLoad / FixEnumerationValues / ConvertEnumerationValuesToInts

    [Fact]
    public void FixAllTypesPostLoad_FixesEntitiesScreensAndGlobalFiles()
    {
        _typeResolution.AddType("float", typeof(float));
        var entityVariable = new CustomVariable { Name = "Health", Type = "float", DefaultValue = 3 };
        AddEntity("Entities\\Player").CustomVariables.Add(entityVariable);
        var screenVariable = new CustomVariable { Name = "Gravity", Type = "float", DefaultValue = 2 };
        AddScreen("Screens\\Level1").CustomVariables.Add(screenVariable);
        var globalFile = new ReferencedFileSave { Name = "GlobalContent/Player.png" };
        globalFile.Properties.Add(new PropertySave { Name = "Scale", Type = "float", Value = 4 });
        _project.GlobalFiles.Add(globalFile);

        _project.FixAllTypesPostLoad();

        Assert.Equal(3f, entityVariable.DefaultValue);
        Assert.Equal(2f, screenVariable.DefaultValue);
        Assert.Equal(4f, globalFile.Properties[0].Value);
    }

    [Fact]
    public void FixEnumerationValues_ConvertsIntsOnEntitiesAndScreens()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var entityVariable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = 1 };
        AddEntity("Entities\\Player").CustomVariables.Add(entityVariable);
        var screenVariable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = 2 };
        AddScreen("Screens\\Level1").CustomVariables.Add(screenVariable);

        _project.FixEnumerationValues();

        Assert.Equal(TestEnum.Second, entityVariable.DefaultValue);
        Assert.Equal(TestEnum.Third, screenVariable.DefaultValue);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_ConvertsEnumsOnEntitiesAndScreens()
    {
        var entityVariable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = TestEnum.Second };
        AddEntity("Entities\\Player").CustomVariables.Add(entityVariable);
        var screenVariable = new CustomVariable { Name = "Mode", Type = "TestEnum", DefaultValue = TestEnum.Third };
        AddScreen("Screens\\Level1").CustomVariables.Add(screenVariable);

        _project.ConvertEnumerationValuesToInts();

        Assert.Equal(1, entityVariable.DefaultValue);
        Assert.Equal(2, screenVariable.DefaultValue);
    }

    #endregion

    #region SearchForDuplicateNamedObjects / SearchForDuplicateEntities

    [Fact]
    public void SearchForDuplicateNamedObjects_DuplicateInEntity_Reports()
    {
        var entity = AddEntity("Entities\\Player");
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });

        _project.SearchForDuplicateNamedObjects();

        var message = Assert.Single(_errors.Messages);
        Assert.Contains("Sprite", message);
        Assert.Contains("Entities\\Player", message);
    }

    [Fact]
    public void SearchForDuplicateNamedObjects_DuplicateInScreen_Reports()
    {
        var screen = AddScreen("Screens\\Level1");
        screen.NamedObjects.Add(new NamedObjectSave { InstanceName = "Map" });
        screen.NamedObjects.Add(new NamedObjectSave { InstanceName = "Map" });

        _project.SearchForDuplicateNamedObjects();

        var message = Assert.Single(_errors.Messages);
        Assert.Contains("Map", message);
        Assert.Contains("Screens\\Level1", message);
    }

    [Fact]
    public void SearchForDuplicateNamedObjects_SameNameInDifferentElements_DoesNotReport()
    {
        AddEntity("Entities\\Player").NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });
        AddEntity("Entities\\Enemy").NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });
        AddScreen("Screens\\Level1").NamedObjects.Add(new NamedObjectSave { InstanceName = "Sprite" });

        _project.SearchForDuplicateNamedObjects();

        Assert.Empty(_errors.Messages);
    }

    [Fact]
    public void SearchForDuplicateEntities_DuplicateName_Reports()
    {
        AddEntity("Entities\\Player");
        _project.Entities.Add(new EntitySave { Name = "Entities\\Player" });

        _project.SearchForDuplicateEntities();

        Assert.Contains("Entities\\Player", Assert.Single(_errors.Messages));
    }

    [Fact]
    public void SearchForDuplicateEntities_UniqueNames_DoesNotReport()
    {
        AddEntity("Entities\\Player");
        AddEntity("Entities\\Enemy");

        _project.SearchForDuplicateEntities();

        Assert.Empty(_errors.Messages);
    }

    #endregion

    #region CleanUnusedVariablesFromStates / FixAttachmentProperties

    static StateSave StateWith(params string[] members)
    {
        var state = new StateSave { Name = "A" };
        foreach (var member in members)
        {
            state.InstructionSaves.Add(new InstructionSave { Member = member, Value = 1f });
        }
        return state;
    }

    [Fact]
    public void CleanUnusedVariablesFromStates_CleansEntitiesAndScreens()
    {
        var entity = AddEntity("Entities\\Player");
        entity.CustomVariables.Add(new CustomVariable { Name = "Known" });
        var entityState = StateWith("Known", "Unknown");
        entity.States.Add(entityState);
        var screen = AddScreen("Screens\\Level1");
        screen.CustomVariables.Add(new CustomVariable { Name = "Gravity" });
        var screenState = StateWith("Unknown", "Gravity");
        screen.States.Add(screenState);

        _project.CleanUnusedVariablesFromStates();

        Assert.Equal(new[] { "Known" }, entityState.InstructionSaves.Select(item => item.Member));
        Assert.Equal(new[] { "Gravity" }, screenState.InstructionSaves.Select(item => item.Member));
    }

    [Fact]
    public void FixAttachmentProperties_ClearsAttachToCameraOnEntityObjectsOnly()
    {
        var entityNos = new NamedObjectSave { InstanceName = "Sprite", AttachToCamera = true };
        AddEntity("Entities\\Player").NamedObjects.Add(entityNos);
        var screenNos = new NamedObjectSave { InstanceName = "Hud", AttachToCamera = true };
        AddScreen("Screens\\Level1").NamedObjects.Add(screenNos);

        _project.FixAttachmentProperties();

        Assert.False(entityNos.AttachToCamera);
        Assert.True(screenNos.AttachToCamera);
    }

    #endregion
}
