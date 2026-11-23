using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// The .gumx read that overlaps Glue's startup. The load must take it only when it read the same .gumx and no Gum
/// file changed since; otherwise it reads the files itself.
/// </summary>
public class GumProjectPrefetchTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "glue-gumprefetch-" + Guid.NewGuid());
    readonly GumProjectSave _originalGumProjectSave;
    readonly string _originalRelativeDirectory;
    readonly FlatRedBall.Glue.SaveClasses.GlueProjectSave _originalGlueProject;

    string Csproj => Path.Combine(_root, "Game.csproj");
    string GumDirectory => Path.Combine(_root, "Content", "GumProject");
    string Gumx => Path.Combine(GumDirectory, "GumProject.gumx");
    string ButtonFile => Path.Combine(GumDirectory, "Components", "Button.gucx");

    public GumProjectPrefetchTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        _originalRelativeDirectory = FileManager.RelativeDirectory;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave
        {
            FileVersion = FlatRedBall.Glue.SaveClasses.GlueProjectSave.LatestVersion
        };
        Directory.CreateDirectory(GumDirectory);
        File.WriteAllText(Csproj, "<Project />");
        SaveGumProject();
    }

    public void Dispose()
    {
        FileReferenceTracker.Self.Prefetch = null;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        Directory.Delete(_root, recursive: true);
    }

    void SaveGumProject()
    {
        var project = new GumProjectSave();
        var component = new ComponentSave { Name = "Button" };
        var state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 5f, SetsValue = true });
        component.States.Add(state);
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { ElementType = ElementType.Component, Name = component.Name });
        project.Save(Gumx, saveElements: true);
    }

    EarlyProjectFiles EarlyFiles(params string[] globalFiles) =>
        new(Csproj, Task.FromResult<IReadOnlyList<string>>(globalFiles));

    async Task<GumProjectPrefetch> StartAndFinish(EarlyProjectFiles earlyFiles)
    {
        var prefetch = new GumProjectPrefetch();
        prefetch.Start(earlyFiles);
        await prefetch.Completion;
        return prefetch;
    }

    static object ButtonX() =>
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Single(item => item.Name == "Button")
            .DefaultState.Variables.Single(item => item.Name == "X").Value;

    // Same length and write time, so the Gum data stamp can't tell: only a project the load did not read itself
    // still has the old value.
    void RewriteButtonXKeepingTheStamp()
    {
        var writeTime = File.GetLastWriteTimeUtc(ButtonFile);
        var text = File.ReadAllText(ButtonFile);
        text.ShouldContain(">5<");
        File.WriteAllText(ButtonFile, text.Replace(">5<", ">6<"));
        File.SetLastWriteTimeUtc(ButtonFile, writeTime);
    }

    [Fact]
    public async Task Load_ShouldTakeThePrefetchedProject_WhenNothingChanged()
    {
        FileReferenceTracker.Self.Prefetch = await StartAndFinish(EarlyFiles("GumProject/GumProject.gumx"));
        RewriteButtonXKeepingTheStamp();

        FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(GumDirectory, force: true);

        ButtonX().ShouldBe(5f);
    }

    [Fact]
    public async Task Load_ShouldReadTheFiles_WhenAGumFileChangedAfterThePrefetch()
    {
        FileReferenceTracker.Self.Prefetch = await StartAndFinish(EarlyFiles("GumProject/GumProject.gumx"));
        RewriteButtonXKeepingTheStamp();
        File.SetLastWriteTimeUtc(ButtonFile, File.GetLastWriteTimeUtc(ButtonFile).AddSeconds(5));

        FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(GumDirectory, force: true);

        ButtonX().ShouldBe(6f);
    }

    [Fact]
    public async Task Load_ShouldReadTheFiles_WhenThePrefetchReadAnotherGumx()
    {
        var otherDirectory = Path.Combine(_root, "Content", "OtherGum");
        Directory.CreateDirectory(otherDirectory);
        File.Copy(Gumx, Path.Combine(otherDirectory, "GumProject.gumx"));
        FileReferenceTracker.Self.Prefetch = await StartAndFinish(EarlyFiles("OtherGum/GumProject.gumx"));
        RewriteButtonXKeepingTheStamp();

        FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(GumDirectory, force: true);

        ButtonX().ShouldBe(6f);
    }

    [Fact]
    public async Task Load_ShouldReadTheFiles_WhenThePrefetchWasAlreadyTaken()
    {
        FileReferenceTracker.Self.Prefetch = await StartAndFinish(EarlyFiles("GumProject/GumProject.gumx"));
        FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(GumDirectory, force: true);
        RewriteButtonXKeepingTheStamp();

        FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(GumDirectory, force: true);

        ButtonX().ShouldBe(6f);
    }

    [Fact]
    public async Task Start_ShouldReadNothing_WhenTheProjectHasNoGumx()
    {
        var prefetch = await StartAndFinish(EarlyFiles("Sprites/Ship.png"));

        prefetch.TryTake(Gumx, FileReferenceTracker.GetGumDataStamp(GumDirectory), out var project, out _).ShouldBeFalse();
        project.ShouldBeNull();
    }

    [Fact]
    public void Start_ShouldReadNothing_WithoutAnEarlyRead()
    {
        var prefetch = new GumProjectPrefetch();
        prefetch.Start(null);

        prefetch.TryTake(Gumx, FileReferenceTracker.GetGumDataStamp(GumDirectory), out var project, out _).ShouldBeFalse();
        project.ShouldBeNull();
    }
}
