using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Microsoft.Build.Evaluation;
using Shouldly;
using Xunit;

namespace GlueUnitTests.IO;

/// <summary>
/// Glue.exe and GlueCli both start evaluating the .csproj while plugins load
/// (<c>ProjectLoader.StartEarlyReads</c>), which only saves time if the load then takes that evaluation
/// instead of running its own.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ProjectLoaderEarlyEvaluationTests
{
    [StaFact]
    public async Task ModelOnlyLoad_ShouldUseTheEarlyEvaluation_InsteadOfEvaluatingAgain()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csproj = Path.Combine(project.Root, "Beefball", "Beefball.csproj");

        var loader = ProjectLoader.Self;
        var originalEarlyEvaluation = loader.EarlyEvaluation;
        var evaluations = 0;
        Project? earlyProject = null;
        try
        {
            loader.EarlyEvaluation = new EarlyProjectEvaluation(path =>
            {
                evaluations++;
                return earlyProject = ProjectCreator.EvaluateProject(path);
            });
            loader.StartEarlyReads(csproj);

            await loader.LoadProject(csproj, ProjectLoadScope.ModelOnly);

            evaluations.ShouldBe(1);
            ((VisualStudioProject)GlueState.Self.CurrentMainProject).Project.ShouldBeSameAs(earlyProject);
        }
        finally
        {
            loader.DiscardEarlyReads();
            loader.EarlyEvaluation = originalEarlyEvaluation;
        }
    }
}
