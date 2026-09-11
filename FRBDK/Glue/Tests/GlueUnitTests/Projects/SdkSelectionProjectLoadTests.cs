using System;
using System.Collections;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Runs Glue.exe's real SDK discovery (dotnet --list-sdks -> MsBuildSdkSelector -> MSBUILD_EXE_PATH) against
/// whatever SDKs this machine actually has, then evaluates checked-in projects through the same
/// <see cref="ProjectCreator"/> call File > Load Project makes. Which SDK Glue picks depends only on what
/// is installed, so CI runs this under several installed-SDK sets (the sdk-matrix job in pr-tests.yml);
/// a machine where Glue picks an SDK too old for the project fails here, naming the SDK, instead of in
/// the user's output window.
///
/// Its own category, run in its own process: the engine reads MSBUILD_EXE_PATH once, on first use, so a
/// test host that has already evaluated a project (SdkStyleProjectLoadTests inherits the var from the
/// `dotnet test` parent) cannot be re-pointed.
/// </summary>
[Trait("Category", "SdkMatrix")]
public class SdkSelectionProjectLoadTests
{
    // The parent `dotnet test` leaks its own MSBuild location into the test host through these. Glue.exe
    // launched from Explorer has none of them, and with them set the SDK resolver ignores MSBUILD_EXE_PATH
    // and reads the CLI's SDK instead, so the selection under test would be the CLI's, not Glue's.
    static readonly string[] LeakedFromDotnetCli = { "MSBUILD_EXE_PATH", "MSBuildSDKsPath", "MSBuildExtensionsPath" };

    public SdkSelectionProjectLoadTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Theory]
    [InlineData("Samples/EditorTest1/EditorTest1/EditorTest1.csproj")]  // Microsoft.NET.Sdk
    [InlineData("Samples/BeefballWeb/BeefballWeb/BeefballWeb.csproj")]  // Microsoft.NET.Sdk.BlazorWebAssembly
    public void CreateProject_EvaluatesWithTheSdkGlueSelects(string relativeCsproj)
    {
        var saved = LeakedFromDotnetCli.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in LeakedFromDotnetCli)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            Glue.MainGlueWindow.SetMsBuildEnvironmentVariable();
            var selected = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");

            var csproj = Path.Combine(GoldProject.FindRepoRoot(), relativeCsproj);

            try
            {
                var result = ProjectCreator.CreateProject(csproj);
                Assert.NotNull(result.Project);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Glue selected MSBUILD_EXE_PATH={selected ?? "(none)"} (engine {MsBuildSdkSelector.EngineVersion}) " +
                    $"and could not load {relativeCsproj}\n" +
                    $"MSBuild/dotnet environment:\n{MsBuildEnvironment()}", e);
            }
        }
        finally
        {
            foreach (var pair in saved)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }

    static string MsBuildEnvironment() => string.Join("\n",
        Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .Select(kv => (Key: kv.Key.ToString(), Value: kv.Value))
            .Where(kv => kv.Key.Contains("MSBUILD", StringComparison.OrdinalIgnoreCase) ||
                         kv.Key.Contains("DOTNET", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key)
            .Select(kv => $"  {kv.Key}={kv.Value}"));
}
