using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

public class ReferencedFileSavePropertyExtensionsTests
{
    [Fact]
    public void GetProperty_PropertyExists_ReturnsValue()
    {
        var rfs = new ReferencedFileSave();
        rfs.SetProperty("SomeProperty", 42);

        Assert.Equal(42, rfs.GetProperty<int>("SomeProperty"));
    }

    [Fact]
    public void GetProperty_PropertyMissing_ReturnsDefault()
    {
        var rfs = new ReferencedFileSave();

        Assert.Equal(0, rfs.GetProperty<int>("SomeProperty"));
        Assert.Null(rfs.GetProperty<string>("SomeProperty"));
    }

    [Fact]
    public void SetProperty_NewProperty_AddsToProperties()
    {
        var rfs = new ReferencedFileSave();

        rfs.SetProperty("SomeProperty", "value");

        Assert.Single(rfs.Properties);
        Assert.Equal("value", rfs.GetProperty<string>("SomeProperty"));
    }

    [Fact]
    public void SetProperty_ExistingProperty_OverwritesValue()
    {
        var rfs = new ReferencedFileSave();
        rfs.SetProperty("SomeProperty", "first");

        rfs.SetProperty("SomeProperty", "second");

        Assert.Single(rfs.Properties);
        Assert.Equal("second", rfs.GetProperty<string>("SomeProperty"));
    }
}
