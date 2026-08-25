using GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;
using Shouldly;

namespace GlueUnitTests.AboutPlugin;

public class DailyBuildUpdateLauncherTests
{
    [Fact]
    public void CreateStartInfo_ShouldWaitForGlueAndAbortTheInstallWhenAFileRemainsLocked()
    {
        var startInfo = DailyBuildUpdateLauncher.CreateStartInfo(
            glueProcessId: 42,
            installDirectory: @"C:\Glue Daily",
            stagedDirectory: @"C:\Glue Daily.updating",
            applicationPath: @"C:\Glue Daily\GlueFormsCore.exe");

        startInfo.FileName.ShouldBe("powershell.exe");
        startInfo.CreateNoWindow.ShouldBeTrue();
        startInfo.ArgumentList.ShouldContain("-Command");

        var script = startInfo.ArgumentList.Last();
        script.ShouldContain("Wait-Process -Id 42");
        script.ShouldContain("Remove-Item -LiteralPath $installDirectory -Recurse -Force -ErrorAction Stop");
        script.ShouldContain("Move-Item -LiteralPath $stagedDirectory -Destination $installDirectory -ErrorAction Stop");
        script.ShouldContain("Start-Process -FilePath $applicationPath");
        script.ShouldContain("$stagedApplicationPath = Join-Path -Path $stagedDirectory -ChildPath 'GlueFormsCore.exe'");
        script.ShouldContain("$restartPath = $stagedApplicationPath");
        script.ShouldContain("$title = 'Glue update installed'");
        script.ShouldContain("Start-Process -FilePath $restartPath");
        script.ShouldContain("catch");
    }
}
