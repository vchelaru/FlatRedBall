using System;
using System.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.SetProperty;

// GitHub issue #2016: toggling "Is Database For Localizing" in the ReferencedFileSave "Settings
// (Preview)" tab crashed Glue with a NullReferenceException, because the handler unboxed oldValue as
// bool unconditionally. ReactToChangedReferencedFile is internal and has several callers that pass no
// old value, so a null oldValue is a legitimate input, not a bug in one caller - this test pins the
// null-safe handling against the real production method.
public class ReferencedFileSaveSetPropertyManagerTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly Action<string> _originalShowMessageImpl;
    private readonly string _tempProjectDirectory;

    public ReferencedFileSaveSetPropertyManagerTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalShowMessageImpl = DialogService.ShowMessageImpl;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";
        // Avoid a real MessageBox popping on the developer's desktop if UsesTranslation flips as a
        // side effect of the toggle below (see DialogService.DefaultShowMessage).
        DialogService.ShowMessageImpl = _ => { };
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        GlueState.Self.CurrentReferencedFileSave = null;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        DialogService.ShowMessageImpl = _originalShowMessageImpl;

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
    public void ReactToChangedReferencedFile_ShouldNotThrow_WhenIsDatabaseForLocalizingChangesWithNullOldValue()
    {
        // A .csv, because that is what IsDatabaseForLocalizing applies to - the path under test runs
        // RemoveCodeForCsv/CsvCodeGenerator and never looks at the extension. It also has to not be a
        // .gumx: assigning CurrentReferencedFileSave round-trips through Find.TreeNodeByTag and raises
        // ReactToItemsSelected for real, and MainGumPlugin answers a .gumx selection by building its WPF
        // GumControl - which throws in a test host and leaves PluginManager disabling the Gum plugin for
        // every test that runs after this one.
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Localization.csv" };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);
        GlueState.Self.CurrentReferencedFileSave = rfs;

        // Matches the contract every caller honors: the new value is already written through to the
        // ReferencedFileSave before ReactToChangedReferencedFile is notified about the change.
        rfs.IsDatabaseForLocalizing = true;

        var sut = new ReferencedFileSaveSetPropertyManager();
        var updateTreeView = true;

        Should.NotThrow(() => sut.ReactToChangedReferencedFile(
            nameof(ReferencedFileSave.IsDatabaseForLocalizing), oldValue: null, ref updateTreeView));

        rfs.IsDatabaseForLocalizing.ShouldBeTrue();
    }
}
