using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace GlueUnitTests.Projects;

// A template-created project used to get its engine from two places at once: FlatRedBallDesktopGLNet6
// from NuGet, and GumCore / Forms / SkiaInGum / StateInterpolation as loose DLLs shipped inside the
// template zip. The two halves versioned independently, and a project whose bundled GumCore predated
// a .gumx format change simply failed to load its own UI at runtime (GitHub issue #1951).
//
// Every one of those assemblies now ships as its own NuGet package. These tests pin the three things
// that have to stay true for that to hold, none of which the compiler checks:
//   * each project actually produces a package, with a <Version> the release tooling can stamp
//   * Engine.yml pushes all of them
//   * no package declares a dependency on something we don't publish
//
// All of it is read off disk, so no Glue bootstrap is needed and it stays in the fast unit run.
public class EnginePackagingTests
{
    // Package id -> csproj path relative to the checkout folder (the one holding FlatRedBall\ and Gum\).
    // This is the set a project created from a template must be able to restore. It is deliberately
    // spelled out here rather than derived: "which projects are published" is a decision, and the
    // point of the test is to catch a project quietly joining or leaving that set.
    private static readonly Dictionary<string, string> PublishedPackages = new()
    {
        // DesktopGL (serves both the .NET 6 and .NET 9 templates -- the projects multi-target)
        ["FlatRedBallDesktopGLNet6"] = @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj",
        ["FlatRedBall.StateInterpolation.DesktopNet6"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.DesktopGlNet6\StateInterpolation.DesktopNet6.csproj",
        ["FlatRedBall.GumCore.DesktopGlNet6"] = @"Gum\GumCore\GumCoreXnaPc\GumCore.DesktopGlNet6\GumCore.DesktopGlNet6.csproj",
        ["FlatRedBall.Forms.DesktopGlNet6"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\FlatRedBall.Forms.DesktopGlNet6.csproj",
        ["FlatRedBall.SkiaInGum"] = @"FlatRedBall\Engines\SkiaGum\SkiaInGum.csproj",

        // FNA
        ["FlatRedBall.FNA"] = @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.FNA\FlatRedBall.FNA.csproj",
        ["FlatRedBall.StateInterpolation.FNA"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.FNA\StateInterpolation.FNA.csproj",
        ["FlatRedBall.GumCore.FNA"] = @"Gum\GumCore\GumCoreXnaPc\GumCore.FNA\GumCore.FNA.csproj",
        ["FlatRedBall.Forms.FNA"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\FlatRedBall.Forms.FNA.csproj",

        // Web (Kni)
        ["FlatRedBallKniWeb"] = @"FlatRedBall\Engines\FlatRedBallXNA\KniWeb\FlatRedBallKniWeb.csproj",
        ["FlatRedBall.StateInterpolation.Kni.Web"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.Kni.Web\StateInterpolation.Kni.Web.csproj",
        ["FlatRedBall.GumCore.Kni.Web"] = @"Gum\GumCore\GumCoreXnaPc\GumCore.Kni.Web\GumCore.Kni.Web.csproj",
        ["FlatRedBall.Forms.Kni.Web"] = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Kni.Web\FlatRedBall.Forms.Kni.Web.csproj",
    };

    // What UpdateAssemblyVersions.ModifyCsprojAssemblyInfoVersion looks for when stamping the release
    // version. A packable project whose <Version> this doesn't match ships as 1.0.0 forever.
    private static readonly Regex StampableVersion = new(@"<Version>[0-9]*.[0-9]*.[0-9]*.[0-9]*</Version>");

    [Fact]
    public void EveryPublishedProject_ShouldBePackable_AndCarryAStampableVersion()
    {
        foreach (var (packageId, relativePath) in PublishedPackages)
        {
            var csproj = LoadCsproj(relativePath);

            string.Equals(GetProperty(csproj, "GeneratePackageOnBuild"), "True", StringComparison.OrdinalIgnoreCase)
                .ShouldBeTrue($"{packageId} is referenced by a template but its project does not produce a package.");

            var declaredId = GetProperty(csproj, "PackageId") ?? Path.GetFileNameWithoutExtension(relativePath);
            declaredId.ShouldBe(packageId,
                $"{relativePath} would publish as '{declaredId}', not '{packageId}'.");

            var version = GetProperty(csproj, "Version");
            version.ShouldNotBeNull($"{packageId} has no <Version> for the release to stamp.");
            StampableVersion.IsMatch($"<Version>{version}</Version>").ShouldBeTrue(
                $"{packageId}'s <Version>{version}</Version> does not match the pattern " +
                "UpdateAssemblyVersions rewrites, so a release would leave it unchanged.");
        }
    }

    [Fact]
    public void EveryPublishedProject_ShouldBePushedByEngineYml()
    {
        var workflow = File.ReadAllText(Path.Combine(FindFrbRoot(), ".github", "workflows", "Engine.yml"));

        // The publish step lists output folders, one quoted path per line, and commented-out entries
        // are the disabled mobile platforms -- those must not count as published.
        var publishedFolders = Regex.Matches(workflow, @"^\s*'([^']+bin\\Debug)',?\s*$", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        publishedFolders.ShouldNotBeEmpty("Could not find the package list in Engine.yml's publish step.");

        foreach (var (packageId, relativePath) in PublishedPackages)
        {
            var expectedFolder = Path.GetDirectoryName(relativePath) + @"\bin\Debug";
            publishedFolders.ShouldContain(expectedFolder,
                $"Engine.yml does not push {packageId}. A release would ship the rest of the set without it.");
        }
    }

    [Fact]
    public void EveryPublishedProject_ShouldOnlyDependOnThingsWeAlsoPublish()
    {
        foreach (var (packageId, relativePath) in PublishedPackages)
        {
            var csproj = LoadCsproj(relativePath);
            var projectDirectory = Path.GetDirectoryName(Path.Combine(FindCheckoutRoot(), relativePath))!;

            foreach (var reference in csproj.Descendants("ProjectReference"))
            {
                // Configuration-conditional references (SkiaInGum under the live-edit configurations)
                // are not present in the Debug build that gets packed.
                if (reference.Attribute("Condition") != null)
                {
                    continue;
                }

                // PrivateAssets="all" deliberately keeps a reference out of the package's dependency
                // list -- that is how FNA, which is not on nuget.org, is handled.
                if (string.Equals((string)reference.Attribute("PrivateAssets"), "all", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var referencedProject = Path.GetFullPath(Path.Combine(projectDirectory,
                    ((string)reference.Attribute("Include")).Replace('\\', Path.DirectorySeparatorChar)));

                var isPublished = PublishedPackages.Values.Any(published =>
                    string.Equals(Path.GetFullPath(Path.Combine(FindCheckoutRoot(), published)),
                        referencedProject, StringComparison.OrdinalIgnoreCase));

                isPublished.ShouldBeTrue(
                    $"{packageId} declares a package dependency on {Path.GetFileName(referencedProject)}, " +
                    "which is not published. Restoring the package would fail. Either publish it too, " +
                    "or mark the reference PrivateAssets=\"all\" so its output is embedded instead.");
            }
        }
    }

    // Template folder -> the package ids its .csproj must reference. A template that references an
    // engine assembly any other way is the bug this whole change exists to remove.
    private static readonly Dictionary<string, string[]> TemplatePackages = new()
    {
        ["FlatRedBallDesktopGlNet6Template"] = new[]
        {
            "FlatRedBallDesktopGLNet6", "FlatRedBall.Forms.DesktopGlNet6", "FlatRedBall.GumCore.DesktopGlNet6",
            "FlatRedBall.SkiaInGum", "FlatRedBall.StateInterpolation.DesktopNet6",
        },
        ["FlatRedBallDesktopGlMonoGameTemplate"] = new[]
        {
            "FlatRedBallDesktopGLNet6", "FlatRedBall.Forms.DesktopGlNet6", "FlatRedBall.GumCore.DesktopGlNet6",
            "FlatRedBall.SkiaInGum", "FlatRedBall.StateInterpolation.DesktopNet6",
        },
        ["FlatRedBallWebTemplate"] = new[]
        {
            "FlatRedBallKniWeb", "FlatRedBall.Forms.Kni.Web", "FlatRedBall.GumCore.Kni.Web",
            "FlatRedBall.StateInterpolation.Kni.Web",
        },
    };

    [Fact]
    public void ConvertedTemplates_ShouldTakeEveryEngineAssemblyFromNuGet()
    {
        foreach (var (templateName, expectedPackages) in TemplatePackages)
        {
            var templateDirectory = Path.Combine(FindFrbRoot(), "Templates", templateName, templateName);
            var csproj = XDocument.Load(Path.Combine(templateDirectory, templateName + ".csproj"));

            var referencedPackages = csproj.Descendants("PackageReference")
                .Select(reference => (string)reference.Attribute("Include"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var package in expectedPackages)
            {
                referencedPackages.ShouldContain(package,
                    $"{templateName} does not reference {package}, so a project created from it would " +
                    "be missing part of the engine or resolve it some other way.");
            }

            // A HintPath into Libraries is exactly how the engine used to arrive half-versioned.
            csproj.Descendants("HintPath")
                .Select(hintPath => hintPath.Value)
                .Any(hintPath => hintPath.Contains("Libraries", StringComparison.OrdinalIgnoreCase))
                .ShouldBeFalse($"{templateName} still references an assembly out of a Libraries folder.");

            Directory.Exists(Path.Combine(templateDirectory, "Libraries")).ShouldBeFalse(
                $"{templateName} still ships a Libraries folder, which will go stale against its packages.");
        }
    }

    [Fact]
    public void ConvertedTemplates_ShouldPinOneVersionAcrossTheWholeEngine()
    {
        foreach (var (templateName, expectedPackages) in TemplatePackages)
        {
            var csprojPath = Path.Combine(FindFrbRoot(), "Templates", templateName, templateName, templateName + ".csproj");
            var csproj = XDocument.Load(csprojPath);

            // Mixed versions here would reintroduce the original bug through the front door.
            var engineVersions = csproj.Descendants("PackageReference")
                .Where(reference => expectedPackages.Contains((string)reference.Attribute("Include"), StringComparer.OrdinalIgnoreCase))
                .Select(reference => (string)reference.Attribute("Version"))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            engineVersions.Count.ShouldBe(1,
                $"{templateName} pins more than one engine version: {string.Join(", ", engineVersions)}. " +
                "Every engine package a project restores has to be the same build.");
        }
    }

    private static XDocument LoadCsproj(string relativePath)
    {
        var fullPath = Path.Combine(FindCheckoutRoot(), relativePath);
        File.Exists(fullPath).ShouldBeTrue($"Expected a project at {fullPath}");
        return XDocument.Load(fullPath);
    }

    private static string GetProperty(XDocument csproj, string name) =>
        csproj.Descendants(name).FirstOrDefault(element => element.Parent?.Name == "PropertyGroup")?.Value;

    // The folder holding both the FlatRedBall and Gum checkouts. Every path above is relative to it,
    // matching how BuildServerUploaderConsole's AllData addresses the same files.
    private static string FindCheckoutRoot()
    {
        var checkoutRoot = Directory.GetParent(FindFrbRoot())!.FullName;
        Directory.Exists(Path.Combine(checkoutRoot, "Gum")).ShouldBeTrue(
            $"Expected a sibling Gum checkout at {Path.Combine(checkoutRoot, "Gum")}. " +
            "GumCore is built from Gum's source but published by FlatRedBall.");
        return checkoutRoot;
    }

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
