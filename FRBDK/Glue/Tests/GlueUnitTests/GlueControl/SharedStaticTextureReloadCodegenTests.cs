using System;
using System.IO;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

// Reproduces the crash reported against CrankyChibiCthulhu (the same project
// GlobalContentReloadIdentifierTests's SelectGlobalReferencedFile test is modeled on): a texture shared
// across many Entities via IsSharedStatic (ChibiCthulhuTiles.png, referenced by Bullet, Checkpoint,
// DestructibleObstacle, Door, Enemy, ... and GlobalContent itself). Each of those holders is a distinct
// static field, but they all point at the same underlying Texture2D instance loaded from disk.
//
// GetReload's generated MaintainInstance reload does `cm.UnloadAsset(instanceName)` unconditionally before
// loading a replacement. When the file changes on disk, Glue tells every holder to reload in turn - the
// first one to run successfully unloads/disposes the shared texture, so every later holder's UnloadAsset
// call finds it no longer tracked and throws ArgumentException ("does not contain the argument
// assetToUnload, or the file has been loaded from XNB..."). That exception aborts the reload for every
// holder still queued behind the one that already succeeded - including GlobalContent's own field, which
// is what brand-new entities (e.g. a boss spawned after the edit) read their texture reference from at
// construction time. The abandoned field still points at the disposed texture, so a freshly spawned
// Sprite crashes with ObjectDisposedException the next time it's drawn - far from this reload code, and
// with no indication a reload ever ran into trouble.
public class SharedStaticTextureReloadCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _tempProjectDirectory;

    public SharedStaticTextureReloadCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
        };
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void GetReload_ForSharedStaticTexture_GuardsUnloadAssetAgainstAlreadyUnloadedInstance()
    {
        var rfs = new ReferencedFileSave
        {
            Name = "GlobalContent/ChibiCthulhuTiles.png",
            LoadedAtRuntime = true,
            IsSharedStatic = true,
        };

        var codeBlock = new CodeBlockBase();
        ReferencedFileSaveCodeGenerator.GetReload(rfs, container: null, codeBlock, LoadType.MaintainInstance);
        var generatedCode = codeBlock.ToString();

        // Sanity check this test actually exercises the Texture2D reload branch (ReplaceTexture is only
        // emitted there) rather than silently falling through to a no-op because the ati failed to resolve.
        Assert.Contains("FlatRedBallServices.ReplaceTexture", generatedCode);

        // Before the fix, this was an unconditional "cm.UnloadAsset(ChibiCthulhuTiles);" - fine the first
        // time any holder of the shared texture reloads it, but every later holder's call throws
        // ArgumentException once the first one has already unloaded/disposed it.
        Assert.Contains("if (cm.IsAssetLoadedByReference(ChibiCthulhuTiles))", generatedCode);
    }
}
