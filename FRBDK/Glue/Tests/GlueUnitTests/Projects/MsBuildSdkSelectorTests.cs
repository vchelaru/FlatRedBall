using FlatRedBall.Glue.VSHelpers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

public class MsBuildSdkSelectorTests
{
    // Real "dotnet --list-sdks" output from a machine with the .NET 10 SDK installed - the case that
    // broke every project-loading test (GitHub issue #2218).
    const string ListSdksOutput = @"3.0.103 [C:\Program Files\dotnet\sdk]
3.1.417 [C:\Program Files\dotnet\sdk]
7.0.101 [C:\Program Files\dotnet\sdk]
8.0.303 [C:\Program Files\dotnet\sdk]
10.0.400 [C:\Program Files\dotnet\sdk]
";

    [Fact]
    public void NewestUsableSdkIsPreferred()
    {
        var paths = MsBuildSdkSelector.GetMsBuildPathsNewestFirst(ListSdksOutput, maximumMajorVersion: 8);

        paths[0].ShouldBe(@"C:\Program Files\dotnet\sdk\8.0.303\MSBuild.dll");
    }

    [Fact]
    public void SdksNewerThanTheRunningRuntimeAreExcluded()
    {
        // Their MSBuild and SDK resolvers are compiled against a newer runtime than the editor process,
        // so loading them throws "Could not load file or assembly 'System.Runtime, Version=10.0.0.0'"
        // rather than resolving anything.
        var paths = MsBuildSdkSelector.GetMsBuildPathsNewestFirst(ListSdksOutput, maximumMajorVersion: 8);

        paths.ShouldNotContain(@"C:\Program Files\dotnet\sdk\10.0.400\MSBuild.dll");
    }

    [Fact]
    public void OlderSdksRemainAsFallbacksInDescendingOrder()
    {
        var paths = MsBuildSdkSelector.GetMsBuildPathsNewestFirst(ListSdksOutput, maximumMajorVersion: 8);

        paths.ShouldBe(new[]
        {
            @"C:\Program Files\dotnet\sdk\8.0.303\MSBuild.dll",
            @"C:\Program Files\dotnet\sdk\7.0.101\MSBuild.dll",
            @"C:\Program Files\dotnet\sdk\3.1.417\MSBuild.dll",
            @"C:\Program Files\dotnet\sdk\3.0.103\MSBuild.dll",
        });
    }

    [Fact]
    public void NoUsableSdkYieldsNoCandidates()
    {
        var paths = MsBuildSdkSelector.GetMsBuildPathsNewestFirst(
            @"10.0.400 [C:\Program Files\dotnet\sdk]", maximumMajorVersion: 8);

        paths.ShouldBeEmpty();
    }
}
