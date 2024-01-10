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
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Threading.Tasks;

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

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
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


        private static async Task<bool> ReactToChangedFile(FileChange file)
        {
            bool wasAnythingChanged = false;

            IgnoreReason reason = IgnoreReason.NotIgnored;
            bool isIgnored = false;

            isIgnored = IsFileIgnoredBasedOnFileType(file.FilePath, out reason);

            if (!isIgnored)
            {
                bool handled = await UpdateReactor.UpdateFile(file.FilePath, file.ChangeType);
                wasAnythingChanged |= handled;
            }
            else if (reason == IgnoreReason.BuiltFile)
            {
                Plugins.PluginManager.ReactToChangedBuiltFile(file.FilePath.FullPath);
                wasAnythingChanged = true;
            }


            return wasAnythingChanged;
        }


        public static void FlushAndClearIgnores()
        {
            Flush();
            mChangedProjectFiles.ClearIgnores();
        }

        /// <summary>
        /// Loops through all files that have been changed since the last flush, allowing
        /// Glue (and plugins) to react to these changed files. 
        /// </summary>
        /// <remarks>
        /// The handling of the system event is in ChangedFileGroup.
        /// </remarks>
        public static async Task Flush()
        {
            await semaphoreSlim.WaitAsync();
            if (!IsFlushing && PerformFlushing)
            {
                IsFlushing = true;

                var filesToFlush = new List<FileChange>();


                if(mChangedProjectFiles.CanFlush)
                {
                    filesToFlush.AddRange(mChangedProjectFiles.AllFiles);
                    mChangedProjectFiles.Clear();
                }

                bool shouldRefreshUnreferencedFiles = false;

                var distinctFiles =
                    filesToFlush.Distinct().ToArray();

                foreach (var file in distinctFiles)
                {

                    var fileCopy = file;

                    // The task internally will skip files if they are to be ignored, but projects can have
                    // *so many* generated files, that putting a check here on generated can eliminate hundreds
                    // of tasks from being created, improving startup performance
                    IgnoreReason reason;
                    bool isIgnored = IsFileIgnoredBasedOnFileType(fileCopy.FilePath, out reason);

                    // November 12, 2019
                    // Vic asks - why do we only ignore files that are generated here?
                    //var skip = isIgnored && reason == IgnoreReason.GeneratedCodeFile;
                    var skip = isIgnored;

                    if(!skip)
                    {
                        // If individual files changed, we will flush. But if a directory changed, we'll ignore that
                        // for refreshing unreferenced files. Unreferenced files can change if a file changes or is deleted 
                        // or added, so the specific file will appear in this list. Use that not the directory. 
                        if(!fileCopy.FilePath.IsDirectory)
                        {
                            shouldRefreshUnreferencedFiles = true;
                        }
                        TaskManager.Self.Add(async () =>
                            {
                                var didReact = await ReactToChangedFile(fileCopy);
                                if (didReact)
                                {
                                    UnreferencedFilesManager.Self.IsRefreshRequested = true;
                                }
                            },
                            "Reacting to changed file " + fileCopy.FilePath);
                    }
                }

                if (shouldRefreshUnreferencedFiles)
                {
                    UnreferencedFilesManager.Self.RefreshUnreferencedFiles(async: true);
                }
                IsFlushing = false;
            }
            semaphoreSlim.Release();
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

            // Oct 11, 2022 - why do we sort? This actually adds a bit to
            // the loading process since so many files can change on disk.
            // Was this for debugging? I can't figure out why we might want this
            // so I'm going to remove this to make Glue load faster:
            //mChangedProjectFiles.SortDelegate = CompareFiles;

            //mExternallyBuiltFileWatcher = new FileSystemWatcher();
            //mExternallyBuiltFileWatcher.Filter = "*.*";
            //mExternallyBuiltFileWatcher.IncludeSubdirectories = true;
            //mExternallyBuiltFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName ;
            //mExternallyBuiltFileWatcher.EnableRaisingEvents = false;
            //mExternallyBuiltFileWatcher.Changed += new FileSystemEventHandler(ExternallyBuiltFileChangedEventRaised);
        }


        public static void IgnoreNextChangeOnFile(FilePath filePath) => IgnoreNextChangeOnFile(filePath.FullPath);
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
            if (ProjectManager.ProjectBase?.FullFileName != null)
            {
                string directory = ProjectManager.ProjectBase.FullFileName.GetDirectoryContainingThis().FullPath;

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


        private static bool IsFileIgnoredBasedOnFileType(FilePath filePath, out IgnoreReason reason)
        {
            bool isIgnored = false;
            reason = IgnoreReason.NotIgnored;

            FilePath projectDirectory = null;
            var glueProjectDirectory = GlueState.Self.CurrentGlueProjectDirectory;
            if (glueProjectDirectory != null)
            {
                projectDirectory = new FilePath(GlueState.Self.CurrentGlueProjectDirectory);
            }

            // early out:
            if(projectDirectory == null)
            {
                return false;
            }

            var contentDirectory = new FilePath(GlueState.Self.ContentDirectory);
            var objFolder = new FilePath(projectDirectory.FullPath + "obj/");
            var binFolder = new FilePath(projectDirectory.FullPath + "bin/");
            // This block of code checks
            // if the changed file sits outside
            // of the current project.  If the file
            // is a .csproj file then we want to still
            // process it.
            if (!projectDirectory.IsRootOf(filePath) && 
                !contentDirectory.IsRootOf(filePath) && 
                filePath.Extension != "csproj" )
            {
                reason = IgnoreReason.OutsideOfProject;
                isIgnored = true;
            }

            if(!isIgnored && objFolder.IsRootOf(filePath))
            {
                reason = IgnoreReason.BuiltFile;
                isIgnored = true;
            }

            if (!isIgnored && binFolder.IsRootOf(filePath))
            {
                reason = IgnoreReason.BuiltFile;
                isIgnored = true;
            }

            if (!isIgnored && filePath.FullPath.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
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

        private static int CompareFiles(FileChange first, FileChange second)
        {
            int firstValue = GetFileValue(first.FilePath.FullPath);
            int secondValue = GetFileValue(second.FilePath.FullPath);
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
            else if (extension == "glux" || extension == "gluj")
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
