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
using FlatRedBall.Glue.FacadeImplementation;
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
using GluePropertyGridClasses.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace FlatRedBall.Glue
{
    #region Enums



    #endregion

    public class ProjectManager : IVsProjectState
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



        static ProjectValues mProjectValues;

        static ProjectBase mProjectBase;
        //static VisualStudioProject mContentProject;

        static List<ProjectBase> mSyncedProjects = new List<ProjectBase>();
        static ReadOnlyCollection<ProjectBase> mSyncedProjectsReadOnly;
        internal static MainGlueWindow mForm;

        static GlueProjectSave mGlueProjectSave;

        static PluginSettings mPluginSettings;

        private static string mGameClass;

        static bool mHaveNewProjectsBeenSyncedSinceSave = false;

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

        [Obsolete("use GlueState.Self.GlueProjectFileName")]
        public static string GlueProjectFileName
        {
            get
            {
#if TEST
                return FileManager.CurrentDirectory + "TestProject.glux";
#else

                if (mProjectBase == null)
                {
                    return null;
                }
                else
                {
                    return FileManager.RemoveExtension(mProjectBase.FullFileName) + ".glux";
                }
#endif

            }

        }

        public static bool WantsToClose { get; set; }



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

        public static ProjectBase ProjectBase
        {
            get { return mProjectBase; }
            set
            {
                mProjectBase = value;
            }
        }

        public static ProjectBase ContentProject
        {
            get
            {
                if (mProjectBase == null)
                {
                    return null;
                }
                else
                {
                    return mProjectBase.ContentProject;
                }
            }
        }

        string IVsProjectState.DefaultNamespace
        {
            get
            {
                return ProjectManager.ProjectNamespace;
            }
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


        public static string StartUpScreen
        {
            get { return mGlueProjectSave.StartUpScreen; }
            set
            {
                // if statement is here to prevent unnecessary saves
                if (mGlueProjectSave.StartUpScreen != value)
                {
                    mGlueProjectSave.StartUpScreen = value;
                    GluxCommands.Self.SaveGlux();
                }
                if (string.IsNullOrEmpty(mGameClass))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Could not set the startup screen because Glue could not find the Game class.");
                }
                else
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();
                }
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
                    return FileManager.GetDirectory(FileManager.GetDirectory(GlueState.Self.GlueProjectFileName));
                }
            }
        }


        public static ReadOnlyCollection<ProjectBase> SyncedProjects
        {
            get { return mSyncedProjectsReadOnly; }
        }

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
            Container.Set<IVsProjectState>(self);
        }

        public static void Initialize()
        {
            CodeProjectHelper = new Projects.CodeProjectHelper();
            mProjectValues = new ProjectValues();
            FacadeContainer.Self.ProjectValues = mProjectValues;



            VerificationId = 0;

        }

        public static ProjectBase AddSyncedProject(string fileName)
        {
            bool doesProjectAlreadyExist = false;

            ProjectBase syncedProject = null;
            string standardizedFileName = FileManager.Standardize(fileName.ToLowerInvariant());

            if (FileManager.Standardize(ProjectManager.ProjectBase.FullFileName.ToLowerInvariant(), null, false) == standardizedFileName)
            {
                doesProjectAlreadyExist = true;
            }

            if (!doesProjectAlreadyExist)
            {
                foreach (var project in mSyncedProjects)
                {
                    string existingFileName = FileManager.Standardize(project.FullFileName.ToLowerInvariant(), null, false);
                    if (existingFileName == standardizedFileName)
                    {
                        doesProjectAlreadyExist = true;
                        break;
                    }
                }
            }

            if (!doesProjectAlreadyExist)
            {
                syncedProject = ProjectCreator.CreateProject(fileName).Project;

                syncedProject.OriginalProjectBaseIfSynced = ProjectManager.ProjectBase;

                syncedProject.Load(fileName);

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

        public static string MakeAbsolute(string relativePath)
        {
            // We standardize to get rid of "../"
            return FileManager.Standardize(MakeAbsolute(relativePath, false));
        }

        /// <summary>
        /// Converts a relative path to an absolute path assuming that the relative path
        /// is relative to the base Project's directory.  This determines whether to use
        /// the base project or the content project according to the extension of the file or whether forceAsContent is true.
        /// </summary>
        /// <param name="relativePath">The path to make absolute.</param>
        /// <param name="forceAsContent">Whether to force as content - can be passed as true if the file should be treated as content despite its extension.</param>
        /// <returns>The absolute file name.</returns>
        public static string MakeAbsolute(string relativePath, bool forceAsContent)
        {
            if (FileManager.IsRelative(relativePath))
            {
                if ((forceAsContent || IsContent(relativePath)))
                {
                    return !relativePath.StartsWith(ContentDirectoryRelative)
                               ? ContentProject.MakeAbsolute(ContentDirectoryRelative + relativePath)
                               : ContentProject.MakeAbsolute(relativePath);
                }
                else
                {
                    return ProjectBase.MakeAbsolute(relativePath);
                }
            }

            return relativePath;
        }

        public static string MakeRelativeContent(string relativePath)
        {
            if (!FileManager.IsRelative(relativePath))
            {
                if (ContentProject != null)
                {
                    // Make it relative to the content project
                    return FileManager.MakeRelative(relativePath, ProjectManager.ContentDirectory);
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

        public static bool IsContent(string file)
        {
            string extension = FileManager.GetExtension(file);

            if (extension == "")
            {
                return false;
            }

            foreach(var ati in AvailableAssetTypes.Self.AllAssetTypes)
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


            if (PluginManager.CanFileReferenceContent(file))
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

        public static void RemoveCustomVariable(CustomVariable customVariable, List<string> additionalFilesToRemove)
        {
            // additionalFilesToRemove is added to keep this consistent with other remove methods

            IElement element = ObjectFinder.Self.GetElementContaining(customVariable);

            if (element == null || !element.CustomVariables.Contains(customVariable))
            {
                throw new ArgumentException();
            }
            else
            {
                element.CustomVariables.Remove(customVariable);
                element.RefreshStatesToCustomVariables();

                List<EventResponseSave> eventsReferencedByVariable = element.GetEventsOnVariable(customVariable.Name);

                foreach (EventResponseSave ers in eventsReferencedByVariable)
                {
                    element.Events.Remove(ers);
                }
            }
            UpdateCurrentTreeNodeAndCodeAndSave();

            UpdateAllDerivedElementFromBaseValues(true);

            PluginManager.ReactToVariableRemoved(customVariable);
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
                SaveProjects();
            }
        }
        
        internal static void RemoveItemFromAllProjects(string itemName, bool performSave)
        {
            mProjectBase.RemoveItem(itemName);
            if (mProjectBase.ContentProject != null)
            {
                mProjectBase.ContentProject.RemoveItem(itemName);
            }
            // We want to make this absolute so that we can pass the same arugment to the projects and each will standardize appropriately
            string absoluteName = mProjectBase.MakeAbsolute(itemName);
            foreach (ProjectBase project in SyncedProjects)
            {
                project.RemoveItem(absoluteName);
                if (project.ContentProject != null)
                {
                    project.ContentProject.RemoveItem(itemName);
                }
            }

            if (performSave)
            {
                SaveProjects();
            }
        }
        
        public static void RemoveCodeFilesForElement(List<string> filesThatCouldBeRemoved, IElement element)
        {
            string elementName = element.Name;


            GlueCommands.Self.ProjectCommands.RemoveFromProjectsTask(
                GlueState.Self.CurrentGlueProjectDirectory + elementName + ".cs");
            filesThatCouldBeRemoved.Add(elementName + ".cs");

            // gotta also remove the generated file
            GlueCommands.Self.ProjectCommands.RemoveFromProjectsTask(
                GlueState.Self.CurrentGlueProjectDirectory + elementName + ".Generated.cs");
            filesThatCouldBeRemoved.Add(elementName + ".Generated.cs");

            string eventFile = elementName + ".Event.cs";
            string absoluteEvent = MakeAbsolute(eventFile);
            GlueCommands.Self.ProjectCommands.RemoveFromProjectsTask(absoluteEvent);
            if (System.IO.File.Exists(absoluteEvent))
            {
                filesThatCouldBeRemoved.Add(eventFile);
            }

            string generatedEventFile = elementName + ".Generated.Event.cs";
            string absoluteGeneratedEventFile = MakeAbsolute(generatedEventFile);
            GlueCommands.Self.ProjectCommands.RemoveFromProjectsTask(absoluteGeneratedEventFile);
            if (System.IO.File.Exists(absoluteGeneratedEventFile))
            {
                filesThatCouldBeRemoved.Add(generatedEventFile);
            }

            string factoryName = "Factories/" + FileManager.RemovePath(elementName) + "Factory.Generated.cs";
            string absoluteFactoryNameFile = MakeAbsolute(factoryName);
            GlueCommands.Self.ProjectCommands.RemoveFromProjectsTask(absoluteFactoryNameFile);
            if (System.IO.File.Exists(absoluteFactoryNameFile))
            {
                filesThatCouldBeRemoved.Add(absoluteFactoryNameFile);
            }
        }


        public static void SaveProjects()
        {
            lock (mProjectBase)
            {
                bool shouldSync = false;
                // IsDirty means that the project has items that haven't
                // been updated to the "evaluated" list, not if it needs to
                // be saved.
                //if (mProjectBase != null && mProjectBase.IsDirty)
                if (mProjectBase != null)
                {
                    bool succeeded = true;
                    try
                    {
                        mProjectBase.Save(mProjectBase.FullFileName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("Could not save the file because the file is in use");
                        succeeded = false;
                    }

                    if (succeeded)
                    {
                        shouldSync = true;
                    }
                }
                if (ContentProject != null && ContentProject != mProjectBase)
                {
                    ContentProject.Save(ContentProject.FullFileName);
                    shouldSync = true;
                }

                //Save projects in case they are dirty
                foreach (var syncedProject in mSyncedProjects)
                {
                    try
                    {
                        syncedProject.Save(syncedProject.FullFileName);
                    }
                    catch(Exception e)
                    {
                        PluginManager.ReceiveError(e.ToString());
                        syncedProject.IsDirty = true;
                    }
                    if (syncedProject.ContentProject != syncedProject)
                    {
                        syncedProject.ContentProject.Save(syncedProject.ContentProject.FullFileName);
                    }
                }

                //Sync all synced projects
                if (shouldSync || mHaveNewProjectsBeenSyncedSinceSave)
                {
                    var syncedProjects = mSyncedProjects.ToArray();
                    foreach (var syncedProject in syncedProjects)
                    {
                        ProjectSyncer.SyncProjects(mProjectBase, syncedProject, false);
                    }
                }

                // It may be that only the synced projects have changed, so we have to save those:
                foreach (var syncedProject in mSyncedProjects)
                {
                    syncedProject.Save(syncedProject.FullFileName);
                    if(syncedProject != syncedProject.ContentProject)
                    {
                        syncedProject.ContentProject.Save(syncedProject.ContentProject.FullFileName);
                    }
                }

                mHaveNewProjectsBeenSyncedSinceSave = false;
            }
        }



        public static void SortAndUpdateUI(EntitySave entitySave)
        {
            mGlueProjectSave.Entities.SortByName();

            ElementViewWindow.UpdateNodeToListIndex(entitySave);
        }

        public static void SortAndUpdateUI(ScreenSave screenSave)
        {
            mGlueProjectSave.Screens.SortByName();

            ElementViewWindow.UpdateNodeToListIndex(screenSave);
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
            PluginManager.ReactToGluxUnload(isExiting);

            if (!isExiting)
            {
                PluginManager.ReactToGluxClose();
            }

            if(mProjectBase != null)
            {
                mProjectBase.Unload();
            }
            mProjectBase = null;

            GlueProjectSave = null;


            foreach(var syncedProject in mSyncedProjects)
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

        public static void UpdateAllDerivedElementFromBaseValues(bool regenerateCode)
        {
            if (EditorLogic.CurrentEntitySave != null)
            {
                List<EntitySave> derivedEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(EditorLogic.CurrentEntitySave.Name);

                List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(EditorLogic.CurrentEntitySave.Name);

                for (int i = 0; i < derivedEntities.Count; i++)
                {
                    EntitySave entitySave = derivedEntities[i];

                    nosList.AddRange(ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entitySave.Name));


                    entitySave.UpdateFromBaseType();
                    // Update the tree nodes
                    EntityTreeNode treeNode = GlueState.Self.Find.EntityTreeNode(entitySave);
                    treeNode.RefreshTreeNodes();

                    if (regenerateCode)
                    {
                        CodeWriter.GenerateCode(entitySave);
                    }
                }

                foreach (NamedObjectSave nos in nosList)
                {
                    nos.UpdateCustomProperties();

                    IElement element = nos.GetContainer();

                    if (element != null)
                    {
                        CodeWriter.GenerateCode(element);
                    }

                }
            }
            else if (EditorLogic.CurrentScreenSave != null)
            {
                List<ScreenSave> derivedScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(EditorLogic.CurrentScreenSave.Name);

                for (int i = 0; i < derivedScreens.Count; i++)
                {
                    ScreenSave screenSave = derivedScreens[i];
                    screenSave.UpdateFromBaseType();

                    ScreenTreeNode treeNode = GlueState.Self.Find.ScreenTreeNode(screenSave);
                    treeNode.RefreshTreeNodes();

                    if (regenerateCode)
                    {
                        CodeWriter.GenerateCode(screenSave);
                    }
                }
            }
            else
            {
                if (EditorLogic.CurrentNamedObject == null)
                {
                    // This means the value is being set on the Screen itself
                    throw new NotImplementedException();
                }
            }
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

        /// <summary>
        /// Adds the argument fileRelativeToProject to the argument project if it's not already part of the project.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="fileRelativeToProject"></param>
        /// <param name="useContentPipeline">Whether this file must be part of the content pipeline. See internal notes on this variable.</param>
        /// <param name="shouldLink"></param>
        /// <param name="parentFile"></param>
        /// <returns>Whether the file was added.</returns>
        [Obsolete("Use GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject")]
        public static bool UpdateFileMembershipInProject(ProjectBase project, string fileRelativeToProject, bool useContentPipeline, bool shouldLink, string parentFile = null)
        {
            return GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(project, fileRelativeToProject, useContentPipeline, shouldLink, parentFile);
        }
        
        #endregion

        #region Internal Methods

        internal static void FindGameClass()
        {
            mGameClass = FindGameClass(mProjectBase);

        }

        internal static string FindGameClass(ProjectBase projectBase)
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
                            !string.IsNullOrEmpty(GlueProjectSave.CustomGameClass) &&
                            CodeParser.InheritsFrom(bi.UnevaluatedInclude, GlueProjectSave.CustomGameClass))
                        {
                            return bi.UnevaluatedInclude;
                        }
                    }
                }
            }

            return null;
        }

        internal static CheckResult VerifyInheritanceGraph(INamedObjectContainer node)
        {
            if (mGlueProjectSave != null)
            {
                VerificationId++;
                string resultString = "";

                if (InheritanceVerificationHelper(ref node, ref resultString) == CheckResult.Failed)
                {
                    MessageBox.Show("This assignment has created an inheritence cycle containing the following classes:\n\n" +
                                    resultString +
                                    "\nThe assignment will be undone.");
                    node.BaseObject = null;
                    return CheckResult.Failed;
                }

            }

            return CheckResult.Passed;
        }


        internal static bool LoadOrCreateProjectSpecificSettings(string projectFolder)
        {
            // The Glue project hasn't been loaded yet so we need to manually get the folder:

            bool wasLoaded = BuildToolAssociationManager.Self.LoadOrCreateProjectSpecificBuildTools(projectFolder);

            AvailableAssetTypes.Self.ReactToProjectLoad(projectFolder);

            return wasLoaded;
        }



        internal static CheckResult VerifyReferenceGraph(IElement element)
        {

            if (mGlueProjectSave != null && element != null)
            {
                VerificationId++;
                string resultString = "";

                Stack<IElement> visitedEntities = new Stack<IElement>();


                if (ReferenceVerificationHelper(element, ref resultString, visitedEntities) == CheckResult.Failed)
                {
                    MessageBox.Show("This assignment has created a reference creation cycle of the following path:\n\n" +
                        resultString +
                        "\nThe assignment will be undone.");

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

        private static void UpdateCurrentTreeNodeAndCodeAndSave()
        {
            EditorLogic.CurrentElementTreeNode?.RefreshTreeNodes();
        }

        private static CheckResult InheritanceVerificationHelper(ref INamedObjectContainer node, ref string cycleString)
        {
            //Assign the current VerificationId to identify nodes that have been visited
            node.VerificationIndex = VerificationId;

            //Travel upward through the inheritence tree from this object, stopping when either the
            //tree stops, or you reach a node that's already been visited.
            if (!string.IsNullOrEmpty(node.BaseObject))
            {
                INamedObjectContainer baseNode = ObjectFinder.Self.GetNamedObjectContainer(node.BaseObject);

                if (baseNode == null)
                {
                    // We do nothing - the base object for this
                    // Entity doesn't exist, so we'll continue as if this thing doesn't really have
                    // a base Entity.  The user will have to address this in the Glue UI
                }
                else if (baseNode.VerificationIndex != VerificationId)
                {

                    //If baseNode verification failed, add this node's name to the list and return Failed
                    if (InheritanceVerificationHelper(ref baseNode, ref cycleString) == CheckResult.Failed)
                    {
                        cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" + cycleString;
                        return CheckResult.Failed;
                    }

                }
                else
                {
                    //If the basenode has already been visited, begin the cycleString and return Failed

                    cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" +
                                    (baseNode as FlatRedBall.Utilities.INameable).Name + "\n";

                    return CheckResult.Failed;
                }
            }


            return CheckResult.Passed;
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
