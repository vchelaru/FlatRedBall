using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Pins the one thing every end-to-end test depends on: that the test host can evaluate a real
/// SDK-style .csproj through the same <see cref="ProjectCreator"/> call Glue.exe's File > Load Project
/// makes. When it can't, the whole LiveGame/gold-project suite fails with a "Missing SDK" exception
/// that says nothing about the real cause (which .NET SDK MSBUILD_EXE_PATH points at). See GitHub
/// issue #2218.
/// </summary>
public class SdkStyleProjectLoadTests
{
    public SdkStyleProjectLoadTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void CreateProject_EvaluatesACheckedInSdkStyleProject()
    {
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();

        var csproj = Path.Combine(GoldProject.FindRepoRoot(),
            "Samples", "EditorTest1", "EditorTest1", "EditorTest1.csproj");

        var result = ProjectCreator.CreateProject(csproj);

        Assert.NotNull(result.Project);
    }
}
