using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GlueUnitTests.TestSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Pins #2366/#2372: at project version ScreenManagerHasRegisterScreenFactory Game1.Generated.cs registers a factory per
// screen (so Native AOT never reaches a trimmed constructor through Assembly.GetType + Activator) and finds the
// command line start screen through the same list. Older versions keep emitting the reflection-only code.
public class ScreenRegistryCodeGenerationTests : IDisposable
{
    const int RegistryVersion = (int)GlueProjectSave.GluxVersions.ScreenManagerHasRegisterScreenFactory;

    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly System.Collections.Generic.List<Game1CodeGenerator> _originalGenerators;
    readonly string _tempProjectDirectory;

    public ScreenRegistryCodeGenerationTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalGenerators = Game1CodeGeneratorManager.Generators.ToList();

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        // Isolate the registry: other plugins' generators emit camera/Gum code this test does not care about.
        Game1CodeGeneratorManager.Generators.Clear();
    }

    public void Dispose()
    {
        Game1CodeGeneratorManager.Generators.Clear();
        Game1CodeGeneratorManager.Generators.AddRange(_originalGenerators);
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); }
        catch { /* best-effort cleanup */ }
    }

    static ScreenSave AbstractScreen(string name)
    {
        var screen = new ScreenSave { Name = name };
        screen.NamedObjects.Add(new NamedObjectSave { InstanceName = "SetByTheDerivedScreen", SetByDerived = true });
        return screen;
    }

    GlueProjectSave ProjectWithScreens(int fileVersion, params ScreenSave[] screens)
    {
        var project = new GlueProjectSave { FileVersion = fileVersion };
        project.Screens.AddRange(screens);
        ObjectFinder.Self.GlueProject = project;
        return project;
    }

    [Fact]
    public void Game1_ShouldRegisterAFactoryForEveryConcreteScreen()
    {
        ProjectWithScreens(RegistryVersion,
            new ScreenSave { Name = "Screens\\Level1" },
            new ScreenSave { Name = "Screens\\Folder\\Level2" });

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldContain("ScreenManager.RegisterScreenFactory(\"TestProject.Screens.Level1\", () => new global::TestProject.Screens.Level1());", Case.Sensitive);
        code.ShouldContain("ScreenManager.RegisterScreenFactory(\"TestProject.Screens.Folder.Level2\", () => new global::TestProject.Screens.Folder.Level2());", Case.Sensitive);
    }

    [Fact]
    public void Game1_ShouldNotRegisterAbstractScreens()
    {
        ProjectWithScreens(RegistryVersion,
            new ScreenSave { Name = "Screens\\Level1" },
            AbstractScreen("Screens\\BaseLevel"));

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        // An abstract generated class cannot be constructed, so `new BaseLevel()` would not compile.
        code.ShouldNotContain("BaseLevel");
    }

    [Fact]
    public void Game1_ShouldRegisterScreensBeforeAnythingCanLoadOne()
    {
        ProjectWithScreens(RegistryVersion, new ScreenSave { Name = "Screens\\Level1" });

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        var earlyIndex = code.IndexOf("GeneratedInitializeEarly", StringComparison.Ordinal);
        var lateIndex = System.Text.RegularExpressions.Regex.Match(code, @"GeneratedInitialize\s*\(").Index;
        var registerCall = code.IndexOf("RegisterScreenFactories();", StringComparison.Ordinal);
        earlyIndex.ShouldBeGreaterThan(-1);
        registerCall.ShouldBeGreaterThan(earlyIndex);
        registerCall.ShouldBeLessThan(lateIndex);
    }

    [Fact]
    public void Game1_ShouldNotGenerateTheRegistry_BelowTheRegistryVersion()
    {
        ProjectWithScreens(RegistryVersion - 1, new ScreenSave { Name = "Screens\\Level1" });

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldNotContain("RegisterScreenFactory");
        code.ShouldNotContain("RegisterScreenFactories");
    }

    [Fact]
    public void StartScreenFromCommandLine_ShouldLookInTheRegistry_AtTheRegistryVersion()
    {
        ProjectWithScreens(RegistryVersion, new ScreenSave { Name = "Screens\\Level1" });
        Game1CodeGeneratorManager.Generators.Add(new Game1GlueControlGenerator());

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldContain("var type = GetScreenTypeByName(item);", Case.Sensitive);
        // The only Assembly.GetType call left is the suppressed fallback inside the generated registry.
        code.ShouldNotContain("thisAssembly.GetType(item)");
    }

    [Fact]
    public void StartScreenFromCommandLine_ShouldKeepUsingReflection_BelowTheRegistryVersion()
    {
        ProjectWithScreens(RegistryVersion - 1, new ScreenSave { Name = "Screens\\Level1" });
        Game1CodeGeneratorManager.Generators.Add(new Game1GlueControlGenerator());

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldContain("var type = thisAssembly.GetType(item);", Case.Sensitive);
        code.ShouldNotContain("GetScreenTypeByName");
    }

    [Fact]
    public void IsRegistryStale_ShouldBeFalse_RightAfterGeneratingGame1()
    {
        var project = ProjectWithScreens(RegistryVersion, new ScreenSave { Name = "Screens\\Level1" });
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        ScreenRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeFalse();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAScreenWasAdded()
    {
        var project = ProjectWithScreens(RegistryVersion, new ScreenSave { Name = "Screens\\Level1" });
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        project.Screens.Add(new ScreenSave { Name = "Screens\\Level2" });

        ScreenRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAScreenWasRemoved()
    {
        var level1 = new ScreenSave { Name = "Screens\\Level1" };
        var project = ProjectWithScreens(RegistryVersion, level1, new ScreenSave { Name = "Screens\\Level2" });
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        project.Screens.Remove(level1);

        ScreenRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAScreenBecameAbstract()
    {
        var level1 = new ScreenSave { Name = "Screens\\Level1" };
        var project = ProjectWithScreens(RegistryVersion, level1);
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        level1.NamedObjects.Add(new NamedObjectSave { InstanceName = "SetByTheDerivedScreen", SetByDerived = true });

        ScreenRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeFalse_BelowTheRegistryVersion()
    {
        var project = ProjectWithScreens(RegistryVersion - 1, new ScreenSave { Name = "Screens\\Level1" });

        ScreenRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeFalse();
    }

    [Fact]
    public void GeneratedRegistry_ShouldCompile_AndCreateScreensWithoutReflection()
    {
        ProjectWithScreens(RegistryVersion,
            new ScreenSave { Name = "Screens\\Level1" },
            new ScreenSave { Name = "Screens\\Folder\\Level2" },
            AbstractScreen("Screens\\BaseLevel"));

        var generated = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        // Stand-ins for the engine and the other generated classes; only the shapes the registry touches.
        const string stubs = """
            namespace FlatRedBall.Screens
            {
                public class Screen { }
                public static class ScreenManager
                {
                    public static readonly System.Collections.Generic.Dictionary<string, System.Func<Screen>> Factories = new();
                    public static void RegisterScreenFactory(string name, System.Func<Screen> factory) => Factories[name] = factory;
                }
            }
            namespace TestProject.Screens
            {
                public class Level1 : FlatRedBall.Screens.Screen { }
                public abstract class BaseLevel : FlatRedBall.Screens.Screen { }
            }
            namespace TestProject.Screens.Folder
            {
                public class Level2 : FlatRedBall.Screens.Screen { }
            }
            namespace TestProject
            {
                public static class GlobalContent { public static void Initialize() { } }

                public partial class Game1
                {
                    partial void GeneratedInitializeEarly();
                    partial void GeneratedInitialize();
                    partial void GeneratedUpdate(object gameTime);
                    partial void GeneratedDrawEarly(object gameTime);
                    partial void GeneratedDraw(object gameTime);

                    public static void RunEarly() => new Game1().GeneratedInitializeEarly();
                    public static System.Type Find(string name) => GetScreenTypeByName(name);
                }
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(item => Path.GetFileName(item) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll" or "System.Linq.dll")
            .Select(item => MetadataReference.CreateFromFile(item));

        var compilation = CSharpCompilation.Create("GeneratedRegistry",
            new[]
            {
                // Game1.Generated.cs mentions GameTime as Microsoft.Xna.Framework.GameTime; the stub above
                // declares the partial methods with object, so swap the type for the compile-only check.
                CSharpSyntaxTree.ParseText(generated.Replace("Microsoft.Xna.Framework.GameTime", "object")),
                CSharpSyntaxTree.ParseText(stubs)
            },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)
            .Select(item => item.ToString()).ShouldBeEmpty();

        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        var screenManager = assembly.GetType("FlatRedBall.Screens.ScreenManager")!;
        var game1 = assembly.GetType("TestProject.Game1")!;

        game1.GetMethod("RunEarly")!.Invoke(null, null);

        var factories = (System.Collections.IDictionary)screenManager.GetField("Factories")!.GetValue(null)!;
        factories.Keys.Cast<string>().OrderBy(item => item).ShouldBe(new[] { "TestProject.Screens.Folder.Level2", "TestProject.Screens.Level1" });

        var find = game1.GetMethod("Find")!;
        ((Type)find.Invoke(null, new object[] { "TestProject.Screens.Level1" })!).FullName.ShouldBe("TestProject.Screens.Level1");
        ((Type)find.Invoke(null, new object[] { "TestProject.Screens.Folder.Level2" })!).FullName.ShouldBe("TestProject.Screens.Folder.Level2");
        // Not generated (abstract): falls through to the reflection fallback, which also finds nothing for a bogus name.
        find.Invoke(null, new object[] { "TestProject.Screens.DoesNotExist" }).ShouldBeNull();
    }
}
