using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

public class NamedObjectSaveCollisionExtensionsTests
{
    static NamedObjectSave WithSourceClassType(string? sourceClassType) => new NamedObjectSave
    {
        SourceClassType = sourceClassType
    };

    [Theory]
    [InlineData("CollisionRelationship")]
    [InlineData("FlatRedBall.Math.Collision.CollisionRelationship")]
    [InlineData("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship<Sprite, Sprite>")]
    [InlineData("FlatRedBall.Math.Collision.ListVsListRelationship<Sprite, Sprite>")]
    [InlineData("FlatRedBall.Math.Collision.DelegateCollisionRelationship<Sprite, Sprite>")]
    [InlineData("CollisionRelationship<Sprite, Sprite>")]
    public void IsCollisionRelationship_KnownCollisionSourceClassType_ReturnsTrue(string sourceClassType)
    {
        Assert.True(WithSourceClassType(sourceClassType).IsCollisionRelationship());
    }

    [Theory]
    [InlineData("FlatRedBall.Sprite")]
    [InlineData("Layer")]
    public void IsCollisionRelationship_NonCollisionSourceClassType_ReturnsFalse(string sourceClassType)
    {
        Assert.False(WithSourceClassType(sourceClassType).IsCollisionRelationship());
    }

    [Fact]
    public void IsCollisionRelationship_NullSourceClassType_ReturnsFalse()
    {
        Assert.False(WithSourceClassType(null).IsCollisionRelationship());
    }
}
