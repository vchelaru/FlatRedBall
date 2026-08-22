using FlatRedBall.Glue.SaveClasses;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

public class GlueProjectSaveNewProjectVersionTests
{
    [Fact]
    public void GetFileVersionForNewProject_ShouldReturnLatestVersion_WhenEngineDllSyntaxVersionIsUnknown()
    {
        // Reproduces the "could not determine engine dll syntax version" case (no FlatRedBall
        // PackageReference resolved yet) - fall back to Glue's own latest version.
        GlueProjectSave.GetFileVersionForNewProject(engineDllSyntaxVersion: null)
            .ShouldBe(GlueProjectSave.LatestVersion);
    }

    [Fact]
    public void GetFileVersionForNewProject_ShouldReturnEngineDllSyntaxVersion_WhenLowerThanLatestVersion()
    {
        // Reproduces #2163: a new project's referenced FlatRedBall NuGet package (e.g. one grabbed
        // before a newer Glue's NuGet packages were published) supports an older syntax version than
        // Glue itself. The new project's FileVersion should not outrun what its own engine dll supports.
        var engineDllSyntaxVersion = GlueProjectSave.LatestVersion - 1;

        GlueProjectSave.GetFileVersionForNewProject(engineDllSyntaxVersion)
            .ShouldBe(engineDllSyntaxVersion);
    }

    [Fact]
    public void GetFileVersionForNewProject_ShouldReturnLatestVersion_WhenEngineDllSyntaxVersionIsHigherOrEqual()
    {
        var engineDllSyntaxVersion = GlueProjectSave.LatestVersion + 1;

        GlueProjectSave.GetFileVersionForNewProject(engineDllSyntaxVersion)
            .ShouldBe(GlueProjectSave.LatestVersion);
    }
}
