using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Managers
{
    public class WildcardReferencedFileSaveLogic
    {

        public static void LoadWildcardReferencedFiles(FilePath fileName, GlueProjectSave mainGlueProjectSave)
        {
            var wildcardRfses = mainGlueProjectSave.GlobalFiles.Where(item => item.Name.Contains("*")).ToArray();

            // the csproj may not have loaded yet, so we can't rely on that:
            FilePath glueProjectDirectory = fileName.GetDirectoryContainingThis();
            var contentFolder = glueProjectDirectory + "Content/";
            var globalContentFolder = contentFolder + "GlobalContent/";

            // To save comparisons, let's do a dictionary:
            Dictionary<FilePath, ReferencedFileSave> rfsDictionary = new Dictionary<FilePath, ReferencedFileSave>();
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
            foreach (var entity in mainGlueProjectSave.Entities)
            {
                foreach (var file in entity.ReferencedFiles)
                {

                    var filePath = GetAbsoluteFilePathFor(contentFolder, file);

                    rfsDictionary[filePath] = file;
                }
            }

            foreach (var wildcardRfs in wildcardRfses)
            {
                mainGlueProjectSave.GlobalFiles.Remove(wildcardRfs);
                //var absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(wildcardRfs);
                var absoluteFile = new FilePath(contentFolder + wildcardRfs.Name);
                var files = GetFilesForWildcard(absoluteFile);

                foreach (var filePathForPossibleRfs in files)
                {
                    if(!rfsDictionary.ContainsKey(filePathForPossibleRfs))
                    {
                        var clone = wildcardRfs.Clone();
                        clone.IsCreatedByWildcard = true;
                        clone.Name = filePathForPossibleRfs.RelativeTo(contentFolder);
                        mainGlueProjectSave.GlobalFiles.Add(clone);
                        rfsDictionary[filePathForPossibleRfs] = clone;
                    }
                }
                mainGlueProjectSave.GlobalFileWildcards.Add(wildcardRfs);
            }


        }


        public static List<FilePath> GetFilesForWildcard(FilePath filePath)
        {
            FilePath directoryWithNoWildcard = filePath;
            while (directoryWithNoWildcard.FullPath.Contains("*"))
            {
                directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
            }

            var suffix = filePath.RelativeTo(directoryWithNoWildcard);

            List<FilePath> files = new List<FilePath>();

            GetFilesForWildcard(directoryWithNoWildcard, suffix, files);

            return files;
        }


        private static List<FilePath> GetFilesForWildcard(FilePath prefix, string suffix, List<FilePath> files)
        {
            var singleSuffix = suffix;
            if (singleSuffix.Contains('/'))
            {
                singleSuffix = singleSuffix.Substring(0, singleSuffix.IndexOf('/'));
            }

            string remainderSuffix = null;
            if (singleSuffix != suffix)
            {
                remainderSuffix = suffix.Substring(singleSuffix.Length, suffix.Length - singleSuffix.Length);

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
                    var tempFiles = FileManager.GetAllFilesInDirectory(prefix.FullPath, null, 0)
                        .Select(item => new FilePath(item))
                        .ToList();

                    files.AddRange(tempFiles);
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
                    GetFilesForWildcard(prefix.FullPath, remainderSuffix ?? "*", files);
                }
                var directories = System.IO.Directory.GetDirectories(prefix.FullPath);
                foreach(var directory in directories)
                {
                    if(remainderSuffix == null)
                    {
                        GetFilesForWildcard(directory, "*", files);
                    }
                    else
                    {
                        GetFilesForWildcard(directory, "**/" + remainderSuffix, files);
                    }
                }
            }
            else if (singleSuffix.Contains("."))
            {
                if(remainderSuffix == null)
                {
                    if(prefix.Exists())
                    {
                        var filesTemp = System.IO.Directory.GetFiles(prefix.FullPath, singleSuffix).Select(item => new FilePath(item));
                        files.AddRange(filesTemp);
                    }
                }
                else
                {
                    // do we allow this?
                }
            }
            return files;
        }



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
