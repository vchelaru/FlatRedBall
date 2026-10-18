using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// An AOT game cannot load a .gumx (XmlSerializer), so its author converts the Gum project to .gumj and
/// swaps the Gum project file in Global Content. These drive that swap through the same calls the UI makes
/// (RemoveReferencedFileAsync, then AddExistingFileManager.AddSingleFile) on a real sample. See GitHub
/// issue #2390.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class GumJsonSwitchTests
{
    // StaFact, not Fact: plugin StartUp methods construct real WPF toolbars, which need an STA thread.
    [StaFact]
    public async Task SwitchingToGumj_PointsGeneratedCodeAtTheGumj_AndAddsTheJsonElementFilesToTheCsproj()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/Platformer/DialogBoxDemo");
        var csproj = Path.Combine(project.Root, "DialogBoxDemo", "DialogBoxDemo.csproj");

        // The .gumj needs a project at the JSON Gum version or later, so start from the latest.
        foreach (var gluj in Directory.GetFiles(project.Root, "*.gluj", SearchOption.AllDirectories))
        {
            File.WriteAllText(gluj, Regex.Replace(File.ReadAllText(gluj), "\"FileVersion\": *[0-9]+",
                "\"FileVersion\": " + GlueProjectSave.LatestVersion));
        }
        GoldProject.DeleteGeneratedCode(project.Root);

        var gumFolder = Path.Combine(project.Root, "DialogBoxDemo", "Content", "GumProject");
        ConvertToJson(Path.Combine(gumFolder, "GumProject.gumx"));

        await GoldProject.LoadInGlueAsync(csproj);
        await TaskManager.Self.WaitForAllTasksFinished();

        var gumxRfs = ObjectFinder.Self.GlueProject.GlobalFiles.Single(item => item.Name.EndsWith(".gumx"));
        await GlueCommands.Self.GluxCommands.RemoveReferencedFileAsync(gumxRfs, null, new System.Collections.Generic.List<string>());
        await TaskManager.Self.WaitForAllTasksFinished();

        await AddExistingFileManager.Self.AddSingleFile(new FilePath(Path.Combine(gumFolder, "GumProject.gumj")), elementToAddTo: null);
        await TaskManager.Self.WaitForAllTasksFinished();

        GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
        await TaskManager.Self.WaitForAllTasksFinished();
        GlueCommands.Self.ProjectCommands.SaveProjects();
        await TaskManager.Self.WaitForAllTasksFinished();

        // No hand edit: the generated load call follows the Gum project file in Global Content.
        var globalContent = File.ReadAllText(Path.Combine(project.Root, "DialogBoxDemo", "GlobalContent.Generated.cs"));
        globalContent.ShouldContain("StaticInitialize(\"Content/GumProject/GumProject.gumj\")", Case.Sensitive);

        // A .gumj project loads .gusj/.gucj/.gutj, so those are what has to reach the output folder.
        var included = XDocument.Load(csproj).Descendants()
            .Where(item => item.Name.LocalName == "None" || item.Name.LocalName == "Content")
            .Select(item => (string)item.Attribute("Include"))
            .Where(item => item != null && item.StartsWith("Content\\GumProject\\"))
            .ToList();

        included.ShouldContain("Content\\GumProject\\GumProject.gumj");
        included.ShouldContain("Content\\GumProject\\Screens\\GameScreenGum.gusj");
        included.ShouldContain("Content\\GumProject\\Components\\Controls\\ButtonStandard.gucj");
        included.Where(item => Regex.IsMatch(item, @"\.(gumx|gusx|gucx|gutx)$")).ShouldBeEmpty(
            "XML Gum files left over from before the swap");
    }

    static void ConvertToJson(string gumx)
    {
        // Not redirected, so this isn't a NestedDotnetCli-style pipe hang risk.
        using var convert = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "Plugins/GumPlugin/Tools/gumcli/gumcli.exe",
            Arguments = $@"convert-to-json ""{gumx}""",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        convert!.WaitForExit();
        convert.ExitCode.ShouldBe(0);
    }
}
