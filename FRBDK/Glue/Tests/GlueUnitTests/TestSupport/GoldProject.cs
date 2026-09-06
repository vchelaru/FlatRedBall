using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Drives one checked-in FlatRedBall game project ("gold project") through the real editor pipeline:
/// copy it out of the repo, run <c>ProjectLoader.LoadProject</c> on the copy exactly as Glue.exe does on
/// File > Load Project, then <c>dotnet build</c> the result.
///
/// This is the only kind of test that exercises code generation as it actually runs. Glue's generators are
/// plugins, and several of them decide what to emit by asking another plugin a question at generation time
/// (<c>PluginManager.CallPluginMethod("Gum Plugin", "HasGum")</c> gating <c>#define HasGum</c> is the
/// canonical one). Codegen unit tests call a single generator directly and so never cross those seams; a
/// gold project crosses all of them at once and then proves the result compiles. See GitHub issue #1973.
///
/// Gold projects are the repo's own sample/test projects, not purpose-built fixtures - they are already
/// maintained, already realistic, and cost nothing to keep current.
/// </summary>
internal static class GoldProject
{
    /// <summary>
    /// Copies <paramref name="repoRelativeProjectDirectory"/> to a fresh temp directory and returns the
    /// copy's root. The copy is what gets loaded and built, so a test run never dirties the working tree
    /// (the load rewrites .Generated.cs files and can rewrite the .csproj and .gluj themselves).
    /// </summary>
    public static TempDir CopyOutOfRepo(string repoRelativeProjectDirectory)
    {
        var source = Path.Combine(FindRepoRoot(), repoRelativeProjectDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Gold project not found at {source}");
        }

        var temp = new TempDir();
        CopyDirectory(source, temp.Root);

        // The samples reference the engine (and Gum, in the sibling Gum repo) with relative ProjectReference
        // paths, which no longer resolve once the project sits in a temp directory at a different depth.
        // Rewriting them to absolute paths back into the repo keeps the point of building against engine
        // *source*: generated code that calls a member the engine no longer has fails the build, which is
        // exactly the class of bug this test exists to catch.
        foreach (var csproj in Directory.GetFiles(temp.Root, "*.csproj", SearchOption.AllDirectories))
        {
            MakeProjectReferencesAbsolute(csproj, Path.Combine(source, Path.GetRelativePath(temp.Root, csproj)));
        }

        return temp;
    }

    /// <summary>
    /// Runs the real editor load on <paramref name="csprojPath"/>. Requires
    /// <see cref="GlueTestBootstrap.EnsureHeadlessProjectLoadReady"/> (and any per-plugin registration the
    /// project needs) to have run first, on an STA thread.
    /// </summary>
    public static async Task LoadInGlueAsync(string csprojPath)
    {
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        GlueTestBootstrap.RecordedDialogMessages.Clear();
        ErrorRecordingPlugin.Errors.Clear();
        ErrorRecordingPlugin.Output.Clear();
        await FlatRedBall.Glue.IO.ProjectLoader.Self.LoadProject(csprojPath);
    }

    /// <summary>
    /// Writes live edit's runtime half - the ~40 files <c>EmbeddedCodeManager</c> copies into a game
    /// project's GlueControl folder - into the currently loaded project, exactly as
    /// <c>MainCompilerPlugin.HandleGluxLoaded</c> does (both calls, in this order).
    ///
    /// Those files are <c>&lt;Compile Remove&gt;</c>d from GameCommunicationPlugin.csproj because they only
    /// compile against a game project, and no checked-in sample has live edit turned on, so a typo in
    /// <c>Embedded\**\*.cs</c> otherwise reaches every live-edit user before anything fails. See GitHub
    /// issue #1986.
    ///
    /// EmbedAll is called directly rather than by turning live edit on in the copied CompilerSettings.json,
    /// because the plugin that would react to that setting builds real WPF tabs and opens sockets on
    /// registration and isn't seamed for the test host. What's at risk is whether the embedded closure
    /// compiles, not whether the plugin decides to embed it.
    /// </summary>
    public static void EmbedLiveEditCode()
    {
        var wasSynchronous = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;

        // FileManager.RelativeDirectory is process-wide, and PluginManager pairs a push of it with a pop on
        // every plugin call, reporting an error when the two disagree. In production EmbedAll runs inside the
        // glux load that set it; called on its own here it leaves it pointing elsewhere, and the *next* gold
        // project load in the process is what fails - naming a plugin that has nothing to do with this.
        var relativeDirectory = FileManager.RelativeDirectory;
        try
        {
            EmbeddedCodeManager.EmbedAll(fullyGenerate: true);
            GlueCallsCodeGenerator.GenerateAll();

            // The samples set EnableDefaultCompileItems to false, so the embedded files only reach the
            // compiler through the Compile items EmbedAll added to the in-memory project.
            GlueCommands.Self.ProjectCommands.SaveProjects();
        }
        finally
        {
            TaskManager.SynchronousMode = wasSynchronous;
            FileManager.RelativeDirectory = relativeDirectory;
        }
    }

