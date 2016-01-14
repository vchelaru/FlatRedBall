using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace PluginTestbed.SceneProgramChooser
{
    public partial class SceneProgramChooserPlugin
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public string FriendlyName
        {
            get { return "Scene Program Chooser Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
