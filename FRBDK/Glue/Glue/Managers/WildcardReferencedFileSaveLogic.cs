using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.Managers;

public class WildcardReferencedFileSaveLogic
{
    private readonly IGlueCommands _glueCommands;
    private readonly IFileCommands _fileCommands;

    public WildcardReferencedFileSaveLogic(
        IGlueCommands glueCommands,
        IFileCommands fileCommands
        )
    {
        _glueCommands = glueCommands;
        _fileCommands = fileCommands;
    }

    public void LoadWildcardReferencedFiles(FilePath glujFilePath, GlueProjectSave mainGlueProjectSave)
    {
        // the csproj may not have loaded yet, so we can't rely on that:
        FilePath glueProjectDirectory = glujFilePath.GetDirectoryContainingThis();
        var contentFolder = glueProjectDirectory + "Content/";
        var globalContentFolder = contentFolder + "GlobalContent/";

        // To save comparisons, let's do a dictionary:
        ConcurrentDictionary<FilePath, ReferencedFileSave> rfsDictionary = new ();
        foreach (var file in mainGlueProjectSave.GlobalFiles)
        {
            var filePath = GetAbsoluteFilePathFor(contentFolder, file);

            rfsDictionary[filePath] = file;
        }
        foreach (var screen in mainGlueProjectSave.Screens)
        {
            foreach (var file in screen.ReferencedFiles)
            {
                var filePath = GetAbsoluteFilePathFor(contentFolder, file);

                rfsDictionary[filePath] = file;
            }
        }
        for (int i = 0; i < mainGlueProjectSave.Entities.Count; i++)
        {
            var entity = mainGlueProjectSave.Entities[i];
            foreach (var file in entity.ReferencedFiles)
            {

                var filePath = GetAbsoluteFilePathFor(contentFolder, file);

                rfsDictionary[filePath] = file;
            }
        }

        var wildcardRfses = mainGlueProjectSave.GlobalFiles.Where(item => item.Name.Contains("*")).ToArray();

        foreach (var wildcardRfs in wildcardRfses)
        {
            mainGlueProjectSave.GlobalFiles.Remove(wildcardRfs);
            mainGlueProjectSave.GlobalFileWildcards.Add(wildcardRfs);
        }

        ConcurrentBag<ReferencedFileSave> newRfses = new();

        //foreach (var wildcardRfs in wildcardRfses)
        Parallel.ForEach(wildcardRfses, wildcardRfs =>
        {
            var absoluteFile = new FilePath(contentFolder + wildcardRfs.Name);
            List<FilePath> files = new List<FilePath>();

            try
            {
                files = GetFilesMatchingWildcardPattern(absoluteFile);
            }
            catch (DirectoryNotFoundException ex)
            {
                GlueCommands.Self.PrintError($"Error processing wildcard pattern {wildcardRfs.Name}:\n{ex}");
            }

            foreach (var filePathForPossibleRfs in files)
            {
                if (!rfsDictionary.ContainsKey(filePathForPossibleRfs))
                {
                    var clone = wildcardRfs.Clone();
                    clone.IsCreatedByWildcard = true;
                    clone.Name = filePathForPossibleRfs.RelativeTo(contentFolder);
                    newRfses.Add(clone);
                    rfsDictionary[filePathForPossibleRfs] = clone;
                }
            }
        });

        // Parallelization causes files to arrive at the list at random times causing sort changes
        // every time startup is run and this causing noise in GlobalContent.Generated.cs.
        // We sort the list to avoid it.
        var sortedReferences = newRfses.OrderBy(r => r.Name).ToList();

        mainGlueProjectSave.GlobalFiles.AddRange(sortedReferences);
    }

