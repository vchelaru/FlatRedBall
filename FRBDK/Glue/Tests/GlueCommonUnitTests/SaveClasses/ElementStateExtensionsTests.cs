using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// CleanUnusedVariablesFromStates reaches ObjectFinderCore.Self through ContainsCustomVariableRecursively
// when the element has a base, so this can't run concurrently with any other test class that swaps it
// out - hence the shared collection (see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ElementStateExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public ElementStateExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    static StateSave StateWith(string name, params string[] members)
    {
        var state = new StateSave { Name = name };
        foreach (var member in members)
        {
            state.InstructionSaves.Add(new InstructionSave { Member = member, Value = 1f });
        }
        return state;
    }

    static string[] Members(StateSave state) => state.InstructionSaves.Select(item => item.Member).ToArray();

    [Fact]
    public void SortStatesToCustomVariables_ReordersUncategorizedAndCategorizedStates()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.CustomVariables.Add(new CustomVariable { Name = "X" });
        entity.CustomVariables.Add(new CustomVariable { Name = "Y" });
        var uncategorized = StateWith("A", "Y", "X");
        var categorized = StateWith("B", "Y", "X");
        entity.States.Add(uncategorized);
        var category = new StateSaveCategory { Name = "Category" };
        category.States.Add(categorized);
        entity.StateCategoryList.Add(category);

        entity.SortStatesToCustomVariables();

        Assert.Equal(new[] { "X", "Y" }, Members(uncategorized));
        Assert.Equal(new[] { "X", "Y" }, Members(categorized));
    }

    [Fact]
    public void CleanUnusedVariablesFromStates_RemovesInstructionsForUnknownVariables()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        entity.CustomVariables.Add(new CustomVariable { Name = "Known" });
        var state = StateWith("A", "Known", "Unknown", "Known2");
        entity.States.Add(state);

        entity.CleanUnusedVariablesFromStates();

        Assert.Equal(new[] { "Known" }, Members(state));
    }

    [Fact]
    public void CleanUnusedVariablesFromStates_KeepsVariablesDefinedOnBaseElement()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "FromBase" });
        _finder.AddElement("Entities\\Base", baseEntity);
        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        var state = StateWith("A", "FromBase", "Unknown");
        var category = new StateSaveCategory { Name = "Category" };
        category.States.Add(state);
        derived.StateCategoryList.Add(category);

        derived.CleanUnusedVariablesFromStates();

        Assert.Equal(new[] { "FromBase" }, Members(state));
    }

    [Fact]
    public void RemoveState_UncategorizedState_RemovesFromStates()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var state = StateWith("A");
        var other = StateWith("B");
        entity.States.Add(state);
        entity.States.Add(other);

        entity.RemoveState(state);

        Assert.Equal(new[] { other }, entity.States);
    }

    [Fact]
    public void RemoveState_CategorizedState_RemovesFromItsCategoryOnly()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var state = StateWith("A");
        var first = new StateSaveCategory { Name = "First" };
        first.States.Add(state);
        var second = new StateSaveCategory { Name = "Second" };
        var survivor = StateWith("B");
        second.States.Add(survivor);
        entity.StateCategoryList.Add(first);
        entity.StateCategoryList.Add(second);

        entity.RemoveState(state);

        Assert.Empty(first.States);
        Assert.Equal(new[] { survivor }, second.States);
    }

    [Fact]
    public void RemoveState_UnknownState_LeavesElementUnchanged()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var kept = StateWith("A");
        entity.States.Add(kept);
        var category = new StateSaveCategory { Name = "Category" };
        var keptInCategory = StateWith("B");
        category.States.Add(keptInCategory);
        entity.StateCategoryList.Add(category);

        entity.RemoveState(StateWith("Missing"));

        Assert.Equal(new[] { kept }, entity.States);
        Assert.Equal(new[] { keptInCategory }, category.States);
    }
}
