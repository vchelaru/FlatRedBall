using System.IO;
using Shouldly;

namespace GlueUnitTests.TestSupport;

public class TestVisualStudioProjectFactoryTests
{
    [Fact]
    public void CreateInNewTempDirectory_ShouldReturnARealMsBuildBackedProject()
    {
        string? directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory, "ProbeProject");

            project.ShouldNotBeNull();
            project.RootNamespace.ShouldBe("ProbeProject");
            project.FullFileName.FullPath.ShouldContain("ProbeProject.csproj");
            project.CodeProject.ShouldBe(project);
        }
        finally
        {
            if (directory != null)
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
