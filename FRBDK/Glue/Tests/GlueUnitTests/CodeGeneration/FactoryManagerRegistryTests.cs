using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Pins #2389: the generated FactoryManager (Performance\IEntityFactory.Generated.cs) looks a factory up in the
// registry first and falls back to reflection for names that were not registered. Native AOT and trimming remove
// what the reflection path needs (the parameterless constructor, the Self property and CreateNew), which is why
// Tiled entities were never created. The test compiles the real generated file with Roslyn and drives it, with
// a "trimmed" factory whose reflection hooks are absent standing in for what the trimmer leaves behind.
public class FactoryManagerRegistryTests : IDisposable
{
    const int RegistryVersion = (int)GlueProjectSave.GluxVersions.EntityFactoryRegistry;

    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _tempProjectDirectory;

    public FactoryManagerRegistryTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); }
        catch { /* best-effort cleanup */ }
    }

    // FactoryManager is written to look like the factories Glue generates: a Self singleton and a static
    // CreateNew(Layer, float, float, float) returning the entity. TrimmedFactory is what the trimmer leaves of
    // one: no public constructor and no Self, so only a registration can reach it.
    const string Stubs = """
        namespace FlatRedBall.Graphics { public class Layer { } }
        namespace Microsoft.Xna.Framework { public struct Vector3 { } }
        namespace TestProject.Entities
        {
            public class Old { }
            public class Npc { }
        }
        namespace TestProject.Factories
        {
            using TestProject.Performance;

            public class OldFactory : IEntityFactory
            {
                public static OldFactory Self { get; } = new OldFactory();
                public static TestProject.Entities.Old CreateNew(FlatRedBall.Graphics.Layer layer, float x, float y, float z) => new TestProject.Entities.Old();
                object IEntityFactory.CreateNew(float x, float y) => null;
                object IEntityFactory.CreateNew(Microsoft.Xna.Framework.Vector3 position) => null;
                object IEntityFactory.CreateNew(FlatRedBall.Graphics.Layer layer) => null;
                int IEntityFactory.NewInstancesCreatedThisScreen => 0;
                void IEntityFactory.Initialize(string contentManager) { }
                void IEntityFactory.ClearListsToAddTo() { }
                void IEntityFactory.Destroy() { }
                System.Collections.Generic.List<System.Collections.IList> IEntityFactory.ListsToAddTo => null;
            }

            public class TrimmedFactory : IEntityFactory
            {
                private TrimmedFactory() { }
                public static TrimmedFactory Create() => new TrimmedFactory();
                object IEntityFactory.CreateNew(float x, float y) => null;
                object IEntityFactory.CreateNew(Microsoft.Xna.Framework.Vector3 position) => null;
                object IEntityFactory.CreateNew(FlatRedBall.Graphics.Layer layer) => null;
                int IEntityFactory.NewInstancesCreatedThisScreen => 0;
                void IEntityFactory.Initialize(string contentManager) { }
                void IEntityFactory.ClearListsToAddTo() { }
                void IEntityFactory.Destroy() { }
                System.Collections.Generic.List<System.Collections.IList> IEntityFactory.ListsToAddTo => null;
            }
        }
        """;

    string CompileFactoryManager(int fileVersion, out System.Reflection.Assembly assembly)
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = fileVersion };

        var contents = FactoryElementCodeGenerator.GetEntityFactoryFileContents();

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(item => Path.GetFileName(item) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll" or "System.Linq.dll")
            .Select(item => MetadataReference.CreateFromFile(item));

        var compilation = CSharpCompilation.Create("GeneratedFactoryManager" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(contents), CSharpSyntaxTree.ParseText(Stubs) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)
            .Select(item => item.ToString()).ShouldBeEmpty();

        assembly = System.Reflection.Assembly.Load(stream.ToArray());
        return contents;
    }

    static Type FactoryManager(System.Reflection.Assembly assembly) => assembly.GetType("TestProject.Performance.FactoryManager")!;

    static object Get(System.Reflection.Assembly assembly, string entityName) =>
        FactoryManager(assembly).GetMethod("Get", new[] { typeof(string) })!.Invoke(null, new object[] { entityName })!;

    static object NewTrimmedFactory(System.Reflection.Assembly assembly) =>
        assembly.GetType("TestProject.Factories.TrimmedFactory")!.GetMethod("Create")!.Invoke(null, null)!;

    static void Register(System.Reflection.Assembly assembly, string entityName, object factory) =>
        FactoryManager(assembly).GetMethod("Register")!.Invoke(null, new[] { entityName, factory });

    [Fact]
    public void Get_ShouldFindARegisteredFactoryThatReflectionCannotReach()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        var trimmed = NewTrimmedFactory(assembly);

        Register(assembly, "TestProject.Entities.Npc", trimmed);

        Get(assembly, "Npc").ShouldBeSameAs(trimmed);
    }

    [Theory]
    [InlineData("Npc")]
    [InlineData("Entities\\Npc")]
    [InlineData("Entities/Npc")]
    [InlineData("TestProject.Entities.Npc")]
    public void Get_ShouldMatchTheEntityNameWhetherOrNotItIsQualified(string requestedName)
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        var trimmed = NewTrimmedFactory(assembly);
        Register(assembly, "TestProject.Entities.Npc", trimmed);

        Get(assembly, requestedName).ShouldBeSameAs(trimmed);
    }

    [Fact]
    public void Get_ShouldUseTheRegistryBeforeReflection()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        var registered = NewTrimmedFactory(assembly);

        // OldFactory is reflection-discoverable, so without the registry Get("Old") would return its Self.
        Register(assembly, "Old", registered);

        Get(assembly, "Old").ShouldBeSameAs(registered);
    }

    [Fact]
    public void Get_ShouldFallBackToReflection_ForANameThatWasNotRegistered()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        // Something is registered, but not "Old".
        Register(assembly, "TestProject.Entities.Npc", NewTrimmedFactory(assembly));

        var found = Get(assembly, "Old");

        found.GetType().FullName.ShouldBe("TestProject.Factories.OldFactory");
    }

    [Fact]
    public void Get_ShouldReturnNullInsteadOfThrowing_WhenARegisteredFactoryCannotBeInspectedByReflection()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        Register(assembly, "TestProject.Entities.Npc", NewTrimmedFactory(assembly));

        // The registered trimmed factory has no CreateNew to inspect; looking for a name nobody has must not throw.
        FactoryManager(assembly).GetMethod("Get", new[] { typeof(string) })!
            .Invoke(null, new object[] { "Missing" }).ShouldBeNull();
    }

    [Fact]
    public void Get_ShouldFindTheFactoryAfterAFailedLookupBeforeItWasRegistered()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        FactoryManager(assembly).GetMethod("Get", new[] { typeof(string) })!
            .Invoke(null, new object[] { "Npc" }).ShouldBeNull();

        var trimmed = NewTrimmedFactory(assembly);
        Register(assembly, "TestProject.Entities.Npc", trimmed);

        // The earlier null answer must not stay cached.
        Get(assembly, "Npc").ShouldBeSameAs(trimmed);
    }

    [Fact]
    public void GetAllFactories_ShouldIncludeRegisteredFactories_AndKeepReflectedOnes()
    {
        CompileFactoryManager(RegistryVersion, out var assembly);
        var trimmed = NewTrimmedFactory(assembly);
        Register(assembly, "TestProject.Entities.Npc", trimmed);

        var all = ((System.Collections.IEnumerable)FactoryManager(assembly).GetMethod("GetAllFactories")!.Invoke(null, null)!)
            .Cast<object>().ToList();

        all.ShouldContain(trimmed);
        all.Select(item => item.GetType().Name).ShouldContain("OldFactory");
    }

    [Fact]
    public void FactoryManager_ShouldNotHaveRegister_BelowTheRegistryVersion()
    {
        CompileFactoryManager(RegistryVersion - 1, out var assembly);

        FactoryManager(assembly).GetMethod("Register").ShouldBeNull();
        // The reflection lookup is untouched.
        Get(assembly, "Old").GetType().FullName.ShouldBe("TestProject.Factories.OldFactory");
    }
}
