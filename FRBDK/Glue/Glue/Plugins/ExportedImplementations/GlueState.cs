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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    public class GlueStateSnapshot
    {
        public TreeNode CurrentTreeNode;
        public BaseElementTreeNode CurrentElementTreeNode;
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

    public class GlueState : IGlueState
    {
        #region Fields

        static GlueState mSelf;

        #endregion

        #region Current Selection Properties

        GlueStateSnapshot snapshot = new GlueStateSnapshot();

        public TreeNode CurrentTreeNode
        {
            get => snapshot.CurrentTreeNode;
            set
            {
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode = value;
            }
        }

        public BaseElementTreeNode CurrentElementTreeNode => snapshot.CurrentElementTreeNode;

        public GlueElement CurrentElement
        {
            get => snapshot.CurrentElement;
            set
            {
                var treeNode = GlueState.Self.Find.ElementTreeNode(value);

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
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode =
                    GlueState.Self.Find.ScreenTreeNode(value);
            }
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get => snapshot.CurrentReferencedFileSave;
            set
            {
                CurrentTreeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(value);
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
                    CurrentTreeNode = GlueState.Self.Find.NamedObjectTreeNode(value);
                }
            }
        }

        public StateSave CurrentStateSave => snapshot.CurrentStateSave;

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
                CurrentTreeNode = GlueState.Self.Find.CustomVariableTreeNode(value);

            }

        }

        public EventResponseSave CurrentEventResponseSave
        {
            get => snapshot.CurrentEventResponseSave;
            set
            {
                TreeNode treeNode = GlueState.Self.Find.EventResponseTreeNode(value);

                ElementViewWindow.SelectedNode = treeNode;

            }
        }


        #endregion

        #region Properties

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
            private set;
        }
        public States.Clipboard Clipboard
        {
            get;
            private set;
        }


        public string ContentDirectory
        {
            get
            {
                return CurrentMainProject?.GetAbsoluteContentFolder();
            }
        }

        /// <summary>
        /// Returns the current Glue code project file name
        /// </summary>
        public string CurrentCodeProjectFileName { get { return ProjectManager.ProjectBase?.FullFileName; } }

        public string CurrentGlueProjectDirectory
        {
            get
            {
                var currentGlueProjectFileName = CurrentCodeProjectFileName;
                if (!string.IsNullOrEmpty(currentGlueProjectFileName))
                {
                    return FlatRedBall.IO.FileManager.GetDirectory(currentGlueProjectFileName);
                }
                else
                {
                    return null;
                }
            }
        }

        public VisualStudioProject CurrentMainProject { get { return ProjectManager.ProjectBase; } }

        public VisualStudioProject CurrentMainContentProject { get { return ProjectManager.ContentProject; } }

        public FilePath CurrentSlnFileName
        {
            get
            {
                return VSHelpers.ProjectSyncer.LocateSolution(CurrentCodeProjectFileName);
            }
        }

        public FilePath GlueExeDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
        public string GlueProjectFileName
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
                        return FileManager.RemoveExtension(CurrentMainProject.FullFileName) + ".gluj";
                    }
                    else
                    {
                        return FileManager.RemoveExtension(CurrentMainProject.FullFileName) + ".glux";
                    }
                }
            }

        }

        public string ProjectSpecificSettingsFolder
        {
            get
            {
                string projectDirectory = FileManager.GetDirectory(GlueProjectFileName);

                return projectDirectory + "GlueSettings/";
            }
        }



        public ErrorListViewModel ErrorList { get; private set; } = new ErrorListViewModel();

        public static object ErrorListSyncLock = new object();
        #endregion

        public GlueState()
        {
            Find = new FindManager();
            Clipboard = new States.Clipboard();

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(
                ErrorList.Errors, ErrorListSyncLock);
        }

        public GlueElement GetElement(string name)
        {
            return ObjectFinder.Self.GetElement(name);
        }

        public NamedObjectSave GetNamedObjectSave(string containerName, string name)
        {
            var container = GetElement(containerName);

            return container == null ? null : container.GetNamedObjectRecursively(name);
        }

        public CustomVariable GetCustomVariable(string containerName, string name)
        {
            var container = GetElement(containerName);

            return container == null ? null : container.GetCustomVariableRecursively(name);
        }

        public StateSave GetState(string containerName, string name)
        {
            var container = GetElement(containerName);

            return container == null ? null : container.GetState(name);
        }

        public StateSaveCategory GetStateCategory(string containerName, string name)
        {
            var container = GetElement(containerName);

            return container == null ? null : container.GetStateCategory(name);
        }

        public IEnumerable<ProjectBase> SyncedProjects
        {
            get
            {
                return ProjectManager.SyncedProjects;
            }
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

        public PluginSettings CurrentPluginSettings
        {
            get
            {
                return ProjectManager.PluginSettings;
            }
        }

        public GlueSettingsSave GlueSettingsSave
        {
            get { return ProjectManager.GlueSettingsSave; }
        }

        public GlueProjectSave CurrentGlueProject
        {
            get { return ObjectFinder.Self.GlueProject; }
        }

        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            return ObjectFinder.Self.GetAllReferencedFiles();
        }

        public void TakeSnapshot()
        {
            var selectedTreeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

            snapshot.CurrentTreeNode = selectedTreeNode;
            snapshot.CurrentElementTreeNode = GetCurrentElementTreeNodeFromSelection();
            snapshot.CurrentElement = GetCurrentElementFromSelection();
            snapshot.CurrentEntitySave = GetCurrentEntitySaveFromSelection();
            snapshot.CurrentScreenSave = GetCurrentScreenSaveFromSelection();
            snapshot.CurrentReferencedFileSave = GetCurrentReferencedFileSaveFromSelection();
            snapshot.CurrentNamedObjectSave = GetCurrentNamedObjectSaveFromSelection();
            snapshot.CurrentStateSave = GetCurrentStateSaveFromSelection();
            snapshot.CurrentStateSaveCategory = GetCurrentStateSaveCategoryFromSelection();
            snapshot.CurrentCustomVariable = GetCurrentCustomVariableFromSelection();
            snapshot.CurrentEventResponseSave = GetCurrentEventResponseSaveFromSelection();


            BaseElementTreeNode GetCurrentElementTreeNodeFromSelection()
            {
                var treeNode = selectedTreeNode;
                while (treeNode != null)
                {
                    if (treeNode is BaseElementTreeNode)
                    {
                        return ((BaseElementTreeNode)treeNode);
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }
                return null;
            }
            GlueElement GetCurrentElementFromSelection()
            {
                return (GlueElement)GetCurrentEntitySaveFromSelection() ?? GetCurrentScreenSaveFromSelection();
            }
            EntitySave GetCurrentEntitySaveFromSelection()
            {
                var treeNode = selectedTreeNode;

                while (treeNode != null)
                {
                    if (treeNode is EntityTreeNode)
                    {
                        return ((EntityTreeNode)treeNode).EntitySave;
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
                    if (treeNode is BaseElementTreeNode && ((BaseElementTreeNode)treeNode).SaveObject is ScreenSave)
                    {
                        return ((BaseElementTreeNode)treeNode).SaveObject as ScreenSave;
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

                if (treeNode != null && treeNode.Tag != null && treeNode.Tag is ReferencedFileSave)
                {
                    return (ReferencedFileSave)treeNode.Tag;
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
                else if (treeNode.Tag != null && treeNode.Tag is NamedObjectSave)
                {
                    return (NamedObjectSave)treeNode.Tag;
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
                else if (treeNode.Tag != null && treeNode.Tag is EventResponseSave)
                {
                    return (EventResponseSave)treeNode.Tag;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}