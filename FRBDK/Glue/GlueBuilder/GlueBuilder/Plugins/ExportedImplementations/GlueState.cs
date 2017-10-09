using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueBuilder.Managers;
using FlatRedBall.IO;

namespace GlueBuilder.Plugins.ExportedImplementations
{
    public class GlueState : IGlueState
    {
        public IElement CurrentElement => throw new NotImplementedException();

        public EntitySave CurrentEntitySave => throw new NotImplementedException();

        public ProjectBase CurrentMainProject
        {
            get
            {
                return ProjectManager.Self.MainProject;
            }
        }

        public ProjectBase CurrentMainContentProject => throw new NotImplementedException();

        public IEnumerable<ProjectBase> SyncedProjects => throw new NotImplementedException();

        public ScreenSave CurrentScreenSave => throw new NotImplementedException();

        public NamedObjectSave CurrentNamedObjectSave => throw new NotImplementedException();

        public EventResponseSave CurrentEventResponseSave => throw new NotImplementedException();

        public CustomVariable CurrentCustomVariable => throw new NotImplementedException();

        public StateSave CurrentStateSave => throw new NotImplementedException();

        public StateSaveCategory CurrentStateSaveCategory => throw new NotImplementedException();

        public ReferencedFileSave CurrentReferencedFileSave => throw new NotImplementedException();

        public GlueProjectSave CurrentGlueProject => ProjectManager.Self.CurrentGlueProjectSave;

        public string CurrentGlueProjectDirectory => FlatRedBall.IO.FileManager.GetDirectory(CurrentMainProject.FullFileName);

        public string ContentDirectory => CurrentMainProject?.GetAbsoluteContentFolder();

        public string ProjectNamespace => throw new NotImplementedException();

        public string GlueProjectFileName
        {
            get
            {
#if TEST
                return FileManager.CurrentDirectory + "TestProject.glux";
#else

                if (CurrentMainProject == null)
                {
                    return null;
                }
                else
                {
                    return FileManager.RemoveExtension(CurrentMainProject.FullFileName) + ".glux";
                }
#endif

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

        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            throw new NotImplementedException();
        }

        public CustomVariable GetCustomVariable(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public IElement GetElement(string name)
        {
            throw new NotImplementedException();
        }

        public NamedObjectSave GetNamedObjectSave(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public List<ProjectBase> GetProjects()
        {
            throw new NotImplementedException();
        }

        public StateSave GetState(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public StateSaveCategory GetStateCategory(string containerName, string name)
        {
            throw new NotImplementedException();
        }
    }
}
