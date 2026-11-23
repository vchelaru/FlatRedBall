using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GlueCli;

/// <summary>
/// Makes the temp copies <c>--dry-run</c> runs against. The runner takes one (tests pass their own temp parent);
/// see <see cref="CliDryRunCopy"/> for what a copy is.
/// </summary>
public sealed class CliDryRunCopier
{
    readonly string? tempParent;
    readonly UnifiedDiff diff;

    /// <param name="tempParent">Where copies go. Null picks the system temp folder, or a folder at the root of
    /// the project's volume when temp is on another one, so art and audio can be hard-linked.</param>
    public CliDryRunCopier(string? tempParent = null, UnifiedDiff? diff = null)
    {
        this.tempParent = string.IsNullOrEmpty(tempParent) ? null : Path.GetFullPath(tempParent);
        this.diff = diff ?? new UnifiedDiff();
    }

    public CliDryRunCopy Copy(string originalCsproj)
    {
        var originalRoot = Path.GetDirectoryName(Path.GetFullPath(originalCsproj))!;
        return new CliDryRunCopy(originalCsproj, tempParent ?? ChooseTempParent(originalRoot), diff);
    }

    static string ChooseTempParent(string originalRoot)
    {
        var temp = Path.GetTempPath();
        var volume = Path.GetPathRoot(originalRoot)!;
        if (!OperatingSystem.IsWindows() ||
            string.Equals(Path.GetPathRoot(temp), volume, StringComparison.OrdinalIgnoreCase))
        {
            return temp;
        }
        var onVolume = Path.Combine(volume, CliDryRunCopy.VolumeTempFolderName);
        try
        {
            Directory.CreateDirectory(onVolume);
            return onVolume;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return temp;
        }
    }
}

/// <summary>
/// What <c>--dry-run</c> runs against: a copy of the .csproj's folder (bin, obj, .vs and .git skipped). The
/// command runs there in full (load, edit, plugin reactions, codegen, save), <see cref="ComputeChanges"/>
/// compares the copy with how it started, and <see cref="Dispose"/> deletes it. Glue and its plugins write files
/// from many places, so intercepting saves could miss one; with a copy, nothing in the real project folder is
/// ever opened for writing.
///
/// On Windows, art, audio and binaries are hard-linked instead of copied when the temp folder is on the
/// project's volume, so a game with hundreds of MB of content stays cheap. A link shares its content with the
/// original, so each one is held open without write sharing for the whole run: sharing is enforced on the file,
/// not the name, so nothing can open the original for writing through either name. A write to one fails (and
/// is reported) instead of reaching the project. Replacing or deleting the link still works and leaves the
/// original alone. Elsewhere everything is copied, since file locks there are advisory.
///
/// The copy sits two levels down in its own container folder, mirroring the original's parent and grandparent
/// with only their .sln files, which Glue needs to load. A write that climbs out of the project folder (to the
/// .sln) lands in the container and is reported as <c>../name</c>.
/// </summary>
public sealed class CliDryRunCopy : IDisposable
{
    internal const string VolumeTempFolderName = ".GlueCliDryRun";

    readonly string container;
    readonly string originalRoot;
    readonly UnifiedDiff diff;
    readonly List<FileStream> linkLocks = new();
    readonly HashSet<string> linkedFiles = new(StringComparer.OrdinalIgnoreCase);
    // Keyed by full path in the copy. Hashed from the source as it is copied: reading a file just written costs
    // an antivirus scan per file, which made up most of the copy's time on a few hundred files.
    readonly Dictionary<string, FileSnapshot> before = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The copy of the .csproj's folder.</summary>
    public string Root { get; }

    /// <summary>The copied .csproj, which Glue loads in place of the original.</summary>
    public string Csproj { get; }

    /// <summary>How many files were hard-linked rather than copied.</summary>
    public int LinkedFileCount => linkedFiles.Count;

    /// <param name="Hash">Null for a hard link, which is never read.</param>
    /// <param name="Text">The content of a file that gets a diff.</param>
    sealed record FileSnapshot(long Length, long WriteTicks, string? Hash, string? Text);

