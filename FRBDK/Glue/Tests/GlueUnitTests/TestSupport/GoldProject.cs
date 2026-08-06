using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

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
    /// Deletes every *.Generated.cs under <paramref name="directory"/>, and returns how many were removed.
    ///
    /// Mandatory before loading, and the single easiest thing to get wrong about this test. Glue does not
    /// rewrite a generated file whose content is unchanged, so with the checked-in files left in place
    /// "codegen produced this" and "codegen never ran and the old file is still sitting there" look
    /// identical - the build passes either way and the test proves nothing. Deleting first turns any
    /// generator that fails to run into a compile error naming the missing file.
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
            // the temp directory starts from scratch anyway.
            if (name is "bin" or "obj")
            {
                continue;
            }
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }
            File.Copy(file, Path.Combine(destination, relative), overwrite: true);
        }
    }

    static string FindRepoRoot()
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
