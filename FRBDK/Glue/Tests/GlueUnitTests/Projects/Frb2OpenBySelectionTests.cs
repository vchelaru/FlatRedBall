using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Everything File &gt; Load Project does after the file picker closes, for the files an FRB2 project
/// actually offers. <c>ProjectFileResolverTests</c> pins what the resolver returns; these prove the
/// path it returns opens the game.
/// </summary>
/// <remarks>
/// Same collection as <see cref="Frb2ProjectLoadTests"/>: loading a project is process-wide state.
/// The one part of the dialog no test can reach is the file filter itself.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2OpenBySelectionTests
{
    const string ProjectName = Frb2ProjectFixture.ProjectName;

    [StaFact]
    public async Task PickingTheGluj_OfAnFrb2Project_OpensTheGame()
    {
        // The reported bug: the .gluj lives under Content/FrbEditor, so swapping the extension in place
        // pointed at Content/FrbEditor/<name>.csproj and the load stopped at "Could not find the
        // project".
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2OpenGluj_");
        var csprojPath = Frb2ProjectFixture.Write(temp.Root);

        // The .gluj is written by the first load, so open by .csproj once to get the state a user's
        // project is already in.
        await GoldProject.LoadInGlueAsync(csprojPath);

        var gluj = Path.Combine(temp.Root, "Content", "FrbEditor", ProjectName + ".gluj");
        Assert.True(File.Exists(gluj), "The first load should have written the .gluj.");

        await GoldProject.LoadInGlueAsync(ProjectFileResolver.ResolveCsproj(gluj));

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.Equal(new FilePath(csprojPath), GlueState.Self.CurrentMainProject.FullFileName);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task PickingTheSlnx_OfAnFrb2Project_OpensTheGame()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2OpenSlnx_");
        var csprojPath = Frb2ProjectFixture.Write(temp.Root);
        var slnx = Path.Combine(temp.Root, ProjectName + ".slnx");

        await GoldProject.LoadInGlueAsync(ProjectFileResolver.ResolveCsproj(slnx));

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.Equal(new FilePath(csprojPath), GlueState.Self.CurrentMainProject.FullFileName);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task PickingTheSlnx_OfATemplateCreatedFrb2Project_OpensTheGame()
    {
        // `dotnet new frb2-desktop` names neither project after the solution - MyGame.slnx holds
        // MyGame.Common and MyGame.Desktop - so there is nothing to match on and the pick has to be
        // settled by which project is the FRB2 game.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2OpenTemplateSlnx_");
        var commonCsproj = Frb2ProjectFixture.WriteTemplateShaped(temp.Root);
        var slnx = Path.Combine(temp.Root, ProjectName + ".slnx");

        await GoldProject.LoadInGlueAsync(ProjectFileResolver.ResolveCsproj(slnx));

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.Equal(new FilePath(commonCsproj), GlueState.Self.CurrentMainProject.FullFileName);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }
}
