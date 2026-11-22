using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Plugins;

// Runs PluginManagerBase's real directory scan over plugin DLLs built on the fly. Part discovery for each
// directory runs in parallel, so this pins that the result does not depend on that: which directories'
// exports are composed, and the order the load errors are reported in.
public class PluginDirectoryLoadingTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "PluginDirectoryLoadingTests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void LoadPlugins_ShouldComposeGoodDirectories_AndReportBadOnesInDirectoryOrder()
    {
        var dependency = Compile("MissingDependency", "public class DependencyBase { }");

        var first = WriteDirectory("1-First", ("FirstPlugin", Compile("FirstPlugin", ExportedClass("FirstExport"))));
        var corrupt = WriteDirectory("2-Corrupt", ("Corrupt", new byte[] { 1, 2, 3, 4 }));
        var missingDependency = WriteDirectory("3-MissingDependency", ("NeedsDependency",
            Compile("NeedsDependency", ExportedClass("NeedsDependencyExport", baseType: "DependencyBase"), dependency)));
        var last = WriteDirectory("4-Last", ("LastPlugin", Compile("LastPlugin", ExportedClass("LastExport"))));

        var manager = new TestPluginManager(new[] { first, corrupt, missingDependency, last })
        {
            AssemblyContext = new AssemblyLoadContext(null, isCollectible: true),
        };
        try
        {
            manager.LoadPlugins(new FilePath(root + "/")).ShouldBeTrue();

            manager.Found.Select(item => item.GetType().Name).ShouldBe(new[] { "FirstExport", "LastExport" }, ignoreOrder: true);
            manager.CompileErrors.Count.ShouldBe(2);
            manager.CompileErrors[0].ShouldStartWith("Failed to load " + Path.Combine(corrupt, "Corrupt.dll"));
            manager.CompileErrors[1].ShouldStartWith("Error trying to load plugins from directory:     " + missingDependency);
        }
        finally
        {
            manager.AssemblyContext.Unload();
        }
    }

    static string ExportedClass(string name, string? baseType = null) =>
        $$"""
        [System.ComponentModel.Composition.Export(typeof(System.IDisposable))]
        public class {{name}} : {{(baseType == null ? "" : baseType + ", ")}}System.IDisposable { public void Dispose() { } }
        """;

    static byte[] Compile(string assemblyName, string source, byte[]? reference = null)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(item => Path.GetFileName(item) is "System.Private.CoreLib.dll" or "System.Runtime.dll")
            .Select(item => (MetadataReference)MetadataReference.CreateFromFile(item))
            .Append(MetadataReference.CreateFromFile(typeof(ExportAttribute).Assembly.Location))
            .ToList();
        if (reference != null)
        {
            references.Add(MetadataReference.CreateFromImage(reference));
        }

        var compilation = CSharpCompilation.Create(assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)
            .Select(item => item.ToString()).ShouldBeEmpty();
        return stream.ToArray();
    }

    string WriteDirectory(string name, params (string AssemblyName, byte[] Bytes)[] files)
    {
        var directory = Path.Combine(root, name) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(directory);
        foreach (var (assemblyName, bytes) in files)
        {
            File.WriteAllBytes(Path.Combine(directory, assemblyName + ".dll"), bytes);
        }
        return directory;
    }

    class TestPluginManager : PluginManagerBase
    {
        readonly string[] directories;

        public TestPluginManager(string[] directories) : base(global: false)
        {
            this.directories = directories;
        }

        [ImportMany]
        public IEnumerable<IDisposable> Found { get; set; } = Array.Empty<IDisposable>();

        protected override void AddDirectoriesForInstance(List<string> pluginDirectories, string searchPath)
        {
            pluginDirectories.AddRange(directories);
        }
    }
}
