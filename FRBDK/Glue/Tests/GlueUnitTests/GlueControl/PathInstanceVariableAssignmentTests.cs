using System;
using System.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;

namespace GlueUnitTests.GlueControlTests;

// Issue #2156: live-editing a PathInstance's "Path" property (drawing/editing the path points in Glue
// while the game is running with Live Edit) throws System.MemberAccessException: "LateBinder could not
// find a field or property by the name of Path in the class GlueControl.Screens.EntityViewingScreen".
//
// Unlike NestedInstanceVariableAssignmentTests (#2076), which drives the forcedItem-provided overload
// (only used by CommandReplayLogic when re-applying commands to a newly-created instance), this drives
// the actual first-time-edit entry point production uses: CommandReceiver.HandleDto(GlueVariableSetData)
// calls VariableAssignmentLogic.SetVariable(dto) with no forcedItem, which loops over
// FlatRedBall.SpriteManager.ManagedPositionedObjects to find the matching entity instance instead.
//
// VariableAssignmentLogic.cs is embedded-only, see VariableAssignmentLogicEnumConversionTests.cs for the
// full rationale on why this must compile+run the real generated file in a scratch project rather than
// call it in-process.
[Trait("Category", "BuildSmoke")]
[Collection(nameof(TaskManagerSequentialCollection))]
public class PathInstanceVariableAssignmentTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly string _tempProjectDirectory;

    public PathInstanceVariableAssignmentTests()
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
    public void LiveEditingPathInstanceProperty_ShouldApplyToTheRealPath_NotTheScreen()
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
        var scratchDirectory = Path.Combine(_tempProjectDirectory, "PathInstanceScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generatedDirectory);
        WriteProgram(scratchDirectory);

        var (exitCode, output) = NestedDotnetCli.Run($"run --project \"{scratchDirectory}\" -c Debug");

        exitCode.ShouldBe(0,
            "Live-editing a PathInstance's Path property (issue #2156) did not apply cleanly to the " +
            "real Path instance:" + Environment.NewLine + output);
        output.ShouldContain("ALL_OK");
    }

    // Root-cause regression for #2156: when the entity-level reflection lookup can't find the named
    // instance (e.g. PathInstance was added to the entity live, without a rebuild, so the compiled type
    // has no such member yet), SetValueOnObjectInElement falls back to
    // Screen.GetInstanceRecursive("this.PathInstance"). Before engine commit 3a8800527 (#2064,
    // "Screen.GetInstanceRecursive returning the container instead of resolving the final path segment"),
    // that fallback's "this.X" base case returned the screen itself instead of resolving X on it, so the
    // fallback treated the Screen as a found (non-null) target and called
    // screen.ApplyVariable("Path", value, screen) - LateBinder.SetValueStatic then threw exactly the
    // reported "LateBinder could not find a field or property by the name of Path in the class
    // GlueControl.Screens.EntityViewingScreen". Both that engine fix and #1985's "Could not find an
    // object named X" guard are on this branch, so this must now fail cleanly (a reported error, not a
    // thrown exception) instead of crashing.
    [Fact]
    public void LiveEditingPathInstanceProperty_WhenTheInstanceCannotBeFound_ShouldFailCleanly_NotCrashFromTheScreen()
    {
        EmbeddedCodeManager.EmbedAll(fullyGenerate: true);
        GlueCallsCodeGenerator.GenerateAll();

        var generatedDirectory = Path.Combine(_tempProjectDirectory, "GlueControl");
        var repoRoot = FindRepoRoot();
        var scratchDirectory = Path.Combine(_tempProjectDirectory, "PathInstanceMissingScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generatedDirectory);
        WriteMissingInstanceProgram(scratchDirectory);

        var (exitCode, output) = NestedDotnetCli.Run($"run --project \"{scratchDirectory}\" -c Debug");

        exitCode.ShouldBe(0,
            "Live-editing a PathInstance's Path property when the instance can't be found on the entity " +
            "(issue #2156's actual root cause, engine commit 3a8800527 / #2064) crashed instead of " +
            "failing cleanly:" + Environment.NewLine + output);
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
                <DefineConstants>MONOGAME_381</DefineConstants>
              </PropertyGroup>
              <ItemGroup>
                <!-- Same minimal slice as VariableAssignmentLogicEnumConversionTests - only what
                     VariableAssignmentLogic.cs itself needs to compile. -->
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

        File.WriteAllText(Path.Combine(scratchDirectory, "PathInstanceScratch.csproj"), csproj);

        // Compile-only stand-ins, same rationale as VariableAssignmentLogicEnumConversionTests, with one
        // deliberate behavior change: CommandReceiver.DoTypesMatch here actually matches (real reflection
        // against the real System.Type), instead of the enum test's hardcoded "=> false". That's what lets
        // the no-forcedItem SpriteManager.ManagedPositionedObjects loop in SetVariable(GlueVariableSetData)
        // actually find our stand-in entity instead of being skipped.
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

            namespace GlueControl
            {
                public static class CommandReceiver
                {
                    public const string ProjectNamespace = "TestProject";
                    public static bool GetIfMatchesCurrentScreen(string elementNameGlue) => false;

                    // Real matching, unlike the enum test's hardcoded false: this is what lets the
                    // no-forcedItem SpriteManager loop in SetVariable(GlueVariableSetData) actually run.
                    public static bool DoTypesMatch(FlatRedBall.PositionedObject positionedObject, string qualifiedTypeName, System.Type possibleType = null) =>
                        possibleType != null && possibleType.IsInstanceOfType(positionedObject);

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
            using System.Reflection;
            using GlueControl.Editing;
            using GlueControl.Dtos;

            int failureCount = 0;

            void Check(string label, bool condition, string detail = null)
            {
                if (condition)
                {
                    Console.WriteLine($"OK: {label}");
                }
                else
                {
                    Console.WriteLine($"FAIL: {label}" + (detail != null ? " - " + detail : ""));
                    failureCount++;
                }
            }

            try
            {
                // Stand-in for the SandBox repro: an Entity (Player) with a PathInstance child object of
                // type FlatRedBall.Math.Paths.Path, matching how Glue codegens a NamedObjectSave as a
                // public field with its .Name set to the instance name (NamedObjectSaveCodeGenerator).
                var player = new PlayerStub();
                player.PathInstance = new FlatRedBall.Math.Paths.Path { Name = "PathInstance" };

                // Production's real entry point (CommandReceiver.HandleDto) calls
                // VariableAssignmentLogic.SetVariable(dto) with NO forcedItem - the initial live-edit path,
                // which loops over FlatRedBall.SpriteManager.ManagedPositionedObjects to find the matching
                // instance, unlike the forcedItem-provided overload used only by command replay. So the
                // stand-in entity has to actually be in that list for this to exercise the real bug.
                var managedPositionedObjectsField = typeof(FlatRedBall.SpriteManager).GetField(
                    "mManagedPositionedObjects", BindingFlags.NonPublic | BindingFlags.Static);
                var managedPositionedObjects = (System.Collections.IList)managedPositionedObjectsField.GetValue(null);
                managedPositionedObjects.Add(player);

                var screen = new FlatRedBall.Screens.Screen();
                var currentScreenField = typeof(FlatRedBall.Screens.ScreenManager).GetField(
                    "mCurrentScreen", BindingFlags.NonPublic | BindingFlags.Static);
                currentScreenField.SetValue(null, screen);

                var dto = new GlueVariableSetData
                {
                    ElementNameGlue = typeof(PlayerStub).FullName,
                    VariableName = "this.PathInstance.Path",
                    VariableValue = "[{\"SegmentType\":0,\"IsRelative\":false,\"StartX\":0.0,\"StartY\":0.0,\"EndX\":0.0,\"EndY\":20.0,\"StartVelocity\":\"0, 0\",\"EndVelocity\":\"0, 0\",\"ArcAngle\":0.0,\"CircleCenter\":\"0, 0\",\"CalculatedLength\":0.0,\"AngleUnit\":0}]",
                    Type = "string",
                    IsState = false,
                };

                var response = VariableAssignmentLogic.SetVariable(dto);

                Check("No exception reported on response", response.Exception == null, response.Exception);
                Check("Variable reported as assigned", response.WasVariableAssigned);
                Check("Path segment applied to the real Path instance, not the Screen",
                    player.PathInstance.Segments.Count == 1,
                    $"actual Segments.Count = {player.PathInstance.Segments.Count}");

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

            public class PlayerStub : FlatRedBall.PositionedObject
            {
                public FlatRedBall.Math.Paths.Path PathInstance;
            }
            """);
    }

    private static void WriteMissingInstanceProgram(string scratchDirectory)
    {
        File.WriteAllText(Path.Combine(scratchDirectory, "Program.cs"), """
            using System;
            using System.Reflection;
            using GlueControl.Editing;
            using GlueControl.Dtos;

            int failureCount = 0;

            void Check(string label, bool condition, string detail = null)
            {
                if (condition)
                {
                    Console.WriteLine($"OK: {label}");
                }
                else
                {
                    Console.WriteLine($"FAIL: {label}" + (detail != null ? " - " + detail : ""));
                    failureCount++;
                }
            }

            try
            {
                // Stand-in for PathInstance having been added to the entity live (no rebuild), so the
                // compiled entity type has no "PathInstance" member at all yet - the entity-level
                // reflection lookup in VariableAssignmentLogic.SetVariable genuinely fails to find it,
                // forcing the fallback through Screen.GetInstanceRecursive("this.PathInstance").
                var player = new PlayerStubWithoutPathInstance();

                var managedPositionedObjectsField = typeof(FlatRedBall.SpriteManager).GetField(
                    "mManagedPositionedObjects", BindingFlags.NonPublic | BindingFlags.Static);
                var managedPositionedObjects = (System.Collections.IList)managedPositionedObjectsField.GetValue(null);
                managedPositionedObjects.Add(player);

                var screen = new FlatRedBall.Screens.Screen();
                var currentScreenField = typeof(FlatRedBall.Screens.ScreenManager).GetField(
                    "mCurrentScreen", BindingFlags.NonPublic | BindingFlags.Static);
                currentScreenField.SetValue(null, screen);

                var dto = new GlueVariableSetData
                {
                    ElementNameGlue = typeof(PlayerStubWithoutPathInstance).FullName,
                    VariableName = "this.PathInstance.Path",
                    VariableValue = "[{\"SegmentType\":0,\"IsRelative\":false,\"StartX\":0.0,\"StartY\":0.0,\"EndX\":0.0,\"EndY\":20.0,\"StartVelocity\":\"0, 0\",\"EndVelocity\":\"0, 0\",\"ArcAngle\":0.0,\"CircleCenter\":\"0, 0\",\"CalculatedLength\":0.0,\"AngleUnit\":0}]",
                    Type = "string",
                    IsState = false,
                };

                var response = VariableAssignmentLogic.SetVariable(dto);

                Check("A clean 'could not find object' message was reported, not a thrown exception",
                    response.Exception != null && response.Exception.Contains("Could not find an object named PathInstance"),
                    $"actual Exception = {response.Exception}");
                Check("The exception is NOT the raw LateBinder crash naming the Screen class",
                    response.Exception == null || !response.Exception.Contains("LateBinder could not find"),
                    $"actual Exception = {response.Exception}");

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

            public class PlayerStubWithoutPathInstance : FlatRedBall.PositionedObject
            {
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
