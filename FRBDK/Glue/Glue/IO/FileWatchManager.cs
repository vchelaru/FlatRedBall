using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.IO;
using Glue;
using FlatRedBall.Glue.VSHelpers;
using System.Collections.ObjectModel;
using FlatRedBall.Glue.Managers;
using System.Threading;

namespace FlatRedBall.Glue.IO
{


    #region 

    enum IgnoreReason
    {
        NotIgnored,
        OutsideOfProject,
        BuiltFile,
        GeneratedCodeFile
    }

    #endregion

    static class FileWatchManager
    {
        #region Fields

        //static FileSystemWatcher mFileSystemWatcher;
        static ChangedFileGroup mChangedProjectFiles;
        //static FileSystemWatcher mExternallyBuiltFileWatcher;

        //static ChangeInformation ContainedFilesChangeInfo = new ChangeInformation();
        //static ChangeInformation BehaviorChangeInfo = new ChangeInformation();
        //static ChangeInformation ExternallyBuiltFilesChangeInfo = new ChangeInformation();
        static ChangeInformation BuiltFileChangeInfo = new ChangeInformation();

        //static List<string> mChangedFiles = new List<string>();
        //static List<string> mChangedBehaviors = new List<string>();
        //static List<string> mChangedExternallyBuiltFiles = new List<string>();

        //static DateTime mLastChangedFileAdded;
        //static DateTime mLastChangedExternallyBuiltFileAdded;



        public static bool PerformFlushing = true;

        static bool IsFlushing;
        #endregion

        #region Properties

        public static object LockObject = new object();

        #endregion

        #region Event Methods

        //static void FileChangedEventRaised(object sender, FileSystemEventArgs e)
        //{
        //            IgnoreReason ignoreReason;
        //            bool isFileIgnored = FileWatchManager.IsFileIgnored(e.FullPath, out ignoreReason);

        //            if (!isFileIgnored)
        //            {
        //                ContainedFilesChangeInfo.Add(fileName);
        //            }
        //            else if (ignoreReason == IgnoreReason.BuiltFile)
        //            {
        //                string fileName = e.FullPath;

        //                string standardizedFileName = FileManager.Standardize(fileName).ToLower();

        //                // If it's in the bin folder, we still want to notify the PluginManager in case any 
        //                // plugins are going to react to this
        //                BuiltFileChangeInfo.Add(standardizedFileName);
        //            }
        //}

        //static void ExternallyBuiltFileChangedEventRaised(object sender, FileSystemEventArgs e)
        //{
        //    lock (LockObject)
        //    {
        //        // Normally we want to ignore files that are outside of the directory tree,
        //        // but not for externally built files.
        //        //bool isFileIgnored = FileWatchManager.IsFileIgnored(e.FullPath);

        //        //if (!isFileIgnored)
        //        {
        //            string fileName = e.FullPath;

        //            ExternallyBuiltFilesChangeInfo.Add(fileName);
        //        }
        //    }
        //}

        #endregion

        #region Methods

        public static void FlushAndClearIgnores()
        {
            Flush();
            mChangedProjectFiles.ClearIgnores();
        }

        public static void Flush()
        {
            lock (FileWatchManager.LockObject)
            {
                if (!IsFlushing && PerformFlushing && !MainGlueWindow.GetIsTortoiseRunning())
                {
                    IsFlushing = true;

                    List<string> filesToFlush = new List<string>();


                    if(mChangedProjectFiles.CanFlush)
                    {
                        filesToFlush.AddRange(mChangedProjectFiles.AllFiles);
                        mChangedProjectFiles.Clear();;
                    }

                    bool anyFlushed = false;

                    foreach (string file in filesToFlush.Distinct())
                    {
                        anyFlushed = true;

                        var fileCopy = file;
                        TaskManager.Self.AddSync(() =>
                            {
                                if(ReactToChangedFile(file))
                                {
                                    UnreferencedFilesManager.Self.IsRefreshRequested = true;

                                }
                            },
                            "Reacting to changed file " + file);
                    }

                    if (anyFlushed)
                    {
                        TaskManager.Self.AddSync(() =>
                            {
                                if (UnreferencedFilesManager.Self.IsRefreshRequested)
                                {
                                    // do this first in case someone else requests a refresh while the refresh is going

                                    UnreferencedFilesManager.Self.IsRefreshRequested = false;

                                    UnreferencedFilesManager.Self.RefreshUnreferencedFiles(async: false);
                                }
                            },
                            "Refreshing unreferenced files");
                    }
                    IsFlushing = false;
                }
            }
        }

        private static bool ReactToChangedFile(string file)
        {
            bool wasAnythingChanged = false;

            IgnoreReason reason;
            bool isIgnored = IsFileIgnored(file, out reason);

            if (!isIgnored)
            {
                bool handled = UpdateReactor.UpdateFile(file);
                wasAnythingChanged |= handled;
            }
            else if (reason == IgnoreReason.BuiltFile)
            {
                Plugins.PluginManager.ReactToChangedBuiltFile(file);
                wasAnythingChanged = true;
            }


            return wasAnythingChanged;
        }

