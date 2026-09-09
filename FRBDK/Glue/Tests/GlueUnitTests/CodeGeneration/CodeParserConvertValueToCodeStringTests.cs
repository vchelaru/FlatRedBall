using System.Collections.Generic;
using FlatRedBall.Glue.Parsing;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// GitHub issue #2258: List<float>/List<int> CustomVariables need the same codegen support
// List<string>/List<Vector2> already have here, or their default value emits as a broken
// ToString() of the list type instead of a valid C# initializer.
public class CodeParserConvertValueToCodeStringTests
{
    [Fact]
    public void ConvertValueToCodeString_ShouldEmitListInitializer_ForListOfFloat()
    {
        var value = new List<float> { 1.5f, -2f };

        var result = CodeParser.ConvertValueToCodeString(value);

        result.ShouldBe("new System.Collections.Generic.List<float> { 1.5f, -2f}");
    }

    [Fact]
    public void ConvertValueToCodeString_ShouldEmitListInitializer_ForListOfInt()
    {
        var value = new List<int> { 1, -2, 3 };

        var result = CodeParser.ConvertValueToCodeString(value);

        result.ShouldBe("new System.Collections.Generic.List<int> { 1, -2, 3}");
    }

    [Fact]
    public void ConvertValueToCodeString_ShouldEmitEmptyListInitializer_ForEmptyListOfFloat()
    {
        var value = new List<float>();

        var result = CodeParser.ConvertValueToCodeString(value);

        result.ShouldBe("new System.Collections.Generic.List<float> { }");
    }
}
