using GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;
using Shouldly;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GlueUnitTests.AboutPlugin;

public class DailyBuildUpdateLauncherTests
{
    [Fact]
    public void GetLatestVersion_ShouldUseTheUtcBuildDateRegardlessOfTheViewerTimezone()
    {
        var utcBuildTime = new DateTimeOffset(2026, 8, 26, 16, 0, 0, TimeSpan.Zero);
        var result = DailyBuildVersionLogic.GetLatestVersion(utcBuildTime);

        result.ShouldBe(new Version(2026, 8, 26));
    }

    [Fact]
    public void DailyBuildDownloadButton_ShouldBeHiddenWhenTheUtcDailyBuildMatchesTheInstalledVersion()
    {
        var viewModel = new AboutViewModel
        {
            Version = new Version(2026, 8, 26),
            LatestDailyBuildVersion = DailyBuildVersionLogic.GetLatestVersion(
                new DateTimeOffset(2026, 8, 26, 16, 0, 0, TimeSpan.Zero))
        };

        viewModel.DailyBuildDownloadButtonVisibility.ShouldBe(Visibility.Collapsed);
    }

    [Fact]
    public void GetLogOpenTarget_ShouldUseTheLogWhenItExists()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "FlatRedBall", "GlueDailyBuildUpdate.log");

        var target = DailyBuildUpdateDiagnostics.GetLogOpenTarget(logPath, _ => true);

        target.ShouldBe(logPath);
    }

    [Fact]
    public void GetLogOpenTarget_ShouldUseTheLogDirectoryWhenNoLogExists()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "FlatRedBall", "GlueDailyBuildUpdate.log");

        var target = DailyBuildUpdateDiagnostics.GetLogOpenTarget(logPath, _ => false);

        target.ShouldBe(Path.GetDirectoryName(logPath));
    }

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
    public void DailyBuildUpdateDiagnostics_ShouldRotateAnOversizedLog()
    {
        var logPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "updater.log");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath, new string('x', 1_048_577));

            DailyBuildUpdateDiagnostics.Append(logPath, "New update attempt");

            File.ReadAllText(logPath).ShouldContain("New update attempt");
            new FileInfo(logPath).Length.ShouldBeLessThan(1_024);
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
    public void CreateStartInfo_ShouldUseTheInstallDirectoryParentWhenThePathEndsInASlash()
    {
        var startInfo = DailyBuildUpdateLauncher.CreateStartInfo(
            glueProcessId: 42,
            installDirectory: @"C:\Glue\Debug\",
            stagedDirectory: @"C:\Glue\Debug.updating",
            applicationPath: @"C:\Glue\Debug\GlueFormsCore.exe");

        startInfo.WorkingDirectory.ShouldBe(@"C:\Glue");
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
        script.ShouldContain("Waiting for Glue process to exit");
        script.ShouldContain("Glue process has exited");
        script.ShouldContain("Starting remove attempt $attempt");
        script.ShouldContain("Remove attempt $attempt failed");
        script.ShouldContain("Remove attempt $attempt succeeded");
        script.ShouldContain("Starting move attempt $attempt");
        script.ShouldContain("Move attempt $attempt succeeded");
        script.ShouldContain("Starting replacement application");
        script.ShouldContain("Replacement application started");
        script.ShouldContain("Update helper reached fallback");
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

    [Fact]
    public void CreateStartInfo_ShouldCreateAParsablePowerShellScript()
    {
        var script = DailyBuildUpdateLauncher.CreateStartInfo(
            glueProcessId: 42,
            installDirectory: @"C:\Glue Daily",
            stagedDirectory: @"C:\Glue Daily.updating",
            applicationPath: @"C:\Glue Daily\GlueFormsCore.exe").ArgumentList.Last();

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var scriptPath = Path.Combine(temporaryDirectory, "DailyBuildUpdate.ps1");

        try
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(scriptPath, script);

            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(
                $"$ErrorActionPreference = 'Stop'; [ScriptBlock]::Create([IO.File]::ReadAllText('{scriptPath.Replace("'", "''")}')) | Out-Null");

            using var process = Process.Start(startInfo)!;
            process.WaitForExit(10_000).ShouldBeTrue();
            process.ExitCode.ShouldBe(0);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }
}
