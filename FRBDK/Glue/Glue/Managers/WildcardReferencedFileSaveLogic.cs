using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Managers
{
    public class WildcardReferencedFileSaveLogic
    {
        private static object glujFilePath;

        public static void LoadWildcardReferencedFiles(FilePath glujFilePath, GlueProjectSave mainGlueProjectSave)
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

            HashSet<FilePath> filesReferencedByWildcards = new HashSet<FilePath>();

            List<FilePath> wildcardRootPaths = GetRootPaths(glujFilePath, wildcardRfses);

            // This may help, but trying plinq first
            //HashSet<FilePath> allFiles = new HashSet<FilePath>();
            //foreach(var root in wildcardRootPaths)
            //{

            //    var filesInRoot = Directory.GetFiles(root.FullPath, "*", SearchOption.AllDirectories)
            //        .Select(file => new FilePath(file));
            //    allFiles.AddRange(filesInRoot);
            //}

            foreach (var wildcardRfs in wildcardRfses)
            {
                mainGlueProjectSave.GlobalFiles.Remove(wildcardRfs);
                mainGlueProjectSave.GlobalFileWildcards.Add(wildcardRfs);

            }

            ConcurrentBag<ReferencedFileSave> newRfses = new();

            //foreach (var wildcardRfs in wildcardRfses)
            Parallel.ForEach(wildcardRfses, wildcardRfs =>
            {
                //var absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(wildcardRfs);
                var absoluteFile = new FilePath(contentFolder + wildcardRfs.Name);
                List<FilePath> files = new List<FilePath>();

                try
                {
                    files = GetFilesForWildcard(absoluteFile, null);
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

            foreach(var item in newRfses)
            {
                mainGlueProjectSave.GlobalFiles.Add(item);
            }


        }

        private static List<FilePath> GetRootPaths(FilePath glujFilePath, ReferencedFileSave[] wildcardRfses)
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

        public static List<FilePath> GetFilesForWildcard(FilePath filePath, HashSet<FilePath>? allFiles)
        {
            FilePath directoryWithNoWildcard = filePath;
            while (directoryWithNoWildcard.FullPath.Contains("*"))
            {
                directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
            }

            var suffix = filePath.RelativeTo(directoryWithNoWildcard);

            List<FilePath> filesObtainedOldWay = new List<FilePath>();
            GetFilesForWildcard(directoryWithNoWildcard, suffix, filesObtainedOldWay);

            // In theory this could be faster, but I'm not sure if it is, and not sure if it's even needed
            // if we plinq everything...
            //List<FilePath>? filesObtainedNewWay = null;
            //if(allFiles != null)
            //{
            //    var matcher = new Matcher();
            //    matcher.AddInclude(suffix);



            //    filesObtainedNewWay = allFiles
            //            .Where(f => matcher.Match(f.FullPath.Substring(3)).HasMatches)
            //            .ToList();


            //}

            return filesObtainedOldWay;
        }


        private static List<FilePath> GetFilesForWildcard(FilePath directoryWithNoWildcard, string relativePathWithWildcard, List<FilePath> foundMatches)
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
                    GetFilesForWildcard(directoryWithNoWildcard.FullPath, remainderSuffix ?? "*", foundMatches);
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
                        GetFilesForWildcard(directory, "*", foundMatches);
                    }
                    else
                    {
                        GetFilesForWildcard(directory, "**/" + remainderSuffix, foundMatches);
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

        // AI says we can do this:
//        using System.IO.Enumeration;

//List<string> allFiles = new List<string>
//{
//    "file1.png",
//    "file2.jpg",
//    "file3.PNG",
//    "subfolder/file4.png"
//};
//    string pattern = "*.png";
//    var matchedFiles = allFiles
//        .Where(f => FileSystemName.MatchesSimpleExpression(pattern, Path.GetFileName(f), ignoreCase: true))
//        .ToList();




    private static object GetRfsFromFile(GlueProjectSave glueProjectSave, FilePath file, FilePath glueProjectDirectory)
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

        private static FilePath GetAbsoluteFilePathFor(FilePath glueProjectDirectory, ReferencedFileSave rfs)
        {
            FilePath prefix = glueProjectDirectory + "Content/";

            return prefix + rfs.Name;
        }
    }
}
