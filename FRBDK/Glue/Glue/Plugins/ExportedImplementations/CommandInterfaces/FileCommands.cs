using EditorObjects.IoC;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.IO;
using HQ.Util.Unmanaged;
using Newtonsoft.Json;
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

        GlueCommands GlueCommands => GlueCommands.Self;

        GlueProjectSave GlueProject
        {
            get
            {
                return GlueState.Self.CurrentGlueProject;
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

        public IEnumerable<FilePath> GetFilePathsReferencedBy(FilePath filePath, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            return GetFilesReferencedBy(filePath.FullPath, topLevelOrRecursive);
        }

        public void ClearFileCache(FilePath absoluteName)
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
            if (GlueState.Self.CurrentMainContentProject != null)
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
            string contentFolder = GlueState.Self.ContentDirectory;

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
            return GlueState.Self.ContentDirectory + rfs.Name;
        }

        public FilePath GetJsonFilePath(GlueElement element)
        {
            var glueDirectory = GlueState.Self.CurrentGlueProjectDirectory;

            if (element is ScreenSave screenSave)
            {
                return new FilePath(glueDirectory + screenSave.Name + "." + GlueProjectSave.ScreenExtension);
            }
            else if (element is EntitySave entitySave)
            {
                return new FilePath(glueDirectory + entitySave.Name + "." + GlueProjectSave.EntityExtension);
            }
            return null;
        }
            public FilePath GetFilePath(ReferencedFileSave rfs)
        {
            return GlueState.Self.ContentDirectory + rfs.Name;
        }

        public FilePath GetCustomCodeFilePath(GlueElement glueElement)
        {
            FilePath filePath = GlueState.Self.CurrentGlueProjectDirectory +
                glueElement.Name + ".cs";

            return filePath;
        }

        public ReferencedFileSave GetReferencedFile(FilePath filePath)
        {
            return GetReferencedFiles(filePath).FirstOrDefault();
        }

        public List<ReferencedFileSave> GetReferencedFiles(FilePath filePath)
        {
            List<ReferencedFileSave> files = new List<ReferencedFileSave>();

            if (GlueProject != null)
            {
                foreach (ScreenSave screenSave in GlueProject.Screens.ToArray())
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles.ToArray())
                    {
                        if(GlueCommands.Self.GetAbsoluteFilePath(rfs) == filePath)
                        {
                            files.Add(rfs);
                        }
                    }
                }

                foreach (EntitySave entitySave in GlueProject.Entities.ToArray())
                {
                    foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles.ToArray())
                    {
                        if (GlueCommands.Self.GetAbsoluteFilePath(rfs) == filePath)
                        {
                            files.Add(rfs);
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in GlueProject.GlobalFiles.ToArray())
                {
                    if (GlueCommands.Self.GetAbsoluteFilePath(rfs) == filePath)
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

        public async Task PasteFolder(FilePath sourceFolder, FilePath destinationFolder)
        {
            if(sourceFolder.Exists() == false)
            {
                GlueCommands.Self.PrintError($"Cannot copy {sourceFolder} because it doesn't exist");
            }

            var rfsesRelativeToSource = GlueState.Self.GetAllReferencedFiles()
                .Where(item => sourceFolder.IsRootOf(GlueCommands.Self.GetAbsoluteFilePath(item)))
                .ToArray();

            FilePath destinationWithSourceAppended = destinationFolder + sourceFolder.NoPath;
            if(!destinationWithSourceAppended.FullPath.EndsWith("/") && !destinationWithSourceAppended.FullPath.EndsWith("\\"))
            {
                destinationWithSourceAppended += "/";
            }
            var currentElement = GlueState.Self.CurrentElement;

            foreach(var rfs in rfsesRelativeToSource)
            {
                var absolutePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                var relativeToSource = absolutePath.RelativeTo(sourceFolder);

                FilePath desiredDestination = destinationWithSourceAppended + relativeToSource;
                desiredDestination = desiredDestination.GetDirectoryContainingThis();

                // make sure this directory exists
                if(!desiredDestination.Exists())
                {
                    System.IO.Directory.CreateDirectory(desiredDestination.FullPath);
                }

                await GlueCommands.Self.GluxCommands.DuplicateAsync(rfs, currentElement, desiredDestination);

            }
        }


        public bool RenameReferencedFileSave(ReferencedFileSave rfs, string newName)
        {
            var oldName = rfs.Name;

            // it's a RFS so it's gotta be content
            // Note - MakeAbsolute will do its best
            // to determine if a file is content. However,
            // a rename may change the extension to something 
            // unrecognizable. In this case we still want to have 
            // it be content
            bool forceAsContent = true;
            var oldFilePath = GlueCommands.GetAbsoluteFilePath(oldName, forceAsContent);
            var newFilePath = GlueCommands.GetAbsoluteFilePath(newName, forceAsContent);

            string instanceName = FileManager.RemovePath(FileManager.RemoveExtension(newName));
            string whyIsntValid;

            var container = ObjectFinder.Self.GetElementContaining(rfs);

            var didRename = false;

            if (oldFilePath.GetDirectoryContainingThis() != newFilePath.GetDirectoryContainingThis())
            {
                MessageBox.Show("The old file was located in \n" + oldFilePath.GetDirectoryContainingThis() + "\n" +
                    "The new file is located in \n" + newFilePath.GetDirectoryContainingThis() + "\n" +
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

            if (String.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            if (AvailableAssetTypes.Self.AllAssetTypes
                .Any(ati => String.Equals(ati.Extension, extension, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Contains(extension))
            {
                return true;
            }


            if (PluginManager.CanFileReferenceContent(filePath.FullPath))
            {
                return true;
            }


            if (String.Equals(extension, "csv", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(extension, "xml", StringComparison.OrdinalIgnoreCase))
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

        public void OpenReferencedFileInDefaultProgram(ReferencedFileSave currentReferencedFileSave) {
            OpenFileInDefaultProgram(GetFileName(currentReferencedFileSave), currentReferencedFileSave.OpensWith);
        }

        public void OpenFileInDefaultProgram(string fileName, string OpensWith = null)
        {
            string textExtension = FileManager.GetExtension(fileName);
            string sourceExtension = null;

            if (GlueState.Self.CurrentReferencedFileSave != null && !string.IsNullOrEmpty(GlueState.Self.CurrentReferencedFileSave.SourceFile))
            {
                sourceExtension = FileManager.GetExtension(GlueState.Self.CurrentReferencedFileSave.SourceFile);
            }

            string effectiveExtension = sourceExtension ?? textExtension;

            string applicationSetInGlue = String.Empty;
            if(string.IsNullOrEmpty(OpensWith)|| OpensWith == "<DEFAULT>")
            {
                applicationSetInGlue = EditorData.FileAssociationSettings.GetApplicationForExtension(effectiveExtension); 
            }
            else
            {
                applicationSetInGlue = OpensWith;
            }

            if (string.IsNullOrEmpty(applicationSetInGlue) || applicationSetInGlue == "<DEFAULT>")
            {
                try
                {
                    var executable = WindowsFileAssociation.GetExecFileAssociatedToExtension(effectiveExtension);

                    if (string.IsNullOrEmpty(executable) && !WindowsFileAssociation.NativelyHandledExtensions.Contains(effectiveExtension))
                    {
                        //Attempt to get relative projects
                        var relativeExe = "";
                        if (GumFileExtensions.Contains(textExtension.ToLower()))
                            relativeExe = GlueState.Self.GlueExeDirectory + "../../../../../../Gum/Gum/bin/Debug/Data/Gum.exe";
                        if (String.Equals(textExtension, "achx", StringComparison.OrdinalIgnoreCase))
                            relativeExe = GlueState.Self.GlueExeDirectory + "../../../../AnimationEditor/PreviewProject/bin/Debug/AnimationEditor.exe";
                        if ((relativeExe != "") && (System.IO.File.Exists(relativeExe)))
                        {
                            Process.Start(new ProcessStartInfo(relativeExe, fileName));
                            return;
                        }

                        var message = $"Windows does not have an association for the extension {effectiveExtension}. You must set the " +
                            $"program to associate with this extension to open the file. Set the association now?";

                        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);
                        if (result == System.Windows.MessageBoxResult.Yes)
                        {
                            OpenProcess();
                        }
                    }
                    else
                    {
                        OpenProcess();
                    }

                    void OpenProcess()
                    {
                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = "\"" + fileName + "\"";
                        startInfo.UseShellExecute = true;

                        System.Diagnostics.Process.Start(startInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error opening " + fileName + "\nTry navigating to this file and opening it through explorer");


                }
            }
            else
            {
                bool applicationFound = true;
                try
                {
                    applicationSetInGlue = FileManager.Standardize(applicationSetInGlue);
                }
                catch
                {
                    applicationFound = false;
                }

                if (!System.IO.File.Exists(applicationSetInGlue) || applicationFound == false)
                {
                    string error = "Could not find the application\n\n" + applicationSetInGlue;

                    System.Windows.Forms.MessageBox.Show(error);
                }
                else
                {
                    //MessageBox.Show("This functionality has been removed as of March 7, 2021. If you need it, please talk to Vic on Discord.");
                    //ProcessManager.OpenProcess(applicationSetInGlue, fileName);
                    var startInfo = new ProcessStartInfo();
                    startInfo.FileName = "\"" + applicationSetInGlue + "\"";
                    // Miguel 19/01/2023
                    // Some apps have trouble with paths using "/" as folder separator so we turn them to "\" so they look like Windows explorer paths
                    startInfo.Arguments = ("\"" + fileName + "\"").Replace("/", @"\");
                    startInfo.UseShellExecute = true;

                    System.Diagnostics.Process.Start(startInfo);
                }
            }
        }

        private static string GetFileName(ReferencedFileSave currentReferencedFileSave)
        {
            string fileName = null;
            if (currentReferencedFileSave != null)
            {
                if (!string.IsNullOrEmpty(currentReferencedFileSave.SourceFile))
                {
                    fileName =
                        GlueCommands.Self.GetAbsoluteFileName(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.SourceFile, true);
                }
                else
                {
                    fileName = GlueCommands.Self.GetAbsoluteFileName(currentReferencedFileSave);
                }
            }

            return fileName;
        }

        public List<string> GumFileExtensions { get; } = new List<string>() { "gusx", "gucx", "gutx", "gumx" };
        public string GetGumExeFilePath() {
            return FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.GlueExeDirectory + "../../../../../../Gum/Gum/bin/Debug/Data/Gum.exe";
        }

    }

}
