using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CommandInterfaces;

public class ProjectCommandsTests
{
    // Regression test for a bug where Glue added directory paths (e.g. from a FileSystemWatcher folder
    // Created/Renamed event) as csproj <Content Include="..."> items. Since directory paths end with a
    // path separator, MSBuild rejected them with "A file item cannot end with a path separator."
    [Theory]
    [InlineData(@"C:\MyProject\Content\Entities\Bosses\ResonatorCoil\")]
    [InlineData(@"C:\MyProject\Content\Entities\Bosses\ResonatorCoil")]
    [InlineData(@"C:/MyProject/Content/Entities/Bosses/ResonatorCoil/")]
    public void ShouldSkipBecauseDirectory_ShouldReturnTrue_ForDirectoryPath(string path)
    {
        ProjectCommands.ShouldSkipBecauseDirectory(new FilePath(path)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(@"C:\MyProject\Content\Entities\Bosses\ResonatorCoil\ResonatorCoil.achx")]
    [InlineData(@"C:\MyProject\Content\Entities\Bosses\ResonatorCoil\Idle.png")]
    public void ShouldSkipBecauseDirectory_ShouldReturnFalse_ForFilePath(string path)
    {
        ProjectCommands.ShouldSkipBecauseDirectory(new FilePath(path)).ShouldBeFalse();
    }
}

// Regression test for GitHub issue #1734: AddNugetIfNotAdded (used to push Newtonsoft.Json into the game
// project whenever live edit is enabled, and by NAudioPlugin/ProfilePlugin) only ever touched
// GlueState.CurrentMainProject. A "synced project" (Glue > Add Synced Project) has its own .csproj that
// mirrors the main project's Compile/Content items (VisualStudioProject.SyncTo), but SyncTo never touches
// PackageReference items - so a synced project's .csproj never got the package and users saw a wall of
// compile errors in it.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ProjectCommandsAddNugetIfNotAddedTests : IDisposable
{
    private readonly bool _originalSynchronousMode;

    public ProjectCommandsAddNugetIfNotAddedTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalSynchronousMode = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        TaskManager.SynchronousMode = _originalSynchronousMode;
    }

    [Fact]
    public async Task AddNugetIfNotAddedWithReturn_ShouldAddPackage_ToSyncedProjectsToo()
    {
        var mainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var mainDirectory, "MainProject");
        var syncedProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var syncedDirectory, "SyncedProject");

        try
        {
            var glueState = new FlatRedBall.Glue.Plugins.ExportedInterfaces.GlueStateSnapshot
            {
                CurrentGlueProject = new GlueProjectSave
                {
                    FileVersion = (int)GlueProjectSave.GluxVersions.NugetPackageInCsproj
                },
                CurrentMainProject = mainProject,
                SyncedProjects = new ProjectBase[] { syncedProject }
            };

            var projectCommands = new ProjectCommands { _glueState = glueState };

            await projectCommands.AddNugetIfNotAddedWithReturn("Newtonsoft.Json", "13.0.3");

            syncedProject.HasPackage("Newtonsoft.Json", out var syncedVersion).ShouldBeTrue();
            syncedVersion.ShouldBe("13.0.3");
        }
        finally
        {
            Directory.Delete(mainDirectory, recursive: true);
            Directory.Delete(syncedDirectory, recursive: true);
        }
    }
}
