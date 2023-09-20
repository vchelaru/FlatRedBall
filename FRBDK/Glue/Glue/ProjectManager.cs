using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.IO;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.Utilities;
using FlatRedBall.Glue.Elements;
using EditorObjects.Parsing;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Plugins;
//using NewProjectCreator.Remote;
using FlatRedBall.Glue.Errors;
using System.Collections;
using FlatRedBall.Glue.CodeGeneration;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Events;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;
using System.Threading;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Collections.ObjectModel;
using EditorObjects.IoC;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using GlueFormsCore.Managers;

namespace FlatRedBall.Glue
{
    #region Enums



    #endregion

    public class ProjectManager
    {
        #region Enums

        static ProjectManager self;

        public enum CheckResult
        {
            Passed,
            Failed
        }
        #endregion

        #region Fields



        static ProjectBase mProjectBase;
        //static VisualStudioProject mContentProject;

        static List<ProjectBase> mSyncedProjects = new List<ProjectBase>();
        static ReadOnlyCollection<ProjectBase> mSyncedProjectsReadOnly;
        internal static MainGlueWindow mForm;

        static GlueProjectSave mGlueProjectSave;

        static PluginSettings mPluginSettings;

        private static string mGameClass;

        static bool mHaveNewProjectsBeenSyncedSinceSave = false;
        public static bool HaveNewProjectsBeenSyncedSinceSave
        {
            get => mHaveNewProjectsBeenSyncedSinceSave;
            set => mHaveNewProjectsBeenSyncedSinceSave = value;
        }

        static GlueSettingsSave mGlueSettingsSave = new GlueSettingsSave();

        #endregion

        #region Properties

        public static CodeProjectHelper CodeProjectHelper
        {
            get;
            private set;
        }

        [Obsolete("Use GlueState.ProjectSpecificSettingsFolder")]
        public static string ProjectSpecificSettingsFolder
        {
            get
            {
                return Container.Get<IGlueState>().ProjectSpecificSettingsFolder;
                
            }
        }

        [Obsolete("Use GlueState.ContentDirectory")]
        public static string ContentDirectory
        {
            get
            {
                return ProjectBase?.GetAbsoluteContentFolder();
            }
        }

        public static string ContentDirectoryRelative
        {
            get { return ContentProject == null ? "" : ContentProject.ContentDirectory; }
        }

        public static string CurrentLibraryVersion
        {
            get;
            private set;
        }

        public static string GameClassFileName
        {
            get { return mGameClass; }
        }

        public static bool WantsToCloseProject { get; set; }


        [Obsolete("Use GlueState.Self.GlueSettingsSave")]
        public static GlueSettingsSave GlueSettingsSave
        {
            get { return mGlueSettingsSave; }
            set { mGlueSettingsSave = value; }
        }

        public static GlueProjectSave GlueProjectSave
        {
            get { return mGlueProjectSave; }
            internal set
            {
                mGlueProjectSave = value;
                ObjectFinder.Self.GlueProject = mGlueProjectSave;
            }
        }

        public static PluginSettings PluginSettings
        {
            get { return mPluginSettings; }
            internal set
            {
                mPluginSettings = value;
            }
        }

        public static VisualStudioProject ProjectBase
        {
            get => mProjectBase as VisualStudioProject; 
            set => mProjectBase = (VisualStudioProject)value;
        }

        public static VisualStudioProject ContentProject
        {
            get => mProjectBase?.ContentProject as VisualStudioProject;
        }

        public static string ProjectNamespace
        {
            get
            {
#if TEST
                return "TestProjectNamespace";
#else
                if (mProjectBase == null)
                {
                    return null;
                }
                else
                {
                    return mProjectBase.RootNamespace;
                }
#endif
                //return FileManager.RemovePath(FileManager.RemoveExtension(mProject.FullFileName));
            }
        }

        /// <summary>
        /// Returns the folder of the .sln for the project.
        /// </summary>
        public static string ProjectRootDirectory
        {
            get
            {
                // February 9, 2012
                // This is a little unsafe
                // because there could be a
                // stray .sln file.  Not sure
                // what to do about that though.
                // We'll live with it for now.

                var foundSlnFileName = GlueState.Self.CurrentSlnFileName;

                if (foundSlnFileName != null)
                {
                    return foundSlnFileName.GetDirectoryContainingThis().FullPath;
                }
                else
                {
                    // if we got here then there is no .sln (the user may have deleted it). In that case we'll
                    // just use the parent directory of the glue project file name
                    return FileManager.GetDirectory(FileManager.GetDirectory(GlueState.Self.GlueProjectFileName.FullPath));
                }
            }
        }


