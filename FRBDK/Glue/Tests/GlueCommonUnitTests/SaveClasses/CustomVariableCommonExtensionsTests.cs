using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json.Linq;

namespace GlueCommonUnitTests.SaveClasses;

// GitHub issue #2258: a .gluj-loaded List<float>/List<int> CustomVariable default value
// deserializes as a raw Newtonsoft JArray (of JValue, boxed long/double), same as
// List<string>/List<Vector2> already handle here. Without a fixup it never becomes the
// actual List<float>/List<int> the rest of Glue (and codegen) expects.
public class CustomVariableCommonExtensionsTests
{
    [Fact]
    public void FixValue_ShouldConvertJArrayToListOfFloat_WhenTypeIsListOfFloat()
    {
        var jArray = JArray.Parse("[1.5, -2, 3.25]");

        var result = CustomVariableCommonExtensions.FixValue(jArray, "List<float>");

        var asList = Assert.IsType<List<float>>(result);
        Assert.Equal(new List<float> { 1.5f, -2f, 3.25f }, asList);
    }

    [Fact]
    public void FixValue_ShouldConvertJArrayToListOfInt_WhenTypeIsListOfInt()
    {
        var jArray = JArray.Parse("[1, -2, 3]");

        var result = CustomVariableCommonExtensions.FixValue(jArray, "List<int>");

        var asList = Assert.IsType<List<int>>(result);
        Assert.Equal(new List<int> { 1, -2, 3 }, asList);
    }

    [Fact]
    public void FixValue_ShouldReturnEmptyList_ForEmptyJArray_ListOfFloat()
    {
        var jArray = JArray.Parse("[]");

        var result = CustomVariableCommonExtensions.FixValue(jArray, "List<float>");

        Assert.Empty(Assert.IsType<List<float>>(result));
    }

    [Fact]
    public void FixValue_ShouldConvertLongToInt_WhenTypeIsInt()
    {
        var result = CustomVariableCommonExtensions.FixValue(5L, "int");

        Assert.Equal(5, Assert.IsType<int>(result));
    }

    [Fact]
    public void FixValue_ShouldConvertIntToFloat_WhenTypeIsFloat()
    {
        var result = CustomVariableCommonExtensions.FixValue(5, "float");

        Assert.Equal(5f, Assert.IsType<float>(result));
    }

    [Fact]
    public void FixValue_ShouldParseFloatRectangle_FromParenthesizedString()
    {
        var result = CustomVariableCommonExtensions.FixValue("(1, 2, 3, 4)", "FloatRectangle?");

        var asRectangle = Assert.IsType<GlueSaveClasses.FloatRectangle>(result);
        Assert.Equal(1f, asRectangle.X);
        Assert.Equal(2f, asRectangle.Y);
        Assert.Equal(3f, asRectangle.Width);
        Assert.Equal(4f, asRectangle.Height);
    }

    [Fact]
    public void FixValue_ShouldReturnNull_ForUnparseableFloatRectangle()
    {
        var result = CustomVariableCommonExtensions.FixValue("not a rectangle", "FloatRectangle?");

        Assert.Null(result);
    }

    [Fact]
    public void FixValue_ShouldReturnValueUnchanged_ForUnknownType()
    {
        var result = CustomVariableCommonExtensions.FixValue("hello", "SomeUnknownType");

        Assert.Equal("hello", result);
    }
}
