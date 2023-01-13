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
                return SelectedFileName?.File.FullPath;
            }
        }

        public Visibility RemoveReferencesVisibility
        {
            get
            {
                if(SelectedFileName == null || SelectedFileName.File == null)
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
                if (SelectedFileName != null && SelectedFileName.File != null)
                {
                    // We want to make sure only the specific file is removed from the specific project, because this list shows all files for all projects:

                    var projectToRemoveFrom = ProjectManager.GetProjectByName(SelectedFileName.ProjectName);
                    anyRemoved |= RemoveReferenceFromProject(SelectedFileName.File.FullPath, projectToRemoveFrom);

                    UnreferencedFiles.Remove(SelectedFileName);
                    NotifyPropertyChanged(nameof(TopMessage));

                    if (anyRemoved)
                    {
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                    }
                }
            }
            else
            {
                var result = MessageBox.Show($"Would like to move {SelectedFileName} to the recycle bin?", "Delete file?", MessageBoxButton.YesNo);
                
                if(result == MessageBoxResult.Yes)
                {
                    FileHelper.MoveToRecycleBin(SelectedAbsoluteFile);

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
                    anyRemoved |= RemoveReferenceFromProject(file.File.FullPath, GlueState.Self.CurrentMainProject.ContentProject);

                    foreach (var project in GlueState.Self.SyncedProjects)
                    {
                        anyRemoved |= RemoveReferenceFromProject(file.File.FullPath, project.ContentProject);
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
                        var file = unprocessedFile.File.FullPath;
                        
                        if (System.IO.File.Exists(file))
                        {
                            FileHelper.MoveToRecycleBin(file);
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

            TaskManager.Self.AddParallelTask(() =>
           {
               var contentDirectory = GlueState.Self.ContentDirectory;

               var referencedFiles = GlueCommands.Self.FileCommands.GetAllFilesNeededOnDisk()
                .Select(item=>FileManager.Standardize(item.FullPath, contentDirectory).ToLowerInvariant())
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
                       UnreferencedFiles.Add(new ProjectSpecificFile { File = processedFile });
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
            TaskManager.Self.Add(() =>
            {
                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(async: false);
                var contentDirectory = GlueState.Self.ContentDirectory;

                var projectSpecificFileList = UnreferencedFilesManager.Self.UnreferencedFiles
                    .Select(item =>
                    {
                        string processedFile = item.File.FullPath;
                        if (FileManager.IsRelativeTo(processedFile, contentDirectory))
                        {
                            processedFile = FileManager.MakeRelative(processedFile, contentDirectory);
                        }
                        return new ProjectSpecificFile { File = processedFile, ProjectName = item.ProjectName };
                    })
                    .OrderBy(item =>item.File.Standardized)
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
            if(SelectedFileName != null && !string.IsNullOrEmpty(SelectedFileName.File?.FullPath))
            {
                file = SelectedFileName.File.FullPath;
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
