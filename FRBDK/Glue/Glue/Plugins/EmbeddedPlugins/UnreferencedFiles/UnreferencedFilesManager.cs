using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using System.IO;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.AutomatedGlue;
using System.Threading;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.Managers
{
    public class UnreferencedFilesManager
    {
        #region Fields

        static List<ProjectSpecificFile> mLastAddedUnreferencedFiles = new List<ProjectSpecificFile>();
        static List<FilePath> mListBeforeAddition = new List<FilePath>();
        static List<ProjectSpecificFile> mUnreferencedFiles = new List<ProjectSpecificFile>();

        public bool mHasHadFailure = false;


        static UnreferencedFilesManager mSelf;



        #endregion

        #region Properties

        public static UnreferencedFilesManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new UnreferencedFilesManager();
                }
                return mSelf;
            }
        }

        public static List<ProjectSpecificFile> LastAddedUnreferencedFiles
        {
            get
            {
                // Makes this getter thread-safe.
                List<ProjectSpecificFile> toReturn = new List<ProjectSpecificFile>();

                // This caused dead locks and I'm not sure we need it
                //lock (mLastAddedUnreferencedFiles)
                {
                    toReturn.AddRange(mLastAddedUnreferencedFiles);
                }

                return toReturn;
            }
        }

        public List<ProjectSpecificFile> UnreferencedFiles
        {
            get
            {
                return mUnreferencedFiles;
            }
        }

        bool mIsRefreshRequested;
        public bool IsRefreshRequested
        {
            get { return mIsRefreshRequested; }
            set 
            { 
                mIsRefreshRequested = value; 
            }
        }

        #endregion

        static bool alreadyShowedMessage = false;
        public void RefreshUnreferencedFiles(bool async)
        {
            if (async)
            {
                TaskManager.Self.Add(RefreshUnreferencedFilesInternal, "Refreshing unreferenced files", TaskExecutionPreference.AddOrMoveToEnd);
            }
            else
            {
                RefreshUnreferencedFilesInternal();
            }
        }

        public void RefreshUnreferencedFilesInternal()
        {
            // This may be called sync and async at the same time
            // and this can cause weird results.  let's make sure that
            // this never happens...
            lock (mLastAddedUnreferencedFiles)
            {
                mLastAddedUnreferencedFiles.Clear();
                if (ProjectManager.ContentProject != null && !mHasHadFailure)
                {
                    // return after;

                    mListBeforeAddition.Clear();
                    mListBeforeAddition.AddRange(mUnreferencedFiles.Select(item => item.File));
                    mUnreferencedFiles.Clear();

                    string contentDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();

                    List<FilePath> referencedFiles = null;

                    try
                    {
                        referencedFiles = GlueCommands.Self.FileCommands.GetAllReferencedFilePaths()
                            .Distinct()
                            .OrderBy(item => item.FullPath)
                            .ToList();
                    }
                    catch
                    {
                        if (!alreadyShowedMessage)
                        {
                            alreadyShowedMessage = true;

                            string message = "Glue was unable to track references to some files.  This means that Glue will be unable to automatically add files " +
                                "to your project, and also will be unable to remove files if necessary.  It is recommended that you fix the file reference errors and restart.";

                            GlueGui.ShowMessageBox(message);

                            mHasHadFailure = true;
                        }
                    }


                    if (referencedFiles != null)
                    {
                        var project = ProjectManager.ProjectBase;

                        // Added this here to make it easier to debug. This isn't necessary for function, as
                        // internally it does a IsContent check
                        var contentItems = ((VisualStudioProject)project.ContentProject).EvaluatedItems
                            .Where(item => GlueCommands.Self.FileCommands.IsContent(item.UnevaluatedInclude.Replace(@"\", @"/")))
                            .ToList();

                        foreach (var evaluatedItem in contentItems)
                        {
                            AddIfUnreferenced(evaluatedItem, project, referencedFiles, mUnreferencedFiles);
                        }

                        Application.DoEvents();

                        // copy the list to make it not lock
                        List<ProjectBase> syncedProjectCopy = new List<ProjectBase>();

                        lock (ProjectManager.SyncedProjects)
                        {
                            syncedProjectCopy.AddRange(ProjectManager.SyncedProjects);
                        }

                        foreach (var syncedProject in syncedProjectCopy)
                        {
                            var cloned = ((VisualStudioProject)syncedProject.ContentProject).EvaluatedItems;
                            foreach (var evaluatedItem in cloned)
                            {
                                AddIfUnreferenced(evaluatedItem, syncedProject, referencedFiles, mUnreferencedFiles);
                                // Since we're in a lock, maybe we shouldn't
								// Allow the application to do its thing
								//Application.DoEvents();
                            }
                        }

                    }

                    mUnreferencedFiles = mUnreferencedFiles.OrderBy(item => item.File.FullPath.ToLowerInvariant()).ToList();
                }
            }
        }

        static bool GetIfIsUnreferenced(ProjectItem item, ProjectBase project, List<FilePath> referencedFiles, out string nameToInclude)
        {
            bool isUnreferenced = false;

            string itemName =  item.UnevaluatedInclude.Replace(@"\", @"/");
            nameToInclude = item.UnevaluatedInclude;

            // no extensions are unsupported.  What do we do with the content pipeline?
            if (!String.IsNullOrEmpty(FileManager.GetExtension(itemName)) && GlueCommands.Self.FileCommands.IsContent(itemName) &&
                // If the project includes a reference to something like System.XML, then that reference
                // will have an UnevaluatedInclude of "System.XML" which will be treated as an XML file.
                // Therefore, ignore items with a Reference item type:
                !String.Equals(item.ItemType, "Reference", StringComparison.OrdinalIgnoreCase))
            {
                FilePath filePath =
                    FileManager.RemoveDotDotSlash(FileManager.GetDirectory(project.ContentProject.FullFileName.FullPath) + nameToInclude);

                isUnreferenced = !referencedFiles.Contains(itemName);

            }
            return isUnreferenced;
        }

        private static void AddIfUnreferenced(ProjectItem item, ProjectBase project, List<FilePath> referencedFiles, List<ProjectSpecificFile> unreferencedFiles)
        {
            string nameToInclude;
            bool isUnreferenced = GetIfIsUnreferenced(item, project, referencedFiles, out nameToInclude);

            if (isUnreferenced)
            {
                nameToInclude = ProjectManager.ContentProject.GetAbsoluteContentFolder() + nameToInclude;
                nameToInclude = nameToInclude.Replace(@"/", @"\");

                if (!mListBeforeAddition.Contains(nameToInclude))
                {
                    var projectSpecificFile = new ProjectSpecificFile()
                    {
                        File = nameToInclude,
                        ProjectName = project.Name
                    };

                    lock (mLastAddedUnreferencedFiles)
                    {
                        mLastAddedUnreferencedFiles.Add(projectSpecificFile);
                    }
                }
                unreferencedFiles.Add(new ProjectSpecificFile()
                {
                    File = nameToInclude,
                    ProjectName = project.Name
                });
            }
        }

        internal void RefreshAndRemoveNewlyAddedUnreferenced()
        {
            RefreshUnreferencedFiles(false);

            bool shouldRefreshAgainst = false;
                // use the property to be thread-safe
                
            var lastAddedUnreferenced = UnreferencedFilesManager.LastAddedUnreferencedFiles;
            foreach (ProjectSpecificFile projectSpecificFile in lastAddedUnreferenced)
            {
                if (projectSpecificFile.File.Exists())
                {
                    DialogResult result =
                        System.Windows.Forms.MessageBox.Show(
                            "The following file is no longer referenced by the project\n\n" +
                            projectSpecificFile +
                            "\n\nRemove and delete this file?", "Remove unreferenced file?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        ProjectManager.GetProjectByName(projectSpecificFile.ProjectName).ContentProject.RemoveItem(
                            projectSpecificFile.File.FullPath);

                        FileHelper.MoveToRecycleBin(projectSpecificFile.File.FullPath);
                        shouldRefreshAgainst = true;
                    }
                }
            }

            if (shouldRefreshAgainst)
            {
                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
            }
        }


        public bool NeedsRefreshOfUnreferencedFiles { get; set; }
        public bool ProcessRefreshOfUnreferencedFiles()
        {
            if (!NeedsRefreshOfUnreferencedFiles) return false;

            RefreshUnreferencedFiles(false);

            var shouldRefreshAgainst = false;
            // makes this threadsafe
            var lastAddedUnreferencedFiles = LastAddedUnreferencedFiles;
            foreach (var projectSpecificFile in lastAddedUnreferencedFiles)
            {
                if (!projectSpecificFile.File.Exists()) continue;

                DialogResult result =
                    System.Windows.Forms.MessageBox.Show(
                        "The following file is no longer referenced by the project\n\n" +
                        projectSpecificFile +
                        "\n\nRemove and delete this file?", "Remove unreferenced file?", MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes) continue;

                ProjectManager.GetProjectByName(projectSpecificFile.ProjectName).ContentProject.RemoveItem(
                    projectSpecificFile.File.FullPath);

                FileHelper.MoveToRecycleBin(projectSpecificFile.File.FullPath);
                shouldRefreshAgainst = true;
            }

            if (shouldRefreshAgainst)
            {
                RefreshUnreferencedFiles(false);
            }

            NeedsRefreshOfUnreferencedFiles = false;
            return true;
        }

    }
    

}
