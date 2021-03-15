using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Navigation;

namespace OfficialPluginsCore.AnimationChainPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAnimationChainPlugin : PluginBase
    {
        public override string FriendlyName => "Animation Chain Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ReactToNewFileHandler += HandleNewFile;
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {
            var extension = FileManager.GetExtension(newFile.Name);
            if(extension == "achx")
            {

            }
        }
    }
}
