#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects;

/// <summary>
/// Evaluates the .csproj Glue is about to open on a background thread, so the second or so MSBuild
/// spends on it overlaps plugin startup instead of being paid at the start of the load. The load takes
/// the result only if it is for the same file, the file has not been written since, and the evaluation
/// succeeded; otherwise it evaluates again itself, so any error is reported the same way as before.
/// Only the evaluation runs early: everything that touches Glue state stays on the load path.
/// </summary>
public class EarlyProjectEvaluation
{
    record Pending(string FullPath, DateTime LastWriteTimeUtc, Task<Project> Evaluation);

    readonly Func<string, Project> evaluate;
    Pending? pending;

    public EarlyProjectEvaluation(Func<string, Project>? evaluate = null)
    {
        this.evaluate = evaluate ?? ProjectCreator.EvaluateProject;
    }

    /// <summary>
    /// Starts evaluating the file. MSBUILD_EXE_PATH must already be final: MSBuild reads it once, on its
    /// first evaluation in the process.
    /// </summary>
    public void Start(string csprojFileName)
    {
        if (string.IsNullOrEmpty(csprojFileName) || !File.Exists(csprojFileName))
        {
            return;
        }
        var fullPath = Path.GetFullPath(csprojFileName);
        var lastWrite = File.GetLastWriteTimeUtc(fullPath);
        pending = new Pending(fullPath, lastWrite, Task.Run(() => evaluate(fullPath)));
    }

    /// <summary>
    /// Returns the early evaluation of the file, waiting for it if it is still running, or null if there
    /// is none usable. Whatever was started is given up either way, so a later load never sees it.
    /// </summary>
    public Project? TryTake(string csprojFileName)
    {
        var taken = Interlocked.Exchange(ref pending, null);
        if (taken == null || string.IsNullOrEmpty(csprojFileName))
        {
            return null;
        }

        if (!string.Equals(Path.GetFullPath(csprojFileName), taken.FullPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Project project;
        try
        {
            project = taken.Evaluation.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // The load evaluates again and reports the error itself.
            return null;
        }

        if (!File.Exists(taken.FullPath) || File.GetLastWriteTimeUtc(taken.FullPath) != taken.LastWriteTimeUtc)
        {
            return null;
        }

        return project;
    }
}
