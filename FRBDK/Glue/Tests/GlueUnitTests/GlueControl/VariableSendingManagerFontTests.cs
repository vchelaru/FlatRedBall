using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

// GitHub issue #2266: during live edit, setting a Text's Font to <NONE> and then "Make Default" crashes
// Glue. Both steps reach the game through RefreshManager's async void handlers, so any exception in the
// DTO conversion below takes the process down instead of landing in the output tab. These pin the Glue-side
// conversion for both shapes the report produces: the instruction holding the "<NONE>" sentinel, and the
// instruction gone entirely (what Make Default leaves behind).
public class VariableSendingManagerFontTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly VariableSendingManager _sendingManager;
    private readonly NamedObjectSave _text;

    public VariableSendingManagerFontTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        glueProject.Screens.Add(screen);
        _text = new NamedObjectSave
        {
            InstanceName = "TextInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Graphics.Text",
        };
        screen.NamedObjects.Add(_text);
        ObjectFinder.Self.GlueProject = glueProject;

        var refreshManager = new RefreshManager((_, _) => System.Threading.Tasks.Task.FromResult(""), (_, _) => { });
        _sendingManager = new VariableSendingManager(refreshManager);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void GetNamedObjectValueChangedDtos_ShouldNotThrow_WhenFontInstructionIsNoneSentinel()
    {
        _text.SetVariable("Font", "<NONE>");

        List<GlueVariableSetData> dtos = null;
        Should.NotThrow(() => dtos = _sendingManager.GetNamedObjectValueChangedDtos(
            "Font", oldValue: "SomeFont", _text, AssignOrRecordOnly.Assign, gameScreenName: "Screens.GameScreen"));

        dtos.ShouldHaveSingleItem();
    }

    [Fact]
    public void GetNamedObjectValueChangedDtos_ShouldNotThrow_WhenFontInstructionWasRemovedByMakeDefault()
    {
        // Make Default removes the instruction outright (PropertyGridRightClickHelper.SetVariableToDefault),
        // then notifies plugins with the old "<NONE>" value:
        _text.InstructionSaves.RemoveAll(item => item.Member == "Font");

        List<GlueVariableSetData> dtos = null;
        Should.NotThrow(() => dtos = _sendingManager.GetNamedObjectValueChangedDtos(
            "Font", oldValue: "<NONE>", _text, AssignOrRecordOnly.Assign, gameScreenName: "Screens.GameScreen"));

        // BitmapFont is a reference type with no primitive default, so the cleared value goes over as null -
        // the game's VariableAssignmentLogic already treats a null BitmapFont as "no font".
        var dto = dtos.ShouldHaveSingleItem();
        dto.Type.ShouldBe("BitmapFont");
        dto.VariableValue.ShouldBeNull();
    }
}
