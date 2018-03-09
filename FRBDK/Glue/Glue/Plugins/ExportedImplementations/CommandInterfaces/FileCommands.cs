using EditorObjects.IoC;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{

    enum ProjectOrDisk
    {
        Project,
        Disk
    }


    class FileCommands : IFileCommands
    {

        IGlueState GlueState => Container.Get<IGlueState>();
        IGlueCommands GlueCommands => Container.Get<IGlueCommands>();

        GlueProjectSave GlueProject
        {
            get
            {
                return GlueState.CurrentGlueProject;
            }
        }

        public IEnumerable<string> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            var absolute = GlueCommands.GetAbsoluteFileName(file);

            return GetFilesReferencedBy(absolute, topLevelOrRecursive);
        }

        public IEnumerable<FilePath> GetFilePathsReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            var absolute = GlueCommands.GetAbsoluteFileName(file);

            return GetFilesReferencedBy(absolute, topLevelOrRecursive).Select(item => new FilePath(item));
        }

        public IEnumerable<string> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
#if GLUE
            return FileReferenceManager.Self.GetFilesReferencedBy(absoluteName, topLevelOrRecursive);
#else
            throw new NotImplementedException();
#endif
        }

        public IEnumerable<FilePath> GetFilePathsReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            return GetFilesReferencedBy(absoluteName, topLevelOrRecursive).Select(item => new FilePath(item));
        }

        public void ClearFileCache(string absoluteName)
        {
#if GLUE
            FileReferenceManager.Self.ClearFileCache(absoluteName);
#else

#endif
        }

        public IEnumerable<string> GetAllFilesNeededOnDisk()
        {
            List<string> allFiles = new List<string>();

            var allRfses = GetAllRfses();

            FillAllFilesWithFilesInList(allFiles, allRfses, TopLevelOrRecursive.Recursive, ProjectOrDisk.Disk);

            string contentProjectDirectory = GlueState.CurrentMainContentProject.ContentProject.GetAbsoluteContentFolder().ToLowerInvariant();

            for (int i = 0; i < allFiles.Count; i++)
            {
                // This fixes slashes:
                allFiles[i] = FileManager.Standardize(allFiles[i], contentProjectDirectory, makeAbsolute: false);

                // This makes the files relative to the content project:
                if (allFiles[i].ToLowerInvariant().StartsWith(contentProjectDirectory))
                {
                    allFiles[i] = allFiles[i].Substring(contentProjectDirectory.Length);
                }
            }

            return allFiles;
        }

        ReferencedFileSave[] GetAllRfses()
        {
            var allRfses =
                GlueProject.Entities.SelectMany(item => item.ReferencedFiles)
                .Concat(GlueProject.Screens.SelectMany(item2 => item2.ReferencedFiles))
                .Concat(GlueProject.GlobalFiles)
                .ToArray();

            return allRfses;
        }

        public List<string> GetAllReferencedFileNames()
        {
            return GetAllReferencedFileNames(TopLevelOrRecursive.Recursive);
        }

        public List<string> GetAllReferencedFileNames(TopLevelOrRecursive topLevelOrRecursive)
        {
            List<string> allFiles = new List<string>();

            var allRfses = GetAllRfses();
            
            FillAllFilesWithFilesInList(allFiles, allRfses, topLevelOrRecursive, ProjectOrDisk.Project);

            string contentProjectDirectory = GlueState.CurrentMainContentProject.GetAbsoluteContentFolder().ToLowerInvariant();

            for (int i = 0; i < allFiles.Count; i++)
            {
                // This fixes slashes:
                allFiles[i] = FileManager.Standardize(allFiles[i], contentProjectDirectory, makeAbsolute: false);

                // This makes the files relative to the content project:
                if (allFiles[i].ToLowerInvariant().StartsWith(contentProjectDirectory))
                {
                    allFiles[i] = allFiles[i].Substring(contentProjectDirectory.Length);
                }
            }

            return allFiles.Distinct().ToList();
        }

        private void AddFilesReferenced(string fileName, List<string> allFiles, TopLevelOrRecursive topLevelOrRecursive, ProjectOrDisk projectOrFile)
        {
            // The project may have been unloaded:
            if (GlueState.CurrentMainContentProject != null)
            {
                string absoluteFileName = GlueCommands.GetAbsoluteFileName(fileName, isContent:true);

                if (File.Exists(absoluteFileName))
                {
#if GLUE

                    List<string> referencedFiles = null;

                    if (projectOrFile == ProjectOrDisk.Project)
                    {
                        referencedFiles = FlatRedBall.Glue.Managers.FileReferenceManager.Self.GetFilesReferencedBy(absoluteFileName, topLevelOrRecursive);
                    }
                    else
                    {
                        referencedFiles = FlatRedBall.Glue.Managers.FileReferenceManager.Self.GetFilesNeededOnDiskBy(absoluteFileName, topLevelOrRecursive);

                    }
#else
                    List<string> referencedFiles = 
                            ContentParser.GetFilesReferencedByAsset(absoluteFileName, topLevelOrRecursive);

#endif
                    // 12/14/2010
                    // The referencedFiles
                    // instance may be null
                    // if the absoluteFileName
                    // references a file that doesn't
                    // exist on the file system.  This
                    // happens if someone checks in a GLUX
                    // file but forgets to check in a newly-
                    // created file.  Not deadly, so Glue shouldn't
                    // crash.  Also, Glue displays warning messages in
                    // a different part of the code, so we shouldn't pester
                    // the user here with another one.
                    if (referencedFiles != null)
                    {
                        allFiles.AddRange(referencedFiles);
                    }
                }
                else
                {
                    // Do nothing?
                }
            }
        }

        private void FillAllFilesWithFilesInList(List<string> allFiles, ReferencedFileSave[] referencedFileList, 
            TopLevelOrRecursive topLevelOrRecursive, ProjectOrDisk projectOrFile)
        {
            foreach(var rfs in referencedFileList)
            {
                allFiles.Add(rfs.Name);

                AddFilesReferenced(rfs.Name, allFiles, topLevelOrRecursive, projectOrFile);

                for (int i = 0; i < rfs.ProjectSpecificFiles.Count; i++)
                {
                    ProjectSpecificFile psf = rfs.ProjectSpecificFiles[i];

                    allFiles.Add(psf.FilePath);

                    AddFilesReferenced(psf.FilePath, allFiles, topLevelOrRecursive, projectOrFile);
                }
            }
        }

        public string GetContentFolder(IElement element)
        {
            string contentFolder = GlueState.ContentDirectory;

            string relativeElementFolder = element.Name + "/";

            return contentFolder + relativeElementFolder;
        }

        public void IgnoreNextChangeOnFile(string absoluteFileName)
        {
#if GLUE
            IO.FileWatchManager.IgnoreNextChangeOnFile(absoluteFileName);
#endif
        }

        public string GetFullFileName(ReferencedFileSave rfs)
        {
            return GlueState.ContentDirectory + rfs.Name;
        }

        // This replaces ObjectFinder.GetReferencedFileSaveFromFile - if any changes are made here, make the changes there too
        public ReferencedFileSave GetReferencedFile(string fileName)
        {
            ////////////////Early Out//////////////////////////////////
            var invalidPathChars = Path.GetInvalidPathChars();
            if (invalidPathChars.Any(item => fileName.Contains(item)))
            {
                // This isn't a RFS, because it's got a bad path. Early out here so that FileManager.IsRelative doesn't throw an exception
                return null;
            }

            //////////////End Early Out////////////////////////////////


            fileName = fileName.ToLower();

            if (FileManager.IsRelative(fileName))
            {

                fileName = GlueCommands.GetAbsoluteFileName(fileName, isContent:true);

            }

            fileName = FileManager.Standardize(fileName).ToLower();


            if (GlueProject != null)
            {
                foreach (ScreenSave screenSave in GlueProject.Screens)
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                    {
                        string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                        if (absoluteRfsFile == fileName)
                        {
                            return rfs;
                        }
                    }
                }

                lock (GlueProject.Entities)
                {
                    foreach (EntitySave entitySave in GlueProject.Entities)
                    {
                        foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                        {
                            string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                            if (absoluteRfsFile == fileName)
                            {
                                return rfs;
                            }
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles)
                {
                    string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                    if (absoluteRfsFile == fileName)
                    {
                        return rfs;
                    }
                }
            }

            return null;


        }

        public GeneralResponse GetLastParseResponse(FilePath file)
        {
            // only return failure if there is an entry in the FileReferenceManager, otherwise return success:
            if(FileReferenceManager.Self.FilesWithFailedGetReferenceCalls.ContainsKey(file))
            {
                return FileReferenceManager.Self.FilesWithFailedGetReferenceCalls[file];
            }
            else
            {
                return GeneralResponse.SuccessfulResponse;
            }
        }
    }

}
