using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetTypeForCsvFile/GetUnqualifiedTypeForCsv read the shared static ObjectFinderCore.Self and
// GlueStateCore.Self, so this can't run concurrently with any other test class that swaps either
// out - hence the shared collection (see ObjectFinderCoreCollection in
// NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveTypeExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new() { GlueProject = new GlueProjectSave() };
    readonly FakeGlueStateCore _glueState = new() { ProjectNamespace = "MyGame" };

    public ReferencedFileSaveTypeExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
        GlueStateCore.Self = _glueState;
    }

    [Fact]
    public void GetTypeForCsvFile_UniformRowTypeSet_ReturnsUniformRowType()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Enemies.csv", UniformRowType = "MyGame.DataTypes.Enemy" };

        Assert.Equal("MyGame.DataTypes.Enemy", rfs.GetTypeForCsvFile());
    }

    [Fact]
    public void GetTypeForCsvFile_NoCustomClass_ReturnsNamespacedTypeFromFileName()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/EnemyFile.csv" };

        Assert.Equal("MyGame.DataTypes.Enemy", rfs.GetTypeForCsvFile());
    }

    [Fact]
    public void GetTypeForCsvFile_CustomClassWithNamespace_ReturnsCustomClassQualifiedName()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Enemies.csv" };
        _finder.GlueProject.CustomClasses.Add(new CustomClassSave
        {
            Name = "Enemy",
            CustomNamespace = "MyGame.Custom",
            CsvFilesUsingThis = { "GlobalContent/Enemies.csv" }
        });

        Assert.Equal("MyGame.Custom.Enemy", rfs.GetTypeForCsvFile());
    }

    [Fact]
    public void GetTypeForCsvFile_CustomClassWithoutNamespace_UsesProjectNamespace()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Enemies.csv" };
        _finder.GlueProject.CustomClasses.Add(new CustomClassSave
        {
            Name = "Enemy",
            CsvFilesUsingThis = { "GlobalContent/Enemies.csv" }
        });

        Assert.Equal("MyGame.DataTypes.Enemy", rfs.GetTypeForCsvFile());
    }

    [Fact]
    public void GetUnqualifiedTypeForCsv_StripsNamespace()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/EnemyFile.csv" };

        Assert.Equal("Enemy", rfs.GetUnqualifiedTypeForCsv());
    }
}
