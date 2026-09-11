using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CollisionPlugin;

/// <summary>
/// Now that selection is owned by GlueState (GitHub issue #2268), setting
/// <c>GlueState.Self.CurrentNamedObjectSave</c> in a test really runs every registered plugin's selection
/// handler. A headless host has no UI thread, so a plugin that builds its WPF view on selection throws
/// (STA required, or cross-thread access on the next test's STA thread). View-model work must still run -
/// that is what these tests exist to exercise - but the view itself is a GUI concern gated on
/// <c>GlueGui.ShowGui</c>, the same switch that keeps dialogs and the load window off screen headless.
///
/// Deliberately a plain (MTA) <c>[Fact]</c>: the point is that no WPF view is constructed. And with
/// <c>PluginManager.HandleExceptions</c> left on, a throwing handler is swallowed and the plugin silently
/// disabled for every test after it - so it is off here, the way the Wizard tests run.
/// </summary>
public class CollisionPluginHeadlessSelectionTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly NamedObjectSave _playerInstance;

    public CollisionPluginHeadlessSelectionTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager();
        PluginManager.HandleExceptions = false;

        _originalGlueProject = ObjectFinder.Self.GlueProject;

        var player = new EntitySave { Name = "Entities\\Player", ImplementsICollidable = true };
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        _playerInstance = new NamedObjectSave
        {
            InstanceName = "PlayerInstance",
            SourceType = SourceType.Entity,
            SourceClassType = "Entities\\Player"
        };
        screen.NamedObjects.Add(_playerInstance);

        var project = new GlueProjectSave();
        project.Entities.Add(player);
        project.Screens.Add(screen);
        ObjectFinder.Self.GlueProject = project;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentTreeNode = null;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        PluginManager.HandleExceptions = true;
    }

    [Fact]
    public void SelectingACollidable_RefreshesTheCollidableViewModel_WithoutBuildingAView()
    {
        GlueState.Self.CurrentNamedObjectSave = _playerInstance;

        GlueTestBootstrap.RegisteredCollisionPlugin.CollidableViewModel.CollisionRelationshipsTitle
            .ShouldBe("PlayerInstance Collision Relationships");
    }
}
