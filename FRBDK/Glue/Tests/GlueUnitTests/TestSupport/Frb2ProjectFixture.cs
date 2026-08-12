using System.IO;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Writes a synthetic FlatRedBall 2 game project on disk for tests that drive the real
/// <c>ProjectLoader.LoadProject</c>.
/// </summary>
/// <remarks>
/// Written rather than checked in because an FRB2 game's only distinguishing feature is a
/// ProjectReference to FlatRedBall2.csproj, which lives in a different repository. MSBuild evaluation
/// does not require a ProjectReference's target to exist, so a synthetic project is a faithful stand-in
/// for what <c>Frb2ProjectDetector</c> and the loader actually look at.
/// </remarks>
internal static class Frb2ProjectFixture
{
    public const string ProjectName = "Frb2LoadTestGame";

    /// <summary>The one .cs file the fixture itself writes, which every "nothing was generated" assertion excludes.</summary>
    public const string HandWrittenCodeFile = "Game1.cs";

    /// <summary>
    /// A single-project FRB2 game referencing the engine by source. Returns the .csproj path.
    /// </summary>
    public static string Write(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Content"));

        var csprojPath = Path.Combine(root, ProjectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{ProjectName}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\FlatRedBall2.csproj"" />
  </ItemGroup>
</Project>");

        File.WriteAllText(Path.Combine(root, HandWrittenCodeFile),
            "namespace " + ProjectName + ";\npublic class Game1\n{\n}\n");

        // A .slnx, not a .sln, because that is what the real FRB2 sample ships and what a solution
        // created in a recent Visual Studio looks like. ProjectSyncer.LocateSolution throws when it
        // finds no solution at all, and that exception surfaces during load - so getting this wrong
        // means the project does not open.
        File.WriteAllText(Path.Combine(root, ProjectName + ".slnx"),
            $"<Solution>\n  <Project Path=\"{ProjectName}.csproj\" />\n</Solution>\n");
        return csprojPath;
    }

    /// <summary>
    /// The layout `dotnet new frb2-desktop` produces: a MyGame.Common holding Game1, Content and the
    /// engine PackageReference, a MyGame.Desktop launcher that only reaches the engine through Common,
    /// and a .slnx one level up. Returns the Common .csproj path.
    /// </summary>
    /// <remarks>
    /// The version is kept out of the Include deliberately - the shipped template writes it inline
    /// (Version="*-*") while central package management puts it in Directory.Packages.props, and the
    /// detector has to match on the package id either way.
    /// </remarks>
    public static string WriteTemplateShaped(string root)
    {
        var commonDirectory = Path.Combine(root, ProjectName + ".Common");
        var desktopDirectory = Path.Combine(root, ProjectName + ".Desktop");
        Directory.CreateDirectory(Path.Combine(commonDirectory, "Content"));
        Directory.CreateDirectory(desktopDirectory);

        File.WriteAllText(Path.Combine(root, "Directory.Packages.props"),
            "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n" +
            "  </PropertyGroup>\n  <ItemGroup>\n    <PackageVersion Include=\"FlatRedBall2.MonoGame\" Version=\"1.0.0\" />\n" +
            "  </ItemGroup>\n</Project>\n");

        var commonCsproj = Path.Combine(commonDirectory, ProjectName + ".Common.csproj");
        File.WriteAllText(commonCsproj, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{ProjectName}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""FlatRedBall2.MonoGame"" />
    <PackageReference Include=""MonoGame.Framework.DesktopGL"" />
  </ItemGroup>
</Project>");

        File.WriteAllText(Path.Combine(commonDirectory, HandWrittenCodeFile),
            "namespace " + ProjectName + ";\npublic class Game1\n{\n}\n");

        File.WriteAllText(Path.Combine(desktopDirectory, ProjectName + ".Desktop.csproj"), $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""MonoGame.Framework.DesktopGL"" />
    <ProjectReference Include=""..\{ProjectName}.Common\{ProjectName}.Common.csproj"" />
  </ItemGroup>
</Project>");

        File.WriteAllText(Path.Combine(root, ProjectName + ".slnx"),
            $"<Solution>\n  <Project Path=\"{ProjectName}.Common/{ProjectName}.Common.csproj\" />\n" +
            $"  <Project Path=\"{ProjectName}.Desktop/{ProjectName}.Desktop.csproj\" />\n</Solution>\n");

        return commonCsproj;
    }
}
