using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.ViewModels;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GlueUnitTests.StateDataPlugin;

// Regression test: the not-yet-named blank row at the bottom of the State grid has no
// backing StateSave yet. Setting one of its variable values used to unconditionally reach
// GlueState.Self.CurrentElement (null outside a loaded project) and StateSave.SetValue with a
// null StateSave, throwing. See StateCategoryViewModel.HandleStateViewModelValueChanged - it
// now no-ops on a null BackingData, matching the guard ApplyViewModelVariableToStateAtIndex
// already had.
//
// This calls the private handler directly via reflection rather than through the
// StateVariableViewModel.Value setter: that setter's PropertyChanged handler
// (StateViewModel.HandleStateVariablePropertyChanged) unconditionally calls
// GlueCommands.Self.GenerateCodeCommands/.GluxCommands afterward for any value change, which
// requires the full app DI bootstrap (Builder.Build()) that isn't set up in a unit test and is
// unrelated to the fix under test here.
public class StateCategoryViewModelTests
{
    [WpfFact]
    public void SettingValueOnBlankRow_DoesNotThrow()
    {
        var element = new EntitySave
        {
            CustomVariables = new List<CustomVariable>
            {
                new CustomVariable { Name = "MainAnimationChains", Type = "AnimationChainList" }
            }
        };

        var category = new StateSaveCategory { Name = "Appearance" };

        var viewModel = new StateCategoryViewModel(category, element, new NameVerifier());

        var blankRow = viewModel.States.Last();
        blankRow.BackingData.ShouldBeNull();

        var variableViewModel = blankRow.Variables.First(v => v.VariableName == "MainAnimationChains");

        var handler = typeof(StateCategoryViewModel).GetMethod(
            "HandleStateViewModelValueChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        handler.ShouldNotBeNull();

        Should.NotThrow(() => handler.Invoke(viewModel, new object[] { blankRow, variableViewModel }));

        blankRow.BackingData.ShouldBeNull();
        category.States.ShouldBeEmpty();
    }
}
