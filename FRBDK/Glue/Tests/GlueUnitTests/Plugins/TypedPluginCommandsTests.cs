using System;
using System.IO;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameJsonCommunicationPlugin.Common;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Plugins;

/// <summary>
/// The typed commands plugins assign to <see cref="GlueCommands"/> at startup in place of string-based
/// <c>PluginManager.CallPluginMethod</c> calls, and the callers that used to make those calls.
/// </summary>
public class TypedPluginCommandsTests : IDisposable
{
    private readonly ICompilerCommands _originalCompilerCommands;
    private readonly IGumCommands _originalGumCommands;
    private readonly IGameCommunicationCommands _originalGameCommunicationCommands;
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private string _tempProjectDirectory;

    public TypedPluginCommandsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalCompilerCommands = GlueCommands.Self.CompilerCommands;
        _originalGumCommands = GlueCommands.Self.GumCommands;
        _originalGameCommunicationCommands = GlueCommands.Self.GameCommunicationCommands;
        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        GlueCommands.Self.CompilerCommands = _originalCompilerCommands;
        GlueCommands.Self.GumCommands = _originalGumCommands;
        GlueCommands.Self.GameCommunicationCommands = _originalGameCommunicationCommands;
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

    [StaFact]
    public void GumPlugin_StartUp_AssignsGumCommands()
    {
        GlueTestBootstrap.EnsureGumPluginRegisteredWithPluginManager();

        GlueCommands.Self.GumCommands.ShouldNotBeNull();
        GlueCommands.Self.GumCommands.HasGum().ShouldBe(GumPlugin.Managers.AppState.Self.GumProjectSave != null);
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

    #endregion

    #region Callers

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

    private class FakeGumCommands : IGumCommands
    {
        private readonly bool _hasGum;
        public FakeGumCommands(bool hasGum) => _hasGum = hasGum;
        public bool HasGum() => _hasGum;
    }

    private class FakeGameCommunicationCommands : IGameCommunicationCommands
    {
        public void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category) =>
            codeBlock.Line($"// reflection assignment for {category.Name}");
    }
}
