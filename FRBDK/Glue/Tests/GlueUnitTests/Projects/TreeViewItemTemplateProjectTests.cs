using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// End to end for the TreeViewItem template registration (#2370 follow-up): the checked-in FormsSampleProject
/// (it has a TreeViewItem component) is loaded through the real editor pipeline, GumIdb.Generated.cs is
/// regenerated at a chosen project version, and the result is built against engine source. The unit tests in
/// FormsAssociationCodegenTests pin the generated text; only this proves the template line compiles next to the
/// runtime it names and that the older registration still builds.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class TreeViewItemTemplateProjectTests
{
    [StaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TreeViewItem_ShouldRegisterThroughTemplatesOnlyAtTheTemplateVersion_AndBuild(bool atTemplateVersion)
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        var csproj = Path.Combine(project.Root, "FormsSampleProject", "FormsSampleProject.csproj");

        GoldProject.DeleteGeneratedCode(project.Root);
        await GoldProject.LoadInGlueAsync(csproj);

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GumTreeViewItemHasDefaultFormsTemplate - (atTemplateVersion ? 0 : 1);
        GumPlugin.Managers.CodeGeneratorManager.Self.GenerateAndSaveRuntimeAssociations();

        var gumIdb = File.ReadAllText(Path.Combine(project.Root, "FormsSampleProject", "GumRuntimes", "GumIdb.Generated.cs"));
        // Every other control registers through templates at both versions; only TreeViewItem differs.
        gumIdb.ShouldContain("DefaultFormsTemplates[typeof(FlatRedBall.Forms.Controls.Button)]");
        if (atTemplateVersion)
        {
            gumIdb.ShouldContain("DefaultFormsTemplates[typeof(FlatRedBall.Forms.Controls.TreeViewItem)]");
            gumIdb.ShouldNotContain("DefaultFormsComponents");
        }
        else
        {
            gumIdb.ShouldContain("DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.TreeViewItem)]");
            gumIdb.ShouldNotContain("DefaultFormsTemplates[typeof(FlatRedBall.Forms.Controls.TreeViewItem)]");
        }

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed for the regenerated gold project:\n{output}");
        output.Split('\n')
            .Where(line => line.Contains("GumIdb.Generated.cs") && line.Contains("CS0618"))
            .ShouldBeEmpty("GumIdb.Generated.cs still uses an obsolete member.");
    }
}
