using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// Removing a file used to ask a question per object that referenced it, each one raised from inside the
/// removal itself, plus a confirm before and a leftover-files dialog after. Deleting one .png used by three
/// Sprites meant five modals. These pin the single-dialog replacement. See GitHub issue #2032.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DeleteFileTests : DeleteDialogTestBase
{
    const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    /// <summary>
    /// Adds a real .png to a Screen - a ReferencedFileSave needs a file on disk to be created from.
    /// </summary>
    static async Task<ReferencedFileSave> AddFileToScreenAsync(TempDir project, ScreenSave screen, string fileName)
    {
        var png = Path.Combine(project.Root, "FormsSampleProject", "Content", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, Convert.FromBase64String(OnePixelPngBase64));

        var response = await GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFileAsync(
            screen, new FilePath(png));

        response.Succeeded.ShouldBeTrue(response.Message);

        return response.Data;
    }

    /// <summary>
    /// The shape that used to produce one popup per object, raised from inside the removal itself.
    /// </summary>
    static NamedObjectSave AddObjectUsingFile(ScreenSave screen, ReferencedFileSave file, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.File,
            SourceFile = file.Name,
            SourceClassType = "FlatRedBall.Sprite"
        };

        screen.NamedObjects.Add(nos);

        return nos;
    }

    /// <summary>
    /// What pressing Delete on a file in the tree view does.
    /// </summary>
    static async Task DeleteSelectedFileAsync(ReferencedFileSave file)
    {
        GlueState.Self.CurrentReferencedFileSave = file;

        await GlueCommands.Self.GluxCommands.RemoveFromProjectOptionalSaveAndRegenerate();
    }

    // The reported symptom, stated as a count: a confirm of its own, then one "what would you like to do?"
    // per object, then the leftover-files dialog.
    [StaFact]
    public async Task RemovingAFileUsedByObjects_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("FileUserScreen");
        var file = await AddFileToScreenAsync(project, screen, "DoomedFile.png");
        AddObjectUsingFile(screen, file, "FirstSprite");
        AddObjectUsingFile(screen, file, "SecondSprite");

        AnswerDeleteDialog();

        await DeleteSelectedFileAsync(file);

        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
    }

    // Each object that used the file is a checkbox in that one dialog rather than a popup of its own.
    [StaFact]
    public async Task TheDeleteDialog_ShouldOfferOneRemoveOption_PerObjectUsingTheFile()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("OptionPerObjectScreen");
        var file = await AddFileToScreenAsync(project, screen, "OptionPerObject.png");
        var first = AddObjectUsingFile(screen, file, "FirstSprite");
        var second = AddObjectUsingFile(screen, file, "SecondSprite");

        AnswerDeleteDialog(confirm: false);

        await DeleteSelectedFileAsync(file);

        TheOnlyDeleteDialog.Options.Count(item => item.Tag is DeletionPlanner.RemoveNamedObjectTag).ShouldBe(2);
        TheOnlyDeleteDialog.IsOptionChecked(new DeletionPlanner.RemoveNamedObjectTag { NamedObject = first })
            .ShouldBeTrue();
        TheOnlyDeleteDialog.IsOptionChecked(new DeletionPlanner.RemoveNamedObjectTag { NamedObject = second })
            .ShouldBeTrue();
    }

    [StaFact]
    public async Task RemovingAFile_ShouldRemoveTheObjectsUsingIt_WhenThoseOptionsAreChecked()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("CheckedObjectScreen");
        var file = await AddFileToScreenAsync(project, screen, "CheckedObject.png");
        AddObjectUsingFile(screen, file, "DoomedSprite");

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = true;
            }
        });

        await DeleteSelectedFileAsync(file);

        screen.NamedObjects.ShouldNotContain(item => item.InstanceName == "DoomedSprite");
    }

    [StaFact]
    public async Task RemovingAFile_ShouldLeaveTheObjectsUsingItAlone_WhenThoseOptionsAreUnchecked()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("UncheckedObjectScreen");
        var file = await AddFileToScreenAsync(project, screen, "UncheckedObject.png");
        AddObjectUsingFile(screen, file, "SurvivingSprite");

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = false;
            }
        });

        await DeleteSelectedFileAsync(file);

        screen.NamedObjects.ShouldContain(item => item.InstanceName == "SurvivingSprite");
    }

    // An object a base element defines can't be removed here at all, so it is a warning in the dialog rather
    // than the message box it used to raise mid-removal.
    [StaFact]
    public async Task TheDeleteDialog_ShouldWarnAboutObjectsDefinedByBase_RatherThanShowingAMessageBox()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("DefinedByBaseScreen");
        var file = await AddFileToScreenAsync(project, screen, "DefinedByBase.png");
        var nos = AddObjectUsingFile(screen, file, "InheritedSprite");
        nos.DefinedByBase = true;

        AnswerDeleteDialog();

        await DeleteSelectedFileAsync(file);

        TheOnlyDeleteDialog.Warnings.ShouldContain(item => item.Contains("InheritedSprite"));
        TheOnlyDeleteDialog.Options.ShouldNotContain(item => item.Tag is DeletionPlanner.RemoveNamedObjectTag,
            "an object a base element defines cannot be removed here, so it is not offered as an option");
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        screen.NamedObjects.ShouldContain(item => item.InstanceName == "InheritedSprite");
    }

    [StaFact]
    public async Task CancellingTheDeleteDialog_ShouldLeaveTheFileInTheProject()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("SurvivingFileScreen");
        var file = await AddFileToScreenAsync(project, screen, "SurvivingFile.png");

        AnswerDeleteDialog(confirm: false);

        await DeleteSelectedFileAsync(file);

        screen.ReferencedFiles.ShouldContain(file);
    }

    // The dialog lists the files before the removal has run, so the planner has to predict what the removal
    // will produce - the same guard the element deletes have.
    [StaFact]
    public async Task ThePlannedFileList_ShouldCoverTheFileBeingRemoved()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("PlannedFileScreen");
        var file = await AddFileToScreenAsync(project, screen, "PlannedFile.png");

        AnswerDeleteDialog(confirm: false);

        await DeleteSelectedFileAsync(file);

        TheOnlyDeleteDialog.FilesToRemove.ShouldContain(
            DeletionPlanner.ToCanonicalPath(file.GetRelativePath()));
    }
}
