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
            "$stagedApplicationPath = Join-Path -Path $stagedDirectory -ChildPath 'GlueFormsCore.exe'",
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
            "    if (Test-Path -LiteralPath $stagedApplicationPath)",
            "    {",
            "        $restartPath = $stagedApplicationPath",
            "        $title = 'Glue update installed'",
            "        $message = \"Glue could not replace the existing installation because it is in use. The updated daily build will start from its staged folder.`n`n$($_.Exception.Message)\"",
            "        $image = [System.Windows.MessageBoxImage]::Information",
            "    }",
            "    else",
            "    {",
            "        $restartPath = $applicationPath",
            "        $title = 'Glue update failed'",
            "        $message = \"Glue could not install the daily build. Glue will restart without updating.`n`n$($_.Exception.Message)\"",
            "        $image = [System.Windows.MessageBoxImage]::Error",
            "    }",
            "    [System.Windows.MessageBox]::Show(",
            "        $message,",
            "        $title,",
            "        [System.Windows.MessageBoxButton]::OK,",
            "        $image) | Out-Null",
            string.Empty,
            "    if (Test-Path -LiteralPath $restartPath)",
            "    {",
            "        Start-Process -FilePath $restartPath",
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
