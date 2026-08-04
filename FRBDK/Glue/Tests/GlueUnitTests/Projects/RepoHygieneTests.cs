using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OfficialPlugins.FrbSourcePlugin.Managers;
using PluginTestbed.GlobalContentManagerPlugins;
using Shouldly;

namespace GlueUnitTests.Projects;

// Structural checks on the repo itself rather than on any one piece of code. Everything a compiler
// or an MSBuild sweep cannot see: a .sln entry pointing at a project that was deleted years ago, a
// ProjectReference into a folder that no longer exists, a project still pinned to .NET Framework.
// None of these break `dotnet build` of the live solutions -- MSBuild silently skips a missing .sln
// entry, and an unreferenced legacy csproj never gets evaluated at all -- so they accumulate.
//
// Read off disk like EnginePackagingTests, so no Glue bootstrap is needed and this stays in the
// fast unit run.
public class RepoHygieneTests
{
    // Folders whose contents are build output, tooling state, or third-party submodule source.
    private static readonly string[] IgnoredFolders = { "bin", "obj", ".git", ".vs", "packages", "node_modules" };

    [Fact]
    public void NoProjectShouldTargetBelowNet6()
    {
        // Repo-wide, with no exclusion list: as of this test's introduction every csproj in the
        // repo -- engine, editor, samples, templates and test projects alike -- is already net6.0
        // or above (or netstandard2.x). The point is to keep it that way, so anything that fails
        // here is a project that needs retargeting, not an exception to add.
        var offenders = new List<string>();

        foreach (var csproj in EnumerateProjectFiles("*.csproj"))
        {
            var document = XDocument.Load(csproj);

            // Legacy (pre-SDK) csprojs use TargetFrameworkVersion, which is .NET Framework by
            // definition. Their mere presence is the failure.
            var legacy = GetProperty(document, "TargetFrameworkVersion");
            if (legacy != null)
            {
                offenders.Add($"{Relative(csproj)} is a legacy csproj targeting {legacy}");
                continue;
            }

            var frameworks = (GetProperty(document, "TargetFrameworks") ?? GetProperty(document, "TargetFramework") ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(framework => framework.Trim())
                .Where(framework => framework.Length > 0)
                .ToList();

            if (frameworks.Count == 0)
            {
                offenders.Add($"{Relative(csproj)} declares no target framework");
                continue;
            }

            foreach (var framework in frameworks.Where(framework => !IsNet6OrAbove(framework)))
            {
                offenders.Add($"{Relative(csproj)} targets {framework}");
            }
        }

        offenders.ShouldBeEmpty("Every project in the repo must target net6.0 or above " +
            "(netstandard2.0/2.1 is also allowed for the plain libraries):" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void EverySolutionShouldOnlyReferenceProjectsThatExist()
    {
        // A .sln entry for a deleted project does not fail a build -- MSBuild skips it -- so these
        // survive indefinitely and make the solution unopenable in Visual Studio instead.
        var offenders = new List<string>();

        foreach (var solution in EnumerateProjectFiles("*.sln"))
        {
            var solutionDirectory = Path.GetDirectoryName(solution)!;

            foreach (var reference in ProjectEntriesIn(solution))
            {
                var referenced = Path.GetFullPath(Path.Combine(solutionDirectory, ToLocalPath(reference)));
                if (!File.Exists(referenced) && !IsInsideUncheckedOutSubmodule(referenced))
                {
                    offenders.Add($"{Relative(solution)} -> {reference}");
                }
            }
        }

        offenders.ShouldBeEmpty("These .sln files list projects that are not on disk:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void EveryProjectReferenceShouldResolve()
    {
        // Unlike a .sln entry, a broken ProjectReference does fail the build -- but only for the
        // project that declares it. Ones sitting in projects nothing builds stay broken forever.
        var offenders = new List<string>();

        foreach (var project in EnumerateProjectFiles("*.csproj")
            .Concat(EnumerateProjectFiles("*.shproj"))
            .Concat(EnumerateProjectFiles("*.projitems")))
        {
            var projectDirectory = Path.GetDirectoryName(project)!;

            foreach (var include in XDocument.Load(project).Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => (string)element.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include)))
            {
                var referenced = Path.GetFullPath(Path.Combine(projectDirectory, ToLocalPath(include)));
                if (!File.Exists(referenced) && !IsInsideUncheckedOutSubmodule(referenced))
                {
                    offenders.Add($"{Relative(project)} -> {include}");
                }
            }
        }

        offenders.ShouldBeEmpty("These ProjectReferences point at projects that are not on disk:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void LinkToSourceShouldOnlyReferenceProjectsThatExist()
    {
        // AddSourceManager's flavour lists are plain path strings, so nothing checks them until a
        // user actually runs "link to source" and the wizard fails on their machine. One flavour
        // named a FlatRedBall.Forms.iOS.csproj that had never existed in the repo.
        var manager = new AddSourceManager();
        var offenders = new List<string>();

        var flavours = new Dictionary<string, List<ProjectReference>>
        {
            [nameof(manager.SharedShprojReferences)] = manager.SharedShprojReferences,
            [nameof(manager.DesktopGlNet6)] = manager.DesktopGlNet6,
            [nameof(manager.DesktopFNA)] = manager.DesktopFNA,
            [nameof(manager.Web)] = manager.Web,
            [nameof(manager.AndroidNet8)] = manager.AndroidNet8,
            [nameof(manager.IosNet8)] = manager.IosNet8,
        };

        foreach (var (flavour, references) in flavours)
        {
            foreach (var reference in references)
            {
                var root = reference.ProjectRootType == FrbOrGum.Gum
                    ? Path.Combine(FindCheckoutRoot(), "Gum")
                    : FindFrbRoot();

                var project = Path.GetFullPath(Path.Combine(root, ToLocalPath(reference.RelativeProjectFilePath)));
                if (!File.Exists(project))
                {
                    offenders.Add($"{flavour} -> {reference.RelativeProjectFilePath}");
                }
            }
        }

        offenders.ShouldBeEmpty("AddSourceManager would add these projects to a user's solution, " +
            "but they are not on disk:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static bool IsNet6OrAbove(string framework)
    {
        // netstandard2.x is not a runtime and carries no .NET Framework baggage; the plain
        // libraries (Localization) use it so they can be consumed from anywhere.
        if (framework is "netstandard2.0" or "netstandard2.1")
        {
            return true;
        }

        // net6.0, net8.0-windows, net9.0-android, ...
        var match = Regex.Match(framework, @"^net(\d+)\.(\d+)(-[a-z0-9.]+)?$", RegexOptions.IgnoreCase);
        return match.Success && int.Parse(match.Groups[1].Value) >= 6;
    }

    private static readonly Regex SolutionProjectEntry =
        new(@"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""([^""]+)""", RegexOptions.Multiline);

    private static IEnumerable<string> ProjectEntriesIn(string solution) =>
        SolutionProjectEntry.Matches(File.ReadAllText(solution))
            .Select(match => match.Groups[1].Value)
            // Solution folders reuse the same syntax with a plain name in place of a path.
            .Where(path => Path.GetExtension(path).EndsWith("proj", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> EnumerateProjectFiles(string pattern) =>
        Directory.EnumerateFiles(FindFrbRoot(), pattern, SearchOption.AllDirectories)
            .Where(path => !IsIgnored(path));

    private static bool IsIgnored(string path) =>
        Relative(path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => IgnoredFolders.Contains(segment, StringComparer.OrdinalIgnoreCase))
        || SubmoduleDirectories.Value.Any(submodule =>
            path.StartsWith(submodule + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

    // Submodules (FNA) are not checked out by a plain `git clone`, and the PR workflow does not
    // fetch them either, so a reference into one is only checkable where someone ran
    // `git submodule update --init`.
    private static readonly Lazy<List<string>> SubmoduleDirectories = new(() =>
    {
        var gitmodules = Path.Combine(FindFrbRoot(), ".gitmodules");
        if (!File.Exists(gitmodules))
        {
            return new List<string>();
        }

        return Regex.Matches(File.ReadAllText(gitmodules), @"^\s*path\s*=\s*(.+)$", RegexOptions.Multiline)
            .Select(match => Path.GetFullPath(Path.Combine(FindFrbRoot(), ToLocalPath(match.Groups[1].Value.Trim()))))
            .ToList();
    });

    private static bool IsInsideUncheckedOutSubmodule(string path) =>
        SubmoduleDirectories.Value.Any(submodule =>
            path.StartsWith(submodule + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && (!Directory.Exists(submodule) || !Directory.EnumerateFileSystemEntries(submodule).Any()));

    private static string ToLocalPath(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    private static string Relative(string path) =>
        Path.GetRelativePath(FindFrbRoot(), path);

    private static string GetProperty(XDocument csproj, string name) =>
        csproj.Descendants().FirstOrDefault(element =>
            element.Name.LocalName == name && element.Parent?.Name.LocalName == "PropertyGroup")?.Value;

    // The folder holding both the FlatRedBall and Gum checkouts, matching EnginePackagingTests.
    private static string FindCheckoutRoot() => Directory.GetParent(FindFrbRoot())!.FullName;

    private static string FindFrbRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Templates")) &&
                File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "Engine.yml")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the FlatRedBall repo root above " + AppContext.BaseDirectory);
    }
}
