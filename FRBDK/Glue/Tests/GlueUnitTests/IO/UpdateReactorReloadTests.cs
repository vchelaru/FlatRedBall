using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.IO;

// Drives UpdateReactor.UpdateFile end to end for an externally edited .glej: the file is really written
// to disk and really re-read, so this covers ReloadGlux's element swap, its re-selection, and its output.
public class UpdateReactorReloadTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly string _tempProjectDirectory;

    public UpdateReactorReloadTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        OutputRecordingPlugin.EnsureRegistered();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FileManager.RelativeDirectory;

        GlueState.Self.CurrentMainProject =
            TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        FileManager.RelativeDirectory = _tempProjectDirectory + "\\";
    }

    public void Dispose()
    {
        GlueState.Self.CurrentElement = null;
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FileManager.RelativeDirectory = _originalRelativeDirectory;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public async Task UpdateFile_ShouldReselectVariableAndNameTheFile_WhenSelectedEntityChangesOnDisk()
    {
        var project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        var entity = new EntitySave { Name = "Entities\\Boss" };
        entity.CustomVariables.Add(new CustomVariable { Name = "Health", Type = "float" });
        project.Entities.Add(entity);
        ObjectFinder.Self.GlueProject = project;

        var glujPath = GlueState.Self.GlueProjectFileName.FullPath;
        project.Save("GLUE", glujPath, out var saveException).ShouldBeTrue(saveException?.ToString());

        GlueState.Self.CurrentCustomVariable = entity.CustomVariables[0];

        // The external edit: another tool adds a variable to the entity's .glej.
        var editedProject = GlueProjectSaveExtensions.Load(glujPath);
        editedProject.Entities[0].CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        editedProject.Save("GLUE", glujPath, out saveException).ShouldBeTrue(saveException?.ToString());

        OutputRecordingPlugin.Output.Clear();
        var glejPath = new FilePath(Path.Combine(_tempProjectDirectory, "Entities", "Boss.glej"));
        File.Exists(glejPath.FullPath).ShouldBeTrue();

        await UpdateReactor.UpdateFile(glejPath);

        var reloadedEntity = ObjectFinder.Self.GetElement("Entities\\Boss");
        reloadedEntity.ShouldNotBeSameAs(entity);
        reloadedEntity.CustomVariables.Select(item => item.Name).ShouldBe(new[] { "Health", "Speed" });

        GlueState.Self.CurrentCustomVariable.ShouldBeSameAs(reloadedEntity.CustomVariables[0]);
        GlueState.Self.CurrentElement.ShouldBeSameAs(reloadedEntity);

        OutputRecordingPlugin.Output.ShouldContain(line => line.EndsWith("Reloading changed file Entities/Boss.glej"));
        OutputRecordingPlugin.Output.ShouldContain(line => line.EndsWith("Refreshed Entities\\Boss"));
        OutputRecordingPlugin.Output.ShouldNotContain(line => line.Contains("full project reload"));
    }

    private class OutputRecordingPlugin : PluginBase
    {
        static readonly object _lock = new();
        static bool _registered;

        public static List<string> Output { get; } = new();

        public static void EnsureRegistered()
        {
            lock (_lock)
            {
                if (!_registered)
                {
                    _registered = true;
                    PluginManager.RegisterPluginForTesting(new OutputRecordingPlugin());
                }
            }
        }

        public override string FriendlyName => "Update Reactor Output Recording Plugin (test)";
        public override Version Version => new(1, 0);

        public override void StartUp()
        {
            OnOutputHandler = output =>
            {
                lock (Output) { Output.Add(output); }
            };
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason) => true;
    }
}