        public static ReadOnlyCollection<ProjectBase> SyncedProjects => mSyncedProjectsReadOnly; 

        //Used to prevent recursive references and inheritence
        public static int VerificationId
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Public Methods

        static ProjectManager()
        {
            mSyncedProjectsReadOnly = new ReadOnlyCollection<ProjectBase>(mSyncedProjects);

            self = new ProjectManager();
        }

        public static void Initialize()
        {
            CodeProjectHelper = new Projects.CodeProjectHelper();
            VerificationId = 0;
        }

        public static ProjectBase AddSyncedProject(FilePath fileName)
        {
            bool doesProjectAlreadyExist = false;

            ProjectBase syncedProject = null;

            if (ProjectManager.ProjectBase.FullFileName == fileName)
            {
                doesProjectAlreadyExist = true;
            }

            if (!doesProjectAlreadyExist)
            {
                foreach (var project in mSyncedProjects)
                {
                    var existingFileName = project.FullFileName;
                    if (existingFileName == fileName)
                    {
                        doesProjectAlreadyExist = true;
                        break;
                    }
                }
            }

            if (!doesProjectAlreadyExist)
            {
                syncedProject = ProjectCreator.CreateProject(fileName.FullPath).Project;

                syncedProject.OriginalProjectBaseIfSynced = ProjectManager.ProjectBase;

                syncedProject.Load(fileName.FullPath);

                lock (ProjectManager.SyncedProjects)
                {
                    mSyncedProjects.Add(syncedProject);
                }

                mHaveNewProjectsBeenSyncedSinceSave = true;
            }
            return syncedProject;
        }

        public static ProjectBase GetProjectByTypeId(string projectId)
        {
            return ProjectBase.ProjectId == projectId ? ProjectBase : SyncedProjects.FirstOrDefault(syncedProject => syncedProject.ProjectId == projectId);
        }

        public static ProjectBase GetProjectByName(string name)
        {
            if (ProjectBase.Name == name)
            {
                return ProjectBase;
            }
            else
            {
                return SyncedProjects.FirstOrDefault(project => project.Name == name);
            }
        }

        public static string MakeRelativeContent(string relativePath)
        {
            if (!FileManager.IsRelative(relativePath))
            {
                if (ContentProject != null)
                {
                    // Make it relative to the content project
                    return FileManager.MakeRelative(relativePath, GlueState.Self.ContentDirectory);
                }
                else
                {
                    return FileManager.MakeRelative(relativePath);
                }
            }
            else
            {
                return FileManager.MakeRelative(relativePath);
            }

        }
        
