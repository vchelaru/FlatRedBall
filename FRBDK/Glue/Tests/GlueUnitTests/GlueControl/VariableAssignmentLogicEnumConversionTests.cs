using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;

namespace GlueUnitTests.GlueControlTests;

// Issue #1978: live-editing an enum-typed Gum variable (XOrigin, XUnits, ...) on a Gum object added
// directly to a screen throws InvalidOperationException, because VariableAssignmentLogic.ConvertStringToType
// only special-cased a hardcoded list of enum types and left Gum's own enums (RenderingLibrary.Graphics.
// HorizontalAlignment, Gum.Converters.GeneralUnitType) as unconverted strings.
//
// VariableAssignmentLogic.cs is embedded-only - EmbeddedCodeManager copies it into a game project's
// GlueControl/ folder at Glux-load time, and it's never linked into Glue.exe or GlueUnitTests directly, so
// no test can call it in-process without first producing that real generated output. This test does exactly
// what MainCompilerPlugin.HandleGluxLoaded does in production (EmbeddedCodeManager.EmbedAll +
// GlueCallsCodeGenerator.GenerateAll together), then compiles the real generated tree against the real
// engine/Gum assemblies and runs it - level 4 on the gum-codegen-test-rigor skill's verification ladder,
// since this is a runtime exception, not a compile error.
[Trait("Category", "BuildSmoke")]
[Collection(nameof(TaskManagerSequentialCollection))]
public class VariableAssignmentLogicEnumConversionTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly string _tempProjectDirectory;

    public VariableAssignmentLogicEnumConversionTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
        };
        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        TaskManager.SynchronousMode = _originalSynchronousMode;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public void LiveEditingGumEnumVariable_ShouldConvertToRealEnum_NotAnUnconvertedString()
    {
        // The exact production trigger (MainCompilerPlugin.HandleGluxLoaded runs both together):
        EmbeddedCodeManager.EmbedAll(fullyGenerate: true);
        GlueCallsCodeGenerator.GenerateAll();

        var generatedDirectory = Path.Combine(_tempProjectDirectory, "GlueControl");
        Directory.Exists(generatedDirectory).ShouldBeTrue(
            $"Expected EmbedAll to have written real generated files under {generatedDirectory}");
        Directory.Exists(Path.Combine(generatedDirectory, "Editing"))
            .ShouldBeTrue("Expected the Editing subfolder (VariableAssignmentLogic.Generated.cs) to exist");

        var repoRoot = FindRepoRoot();
        var scratchDirectory = Path.Combine(_tempProjectDirectory, "EnumConversionScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generatedDirectory);
        WriteProgram(scratchDirectory);

        var (exitCode, output) = NestedDotnetCli.Run($"run --project \"{scratchDirectory}\" -c Debug");

        exitCode.ShouldBe(0,
            "Live-editing a Gum enum variable (XOrigin/XUnits on a Gum object added directly to a screen) " +
            "did not convert cleanly to the real enum type:" + Environment.NewLine + output);
        output.ShouldContain("ALL_OK");
    }

    // Issue #2141: live-editing RepositionDirections on an AxisAlignedRectangle in Edit mode throws
    // InvalidOperationException. Root cause is the same class of bug as #1978, but on the other side of
    // ConvertStringToType's fallback: reflection-discovered variables (ExposedVariableManager.GetMemberVariables,
    // via property.PropertyType.Name) report Type as an unqualified short name like "RepositionDirections",
    // never "FlatRedBall.Math.Geometry.RepositionDirections". The generic enum fallback only matched a
    // namespace-qualified name (Type.GetType / assembly.GetType(fullName)), so short-named engine enums that
    // aren't hardcoded (unlike HorizontalAlignment/VerticalAlignment) were left as unconverted strings.
    [Fact]
    public void LiveEditingEngineEnumVariable_ShouldConvertShortEnumName_NotAnUnconvertedString()
    {
        EmbeddedCodeManager.EmbedAll(fullyGenerate: true);
        GlueCallsCodeGenerator.GenerateAll();

        var generatedDirectory = Path.Combine(_tempProjectDirectory, "GlueControl");
        var repoRoot = FindRepoRoot();
        var scratchDirectory = Path.Combine(_tempProjectDirectory, "EngineEnumConversionScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generatedDirectory);
        WriteEngineEnumProgram(scratchDirectory);

        var (exitCode, output) = NestedDotnetCli.Run($"run --project \"{scratchDirectory}\" -c Debug");

        exitCode.ShouldBe(0,
            "Live-editing RepositionDirections (an engine enum reported by its short, unqualified type " +
            "name) did not convert cleanly to the real enum type:" + Environment.NewLine + output);
        output.ShouldContain("ALL_OK");
    }

    private static void WriteScratchProject(string scratchDirectory, string repoRoot, string generatedDirectory)
    {
        Directory.CreateDirectory(scratchDirectory);

        var formsProject = Path.Combine(repoRoot, "Engines", "Forms", "FlatRedBall.Forms",
            "FlatRedBall.Forms.DesktopGlNet6", "FlatRedBall.Forms.DesktopGlNet6.csproj");
        File.Exists(formsProject).ShouldBeTrue($"Expected the engine Forms project at {formsProject}");

        var skiaProject = Path.Combine(repoRoot, "Engines", "SkiaGum", "SkiaInGum.csproj");
        var monoGameVersion = ReadNet6MonoGameVersion(skiaProject);

        // The real game project's own root namespace (TestVisualStudioProjectFactory always names it
        // "TestProject") - GlueControlCodeGenerator.GetEmbeddedStringContents bakes `using {ProjectNamespace};`
        // into every generated file's {CompilerDirectives} substitution, so something has to declare it.
        var generatedIncludePath = generatedDirectory.Replace("\\", "/");

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>disable</Nullable>
                <LangVersion>latest</LangVersion>
                <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
                <EnableDefaultCompileItems>true</EnableDefaultCompileItems>
                <!-- Real desktop-GL game projects (FlatRedBallDesktopGlMonoGameTemplate) define MONOGAME_381 -
                     ToEnum<T>'s Enum.TryParse branch in VariableAssignmentLogic.cs is gated behind it, so
                     without this define the existing FRB HorizontalAlignment fast path silently returns
                     default(T) instead of parsing, which is a fidelity gap in this scratch project, not a
                     real bug. -->
                <DefineConstants>MONOGAME_381</DefineConstants>
              </PropertyGroup>
              <ItemGroup>
                <!-- Only the files VariableAssignmentLogic.cs itself (the file under test) actually needs
                     to compile - not the rest of the ~30-file GlueControl closure EmbedAll writes (CommandReceiver,
                     InstanceLogic, CameraLogic, EntityViewingScreen, ...), which pull in a large amount of
                     unrelated per-project generated infrastructure (CameraSetup, GlobalContent, FactoryManager,
                     TileEntities) that has nothing to do with this bug. Those few remaining external symbols
                     VariableAssignmentLogic.cs itself references (CommandReceiver, GlueState, EditingManager)
                     are compile-only stubs below, matching real signatures. -->
                <Compile Include="{generatedIncludePath}/Editing/VariableAssignmentLogic.Generated.cs" />
                <Compile Include="{generatedIncludePath}/Dtos.Generated.cs" />
                <Compile Include="{generatedIncludePath}/Models/**/*.cs" />
                <Compile Include="{generatedIncludePath}/Editing/Managers/ObjectFinder.Generated.cs" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="{formsProject}" />
                <PackageReference Include="MonoGame.Framework.DesktopGL" Version="{monoGameVersion}" />
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(scratchDirectory, "EnumConversionScratch.csproj"), csproj);

        // Compile-only stand-ins for game-project-specific types the whole embedded closure needs to exist
        // in order to compile, but that this test's exercised code path (ConvertStringToType on enum
        // variables) never touches at runtime:
        //  - Performance: an empty using-only namespace.
        //  - CameraSetupData: normally generated per-project by CameraSetupCodeGenerator; SetCameraSetupDto
        //    just needs it to exist as a base class, no members are read.
        //  - TileCollisions/TileGraphics types: normally generated per-project by TileGraphicsPlugin (a
        //    second, parallel embedding mechanism to GlueControl's, with its own {CompilerDirectives}-style
        //    templates that can't be compiled raw). Only referenced from the tile-shape-collection editing
        //    branches of the embedded closure, which this test's enum-conversion assertions never reach -
        //    these exist purely to satisfy the compiler with matching shapes, not real behavior.
        File.WriteAllText(Path.Combine(scratchDirectory, "TestProjectNamespaceStub.cs"), """
            namespace TestProject
            {
                public class CameraSetupData { }
            }

            namespace TestProject.Performance { }

            namespace FlatRedBall.TileGraphics
            {
                public class LayeredTileMap { }

                public class MapDrawableBatch
                {
                    public MapDrawableBatch(int numberOfTiles, Microsoft.Xna.Framework.Graphics.Texture2D texture) { }
                    public Microsoft.Xna.Framework.Graphics.Texture2D Texture { get; set; }
                    public void MergeOntoThis(System.Collections.Generic.List<MapDrawableBatch> layers) { }
                }
            }

            namespace FlatRedBall.TileCollisions
            {
                public class TileShapeCollection : FlatRedBall.Math.Geometry.ShapeCollection
                {
                    public System.Collections.Generic.List<object> Rectangles { get; } = new();
                    public float GridSize { get; set; }
                    public float LeftSeedX { get; set; }
                    public float BottomSeedY { get; set; }
                    public FlatRedBall.Math.Axis SortAxis { get; set; }
                    public void AddCollisionAtWorld(float x, float y) { }
                }

                public static class TileShapeCollectionLayeredTileMapExtensions
                {
                    public static void AddMergedCollisionFromTilesWithType(
                        TileShapeCollection tileShapeCollection, FlatRedBall.TileGraphics.LayeredTileMap map, string typeName) { }

                    public static void AddCollisionFromTilesWithType(
                        TileShapeCollection tileShapeCollection, FlatRedBall.TileGraphics.LayeredTileMap map, string typeName, bool removeTiles) { }
                }
            }

            namespace FlatRedBall.Math.Collision
            {
                public class CollidableListVsTileShapeCollectionRelationship<T> { }
                public class CollidableVsTileShapeCollectionRelationship<T> { }
            }

            // Compile-only stand-ins (matching real signatures) for the sibling embedded types
            // VariableAssignmentLogic.cs references but this test deliberately doesn't compile in real -
            // CommandReceiver, InstanceLogic, and EditingManager are large files with their own big
            // dependency closures (socket dispatch, drag/selection input) unrelated to enum conversion.
            namespace GlueControl
            {
                public static class CommandReceiver
                {
                    public const string ProjectNamespace = "TestProject";
                    public static bool GetIfMatchesCurrentScreen(string elementNameGlue) => false;
                    public static bool DoTypesMatch(FlatRedBall.PositionedObject positionedObject, string qualifiedTypeName, System.Type possibleType = null) => false;
                    public static string GlueToGameElementName(string elementName) => elementName;
                    public static string GameElementTypeToGlueElement(string gameType) => gameType;
                }

                public class InstanceLogic
                {
                    public static InstanceLogic Self { get; } = new InstanceLogic();
                    public System.Collections.Generic.Dictionary<string, GlueControl.Models.GlueElement> CustomGlueElements { get; } = new();
                    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<GlueControl.Models.CustomVariable>> CustomVariablesAddedAtRuntime { get; } = new();
                    public System.Collections.Generic.List<object> ListsAddedAtRuntime { get; } = new();
                    public System.Collections.Generic.List<FlatRedBall.Math.Geometry.ShapeCollection> ShapeCollectionsAddedAtRuntime { get; } = new();
                    public System.Collections.Generic.List<FlatRedBall.Camera> CamerasAddedAtRuntime { get; } = new();
                    public FlatRedBall.Camera GetCameraByName(string objectName) => null;
                    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<GlueControl.Models.StateSaveCategory>> StatesAddedAtRuntime { get; } = new();
                }

                public class NameableWrapper : FlatRedBall.Utilities.INameable
                {
                    public string Name { get; set; }
                    public object ContainedObject { get; set; }
                }
            }

            namespace GlueControl.Runtime
            {
                public class DynamicEntity
                {
                    public string EditModeType { get; set; }
                }
            }

            namespace GlueControl.Screens
            {
                public class EntityViewingScreen : FlatRedBall.Screens.Screen
                {
                    public object CurrentEntity { get; set; }
                }
            }

            namespace TestProject
            {
                public static class GlobalContent
                {
                    public static object GetFile(string name) => null;
                }
            }

            namespace GlueControl.Editing
            {
                public class EditingManager
                {
                    public static EditingManager Self { get; } = new EditingManager();
                    public System.Collections.Generic.List<GlueControl.Models.NamedObjectSave> CurrentNamedObjects { get; } = new();
                    public void Select(GlueControl.Models.NamedObjectSave namedObject) { }
                    public bool GetIfShouldSuppressVariableAssignment(string variableName, FlatRedBall.Utilities.INameable targetInstance) => false;
                }
            }

            namespace GlueControl.Managers
            {
                public class GlueState
                {
                    public static GlueState Self { get; } = new GlueState();
                    public object CurrentElement { get; set; }
                }
            }
            """ + Environment.NewLine);
    }

    private static void WriteProgram(string scratchDirectory)
    {
        File.WriteAllText(Path.Combine(scratchDirectory, "Program.cs"), """
            using System;
            using GlueControl.Editing;

            int failureCount = 0;

            void Check(string label, object actual, object expected)
            {
                if (Equals(actual, expected))
                {
                    Console.WriteLine($"OK: {label} -> {actual} ({actual?.GetType()})");
                }
                else
                {
                    Console.WriteLine($"FAIL: {label} - expected {expected} ({expected?.GetType()}), got {actual} ({actual?.GetType()})");
                    failureCount++;
                }
            }

            try
            {
                // Issue #1978 repro case 1: Gum's own HorizontalAlignment (not FRB's), as seen on a Gum
                // object's XOrigin. Pre-fix this returned the raw string "Center" unconverted.
                var gumHorizontal = VariableAssignmentLogic.ConvertStringToType(
                    "RenderingLibrary.Graphics.HorizontalAlignment", "Center", isState: false, out _);
                Check("Gum RenderingLibrary.Graphics.HorizontalAlignment (XOrigin)",
                    gumHorizontal, RenderingLibrary.Graphics.HorizontalAlignment.Center);

                // Issue #1978 repro case 2: Gum.Converters.GeneralUnitType, as seen on a Gum object's XUnits.
                var gumUnitType = VariableAssignmentLogic.ConvertStringToType(
                    "Gum.Converters.GeneralUnitType", "PercentageOfFile", isState: false, out _);
                Check("Gum.Converters.GeneralUnitType (XUnits)",
                    gumUnitType, Gum.Converters.GeneralUnitType.PercentageOfFile);

                // Regression check: the pre-existing hardcoded fast path for FRB's own (unqualified)
                // "HorizontalAlignment" must keep working.
                var frbHorizontal = VariableAssignmentLogic.ConvertStringToType(
                    "HorizontalAlignment", "Center", isState: false, out _);
                Check("FRB FlatRedBall.Graphics.HorizontalAlignment (existing hardcoded case)",
                    frbHorizontal, FlatRedBall.Graphics.HorizontalAlignment.Center);

                // Closes the loop against the exact call path a live game hits: apply the converted value
                // onto a real Gum object's real XOrigin property the same way Screen.ApplyVariable does,
                // via FlatRedBall's reflection-based LateBinder - this is what actually threw
                // InvalidOperationException in the original bug report.
                var gue = new Gum.Wireframe.GraphicalUiElement();
                FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(gue, "XOrigin", gumHorizontal);
                Check("LateBinder.SetValueStatic applied XOrigin without throwing",
                    gue.XOrigin, RenderingLibrary.Graphics.HorizontalAlignment.Center);

                if (failureCount == 0)
                {
                    Console.WriteLine("ALL_OK");
                    Environment.Exit(0);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: unhandled exception: " + ex);
                Environment.Exit(1);
            }
            """);
    }

    private static void WriteEngineEnumProgram(string scratchDirectory)
    {
        File.WriteAllText(Path.Combine(scratchDirectory, "Program.cs"), """
            using System;
            using GlueControl.Editing;

            int failureCount = 0;

            void Check(string label, object actual, object expected)
            {
                if (Equals(actual, expected))
                {
                    Console.WriteLine($"OK: {label} -> {actual} ({actual?.GetType()})");
                }
                else
                {
                    Console.WriteLine($"FAIL: {label} - expected {expected} ({expected?.GetType()}), got {actual} ({actual?.GetType()})");
                    failureCount++;
                }
            }

            try
            {
                // Issue #2141 repro: ConvertVariableValue (the actual production caller) invokes
                // ConvertStringToType with only data.Type/data.VariableValue - no contextualInstanceType -
                // exactly as reproduced here. ExposedVariableManager reports RepositionDirections' Type as
                // the short, unqualified name "RepositionDirections", not "FlatRedBall.Math.Geometry.RepositionDirections".
                var repositionDirections = VariableAssignmentLogic.ConvertStringToType(
                    "RepositionDirections", "Up", isState: false, out _);
                Check("Engine FlatRedBall.Math.Geometry.RepositionDirections (short, unqualified Type)",
                    repositionDirections, FlatRedBall.Math.Geometry.RepositionDirections.Up);

                // Closes the loop against the exact call path a live game hits: apply the converted value
                // onto a real AxisAlignedRectangle's real RepositionDirections property the same way
                // Screen.ApplyVariable does, via FlatRedBall's reflection-based LateBinder - this is what
                // actually threw InvalidOperationException in the original bug report.
                var rectangle = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
                FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(rectangle, "RepositionDirections", repositionDirections);
                Check("LateBinder.SetValueStatic applied RepositionDirections without throwing",
                    rectangle.RepositionDirections, FlatRedBall.Math.Geometry.RepositionDirections.Up);

                if (failureCount == 0)
                {
                    Console.WriteLine("ALL_OK");
                    Environment.Exit(0);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: unhandled exception: " + ex);
                Environment.Exit(1);
            }
            """);
    }

    private static string ReadNet6MonoGameVersion(string skiaProjectPath)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(skiaProjectPath),
            """<PackageReference\s+Include="MonoGame\.Framework\.DesktopGL"\s+Version="([^"]+)"\s+Condition="'\$\(TargetFramework\)'\s*!=\s*'net8\.0'""");

        match.Success.ShouldBeTrue(
            $"Could not read the net6 MonoGame version out of {skiaProjectPath}. If the engine's MonoGame " +
            "reference was restructured, this test's scratch project needs to follow it.");

        return match.Groups[1].Value;
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
}
