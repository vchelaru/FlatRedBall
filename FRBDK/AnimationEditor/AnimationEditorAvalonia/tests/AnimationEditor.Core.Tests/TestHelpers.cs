using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Shared helpers for tests that exercise singleton state.
/// Call <see cref="SetupFreshAcls"/> at the beginning of every test that
/// touches AppCommands, ProjectManager, or SelectedState.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Resets all singletons to a clean, isolated state and returns the new ACLS.
    /// </summary>
    public static AnimationChainListSave SetupFreshAcls()
    {
        var acls = new AnimationChainListSave();
        ProjectManager.Self.AnimationChainListSave = acls;
        ProjectManager.Self.FileName = null;

        // Clearing SelectedChain also clears frame / rect / circle
        SelectedState.Self.SelectedChain = null;
        SelectedState.Self.SelectedNodes = new List<object>();

        // Default: always confirm dialogs; can be overridden per-test
        AppCommands.Self.ConfirmAsync = (msg, title) => Task.FromResult(true);

        // Run UI-thread dispatches inline so tests don't need a dispatcher
        AppCommands.Self.DoOnUiThread = action => action();

        // Reset FileDialogService to null (no-op) so save-as tests are isolated
        AppCommands.Self.FileDialogService = NullFileDialogService.Instance;

        // Reset AppState to known defaults so default-value tests are order-independent
        AppState.Self.GridSize = 16;
        AppState.Self.IsSnapToGridChecked = false;
        AppState.Self.WireframeZoomValue = 100;
        AppState.Self.UnitType = AnimationEditor.Core.Data.UnitType.Pixel;
        AppState.Self.ProjectFolder = null;

        return acls;
    }

    /// <summary>Creates a frame with an initialized ShapeCollectionSave.</summary>
    public static AnimationFrameSave MakeFrame(string textureName = "Tex.png")
    {
        return new AnimationFrameSave
        {
            TextureName      = textureName,
            LeftCoordinate   = 0f,
            RightCoordinate  = 1f,
            TopCoordinate    = 0f,
            BottomCoordinate = 1f,
            FrameLength      = 0.1f,
            ShapeCollectionSave = new ShapeCollectionSave()
        };
    }

    /// <summary>Creates a named chain, adds <paramref name="frameCount"/> frames, and adds it to <paramref name="acls"/>.</summary>
    public static AnimationChainSave MakeChain(AnimationChainListSave acls, string name, int frameCount = 0)
    {
        var chain = new AnimationChainSave { Name = name };
        for (int i = 0; i < frameCount; i++)
            chain.Frames.Add(MakeFrame($"frame{i}.png"));
        acls.AnimationChains.Add(chain);
        return chain;
    }

    /// <summary>Disposable helper that creates a temp directory and cleans it up.</summary>
    public sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "AnimationEditorCoreTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
