using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ExportedImplementations;

/// <summary>
/// GlueState owns the selection: its Current* setters take effect from the model object alone, with the
/// tree view (when there is one) following along. These run with no tree at all - FakeFindManager never
/// resolves a tree node - so they pin that selection is a model concept, not a tree-view one.
/// See GitHub issue #2268.
/// </summary>
public class GlueStateSelectionTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly ScreenSave _screen;
    private readonly NamedObjectSave _first;
    private readonly NamedObjectSave _second;

    public GlueStateSelectionTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalGlueProject = ObjectFinder.Self.GlueProject;

        _screen = new ScreenSave { Name = "Screens\\SelectionScreen" };
        _first = new NamedObjectSave { InstanceName = "First", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };
        _second = new NamedObjectSave { InstanceName = "Second", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite" };
        _screen.NamedObjects.Add(_first);
        _screen.NamedObjects.Add(_second);

        var project = new GlueProjectSave();
        project.Screens.Add(_screen);
        ObjectFinder.Self.GlueProject = project;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentTreeNode = null;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void CurrentNamedObjectSave_SetWithoutATree_ReadsBackAndDerivesTheElement()
    {
        GlueState.Self.CurrentNamedObjectSave = _first;

        GlueState.Self.CurrentNamedObjectSave.ShouldBe(_first);
        GlueState.Self.CurrentElement.ShouldBe(_screen);
        GlueState.Self.CurrentScreenSave.ShouldBe(_screen);
    }

    [Fact]
    public void CurrentNamedObjectSave_SetToAnObjectInNoElement_ClearsTheSelection()
    {
        GlueState.Self.CurrentNamedObjectSave = _first;

        GlueState.Self.CurrentNamedObjectSave = new NamedObjectSave { InstanceName = "Orphan" };

        GlueState.Self.CurrentNamedObjectSave.ShouldBeNull();
        GlueState.Self.CurrentElement.ShouldBeNull();
    }

    [Fact]
    public void CurrentNamedObjectSaves_SetWithoutATree_SelectsEveryObject()
    {
        GlueState.Self.CurrentNamedObjectSaves = new List<NamedObjectSave> { _first, _second };

        GlueState.Self.CurrentNamedObjectSaves.ShouldBe(new[] { _first, _second });
        GlueState.Self.CurrentElement.ShouldBe(_screen);
    }

    [Fact]
    public void CurrentStateSave_SetWithoutATree_DerivesTheCategoryAndElement()
    {
        var state = new StateSave { Name = "Open" };
        var category = new StateSaveCategory { Name = "DoorStates" };
        category.States.Add(state);
        _screen.StateCategoryList.Add(category);

        GlueState.Self.CurrentStateSave = state;

        GlueState.Self.CurrentStateSave.ShouldBe(state);
        GlueState.Self.CurrentStateSaveCategory.ShouldBe(category);
        GlueState.Self.CurrentElement.ShouldBe(_screen);
    }

    [Fact]
    public void CurrentElement_SetWithoutATree_ReadsBack()
    {
        GlueState.Self.CurrentElement = _screen;

        GlueState.Self.CurrentElement.ShouldBe(_screen);
        GlueState.Self.CurrentNamedObjectSave.ShouldBeNull();
    }

    [Fact]
    public void SelectingWithoutATree_NotifiesPluginsWithTheSnapshotAlreadyTaken()
    {
        SelectionRecordingPlugin.EnsureRegistered();
        SelectionRecordingPlugin.SeenNamedObjectSaves.Clear();

        GlueState.Self.CurrentNamedObjectSave = _second;

        SelectionRecordingPlugin.SeenNamedObjectSaves.ShouldBe(new[] { _second });
    }

    /// <summary>
    /// Records what <c>GlueState.Self.CurrentNamedObjectSave</c> reads as at the moment plugins are told
    /// about a selection - the same read every real plugin's handler makes.
    /// </summary>
    private class SelectionRecordingPlugin : PluginBase
    {
        private static bool _registered;

        public static List<NamedObjectSave> SeenNamedObjectSaves { get; } = new();

        public static void EnsureRegistered()
        {
            if (!_registered)
            {
                PluginManager.RegisterPluginForTesting(new SelectionRecordingPlugin());
                _registered = true;
            }
        }

        public override string FriendlyName => nameof(SelectionRecordingPlugin);
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            ReactToItemsSelected += _ => SeenNamedObjectSaves.Add(GlueState.Self.CurrentNamedObjectSave);
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
