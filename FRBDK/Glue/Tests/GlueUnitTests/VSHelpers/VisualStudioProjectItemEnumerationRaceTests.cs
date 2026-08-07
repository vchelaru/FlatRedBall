using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.VSHelpers;

// Pins https://github.com/vchelaru/FlatRedBall/issues/2014: rapid clicks on Glue's "Enable live edit"
// checkbox re-enter MainCompilerPlugin's handler before the previous click's TaskManager-queued codegen
// finishes. That codegen adds project items and reevaluates the MSBuild Project on TaskManager's
// background thread while the re-entrant call is mid-`foreach` over
// VisualStudioProject.IsFrbSourceLinked/HasProjectReference on the UI thread - "Collection was modified;
// enumeration operation may not execute." VisualStudioProject.EvaluatedItems already solves this exact
// hazard by cloning the list before returning it; these two methods just didn't use it.
public class VisualStudioProjectItemEnumerationRaceTests : IDisposable
{
    private readonly string _directory;
    private readonly ClassLibraryProject _project;

    public VisualStudioProjectItemEnumerationRaceTests()
    {
        _project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _directory);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void IsFrbSourceLinked_ShouldReturnFalse_WhenProjectHasNoFrbProjectReference()
    {
        _project.IsFrbSourceLinked().ShouldBeFalse();
    }

    [Fact]
    public void IsFrbSourceLinked_ShouldReturnTrue_WhenProjectReferencesFlatRedBallDesktopGLNet6()
    {
        _project.Project.AddItem("ProjectReference", @"..\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj");
        _project.Project.MarkDirty();
        _project.Project.ReevaluateIfNecessary();

        _project.IsFrbSourceLinked().ShouldBeTrue();
    }

    [Fact]
    public void IsFrbSourceLinked_ShouldNotThrow_WhenProjectIsReevaluatedConcurrently()
    {
        RunConcurrentEnumerationRace(() => _project.IsFrbSourceLinked());
    }

    [Fact]
    public void HasProjectReference_ShouldReturnFalse_WhenNoMatchingReferenceExists()
    {
        _project.HasProjectReference("SomeOther.csproj").ShouldBeFalse();
    }

    [Fact]
    public void HasProjectReference_ShouldReturnTrue_WhenMatchingReferenceExists()
    {
        _project.Project.AddItem("ProjectReference", @"..\SomeOther\SomeOther.csproj");
        _project.Project.MarkDirty();
        _project.Project.ReevaluateIfNecessary();

        _project.HasProjectReference("SomeOther.csproj").ShouldBeTrue();
    }

    [Fact]
    public void HasProjectReference_ShouldNotThrow_WhenProjectIsReevaluatedConcurrently()
    {
        RunConcurrentEnumerationRace(() => _project.HasProjectReference("SomeOther.csproj"));
    }

    // Mirrors the real crash's two sides: a writer thread that keeps adding items via
    // VisualStudioProject's own (lock-protected) mutator - the same shape as TaskManager's codegen
    // thread calling AddCodeBuildItem/AddNugetPackage/RemoveItem - racing a reader thread that keeps
    // calling the given method (the UI thread's re-entrant handler). A large seeded item count widens
    // the window each reevaluation/enumeration takes, which is what makes the race land reliably
    // instead of rarely.
    private void RunConcurrentEnumerationRace(Action callUnderTest)
    {
        for (var i = 0; i < 800; i++)
        {
            _project.Project.AddItem("Compile", $"seed{i}.cs");
        }
        _project.Project.MarkDirty();
        _project.Project.ReevaluateIfNecessary();

        using var stop = new CancellationTokenSource();
        Exception readerException = null;

        var writer = Task.Run(() =>
        {
            var i = 0;
            while (!stop.IsCancellationRequested)
            {
                _project.AddNugetPackage($"Package{i++}", "1.0.0");
            }
        });

        var reader = Task.Run(() =>
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    callUnderTest();
                }
            }
            catch (Exception ex)
            {
                readerException = ex;
            }
        });

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(4);
        while (DateTime.UtcNow < deadline && readerException == null)
        {
            Thread.Sleep(50);
        }
        stop.Cancel();
        Task.WaitAll(writer, reader);

        if (readerException != null)
        {
            throw readerException;
        }
    }
}
