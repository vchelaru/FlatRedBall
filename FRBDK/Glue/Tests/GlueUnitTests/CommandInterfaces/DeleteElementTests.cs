using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// Deleting a Screen used to ask up to four separate questions - the confirm, one popup per derived Screen
/// about resetting its inheritance, the Gum plugin's own popup about its Screen, and the leftover-files
/// dialog - each of them a modal that could end up behind the editor window, and each raised part-way
/// through the delete itself. These pin the single-dialog replacement: everything is asked once, up front,
/// and the delete then applies the answers. See GitHub issue #429.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DeleteElementTests : DeleteDialogTestBase
{
    private static async Task<(ScreenSave baseScreen, ScreenSave derived)> AddScreenWithDerivedAsync(
        string baseName, string derivedName)
    {
        var baseScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(baseName);
        var derived = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(derivedName);

        derived.BaseScreen = baseScreen.Name;

        return (baseScreen, derived);
    }

    // The reported symptom, stated as a count. Before the fix this screen produced a confirm, then one
    // "Reset the inheritance for..." popup per derived screen from inside RemoveScreen, then the Gum
    // plugin's own popup from inside ReactToScreenRemoved.
    [StaFact]
    public async Task DeletingAScreenWithDerivedScreens_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var (baseScreen, _) = await AddScreenWithDerivedAsync("DoomedBaseScreen", "DerivedOfDoomedScreen");
        await AddScreenWithDerivedAsync("Unused", "SecondDerivedOfDoomedScreen");
        ObjectFinder.Self.GlueProject.Screens
            .First(item => item.Name.EndsWith("SecondDerivedOfDoomedScreen")).BaseScreen = baseScreen.Name;

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(baseScreen);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
    }

    // The two derived screens are two separate opt-in choices in that one dialog, not two popups.
    [StaFact]
    public async Task TheDeleteDialog_ShouldOfferOneInheritanceResetOption_PerDerivedScreen()
    {
        using var project = await LoadFormsSampleAsync();

        var (baseScreen, derived) = await AddScreenWithDerivedAsync("OptionBaseScreen", "OptionDerivedScreen");

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(baseScreen);

        TheOnlyDeleteDialog.IsOptionChecked(new DeletionPlanner.ResetInheritanceTag { DerivedElement = derived })
            .ShouldBeTrue();
    }

    [StaFact]
    public async Task DeletingABaseScreen_ShouldResetTheDerivedScreensInheritance_WhenThatOptionIsChecked()
    {
        using var project = await LoadFormsSampleAsync();

        var (baseScreen, derived) = await AddScreenWithDerivedAsync("CheckedBaseScreen", "CheckedDerivedScreen");

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = true;
            }
        });

        await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(baseScreen);

        derived.BaseScreen.ShouldBe("");
    }

    [StaFact]
    public async Task DeletingABaseScreen_ShouldLeaveTheDerivedScreensInheritanceAlone_WhenThatOptionIsUnchecked()
    {
        using var project = await LoadFormsSampleAsync();

        var (baseScreen, derived) = await AddScreenWithDerivedAsync("UncheckedBaseScreen", "UncheckedDerivedScreen");
        var originalBaseName = derived.BaseScreen;

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = false;
            }
        });

        await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(baseScreen);

        derived.BaseScreen.ShouldBe(originalBaseName);
    }

    [StaFact]
    public async Task CancellingTheDeleteDialog_ShouldLeaveTheScreenInTheProject()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("SurvivingScreen");

        AnswerDeleteDialog(confirm: false);

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(screen);

        wasRemoved.ShouldBeFalse();
        ObjectFinder.Self.GlueProject.Screens.ShouldContain(screen);
    }

    // Entities take the same path, which is the point of unifying them - a fix or a regression on one is a
    // fix or a regression on both.
    [StaFact]
    public async Task DeletingAnEntity_ShouldAskTheUserExactlyOnce_AndHonorTheInheritanceOption()
    {
        using var project = await LoadFormsSampleAsync();

        var baseEntity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new GlueFormsCore.ViewModels.AddEntityViewModel { Name = "DoomedBaseEntity" });
        var derivedEntity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new GlueFormsCore.ViewModels.AddEntityViewModel { Name = "DerivedOfDoomedEntity" });
        derivedEntity.BaseEntity = baseEntity.Name;

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveEntityAsync(baseEntity);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        derivedEntity.BaseEntity.ShouldBe("");
    }

    // The dialog lists the files up front, before the delete has run, so the planner has to predict what
    // the delete will produce. This is the guard against the two drifting apart and the dialog quietly
    // under-reporting what it is about to delete.
    [StaFact]
    public async Task ThePlannedFileList_ShouldCoverEveryFileTheDeleteActuallyProduces()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("FileListScreen");

        List<string> planned = null;
        AnswerDeleteDialog(viewModel => planned = viewModel.FilesToRemove.ToList());

        var actual = new List<string>();
        var executionViewModel = DeletionPlanner.CreateForScreen(screen);
        await GlueCommands.Self.GluxCommands.RemoveScreenAsync(screen, executionViewModel, actual);

        planned = DeletionPlanner.GetFilesThatWouldBeRemoved(screen);

        foreach (var file in actual)
        {
            planned.ShouldContain(file,
                $"the delete produced {file}, which the dialog never showed the user");
        }
    }

    // The Gum plugin used to raise its own MessageBox from inside ReactToScreenRemoved. It now contributes
    // a checkbox to the same dialog, which is the plugin half of the fix.
    [StaFact]
    public async Task TheGumPlugin_ShouldContributeAnOptionToTheDeleteDialog_RatherThanShowingItsOwn()
    {
        using var project = await LoadFormsSampleAsync();

        // MainMenu is the sample's Screen that has a matching Gum screen.
        var screen = ObjectFinder.Self.GlueProject.Screens
            .First(item => item.Name.EndsWith("MainMenu"));

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(screen);

        TheOnlyDeleteDialog.Options.ShouldContain(
            option => Equals(option.Tag, "GumPlugin.DeleteGumScreen"),
            "the Gum plugin should ask about its screen as a checkbox in Glue's delete dialog");
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
    }
}