        public static bool CollectionContains(ICollection collection, string itemToSearchFor)
        {
            foreach (object o in collection)
            {
                if (((string)o) == itemToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }

        internal static void RemoveItemFromProject(ProjectBase projectBaseToRemoveFrom, string itemName)
        {
            RemoveItemFromProject(projectBaseToRemoveFrom, itemName, true);
        }

        internal static void RemoveItemFromProject(ProjectBase projectBaseToRemoveFrom, string itemName, bool performSave)
        {
            if (projectBaseToRemoveFrom != null)
            {
                projectBaseToRemoveFrom.RemoveItem(itemName);
            }

            if (performSave)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }
        
        /// <summary>
        /// Removes the argument item from all projects (main and synced) if the item is present in the project.
        /// If so, and if performSaved is true, projects are saved after removal.
        /// </summary>
        /// <param name="itemName">The item name, either realtive to the project or absolute.</param>
        /// <param name="performSave">Whether to save after removal.</param>
        internal static void RemoveItemFromAllProjects(string itemName, bool performSave)
        {
            var wasRemoved = mProjectBase.RemoveItem(itemName);
            if (mProjectBase.ContentProject != null)
            {
                var wasRemovedContent = mProjectBase.ContentProject.RemoveItem(itemName);
                wasRemoved |= wasRemovedContent;
            }
            // We want to make this absolute so that we can pass the same arugment to the projects and each will standardize appropriately
            string absoluteName = mProjectBase.MakeAbsolute(itemName);
            foreach (ProjectBase project in SyncedProjects)
            {
                var wasRemovedSynced = project.RemoveItem(absoluteName);
                wasRemoved |= wasRemovedSynced;
                if (project.ContentProject != null)
                {
                    var wasRemovedSyncedContent = project.ContentProject.RemoveItem(itemName);
                    wasRemoved |= wasRemovedSyncedContent;
                }
            }

            if (performSave && wasRemoved)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }
        
        public static CheckResult StatusCheck()
        {
            //if (IdeManager.HasOnlyExpress)
            //{
            //    return CheckResult.Passed;
            //}


            //if (IdeManager.IsDebugging)
            //{
            //    System.Windows.Forms.MessageBox.Show("You must finish debugging your project before you can continue with this action.");

            //    return CheckResult.Failed;
            //}

            //else
            //{
                return CheckResult.Passed;
            //}
        }

        public static void UnloadProject(bool isExiting)
        {
            // Sept 30, 2021
            // Vic asks - why
            // do we raise the 
            // plugin events before
            // setting the GlueProjectSave
            // to null? Plugins will know that
            // the GlueProjectSave is being unloaded
            // based on the method being called, but any
            // internal checks on the GlueState.Self.CurrentGlueProject
            // will return non-null. Should these plugin calls be moved below
            // where the objects are nulled out? Not sure.
            // I had to work around this issue in the MainQuickActionPlugin.
            PluginManager.ReactToGluxUnload(isExiting);

            if (!isExiting)
            {
                PluginManager.ReactToGluxClose();
            }

            if(mProjectBase != null)
            {
                mProjectBase.Unload();
            }

            GlueState.Self.TiledCache.ReactToClosedProject();

            mProjectBase = null;

            GlueProjectSave = null;

            foreach (var syncedProject in mSyncedProjects)
            {
                syncedProject.Unload();
            }

            mSyncedProjects.Clear();

            if(isExiting)
            {
                // If we're exiting we don't care about crashes here...especially
                // since we may have gotten here because of a missing XNA so we can't 
                // initialize plugins anyway

                try
                {
                    PluginManager.Initialize(false);
                }
                catch
                {
                    // do nothing
                }
            }
            else
            {
                PluginManager.Initialize(false);
            }

            FileWatchManager.UpdateToProjectDirectory();

        }

        static object mUpdateExternallyBuiltFileLock = new object();
        public static bool UpdateExternallyBuiltFile(string changedFile)
        {
            bool wasAnythingBuild = false;

            lock (mUpdateExternallyBuiltFileLock)
            {
                List<ReferencedFileSave> rfsesToUpdate = ObjectFinder.Self.GetReferencedFileSavesFromSource(changedFile);

                foreach (ReferencedFileSave rfs in rfsesToUpdate)
                {
                    rfs.PerformExternalBuild(runAsync:true);
                    wasAnythingBuild = true;
                }
            }

            return wasAnythingBuild;
        }

        /// <summary>
        /// Updates the presence of the RFS in the main project.  If the RFS has project specific files, then those
        /// files are updated in the appropriate synced project.  
        /// </summary>
        /// <remarks>
        /// This method does not update synced projects if the synced projects use the same file.  The reason is because
        /// this is taken care of when the projects are saved later on.
        /// </remarks>
        /// <param name="referencedFileSave">The RFS representing the file to update membership on.</param>
        /// <returns>Whether anything was added to any projects.</returns>
        [Obsolete("Use GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject")]
        public static bool UpdateFileMembershipInProject(ReferencedFileSave referencedFileSave)
        {
            return GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSave);
        }
        
        #endregion

        #region Internal Methods

        internal static void FindGameClass()
        {
            mGameClass = FindGameClass((VisualStudioProject)mProjectBase);

        }

        internal static string FindGameClass(VisualStudioProject projectBase)
        {
            foreach (var bi in projectBase.EvaluatedItems)
            {
                if (bi.ItemType == "Compile" && bi.UnevaluatedInclude.EndsWith(".cs") && !bi.UnevaluatedInclude.EndsWith("Generated.cs") &&
                        !bi.UnevaluatedInclude.StartsWith("Entities\\") &&
                            !bi.UnevaluatedInclude.StartsWith("Screens\\")


                    )
                {
                    if (FileManager.FileExists(bi.UnevaluatedInclude))
                    {

                        if ((CodeParser.InheritsFrom(bi.UnevaluatedInclude, "Game") ||
                        CodeParser.InheritsFrom(bi.UnevaluatedInclude, "Microsoft.Xna.Framework.Game")))
                        {
                            return bi.UnevaluatedInclude;
                        }

                        if (GlueProjectSave != null &&
                            !string.IsNullOrEmpty(GlueProjectSave.CustomGameClass))
                        {
                            if ( CodeParser.InheritsFrom(bi.UnevaluatedInclude, GlueProjectSave.CustomGameClass) ||
                                CodeParser.HasClass(bi.UnevaluatedInclude, GlueProjectSave.CustomGameClass))
                            return bi.UnevaluatedInclude;
                        }
                    }
                }
            }

            return null;
        }

        internal static void LoadOrCreateProjectSpecificSettings(string projectFolder)
        {
            // The Glue project hasn't been loaded yet so we need to manually get the folder:

            AvailableAssetTypes.Self.ReactToProjectLoad(projectFolder);
        }



        internal static CheckResult CheckForCircularObjectReferences(IElement element)
        {

            if (mGlueProjectSave != null && element != null)
            {
                VerificationId++;
                string resultString = "";

                Stack<IElement> visitedEntities = new Stack<IElement>();


                if (ReferenceVerificationHelper(element, ref resultString, visitedEntities) == CheckResult.Failed)
                {
                    return CheckResult.Failed;
                }

            }

            return CheckResult.Passed;
        }

        #endregion

        #region Private Methods


        //private static string GetCurrentRemoteDllVersion(ProjectBase project)
        //{
        //    try
        //    {
        //        RemoteFileManager.Initialize();

        //        return RemoteFileManager.GetVersionString(project);
        //    }
        //    catch
        //    {
        //        // Vic says:  This probably means the user isn't connected to the Internet, so let's return 
        //        // 0.0.0.0 so that nothing ever gets updated
        //        return "0.0.0.0";
        //    }
        //}



        private static string RemoveTypeAtEndOfName(string name)
        {
            if (!name.EndsWith(")"))
            {
                throw new ArgumentException("The name " + name + " doesn't have a type");
                // FINISH THIS
            }

            int lastOpenParen = name.LastIndexOf('(');

            name = name.Substring(0, lastOpenParen - 1);

            return name;

        }

        private static CheckResult ReferenceVerificationHelper(IElement element, ref string cycleString, Stack<IElement> visitedElements)
        {
            List<string> typesReferenced = new List<string>();

            //Assign the current VerificationId to identify nodes that have been visited
            ((INamedObjectContainer)element).VerificationIndex = VerificationId;

            if (visitedElements.Contains(element))
            {
                cycleString += "The type " + element + " causes a circular reference";
                return CheckResult.Failed;
            }
            else
            {
                visitedElements.Push(element);


                foreach (NamedObjectSave namedObject in element.NamedObjects)
                {
                    if ((!namedObject.SetByContainer && !namedObject.SetByDerived) &&
                        namedObject.SourceType == SourceType.Entity)
                    {
                        EntitySave nosEntity = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassType);
                        if (nosEntity != null)
                        {
                            CheckResult returnValue = ReferenceVerificationHelper(nosEntity, ref cycleString, visitedElements);

                            if (returnValue == CheckResult.Failed)
                            {
                                return CheckResult.Failed;
                            }
                        }
                    }
                }

                visitedElements.Pop();
                return CheckResult.Passed;
            }
        }


        
        private static bool VersionIsOutdated(string projectVersion, string webVersion)
        {
            string[] projectArray = projectVersion.Split(new char[1] { '.' });
            string[] webArray = webVersion.Split(new char[1] { '.' });

            int version1;
            int version2;

            for (int i = 0; i < projectArray.Length; ++i)
            {
                version1 = Int32.Parse(projectArray[i]);
                try
                {
                    version2 = Int32.Parse(webArray[i]);
                }
                catch
                {
                    version2 = 0;
                }

                if (version1 != version2)
                {
                    if (version1 < version2)
                        return true;
                    else
                        return false;
                }
            }
            if (webArray.Length > projectArray.Length)
                return true;
            else
                return false;
        }

        #endregion

        #endregion

        internal static void RemoveSyncedProject(VSHelpers.Projects.ProjectBase project)
        {
            mSyncedProjects.Remove(project);
        }

        internal static void AddSyncedProject(VSHelpers.Projects.ProjectBase vsp)
        {
            mSyncedProjects.Add(vsp);
            PluginManager.ReactToSyncedProjectLoad(vsp);
        }
    }
}
