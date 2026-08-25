using GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;
using Shouldly;
using System.IO;

namespace GlueUnitTests.AboutPlugin;

public class DailyBuildUpdateLauncherTests
{
    [Fact]
    public void DailyBuildUpdateDiagnostics_ShouldPersistAnEntry()
    {
        var logPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "updater.log");

        try
        {
            DailyBuildUpdateDiagnostics.Append(logPath, "Starting updater helper");

            File.ReadAllText(logPath).ShouldContain("Starting updater helper");
        }
        finally
        {
            var directory = Path.GetDirectoryName(logPath)!;
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

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
        startInfo.WorkingDirectory.ShouldBe(Path.GetDirectoryName(@"C:\Glue Daily"));
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

    [Fact]
    public void CreateStartInfo_ShouldWriteLockDiagnosticsWhenReplacementFails()
    {
        var startInfo = DailyBuildUpdateLauncher.CreateStartInfo(
            glueProcessId: 42,
            installDirectory: @"C:\Glue Daily",
            stagedDirectory: @"C:\Glue Daily.updating",
            applicationPath: @"C:\Glue Daily\GlueFormsCore.exe");

        var script = startInfo.ArgumentList.Last();
        script.ShouldContain("GlueDailyBuildUpdate.log");
        script.ShouldContain("function Write-UpdateLog");
        script.ShouldContain("function Get-LockingProcesses");
        script.ShouldContain("Restart Manager lock owners");
        script.ShouldContain("Remove attempt $attempt failed");
        script.ShouldContain("Diagnostics: $logPath");
    }

    [Fact]
    public void CreateStartInfo_ShouldCatchLogInitializationFailures()
    {
        var startInfo = DailyBuildUpdateLauncher.CreateStartInfo(
            glueProcessId: 42,
            installDirectory: @"C:\Glue Daily",
            stagedDirectory: @"C:\Glue Daily.updating",
            applicationPath: @"C:\Glue Daily\GlueFormsCore.exe");

        var script = startInfo.ArgumentList.Last();
        script.IndexOf("try", StringComparison.Ordinal).ShouldBeLessThan(
            script.IndexOf("New-Item -ItemType Directory -Path $logDirectory -Force", StringComparison.Ordinal));
    }
}
