using System;
using System.IO;
using FlatRedBall.Glue.Diagnostics;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Diagnostics;

public class CrashLogTests : IDisposable
{
    private readonly string _originalDirectory = CrashLog.Directory;
    private readonly TempDir _tempDir = new();

    public CrashLogTests()
    {
        CrashLog.Directory = Path.Combine(_tempDir.Root, "Diagnostics");
    }

    public void Dispose()
    {
        CrashLog.Directory = _originalDirectory;
        _tempDir.Dispose();
    }

    [Fact]
    public void Write_ShouldPersistTheExceptionText_AndReturnThePath()
    {
        var exception = new InvalidOperationException("Could not find the value for type BitmapFont");

        var path = CrashLog.Write(exception);

        path.ShouldNotBeNull();
        File.ReadAllText(path).ShouldContain("Could not find the value for type BitmapFont");
    }

    [Fact]
    public void Write_ShouldReturnNull_InsteadOfThrowing_WhenTheDirectoryCannotBeCreated()
    {
        // A file where the directory should be makes CreateDirectory fail.
        File.WriteAllText(CrashLog.Directory, "");

        Should.NotThrow(() => CrashLog.Write(new Exception("x"))).ShouldBeNull();
    }
}
