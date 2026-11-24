using System;
using System.Threading.Tasks;
using GumPlugin.Managers;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

public class GumCliRunnerProcessTests
{
    // gumcli fonts prints a warning per element it can't load. Once stderr holds more than a pipe buffer,
    // a runner that drains stdout first leaves gumcli blocked writing stderr while it waits for stdout to
    // end, and Glue hangs waiting on the fonts task.
    [Fact]
    public async Task RunProcessAsync_ShouldFinish_WhenTheProcessWritesMoreThanAPipeBufferToStderr()
    {
        var run = GumCliRunner.RunProcessAsync(
            "powershell.exe",
            "-NoProfile -Command \"[Console]::Error.Write('x' * 200000); [Console]::Out.Write('done')\"");

        var finished = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30)));

        finished.ShouldBe(run, "RunProcessAsync deadlocked on a full stderr pipe");
        var result = await run;
        result.ExitCode.ShouldBe(0);
        result.Output.ShouldBe("done");
        result.Error.Length.ShouldBe(200000);
    }
}
