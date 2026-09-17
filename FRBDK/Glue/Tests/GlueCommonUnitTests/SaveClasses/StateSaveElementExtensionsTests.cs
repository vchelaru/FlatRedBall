using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// StateSaveToString reads the shared static ObjectFinderCore.Self, so this can't run concurrently
// with any other test class that swaps it out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class StateSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public StateSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
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
}
