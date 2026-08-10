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
}
