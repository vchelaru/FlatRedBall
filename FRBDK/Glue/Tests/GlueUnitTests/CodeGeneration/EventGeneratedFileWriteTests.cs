using System;
using System.IO;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Every project load rewrote each element's .Generated.Event.cs even when its contents were unchanged, which
// touched the file and made the game project recompile it. It is now written only when it differs.
public class EventGeneratedFileWriteTests : IDisposable
{
    static readonly DateTime LongAgo = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    readonly VisualStudioProject originalMainProject;
    readonly GlueProjectSave originalGlueProject;
    readonly string directory;
    readonly ScreenSave screen;
    readonly string eventFile;

    public EventGeneratedFileWriteTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalMainProject = GlueState.Self.CurrentMainProject;
        originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);
        screen = new ScreenSave { Name = "Screens\\EventScreen" };
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject.Screens.Add(screen);
        eventFile = Path.Combine(directory, EventManager.GetGeneratedEventFileNameForElement(screen));
        Directory.CreateDirectory(Path.GetDirectoryName(eventFile)!);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = originalMainProject;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void GenerateEventGeneratedFile_ShouldNotRewriteTheFile_WhenItsContentsAreUnchanged()
    {
        screen.Events.Add(new EventResponseSave { EventName = "Activity" });
        EventCodeGenerator.GenerateEventGeneratedFile(screen);
        var contents = File.ReadAllText(eventFile);
        File.SetLastWriteTimeUtc(eventFile, LongAgo);

        EventCodeGenerator.GenerateEventGeneratedFile(screen);

        File.GetLastWriteTimeUtc(eventFile).ShouldBe(LongAgo);
        File.ReadAllText(eventFile).ShouldBe(contents);
    }

    [Fact]
    public void GenerateEventGeneratedFile_ShouldNotRewriteAnEmptyFile_WhenTheElementHasNoEvents()
    {
        File.WriteAllText(eventFile, "");
        File.SetLastWriteTimeUtc(eventFile, LongAgo);

        EventCodeGenerator.GenerateEventGeneratedFile(screen);

        File.GetLastWriteTimeUtc(eventFile).ShouldBe(LongAgo);
    }

    [Fact]
    public void GenerateEventGeneratedFile_ShouldEmptyTheFile_WhenTheElementNoLongerHasEvents()
    {
        File.WriteAllText(eventFile, "// old events");

        EventCodeGenerator.GenerateEventGeneratedFile(screen);

        File.ReadAllText(eventFile).ShouldBe("");
    }
}
