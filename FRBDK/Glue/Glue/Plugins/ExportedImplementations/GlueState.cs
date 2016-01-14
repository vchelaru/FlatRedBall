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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
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

        public FindManager Find
        {
            get;
            private set;
        }

        public IElement CurrentElement
        {
            get { return EditorLogic.CurrentElement; }
            set { EditorLogic.CurrentElement = value; }

        }

        public EntitySave CurrentEntitySave
        {
            get { return EditorLogic.CurrentEntitySave; }
        }

        public ScreenSave CurrentScreenSave
        {
            get
            {
                return EditorLogic.CurrentScreenSave;
            }
            set
            {
                EditorLogic.CurrentScreenSave = value;
            }
        }

        public EventResponseSave CurrentEventResponseSave
        {
            get
            {
                return EditorLogic.CurrentEventResponseSave;
            }
            set
            {
                EditorLogic.CurrentEventResponseSave = value;
            }
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get
            {
                return EditorLogic.CurrentReferencedFile;
            }
            set
            {
                EditorLogic.CurrentReferencedFile = value;
            }
        }

        public TreeNode CurrentTreeNode
        {
            get { return EditorLogic.CurrentTreeNode; }
            set { EditorLogic.CurrentTreeNode = value; }
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get
            {

                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

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
        }

        public StateSave CurrentStateSave
        {
            get
            {
                return EditorLogic.CurrentStateSave;
            }
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get
            {
                return EditorLogic.CurrentStateSaveCategory;
            }
        }

        public CustomVariable CurrentCustomVariable
        {
            get
            {
                return EditorLogic.CurrentCustomVariable;
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
                return ProjectManager.ContentDirectory;
            }
        }

        public string CurrentGlueProjectFileName { get { return ProjectManager.ProjectBase.FullFileName; } }

        public string CurrentGlueProjectDirectory
        {
            get
            {
                return FlatRedBall.IO.FileManager.GetDirectory(CurrentGlueProjectFileName);
            }
        }

        public ProjectBase CurrentMainProject { get { return ProjectManager.ProjectBase; } }

        public ProjectBase CurrentMainContentProject { get { return ProjectManager.ContentProject; } }

        public string ProjectNamespace
        {
            get
            {
                return ProjectManager.ProjectNamespace;
            }

        }

        #endregion

        public GlueState()
        {
            Find = new FindManager();
            Clipboard = new States.Clipboard();
        }

        public IElement GetElement(string name)
        {
            return ObjectFinder.Self.GetIElement(name);
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

        
    }
}
