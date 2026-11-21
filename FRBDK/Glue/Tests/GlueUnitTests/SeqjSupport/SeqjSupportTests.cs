using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
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
using Xunit;
using AnimationChainAtiManager = OfficialPlugins.AnimationChainPlugin.Managers.AssetTypeInfoManager;

namespace GlueUnitTests.SeqjSupport;

/// <summary>
/// Glue treats a .seqj (named sequences over .achx/.achj chains, written by the FlatRedBall 2 Animation Editor)
/// as a SequenceList file: it can be added to a project, its sources are tracked as dependencies, and the
/// generated code loads it through FlatRedBallServices.Load.
/// </summary>
public class SeqjSupportTests : IDisposable
{
    const string SequenceListQualifiedType = "FlatRedBall.Graphics.Animation.SequenceList";

    readonly string _directory = Path.Combine(Path.GetTempPath(), "glue-seqj-" + Guid.NewGuid());
    readonly MainAnimationChainPlugin _plugin = new MainAnimationChainPlugin();

    public SeqjSupportTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        _plugin.UnregisterAssetTypeInfos();
        Directory.Delete(_directory, recursive: true);
    }

    string Write(string fileName, string contents)
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void DoesFileReferenceContent_Seqj_ReturnsTrue()
    {
        FileHelper.DoesFileReferenceContent("Content/hero.seqj").ShouldBeTrue();
    }

    [Fact]
    public void FillWithReferencedFiles_Seqj_ReturnsEverySourceRelativeToTheFile()
    {
        var path = Write("hero.seqj", """
            { "version": 1, "sources": [ "hero.achx", "sub/extra.achj" ], "sequences": [] }
            """);
        var referenced = new HashSet<FilePath>();

        var response = _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced);

        response.Succeeded.ShouldBeTrue();
        referenced.ShouldBe(new[]
        {
            new FilePath(Path.Combine(_directory, "hero.achx")),
            new FilePath(Path.Combine(_directory, "sub", "extra.achj")),
        }, ignoreOrder: true);
    }

    [Fact]
    public void FillWithReferencedFiles_SeqjThatIsNotValidJson_DoesNotThrow()
    {
        var path = Write("broken.seqj", "{ \"sources\": [ ");
        var referenced = new HashSet<FilePath>();

        Should.NotThrow(() => _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced));

        referenced.ShouldBeEmpty();
    }

    [Fact]
    public void FillWithReferencedFiles_SeqjWithByteOrderMark_ReadsSources()
    {
        var path = Path.Combine(_directory, "bom.seqj");
        File.WriteAllText(path, "{ \"sources\": [ \"hero.achx\" ] }", new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var referenced = new HashSet<FilePath>();

        _plugin.HandleFillWithReferencedFilesNew(new FilePath(path), referenced);

        referenced.ShouldBe(new[] { new FilePath(Path.Combine(_directory, "hero.achx")) });
    }

    [Fact]
    public void IsCopiedExtension_LiveEditCopiesChangedSeqjToBuildFolder()
    {
        FileChangeManager.IsCopiedExtension("seqj").ShouldBeTrue();
    }

    [Fact]
    public void IsKnownNonPipelineExtension_Seqj_IsTrue()
    {
        MainContentPipelinePlugin.IsKnownNonPipelineExtension("seqj").ShouldBeTrue();
    }

    [Fact]
    public void IsOpenedByAnimationEditor_Seqj_IsTrue()
    {
        FileCommands.IsOpenedByAnimationEditor("seqj").ShouldBeTrue();
    }

    [Fact]
    public void BuiltFileSize_Seqj_IsCategorizedAsAnimationChains()
    {
        BuiltFileSizeViewModel.GetCategoryForFile("Content/hero.seqj").ShouldBe("Animation Chains");
    }

    [Fact]
    public void FileAssociationSettings_ListsSeqjAlongsideAchx()
    {
        var settings = new FlatRedBall.Glue.Settings.FileAssociationSettings();

        settings.GetApplicationForExtension("seqj").ShouldBe(settings.GetApplicationForExtension("achx"));
        settings.GetApplicationForExtension("seqj").ShouldNotBeNull();
    }

    [Theory]
    [InlineData((int)GlueProjectSave.GluxVersions.EntityFactoryRegistry, false, false)]
    [InlineData((int)GlueProjectSave.GluxVersions.SeqjSequenceFiles, false, true)]
    [InlineData(0, true, true)]
    public void ShouldRegisterSeqjAti_RequiresEngineThatLoadsSequenceList(int fileVersion, bool isSourceLinked, bool expected)
    {
        AnimationChainAtiManager.ShouldRegisterSeqjAti(fileVersion, isSourceLinked).ShouldBe(expected);
    }

    [Fact]
    public void GetSeqjAti_IsASequenceListThatCanBeAddedButNotCreatedOrPiped()
    {
        var ati = AnimationChainAtiManager.Self.GetSeqjAti();

        ati.Extension.ShouldBe("seqj");
        ati.QualifiedRuntimeTypeName.QualifiedType.ShouldBe(SequenceListQualifiedType);
        ati.HideFromNewFileWindow.ShouldBeTrue();
        ati.CanBeAddedToContentPipeline.ShouldBeFalse();
    }

    [Fact]
    public void AtOrAboveTheGateVersion_SeqjFilesResolveToASequenceListAti()
    {
        _plugin.AddSeqjAssetTypeInfoIfSupported(GlueProjectSave.LatestVersion, isFrbSourceLinked: false);

        var rfs = new ReferencedFileSave { Name = "Entities/Hero/hero.seqj" };
        var ati = rfs.GetAssetTypeInfo();

        ati.ShouldNotBeNull();
        ati.QualifiedRuntimeTypeName.QualifiedType.ShouldBe(SequenceListQualifiedType);
        GlueCommands.Self.FileCommands.IsContent(new FilePath("Content/hero.seqj")).ShouldBeTrue();
    }

    [Fact]
    public void BelowTheGateVersion_SeqjIsNotAnAssetType()
    {
        _plugin.AddSeqjAssetTypeInfoIfSupported((int)GlueProjectSave.GluxVersions.EntityFactoryRegistry, isFrbSourceLinked: false);

        AvailableAssetTypes.Self.GetAssetTypeFromExtension("seqj").ShouldBeNull();
    }
}

/// <summary>The generated game code for a .seqj loads a SequenceList through FlatRedBallServices.Load.</summary>
public class SeqjCodegenTests : IDisposable
{
    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _tempProjectDirectory;
    readonly MainAnimationChainPlugin _plugin = new MainAnimationChainPlugin();

    public SeqjCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };

        _plugin.AddSeqjAssetTypeInfoIfSupported(GlueProjectSave.LatestVersion, isFrbSourceLinked: false);
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
    public void GlobalContentSeqj_LoadsAsASequenceList()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/hero.seqj", LoadedAtRuntime = true };

        var load = new FlatRedBall.Glue.CodeGeneration.CodeBuilder.CodeBlockBase();
        FlatRedBall.Glue.CodeGeneration.ReferencedFileSaveCodeGenerator.GetInitializationForReferencedFile(
            rfs, container: null, load, FlatRedBall.Glue.CodeGeneration.LoadType.CompleteLoad);

        load.ToString().ShouldContain("Load<FlatRedBall.Graphics.Animation.SequenceList>(");
        load.ToString().ShouldContain("hero.seqj\"");
    }
}