    /// <summary>
    /// The <c>#define</c> symbols at the top of an embedded GlueControl file, as emitted by
    /// <c>GlueControlCodeGenerator</c>'s {CompilerDirectives} substitution. Use this rather than searching the
    /// file text for "#define Foo": several defines prefix each other (<c>HasGum</c> is a prefix of the
    /// GluxVersions member <c>HasGumSkiaElements</c>), so a substring assertion passes on the wrong line and
    /// the conditional region it was meant to pin is never compiled.
    /// </summary>
    public static IReadOnlyCollection<string> EmbeddedDefines(string generatedFilePath) =>
        File.ReadAllLines(generatedFilePath)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("#define "))
            .Select(line => line.Substring("#define ".Length).Trim())
            .ToHashSet();

    /// <summary>
    /// Deletes every *.Generated.cs under <paramref name="directory"/>, and returns how many were removed.
    ///
    /// Mandatory before loading, and the single easiest thing to get wrong about this test. Glue does not
    /// rewrite a generated file whose content is unchanged, so with an earlier run's files left in place
    /// "codegen produced this" and "codegen never ran and the old file is still sitting there" look
    /// identical - the build passes either way and the test proves nothing. Deleting first turns any
    /// generator that fails to run into a compile error naming the missing file.
    ///
    /// Expect zero on a fresh clone: *.Generated.cs is gitignored repo-wide, so the count only reflects
    /// what a previous run or a real Glue session happened to leave behind. Don't assert on it.
    /// </summary>
    public static int DeleteGeneratedCode(string directory)
    {
        var files = Directory.GetFiles(directory, "*.Generated.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            File.Delete(file);
        }
        return files.Length;
    }

    /// <summary>
    /// Every *.Generated.cs under <paramref name="directory"/>, as paths relative to it, for asserting on
    /// the set of files a load produced. Asserting only on a zero build exit code is not enough: a
    /// regression that stops a plugin from generating anything makes the project *smaller*, and smaller
    /// still compiles.
    /// </summary>
    public static IReadOnlyList<string> GeneratedFiles(string directory) =>
        Directory.GetFiles(directory, "*.Generated.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(directory, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Calls FlatRedBallServices.InitializeCommandLine() on <paramref name="engineAssembly"/> via
    /// reflection, unless it is already initialized - re-running it isn't safe
    /// (InstructionManager.CreateInterpolators populates a static dictionary and throws on a duplicate
    /// key the second time). Multiple gold-project tests can resolve to the same resident engine
    /// assembly instance in one process (see PlatformerLadderGoldProject's engine-assembly-reuse
    /// comment), so whichever test's [StaFact] thread runs second must not re-initialize - it only needs
    /// to re-point the private static mPrimaryThreadId at its own thread, since
    /// FlatRedBallServices.IsThreadPrimary() enforces same-thread-as-init for things like ShapeManager.
    /// GlueUnitTests runs its whole assembly non-parallel (see glue-unit-test-bootstrap), so reassigning
    /// it here is race-free.
    /// </summary>
    public static void EnsureEngineInitialized(Assembly engineAssembly)
    {
        var flatRedBallServicesType = engineAssembly.GetType("FlatRedBall.FlatRedBallServices")!;
        var isInitialized = (bool)flatRedBallServicesType
            .GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        if (!isInitialized)
        {
            flatRedBallServicesType.GetMethod("InitializeCommandLine", Type.EmptyTypes)!.Invoke(null, null);
        }
        else
        {
            var primaryThreadIdField = flatRedBallServicesType.GetField(
                "mPrimaryThreadId", BindingFlags.NonPublic | BindingFlags.Static)!;
            primaryThreadIdField.SetValue(null, (int?)Environment.CurrentManagedThreadId);
        }
    }

    static void MakeProjectReferencesAbsolute(string copiedCsproj, string originalCsproj)
    {
        var document = XDocument.Load(copiedCsproj);
        var originalDirectory = Path.GetDirectoryName(originalCsproj)!;

        var references = document.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include"))
            .Where(a => a != null && !Path.IsPathRooted(a.Value))
            .ToList();

        if (references.Count == 0)
        {
            return;
        }

        foreach (var include in references)
        {
            var relative = include!.Value.Replace('/', Path.DirectorySeparatorChar);
            include.Value = Path.GetFullPath(Path.Combine(originalDirectory, relative));
        }

        document.Save(copiedCsproj);
    }

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(directory);
            // Copying a previous build's output roughly triples the copy and gains nothing - the build in
            // the temp directory starts from scratch anyway. ".vs" is Visual Studio's local IntelliSense
            // cache - copying it is pointless and its files (e.g. FileContentIndex/*.vsidx) are commonly
            // locked by an open VS instance, throwing IOException here for a reason unrelated to the
            // project being copied.
            if (name is "bin" or "obj" or ".vs")
            {
                continue;
            }
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj" or ".vs"))
            {
                continue;
            }
            File.Copy(file, Path.Combine(destination, relative), overwrite: true);
        }
    }

    internal static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Samples")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Engines")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate the repo root (a directory containing both Samples/ and Engines/) above " +
            AppContext.BaseDirectory);
    }
}
