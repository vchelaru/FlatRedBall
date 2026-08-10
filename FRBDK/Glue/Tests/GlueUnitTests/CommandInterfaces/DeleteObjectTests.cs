using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// Objects had a delete dialog of their own, <c>RemoveObjectWindow</c>, which predated the shared view
/// model: no options, no file section, and a leftover-files dialog afterwards if anything was orphaned.
/// These pin the move onto the one delete dialog. See GitHub issue #2032.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DeleteObjectTests : DeleteDialogTestBase
{
    static async Task<NamedObjectSave> AddObjectAsync(ScreenSave screen, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };

        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(nos, screen);

        return nos;
    }

    [StaFact]
    public async Task DeletingAnObject_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("ObjectOwnerScreen");
        var nos = await AddObjectAsync(screen, "DoomedSprite");

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveObjectAsync(nos);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        screen.NamedObjects.ShouldNotContain(nos);
    }

    [StaFact]
    public async Task TheDeleteDialog_ShouldListTheVariablesThatComeWithTheObject()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("TunneledVariableScreen");
        var nos = await AddObjectAsync(screen, "TunnelledSprite");

        screen.CustomVariables.Add(new CustomVariable
        {
            Name = "SpriteX",
            SourceObject = nos.InstanceName,
            SourceObjectProperty = "X",
            Type = "float"
        });

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveObjectAsync(nos);

        TheOnlyDeleteDialog.ObjectsToRemove.ShouldContain(item => item.Contains("SpriteX"));
    }

    [StaFact]
    public async Task CancellingTheDeleteDialog_ShouldLeaveTheObjectInTheProject()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("SurvivingObjectScreen");
        var nos = await AddObjectAsync(screen, "SurvivingSprite");

        AnswerDeleteDialog(confirm: false);

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveObjectAsync(nos);

        wasRemoved.ShouldBeFalse();
        screen.NamedObjects.ShouldContain(nos);
    }

    // Deleting several objects at once is still one dialog, not one per object.
    [StaFact]
    public async Task DeletingSeveralObjects_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("SeveralObjectsScreen");
        var first = await AddObjectAsync(screen, "FirstSprite");
        var second = await AddObjectAsync(screen, "SecondSprite");

        AnswerDeleteDialog();

        await GlueCommands.Self.DialogCommands.AskToRemoveObjectListAsync(
            new List<NamedObjectSave> { first, second });

        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        screen.NamedObjects.ShouldBeEmpty();
    }
}