    // Files that get a unified diff in the report. Generated code is listed without one: it is long and
    // follows from the JSON.
    static readonly HashSet<string> DiffedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gluj", ".glsj", ".glej", ".json",
    };

    // Art, audio, video and binaries: the bulk of a game folder, read by Glue but never written by it.
    // Everything else (JSON, code, .csproj, Tiled, .achx, Gum files, anything a plugin might rewrite) is copied.
    static readonly HashSet<string> LinkedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".dds", ".psd", ".ase", ".aseprite", ".webp",
        ".wav", ".ogg", ".mp3", ".wma", ".m4a", ".flac", ".mp4", ".webm", ".wmv", ".avi",
        ".xnb", ".ttf", ".otf", ".dll", ".pdb", ".exe", ".zip",
    };

    internal CliDryRunCopy(string originalCsproj, string tempParent, UnifiedDiff diff)
    {
        this.diff = diff;
        originalRoot = Path.GetDirectoryName(Path.GetFullPath(originalCsproj))!;
        container = Path.Combine(tempParent, "GlueCliDryRun_" + Guid.NewGuid().ToString("N"));
        var originalParent = Path.GetDirectoryName(originalRoot);
        var parentName = originalParent == null ? "Parent" : Path.GetFileName(originalParent);
        Root = Path.Combine(container, string.IsNullOrEmpty(parentName) ? "Parent" : parentName, Path.GetFileName(originalRoot));
        Csproj = Path.Combine(Root, Path.GetFileName(originalCsproj));
        try
        {
            CopyDirectory(originalRoot, Root);
            // Glue looks for the project's .sln next to the .csproj and up to two folders above it
            // (ProjectSyncer.LocateSolution), and loading fails without one.
            CopySolutionFiles(originalParent, Path.GetDirectoryName(Root)!);
            CopySolutionFiles(originalParent == null ? null : Path.GetDirectoryName(originalParent), container);
            foreach (var csproj in Directory.GetFiles(Root, "*.csproj", SearchOption.AllDirectories))
            {
                MakeOutsidePathsAbsolute(csproj, Path.Combine(originalRoot, Path.GetRelativePath(Root, csproj)));
            }
            foreach (var file in linkedFiles)
            {
                linkLocks.Add(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            if (LinkedExtensions.Contains(Path.GetExtension(file)) && OperatingSystem.IsWindows() &&
                CreateHardLink(target, file, IntPtr.Zero))
            {
                linkedFiles.Add(target);
                Record(target, bytes: null);
            }
            else
            {
                CopyFile(file, target);
            }
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            if (!IsSkippedFolder(Path.GetFileName(directory)))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }

    void CopySolutionFiles(string? source, string destination)
    {
        if (source == null || !Directory.Exists(source))
        {
            return;
        }
        foreach (var sln in Directory.EnumerateFiles(source, "*.sln").Concat(Directory.EnumerateFiles(source, "*.slnx")))
        {
            CopyFile(sln, Path.Combine(destination, Path.GetFileName(sln)));
        }
    }

    // File.Copy keeps the source's write time, which is what makes a later write always look different.
    void CopyFile(string source, string target)
    {
        File.Copy(source, target);
        Record(target, File.ReadAllBytes(source));
    }

    void Record(string file, byte[]? bytes)
    {
        var info = new FileInfo(file);
        before[file] = new FileSnapshot(info.Length, info.LastWriteTimeUtc.Ticks,
            bytes == null ? null : Hash(bytes), bytes == null ? null : DiffableText(file, bytes));
    }

    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    static string? DiffableText(string file, byte[] bytes) =>
        DiffedExtensions.Contains(Path.GetExtension(file)) ? Encoding.UTF8.GetString(bytes) : null;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    // Build output and tool caches: large, rebuilt from scratch, and never part of an edit.
    static bool IsSkippedFolder(string name) => name is "bin" or "obj" or ".vs" or ".git";

    static readonly Regex PathAttribute = new(
        "(<(?:ProjectReference|Import)\\b[^>]*?\\b(?:Include|Project)\\s*=\\s*\")([^\"]+)(\")",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// A ProjectReference or Import that climbs out of the project folder would point at nothing from the copy,
    /// and a missing Import fails the load. Those become absolute paths to where they point from the original.
    /// Only the attribute values change, and the comparison baseline is taken after this, so it never shows up
    /// as a change.
    /// </summary>
    void MakeOutsidePathsAbsolute(string copiedCsproj, string originalCsproj)
    {
        var bytes = File.ReadAllBytes(copiedCsproj);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var text = File.ReadAllText(copiedCsproj);
        var originalDirectory = Path.GetDirectoryName(originalCsproj)!;

        var changed = false;
        var rewritten = PathAttribute.Replace(text, match =>
        {
            var value = match.Groups[2].Value;
            if (value.Contains("$(") || Path.IsPathRooted(value))
            {
                return match.Value;
            }
            var absolute = Path.GetFullPath(Path.Combine(originalDirectory, value.Replace('\\', Path.DirectorySeparatorChar)));
            if (IsUnder(absolute, originalRoot))
            {
                return match.Value;
            }
            changed = true;
            return match.Groups[1].Value + absolute + match.Groups[3].Value;
        });

        if (changed)
        {
            File.WriteAllText(copiedCsproj, rewritten, new UTF8Encoding(hasBom));
            File.SetLastWriteTimeUtc(copiedCsproj, File.GetLastWriteTimeUtc(originalCsproj));
            Record(copiedCsproj, File.ReadAllBytes(copiedCsproj));
        }
    }

    static bool IsUnder(string path, string root) =>
        path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    static IEnumerable<string> EnumerateFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (IsSkippedFolder(Path.GetFileName(child)))
            {
                continue;
            }
            foreach (var file in EnumerateFiles(child))
            {
                yield return file;
            }
        }
    }

    /// <summary>What changed in the copy since it was made, relative to the .csproj folder.</summary>
    /// <remarks>
    /// A file with its recorded size and write time is unchanged and is not read. Anything else is read and
    /// hashed, so a file rewritten with the same content is not reported. A hard link is locked against
    /// writing, so it changes only by being deleted or replaced, which gives it a new write time.
    /// </remarks>
    public List<CliFileChange> ComputeChanges()
    {
        var changes = new List<CliFileChange>();
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in EnumerateFiles(container))
        {
            present.Add(file);
            var info = new FileInfo(file);
            before.TryGetValue(file, out var old);
            if (old != null && old.Length == info.Length && old.WriteTicks == info.LastWriteTimeUtc.Ticks)
            {
                continue;
            }

            var path = RelativePath(file);
            if (old != null && old.Hash == null)
            {
                changes.Add(new CliFileChange(path, "modified", null));
                continue;
            }
            var bytes = File.ReadAllBytes(file);
            if (old != null && old.Hash == Hash(bytes))
            {
                continue;
            }
            var text = DiffableText(file, bytes);
            changes.Add(old == null
                ? new CliFileChange(path, "added", text == null ? null : diff.Create(path, "", text))
                : new CliFileChange(path, "modified", text == null || old.Text == null ? null : diff.Create(path, old.Text, text)));
        }
        foreach (var (file, old) in before)
        {
            if (!present.Contains(file))
            {
                var path = RelativePath(file);
                changes.Add(new CliFileChange(path, "deleted", old.Text == null ? null : diff.Create(path, old.Text, "")));
            }
        }
        return changes.OrderBy(change => change.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    string RelativePath(string file) => Path.GetRelativePath(Root, file).Replace('\\', '/');

    /// <summary>Whether <paramref name="path"/> is inside the copy's container.</summary>
    public bool Contains(string path) => IsUnder(Path.GetFullPath(path), container);

    /// <summary>Replaces the copy's folder with the original's in <paramref name="text"/>.</summary>
    public string MapPaths(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        var pairs = new[]
        {
            (Root, originalRoot),
            (Root.Replace('\\', '/'), originalRoot.Replace('\\', '/')),
            // As it appears inside JSON strings.
            (Root.Replace("\\", "\\\\"), originalRoot.Replace("\\", "\\\\")),
        };
        foreach (var (from, to) in pairs)
        {
            text = text.Replace(from, to, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }

    /// <summary>Points every path in <paramref name="result"/> back at the original project.</summary>
    public void MapResultPaths(CliResult result)
    {
        result.Failure = result.Failure == null ? null : MapPaths(result.Failure);
        result.EditSummary = result.EditSummary == null ? null : MapPaths(result.EditSummary);
        result.EditFailures = result.EditFailures.Select(MapPaths).ToList();
        result.Errors = result.Errors.Select(error => error with { Details = MapPaths(error.Details) }).ToList();
        result.OutputErrors = result.OutputErrors.Select(MapPaths).ToList();
        result.DialogMessages = result.DialogMessages.Select(MapPaths).ToList();
        result.PreexistingErrors = result.PreexistingErrors.Select(error => error with { Details = MapPaths(error.Details) }).ToList();
        // Edit is a command's own object; the runner maps it in the serialized --json output instead.
    }

    /// <summary>Releases the link locks and deletes the copy. Deleting a hard link removes only that name.</summary>
    public void Dispose()
    {
        foreach (var handle in linkLocks)
        {
            handle.Dispose();
        }
        linkLocks.Clear();
        try
        {
            if (Directory.Exists(container))
            {
                Directory.Delete(container, recursive: true);
            }
            var parent = Path.GetDirectoryName(container)!;
            if (Path.GetFileName(parent) == VolumeTempFolderName && !Directory.EnumerateFileSystemEntries(parent).Any())
            {
                Directory.Delete(parent);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A file watcher or plugin may still hold a handle. It is a temp folder; leaving it is harmless.
        }
    }
}

/// <summary>A file a dry run would create, change or delete. <see cref="Diff"/> is set for JSON files.</summary>
public sealed record CliFileChange(string Path, string Kind, string? Diff);

/// <summary>A line-based unified diff with three lines of context.</summary>
public sealed class UnifiedDiff
{
    const int Context = 3;

    // Past this many cells the changed middle of the file is shown as removed then added rather than aligned.
    const long MaxLcsCells = 4_000_000;

    public string Create(string path, string before, string after)
    {
        var ops = Diff(SplitLines(before), SplitLines(after));

        var lines = new List<string> { $"--- a/{path}", $"+++ b/{path}" };
        var i = 0;
        while (i < ops.Count)
        {
            if (ops[i].Kind == ' ')
            {
                i++;
                continue;
            }
            var start = Math.Max(0, i - Context);
            var end = i;
            // Grow the hunk while the next change is close enough to share context.
            while (true)
            {
                while (end < ops.Count && ops[end].Kind != ' ') end++;
                var next = end;
                while (next < ops.Count && ops[next].Kind == ' ') next++;
                if (next < ops.Count && next - end <= Context * 2)
                {
                    end = next;
                    continue;
                }
                end = Math.Min(ops.Count, end + Context);
                break;
            }

            var hunk = ops.GetRange(start, end - start);
            var aCount = hunk.Count(op => op.Kind != '+');
            var bCount = hunk.Count(op => op.Kind != '-');
            var aStart = aCount == 0 ? ops[start].ALine : ops[start].ALine + 1;
            var bStart = bCount == 0 ? ops[start].BLine : ops[start].BLine + 1;
            lines.Add($"@@ -{aStart},{aCount} +{bStart},{bCount} @@");
            lines.AddRange(hunk.Select(op => op.Kind + op.Text));
            i = end;
        }
        return string.Join("\n", lines);
    }

    static string[] SplitLines(string text) =>
        text.Length == 0 ? Array.Empty<string>() : text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

    /// <param name="ALine">Zero-based line in the old file: this line, or for an add the next old line.</param>
    /// <param name="BLine">Zero-based line in the new file: this line, or for a removal the next new line.</param>
    readonly record struct Op(char Kind, string Text, int ALine, int BLine);

    static List<Op> Diff(string[] a, string[] b)
    {
        var prefix = 0;
        while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix]) prefix++;
        var suffix = 0;
        while (suffix < a.Length - prefix && suffix < b.Length - prefix &&
            a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix]) suffix++;

        var ops = new List<Op>();
        for (var k = 0; k < prefix; k++) ops.Add(new Op(' ', a[k], k, k));

        var aMid = a.Length - prefix - suffix;
        var bMid = b.Length - prefix - suffix;
        if ((long)aMid * bMid > MaxLcsCells)
        {
            for (var k = 0; k < aMid; k++) ops.Add(new Op('-', a[prefix + k], prefix + k, prefix));
            for (var k = 0; k < bMid; k++) ops.Add(new Op('+', b[prefix + k], prefix + aMid, prefix + k));
        }
        else
        {
            // lcs[x, y]: length of the longest common subsequence of the middles from x and y onwards.
            var lcs = new int[aMid + 1, bMid + 1];
            for (var x = aMid - 1; x >= 0; x--)
                for (var y = bMid - 1; y >= 0; y--)
                    lcs[x, y] = a[prefix + x] == b[prefix + y] ? lcs[x + 1, y + 1] + 1 : Math.Max(lcs[x + 1, y], lcs[x, y + 1]);

            int i = 0, j = 0;
            while (i < aMid || j < bMid)
            {
                if (i < aMid && j < bMid && a[prefix + i] == b[prefix + j])
                {
                    ops.Add(new Op(' ', a[prefix + i], prefix + i, prefix + j));
                    i++; j++;
                }
                else if (i < aMid && (j == bMid || lcs[i + 1, j] >= lcs[i, j + 1]))
                {
                    ops.Add(new Op('-', a[prefix + i], prefix + i, prefix + j));
                    i++;
                }
                else
                {
                    ops.Add(new Op('+', b[prefix + j], prefix + i, prefix + j));
                    j++;
                }
            }
        }

        for (var k = 0; k < suffix; k++)
        {
            ops.Add(new Op(' ', a[a.Length - suffix + k], a.Length - suffix + k, b.Length - suffix + k));
        }
        return ops;
    }
}
