using FlatRedBall.Gum;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Gum;

// A .gumj project names its elements .gusj/.gucj/.gutj. GumIdb.LoadFromFile picks the screen/component/
// standard list from the file name's extension, and it only knew the XML ones, so a generated
// LoadFromFile("Screens/MainMenu.gusj") found no element and threw a NullReferenceException.
public class GumIdbElementExtensionTests
{
    [Theory]
    [InlineData("gusj", "gusx")]
    [InlineData("gucj", "gucx")]
    [InlineData("gutj", "gutx")]
    public void JsonElementExtension_MapsToItsXmlCounterpart(string json, string xml)
    {
        GumIdb.GetElementExtensionForLookup(json).ShouldBe(xml);
    }

    [Theory]
    [InlineData("gusx")]
    [InlineData("gucx")]
    [InlineData("gutx")]
    [InlineData("png")]
    public void OtherExtensions_AreUnchanged(string extension)
    {
        GumIdb.GetElementExtensionForLookup(extension).ShouldBe(extension);
    }
}
