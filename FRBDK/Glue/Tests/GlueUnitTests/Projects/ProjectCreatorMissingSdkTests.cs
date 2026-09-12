using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Pins the "Missing SDK" error ProjectCreator.CreateProject raises when a .csproj asks for an SDK the
/// selected MSBuild engine cannot find (e.g. Blazor's SDK missing under an old fallback engine, GitHub
/// issue #2278). The old message only echoed MSBuild's raw exception, which never says which MSBuild
/// Glue picked or that installing a newer SDK would fix it.
/// </summary>
public class ProjectCreatorMissingSdkTests
{
    public ProjectCreatorMissingSdkTests()
    {
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
    }

    [Fact]
    public void CreateProject_NamesTheSelectedMsBuildAndSuggestsInstallingANewerSdk()
    {
        using var tempDir = new TempDir();
        var csproj = Path.Combine(tempDir.Root, "MissingSdk.csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk.ThisSdkDoesNotExist">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var msBuildExePath = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");

        var exception = Should.Throw<Exception>(() => ProjectCreator.CreateProject(csproj));

        exception.Message.ShouldContain("Missing SDK");
        exception.Message.ShouldContain(msBuildExePath);
        exception.Message.ShouldContain("Install a newer .NET SDK");
    }
}
