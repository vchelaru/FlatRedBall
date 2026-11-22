using System;
using System.IO;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// Asking the tracker which files a .gumx references used to reload the whole Gum project from disk
/// every time (about 1.3 s on a large project, several times per project load). It must reload only
/// when something under the Gum folder actually changed.
/// </summary>
public class GumProjectReloadOnReferenceLookupTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "glue-gumreload-" + Guid.NewGuid());
    readonly GumProjectSave _originalGumProjectSave;
    readonly string _originalRelativeDirectory;
    readonly FlatRedBall.Glue.SaveClasses.GlueProjectSave _originalGlueProject;

    public GumProjectReloadOnReferenceLookupTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        _originalRelativeDirectory = FileManager.RelativeDirectory;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave
        {
            FileVersion = FlatRedBall.Glue.SaveClasses.GlueProjectSave.LatestVersion
        };
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        Directory.Delete(_directory, recursive: true);
    }

    string SaveProject()
    {
        var project = new GumProjectSave();

        var component = new ComponentSave { Name = "Button" };
        component.States.Add(new StateSave { Name = "Default" });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { ElementType = ElementType.Component, Name = component.Name });

        var path = Path.Combine(_directory, "GumProject.gumx");
        project.Save(path, saveElements: true);
        return path;
    }

    void LookUpReferencesOf(string gumxPath)
    {
        var result = new System.Collections.Generic.HashSet<FilePath>();
        FileReferenceTracker.Self.HandleFillWithReferencedFiles(new FilePath(gumxPath), result)
            .Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ReferenceLookupOfTheProject_NothingChangedOnDisk_DoesNotReloadTheProject()
    {
        var path = SaveProject();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        LookUpReferencesOf(path);
        var afterFirstLookup = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        LookUpReferencesOf(path);

        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(afterFirstLookup);
    }

    [Fact]
    public void ReferenceLookupOfTheProject_ElementFileEditedOnDisk_ReloadsTheProject()
    {
        var path = SaveProject();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        LookUpReferencesOf(path);
        var beforeEdit = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        // Like the Gum tool saving the component while Glue has the project open.
        var componentFile = Path.Combine(_directory, "Components", "Button.gucx");
        File.SetLastWriteTimeUtc(componentFile, File.GetLastWriteTimeUtc(componentFile).AddSeconds(5));

        LookUpReferencesOf(path);

        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldNotBeSameAs(beforeEdit);
    }

    [Fact]
    public void ReferenceLookupOfTheProject_ElementFileAddedOnDisk_ReloadsTheProject()
    {
        var path = SaveProject();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        LookUpReferencesOf(path);
        var beforeEdit = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        File.WriteAllText(Path.Combine(_directory, "Components", "Other.gucx"), "<ComponentSave />");

        LookUpReferencesOf(path);

        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldNotBeSameAs(beforeEdit);
    }
}
