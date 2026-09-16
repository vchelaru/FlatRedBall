using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Build;

namespace GlueCommonUnitTests.SaveClasses;

// Reads the shared statics GlueStateCore.Self/BuildToolAssociationCore.Self, so this can't run
// concurrently with any other test class that swaps either out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveBuildExtensionsTests : IDisposable
{
    readonly string _tempDirectory;
    readonly FakeGlueStateCore _glueState;
    readonly FakeBuildToolAssociationCore _buildToolAssociation = new();

    public ReferencedFileSaveBuildExtensionsTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ReferencedFileSaveBuildExtensionsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);

        _glueState = new FakeGlueStateCore { CurrentMainProjectDirectory = _tempDirectory + Path.DirectorySeparatorChar };

        GlueStateCore.Self = _glueState;
        BuildToolAssociationCore.Self = _buildToolAssociation;
    }

    public void Dispose() => Directory.Delete(_tempDirectory, recursive: true);

    string WriteFile(string relativeName, DateTime lastWriteTime)
    {
        var fullName = Path.Combine(_tempDirectory, relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);
        File.WriteAllText(fullName, "");
        File.SetLastWriteTime(fullName, lastWriteTime);
        return fullName;
    }

    [Fact]
    public void GetIsFileOutOfDate_DestinationDoesNotExist_ReturnsTrue()
    {
        var source = WriteFile("source.txt", DateTime.Now);
        var destination = Path.Combine(_tempDirectory, "destination.txt");

        var rfs = new ReferencedFileSave();

        Assert.True(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_SourceNewerThanDestination_ReturnsTrue()
    {
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 1));
        var source = WriteFile("source.txt", new DateTime(2020, 1, 2));

        var rfs = new ReferencedFileSave();

        Assert.True(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_SourceOlderThanDestination_NoBuildTool_ReturnsFalse()
    {
        var source = WriteFile("source.txt", new DateTime(2020, 1, 1));
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 2));

        var rfs = new ReferencedFileSave();

        Assert.False(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_BuildToolNewerThanDestination_ReturnsTrue()
    {
        var source = WriteFile("source.txt", new DateTime(2020, 1, 1));
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 2));
        WriteFile("tool.exe", new DateTime(2020, 1, 3));

        var rfs = new ReferencedFileSave();
        _buildToolAssociation.SetBuildToolProcessed(rfs, "tool.exe");

        Assert.True(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_BuildToolOlderThanDestination_ReturnsFalse()
    {
        var source = WriteFile("source.txt", new DateTime(2020, 1, 1));
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 2));
        WriteFile("tool.exe", new DateTime(2019, 1, 1));

        var rfs = new ReferencedFileSave();
        _buildToolAssociation.SetBuildToolProcessed(rfs, "tool.exe");

        Assert.False(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_BuildToolProcessedFileDoesNotExist_ReturnsFalse()
    {
        var source = WriteFile("source.txt", new DateTime(2020, 1, 1));
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 2));

        var rfs = new ReferencedFileSave();
        _buildToolAssociation.SetBuildToolProcessed(rfs, "missing-tool.exe");

        Assert.False(rfs.GetIsFileOutOfDate(source, destination));
    }

    [Fact]
    public void GetIsFileOutOfDate_NoBuildToolAssociation_ReturnsFalse()
    {
        var source = WriteFile("source.txt", new DateTime(2020, 1, 1));
        var destination = WriteFile("destination.txt", new DateTime(2020, 1, 2));

        var rfs = new ReferencedFileSave();

        Assert.False(rfs.GetIsFileOutOfDate(source, destination));
    }
}
