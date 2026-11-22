using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

// Replaces ObjectFinder.Self.GlueProject, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class CustomVariableGetIsCsvTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly GlueProjectSave _project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
    private readonly EntitySave _entity = new EntitySave { Name = "Entities\\Enemy" };

    public CustomVariableGetIsCsvTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _project.Entities.Add(_entity);
        _project.Screens.Add(new ScreenSave { Name = "Screens\\GameScreen" });
        // A csv in global content, one in an entity, and one with a uniform row type.
        _project.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/EnemyInfo.csv" });
        _entity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Entities/Enemy/LootTable.csv" });
        _project.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/Names.csv", UniformRowType = "string" });
        _project.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/Hero.png" });
        ObjectFinder.Self.GlueProject = _project;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static bool IsCsv(string type) => new CustomVariable { Name = "Variable", Type = type }.GetIsCsv();

    // Row types are namespace-qualified, so they're derived from the file the same way Glue does.
    private static string RowTypeOf(string fileName) => new ReferencedFileSave { Name = fileName }.GetTypeForCsvFile();

    [Theory]
    [InlineData("GlobalContent/EnemyInfo.csv", true)]
    [InlineData("Entities/Enemy/LootTable.csv", true)]
    [InlineData("GlobalContent/Hero.png", false)]
    [InlineData("GlobalContent/Missing.csv", false)]
    public void GetIsCsv_RowTypeOfAFile_SameAnswerWithAndWithoutTheCsvTypeCache(string fileName, bool expected)
    {
        var type = RowTypeOf(fileName);

        IsCsv(type).ShouldBe(expected);

        using (CustomVariableExtensionMethods.CacheCsvTypes())
        {
            IsCsv(type).ShouldBe(expected);
        }
    }

    [Theory]
    [InlineData("string", true)] // the uniform row type of Names.csv
    [InlineData("float", false)]
    [InlineData("GlobalContent/Other.csv", true)]
    public void GetIsCsv_TypeName_SameAnswerWithAndWithoutTheCsvTypeCache(string type, bool expected)
    {
        IsCsv(type).ShouldBe(expected);

        using (CustomVariableExtensionMethods.CacheCsvTypes())
        {
            IsCsv(type).ShouldBe(expected);
        }
    }

    [Fact]
    public void GetIsCsv_CsvAddedAfterTheCacheScopeEnds_IsSeen()
    {
        var type = RowTypeOf("GlobalContent/WeaponInfo.csv");
        using (CustomVariableExtensionMethods.CacheCsvTypes())
        {
            IsCsv(type).ShouldBeFalse();
        }

        _project.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/WeaponInfo.csv" });

        IsCsv(type).ShouldBeTrue();
    }
}
