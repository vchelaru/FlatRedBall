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
        #region Fields/Properties

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

        #endregion

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
            this.ReactToReferencedFileChangedValueHandler += HandleReferencedFileValueChanged;
        }

        private void HandleReferencedFileValueChanged(string memberName, object oldValue)
        {
            if(memberName == nameof(ReferencedFileSave.UseContentPipeline))
            {
                var rfs = GlueState.Self.CurrentReferencedFileSave;
                BuildLogic.Self.TryHandleReferencedFile(GlueState.Self.CurrentMainProject, rfs);

                foreach(var syncedProject in GlueState.Self.SyncedProjects)
                {
                    BuildLogic.Self.TryHandleReferencedFile(syncedProject, rfs);
                }
            }
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {

            if(GetIfShouldBuild( GlueState.Self.CurrentMainProject ))
            {
                BuildLogic.Self.TryHandleReferencedFile(GlueState.Self.CurrentMainProject, newFile);
            }
            foreach(var project in GlueState.Self.SyncedProjects)
            {
                if(GetIfShouldBuild( project ))
                {
                    BuildLogic.Self.TryHandleReferencedFile(project, newFile);
                }
            }
        }

        private bool GetIfShouldBuild(ProjectBase project)
        {
            return project is DesktopGlProject;
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
