using System;
using System.IO;
using System.Linq;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// A throwaway directory under the system temp path, deleted on Dispose. Shared by tests that copy a real
/// project out of the repo so a run never dirties the working tree.
/// </summary>
internal sealed class TempDir : IDisposable
{
    public string Root { get; }

    public TempDir(string prefix = "FrbGoldProject_")
    {
        Root = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public void Dispose()
    {
        try
        {
            UnloadGlueProjectLoadedFromHere();
        }
        finally
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup - a build server can still hold handles in the copied bin/obj */ }
        }
    }

    /// <summary>
    /// A project loaded from this directory must not outlive it. A loaded project is process-wide state
    /// (GlueState, the Gum plugin's GumProjectSave, the Tiled cache), and in-memory codegen tests that run
    /// later generate different text while it is still loaded: Gum layer lines appear, for one (#2429).
    /// Unloads the way File > Close Project does, so plugins clear their own state.
    ///
    /// Throws if a plugin reported an error while unloading: PluginManager disables a plugin whose handler
    /// throws, so swallowing it would turn this cleanup into a plugin that silently does nothing for every
    /// later test in the process.
    /// </summary>
    void UnloadGlueProjectLoadedFromHere()
    {
        if (FlatRedBall.Glue.ProjectManager.GlueProjectSave == null)
        {
            return;
        }

        var glueProjectFile = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.GlueProjectFileName;
        if (glueProjectFile == null || !new FlatRedBall.IO.FilePath(Root).IsRootOf(glueProjectFile))
        {
            return;
        }

        int errorCountBefore;
        lock (ErrorRecordingPlugin.Errors) { errorCountBefore = ErrorRecordingPlugin.Errors.Count; }

        FlatRedBall.Glue.ProjectManager.UnloadProject(isExiting: false);

        lock (ErrorRecordingPlugin.Errors)
        {
            if (ErrorRecordingPlugin.Errors.Count > errorCountBefore)
            {
                throw new InvalidOperationException("A plugin failed while unloading the project:" +
                    Environment.NewLine + string.Join(Environment.NewLine, ErrorRecordingPlugin.Errors.Skip(errorCountBefore)));
            }
        }
    }
}
