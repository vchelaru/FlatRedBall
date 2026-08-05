using System;
using System.IO;

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
        try { Directory.Delete(Root, recursive: true); }
        catch { /* best-effort cleanup - a build server can still hold handles in the copied bin/obj */ }
    }
}