    private List<FilePath> GetRootPaths(FilePath glujFilePath, ReferencedFileSave[] wildcardRfses)
    {
        var rootPaths = new List<FilePath>();

        FilePath glueProjectDirectory = glujFilePath.GetDirectoryContainingThis();
        var contentFolder = glueProjectDirectory + "Content/";

        foreach (var wildcardRfs in wildcardRfses)
        {
            var absoluteFile = new FilePath(contentFolder + wildcardRfs.Name);

            FilePath directoryWithNoWildcard = absoluteFile;
            while (directoryWithNoWildcard.FullPath.Contains("*"))
            {
                directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
            }

            var alreadyContained = rootPaths.Any(item => item == directoryWithNoWildcard);
            if(alreadyContained)
            {
                continue;
            }

            var isParentOfAny = rootPaths.Any(item => item.IsRelativeTo(directoryWithNoWildcard));
            if(isParentOfAny)
            {
                rootPaths.RemoveAll(item => item.IsRelativeTo(directoryWithNoWildcard));

                rootPaths.Add(directoryWithNoWildcard);
                continue;
            }

            var isChildOfAny = rootPaths.Any(item => directoryWithNoWildcard.IsRelativeTo(item));
            if(isChildOfAny)
            {
                continue;
            }

            // If we got here, it's a new path
            rootPaths.Add(directoryWithNoWildcard);
        }

        return rootPaths;
    }

    public List<FilePath> GetFilesMatchingWildcardPattern(FilePath filePath)
    {
        FilePath directoryWithNoWildcard = filePath;
        while (directoryWithNoWildcard.FullPath.Contains("*"))
        {
            directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
        }

        var suffix = filePath.RelativeTo(directoryWithNoWildcard);

        var filesObtained = new List<FilePath>();
        GetFilesMatchingWildcardPattern(directoryWithNoWildcard, suffix, filesObtained);

        return filesObtained;
    }

    private List<FilePath> GetFilesMatchingWildcardPattern(FilePath directoryWithNoWildcard, string relativePathWithWildcard, List<FilePath> foundMatches)
    {
        var singleSuffix = relativePathWithWildcard;
        if (singleSuffix.Contains('/'))
        {
            singleSuffix = singleSuffix.Substring(0, singleSuffix.IndexOf('/'));
        }

        string remainderSuffix = null;
        if (singleSuffix != relativePathWithWildcard)
        {
            remainderSuffix = relativePathWithWildcard.Substring(singleSuffix.Length, relativePathWithWildcard.Length - singleSuffix.Length);

            if(remainderSuffix.StartsWith("/"))
            {
                remainderSuffix = remainderSuffix.Substring(1);
            }
        }

        if (singleSuffix == "*")
        {
            if(remainderSuffix == null)
            {
                // for now assume /*. Expand on this...
                var tempFiles = FileManager.GetAllFilesInDirectory(directoryWithNoWildcard.FullPath, null, 0)
                    .Select(item => new FilePath(item))
                    .ToList();

                foundMatches.AddRange(tempFiles);
            }
            else
            {
                // do we allow this?
            }

        }
        else if (singleSuffix == "**")
        {
            if(remainderSuffix?.Contains('/') != true)
            {
                // We don't have anymore folders, so that means we want to have all files in here too. For example
                // "**/*.txt" means "all txt files in this folder plus subfolders"
                GetFilesMatchingWildcardPattern(directoryWithNoWildcard.FullPath, remainderSuffix ?? "*", foundMatches);
            }

            if(System.IO.Directory.Exists(directoryWithNoWildcard.FullPath) == false)
            {
                throw new DirectoryNotFoundException("Could not find the directory " + directoryWithNoWildcard.FullPath);
            }

            var directories = System.IO.Directory.GetDirectories(directoryWithNoWildcard.FullPath);
            foreach(var directory in directories)
            {
                if(remainderSuffix == null)
                {
                    GetFilesMatchingWildcardPattern(directory, "*", foundMatches);
                }
                else
                {
                    GetFilesMatchingWildcardPattern(directory, "**/" + remainderSuffix, foundMatches);
                }
            }
        }
        else if (singleSuffix.Contains("."))
        {
            if(remainderSuffix == null)
            {
                if(directoryWithNoWildcard.Exists())
                {
                    var filesTemp = System.IO.Directory.GetFiles(directoryWithNoWildcard.FullPath, singleSuffix).Select(item => new FilePath(item));
                    foundMatches.AddRange(filesTemp);
                }
            }
            else
            {
                // do we allow this?
            }
        }
        return foundMatches;
    }

