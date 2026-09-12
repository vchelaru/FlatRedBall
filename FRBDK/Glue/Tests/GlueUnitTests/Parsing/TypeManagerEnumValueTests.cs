using FlatRedBall.Glue.Parsing;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Parsing;

// GitHub issue #2272: "Make Default" on an enum-typed variable has to send the game the variable's real
// default, and TypeManager is what turns the AssetTypeInfo's declared default (a member name out of
// ContentTypes.csv) into something the game can read back.
public class TypeManagerEnumValueTests
{
    public TypeManagerEnumValueTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Theory]
    // The four the report names, by the unqualified type name the AssetTypeInfo actually carries:
    [InlineData("MaxWidthBehavior", "Chop", "0")]
    [InlineData("HorizontalAlignment", "Left", "0")]
    [InlineData("BlendOperation", "Regular", "0")]
    // Center is 2, not 0 - VerticalAlignment is Top, Bottom, Center - which is why the declared default
    // has to win over the CLR default for the type.
    [InlineData("VerticalAlignment", "Center", "2")]
    // Same story for a Text's ColorOperation, where TypeManager's own primitive table says "0" (Texture).
    [InlineData("ColorOperation", "ColorTextureAlpha", "6")]
    public void TryGetEnumValueAsNumber_ShouldResolveMemberName_ToItsUnderlyingValue(
        string type, string memberName, string expected)
    {
        TypeManager.TryGetEnumValueAsNumber(type, memberName, out string value).ShouldBeTrue();
        value.ShouldBe(expected);
    }

    [Fact]
    public void TryGetEnumValueAsNumber_ShouldTolerateSurroundingWhitespace()
    {
        // ContentTypes.csv is written with loose spacing ("Name=MaxWidthBehavior, Category = Text, ...").
        TypeManager.TryGetEnumValueAsNumber(" MaxWidthBehavior ", " Wrap ", out string value).ShouldBeTrue();
        value.ShouldBe("1");
    }

    [Theory]
    // Not an enum at all - a reference type whose AssetTypeInfo default ("Default" for a Text's Font) is a
    // UI label, not a value, so it must not be sent to the game.
    [InlineData("BitmapFont", "Default")]
    [InlineData("Texture2D", "None")]
    [InlineData("float", "0")]
    // An enum, but not a member of it:
    [InlineData("MaxWidthBehavior", "NotAMember")]
    // Nothing to go on:
    [InlineData("MaxWidthBehavior", null)]
    [InlineData(null, "Chop")]
    [InlineData("SomeTypeThatDoesNotExist", "Chop")]
    public void TryGetEnumValueAsNumber_ShouldReturnFalse_WhenTheTypeIsNotAnEnumWithThatMember(
        string type, string memberName)
    {
        TypeManager.TryGetEnumValueAsNumber(type, memberName, out string value).ShouldBeFalse();
        value.ShouldBeNull();
    }
}
