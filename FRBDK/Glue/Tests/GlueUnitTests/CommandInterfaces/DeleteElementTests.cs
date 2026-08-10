using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
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
public class DeleteElementTests : IDisposable
{
    private readonly Func<DeleteOptionsViewModel, bool> _originalShowDeleteImpl = DialogService.ShowDeleteImpl;
    private readonly Func<string, DialogButton, DialogButton, DialogButton?> _originalShowConfirmImpl = DialogService.ShowConfirmImpl;
    private readonly Func<string, (string label, object value)[], object> _originalShowChoiceImpl = DialogService.ShowChoiceImpl;

    /// <summary>
    /// Every view model handed to the one delete dialog, in order. Length is the assertion that matters:
    /// the whole point of the issue is that a delete asks once.
    /// </summary>
    private readonly List<DeleteOptionsViewModel> _shownDeleteDialogs = new();

    /// <summary>
    /// Anything that reached one of the *other* dialog seams during a delete. A delete that populates this
    /// is asking a question outside the single dialog, which is the regression being guarded against.
    /// </summary>
    private readonly List<string> _otherPrompts = new();

    public DeleteElementTests()
    {
        DialogService.ShowConfirmImpl = (text, _, __) => { _otherPrompts.Add(text); return null; };
        DialogService.ShowChoiceImpl = (text, _) => { _otherPrompts.Add(text); return null; };
    }

    public void Dispose()
    {
        DialogService.ShowDeleteImpl = _originalShowDeleteImpl;
        DialogService.ShowConfirmImpl = _originalShowConfirmImpl;
        DialogService.ShowChoiceImpl = _originalShowChoiceImpl;
    }

    /// <summary>
    /// Answers the delete dialog the way a user would: records what it was asked, optionally adjusts the
    /// options, and clicks Delete.
    /// </summary>
    private void AnswerDeleteDialog(Action<DeleteOptionsViewModel> adjust = null, bool confirm = true)
    {
        DialogService.ShowDeleteImpl = viewModel =>
        {
            _shownDeleteDialogs.Add(viewModel);
            adjust?.Invoke(viewModel);
            return confirm;
        };
    }

    private static async Task<(ScreenSave baseScreen, ScreenSave derived)> AddScreenWithDerivedAsync(
        string baseName, string derivedName)
    {
        var baseScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(baseName);
        var derived = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(derivedName);

        derived.BaseScreen = baseScreen.Name;

        return (baseScreen, derived);
    }

    private static async Task<TempDir> LoadFormsSampleAsync()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        await GoldProject.LoadInGlueAsync(
            Path.Combine(project.Root, "FormsSampleProject", "FormsSampleProject.csproj"));

        return project;
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
        _shownDeleteDialogs.Count.ShouldBe(1);
        _otherPrompts.ShouldBeEmpty(
            "every question a delete asks belongs in the one delete dialog: " + string.Join(" | ", _otherPrompts));
    }

    // The two derived screens are two separate opt-in choices in that one dialog, not two popups.
    [StaFact]
    public async Task TheDeleteDialog_ShouldOfferOneInheritanceResetOption_PerDerivedScreen()
    {
        using var project = await LoadFormsSampleAsync();

        var (baseScreen, derived) = await AddScreenWithDerivedAsync("OptionBaseScreen", "OptionDerivedScreen");

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveScreenAsync(baseScreen);

        var viewModel = _shownDeleteDialogs.Single();
        viewModel.IsOptionChecked(new DeletionPlanner.ResetInheritanceTag { DerivedElement = derived })
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
        _shownDeleteDialogs.Count.ShouldBe(1);
        _otherPrompts.ShouldBeEmpty(
            "every question a delete asks belongs in the one delete dialog: " + string.Join(" | ", _otherPrompts));
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

        var viewModel = _shownDeleteDialogs.Single();
        viewModel.Options.ShouldContain(
            option => Equals(option.Tag, "GumPlugin.DeleteGumScreen"),
            "the Gum plugin should ask about its screen as a checkbox in Glue's delete dialog");
        _otherPrompts.ShouldBeEmpty();
    }
}
