using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.SaveClasses;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// A Gum project can be .gumx (XML) or .gumj (JSON, with .gusj/.gucj/.gutj/.behj/.ganj siblings).
/// Gum's own loader picks the format from the extension; these pin that Glue's Gum plugin does too,
/// everywhere it used to assume the XML spelling.
/// </summary>
public class GumJsonProjectLoadingTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "glue-gumj-" + Guid.NewGuid());
    readonly GumProjectSave _originalGumProjectSave;
    readonly string _originalRelativeDirectory;

    public GumJsonProjectLoadingTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        _originalRelativeDirectory = FileManager.RelativeDirectory;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        // The tracker asks the Glue project's version which Gum features to scan for.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave
        {
            FileVersion = FlatRedBall.Glue.SaveClasses.GlueProjectSave.LatestVersion
        };
        Directory.CreateDirectory(_directory);
    }

    readonly FlatRedBall.Glue.SaveClasses.GlueProjectSave _originalGlueProject;

    public void Dispose()
    {
        // Gives back any ATI a test registered, so a later test can't see it.
        AssetTypeInfoManager.Self.UnloadProjectSpecificAtis();
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        Directory.Delete(_directory, recursive: true);
    }

    /// <summary>Saves a project with one screen and one component, and returns the project file's path.</summary>
    string SaveProject(string projectFileName, Action<ComponentSave> customizeComponent = null)
    {
        var project = new GumProjectSave();

        var screen = new ScreenSave { Name = "MainScreen" };
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { ElementType = ElementType.Screen, Name = screen.Name });

        var component = new ComponentSave { Name = "Button" };
        component.States.Add(new StateSave { Name = "Default" });
        customizeComponent?.Invoke(component);
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { ElementType = ElementType.Component, Name = component.Name });

        var path = Path.Combine(_directory, projectFileName);
        project.Save(path, saveElements: true);
        return path;
    }

    HashSet<string> ReferencedFilesOf(string file)
    {
        // The tracker only tracks files once a Gum project is loaded, like in a real session.
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(
            Directory.GetFiles(_directory).Single(item => GumProjectSave.IsProjectFile(item)));

        var result = new HashSet<FilePath>();
        var response = FileReferenceTracker.Self.HandleFillWithReferencedFiles(new FilePath(file), result);

        response.Succeeded.ShouldBeTrue(response.Message);

        return result
            .Select(item => item.FullPath.Replace('\\', '/').Substring(_directory.Length + 1))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    #region Project file tracking

    [Theory]
    [InlineData("GumProject.gumx", "Screens/MainScreen.gusx", "Components/Button.gucx")]
    [InlineData("GumProject.gumj", "Screens/MainScreen.gusj", "Components/Button.gucj")]
    public void ReferencedFiles_OfProject_AreElementFilesInTheProjectsFormat(
        string projectFile, string expectedScreen, string expectedComponent)
    {
        var path = SaveProject(projectFile);

        var referenced = ReferencedFilesOf(path);

        referenced.ShouldContain(expectedScreen);
        referenced.ShouldContain(expectedComponent);
        referenced.Count(item => item.EndsWith("x", StringComparison.OrdinalIgnoreCase) && projectFile.EndsWith("j"))
            .ShouldBe(0, "a .gumj project must not list XML element files");
    }

    [Fact]
    public void ReferencedFiles_OfJsonProject_IncludeAJsonAnimationSidecarThatExists()
    {
        var path = SaveProject("GumProject.gumj");
        File.WriteAllText(Path.Combine(_directory, "Screens", "MainScreenAnimations.ganj"), "{}");
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        // Only the on-disk lookup (what Glue copies to the output folder) looks for the sidecar.
        var onDisk = new List<string>();
        FileReferenceTracker.Self.HandleGetFilesNeededOnDiskBy(
            Path.Combine(_directory, "Screens", "MainScreen.gusj"), onDisk);

        onDisk.Select(item => Path.GetFileName(item)).ShouldContain("MainScreenAnimations.ganj");
    }

    [Theory]
    [InlineData("Components/Button.gucj")]
    [InlineData("Screens/MainScreen.gusj")]
    [InlineData("GumProject.gumj")]
    [InlineData("Components/Button.gucx")]
    public void CanTrackDependenciesOn_GumFileInProjectFolder_IsTrueInEitherFormat(string relativeFile)
    {
        var path = SaveProject(relativeFile.EndsWith("x") ? "GumProject.gumx" : "GumProject.gumj");
        Gum.Managers.ObjectFinder.Self.GumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave
            ?? GumProjectSave.Load(path);

        FileReferenceTracker.CanTrackDependenciesOn(Path.Combine(_directory, relativeFile)).ShouldBeTrue();
    }

    #endregion

    #region Element file tracking

    [Fact]
    public void ReferencedFiles_OfJsonComponent_IncludeItsTexture()
    {
        var path = SaveProject("GumProject.gumj", component =>
        {
            component.Instances.Add(new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite" });
            component.DefaultState.Variables.Add(new VariableSave
            {
                Name = "SpriteInstance.SourceFile",
                Type = "string",
                Value = "hero.png",
                IsFile = true,
            });
        });

        // Load first so the element is the project's own, like a real session.
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        var result = new HashSet<FilePath>();
        var response = FileReferenceTracker.Self.HandleFillWithReferencedFiles(
            new FilePath(Path.Combine(_directory, "Components", "Button.gucj")), result);

        response.Succeeded.ShouldBeTrue(response.Message);
        result.Select(item => Path.GetFileName(item.FullPath)).ShouldContain("hero.png");
    }

    #endregion

    #region Animations

    [Theory]
    [InlineData("GumProject.gumx", "MainScreenAnimations.ganx")]
    [InlineData("GumProject.gumj", "MainScreenAnimations.ganj")]
    public void GetAnimationsFor_ReadsTheSidecarInTheProjectsFormat(string projectFile, string sidecar)
    {
        var path = SaveProject(projectFile);
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        var animations = new ElementAnimationsSave();
        animations.Animations.Add(new AnimationSave { Name = "Intro" });
        animations.Save(Path.Combine(_directory, "Screens", sidecar));

        var screen = Gum.Managers.ObjectFinder.Self.GumProjectSave.Screens.Single();

        var loaded = AnimationLogic.GetAnimationsFor(screen);

        loaded.ShouldNotBeNull();
        loaded.Animations.Select(item => item.Name).ShouldBe(new[] { "Intro" });
    }

    #endregion

    #region Format-aware extensions

    [Theory]
    [InlineData("GumProject.gumx", "gusx", "gusx")]
    [InlineData("GumProject.gumj", "gusx", "gusj")]
    [InlineData("GumProject.gumj", "gucx", "gucj")]
    [InlineData("GumProject.gumj", "gutx", "gutj")]
    [InlineData("GumProject.gumj", "behx", "behj")]
    public void GetExtensionForProjectFormat_FollowsTheLoadedProject(string projectFile, string xml, string expected)
    {
        var path = SaveProject(projectFile);
        Gum.Managers.ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(path);

        AppState.Self.GetExtensionForProjectFormat(xml).ShouldBe(expected);
    }

    [Fact]
    public void GetExtensionForProjectFormat_NoProjectLoaded_StaysXml()
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;

        AppState.Self.GetExtensionForProjectFormat("gusx").ShouldBe("gusx");
    }

    #endregion

    #region Reading single element files

    [Fact]
    public void ReadElement_JsonAndXmlFilesOfTheSameElement_ProduceTheSameElement()
    {
        var xmlProject = SaveProject("a.gumx");
        var jsonProject = SaveProject("b.gumj");

        var fromXml = GumElementFileReader.ReadElement<ScreenSave>(
            Path.Combine(_directory, "Screens", "MainScreen.gusx"), int.MaxValue);
        var fromJson = GumElementFileReader.ReadElement<ScreenSave>(
            Path.Combine(_directory, "Screens", "MainScreen.gusj"), int.MaxValue);

        fromJson.Name.ShouldBe(fromXml.Name);
        fromJson.ShouldNotBeNull();
        xmlProject.ShouldNotBeNull();
        jsonProject.ShouldNotBeNull();
    }

    #endregion

    #region Content project cleanup

    [Theory]
    [InlineData("GumProject.gumj", "GumProject.gumj", false)]   // the loaded project is always kept
    [InlineData("GumProject.gumj", "GumProject.gumx", true)]    // a stale XML project left after converting
    [InlineData("GumProject.gumx", "GumProject.gumx", false)]
    [InlineData("GumProject.gumx", "GumProject.gumj", true)]
    [InlineData("GumProject.gumj", "Screens/Old.gusx", true)]   // unreferenced element file
    public void ShouldRemoveGumRelativeFile_KeepsOnlyTheLoadedProjectFile(string loadedProject, string file, bool expected)
    {
        var gumFolder = FileManager.Standardize(_directory + "/");
        var projectPath = gumFolder + loadedProject;
        var filePath = FileManager.Standardize(gumFolder + file);

        FileReferenceTracker.ShouldRemoveGumRelativeFile(filePath, projectPath, new string[0]).ShouldBe(expected);
    }

    [Fact]
    public void ShouldRemoveGumRelativeFile_ReferencedFile_IsKept()
    {
        var gumFolder = FileManager.Standardize(_directory + "/");
        var screen = FileManager.Standardize(gumFolder + "Screens/Main.gusj");

        FileReferenceTracker.ShouldRemoveGumRelativeFile(screen, gumFolder + "GumProject.gumj", new[] { screen })
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldRemoveGumRelativeFile_FileOutsideTheGumFolder_IsKept()
    {
        var gumFolder = FileManager.Standardize(_directory + "/");
        var outside = FileManager.Standardize(Path.GetTempPath() + "elsewhere.png");

        FileReferenceTracker.ShouldRemoveGumRelativeFile(outside, gumFolder + "GumProject.gumj", new string[0])
            .ShouldBeFalse();
    }

    #endregion

    #region Asset type registration

    [Fact]
    public void JsonAtis_CoverEachGumFileTypeWithDistinctExtensionsAndNames()
    {
        var atis = AssetTypeInfoManager.Self.JsonAtis.ToList();

        atis.Select(item => item.Extension).ShouldBe(new[] { "gumj", "gucj", "gusj", "gusj" });
        atis.Select(item => item.FriendlyName).Distinct().Count().ShouldBe(atis.Count - 1,
            "the two .gusj ATIs differ by runtime type, not by name");
    }

    [Theory]
    [InlineData("gumj")]
    [InlineData("gucj")]
    [InlineData("gusj")]
    public void RefreshProjectSpecificAtis_AtTheJsonVersion_RegistersTheJsonAtis(string extension)
    {
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumJsonProjects;

        AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

        FlatRedBall.Glue.Elements.AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension).ShouldNotBeNull();
    }

    [Fact]
    public void RefreshProjectSpecificAtis_BelowTheJsonVersion_DoesNotRegisterTheJsonAtis()
    {
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumJsonProjects - 1;

        AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

        FlatRedBall.Glue.Elements.AvailableAssetTypes.Self.GetAssetTypeFromExtension("gumj").ShouldBeNull();
    }

    [Fact]
    public void UnloadProjectSpecificAtis_RemovesTheJsonAtis()
    {
        AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();
        FlatRedBall.Glue.Elements.AvailableAssetTypes.Self.GetAssetTypeFromExtension("gumj").ShouldNotBeNull();

        AssetTypeInfoManager.Self.UnloadProjectSpecificAtis();

        FlatRedBall.Glue.Elements.AvailableAssetTypes.Self.GetAssetTypeFromExtension("gumj").ShouldBeNull();
    }

    [Theory]
    [InlineData(74, false, false)]
    [InlineData(75, false, true)]
    [InlineData(74, true, true)]
    public void ShouldRegisterJsonAtis_RequiresTheJsonVersionOrSourceLinking(int fileVersion, bool sourceLinked, bool expected)
    {
        AssetTypeInfoManager.ShouldRegisterJsonAtis(fileVersion, sourceLinked).ShouldBe(expected);
        ((int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumJsonProjects).ShouldBe(75);
    }

    #endregion
}
