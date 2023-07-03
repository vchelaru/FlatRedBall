using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers;
using NAudioPlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace NAudioPlugin
{
    [Export(typeof(PluginBase))]
    public class MainNAudioPlugin : PluginBase
    {
        public override string FriendlyName => "NAduio Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            base.ShutDown(shutDownReason);
            return true;
        }

        public override void StartUp()
        {
            RegisterCodeGenerator(new ElementCodeGenerator());

            AddMenuItemTo("Embed NAudio Classes", HandleEmbedNAudioFiles, "Content");

            this.ReactToLoadedGluxEarly += HandleGluxLoadedEarly;
        }

        private void HandleGluxLoadedEarly()
        {
            // Do this on every glux load so that we can add the ati according to the glux version #
            Managers.AssetTypeInfoManager.ResetAssetTypes();

        }

        private void HandleEmbedNAudioFiles(object sender, EventArgs e)
        {
            TaskManager.Self.Add(() =>
            {
                var codeItemAdder = new CodeBuildItemAdder();
                codeItemAdder.OutputFolderInProject = "NAudio";
                var thisAssembly = this.GetType().Assembly;

                codeItemAdder.AddFolder("NAudioPlugin/Embedded", thisAssembly);

                codeItemAdder.PerformAddAndSaveTask(thisAssembly);

                var nugetPackageName = "NAudio";
                GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded(nugetPackageName, "2.1.0");

                GlueCommands.Self.ProjectCommands.SaveProjects();

            }, nameof(HandleEmbedNAudioFiles));

        }
    }
}
