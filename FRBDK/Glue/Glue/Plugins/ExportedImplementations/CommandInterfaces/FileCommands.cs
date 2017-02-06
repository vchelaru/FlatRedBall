using EditorObjects.Parsing;
using FlatRedBall.Glue.Managers;
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
        GlueProjectSave GlueProject
        {
            get
            {
                return GlueState.Self.CurrentGlueProject;
            }
        }

        public IEnumerable<string> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            var absolute = GlueCommands.Self.GetAbsoluteFileName(file);

            return GetFilesReferencedBy(absolute, topLevelOrRecursive);
        }

        public IEnumerable<string> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            return FileReferenceManager.Self.GetFilesReferencedBy(absoluteName, topLevelOrRecursive);
        }

        public void ClearFileCache(string absoluteName)
        {
            FileReferenceManager.Self.ClearFileCache(absoluteName);
        }

        public IEnumerable<string> GetAllFilesNeededOnDisk()
        {
            List<string> allFiles = new List<string>();

            var allRfses = GetAllRfses();

            FillAllFilesWithFilesInList(allFiles, allRfses, TopLevelOrRecursive.Recursive, ProjectOrDisk.Disk);

            string contentProjectDirectory = ProjectManager.ContentProject.Directory.ToLowerInvariant();

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

        IEnumerable<ReferencedFileSave> GetAllRfses()
        {
            IEnumerable<ReferencedFileSave> allRfses =
                GlueProject.Entities.SelectMany(item => item.ReferencedFiles)
                .Concat(GlueProject.Screens.SelectMany(item2 => item2.ReferencedFiles))
                .Concat(GlueProject.GlobalFiles);

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

            string contentProjectDirectory = ProjectManager.ContentProject.Directory.ToLowerInvariant();

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

        private void AddFilesReferenced(string fileName, List<string> allFiles, TopLevelOrRecursive topLevelOrRecursive, ProjectOrDisk projectOrFile)
        {
            string absoluteFileName = ProjectManager.MakeAbsolute(fileName);

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

        private void FillAllFilesWithFilesInList(List<string> allFiles, IEnumerable<ReferencedFileSave> referencedFileList, 
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
            string contentFolder = ProjectManager.ContentDirectory;

            string relativeElementFolder = element.Name + "/";

            return contentFolder + relativeElementFolder;
        }

        public void IgnoreNextChangeOnFile(string absoluteFileName)
        {
            IO.FileWatchManager.IgnoreNextChangeOnFile(absoluteFileName);
        }
    }

}
