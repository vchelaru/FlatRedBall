using EditorObjects.IoC;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    #region Enums

    enum ProjectOrDisk
    {
        Project,
        Disk
    }

    #endregion

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

        public IEnumerable<FilePath> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            var absolute = GlueCommands.GetAbsoluteFileName(file);

            return GetFilesReferencedBy(absolute, topLevelOrRecursive);
        }

        public IEnumerable<FilePath> GetFilePathsReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            var absolute = GlueCommands.GetAbsoluteFileName(file);

            return GetFilesReferencedBy(absolute, topLevelOrRecursive);
        }

        public IEnumerable<FilePath> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            return FileReferenceManager.Self.GetFilesReferencedBy(absoluteName, topLevelOrRecursive);
        }

        public IEnumerable<FilePath> GetFilePathsReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            return GetFilesReferencedBy(absoluteName, topLevelOrRecursive);
        }

        public void ClearFileCache(string absoluteName)
        {
            FileReferenceManager.Self.ClearFileCache(absoluteName);
        }

        public IEnumerable<FilePath> GetAllFilesNeededOnDisk()
        {
            var allFiles = new List<FilePath>();

            var allRfses = GetAllRfses();

            FillAllFilesWithFilesInList(allFiles, allRfses, TopLevelOrRecursive.Recursive, ProjectOrDisk.Disk);

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

        public List<FilePath> GetAllReferencedFileNames()
        {
            return GetAllReferencedFileNames(TopLevelOrRecursive.Recursive);
        }

        public List<FilePath> GetAllReferencedFilePaths()
        {
            var allFiles = new List<FilePath>();

            var allRfses = GetAllRfses();

            FillAllFilesWithFilesInList(allFiles, allRfses, TopLevelOrRecursive.Recursive, ProjectOrDisk.Project);

            return allFiles;
        }

        public List<FilePath> GetAllReferencedFileNames(TopLevelOrRecursive topLevelOrRecursive)
        {
            var allFiles = new List<FilePath>();

            var allRfses = GetAllRfses();
            
            FillAllFilesWithFilesInList(allFiles, allRfses, topLevelOrRecursive, ProjectOrDisk.Project);

            return allFiles.Distinct().ToList();
        }

        private void AddFilesReferenced(FilePath filePath, List<FilePath> allFiles, TopLevelOrRecursive topLevelOrRecursive, ProjectOrDisk projectOrFile)
        {
            // The project may have been unloaded:
            if (GlueState.CurrentMainContentProject != null)
            {

                if (File.Exists(filePath.FullPath))
                {
                    List<FilePath> referencedFiles = null;

                    if (projectOrFile == ProjectOrDisk.Project)
                    {
                        referencedFiles = FlatRedBall.Glue.Managers.FileReferenceManager.Self.GetFilesReferencedBy(filePath.FullPath, topLevelOrRecursive);
                    }
                    else
                    {
                        referencedFiles = FlatRedBall.Glue.Managers.FileReferenceManager.Self.GetFilesNeededOnDiskBy(filePath.FullPath, topLevelOrRecursive);

                    }

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

        private void FillAllFilesWithFilesInList(List<FilePath> allFiles, ReferencedFileSave[] referencedFileList, 
            TopLevelOrRecursive topLevelOrRecursive, ProjectOrDisk projectOrFile)
        {
            foreach(var rfs in referencedFileList)
            {
                var rfsFilePath = GlueCommands.GetAbsoluteFileName(rfs);
                allFiles.Add(rfsFilePath);

                AddFilesReferenced(rfsFilePath, allFiles, topLevelOrRecursive, projectOrFile);

                for (int i = 0; i < rfs.ProjectSpecificFiles.Count; i++)
                {
                    ProjectSpecificFile psf = rfs.ProjectSpecificFiles[i];


                    if(psf.File != null)
                    {
                        allFiles.Add(psf.File);
                        AddFilesReferenced(psf.File, allFiles, topLevelOrRecursive, projectOrFile);
                    }
                    else
                    {
                        // do we care?
                        int m = 3;
                    }
                }
            }
        }

        public string GetContentFolder(IElement element)
        {
            string contentFolder = GlueState.ContentDirectory;

            string relativeElementFolder = element.Name + "/";

            return contentFolder + relativeElementFolder;
        }

        public FilePath GetGlobalContentFolder() =>
            ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";


        public void IgnoreNextChangeOnFile(string absoluteFileName)
        {
            IO.FileWatchManager.IgnoreNextChangeOnFile(absoluteFileName);
        }

        public string GetFullFileName(ReferencedFileSave rfs)
        {
            return GlueState.ContentDirectory + rfs.Name;
        }

        public FilePath GetFilePath(ReferencedFileSave rfs)
        {
            return GlueState.ContentDirectory + rfs.Name;
        }

        public FilePath GetCustomCodeFilePath(GlueElement glueElement)
        {
            FilePath filePath = GlueState.CurrentGlueProjectDirectory +
                glueElement.Name + ".cs";

            return filePath;
        }

        public ReferencedFileSave GetReferencedFile(string fileName)
        {
            return GetReferencedFiles(fileName).FirstOrDefault();
        }

        public List<ReferencedFileSave> GetReferencedFiles(string fileName)
        {
            List<ReferencedFileSave> files = new List<ReferencedFileSave>();
            ////////////////Early Out//////////////////////////////////
            var invalidPathChars = Path.GetInvalidPathChars();
            if (invalidPathChars.Any(item => fileName.Contains(item)))
            {
                // This isn't a RFS, because it's got a bad path. Early out here so that FileManager.IsRelative doesn't throw an exception
                return files;
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
                foreach (ScreenSave screenSave in GlueProject.Screens.ToArray())
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles.ToArray())
                    {
                        string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                        if (absoluteRfsFile == fileName)
                        {
                            files.Add(rfs);
                        }
                    }
                }

                foreach (EntitySave entitySave in GlueProject.Entities.ToArray())
                {
                    foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles.ToArray())
                    {
                        string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                        if (absoluteRfsFile == fileName)
                        {
                            files.Add(rfs);
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles.ToArray())
                {
                    string absoluteRfsFile = FileManager.Standardize(GlueCommands.GetAbsoluteFileName(rfs)).ToLower();

                    if (absoluteRfsFile == fileName)
                    {
                        files.Add(rfs);
                    }
                }
            }

            return files;
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

        public FilePath GetGlueExecutingFolder()
        {
            FilePath filePath = System.Reflection.Assembly.GetExecutingAssembly()
                .Location;
            return filePath.GetDirectoryContainingThis();
        }

        public bool RenameReferencedFileSave(ReferencedFileSave rfs, string newName)
        {
            var oldName = rfs.Name;

            string oldDirectory = FileManager.GetDirectory(oldName);
            string newDirectory = FileManager.GetDirectory(newName);

            // it's a RFS so it's gotta be content
            // Note - MakeAbsolute will do its best
            // to determine if a file is content. However,
            // a rename may change the extension to something 
            // unrecognizable. In this case we still want to have 
            // it be content
            bool forceAsContent = true;
            var oldFilePath = new FilePath(ProjectManager.MakeAbsolute(oldName, forceAsContent));
            var newFilePath = new FilePath(ProjectManager.MakeAbsolute(newName, forceAsContent));

            string instanceName = FileManager.RemovePath(FileManager.RemoveExtension(newName));
            string whyIsntValid;

            var container = ObjectFinder.Self.GetElementContaining(rfs);

            var didRename = false;

            if (oldDirectory != newDirectory)
            {
                MessageBox.Show("The old file was located in \n" + oldDirectory + "\n" +
                    "The new file is located in \n" + newDirectory + "\n" +
                    "Currently Glue does not support changing directories.", "Warning");

                //rfs.SetNameNoCall(oldName);
            }
            else if (NameVerifier.IsReferencedFileNameValid(instanceName, rfs.GetAssetTypeInfo(), rfs, container, out whyIsntValid) == false)
            {
                MessageBox.Show(whyIsntValid);
                //rfs.SetNameNoCall(oldName);
            }
            else
            {
                rfs.Name = newName;
                ReferencedFileSaveSetPropertyManager.ReactToRenamedReferencedFile(
                    oldName, rfs.Name, rfs, container);
                didRename = true;
            }

            return didRename;
        }

        public bool IsContent(FilePath filePath)
        {
            string extension = filePath.Extension;

            if (extension == "")
            {
                return false;
            }

            foreach (var ati in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if (ati.Extension == extension)
                {
                    return true;
                }
            }

            if (AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Contains(extension))
            {
                return true;
            }


            if (PluginManager.CanFileReferenceContent(filePath.FullPath))
            {
                return true;
            }


            if (extension == "csv" ||
                extension == "xml")
            {
                return true;
            }


            return false;
        }

        public void ViewInExplorer(FilePath filePath)
        {
            var extension = filePath.Extension;
            bool isFile = !string.IsNullOrEmpty(extension);
            if (isFile)
            {
                if (!filePath.Exists())
                {
                    filePath = filePath.GetDirectoryContainingThis();
                }
            }
            else
            {
                // the location may not exis if it's something like global content, so let's try the parent
                if (!Directory.Exists(filePath.FullPath))
                {
                    filePath = filePath.FullPath;
                }
            }

            //fileToShow = @"d:/Projects";
            // The file might begin with something like c:\.  Make sure it shows "c:\" and not "c:/"
            var locationToShow = filePath.FullPath.Replace("/", "\\");

            // Make sure the quites are
            // added after everything else.
            locationToShow = "\"" + locationToShow + "\"";

            if (isFile)
            {
                Process.Start("explorer.exe", "/select," + locationToShow);
            }
            else
            {
                Process.Start("explorer.exe", "/root," + locationToShow);
            }
        }

        public void Open(FilePath filePath)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "\"" + filePath.FullPath + "\"";
            startInfo.UseShellExecute = true;

            System.Diagnostics.Process.Start(startInfo);
        }
    }

}
