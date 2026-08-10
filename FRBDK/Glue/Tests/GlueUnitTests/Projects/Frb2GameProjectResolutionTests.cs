using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// `dotnet new frb2-desktop` produces two projects: MyGame.Common, which holds Game1, the content and
/// the engine reference, and MyGame.Desktop, a launcher that reaches the engine only through Common.
/// Common is the one Glue edits, but Desktop is the runnable one and an equally natural thing to pick
/// from a file dialog - and picking it used to end at "could not determine the project type", with the
/// project not loading at all.
/// </summary>
public class Frb2GameProjectResolutionTests : IDisposable
{
    readonly string _root;

    public Frb2GameProjectResolutionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Frb2Resolve_" + Guid.NewGuid());
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

    string WriteProject(string name, string itemGroupXml)
    {
        var directory = Path.Combine(_root, name);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".csproj");
        File.WriteAllText(path, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
{itemGroupXml}
  </ItemGroup>
</Project>");
        return path;
    }

    string WriteFrb2Common() =>
        WriteProject("MyGame.Common", @"    <PackageReference Include=""FlatRedBall2.MonoGame"" Version=""*-*"" />");

    string WriteDesktopLauncher() =>
        WriteProject("MyGame.Desktop",
            @"    <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.4.1"" />
    <ProjectReference Include=""..\MyGame.Common\MyGame.Common.csproj"" />");

    [Fact]
    public void PickingTheDesktopLauncher_ResolvesToTheCommonProject()
    {
        var common = WriteFrb2Common();
        var desktop = WriteDesktopLauncher();

        var resolved = Frb2ProjectDetector.FindFrb2GameProjectFor(desktop);

        Assert.Equal(Path.GetFullPath(common), Path.GetFullPath(resolved));
    }

    [Fact]
    public void PickingTheCommonProject_ResolvesToItself()
    {
        var common = WriteFrb2Common();

        Assert.Equal(Path.GetFullPath(common), Path.GetFullPath(Frb2ProjectDetector.FindFrb2GameProjectFor(common)));
    }

    [Fact]
    public void AnFrb1Project_ResolvesToNothing()
    {
        // Nothing here may redirect: silently opening a different project than the user picked would be
        // far worse than the message they get today.
        var frb1 = WriteProject("Frb1Game",
            @"    <PackageReference Include=""FlatRedBall.Forms"" Version=""1.0.0"" />");

        Assert.Null(Frb2ProjectDetector.FindFrb2GameProjectFor(frb1));
    }

    [Fact]
    public void AProjectReferencingSomethingMissing_ResolvesToNothingRatherThanThrowing()
    {
        var dangling = WriteProject("Dangling",
            @"    <ProjectReference Include=""..\NotThere\NotThere.csproj"" />");

        Assert.Null(Frb2ProjectDetector.FindFrb2GameProjectFor(dangling));
    }

    [Fact]
    public void AMissingOrUnreadableProject_ResolvesToNothingRatherThanThrowing()
    {
        Assert.Null(Frb2ProjectDetector.FindFrb2GameProjectFor(Path.Combine(_root, "Nope", "Nope.csproj")));
        Assert.Null(Frb2ProjectDetector.FindFrb2GameProjectFor(null));
    }

    [Fact]
    public void ResolutionIsOneHopOnly()
    {
        // A launcher pointing at a launcher pointing at Common is not a shape the template produces, and
        // chasing an arbitrary graph risks picking something the user never intended.
        WriteFrb2Common();
        WriteDesktopLauncher();
        var outer = WriteProject("MyGame.Outer",
            @"    <ProjectReference Include=""..\MyGame.Desktop\MyGame.Desktop.csproj"" />");

        Assert.Null(Frb2ProjectDetector.FindFrb2GameProjectFor(outer));
    }
}
