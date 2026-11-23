using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin;
using GameCommunicationPlugin.GlueControl;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.Managers;
using GameJsonCommunicationPlugin.Common;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Plugins;

/// <summary>
/// The typed commands plugins assign to <see cref="GlueCommands"/> at startup in place of string-based
/// <c>PluginManager.CallPluginMethod</c> calls, and the callers that used to make those calls. In the
/// TaskManager collection so it does not run alongside the wizard tests, which also swap these commands.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class TypedPluginCommandsTests : IDisposable
{
    private readonly ICompilerCommands _originalCompilerCommands;
    private IGumCommands _originalGumCommands;
    private readonly IGameCommunicationCommands _originalGameCommunicationCommands;
    private readonly IGameHostCommands _originalGameHostCommands;
    private ITiledCommands _originalTiledCommands;
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private string _tempProjectDirectory;

    public TypedPluginCommandsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalCompilerCommands = GlueCommands.Self.CompilerCommands;
        _originalGumCommands = GlueCommands.Self.GumCommands;
        _originalGameCommunicationCommands = GlueCommands.Self.GameCommunicationCommands;
        _originalGameHostCommands = GlueCommands.Self.GameHostCommands;
        _originalTiledCommands = GlueCommands.Self.TiledCommands;
        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        GlueCommands.Self.CompilerCommands = _originalCompilerCommands;
        GlueCommands.Self.GumCommands = _originalGumCommands;
        GlueCommands.Self.GameCommunicationCommands = _originalGameCommunicationCommands;
        GlueCommands.Self.GameHostCommands = _originalGameHostCommands;
        GlueCommands.Self.TiledCommands = _originalTiledCommands;
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        if (_tempProjectDirectory != null)
        {
            try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
        }
    }

    #region Registration

    [StaFact]
    public void CompilerPlugin_StartUp_AssignsCompilerCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueCommands.Self.CompilerCommands = null;
        var plugin = new global::CompilerPlugin.CompilerPlugin();

        plugin.StartUp();
        GlueCommands.Self.CompilerCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.CompilerCommands.ShouldBeNull();
    }

    /// <summary>
    /// The registered Gum plugin is shared by the whole run, and an earlier test can make it throw, which disables
    /// it and runs its ShutDown. Either way GumCommands must match: set while enabled, cleared once shut down.
    /// </summary>
    [StaFact]
    public void GumPlugin_GumCommands_AreSetExactlyWhileThePluginIsEnabled()
    {
        GlueTestBootstrap.EnsureGumPluginRegisteredWithPluginManager();

        var gumContainer = FlatRedBall.Glue.Plugins.PluginManager.GetGlobalPluginManager().PluginContainers.Values
            .Single(item => item.Plugin is GumPlugin.MainGumPlugin);
        if (gumContainer.IsEnabled)
        {
            GlueCommands.Self.GumCommands.ShouldNotBeNull();
            GlueCommands.Self.GumCommands.HasGum().ShouldBe(GumPlugin.Managers.AppState.Self.GumProjectSave != null);
        }
        else
        {
            GlueCommands.Self.GumCommands.ShouldBeNull();
        }
    }

    [Fact]
    public void GameCommunicationPlugin_StartUp_AssignsGameCommunicationCommands_AndShutDownClearsThem()
    {
        var originalConnectionManager = GameConnectionManager.Self;
        GlueCommands.Self.GameCommunicationCommands = null;
        var plugin = new MainGameCommunicationPlugin();
        try
        {
            plugin.StartUp();
            GlueCommands.Self.GameCommunicationCommands.ShouldNotBeNull();

            plugin.ShutDown(PluginShutDownReason.PluginInitiated);
            GlueCommands.Self.GameCommunicationCommands.ShouldBeNull();
        }
        finally
        {
            GameConnectionManager.Self = originalConnectionManager;
        }
    }

    /// <summary>
    /// Checks the registered Tiled plugin, since a second StartUp would add its asset types and code generators
    /// again for the rest of the run.
    /// </summary>
    [StaFact]
    public void TiledPlugin_StartUp_AssignsTiledCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        // Registering assigns these for the rest of the run, so Dispose restores the registered ones.
        _originalGumCommands = GlueCommands.Self.GumCommands;
        _originalTiledCommands = GlueCommands.Self.TiledCommands;
        var tiledContainer = FlatRedBall.Glue.Plugins.PluginManager.GetGlobalPluginManager().PluginContainers.Values
            .Single(item => item.Plugin is TileGraphicsPlugin.MainTiledPluginClass);
        tiledContainer.IsEnabled.ShouldBeTrue();
        GlueCommands.Self.TiledCommands.ShouldNotBeNull();

        new TileGraphicsPlugin.MainTiledPluginClass().ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.TiledCommands.ShouldBeNull();
    }

    [StaFact]
    public void GlueCompilerPlugin_StartUp_AssignsGameHostCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        var originalConnectionManager = GameConnectionManager.Self;
        GlueCommands.Self.GameHostCommands = null;
        var plugin = new MainCompilerPlugin();
        try
        {
            plugin.StartUp();
            GlueCommands.Self.GameHostCommands.ShouldNotBeNull();

            plugin.ShutDown(PluginShutDownReason.PluginInitiated);
            GlueCommands.Self.GameHostCommands.ShouldBeNull();
        }
        finally
        {
            // Its Game1 and element code generators would otherwise write into later tests' generated code.
            plugin.UnregisterAllCodeGenerators();
            GameConnectionManager.Self = originalConnectionManager;
        }
    }

    #endregion

    #region Implementations

    [Fact]
    public void GameHostCommands_ReadAndSetTheCompilerViewModel()
    {
        var viewModel = CompilerViewModel.Self;
        var (wasRunning, wasLoggingSent, wasLoggingReceived) = (viewModel.IsRunning,
            viewModel.IsPrintEditorToGameCheckboxChecked, viewModel.IsPrintGameToEditorCheckboxChecked);
        var commands = new GameHostCommands(viewModel, () => Task.CompletedTask, refreshManager: null);
        try
        {
            viewModel.IsRunning = false;
            commands.IsGameRunning().ShouldBeFalse();
            viewModel.IsRunning = true;
            commands.IsGameRunning().ShouldBeTrue();

            commands.SetIsLoggingSentCommands(!wasLoggingSent);
            viewModel.IsPrintEditorToGameCheckboxChecked.ShouldBe(!wasLoggingSent);

            commands.SetIsLoggingReceivedCommands(!wasLoggingReceived);
            viewModel.IsPrintGameToEditorCheckboxChecked.ShouldBe(!wasLoggingReceived);
        }
        finally
        {
            viewModel.IsRunning = wasRunning;
            viewModel.IsPrintEditorToGameCheckboxChecked = wasLoggingSent;
            viewModel.IsPrintGameToEditorCheckboxChecked = wasLoggingReceived;
        }
    }

    #endregion

    #region Callers

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveOrphanedGeneratedCsprojEntries_KeepsFormsCode_OnlyWhenGumCommandsClaimIt(bool gumLoaded)
    {
        const string formsFile = @"Forms\Controls\ButtonRuntime.Generated.cs";
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        project.AddCodeBuildItem(formsFile);
        GlueState.Self.CurrentMainProject = project;
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = (int)GlueProjectSave.LatestVersion };
        GlueCommands.Self.GumCommands = gumLoaded
            ? new FakeGumCommands(hasGum: true) { FormsGeneratedRelativePaths = { formsFile } }
            : null;

        new FlatRedBall.Glue.IO.ProjectLoadOperations(GlueCommands.Self, GlueState.Self)
            .RemoveOrphanedGeneratedCsprojEntries();

        project.IsFilePartOfProject(formsFile, FlatRedBall.Glue.VSHelpers.Projects.BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBe(gumLoaded);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    public void GlueControlCode_DefinesHasGum_OnlyWhenGumCommandsSayTheProjectHasGum(bool? hasGum, bool shouldDefine)
    {
        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = (int)GlueProjectSave.LatestVersion };
        GlueCommands.Self.GumCommands = hasGum == null ? null : new FakeGumCommands(hasGum.Value);

        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(
            "GameCommunicationPlugin.GlueControl.Embedded.GlueControlManager.cs");

        // Whole line: the Glue version defines include HasGumSkiaElements.
        generated.Contains("#define HasGum\r\n").ShouldBe(shouldDefine);
    }

    [Fact]
    public void StateCodeGenerator_AsksGameCommunicationCommands_ForEachCategorySetter()
    {
        var entity = CreateEntityWithStateCategory();
        GlueCommands.Self.GameCommunicationCommands = new FakeGameCommunicationCommands();

        var generated = GenerateStateFields(entity);

        generated.ShouldContain("// reflection assignment for Health");
    }

    [Fact]
    public void StateCodeGenerator_AddsNoReflectionAssignment_WhenGameCommunicationPluginIsNotLoaded()
    {
        var entity = CreateEntityWithStateCategory();
        GlueCommands.Self.GameCommunicationCommands = null;

        var generated = GenerateStateFields(entity);

        generated.ShouldContain("bool assignedThroughReflection = false;");
        generated.ShouldNotContain("// reflection assignment");
    }

    private static EntitySave CreateEntityWithStateCategory()
    {
        var category = new StateSaveCategory { Name = "Health" };
        category.States.Add(new StateSave { Name = "Wounded" });
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        entity.StateCategoryList.Add(category);
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = (int)GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject.Entities.Add(entity);
        return entity;
    }

    private static string GenerateStateFields(EntitySave entity)
    {
        ICodeBlock codeBlock = new CodeDocument();
        new StateCodeGenerator().GenerateFields(codeBlock, entity);
        return codeBlock.ToString();
    }

    #endregion

    internal class FakeGumCommands : IGumCommands
    {
        private readonly bool _hasGum;
        public FakeGumCommands(bool hasGum) => _hasGum = hasGum;
        public bool HasGum() => _hasGum;

        public List<string> FormsGeneratedRelativePaths { get; } = new();
        public List<(bool IncludeForms, bool AskToOverwrite)> CreatedProjects { get; } = new();

        public bool IsAssetTypeInfoGum(AssetTypeInfo ati) => false;
        public IReadOnlyList<string> GetFormsGeneratedRelativePaths() => FormsGeneratedRelativePaths;
        public Task AskToCreateGumProjectAsync() => Task.CompletedTask;

        public Task CreateGumProjectAsync(bool includeForms, bool askToOverwrite)
        {
            CreatedProjects.Add((includeForms, askToOverwrite));
            return Task.CompletedTask;
        }
    }

    private class FakeGameCommunicationCommands : IGameCommunicationCommands
    {
        public void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category) =>
            codeBlock.Line($"// reflection assignment for {category.Name}");
    }
}
