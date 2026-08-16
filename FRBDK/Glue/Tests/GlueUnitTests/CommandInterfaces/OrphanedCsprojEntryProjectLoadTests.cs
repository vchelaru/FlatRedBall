using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// GitHub issue #2103: end-to-end pin that a real <c>ProjectLoader.LoadProject</c> - the same call
/// Glue.exe makes on File > Load Project - removes an orphaned generated-file csproj entry (no backing
/// file, no owning element) on its own, reproducing the reported symptom (CS2001 for a deleted Forms
/// screen's ".Generated.cs") and then proving it's gone. <see cref="OrphanedGeneratedCompileItemReconciliationTests"/>
/// and <see cref="GlueUnitTests.VSHelpers.OrphanedGeneratedCompileItemFinderTests"/> cover the decision
/// matrix; this covers the wiring - that <c>ProjectLoader</c> actually calls it, in the right order
/// relative to codegen.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class OrphanedCsprojEntryProjectLoadTests
{
    [StaFact]
    public async Task LoadProject_ShouldRemoveOrphanedGeneratedCsprojEntry_AndLoadCleanly()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        var csprojPath = Path.Combine(project.Root, "FormsSampleProject", "FormsSampleProject.csproj");

        const string orphanedInclude = @"Screens\Issue2102DoesNotExist.Generated.cs";
        AddStrayCompileItem(csprojPath, orphanedInclude);

        await GoldProject.LoadInGlueAsync(csprojPath);

        var mainProject = GlueState.Self.CurrentMainProject;
        mainProject.IsFilePartOfProject(orphanedInclude, BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBeFalse("the orphaned entry should have been reconciled away on load");

        ErrorRecordingPlugin.Errors
            .Any(e => e.Contains("Issue2102DoesNotExist"))
            .ShouldBeFalse("no error should reference the file the orphaned entry pointed at");
    }

    static void AddStrayCompileItem(string csprojPath, string include)
    {
        var document = XDocument.Load(csprojPath);
        var ns = document.Root!.Name.Namespace;

        var itemGroup = new XElement(ns + "ItemGroup",
            new XElement(ns + "Compile", new XAttribute("Include", include)));

        document.Root.Add(itemGroup);
        document.Save(csprojPath);
    }
}
