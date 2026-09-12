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

    // GitHub issue #2283: #2272 only fixed VariableSendingManager's own two-step check (declared enum
    // default, then TryGetDefaultForType). TryGetVariableDefaultValue is the one place all callers should
    // go through instead, so the same reasoning can't drift out of sync again.
    [Theory]
    [InlineData("VerticalAlignment", "Center", "2")]
    [InlineData("ColorOperation", "ColorTextureAlpha", "6")]
    [InlineData("float", null, "0")]
    [InlineData("bool", null, "false")]
    public void TryGetVariableDefaultValue_ShouldPreferTheDeclaredDefault_WhenOneIsGiven(
        string type, string declaredDefault, string expected)
    {
        TypeManager.TryGetVariableDefaultValue(type, declaredDefault, out string value).ShouldBeTrue();
        value.ShouldBe(expected);
    }

    [Theory]
    // No declared default at all - an AssetTypeInfo entry missing DefaultValue, or a plain
    // CustomVariable with no AssetTypeInfo behind it. Falling through to null here is exactly what made
    // #2272's crash possible (the game cannot assign null to an enum), so this must resolve to the enum's
    // own CLR zero instead - "0", regardless of whether Chop/Left/Regular/whatever member actually is 0.
    [InlineData("MaxWidthBehavior", null, "0")]
    [InlineData("MaxWidthBehavior", "", "0")]
    // A declared default that isn't actually a member of the enum (stale/typo'd CSV entry) falls back the
    // same way rather than propagating the bad name.
    [InlineData("VerticalAlignment", "NotAMember", "0")]
    public void TryGetVariableDefaultValue_ShouldFallBackToClrZero_WhenNoDeclaredDefaultResolves(
        string type, string declaredDefault, string expected)
    {
        TypeManager.TryGetVariableDefaultValue(type, declaredDefault, out string value).ShouldBeTrue();
        value.ShouldBe(expected);
    }

    [Fact]
    public void TryGetVariableDefaultValue_ShouldReturnFalse_ForAReferenceTypeWithNoKnownDefault()
    {
        // A BitmapFont, Layer, entity type, etc: callers must still send null/decide their own fallback.
        TypeManager.TryGetVariableDefaultValue("BitmapFont", null, out string value).ShouldBeFalse();
        value.ShouldBeNull();
    }
}
