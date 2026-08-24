using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// `dotnet new frb2-desktop` produces MyGame.Common (what Glue edits, with no OutputType of its
/// own) and MyGame.Desktop (the runnable launcher). Pressing Play used to build/run Common, which
/// has no .exe to launch - so the button silently did nothing (#2188). This is what finds the
/// launcher given the game project Glue actually has loaded.
/// </summary>
public class Frb2LauncherProjectDetectorTests : IDisposable
{
    readonly string _root;

    public Frb2LauncherProjectDetectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Frb2Launcher_" + Guid.NewGuid());
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

    string WriteProject(string name, string itemGroupXml, string outputType = null)
    {
        var directory = Path.Combine(_root, name);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".csproj");
        var outputTypeXml = outputType == null ? "" : $"    <OutputType>{outputType}</OutputType>\n";
        File.WriteAllText(path, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
{outputTypeXml}  </PropertyGroup>
  <ItemGroup>
{itemGroupXml}
  </ItemGroup>
</Project>");
        return path;
    }

    string WriteCommon() =>
        WriteProject("MyGame.Common", @"    <PackageReference Include=""FlatRedBall2.MonoGame"" Version=""*-*"" />");

    string WriteDesktopLauncher() =>
        WriteProject("MyGame.Desktop",
            @"    <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.4.1"" />
    <ProjectReference Include=""..\MyGame.Common\MyGame.Common.csproj"" />",
            outputType: "WinExe");

    [Fact]
    public void ReferencingProjectWithExeOutputType_IsFoundAsTheLauncher()
    {
        var common = WriteCommon();
        var desktop = WriteDesktopLauncher();

        var found = Frb2ProjectDetector.FindFrb2LauncherProjectFor(common, new[] { common, desktop });

        Assert.Equal(Path.GetFullPath(desktop), found);
    }

    [Fact]
    public void ReferencingProjectWithoutExeOutputType_IsNotALauncher()
    {
        // A plain ProjectReference back to Common - e.g. a future test/tools project - is not
        // something Glue should ever try to run.
        var common = WriteCommon();
        var libraryReferencingCommon = WriteProject("MyGame.Tests",
            @"    <ProjectReference Include=""..\MyGame.Common\MyGame.Common.csproj"" />");

        var found = Frb2ProjectDetector.FindFrb2LauncherProjectFor(
            common, new[] { common, libraryReferencingCommon });

        Assert.Null(found);
    }

    [Fact]
    public void ExecutableProjectNotReferencingTheGame_IsNotALauncher()
    {
        var common = WriteCommon();
        var unrelatedExe = WriteProject("SomeOtherTool", "", outputType: "Exe");

        var found = Frb2ProjectDetector.FindFrb2LauncherProjectFor(common, new[] { common, unrelatedExe });

        Assert.Null(found);
    }

    [Fact]
    public void TwoCandidateLaunchers_ResolvesToNothingRatherThanGuessing()
    {
        var common = WriteCommon();
        var desktop = WriteDesktopLauncher();
        var android = WriteProject("MyGame.Android",
            @"    <ProjectReference Include=""..\MyGame.Common\MyGame.Common.csproj"" />",
            outputType: "Exe");

        var found = Frb2ProjectDetector.FindFrb2LauncherProjectFor(
            common, new[] { common, desktop, android });

        Assert.Null(found);
    }

    [Fact]
    public void TheGameProjectItself_IsNeverReturnedAsItsOwnLauncher()
    {
        var common = WriteCommon();

        var found = Frb2ProjectDetector.FindFrb2LauncherProjectFor(common, new[] { common });

        Assert.Null(found);
    }

    [Fact]
    public void NoCandidates_ResolvesToNull()
    {
        var common = WriteCommon();

        Assert.Null(Frb2ProjectDetector.FindFrb2LauncherProjectFor(common, Array.Empty<string>()));
        Assert.Null(Frb2ProjectDetector.FindFrb2LauncherProjectFor(common, null));
    }

    [Fact]
    public void MissingGameProject_ResolvesToNullRatherThanThrowing()
    {
        var desktop = WriteDesktopLauncher();

        Assert.Null(Frb2ProjectDetector.FindFrb2LauncherProjectFor(
            Path.Combine(_root, "Nope", "Nope.csproj"), new[] { desktop }));
        Assert.Null(Frb2ProjectDetector.FindFrb2LauncherProjectFor(null, new[] { desktop }));
    }
}
