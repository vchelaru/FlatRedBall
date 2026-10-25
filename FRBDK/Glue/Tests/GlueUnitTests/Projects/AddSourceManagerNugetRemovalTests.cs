using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlueUnitTests.TestSupport;
using Microsoft.Build.Construction;
using OfficialPlugins.FrbSourcePlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

// GitHub issue #1738: linking an Android project to source left the FlatRedBallAndroid package beside
// the new ProjectReferences. The general condition is "a package id a template ships has no removal
// entry", so every platform template is checked, not just Android. A new template, or a new package id
// in an existing one, is picked up automatically.
public class AddSourceManagerNugetRemovalTests
{
    // The FRB-owned package ids all start with this (FlatRedBallAndroid, FlatRedBalliOS,
    // FlatRedBallDesktopGLNet6, FlatRedBallKniWeb, FlatRedBall.Forms.*, ...). Third-party packages
    // (MonoGame.*, nkast.*, SkiaSharp, ...) must survive linking.
    private const string FrbPackagePrefix = "FlatRedBall";

    public static IEnumerable<object[]> TemplateProjects() =>
        Directory.EnumerateDirectories(Path.Combine(RepoPaths.FrbRoot, "Templates"))
            .Select(directory => Path.Combine(directory, Path.GetFileName(directory), Path.GetFileName(directory) + ".csproj"))
            .Where(File.Exists)
            .Select(csproj => new object[] { Path.GetFileName(Path.GetDirectoryName(csproj)) });

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void RemoveNugetReferences_RemovesEveryFrbPackageFromTemplate(string templateName)
    {
        // Before the first MSBuild use in the process, including reading the template below: the
        // toolset is cached on first use and a later real-project load would fail without it.
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();

        var templateCsproj = Path.Combine(RepoPaths.FrbRoot, "Templates", templateName, templateName, templateName + ".csproj");
        var packages = ReadPackageReferences(templateCsproj);

        // The linked state: the template's own package references, plus the ProjectReference that
        // linking adds beside them.
        var items = string.Join(Environment.NewLine,
            packages.Select(item => $"<PackageReference Include=\"{item}\" Version=\"1.0.0\" />")
                .Append("<ProjectReference Include=\"..\\FlatRedBall\\FlatRedBall.csproj\" />"));

        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory, extraItemGroupXml: items);
        try
        {
            // Glue loads every project through Load, which indexes the items; RemoveItem only
            // removes items it finds in that index.
            project.Load(Path.Combine(directory, "TestProject.csproj"));

            AddSourceManager.RemoveNugetReferences(project);

            var remaining = project.EvaluatedItems
                .Where(item => item.ItemType == "PackageReference")
                .Select(item => item.EvaluatedInclude)
                .ToList();

            remaining.Where(id => id.StartsWith(FrbPackagePrefix, StringComparison.Ordinal)).ShouldBeEmpty(
                $"{templateName} ships these FlatRedBall packages, but linking to source does not remove them. " +
                "Add them to AddSourceManager.NugetPackagesRemovedOnLink.");

            // Distinct: the Web template lists some packages twice, once per target framework condition.
            remaining.Distinct().OrderBy(id => id).ShouldBe(
                packages.Where(id => !id.StartsWith(FrbPackagePrefix, StringComparison.Ordinal)).Distinct().OrderBy(id => id),
                "Third-party packages must be left alone.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Templates_ShipAtLeastOneFrbPackage_SoTheTheoryIsNotVacuous()
    {
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();

        TemplateProjects().Count().ShouldBeGreaterThanOrEqualTo(5);

        var withFrbPackage = TemplateProjects()
            .Select(row => (string)row[0])
            .Count(name => ReadPackageReferences(
                Path.Combine(RepoPaths.FrbRoot, "Templates", name, name, name + ".csproj"))
                .Any(id => id.StartsWith(FrbPackagePrefix, StringComparison.Ordinal)));

        withFrbPackage.ShouldBeGreaterThanOrEqualTo(4);
    }

    private static List<string> ReadPackageReferences(string csproj)
    {
        // Read as XML, not evaluated: the templates are SDK-style, which needs SDK resolution.
        var root = ProjectRootElement.Open(csproj, new Microsoft.Build.Evaluation.ProjectCollection(), preserveFormatting: false);
        return root.Items
            .Where(item => item.ItemType == "PackageReference")
            .Select(item => item.Include)
            .ToList();
    }
}
