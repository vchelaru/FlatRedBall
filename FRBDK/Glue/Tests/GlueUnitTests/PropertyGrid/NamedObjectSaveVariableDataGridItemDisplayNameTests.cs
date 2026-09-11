using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.PropertyGrid;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2256: VariableDefinition.DisplayName lets a checkbox override its property grid label
// without changing the underlying variable name used for storage/codegen - added so "Set Collision From
// Animation" can read as "Set Collision/Shapes From Animation" once it also works on non-ICollidable
// entities.
public class NamedObjectSaveVariableDataGridItemDisplayNameTests
{
    public NamedObjectSaveVariableDataGridItemDisplayNameTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void RefreshFrom_ShouldUseVariableDefinitionDisplayName_WhenSet()
    {
        var container = new EntitySave { Name = "Entities\\Enemy" };
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        var variableDefinition = new VariableDefinition
        {
            Name = "SetCollisionFromAnimation",
            DisplayName = "Set Collision/Shapes From Animation",
            Type = "bool"
        };

        var item = new NamedObjectSaveVariableDataGridItem();
        item.RefreshFrom(nos, variableDefinition, container, categories: null, customTypeName: null, nameOnInstance: "SetCollisionFromAnimation");

        item.DisplayName.ShouldBe("Set Collision/Shapes From Animation");
    }

    [Fact]
    public void RefreshFrom_ShouldFallBackToSpacedName_WhenDisplayNameIsNotSet()
    {
        var container = new EntitySave { Name = "Entities\\Enemy" };
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        var variableDefinition = new VariableDefinition
        {
            Name = "SetCollisionFromAnimation",
            Type = "bool"
        };

        var item = new NamedObjectSaveVariableDataGridItem();
        item.RefreshFrom(nos, variableDefinition, container, categories: null, customTypeName: null, nameOnInstance: "SetCollisionFromAnimation");

        item.DisplayName.ShouldBe("Set Collision From Animation");
    }
}
