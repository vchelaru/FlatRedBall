using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.VSHelpers;

// VisualStudioProject.ResolveDuplicateProjectEntry used to construct/show a WPF window
// (MultiButtonMessageBoxWpf) directly. That's fine on Glue's UI thread, but a project reload triggered by
// an external file change is queued via TaskManager.Self.Add(..., doOnUiThread: false) (see
// UpdateReactor.TryHandleSpecificProjectFileChange), so this method can run on TaskManager's background
// thread - crashing WPF with "Cannot access Freezable ... across threads because it cannot be frozen."
// The dialog is now shown through DuplicateProjectEntryDialog.Show, a swappable seam (same shape as
// TaskManager.UiThreadMarshaller) whose default implementation wraps the WPF construction in
// TaskManager.Self.OnUiThread. These tests fake that seam so ResolveDuplicateProjectEntry's branch logic -
// previously untestable, since it was entangled with a real WPF window - is pinned without popping a
// dialog.
public class VisualStudioProjectDuplicateEntryTests : IDisposable
{
    private readonly Func<string, DuplicateProjectEntryResolution> _originalShow = DuplicateProjectEntryDialog.Show;

    public void Dispose()
    {
        DuplicateProjectEntryDialog.Show = _originalShow;
    }

    [Fact]
    public void ResolveDuplicateProjectEntry_ShouldRemoveTheDuplicateItem_WhenResolutionIsRemoveDuplicate()
    {
        string? directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);
            project.Project.AddItem("Compile", "Foo.cs");
            var duplicate = project.Project.AddItem("Compile", "Foo.cs").First();

            DuplicateProjectEntryDialog.Show = _ => DuplicateProjectEntryResolution.RemoveDuplicate;

            project.ResolveDuplicateProjectEntry(duplicate);

            project.Project.GetItems("Compile").ShouldNotContain(duplicate);
        }
        finally
        {
            if (directory != null) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ResolveDuplicateProjectEntry_ShouldThrow_WhenResolutionIsCancelLoad()
    {
        string? directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);
            project.Project.AddItem("Compile", "Foo.cs");
            var duplicate = project.Project.AddItem("Compile", "Foo.cs").First();

            DuplicateProjectEntryDialog.Show = _ => DuplicateProjectEntryResolution.CancelLoad;

            Should.Throw<Exception>(() => project.ResolveDuplicateProjectEntry(duplicate))
                .Message.ShouldContain("Foo.cs");
        }
        finally
        {
            if (directory != null) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ResolveDuplicateProjectEntry_ShouldPassTheDuplicateItemsInclude_ToTheDialog()
    {
        string? directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);
            project.Project.AddItem("Compile", "Foo.cs");
            var duplicate = project.Project.AddItem("Compile", "Foo.cs").First();

            string? passedInclude = null;
            DuplicateProjectEntryDialog.Show = include =>
            {
                passedInclude = include;
                return DuplicateProjectEntryResolution.RemoveDuplicate;
            };

            project.ResolveDuplicateProjectEntry(duplicate);

            passedInclude.ShouldBe("Foo.cs");
        }
        finally
        {
            if (directory != null) Directory.Delete(directory, recursive: true);
        }
    }
}
