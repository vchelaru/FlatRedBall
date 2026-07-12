using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.IO;
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
