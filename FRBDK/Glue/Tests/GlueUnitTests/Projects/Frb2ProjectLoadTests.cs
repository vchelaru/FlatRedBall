using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// The whole point of issue #2021, driven through the same <c>ProjectLoader.LoadProject</c> Glue.exe
/// runs on File > Load Project: pointing the editor at an FRB2 game project gets a .gluj and nothing
/// else - no generated code, and the .csproj untouched.
/// </summary>
/// <remarks>
/// The fixture is written here rather than checked in because an FRB2 game's only distinguishing
/// feature is a ProjectReference to FlatRedBall2.csproj, which lives in a different repository. MSBuild
/// evaluation does not require a ProjectReference's target to exist, so a synthetic project is a
/// faithful stand-in for what <see cref="Frb2ProjectDetector"/> and the loader actually look at.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2ProjectLoadTests
{
    const string ProjectName = "Frb2LoadTestGame";

    static string WriteFrb2Project(string root)
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

        File.WriteAllText(Path.Combine(root, "Game1.cs"),
            "namespace " + ProjectName + ";\npublic class Game1\n{\n}\n");

        // A .slnx, not a .sln, because that is what the real FRB2 sample ships and what a solution
        // created in a recent Visual Studio looks like. ProjectSyncer.LocateSolution throws when it
        // finds no solution at all, and that exception surfaces during load - so getting this wrong
        // means the project does not open.
        File.WriteAllText(Path.Combine(root, ProjectName + ".slnx"),
            $"<Solution>\n  <Project Path=\"{ProjectName}.csproj\" />\n</Solution>\n");
        return csprojPath;
    }

    [StaFact]
    public async Task LoadingAnFrb2Project_CreatesTheGluj_AndWritesNothingElse()
    {
        // The full game-project plugin set, not just the embedded ones: which plugins are registered is
        // process-wide, so registering them here is what makes this assertion mean the same thing run
        // on its own and run in the middle of the suite. It is also the point - a generator that writes
        // outside GenerateCodeCommands only shows up when its plugin is loaded.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Load_");
        var csprojPath = WriteFrb2Project(temp.Root);
        var csprojBefore = File.ReadAllText(csprojPath);

        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        // The .gluj lands next to the .csproj, which is where FRB2 resolves its Content folder from.
        Assert.True(File.Exists(Path.Combine(temp.Root, ProjectName + ".gluj")),
            "Loading an FRB2 project should have created its .gluj at the project root.");

        // Glue does not own this project's .csproj.
        Assert.Equal(csprojBefore, File.ReadAllText(csprojPath));

        // Nothing generated. Game1.cs is the one file the fixture itself wrote.
        var codeFiles = Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .ToList();
        Assert.Equal(new[] { "Game1.cs" }, codeFiles);

        // Directories too, not just files. Suppressing a generator at its file write still leaves it
        // creating the destination folder and announcing "Added file to project" for a file that was
        // never written - which is what the user sees, and it reads as Glue scaffolding into a project
        // it is meant to leave alone. Content/ and GlueSettings/ are Glue's to manage.
        // Allow-list rather than a list of the folders that have gone wrong so far, so a generator
        // nobody has thought of yet fails this too. Content/ and GlueSettings/ are Glue's to write.
        var unexpectedDirectories = Directory.GetDirectories(temp.Root, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(temp.Root, d).Replace('\\', '/'))
            .Where(d => d != "Content" && d != "GlueSettings")
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.Empty(unexpectedDirectories);
    }
}
