using System;
using System.IO;
using GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.AboutPlugin;

public class SourceRepoLocatorTests : IDisposable
{
    private readonly string _directory;
    private readonly FlatRedBall.Glue.VSHelpers.Projects.ClassLibraryProject _project;

    public SourceRepoLocatorTests()
    {
        _project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _directory);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void TryGetFrb1SourceRoots_ShouldReturnFalse_WhenProjectHasNoEngineProjectReference()
    {
        SourceRepoLocator.TryGetFrb1SourceRoots(_project, out var frbRoot, out var gumRoot).ShouldBeFalse();
        frbRoot.ShouldBeNull();
        gumRoot.ShouldBeNull();
    }

    [Fact]
    public void TryGetFrb1SourceRoots_ShouldResolveBothRoots_WhenProjectReferencesFrbAndGumEngineProjects()
    {
        AddProjectReference(@"..\Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj");
        AddProjectReference(@"..\GumCore\GumCoreXnaPc\GumCore.DesktopGlNet6\GumCore.DesktopGlNet6.csproj");

        var expectedRoot = Directory.GetParent(_directory).FullName;

        SourceRepoLocator.TryGetFrb1SourceRoots(_project, out var frbRoot, out var gumRoot).ShouldBeTrue();
        frbRoot.ShouldBe(expectedRoot);
        gumRoot.ShouldBe(expectedRoot);
    }

    [Fact]
    public void TryGetFrb2SourceRoot_ShouldReturnFalse_WhenProjectHasNoEngineProjectReference()
    {
        SourceRepoLocator.TryGetFrb2SourceRoot(_project, out var frbRoot).ShouldBeFalse();
        frbRoot.ShouldBeNull();
    }

    [Fact]
    public void TryGetFrb2SourceRoot_ShouldResolveRoot_WhenProjectReferencesFlatRedBall2Engine()
    {
        AddProjectReference(@"..\src\FlatRedBall2.csproj");

        var expectedRoot = Directory.GetParent(_directory).FullName;

        SourceRepoLocator.TryGetFrb2SourceRoot(_project, out var frbRoot).ShouldBeTrue();
        frbRoot.ShouldBe(expectedRoot);
    }

    private void AddProjectReference(string relativePath)
    {
        _project.Project.AddItem("ProjectReference", relativePath);
        _project.Project.MarkDirty();
        _project.Project.ReevaluateIfNecessary();
    }
}
