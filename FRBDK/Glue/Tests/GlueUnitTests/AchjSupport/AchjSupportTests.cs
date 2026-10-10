using EditorObjects.Parsing;
using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using OfficialPlugins.AnimationChainPlugin;
using OfficialPlugins.BuiltFileSizeInspector.ViewModels;
using OfficialPlugins.MonoGameContent;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using AnimationChainAtiManager = OfficialPlugins.AnimationChainPlugin.Managers.AssetTypeInfoManager;
using SpriteAtiManager = OfficialPlugins.SpritePlugin.Managers.AssetTypeInfoManager;

namespace GlueUnitTests.AchjSupport;

/// <summary>
/// Glue treats .achj (the JSON twin of .achx, written by the FlatRedBall Animation Editor) as an
/// AnimationChainList file everywhere it treats .achx as one. The engine reads it via
/// AnimationChainListSave.FromFile's extension dispatch; these tests pin the Glue side.
/// </summary>
public class AchjSupportTests : IDisposable
{
    const string AnimationChainListQualifiedType = "FlatRedBall.Graphics.Animation.AnimationChainList";

    readonly string _directory = Path.Combine(Path.GetTempPath(), "glue-achj-" + Guid.NewGuid());
    readonly MainAnimationChainPlugin _plugin = new MainAnimationChainPlugin();

