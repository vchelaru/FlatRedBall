using System;
using System.Linq;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.PropertyGrid;
using GlueUnitTests.TestSupport;
using Gum.DataTypes;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// Asking plugins for a file's asset types with no project loaded (the property parity tests do, through the
/// RuntimeType options) used to throw inside the Gum plugin. PluginManager catches that and disables the plugin for
/// the rest of the run, which broke later Gum tests depending on order.
/// </summary>
public class GumAvailableAssetTypesNoProjectTests : IDisposable
{
    readonly VisualStudioProject? originalMainProject;
    readonly GumProjectSave? originalGumProject;
    readonly GlueProjectSave originalGlueProject;
    readonly bool originalHandleExceptions;

    public GumAvailableAssetTypesNoProjectTests()
    {
        GlueTestBootstrap.EnsureGumPluginRegisteredWithPluginManager();
        originalMainProject = GlueState.Self.CurrentMainProject;
        originalGumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        originalGlueProject = ObjectFinder.Self.GlueProject;
        originalHandleExceptions = PluginManager.HandleExceptions;
        GlueState.Self.CurrentMainProject = null;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = originalMainProject;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = originalGumProject;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        PluginManager.HandleExceptions = originalHandleExceptions;
    }

    [StaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetAvailableAssetTypes_WithNoProjectLoaded_ShouldNotDisableAnyPlugin(bool hasGumProject)
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = hasGumProject ? new GumProjectSave() : null;
        var failedBefore = FailedPlugins();

        // Let a throwing handler surface here instead of being swallowed into a disabled plugin.
        PluginManager.HandleExceptions = false;
        var assetTypes = PluginManager.GetAvailableAssetTypes(new ReferencedFileSave { Name = "Screens/GameScreen/Background.png" });

        assetTypes.ShouldBeEmpty();
        FailedPlugins().ShouldBe(failedBefore);
    }

    [StaFact]
    public void RuntimeTypeOptions_WithNoProjectLoaded_ShouldLeaveTheGumPluginWorking()
    {
        // The order that used to fail: the Gum plugin is registered, the property parity test lists a file's
        // RuntimeType options with no project loaded, then a later test calls into the Gum plugin.
        var fixture = EditorPropertyFixtures.Create();
        ObjectFinder.Self.GlueProject = fixture.Project;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;

        var rfs = fixture.Files.Single(item => item.Key == "screen png").File;
        var runtimeType = Builder.Get<ReferencedFileSavePropertyModel>().GetProperties(rfs)
            .Single(item => item.Name == nameof(ReferencedFileSave.RuntimeType));
        runtimeType.Options!.Invoke().ShouldBe(new[] { "Microsoft.Xna.Framework.Graphics.Texture2D" });

        var gumPlugin = PluginManagerBase.AllPluginContainers.Single(container => container.Plugin is global::GumPlugin.MainGumPlugin);
        gumPlugin.FailureException.ShouldBeNull();
        gumPlugin.IsEnabled.ShouldBeTrue();
    }

    static string[] FailedPlugins() => PluginManagerBase.AllPluginContainers
        .Where(container => !container.IsEnabled || container.FailureException != null)
        .Select(container => container.Name)
        .OrderBy(name => name)
        .ToArray();
}
