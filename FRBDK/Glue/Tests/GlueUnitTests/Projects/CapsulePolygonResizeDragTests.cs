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
/// Reproduces issue #2087 end to end against a real running game: drag-resizing a CapsulePolygon's
/// height down past its minimum (CapsulePolygon.Height throws for value &lt; 1) crashed the game with an
/// unhandled ArgumentException. Drives SelectionMarker.SimulateResizeDragForTesting (Embedded) - the
/// exact same production drag code a real mouse drag on a resize handle would run, just fed an explicit
/// world-space delta instead of reading Cursor/hardware, so this exercises the real DTO -> EditingManager
/// -> SelectionMarker.ChangeSizeBy wiring, not just CapsulePolygon's own validation in isolation.
/// </summary>
[Trait("Category", "LiveGame")]
public class CapsulePolygonResizeDragTests
{
    [StaFact]
    public async Task DraggingACapsulePolygonResizeHandle_PastItsMinimumHeight_ShouldNotCrash()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await LiveGameProcess.StartAsync(
            "Samples/EditorTest1",
            csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
            exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe",
            afterLoadBeforeEmbed: AddTestCapsuleToGameScreen);

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

        var selectResponse = await game.Send(new SelectObjectDto
        {
            ElementNameGlue = "Screens\\GameScreen",
            ScreenSave = screen,
            NamedObjectNames = { "TestCapsule" },
        });
        selectResponse.Succeeded.ShouldBeTrue(selectResponse.Message);

        // Drag the Bottom handle far enough in one step to try to shrink height well past 0 - a real
        // drag can easily move the mouse further in a frame than the object's remaining size.
        // ChangeSizeBy's heightMultiple for Bottom is -1 (the bottom edge moves toward the center as it
        // shrinks), so a POSITIVE WorldYChange here is what shrinks the capsule, not a negative one.
        var dragResponse = await game.Send<SimulateResizeDragResponse>(new SimulateResizeDragDto
        {
            Side = "Bottom",
            WorldXChange = 0,
            WorldYChange = 1000,
        });

        dragResponse.Succeeded.ShouldBeTrue(dragResponse.Message);
        dragResponse.Data.ScaleY.ShouldBeGreaterThanOrEqualTo(0.5f,
            "CapsulePolygon.Height cannot go below 1 (ScaleY below 0.5) - the drag should clamp instead of crashing");

        var capturedOutput = game.GetCapturedStandardOutputLines();
        capturedOutput.ShouldNotContain(line => line.Contains("ArgumentException"),
            "a rejected resize should be handled gracefully, not thrown up through Update() and logged as an unhandled exception");
    }

    static async Task AddTestCapsuleToGameScreen()
    {
        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

        var nos = new NamedObjectSave();
        nos.SetDefaults();
        nos.InstanceName = "TestCapsule";
        nos.SourceType = SourceType.FlatRedBallType;
        nos.SourceClassType = "FlatRedBall.Math.Geometry.CapsulePolygon";

        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            nos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: true, updateUi: false);
    }
}
