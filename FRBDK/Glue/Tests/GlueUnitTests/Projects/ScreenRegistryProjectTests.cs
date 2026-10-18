using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GlueUnitTests.TestSupport;
using Npc;
using Npc.ViewModels;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// End to end for the screen registry (#2366/#2372): a new project made from the DesktopGL template, pointed at
/// engine source, gets screens (one abstract, one in a subfolder) added through Glue's own commands, is
/// regenerated, and is built. The unit tests pin the generated text; only this proves the generated code
/// compiles against the real ScreenManager.RegisterScreenFactory and that an older project version
/// emits no registry.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ScreenRegistryProjectTests
{
    sealed class Notifier : IProjectCreationNotifier
    {
        public System.Collections.Generic.List<string> Messages { get; } = new();
        public void ShowMessage(string message) => Messages.Add(message);
    }

    [StaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProjectWithScreens_ShouldRegisterScreensOnlyAtTheRegistryVersion_AndBuild(bool atRegistryVersion)
    {
        var fileVersion = (int)GlueProjectSave.GluxVersions.ScreenManagerHasRegisterScreenFactory - (atRegistryVersion ? 0 : 1);
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        // MainCompilerPlugin registers this in the real editor; it emits camera setup and the start screen.
        var generators = Game1CodeGeneratorManager.Generators.ToList();
        Game1CodeGeneratorManager.Generators.Add(new Game1GlueControlGenerator());
        var originalNotifier = ProjectCreationHelper.Notifier;
        using var temp = new TempDir();
        try
        {
            var repoRoot = GoldProject.FindRepoRoot();
            var notifier = new Notifier();
            ProjectCreationHelper.Notifier = notifier;
            var viewModel = new NewProjectViewModel
            {
                ProjectName = "RegistryGame",
                ProjectDestinationLocation = temp.Root,
                IsCreateProjectDirectoryChecked = true,
                UseLocalCopy = true,
                OpenSlnFolderAfterCreation = false,
                IsOpenNewProjectWizardChecked = false,
                SelectedProject = new PlatformProjectInfo
                {
                    FriendlyName = "FlatRedBallDesktopGlMonoGameTemplate",
                    Namespace = "FlatRedBallDesktopGlMonoGameTemplate",
                    LocalSourceFile = new FilePath(Path.Combine(repoRoot, "Templates", "FlatRedBallDesktopGlMonoGameTemplate")),
                    SupportedInGlue = true,
                },
            };
            (await ProjectCreationHelper.MakeNewProject(viewModel)).ShouldBeTrue();
            notifier.Messages.ShouldBeEmpty();

            var csproj = Directory.GetFiles(viewModel.FinalDirectory, "RegistryGame.csproj", SearchOption.AllDirectories).Single();
            PointAtEngineSource(csproj, repoRoot);

            GoldProject.DeleteGeneratedCode(temp.Root);
            await GoldProject.LoadInGlueAsync(csproj);
            var project = ObjectFinder.Self.GlueProject;
            project.FileVersion = fileVersion;

            var screens = GlueCommands.Self.GluxCommands.ScreenCommands;
            var menu = await screens.AddScreen("Screens\\MenuScreen");
            var levelBase = await screens.AddScreen("Screens\\LevelBase");
            var level1 = await screens.AddScreen("Screens\\Level1");
            var level2 = await screens.AddScreen("Screens\\Levels\\Level2");

            // A SetByDerived object is what makes a Glue screen abstract.
            var marker = new NamedObjectSave();
            marker.SetDefaults();
            marker.InstanceName = "Marker";
            marker.SourceType = SourceType.FlatRedBallType;
            marker.SourceClassType = "FlatRedBall.Math.Geometry.Circle";
            marker.SetByDerived = true;
            await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
                marker, levelBase, listToAddTo: null, selectNewNos: false, performSaveAndGenerateCode: true, updateUi: false);
            foreach (var derived in new[] { level1, level2 })
            {
                derived.BaseScreen = levelBase.Name;
                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(derived);
            }
            project.StartUpScreen = menu.Name;

            await TaskManager.Self.WaitForAllTasksFinished();
            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            await TaskManager.Self.WaitForAllTasksFinished();
            GlueCommands.Self.ProjectCommands.SaveProjects();
            await TaskManager.Self.WaitForAllTasksFinished();

            GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
            ErrorRecordingPlugin.Errors.ShouldBeEmpty();

            var game1 = File.ReadAllText(Path.Combine(Path.GetDirectoryName(csproj)!, "Game1.Generated.cs"));
            if (atRegistryVersion)
            {
                game1.ShouldContain("RegisterScreenFactory(\"RegistryGame.Screens.MenuScreen\"");
                game1.ShouldContain("RegisterScreenFactory(\"RegistryGame.Screens.Level1\"");
                game1.ShouldContain("RegisterScreenFactory(\"RegistryGame.Screens.Levels.Level2\"");
                game1.ShouldNotContain("LevelBase\", () =>");
                game1.ShouldContain("var type = GetScreenTypeByName(item);");
            }
            else
            {
                game1.ShouldNotContain("RegisterScreenFactory");
                game1.ShouldNotContain("GetScreenTypeByName");
                game1.ShouldContain("thisAssembly.GetType(item)");
            }

            var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
            exitCode.ShouldBe(0, $"dotnet build failed for the regenerated project:\n{output}");
        }
        finally
        {
            ProjectCreationHelper.Notifier = originalNotifier;
            Game1CodeGeneratorManager.Generators.Clear();
            Game1CodeGeneratorManager.Generators.AddRange(generators);
        }
    }

    // The template references released NuGet packages; the registry needs the engine in this repo.
    static void PointAtEngineSource(string csproj, string repoRoot)
    {
        var doc = XDocument.Load(csproj, LoadOptions.PreserveWhitespace);
        var ns = doc.Root!.Name.Namespace;
        foreach (var reference in doc.Descendants(ns + "PackageReference").ToList())
        {
            reference.Remove();
        }
        doc.Descendants(ns + "TargetFramework").Single().Value = "net8.0";
        doc.Root.Add(new XElement(ns + "ItemGroup",
            new XElement(ns + "PackageReference", new XAttribute("Include", "MonoGame.Framework.DesktopGL"), new XAttribute("Version", "3.8.4.1")),
            new XElement(ns + "ProjectReference", new XAttribute("Include",
                Path.Combine(repoRoot, "Engines", "FlatRedBallXNA", "FlatRedBallDesktopGLNet6", "FlatRedBallDesktopGLNet6.csproj"))),
            new XElement(ns + "ProjectReference", new XAttribute("Include",
                Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms", "StateInterpolation", "StateInterpolation.DesktopGlNet6", "StateInterpolation.DesktopNet6.csproj")))));
        doc.Save(csproj);
    }

    sealed class TempDir : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "FrbRegistry_" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Root);

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
