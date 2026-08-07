using System;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ExportedImplementations;

// Assigns GlueState.Self.CurrentMainProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class GlueStateEngineDllSyntaxVersionTests : IDisposable
{
    private readonly VisualStudioProject _originalMainProject;

    public GlueStateEngineDllSyntaxVersionTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
    }

    [Fact]
    public void EngineDllSyntaxVersion_ShouldReturnNull_WhenNoProjectIsLoaded()
    {
        // Reproduces #1999: Help -> About after closing a project (CurrentMainProject == null)
        // used to throw a NullReferenceException instead of showing "<No Project Loaded>".
        GlueState.Self.CurrentMainProject = null;

        GlueState.Self.EngineDllSyntaxVersion.ShouldBeNull();
    }
}
