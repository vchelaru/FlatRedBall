using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GlueUnitTests.TestSupport;
using Npc;
using Npc.ViewModels;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// End to end for #2371: a new project with live edit turned on (edit mode code embedded, plus the Game1,
/// entity and screen wiring Glue emits for it) has to build in both Debug and Release. The edit mode code is
/// compiled out of Release, so this is what proves nothing the generated code still references went with it.
/// The unit tests pin the generated text; only this compiles it.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class EditModeReleaseBuildTests
{
    /// <summary>
    /// When set, the generated project is copied here before the test cleans up, so it can be published
    /// with PublishAot to count trim/AOT warnings (a plain build never shows them: analyzers skip *.Generated.cs).
    /// </summary>
    public const string KeepProjectDirectoryVariable = "FRB_EDITMODE_KEEP_PROJECT_DIR";

    sealed class Notifier : IProjectCreationNotifier
    {
        public System.Collections.Generic.List<string> Messages { get; } = new();
        public void ShowMessage(string message) => Messages.Add(message);
    }

    [StaFact]
    public async Task ProjectWithLiveEdit_ShouldBuildInDebugAndRelease()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        // MainCompilerPlugin and MainGameCommunicationPlugin register these in the real editor; the plugins
        // themselves open sockets and build WPF tabs, so only the code generators are wired up here.
        var game1Generators = Game1CodeGeneratorManager.Generators.ToList();
        var elementGenerators = CodeWriter.CodeGenerators.ToList();
        var originalConnection = GameJsonCommunicationPlugin.Common.GameConnectionManager.Self;
        Game1CodeGeneratorManager.Generators.Add(new Game1GlueControlGenerator
        {
            IsGlueControlManagerGenerationEnabled = true,
            PortNumber = 8889,
        });
        Game1CodeGeneratorManager.Generators.Add(new Game1GlueCommunicationGenerator(true, 8889));
        CodeWriter.CodeGenerators.Add(new ApplyEditorCommandsCodeGenerator { IsGameCommunicationEnabled = true });
        GameJsonCommunicationPlugin.Common.GameConnectionManager.Self =
            new GameJsonCommunicationPlugin.Common.GameConnectionManager((_, __) => { }, 0);

        var originalNotifier = ProjectCreationHelper.Notifier;
        using var temp = new TempDir();
        try
        {
            var repoRoot = GoldProject.FindRepoRoot();
            var notifier = new Notifier();
            ProjectCreationHelper.Notifier = notifier;
            var viewModel = new NewProjectViewModel
            {
                ProjectName = "EditModeGame",
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

            var csproj = Directory.GetFiles(viewModel.FinalDirectory, "EditModeGame.csproj", SearchOption.AllDirectories).Single();
            PointAtEngineSource(csproj, repoRoot);

            GoldProject.DeleteGeneratedCode(temp.Root);
            await GoldProject.LoadInGlueAsync(csproj);
            var project = ObjectFinder.Self.GlueProject;

            var menu = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("Screens\\MenuScreen");
            var player = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity("Player", is2D: true);
            project.StartUpScreen = menu.Name;

            await TaskManager.Self.WaitForAllTasksFinished();
            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            await TaskManager.Self.WaitForAllTasksFinished();

            // After codegen: EmbedAll adds the GlueControl files to the in-memory project and saves it.
            GoldProject.EmbedLiveEditCode();
            await TaskManager.Self.WaitForAllTasksFinished();

            GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
            ErrorRecordingPlugin.Errors.ShouldBeEmpty();

            var projectDirectory = Path.GetDirectoryName(csproj)!;
            File.Exists(Path.Combine(projectDirectory, "GlueControl", "Editing", "VariableAssignmentLogic.Generated.cs")).ShouldBeTrue();
            File.ReadAllText(Path.Combine(projectDirectory, "Game1.Generated.cs")).ShouldContain("GlueControlManager");
            File.ReadAllText(Path.Combine(projectDirectory, "Entities", "Player.Generated.cs")).ShouldContain("ApplyEditorCommandsToNewEntity");

            var keepDirectory = Environment.GetEnvironmentVariable(KeepProjectDirectoryVariable);
            if (!string.IsNullOrEmpty(keepDirectory))
            {
                CopyDirectory(temp.Root, keepDirectory);
            }

            foreach (var configuration in new[] { "Debug", "Release" })
            {
                var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c {configuration}");
                exitCode.ShouldBe(0, $"dotnet build -c {configuration} failed for the live edit project:\n{output}");
            }
        }
        finally
        {
            ProjectCreationHelper.Notifier = originalNotifier;
            Game1CodeGeneratorManager.Generators.Clear();
            Game1CodeGeneratorManager.Generators.AddRange(game1Generators);
            CodeWriter.CodeGenerators.Clear();
            CodeWriter.CodeGenerators.AddRange(elementGenerators);
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self?.Dispose();
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self = originalConnection;
        }
    }

    // The template references released NuGet packages; the live edit code needs the engine in this repo.
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
            new XElement(ns + "PackageReference", new XAttribute("Include", "Newtonsoft.Json"), new XAttribute("Version", "13.0.3")),
            new XElement(ns + "ProjectReference", new XAttribute("Include",
                Path.GetFullPath(Path.Combine(repoRoot, "..", "Gum", "GumCore", "GumCoreXnaPc", "GumCore.DesktopGlNet6", "GumCore.DesktopGlNet6.csproj")))),
            new XElement(ns + "ProjectReference", new XAttribute("Include",
                Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms", "FlatRedBall.Forms.DesktopGlNet6", "FlatRedBall.Forms.DesktopGlNet6.csproj"))),
            new XElement(ns + "ProjectReference", new XAttribute("Include",
                Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms", "StateInterpolation", "StateInterpolation.DesktopGlNet6", "StateInterpolation.DesktopNet6.csproj")))));
        doc.Save(csproj);
    }

    static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination));
        }
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination), overwrite: true);
        }
    }

    sealed class TempDir : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "FrbEditMode_" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Root);

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
