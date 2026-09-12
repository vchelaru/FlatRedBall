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

// GitHub issue #2272: during live edit, changing a Text's MaxWidthBehavior (or HorizontalAlignment,
// VerticalAlignment, BlendOperation) and then "Make Default" crashed Glue. The throw itself was the same
// one as issue #2266 - TypeManager.GetDefaultForType has no default for any of those type names - and it
// reached Glue's process-level handler through RefreshManager's async void handlers.
//
// Not throwing is only half of it though: "Make Default" has to tell the running game what the default
// actually is. These pin the value each of the reported variables sends once its instruction is gone,
// which is the shape Make Default leaves behind (PropertyGridRightClickHelper.SetVariableToDefault
// removes the instruction, then notifies plugins with the old value).
public class VariableSendingManagerMakeDefaultEnumTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly VariableSendingManager _sendingManager;
    private readonly NamedObjectSave _text;

    public VariableSendingManagerMakeDefaultEnumTests()
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

    [Theory]
    // The report's own repro: set MaxWidthBehavior to Wrap, then Make Default. Chop is 0.
    [InlineData("MaxWidthBehavior", "Wrap", "MaxWidthBehavior", "0")]
    [InlineData("HorizontalAlignment", "Right", "HorizontalAlignment", "0")]
    // Center is 2 (the members are Top, Bottom, Center), so this is the case that proves the value comes
    // from the AssetTypeInfo's declared default rather than from the type's CLR default.
    [InlineData("VerticalAlignment", "Top", "VerticalAlignment", "2")]
    [InlineData("BlendOperation", "Add", "BlendOperation", "0")]
    // ColorOperation didn't crash, because TypeManager does have an entry for it - but that entry says
    // "0" (Texture), while a Text's ColorOperation default is ColorTextureAlpha (6).
    [InlineData("ColorOperation", "Add", "ColorOperation", "6")]
    public void GetNamedObjectValueChangedDtos_ShouldSendTheDeclaredDefault_WhenAnEnumIsMadeDefault(
        string memberName, string oldValue, string expectedType, string expectedValue)
    {
        // Make Default removes the instruction outright, then notifies plugins with the old value:
        _text.SetVariable(memberName, oldValue);
        _text.InstructionSaves.RemoveAll(item => item.Member == memberName);

        List<GlueVariableSetData> dtos = null;
        Should.NotThrow(() => dtos = _sendingManager.GetNamedObjectValueChangedDtos(
            memberName, oldValue, _text, AssignOrRecordOnly.Assign, gameScreenName: "Screens.GameScreen"));

        var dto = dtos.ShouldHaveSingleItem();
        dto.Type.ShouldBe(expectedType);
        // The underlying number, not the member name: the game's ConvertStringToType parses an integer for
        // every enum it handles, but only parses a member name under the MONOGAME_381/FNA define.
        dto.VariableValue.ShouldBe(expectedValue);
    }

    [Fact]
    public void GetNamedObjectValueChangedDtos_ShouldStillSendTheSetValue_WhenAnEnumIsNotDefault()
    {
        // The declared default must only stand in for a cleared variable, never overwrite a real one.
        _text.SetVariable("MaxWidthBehavior", "Wrap");

        var dto = _sendingManager.GetNamedObjectValueChangedDtos(
            "MaxWidthBehavior", "Chop", _text, AssignOrRecordOnly.Assign, gameScreenName: "Screens.GameScreen")
            .ShouldHaveSingleItem();

        dto.VariableValue.ShouldBe("Wrap");
    }
}
