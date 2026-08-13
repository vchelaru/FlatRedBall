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

// Issue #2076: live-editing a nested named object's variable while viewing an entity directly
// (this.Glow.CurrentChainName on Entities\Bosses\ResonatorPyramid, "Glow" a child Sprite) reportedly
// threw System.MemberAccessException: "LateBinder could not find a field or property by the name of
// CurrentChainName in the class GlueControl.Screens.EntityViewingScreen" - i.e. VariableAssignmentLogic
// fell back to assigning the variable directly on the Screen instead of on the nested "Glow" instance.
//
// Commit 74e617ac8 (#1985, closes #1984, merged a week before this issue was filed) changed exactly
// that fallback: when SetValueOnObjectInElement's targetInstance lookup fails, it no longer calls
// screen.ApplyVariable(variableName, variableValue) unconditionally (which crashed via LateBinder
// naming the screen's own class) - it now reports a clean "Could not find an object named X" instead.
// That fixes the crash shape, but doesn't by itself prove the nested lookup succeeds. This test drives
// the real forcedItem-provided path (SetVariable(string, object, PositionedObject forcedItem, ...)) with
// a real PositionedObject entity stand-in exposing a real child Sprite, to confirm "Glow" is actually
// found and CurrentChainName is actually applied - not just that the crash no longer happens.
//
// VariableAssignmentLogic.cs is embedded-only, see VariableAssignmentLogicEnumConversionTests.cs for the
// full rationale on why this must compile+run the real generated file in a scratch project rather than
// call it in-process.
[Trait("Category", "BuildSmoke")]
[Collection(nameof(TaskManagerSequentialCollection))]
public class NestedInstanceVariableAssignmentTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly string _tempProjectDirectory;

    public NestedInstanceVariableAssignmentTests()
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
    public void LiveEditingNestedInstanceVariable_ShouldApplyOnTheNestedInstance_NotTheScreen()
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
        var scratchDirectory = Path.Combine(_tempProjectDirectory, "NestedInstanceScratch");
        WriteScratchProject(scratchDirectory, repoRoot, generatedDirectory);
        WriteProgram(scratchDirectory);

        var (exitCode, output) = NestedDotnetCli.Run($"run --project \"{scratchDirectory}\" -c Debug");

        exitCode.ShouldBe(0,
            "Live-editing a nested instance's variable (this.Glow.CurrentChainName on an entity viewed " +
            "directly, issue #2076) did not apply cleanly to the nested instance:" + Environment.NewLine + output);
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

        File.WriteAllText(Path.Combine(scratchDirectory, "NestedInstanceScratch.csproj"), csproj);

        // Compile-only stand-ins, same rationale as VariableAssignmentLogicEnumConversionTests, with one
        // deliberate behavior change: CommandReceiver.DoTypesMatch here actually matches (real reflection
        // against the real System.Type), instead of the enum test's hardcoded "=> false". That's the
        // difference that lets the forcedItem-provided branch in SetVariable(string, object,
        // PositionedObject, string, GlueVariableSetDataResponse) actually run instead of being skipped.
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

                    // Real matching, unlike the enum test's hardcoded false: this is the behavior
                    // difference under test - it lets SetVariable's forcedItem branch actually run.
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
                // ScreenManager.CurrentScreen has no public setter (it's only ever set internally by
                // LoadScreen/etc). Reflection onto the private static backing field is the lightest way
                // to stand up "a real Screen is running" without going through a full screen-load pipeline
                // (which needs FlatRedBallServices/graphics device init this test doesn't have).
                var screen = new FlatRedBall.Screens.Screen();
                var currentScreenField = typeof(FlatRedBall.Screens.ScreenManager).GetField(
                    "mCurrentScreen", BindingFlags.NonPublic | BindingFlags.Static);
                currentScreenField.SetValue(null, screen);

                // Stand-in for Entities\Bosses\ResonatorPyramid: a real PositionedObject subclass (must live
                // in this same assembly - VariableAssignmentLogic.SetVariable resolves the owner type via
                // typeof(VariableAssignmentLogic).Assembly.GetType(instanceOwnerElementGameType)) exposing a
                // public "Glow" field, matching how Glue codegens named objects as public fields on the entity.
                var boss = new ResonatorPyramidStub();
                var glow = new FlatRedBall.Sprite { Name = "Glow" };
                glow.AnimationChains = new FlatRedBall.Graphics.Animation.AnimationChainList();
                var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "PyramidGlow" };
                chain.Add(new FlatRedBall.Graphics.Animation.AnimationFrame());
                glow.AnimationChains.Add(chain);
                boss.Glow = glow;

                var response = new GlueVariableSetDataResponse();

                // The real call VariableAssignmentLogic.SetVariable(GlueVariableSetData, forcedItem) makes
                // internally, driven directly - same shape as the reported "this.Glow.CurrentChainName" =
                // "PyramidGlow" on Entities\Bosses\ResonatorPyramid while viewing it in EntityViewingScreen.
                var convertedValue = VariableAssignmentLogic.SetVariable(
                    "this.Glow.CurrentChainName",
                    "PyramidGlow",
                    boss,
                    typeof(ResonatorPyramidStub).FullName,
                    response);

                Check("No exception reported on response", response.Exception == null, response.Exception);
                Check("CurrentChainName applied to the nested Glow Sprite, not the Screen",
                    glow.CurrentChainName == "PyramidGlow", $"actual CurrentChainName = {glow.CurrentChainName}");

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

            public class ResonatorPyramidStub : FlatRedBall.PositionedObject
            {
                public FlatRedBall.Sprite Glow;
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
