using System;
using System.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Builds a real, MSBuild-backed <see cref="VisualStudioProject"/> for tests, working around the
/// blocker documented in REFACTORING.md's "Known Areas Needing Improvement": VisualStudioProject's
/// constructor requires a live Microsoft.Build.Evaluation.Project and dereferences it immediately
/// (TargetFrameworkVersion, RootNamespace), so it can't be mocked/faked - only a real Project will do.
///
/// Loading a real *SDK-style* .csproj (the kind Glue actually ships) needs MSBuildLocator/SDK
/// resolution that only Glue.exe's own startup registers. But a bare, non-SDK-style project file (no
/// `Sdk="..."` attribute, no imports) evaluates with zero SDK/toolset resolution, so it constructs
/// cleanly in a plain xunit test host. This is a real VisualStudioProject with a real backing MSBuild
/// project - not a mock - just a minimal one.
/// </summary>
internal static class TestVisualStudioProjectFactory
{
    /// <summary>
    /// Creates a real ClassLibraryProject (the lightest concrete VisualStudioProject subclass) backed by
    /// a freshly-written, minimal .csproj in a new temp directory. Callers own cleanup of the returned
    /// directory.
    /// </summary>
    public static ClassLibraryProject CreateInNewTempDirectory(out string directory, string projectName = "TestProject")
    {
        directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_" + Guid.NewGuid());
        Directory.CreateDirectory(directory);

        var csprojPath = Path.Combine(directory, projectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>
</Project>");

        var project = new Project(csprojPath, null, null, new ProjectCollection());
        return new ClassLibraryProject(project);
    }
}
