using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Glue.exe evaluates the .csproj it is about to open while plugins load (#2422). The load must use that
/// evaluation when it is for the same unchanged file, and evaluate again itself in every other case.
/// </summary>
[Collection("Frb2ProjectLoad")]
public class EarlyProjectEvaluationTests
{
    public EarlyProjectEvaluationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
    }

    static Project EmptyProject() =>
        new Project(ProjectRootElement.Create(), null, null, new ProjectCollection());

    [StaFact]
    public async Task LoadProject_ShouldUseTheEarlyEvaluation_WhenItIsForTheProjectBeingOpened()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var temp = new TempDir("EarlyEval_");
        var csproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);

        Project? evaluatedEarly = null;
        var original = ProjectLoader.Self.EarlyEvaluation;
        ProjectLoader.Self.EarlyEvaluation = new EarlyProjectEvaluation(
            path => evaluatedEarly = ProjectCreator.EvaluateProject(path));
        try
        {
            ProjectLoader.Self.EarlyEvaluation.Start(csproj);

            await GoldProject.LoadInGlueAsync(csproj);

            Assert.NotNull(evaluatedEarly);
            Assert.Same(evaluatedEarly, GlueState.Self.CurrentMainProject.Project);
        }
        finally
        {
            ProjectLoader.Self.EarlyEvaluation = original;
        }
    }

    [Fact]
    public void TryTake_ShouldReturnTheEvaluation_OnceOnly()
    {
        using var temp = new TempDir("EarlyEvalOnce_");
        var csproj = WriteFile(temp.Root, "Game.csproj");
        var project = EmptyProject();
        var early = new EarlyProjectEvaluation(_ => project);

        early.Start(csproj);

        Assert.Same(project, early.TryTake(csproj));
        Assert.Null(early.TryTake(csproj));
    }

    [Fact]
    public void TryTake_ShouldReturnNull_ForAnotherFile()
    {
        using var temp = new TempDir("EarlyEvalOther_");
        var csproj = WriteFile(temp.Root, "Game.csproj");
        var other = WriteFile(temp.Root, "Other.csproj");
        var early = new EarlyProjectEvaluation(_ => EmptyProject());

        early.Start(csproj);

        Assert.Null(early.TryTake(other));
        // Given up, so a later open of the first file evaluates it fresh.
        Assert.Null(early.TryTake(csproj));
    }

    [Fact]
    public void TryTake_ShouldReturnNull_WhenTheFileChangedSinceTheEvaluationStarted()
    {
        using var temp = new TempDir("EarlyEvalChanged_");
        var csproj = WriteFile(temp.Root, "Game.csproj");
        var early = new EarlyProjectEvaluation(_ => EmptyProject());

        early.Start(csproj);
        File.SetLastWriteTimeUtc(csproj, File.GetLastWriteTimeUtc(csproj).AddSeconds(5));

        Assert.Null(early.TryTake(csproj));
    }

    [Fact]
    public void TryTake_ShouldReturnNull_WhenTheEvaluationFailed()
    {
        using var temp = new TempDir("EarlyEvalFailed_");
        var csproj = WriteFile(temp.Root, "Game.csproj");
        var early = new EarlyProjectEvaluation(_ => throw new InvalidOperationException("bad project"));

        early.Start(csproj);

        Assert.Null(early.TryTake(csproj));
    }

    static string WriteFile(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, "<Project />");
        return path;
    }
}
