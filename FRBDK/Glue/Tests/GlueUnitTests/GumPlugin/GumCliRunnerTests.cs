using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// Loading a project runs "gumcli fonts" and awaits its output, so a gumcli that never exits hangs the load
// (and every test that loads a sample project) with no error. The runner has to give up and kill it.
public class GumCliRunnerTests
{
    [Fact]
    public async Task RunAsync_WhenTheProcessOutlivesTheTimeout_ShouldKillItAndReturnFalse()
    {
        GlueTestBootstrap.EnsureInitialized();

        var stopwatch = Stopwatch.StartNew();

        // ping -n 60 runs for about a minute.
        var succeeded = await GumCliRunner.RunAsync(
            "cmd.exe", "/c ping -n 60 127.0.0.1 > nul", "test process", TimeSpan.FromSeconds(1));

        succeeded.ShouldBeFalse();
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task RunAsync_WhenTheProcessExitsCleanly_ShouldReturnTrue()
    {
        GlueTestBootstrap.EnsureInitialized();

        var succeeded = await GumCliRunner.RunAsync(
            "cmd.exe", "/c echo hello", "test process", TimeSpan.FromSeconds(30));

        succeeded.ShouldBeTrue();
    }
}
