using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Reproduces issue #2125 end to end against a real running game: Edit Mode selection previously only
/// let Ctrl+click add/remove an object from the current selection - Shift+click did nothing but replace
/// it, same as a plain click. Drives EditingManager.SimulateClickSelectForTesting (Embedded) - the exact
/// same production click-selection decision (EditingManager.PerformClickSelection) a real mouse click with
/// a modifier key held would run, just fed an explicit "modifier held" bool instead of reading
/// InputManager.Keyboard, so this exercises the real DTO -> EditingManager wiring, not just the modifier
/// key read in isolation.
/// </summary>
[Trait("Category", "LiveGame")]
public class ShiftClickMultiSelectTests
{
    [StaFact]
    public async Task ShiftClickingAnUnselectedObject_AddsItToTheSelection_AndShiftClickingItAgain_RemovesIt()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTwoTestObjectsToGameScreen);

        var screen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var loadScreenResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
        });
        loadScreenResponse.Succeeded.ShouldBeTrue(loadScreenResponse.Message);

        // See PolygonPointDragSnapTests for why edit mode must be entered before selecting.
        var editModeResponse = await game.Send(new SetEditMode
        {
            IsInEditMode = true,
            AbsoluteGlueProjectFilePath = System.IO.Path.Combine(game.ProjectRoot, "EditorTest1", "EditorTest1.gluj"),
        });
        editModeResponse.Succeeded.ShouldBeTrue(editModeResponse.Message);
        await Task.Delay(1000);

        // Plain click on TestObjectA - selects it alone.
        var clickAResponse = await game.Send<SimulateClickSelectResponse>(new SimulateClickSelectDto
        {
            ObjectName = "TestObjectA",
            AdditiveModifierDown = false,
        });
        clickAResponse.Succeeded.ShouldBeTrue(clickAResponse.Message);
        clickAResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectA" });

        // Shift+click on TestObjectB - adds it, TestObjectA stays selected.
        var shiftClickBResponse = await game.Send<SimulateClickSelectResponse>(new SimulateClickSelectDto
        {
            ObjectName = "TestObjectB",
            AdditiveModifierDown = true,
        });
        shiftClickBResponse.Succeeded.ShouldBeTrue(shiftClickBResponse.Message);
        shiftClickBResponse.Data.SelectedObjectNames.OrderBy(name => name).ShouldBe(new[] { "TestObjectA", "TestObjectB" });

        // Shift+click on TestObjectA again - toggles it back off, TestObjectB stays selected.
        var shiftClickARemoveResponse = await game.Send<SimulateClickSelectResponse>(new SimulateClickSelectDto
        {
            ObjectName = "TestObjectA",
            AdditiveModifierDown = true,
        });
        shiftClickARemoveResponse.Succeeded.ShouldBeTrue(shiftClickARemoveResponse.Message);
        shiftClickARemoveResponse.Data.SelectedObjectNames.ShouldBe(new[] { "TestObjectB" });
    }

    static async Task AddTwoTestObjectsToGameScreen()
    {
        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

        foreach (var instanceName in new[] { "TestObjectA", "TestObjectB" })
        {
            var nos = new NamedObjectSave();
            nos.SetDefaults();
            nos.InstanceName = instanceName;
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "FlatRedBall.Math.Geometry.AxisAlignedRectangle";

            await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
                nos, gameScreen, listToAddTo: null, selectNewNos: false,
                performSaveAndGenerateCode: true, updateUi: false);
        }
    }
}
