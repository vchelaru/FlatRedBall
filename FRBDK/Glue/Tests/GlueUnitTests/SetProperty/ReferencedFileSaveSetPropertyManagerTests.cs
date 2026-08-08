using System;
using System.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.SetProperty;

// GitHub issue #2016: toggling "Is Database For Localizing" in the ReferencedFileSave "Settings
// (Preview)" tab crashed Glue with a NullReferenceException. Root cause: that tab is backed by
// WpfDataUi's DataUiGrid, whose PropertyChange event never populates PropertyChangedArgs.OldValue
// (only NewValue) - see DataUiGrid.HandleInstanceMemberSetByUi in the sibling Gum repo. So
// MainPropertyGridPlugin.HandlePropertyChanged always forwards oldValue: null for any property changed
// through this grid, unlike the WinForms PropertyGrid path (SetPropertyManager.PropertyValueChanged),
// which gets a real old value from System.ComponentModel. This test reproduces that always-null
// oldValue directly against the real production method.
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
        var rfs = new ReferencedFileSave { Name = "GlobalContent/GumProject.gumx" };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);
        GlueState.Self.CurrentReferencedFileSave = rfs;

        // Mirrors what the real WPF "Settings (Preview)" grid does before it raises PropertyChange:
        // WpfDataUi's InstanceMember already wrote the new value through to the instance, and then
        // fires PropertyChange with OldValue left at its default (null) - see the class comment above.
        rfs.IsDatabaseForLocalizing = true;

        var sut = new ReferencedFileSaveSetPropertyManager();
        var updateTreeView = true;

        Should.NotThrow(() => sut.ReactToChangedReferencedFile(
            nameof(ReferencedFileSave.IsDatabaseForLocalizing), oldValue: null, ref updateTreeView));

        rfs.IsDatabaseForLocalizing.ShouldBeTrue();
    }
}
