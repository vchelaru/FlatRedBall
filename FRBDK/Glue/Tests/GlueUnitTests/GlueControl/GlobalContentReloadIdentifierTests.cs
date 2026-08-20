using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
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
//
// Fixing the identifier's formatting alone wasn't enough: the same physical file can also be
// separately referenced by one or more elements (e.g. a test entity referencing a shared .achx
// directly for standalone testing), each producing its own non-global ReferencedFileSave.
// HandleFileChanged's firstRfs was whichever of those happened to come first - not necessarily the
// actual global entry - so SelectGlobalReferencedFileTests reproduces that multi-reference scenario.
public class GlobalContentReloadIdentifierTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public GlobalContentReloadIdentifierTests()
    {
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void GetGlobalContentReloadIdentifier_ForFileNestedInSubfolder_ReturnsFolderQualifiedName()
    {
        // GetInstanceName()'s "GlobalContent/" folder-qualification path only applies when the
        // referenced file has no containing element (GetContainer() returns null) - matching a
        // real global content file, which isn't listed in any Screen/Entity's ReferencedFiles.
        ObjectFinder.Self.GlueProject = null;

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
        ObjectFinder.Self.GlueProject = null;

        var rfs = new ReferencedFileSave
        {
            Name = "GlobalContent/ChibiCthulhuTiles.tmx",
            IncludeDirectoryRelativeToContainer = true,
        };

        var identifier = RefreshManager.GetGlobalContentReloadIdentifier(rfs);

        identifier.ShouldBe("ChibiCthulhuTiles");
    }

    [Fact]
    public void SelectGlobalReferencedFile_WhenSameFileIsAlsoReferencedByEntities_PicksTheEntryWithNoContainer()
    {
        // Reproduces CrankyChibiCthulhu's Entities/Player/AnimationChainListFile.achx: declared
        // IsSharedStatic in both Player.glej and TestEntity.glej (each producing an owned,
        // non-global ReferencedFileSave), plus the actual global entry with no container.
        var project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = project;

        var playerOwnedRfs = new ReferencedFileSave
        {
            Name = "Entities/Player/AnimationChainListFile.achx",
            IncludeDirectoryRelativeToContainer = true,
        };
        var playerEntity = new EntitySave { Name = "Entities\\Player" };
        playerEntity.ReferencedFiles.Add(playerOwnedRfs);
        project.Entities.Add(playerEntity);

        var testEntityOwnedRfs = new ReferencedFileSave
        {
            Name = "Entities/Player/AnimationChainListFile.achx",
            IncludeDirectoryRelativeToContainer = true,
        };
        var testEntity = new EntitySave { Name = "Entities\\TestEntity" };
        testEntity.ReferencedFiles.Add(testEntityOwnedRfs);
        project.Entities.Add(testEntity);

        var globalRfs = new ReferencedFileSave
        {
            Name = "GlobalContent/Entities/Player/AnimationChainListFile.achx",
            IncludeDirectoryRelativeToContainer = true,
        };

        var rfses = new List<ReferencedFileSave> { playerOwnedRfs, testEntityOwnedRfs, globalRfs };

        // firstRfs mirrors HandleFileChanged's rfses.FirstOrDefault() - the (wrong) value it used
        // to send before this fix.
        var selected = RefreshManager.SelectGlobalReferencedFile(rfses, fallback: playerOwnedRfs);

        selected.ShouldBeSameAs(globalRfs);
        RefreshManager.GetGlobalContentReloadIdentifier(selected).ShouldBe("Entities_Player_AnimationChainListFile");
    }
}
