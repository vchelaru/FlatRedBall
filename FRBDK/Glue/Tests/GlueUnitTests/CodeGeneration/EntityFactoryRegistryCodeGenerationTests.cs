using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Pins #2389: at project version EntityFactoryRegistry Game1.Generated.cs registers every entity factory with
// FactoryManager.Register (so Native AOT never needs the trimmed reflection lookup). Older versions keep emitting
// nothing for factories.
public class EntityFactoryRegistryCodeGenerationTests : IDisposable
{
    const int RegistryVersion = (int)GlueProjectSave.GluxVersions.EntityFactoryRegistry;

    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly System.Collections.Generic.List<Game1CodeGenerator> _originalGenerators;
    readonly string _tempProjectDirectory;

    public EntityFactoryRegistryCodeGenerationTests()
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

    static EntitySave Factoried(string name, bool createdByOtherEntities = true)
    {
        return new EntitySave { Name = name, CreatedByOtherEntities = createdByOtherEntities };
    }

    static EntitySave AbstractFactoried(string name)
    {
        var entity = Factoried(name);
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "SetByTheDerivedEntity", SetByDerived = true });
        return entity;
    }

    GlueProjectSave ProjectWithEntities(int fileVersion, params EntitySave[] entities)
    {
        var project = new GlueProjectSave { FileVersion = fileVersion };
        project.Entities.AddRange(entities);
        ObjectFinder.Self.GlueProject = project;
        return project;
    }

    [Fact]
    public void Game1_ShouldRegisterEveryEntityFactory()
    {
        ProjectWithEntities(RegistryVersion,
            Factoried("Entities\\Npc"),
            Factoried("Entities\\Folder\\Enemy"));

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldContain("global::TestProject.Performance.FactoryManager.Register(\"TestProject.Entities.Npc\", global::TestProject.Factories.NpcFactory.Self);", Case.Sensitive);
        // Factories are generated flat in the Factories namespace whatever folder the entity is in.
        code.ShouldContain("global::TestProject.Performance.FactoryManager.Register(\"TestProject.Entities.Folder.Enemy\", global::TestProject.Factories.EnemyFactory.Self);", Case.Sensitive);
    }

    [Fact]
    public void Game1_ShouldNotRegisterAbstractEntities_OrEntitiesWithoutAFactory()
    {
        ProjectWithEntities(RegistryVersion,
            Factoried("Entities\\Npc"),
            AbstractFactoried("Entities\\BaseNpc"),
            Factoried("Entities\\Door", createdByOtherEntities: false));

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        // No factory class is generated for either, so a reference to it would not compile.
        code.ShouldNotContain("BaseNpcFactory");
        code.ShouldNotContain("DoorFactory");
    }

    [Fact]
    public void Game1_ShouldRegisterFactoriesBeforeAnythingCanCreateAnEntity()
    {
        ProjectWithEntities(RegistryVersion, Factoried("Entities\\Npc"));

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        var earlyIndex = code.IndexOf("GeneratedInitializeEarly", StringComparison.Ordinal);
        var lateIndex = System.Text.RegularExpressions.Regex.Match(code, @"GeneratedInitialize\s*\(").Index;
        var registerCall = code.IndexOf("RegisterEntityFactories();", StringComparison.Ordinal);
        earlyIndex.ShouldBeGreaterThan(-1);
        registerCall.ShouldBeGreaterThan(earlyIndex);
        registerCall.ShouldBeLessThan(lateIndex);
    }

    [Fact]
    public void Game1_ShouldNotGenerateTheRegistry_BelowTheRegistryVersion()
    {
        ProjectWithEntities(RegistryVersion - 1, Factoried("Entities\\Npc"));

        var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        code.ShouldNotContain("FactoryManager");
        code.ShouldNotContain("RegisterEntityFactories");
    }

    [Fact]
    public void IsRegistryStale_ShouldBeFalse_RightAfterGeneratingGame1()
    {
        var project = ProjectWithEntities(RegistryVersion, Factoried("Entities\\Npc"));
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeFalse();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAFactoryWasAdded()
    {
        var project = ProjectWithEntities(RegistryVersion, Factoried("Entities\\Npc"));
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        project.Entities.Add(Factoried("Entities\\Enemy"));

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAnEntityStoppedBeingCreatedByOtherEntities()
    {
        var npc = Factoried("Entities\\Npc");
        var project = ProjectWithEntities(RegistryVersion, npc, Factoried("Entities\\Enemy"));
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        npc.CreatedByOtherEntities = false;

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAnEntityWasRemoved()
    {
        var npc = Factoried("Entities\\Npc");
        var project = ProjectWithEntities(RegistryVersion, npc, Factoried("Entities\\Enemy"));
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        project.Entities.Remove(npc);

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeTrue_WhenAnEntityBecameAbstract()
    {
        var npc = Factoried("Entities\\Npc");
        var project = ProjectWithEntities(RegistryVersion, npc);
        Game1CodeGeneratorManager.GetGame1GeneratedContents();

        npc.NamedObjects.Add(new NamedObjectSave { InstanceName = "SetByTheDerivedEntity", SetByDerived = true });

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistryStale_ShouldBeFalse_BelowTheRegistryVersion()
    {
        var project = ProjectWithEntities(RegistryVersion - 1, Factoried("Entities\\Npc"));

        EntityFactoryRegistryCodeGenerator.IsRegistryStale(project, "TestProject").ShouldBeFalse();
    }

    [Fact]
    public void GeneratedRegistry_ShouldCompile_AndRegisterEveryFactory()
    {
        ProjectWithEntities(RegistryVersion,
            Factoried("Entities\\Npc"),
            Factoried("Entities\\Folder\\Enemy"),
            AbstractFactoried("Entities\\BaseNpc"));

        var generated = Game1CodeGeneratorManager.GetGame1GeneratedContents();

        // Stand-ins for the generated factory manager and factories; only the shapes the registry touches.
        const string stubs = """
            namespace TestProject.Performance
            {
                public interface IEntityFactory { }
                public static class FactoryManager
                {
                    public static readonly System.Collections.Generic.Dictionary<string, IEntityFactory> Registered = new();
                    public static void Register(string entityName, IEntityFactory factory) => Registered[entityName] = factory;
                }
            }
            namespace TestProject.Factories
            {
                public class NpcFactory : TestProject.Performance.IEntityFactory { public static NpcFactory Self { get; } = new(); }
                public class EnemyFactory : TestProject.Performance.IEntityFactory { public static EnemyFactory Self { get; } = new(); }
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
                }
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(item => Path.GetFileName(item) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll" or "System.Linq.dll")
            .Select(item => MetadataReference.CreateFromFile(item));

        var compilation = CSharpCompilation.Create("GeneratedFactoryRegistry",
            new[]
            {
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
        assembly.GetType("TestProject.Game1")!.GetMethod("RunEarly")!.Invoke(null, null);

        var registered = (System.Collections.IDictionary)assembly.GetType("TestProject.Performance.FactoryManager")!
            .GetField("Registered")!.GetValue(null)!;
        registered.Keys.Cast<string>().OrderBy(item => item).ShouldBe(new[] { "TestProject.Entities.Folder.Enemy", "TestProject.Entities.Npc" });
        registered["TestProject.Entities.Npc"]!.GetType().FullName.ShouldBe("TestProject.Factories.NpcFactory");
    }
}
