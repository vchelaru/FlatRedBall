using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Controls;

namespace GlueCommonUnitTests.SaveClasses;

// StateSaveToString and SetValue read the shared static ObjectFinderCore.Self (SetValue also
// ErrorReportingCore.Self), so this can't run concurrently with any other test class that swaps
// either out - hence the shared collection (see ObjectFinderCoreCollection in
// NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class StateSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();
    readonly FakeErrorReportingCore _errors = new();

    public StateSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        ErrorReportingCore.Self = _errors;
    }

    static (EntitySave Owner, StateSave Uncategorized, StateSave Categorized, StateSaveCategory Category) BuildOwner()
    {
        var owner = new EntitySave { Name = "Entities\\Player" };
        var uncategorized = new StateSave { Name = "Idle" };
        owner.States.Add(uncategorized);
        var category = new StateSaveCategory { Name = "Movement" };
        var categorized = new StateSave { Name = "Walking" };
        category.States.Add(categorized);
        owner.StateCategoryList.Add(category);
        return (owner, uncategorized, categorized, category);
    }

    #region GetStateTypeFromCurrentVariableName

    [Theory]
    [InlineData("CurrentState", "VariableState")]
    [InlineData("CurrentMovementState", "Movement")]
    public void GetStateTypeFromCurrentVariableName_ReturnsCategoryName(string memberName, string expected)
    {
        Assert.Equal(expected, StateSaveElementExtensions.GetStateTypeFromCurrentVariableName(memberName));
    }

    #endregion

    #region ContainsCategoryName

    [Fact]
    public void ContainsCategoryName_MatchingName_ReturnsTrue()
    {
        var categories = new List<StateSaveCategory> { new() { Name = "Movement" }, new() { Name = "Color" } };

        Assert.True(categories.ContainsCategoryName("Color"));
    }

    [Fact]
    public void ContainsCategoryName_NoMatch_ReturnsFalse()
    {
        var categories = new List<StateSaveCategory> { new() { Name = "Movement" } };

        Assert.False(categories.ContainsCategoryName("movement"));
        Assert.False(new List<StateSaveCategory>().ContainsCategoryName("Movement"));
    }

    #endregion

    #region GetExposedVariableName

    [Fact]
    public void GetExposedVariableName_UncategorizedState_ReturnsCurrentState()
    {
        var (owner, uncategorized, _, _) = BuildOwner();

        Assert.Equal("CurrentState", uncategorized.GetExposedVariableName(owner));
    }

    [Fact]
    public void GetExposedVariableName_CategorizedState_ReturnsCurrentCategoryState()
    {
        var (owner, _, categorized, _) = BuildOwner();

        Assert.Equal("CurrentMovementState", categorized.GetExposedVariableName(owner));
    }

    [Fact]
    public void GetExposedVariableName_StateNotInContainer_ReturnsCurrentState()
    {
        var (owner, _, _, _) = BuildOwner();

        Assert.Equal("CurrentState", new StateSave { Name = "Elsewhere" }.GetExposedVariableName(owner));
    }

    #endregion

    #region GetEnumTypeName

    [Fact]
    public void GetEnumTypeName_UncategorizedState_ReturnsVariableState()
    {
        var (owner, uncategorized, _, _) = BuildOwner();

        Assert.Equal("VariableState", uncategorized.GetEnumTypeName(owner));
    }

    [Fact]
    public void GetEnumTypeName_CategorizedState_ReturnsCategoryName()
    {
        var (owner, _, categorized, _) = BuildOwner();

        Assert.Equal("Movement", categorized.GetEnumTypeName(owner));
    }

    [Fact]
    public void GetEnumTypeName_StateNotInContainer_ReturnsVariableState()
    {
        var (owner, _, _, _) = BuildOwner();

        Assert.Equal("VariableState", new StateSave { Name = "Elsewhere" }.GetEnumTypeName(owner));
    }

    #endregion

    #region StateSaveToString

    [Fact]
    public void StateSaveToString_Contained_IncludesContainerName()
    {
        var (owner, uncategorized, _, _) = BuildOwner();
        _finder.SetContainer(uncategorized, owner);

        Assert.Equal($"Idle(State in {owner})", StateSaveElementExtensions.StateSaveToString(uncategorized));
    }

    [Fact]
    public void StateSaveToString_Uncontained_IncludesEmptyContainer()
    {
        var state = new StateSave { Name = "Idle" };

        Assert.Equal("Idle(State in )", StateSaveElementExtensions.StateSaveToString(state));
    }

    #endregion
    #region RemoveVariable

    [Fact]
    public void RemoveVariable_ExistingMember_RemovesOnlyThatInstruction()
    {
        var state = new StateSave { Name = "Idle" };
        state.InstructionSaves.Add(new InstructionSave { Member = "X", Value = 1f });
        state.InstructionSaves.Add(new InstructionSave { Member = "Y", Value = 2f });

        state.RemoveVariable("X");

        Assert.Equal(new[] { "Y" }, state.InstructionSaves.Select(item => item.Member));
    }

    [Fact]
    public void RemoveVariable_MissingMember_LeavesInstructionsAlone()
    {
        var state = new StateSave { Name = "Idle" };
        state.InstructionSaves.Add(new InstructionSave { Member = "X", Value = 1f });

        state.RemoveVariable("Z");

        Assert.Single(state.InstructionSaves);
    }

    #endregion

    #region SetValue

    (EntitySave Owner, StateSave State) BuildSetValueOwner()
    {
        var owner = new EntitySave { Name = "Entities\\Player" };
        owner.CustomVariables.Add(new CustomVariable { Name = "X", Type = "float" });
        owner.CustomVariables.Add(new CustomVariable { Name = "Y", Type = "float" });
        var state = new StateSave { Name = "Idle" };
        owner.States.Add(state);
        _finder.SetContainer(state, owner);
        return (owner, state);
    }

    [Fact]
    public void SetValue_NullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((StateSave)null).SetValue("X", 1f));
    }

    [Fact]
    public void SetValue_NewVariable_AddsInstructionInCustomVariableOrder()
    {
        var (_, state) = BuildSetValueOwner();

        state.SetValue("Y", 2f);
        state.SetValue("X", 1f);

        Assert.Equal(new[] { "X", "Y" }, state.InstructionSaves.Select(item => item.Member));
        var x = state.InstructionSaves[0];
        Assert.Equal(1f, x.Value);
        Assert.Equal("float", x.Type);
        Assert.Empty(_errors.Confirms);
    }

    [Fact]
    public void SetValue_UnknownVariable_UsesValueTypeName()
    {
        var (owner, state) = BuildSetValueOwner();
        owner.CustomVariables.Add(new CustomVariable { Name = "Label" });

        state.SetValue("Label", "hi");

        var instruction = Assert.Single(state.InstructionSaves);
        Assert.Equal("String", instruction.Type);
    }

    [Fact]
    public void SetValue_ExistingVariable_OverwritesValue()
    {
        var (_, state) = BuildSetValueOwner();
        state.InstructionSaves.Add(new InstructionSave { Member = "X", Value = 1f, Type = "float" });

        state.SetValue("X", 5f);

        var instruction = Assert.Single(state.InstructionSaves);
        Assert.Equal(5f, instruction.Value);
    }

    [Fact]
    public void SetValue_SetInOtherCategory_ConfirmYes_SetsStrippedName()
    {
        var (_, state) = BuildSetValueOwner();
        _errors.ConfirmResult = DialogButton.Yes;

        state.SetValue("X set in Movement", 3f);

        var confirm = Assert.Single(_errors.Confirms);
        Assert.Contains("The variable X is set in other categories", confirm);
        var instruction = Assert.Single(state.InstructionSaves);
        Assert.Equal("X", instruction.Member);
        Assert.Equal(3f, instruction.Value);
    }

    [Fact]
    public void SetValue_SetInOtherCategory_ConfirmNo_KeepsFullNameAndSortDropsIt()
    {
        var (_, state) = BuildSetValueOwner();
        _errors.ConfirmResult = DialogButton.No;

        state.SetValue("X set in Movement", 3f);

        Assert.Single(_errors.Confirms);
        // The unstripped name matches no CustomVariable, so SortInstructionSaves prunes it.
        Assert.Empty(state.InstructionSaves);
    }

    [Fact]
    public void SetValue_SetInOtherCategory_ConfirmClosed_KeepsFullNameAndSortDropsIt()
    {
        var (_, state) = BuildSetValueOwner();
        _errors.ConfirmResult = null;

        state.SetValue("X set in Movement", 3f);

        Assert.Single(_errors.Confirms);
        Assert.Empty(state.InstructionSaves);
    }

    #endregion
}
