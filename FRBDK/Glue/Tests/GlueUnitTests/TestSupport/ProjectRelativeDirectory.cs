using System;
using FlatRedBall.IO;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Points <see cref="FileManager.RelativeDirectory"/> at a test project's directory on every thread, as
/// <c>ProjectLoader</c> does for a real load, and restores both values on Dispose.
/// </summary>
/// <remarks>
/// <see cref="FileManager.RelativeDirectory"/> is per thread. Setting only it covers the constructor's
/// thread, while an async test resumes on pool threads after its awaits, and those read
/// <see cref="FileManager.DefaultRelativeDirectory"/>: the test's bin folder. Relative names then resolve
/// in two places at once, the project's own lookups against the project and file paths against bin
/// (#2443).
/// </remarks>
internal sealed class ProjectRelativeDirectory : IDisposable
{
    readonly string _originalRelativeDirectory;
    readonly string _originalDefaultRelativeDirectory;

    public ProjectRelativeDirectory(string projectDirectory)
    {
        _originalRelativeDirectory = FileManager.RelativeDirectory;
        _originalDefaultRelativeDirectory = FileManager.DefaultRelativeDirectory;

        var directory = projectDirectory.TrimEnd('\\', '/') + "\\";
        FileManager.RelativeDirectory = directory;
        FileManager.DefaultRelativeDirectory = FileManager.RelativeDirectory;
    }

    public void Dispose()
    {
        FileManager.DefaultRelativeDirectory = _originalDefaultRelativeDirectory;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
    }
}
