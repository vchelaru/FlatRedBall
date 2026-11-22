using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Generating all code builds each element's code on several threads at once (#2422), inside the one
// "Generating all code" task. Files and project entries are still written one element at a time, in order.
public class GenerateAllElementCodeTests : IDisposable
{
    readonly VisualStudioProject originalMainProject;
    readonly GlueProjectSave originalGlueProject;
    readonly string originalRelativeDirectory;
    readonly string directory;
    readonly GlueProjectSave project;

    public GenerateAllElementCodeTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalMainProject = GlueState.Self.CurrentMainProject;
        originalGlueProject = ObjectFinder.Self.GlueProject;
        originalRelativeDirectory = FileManager.RelativeDirectory;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);
        FileManager.RelativeDirectory = directory;
        project = new GlueProjectSave { FileVersion = (int)GlueProjectSave.GluxVersions.ScreensHaveActivityEditMode };
        ObjectFinder.Self.GlueProject = project;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = originalMainProject;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        FileManager.RelativeDirectory = originalRelativeDirectory;
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    // Waits in GenerateInitialize until every element it is told about is there too, so it only gets through
    // when those elements are being built at the same time.
    sealed class MeetingGenerator : ElementComponentCodeGenerator
    {
        readonly Barrier barrier;
        public readonly ConcurrentBag<bool> Met = new();
        public MeetingGenerator(int elements) => barrier = new Barrier(elements);

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            Met.Add(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
            return codeBlock;
        }
    }

    void GenerateAllWhileMeeting(MeetingGenerator generator)
    {
        CodeWriter.CodeGenerators.Add(generator);
        try
        {
            GenerateCodeCommands.GenerateAllElementCode(project).ShouldBeFalse();
        }
        finally
        {
            CodeWriter.CodeGenerators.Remove(generator);
        }
    }

    [Fact]
    public void GenerateAllElementCode_ShouldBuildElementsAtTheSameTime()
    {
        project.Screens.Add(new ScreenSave { Name = "Screens\\First" });
        project.Screens.Add(new ScreenSave { Name = "Screens\\Second" });
        var generator = new MeetingGenerator(2);

        GenerateAllWhileMeeting(generator);

        generator.Met.ShouldBe(new[] { true, true });
    }

    // The performance timing lines are written through a code block remembered between calls. Shared between
    // threads, one element's lines landed in the other element's file.
    [Fact]
    public void GenerateAllElementCode_ShouldKeepEachElementsTimingLinesInItsOwnFile()
    {
        project.PerformanceSettingsSave.RecordInitializeSegments = true;
        project.Screens.Add(new ScreenSave { Name = "Screens\\First" });
        project.Screens.Add(new ScreenSave { Name = "Screens\\Second" });
        var generator = new MeetingGenerator(2);

        GenerateAllWhileMeeting(generator);

        // Premise: both were mid-Initialize at once, which is when the shared block was swapped.
        generator.Met.ShouldBe(new[] { true, true });
        var first = File.ReadAllText(Path.Combine(directory, "Screens", "First.Generated.cs"));
        var second = File.ReadAllText(Path.Combine(directory, "Screens", "Second.Generated.cs"));
        first.ShouldContain("GetAndStartContextAndTime(\"FirstPost Initialize\")");
        first.ShouldNotContain("\"Second");
        second.ShouldContain("GetAndStartContextAndTime(\"SecondPost Initialize\")");
        second.ShouldNotContain("\"First");
    }

    [Fact]
    public void GenerateAllElementCode_ShouldWriteEachElementsCode_AndAddItsFilesToTheProject()
    {
        project.Screens.Add(new ScreenSave { Name = "Screens\\Menu" });
        project.Entities.Add(new EntitySave { Name = "Entities\\Bullet", CreatedByOtherEntities = true });
        project.Entities.Add(new EntitySave { Name = "Entities\\Ship" });

        GenerateCodeCommands.GenerateAllElementCode(project).ShouldBeFalse();

        File.ReadAllText(Path.Combine(directory, "Screens", "Menu.Generated.cs")).ShouldContain("public partial class Menu");
        File.ReadAllText(Path.Combine(directory, "Entities", "Bullet.Generated.cs")).ShouldContain("public partial class Bullet");
        File.ReadAllText(Path.Combine(directory, "Entities", "Ship.Generated.cs")).ShouldContain("public partial class Ship");
        GlueState.Self.CurrentMainProject.EvaluatedItems
            .Where(item => item.ItemType == "Compile")
            .Select(item => item.UnevaluatedInclude.Replace('/', '\\'))
            .ShouldBe(new[]
            {
                "Entities\\Bullet.Generated.cs",
                "Entities\\Ship.Generated.cs",
                "Factories\\BulletFactory.Generated.cs",
                "Screens\\Menu.Generated.cs",
            }, ignoreOrder: true);
    }

    [Fact]
    public void RunInParallel_ShouldCountAsInTheTask_OnEveryThread()
    {
        var barrier = new Barrier(2);
        var results = new ConcurrentBag<(bool met, int thread, bool inTask, string relativeDirectory)>();

        TaskManager.Self.RunInParallel(new[] { 0, 1 }, _ =>
        {
            var met = barrier.SignalAndWait(TimeSpan.FromSeconds(5));
            results.Add((met, Environment.CurrentManagedThreadId, TaskManager.Self.IsInTask(), FileManager.RelativeDirectory));
        });

        // Premise: the two calls ran on two threads at once, so one is not the caller's.
        results.Select(r => r.met).ShouldBe(new[] { true, true });
        results.Select(r => r.thread).Distinct().Count().ShouldBe(2);
        results.ShouldAllBe(r => r.inTask);
        results.ShouldAllBe(r => r.relativeDirectory == FileManager.RelativeDirectory);
    }
}
