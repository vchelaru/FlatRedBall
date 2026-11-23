using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueUnitTests.TestSupport;
using GumPlugin;
using Shouldly;
using TileGraphicsPlugin;
using Xunit;

namespace GlueUnitTests.Plugins;

/// <summary>
/// Plugins build their views in <see cref="PluginBase.StartUpUi"/>, which only runs when Glue shows its GUI, so
/// a headless host (GlueCli, these tests) never pays for XAML. These tests pin that dispatch, and that each
/// plugin whose views moved there still registers its model pieces headless.
/// </summary>
public class StartUpUiTests
{
    public StartUpUiTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void RegisterPlugin_Headless_RunsStartUpButNotStartUpUi()
    {
        GlueGui.ShowGui.ShouldBeFalse();
        var plugin = new RecordingPlugin();

        PluginManager.RegisterPlugin(plugin);

        plugin.Calls.ShouldBe(new[] { "StartUp" });
    }

    [Fact]
    public void RegisterPlugin_WithGui_RunsStartUpUiRightAfterStartUp()
    {
        var plugin = new RecordingPlugin();

        GlueGui.ShowGui = true;
        try
        {
            PluginManager.RegisterPlugin(plugin);
        }
        finally
        {
            GlueGui.ShowGui = false;
        }

        plugin.Calls.ShouldBe(new[] { "StartUp", "StartUpUi" });
    }

    // StaFact: the game-project plugins' StartUp needs an STA thread (see EnsureHeadlessProjectLoadReady).
    [StaFact]
    public void PluginsWithViews_Headless_StartWithModelPiecesAndNoViews()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var gum = Registered<MainGumPlugin>();
        CodeWriter.CodeGenerators.ShouldContain(generator => generator.GetType().Assembly == typeof(MainGumPlugin).Assembly);
        gum.ReactToLoadedGlux.ShouldNotBeNull();

        var tiled = Registered<MainTiledPluginClass>();
        CodeWriter.CodeGenerators.ShouldContain(generator => generator.GetType().Assembly == typeof(MainTiledPluginClass).Assembly);
        tiled.ReactToLoadedGlux.ShouldNotBeNull();

        var output = Registered<OutputPrintPlugin>();
        output.OnOutputHandler.ShouldNotBeNull();

        var camera = Registered<FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin.CameraMainPlugin>();
        camera.ReactToLoadedGlux.ShouldNotBeNull();

        var tasks = Registered<TaskDisplayerPlugin>();

        foreach (var plugin in new PluginBase[] { gum, tiled, output, camera, tasks })
        {
            BuiltViews(plugin).ShouldBeEmpty($"{plugin.GetType().Name} built views headless");
        }
    }

    // Loading a project runs every plugin's ReactToLoadedGlux. A handler that touches a view StartUpUi never
    // built would throw here instead of quietly disabling its plugin.
    [StaFact]
    [Trait("Category", "BuildSmoke")]
    public async System.Threading.Tasks.Task LoadProject_Headless_NoPluginFails()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        PluginManager.HandleExceptions = false;
        try
        {
            await GoldProject.LoadInGlueAsync(System.IO.Path.Combine(project.Root, "Beefball", "Beefball.csproj"));
        }
        finally
        {
            PluginManager.HandleExceptions = true;
        }

        PluginManagerBase.AllPluginContainers
            .Where(container => container.FailureException != null || !container.IsEnabled)
            .Select(container => $"{container.Name}: {container.FailureException}")
            .ShouldBeEmpty();
    }

    static T Registered<T>() where T : PluginBase
    {
        var container = PluginManagerBase.AllPluginContainers.Single(item => item.Plugin is T);
        container.IsEnabled.ShouldBeTrue($"{typeof(T).Name} failed to start: {container.FailureException}");
        return (T)container.Plugin;
    }

    static List<string> BuiltViews(PluginBase plugin) =>
        plugin.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => typeof(System.Windows.FrameworkElement).IsAssignableFrom(field.FieldType)
                && field.GetValue(plugin) != null)
            .Select(field => field.Name)
            .ToList();

    class RecordingPlugin : PluginBase
    {
        public List<string> Calls { get; } = new();
        public override string FriendlyName => "StartUpUi test plugin";
        public override Version Version => new(1, 0);
        public override void StartUp() => Calls.Add("StartUp");
        public override void StartUpUi() => Calls.Add("StartUpUi");
        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
