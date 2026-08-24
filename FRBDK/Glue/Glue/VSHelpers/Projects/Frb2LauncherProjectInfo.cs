using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// Where an FRB2 launcher project (e.g. Desktop) puts its build output, computed without an
    /// MSBuild evaluation - the launcher is never loaded into Glue as a <see cref="ProjectBase"/>,
    /// so there is no <see cref="VisualStudioProject"/> to ask.
    /// </summary>
    public readonly struct Frb2LauncherOutputInfo
    {
        public Frb2LauncherOutputInfo(string executableName, string exeLocation)
        {
            ExecutableName = executableName;
            ExeLocation = exeLocation;
        }

        public string ExecutableName { get; }
        public string ExeLocation { get; }
    }

    /// <summary>
    /// Reads an FRB2 launcher's own output directory and executable name straight from its
    /// .csproj's literal &lt;TargetFramework&gt;/&lt;AssemblyName&gt; elements, the same way
    /// <see cref="Frb2ProjectDetector"/> avoids a real MSBuild evaluation.
    /// </summary>
    public static class Frb2LauncherProjectInfo
    {
        /// <summary>
        /// Null when the launcher can't be read, is multi-targeted (&lt;TargetFrameworks&gt;), or
        /// customizes &lt;OutputPath&gt; - guessing wrong there would send Runner looking in the
        /// wrong place instead of correctly reporting "could not find game .exe".
        /// </summary>
        public static Frb2LauncherOutputInfo? TryGet(string launcherCsprojPath, string configuration)
        {
            if (string.IsNullOrEmpty(launcherCsprojPath) || !System.IO.File.Exists(launcherCsprojPath))
            {
                return null;
            }

            var properties = ReadTopLevelProperties(launcherCsprojPath);

            if (properties == null ||
                properties.ContainsKey("TargetFrameworks") ||
                properties.ContainsKey("OutputPath") ||
                !properties.TryGetValue("TargetFramework", out var targetFramework) ||
                string.IsNullOrEmpty(targetFramework))
            {
                return null;
            }

            var executableName = properties.TryGetValue("AssemblyName", out var assemblyName) && !string.IsNullOrEmpty(assemblyName)
                ? assemblyName
                : FileManager.RemoveExtension(FileManager.RemovePath(launcherCsprojPath));

            var directory = FileManager.GetDirectory(launcherCsprojPath);
            var outputDirectory = $"{directory}bin/{configuration}/{targetFramework}/";
            var exeLocation = outputDirectory + executableName + ".exe";

            return new Frb2LauncherOutputInfo(executableName, exeLocation);
        }

        // Later PropertyGroups win, matching MSBuild's own document-order evaluation - a project that
        // sets the same property twice means the second one.
        static Dictionary<string, string> ReadTopLevelProperties(string csprojPath)
        {
            try
            {
                return XDocument.Load(csprojPath)
                    .Descendants()
                    .Where(element => element.Parent?.Name.LocalName == "PropertyGroup")
                    .GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
