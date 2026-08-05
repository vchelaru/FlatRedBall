using System;
using System.IO;
using GlueUnitTests.TestSupport;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// Issue #1967 bug #2 - nothing stopped a v3 gumx project from generating FilledStrokedRectangle
// codegen against a referenced GumCore.*.dll that predates that type (a stale, non-source-linked
// binary reference), producing a runtime NRE instead of a clear Glue-time error. This pins the
// reader that makes the check possible: it reads the GumSyntaxVersion assembly attribute (see the
// sibling Gum repo's GumDataTypes/GumSyntaxVersionAttribute.cs, bumped to 4 for FilledStrokedRectangle)
// off a referenced runtime DLL without loading it (a real GumCore.*.dll drags in MonoGame/XNA native
// deps Glue has no business loading just to read one attribute).
public class GumRuntimeSyntaxVersionReaderTests
{
    [Fact]
    public void ReadVersion_NonexistentFile_ReturnsNull()
    {
        GumRuntimeSyntaxVersionReader.ReadVersion(@"C:\this\path\does\not\exist.dll").ShouldBeNull();
    }

    [Fact]
    public void ReadVersion_NullOrEmptyPath_ReturnsNull()
    {
        GumRuntimeSyntaxVersionReader.ReadVersion(null).ShouldBeNull();
        GumRuntimeSyntaxVersionReader.ReadVersion("").ShouldBeNull();
    }

    [Fact]
    public void ReadVersion_RealAssemblyWithoutTheAttribute_ReturnsNull()
    {
        // A real, valid PE/metadata assembly (proves this doesn't crash on real-world DLLs) that
        // simply never declares GumSyntaxVersion - the "absent attribute" contract the attribute's
        // own doc comment calls out (assume pre-unification / too old).
        var corlibPath = typeof(object).Assembly.Location;
        File.Exists(corlibPath).ShouldBeTrue();

        GumRuntimeSyntaxVersionReader.ReadVersion(corlibPath).ShouldBeNull();
    }

    [Fact]
    public void TryFindHintPath_ParsesReferenceHintPath()
    {
        var csproj = """
            <Project>
              <ItemGroup>
                <Reference Include="GumCore.DesktopGlNet6">
                  <HintPath>Libraries\DesktopGl\Debug\GumCore.DesktopGlNet6.dll</HintPath>
                </Reference>
              </ItemGroup>
            </Project>
            """;

        var result = GumRuntimeSyntaxVersionReader.TryFindHintPath(
            csproj, @"C:\MyProject", "GumCore.DesktopGlNet6");

        result.ShouldBe(Path.GetFullPath(Path.Combine(@"C:\MyProject", @"Libraries\DesktopGl\Debug\GumCore.DesktopGlNet6.dll")));
    }

    [Fact]
    public void TryFindHintPath_NoMatchingReference_ReturnsNull()
    {
        var csproj = "<Project><ItemGroup></ItemGroup></Project>";

        GumRuntimeSyntaxVersionReader
            .TryFindHintPath(csproj, @"C:\MyProject", "GumCore.DesktopGlNet6")
            .ShouldBeNull();
    }

    // Issue #1967 bug #2, real end-to-end pin: builds the actual GumCore.DesktopGlNet6.csproj from
    // the sibling Gum repo (same real-compile technique GumRuntimeMemberContractTests uses) and
    // reads the freshly-built DLL's GumSyntaxVersion, proving the Gum-side attribute wiring AND this
    // reader agree on the real value - not a hand-authored fixture that could silently drift from
    // what Gum actually ships.
    [Trait("Category", "BuildSmoke")]
    [Fact]
    public void ReadVersion_RealBuiltGumCoreDll_Returns4()
    {
        var repoRoot = FindRepoRoot();
        var gumRepoRoot = Path.GetFullPath(Path.Combine(repoRoot, "..", "Gum"));
        var csprojPath = Path.Combine(gumRepoRoot, "GumCore", "GumCoreXnaPc", "GumCore.DesktopGlNet6", "GumCore.DesktopGlNet6.csproj");
        File.Exists(csprojPath).ShouldBeTrue($"Expected the Gum sibling repo's GumCore.DesktopGlNet6.csproj at {csprojPath}");

        var (exitCode, output) = RunDotnetBuild(csprojPath);
        exitCode.ShouldBe(0, $"Could not build GumCore.DesktopGlNet6.csproj:\n{output}");

        var dllPath = Path.Combine(gumRepoRoot, "GumCore", "GumCoreXnaPc", "GumCore.DesktopGlNet6", "bin", "Debug", "net6.0", "GumCore.DesktopGlNet6.dll");
        File.Exists(dllPath).ShouldBeTrue($"Expected a built assembly at {dllPath}");

        GumRuntimeSyntaxVersionReader.ReadVersion(dllPath).ShouldBe(4);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Engines", "SkiaGum")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the FlatRedBall repo root above " + AppContext.BaseDirectory);
    }

    private static (int exitCode, string output) RunDotnetBuild(string projectPath) =>
        NestedDotnetCli.Run($"build \"{projectPath}\" -c Debug -f net6.0");
}
