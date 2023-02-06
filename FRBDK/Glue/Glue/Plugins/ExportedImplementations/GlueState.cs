using System.Collections.Generic;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.Managers;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.Errors;
using System.Linq;
using FlatRedBall.Glue.IO;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Tiled;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    #region GlueStateSnapshot

    public class GlueStateSnapshot
    {
        public ITreeNode CurrentTreeNode;
        public GlueElement CurrentElement;
        public EntitySave CurrentEntitySave;
        public ScreenSave CurrentScreenSave;
        public ReferencedFileSave CurrentReferencedFileSave;
        public NamedObjectSave CurrentNamedObjectSave;
        public StateSave CurrentStateSave;
        public StateSaveCategory CurrentStateSaveCategory;
        public CustomVariable CurrentCustomVariable;
        public EventResponseSave CurrentEventResponseSave;

    }

    #endregion

    public class GlueState : IGlueState
    {
        #region Current Selection Properties

        public ITreeNode CurrentTreeNode
        {
            get => snapshot.CurrentTreeNode;
            set
            {
                UpdateToSetTreeNode(value, recordState:true);
            }
        }

        public GlueElement CurrentElement
        {
            get => snapshot.CurrentElement;
            set
            {
                var treeNode = GlueState.Self.Find.TreeNodeByTag(value);

                CurrentTreeNode = treeNode;
            }

        }

        public EntitySave CurrentEntitySave
        {
            get => snapshot.CurrentEntitySave;
            set => CurrentElement = value; 
        }

        public ScreenSave CurrentScreenSave
        {
            get => snapshot.CurrentScreenSave;
            set
            {
                CurrentTreeNode = GlueState.Self.Find.TreeNodeByTag(value);
            }
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get => snapshot.CurrentReferencedFileSave;
            set
            {
                CurrentTreeNode = GlueState.Self.Find.TreeNodeByTag(value);
            }
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get => snapshot.CurrentNamedObjectSave;
            set
            {
                if (value == null)
                {
                    CurrentTreeNode = null;
                }
                else
                {
                    CurrentTreeNode =  GlueState.Self.Find.TreeNodeByTag(value);
                }
            }
        }

        public StateSave CurrentStateSave
        {
            get => snapshot.CurrentStateSave;
            set
            {
                var treeNode = GlueState.Self.Find.TreeNodeByTag(value);
                if (treeNode != null)
                {
                    CurrentTreeNode = treeNode;
                }
            }
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get => snapshot.CurrentStateSaveCategory;
            set
            {
                var treeNode = GlueState.Self.Find.TreeNodeByTag(value);
                if(treeNode != null)
                {
                    CurrentTreeNode = treeNode;
                }
            }
        }

        public CustomVariable CurrentCustomVariable
        {
            get => snapshot.CurrentCustomVariable;

            set
            {
                CurrentTreeNode = GlueState.Self.Find.TreeNodeByTag(value);

            }

        }

        public EventResponseSave CurrentEventResponseSave
        {
            get => snapshot.CurrentEventResponseSave;
            set
            {
                CurrentTreeNode = GlueState.Self.Find.TreeNodeByTag(value);
            }
        }

        public string[] CurrentFocusedTabs
        {
            get
            {
                string GetFocusFor(TabContainerViewModel tabContainerVm)
                {
                    foreach(var tabPage in tabContainerVm.Tabs)
                    {
                        if(tabPage.IsSelected)
                        {
                            return tabPage.Title;
                        }
                    }
                    return null;
                }

                List<string> listToReturn = new List<string>();

                GlueCommands.Self.DoOnUiThread(() =>
                {
                    void AddIfNotNull(string value) 
                    {
                        if(value != null)
                        {
                            listToReturn.Add(value);
                        }
                    };

                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.TopTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.BottomTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.LeftTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.RightTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.CenterTabItems));

                });

                return listToReturn.ToArray();
            }
        }

        #endregion

        #region Project Properties

        public string ContentDirectory
        {
            get
            {
                return CurrentMainProject?.GetAbsoluteContentFolder();
            }
        }

        public FilePath ContentDirectoryPath => ContentDirectory != null
            ? new FilePath(ContentDirectory) : null;

        /// <summary>
        /// Returns the current Glue code project file name
        /// </summary>
        public FilePath CurrentCodeProjectFileName { get { return ProjectManager.ProjectBase?.FullFileName; } }

        public string CurrentGlueProjectDirectory
        {
            get
            {
                return CurrentCodeProjectFileName?.GetDirectoryContainingThis().FullPath;
            }
        }

        public VisualStudioProject CurrentMainProject { get { return ProjectManager.ProjectBase; } }

        public VisualStudioProject CurrentMainContentProject { get { return ProjectManager.ContentProject; } }

        public FilePath CurrentSlnFileName
        {
            get
            {
                if(CurrentCodeProjectFileName == null)
                {
                    return null;
                }
                else
                {
                    return VSHelpers.ProjectSyncer.LocateSolution(CurrentCodeProjectFileName.FullPath);
                }
            }
        }

        public FilePath GlueExeDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
            }
        }

        public string ProjectNamespace
        {
            get
            {
                return ProjectManager.ProjectNamespace;
            }

        }

        /// <summary>
        /// The file name of the GLUX
        /// </summary>
        public FilePath GlueProjectFileName
        {
            get
            {
                if (CurrentMainProject == null)
                {
                    return null;
                }
                else
                {
                    if (CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    {
                        return CurrentMainProject.FullFileName.RemoveExtension() + ".gluj";
                    }
                    else
                    {
                        return CurrentMainProject.FullFileName.RemoveExtension() + ".glux";
                    }
                }
            }

        }

        public string ProjectSpecificSettingsFolder
        {
            get
            {
                var projectDirectory = GlueProjectFileName.GetDirectoryContainingThis();

                return projectDirectory.FullPath + "GlueSettings/";
            }
        }

        public FilePath ProjectSpecificSettingsPath => new FilePath(ProjectSpecificSettingsFolder);

        public IEnumerable<ProjectBase> SyncedProjects => ProjectManager.SyncedProjects;

        public GlueProjectSave CurrentGlueProject => ObjectFinder.Self.GlueProject; 

        public PluginSettings CurrentPluginSettings
        {
            get
            {
                return ProjectManager.PluginSettings;
            }
        }


        /// <summary>
        /// The global glue settings for the current user, not tied to a particular project.
        /// </summary>
        public GlueSettingsSave GlueSettingsSave => ProjectManager.GlueSettingsSave; 

        #endregion

        #region Sub-containers and Self

        static GlueState mSelf;
        public static GlueState Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new GlueState();
                }
                return mSelf;
            }
        }
        public IFindManager Find
        {
            get;
            set;
        }
        public States.Clipboard Clipboard
        {
            get;
            private set;
        }

        public TiledCache TiledCache { get; private set; } = new TiledCache();

        #endregion

        #region Properties

        ITreeNode draggedTreeNode;
        public ITreeNode DraggedTreeNode 
        {
            get => draggedTreeNode;
            set
            {
                if(value != draggedTreeNode)
                {
                    if(draggedTreeNode != null)
                    {
                        PluginManager.ReactToGrabbedTreeNodeChanged(draggedTreeNode, TreeNodeAction.Released);
                    }
                    draggedTreeNode = value;
                    if(draggedTreeNode == null)
                    {
                        //GlueCommands.Self.PrintOutput("Released node");
                    }
                    else
                    {
                        if (value != null)
                        {
                            PluginManager.ReactToGrabbedTreeNodeChanged(draggedTreeNode, TreeNodeAction.Grabbed);
                        }

                    }
                }
            }
        }

        GlueStateSnapshot snapshot = new GlueStateSnapshot();

        public ErrorListViewModel ErrorList { get; private set; } = new ErrorListViewModel();

        public static object ErrorListSyncLock = new object();

        public bool IsReferencingFrbSource
        {
            get
            {
                if(CurrentMainProject == null)
                {
                    return false;
                }
                else
                {
                    if(CurrentMainProject is DesktopGlProject)
                    {
                        // todo - handle different types of projects
                        string projectReferenceName;
                        if(CurrentMainProject.DotNetVersionNumber >= 6)
                        {
                            projectReferenceName = "FlatRedBallDesktopGLNet6";
                        }
                        else
                        {
                            projectReferenceName = "FlatRedBallDesktopGL";
                        }
                        return CurrentMainProject.HasProjectReference(projectReferenceName);

                    }
                    return false;
                }
            }
        }

        #endregion

        public GlueState()
        {
            // find will be assigned by plugins
            Clipboard = new States.Clipboard();

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(
                ErrorList.Errors, ErrorListSyncLock);
        }

        /// <summary>
        /// Returns all loaded IDE projects, including the main project and all synced projects.
        /// </summary>
        /// <returns></returns>
        public List<ProjectBase> GetProjects()
        {
            var list = new List<ProjectBase>();

            list.Add(ProjectManager.ProjectBase);

            list.AddRange(ProjectManager.SyncedProjects);

            return list;
        }

        public void SetCurrentTreeNode(ITreeNode treeNode, bool recordState) =>
            UpdateToSetTreeNode(treeNode, recordState);

        private void UpdateToSetTreeNode(ITreeNode value, bool recordState)
        {
            var isSame = value == snapshot?.CurrentTreeNode;

            // push before taking a snapshot, so that the "old" one is pushed
            if (!isSame && snapshot?.CurrentTreeNode != null && recordState)
            {
                TreeNodeStackManager.Self.Push(snapshot.CurrentTreeNode);
            }

            // Snapshot should come first so everyone can update to the snapshot
            GlueState.Self.TakeSnapshot(value);

            // If we don't check for isSame, then selecting the same tree node will result in double-selects in the game.
            if(!isSame)
            {
                PluginManager.ReactToItemSelect(value);
            }
        }


        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            return ObjectFinder.Self.GetAllReferencedFiles();
        }

        void TakeSnapshot(ITreeNode selectedTreeNode)
        {
            snapshot.CurrentTreeNode = selectedTreeNode;
            snapshot.CurrentElement = GetCurrentElementFromSelection();
            snapshot.CurrentEntitySave = GetCurrentEntitySaveFromSelection();
            snapshot.CurrentScreenSave = GetCurrentScreenSaveFromSelection();
            snapshot.CurrentReferencedFileSave = GetCurrentReferencedFileSaveFromSelection();
            snapshot.CurrentNamedObjectSave = GetCurrentNamedObjectSaveFromSelection();
            snapshot.CurrentStateSave = GetCurrentStateSaveFromSelection();
            snapshot.CurrentStateSaveCategory = GetCurrentStateSaveCategoryFromSelection();
            snapshot.CurrentCustomVariable = GetCurrentCustomVariableFromSelection();
            snapshot.CurrentEventResponseSave = GetCurrentEventResponseSaveFromSelection();


            GlueElement GetCurrentElementFromSelection()
            {
                return (GlueElement)GetCurrentEntitySaveFromSelection() ?? GetCurrentScreenSaveFromSelection();
            }
            EntitySave GetCurrentEntitySaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                while (treeNode != null)
                {
                    if (treeNode.Tag is EntitySave entitySave)
                    {
                        return entitySave;
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }

                return null;
            }
            ScreenSave GetCurrentScreenSaveFromSelection()
            {

                var treeNode = selectedTreeNode;

                while (treeNode != null)
                {
                    if (treeNode.Tag is ScreenSave screenSave)
                    {
                        return screenSave;
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }

                return null;
            }
            ReferencedFileSave GetCurrentReferencedFileSaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode != null && treeNode.Tag != null && treeNode.Tag is ReferencedFileSave rfs)
                {
                    return rfs;
                }
                else
                {
                    return null;
                }
            }
            NamedObjectSave GetCurrentNamedObjectSaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode == null)
                {
                    return null;
                }
                else if (treeNode.Tag != null && treeNode.Tag is NamedObjectSave nos)
                {
                    return nos;
                }
                else
                {
                    return null;
                }
            }
            StateSave GetCurrentStateSaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode != null && treeNode.IsStateNode())
                {
                    return (StateSave)treeNode.Tag;
                }

                return null;
            }
            StateSaveCategory GetCurrentStateSaveCategoryFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode != null)
                {
                    if (treeNode.IsStateCategoryNode())
                    {
                        return (StateSaveCategory)treeNode.Tag;
                    }
                    // if the current node is a state, maybe the parent is a category
                    else if (treeNode.Parent != null && treeNode.Parent.IsStateCategoryNode())
                    {
                        return (StateSaveCategory)treeNode.Parent.Tag;
                    }
                }

                return null;
            }
            CustomVariable GetCurrentCustomVariableFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode == null)
                {
                    return null;
                }
                else if (treeNode.IsCustomVariable())
                {
                    return (CustomVariable)treeNode.Tag;
                }
                else
                {
                    return null;
                }
            }
            EventResponseSave GetCurrentEventResponseSaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                if (treeNode == null)
                {
                    return null;
                }
                else if (treeNode.Tag != null && treeNode.Tag is EventResponseSave eventResponse)
                {
                    return eventResponse;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}