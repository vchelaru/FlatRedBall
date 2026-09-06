using System;
using System.Collections.Generic;
using FlatRedBall.Glue.VSHelpers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

public class MsBuildSdkSelectorTests
{
    // Real "dotnet --list-sdks" output shape, covering the three ways an SDK can be unusable: too old to
    // exist on disk, MSBuild newer than the engine (8.0.4xx), and a newer major entirely (10.x).
    const string ListSdksOutput = @"3.1.417 [C:\Program Files\dotnet\sdk]
6.0.428 [C:\Program Files\dotnet\sdk]
8.0.303 [C:\Program Files\dotnet\sdk]
8.0.424 [C:\Program Files\dotnet\sdk]
10.0.400 [C:\Program Files\dotnet\sdk]
";

    // What each of those SDKs ships, as read off its MSBuild.dll.
    static readonly Dictionary<string, Version> InstalledMsBuildVersions = new()
    {
        [@"C:\Program Files\dotnet\sdk\3.1.417\MSBuild.dll"] = new Version(16, 7),
        [@"C:\Program Files\dotnet\sdk\6.0.428\MSBuild.dll"] = new Version(17, 3),
        [@"C:\Program Files\dotnet\sdk\8.0.303\MSBuild.dll"] = new Version(17, 10),
        [@"C:\Program Files\dotnet\sdk\8.0.424\MSBuild.dll"] = new Version(17, 14),
        [@"C:\Program Files\dotnet\sdk\10.0.400\MSBuild.dll"] = new Version(18, 9),
    };

    static Version LookUp(string path) =>
        InstalledMsBuildVersions.TryGetValue(path, out var version) ? version : null;

    static readonly Version Engine = new(17, 3);

    [Fact]
    public void NewestSdkTheEngineCanReadIsPreferred()
    {
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(ListSdksOutput, Engine, LookUp);

        paths[0].ShouldBe(@"C:\Program Files\dotnet\sdk\6.0.428\MSBuild.dll");
    }

    [Fact]
    public void SdksShippingAnMsBuildNewerThanTheEngineAreExcluded()
    {
        // Their Microsoft.Common.CurrentVersion.targets calls property functions the engine does not have,
        // e.g. "[MSBuild]::SubstringByAsciiChars" in the 8.0.4xx SDK, and every evaluation dies on it.
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(ListSdksOutput, Engine, LookUp);

        paths.ShouldNotContain(@"C:\Program Files\dotnet\sdk\8.0.424\MSBuild.dll");
        paths.ShouldNotContain(@"C:\Program Files\dotnet\sdk\8.0.303\MSBuild.dll");
        paths.ShouldNotContain(@"C:\Program Files\dotnet\sdk\10.0.400\MSBuild.dll");
    }

    [Fact]
    public void OlderSdksRemainAsFallbacksInDescendingOrder()
    {
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(ListSdksOutput, Engine, LookUp);

        paths.ShouldBe(new[]
        {
            @"C:\Program Files\dotnet\sdk\6.0.428\MSBuild.dll",
            @"C:\Program Files\dotnet\sdk\3.1.417\MSBuild.dll",
        });
    }

    [Fact]
    public void ANewerEngineUnlocksNewerSdks()
    {
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(
            ListSdksOutput, new Version(17, 14), LookUp);

        paths[0].ShouldBe(@"C:\Program Files\dotnet\sdk\8.0.424\MSBuild.dll");
    }

    [Fact]
    public void SdksWhoseMsBuildIsMissingAreSkipped()
    {
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(
            @"5.0.100 [C:\Program Files\dotnet\sdk]", Engine, LookUp);

        paths.ShouldBeEmpty();
    }

    [Fact]
    public void NoUsableSdkYieldsNoCandidates()
    {
        var paths = MsBuildSdkSelector.GetUsableMsBuildPathsNewestFirst(
            @"10.0.400 [C:\Program Files\dotnet\sdk]", Engine, LookUp);

        paths.ShouldBeEmpty();
    }

    [Fact]
    public void EngineVersionIsTheMsBuildGlueActuallyLoads()
    {
        // Guards the assumption the whole rule rests on: that the version is readable at all. A default of
        // 0.0 (unreadable Location, e.g. a single-file publish) would exclude every installed SDK.
        MsBuildSdkSelector.EngineVersion.Major.ShouldBeGreaterThan(0);
    }
}
