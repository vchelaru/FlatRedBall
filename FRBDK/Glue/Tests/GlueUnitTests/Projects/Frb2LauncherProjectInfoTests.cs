using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// <see cref="Frb2LauncherProjectInfo"/> tells Runner where an FRB2 launcher's own build output
/// lands, without an MSBuild evaluation - so Runner can find MyGame.Desktop.exe instead of trying
/// (and failing) to run MyGame.Common, which has no OutputType of its own (#2188).
/// </summary>
public class Frb2LauncherProjectInfoTests : IDisposable
{
    readonly string _root;

    public Frb2LauncherProjectInfoTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Frb2LauncherInfo_" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    string WriteCsproj(string propertyGroupXml)
    {
        var path = Path.Combine(_root, "MyGame.Desktop.csproj");
        File.WriteAllText(path, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
{propertyGroupXml}
  </PropertyGroup>
</Project>");
        return path;
    }

    [Fact]
    public void DefaultAssemblyName_FallsBackToTheProjectFileName()
    {
        var csproj = WriteCsproj("    <TargetFramework>net8.0</TargetFramework>");

        var info = Frb2LauncherProjectInfo.TryGet(csproj, "Debug");

        Assert.NotNull(info);
        Assert.Equal("MyGame.Desktop", info.Value.ExecutableName);
        Assert.Equal(
            Path.Combine(_root, "bin", "Debug", "net8.0", "MyGame.Desktop.exe").Replace('\\', '/'),
            info.Value.ExeLocation.Replace('\\', '/'));
    }

    [Fact]
    public void ExplicitAssemblyName_IsUsedForBothTheExeNameAndProcessName()
    {
        var csproj = WriteCsproj(
            "    <TargetFramework>net8.0</TargetFramework>\n    <AssemblyName>MyGame</AssemblyName>");

        var info = Frb2LauncherProjectInfo.TryGet(csproj, "Debug");

        Assert.NotNull(info);
        Assert.Equal("MyGame", info.Value.ExecutableName);
        Assert.EndsWith("MyGame.exe", info.Value.ExeLocation);
    }

    [Fact]
    public void ConfigurationIsHonored_UnlikeTheCommonProjectPath()
    {
        var csproj = WriteCsproj("    <TargetFramework>net8.0</TargetFramework>");

        var info = Frb2LauncherProjectInfo.TryGet(csproj, "Release");

        Assert.NotNull(info);
        Assert.Contains("/Release/", info.Value.ExeLocation.Replace('\\', '/'));
    }

    [Fact]
    public void MultiTargetedProject_ResolvesToNull()
    {
        // Which folder wins depends on which TFM built (and ran) last - not worth guessing.
        var csproj = WriteCsproj("    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>");

        Assert.Null(Frb2LauncherProjectInfo.TryGet(csproj, "Debug"));
    }

    [Fact]
    public void CustomOutputPath_ResolvesToNullRatherThanGuessingWrong()
    {
        var csproj = WriteCsproj(
            "    <TargetFramework>net8.0</TargetFramework>\n    <OutputPath>CustomOut\\</OutputPath>");

        Assert.Null(Frb2LauncherProjectInfo.TryGet(csproj, "Debug"));
    }

    [Fact]
    public void MissingProject_ResolvesToNullRatherThanThrowing()
    {
        Assert.Null(Frb2LauncherProjectInfo.TryGet(
            Path.Combine(_root, "Nope.csproj"), "Debug"));
        Assert.Null(Frb2LauncherProjectInfo.TryGet(null, "Debug"));
    }
}
