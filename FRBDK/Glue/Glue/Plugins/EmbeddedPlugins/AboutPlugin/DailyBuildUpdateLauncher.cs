using System;
using System.Diagnostics;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;

internal static class DailyBuildUpdateLauncher
{
    const int MaximumFileOperationAttempts = 10;

    public static ProcessStartInfo CreateStartInfo(
        int glueProcessId,
        string installDirectory,
        string stagedDirectory,
        string applicationPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(installDirectory);
        ArgumentException.ThrowIfNullOrEmpty(stagedDirectory);
        ArgumentException.ThrowIfNullOrEmpty(applicationPath);

        var script = string.Join(Environment.NewLine, new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"$installDirectory = {ToPowerShellString(installDirectory)}",
            $"$stagedDirectory = {ToPowerShellString(stagedDirectory)}",
            $"$applicationPath = {ToPowerShellString(applicationPath)}",
            string.Empty,
            "try",
            "{",
            $"    $glueProcess = Get-Process -Id {glueProcessId} -ErrorAction SilentlyContinue",
            "    if ($null -ne $glueProcess)",
            "    {",
            $"        Wait-Process -Id {glueProcessId}",
            "    }",
            string.Empty,
            $"    for ($attempt = 1; $attempt -le {MaximumFileOperationAttempts}; $attempt++)",
            "    {",
            "        try",
            "        {",
            "            Remove-Item -LiteralPath $installDirectory -Recurse -Force -ErrorAction Stop",
            "            break",
            "        }",
            "        catch",
            "        {",
            $"            if ($attempt -eq {MaximumFileOperationAttempts})",
            "            {",
            "                throw",
            "            }",
            string.Empty,
            "            Start-Sleep -Seconds 1",
            "        }",
            "    }",
            string.Empty,
            $"    for ($attempt = 1; $attempt -le {MaximumFileOperationAttempts}; $attempt++)",
            "    {",
            "        try",
            "        {",
            "            Move-Item -LiteralPath $stagedDirectory -Destination $installDirectory -ErrorAction Stop",
            "            break",
            "        }",
            "        catch",
            "        {",
            $"            if ($attempt -eq {MaximumFileOperationAttempts})",
            "            {",
            "                throw",
            "            }",
            string.Empty,
            "            Start-Sleep -Seconds 1",
            "        }",
            "    }",
            string.Empty,
            "    Start-Process -FilePath $applicationPath",
            "}",
            "catch",
            "{",
            "    Add-Type -AssemblyName PresentationFramework",
            "    [System.Windows.MessageBox]::Show(",
            "        \"Glue could not install the daily build. Glue will restart without updating.`n`n$($_.Exception.Message)\",",
            "        'Glue update failed',",
            "        [System.Windows.MessageBoxButton]::OK,",
            "        [System.Windows.MessageBoxImage]::Error) | Out-Null",
            string.Empty,
            "    if (Test-Path -LiteralPath $applicationPath)",
            "    {",
            "        Start-Process -FilePath $applicationPath",
            "    }",
            "}"
        });

        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-WindowStyle");
        startInfo.ArgumentList.Add("Hidden");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);

        return startInfo;
    }

    static string ToPowerShellString(string value) => $"'{value.Replace("'", "''")}'";
}
