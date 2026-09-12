using FlatRedBall.Glue.Parsing;
using Microsoft.Xna.Framework;

namespace GlueCommonUnitTests.Parsing;

public class TypeConversionTests
{
    [Theory]
    [InlineData("System.Single", "float")]
    [InlineData("Single", "float")]
    [InlineData("System.Boolean", "bool")]
    [InlineData("System.Int32", "int")]
    [InlineData("System.String", "string")]
    [InlineData("Double", "double")]
    [InlineData("System.Decimal", "decimal")]
    [InlineData("System.Nullable`1[[System.Int32,", "int?")]
    [InlineData("FlatRedBall.Graphics.ColorOperation", "FlatRedBall.Graphics.ColorOperation")]
    public void GetCommonTypeName_ReturnsExpectedCodeFriendlyName(string qualifiedName, string expected)
    {
        Assert.Equal(expected, TypeConversion.GetCommonTypeName(qualifiedName));
    }

    [Fact]
    public void GetFriendlyGenericName_NonGenericType_ReturnsPlainName()
    {
        Assert.Equal("Int32", TypeConversion.GetFriendlyGenericName(typeof(int)));
    }

    [Fact]
    public void GetFriendlyGenericName_GenericType_StripsArityAndAddsArguments()
    {
        Assert.Equal("List<Int32>", TypeConversion.GetFriendlyGenericName(typeof(List<int>)));
    }

    [Fact]
    public void GetElementType_SpriteListFullName_ReturnsSprite()
    {
        Assert.Equal(typeof(FlatRedBall.Sprite), TypeConversion.GetElementType(typeof(FlatRedBall.SpriteList)));
    }

    [Fact]
    public void GetElementType_ArrayType_ReturnsElementType()
    {
        Assert.Equal(typeof(int), TypeConversion.GetElementType(typeof(int[])));
    }

    [Theory]
    [InlineData("string", "null")]
    [InlineData("bool", "false")]
    [InlineData("float", "0")]
    [InlineData("int", "0")]
    [InlineData("int?", "null")]
    public void TryGetDefaultForType_KnownPrimitive_ReturnsTrueAndDefault(string type, string expectedDefault)
    {
        Assert.True(TypeConversion.TryGetDefaultForType(type, out var defaultValue));
        Assert.Equal(expectedDefault, defaultValue);
    }

    [Fact]
    public void TryGetDefaultForType_UnknownType_ReturnsFalse()
    {
        Assert.False(TypeConversion.TryGetDefaultForType("FlatRedBall.Graphics.Layer", out var defaultValue));
        Assert.Null(defaultValue);
    }

    [Fact]
    public void GetDefaultForType_KnownPrimitive_ReturnsDefault()
    {
        Assert.Equal("false", TypeConversion.GetDefaultForType("bool"));
    }

    [Fact]
    public void GetDefaultForType_UnknownType_Throws()
    {
        Assert.Throws<ArgumentException>(() => TypeConversion.GetDefaultForType("FlatRedBall.Graphics.Layer"));
    }

    [Theory]
    [InlineData("bool", "true", true)]
    [InlineData("int", "42", 42)]
    [InlineData("float", "1.5", 1.5f)]
    public void Parse_KnownPrimitive_ParsesValue(string typeName, string value, object expected)
    {
        Assert.Equal(expected, TypeConversion.Parse(typeName, value));
    }

    [Fact]
    public void Parse_UnknownType_ReturnsRawStringValue()
    {
        Assert.Equal("hello", TypeConversion.Parse("SomeEnum", "hello"));
    }

    [Fact]
    public void TryConvertStringValue_Int_ParsesToInt()
    {
        Assert.True(TypeConversion.TryConvertStringValue("int", "5", out var converted));
        Assert.Equal(5, converted);
    }

    [Fact]
    public void TryConvertStringValue_EmptyFloat_DefaultsToZero()
    {
        Assert.True(TypeConversion.TryConvertStringValue("float", "", out var converted));
        Assert.Equal(0f, converted);
    }

    [Fact]
    public void TryConvertStringValue_Color_ResolvesNamedProperty()
    {
        Assert.True(TypeConversion.TryConvertStringValue("Microsoft.Xna.Framework.Color", "Red", out var converted));
        Assert.Equal(Color.Red, converted);
    }

    [Fact]
    public void TryConvertStringValue_UnknownType_ReturnsFalse()
    {
        Assert.False(TypeConversion.TryConvertStringValue("SomeUnknownType", "1", out var converted));
        Assert.Null(converted);
    }

    [Fact]
    public void TryCastValue_LongToInt_Casts()
    {
        Assert.True(TypeConversion.TryCastValue("int", 5L, out var converted));
        Assert.Equal(5, converted);
    }

    [Fact]
    public void TryCastValue_IntToFloat_Casts()
    {
        Assert.True(TypeConversion.TryCastValue("float", 5, out var converted));
        Assert.Equal(5f, converted);
    }

    [Fact]
    public void TryCastValue_UnhandledCombination_ReturnsFalseAndOriginalValue()
    {
        Assert.False(TypeConversion.TryCastValue("bool", 5, out var converted));
        Assert.Equal(5, converted);
    }
}
