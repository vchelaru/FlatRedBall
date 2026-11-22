using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.Projects;

// A project loaded from a TempDir used to stay loaded after the test, and in-memory codegen tests that ran
// later generated different text (Gum layer lines from the Gum plugin's still-loaded project) (#2429).
[Collection("Frb2ProjectLoad")]
public class TempDirProjectUnloadTests
{
    [StaFact]
    public async Task Dispose_UnloadsAProjectLoadedFromTheDirectory()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var temp = new TempDir("TempDirUnload_");
        await GoldProject.LoadInGlueAsync(Frb2ProjectFixture.Write(temp.Root));
        // Premise: something is loaded, or the assertions below pass vacuously.
        FlatRedBall.Glue.ProjectManager.GlueProjectSave.ShouldNotBeNull();

        temp.Dispose();

        FlatRedBall.Glue.ProjectManager.GlueProjectSave.ShouldBeNull();
        GlueState.Self.CurrentMainProject.ShouldBeNull();
    }
}
