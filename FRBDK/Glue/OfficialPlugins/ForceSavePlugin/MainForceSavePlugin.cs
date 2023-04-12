using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ForceSavePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainForceSavePlugin : PluginBase
    {
        public override string FriendlyName => "Force Save Plugin";

        public override void StartUp()
        {
            this.AddMenuItemTo("Force Save Project", HandleForceSaveProject, "Project");
        }

        private void HandleForceSaveProject(object sender, EventArgs e)
        {
            if(GlueState.Self.CurrentGlueProject != null)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }
    }
}
