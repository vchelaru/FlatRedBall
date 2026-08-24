using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using GumPlugin.Managers;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

// Namespace is "GumPluginTests", not "GumPlugin" - see GumProjectCreationTests.cs for why
// (the real Gum plugin project's root namespace is the bare, non-nested "GumPlugin").
namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// GitHub issue #2195: renaming/deleting a Gum Screen/Component via Gum's own commands - reported through
/// <c>gum_events.json</c> (EventOutputPlugin, Gum-side) and reacted to by
/// <see cref="EventExportManager.HandleEventExportFileChanged"/> - already migrated/removed the paired
/// "GumRuntimes/{name}Runtime.cs" custom code, but never touched the paired "Forms/{Screens|Components}/
/// {name}Forms.cs"/".Generated.cs". Left orphaned, the old Forms.cs (hand-authored, so never
/// auto-deleted) keeps its `partial void CustomInitialize()` implementing declaration in the .csproj
/// forever; once the stale, gitignored Forms.Generated.cs that used to supply its defining declaration
/// stops being regenerated under the old name, the next build fails with CS0759. Reproduced live in a
/// real KidDefense project before this fix.
/// </summary>
[Collection(nameof(GlueUnitTests.Tasks.TaskManagerSequentialCollection))]
public class EventExportFormsCleanupTests : IDisposable
{
    private readonly VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public EventExportFormsCleanupTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    static void WriteFile(string fullPath, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
    }

    string WriteGumEventsJson(GumEventTypes eventType, string elementType, string oldName, string newName)
    {
        var exportedEvent = new ExportedEvent
        {
            NewName = newName,
            OldName = oldName,
            ElementType = elementType,
            EventType = eventType,
            TimestampUtc = DateTime.UtcNow,
        };
        var collection = new ExportedEventCollection
        {
            UserEvents = new() { ["testuser"] = new() { exportedEvent } }
        };

        var path = Path.Combine(_tempProjectDirectory, "gum_events.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(collection));
        return path;
    }

    [Fact]
    public async Task HandleEventExportFileChanged_OnElementDeleted_ShouldRemoveOldFormsFiles_LikeItAlreadyDoesForRuntimeFiles()
    {
        var customRuntime = Path.Combine(_tempProjectDirectory, "GumRuntimes", "OldScreenRuntime.cs");
        var generatedRuntime = Path.Combine(_tempProjectDirectory, "GumRuntimes", "OldScreenRuntime.Generated.cs");
        var customForms = Path.Combine(_tempProjectDirectory, "Forms", "Screens", "OldScreenForms.cs");
        var generatedForms = Path.Combine(_tempProjectDirectory, "Forms", "Screens", "OldScreenForms.Generated.cs");

        WriteFile(customRuntime, "// custom runtime code");
        WriteFile(generatedRuntime, "// generated runtime code");
        WriteFile(customForms, "partial class OldScreenForms { partial void CustomInitialize() { } }");
        WriteFile(generatedForms, "partial class OldScreenForms { partial void CustomInitialize(); }");

        var vsProject = GlueState.Self.CurrentMainProject;
        vsProject.AddCodeBuildItem(@"GumRuntimes\OldScreenRuntime.cs");
        vsProject.AddCodeBuildItem(@"GumRuntimes\OldScreenRuntime.Generated.cs");
        vsProject.AddCodeBuildItem(@"Forms\Screens\OldScreenForms.cs");
        vsProject.AddCodeBuildItem(@"Forms\Screens\OldScreenForms.Generated.cs");

        var eventsPath = WriteGumEventsJson(GumEventTypes.ElementDeleted, "Screens", oldName: "OldScreen", newName: null);

        await EventExportManager.Self.HandleEventExportFileChanged(eventsPath);

        vsProject.IsFilePartOfProject(@"GumRuntimes\OldScreenRuntime.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBeFalse("this already worked before the fix - pinning it stays green as a regression guard");
        vsProject.IsFilePartOfProject(@"Forms\Screens\OldScreenForms.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBeFalse("the Forms.Generated.cs entry for a deleted Gum element must be removed just like the Runtime one");
        vsProject.IsFilePartOfProject(@"Forms\Screens\OldScreenForms.cs", BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBeFalse("the hand-authored Forms.cs entry must be removed from the csproj so its now-orphaned CustomInitialize() can't CS0759");
    }

    [Fact]
    public async Task HandleEventExportFileChanged_OnElementRenamed_ShouldCopyFormsCsToTheNewName_LikeItAlreadyDoesForRuntimeCs()
    {
        var oldCustomRuntime = Path.Combine(_tempProjectDirectory, "GumRuntimes", "OldScreenRuntime.cs");
        var oldCustomForms = Path.Combine(_tempProjectDirectory, "Forms", "Screens", "OldScreenForms.cs");

        WriteFile(oldCustomRuntime, "namespace TestProject.GumRuntimes { public partial class OldScreenRuntime { } }");
        WriteFile(oldCustomForms,
            "namespace TestProject.FormsControls.Screens { public partial class OldScreenForms { partial void CustomInitialize() { MyCustomField = 1; } } }");

        var vsProject = GlueState.Self.CurrentMainProject;
        vsProject.AddCodeBuildItem(@"GumRuntimes\OldScreenRuntime.cs");
        vsProject.AddCodeBuildItem(@"Forms\Screens\OldScreenForms.cs");

        Gum.Managers.ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave
        {
            Screens = { new Gum.DataTypes.ScreenSave { Name = "NewScreen" } }
        };

        var eventsPath = WriteGumEventsJson(GumEventTypes.ElementRenamed, "Screens", oldName: "OldScreen", newName: "NewScreen");

        await EventExportManager.Self.HandleEventExportFileChanged(eventsPath);

        var newFormsPath = Path.Combine(_tempProjectDirectory, "Forms", "Screens", "NewScreenForms.cs");
        File.Exists(newFormsPath)
            .ShouldBeTrue("the hand-authored Forms.cs must be copied to the new name, same as GumRuntimes/OldScreenRuntime.cs already is");

        var newFormsContents = File.ReadAllText(newFormsPath);
        newFormsContents.Contains("class NewScreenForms").ShouldBeTrue();
        // the user's actual CustomInitialize body must survive the rename, not just an empty stub:
        newFormsContents.Contains("MyCustomField = 1").ShouldBeTrue();

        vsProject.IsFilePartOfProject(@"Forms\Screens\OldScreenForms.cs", BuildItemMembershipType.CompileOrContentPipeline)
            .ShouldBeFalse("the old Forms.cs entry must not be left behind alongside the new one");
    }
}