    public AchjSupportTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        // Gives back any ATI a test registered, so a later test can't see it.
        _plugin.UnregisterAssetTypeInfos();
        Directory.Delete(_directory, recursive: true);
    }

    string Write(string fileName, string contents)
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    string WriteWalkAndIdle() => Write("hero.achj", """
        {
          "animationChains": [
            { "name": "Walk", "frames": [
              { "textureName": "hero.png", "frameLength": 0.1 },
              { "textureName": "sub/hero2.png", "frameLength": 0.1 }
            ] },
            { "name": "Idle", "frames": [ { "textureName": "hero.png", "frameLength": 0.1 } ] }
          ]
        }
        """);

    #region Content detection

    [Fact]
    public void DoesFileReferenceContent_Achj_ReturnsTrue()
    {
        FileHelper.DoesFileReferenceContent("Content/hero.achj").ShouldBeTrue();
    }

    [Fact]
    public void GetNamedObjectsIn_Achj_ListsTheChains()
    {
        var path = WriteWalkAndIdle();
        var names = new List<string>();

        var handled = ContentParser.GetNamedObjectsIn(path, names);

        handled.ShouldBeTrue();
        names.ShouldBe(new[] { "Walk (AnimationChain)", "Idle (AnimationChain)" });
    }

    #endregion

    #region Referenced files

    [Fact]
    public void FillWithReferencedFiles_Achj_ReturnsEveryTextureRelativeToTheFile()
    {
        var path = WriteWalkAndIdle();
        var referenced = new HashSet<FilePath>();

        var response = _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced);

        response.Succeeded.ShouldBeTrue();
        referenced.ShouldBe(new[]
        {
            new FilePath(Path.Combine(_directory, "hero.png")),
            new FilePath(Path.Combine(_directory, "sub", "hero2.png")),
        }, ignoreOrder: true);
    }

    [Fact]
    public void FillWithReferencedFiles_AchjThatIsNotValidJson_DoesNotThrow()
    {
        var path = Write("broken.achj", "{ \"animationChains\": [ ");
        var referenced = new HashSet<FilePath>();

        Should.NotThrow(() => _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced));

        referenced.ShouldBeEmpty();
    }

    [Fact]
    public void FillWithReferencedFiles_Achx_StillReadsTextureNameElements()
    {
        var path = Write("hero.achx", "<AnimationChainArraySave><AnimationChain><Frame>\n<TextureName>hero.png</TextureName>\n</Frame></AnimationChain></AnimationChainArraySave>");
        var referenced = new HashSet<FilePath>();

        _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced);

        referenced.ShouldBe(new[] { new FilePath(Path.Combine(_directory, "hero.png")) });
    }

    #endregion

    #region File change reactions

    [Theory]
    [InlineData("achx", true)]
    [InlineData("achj", true)]
    [InlineData("png", false)]
    public void AffectsAnimationErrors_ByExtension(string extension, bool expected)
    {
        MainAnimationChainPlugin.AffectsAnimationErrors(extension).ShouldBe(expected);
    }

    [Theory]
    [InlineData("achx", true)]
    [InlineData("achj", true)]
    [InlineData("aseprite", false)]
    public void IsCopiedExtension_LiveEditCopiesChangedFileToBuildFolder(string extension, bool expected)
    {
        FileChangeManager.IsCopiedExtension(extension).ShouldBe(expected);
    }

    [Theory]
    [InlineData("achx", true)]
    [InlineData("achj", true)]
    [InlineData("png", false)]
    public void IsKnownNonPipelineExtension_ByExtension(string extension, bool expected)
    {
        MainContentPipelinePlugin.IsKnownNonPipelineExtension(extension).ShouldBe(expected);
    }

    #endregion

    #region UI surfaces

    [Theory]
    [InlineData("achx", true)]
    [InlineData("ACHX", true)]
    [InlineData("achj", true)]
    [InlineData("png", false)]
    public void IsOpenedByAnimationEditor_ByExtension(string extension, bool expected)
    {
        FileCommands.IsOpenedByAnimationEditor(extension).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Content/hero.achx")]
    [InlineData("Content/hero.achj")]
    public void BuiltFileSize_AnimationFiles_AreCategorizedAsAnimationChains(string fileName)
    {
        BuiltFileSizeViewModel.GetCategoryForFile(fileName).ShouldBe("Animation Chains");
    }

    [Theory]
    [InlineData("hero.achx", true)]
    [InlineData("hero.ACHX", true)]
    [InlineData("hero.achj", true)]
    [InlineData("hero.png", false)]
    public void HasAnimationChainFile_ByFileName(string fileName, bool expected)
    {
        var files = new[] { new ReferencedFileSave { Name = fileName } };

        SpriteAtiManager.HasAnimationChainFile(files).ShouldBe(expected);
    }

    [Fact]
    public void FileAssociationSettings_ListsAchjAlongsideAchx()
    {
        var settings = new FlatRedBall.Glue.Settings.FileAssociationSettings();

        settings.GetApplicationForExtension("achj").ShouldBe(settings.GetApplicationForExtension("achx"));
        settings.GetApplicationForExtension("achj").ShouldNotBeNull();
    }

    #endregion

    #region Asset type and version gate

    [Theory]
    [InlineData((int)GlueProjectSave.GluxVersions.GumFrameworkElementHasDefaultFormsTemplates, false, false)]
    [InlineData((int)GlueProjectSave.GluxVersions.AchjAnimationFiles, false, true)]
    [InlineData((int)GlueProjectSave.LatestVersion, false, true)]
    // A source-linked project builds against current engine source whatever its FileVersion says.
    [InlineData(0, true, true)]
    public void ShouldRegisterAchjAti_RequiresEngineThatReadsAchj(int fileVersion, bool isSourceLinked, bool expected)
    {
        AnimationChainAtiManager.ShouldRegisterAchjAti(fileVersion, isSourceLinked).ShouldBe(expected);
    }

    [Fact]
    public void EngineSyntaxVersion_MatchesLatestGluxVersion()
    {
        var attribute = typeof(FlatRedBallServices).GetCustomAttribute<SyntaxVersionAttribute>();

        attribute.ShouldNotBeNull();
        attribute.Version.ShouldBe(GlueProjectSave.LatestVersion);
    }

    [Fact]
    public void GetAchjAti_IsAnAnimationChainListThatCannotBeCreatedOrPiped()
    {
        var ati = AnimationChainAtiManager.Self.GetAchjAti();

        ati.Extension.ShouldBe("achj");
        ati.QualifiedRuntimeTypeName.QualifiedType.ShouldBe(AnimationChainListQualifiedType);
        // Glue writes new files as XML, so offering .achj in the New File window would corrupt it.
        ati.HideFromNewFileWindow.ShouldBeTrue();
        // The pipeline importer is the XML one.
        ati.CanBeAddedToContentPipeline.ShouldBeFalse();
        ati.ContentImporter.ShouldBeNull();
        ati.ContentProcessor.ShouldBeNull();
    }

    [Fact]
    public void AtOrAboveTheGateVersion_AchjFilesResolveToAnAnimationChainListAti()
    {
        _plugin.AddAchjAssetTypeInfoIfSupported(GlueProjectSave.LatestVersion, isFrbSourceLinked: false);

        var rfs = new ReferencedFileSave { Name = "Entities/Hero/hero.achj" };
        var ati = rfs.GetAssetTypeInfo();

        ati.ShouldNotBeNull();
        ati.QualifiedRuntimeTypeName.QualifiedType.ShouldBe(AnimationChainListQualifiedType);
        ati.Extension.ShouldBe("achj");
        GlueCommands.Self.FileCommands.IsContent(new FilePath("Content/hero.achj")).ShouldBeTrue();
        // The .achx ATI is untouched: it is still what the type and the extension resolve to.
        AvailableAssetTypes.Self.GetAssetTypeFromExtension("achx").ShouldBeSameAs(AvailableAssetTypes.CommonAtis.AnimationChainList);
        AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(AnimationChainListQualifiedType, null)
            .ShouldBeSameAs(AvailableAssetTypes.CommonAtis.AnimationChainList);
    }

    [Fact]
    public void BelowTheGateVersion_AchjIsNotAnAssetType()
    {
        _plugin.AddAchjAssetTypeInfoIfSupported(
            (int)GlueProjectSave.GluxVersions.GumFrameworkElementHasDefaultFormsTemplates, isFrbSourceLinked: false);

        AvailableAssetTypes.Self.GetAssetTypeFromExtension("achj").ShouldBeNull();
    }

    [Fact]
    public void AddAssetTypeInfo_SameRuntimeTypeDifferentExtension_RegistersBoth()
    {
        // The Aseprite ATI is also a clone of the .achx one, so it shares the .achj ATI's runtime type.
        var plugin = new AtiAddingPlugin();
        var first = AnimationChainAtiManager.Self.GetAchjAti();
        first.Extension = "achjfirst";
        var second = AnimationChainAtiManager.Self.GetAchjAti();

        try
        {
            plugin.Add(first);
            plugin.Add(second);

            AvailableAssetTypes.Self.GetAssetTypeFromExtension("achjfirst").ShouldBeSameAs(first);
            AvailableAssetTypes.Self.GetAssetTypeFromExtension("achj").ShouldBeSameAs(second);
        }
        finally
        {
            plugin.UnregisterAssetTypeInfos();
        }
    }

    [Fact]
    public void AddAssetTypeInfo_SameRuntimeTypeAndExtensionTwice_RegistersOnce()
    {
        var plugin = new AtiAddingPlugin();
        var ati = AnimationChainAtiManager.Self.GetAchjAti();

        try
        {
            plugin.Add(ati);
            plugin.Add(AnimationChainAtiManager.Self.GetAchjAti());

            AvailableAssetTypes.Self.AllAssetTypes.Count(item => item.Extension == "achj").ShouldBe(1);
        }
        finally
        {
            plugin.UnregisterAssetTypeInfos();
        }
    }

    class AtiAddingPlugin : PluginBase
    {
        public override string FriendlyName => "ATI adding plugin (test)";
        public override Version Version => new(1, 0);
        public override void StartUp() { }
        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason) => true;

        public void Add(AssetTypeInfo ati) => AddAssetTypeInfo(ati);
    }

    #endregion
}

