using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// States, state categories and variables each asked a plain yes/no and then, separately, what to do with
/// any files left behind - and a state delete raised its own popup per variable it had orphaned, from
/// inside the delete. These pin the move onto the one delete dialog. See GitHub issue #2032.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DeleteStateTests : DeleteDialogTestBase
{
    static async Task<StateSave> AddStateAsync(ScreenSave screen, string name, StateSaveCategory category = null)
    {
        var state = new StateSave { Name = name };

        await GlueCommands.Self.GluxCommands.AddStateSave(state, category, screen);

        return state;
    }

    /// <summary>
    /// A variable exposing the element's uncategorized states. It is the one left dangling when the last
    /// such state goes away, which is what the per-variable popup used to be about.
    /// </summary>
    static CustomVariable AddCurrentStateVariable(ScreenSave screen)
    {
        var variable = new CustomVariable
        {
            Name = "CurrentState",
            Type = "VariableState"
        };

        screen.CustomVariables.Add(variable);

        return variable;
    }

    [StaFact]
    public async Task DeletingAState_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("StateOwnerScreen");
        var state = await AddStateAsync(screen, "DoomedState");

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(state);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        screen.States.ShouldNotContain(state);
    }

    // The variable left without any state to refer to is a checkbox in that one dialog rather than a popup
    // raised part-way through the delete.
    [StaFact]
    public async Task TheDeleteDialog_ShouldOfferToRemoveVariablesLeftWithoutAState()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("OrphanedVariableScreen");
        var state = await AddStateAsync(screen, "OnlyState");
        var variable = AddCurrentStateVariable(screen);

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(state);

        TheOnlyDeleteDialog.IsOptionChecked(
            new DeletionPlanner.RemoveCustomVariableTag { CustomVariable = variable }).ShouldBeTrue();
    }

    [StaFact]
    public async Task DeletingAState_ShouldRemoveTheOrphanedVariable_WhenThatOptionIsChecked()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("CheckedVariableScreen");
        var state = await AddStateAsync(screen, "OnlyState");
        var variable = AddCurrentStateVariable(screen);

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = true;
            }
        });

        await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(state);

        screen.CustomVariables.ShouldNotContain(variable);
    }

    [StaFact]
    public async Task DeletingAState_ShouldLeaveTheOrphanedVariableAlone_WhenThatOptionIsUnchecked()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("UncheckedVariableScreen");
        var state = await AddStateAsync(screen, "OnlyState");
        var variable = AddCurrentStateVariable(screen);

        AnswerDeleteDialog(viewModel =>
        {
            foreach (var option in viewModel.Options)
            {
                option.IsChecked = false;
            }
        });

        await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(state);

        screen.CustomVariables.ShouldContain(variable);
    }

    // A state that isn't the last one leaves the variable with something to refer to, so there is nothing
    // to offer.
    [StaFact]
    public async Task TheDeleteDialog_ShouldOfferNothing_WhenAnotherStateRemains()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("SurvivingStateScreen");
        var doomed = await AddStateAsync(screen, "DoomedState");
        await AddStateAsync(screen, "SurvivingState");
        AddCurrentStateVariable(screen);

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(doomed);

        TheOnlyDeleteDialog.Options.ShouldBeEmpty();
    }

    [StaFact]
    public async Task CancellingTheDeleteDialog_ShouldLeaveTheStateInTheElement()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("CancelledStateScreen");
        var state = await AddStateAsync(screen, "SurvivingState");

        AnswerDeleteDialog(confirm: false);

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveStateAsync(state);

        wasRemoved.ShouldBeFalse();
        screen.States.ShouldContain(state);
    }

    // A category takes every variable of its type with it - not a choice, so the dialog lists them.
    [StaFact]
    public async Task DeletingAStateCategory_ShouldAskOnce_AndListTheVariablesItTakesWithIt()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("CategoryOwnerScreen");
        var category = new StateSaveCategory { Name = "DoomedCategory" };
        screen.StateCategoryList.Add(category);

        var variable = new CustomVariable
        {
            Name = "CurrentDoomedCategoryState",
            Type = screen.Name.Replace("\\", ".") + ".DoomedCategory"
        };
        screen.CustomVariables.Add(variable);

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveStateCategoryAsync(category);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        ShownDeleteDialogs.Single().ObjectsToRemove.ShouldContain(item => item.Contains("CurrentDoomedCategoryState"));
        screen.StateCategoryList.ShouldNotContain(category);
        screen.CustomVariables.ShouldNotContain(variable);
    }

    [StaFact]
    public async Task DeletingACustomVariable_ShouldAskTheUserExactlyOnce()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("VariableOwnerScreen");
        var variable = new CustomVariable { Name = "DoomedVariable", Type = "float" };
        screen.CustomVariables.Add(variable);

        AnswerDeleteDialog();

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveCustomVariableAsync(variable);

        wasRemoved.ShouldBeTrue();
        ShownDeleteDialogs.Count.ShouldBe(1);
        OtherPrompts.ShouldBeEmpty(OtherPromptsMessage);
        screen.CustomVariables.ShouldNotContain(variable);
    }

    [StaFact]
    public async Task CancellingTheDeleteDialog_ShouldLeaveTheVariableInTheElement()
    {
        using var project = await LoadFormsSampleAsync();

        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("CancelledVariableScreen");
        var variable = new CustomVariable { Name = "SurvivingVariable", Type = "float" };
        screen.CustomVariables.Add(variable);

        AnswerDeleteDialog(confirm: false);

        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveCustomVariableAsync(variable);

        wasRemoved.ShouldBeFalse();
        screen.CustomVariables.ShouldContain(variable);
    }
}
