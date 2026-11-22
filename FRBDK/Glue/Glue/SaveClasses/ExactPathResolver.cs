using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FlatRedBall.Glue.SaveClasses;

/// <summary>
/// Finds the capitalization a file actually has on disk, for every folder in its path.
/// </summary>
internal class ExactPathResolver
{
    /// <summary>
    /// How many times a folder was asked for the on-disk spelling of one of its entries.
    /// </summary>
    public int DirectoryLookupCount { get; private set; }

    // The on-disk spelling of each folder already resolved, keyed by its path in whatever capitalization it was
    // asked for. The file at the end of a path is never remembered, only the folders above it.
    private readonly Dictionary<string, string> _folderNames = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    public bool TryGetExactPath(string path, out string exactPath)
    {
        bool result = false;
        exactPath = null;

        // DirectoryInfo accepts either a file path or a directory path, and most of its properties work for either.
        // However, its Exists property only works for a directory path.
        DirectoryInfo directory = new DirectoryInfo(path);
        if (File.Exists(path) || directory.Exists)
        {
            List<string> parts = new List<string>();

            DirectoryInfo parentDirectory = directory.Parent;
            bool isLeaf = true;
            while (parentDirectory != null)
            {
                string exactName;
                if (isLeaf ||!_folderNames.TryGetValue(directory.FullName, out exactName))
                {
                    DirectoryLookupCount++;
                    FileSystemInfo entry = parentDirectory.EnumerateFileSystemInfos(directory.Name).First();
                    exactName = entry.Name;

                    if (!isLeaf)
                    {
                        _folderNames[directory.FullName] = exactName;
                    }
                }
                isLeaf = false;
                parts.Add(exactName);

                directory = parentDirectory;
                parentDirectory = directory.Parent;
            }

            // Handle the root part (i.e., drive letter or UNC \\server\share).
            string root = directory.FullName;
            if (root.Contains(':'))
            {
                root = root.ToUpper();
            }
            else
            {
                string[] rootParts = root.Split('\\');
                root = string.Join("\\", rootParts.Select(part => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(part)));
            }

            parts.Add(root);
            parts.Reverse();
            exactPath = Path.Combine(parts.ToArray());
            result = true;
        }

        return result;
    }
}
