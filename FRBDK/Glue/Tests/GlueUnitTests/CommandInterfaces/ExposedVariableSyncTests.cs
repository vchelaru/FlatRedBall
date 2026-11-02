using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// An exposed (tunneled) variable and the instance member it points at are supposed to hold the same value, and
/// the generated code relies on that: the instance value is written first and the exposed default is written
/// over it later, so a disagreement makes the object "switch back" to the exposed value. See GitHub issue #1728.
/// Setting an instance value one at a time keeps the exposed default in step; setting several at once
/// (<see cref="IGluxCommands.SetVariableOnList"/>, used by multi-select edits and edits pushed from the game) must too.
/// </summary>
// Loads a real project, which replaces ObjectFinder.Self.GlueProject, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ExposedVariableSyncTests : DeleteDialogTestBase
{
    private static async Task<(ScreenSave screen, NamedObjectSave sprite, CustomVariable exposed)> AddScreenWithExposedRedAsync(
        string screenName)
    {
        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(screenName);

        var sprite = new NamedObjectSave
        {
            InstanceName = "SpriteInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(sprite, screen);

        var exposed = new CustomVariable
        {
            Name = "SpriteInstanceRed",
            Type = "float",
            SourceObject = sprite.InstanceName,
            SourceObjectProperty = "Red",
            DefaultValue = 0.5f
        };
        screen.CustomVariables.Add(exposed);

        return (screen, sprite, exposed);
    }

    [StaFact]
    public async Task SettingOneInstanceValue_UpdatesTheExposedDefault()
    {
        using var project = await LoadFormsSampleAsync();
        var (_, sprite, exposed) = await AddScreenWithExposedRedAsync("SyncSingleScreen");

        await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
            sprite, "Red", 0.2f, performSaveAndGenerateCode: false, updateUi: false);

        exposed.DefaultValue.ShouldBe(0.2f);
    }

    [StaFact]
    public async Task SettingInstanceValuesAsAList_UpdatesTheExposedDefault()
    {
        using var project = await LoadFormsSampleAsync();
        var (_, sprite, exposed) = await AddScreenWithExposedRedAsync("SyncListScreen");

        await GlueCommands.Self.GluxCommands.SetVariableOnList(
            new List<NosVariableAssignment>
            {
                new NosVariableAssignment { NamedObjectSave = sprite, VariableName = "Red", Value = 0.2f }
            },
            performSaveAndGenerateCode: false, updateUi: false);

        exposed.DefaultValue.ShouldBe(0.2f);
    }
}
