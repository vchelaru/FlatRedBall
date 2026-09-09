using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

// GitHub issue #2258: a .gluj-loaded List<float>/List<int> CustomVariable default value
// deserializes as a raw Newtonsoft JArray (of JValue, boxed long/double), same as
// List<string>/List<Vector2> already handle here. Without a fixup it never becomes the
// actual List<float>/List<int> the rest of Glue (and codegen) expects.
public class CustomVariableExtensionMethodsFixValueTests
{
    [Fact]
    public void FixValue_ShouldConvertJArrayToListOfFloat_WhenTypeIsListOfFloat()
    {
        var jArray = JArray.Parse("[1.5, -2, 3.25]");

        var result = CustomVariableExtensionMethods.FixValue(jArray, "List<float>");

        var asList = result.ShouldBeOfType<List<float>>();
        asList.ShouldBe(new List<float> { 1.5f, -2f, 3.25f });
    }

    [Fact]
    public void FixValue_ShouldConvertJArrayToListOfInt_WhenTypeIsListOfInt()
    {
        var jArray = JArray.Parse("[1, -2, 3]");

        var result = CustomVariableExtensionMethods.FixValue(jArray, "List<int>");

        var asList = result.ShouldBeOfType<List<int>>();
        asList.ShouldBe(new List<int> { 1, -2, 3 });
    }

    [Fact]
    public void FixValue_ShouldReturnEmptyList_ForEmptyJArray_ListOfFloat()
    {
        var jArray = JArray.Parse("[]");

        var result = CustomVariableExtensionMethods.FixValue(jArray, "List<float>");

        result.ShouldBeOfType<List<float>>().ShouldBeEmpty();
    }
}
