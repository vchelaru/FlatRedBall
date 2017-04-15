using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using System.Diagnostics;

namespace OfficialPlugins.MonoGameContent
{
    [Export(typeof (PluginBase))]
    public class MainPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get
            {
                return "MonoGame Content Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToLoadedGlux += HandleLoadedGlux;
            this.ReactToLoadedSyncedProject += HandleLoadedSyncedProject;
            this.ReactToNewFileHandler += HandleNewFile;
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {
            if(GlueState.Self.CurrentMainProject is DesktopGlProject)
            {
                BuildLogic.Self.TryHandleReferencedFile(GlueState.Self.CurrentMainProject, newFile);
            }
            foreach(var project in GlueState.Self.SyncedProjects)
            {
                if(project is DesktopGlProject)
                {
                    BuildLogic.Self.TryHandleReferencedFile(project, newFile);
                }
            }
        }

        private void HandleLoadedGlux()
        {
            BuildLogic.Self.RefreshBuiltFilesFor(GlueState.Self.CurrentMainProject);
        }

        private void HandleLoadedSyncedProject(ProjectBase project)
        {
            BuildLogic.Self.RefreshBuiltFilesFor(project);
        }

        private void HandleFileChanged(string fileName)
        {
            // todo - build?
        }
    }
}
