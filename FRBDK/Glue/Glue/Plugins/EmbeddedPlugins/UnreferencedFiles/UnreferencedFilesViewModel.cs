using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.IO;
using System.Diagnostics;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.UnreferencedFiles
{
    public class UnreferencedFilesViewModel : ViewModel
    {
        bool mIsStillScanning = false;
        public bool IsStillScanning
        {
            get{ return mIsStillScanning;}
            set
            {
                mIsStillScanning = value;
                NotifyPropertyChanged("TopMessage");
            }
        }

        public string TopMessage
        {
            get
            {
                if(mIsStillScanning)
                {
                    return "Scanning main project for unreferenced files...";
                }
                if (ScanProject)
                {
                    if (UnreferencedFiles.Count == 0)
                    {
                        return "No unreferenced files (project is clean)";
                    }

                    else
                    {
                        return "Unreferenced files in Visual Studio project (" + UnreferencedFiles.Count + "):";
                    }
                }
                else
                {
                    if (UnreferencedFiles.Count == 0)
                    {
                        return "No unreferenced files on disk";
                    }

                    else
                    {
                        return "Unreferenced files on disk (" + UnreferencedFiles.Count + "):";
                    }
                }

            }
        }


        bool scanProject = true;
        public bool ScanProject
        {
            get
            {
                return scanProject;
            }
            set
            {
                scanProject = value;
                NotifyPropertyChanged(nameof(ScanProject));
                NotifyPropertyChanged(nameof(ScanFileSystem));

            }
        }

        public bool ScanFileSystem
        {
            get
            {
                return !scanProject;
            }
            set
            {
                scanProject = !value;
                NotifyPropertyChanged(nameof(ScanProject));
                NotifyPropertyChanged(nameof(ScanFileSystem));

            }
        }

        ProjectSpecificFile mSelectedFileName;
        public ProjectSpecificFile SelectedFileName
        {
            get { return mSelectedFileName; }
            set 
            { 
                mSelectedFileName = value;
                NotifyPropertyChanged("RemoveReferencesVisibility");
            
            }
        }

        public string SelectedAbsoluteFile
        {
            get
            {
                if(SelectedFileName != null && !string.IsNullOrEmpty(SelectedFileName.FilePath) && FileManager.IsRelative(SelectedFileName.FilePath))
                {
                    return GlueState.Self.ContentDirectory + SelectedFileName;
                }
                return SelectedFileName.FilePath;
            }
        }

        public Visibility RemoveReferencesVisibility
        {
            get
            {
                if(SelectedFileName == null || string.IsNullOrEmpty(SelectedFileName.FilePath))
                {
                    return Visibility.Hidden;

                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public ObservableCollection<ProjectSpecificFile> UnreferencedFiles
        {
            get;
            set;
        }

        public UnreferencedFilesViewModel()
        {
            UnreferencedFiles = new ObservableCollection<ProjectSpecificFile>();
        }

        internal void RemoveSelectedReference()
        {
            bool anyRemoved = false;

            if (ScanProject)
            {
                if (SelectedFileName != null && !string.IsNullOrEmpty(SelectedFileName.FilePath))
                {
                    anyRemoved |= RemoveReferenceFromProject(SelectedFileName.FilePath, GlueState.Self.CurrentMainProject.ContentProject);

                    foreach (var project in GlueState.Self.SyncedProjects)
                    {
                        anyRemoved |= RemoveReferenceFromProject(SelectedFileName.FilePath, project.ContentProject);
                    }

                    if (anyRemoved)
                    {
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                        UnreferencedFiles.Remove(SelectedFileName);
                        NotifyPropertyChanged("TopMessage");
                    }
                }
            }
            else
            {
                var result = MessageBox.Show($"Would like to move {SelectedFileName} to the recycle bin?", "Delete file?", MessageBoxButton.YesNo);
                
                if(result == MessageBoxResult.Yes)
                {
                    FileHelper.DeleteFile(SelectedAbsoluteFile);

                    UnreferencedFiles.Remove(SelectedFileName);

                    NotifyPropertyChanged("TopMessage");
                }
            }

            

        }

        private bool RemoveReferenceFromProject(string SelectedFileName, ProjectBase projectBase)
        {
            string itemToRemove = SelectedFileName;

            if (!string.IsNullOrEmpty(itemToRemove) && FileManager.IsRelative(itemToRemove))
            {
                itemToRemove = GlueState.Self.ContentDirectory + SelectedFileName;
            }

            return projectBase.RemoveItem(itemToRemove);
        }

        internal void RemoveAllReferences()
        {
            if (ScanProject)
            {
                bool anyRemoved = false;
                foreach (var file in UnreferencedFiles)
                {
                    anyRemoved |= RemoveReferenceFromProject(file.FilePath, GlueState.Self.CurrentMainProject.ContentProject);

                    foreach (var project in GlueState.Self.SyncedProjects)
                    {
                        anyRemoved |= RemoveReferenceFromProject(file.FilePath, project.ContentProject);
                    }
                }

                if (anyRemoved)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                    UnreferencedFiles.Clear();
                    NotifyPropertyChanged("TopMessage");

                }
            }
            else
            {
                var result = MessageBox.Show($"Would like to move all selected files to the recycle bin?", "Delete file?", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    var contentFolder = GlueState.Self.ContentDirectory;

                    foreach (var unprocessedFile in UnreferencedFiles)
                    {
                        var file = unprocessedFile.FilePath;
                        
                        if(FileManager.IsRelative(file))
                        {
                            file = contentFolder + file;
                        }

                        if (System.IO.File.Exists(file))
                        {
                            FileHelper.DeleteFile(file);
                        }
                    }

                    UnreferencedFiles.Clear();


                    NotifyPropertyChanged("TopMessage");

                }

            }
        }

        internal void Refresh()
        {
            UnreferencedFiles.Clear();

            if(ScanProject)
            {
                RefreshUnreferencedProjectFiles();
            }
            else
            {
                RefreshFileSystemFiles();
            }
        }

        private void RefreshFileSystemFiles()
        {
            IsStillScanning = true;

            TaskManager.Self.AddAsyncTask(() =>
           {
               var contentDirectory = GlueState.Self.ContentDirectory;

               var referencedFiles = GlueCommands.Self.FileCommands.GetAllFilesNeededOnDisk()
                .Select(item=>FileManager.Standardize(item, contentDirectory).ToLowerInvariant())
                .Distinct().ToList();

               var filesInContentFolder = GetAllFilesInContentFolder()
                    .Select(item => FileManager.Standardize(item).ToLowerInvariant())
                    .Distinct().ToList();

               var unreferenced = filesInContentFolder
                    .Except(referencedFiles)
                    .Where(item => IsIgnoredInFileRemoval(item) == false)
                    .ToList();


               TaskManager.Self.OnUiThread(() =>
               {
                   // Do a last-minute clear in case this somehow runs multiple times
                   UnreferencedFiles.Clear();

                   foreach (string file in unreferenced.OrderBy(item =>item.ToLowerInvariant()))
                   {
                       string processedFile = file;
                       if (FileManager.IsRelativeTo(file, contentDirectory))
                       {
                           processedFile = FileManager.MakeRelative(file, contentDirectory);
                       }
                       UnreferencedFiles.Add(new ProjectSpecificFile { FilePath = processedFile });
                   }
                   IsStillScanning = false;
               });

           },
            "Getting unreferenced files from file system");
        }

        private bool IsIgnoredInFileRemoval(string file)
        {
            var extension = FileManager.GetExtension(file);

            if(extension == "csproj" || extension == "contentproj")
            {
                return true;
            }

            // Only consider this for removal if it's either got an ATI or if plugins recognize it:
            var foundAti = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

            if (foundAti == null)
            {
                // If there's no ATI, then Glue doesn't know about this type
                // of content so we shouldn't try to remove it
                // At first I thought we should also consider files which can
                // reference content, but then Glue was capturing Gum animation (.ganx)
                // files which really were referenced.

                //var isContentFile = PluginManager.CanFileReferenceContent(file);

                return true;
            }

            var objFolder = (GlueState.Self.ContentDirectory + "obj/");

            if(FileManager.IsRelativeTo(file, objFolder))
            {
                return true;
            }

            return false;
        }

        private IEnumerable<string> GetAllFilesInContentFolder()
        {
            var contentDirectory = GlueState.Self.ContentDirectory;

            var toReturn = FileManager.GetAllFilesInDirectory(contentDirectory)
                .Where(item=>item.StartsWith("obj/") == false);

            return toReturn;
        }

        private void RefreshUnreferencedProjectFiles()
        {
            IsStillScanning = true;
            TaskManager.Self.AddSync(() =>
            {
                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(async: false);
                var contentDirectory = GlueState.Self.ContentDirectory;

                var projectSpecificFileList = UnreferencedFilesManager.Self.UnreferencedFiles
                    .Select(item =>
                    {
                        string processedFile = item.FilePath;
                        if (FileManager.IsRelativeTo(processedFile, contentDirectory))
                        {
                            processedFile = FileManager.MakeRelative(processedFile, contentDirectory);
                        }
                        return new ProjectSpecificFile { FilePath = processedFile, ProjectId = item.ProjectId };
                    })
                    .OrderBy(item =>item.FilePath)
                    .ToList();

                TaskManager.Self.OnUiThread(() =>
                {
                    // Do a last-minute clear in case this somehow runs multiple times
                    UnreferencedFiles.Clear();



                    foreach (var file in projectSpecificFileList)
                    {
                        UnreferencedFiles.Add(file);
                    }
                    IsStillScanning = false;

                });
            },
            "Getting unreferenced files for display");
        }

        internal void ViewInExplorer()
        {
            string file = null;
            if(SelectedFileName != null && !string.IsNullOrEmpty(SelectedFileName.FilePath))
            {
                file = SelectedFileName.FilePath;
                if(FileManager.IsRelative(file))
                {
                    file = GlueState.Self.ContentDirectory + file;
                }
            }

            if(!string.IsNullOrEmpty(file))
            {

                file = "\"" + file.Replace("/", "\\") + "\"";

                Process.Start("explorer.exe", $"/select,{file}");
            }
        }
    }


}
