using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GumPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

public class GumViewModelSetFromTests
{
    public GumViewModelSetFromTests() => GlueTestBootstrap.EnsureInitialized();

    [StaFact]
    public void SetFrom_ToleratesNoLoadedGumProject()
    {
        // MainGumPlugin.HandleItemSelected passes AppState.Self.GumProjectSave straight through when a
        // .gumx is selected, and that is null until the Gum project has actually loaded. The resulting
        // NullReferenceException does not just fail the selection: PluginManager catches it and calls
        // PluginContainer.Fail, which disables the Gum plugin for the rest of the session.
        var viewModel = new GumViewModel();
        var rfs = new ReferencedFileSave { Name = "GlobalContent/GumProject.gumx" };

        Should.NotThrow(() => viewModel.SetFrom(null, rfs));
    }

    [StaFact]
    public void SetFrom_WhileUpdatingFromGlueObject_StaysUpdatingForEveryPropertyItRaises()
    {
        // MainGumPlugin saves the whole Glue project for any GumViewModel property change raised while
        // IsUpdatingFromGlueObject is false. RefreshGumViewModel sets the flag around SetFrom, but
        // UpdateFromGlueObject cleared it partway through, so GumProjectInfo saved the project on every load.
        var viewModel = new GumViewModel();
        var rfs = new ReferencedFileSave { Name = "GlobalContent/GumProject.gumx" };
        var raisedWhileNotUpdating = new System.Collections.Generic.List<string>();
        viewModel.PropertyChanged += (_, e) =>
        {
            if (!viewModel.IsUpdatingFromGlueObject) raisedWhileNotUpdating.Add(e.PropertyName);
        };

        viewModel.IsUpdatingFromGlueObject = true;
        viewModel.SetFrom(new Gum.DataTypes.GumProjectSave(), rfs);

        raisedWhileNotUpdating.ShouldBeEmpty();
        viewModel.IsUpdatingFromGlueObject.ShouldBeTrue();
    }

    [StaFact]
    public void UpdateFromGlueObject_WhenNotAlreadyUpdating_EndsNotUpdating()
    {
        // Callers such as MainSongControlViewModel set properties right after the base call and expect them to persist.
        var viewModel = new GumViewModel();
        viewModel.GlueObject = new ReferencedFileSave { Name = "GlobalContent/GumProject.gumx" };

        viewModel.UpdateFromGlueObject();

        viewModel.IsUpdatingFromGlueObject.ShouldBeFalse();
    }
}
