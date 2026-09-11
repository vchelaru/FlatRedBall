using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// After a delete, Glue selects a neighbour of the removed object. <c>GlueElement.NamedObjects</c> is a
/// flat list, but the tree shows plain objects at the top level and layers / collision relationships
/// under their own folder nodes, so a neighbour picked by list index can land in a different folder and
/// pop it open. See GitHub issue #2265.
/// </summary>
public class RemovalSelectionTests
{
    static NamedObjectSave Sprite(string name) => new NamedObjectSave
    {
        InstanceName = name,
        SourceType = SourceType.FlatRedBallType,
        SourceClassType = "FlatRedBall.Sprite"
    };

    static NamedObjectSave CollisionRelationship(string name) => new NamedObjectSave
    {
        InstanceName = name,
        SourceType = SourceType.FlatRedBallType,
        SourceClassType = "FlatRedBall.Math.Collision.CollisionRelationship"
    };

    static NamedObjectSave Layer(string name) => new NamedObjectSave
    {
        InstanceName = name,
        SourceType = SourceType.FlatRedBallType,
        SourceClassType = "Layer"
    };

    // Simulates what the callers hand over: the list *after* removal plus the removed object's old index.
    static NamedObjectSave PickAfterRemoving(List<NamedObjectSave> list, NamedObjectSave removed)
    {
        var index = list.IndexOf(removed);
        list.Remove(removed);
        return RemovalSelection.PickSuccessor(list, removed, index);
    }

    [Fact]
    public void RemovingAPlainObject_ShouldSelectThePlainObjectThatFollowedIt()
    {
        var first = Sprite("First");
        var second = Sprite("Second");
        var third = Sprite("Third");

        PickAfterRemoving(new List<NamedObjectSave> { first, second, third }, second).ShouldBe(third);
    }

    [Fact]
    public void RemovingTheLastPlainObject_ShouldSelectThePlainObjectBeforeIt_NotTheCollisionRelationshipAfterIt()
    {
        var first = Sprite("First");
        var second = Sprite("Second");
        var relationship = CollisionRelationship("FirstVsSecond");

        PickAfterRemoving(new List<NamedObjectSave> { first, second, relationship }, second).ShouldBe(first);
    }

    [Fact]
    public void RemovingTheOnlyPlainObject_ShouldSelectNothing_WhenOnlyCollisionRelationshipsRemain()
    {
        var only = Sprite("Only");
        var relationship = CollisionRelationship("OnlyVsOnly");

        PickAfterRemoving(new List<NamedObjectSave> { only, relationship }, only).ShouldBeNull();
    }

    [Fact]
    public void RemovingACollisionRelationship_ShouldSelectAnotherCollisionRelationship_NotAPlainObject()
    {
        var firstRelationship = CollisionRelationship("FirstRelationship");
        var secondRelationship = CollisionRelationship("SecondRelationship");
        // Objects added after the relationships land after them in the flat list
        var sprite = Sprite("Sprite");

        PickAfterRemoving(new List<NamedObjectSave> { firstRelationship, secondRelationship, sprite }, secondRelationship)
            .ShouldBe(firstRelationship);
    }

    [Fact]
    public void RemovingALayer_ShouldNotSelectACollisionRelationship()
    {
        var layer = Layer("Layer");
        var relationship = CollisionRelationship("Relationship");

        PickAfterRemoving(new List<NamedObjectSave> { layer, relationship }, layer).ShouldBeNull();
    }
}
