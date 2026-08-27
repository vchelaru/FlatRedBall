using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

/// <summary>
/// A user reported ObjectDisposedException crashes (CrankyChibiCthulhu, ChibiCthulhuTiles.png) that
/// persisted even after #2211/#2212 fixed a different bug in the same reload path, and confirmed it was
/// inconsistent across machines running the exact same project/build - not something baked into the
/// compiled game, which rules out a codegen/reflection bug (those would reproduce identically everywhere).
///
/// HandleFileChanged sends two independent commands for a PNG that's both global content and separately
/// IsSharedStatic on one or more Entities (this project's shape): a ForceReloadFileDto (whose handler
/// reloads every Entity's own copy of the field, then reloads GlobalContent's own copy too), and - with no
/// dependency between the two - a ReloadGlobalContentDto (whose handler *only* reloads GlobalContent's
/// copy). The second dispatch disposes the texture the first one just loaded and assigned to GlobalContent,
/// loads a replacement, and fixes up every currently-drawn Sprite - but never re-notifies the Entity types
/// the first dispatch already updated. Their static fields still hold the now-disposed instance. Whether
/// this actually crashes depends on whether gameplay later builds a new Sprite from one of those stale
/// fields before another full reload/restart - not on anything different between machines' builds, which
/// is exactly the "same project, inconsistent" symptom reported.
///
/// ShouldSendReloadGlobalContentDto is the extracted decision HandleFileChanged now calls: skip the second
/// send when a ForceReloadFileDto already covered the same change.
/// </summary>
public class RedundantGlobalContentReloadTests
{
    [Fact]
    public void ShouldSendReloadGlobalContentDto_WhenForceReloadFileDtoAlreadyCoveredIt_ReturnsFalse()
    {
        // Reproduces the crash shape: ChibiCthulhuTiles.png is global content (isGlobalContent: true), not
        // content-pipeline, and a ForceReloadFileDto was already sent and already reloaded GlobalContent's
        // own copy - sending a ReloadGlobalContentDto too would dispose that fresh texture again without
        // any Entity type being told to reload.
        RefreshManager.ShouldSendReloadGlobalContentDto(
            isGlobalContent: true, isContentPipeline: false, forceReloadFileDtoWasSent: true)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldSendReloadGlobalContentDto_WhenNoForceReloadFileDtoWasSent_ReturnsTrue()
    {
        // E.g. a global .achx: shouldReloadFile is false for that extension, so ForceReloadFileDto is never
        // sent (a RestartScreenDto is sent instead, which doesn't reload GlobalContent) - the
        // ReloadGlobalContentDto here is the only thing that reloads GlobalContent's copy, so it must
        // still be sent.
        RefreshManager.ShouldSendReloadGlobalContentDto(
            isGlobalContent: true, isContentPipeline: false, forceReloadFileDtoWasSent: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldSendReloadGlobalContentDto_WhenNotGlobalContent_ReturnsFalse()
    {
        RefreshManager.ShouldSendReloadGlobalContentDto(
            isGlobalContent: false, isContentPipeline: false, forceReloadFileDtoWasSent: false)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldSendReloadGlobalContentDto_WhenContentPipeline_ReturnsFalse()
    {
        // FRB throws if a content-pipeline asset is reloaded individually - unaffected by the redundant
        // dispatch fix, kept as its own case since it's a separate reason to skip the send.
        RefreshManager.ShouldSendReloadGlobalContentDto(
            isGlobalContent: true, isContentPipeline: true, forceReloadFileDtoWasSent: false)
            .ShouldBeFalse();
    }
}
