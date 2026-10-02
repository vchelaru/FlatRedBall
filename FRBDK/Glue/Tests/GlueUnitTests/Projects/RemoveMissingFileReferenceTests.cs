using System.IO;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// GitHub issue #2347: the "file is referenced in the .csproj, but it does not exist on disk" error offers
/// a "Remove Reference" action, backed by VisualStudioProject.RemoveMissingFileReference.
/// </summary>
public class RemoveMissingFileReferenceTests
{
    [Fact]
    public void RemoveMissingFileReference_ShouldRemoveItem_WhenFileDoesNotExist()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Data\Missing.cs");
            var filePath = new FilePath(Path.Combine(directory, "Data", "Missing.cs"));
            project.IsFileReferenced(filePath).ShouldBeTrue();

            var removed = project.RemoveMissingFileReference(filePath);

            removed.ShouldBeTrue();
            project.IsFileReferenced(filePath).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveMissingFileReference_ShouldKeepItem_WhenFileExists()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Data\Present.cs");
            Directory.CreateDirectory(Path.Combine(directory, "Data"));
            File.WriteAllText(Path.Combine(directory, "Data", "Present.cs"), "// test file");
            var filePath = new FilePath(Path.Combine(directory, "Data", "Present.cs"));

            var removed = project.RemoveMissingFileReference(filePath);

            removed.ShouldBeFalse();
            project.IsFileReferenced(filePath).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveMissingFileReference_ShouldLeaveOtherMissingItemsAlone()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Data\MissingA.cs");
            project.AddCodeBuildItem(@"Data\MissingB.cs");
            var a = new FilePath(Path.Combine(directory, "Data", "MissingA.cs"));
            var b = new FilePath(Path.Combine(directory, "Data", "MissingB.cs"));

            project.RemoveMissingFileReference(a);

            project.IsFileReferenced(a).ShouldBeFalse();
            project.IsFileReferenced(b).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
