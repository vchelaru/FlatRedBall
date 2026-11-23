using System.Linq;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

/// <summary>
/// IsSharedStatic defaults to true in the constructor for every container. Element files are saved ignoring
/// defaults and the .gluj ignoring and populating them; all three are loaded with plain JsonConvert, so the
/// constructor's true fills a missing value. These pin that old files (true written out, or nothing written)
/// load as they always did, and that a false now survives the round trip.
/// </summary>
public class ReferencedFileSaveIsSharedStaticSerializationTests
{
    // GlueProjectSaveExtensions.RemoveAndSaveElements (.glsj/.glej) and SaveGlujFile/SaveMainAndElementsToFile (.gluj).
    static readonly JsonSerializerSettings ElementFileSettings = new() { DefaultValueHandling = DefaultValueHandling.Ignore };
    static readonly JsonSerializerSettings GlujSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
    };

    [Theory]
    [InlineData(@"{""Name"":""Screens\\GameScreen"",""ReferencedFiles"":[{""Name"":""Screens/GameScreen/A.png"",""IsSharedStatic"":true}]}", true)]
    [InlineData(@"{""Name"":""Screens\\GameScreen"",""ReferencedFiles"":[{""Name"":""Screens/GameScreen/A.png""}]}", true)]
    [InlineData(@"{""Name"":""Screens\\GameScreen"",""ReferencedFiles"":[{""Name"":""Screens/GameScreen/A.png"",""IsSharedStatic"":false}]}", false)]
    public void OldScreenFile_ShouldLoadIsSharedStaticAsWritten_OrTrueWhenMissing(string json, bool expected)
    {
        JsonConvert.DeserializeObject<ScreenSave>(json)!.ReferencedFiles.Single().IsSharedStatic.ShouldBe(expected);
    }

    [Theory]
    [InlineData(@"{""Name"":""Entities\\Enemy"",""ReferencedFiles"":[{""Name"":""Entities/Enemy/A.png"",""IsSharedStatic"":true}]}")]
    [InlineData(@"{""Name"":""Entities\\Enemy"",""ReferencedFiles"":[{""Name"":""Entities/Enemy/A.png""}]}")]
    public void OldEntityFile_ShouldLoadIsSharedStaticAsTrue(string json)
    {
        JsonConvert.DeserializeObject<EntitySave>(json)!.ReferencedFiles.Single().IsSharedStatic.ShouldBeTrue();
    }

    [Theory]
    [InlineData(@"{""FileVersion"":11,""GlobalFiles"":[{""Name"":""GlobalContent/A.csv"",""IsSharedStatic"":true}]}")]
    [InlineData(@"{""FileVersion"":11,""GlobalFiles"":[{""Name"":""GlobalContent/A.csv""}]}")]
    public void OldGlujGlobalFile_ShouldLoadIsSharedStaticAsTrue(string json)
    {
        JsonConvert.DeserializeObject<GlueProjectSave>(json)!.GlobalFiles.Single().IsSharedStatic.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScreenFile_ShouldRoundTripIsSharedStatic(bool value)
    {
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        screen.ReferencedFiles.Add(new ReferencedFileSave { Name = "Screens/GameScreen/A.png", IsSharedStatic = value });

        var json = JsonConvert.SerializeObject(screen, ElementFileSettings);

        JsonConvert.DeserializeObject<ScreenSave>(json)!.ReferencedFiles.Single().IsSharedStatic.ShouldBe(value);
    }

    [Fact]
    public void GlujGlobalFile_ShouldStayTrueAfterRoundTrip()
    {
        var project = new GlueProjectSave();
        project.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/A.csv" });

        var json = JsonConvert.SerializeObject(project, GlujSettings);

        JsonConvert.DeserializeObject<GlueProjectSave>(json)!.GlobalFiles.Single().IsSharedStatic.ShouldBeTrue();
    }
}
