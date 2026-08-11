using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.VariableDisplay;
using Shouldly;
using WpfDataUi;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2034: selecting a named object whose source file went missing threw a
// KeyNotFoundException in the property grid. UpdateShownVariables builds the grid's categories
// from the AssetTypeInfo the caller passes in, but on a non-full-refresh update it re-derives a
// *fresh* AssetTypeInfo via NamedObjectSave.GetAssetTypeInfo() to look up each member's
// VariableDefinition. If that fresh lookup no longer contains a variable the caller-supplied ATI
// produced a grid member for - exactly what happens once the source file backing the instance's
// own ATI resolution disappears - the direct dictionary indexer threw instead of degrading
// gracefully.
public class NamedObjectVariableShowingLogicTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public NamedObjectVariableShowingLogicTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [StaFact]
    public void UpdateShownVariables_ShouldNotThrow_WhenInstanceAssetTypeInfoNoLongerHasAMemberTheGridWasBuiltFrom()
    {
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        // A caller-supplied ATI (mirrors MainPropertyGridPlugin.HandleNamedObjectSelect passing an
        // already-resolved ATI into UpdateShownVariables) with a variable that instance.ClassType
        // being unset means NamedObjectSave.GetAssetTypeInfo() will never independently resolve.
        var ati = new AssetTypeInfo { FriendlyName = "TestType" };
        ati.VariableDefinitions.Add(new VariableDefinition { Name = "ExtraVariable", Type = "float" });

        var instance = new NamedObjectSave
        {
            InstanceName = "TestInstance",
            SourceType = SourceType.FlatRedBallType,
        };
        screen.NamedObjects.Add(instance);

        var grid = new DataUiGrid();

        // First selection: grid is empty, so this always takes the full-refresh path and populates
        // grid.Categories directly from the categories built off the passed-in `ati`.
        NamedObjectVariableShowingLogic.UpdateShownVariables(grid, instance, screen, ati);
        grid.Categories.ShouldNotBeEmpty();

        // Second update with the exact same inputs: the newly-built categories match the grid's
        // existing shape, so this takes the non-full-refresh path, which looks up each member via
        // instance.GetAssetTypeInfo() instead of the `ati` that was passed in. That call returns
        // null (instance.ClassType is unset), so "ExtraVariable" is missing from the freshly
        // derived dictionary even though the grid has a member for it.
        Should.NotThrow(() => NamedObjectVariableShowingLogic.UpdateShownVariables(grid, instance, screen, ati));
    }
}