/// <summary>
/// The generated game code for an .achj is the same as for an .achx: the engine picks the parser from the
/// extension inside FlatRedBallServices.Load&lt;AnimationChainList&gt;, so only the file name differs.
/// </summary>
public class AchjCodegenTests : IDisposable
{
    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _tempProjectDirectory;
    readonly MainAnimationChainPlugin _plugin = new MainAnimationChainPlugin();

    public AchjCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };

        _plugin.AddAchjAssetTypeInfoIfSupported(GlueProjectSave.LatestVersion, isFrbSourceLinked: false);
    }

    public void Dispose()
    {
        _plugin.UnregisterAssetTypeInfos();
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); }
        catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void GlobalContentAchj_LoadsAndReloadsAsAnAnimationChainList()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/hero.achj", LoadedAtRuntime = true };

        var load = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        FlatRedBall.Glue.CodeGeneration.ReferencedFileSaveCodeGenerator.GetInitializationForReferencedFile(
            rfs, container: null, load, FlatRedBall.Glue.CodeGeneration.LoadType.CompleteLoad);
        var reload = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        FlatRedBall.Glue.CodeGeneration.ReferencedFileSaveCodeGenerator.GetReload(
            rfs, container: null, reload, FlatRedBall.Glue.CodeGeneration.LoadType.MaintainInstance);

        load.ToString().ShouldContain("Load<FlatRedBall.Graphics.Animation.AnimationChainList>(");
        load.ToString().ShouldContain("hero.achj\"");
        reload.ToString().ShouldContain("hero.achj\"");
        reload.ToString().ShouldContain(".ReplaceValues(");
    }
}
