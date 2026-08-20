using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using System;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

// RefreshManager.HandleFileChanged used to send a bare, path-stripped file name
// (FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name))) as the
// ReloadGlobalContentDto identifier. GlobalContent.GetFile's generated switch keys on
// rfs.GetInstanceName() instead (ReferencedFileSaveCodeGenerator.GenerateGetFileMethodByName),
// which is folder-qualified for anything nested under a subfolder. For a global file like
// GlobalContent/Entities/Player/AnimationChainListFile.achx, the bare name "AnimationChainListFile"
// doesn't match any switch case, so GetFile returned null and GlobalContent.Reload(null) silently
// did nothing - a live .achx (or any nested global content) edit reloaded the file on disk but never
// visibly updated the running game.
public class GlobalContentReloadIdentifierTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public GlobalContentReloadIdentifierTests()
    {
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        // GetInstanceName()'s "GlobalContent/" folder-qualification path only applies when the
        // referenced file has no containing element (GetContainer() returns null) - matching a
        // real global content file, which isn't listed in any Screen/Entity's ReferencedFiles.
        ObjectFinder.Self.GlueProject = null;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void GetGlobalContentReloadIdentifier_ForFileNestedInSubfolder_ReturnsFolderQualifiedName()
    {
        var rfs = new ReferencedFileSave
        {
            Name = "GlobalContent/Entities/Player/AnimationChainListFile.achx",
            IncludeDirectoryRelativeToContainer = true,
        };

        var identifier = RefreshManager.GetGlobalContentReloadIdentifier(rfs);

        identifier.ShouldBe("Entities_Player_AnimationChainListFile");
    }

    [Fact]
    public void GetGlobalContentReloadIdentifier_ForFileAtGlobalContentRoot_ReturnsBareName()
    {
        var rfs = new ReferencedFileSave
        {
            Name = "GlobalContent/ChibiCthulhuTiles.tmx",
            IncludeDirectoryRelativeToContainer = true,
        };

        var identifier = RefreshManager.GetGlobalContentReloadIdentifier(rfs);

        identifier.ShouldBe("ChibiCthulhuTiles");
    }
}