    private object GetRfsFromFile(GlueProjectSave glueProjectSave, FilePath file, FilePath glueProjectDirectory)
    {
        foreach (var candidate in glueProjectSave.GlobalFiles)
        {
            if (file == GetAbsoluteFilePathFor(glueProjectDirectory, candidate))
            {
                return candidate;
            }
        }
        foreach (var screen in glueProjectSave.Screens)
        {
            foreach (var candidate in screen.ReferencedFiles)
            {
                if (file == GetAbsoluteFilePathFor(glueProjectDirectory, candidate))
                {
                    return candidate;
                }
            }
        }
        foreach (var entity in glueProjectSave.Entities)
        {
            foreach (var candidate in entity.ReferencedFiles)
            {
                if (file == GetAbsoluteFilePathFor(glueProjectDirectory, candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private FilePath GetAbsoluteFilePathFor(FilePath glueProjectDirectory, ReferencedFileSave rfs)
    {
        FilePath prefix = glueProjectDirectory + "Content/";

        return prefix + rfs.Name;
    }

    internal void RemoveMissingWildcardFilesFrom(
        GlueProjectSave glueProject,
        VSHelpers.Projects.VisualStudioProject visualStudioProject)
    {
        var projectFile = visualStudioProject.Directory;

        var toRemove = new List<ProjectItem>();

        foreach (var itemInVisualStudio in visualStudioProject.EvaluatedItems)
        //Parallel.ForEach(visualStudioProject.EvaluatedItems, itemInVisualStudio =>
        {
            var relativeFile = itemInVisualStudio.EvaluatedInclude;

            if (relativeFile != null)
            {
                FilePath absoluteFile = projectFile + relativeFile;

                var isContent =
                    itemInVisualStudio.ItemType == "None" &&
                    !string.IsNullOrEmpty(absoluteFile.Extension) &&
                    absoluteFile.Extension != "cs" &&
                    _fileCommands.IsContent(absoluteFile);

                if (isContent && absoluteFile.Exists() == false)
                {
                    foreach (var pattern in glueProject.GlobalFileWildcards)
                    {
                        var absolutePattern = _glueCommands.GetAbsoluteFilePath(pattern);
                        // does it match?
                        if (IsMatch(absolutePattern, absoluteFile))
                        {
                            // remove it!
                            toRemove.Add(itemInVisualStudio);
                            break;
                        }
                    }
                }
            }
        }//);

        if(toRemove.Count > 0)
        {
            visualStudioProject.RemoveItems(toRemove);
            
            _glueCommands.TryMultipleTimes(visualStudioProject.Save);
        }
    }

    public bool IsMatch(FilePath absoluteFile, FilePath pattern)
    {
        return IsMatch(absoluteFile.FullPath, pattern.FullPath);
    }

    bool IsMatch(string pattern, string path)
    {
        // Normalize path separators to forward slashes
        pattern = pattern.Replace('\\', '/');
        path = path.Replace('\\', '/');

        // Convert the pattern to a regex pattern
        string regexPattern = ConvertToRegex(pattern);

        // Match the path against the regex
        return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    string ConvertToRegex(string pattern)
    {
        var result = new System.Text.StringBuilder();
        result.Append("^");

        int i = 0;
        while (i < pattern.Length)
        {
            if (i < pattern.Length - 1 && pattern[i] == '*' && pattern[i + 1] == '*')
            {
                // Handle ** (matches zero or more path segments)
                result.Append(".*");
                i += 2;

                // Skip the following slash if present
                if (i < pattern.Length && pattern[i] == '/')
                {
                    i++;
                }
            }
            else if (pattern[i] == '*')
            {
                // Handle * (matches anything except path separator)
                result.Append("[^/]*");
                i++;
            }
            else if (pattern[i] == '?')
            {
                // Handle ? (matches single character except path separator)
                result.Append("[^/]");
                i++;
            }
            else
            {
                // Escape special regex characters
                char c = pattern[i];
                if (".^$+{}[]()\\|".Contains(c))
                {
                    result.Append('\\');
                }
                result.Append(c);
                i++;
            }
        }

        result.Append("$");
        return result.ToString();
    }
}
