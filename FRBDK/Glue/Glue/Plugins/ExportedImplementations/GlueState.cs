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
    public static class GlueStateSnapshot
    {
        public static BaseElementTreeNode CurrentElementTreeNode;
        public static IElement CurrentElement;
        public static TreeNode CurrentTreeNode;
        public static StateSave CurrentState;
        public static NamedObjectSave CurrentNamedObject;
    }

    public class GlueState : IGlueState
    {
        #region Fields

        static GlueState mSelf;

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

        public GlueElement CurrentElement
        {
            get 
            {
                if (CurrentEntitySave != null)
                {
                    return CurrentEntitySave;
                }
                else if (CurrentScreenSave != null)
                {
                    return CurrentScreenSave;
                }
                else
                {
                    return null;
                }
            }
            set 
            {
                var treeNode = GlueState.Self.Find.ElementTreeNode(value);

                CurrentTreeNode = treeNode;
            }

        }

        public EntitySave CurrentEntitySave
        {
            get 
            {
                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

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
            set { CurrentElement = value; }
        }

        public ScreenSave CurrentScreenSave
        {
            get
            {

#if UNIT_TESTS
                return null;
#endif
                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

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
            set
            {
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode =
                    GlueState.Self.Find.ScreenTreeNode(value);
            }
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get
            {
                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

                if (treeNode != null && treeNode.Tag != null && treeNode.Tag is ReferencedFileSave)
                {
                    return (ReferencedFileSave)treeNode.Tag;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                CurrentTreeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(value);
            }
        }

        public TreeNode CurrentTreeNode
        {
            get => GetCurrentTreeNodeFromSelection();
            set
            {
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode = value;
            }
        }
        private static TreeNode GetCurrentTreeNodeFromSelection()
        {
            return MainExplorerPlugin.Self.ElementTreeView.SelectedNode;
        }

        public BaseElementTreeNode CurrentElementTreeNode => GetCurrentElementTreeNodeFromSelection();
        private BaseElementTreeNode GetCurrentElementTreeNodeFromSelection()
        {
            TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

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

        public NamedObjectSave CurrentNamedObjectSave
        {
            get
            {

                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

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
            set
            {
                if(value == null)
                {
                    CurrentTreeNode = null;
                }
                else
                {
                    CurrentTreeNode = GlueState.Self.Find.NamedObjectTreeNode(value);
                }
            }
        }

        public StateSave CurrentStateSave => GetCurrentStateSaveFromSelection();
        StateSave GetCurrentStateSaveFromSelection()
        {
            var treeNode = CurrentTreeNode;

            if (treeNode != null && treeNode.IsStateNode())
            {
                return (StateSave)treeNode.Tag;
            }

            return null;
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

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
        }

        public CustomVariable CurrentCustomVariable
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

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

            set
            {
                CurrentTreeNode = GlueState.Self.Find.CustomVariableTreeNode(value);

            }

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
                if(!string.IsNullOrEmpty(currentGlueProjectFileName))
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
                    if(CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
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

        public EventResponseSave CurrentEventResponseSave
        {
            get
            {
                //This is needed because of designer issues.
                if (MainGlueWindow.Self == null) return null;

                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

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
            set
            {
                TreeNode treeNode = GlueState.Self.Find.EventResponseTreeNode(value);

                ElementViewWindow.SelectedNode = treeNode;

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
            MainGlueWindow.Self.Invoke((MethodInvoker)delegate
            {
                GlueStateSnapshot.CurrentElementTreeNode = CurrentElementTreeNode;
                GlueStateSnapshot.CurrentState = CurrentStateSave;
                GlueStateSnapshot.CurrentTreeNode = CurrentTreeNode;
                GlueStateSnapshot.CurrentNamedObject = CurrentNamedObjectSave;
                GlueStateSnapshot.CurrentElement = CurrentElement;
            });
        }

    }
}
