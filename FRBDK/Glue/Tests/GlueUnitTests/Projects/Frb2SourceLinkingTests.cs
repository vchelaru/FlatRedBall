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

    /// <summary>
    /// Writes a minimal engine project - used for tests that pass its path explicitly. Mirrors the real
    /// FlatRedBall2 repo's layout (see samples/PlatformKing/PlatformKing.slnx): FlatRedBall2.csproj
    /// sits beside an AnimationChain.Common sibling that it references itself, unless
    /// <paramref name="withAnimationChainCommonSibling"/> is false.
    /// </summary>
    static string WriteFakeEngineProject(string root, bool withAnimationChainCommonSibling = true)
    {
        var srcDirectory = Path.Combine(root, "FakeFlatRedBall2Sibling", "src");
        Directory.CreateDirectory(srcDirectory);

        var csprojPath = Path.Combine(srcDirectory, "FlatRedBall2.csproj");
        File.WriteAllText(csprojPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");

        if (withAnimationChainCommonSibling)
        {
            var animationChainDirectory = Path.Combine(srcDirectory, "AnimationChain.Common");
            Directory.CreateDirectory(animationChainDirectory);
            File.WriteAllText(Path.Combine(animationChainDirectory, "AnimationChain.Common.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");
        }

        return csprojPath;
    }

    /// <summary>
    /// Writes the engine project (with its AnimationChain.Common sibling) at the exact relative layout
    /// <see cref="Frb2AddSourceManager"/>'s default lookup expects below a "Documents/GitHub"-shaped
    /// root: "FlatRedBall2/src/FlatRedBall2.csproj".
    /// </summary>
    static string WriteFakeEngineRepoUnderGithubRoot(string githubRoot)
    {
        var srcDirectory = Path.Combine(githubRoot, "FlatRedBall2", "src");
        Directory.CreateDirectory(srcDirectory);

        var csprojPath = Path.Combine(srcDirectory, "FlatRedBall2.csproj");
        File.WriteAllText(csprojPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var animationChainDirectory = Path.Combine(srcDirectory, "AnimationChain.Common");
        Directory.CreateDirectory(animationChainDirectory);
        File.WriteAllText(Path.Combine(animationChainDirectory, "AnimationChain.Common.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk"">
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

        // AnimationChain.Common is not a direct reference on the game project -
        // FlatRedBall2.csproj already references it, and the game gets it transitively. See the
        // real shape: FlatRedBall2 repo's samples/PlatformKing/PlatformKing.Common.csproj only
        // references FlatRedBall2.csproj, never AnimationChain.Common.csproj directly.
        Assert.DoesNotContain("AnimationChain.Common.csproj", savedCsprojText);

        // Both engine projects still show up in the solution though, matching
        // samples/PlatformKing/PlatformKing.slnx (minus its Blazor/KNI-only entries, out of scope here).
        var slnReferences = ReadSlnx(temp.Root).ReferencedProjects;
        Assert.Contains(slnReferences, reference => reference.Name.Contains("FlatRedBall2.csproj"));
        Assert.Contains(slnReferences, reference => reference.Name.Contains("AnimationChain.Common.csproj"));
    }

    [StaFact]
    public async Task LinkFrb2ProjectToSource_AddsNoFrb1StyleReferences()
    {
        // GitHub issue #2167 follow-up: the FRB1 "link to source" flow (AddSourceManager) adds a
        // whole tree of shared/platform projects - FlatRedBallShared.shproj, GumCore.*, SkiaInGum,
        // StateInterpolation.* - because FRB1's engine is split across that many projects. FRB2 ships
        // as one engine project (plus its AnimationChain.Common sibling), so none of that belongs on
        // an FRB2 project's solution or .csproj.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLinkNoBloat_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var engineCsproj = WriteFakeEngineProject(temp.Root);

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        Assert.True(new Frb2AddSourceManager().LinkFrb2ProjectToSource(frb2Project, engineCsproj).Succeeded);

        var frb1StyleNames = new[]
        {
            ".shproj", "GumCore", "SkiaInGum", "StateInterpolation", "FlatRedBallDesktopGLNet6",
            "FlatRedBall.Forms",
        };

        var savedCsprojText = File.ReadAllText(commonCsproj);
        var slnReferenceNames = ReadSlnx(temp.Root).ReferencedProjects.Select(reference => reference.Name).ToList();

        foreach (var frb1StyleName in frb1StyleNames)
        {
            Assert.DoesNotContain(frb1StyleName, savedCsprojText);
            Assert.DoesNotContain(slnReferenceNames, name => name.Contains(frb1StyleName));
        }

        // Exactly the two engine projects were added - Common and Desktop were already there.
        Assert.Equal(4, ReadSlnx(temp.Root).ReferencedProjects.Count);
    }

    [StaFact]
    public async Task LinkFrb2ProjectToSource_WhenAnimationChainCommonSiblingIsAbsent_StillLinksTheEngine()
    {
        // AnimationChain.Common is added to the solution for parity with a real hand-linked game, but
        // it is cosmetic (see the remarks on LinkFrb2ProjectToSource) - a layout that lacks it must not
        // block linking the engine itself.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SourceLinkNoAnimChain_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var engineCsproj = WriteFakeEngineProject(temp.Root, withAnimationChainCommonSibling: false);

        await GoldProject.LoadInGlueAsync(commonCsproj);
        var frb2Project = Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        var response = new Frb2AddSourceManager().LinkFrb2ProjectToSource(frb2Project, engineCsproj);

        Assert.True(response.Succeeded, response.Message);
        Assert.Contains("FlatRedBall2.csproj", File.ReadAllText(commonCsproj));
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
