using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.PropertyGrid;
using OfficialPlugins.VariableDisplay;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;
using Xunit;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2221: with multiple objects selected (via ctrl+click in the tree view or in edit
// mode - both already funnel into GlueState.CurrentNamedObjectSaves), changing a property in the
// Variables tab only applied to the last-selected object. UpdateShownVariablesForMultipleObjects
// wraps each property shared across the selection in a MultiSelectInstanceMember (WpfDataUi's
// SetMultipleCategoryLists) instead of building the grid for a single object, and
// NamedObjectSaveVariableDataGridItem.MultiSetBatchTarget is what makes an edit apply to every
// wrapped object instead of just the one the grid item happened to be built from.
public class NamedObjectVariableShowingLogicMultiSelectTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public NamedObjectVariableShowingLogicMultiSelectTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static (ScreenSave screen, NamedObjectSave instance1, NamedObjectSave instance2, AssetTypeInfo ati)
        MakeTwoInstancesSharingAVariable()
    {
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        var ati = new AssetTypeInfo { FriendlyName = "TestType" };
        // RefreshFrom derives the grid's DisplayName from this by inserting spaces before capitals
        // (StringFunctions.InsertSpacesInCamelCaseString), so the raw variable name "ExtraVariable"
        // shows up (and gets grouped by SetMultipleCategoryLists) as DisplayName "Extra Variable":
        ati.VariableDefinitions.Add(new VariableDefinition { Name = "ExtraVariable", Type = "float" });

        var instance1 = new NamedObjectSave { InstanceName = "Instance1", SourceType = SourceType.FlatRedBallType };
        var instance2 = new NamedObjectSave { InstanceName = "Instance2", SourceType = SourceType.FlatRedBallType };
        screen.NamedObjects.Add(instance1);
        screen.NamedObjects.Add(instance2);

        return (screen, instance1, instance2, ati);
    }

    [StaFact]
    public void UpdateShownVariablesForMultipleObjects_ShouldGroupSharedPropertyIntoOneMultiSelectMemberCoveringBothInstances()
    {
        var (screen, instance1, instance2, ati) = MakeTwoInstancesSharingAVariable();

        var grid = new DataUiGrid();

        NamedObjectVariableShowingLogic.UpdateShownVariablesForMultipleObjects(
            grid, new List<NamedObjectSave> { instance1, instance2 }, screen, ati);

        var allMembers = grid.Categories.SelectMany(category => category.Members).ToList();

        var multiSelectMember = allMembers.OfType<MultiSelectInstanceMember>()
            .FirstOrDefault(item => item.DisplayName == "Extra Variable");

        multiSelectMember.ShouldNotBeNull();
        multiSelectMember.InstanceMembers.Count.ShouldBe(2);
        multiSelectMember.InstanceMembers.OfType<NamedObjectSaveVariableDataGridItem>()
            .Select(item => item.NamedObjectSave)
            .ShouldBe(new[] { instance1, instance2 }, ignoreOrder: true);
    }

    [StaFact]
    public void UpdateShownVariablesForMultipleObjects_ShouldNotShowNameMember()
    {
        // Renaming is destructive if applied identically to every selected object - it would give
        // them all the same name - so unlike every other property, "Name" is excluded from multi-select:
        var (screen, instance1, instance2, ati) = MakeTwoInstancesSharingAVariable();

        var grid = new DataUiGrid();

        NamedObjectVariableShowingLogic.UpdateShownVariablesForMultipleObjects(
            grid, new List<NamedObjectSave> { instance1, instance2 }, screen, ati);

        var allMembers = grid.Categories.SelectMany(category => category.Members).ToList();

        allMembers.Any(item => item.DisplayName == "Name").ShouldBeFalse();
    }

    [StaFact]
    public void MultiSetBatchTarget_ShouldReceiveOneAssignmentPerSelectedObject_NotJustTheLast()
    {
        // This is the literal regression from #2221: before this batching was added, each grid item
        // independently called GluxCommands.SetVariableOn for its own object, so nothing here fanned a
        // single edit out across the whole selection. WireUpBatchedMultiSet's BeforeMultiSet points
        // every selected object's grid item at the same batch list; this pins that each object - not
        // just whichever one happens to run last - contributes its own entry to that shared list.
        var (screen, instance1, instance2, ati) = MakeTwoInstancesSharingAVariable();

        var grid = new DataUiGrid();
        NamedObjectVariableShowingLogic.UpdateShownVariablesForMultipleObjects(
            grid, new List<NamedObjectSave> { instance1, instance2 }, screen, ati);

        var multiSelectMember = grid.Categories.SelectMany(category => category.Members)
            .OfType<MultiSelectInstanceMember>()
            .First(item => item.DisplayName == "Extra Variable");

        var innerItems = multiSelectMember.InstanceMembers.OfType<NamedObjectSaveVariableDataGridItem>().ToList();

        var batch = new List<NosVariableAssignment>();
        foreach (var innerItem in innerItems)
        {
            innerItem.MultiSetBatchTarget = batch;
        }

        // Simulates MultiSelectInstanceMember.HandleCustomSetEvent's fan-out (foreach inner.SetValue):
        foreach (var innerItem in innerItems)
        {
            innerItem.SetValue(2.5f, SetPropertyCommitType.Full);
        }

        batch.Count.ShouldBe(2);
        batch.Select(item => item.NamedObjectSave).ShouldBe(new[] { instance1, instance2 }, ignoreOrder: true);
        batch.ShouldAllBe(item => item.VariableName == "ExtraVariable" && (float)item.Value == 2.5f);
    }
}
