using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.FrbSourcePlugin.Managers;
using PluginTestbed.GlobalContentManagerPlugins;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// GitHub issue #2167: creating an FRB2 project with "link to source" checked ran FRB1's
/// AddSourceManager (.shproj shared projects, a classic .sln) against an FRB2 project, which uses
/// neither - it failed with "Unable to parse solution file" and surfaced that through a raw
/// MessageBox.Show on a background TaskManager thread, which read as Glue hanging rather than an
/// obvious dialog. These pin the real FRB2 linking path added in its place.
/// </summary>
[Collection("Frb2ProjectLoad")]
public class Frb2SourceLinkingTests
{
    const string ProjectName = Frb2ProjectFixture.ProjectName;

    /// <summary>Writes a minimal engine project anywhere - used for tests that pass its path explicitly.</summary>
    static string WriteFakeEngineProject(string root)
    {
        var engineDirectory = Path.Combine(root, "FakeFlatRedBall2Sibling", "src");
        Directory.CreateDirectory(engineDirectory);

        var csprojPath = Path.Combine(engineDirectory, "FlatRedBall2.csproj");
        File.WriteAllText(csprojPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");

        return csprojPath;
    }

    /// <summary>
    /// Writes the engine project at the exact relative layout <see cref="Frb2AddSourceManager"/>'s
    /// default lookup expects below a "Documents/GitHub"-shaped root: "FlatRedBall2/src/FlatRedBall2.csproj".
    /// </summary>
    static string WriteFakeEngineRepoUnderGithubRoot(string githubRoot)
    {
        var engineDirectory = Path.Combine(githubRoot, "FlatRedBall2", "src");
        Directory.CreateDirectory(engineDirectory);

        var csprojPath = Path.Combine(engineDirectory, "FlatRedBall2.csproj");
        File.WriteAllText(csprojPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");

        return csprojPath;
    }

    static VSSolution ReadSlnx(string root) =>
        VSSolution.FromFile(new FilePath(Path.Combine(root, ProjectName + ".slnx")));

    [StaFact]
    public async Task LinkFrb2ProjectToSource_SwapsThePackageReference_ForAProjectReferenceToTheSiblingEngine()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLink_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var engineCsproj = WriteFakeEngineProject(temp.Root);

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        var response = new Frb2AddSourceManager().LinkFrb2ProjectToSource(frb2Project, engineCsproj);

        Assert.True(response.Succeeded, response.Message);

        var savedCsprojText = File.ReadAllText(commonCsproj);
        Assert.DoesNotContain("FlatRedBall2.MonoGame", savedCsprojText);
        Assert.Contains("FlatRedBall2.csproj", savedCsprojText);

        Assert.Contains(ReadSlnx(temp.Root).ReferencedProjects,
            reference => reference.Name.Contains("FlatRedBall2.csproj"));
    }

    [StaFact]
    public async Task LinkFrb2ProjectToSource_CalledTwice_DoesNotDuplicateTheReference()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLinkTwice_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var engineCsproj = WriteFakeEngineProject(temp.Root);

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        var manager = new Frb2AddSourceManager();
        Assert.True(manager.LinkFrb2ProjectToSource(frb2Project, engineCsproj).Succeeded);
        var secondResponse = manager.LinkFrb2ProjectToSource(frb2Project, engineCsproj);

        Assert.True(secondResponse.Succeeded, secondResponse.Message);

        var projectReferenceCount = frb2Project.EvaluatedItems
            .Count(item => item.ItemType == "ProjectReference" && item.EvaluatedInclude.Contains("FlatRedBall2.csproj"));
        Assert.Equal(1, projectReferenceCount);

        Assert.Equal(1, ReadSlnx(temp.Root).ReferencedProjects
            .Count(reference => reference.Name.Contains("FlatRedBall2.csproj")));
    }

    [StaFact]
    public async Task LinkFrb2ProjectToSource_WhenEngineSourceIsMissing_ReturnsAnErrorWithoutModifyingTheProject()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLinkMissing_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var missingEngineCsproj = Path.Combine(temp.Root, "NoSuchSibling", "src", "FlatRedBall2.csproj");

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        var csprojBefore = File.ReadAllText(commonCsproj);

        var response = new Frb2AddSourceManager().LinkFrb2ProjectToSource(frb2Project, missingEngineCsproj);

        Assert.False(response.Succeeded);
        Assert.Equal(csprojBefore, File.ReadAllText(commonCsproj));
    }

    [StaFact]
    public async Task AddFrbSourceToDefaultLocation_OnAnFrb2Project_LinksFrb2SourceInsteadOfRunningFrb1Logic()
    {
        // Before the fix, this call reached AddSourceManager (FlatRedBallShared.shproj etc. against a
        // .slnx solution) and failed with "Unable to parse solution file".
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLinkDefault_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var githubRoot = Path.Combine(temp.Root, "GithubRoot");
        WriteFakeEngineRepoUnderGithubRoot(githubRoot);

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        Frb2AddSourceManager.GithubFilePathOverrideForTesting = githubRoot;
        try
        {
            await new FrbSourcePlugin().AddFrbSourceToDefaultLocation(frb2Project);
        }
        finally
        {
            Frb2AddSourceManager.GithubFilePathOverrideForTesting = null;
        }

        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
        Assert.Contains("FlatRedBall2.csproj", File.ReadAllText(commonCsproj));
    }

    [Fact]
    public void HasFrbAndGumReposInDefaultLocation_IsTrue_WhenOnlyTheFrb2RepoExists()
    {
        using var temp = new TempDir("Frb2DefaultRepoCheck_");
        WriteFakeEngineRepoUnderGithubRoot(temp.Root);

        Frb2AddSourceManager.GithubFilePathOverrideForTesting = temp.Root;
        try
        {
            Assert.True(new FrbSourcePlugin().HasFrbAndGumReposInDefaultLocation());
        }
        finally
        {
            Frb2AddSourceManager.GithubFilePathOverrideForTesting = null;
        }
    }
}
