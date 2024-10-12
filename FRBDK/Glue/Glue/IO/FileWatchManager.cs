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
    #region Enums

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

        static ChangedFileGroup filesWaitingToBeFlushed;

        public static bool PerformFlushing = true;

        public static bool IsPrintingDiagnosticOutput = false;

        static bool IsFlushing;
        #endregion

        #region Properties

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        #endregion

        #region Methods

        public static void Initialize()
        {
            filesWaitingToBeFlushed = new ChangedFileGroup();
        }

        public static void IgnoreNextChangeOnFile(FilePath filePath) => IgnoreNextChangeOnFile(filePath.FullPath);

        public static void IgnoreNextChangeOnFile(string file)
        {
            // all changed file groups share the same instance, so we only
            // have to use one of them:
            
#if !UNIT_TESTS
            filesWaitingToBeFlushed.IgnoreNextChangeOn(file);
            //if(FileManager.GetExtension(file) == "csproj")
            //{
            //    Plugins.PluginManager.ReceiveOutput($"Ignore {file} {mChangedProjectFiles.NumberOfTimesToIgnore(file)} times");
            //}

#endif
        }

        public static void IgnoreChangeOnFileUntil(FilePath filePath, DateTimeOffset expiration) =>
            filesWaitingToBeFlushed.IgnoreChangeOnFileUntil(filePath, expiration);

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
                if (filesWaitingToBeFlushed != null)
                {
                    filesWaitingToBeFlushed.Path = directory;
                    filesWaitingToBeFlushed.Enabled = true;
                }
            }
            else
            {
                // Could be null if initialization failed due to XNA not being installed
                if (filesWaitingToBeFlushed != null)
                {
                    filesWaitingToBeFlushed.Enabled = false;
                }
            }
        }

        private static bool ShouldMoveUpForRoot(string directory)
        {
            string folderName = FileManager.RemovePath(directory).Replace("/", "").Replace("\\", "");

            bool returnValue = folderName == ProjectManager.ProjectBase.Name && string.IsNullOrEmpty(Directory.GetFiles(directory).FirstOrDefault(s => FileManager.GetExtension(s) == "sln"));
            return returnValue;
        }

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
            var solutionFolder = GlueState.Self.CurrentSlnFileName?.GetDirectoryContainingThis();

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

                // don't ignore if it's a built XNB, we want those copied over:
                if(filePath.Extension == "xnb" && solutionFolder != null && filePath.IsRelativeTo(solutionFolder))
                {
                    // don't ignore it!
                }
                else
                {
                    reason = IgnoreReason.OutsideOfProject;
                    isIgnored = true;
                }
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

        #endregion

        #region Flush Files

        public static void FlushAndClearIgnores()
        {
            _ = Flush();
            filesWaitingToBeFlushed.ClearIgnores();
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


                if (filesWaitingToBeFlushed.CanFlush)
                {
                    filesToFlush.AddRange(filesWaitingToBeFlushed.AllFiles);
                    if (FileWatchManager.IsPrintingDiagnosticOutput && filesToFlush.Count > 0)
                    {
                        GlueCommands.Self.PrintOutput($"Flushing {filesToFlush.Count} files");
                    }
                    filesWaitingToBeFlushed.Clear();
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

                    if (!skip)
                    {
                        // If individual files changed, we will flush. But if a directory changed, we'll ignore that
                        // for refreshing unreferenced files. Unreferenced files can change if a file changes or is deleted 
                        // or added, so the specific file will appear in this list. Use that not the directory. 
                        if (!fileCopy.FilePath.IsDirectory)
                        {
                            shouldRefreshUnreferencedFiles = true;
                        }
                        TaskManager.Self.Add(async () =>
                        {
                            if (FileWatchManager.IsPrintingDiagnosticOutput)
                            {
                                GlueCommands.Self.PrintOutput($"Flushing {file} files");
                            }

                            var didReact = await FlushChangedFile(fileCopy);
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


        private static async Task<bool> FlushChangedFile(FileChange file)
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

        #endregion
    }
}
