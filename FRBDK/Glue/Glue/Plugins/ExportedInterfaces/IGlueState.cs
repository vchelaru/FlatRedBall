using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using System.Linq;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces
{
    public interface IGlueState
    {
        #region Properties

        GlueElement CurrentElement
        {
            get;
        }

        ITreeNode CurrentTreeNode
        {
            get;
        }

        Managers.IFindManager Find { get; }

        EntitySave CurrentEntitySave
        {
            get;
        }
        VisualStudioProject CurrentMainProject
        {
            get;
        }
        VisualStudioProject CurrentMainContentProject { get; }

        IEnumerable<ProjectBase> SyncedProjects { get; }

        ScreenSave CurrentScreenSave
        {
            get;
        }

        NamedObjectSave CurrentNamedObjectSave
        {
            get;
        }

        EventResponseSave CurrentEventResponseSave
        {
            get;
        }

        CustomVariable CurrentCustomVariable
        {
            get;
        }

        StateSave CurrentStateSave
        {
            get;
        }

        StateSaveCategory CurrentStateSaveCategory { get; set; }

        ReferencedFileSave CurrentReferencedFileSave
        {
            get;
        }

        GlueProjectSave CurrentGlueProject
        {
            get;
        }

        string CurrentGlueProjectDirectory { get; }

        string ContentDirectory
        {
            get;
        }

        string ProjectNamespace { get; }

        string ProjectSpecificSettingsFolder { get; }

        ITreeNode DraggedTreeNode { get; set; }

        bool IsReferencingFrbSource { get; }

        #endregion

        List<ProjectBase> GetProjects();
        IEnumerable<ReferencedFileSave> GetAllReferencedFiles();
        bool IsProjectLoaded(VisualStudioProject project);
    }

    public class GlueStateSnapshot : IGlueState
    {
        public GlueElement CurrentElement
        {
            get;
            set;
        }

        public ITreeNode CurrentTreeNode
        {
            get;
            set;
        }
        public Managers.IFindManager Find { get; }

        public ITreeNode DraggedTreeNode { get; set; }
        public EntitySave CurrentEntitySave
        {
            get;
            set;
        }

        public ScreenSave CurrentScreenSave
        {
            get;
            set;
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get;
            set;
        }

        public EventResponseSave CurrentEventResponseSave
        {
            get;
            set;
        }

        public CustomVariable CurrentCustomVariable
        {
            get;
            set;
        }

        public StateSave CurrentStateSave
        {
            get;
            set;
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get;
            set;
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get;
            set;
        }

        public GlueProjectSave CurrentGlueProject
        {
            get;
            set;
        }

        public string ContentDirectory
        {
            get;
            set;
        }

        public VisualStudioProject CurrentMainProject
        {
            get;
            set;
        }

        public IEnumerable<ProjectBase> SyncedProjects { get; set; }

        public bool IsProjectLoaded(VisualStudioProject project)
        {
            return CurrentMainProject == project || SyncedProjects.Contains(project);
        }

        public VisualStudioProject CurrentMainContentProject
        {
            get;
            set;
        }

        public string ProjectNamespace
        {
            get;
            set;
        }

        public string ProjectSpecificSettingsFolder
        {
            get;
            set;
        }

        public string CurrentGlueProjectDirectory { get; set; }

        public bool IsReferencingFrbSource { get; set; }

        // STOP!  If adding more properties, here be sure to add to SetFrom too

        public void SetFrom(IGlueState glueState)
        {
            this.CurrentElement = glueState.CurrentElement;

            this.CurrentTreeNode = glueState.CurrentTreeNode;

            this.CurrentEntitySave = glueState.CurrentEntitySave;

            this.CurrentScreenSave = glueState.CurrentScreenSave;

            this.CurrentNamedObjectSave = glueState.CurrentNamedObjectSave;

            this.CurrentEventResponseSave = glueState.CurrentEventResponseSave;

            this.CurrentCustomVariable = glueState.CurrentCustomVariable;

            this.CurrentStateSave = glueState.CurrentStateSave;

            this.CurrentStateSaveCategory = glueState.CurrentStateSaveCategory;

            this.CurrentReferencedFileSave = glueState.CurrentReferencedFileSave;

            this.CurrentGlueProject = glueState.CurrentGlueProject;

            // do we need to foreach it instead?
            this.SyncedProjects = glueState.SyncedProjects;

            this.ContentDirectory = glueState.ContentDirectory;

            this.CurrentMainProject = glueState.CurrentMainProject;

            this.CurrentMainContentProject = glueState.CurrentMainContentProject;

            this.ProjectNamespace = glueState.ProjectNamespace;

            this.IsReferencingFrbSource = glueState.IsReferencingFrbSource;

            if(glueState.CurrentGlueProject != null)
            {
                this.ProjectSpecificSettingsFolder = glueState.ProjectSpecificSettingsFolder;

                this.CurrentGlueProjectDirectory = glueState.CurrentGlueProjectDirectory;
            }
        }

        public GlueElement GetElement(string name)
        {
            throw new System.NotImplementedException();
        }

        public NamedObjectSave GetNamedObjectSave(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public CustomVariable GetCustomVariable(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public List<ProjectBase> GetProjects()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            throw new System.NotImplementedException();
        }
    }
}
