using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.Reflection;
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

    // GlueState.CurrentNamedObjectSave's setter round-trips through Find.TreeNodeByTag, which FakeFindManager
    // deliberately doesn't resolve for NamedObjectSave (see FakeFindManager's doc comment) - so this reaches
    // past the snapshot field directly instead, to mark the object selected without touching real tree/plugin
    // selection-reaction code that has nothing to do with the bug under test.
    static void SetCurrentNamedObjectSaveDirectly(NamedObjectSave nos)
    {
        var snapshotField = typeof(GlueState).GetField("snapshot", BindingFlags.NonPublic | BindingFlags.Instance);
        var snapshot = snapshotField.GetValue(GlueState.Self);
        var namedObjectSavesField = snapshot.GetType().GetField("CurrentNamedObjectSaves");
        namedObjectSavesField.SetValue(snapshot, new List<NamedObjectSave> { nos });
    }

    // Reported crash (GitHub issue #2142): removal is tasked, so a fast double-removal (e.g. the running
    // game's live sync racing a manual delete) can find the object already gone from its element by the
    // time RemoveNamedObjectAsync looks it up, while it's still the current selection. RemoveNamedObjectAsync
    // isn't on IGluxCommands (it's an internal implementation detail used by RemoveNamedObjectListAsync), so
    // this calls the concrete type directly to reproduce the exact method named in the reported stack trace.
    [StaFact]
    public async Task RemovingTheSelectedObject_ShouldNotThrow_WhenItWasAlreadyRemovedFromItsElement()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("AlreadyRemovedScreen");
        var nos = await AddObjectAsync(screen, "DoomedSprite");

        SetCurrentNamedObjectSaveDirectly(nos);
        screen.NamedObjects.Remove(nos);

        var gluxCommands = (GluxCommands)GlueCommands.Self.GluxCommands;
        await gluxCommands.RemoveNamedObjectAsync(nos);
    }
}
