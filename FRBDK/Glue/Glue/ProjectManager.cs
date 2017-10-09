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
using NewProjectCreator.Remote;
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

        [Obsolete("use GlueState.GlueProjectFileName")]
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

                    CodeWriter.RefreshStartupScreenCode();


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
                string projectFileName = GlueProjectFileName;
                string directory = FileManager.GetDirectory(projectFileName);

                bool foundSln = false;

                while (!string.IsNullOrEmpty(directory))
                {
                    List<string> foundSlnFiles = FileManager.GetAllFilesInDirectory(directory, "sln", 0);
                    foundSln |= foundSlnFiles.Count != 0;

                    if (foundSln && FileManager.IsRelativeTo(ContentDirectory, directory))
                    {
                        return directory;
                    }


                    //We'll assume the root is in the location of the .sln
                    directory = FileManager.GetDirectory(directory);
                }

                return null;
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
                syncedProject = ProjectCreator.CreateProject(fileName);

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

        public static void RemoveNamedObject(NamedObjectSave namedObjectToRemove)
        {
            RemoveNamedObject(namedObjectToRemove, true, true, null);
        }

        public static void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSave, bool updateUi, List<string> additionalFilesToRemove)
        {
            StringBuilder removalInformation = new StringBuilder();

            // The additionalFilesToRemove is included for consistency with other methods.  It may be used later

            // There are the following things that need to happen:
            // 1.  Remove the NamedObject from the Glue project (GLUX)
            // 2.  Remove any variables that use this NamedObject as their source
            // 3.  Remove the named object from the GUI
            // 4.  Update the variables for any NamedObjects that use this element containing this NamedObject
            // 5.  Find any Elements that contain NamedObjects that are DefinedByBase - if so, see if we should remove those or make them not DefinedByBase
            // 6.  Remove any events that tunnel into this.

            IElement element = namedObjectToRemove.GetContainer();

            if (element != null)
            {

                if (!namedObjectToRemove.RemoveSelfFromNamedObjectList(element.NamedObjects))
                {
                    throw new ArgumentException();
                }

                #region Remove all CustomVariables that reference the removed NamedObject
                for (int i = element.CustomVariables.Count - 1; i > -1; i--)
                {
                    CustomVariable variable = element.CustomVariables[i];

                    if (variable.SourceObject == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed variable " + variable.ToString());

                        element.CustomVariables.RemoveAt(i);
                    }
                }
                #endregion

                // Remove any events that use this
                for (int i = element.Events.Count - 1; i > -1; i--)
                {
                    EventResponseSave ers = element.Events[i];
                    if (ers.SourceObject == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed event " + ers.ToString());
                        element.Events.RemoveAt(i);
                    }
                }

                // Remove any objects that use this as a layer
                for (int i = 0; i < element.NamedObjects.Count; i++)
                {
                    if (element.NamedObjects[i].LayerOn == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed the following object from the deleted Layer: " + element.NamedObjects[i].ToString());
                        element.NamedObjects[i].LayerOn = null;
                    }
                }




                element.RefreshStatesToCustomVariables();

                #region Ask the user what to do with all NamedObjects that are DefinedByBase

                List<IElement> derivedElements = new List<IElement>();
                if (element is EntitySave)
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllEntitiesThatInheritFrom(element as EntitySave));
                }
                else
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllScreensThatInheritFrom(element as ScreenSave));
                }

                foreach (IElement derivedElement in derivedElements)
                {
                    // At this point, namedObjectToRemove is already removed from the current Element, so this will only
                    // return NamedObjects that exist in the derived.
                    NamedObjectSave derivedNamedObject = derivedElement.GetNamedObjectRecursively(namedObjectToRemove.InstanceName);

                    if (derivedNamedObject != null && derivedNamedObject != namedObjectToRemove && derivedNamedObject.DefinedByBase)
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                        mbmb.MessageText = "What would you like to do with the object " + derivedNamedObject.ToString();
                        mbmb.AddButton("Keep it", DialogResult.OK);
                        mbmb.AddButton("Delete it", DialogResult.Cancel);

                        DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                        if (result == DialogResult.OK)
                        {
                            // Keep it
                            derivedNamedObject.DefinedByBase = false;
                            BaseElementTreeNode treeNode = GlueState.Self.Find.ElementTreeNode(derivedElement);

                            if (updateUi)
                            {
                                treeNode.UpdateReferencedTreeNodes();
                            }
                            CodeWriter.GenerateCode(derivedElement);
                        }
                        else
                        {
                            // Delete it
                            RemoveNamedObject(derivedNamedObject, performSave, updateUi, additionalFilesToRemove);
                        }


                    }

                }
                #endregion


                if (element is ScreenSave)
                {
                    ScreenTreeNode stn = GlueState.Self.Find.ScreenTreeNode(element as ScreenSave);
                    if (updateUi)
                    {
                        stn.UpdateReferencedTreeNodes();
                    }
                    CodeWriter.GenerateCode(element);
                }
                else
                {
                    EntityTreeNode etn = GlueState.Self.Find.EntityTreeNode(element as EntitySave);
                    if (updateUi)
                    {
                        etn.UpdateReferencedTreeNodes();
                    }
                    CodeWriter.GenerateCode(element);

                    List<NamedObjectSave> entityNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(element.Name);

                    foreach (NamedObjectSave nos in entityNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }




            }

            if (performSave)
            {
                GluxCommands.Self.SaveGlux();
            }
        }

        public static void RemoveCustomVariable(CustomVariable customVariable, List<string> additionalFilesToRemove)
        {
            // additionalFilesToRemove is added to keep this consistent with other remove methods

            IElement iElement = ObjectFinder.Self.GetElementContaining(customVariable);

            if (iElement == null || !iElement.CustomVariables.Contains(customVariable))
            {
                throw new ArgumentException();
            }
            else
            {
                iElement.CustomVariables.Remove(customVariable);
                iElement.RefreshStatesToCustomVariables();

                List<EventResponseSave> eventsReferencedByVariable = iElement.GetEventsOnVariable(customVariable.Name);

                foreach (EventResponseSave ers in eventsReferencedByVariable)
                {
                    iElement.Events.Remove(ers);
                }
            }
            UpdateCurrentTreeNodeAndCodeAndSave(false);

            UpdateAllDerivedElementFromBaseValues(false, true);
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
        
        internal static void RemoveItemFromAllProjects(string itemName)
        {
            RemoveItemFromAllProjects(itemName, true);
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

            RemoveItemFromAllProjects(elementName + ".cs");
            filesThatCouldBeRemoved.Add(elementName + ".cs");

            // gotta also remove the generated file
            RemoveItemFromAllProjects(elementName + ".Generated.cs");
            filesThatCouldBeRemoved.Add(elementName + ".Generated.cs");

            string eventFile = elementName + ".Event.cs";
            string absoluteEvent = MakeAbsolute(eventFile);
            RemoveItemFromAllProjects(eventFile);
            if (System.IO.File.Exists(absoluteEvent))
            {
                filesThatCouldBeRemoved.Add(eventFile);
            }

            string generatedEventFile = elementName + ".Generated.Event.cs";
            string absoluteGeneratedEventFile = MakeAbsolute(generatedEventFile);
            RemoveItemFromAllProjects(generatedEventFile);
            if (System.IO.File.Exists(absoluteGeneratedEventFile))
            {
                filesThatCouldBeRemoved.Add(generatedEventFile);
            }

            string factoryName = "Factories/" + FileManager.RemovePath(elementName) + "Factory.Generated.cs";
            string absoluteFactoryNameFile = MakeAbsolute(factoryName);
            RemoveItemFromAllProjects(factoryName);
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
                    foreach (var syncedProject in mSyncedProjects)
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

            PluginManager.Initialize(false);

            FileWatchManager.UpdateToProjectDirectory();

        }

        public static void UpdateAllDerivedElementFromBaseValues(bool performSave, bool regenerateCode)
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
                    treeNode.UpdateReferencedTreeNodes(performSave);

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
                    treeNode.UpdateReferencedTreeNodes(performSave);

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

        public static ScreenSave AddScreen(string screenName)
        {
            string fileName = @"Screens\" + screenName + ".cs";

            ScreenSave screenSave = new ScreenSave();
            screenSave.Name = FileManager.RemoveExtension(fileName);

            AddScreen(screenSave);

            return screenSave;
        }

        internal static void AddScreen(ScreenSave screenSave)
        {
            AddScreen(screenSave, false);
        }

        internal static void AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage)
        {
            string screenName = FileManager.RemovePath(screenSave.Name);

            string fileName = screenSave.Name + ".cs";

            screenSave.Tags.Add("GLUE");
            screenSave.Source = "GLUE";

            mGlueProjectSave.Screens.Add(screenSave);
            mGlueProjectSave.Screens.SortByName();

            #region Create the Screen code (not the generated version)


            var item = mProjectBase.AddCodeBuildItem(fileName);


            string projectNamespace = ProjectNamespace;

            StringBuilder stringBuilder = new StringBuilder(CodeWriter.ScreenTemplateCode);

            CodeWriter.SetClassNameAndNamespace(
                projectNamespace + ".Screens",
                screenName,
                stringBuilder);

            string modifiedTemplate = stringBuilder.ToString();

            string fullNonGeneratedFileName = FileManager.RelativeDirectory + fileName;

            if (FileManager.FileExists(fullNonGeneratedFileName))
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + fullNonGeneratedFileName + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            else
            {

                FileManager.SaveText(
                    modifiedTemplate,
                    fullNonGeneratedFileName
                    );
            }


            #endregion

            #region Create <ScreenName>.Generated.cs

            string generatedFileName = @"Screens\" + screenName + ".Generated.cs";
            CodeProjectHelper.CreateAndAddPartialCodeFile(generatedFileName, true);


            #endregion

            // We used to set the 
            // StartUpScreen whenever
            // the user made a new Screen.
            // The reason is we assumed that
            // the user wanted to work on this
            // Screen, so we set it as the startup
            // so they could run the game right away.
            // Now we only want to do it if there are no
            // other Screens.  Otherwise they can just use
            // GlueView.
            ScreenTreeNode screenTreeNode = ElementViewWindow.AddScreen(screenSave);
            if (mGlueProjectSave.Screens.Count == 1)
            {
                ElementViewWindow.StartUpScreen = screenTreeNode;
            }

            PluginManager.ReactToNewScreenCreated(screenSave);


            SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

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

        private static void UpdateCurrentTreeNodeAndCodeAndSave(bool generateAndSave)
        {
            if (EditorLogic.CurrentScreenTreeNode != null)
            {
                EditorLogic.CurrentScreenTreeNode.UpdateReferencedTreeNodes(generateAndSave);
            }
            else if (EditorLogic.CurrentEntityTreeNode != null)
            {
                EditorLogic.CurrentEntityTreeNode.UpdateReferencedTreeNodes(generateAndSave);
            }

            if (generateAndSave)
            {
                ElementViewWindow.GenerateSelectedElementCode();
                GluxCommands.Self.SaveGlux();
            }
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
