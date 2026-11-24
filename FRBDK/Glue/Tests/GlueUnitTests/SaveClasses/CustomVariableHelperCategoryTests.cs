using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

// Both the Add Variable dialog and the "Set Variable Category" right-click dialog offer this list
// in their editable category dropdowns.
public class CustomVariableHelperCategoryTests
{
    [Fact]
    public void GetAvailableCategories_ShouldIncludeOwnAndBaseCategories_DistinctAndNonEmpty()
    {
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "Speed", Category = "Movement" });
        baseEntity.CustomVariables.Add(new CustomVariable { Name = "Health", Category = "Combat" });

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        derived.CustomVariables.Add(new CustomVariable { Name = "Jump", Category = "Movement" });
        derived.CustomVariables.Add(new CustomVariable { Name = "Color", Category = "Visuals" });
        derived.CustomVariables.Add(new CustomVariable { Name = "Uncategorized" });

        var categories = CustomVariableHelper.GetAvailableCategories(derived, new[] { baseEntity });

        categories.ShouldBe(new[] { "Movement", "Visuals", "Combat" });
    }
}