        public static void Initialize()
        {
            mChangedProjectFiles = new ChangedFileGroup();

            // Files like 
            // the .glux and
            // .csproj files are
            // saved by Glue, but
            // when they change on
            // disk Glue needs to react
            // to the change.  To react to
            // the change, Glue keeps a file
            // watch on these files.  However
            // when Glue saves these files it kicks
            // of a file change.  Therefore, any time
            // Glue changes one of these files it needs
            // to know to ignore the next file change since
            // it came from itself.  Furthermore, multiple plugins
            // and parts of Glue may kick off multiple saves.  Therefore
            // we can't just keep track of a bool on whether to ignore the
            // next change or not - instead we have to keep track of an int
            // to mark how many changes Glue should ignore.
            Dictionary<string, int> mChangesToIgnore = new Dictionary<string, int>();
            mChangedProjectFiles.SetIgnoreDictionary(mChangesToIgnore);
            mChangedProjectFiles.SortDelegate = CompareFiles;

            //mExternallyBuiltFileWatcher = new FileSystemWatcher();
            //mExternallyBuiltFileWatcher.Filter = "*.*";
            //mExternallyBuiltFileWatcher.IncludeSubdirectories = true;
            //mExternallyBuiltFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName ;
            //mExternallyBuiltFileWatcher.EnableRaisingEvents = false;
            //mExternallyBuiltFileWatcher.Changed += new FileSystemEventHandler(ExternallyBuiltFileChangedEventRaised);
        }

        public static void IgnoreNextChangeOnFile(string file)
        {
            // all changed file groups share the same instance, so we only
            // have to use one of them:
            
#if !UNIT_TESTS
            mChangedProjectFiles.IgnoreNextChangeOn(file);
            //if(FileManager.GetExtension(file) == "csproj")
            //{
            //    Plugins.PluginManager.ReceiveOutput($"Ignore {file} {mChangedProjectFiles.NumberOfTimesToIgnore(file)} times");
            //}

#endif
        }

        public static void UpdateToProjectDirectory()
        {
            if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
            {
                string directory = FileManager.GetDirectory(ProjectManager.ProjectBase.FullFileName);

                // XNA 4 has the content project level with the code project.  That means we should be watching
                // one directory above where the .csproj file is so that we're capturing all changes including ones
                // occurring in the Content project
                directory = FileManager.GetDirectory(directory);
                while(ShouldMoveUpForRoot(directory))
                {
                    directory = FileManager.GetDirectory(directory);
                }

                // Could be null if initialization failed due to XNA not being installed
                if (mChangedProjectFiles != null)
                {
                    mChangedProjectFiles.Path = directory;
                    mChangedProjectFiles.Enabled = true;
                }
            }
            else
            {
                // Could be null if initialization failed due to XNA not being installed
                if (mChangedProjectFiles != null)
                {
                    mChangedProjectFiles.Enabled = false;
                }
            }
        }

        private static bool ShouldMoveUpForRoot(string directory)
        {
            string folderName = FileManager.RemovePath(directory).Replace("/", "").Replace("\\", "");

            bool returnValue = folderName == ProjectManager.ProjectBase.Name && string.IsNullOrEmpty(Directory.GetFiles(directory).FirstOrDefault(s => FileManager.GetExtension(s) == "sln"));
            return returnValue;
        }

        //public static void SetExternallyBuiltContentDirectory(string absoluteDirectory)
        //{

        //    mExternallyBuiltFileWatcher.Path = absoluteDirectory;
        //    mExternallyBuiltFileWatcher.EnableRaisingEvents = true;
        //}


        private static bool IsFileIgnored(string fileName, out IgnoreReason reason)
        {
            bool isIgnored = false;
            reason = IgnoreReason.NotIgnored;

            fileName = FlatRedBall.IO.FileManager.Standardize(fileName, "", false);

            // This block of code checks
            // if the changed file sits outside
            // of the current project.  If the file
            // is a .csproj file then we want to still
            // process it.
            if (!FileManager.IsRelative(fileName) && FileManager.GetExtension(fileName) != "csproj")
            {
                if (!FileManager.IsRelativeTo(fileName, FileManager.RelativeDirectory))
                {
                    if (ProjectManager.ContentProject == null ||
                        !FileManager.IsRelativeTo(fileName, ProjectManager.ContentProject.Directory))
                    {
                        reason = IgnoreReason.OutsideOfProject;
                        isIgnored = true;
                    }

                }
                fileName = FileManager.MakeRelative(fileName);
            }

            if(!isIgnored && fileName.StartsWith("obj/"))
            {
                reason = IgnoreReason.BuiltFile;
                isIgnored = true;
            }

            if (!isIgnored && IsBuiltFile(fileName))
            {
                reason = IgnoreReason.BuiltFile;
                isIgnored = true;
            }

            if (!isIgnored && fileName.ToLower().EndsWith(".generated.cs"))
            {
                reason = IgnoreReason.GeneratedCodeFile;
                isIgnored = true;
            }

            return isIgnored;                
        }

        private static bool IsBuiltFile(string fileName)
        {
            if (!FileManager.IsRelative(fileName))
            {
                string projectDirectory = ProjectManager.ProjectRootDirectory;
                fileName = FileManager.MakeRelative(fileName, projectDirectory);
            }
            return fileName.StartsWith("bin/") || fileName.StartsWith("bin\\");
        }

        private static int CompareFiles(string first, string second)
        {
            int firstValue = GetFileValue(first);
            int secondValue = GetFileValue(second);
            // I think I had this backwards, if the second has a greater value, then it should be positive
//            return firstValue - secondValue;
            return secondValue - firstValue;
        }


        static int GetFileValue(string file)
        {
            // CSProj files first
            
            string extension = FileManager.GetExtension(file);
            if (extension == "csproj" ||
                extension == "vcproj")
            {
                return 3;
            }
            else if (extension == "contentproj")
            {
                return 2;
            }
            else if (extension == "glux")
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}
