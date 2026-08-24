using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    internal readonly struct GitStatusResult
    {
        public bool Succeeded { get; }
        public int CommitsBehind { get; }
        public string ErrorMessage { get; }

        public GitStatusResult(bool succeeded, int commitsBehind, string errorMessage)
        {
            Succeeded = succeeded;
            CommitsBehind = commitsBehind;
            ErrorMessage = errorMessage;
        }
    }

    internal readonly struct GitPullResult
    {
        public bool Succeeded { get; }
        public string ErrorMessage { get; }

        public GitPullResult(bool succeeded, string errorMessage)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Shells out to the git CLI to check whether a source-linked repo checkout is behind its
    /// upstream branch, and to fast-forward it. Uses <c>@{u}</c> (the current branch's configured
    /// upstream) rather than a hardcoded branch name, since FRB1/Gum default to NetStandard while
    /// FRB2 defaults to main.
    /// </summary>
    internal static class GitRepoStatusChecker
    {
        public static async Task<GitStatusResult> CheckStatusAsync(string repoDirectory)
        {
            var fetch = await RunGitAsync(repoDirectory, "fetch");
            if (fetch.ExitCode != 0)
            {
                return new GitStatusResult(false, 0, FirstNonEmpty(fetch.StdErr, "git fetch failed"));
            }

            var revList = await RunGitAsync(repoDirectory, "rev-list --count HEAD..@{u}");
            if (revList.ExitCode != 0)
            {
                return new GitStatusResult(false, 0, FirstNonEmpty(revList.StdErr, "Could not determine upstream branch"));
            }

            if (!int.TryParse(revList.StdOut.Trim(), out var commitsBehind))
            {
                return new GitStatusResult(false, 0, "Unexpected git output");
            }

            return new GitStatusResult(true, commitsBehind, null);
        }

        public static async Task<GitPullResult> PullAsync(string repoDirectory)
        {
            var pull = await RunGitAsync(repoDirectory, "pull --ff-only");
            return pull.ExitCode == 0
                ? new GitPullResult(true, null)
                : new GitPullResult(false, FirstNonEmpty(pull.StdErr, "git pull --ff-only failed"));
        }

        static string FirstNonEmpty(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        static async Task<(int ExitCode, string StdOut, string StdErr)> RunGitAsync(string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync());

                return (process.ExitCode, await stdOutTask, await stdErrTask);
            }
            catch (Exception ex)
            {
                return (-1, string.Empty, ex.Message);
            }
        }
    }
}
